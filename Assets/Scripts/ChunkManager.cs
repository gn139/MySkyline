using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Algorithm;

using UnityEngine;

public class ChunkManager : MonoBehaviour {
    public int spawnRadius = 8;
    public GameObject chunkPrefab;
    public int chunkLod = 1;
    public float surface = 0.01f;
    public Material chunkMaterial;

    public Dictionary<Vector3, Chunk> chunks;
    private Queue<Action> tasks;
    public Queue<Chunk> activeQueue;
    public Queue<Chunk> despawnQueue;
    public ConcurrentQueue<Chunk> meshQueue;
    private FractalNoise fractal;
    private MarchingCubes marching;
    private Vector3 position;
    private Vector3 lastPosition = new Vector3 (float.MinValue, float.MinValue, float.MinValue);
    private GameObject world;
    private int chunksNum;
    private float sqrActiveDist;
    private float sqrInactiveDist;
    private CancellationTokenSource cts;
    private TaskFactory factory;
    private static SemaphoreSlim semaphore;

    // private bool isModified = false;
    private bool stop = false;
    void Awake () {

        world = GameObject.FindGameObjectWithTag ("World");
        chunksNum = spawnRadius * spawnRadius * spawnRadius;
        chunks = new Dictionary<Vector3, Chunk> (chunksNum);
        // chunksKeysList = new List<Vector3> (chunksNum);
        fractal = new FractalNoise (new PerlinNoise (), 3);
        marching = new MarchingCubes (surface);
        marching.Lod = chunkLod;

        tasks = new Queue<Action> ();
        activeQueue = new Queue<Chunk> ();
        meshQueue = new ConcurrentQueue<Chunk> ();
        despawnQueue = new Queue<Chunk> ();
        cts = new CancellationTokenSource ();
        factory = new TaskFactory (cts.Token);
        semaphore = new SemaphoreSlim (6, 6);

        // ChunksManager ();
        ChunksMeshQueue ();
    }

    // async void ChunksManager () {
    //     await factory.StartNew (() => {
    //         while (!stop) {
    //             if (chunks.Count < 1)
    //                 continue;
    //             var chunksList = chunks.ToList ();
    //             foreach (var pair in chunksList)
    //                 ManageChunk (pair.Value);
    //         }
    //     }, TaskCreationOptions.LongRunning);
    // }

    private void ManageChunk (Chunk chunk) {
        if (chunk == null)
            return;
        Vector3 dist = chunk.Position - position;
        switch (chunk.State) {
        case ChunkState.Created:
            meshQueue.Enqueue (chunk);
            chunk.State = ChunkState.Processing;
            break;
        case ChunkState.Processing:
            return;
        case ChunkState.Processed:
            if (dist.sqrMagnitude <= sqrInactiveDist) {
                // Debug.Log ("activeQueue Enqueue");
                chunk.State = ChunkState.InActive;
                activeQueue.Enqueue (chunk);
            }
            break;
        case ChunkState.InActive:
            if (dist.sqrMagnitude > sqrInactiveDist) {
                chunk.State = ChunkState.Disposing;
                despawnQueue.Enqueue (chunk);
            }
            break;
        case ChunkState.Active:
            if (dist.sqrMagnitude > sqrInactiveDist) {
                chunk.State = ChunkState.Disposing;
                despawnQueue.Enqueue (chunk);
            }
            break;
        case ChunkState.Disposing:
            if (dist.sqrMagnitude <= sqrInactiveDist)
                chunk.State = ChunkState.Active;
            break;
        case ChunkState.Disposed:
            return;
        }
    }

    async void ChunksMeshQueue () {
        await factory.StartNew (async () => {
            while (!stop) {
                // Debug.Log ("mesh " + meshQueue.Count);

                // meshQueue.OrderBy (sortChunk => (position - sortChunk.Position).sqrMagnitude);
#if UNITY_ANDROID
                if (meshQueue.Count < 1 && tasks.Count < 1)
                    continue;
                if (meshQueue.TryDequeue (out var chunk) && chunk != null)
                    tasks.Enqueue (() => {
                        semaphore.Wait ();
                        chunk.BuildMesh ();
                        semaphore.Release ();
                    });

                if (semaphore.CurrentCount > 0 && tasks.Count > 0)
                    await Task.Run (tasks.Dequeue (), cts.Token);
#endif

                // #if UNITY_EDITOR
                //                 if (meshQueue.TryDequeue (out var chunk) && chunk != null)
                //                     chunk.BuildMesh ();
                // #endif
            }
        }, TaskCreationOptions.LongRunning);

    }

