﻿using Foxworks.Voxels;
using Unity.Collections;
using UnityEngine;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
{
    /// <summary>
    /// This class is used to visualize a sphere.
    /// </summary>
    public class SphereVisualizer : MarchingCubeRendererBase
    {
        [Range(0f, 200f)]
        [SerializeField] private float _planetSurface;
        
        public override void GetVertexValues(NativeArray<int> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;
            
            Vector3 middleOfPlanet = new (VertexAmountX / 2f, VertexAmountY / 2f, VertexAmountZ / 2f);
            
            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3Int pos = MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                float distance = Vector3.Distance(middleOfPlanet, pos);
                verticesValues[i] = VoxelDataUtils.PackValueAndVertexColor(distance < _planetSurface ? 1f : 0f);
            }
        }
    }
}