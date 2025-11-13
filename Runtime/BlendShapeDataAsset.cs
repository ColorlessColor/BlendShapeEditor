using System;
using System.Collections.Generic;
using UnityEngine;


namespace io.github.colorlesscolor.blendshapeeditor
{

    [Serializable]
    public sealed class BlendShapeFrameData
    {
        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public Vector3[] deltaTangents;
        public float weight;

        public BlendShapeFrameData(int vertexCount, float weight)
        {
            deltaVertices = new Vector3[vertexCount];
            deltaNormals = new Vector3[vertexCount];
            deltaTangents = new Vector3[vertexCount];
            this.weight = weight;
        }
    }

    [Serializable]
    public sealed class BlendShapeData
    {
        public string blendShapeName;
        public int vertexCount;

        public List<BlendShapeFrameData> blendShapeFrames;

        public BlendShapeData(string blendShapeName, int vertexCount)
        {
            this.blendShapeName = blendShapeName;
            this.vertexCount = vertexCount;
            blendShapeFrames = new();
        }

        public void AddFrame(BlendShapeFrameData frame)
        {
            if (frame.deltaNormals.Length == vertexCount)
            {
                blendShapeFrames.Add(frame);
            }
            else
            {
                throw new ArgumentException($"VertexCount not match: vertexCount={vertexCount}, frame.deltaVertices.Length={frame.deltaVertices.Length}");
            }
        }
    }

    public sealed class BlendShapeDataAsset : ScriptableObject
    {
        public List<BlendShapeData> blendShapeDataList;
        public int vertexCount;

        public void SetVertexCount(int vertexCount)
        {
            this.vertexCount = vertexCount;
            blendShapeDataList = new();
        }

        public void AddBlendShapeData(BlendShapeData blendShapeData)
        {
            if (vertexCount == blendShapeData.vertexCount)
            {
                blendShapeDataList.Add(blendShapeData);
            }
            else
            {
                throw new ArgumentException($"VertexCount not match: vertexCount={vertexCount}, blendShapeData.vertexCount={blendShapeData.vertexCount}");
            }
        }

        public BlendShapeData GetBlendShapeByIndex(int index)
        {
            return blendShapeDataList[index];
        }
    }
}