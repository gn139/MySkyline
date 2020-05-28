using System.Collections;
using System.Collections.Generic;

using Algorithm;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
    public class MarchingTest {
        private Marching marching;
        private List<Vector3> verts;
        private List<int> indices;
        private float[] densities;
        private FractalNoise fractal;
        [SetUp]
        public void SetUp () {
            densities = new float[32 * 32 * 32];
            fractal = new FractalNoise (new PerlinNoise (), 3);
            marching = new MarchingCubes ();
            verts = new List<Vector3> ();
            indices = new List<int> ();
        }
        // A Test behaves as an ordinary method
        [Test]
        public void MarchingTestSimplePasses () {

            for (int x = 0; x < 32; x++) {
                for (int y = 0; y < 32; y++) {
                    for (int z = 0; z < 32; z++) {
                        float fx = x;
                        float fy = y;
                        float fz = z;

                        densities[x + y * 32 + z * 32 * 32] = fractal.Noise3D (fx, fy, fz);
                        TestContext.WriteLine (densities[x + y * 32 + z * 32 * 32]);
                    }
                }
            }
            TestContext.WriteLine ($"densities {densities.Length}");

            // marching.Generate (densities, 32, 32, 32, verts, indices);
            TestContext.WriteLine ($"verts {verts.Count}");
            TestContext.WriteLine ($"indices {indices.Count}");
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator MarchingTestWithEnumeratorPasses () {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
