using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC;

namespace io.github.colorlesscolor.blendshapeeditor
{
    public class BlendShapeExportTool : EditorWindow
    {
        private string[] tabText = { "导出", "导入" };
        private int tabIndex = 0;

        // Export Settings
        private SkinnedMeshRenderer exportSMR;
        private List<BlendShapeCheck> exportList;
        private Vector2 exportListScrollPos;

        // Import Settings
        private SkinnedMeshRenderer importSMR;
        private int importSmrVertexCount;
        private List<BlendShapeCheck> importList;
        private Vector2 importListScrollPos;
        private BlendShapeDataAsset importAsset;
        private bool autoRenameSameNameBS;

        private class BlendShapeCheck
        {
            public int shapeIndex;
            public bool isChecked;
            public string originalBlendShapeName;
            public string rename;
        }

        [MenuItem("Tools/BlendShape Export Tool")]
        private static void Create()
        {
            GetWindow<BlendShapeExportTool>("BlendShape Export Tool");
        }

        private void OnEnable()
        {
            exportList = new();
            importList = new();
        }

        private void OnGUI()
        {
            tabIndex = GUILayout.Toolbar(tabIndex, tabText);
            switch (tabIndex)
            {
                case 0:
                    OnExportGUI();
                    break;
                case 1:
                    OnImportGUI();
                    break;
            }
        }

        private void OnExportGUI()
        {
            // Export Settings
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            exportSMR = EditorGUILayout.ObjectField("Skinned Mesh Renderer", exportSMR, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
            if (EditorGUI.EndChangeCheck() || GUILayout.Button("刷新"))
            {
                if (exportSMR) exportList = RebuildExportCheckList(exportSMR.sharedMesh, exportList);
                else exportList.Clear();
            }
            EditorGUILayout.EndHorizontal();

            if (exportSMR)
            {
                exportListScrollPos = EditorGUILayout.BeginScrollView(exportListScrollPos);
                DrawCheckList(exportList);
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("全选")) SelectAllExportCheckList(exportList);

                if (GUILayout.Button("清空选择")) UncheckAllExportCheckList(exportList);

                // Export to asset
                if (GUILayout.Button("导出"))
                {
                    List<BlendShapeCheck> filterList = exportList.Where(it => it.isChecked).ToList();
                    bool haveSameName = filterList.GroupBy(it => it.rename).Select(group => group.Count()).Any(count => count != 1);

                    if (filterList.Count > 0 && !haveSameName)
                    {
                        string savePath = EditorUtility.SaveFilePanelInProject("保存导出的混合形状", $"{exportSMR.gameObject.name}", "asset", "保存导出的混合形状");
                        if (savePath.Length != 0)
                        {
                            SaveBlendShapeToAssets(exportSMR.sharedMesh, filterList, savePath);
                        }
                    }
                    else if (haveSameName)
                    {
                        EditorUtility.DisplayDialog("导出失败", "导出的混合形状不能有相同命名", "Ok");
                    }
                    else if (filterList.Count > 0)
                    {
                        EditorUtility.DisplayDialog("导出失败", "请至少导出一个混合形状", "Ok");
                    }
                }
            }
        }