    void Start () {
        SimplePool.Preload (chunkPrefab, spawnRadius * spawnRadius * spawnRadius * 8);
    }

    private void OnEnable () {
        sqrActiveDist = (spawnRadius >> 1) * Chunk.ChunkSize * (spawnRadius >> 1) * Chunk.ChunkSize;
        sqrInactiveDist = spawnRadius * Chunk.ChunkSize * spawnRadius * Chunk.ChunkSize;
        StartCoroutine (InstantiateChunks ());
        StartCoroutine (ManageAllChunks ());
        StartCoroutine (ActivePlayerChunks ());
        StartCoroutine (DespawnChunks ());
    }

    IEnumerator DespawnChunks () {
        while (this.gameObject.activeSelf) {
            if (despawnQueue.Count < 1)
                goto despawnSkip;

            // while (despawnQueue.Count > 0)
            var chunk = despawnQueue.Dequeue ();
            if (chunk != null && chunk.State == ChunkState.Disposing) {
                chunk.Dispose ();
                chunks.Remove (chunk.transform.position);
                SimplePool.Despawn (chunk.gameObject);
                // Destroy (chunk.gameObject);
                // Debug.Log ("Despawn");
            }

            despawnSkip:
                yield return null;
        }
    }

    IEnumerator ActivePlayerChunks () {
        while (this.gameObject.activeSelf) {
            if (activeQueue.Count < 1)
                goto activeSkip;

            // while (activeQueue.Count > 0)
            var chunk = activeQueue.Dequeue ();
            // Debug.Log ("activeQueue dequeue");
            if (chunk != null && chunk.State == ChunkState.InActive) {
                chunk.State = ChunkState.Active;
                chunk.gameObject.SetActive (true);
            }

            activeSkip:
                yield return null;

        }
    }

    IEnumerator ManageAllChunks () {
        while (this.gameObject.activeSelf) {
            foreach (var pair in chunks)
                ManageChunk (pair.Value);
            yield return null;
        }
    }

