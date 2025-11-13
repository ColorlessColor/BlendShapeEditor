using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace io.github.colorlesscolor.blendshapeeditor
{
    public class MeshBlendShapeBuilder
    {
        /// <summary>
        ///  Dictionary<shapeIndex, List<weight, BlendShapeFrame>>
        /// </summary>
        private List<BlendShapeFrame> container;
        private Mesh newMesh;
        private Mesh oldMesh;

        /// <summary>
        /// 将 BlendShape 应用到基础网格的 pass
        /// (container, Mesh newMesh) => {}
        /// </summary>
        private MorphBaseMeshApply morphBaseMeshApply;

        /// <summary>
        /// 拷贝旧 BlendShape 的 pass
        /// (Mesh oldMesh, Mesh newMesh) => {}
        /// </summary>
        private BlendShapeCopyer blendShapeCopyer;

        /// <summary>
        /// 应用 BlendShape 的 passes
        /// (container, Mesh newMesh) => {}
        /// </summary>
        private List<FrameApplyPass> blendShapeFrameApplyPasses;

        /// <summary>
        /// 应用形变到基础网格
        /// </summary>
        /// <param name="container"></param>
        /// <param name="mesh"></param>
        public delegate void MorphBaseMeshApply(List<BlendShapeFrame> container, Mesh mesh);
        /// <summary>
        /// 拷贝旧网格上 BlendShape 的方法
        /// </summary>
        /// <param name="newMesh"></param>
        /// <param name="oldMesh"></param>
        public delegate void BlendShapeCopyer(Mesh newMesh, Mesh oldMesh);
        /// <summary>
        /// 处理 BlendShape Frames 的方法
        /// </summary>
        /// <param name="container"></param>
        /// <param name="mesh"></param>
        public delegate void FrameApplyPass(List<BlendShapeFrame> container, Mesh mesh);


        public MeshBlendShapeBuilder(Mesh mesh)
        {
            container = new();
            oldMesh = mesh;
            newMesh = MeshUtils.CloneMeshOnly(oldMesh);
            // 默认不应用混合形状
            morphBaseMeshApply = null;
            // 默认拷贝方法
            blendShapeCopyer = new(MeshUtils.CopyBlendShapeAllFromMesh);
            // 默认不添加形态键
            blendShapeFrameApplyPasses = new();
        }

        public MeshBlendShapeBuilder AddBlendShapeFrame(int shapeIndex, float weight)
        {
            int frameIndex = 0;
            for (int i = 0; i < oldMesh.GetBlendShapeFrameCount(shapeIndex); i++)
            {
                if (oldMesh.GetBlendShapeFrameWeight(shapeIndex, i) >= weight)
                {
                    frameIndex = i;
                    break;
                }
            }

            if (frameIndex == 0)
            {
                // 只有 1 帧 blendshape, 谢天谢地
                BlendShapeFrame frame = new(oldMesh, shapeIndex, frameIndex);

                // weight 范围是 0~100, 转换成百分比
                frame.MulInPlace(weight / 100f);

                container.Add(frame);
            }
            else
            {
                // 有多帧, 此时应当满足 frame0.weight < weight <= frame1.weight
                // 非常不想插值但是这里需要插值, 不知道在法线和切线上插值是否是正确选择
                // 这里有一个大大的**警告**: 用到这段代码你只能多加小心!

                // 获得前后 2 帧权重
                BlendShapeFrame frame0 = new(oldMesh, shapeIndex, frameIndex - 1);
                BlendShapeFrame frame1 = new(oldMesh, shapeIndex, frameIndex);

                // 获得前后 2 帧权重
                float weightFrame0 = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex - 1);
                float weightFrame1 = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);

                // lerp 比例是?
                float t = (weight - weightFrame0) /
                    (weightFrame1 - weightFrame0);

                // 在 2 帧之间线性插值
                frame0.LerpInPlace(frame1, t);

                container.Add(frame0);
            }


            return this;
        }

        public MeshBlendShapeBuilder ClearFrames()
        {
            container.Clear();

            return this;
        }

        /// <summary>
        /// 使用自定义方法拷贝 Mesh 上的形态键
        /// </summary>
        /// <param name="copyer">(Mesh oldMesh, Mesh oldMesh) => {}</param>
        /// <returns></returns>
        public MeshBlendShapeBuilder SetCopyOldBlendShapesMethod(BlendShapeCopyer copyer)
        {
            blendShapeCopyer = copyer;
            return this;
        }

        /// <summary>
        /// 将混合形状帧混合后添加到 mesh
        /// </summary>
        /// <param name="m"></param>
        /// <param name="blendShapeName"></param>
        /// <returns></returns>
        public MeshBlendShapeBuilder BlendFramesToMesh(string blendShapeName)
        {
            blendShapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendShapeFrame(mesh.vertexCount);
                    foreach (var frame in container)
                    {
                        newFrame.AddInPlace(frame);
                    }
                    newFrame.WriteBlendShapeFrameToMesh(mesh, blendShapeName, 100f);
                })
            );
            return this;
        }

        /// <summary>
        /// 将混合形状帧混合后反转再添加到 mesh
        /// </summary>
        /// <param name="blendShapeName"></param>
        /// <returns></returns>
        public MeshBlendShapeBuilder BlendFramesInverseToMesh(string blendShapeName)
        {
            blendShapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendShapeFrame(mesh.vertexCount);
                    foreach (var frame in container)
                    {
                        newFrame.AddInPlace(frame);
                    }
                    newFrame.MulInPlace(-1f);
                    newFrame.WriteBlendShapeFrameToMesh(mesh, blendShapeName, 100f);
                })
            );

            return this;
        }

        public MeshBlendShapeBuilder ConnectBlendFramesToMesh(string blendShapeName)
        {
            blendShapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    float weightPerFrame = 100f / container.Count;
                    float currentWeight = weightPerFrame;
                    foreach (var frame in container)
                    {
                        frame.WriteBlendShapeFrameToMesh(mesh, blendShapeName, currentWeight);
                        currentWeight += weightPerFrame;
                    }
                })
            );

            return this;
        }

        public MeshBlendShapeBuilder BlendAndConnectFramesToMesh(string blendShapeName)
        {
            blendShapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendShapeFrame(mesh.vertexCount);
                    float weightPerFrame = 100f / container.Count;
                    float currentWeight = weightPerFrame;
                    foreach (var frame in container)
                    {
                        newFrame.AddInPlace(frame);
                        newFrame.WriteBlendShapeFrameToMesh(mesh, blendShapeName, currentWeight);
                        currentWeight += weightPerFrame;
                    }
                })
            );

            return this;
        }

        public MeshBlendShapeBuilder ApplyToBaseMeshAndCreateInverse(string blendShapeName)
        {
            // 先应用形变到基础网格
            morphBaseMeshApply = new MorphBaseMeshApply((container, mesh) =>
            {
                var newFrame = new BlendShapeFrame(mesh.vertexCount);
                foreach (var frame in container)
                {
                    newFrame.AddInPlace(frame);
                }

                // 应用形变到新 mesh
                newFrame.ApplyBlendFrameToMesh(mesh);
            });

            // 中间会进行旧 BlendShape 拷贝
            // 也就是执行 blendshapeCopyer() 委托

            // 最后添加反转 BlendShape
            BlendFramesInverseToMesh(blendShapeName);

            return this;
        }

        public MeshBlendShapeBuilder ApplyMaskedBlendShapeToMesh(string blendShapeName, Func<Mesh, float[]> createMask)
        {
            blendShapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendShapeFrame(mesh.vertexCount);
                    foreach (var frame in container)
                    {
                        newFrame.AddInPlace(frame);
                    }

                    var mask = createMask(mesh);
                    newFrame.MulInPlace(mask);
                    newFrame.WriteBlendShapeFrameToMesh(mesh, blendShapeName, 100f);
                })
            );

            return this;
        }

        public Mesh BuildMesh()
        {
            if (morphBaseMeshApply != null)
            {
                morphBaseMeshApply(container, newMesh);
            }

            if (blendShapeCopyer != null)
            {
                blendShapeCopyer(newMesh, oldMesh);
            }

            if (blendShapeFrameApplyPasses.Count > 0)
            {
                foreach (var pass in blendShapeFrameApplyPasses)
                {
                    pass(container, newMesh);
                }
            }

            return newMesh;
        }
    }


    public class BlendShapeFrame
    {
        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public Vector3[] deltaTangents;

        /// <summary>
        /// 初始化空的 BlendShapeFrame
        /// </summary>
        /// <param name="vertexCount"></param>
        public BlendShapeFrame(int vertexCount)
        {
            deltaVertices = new Vector3[vertexCount];
            deltaNormals = new Vector3[vertexCount];
            deltaTangents = new Vector3[vertexCount];
        }


        public BlendShapeFrame(BlendShapeFrame frame)
        {
            deltaVertices = new Vector3[frame.deltaVertices.Length];
            deltaNormals = new Vector3[frame.deltaNormals.Length];
            deltaTangents = new Vector3[frame.deltaTangents.Length];
            frame.deltaVertices.CopyTo(deltaVertices.AsSpan());
            frame.deltaNormals.CopyTo(deltaNormals.AsSpan());
            frame.deltaTangents.CopyTo(deltaTangents.AsSpan());
        }

        /// <summary>
        /// 从 Mesh 指定的混合形状和帧初始化 BlendShapeFrame
        /// </summary>
        /// <param name="mesh">Mesh</param>
        /// <param name="shapeIndex">混合形状索引</param>
        /// <param name="frameIndex">关键帧索引</param>
        public BlendShapeFrame(Mesh mesh, int shapeIndex, int frameIndex)
        {
            deltaVertices = new Vector3[mesh.vertexCount];
            deltaNormals = new Vector3[mesh.vertexCount];
            deltaTangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
        }

        public static void Vector3AddInPlace(Vector3[] left, Vector3[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                left[i] += right[i];
            }
        }

        public static void Vector3MulInPlace(Vector3[] left, float right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                left[i] *= right;
            }
        }

        public static void Vector3MulInPlace(Vector3[] left, float[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                left[i] *= right[i];
            }
        }

        public static void Vector3LerpInPlace(Vector3[] a, Vector3[] b, float t)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = Vector3.Lerp(a[i], b[i], t);
            }
        }

        public void AddInPlace(BlendShapeFrame frame)
        {
            Vector3AddInPlace(deltaVertices, frame.deltaVertices);
            Vector3AddInPlace(deltaNormals, frame.deltaNormals);
            Vector3AddInPlace(deltaTangents, frame.deltaTangents);
        }

        public void MulInPlace(float value)
        {
            Vector3MulInPlace(deltaVertices, value);
            Vector3MulInPlace(deltaNormals, value);
            Vector3MulInPlace(deltaTangents, value);
        }

        public void MulInPlace(float[] values)
        {
            Vector3MulInPlace(deltaVertices, values);
            Vector3MulInPlace(deltaNormals, values);
            Vector3MulInPlace(deltaTangents, values);
        }

        public void LerpInPlace(BlendShapeFrame b, float t)
        {
            Vector3LerpInPlace(deltaVertices, b.deltaVertices, t);
            Vector3LerpInPlace(deltaNormals, b.deltaNormals, t);
            Vector3LerpInPlace(deltaTangents, b.deltaTangents, t);
        }

        public void WriteBlendShapeFrameToMesh(Mesh mesh, string name, float weight)
        {
            mesh.AddBlendShapeFrame(name, weight, deltaVertices, deltaNormals, deltaTangents);
        }

        public void ApplyBlendFrameToMesh(Mesh mesh)
        {
            Vector3[] meshVertices = new Vector3[mesh.vertexCount];
            Vector3[] meshNormals = new Vector3[mesh.vertexCount];
            Vector4[] meshTangents = new Vector4[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                meshVertices[i] = mesh.vertices[i] + deltaVertices[i];
                meshNormals[i] = mesh.normals[i] + deltaNormals[i];
                meshTangents[i] = mesh.tangents[i] + new Vector4(deltaTangents[i].x, deltaTangents[i].y, deltaTangents[i].z, 0);
            }
            mesh.vertices = meshVertices;
            mesh.normals = meshNormals;
            mesh.tangents = meshTangents;
        }

        public static void Lerp(Vector3[] from, Vector3[] to, Vector3[] dest, float lerpRate)
        {
            for (int i = 0; i < from.Length; i++)
            {
                dest[i] = Vector3.Lerp(from[i], to[i], lerpRate);
            }
        }

        public static BlendShapeFrame Lerp(BlendShapeFrame frameFrom, BlendShapeFrame frameTo, float lerpRate)
        {
            var newFrame = new BlendShapeFrame(frameFrom);
            Lerp(frameFrom.deltaVertices, frameTo.deltaVertices, newFrame.deltaVertices, lerpRate);
            Lerp(frameFrom.deltaNormals, frameTo.deltaNormals, newFrame.deltaNormals, lerpRate);
            Lerp(frameFrom.deltaTangents, frameTo.deltaTangents, newFrame.deltaTangents, lerpRate);
            return newFrame;
        }
    }

    public static class MeshUtils
    {
        public const string savePath = "Assets/BlendShapeEditor/";

        public static void CreateMeshAssetAndSave(Mesh mesh, string fileName)
        {
            string now = DateTime.Now.ToString("yyyy.M.d_HH.mm.ss_fff");
            AssetDatabase.CreateAsset(mesh, savePath + fileName + "_" + now + ".asset");
        }

        public static Mesh CloneMeshOnly(Mesh oldMesh)
        {
            Mesh newMesh = new Mesh();

            // 复制所有属性
            newMesh.indexFormat = oldMesh.indexFormat;
            newMesh.vertices = oldMesh.vertices;
            newMesh.uv = oldMesh.uv;
            newMesh.uv2 = oldMesh.uv2;
            newMesh.uv3 = oldMesh.uv3;
            newMesh.uv4 = oldMesh.uv4;
            newMesh.uv5 = oldMesh.uv5;
            newMesh.uv6 = oldMesh.uv6;
            newMesh.uv7 = oldMesh.uv7;
            newMesh.uv8 = oldMesh.uv8;

            newMesh.bindposes = oldMesh.bindposes;
            newMesh.boneWeights = oldMesh.boneWeights;
            newMesh.bounds = oldMesh.bounds;
            newMesh.colors = oldMesh.colors;
            newMesh.colors32 = oldMesh.colors32;
            newMesh.normals = oldMesh.normals;
            newMesh.subMeshCount = oldMesh.subMeshCount;
            newMesh.tangents = oldMesh.tangents;

            // 复制所有 subMesh, 不要重新计算边界
            for (int subMesh = 0; subMesh < oldMesh.subMeshCount; subMesh++)
            {
                newMesh.SetTriangles(oldMesh.GetTriangles(subMesh), subMesh, false, (int)oldMesh.GetBaseVertex(subMesh));
                newMesh.SetSubMesh(subMesh, oldMesh.GetSubMesh(subMesh), MeshUpdateFlags.DontRecalculateBounds);
            }

            return newMesh;
        }

        public static void ReplaceSharedMeshKeepBlendShapeWeight(SkinnedMeshRenderer smr, Mesh newMesh)
        {
            // 获取旧的 Dictionary<形态键名, 权重> 列表
            Dictionary<string, float> originalBlendShapeWeight = new();
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                originalBlendShapeWeight.Add(smr.sharedMesh.GetBlendShapeName(i), smr.GetBlendShapeWeight(i));
            }

            // 以防万一先归零 SMR 权重
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                smr.SetBlendShapeWeight(i, 0);
            }

            // 替换 Mesh
            smr.sharedMesh = newMesh;

            // 重新填充权重
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                string blendShapeName = smr.sharedMesh.GetBlendShapeName(i);
                if (originalBlendShapeWeight.TryGetValue(blendShapeName, out var weight))
                {
                    smr.SetBlendShapeWeight(i, weight);
                }
            }
        }

        public static void CopyBlendShapeFromBlendShapeData(Mesh newMesh, BlendShapeData blendShapeData)
        {
            foreach (var frame in blendShapeData.blendShapeFrames)
            {
                newMesh.AddBlendShapeFrame(blendShapeData.blendShapeName, frame.weight, frame.deltaVertices, frame.deltaNormals, frame.deltaTangents);
            }
        }

        public static void CopyBlendShapeFromBlendShapeData(Mesh newMesh, BlendShapeData blendShapeData, string blendShapeName)
        {
            foreach (var frame in blendShapeData.blendShapeFrames)
            {
                newMesh.AddBlendShapeFrame(blendShapeName, frame.weight, frame.deltaVertices, frame.deltaNormals, frame.deltaTangents);
            }
        }

        public static void CopyBlendShapeAllFromMesh(Mesh newMesh, Mesh oldMesh)
        {
            // 复制所有 blendshape
            for (int shapeIndex = 0; shapeIndex < oldMesh.blendShapeCount; shapeIndex++)
            {
                for (int frameIndex = 0; frameIndex < oldMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                {
                    string shapeName = oldMesh.GetBlendShapeName(shapeIndex);
                    float weight = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                    var oldMeshFrame = new BlendShapeFrame(oldMesh, shapeIndex, frameIndex);

                    oldMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                }
            }
        }

        public static void CopyBlendShapeByIndexFromMesh(Mesh newMesh, Mesh oldMesh, int oldMeshShapeIndex)
        {
            // 复制一个旧 blendshape
            for (int frameIndex = 0; frameIndex < oldMesh.GetBlendShapeFrameCount(oldMeshShapeIndex); frameIndex++)
            {
                string shapeName = oldMesh.GetBlendShapeName(oldMeshShapeIndex);
                float weight = oldMesh.GetBlendShapeFrameWeight(oldMeshShapeIndex, frameIndex);
                var oldMeshFrame = new BlendShapeFrame(oldMesh, oldMeshShapeIndex, frameIndex);

                oldMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
            }
        }

        public static void CopyBlendShapeFillteredFromMesh(Mesh newMesh, Mesh oldMesh, Predicate<string> accept)
        {
            // 复制所有 blendshape
            for (int shapeIndex = 0; shapeIndex < oldMesh.blendShapeCount; shapeIndex++)
            {
                if (accept(oldMesh.GetBlendShapeName(shapeIndex)))
                {
                    for (int frameIndex = 0; frameIndex < oldMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                    {
                        string shapeName = oldMesh.GetBlendShapeName(shapeIndex);
                        float weight = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                        var oldMeshFrame = new BlendShapeFrame(oldMesh, shapeIndex, frameIndex);

                        oldMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                    }
                }
            }
        }

        public static void CopyBlendShapeFillteredFromMesh(Mesh newMesh, Mesh oldMesh, Predicate<int> accept)
        {
            // 复制所有 blendshape
            for (int shapeIndex = 0; shapeIndex < oldMesh.blendShapeCount; shapeIndex++)
            {
                if (accept(shapeIndex))
                {
                    for (int frameIndex = 0; frameIndex < oldMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                    {
                        string shapeName = oldMesh.GetBlendShapeName(shapeIndex);
                        float weight = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                        var oldMeshFrame = new BlendShapeFrame(oldMesh, shapeIndex, frameIndex);

                        oldMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                    }
                }
            }
        }

        public static void CopyBlendShapeSortedFromMesh(Mesh newMesh, Mesh oldMesh, Func<Mesh, IEnumerable<(int, string)>> getSortedBlendShapeListWithName)
        {
            // 复制所有 blendshape
            foreach (var (shapeIndex, shapeName) in getSortedBlendShapeListWithName(oldMesh))
            {
                for (int frameIndex = 0; frameIndex < oldMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                {
                    float weight = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                    var copyedMeshFrame = new BlendShapeFrame(oldMesh, shapeIndex, frameIndex);

                    copyedMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                }
            }
        }
    }
}