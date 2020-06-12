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

    private Dictionary<Vector3, Chunk> chunks;
    private Queue<Action> tasks;
    public Queue<Chunk> activeQueue;
    private Queue<Chunk> despawnQueue;
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
        semaphore = new SemaphoreSlim (4, 4);

        // ChunksManager ();
        ChunksMeshQueue ();
    }

    // async void ChunksManager () {
    //     await factory.StartNew (() => {
    //         while (!stop) {
    //             Vector3 localPos = position;
    //             for (int x = 0; x < spawnRadius; x++)
    //                 for (int y = 0; y < spawnRadius; y++)
    //                     for (int z = 0; z < spawnRadius; z++) {
    //                         //Octant I
    //                         Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosI) && chunks.TryGetValue (chunkPosI, out var chunk))
    //                             ManageChunk (chunk, localPos);

    //                         // ManageChunk (chunks[chunkPosI]);

    //                         //Octant II
    //                         Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosII) && chunks.TryGetValue (chunkPosII, out var chunk1))
    //                             // ManageChunk (chunks[chunkPosII]);
    //                             ManageChunk (chunk1, localPos);

    //                         //Octant III
    //                         Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosIII) && chunks.TryGetValue (chunkPosIII, out var chunk2))
    //                             // ManageChunk (chunks[chunkPosIII]);

    //                             ManageChunk (chunk2, localPos);

    //                         //Octant IV
    //                         Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosIV) && chunks.TryGetValue (chunkPosIV, out var chunk3))
    //                             // ManageChunk (chunks[chunkPosIV]);
    //                             ManageChunk (chunk3, localPos);

    //                         //Octant V
    //                         Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosV) && chunks.TryGetValue (chunkPosV, out var chunk4))
    //                             // ManageChunk (chunks[chunkPosV]);
    //                             ManageChunk (chunk4, localPos);

    //                         //Octant VI
    //                         Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosVI) && chunks.TryGetValue (chunkPosVI, out var chunk5))
    //                             // ManageChunk (chunks[chunkPosVI]);
    //                             ManageChunk (chunk5, localPos);

    //                         //Octant VII
    //                         Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosVII) && chunks.TryGetValue (chunkPosVII, out var chunk6))
    //                             // ManageChunk (chunks[chunkPosVII]);
    //                             ManageChunk (chunk6, localPos);

    //                         //Octant VIII
    //                         Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                         if (chunks.ContainsKey (chunkPosVIII) && chunks.TryGetValue (chunkPosVIII, out var chunk7))
    //                             // ManageChunk (chunks[chunkPosVIII]);
    //                             ManageChunk (chunk7, localPos);

    //                     }
    //         }
    //     }, TaskCreationOptions.LongRunning);
    // }

    private bool ManageChunk (Chunk chunk) {
        if (chunk == null)
            return false;
        if (chunk.IsDisposed == 1)
            return false;
        Vector3 dist = chunk.Position - position;

        if (dist.sqrMagnitude > sqrInactiveDist && !chunk.IfDespawn) {
            // lock (chunk)
            chunk.IfDespawn = true;
            despawnQueue.Enqueue (chunk);

            // isModified = true;
            return false;
            // yield return null;
        }

        if (dist.sqrMagnitude <= sqrInactiveDist) {
            // lock (chunk)
            chunk.IfDespawn = false;
        }

        // if (chunk.IsGenerated == 0) {
        //     lock (densitiesQueue)
        //     densitiesQueue.Enqueue (chunk);
        // }
        if (chunk.IsMeshed == 0) {
            chunk.IsMeshed = 1;
            meshQueue.Enqueue (chunk);
            // Debug.Log("mesh Enqueue in Manage");
        }

        if (chunk.IsMeshed == 2 && !chunk.IfActive) {
            // lock (chunk)
            chunk.IfActive = true;
            chunk.gameObject.SetActive (true);
            // activeQueue.Enqueue (chunk);
            // Debug.Log ($"Enqueue Success {Thread.CurrentThread.ManagedThreadId}");
        }
        return false;
    }

    async void ChunksMeshQueue () {
        await factory.StartNew (async () => {
            while (!stop) {
                // Debug.Log ("mesh " + meshQueue.Count);
                if (meshQueue.Count < 1)
                    continue;
                meshQueue.OrderBy (sortChunk => (position - sortChunk.Position).sqrMagnitude);
                if (meshQueue.TryDequeue (out var chunk) && chunk != null && chunk.IsMeshed == 1)
                    tasks.Enqueue (() => {
                        semaphore.Wait ();
                        chunk.BuildMesh ();
                        semaphore.Release ();
                    });

                if (semaphore.CurrentCount > 0 && tasks.Count > 0)
                    await Task.Run(tasks.Dequeue(), cts.Token);
                // chunk.BuildMesh ();
            }
        }, TaskCreationOptions.LongRunning);

    }

    void Start () {
        SimplePool.Preload (chunkPrefab, spawnRadius * spawnRadius * spawnRadius * 8);
    }

    private void OnEnable () {
        sqrActiveDist = (spawnRadius >> 1) * Chunk.ChunkSize * (spawnRadius >> 1) * Chunk.ChunkSize;
        sqrInactiveDist = spawnRadius * Chunk.ChunkSize * spawnRadius * Chunk.ChunkSize;
        // StartCoroutine (InstantiateChunks ());
        // StartCoroutine (ActivePlayerChunks ());
        StartCoroutine (DespawnChunks ());
    }

    IEnumerator DespawnChunks () {
        while (this.gameObject.activeSelf) {
            if (despawnQueue.Count < 1)
                goto despawnSkip;

            // while (despawnQueue.Count > 0)
            var chunk = despawnQueue.Dequeue ();
            if (chunk != null && chunk.IfDespawn) {
                chunk.Dispose ();
                SimplePool.Despawn (chunk.gameObject);
                chunks.Remove (chunk.transform.position);
                Debug.Log ("Despawn");
            }

            despawnSkip:
                yield return null;
        }
    }

    // IEnumerator ActivePlayerChunks () {
    //     while (this.gameObject.activeSelf) {
    //         // if (activeQueue.Count < 1)
    //         //     goto activeSkip;

    //         // // while (activeQueue.Count > 0)

    //         // activeQueue.OrderBy (sortChunk => (position - sortChunk.Position).sqrMagnitude);
    //         // if (activeQueue.TryDequeue (out var chunk) && chunk != null && chunk.IfActive) {
    //         //     Debug.Log ("Dequeue Success");
    //         //     lock (chunk)
    //         //     chunk.gameObject.SetActive (true);
    //         // }

    //         // activeSkip:
    //         //     yield return null;
    //         for (int x = 0; x < 2; x++) {
    //             for (int y = 0; y < 2; y++) {
    //                 for (int z = 0; z < 2; z++) {
    //                     //Octant I
    //                     Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosI))
    //                         InstantiateChunk (chunkPosI);

    //                     //Octant II
    //                     Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosII))
    //                         InstantiateChunk (chunkPosII);

    //                     //Octant III
    //                     Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosIII))
    //                         InstantiateChunk (chunkPosIII);

    //                     //Octant IV
    //                     Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosIV))
    //                         InstantiateChunk (chunkPosIV);

    //                     //Octant V
    //                     Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosV))
    //                         InstantiateChunk (chunkPosV);

    //                     //Octant VI
    //                     Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosVI))
    //                         InstantiateChunk (chunkPosVI);

    //                     //Octant VII
    //                     Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosVII))
    //                         InstantiateChunk (chunkPosVII);

    //                     //Octant VIII
    //                     Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                     if (!chunks.ContainsKey (chunkPosVIII))
    //                         InstantiateChunk (chunkPosVIII);

    //                 }
    //             }
    //         }
    //         yield return null;
    //     }
    // }

    // IEnumerator InstantiateChunks () {
    //     while (this.gameObject.activeSelf) {
    //         Vector3 localPos = position;
    //         // Debug.Log($"position {position}");
    //         // Debug.Log($"lastPosition {lastPosition}");
    //         // Debug.Log ("mesh " + meshQueue.Count);
    //         if (localPos == lastPosition)
    //             goto skip;

    //         lastPosition = localPos;

    //         //因为生成范围是以position为中心的球体，所以只循环第一卦限中的点并利用对称特性获得其他点
    //         for (int x = 0; x < spawnRadius; x++) {
    //             for (int y = 0; y < spawnRadius; y++) {
    //                 for (int z = 0; z < spawnRadius; z++) {
    //                     //Octant I
    //                     Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosI))
    //                         InstantiateChunk (chunkPosI);

    //                     //Octant II
    //                     Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosII))
    //                         InstantiateChunk (chunkPosII);

    //                     //Octant III
    //                     Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosIII))
    //                         InstantiateChunk (chunkPosIII);

    //                     //Octant IV
    //                     Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosIV))
    //                         InstantiateChunk (chunkPosIV);

    //                     //Octant V
    //                     Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosV))
    //                         InstantiateChunk (chunkPosV);

    //                     //Octant VI
    //                     Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosVI))
    //                         InstantiateChunk (chunkPosVI);

    //                     //Octant VII
    //                     Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosVII))
    //                         InstantiateChunk (chunkPosVII);

    //                     //Octant VIII
    //                     Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
    //                         new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + localPos;
    //                     if (!chunks.ContainsKey (chunkPosVIII))
    //                         InstantiateChunk (chunkPosVIII);

    //                 }
    //             }
    //         }

    //         skip:
    //             yield return null;
    //     }
    //     // }
    // }

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

        for (int x = 0; x < spawnRadius; x++) {
            for (int y = 0; y < spawnRadius; y++) {
                for (int z = 0; z < spawnRadius; z++) {
                    //Octant I
                    Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosI))
                        ManageChunk (chunks[chunkPosI]);
                    else if ((chunkPosI - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosI);

                    //Octant II
                    Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosII))
                        ManageChunk (chunks[chunkPosII]);
                    else if ((chunkPosII - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosII);

                    //Octant III
                    Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosIII))
                        ManageChunk (chunks[chunkPosIII]);
                    else if ((chunkPosIII - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosIII);

                    //Octant IV
                    Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosIV))
                        ManageChunk (chunks[chunkPosIV]);
                    else if ((chunkPosIV - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosIV);

                    //Octant V
                    Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosV))
                        ManageChunk (chunks[chunkPosV]);
                    else if ((chunkPosV - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosV);

                    //Octant VI
                    Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosVI))
                        ManageChunk (chunks[chunkPosVI]);
                    else if ((chunkPosVI - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosVI);

                    //Octant VII
                    Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosVII))
                        ManageChunk (chunks[chunkPosVII]);
                    else if ((chunkPosVII - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosVII);

                    //Octant VIII
                    Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosVIII))
                        ManageChunk (chunks[chunkPosVIII]);
                    else if ((chunkPosVIII - position).sqrMagnitude < sqrInactiveDist)
                        InstantiateChunk (chunkPosVIII);

                }
            }
        }

    }

    private void InstantiateChunk (Vector3 chunkPos) {
        if (chunks.ContainsKey (chunkPos))
            return;

        GameObject newChunk = SimplePool.Spawn (chunkPrefab, chunkPos, Quaternion.identity, false);
        newChunk.transform.SetParent (world.gameObject.transform);
        Chunk chunk = newChunk.GetComponent<Chunk> ();
        if (chunk == null)
            chunk = newChunk.AddComponent<Chunk> ();
        chunk.Lod = chunkLod;
        chunk.Position = chunkPos;
        chunk.Fractal = fractal;
        chunk.Marching = marching;
        chunk.Material = chunkMaterial;
        chunk.IfDespawn = false;
        chunk.IfActive = false;
        chunk.IsMeshed = 0;
        chunk.IsDisposed = 0;

#if UNITY_EDITOR
        newChunk.name = $"Chunk {chunkPos.x}, {chunkPos.y}, {chunkPos.z} IsMeshed{chunk.IsMeshed}";
#endif
        chunks.Add (chunkPos, chunk);
        chunk.IsMeshed = 1;
        meshQueue.Enqueue (chunk);
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
