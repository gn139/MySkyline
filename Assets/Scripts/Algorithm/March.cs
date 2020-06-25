using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Algorithm {
    public class MeshData {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<int> indices;

        public MeshData (List<Vector3> vertices, List<Vector3> normals, List<int> indices) {
            this.vertices = vertices;
            this.normals = normals;
            this.indices = indices;
        }

    }
    public abstract class Marching : IMarching {

        public float Surface { get; set; }
        public int Lod { get; set; }

        /// <summary>
        /// Winding order of triangles use 2,1,0 or 0,1,2
        /// </summary>

        public Marching (float surface = 0.5f) {
            Surface = surface;

        }

        public virtual MeshData Generate (IList<float> voxels, Vector3 position, FractalNoise fractal) {

            float[] cube = new float[8];
            int[] windingOrder = new int[] { 0, 1, 2 };
            List<Vector3> verts = new List<Vector3> ();
            List<Vector3> normals = new List<Vector3> ();
            List<int> indices = new List<int> ();

            if (Surface > 0.0f) {
                windingOrder[0] = 0;
                windingOrder[1] = 1;
                windingOrder[2] = 2;
            }
            else {
                windingOrder[0] = 2;
                windingOrder[1] = 1;
                windingOrder[2] = 0;
            }
            int x, y, z, i;
            int ix, iy, iz;
            // Debug.Log (width + " " + height + " " + depth);
            for (x = 0; x < Chunk.ChunkSize; x += Lod) {
                for (y = 0; y < Chunk.ChunkSize; y += Lod) {
                    for (z = 0; z < Chunk.ChunkSize; z += Lod) {
                        //Get the values in the 8 neighbours which make up a cube
                        for (i = 0; i < 8; i++) {
                            ix = x + VertexOffset[i, 0] * Lod;
                            iy = y + VertexOffset[i, 1] * Lod;
                            iz = z + VertexOffset[i, 2] * Lod;
                            cube[i] = (float) OpenSimplexNoise.Evaluate ((ix + position.x) / (Chunk.ChunkSize - 1),
                                (iy + position.y) / (Chunk.ChunkSize - 1), (iz + position.z) / (Chunk.ChunkSize - 1));
                            // cube[i] = fractal.Noise3D ((ix + position.x) / (Chunk.ChunkSize - 1),
                            //     (iy + position.y) / (Chunk.ChunkSize - 1), (iz + position.z) / (Chunk.ChunkSize - 1));
                            // cube[i] = voxels[ix + iy * width + iz * width * height];

                        }

                        //Perform algorithm
                        March (x, y, z, cube, verts, indices, normals, windingOrder);
                    }
                }
            }

            return new MeshData (verts, normals, indices);
        }

        /// <summary>
        /// MarchCube performs the Marching algorithm on a single cube
        /// </summary>
        protected abstract void March (float x, float y, float z,
            float[] cube, IList<Vector3> vertList, IList<int> indexList, IList<Vector3> normals, int[] windingOrder);

        /// <summary>
        /// GetOffset finds the approximate point of intersection of the surface
        /// between two points with the values v1 and v2
        /// </summary>
        protected virtual float GetOffset (float v1, float v2) {
            float delta = v2 - v1;
            return (delta == 0.0f) ? Surface : (Surface - v1) / delta;
        }

        /// <summary>
        /// VertexOffset lists the positions, relative to vertex0, 
        /// of each of the 8 vertices of a cube.
        /// vertexOffset[8][3]
        /// </summary>
        protected static readonly int[, ] VertexOffset = new int[, ] { { 0, 0, 0 }, { 1, 0, 0 }, { 1, 1, 0 }, { 0, 1, 0 }, { 0, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 0, 1, 1 }
        };

    }
}
