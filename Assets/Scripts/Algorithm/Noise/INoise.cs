using UnityEngine;

namespace Algorithm {
    /// <summary>
    /// Interface for generating noise.
    /// </summary>
	public interface INoise 
	{

        // /// <summary>
        // ///  noise in 1 dimension.
        // /// </summary>
		// float Noise1D(float x);

        // /// <summary>
        // ///  noise in 2 dimensions.
        // /// </summary>
		// float Noise2D(float x, float y);

        /// <summary>
        ///  noise in 3 dimensions.
        /// </summary>
		float Noise3D(float x, float y, float z);


	}

}
