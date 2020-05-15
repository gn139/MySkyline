using System;

namespace Algorithm {
    /// <summary>
    /// https://code.google.com/archive/p/fractalterraingeneration/wikis/Fractional_Brownian_Motion.wiki
    /// </summary>
    public class FractalNoise : INoise {
        public int Octaves { get; set; }

        private INoise[] noises;
        private float[] amplitudes;
        private float[] frequencies;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="noise"></param>
        /// <param name="octaves">Octaves are how many layers you are putting together. 
        /// If you start with big features, the number of octaves determines how detailed the map will look.</param>
        /// <param name="lacunarity">Lacunarity is what makes the frequency grow.</param>
        /// <param name="gain(persistence)">Gain, also called persistence, is what makes the amplitude shrink (or not shrink).</param>
        public FractalNoise (INoise noise, int octaves, float lacunarity = 2.0f, float gain = .5f) {
            Octaves = octaves;
            UpdateTable (new INoise[] { noise }, lacunarity, gain);
        }

        public FractalNoise (INoise[] noises, int octaves, float lacunarity = 2.0f, float gain = .5f) {
            Octaves = octaves;
            UpdateTable (noises, lacunarity, gain);
        }

        private void UpdateTable (INoise[] noisesTemp, float lacunarity, float gain) {
            amplitudes = new float[Octaves];
            frequencies = new float[Octaves];
            noises = new INoise[Octaves];

            float amplitude = 1;
            float frequency = 1;
            for (int i = 0; i < Octaves; i++) {
                noises[i] = noisesTemp[Math.Min (i, noisesTemp.Length - 1)];
                frequencies[i] = frequency;
                amplitudes[i] = amplitude;

                amplitude *= gain;
                frequency *= lacunarity;
            }

        }

        public float Octave3D (int i, float x, float y, float z) {
            if (i < 0 || i > Octaves - 1) return 0.0f;
            if (noises[i] == null) return 0.0f;

            float frequency = frequencies[i];
            return noises[i].Noise3D (x * frequency, y * frequency, z * frequency) * amplitudes[i];
        }

        public float Noise3D (float x, float y, float z) {
            float sum = 0, frequency;
            for (int i = 0; i < Octaves; i++) {
                frequency = frequencies[i];
                if (noises[i] != null)
                    sum += noises[i].Noise3D (x * frequency, y * frequency, z * frequency) * amplitudes[i];
            }
            return sum;
        }
    }
}
