﻿
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace WFCTD.GridManagement
{
    public class MarchingCubesVisualizer
    {
        public Vector3[] BaseVertices { get; private set; }
        public float[] VerticesValues { get; private set; }

        public Vector3[] SubVertices { get; private set; }
        public int[] Triangles { get; private set; }
        public int[] ValidTriangles { get; private set; }

        public void MarchCubes(
            GenerationProperties generationProperties, 
            Vector3Int vertexAmount, 
            float surface, 
            MeshFilter gridMeshFilter,
            Func<int, Vector3, GenerationProperties, float> getVertexValue,
            int maxTriangles = int.MaxValue,
            bool useLerp = true,
            bool enforceEmptyBorder = true)
        {
            if (generationProperties == null)
            {
                return;
            }
            
            Profiler.BeginSample("MarchingCubesVisualizer.Setup");

            int cubeAmountX = vertexAmount.x - 1;
            int cubeAmountY = vertexAmount.y - 1;
            int cubeAmountZ = vertexAmount.z - 1;
            int amountOfCubes = cubeAmountX * cubeAmountY * cubeAmountZ;
            int preAllocatedVerticesValues = vertexAmount.x * vertexAmount.y * vertexAmount.z;
            int floorSize = vertexAmount.x * vertexAmount.z;
            int frontFaceSize = vertexAmount.x * vertexAmount.y;
            int sideFaceSize = vertexAmount.z * vertexAmount.y;
            
            int preAllocatedVertices = preAllocatedVerticesValues + (vertexAmount.y - 1) * floorSize + (vertexAmount.z - 1) * frontFaceSize + (vertexAmount.x - 1) * sideFaceSize;
            bool recalculateVertices = SubVertices == null || preAllocatedVertices != SubVertices.Length;
            if (recalculateVertices)
            {
                SubVertices = new Vector3[preAllocatedVertices];
            }
            else
            {
                Array.Fill(SubVertices, Vector3.zero);
            }
            
            bool recalculateVertexValues = VerticesValues == null || preAllocatedVerticesValues != VerticesValues.Length;
            if (recalculateVertexValues)
            {
                BaseVertices = new Vector3[preAllocatedVerticesValues];
                VerticesValues = new float[preAllocatedVerticesValues];
            }
            
            for (int i = 0; i < preAllocatedVerticesValues; i++)
            {
                Vector3Int pos = Vector3Int.zero;
                pos.x = i % vertexAmount.x;
                pos.z  = (i % floorSize) / vertexAmount.x;
                pos.y  = i / floorSize;

                BaseVertices[i] = pos;
                
                if (enforceEmptyBorder)
                {
                    if (IsBorder(pos, vertexAmount))
                    {
                        VerticesValues[i] = 0;
                        continue;
                    }
                }
                
                VerticesValues[i] = getVertexValue(i, pos, generationProperties);
            }
            
            // Triangle has 3 vertices
            int preAllocatedTriangles = amountOfCubes * MarchingCubeUtils.MaximumTrianglesPerCube * 3;
            if (Triangles == null || preAllocatedTriangles != Triangles.Length)
            {
                Triangles = new int[preAllocatedTriangles];
            }
            
            Array.Fill(Triangles, -1);
            
            Profiler.EndSample();
            

            int[] baseVerticesOffsets =
            {
                0, 1, floorSize + 1, floorSize, vertexAmount.x, vertexAmount.x + 1, vertexAmount.x + floorSize + 1, vertexAmount.x + floorSize
            };

            int middleOffset = vertexAmount.x * cubeAmountZ + vertexAmount.z * cubeAmountX;
            int topOffset = vertexAmount.x * vertexAmount.z + middleOffset;
            
            int[] verticesOffsets = new int[12];

            int cubeFloor = cubeAmountX * cubeAmountZ;
            int triangleCount = 0;
            
            Profiler.BeginSample("MarchingCubesVisualizer.ConstructVertices");
            for (int i = 0; i < amountOfCubes; i++)
            {
                int xIndex = i % cubeAmountX;
                int zIndex = (i % cubeFloor) / cubeAmountX;
                int yIndex = i / cubeFloor;
                
                int indexOffset = xIndex + zIndex * (cubeAmountX + vertexAmount.x) + yIndex * topOffset;
                int baseIndexOffset = xIndex + zIndex * vertexAmount.x + yIndex * floorSize;
                
                int middleIndexOffset = middleOffset - zIndex * cubeAmountX;
                
                // Front face
                verticesOffsets[0] = indexOffset;
                verticesOffsets[1] = indexOffset + middleIndexOffset + 1;
                verticesOffsets[2] = indexOffset + topOffset;
                verticesOffsets[3] = indexOffset + middleIndexOffset;
                
                // Back face
                verticesOffsets[4] = indexOffset + cubeAmountX + vertexAmount.x;
                verticesOffsets[5] = indexOffset + middleIndexOffset + vertexAmount.x + 1;
                verticesOffsets[6] = indexOffset + topOffset + vertexAmount.x + cubeAmountX;
                verticesOffsets[7] = indexOffset + middleIndexOffset + vertexAmount.x;
                
                // Middle face
                verticesOffsets[8] = indexOffset + cubeAmountX;
                verticesOffsets[9] = indexOffset + cubeAmountX + 1;
                verticesOffsets[10] = indexOffset + topOffset + cubeAmountX + 1;
                verticesOffsets[11] = indexOffset + topOffset + cubeAmountX;

                triangleCount = MarchingCubeUtils.GetMarchedCube(
                    baseVerticesOffsets, 
                    VerticesValues, 
                    verticesOffsets, 
                    surface, 
                    SubVertices, 
                    Triangles,
                    baseIndexOffset,
                    triangleCount,
                    xIndex,
                    yIndex,
                    zIndex,
                    useLerp);
            }
            Profiler.EndSample();

            Profiler.BeginSample("MarchingCubesVisualizer.PruneTriangles");
            int maxItemsToPick = maxTriangles * 3;
            if (maxItemsToPick < 0 || maxItemsToPick > Triangles.Length)
            {
                maxItemsToPick = Triangles.Length;
            }
            
            maxItemsToPick = Mathf.Min(maxItemsToPick, triangleCount);

            // Create and fill the ValidTriangles array
            ValidTriangles = new int[maxItemsToPick];
            for (int i = 0; i < maxItemsToPick; i++)
            {
                ValidTriangles[i] = Triangles[i];
            }
            
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMesh");
            Mesh sharedMesh = gridMeshFilter.sharedMesh;
            if (sharedMesh != null)
            {
                sharedMesh.vertices = SubVertices;
                sharedMesh.triangles = ValidTriangles;
            }
            else
            {
                sharedMesh = new Mesh()
                {
                    indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                    vertices = SubVertices,
                    triangles = ValidTriangles
                };
            }
            
            Profiler.EndSample();
            
            sharedMesh.RecalculateBounds();
            
            Profiler.BeginSample("MarchingCubesVisualizer.AssignMesh");
            gridMeshFilter.sharedMesh = sharedMesh;
            gridMeshFilter.sharedMesh.hideFlags = HideFlags.DontSave;
            Profiler.EndSample();
        }

        private static bool IsBorder(Vector3Int pos, Vector3Int vertexAmount)
        {
            return pos.x == 0 || pos.x == vertexAmount.x - 1 || pos.y == 0 || pos.y == vertexAmount.y - 1 || pos.z == 0 || pos.z == vertexAmount.z - 1;
        }
    }
}