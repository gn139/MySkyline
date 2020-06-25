using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Algorithm;

using UnityEngine;

public class Chunk : MonoBehaviour, IDisposable {
    public const int ChunkSize = 64;
    public const int BitShift = 6;
    public ChunkState State { get => state; set => state = value; }
    public Material Material { get; set; }
    // public bool IfDespawn { get; set; } = false;
    // public bool IfActive { get; set; } = false;
    // public int IsMeshed { get => isMeshed; set => Interlocked.Exchange (ref isMeshed, value); }
    // // public int Lod { get; set; }
    // public int IsDisposed { get => isDisposed; set => isDisposed = value; }
    public Vector3 Position { get; set; }
    public MeshData MeshData { get => meshData; set => meshData = value; }

    // private int isMeshed = 0; // 0 未进入meshqueue 1 已进入meshqueue 2 已生成mesh
    // private int isDisposed = 0;
    // private Mesh mesh;
    private float[] densities;
    public FractalNoise Fractal { get; set; }
    public Marching Marching { get; set; }
    public int Lod { get; set; }
    // private List<Vector3> verts;
    // private List<int> indices;
    // private CancellationTokenSource cts;
    // private Task task;
    // private Task<MeshData> continueTask;
    private MeshFilter filter;
    private MeshRenderer render;
    private MeshCollider meshCollider;
    private MeshData meshData = null;
    private ChunkState state;
    void Awake () {
        filter = this.gameObject.GetComponent<MeshFilter> ();
        render = this.gameObject.GetComponent<MeshRenderer> ();
        meshCollider = this.gameObject.GetComponent<MeshCollider> ();
        //  filter = this.gameObject.AddComponent<MeshFilter> ();
        // render = this.gameObject.AddComponent<MeshRenderer> ();
        // meshCollider = this.gameObject.AddComponent<MeshCollider> ();
#if UNITY_EDITOR
        // Debug.Log (gameObject.name + " On LastPostion");
        // gameObject.name =
        //     $"Chunk {transform.position.x},{transform.position.y},{transform.position.z} IsMeshed {IsMeshed} IsDisposed {IsDisposed}";
#endif
    }

    private void Start () {
#if UNITY_EDITOR
        // Debug.Log (gameObject.name + " On LastPostion");
        // gameObject.name =
        //     $"Chunk {transform.position.x},{transform.position.y},{transform.position.z} IsMeshed {IsMeshed} IsDisposed {IsDisposed}";
#endif
    }

    private void OnEnable () {

        // // if (mesh == null)
        // mesh = new Mesh ();

        // if (filter == null)
        //     filter = this.gameObject.GetComponent<MeshFilter> ();

        // // if (filter.mesh == null)
        // filter.mesh = mesh;

        if (render == null)
            render = this.gameObject.GetComponent<MeshRenderer> ();

        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.material = Material;

        // count++;
        // if (Fractal == null || Marching == null) {
        //     Destroy (this);
        //     return;
        // }

        if (state == ChunkState.Active)
            UpdateMesh ();
        // verts.Clear ();
        // indices.Clear ();

    }

    public void BuildMesh () {
        Interlocked.Exchange (ref meshData, Marching.Generate (densities, Position, Fractal));
        // Interlocked.Exchange (ref state, ChunkState.Processed);
        lock (this)
        state = ChunkState.Processed;
    }

    void Update () {
        // Debug.Log (gameObject.name + " On LastPostion");
// #if UNITY_EDITOR
//         gameObject.name =
//             $"Chunk {transform.position.x},{transform.position.y},{transform.position.z} State {state}";
// #endif

    }

    private void OnWillRenderObject () {

    }

    void UpdateMesh () {
        // if (IsDisposed)
        //     return;
        // int pointNums = ChunkSize + 1;
        // Vector3 position = transform.position;
        // continueTask = task.ContinueWith<MeshData> ((attendence) => {
        //     // verts = new List<Vector3> ();
        //     // indices = new List<int> ();
        //     return Marching.Generate (densities, pointNums, pointNums, pointNums);
        // }, cts.Token);

        // MeshData meshData = await continueTask;

        // if (continueTask.Status != TaskStatus.RanToCompletion)
        //     return;
        Mesh mesh = new Mesh ();
        mesh.SetVertices (meshData.vertices);
        mesh.SetNormals (meshData.normals);
        mesh.SetIndices (meshData.indices.ToArray (), MeshTopology.Triangles, 0);

        // mesh.SetTriangles (indices, 0);
        // mesh.RecalculateBounds ();
        // mesh.RecalculateNormals ();

        if (filter == null)
            filter = this.gameObject.AddComponent<MeshFilter> ();
        filter.mesh = mesh;

        if (meshCollider == null)
            meshCollider = this.gameObject.AddComponent<MeshCollider> ();
        meshCollider.sharedMesh = mesh;
    }

    private void OnDisable () {
        // if (cts != null)
        //     cts.Cancel ();
        // Dispose ();
    }

    private void OnDestroy () {
        // if (cts != null)
        //     cts.Dispose ();
    }
#if UNITY_EDITOR
    private void OnDrawGizmos () {
        Gizmos.color = Color.red;
        // if (gameObject.activeSelf)
        Gizmos.DrawWireCube (transform.position + transform.TransformVector (ChunkSize / 2, ChunkSize / 2, ChunkSize / 2),
            transform.TransformVector (new Vector3 (ChunkSize, ChunkSize, ChunkSize)));
        // Gizmos.color = Color.yellow;
        // for (int x = 0; x < ChunkSize - 1; x++) {
        //     for (int y = 0; y < ChunkSize - 1; y++) {
        //         for (int z = 0; z < ChunkSize - 1; z++) {
        //             Gizmos.DrawWireCube (transform.TransformPoint (new Vector3 (x + 0.5f, y + 0.5f, z + 0.5f)),
        //                 transform.TransformVector (new Vector3 (1, 1, 1)));
        //         }
        //     }
        // }
    }

#endif
    public void Dispose () {
        state = ChunkState.Disposed;
#if UNITY_EDITOR
        gameObject.name =
            $"Chunk {transform.position.x},{transform.position.y},{transform.position.z} State {state}";
#endif
        meshData = null;
        if (filter == null)
            return;
        filter.mesh = null;
        if (render == null)
            return;
        render.material = null;
        if (meshCollider == null)
            return;
        meshCollider.sharedMesh = null;
    }
}

public enum ChunkState {
    Created,
    Processing,
    Processed,
    InActive,
    Active,
    Disposing,
    Disposed
}