        private void OnImportGUI()
        {
            // Import Settings
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            importSMR = EditorGUILayout.ObjectField("Skinned Mesh Renderer", importSMR, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
            if (EditorGUI.EndChangeCheck())
            {
                if (importSMR) importSmrVertexCount = importSMR.sharedMesh.vertexCount;
                else importSmrVertexCount = -1;
            }

            EditorGUI.BeginChangeCheck();
            importAsset = EditorGUILayout.ObjectField("BlendShapeDataAsset", importAsset, typeof(BlendShapeDataAsset), false) as BlendShapeDataAsset;
            if (EditorGUI.EndChangeCheck())
            {
                if (importAsset) RebuildImportCheckList(importAsset, importList);
                else importList.Clear();
            }

            if (GUILayout.Button("刷新"))
            {
                if (importSMR) importSmrVertexCount = importSMR.sharedMesh.vertexCount;
                else importSmrVertexCount = -1;

                if (importAsset) RebuildImportCheckList(importAsset, importList);
                else importList.Clear();
            }
            EditorGUILayout.EndHorizontal();


            if (importSMR && importAsset && (importSmrVertexCount != importAsset.vertexCount))
            {
                EditorGUILayout.HelpBox("导入混合形状与网格顶点数不匹配", MessageType.Error);
            }

            // 显示导入列表
            if (importAsset)
            {
                importListScrollPos = EditorGUILayout.BeginScrollView(importListScrollPos);
                DrawCheckList(importList);
                EditorGUILayout.EndScrollView();

                autoRenameSameNameBS = GUILayout.Toggle(autoRenameSameNameBS, "自动重命名同名混合形状而不是原地替换");
                if (GUILayout.Button("全选")) SelectAllExportCheckList(importList);
                if (GUILayout.Button("清空选择")) UncheckAllExportCheckList(importList);
            }

            // 检查是否满足导入条件
            using (new EditorGUI.DisabledScope(!(importSMR && importAsset && (importSmrVertexCount == importAsset.vertexCount))))
            {
                if (GUILayout.Button("导入"))
                {
                    // 处理混合形状并生成新网格
                    var filterList = importList.Where(it => it.isChecked).ToList();
                    bool haveSameName = filterList.GroupBy(it => it.rename).Select(group => group.Count()).Any(count => count != 1);
                    if (filterList.Count > 0 && !haveSameName)
                    {
                        // 创建新网格
                        Mesh newMesh;
                        // 要考虑自动重命名的话...
                        if (autoRenameSameNameBS) newMesh = ImportBlendShapeToMeshAutoRename(importSMR.sharedMesh, importAsset, filterList);
                        else newMesh = ImportBlendShapeToMeshInplace(importSMR.sharedMesh, importAsset, filterList);

                        // 保存资产
                        MeshUtils.CreateMeshAssetAndSave(newMesh, importSMR.gameObject.name);
                        // 替换 SMR 的网格并保持权重
                        MeshUtils.ReplaceSharedMeshKeepBlendShapeWeight(importSMR, newMesh);
                        AssetDatabase.SaveAssets();
                    }
                    else if (haveSameName)
                    {
                        EditorUtility.DisplayDialog("导入失败", "导入的混合形状不能有相同命名", "Ok");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("导入失败", "请至少导入一个混合形状", "Ok");
                    }
                }
            }
        }

        private static List<BlendShapeCheck> RebuildExportCheckList(Mesh mesh, List<BlendShapeCheck> checkList)
        {
            checkList.Clear();
            for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; shapeIndex++)
            {
                string blendShapeName = mesh.GetBlendShapeName(shapeIndex);
                checkList.Add
                (
                    new BlendShapeCheck()
                    {
                        shapeIndex = shapeIndex,
                        isChecked = false,
                        originalBlendShapeName = blendShapeName,
                        rename = blendShapeName
                    }
                );
            }
            return checkList;
        }

        private static List<BlendShapeCheck> RebuildImportCheckList(BlendShapeDataAsset importAsset, List<BlendShapeCheck> checkList)
        {
            checkList.Clear();
            for (int shapeIndex = 0; shapeIndex < importAsset.blendShapeDataList.Count; shapeIndex++)
            {
                checkList.Add
                (
                    new BlendShapeCheck()
                    {
                        shapeIndex = shapeIndex,    // 这里用于 importAsset.blendShapeDataList 索引
                        isChecked = false,
                        originalBlendShapeName = importAsset.blendShapeDataList[shapeIndex].blendShapeName,
                        rename = importAsset.blendShapeDataList[shapeIndex].blendShapeName
                    }
                );
            }
            return checkList;
        }

        private static void UncheckAllExportCheckList(List<BlendShapeCheck> checkList)
        {
            foreach (var item in checkList)
            {
                item.isChecked = false;
            }
        }

        private static void SelectAllExportCheckList(List<BlendShapeCheck> checkList)
        {
            foreach (var item in checkList)
            {
                item.isChecked = true;
            }
        }

