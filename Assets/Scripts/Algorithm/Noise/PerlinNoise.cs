using UnityEngine;
/// <summary>
/// Original Perlin noise https://mrl.nyu.edu/~perlin/noise/
/// Improved Perlin https://gist.github.com/Flafla2/1a0b9ebef678bbce3215
/// </summary>
namespace Algorithm {
    public class PerlinNoise : INoise {

        // Hash lookup table as defined by Ken Perlin.  This is a randomly
        // arranged array of all numbers from 0-255 inclusive.
        private static readonly int[] permutation = {
            151,
            160,
            137,
            91,
            90,
            15,
            131,
            13,
            201,
            95,
            96,
            53,
            194,
            233,
            7,
            225,
            140,
            36,
            103,
            30,
            69,
            142,
            8,
            99,
            37,
            240,
            21,
            10,
            23,
            190,
            6,
            148,
            247,
            120,
            234,
            75,
            0,
            26,
            197,
            62,
            94,
            252,
            219,
            203,
            117,
            35,
            11,
            32,
            57,
            177,
            33,
            88,
            237,
            149,
            56,
            87,
            174,
            20,
            125,
            136,
            171,
            168,
            68,
            175,
            74,
            165,
            71,
            134,
            139,
            48,
            27,
            166,
            77,
            146,
            158,
            231,
            83,
            111,
            229,
            122,
            60,
            211,
            133,
            230,
            220,
            105,
            92,
            41,
            55,
            46,
            245,
            40,
            244,
            102,
            143,
            54,
            65,
            25,
            63,
            161,
            1,
            216,
            80,
            73,
            209,
            76,
            132,
            187,
            208,
            89,
            18,
            169,
            200,
            196,
            135,
            130,
            116,
            188,
            159,
            86,
            164,
            100,
            109,
            198,
            173,
            186,
            3,
            64,
            52,
            217,
            226,
            250,
            124,
            123,
            5,
            202,
            38,
            147,
            118,
            126,
            255,
            82,
            85,
            212,
            207,
            206,
            59,
            227,
            47,
            16,
            58,
            17,
            182,
            189,
            28,
            42,
            223,
            183,
            170,
            213,
            119,
            248,
            152,
            2,
            44,
            154,
            163,
            70,
            221,
            153,
            101,
            155,
            167,
            43,
            172,
            9,
            129,
            22,
            39,
            253,
            19,
            98,
            108,
            110,
            79,
            113,
            224,
            232,
            178,
            185,
            112,
            104,
            218,
            246,
            97,
            228,
            251,
            34,
            242,
            193,
            238,
            210,
            144,
            12,
            191,
            179,
            162,
            241,
            81,
            51,
            145,
            235,
            249,
            14,
            239,
            107,
            49,
            192,
            214,
            31,
            181,
            199,
            106,
            157,
            184,
            84,
            204,
            176,
            115,
            121,
            50,
            45,
            127,
            4,
            150,
            254,
            138,
            236,
            205,
            93,
            222,
            114,
            67,
            29,
            24,
            72,
            243,
            141,
            128,
            195,
            78,
            66,
            215,
            61,
            156,
            180
        };

        private static readonly int[] p;

        static PerlinNoise () {
            // Doubled permutation to avoid overflow
            p = new int[512];
            for (int x = 0; x < 512; x++) {
                p[x] = permutation[x % 256];
            }
        }

        public PerlinNoise () {

        }

