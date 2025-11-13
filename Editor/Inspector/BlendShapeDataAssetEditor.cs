using UnityEngine;
using UnityEditor;

namespace io.github.colorlesscolor.blendshapeeditor
{
    [CustomEditor(typeof(BlendShapeDataAsset))]
    public class BlendShapeDataAssetEditor : Editor
    {
        Vector2 scrollPos = new();
        public void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            BlendShapeDataAsset asset = (BlendShapeDataAsset)target;
            EditorGUILayout.LabelField($"顶点数: {asset.vertexCount}");

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField($"混合形状列表:");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var blendshape in asset.blendShapeDataList)
            {
                EditorGUILayout.LabelField($"{blendshape.blendShapeName}");
            }
            EditorGUILayout.EndScrollView();
        }
    }
}