using System.Collections;
using System.Collections.Generic;

using Algorithm;

using UnityEngine;

public class Chunk : MonoBehaviour {
    public const int ChunkSize = 32;
    public const int BitShift = 5;
    public Material material;

    private Mesh mesh;
    private float[] densities;
    public FractalNoise Fractal { get; set; }
    public Marching Marching { get; set; }
    private List<Vector3> verts;
    private List<int> indices;
    private Vector3 lastPostion;

    void Awake () { }

    private void OnEnable () {
        if (lastPostion == transform.position) {
#if UNITY_EDITOR
            Debug.Log (gameObject.name + " On LastPostion");
#endif
            return;
        }
        if (Fractal == null || Marching == null) {
            Destroy (this);
            return;
        }
        int pointNums = ChunkSize + 1;
        densities = new float[pointNums * pointNums * pointNums];
        verts = new List<Vector3> ();
        indices = new List<int> ();

        for (int x = 0; x < pointNums; x++) {
            for (int y = 0; y < pointNums; y++) {
                for (int z = 0; z < pointNums; z++) {
                    float fx = (x + transform.position.x) / (ChunkSize - 1);
                    float fy = (y + transform.position.y) / (ChunkSize - 1);
                    float fz = (z + transform.position.z) / (ChunkSize - 1);
                    // float fx = x + transform.position.x + 0.5f;
                    // float fy = y + transform.position.y + 0.5f;
                    // float fz = z + transform.position.z + 0.5f;
                    // Debug.Log ($"{fx}, {fy}, {fz}");

                    densities[x + y * pointNums + z * pointNums * pointNums] = Fractal.Noise3D (fx, fy, fz);
                }
            }
        }
        // Debug.Log ($"densities {densities.Length}");

        Marching.Generate (densities, pointNums, pointNums, pointNums, verts, indices);
        // Debug.Log ($"verts {verts.Count}");
        // Debug.Log ($"indices {indices.Count}");

        mesh = new Mesh ();
        mesh.SetVertices (verts);
        mesh.SetTriangles (indices, 0);
        mesh.RecalculateBounds ();
        mesh.RecalculateNormals ();
        this.gameObject.AddComponent<MeshCollider> ();
        MeshFilter Filter = this.gameObject.GetComponent<MeshFilter> ();
        Filter.mesh = mesh;
        MeshRenderer Renderer = this.gameObject.GetComponent<MeshRenderer> ();
        Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Renderer.receiveShadows = false;
        Renderer.material = material;
        MeshCollider meshC = this.gameObject.GetComponent<MeshCollider> ();
        meshC.sharedMesh = mesh;
    }

    void Start () {

    }

    void Update () {

    }
#if UNITY_EDITOR
    private void OnDrawGizmos () {
        Gizmos.color = Color.red;

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

}
