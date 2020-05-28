using System.Collections.Generic;

using UnityEngine;

namespace Algorithm {
    public interface IMarching {

        float Surface { get; set; }

        MeshData Generate (IList<float> voxels, Vector3 position, FractalNoise fractal);

    }

}