    IEnumerator InstantiateChunks () {
        while (this.gameObject.activeSelf) {
            Vector3 localPos = position;
            // Debug.Log($"position {position}");
            // Debug.Log($"lastPosition {lastPosition}");
            // Debug.Log ("mesh " + meshQueue.Count);
            if (localPos == lastPosition)
                goto skip;

            lastPosition = localPos;

            //因为生成范围是以position为中心的球体，所以只循环第一卦限中的点并利用对称特性获得其他点
            for (int x = 0; x < spawnRadius; x++) {
                for (int y = 0; y < spawnRadius; y++) {
                    for (int z = 0; z < spawnRadius; z++) {
                        //Octant I
                        Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosI))
                        //     ManageChunk (chunks[chunkPosI]);
                        if (!chunks.ContainsKey (chunkPosI) && (chunkPosI - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosI);

                        //Octant II
                        Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosII))
                        //     ManageChunk (chunks[chunkPosII]);
                        if (!chunks.ContainsKey (chunkPosII) && (chunkPosII - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosII);

                        //Octant III
                        Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosIII))
                        //     ManageChunk (chunks[chunkPosIII]);
                        if (!chunks.ContainsKey (chunkPosIII) && (chunkPosIII - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosIII);

                        //Octant IV
                        Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosIV))
                        //     ManageChunk (chunks[chunkPosIV]);
                        if (!chunks.ContainsKey (chunkPosIV) && (chunkPosIV - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosIV);

                        //Octant V
                        Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosV))
                        //     ManageChunk (chunks[chunkPosV]);
                        if (!chunks.ContainsKey (chunkPosV) && (chunkPosV - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosV);

                        //Octant VI
                        Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosVI))
                        //     ManageChunk (chunks[chunkPosVI]);
                        if (!chunks.ContainsKey (chunkPosVI) && (chunkPosVI - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosVI);

                        //Octant VII
                        Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosVII))
                        //     ManageChunk (chunks[chunkPosVII]);
                        if (!chunks.ContainsKey (chunkPosVII) && (chunkPosVII - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosVII);

                        //Octant VIII
                        Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
                        // if (chunks.ContainsKey (chunkPosVIII))
                        //     ManageChunk (chunks[chunkPosVIII]);
                        if (!chunks.ContainsKey (chunkPosVIII) && (chunkPosVIII - localPos).sqrMagnitude < sqrInactiveDist)
                            InstantiateChunk (chunkPosVIII);

                    }
                }
            }

            skip:
                yield return null;
        }
        // }
    }

    void Update () {

        position = ToChunkSpace (transform.position);
        // Debug.Log($"position {position}");
        // Debug.Log($"lastPosition {lastPosition}");
        // Debug.Log ("mesh " + meshQueue.Count);
        // Debug.Log ("activeQueue " + activeQueue.Count);
        // if (position == lastPosition)
        //     return;

        // lastPosition = position;
        //因为生成范围是以position为中心的球体，所以只循环第一卦限中的点并利用对称特性获得其他点

        // for (int x = 0; x < 2; x++) {
        //     for (int y = 0; y < 2; y++) {
        //         for (int z = 0; z < 2; z++) {
        //             //Octant I
        //             Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosI))
        //                 HandleNearChunk (chunks[chunkPosI]);
        //             else if ((chunkPosI - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosI);

        //             //Octant II
        //             Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosII))
        //                 HandleNearChunk (chunks[chunkPosI]);
        //             else if ((chunkPosII - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosII);

        //             //Octant III
        //             Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosIII))
        //                 HandleNearChunk (chunks[chunkPosI]);
        //             else if ((chunkPosIII - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosIII);

        //             //Octant IV
        //             Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosIV))
        //                 HandleNearChunk (chunks[chunkPosIV]);
        //             else if ((chunkPosIV - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosIV);

        //             //Octant V
        //             Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosV))
        //                 HandleNearChunk (chunks[chunkPosV]);
        //             else if ((chunkPosV - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosV);

        //             //Octant VI
        //             Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosVI))
        //                 HandleNearChunk (chunks[chunkPosVI]);
        //             else if ((chunkPosVI - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosVI);

        //             //Octant VII
        //             Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosVII))
        //                 HandleNearChunk (chunks[chunkPosVII]);
        //             else if ((chunkPosVII - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosVII);

        //             //Octant VIII
        //             Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
        //                 new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
        //             if (chunks.ContainsKey (chunkPosVIII))
        //                 HandleNearChunk (chunks[chunkPosVIII]);
        //             else if ((chunkPosVIII - position).sqrMagnitude < sqrInactiveDist)
        //                 InstantiateChunk (chunkPosVIII);

        //         }
        //     }
        // }

    }

    private void HandleNearChunk (Chunk chunk) {
        switch (chunk.State) {
        case ChunkState.Created:
            meshQueue.Enqueue (chunk);
            chunk.State = ChunkState.Processing;
            break;
        case ChunkState.Processing:
            meshQueue.OrderBy (sortChunk => (position - sortChunk.Position).sqrMagnitude);
            break;
        case ChunkState.Processed:
            chunk.State = ChunkState.InActive;
            break;
        case ChunkState.InActive:
            activeQueue.OrderBy (sortChunk => (position - sortChunk.Position).sqrMagnitude);
            break;
        }

    }

    private void InstantiateChunk (Vector3 chunkPos) {
        if (chunks.ContainsKey (chunkPos))
            return;

        GameObject newChunk = SimplePool.Spawn (chunkPrefab, chunkPos, Quaternion.identity, false);
        // GameObject newChunk = new GameObject ();
        newChunk.transform.position = chunkPos;
        newChunk.transform.SetParent (world.gameObject.transform);
        // newChunk.SetActive(false);
        Chunk chunk = newChunk.GetComponent<Chunk> ();
        if (chunk == null)
            chunk = newChunk.AddComponent<Chunk> ();
        chunk.Lod = chunkLod;
        chunk.Position = chunkPos;
        chunk.Fractal = fractal;
        chunk.Marching = marching;
        chunk.Material = chunkMaterial;
        chunk.State = ChunkState.Created;

#if UNITY_EDITOR
        newChunk.name = $"Chunk {chunkPos.x}, {chunkPos.y}, {chunkPos.z} State {chunk.State}";
#endif
        chunks.Add (chunkPos, chunk);
        // chunk.IsMeshed = 1;
        // meshQueue.Enqueue (chunk);
        // Debug.Log("mesh Enqueue in Instantiate");
    }

    private void OnDisable () {
        StopAllCoroutines ();
        stop = true;
        cts.Cancel ();
    }

    private void OnDestroy () {

    }

    private void OnApplicationQuit () {
        stop = true;
        cts.Cancel ();
    }
    public Vector3 ToChunkSpace (Vector3 Vec3) {
        int chunkX = (int) Vec3.x >> Chunk.BitShift;
        int chunkY = (int) Vec3.y >> Chunk.BitShift;
        int chunkZ = (int) Vec3.z >> Chunk.BitShift;

        chunkX *= Chunk.ChunkSize;
        chunkY *= Chunk.ChunkSize;
        chunkZ *= Chunk.ChunkSize;

        return new Vector3 (chunkX, chunkY, chunkZ);
    }
}