        /// <summary>
        /// https://gamedev.stackexchange.com/questions/166124/perlin-noise-generation-always-returning-zero
        /// https://forum.libcinder.org/topic/perlin-returns-zero-for-all-integer-arguments
        /// For whatever input point P you're evaluating, and for each surrounding grid point Q, P - Q is the vector from the grid point Q to the input point P. So if P is on an integer coordinate, P-Q will have length zero! And therefore its dot product with the gradient vector will be zero. 
        /// </summary>
        /// <param name="x">NO INT</param>
        /// <param name="y">NO INT</param>
        /// <param name="z">NO INT</param>
        /// <returns></returns>
        public float Noise3D (float x, float y, float z) {

            int ix0, iy0, iz0; //(ix0, iy0, iz0) is the first vertex of the unit cube which (x,y,z) is located in.
            float fx0, fy0, fz0; //(fx0, fy0, fz0) is the distance vector of the first vertex and (x, y, z).

            //Caculate the first vertex of the unit cube, and we can get all vertex.
            ix0 = Mathf.FloorToInt (x) & 0xff;
            iy0 = Mathf.FloorToInt (y) & 0xff;
            iz0 = Mathf.FloorToInt (z) & 0xff;

            //Because its a unit cube so we also get all distance vector. 
            //There should be just like the Orignal Perlin noise for negative input.
            fx0 = x - Mathf.FloorToInt (x);
            fy0 = y - Mathf.FloorToInt (y);
            fz0 = z - Mathf.FloorToInt (z);

            float sx, sy, sz; //Proportionality coefficient used to interpolation.
            sx = Fade (fx0);
            sy = Fade (fy0);
            sz = Fade (fz0);

            //Hash function to get a unique value for every coordinate input.
            int v000, v010, v001, v011, v100, v110, v101, v111;
            v000 = p[p[p[ix0] + iy0] + iz0];
            v010 = p[p[p[ix0] + iy0 + 1] + iz0];
            v001 = p[p[p[ix0] + iy0] + iz0 + 1];
            v011 = p[p[p[ix0] + iy0 + 1] + iz0 + 1];
            v100 = p[p[p[ix0 + 1] + iy0] + iz0];
            v110 = p[p[p[ix0 + 1] + iy0 + 1] + iz0];
            v101 = p[p[p[ix0 + 1] + iy0] + iz0 + 1];
            v111 = p[p[p[ix0 + 1] + iy0 + 1] + iz0 + 1];

            float vx1, vx2, vy1, vy2;
            vx1 = Lerp (Grad (v000, fx0, fy0, fz0), Grad (v100, fx0 - 1, fy0, fz0), sx);
            vx2 = Lerp (Grad (v010, fx0, fy0 - 1, fz0), Grad (v110, fx0 - 1, fy0 - 1, fz0), sx);
            vy1 = Lerp (vx1, vx2, sy);

            vx1 = Lerp (Grad (v001, fx0, fy0, fz0 - 1), Grad (v101, fx0 - 1, fy0, fz0 - 1), sx);
            vx2 = Lerp (Grad (v011, fx0, fy0 - 1, fz0 - 1), Grad (v111, fx0 - 1, fy0 - 1, fz0 - 1), sx);
            vy2 = Lerp (vx1, vx2, sy);
            return Lerp (vy1, vy2, sz);
        }

        /// <summary>
        /// Fade function as defined by Ken Perlin.  This eases coordinate values
        /// so that they will "ease" towards integral values.  This ends up smoothing
        /// the final output.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private float Fade (float t) {
            return t * t * t * (t * (t * 6 - 15) + 10); // 6t^5 - 15t^4 + 10t^3
        }

        /// <summary>
        /// theory http://mrl.nyu.edu/~perlin/paper445.pdf
        /// </summary>
        /// <param name="hash">p[] hash</param>
        /// <param name="x">distanc vector x</param>
        /// <param name="y">distanc vector y</param>
        /// <param name="z">distanc vector z</param>
        /// <returns></returns>
        private float Grad (int hash, float x, float y, float z) {
            // the original perlin Grad
            // int h = hash & 15;     // Convert low 4 bits of hash code into 12 simple
            // float u = h < 8 ? x : y; // gradient directions, and compute dot product.
            // float v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
            // return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v);

            // the more efficient and more easy-to-understand Grad 
            // from http://riven8192.blogspot.com/2010/08/calculate-perlinnoise-twice-as-fast.html
            switch (hash & 0xF) {
            case 0x0:
                return x + y;
            case 0x1:
                return -x + y;
            case 0x2:
                return x - y;
            case 0x3:
                return -x - y;
            case 0x4:
                return x + z;
            case 0x5:
                return -x + z;
            case 0x6:
                return x - z;
            case 0x7:
                return -x - z;
            case 0x8:
                return y + z;
            case 0x9:
                return -y + z;
            case 0xA:
                return y - z;
            case 0xB:
                return -y - z;
            case 0xC:
                return y + x;
            case 0xD:
                return -y + z;
            case 0xE:
                return y - x;
            case 0xF:
                return -y - z;
            default:
                return 0; // never happens
            }
        }

        /// <summary>
        ///  linear interpolation
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        private float Lerp (float a, float b, float s) {
            return a + s * (b - a);
        }

    }
}
