using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace MYB.Jitter
{
    /// <summary>
    /// SkinnedMeshRendererのBlendShapeを任意の波形で振幅させます。
    /// SkinnedMeshRendererコンポーネントからBlendShapesの値を変更し、Setを押すことで振幅させるモーフを設定出来ます。
    /// PlayOnce()を実行することで、Onceの波形を任意のタイミングでLoopの波形に加算出来ます。
    /// </summary>
    public class BlendShapeJitter : BlendShapeJitterImpl
    {
        void OnDisable()
        {
            if (skinnedMeshRenderer == null) return;

            Initialize();
        }

        #region ********** LOOP ***********    
        /// <summary>
        /// ループ再生開始
        /// </summary>
        public override void PlayLoop()
        {
            _PlayLoop(PlayState.Play, 1f);
        }

        /// <summary>
        /// ループ再生開始　振幅倍率設定あり
        /// </summary>
        public override void PlayLoop(float magnification)
        {
            _PlayLoop(PlayState.Play, Mathf.Max(0f, magnification));
        }

        /// <summary>
        /// ループ再生フェードイン
        /// </summary>
        /// <param name="second">フェード時間</param>
        public override void FadeIn(float second)
        {
            if (playState == PlayState.Play ||
                playState == PlayState.Fadein) return;

            playState = PlayState.Fadein;
            fadeSpeed = 1f / Mathf.Max(0.01f, second);
            
            _PlayLoop(PlayState.Fadein, 0f);
        }

        /// <summary>
        /// ループ再生フェードアウト
        /// </summary>
        /// <param name="second">フェード時間</param>
        public override void FadeOut(float second)
        {
            if (playState == PlayState.Stop) return;

            playState = PlayState.Fadeout;
            fadeSpeed = 1f / Mathf.Max(0.01f, second);
        }

        #endregion

        #region ********** ONCE ***********
        /// <summary>
        /// 1周再生
        /// </summary>
        public override void PlayOnce()
        {
            _PlayOnce(1f);
        }

        /// <summary>
        /// 1周再生　振幅倍率設定あり
        /// </summary>
        public override void PlayOnce(float magnification)
        {
            _PlayOnce(Mathf.Max(0f, magnification));
        }

        #endregion


        #region Inspector拡張
#if UNITY_EDITOR
        [CanEditMultipleObjects]
        [CustomEditor(typeof(BlendShapeJitter))]
        public class BlendShapeJitterEditor : Editor
        {
            SerializedProperty playOnAwakeProperty;
            SerializedProperty syncProperty;
            SerializedProperty overrideOnceProperty;
            SerializedProperty helperListProperty;
            SerializedProperty damperListProperty;
            SerializedProperty loopParameterProperty;
            SerializedProperty onceParameterProperty;

            ReorderableList helperReorderableList;
            ReorderableList damperReorderableList;

            void OnEnable()
            {
                var self = target as BlendShapeJitter;

                playOnAwakeProperty = serializedObject.FindProperty("playOnAwake");
                syncProperty = serializedObject.FindProperty("sync");
                overrideOnceProperty = serializedObject.FindProperty("overrideOnce");
                helperListProperty = serializedObject.FindProperty("helperList");
                damperListProperty = serializedObject.FindProperty("damperList");
                loopParameterProperty = serializedObject.FindProperty("loopParameter");
                onceParameterProperty = serializedObject.FindProperty("onceParameter");

                //helperReorderableList設定
                helperReorderableList = new ReorderableList(serializedObject, helperListProperty);

                helperReorderableList.drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "Main Morph & Magnification");
                };

                helperReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = helperListProperty.GetArrayElementAtIndex(index);
                    rect.height -= 4;
                    rect.y += 2;
                    EditorGUI.PropertyField(rect, element);
                };

                //再生中はListの追加、削除禁止
                helperReorderableList.onAddCallback = (list) =>
                {
                    if (!EditorApplication.isPlaying)
                    {
                        self.helperList.Add(new BlendShapeJitterHelper(self, -1, "", 100f));
                        list.index = helperListProperty.arraySize;
                    }
                };

                helperReorderableList.onCanRemoveCallback = (list) =>
                {
                    return !EditorApplication.isPlaying;
                };

                //ReorderableList設定
                damperReorderableList = new ReorderableList(serializedObject, damperListProperty);

                damperReorderableList.drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "Damper Morph & Magnification");
                };

                damperReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = damperListProperty.GetArrayElementAtIndex(index);
                    rect.height -= 4;
                    rect.y += 2;
                    EditorGUI.PropertyField(rect, element);
                };

                damperReorderableList.onAddCallback = (list) =>
                {
                    self.damperList.Add(new BlendShapeJitterDamper(self, -1, "", 0.5f));
                    list.index = damperListProperty.arraySize;
                };
            }

            public override void OnInspectorGUI()
            {
                var self = target as BlendShapeJitter;

                serializedObject.Update();

                self.skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Target", self.skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

                if (self.skinnedMeshRenderer == null) return;
                if(self.skinnedMeshRenderer.sharedMesh.blendShapeCount == 0)
                {
                    EditorGUILayout.LabelField("Blend Shapes not found");
                    return;
                }

                playOnAwakeProperty.boolValue = EditorGUILayout.Toggle(playOnAwakeProperty.displayName, playOnAwakeProperty.boolValue);

                //sync
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                syncProperty.boolValue = EditorGUILayout.Toggle(syncProperty.displayName, syncProperty.boolValue);
                EditorGUI.EndDisabledGroup();
                overrideOnceProperty.boolValue = EditorGUILayout.Toggle(overrideOnceProperty.displayName, overrideOnceProperty.boolValue);

                //JitterParameter (Loop)
                EditorGUI.BeginChangeCheck();
                self.loopGroupEnabled = EditorGUILayout.ToggleLeft("--- LOOP ---", self.loopGroupEnabled, EditorStyles.boldLabel);
                if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying)
                {
                    if (self.loopGroupEnabled)
                        self.PlayLoop();
                    else
                        self.StopLoop();
                }

                if (self.loopGroupEnabled)
                {
                    self.loopParameter.periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.loopParameter.periodToAmplitude);
                    EditorGUILayout.PropertyField(loopParameterProperty);
                }

                //JitterParameter (Once)
                EditorGUI.BeginChangeCheck();
                self.onceGroupEnabled = EditorGUILayout.ToggleLeft("--- ONCE ---", self.onceGroupEnabled, EditorStyles.boldLabel);
                if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying)
                {
                    if (!self.onceGroupEnabled)
                        self.StopOnce();
                }

                if (self.onceGroupEnabled)
                {
                    self.onceParameter.periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.onceParameter.periodToAmplitude);
                    EditorGUILayout.PropertyField(onceParameterProperty);
                }

                //Helper List
                helperReorderableList.DoLayoutList();

                //Exclusive List
                damperReorderableList.DoLayoutList();

                //Button
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                    if (GUILayout.Button("Get Main", GUILayout.Width(80))) self.GetMainMorph();
                    if (GUILayout.Button("Get Damper", GUILayout.Width(80))) self.GetDampMorph();
                    EditorGUI.EndDisabledGroup();

                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
                    if (GUILayout.Button("Fadein", GUILayout.Width(60))) self.FadeIn(3f);
                    if (GUILayout.Button("Fadeout", GUILayout.Width(60))) self.FadeOut(3f);
                    if (GUILayout.Button("Play Once", GUILayout.Width(80))) self.PlayOnce();
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

                EditorGUILayout.BeginHorizontal();
                {
                    self.asset = (BlendShapeJitterAsset)EditorGUILayout.ObjectField(self.asset, typeof(BlendShapeJitterAsset), false);

                    EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                    if (GUILayout.Button("Import", GUILayout.Width(60))) self.Import();
                    if (GUILayout.Button("Export", GUILayout.Width(60))) self.Export();
                    EditorGUI.EndDisabledGroup();

                }
                EditorGUILayout.EndHorizontal();

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
        #endregion
    }
}