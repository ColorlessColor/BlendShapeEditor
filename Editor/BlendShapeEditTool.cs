using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace io.github.colorlesscolor.blendshapeeditor
{
    /// <summary>
    /// BlendShapeEditor
    /// 作者: https://hyular.booth.pm/items/4662982
    /// 二次修改: https://hrenact.github.io/HrenactNET/BlendShapeEditor/Description
    /// </summary>
    public class BlendShapeEditTool : EditorWindow
    {
        /// <summary>
        /// 目标 SkinnedMeshRenderer
        /// </summary>
        private SkinnedMeshRenderer smr;

        // UI
        private int tab;
        private string[] tabText = new string[3] { "排序 & 重命名", "创建", "对称分割" };
        private List<MorphData> morphDatas;
        private List<MorphData> selectedMorphDatas;
        private string[] morphNames;
        private ReorderableList sortMorphList;
        private ReorderableList blendMorphList;
        private const float minBlendShapeWeight = 0f;
        private const float maxBlendShapeWeight = 100f;

        private int separateMorphID;
        private float separateSmoothRange = 0.001f;
        private Vector2 scrollPos = Vector2.zero;
        /// <summary>
        /// 新增 BlendShape 默认命名
        /// </summary>
        private string newBlendShapeName = "Morph";
        bool blendShapeNameFlag = false;

        /// <summary>
        /// 控制是否显示警告弹窗
        /// </summary>
        private const string ShowWarningKey = "BlendShapeEditor.ShowWarning";

        /// <summary>
        /// 形变列表
        /// </summary>
        private struct MorphData
        {
            public int shapeIndex;
            public string shapeName;
            public float weight;

            public MorphData(int shapeIndex, string shapeName)
            {
                this.shapeIndex = shapeIndex;
                this.shapeName = shapeName;
                weight = 100f;
            }

            public MorphData(int shapeIndex, string shapeName, float weight)
            {
                this.shapeIndex = shapeIndex;
                this.shapeName = shapeName;
                this.weight = weight;
            }
        }

        // 注册菜单
        [MenuItem("Tools/BlendShape Edit Tool")]
        private static void Create()
        {
            //ウインドウ作成
            GetWindow<BlendShapeEditTool>("BlendShape Edit Tool");
        }

        // 启动时初始化及检查
        private void OnEnable()
        {
            ResetMorphDatas();
            scrollPos = Vector2.zero;
            if (!EditorPrefs.GetBool(ShowWarningKey, false))
            {
                ShowWarningDialog();
            }
        }

        // 显示警告弹窗
        private void ShowWarningDialog()
        {
            if (EditorUtility.DisplayDialog("警告", "请注意，挂载到物体上的部分 NDMF 系组件会导致插件无法正常工作，如有必要请事先禁用或移除。\n\n此弹窗仅显示一次。", "确定"))
            {
                // 设置 EditorPrefs 标志，表示警告已显示
                EditorPrefs.SetBool(ShowWarningKey, true);
            }
        }

        // GUI
        private void OnGUI()
        {

            EditorGUI.BeginChangeCheck();
            smr = EditorGUILayout.ObjectField("Skinned Mesh Renderer", smr, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
            if (EditorGUI.EndChangeCheck() && smr)
            {
                // リセット
                ResetMorphDatas();
                scrollPos = Vector2.zero;
                switch (tab)
                {
                    case 0:
                        blendShapeNameFlag = CheckBlendShapeName();
                        break;
                    case 1:
                        blendShapeNameFlag = CheckBlendShapeName(newBlendShapeName);
                        break;
                }
            }

            GUILayout.Space(10);

            if (smr)
            {
                if (smr.sharedMesh.blendShapeCount > 0)
                {
                    // 功能选择
                    EditorGUI.BeginChangeCheck();
                    tab = GUILayout.Toolbar(tab, tabText);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // 切换 TAB 时重新初始化
                        ResetMorphDatas();
                        scrollPos = Vector2.zero;

                        switch (tab)
                        {
                            case 0:
                                blendShapeNameFlag = CheckBlendShapeName();
                                break;
                            case 1:
                                blendShapeNameFlag = CheckBlendShapeName(newBlendShapeName);

                                /* 删除 切换选项卡时的初始化 SMR blendshape 值
                                // BlendShapeのプレビュー
                                for(int i=0;i<smr.sharedMesh.blendShapeCount;i++){
                                    smr.SetBlendShapeWeight(i, 0f);
                                }
                                */

                                for (int i = 0; i < selectedMorphDatas.Count; i++)
                                {
                                    smr.SetBlendShapeWeight(selectedMorphDatas[i].shapeIndex, selectedMorphDatas[i].weight);
                                }
                                break;
                        }
                    }

                    // 各機能の描画
                    switch (tab)
                    {
                        case 0:     // Sort
                            DoSortTab();
                            break;
                        case 1:     // Blend
                            DoBlendTab();
                            break;
                        case 2:     // Separate
                            DoSeparateTab();
                            break;
                        default:

                            break;
                    }
                }
                else
                {
                    // BlendShapeがない場合
                    EditorGUILayout.HelpBox("此网格没有 BlendShape", MessageType.Info);
                }
            }
            else
            {
                // BlendShapeがない場合
                EditorGUILayout.HelpBox("请选择一个 Skinned Mesh Renderer", MessageType.Info);
            }
        }

        // MorphDatasの初期化
        private void ResetMorphDatas()
        {
            if (smr)
            {
                morphDatas = new List<MorphData>();
                morphNames = new string[smr.sharedMesh.blendShapeCount];
                for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                {
                    morphDatas.Add(new MorphData(i, smr.sharedMesh.GetBlendShapeName(i)));
                    morphNames[i] = smr.sharedMesh.GetBlendShapeName(i);
                }

                selectedMorphDatas = new List<MorphData>();
                if (smr.sharedMesh.blendShapeCount > 0)
                {
                    selectedMorphDatas.Add(morphDatas[0]);
                }

                sortMorphList = null;
                blendMorphList = null;
            }
        }

        // Sort機能
        private void DoSortTab()
        {
            EditorGUILayout.HelpBox("你可以在此对 BlendShape 进行排序、重命名以及删除操作", MessageType.Info);
            if (sortMorphList == null)
            {
                // ReorderableListの準備
                sortMorphList = new ReorderableList(morphDatas, typeof(MorphData));
                sortMorphList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "BlendShape");
                sortMorphList.drawElementCallback = (rect, i, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(rect, "Morph " + i);
                    rect.x += 75;
                    rect.width = rect.width - 60;
                    morphDatas[i] = new MorphData(morphDatas[i].shapeIndex, EditorGUI.TextField(rect, morphDatas[i].shapeName));
                };
                sortMorphList.onCanAddCallback = list =>
                {
                    return false;
                };
            }
            EditorGUI.BeginChangeCheck();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            sortMorphList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                // Morph名の重複チェック
                blendShapeNameFlag = CheckBlendShapeName();
            }

            GUILayout.Space(10);
            if (!blendShapeNameFlag)
            {
                EditorGUILayout.HelpBox("存在相同的 BlendShape 名称", MessageType.Error);
            }
            else
            {
                if (GUILayout.Button("应用 BlendShape 修改"))
                {
                    SaveMesh(SortBlendShapeMesh(smr.sharedMesh));
                }
            }
            GUILayout.Space(10);
            if (GUILayout.Button("重置 BlendShape 修改"))
            {
                ResetMorphDatas();
            }
        }

        // Blend機能
        private void DoBlendTab()
        {
            EditorGUILayout.HelpBox("修改此处的 BlendShape 值将会同步应用至 Inspector 的 BlendShape 的值", MessageType.Info);

            // EditorGUILayout.BeginHorizontal();
            // minBlendShapeWeight = EditorGUILayout.FloatField("权重下限", minBlendShapeWeight);
            // maxBlendShapeWeight = EditorGUILayout.FloatField("权重上限", maxBlendShapeWeight);
            // EditorGUILayout.EndHorizontal();
            // if (minBlendShapeWeight > maxBlendShapeWeight)
            // {
            //     minBlendShapeWeight = maxBlendShapeWeight;
            // }

            if (blendMorphList == null)
            {
                // ReorderableListの準備
                blendMorphList = new ReorderableList(selectedMorphDatas, typeof(MorphData));
                blendMorphList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "BlendShape");
                blendMorphList.drawElementCallback = (rect, i, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(rect, "Morph " + i);
                    rect.x += 75;
                    rect.width = (rect.width - 70) / 2f;
                    int id = EditorGUIKit.Popup(rect, selectedMorphDatas[i].shapeIndex, morphNames);
                    rect.x += rect.width + 10;
                    selectedMorphDatas[i] = new MorphData(morphDatas[id].shapeIndex, morphDatas[id].shapeName, EditorGUI.Slider(rect, selectedMorphDatas[i].weight, minBlendShapeWeight, maxBlendShapeWeight));
                };
                blendMorphList.onAddCallback = list =>
                {
                    selectedMorphDatas.Add(new MorphData(morphDatas[0].shapeIndex, morphDatas[0].shapeName));
                };
                blendMorphList.onCanRemoveCallback = list =>
                {
                    return list.count > 1;
                };
            }
            EditorGUI.BeginChangeCheck();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            blendMorphList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                // BlendShapeのプレビュー

                /* 删除 Blend 操作预览
                for(int i=0;i<smr.sharedMesh.blendShapeCount;i++){
                    smr.SetBlendShapeWeight(i, 0f);
                }
                */

                for (int i = 0; i < selectedMorphDatas.Count; i++)
                {
                    smr.SetBlendShapeWeight(selectedMorphDatas[i].shapeIndex, selectedMorphDatas[i].weight);
                }
            }

            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            newBlendShapeName = EditorGUILayout.TextField("新 BlendShape 名称", newBlendShapeName);
            if (EditorGUI.EndChangeCheck())
            {
                // Morph名の重複チェック
                blendShapeNameFlag = CheckBlendShapeName(newBlendShapeName);
            }

            GUILayout.Space(10);

            if (!blendShapeNameFlag)
            {
                EditorGUILayout.HelpBox("已存在相同的 BlendShape 名称", MessageType.Error);
            }
            else if (newBlendShapeName == "")
            {
                EditorGUILayout.HelpBox("请键入 BlendShape 名称", MessageType.Error);
            }
            else
            {
                // 合成
                if (GUILayout.Button("创建 BlendShape"))
                {
                    SaveMesh(BlendBlendShapeMesh(smr.sharedMesh, newBlendShapeName));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                // 反転
                if (GUILayout.Button("创建反向 BlendShape"))
                {
                    SaveMesh(InverseBlendShapeMesh(smr.sharedMesh, newBlendShapeName));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                // 連結
                if (GUILayout.Button("按顺序创建多帧 BlendShape"))
                {
                    SaveMesh(ConnectBlendShapeMesh(smr.sharedMesh, newBlendShapeName));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                // 累積連結
                if (GUILayout.Button("按顺序叠加创建多帧 BlendShape"))
                {
                    SaveMesh(BlendThenConnectBlendShapeMesh(smr.sharedMesh, newBlendShapeName));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                // 基本形状に適用
                if (GUILayout.Button("应用形变至基础网格并创建反向 BlendShape"))
                {
                    SaveMesh(ApplyBaseShapeMeshCreateInverse(smr.sharedMesh, newBlendShapeName));
                    blendShapeNameFlag = false;
                }
            }
        }

        //Morph名の重複チェック
        private bool CheckBlendShapeName()
        {
            List<string> checkedNames = new List<string>();
            for (int i = 0; i < morphDatas.Count; i++)
            {
                if (!checkedNames.Contains(morphDatas[i].shapeName))
                {
                    checkedNames.Add(morphDatas[i].shapeName);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckBlendShapeName(string name)
        {
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                if (smr.sharedMesh.GetBlendShapeName(i) == name)
                {
                    return false;
                }
            }
            return true;
        }

        //Separate機能
        private void DoSeparateTab()
        {
            EditorGUILayout.HelpBox("此工具将会为你选择的 BlendShape 分别制作对应的仅左半部分 _L 和仅右半部分 _R 版本", MessageType.Info);
            separateMorphID = EditorGUILayoutKit.Popup(separateMorphID, morphNames);
            separateSmoothRange = EditorGUILayout.Slider("平滑半径", separateSmoothRange, 0.001f, 10f);
            if (GUILayout.Button("分割 BlendShape"))
            {
                SaveMesh(SeparateBlendShapeMesh(smr.sharedMesh));
            }
        }

        /* 删除 关闭窗口时 BlendShape 值归零
        //Window閉じた時の処理
        void OnDestroy(){
            if(smr){
                for(int i=0;i<smr.sharedMesh.blendShapeCount;i++){
                    smr.SetBlendShapeWeight(i, 0);
                }
            }
        }
        */

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //実際にモデルを編集する
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        //Meshの保存
        private void SaveMesh(Mesh m)
        {
            // 替换网格并保持旧权重
            MeshUtils.ReplaceSharedMeshKeepBlendShapeWeight(smr, m);

            // 保存 Mesh
            MeshUtils.CreateMeshAssetAndSave(smr.sharedMesh, smr.gameObject.name);
            AssetDatabase.SaveAssets();

            // 重置 MorphDatas
            ResetMorphDatas();
        }

        //MorphDatasに従って整理したMeshのコピーを作成
        private Mesh SortBlendShapeMesh(Mesh oldMesh)
        {
            return new MeshBlendShapeBuilder(oldMesh).SetCopyOldBlendShapesMethod
            (
                (newMesh, oldMesh) =>
                {
                    MeshUtils.CopyBlendShapeSortedFromMesh(newMesh, oldMesh, (_) => morphDatas.Select(it => (it.shapeIndex, it.shapeName)));
                }
            )
            .BuildMesh();
        }

        // 混合创建 BlendShape
        private Mesh BlendBlendShapeMesh(Mesh oldMesh, string newBlendShapeName)
        {
            MeshBlendShapeBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendShapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.BlendFramesToMesh(newBlendShapeName).BuildMesh();
        }

        // 混合并反转后创建 BlendShape
        private Mesh InverseBlendShapeMesh(Mesh oldMesh, string newBlendShapeName)
        {
            MeshBlendShapeBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendShapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.BlendFramesInverseToMesh(newBlendShapeName).BuildMesh();
        }

        // 按顺序连接多帧 BlendShape
        private Mesh ConnectBlendShapeMesh(Mesh oldMesh, string newBlendShapeName)
        {
            MeshBlendShapeBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendShapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.ConnectBlendFramesToMesh(newBlendShapeName).BuildMesh();
        }

        // 按顺序叠加创建多帧 BlendShape
        private Mesh BlendThenConnectBlendShapeMesh(Mesh oldMesh, string newBlendShapeName)
        {
            MeshBlendShapeBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendShapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.BlendAndConnectFramesToMesh(newBlendShapeName).BuildMesh();
        }

        // 应用形变至基础网格并创建反向 BlendShape
        private Mesh ApplyBaseShapeMeshCreateInverse(Mesh oldMesh, string inverseBlendShapeName)
        {
            MeshBlendShapeBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendShapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.ApplyToBaseMeshAndCreateInverse(inverseBlendShapeName).BuildMesh();
        }

        //指定したBlendShapeの左右分割
        private Mesh SeparateBlendShapeMesh(Mesh oldMesh)
        {
            string separateMorphName = oldMesh.GetBlendShapeName(separateMorphID);
            string leftName = separateMorphName + "_L";
            string rightName = separateMorphName + "_R";
            HashSet<string> filter = new() { leftName, rightName };

            MeshBlendShapeBuilder builder = new(oldMesh);

            return builder.SetCopyOldBlendShapesMethod
            (
                (newMesh, oldMesh) =>
                {
                    MeshUtils.CopyBlendShapeFillteredFromMesh(newMesh, oldMesh,
                        (string name) => !filter.Contains(name)
                    );
                }
            )
            .AddBlendShapeFrame(separateMorphID, 100f)
            .ApplyMaskedBlendShapeToMesh
            (
                leftName,
                mesh =>
                {
                    float[] weight = new float[mesh.vertexCount];
                    for (int i = 0; i < mesh.vertexCount; i++)
                    {
                        weight[i] = Mathf.InverseLerp(separateSmoothRange, -separateSmoothRange, mesh.vertices[i].x);
                    }
                    return weight;
                }
            )
            .ApplyMaskedBlendShapeToMesh
            (
                rightName,
                mesh =>
                {
                    float[] weight = new float[mesh.vertexCount];
                    for (int i = 0; i < mesh.vertexCount; i++)
                    {
                        weight[i] = Mathf.InverseLerp(-separateSmoothRange, separateSmoothRange, mesh.vertices[i].x);
                    }
                    return weight;
                }
            )
            .BuildMesh();
        }
    }

    /// <summary>
    /// 自定义的 EditorGUILauout 工具箱，自动布局
    /// https://zhuanlan.zhihu.com/p/626207442
    /// </summary>
    public static class EditorGUILayoutKit
    {
        /// <summary>
        /// 制作一个通用弹窗选择字段
        /// </summary>
        /// <param name="selectIndex"></param>
        /// <param name="displayedOptions"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static int Popup(int selectIndex, string[] displayedOptions, params GUILayoutOption[] options)
        {

            if (displayedOptions == null || displayedOptions.Length == 0)
                return 0;

            int contrelId = GUIUtility.GetControlID(FocusType.Passive);

            string display = "（空）";

            if (selectIndex >= 0 && selectIndex < displayedOptions.Length)
                display = displayedOptions[selectIndex];

            if (GUILayout.Button(display, options))
            {
                CustomPopup popup = new CustomPopup();
                popup.select = selectIndex;
                popup.displayedOptions = displayedOptions;
                popup.info = new CustomPopupInfo(contrelId, selectIndex);
                CustomPopupInfo.instance = popup.info;
                PopupWindow.Show(CustomPopupTempStyle.Get(contrelId).rect, popup);
            }

            if (Event.current.type == EventType.Repaint)
            {
                CustomPopupTempStyle style = new CustomPopupTempStyle();
                style.rect = GUILayoutUtility.GetLastRect();
                CustomPopupTempStyle.Set(contrelId, style);
            }
            return CustomPopupInfo.Get(contrelId, selectIndex);
        }
    }

    /// <summary>
    /// 自定义的 EditorGUI 工具箱，手动布局
    /// </summary>
    public static class EditorGUIKit
    {
        /// <summary>
        /// 制作一个通用弹窗选择字段。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="selectIndex"></param>
        /// <param name="displayedOptions"></param>
        /// <returns></returns>
        public static int Popup(Rect position, int selectIndex, string[] displayedOptions)
        {

            if (displayedOptions == null || displayedOptions.Length == 0)
                return 0;

            int contrelId = GUIUtility.GetControlID(FocusType.Passive);

            string display = "（空）";

            if (selectIndex >= 0 && selectIndex < displayedOptions.Length)
                display = displayedOptions[selectIndex];

            if (GUI.Button(position, display))
            {
                CustomPopup popup = new CustomPopup();
                popup.select = selectIndex;
                popup.displayedOptions = displayedOptions;
                popup.info = new CustomPopupInfo(contrelId, selectIndex);
                CustomPopupInfo.instance = popup.info;
                PopupWindow.Show(CustomPopupTempStyle.Get(contrelId).rect, popup);
            }

            if (Event.current.type == EventType.Repaint)
            {
                CustomPopupTempStyle style = new CustomPopupTempStyle();
                style.rect = GUILayoutUtility.GetLastRect();
                CustomPopupTempStyle.Set(contrelId, style);
            }
            return CustomPopupInfo.Get(contrelId, selectIndex);
        }
    }

    /// <summary>
    /// 打开popup的选择界面
    /// </summary>
    public class CustomPopup : PopupWindowContent
    {
        public int select;
        public string[] displayedOptions;
        public bool hasopen;
        string filter;
        public CustomPopupInfo info;

        Vector2 scrollPosition;
        public override void OnGUI(Rect rect)
        {
            editorWindow.minSize = new Vector2(400, 400);
            GUILayout.Label("搜索：");
            filter = EditorGUILayout.TextField(filter);
            GUILayout.Space(20);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < displayedOptions.Length; i++)
            {
                string info = displayedOptions[i];

                if (this.filter != null && this.filter.Length != 0)
                {
                    if (!info.Contains(this.filter, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                }

                if (select == i)
                {
                    info = "--->" + info;
                }
                if (GUILayout.Button(info))
                {
                    select = i;
                    this.info.Set(i);
                    editorWindow.Close();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public override void OnOpen()
        {
            hasopen = true;
            base.OnOpen();
        }
    }


    /// <summary>
    /// 自定义Popup的Style缓存可以有多个参数，不止是Rect，也可以自定义其他的
    /// </summary>
    public class CustomPopupTempStyle
    {

        public Rect rect;

        static Dictionary<int, CustomPopupTempStyle> temp = new();

        public static CustomPopupTempStyle Get(int contrelId)
        {
            if (!temp.ContainsKey(contrelId))
            {
                return null;
            }
            CustomPopupTempStyle t;
            temp.Remove(contrelId, out t);
            return t;
        }

        public static void Set(int contrelId, CustomPopupTempStyle style)
        {
            temp[contrelId] = style;
        }
    }

    /// <summary>
    /// 存储popup的信息如选择等
    /// </summary>
    public class CustomPopupInfo
    {
        public int SelectIndex { get; private set; }
        public int contrelId;
        public bool used;
        public static CustomPopupInfo instance;

        public CustomPopupInfo(int contrelId, int selectIndex)
        {
            this.contrelId = contrelId;
            this.SelectIndex = selectIndex;
        }

        public static int Get(int controlID, int selected)
        {
            if (instance == null)
            {
                return selected;
            }

            if (instance.contrelId == controlID && instance.used)
            {
                GUI.changed = selected != instance.SelectIndex;
                selected = instance.SelectIndex;
                instance = null;
            }

            return selected;
        }

        public void Set(int selected)
        {
            SelectIndex = selected;
            used = true;
        }
    }
}