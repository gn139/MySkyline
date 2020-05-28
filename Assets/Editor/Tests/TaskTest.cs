using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Algorithm;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
    public class TaskTest {
        FractalNoise fractal;
        [SetUp]
        public void SetUp () {
            fractal = new FractalNoise (new PerlinNoise (), 3);
        }

        // A Test behaves as an ordinary method
        [Test]
        public void TaskTestSimplePasses () {
            // Use the Assert class to test conditions
            UpdateMesh ();
            int pointNums = 33;
            float[] densities = new float[pointNums * pointNums * pointNums];
            for (int x = 0; x < pointNums; x++) {
                for (int y = 0; y < pointNums; y++) {
                    for (int z = 0; z < pointNums; z++) {
                        float fx = x / 32;
                        float fy = y / 32;
                        float fz = z / 32;
                        // float fx = x + transform.position.x + 0.5f;
                        // float fy = y + transform.position.y + 0.5f;
                        // float fz = z + transform.position.z + 0.5f;
                        // Debug.Log ($"{fx}, {fy}, {fz}");

                        densities[x + y * pointNums + z * pointNums * pointNums] = fractal.Noise3D (fx, fy, fz);
                    }
                }
            }
            Task.WaitAll ();
            TestContext.WriteLine ("NoAsync data");
            foreach (var item in densities) {
                int i = 0;
                TestContext.WriteLine ($"D[{i}] = {item}");
                i++;
            }
        }
        async void UpdateMesh () {
            int pointNums = 33;
            float[] densities = new float[pointNums * pointNums * pointNums];
            await Task.Run (() => {
                for (int x = 0; x < pointNums; x++) {
                    for (int y = 0; y < pointNums; y++) {
                        for (int z = 0; z < pointNums; z++) {
                            float fx = x / 32;
                            float fy = y / 32;
                            float fz = z / 32;
                            // float fx = x + transform.position.x + 0.5f;
                            // float fy = y + transform.position.y + 0.5f;
                            // float fz = z + transform.position.z + 0.5f;
                            // Debug.Log ($"{fx}, {fy}, {fz}");

                            densities[x + y * pointNums + z * pointNums * pointNums] = fractal.Noise3D (fx, fy, fz);
                        }
                    }
                }
            });

            TestContext.WriteLine ("Async data");
            foreach (var item in densities) {
                int i = 0;
                TestContext.WriteLine ($"asycD[{i}] = {item}");
                i++;
            }
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TaskTestWithEnumeratorPasses () {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
