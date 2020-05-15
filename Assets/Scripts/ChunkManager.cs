using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Algorithm;

using UnityEngine;

public class ChunkManager : MonoBehaviour {
    public int spawnRadius = 4;
    public GameObject chunkPrefab;

    public Material chunkMaterial;
    private Dictionary<Vector3, Chunk> activeChunks;
    private Dictionary<Vector3, Chunk> inactiveChunks;
    private FractalNoise fractal;
    private MarchingCubes marching;
    private Vector3 position;
    private Vector3 lastPosition = new Vector3 (float.MinValue, float.MinValue, float.MinValue);
    private GameObject world;
    private int activeChunksNum;
    private int inactiveChunksNum;
    void Awake () {
        activeChunksNum = (spawnRadius >> 1) * (spawnRadius >> 1) * (spawnRadius >> 1);
        inactiveChunksNum = spawnRadius * spawnRadius * spawnRadius;
        activeChunks = new Dictionary<Vector3, Chunk> (activeChunksNum);
        inactiveChunks = new Dictionary<Vector3, Chunk> (inactiveChunksNum);
        fractal = new FractalNoise (new PerlinNoise (), 3);
        marching = new MarchingCubes (0.2f);
    }

    void Start () {
        world = GameObject.FindGameObjectWithTag ("World");
        SimplePool.Preload (chunkPrefab, inactiveChunksNum);
    }

    private void OnEnable() {
        StartCoroutine(ManageActiveChunks());
        StartCoroutine(ManageInactiveChunks());
    }

    IEnumerator ManageActiveChunks () {
        while (this.gameObject.activeSelf) {
            if (activeChunks.Count < activeChunksNum)
                yield return null;
            foreach (var key in activeChunks.Keys.ToList()) {
                if ((key - transform.position).sqrMagnitude <=
                    (spawnRadius >> 1) * Chunk.ChunkSize * (spawnRadius >> 1) * Chunk.ChunkSize)
                    continue;
                Chunk chunk = activeChunks[key];
                chunk.gameObject.SetActive (false);
                inactiveChunks.Add (key, chunk);
                activeChunks.Remove (key);
            }
            yield return null;
        }
    }

    IEnumerator ManageInactiveChunks () {
        while (this.gameObject.activeSelf) {
            if (inactiveChunks.Count < inactiveChunksNum)
                yield return null;

            foreach (var key in inactiveChunks.Keys.ToList()) {
                if ((key - transform.position).sqrMagnitude <= spawnRadius * Chunk.ChunkSize * spawnRadius * Chunk.ChunkSize)
                    continue;
                Chunk chunk = inactiveChunks[key];
                SimplePool.Despawn (chunk.gameObject);
                inactiveChunks.Remove (key);
            }
            yield return null;
        }
    }

    void Update () {

        position = ToChunkSpace (transform.position);
        // Debug.Log($"position {position}");
        // Debug.Log($"lastPosition {lastPosition}");
        if (position == lastPosition)
            return;

        for (int x = -spawnRadius; x < spawnRadius; x++) {
            for (int y = -spawnRadius; y < spawnRadius; y++) {
                for (int z = -spawnRadius; z < spawnRadius; z++) {
                    Vector3 offset = Vector3.Scale (new Vector3 (x, y, z),
                        new Vector3 (Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize));
                    Vector3 chunkPos = offset + position;
                    if (activeChunks.ContainsKey (chunkPos))
                        continue;
                    if (inactiveChunks.ContainsKey (chunkPos)) {
                        inactiveChunks[chunkPos].gameObject.SetActive (true);
                        continue;
                    }

                    // GameObject NewChunk = new GameObject ("Chunk " + (chunkPos.x) + " " + (chunkPos.y) + " " + (chunkPos.z));
                    GameObject newChunk = SimplePool.Spawn (chunkPrefab, chunkPos, Quaternion.identity, false);
                    newChunk.name = $"Chunk {chunkPos.x}, {chunkPos.y}, {chunkPos.z}";
                    newChunk.transform.SetParent (world.gameObject.transform);
                    Chunk chunk = newChunk.GetComponent<Chunk> ();
                    if (chunk == null)
                        chunk = newChunk.AddComponent<Chunk> ();
                    chunk.Fractal = fractal;
                    chunk.Marching = marching;
                    chunk.material = chunkMaterial;
                    newChunk.SetActive (true);
                    activeChunks.Add (chunkPos, chunk);
                }
            }
        }

        lastPosition = position;

    }

    private void OnDisable() {
        StopAllCoroutines();
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
