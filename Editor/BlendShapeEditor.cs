using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;

namespace usefulunitytools.editorscript.BlendShapeEditor
{
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
            editorWindow.minSize = new Vector2(200, 400);
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

    /// <summary>
    /// BlendShapeEditor
    /// 作者: https://hyular.booth.pm/items/4662982
    /// 二次修改: https://hrenact.github.io/HrenactNET/BlendShapeEditor/Description
    /// </summary>
    public class BlendShapeEditor : EditorWindow
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
        private const float minBlendshapeWeight = 0f;
        private const float maxBlendshapeWeight = 100f;

        private int separateMorphID;
        private float separateSmoothRange = 0.001f;
        private Vector2 scrollPos = Vector2.zero;
        /// <summary>
        /// 新增 Blendshape 默认命名
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
        [MenuItem("Tools/BlendShape Editor")]
        private static void Create()
        {
            //ウインドウ作成
            GetWindow<BlendShapeEditor>("BlendShapeEditor");
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
                switch(tab)
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
            EditorGUILayout.HelpBox("你可以在此对 Blendshape 进行排序、重命名以及删除操作", MessageType.Info);
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
                if (GUILayout.Button("应用 Blendshape 修改"))
                {
                    SaveMesh(SortBlendShapeMesh(smr.sharedMesh));
                }
            }
            GUILayout.Space(10);
            if (GUILayout.Button("重置 Blendshape 修改"))
            {
                ResetMorphDatas();
            }
        }

        // Blend機能
        private void DoBlendTab()
        {
            EditorGUILayout.HelpBox("修改此处的 Blendshape 值将会同步应用至 Inspector 的 Blendshapes 的值", MessageType.Info);

            // EditorGUILayout.BeginHorizontal();
            // minBlendshapeWeight = EditorGUILayout.FloatField("权重下限", minBlendshapeWeight);
            // maxBlendshapeWeight = EditorGUILayout.FloatField("权重上限", maxBlendshapeWeight);
            // EditorGUILayout.EndHorizontal();
            // if (minBlendshapeWeight > maxBlendshapeWeight)
            // {
            //     minBlendshapeWeight = maxBlendshapeWeight;
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
                    selectedMorphDatas[i] = new MorphData(morphDatas[id].shapeIndex, morphDatas[id].shapeName, EditorGUI.Slider(rect, selectedMorphDatas[i].weight, minBlendshapeWeight, maxBlendshapeWeight));
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
                if (GUILayout.Button("应用形变至基础网格并创建反向 Blendshape"))
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
            EditorGUILayout.HelpBox("此工具将会为你选择的 Blendshape 分别制作对应的仅左半部分 _L 和仅右半部分 _R 版本", MessageType.Info);
            separateMorphID = EditorGUILayoutKit.Popup(separateMorphID, morphNames);
            separateSmoothRange = EditorGUILayout.Slider("平滑半径", separateSmoothRange, 0.001f, 10f);
            if (GUILayout.Button("分割 Blendshape"))
            {
                SaveMesh(SeparateBlendShapeMesh(smr.sharedMesh));
            }
        }

        /* 删除 关闭窗口时 Blendshape 值归零
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
            /*
            //BlendShapeのリセット
            for(int i=0;i<smr.sharedMesh.blendShapeCount;i++){
                smr.SetBlendShapeWeight(i, 0);
            }
            */

            var originalBlendShapeWeight = new Dictionary<string, float>();
            // 获取旧的 Dictionary<形态键名, 权重> 列表

            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                originalBlendShapeWeight.Add(smr.sharedMesh.GetBlendShapeName(i), smr.GetBlendShapeWeight(i));
            }

            // 归零，虽然不知道意义何在
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                smr.SetBlendShapeWeight(i, 0);
            }

            //Meshを差し替え
            smr.sharedMesh = m;

            // 重新填充权重
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                var blendShapeName = smr.sharedMesh.GetBlendShapeName(i);
                if (originalBlendShapeWeight.TryGetValue(blendShapeName, out var weight))
                {
                    smr.SetBlendShapeWeight(i, weight);
                }
            }

            //Meshを保存
            AssetDatabase.CreateAsset(smr.sharedMesh, "Assets/BlendShapeEditor/" + smr.transform.name + "_" + DateTime.Now.ToString("yyyy.M.d_HH.mm.ss_fff") + ".asset");
            AssetDatabase.SaveAssets();

            //MorphDatasの初期化
            ResetMorphDatas();
        }

        //MorphDatasに従って整理したMeshのコピーを作成
        private Mesh SortBlendShapeMesh(Mesh oldMesh)
        {
            return new MeshBuilder(oldMesh).SetCopyOldBlendshapesMethod
            (
                (newMesh, oldMesh) =>
                {
                    MeshUtils.CopyBlendShapeSorted(newMesh, oldMesh, (_) => morphDatas.Select(it => (it.shapeIndex, it.shapeName)));
                }
            )
            .BuildMesh();
        }

        // 混合创建 Blendshape
        private Mesh BlendBlendShapeMesh(Mesh oldMesh, string newBlendshapeName)
        {
            MeshBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendshapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.BlendFramesToMesh(newBlendshapeName).BuildMesh();
        }

        // 混合并反转后创建 Blendshape
        private Mesh InverseBlendShapeMesh(Mesh oldMesh, string newBlendshapeName)
        {
            MeshBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendshapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.BlendFramesInverseToMesh(newBlendshapeName).BuildMesh();
        }

        // 按顺序连接多帧 BlendShape
        private Mesh ConnectBlendShapeMesh(Mesh oldMesh, string newBlendshapeName)
        {
            MeshBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendshapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.ConnectBlendFramesToMesh(newBlendshapeName).BuildMesh();
        }

        // 按顺序叠加创建多帧 BlendShape
        private Mesh BlendThenConnectBlendShapeMesh(Mesh oldMesh, string newBlendshapeName)
        {
            MeshBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendshapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.BlendAndConnectFramesToMesh(newBlendshapeName).BuildMesh();
        }

        // 应用形变至基础网格并创建反向 Blendshape
        private Mesh ApplyBaseShapeMeshCreateInverse(Mesh oldMesh, string inverseBlendshapeName)
        {
            MeshBuilder builder = new(oldMesh);
            foreach (var morph in selectedMorphDatas)
            {
                builder.AddBlendshapeFrame(morph.shapeIndex, morph.weight);
            }
            return builder.ApplyToBaseMeshAndCreateInverse(inverseBlendshapeName).BuildMesh();
        }

        //指定したBlendShapeの左右分割
        private Mesh SeparateBlendShapeMesh(Mesh oldMesh)
        {
            string separateMorphName = oldMesh.GetBlendShapeName(separateMorphID);
            string leftName = separateMorphName + "_L";
            string rightName = separateMorphName + "_R";
            HashSet<string> filter = new() { leftName, rightName };

            MeshBuilder builder = new(oldMesh);

            return builder.SetCopyOldBlendshapesMethod
            (
                (newMesh, oldMesh) =>
                {
                    MeshUtils.CopyBlendShapeWithFilter(newMesh, oldMesh,
                        (string name) => !filter.Contains(name)
                    );
                }
            )
            .AddBlendshapeFrame(separateMorphID, 100f)
            .ApplyMaskedBlendshapeToMesh
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
            .ApplyMaskedBlendshapeToMesh
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


    public class MeshBuilder
    {
        /// <summary>
        ///  Dictionary<shapeIndex, List<weight, BlendshapeFrame>>
        /// </summary>
        private List<BlendshapeFrame> container;
        private Mesh newMesh;
        private Mesh oldMesh;

        /// <summary>
        /// 将 Blendshape 应用到基础网格的 pass
        /// (container, Mesh newMesh) => {}
        /// </summary>
        private MorphBaseMeshApply morphBaseMeshApply;

        /// <summary>
        /// 拷贝旧 Blendshape 的 pass
        /// (Mesh oldMesh, Mesh newMesh) => {}
        /// </summary>
        private BlendshapeCopyer blendshapeCopyer;

        /// <summary>
        /// 应用 Blendshape 的 passes
        /// (container, Mesh newMesh) => {}
        /// </summary>
        private List<FrameApplyPass> blendshapeFrameApplyPasses;

        /// <summary>
        /// 应用形变到基础网格
        /// </summary>
        /// <param name="container"></param>
        /// <param name="mesh"></param>
        public delegate void MorphBaseMeshApply(List<BlendshapeFrame> container, Mesh mesh);
        /// <summary>
        /// 拷贝旧网格上 Blendshape 的方法
        /// </summary>
        /// <param name="newMesh"></param>
        /// <param name="oldMesh"></param>
        public delegate void BlendshapeCopyer(Mesh newMesh, Mesh oldMesh);
        /// <summary>
        /// 处理 Blendshape Frames 的方法
        /// </summary>
        /// <param name="container"></param>
        /// <param name="mesh"></param>
        public delegate void FrameApplyPass(List<BlendshapeFrame> container, Mesh mesh);


        public MeshBuilder(Mesh mesh)
        {
            container = new();
            oldMesh = mesh;
            newMesh = MeshUtils.CloneMesh(oldMesh);
            // 默认不应用混合形状
            morphBaseMeshApply = null;
            // 默认拷贝方法
            blendshapeCopyer = new(MeshUtils.CopyBlendShape);
            // 默认不添加形态键
            blendshapeFrameApplyPasses = new();
        }

        public MeshBuilder AddBlendshapeFrame(int shapeIndex, float weight)
        {
            BlendshapeFrame frame = new(oldMesh.vertexCount);
            // TODO 此处应该有如何从 Mesh 里想办法插值出目标权重
            // 重新实现下面的部分

            //Weightが指定したWeight以上になる最小のフレームを探す
            int frameIndex = 0;
            for (int i = 0; i < oldMesh.GetBlendShapeFrameCount(shapeIndex); i++)
            {
                if (oldMesh.GetBlendShapeFrameWeight(shapeIndex, i) >= weight)
                {
                    frameIndex = i;
                    break;
                }
            }

            //フレームの適用割合を算出
            float applyRate;
            if (frameIndex == 0)
            {
                applyRate = weight / 100f;
            }
            else
            {
                applyRate = (weight - oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex - 1)) / (oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex) - oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex - 1));
            }

            oldMesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, frame.deltaVertices, frame.deltaNormals, frame.deltaTangents);

            //差分ベクトルに適応割合を乗算
            for (int i = 0; i < oldMesh.vertexCount; i++)
            {
                frame.deltaVertices[i] *= applyRate;
                frame.deltaNormals[i] *= applyRate;
                frame.deltaTangents[i] *= applyRate;
            }

            container.Add(frame);

            return this;
        }

        public MeshBuilder ClearFrames()
        {
            container.Clear();

            return this;
        }

        /// <summary>
        /// 使用自定义方法拷贝 Mesh 上的形态键
        /// </summary>
        /// <param name="copyer">(Mesh oldMesh, Mesh oldMesh) => {}</param>
        /// <returns></returns>
        public MeshBuilder SetCopyOldBlendshapesMethod(BlendshapeCopyer copyer)
        {
            blendshapeCopyer = copyer;
            return this;
        }

        /// <summary>
        /// 将混合形状帧混合后添加到 mesh
        /// </summary>
        /// <param name="m"></param>
        /// <param name="blendShapeName"></param>
        /// <returns></returns>
        public MeshBuilder BlendFramesToMesh(string blendShapeName)
        {
            blendshapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendshapeFrame(mesh.vertexCount);
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
        public MeshBuilder BlendFramesInverseToMesh(string blendShapeName)
        {
            blendshapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendshapeFrame(mesh.vertexCount);
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

        public MeshBuilder ConnectBlendFramesToMesh(string blendShapeName)
        {
            blendshapeFrameApplyPasses.Add
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

        public MeshBuilder BlendAndConnectFramesToMesh(string blendShapeName)
        {
            blendshapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendshapeFrame(mesh.vertexCount);
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

        public MeshBuilder ApplyToBaseMeshAndCreateInverse(string blendShapeName)
        {
            // 先应用形变到基础网格
            morphBaseMeshApply = new MorphBaseMeshApply((container, mesh) =>
            {
                var newFrame = new BlendshapeFrame(mesh.vertexCount);
                foreach (var frame in container)
                {
                    newFrame.AddInPlace(frame);
                }

                // 应用形变到新 mesh
                newFrame.ApplyBlendFrameToMesh(mesh);
            });

            // 中间会进行旧 Blendshape 拷贝
            // 也就是执行 blendshapeCopyer() 委托

            // 最后添加反转 Blendshape
            BlendFramesInverseToMesh(blendShapeName);

            return this;
        }

        public MeshBuilder ApplyMaskedBlendshapeToMesh(string blendShapeName, Func<Mesh, float[]> createMask)
        {
            blendshapeFrameApplyPasses.Add
            (
                new FrameApplyPass((container, mesh) =>
                {
                    var newFrame = new BlendshapeFrame(mesh.vertexCount);
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

            if (blendshapeCopyer != null)
            {
                blendshapeCopyer(newMesh, oldMesh);
            }

            if (blendshapeFrameApplyPasses.Count > 0)
            {
                foreach (var pass in blendshapeFrameApplyPasses)
                {
                    pass(container, newMesh);
                }
            }

            return newMesh;
        }
    }


    public class BlendshapeFrame
    {
        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public Vector3[] deltaTangents;

        /// <summary>
        /// 初始化空的 BlendshapeFrame
        /// </summary>
        /// <param name="vertexCount"></param>
        public BlendshapeFrame(int vertexCount)
        {
            deltaVertices = new Vector3[vertexCount];
            deltaNormals = new Vector3[vertexCount];
            deltaTangents = new Vector3[vertexCount];
        }


        public BlendshapeFrame(BlendshapeFrame frame)
        {
            deltaVertices = new Vector3[frame.deltaVertices.Length];
            deltaNormals = new Vector3[frame.deltaNormals.Length];
            deltaTangents = new Vector3[frame.deltaTangents.Length];
            frame.deltaVertices.CopyTo(deltaVertices.AsSpan());
            frame.deltaNormals.CopyTo(deltaNormals.AsSpan());
            frame.deltaTangents.CopyTo(deltaTangents.AsSpan());
        }

        /// <summary>
        /// 从 Mesh 指定的混合形状和帧初始化 BlendshapeFrame
        /// </summary>
        /// <param name="mesh">Mesh</param>
        /// <param name="shapeIndex">混合形状索引</param>
        /// <param name="frameIndex">关键帧索引</param>
        public BlendshapeFrame(Mesh mesh, int shapeIndex, int frameIndex)
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

        public void AddInPlace(BlendshapeFrame frame)
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

        public static BlendshapeFrame Lerp(BlendshapeFrame frameFrom, BlendshapeFrame frameTo, float lerpRate)
        {
            var newFrame = new BlendshapeFrame(frameFrom);
            Lerp(frameFrom.deltaVertices, frameTo.deltaVertices, newFrame.deltaVertices, lerpRate);
            Lerp(frameFrom.deltaNormals, frameTo.deltaNormals, newFrame.deltaNormals, lerpRate);
            Lerp(frameFrom.deltaTangents, frameTo.deltaTangents, newFrame.deltaTangents, lerpRate);
            return newFrame;
        }
    }

    public static class MeshUtils
    {
        public static Mesh CloneMesh(Mesh oldMesh)
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

        public static void CopyBlendShape(Mesh newMesh, Mesh oldMesh)
        {
            // 复制所有 blendshape
            for (int shapeIndex = 0; shapeIndex < oldMesh.blendShapeCount; shapeIndex++)
            {
                for (int frameIndex = 0; frameIndex < oldMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                {
                    string shapeName = oldMesh.GetBlendShapeName(shapeIndex);
                    float weight = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                    var oldMeshFrame = new BlendshapeFrame(oldMesh, shapeIndex, frameIndex);

                    oldMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                }
            }
        }

        public static void CopyBlendShapeWithFilter(Mesh newMesh, Mesh oldMesh, Predicate<string> accept)
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
                        var oldMeshFrame = new BlendshapeFrame(oldMesh, shapeIndex, frameIndex);

                        oldMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                    }
                }
            }
        }

        public static void CopyBlendShapeWithFilter(Mesh newMesh, Mesh oldMesh, Predicate<int> accept)
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
                        var oldMeshFrame = new BlendshapeFrame(oldMesh, shapeIndex, frameIndex);

                        oldMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                    }
                }
            }
        }

        public static void CopyBlendShapeSorted(Mesh newMesh, Mesh oldMesh, Func<Mesh, IEnumerable<(int, string)>> getSortedBlendshapeListWithName)
        {
            // 复制所有 blendshape
            foreach (var (shapeIndex, shapeName) in getSortedBlendshapeListWithName(oldMesh))
            {
                for (int frameIndex = 0; frameIndex < oldMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                {
                    float weight = oldMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
                    var copyedMeshFrame = new BlendshapeFrame(oldMesh, shapeIndex, frameIndex);

                    copyedMeshFrame.WriteBlendShapeFrameToMesh(newMesh, shapeName, weight);
                }
            }
        }
    }
}