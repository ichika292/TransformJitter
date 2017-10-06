using UnityEngine;
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
        void OnEnable()
        {
            if (skinnedMeshRenderer == null) return;

            PlayLoop();
        }

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
            _PlayLoop(1f);
        }

        /// <summary>
        /// ループ再生開始　振幅倍率設定あり
        /// </summary>
        public override void PlayLoop(float magnification)
        {
            _PlayLoop(Mathf.Max(0f, magnification));
        }

        /// <summary>
        /// ループ再生停止
        /// </summary>
        public override void StopLoop()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) return;
#endif
            ResetRoutineList(loopRoutineList);
            ResetAllLoopState();

            SetMorphWeight();
        }

        /// <summary>
        /// ループ再生フェードイン
        /// </summary>
        /// <param name="second">フェード時間</param>
        public override void FadeIn(float second)
        {
            if (fadeInRoutine != null) return;
            if (fadeOutRoutine != null)
            {
                StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = null;
            }
            if (!loopGroupEnabled)
            {
                loopGroupEnabled = true;
                _PlayLoop(0f);
            }

            fadeInRoutine = StartCoroutine(FadeInCoroutine(second));
        }

        /// <summary>
        /// ループ再生フェードアウト
        /// </summary>
        /// <param name="second">フェード時間</param>
        public override void FadeOut(float second)
        {
            if (fadeOutRoutine != null) return;

            if (fadeInRoutine != null)
            {
                StopCoroutine(fadeInRoutine);
                fadeInRoutine = null;
            }

            fadeOutRoutine = StartCoroutine(FadeOutCoroutine(second, StopLoop));
        }

        void _PlayLoop(float magnification)
        {
            if (helperList.Count == 0) return;

            StopLoop();

            //振幅倍率
            loopParameter.magnification = magnification;

            if (!loopGroupEnabled) return;

            if (sync)
            {
                //helperList[0]のstateを全モーフで共有
                var routine = StartCoroutine(LoopCoroutine(helperList[0].loopState));
                loopRoutineList.Add(routine);
            }
            else
            {
                foreach (BlendShapeJitterHelper h in helperList)
                {
                    var routine = StartCoroutine(LoopCoroutine(h.loopState));
                    loopRoutineList.Add(routine);
                }
            }
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

        void _PlayOnce(float magnification)
        {
            if (!onceGroupEnabled) return;

            //動作中で上書き不可ならば return
            if (OnceIsProcessing && !overrideOnce) return;

            StopOnce();

            //振幅倍率
            onceParameter.magnification = magnification;

            foreach (BlendShapeJitterHelper h in helperList)
            {
                //再生終了時にループ再生していない場合、初期化
                var routine = StartCoroutine(OnceCoroutine(h.onceState, StopOnce));
                onceRoutineList.Add(routine);
            }
        }

        /// <summary>
        /// 1周再生停止
        /// </summary>
        public void StopOnce()
        {
            ResetRoutineList(onceRoutineList);
            ResetAllOnceState();

            SetMorphWeight();
        }

        #endregion

        /// <summary>
        /// 全再生停止 & 初期化
        /// </summary>
        public override void Initialize()
        {
            ResetRoutineList(loopRoutineList);
            ResetAllLoopState();

            ResetRoutineList(onceRoutineList);
            ResetAllOnceState();

            SetMorphWeight();
        }

        #region Inspector拡張
#if UNITY_EDITOR
        [CanEditMultipleObjects]
        [CustomEditor(typeof(BlendShapeJitter))]
        public class BlendShapeJitterEditor : Editor
        {
            SerializedProperty syncProperty;
            SerializedProperty overrideOnceProperty;
            SerializedProperty helperListProperty;
            SerializedProperty loopParameterProperty;
            SerializedProperty onceParameterProperty;

            ReorderableList helperReorderableList;

            void OnEnable()
            {
                var self = target as BlendShapeJitter;

                syncProperty = serializedObject.FindProperty("sync");
                overrideOnceProperty = serializedObject.FindProperty("overrideOnce");
                helperListProperty = serializedObject.FindProperty("helperList");
                loopParameterProperty = serializedObject.FindProperty("loopParameter");
                onceParameterProperty = serializedObject.FindProperty("onceParameter");

                //ReorderableList設定
                helperReorderableList = new ReorderableList(serializedObject, helperListProperty);

                helperReorderableList.drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "Morph name & Magnification");
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

                //MecanimModelからWeight>0のモーフを取得
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                    if (GUILayout.Button("Set", GUILayout.Width(80))) self.SetMorph();
                    if (GUILayout.Button("Reset", GUILayout.Width(80))) self.ResetMorph();
                    EditorGUI.EndDisabledGroup();

                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying | !self.onceGroupEnabled);
                    if (GUILayout.Button("Play Once", GUILayout.Width(100))) self.PlayOnce();
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