        private static void DrawCheckList(List<BlendShapeCheck> checkList)
        {
            foreach (BlendShapeCheck item in checkList)
            {
                EditorGUILayout.BeginHorizontal();
                // why 15f???
                GUILayoutOption[] options = { GUILayout.Width(EditorGUIUtility.currentViewWidth / 2f - 15f) };
                item.isChecked = GUILayout.Toggle(item.isChecked, item.originalBlendShapeName, options);
                item.rename = GUILayout.TextField(item.rename, options);
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void SaveBlendShapeToAssets(Mesh mesh, List<BlendShapeCheck> blendShapeList, string savePath)
        {
            ScriptableObject scriptableObject = CreateInstance<BlendShapeDataAsset>();
            BlendShapeDataAsset asset = (BlendShapeDataAsset)scriptableObject;
            asset.SetVertexCount(mesh.vertexCount);
            foreach (BlendShapeCheck blendShape in blendShapeList)
            {
                BlendShapeData data = new(blendShape.rename, mesh.vertexCount);
                for (int frameIndex = 0; frameIndex < mesh.GetBlendShapeFrameCount(blendShape.shapeIndex); frameIndex++)
                {
                    BlendShapeFrameData frame = new(data.vertexCount, mesh.GetBlendShapeFrameWeight(blendShape.shapeIndex, frameIndex));
                    mesh.GetBlendShapeFrameVertices(blendShape.shapeIndex, frameIndex, frame.deltaVertices, frame.deltaNormals, frame.deltaTangents);
                    data.AddFrame(frame);
                }
                asset.AddBlendShapeData(data);
            }
            scriptableObject.MarkDirty();

            AssetDatabase.CreateAsset(asset, savePath);
            AssetDatabase.SaveAssets();
        }

        private static Mesh ImportBlendShapeToMeshInplace(Mesh oldMesh, BlendShapeDataAsset blendShapeDataAsset, List<BlendShapeCheck> checkedBlendShapeList)
        {
            Mesh newMesh = MeshUtils.CloneMeshOnly(oldMesh);

            // 转换导入列表为字典用来查表
            Dictionary<string, BlendShapeCheck> checkedBlendShapesDict = new();
            foreach (BlendShapeCheck item in checkedBlendShapeList)
            {
                // 这里的 shape.shapeIndex 是指 BlendShapeDataAsset.GetBlendShapeByIndex 的 Index
                // 前面就是从这生成的
                checkedBlendShapesDict.TryAdd(item.rename, item);
            }

            // 遍历旧网格的混合形状, 视情况插入新网格
            for (int shapeIndex = 0; shapeIndex < oldMesh.blendShapeCount; shapeIndex++)
            {
                string oldMeshBlendShapeName = oldMesh.GetBlendShapeName(shapeIndex);

                if (checkedBlendShapesDict.TryGetValue(oldMeshBlendShapeName, out BlendShapeCheck checkedBlendShape))
                {
                    // 如果导入列表里有同名的, 替换之
                    BlendShapeData blendShapeData = blendShapeDataAsset.GetBlendShapeByIndex(checkedBlendShape.shapeIndex);
                    MeshUtils.CopyBlendShapeFromBlendShapeData(newMesh, blendShapeData, checkedBlendShape.rename);

                    // 导入过了, 删除之
                    // 什么叫旧网格可能有重名的 BlendShape ?? 我不管，只替换第一个
                    checkedBlendShapesDict.Remove(oldMeshBlendShapeName);
                }
                else
                {
                    // 没有就原样拷贝
                    MeshUtils.CopyBlendShapeByIndexFromMesh(newMesh, oldMesh, shapeIndex);
                }
            }

            // 剩余混合形状需要有序导入
            // 我不知道为什么一定要有序, 但是考虑到用户友好, 反正你不会在乎这点 CPU 时间吧?
            foreach (BlendShapeCheck item in checkedBlendShapeList)
            {
                if (checkedBlendShapesDict.TryGetValue(item.originalBlendShapeName, out BlendShapeCheck checkedBlendShape))
                {
                    // 剩下没导入的还在 dict 中
                    BlendShapeData blendShapeData = blendShapeDataAsset.GetBlendShapeByIndex(checkedBlendShape.shapeIndex);
                    MeshUtils.CopyBlendShapeFromBlendShapeData(newMesh, blendShapeData, checkedBlendShape.rename);
                    // 就不删除浪费时间了
                }
            }

            // 还算简单
            return newMesh;
        }

        private static Mesh ImportBlendShapeToMeshAutoRename(Mesh oldMesh, BlendShapeDataAsset blendShapeDataAsset, List<BlendShapeCheck> checkedBlendShapeList)
        {
            Mesh newMesh = MeshUtils.CloneMeshOnly(oldMesh);

            // 原样拷贝原始网格的混合形状同时记录所有命名
            Dictionary<string, int> nameCounter = new();
            for (int shapeIndex = 0; shapeIndex < oldMesh.blendShapeCount; shapeIndex++)
            {
                string oldMeshBlendShapeName = oldMesh.GetBlendShapeName(shapeIndex);
                // 因为有自动重命名需求, 所以还得记一下重名次数
                nameCounter.TryAdd(oldMeshBlendShapeName, 0);
                // 原样拷贝混合形状
                MeshUtils.CopyBlendShapeByIndexFromMesh(newMesh, oldMesh, shapeIndex);
            }

            // 有序导入剩余混合形状, 但如果遇到重名的就名字序数 +1
            // 怎么这么麻烦啊
            foreach (BlendShapeCheck checkedBlendShape in checkedBlendShapeList)
            {
                // 运气好的话直接用
                string newName = checkedBlendShape.rename;

                // 可能多次发生碰撞! 所以只能循环检查
                string collisionName = newName;
                while (nameCounter.TryGetValue(newName, out int collisionCount))
                {
                    // 看看还在不在列表里, 还在我就得继续加长, 无语
                    collisionName = newName;
                    newName += $"_{collisionCount + 1}";
                }
                // 应该没重名了吧

                // 不一样就是撞了
                if (newName != collisionName) nameCounter[collisionName]++;

                // 无论如何得到的名字都可以安全写入
                BlendShapeData blendShapeData = blendShapeDataAsset.GetBlendShapeByIndex(checkedBlendShape.shapeIndex);
                MeshUtils.CopyBlendShapeFromBlendShapeData(newMesh, blendShapeData, newName);
            }

            // 噩梦结束了
            return newMesh;
        }
    }
}