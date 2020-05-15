using System;
using System.Collections;
using System.Collections.Generic;

using Algorithm;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
    public class NoisTestScript {
        private INoise noise;
        private float[, ] dataArray;
        [SetUp]
        public void SetUp () {
            // noise = new PerlinNoise ();
            int octaves = 3;
            noise = new FractalNoise (new PerlinNoise (), octaves);
            TestContext.WriteLine ($"octaves {octaves}");
        }

        [Test]
        public void NoiseFunctionTest () {
            dataArray = new float[, ] { { 1, 2, 3 }, { 0, 0, 0 }, { 1, 1, 1 }, {-4.3f, -0.2f, 0.1f }, {-4.3f, -0.2f, 0.1f }, {-2.34f, -3.45f, -4.56f }, { 0.1f, 0.2f, 0.3f }, { 100.23f, 1000.45f, 10000.64f }
            };
            for (int i = 0; i < dataArray.GetLength (0); i++) {
                TestContext.WriteLine ($"{dataArray[i,0]} {dataArray[i,1]} {dataArray[i,2]}");
                float result = noise.Noise3D (dataArray[i, 0], dataArray[i, 1], dataArray[i, 2]);
                Assert.NotNull (result);
                TestContext.WriteLine (" " + result);
            }
        }

        [Test]
        public void NoisePerformanceTest () {
            dataArray = new float[100000000, 3];
            for (int i = 0; i < dataArray.GetLength (0); i++) {
                for (int j = 0; j < dataArray.GetLength (1); j++) {
                    dataArray[i, j] = UnityEngine.Random.Range (float.MinValue, float.MaxValue);
                }
            }

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch ();
            watch.Start (); //开始监视代码运行时间
            //需要测试的代码
            //Perlin noise
            for (int i = 0; i < dataArray.GetLength (0); i++) {
                float result = noise.Noise3D (dataArray[i, 0], dataArray[i, 1], dataArray[i, 2]);
                Assert.NotNull (result);
            }
            watch.Stop (); //停止监视
            TimeSpan timespan = watch.Elapsed; //获取当前实例测量得出的总时间
            TestContext.WriteLine ($"PerlinNoise {timespan.TotalMilliseconds}");

            watch.Start (); //开始监视代码运行时间
            //需要测试的代码
            //simplex noise
            for (int i = 0; i < dataArray.GetLength (0); i++) {
                float result = (float) OpenSimplexNoise.Evaluate (dataArray[i, 0], dataArray[i, 1], dataArray[i, 2]);
                Assert.NotNull (result);
            }
            watch.Stop (); //停止监视
            TimeSpan timespanS = watch.Elapsed; //获取当前实例测量得出的总时间
            TestContext.WriteLine ($"SimplexNoise {timespanS.TotalMilliseconds}");
        }
    }
}
