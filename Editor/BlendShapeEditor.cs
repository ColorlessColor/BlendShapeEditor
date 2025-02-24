using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;

namespace usefulunitytools.editorscript.blendshape
{
    /// <summary>
    /// 自定义的 EditorGUILauout 工具箱，自动布局
    /// 作者: https://zhuanlan.zhihu.com/p/626207442
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
                    if (!info.Contains(this.filter))
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

        //基礎設定
        private GameObject character;                           //変形対象のキャラクタ
        private SkinnedMeshRenderer smr;                        //変形対象のSkinnedMeshRenderer

        //UI用
        private int tab;
        private string[] tabText = new string[3] { "排序 & 重命名", "创建", "对称分割" };
        private List<MorphData> morphDatas;
        private List<MorphData> selectedMorphDatas;
        private string[] morphNames;
        private ReorderableList sortMorphList;
        private ReorderableList blendMorphList;

        private int separateMorphID;
        private float separateSmoothRange = 0.001f;
        private Vector2 scrollPos = Vector2.zero;
        private string blendShapeName = "Morph";                            //追加するBlendShape名
        bool blendShapeNameFlag = false;

        private const string ShowWarningKey = "BlendShapeEditor.ShowWarning";                           // 控制是否显示警告弹窗

        //MorphData構造体
        private struct MorphData
        {
            public int id;
            public string name;
            public float weight;

            public MorphData(int i, string str)
            {
                id = i;
                name = str;
                weight = 100f;
            }

            public MorphData(int i, string str, float w)
            {
                id = i;
                name = str;
                weight = w;
            }
        }

        //メニューへの登録
        [MenuItem("Tools/BlendShape Editor")]
        private static void Create()
        {
            //ウインドウ作成
            GetWindow<BlendShapeEditor>("BlendShapeEditor");
        }

        //開始
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

        //GUI
        private void OnGUI()
        {

            EditorGUI.BeginChangeCheck();
            smr = EditorGUILayout.ObjectField("Skinned Mesh Renderer", smr, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
            if (EditorGUI.EndChangeCheck() && smr)
            {
                //リセット
                ResetMorphDatas();
                scrollPos = Vector2.zero;
                blendShapeNameFlag = CheckBlendShapeName(blendShapeName);
            }

            GUILayout.Space(10);

            if (smr)
            {
                if (smr.sharedMesh.blendShapeCount > 0)
                {

                    /*
					EditorGUILayout.HelpBox("请注意，挂载到物体上的部分 NDMF 系组件会导致插件无法正常工作，如有必要请事先禁用或移除", MessageType.Error);
					*/

                    //機能の選択
                    EditorGUI.BeginChangeCheck();
                    tab = GUILayout.Toolbar(tab, tabText);
                    if (EditorGUI.EndChangeCheck())
                    {       //tab切り替え時の初期化処理
                        ResetMorphDatas();
                        scrollPos = Vector2.zero;

                        switch (tab)
                        {
                            case 0:
                                blendShapeNameFlag = CheckBlendShapeName();
                                break;
                            case 1:
                                blendShapeNameFlag = CheckBlendShapeName(blendShapeName);

                                /* 删除 切换选项卡时的初始化
								//BlendShapeのプレビュー
								for(int i=0;i<smr.sharedMesh.blendShapeCount;i++){
									smr.SetBlendShapeWeight(i, 0f);
								}
								*/

                                for (int i = 0; i < selectedMorphDatas.Count; i++)
                                {
                                    smr.SetBlendShapeWeight(selectedMorphDatas[i].id, selectedMorphDatas[i].weight);
                                }
                                break;
                        }
                    }

                    //各機能の描画
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, true);
                    switch (tab)
                    {
                        case 0:     //Sort
                            DoSortTab();
                            break;
                        case 1:     //Blend
                            DoBlendTab();
                            break;
                        case 2:     //Separate
                            DoSeparateTab();
                            break;
                        default:

                            break;
                    }
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    //BlendShapeがない場合
                    EditorGUILayout.HelpBox("此网格没有 BlendShape", MessageType.Error);
                }
            }
        }

        //MorphDatasの初期化
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

