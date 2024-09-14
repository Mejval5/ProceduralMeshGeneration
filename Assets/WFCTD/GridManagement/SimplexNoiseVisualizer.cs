﻿using UnityEngine;

namespace WFCTD.GridManagement
{
    [ExecuteAlways]
    public class SimplexNoiseVisualizer : MarchingCubeRendererBase
    {
        public override float GetGridValue(int i, Vector3 position, GenerationProperties generationProperties)
        {
            float x = (position.x + generationProperties.Origin.x) * generationProperties.Frequency / 1000f;
            float y = (position.y + generationProperties.Origin.y) * generationProperties.Frequency / 1000f;
            float z = (position.z + generationProperties.Origin.z) * generationProperties.Frequency / 1000f;
            
            return CustomNoiseSimplex(x, y, z);
        }

        private static float CustomNoiseSimplex(float x, float y, float z)
        {
            return Mathf.Clamp01(Mathf.Pow(SimplexNoise.Generate(x, y, z), 2));
        }
    }
}