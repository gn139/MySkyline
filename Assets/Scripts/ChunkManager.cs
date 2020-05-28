using System.Collections;
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
    // private List<Vector3> chunksKeysList;
    // private Queue<Chunk> densitiesQueue;
    private Queue<Chunk> meshQueue;
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
    // private bool isModified = false;
    void Awake () {

        world = GameObject.FindGameObjectWithTag ("World");
        chunksNum = spawnRadius * spawnRadius * spawnRadius;
        chunks = new Dictionary<Vector3, Chunk> (chunksNum);
        // chunksKeysList = new List<Vector3> (chunksNum);
        fractal = new FractalNoise (new PerlinNoise (), 3);
        marching = new MarchingCubes (surface);
        marching.Lod = chunkLod;
        // densitiesQueue = new Queue<Chunk> ();
        meshQueue = new Queue<Chunk> ();
        cts = new CancellationTokenSource ();
        factory = new TaskFactory (cts.Token);
        // ChunksManager ();
        ChunksMeshQueue ();
    }

    // async void ChunksManager () {
    //     await factory.StartNew (() => {
    //         while (true) {
    //             for (int x = 0; x < spawnRadius; x++)
    //                 for (int y = 0; y < spawnRadius; y++)
    //                     for (int z = 0; z < spawnRadius; z++) {
    //                         //Octant I
    //                         Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosI))
    //                             // ManageChunk (chunks[chunkPosI]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => ManageChunk (chunks[chunkPosI]));
    //                         else
    //                             // InstantiateChunk (chunkPosI);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosI));

    //                         //Octant II
    //                         Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosII))
    //                             // ManageChunk (chunks[chunkPosII]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => ManageChunk (chunks[chunkPosII]));
    //                         else
    //                             // InstantiateChunk (chunkPosII);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosII));

    //                         //Octant III
    //                         Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosIII))
    //                             // ManageChunk (chunks[chunkPosIII]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => ManageChunk (chunks[chunkPosIII]));
    //                         else
    //                             // InstantiateChunk (chunkPosIII);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosIII));

    //                         //Octant IV
    //                         Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosIV))
    //                             // ManageChunk (chunks[chunkPosIV]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => ManageChunk (chunks[chunkPosIV]));
    //                         else
    //                             // InstantiateChunk (chunkPosIV);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosIV));

    //                         //Octant V
    //                         Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosV))
    //                             // ManageChunk (chunks[chunkPosV]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => ManageChunk (chunks[chunkPosV]));
    //                         else
    //                             // InstantiateChunk (chunkPosV);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosV));

    //                         //Octant VI
    //                         Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosVI))
    //                             // ManageChunk (chunks[chunkPosVI]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => ManageChunk (chunks[chunkPosVI]));
    //                         else
    //                             // InstantiateChunk (chunkPosVI);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosV));

    //                         //Octant VII
    //                         Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosVII))
    //                             // ManageChunk (chunks[chunkPosVII]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosVII));
    //                         else
    //                             // InstantiateChunk (chunkPosVII);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosVII));

    //                         //Octant VIII
    //                         Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
    //                             new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
    //                         if (chunks.ContainsKey (chunkPosVIII))
    //                             // ManageChunk (chunks[chunkPosVIII]);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosVIII));
    //                         else
    //                             // InstantiateChunk (chunkPosVIII);
    //                             UnityMainThreadDispatcher.Instance ().Enqueue (() => InstantiateChunk (chunkPosVIII));
    //                     }
    //         }
    //     }, TaskCreationOptions.LongRunning);
    // }

    async void ChunksMeshQueue () {
        await factory.StartNew (() => {
            while (true) {
                // Debug.Log ("mesh " + meshQueue.Count);
                if (meshQueue.Count < 1)
                    continue;
                Chunk chunk = null;
                lock (meshQueue)
                chunk = meshQueue.Dequeue ();
                if (chunk != null && chunk.IsMeshed == 1)
                    chunk.BuildMesh ();

            }
        }, TaskCreationOptions.LongRunning);

    }

    void Start () {
        SimplePool.Preload (chunkPrefab, spawnRadius * spawnRadius * spawnRadius);
    }

    private void OnEnable () {
        sqrActiveDist = (spawnRadius >> 1) * Chunk.ChunkSize * (spawnRadius >> 1) * Chunk.ChunkSize;
        sqrInactiveDist = spawnRadius * Chunk.ChunkSize * spawnRadius * Chunk.ChunkSize;
        // StartCoroutine (ManageChunks ());
    }

    IEnumerator ManageChunks () {
        while (this.gameObject.activeSelf) {
            position = ToChunkSpace (transform.position);
            // Debug.Log($"position {position}");
            // Debug.Log($"lastPosition {lastPosition}");
            // Debug.Log ("mesh " + meshQueue.Count);
            // if (position == lastPosition)
            //     return;

            //因为生成范围是以position为中心的球体，所以只循环第一卦限中的点并利用对称特性获得其他点
            for (int x = 0; x < spawnRadius; x++) {
                for (int y = 0; y < spawnRadius; y++) {
                    for (int z = 0; z < spawnRadius; z++) {
                        //Octant I
                        Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosI) && ManageChunk (chunks[chunkPosI]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosI))
                            InstantiateChunk (chunkPosI);

                        //Octant II
                        Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosII) && ManageChunk (chunks[chunkPosII]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosII))
                            InstantiateChunk (chunkPosII);

                        //Octant III
                        Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosIII) && ManageChunk (chunks[chunkPosIII]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosIII))
                            InstantiateChunk (chunkPosIII);

                        //Octant IV
                        Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosIV) && ManageChunk (chunks[chunkPosIV]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosIV))
                            InstantiateChunk (chunkPosIV);

                        //Octant V
                        Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosV) && ManageChunk (chunks[chunkPosV]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosV))
                            InstantiateChunk (chunkPosV);

                        //Octant VI
                        Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosVI) && ManageChunk (chunks[chunkPosVI]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosVI))
                            InstantiateChunk (chunkPosVI);

                        //Octant VII
                        Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosVII) && ManageChunk (chunks[chunkPosVII]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosVII))
                            InstantiateChunk (chunkPosVII);

                        //Octant VIII
                        Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
                            new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                        if (chunks.ContainsKey (chunkPosVIII) && ManageChunk (chunks[chunkPosVIII]))
                            yield return null;
                        else if (!chunks.ContainsKey (chunkPosVIII))
                            InstantiateChunk (chunkPosVIII);

                    }
                }
            }
        }
        yield return null;
        // }
    }

    void Update () {

        position = ToChunkSpace (transform.position);
        // Debug.Log($"position {position}");
        // Debug.Log($"lastPosition {lastPosition}");
        // Debug.Log ("mesh " + meshQueue.Count);
        // Debug.Log ("densities " + densitiesQueue.Count);
        // if (position == lastPosition)
        //     return;

        //因为生成范围是以position为中心的球体，所以只循环第一卦限中的点并利用对称特性获得其他点

        for (int x = 0; x < spawnRadius; x++) {
            for (int y = 0; y < spawnRadius; y++) {
                for (int z = 0; z < spawnRadius; z++) {
                    //Octant I
                    Vector3 chunkPosI = Vector3.Scale (new Vector3 (x, y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosI))
                        ManageChunk (chunks[chunkPosI]);
                    else
                        InstantiateChunk (chunkPosI);

                    //Octant II
                    Vector3 chunkPosII = Vector3.Scale (new Vector3 (-x, y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosII))
                        ManageChunk (chunks[chunkPosII]);
                    else
                        InstantiateChunk (chunkPosII);

                    //Octant III
                    Vector3 chunkPosIII = Vector3.Scale (new Vector3 (-x, -y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosIII))
                        ManageChunk (chunks[chunkPosIII]);
                    else
                        InstantiateChunk (chunkPosIII);

                    //Octant IV
                    Vector3 chunkPosIV = Vector3.Scale (new Vector3 (x, -y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosIV))
                        ManageChunk (chunks[chunkPosIV]);
                    else
                        InstantiateChunk (chunkPosIV);

                    //Octant V
                    Vector3 chunkPosV = Vector3.Scale (new Vector3 (x, y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosV))
                        ManageChunk (chunks[chunkPosV]);
                    else
                        InstantiateChunk (chunkPosV);

                    //Octant VI
                    Vector3 chunkPosVI = Vector3.Scale (new Vector3 (-x, y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosVI))
                        ManageChunk (chunks[chunkPosVI]);
                    else
                        InstantiateChunk (chunkPosVI);

                    //Octant VII
                    Vector3 chunkPosVII = Vector3.Scale (new Vector3 (-x, -y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosVII))
                        ManageChunk (chunks[chunkPosVII]);
                    else
                        InstantiateChunk (chunkPosVII);

                    //Octant VIII
                    Vector3 chunkPosVIII = Vector3.Scale (new Vector3 (x, -y, -z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize)) + position;
                    if (chunks.ContainsKey (chunkPosVIII))
                        ManageChunk (chunks[chunkPosVIII]);
                    else
                        InstantiateChunk (chunkPosVIII);

                }
            }
        }

        // lastPosition = position;

    }

    private bool ManageChunk (Chunk chunk) {
        if (chunk.IsDisposed == 1)
            return false;
        Vector3 dist = chunk.transform.position - this.transform.position;
        if (dist.sqrMagnitude > sqrActiveDist) {
            chunk.gameObject.SetActive (false);
            return false;
        }
        // isModified = false;
        if (dist.sqrMagnitude > sqrInactiveDist) {
            chunk.Dispose ();
            SimplePool.Despawn (chunk.gameObject);
            chunks.Remove (chunk.transform.position);
            // isModified = true;
            return false;
            // yield return null;
        }

        // if (chunk.IsGenerated == 0) {
        //     lock (densitiesQueue)
        //     densitiesQueue.Enqueue (chunk);
        // }
        if (chunk.IsMeshed == 0) {
            lock (meshQueue)
            meshQueue.Enqueue (chunk);
            lock (chunk)
            chunk.IsMeshed = 1;
        }
        if (chunk.IsMeshed == 2) {
            chunk.gameObject.SetActive (true);
            return true;
        }
        return false;
    }

    private void InstantiateChunk (Vector3 chunkPos) {
        if (chunks.ContainsKey (chunkPos))
            return;

        GameObject newChunk = SimplePool.Spawn (chunkPrefab, chunkPos, Quaternion.identity, false);
        newChunk.name = $"Chunk {chunkPos.x}, {chunkPos.y}, {chunkPos.z}";
        newChunk.transform.SetParent (world.gameObject.transform);
        Chunk chunk = newChunk.GetComponent<Chunk> ();
        if (chunk == null)
            chunk = newChunk.AddComponent<Chunk> ();
        lock (chunk) {
            chunk.Lod = chunkLod;
            chunk.Position = chunkPos;
            chunk.Fractal = fractal;
            chunk.Marching = marching;
            chunk.Material = chunkMaterial;
            chunk.IsGenerated = 0;
            chunk.IsMeshed = 0;
            chunk.IsDisposed = 0;
        }
        chunks.Add (chunkPos, chunk);
    }

    private void OnDisable () {
        StopAllCoroutines ();
        cts.Cancel ();
    }

    private void OnDestroy () {

    }

    private void OnApplicationQuit () {
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