        //Sort機能
        private void DoSortTab()
        {
            EditorGUILayout.HelpBox("你可以在此对 Blendshape 进行排序、重命名以及删除操作", MessageType.Info);
            if (sortMorphList == null)
            {
                //ReorderableListの準備
                sortMorphList = new ReorderableList(morphDatas, typeof(MorphData));
                sortMorphList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "BlendShape");
                sortMorphList.drawElementCallback = (rect, i, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(rect, "Morph " + i);
                    rect.x += 75;
                    rect.width = rect.width - 60;
                    morphDatas[i] = new MorphData(morphDatas[i].id, EditorGUI.TextField(rect, morphDatas[i].name));
                };
                sortMorphList.onCanAddCallback = list =>
                {
                    return false;
                };
            }
            EditorGUI.BeginChangeCheck();
            sortMorphList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                //Morph名の重複チェック
                blendShapeNameFlag = CheckBlendShapeName();
            }

            GUILayout.Space(10);
            if (!blendShapeNameFlag)
            {
                EditorGUILayout.HelpBox("已存在相同的 BlendShape 名称", MessageType.Error);
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

        //Blend機能
        private void DoBlendTab()
        {
            EditorGUILayout.HelpBox("修改此处的 Blendshape 值将会同步应用至 Inspector 的 Blendshapes 的值", MessageType.Info);
            if (blendMorphList == null)
            {
                //ReorderableListの準備
                blendMorphList = new ReorderableList(selectedMorphDatas, typeof(MorphData));
                blendMorphList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "BlendShape");
                blendMorphList.drawElementCallback = (rect, i, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(rect, "Morph " + i);
                    rect.x += 75;
                    rect.width = (rect.width - 70) / 2f;
                    int id = EditorGUIKit.Popup(rect, selectedMorphDatas[i].id, morphNames);
                    rect.x += rect.width + 10;
                    selectedMorphDatas[i] = new MorphData(morphDatas[id].id, morphDatas[id].name, EditorGUI.Slider(rect, selectedMorphDatas[i].weight, 0f, 100f));
                };
                blendMorphList.onAddCallback = list =>
                {
                    selectedMorphDatas.Add(new MorphData(morphDatas[0].id, morphDatas[0].name));
                };
                blendMorphList.onCanRemoveCallback = list =>
                {
                    return list.count > 1;
                };
            }
            EditorGUI.BeginChangeCheck();
            blendMorphList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                //BlendShapeのプレビュー

                /* 删除 Blend 操作预览
				for(int i=0;i<smr.sharedMesh.blendShapeCount;i++){
					smr.SetBlendShapeWeight(i, 0f);
				}
				*/

                for (int i = 0; i < selectedMorphDatas.Count; i++)
                {
                    smr.SetBlendShapeWeight(selectedMorphDatas[i].id, selectedMorphDatas[i].weight);
                }
            }

            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            blendShapeName = EditorGUILayout.TextField("新 BlendShape 名称", blendShapeName);
            if (EditorGUI.EndChangeCheck())
            {
                //Morph名の重複チェック
                blendShapeNameFlag = CheckBlendShapeName(blendShapeName);
            }

            GUILayout.Space(10);

            if (!blendShapeNameFlag)
            {
                EditorGUILayout.HelpBox("已存在相同的 BlendShape 名称", MessageType.Error);
            }
            else if (blendShapeName == "")
            {
                EditorGUILayout.HelpBox("请键入 BlendShape 名称", MessageType.Error);
            }
            else
            {
                //合成
                if (GUILayout.Button("创建 BlendShape"))
                {
                    SaveMesh(BlendBlendShapeMesh(smr.sharedMesh));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                //反転
                if (GUILayout.Button("反转 BlendShape"))
                {
                    SaveMesh(InverseBlendShapeMesh(smr.sharedMesh));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                //連結
                if (GUILayout.Button("制作 BlendShape 动画"))
                {
                    SaveMesh(ConnectBlendShapeMesh(smr.sharedMesh));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                //累積連結
                if (GUILayout.Button("创建并制作 BlendShape 动画"))
                {
                    SaveMesh(BlendConnectBlendShapeMesh(smr.sharedMesh));
                    blendShapeNameFlag = false;
                }
                GUILayout.Space(10);

                //基本形状に適用
                if (GUILayout.Button("应用 Blendshape 形变至基础网格"))
                {
                    SaveMesh(ApplyBaseShapeMesh(smr.sharedMesh));
                    blendShapeNameFlag = false;
                }
            }
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

        //Morph名の重複チェック
        private bool CheckBlendShapeName()
        {
            List<string> checkedNames = new List<string>();
            for (int i = 0; i < morphDatas.Count; i++)
            {
                if (!checkedNames.Contains(morphDatas[i].name))
                {
                    checkedNames.Add(morphDatas[i].name);
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
            AssetDatabase.CreateAsset(smr.sharedMesh, "Assets/BlendShapeEditor/" + smr.transform.name + "_" + DateTime.Now.ToString("yyyy_M_d_HH_mm_ss") + ".asset");
            AssetDatabase.SaveAssets();

            //MorphDatasの初期化
            ResetMorphDatas();
        }


        //Meshのコピー
        private Mesh CopyMesh(Mesh m)
        {
            Mesh mesh = new Mesh();

            mesh.indexFormat = m.indexFormat;
            mesh.vertices = m.vertices;
            mesh.uv = m.uv;
            mesh.uv2 = m.uv2;
            mesh.uv3 = m.uv3;
            mesh.uv4 = m.uv4;
            mesh.uv5 = m.uv5;
            mesh.uv6 = m.uv6;
            mesh.uv7 = m.uv7;
            mesh.uv8 = m.uv8;

            mesh.bindposes = m.bindposes;
            mesh.boneWeights = m.boneWeights;
            mesh.bounds = m.bounds;
            mesh.colors = m.colors;
            mesh.colors32 = m.colors32;
            mesh.normals = m.normals;
            mesh.subMeshCount = m.subMeshCount;
            mesh.tangents = m.tangents;

            //SubMeshのコピー(マテリアルごとに分けられたメッシュ)
            for (int i = 0; i < m.subMeshCount; i++)
            {
                mesh.SetTriangles(m.GetTriangles(i), i, false, (int)m.GetBaseVertex(i));
                mesh.SetSubMesh(i, m.GetSubMesh(i), MeshUpdateFlags.DontRecalculateBounds);
            }

            //BlendShapeのコピー
            for (int i = 0; i < m.blendShapeCount; i++)
            {
                for (int j = 0; j < m.GetBlendShapeFrameCount(i); j++)
                {
                    Vector3[] deltaVertices = new Vector3[m.vertexCount];
                    Vector3[] deltaNormals = new Vector3[m.vertexCount];
                    Vector3[] deltaTangents = new Vector3[m.vertexCount];
                    m.GetBlendShapeFrameVertices(i, j, deltaVertices, deltaNormals, deltaTangents);
                    mesh.AddBlendShapeFrame(m.GetBlendShapeName(i), m.GetBlendShapeFrameWeight(i, j), deltaVertices, deltaNormals, deltaTangents);
                }
            }

            return mesh;
        }

        //MorphDatasに従って整理したMeshのコピーを作成
        private Mesh SortBlendShapeMesh(Mesh m)
        {
            Mesh mesh = new Mesh();

            mesh.indexFormat = m.indexFormat;
            mesh.vertices = m.vertices;
            mesh.uv = m.uv;
            mesh.uv2 = m.uv2;
            mesh.uv3 = m.uv3;
            mesh.uv4 = m.uv4;
            mesh.uv5 = m.uv5;
            mesh.uv6 = m.uv6;
            mesh.uv7 = m.uv7;
            mesh.uv8 = m.uv8;

            mesh.bindposes = m.bindposes;
            mesh.boneWeights = m.boneWeights;
            mesh.bounds = m.bounds;
            mesh.colors = m.colors;
            mesh.colors32 = m.colors32;
            mesh.normals = m.normals;
            mesh.subMeshCount = m.subMeshCount;
            mesh.tangents = m.tangents;

            //SubMeshのコピー(マテリアルごとに分けられたメッシュ)
            for (int i = 0; i < m.subMeshCount; i++)
            {
                mesh.SetTriangles(m.GetTriangles(i), i, false, (int)m.GetBaseVertex(i));
                mesh.SetSubMesh(i, m.GetSubMesh(i), MeshUpdateFlags.DontRecalculateBounds);
            }

            //BlendShapeのコピー
            for (int i = 0; i < morphDatas.Count; i++)
            {
                for (int j = 0; j < m.GetBlendShapeFrameCount(morphDatas[i].id); j++)
                {
                    Vector3[] deltaVertices = new Vector3[m.vertexCount];
                    Vector3[] deltaNormals = new Vector3[m.vertexCount];
                    Vector3[] deltaTangents = new Vector3[m.vertexCount];
                    m.GetBlendShapeFrameVertices(morphDatas[i].id, j, deltaVertices, deltaNormals, deltaTangents);
                    mesh.AddBlendShapeFrame(morphDatas[i].name, m.GetBlendShapeFrameWeight(morphDatas[i].id, j), deltaVertices, deltaNormals, deltaTangents);
                }
            }

            return mesh;
        }

        //指定したBlensShapeの指定したWeightのときのdeltaVertices、deltaNormals、deltaTangentsを取得
        private void GetBlendShapeWeightVertices(Mesh m, int shapeIndex, float weight, ref Vector3[] deltaVertices, ref Vector3[] deltaNormals, ref Vector3[] deltaTangents)
        {
            //Weightが指定したWeight以上になる最小のフレームを探す
            int frameIndex = 0;
            for (int i = 0; i < m.blendShapeCount; i++)
            {
                if (m.GetBlendShapeFrameWeight(shapeIndex, i) >= weight)
                {
                    frameIndex = i;
                    break;
                }
            }

            //フレームの適用割合を算出
            float applyRate = 1;
            if (frameIndex == 0)
            {
                applyRate = weight / 100f;
            }
            else
            {
                applyRate = (weight - m.GetBlendShapeFrameWeight(shapeIndex, frameIndex - 1)) / (m.GetBlendShapeFrameWeight(shapeIndex, frameIndex) - m.GetBlendShapeFrameWeight(shapeIndex, frameIndex - 1));
            }

            m.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);

            //差分ベクトルに適応割合を乗算
            for (int i = 0; i < m.vertexCount; i++)
            {
                deltaVertices[i] *= applyRate;
                deltaNormals[i] *= applyRate;
                deltaTangents[i] *= applyRate;
            }
        }

        //BlendShapeを合成したMeshを生成
        private Mesh BlendBlendShapeMesh(Mesh m)
        {
            Mesh mesh = CopyMesh(m);

            //BlendShapeの合成
            Vector3[] deltaVertices = new Vector3[m.vertexCount];
            Vector3[] deltaNormals = new Vector3[m.vertexCount];
            Vector3[] deltaTangents = new Vector3[m.vertexCount];
            Vector3[] deltaVertices2 = new Vector3[m.vertexCount];
            Vector3[] deltaNormals2 = new Vector3[m.vertexCount];
            Vector3[] deltaTangents2 = new Vector3[m.vertexCount];

            GetBlendShapeWeightVertices(mesh, selectedMorphDatas[0].id, selectedMorphDatas[0].weight, ref deltaVertices, ref deltaNormals, ref deltaTangents);

            for (int i = 1; i < selectedMorphDatas.Count; i++)
            {
                GetBlendShapeWeightVertices(mesh, selectedMorphDatas[i].id, selectedMorphDatas[i].weight, ref deltaVertices2, ref deltaNormals2, ref deltaTangents2);

                for (int j = 0; j < m.vertexCount; j++)
                {
                    deltaVertices[j] += deltaVertices2[j];
                    deltaNormals[j] += deltaNormals2[j];
                    deltaTangents[j] += deltaTangents2[j];
                }
            }

            //BlendShapeの追加
            mesh.AddBlendShapeFrame(blendShapeName, 100, deltaVertices, deltaNormals, deltaTangents);

            return mesh;
        }

        //BlendShapeを反転したMeshを生成
        private Mesh InverseBlendShapeMesh(Mesh m)
        {
            Mesh mesh = CopyMesh(m);

            //BlendShapeの合成
            Vector3[] deltaVertices = new Vector3[m.vertexCount];
            Vector3[] deltaNormals = new Vector3[m.vertexCount];
            Vector3[] deltaTangents = new Vector3[m.vertexCount];
            Vector3[] deltaVertices2 = new Vector3[m.vertexCount];
            Vector3[] deltaNormals2 = new Vector3[m.vertexCount];
            Vector3[] deltaTangents2 = new Vector3[m.vertexCount];

            GetBlendShapeWeightVertices(mesh, selectedMorphDatas[0].id, selectedMorphDatas[0].weight, ref deltaVertices, ref deltaNormals, ref deltaTangents);

            for (int i = 1; i < selectedMorphDatas.Count; i++)
            {
                GetBlendShapeWeightVertices(mesh, selectedMorphDatas[i].id, selectedMorphDatas[i].weight, ref deltaVertices2, ref deltaNormals2, ref deltaTangents2);

                for (int j = 0; j < m.vertexCount; j++)
                {
                    deltaVertices[j] += deltaVertices2[j];
                    deltaNormals[j] += deltaNormals2[j];
                    deltaTangents[j] += deltaTangents2[j];
                }
            }

            //反転
            for (int j = 0; j < m.vertexCount; j++)
            {
                deltaVertices[j] *= -1f;
                deltaNormals[j] *= -1f;
                deltaTangents[j] *= -1f;
            }

            //BlendShapeの追加
            mesh.AddBlendShapeFrame(blendShapeName, 100, deltaVertices, deltaNormals, deltaTangents);

            return mesh;
        }

        //BlendShapeを連結したMeshを生成
        private Mesh ConnectBlendShapeMesh(Mesh m)
        {
            Mesh mesh = CopyMesh(m);

            //frameWeightの計算
            float frameWeight = 100f / (float)(selectedMorphDatas.Count);

            //BlendShapeの追加
            Vector3[] deltaVertices = new Vector3[m.vertexCount];
            Vector3[] deltaNormals = new Vector3[m.vertexCount];
            Vector3[] deltaTangents = new Vector3[m.vertexCount];

            for (int i = 0; i < selectedMorphDatas.Count; i++)
            {
                GetBlendShapeWeightVertices(mesh, selectedMorphDatas[i].id, selectedMorphDatas[i].weight, ref deltaVertices, ref deltaNormals, ref deltaTangents);
                mesh.AddBlendShapeFrame(blendShapeName, frameWeight * (i + 1), deltaVertices, deltaNormals, deltaTangents);
            }

            return mesh;
        }

        //BlendShapeを累積連結したMeshを生成
        private Mesh BlendConnectBlendShapeMesh(Mesh m)
        {
            Mesh mesh = CopyMesh(m);

            //frameWeightの計算
            float frameWeight = 100f / (float)(selectedMorphDatas.Count);

            //BlendShapeの追加
            Vector3[] deltaVertices = new Vector3[m.vertexCount];
            Vector3[] deltaNormals = new Vector3[m.vertexCount];
            Vector3[] deltaTangents = new Vector3[m.vertexCount];
            Vector3[] deltaVertices2 = new Vector3[m.vertexCount];
            Vector3[] deltaNormals2 = new Vector3[m.vertexCount];
            Vector3[] deltaTangents2 = new Vector3[m.vertexCount];

            GetBlendShapeWeightVertices(mesh, selectedMorphDatas[0].id, selectedMorphDatas[0].weight, ref deltaVertices, ref deltaNormals, ref deltaTangents);
            mesh.AddBlendShapeFrame(blendShapeName, frameWeight, deltaVertices, deltaNormals, deltaTangents);

            for (int i = 1; i < selectedMorphDatas.Count; i++)
            {
                GetBlendShapeWeightVertices(mesh, selectedMorphDatas[i].id, selectedMorphDatas[i].weight, ref deltaVertices2, ref deltaNormals2, ref deltaTangents2);

                for (int j = 0; j < m.vertexCount; j++)
                {
                    deltaVertices[j] += deltaVertices2[j];
                    deltaNormals[j] += deltaNormals2[j];
                    deltaTangents[i] += deltaTangents2[j];
                }

                mesh.AddBlendShapeFrame(blendShapeName, frameWeight * (i + 1), deltaVertices, deltaNormals, deltaTangents);
            }

            return mesh;
        }

        //BlendShapeを適用したMeshを生成
        private Mesh ApplyBaseShapeMesh(Mesh m)
        {
            Mesh mesh = new Mesh();

            mesh.indexFormat = m.indexFormat;
            mesh.vertices = m.vertices;
            mesh.uv = m.uv;
            mesh.uv2 = m.uv2;
            mesh.uv3 = m.uv3;
            mesh.uv4 = m.uv4;
            mesh.uv5 = m.uv5;
            mesh.uv6 = m.uv6;
            mesh.uv7 = m.uv7;
            mesh.uv8 = m.uv8;

            mesh.bindposes = m.bindposes;
            mesh.boneWeights = m.boneWeights;
            mesh.bounds = m.bounds;
            mesh.colors = m.colors;
            mesh.colors32 = m.colors32;
            mesh.normals = m.normals;
            mesh.subMeshCount = m.subMeshCount;
            mesh.tangents = m.tangents;

            //SubMeshのコピー(マテリアルごとに分けられたメッシュ)
            for (int i = 0; i < m.subMeshCount; i++)
            {
                mesh.SetTriangles(m.GetTriangles(i), i, false, (int)m.GetBaseVertex(i));
                mesh.SetSubMesh(i, m.GetSubMesh(i), MeshUpdateFlags.DontRecalculateBounds);
            }

            //適用するBlensShapeの差分ベクトルを算出
            Vector3[] deltaVertices = new Vector3[m.vertexCount];
            Vector3[] deltaNormals = new Vector3[m.vertexCount];
            Vector3[] deltaTangents = new Vector3[m.vertexCount];
            Vector3[] deltaVertices2 = new Vector3[m.vertexCount];
            Vector3[] deltaNormals2 = new Vector3[m.vertexCount];
            Vector3[] deltaTangents2 = new Vector3[m.vertexCount];

            GetBlendShapeWeightVertices(m, selectedMorphDatas[0].id, selectedMorphDatas[0].weight, ref deltaVertices, ref deltaNormals, ref deltaTangents);

            for (int i = 1; i < selectedMorphDatas.Count; i++)
            {
                GetBlendShapeWeightVertices(m, selectedMorphDatas[i].id, selectedMorphDatas[i].weight, ref deltaVertices2, ref deltaNormals2, ref deltaTangents2);

                for (int j = 0; j < m.vertexCount; j++)
                {
                    deltaVertices[j] += deltaVertices2[j];
                    deltaNormals[j] += deltaNormals2[j];
                    deltaTangents[j] += deltaTangents2[j];
                }
            }

            //Meshに適用
            Vector3[] meshVertices = new Vector3[m.vertexCount];
            Vector3[] meshNormals = new Vector3[m.vertexCount];
            Vector4[] meshTangents = new Vector4[m.vertexCount];
            for (int i = 0; i < m.vertexCount; i++)
            {
                meshVertices[i] = mesh.vertices[i] + deltaVertices[i];
                meshNormals[i] = mesh.normals[i] + deltaNormals[i];
                meshTangents[i] = mesh.tangents[i] + new Vector4(deltaTangents[i].x, deltaTangents[i].y, deltaTangents[i].z, 0);
            }
            mesh.vertices = meshVertices;
            mesh.normals = meshNormals;
            meshTangents = mesh.tangents;

            //差分ベクトルを反転
            for (int i = 0; i < m.vertexCount; i++)
            {
                deltaVertices[i] *= -1;
                deltaNormals[i] *= -1;
                deltaTangents[i] *= -1;
            }

            //BlendShapeのコピー
            for (int i = 0; i < m.blendShapeCount; i++)
            {
                for (int j = 0; j < m.GetBlendShapeFrameCount(i); j++)
                {
                    deltaVertices2 = new Vector3[m.vertexCount];
                    deltaNormals2 = new Vector3[m.vertexCount];
                    deltaTangents2 = new Vector3[m.vertexCount];
                    m.GetBlendShapeFrameVertices(i, j, deltaVertices2, deltaNormals2, deltaTangents2);
                    mesh.AddBlendShapeFrame(m.GetBlendShapeName(i), m.GetBlendShapeFrameWeight(i, j), deltaVertices2, deltaNormals2, deltaTangents2);
                }
            }

            //元形状に戻すBlendShapeを追加
            mesh.AddBlendShapeFrame(blendShapeName, 100, deltaVertices, deltaNormals, deltaTangents);

            return mesh;
        }

        //指定したBlendShapeの左右分割
        private Mesh SeparateBlendShapeMesh(Mesh m)
        {
            Mesh mesh = new Mesh();

            mesh.indexFormat = m.indexFormat;
            mesh.vertices = m.vertices;
            mesh.uv = m.uv;
            mesh.uv2 = m.uv2;
            mesh.uv3 = m.uv3;
            mesh.uv4 = m.uv4;
            mesh.uv5 = m.uv5;
            mesh.uv6 = m.uv6;
            mesh.uv7 = m.uv7;
            mesh.uv8 = m.uv8;

            mesh.bindposes = m.bindposes;
            mesh.boneWeights = m.boneWeights;
            mesh.bounds = m.bounds;
            mesh.colors = m.colors;
            mesh.colors32 = m.colors32;
            mesh.normals = m.normals;
            mesh.subMeshCount = m.subMeshCount;
            mesh.tangents = m.tangents;

            //SubMeshのコピー(マテリアルごとに分けられたメッシュ)
            for (int i = 0; i < m.subMeshCount; i++)
            {
                mesh.SetTriangles(m.GetTriangles(i), i, false, (int)m.GetBaseVertex(i));
                mesh.SetSubMesh(i, m.GetSubMesh(i), MeshUpdateFlags.DontRecalculateBounds);
            }

            //BlendShapeのコピー(今回生成する予定のBlendShapeと同名のメッシュはコピーしない)
            for (int i = 0; i < m.blendShapeCount; i++)
            {
                if (m.GetBlendShapeName(i) == m.GetBlendShapeName(separateMorphID) + "_L") continue;
                if (m.GetBlendShapeName(i) == m.GetBlendShapeName(separateMorphID) + "_R") continue;
                for (int j = 0; j < m.GetBlendShapeFrameCount(i); j++)
                {
                    Vector3[] deltaVertices = new Vector3[m.vertexCount];
                    Vector3[] deltaNormals = new Vector3[m.vertexCount];
                    Vector3[] deltaTangents = new Vector3[m.vertexCount];
                    m.GetBlendShapeFrameVertices(i, j, deltaVertices, deltaNormals, deltaTangents);
                    mesh.AddBlendShapeFrame(m.GetBlendShapeName(i), m.GetBlendShapeFrameWeight(i, j), deltaVertices, deltaNormals, deltaTangents);
                }
            }

            //左側
            //BlendShapeの取得準備
            Vector3[] deltaVertices2 = new Vector3[m.vertexCount];
            Vector3[] deltaNormals2 = new Vector3[m.vertexCount];
            Vector3[] deltaTangents2 = new Vector3[m.vertexCount];

            GetBlendShapeWeightVertices(mesh, separateMorphID, 100f, ref deltaVertices2, ref deltaNormals2, ref deltaTangents2);

            float weight = 0f;
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                weight = Mathf.InverseLerp(separateSmoothRange, -separateSmoothRange, mesh.vertices[i].x);
                deltaVertices2[i] *= weight;
                deltaNormals2[i] *= weight;
                deltaTangents2[i] *= weight;
            }

            //BlendShapeの追加
            mesh.AddBlendShapeFrame(mesh.GetBlendShapeName(separateMorphID) + "_L", 100, deltaVertices2, deltaNormals2, deltaTangents2);

            //右側
            //BlendShapeの取得準備
            GetBlendShapeWeightVertices(mesh, separateMorphID, 100f, ref deltaVertices2, ref deltaNormals2, ref deltaTangents2);

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                weight = Mathf.InverseLerp(-separateSmoothRange, separateSmoothRange, mesh.vertices[i].x);
                deltaVertices2[i] *= weight;
                deltaNormals2[i] *= weight;
                deltaTangents2[i] *= weight;
            }

            //BlendShapeの追加
            mesh.AddBlendShapeFrame(mesh.GetBlendShapeName(separateMorphID) + "_R", 100, deltaVertices2, deltaNormals2, deltaTangents2);

            return mesh;
        }
    }
}