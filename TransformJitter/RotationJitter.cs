using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    /// <summary>
    /// TransformのRotationのX,Y,Zそれぞれを任意の波形で振幅させます。
    /// PlayOnce()を実行することで、Onceの波形を任意のタイミングでLoopの波形に加算出来ます。
    /// </summary>
    public class RotationJitter : RotationJitImpl
    {
        void OnEnable()
        {
            if (target == null) return;

            PlayLoop();
        }

        void OnDisable()
        {
            if (target == null) return;

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
            _PlayLoop(magnification);
        }

        /// <summary>
        /// ループ再生停止
        /// </summary>
        public override void StopLoop()
        {
            ResetRoutineList(loopRoutineList);
            ResetAllLoopState();

            if (!isProcessing) ResetRotation();
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
            if(isProcessing) StopLoop();

            //振幅倍率 x:Loop
            this.amplitudeMagnification.x = magnification;

            if (!loopGroupEnabled) return;

            foreach (JitterHelper h in helperList)
            {
                var routine = StartCoroutine(LoopCoroutine(h.loopState));
                loopRoutineList.Add(routine);
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
            _PlayOnce(magnification);
        }
        
        void _PlayOnce(float magnification)
        {
            if (!onceGroupEnabled) return;

            //動作中で上書き不可ならば return
            if (OnceIsProcessing && !overrideOnce) return;

            StopOnce();

            //振幅倍率 y:Once
            this.amplitudeMagnification.y = magnification;

            foreach (JitterHelper h in helperList)
            {
                //再生終了時にループ再生していない場合、初期化
                var routine = StartCoroutine(OnceCoroutine(h.onceState));
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

            if (!isProcessing) ResetRotation();
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

            ResetRotation();
        }

        #region Inspector拡張
#if UNITY_EDITOR
        [CanEditMultipleObjects]
        [CustomEditor(typeof(RotationJitter))]
        public class RotationJitterEditor : Editor
        {
            SerializedProperty updateModeProperty;
            SerializedProperty referenceRotationProperty;
            SerializedProperty syncAxisProperty;
            SerializedProperty overrideOnceProperty;
            SerializedProperty magnificationProperty;
            SerializedProperty maxDegreesDeltaProperty;
            SerializedProperty loopParameterProperty;
            SerializedProperty onceParameterProperty;

            void OnEnable()
            {
                updateModeProperty = serializedObject.FindProperty("updateMode");
                referenceRotationProperty = serializedObject.FindProperty("referenceRotation");
                syncAxisProperty = serializedObject.FindProperty("syncAxis");
                overrideOnceProperty = serializedObject.FindProperty("overrideOnce");
                magnificationProperty = serializedObject.FindProperty("magnification");
                maxDegreesDeltaProperty = serializedObject.FindProperty("maxDegreesDelta");
                loopParameterProperty = serializedObject.FindProperty("loopParameter");
                onceParameterProperty = serializedObject.FindProperty("onceParameter");
            }

            bool childrenFolding = true;

            public override void OnInspectorGUI()
            {
                var self = target as RotationJitter;

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();
                {
                    self.target = (Transform)EditorGUILayout.ObjectField("Target", self.target, typeof(Transform), true);
                    if (self.target == null) return;
                }
                if (EditorGUI.EndChangeCheck()) self.SearchParent();

                //children
                if (EditorApplication.isPlaying && !self.isChild)
                {
                    List<RotationJitter> list = self.children;
                    EditorGUI.BeginDisabledGroup(true);
                    if (childrenFolding = EditorGUILayout.Foldout(childrenFolding, "RotationJitter Children " + list.Count))
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            self.children[i] = (RotationJitter)EditorGUILayout.ObjectField(self.children[i], typeof(RotationJitter), true);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }

                //isChild
                EditorGUI.BeginDisabledGroup(true);
                {
                    if (self.isChild)
                        self.isChild = EditorGUILayout.Toggle("is Child", self.isChild);
                }
                EditorGUI.EndDisabledGroup();

                //UpdateMode
                var updateMode = (UpdateMode)updateModeProperty.enumValueIndex;
                updateModeProperty.enumValueIndex = (int)(UpdateMode)EditorGUILayout.EnumPopup(
                    updateModeProperty.displayName, (System.Enum)updateMode);

                //Reference
                if (!self.isChild && updateMode == UpdateMode.Reference)
                    referenceRotationProperty.vector3Value = EditorGUILayout.Vector3Field(referenceRotationProperty.displayName, referenceRotationProperty.vector3Value);

                //sync Axis
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        syncAxisProperty.boolValue = EditorGUILayout.Toggle(syncAxisProperty.displayName, syncAxisProperty.boolValue);
                    }
                    if (EditorGUI.EndChangeCheck()) self.OnValidate();
                }
                EditorGUI.EndDisabledGroup();
                overrideOnceProperty.boolValue = EditorGUILayout.Toggle(overrideOnceProperty.displayName, overrideOnceProperty.boolValue);

                if (!self.isChild)
                {
                    //angle Parameter
                    magnificationProperty.floatValue = EditorGUILayout.FloatField(magnificationProperty.displayName, magnificationProperty.floatValue);
                    var delta = EditorGUILayout.FloatField(maxDegreesDeltaProperty.displayName, maxDegreesDeltaProperty.floatValue);
                    maxDegreesDeltaProperty.floatValue = Mathf.Max(0, delta);
                }

                //JitterParameter (Loop)
                if (!self.isChild)
                {
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
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < 3; i++)
                        {
                            EditorGUI.BeginChangeCheck();
                            {
                                self.loopEnabled[i] = EditorGUILayout.ToggleLeft(self.axisLabel[i], self.loopEnabled[i], EditorStyles.boldLabel);
                                if (self.loopEnabled[i])
                                    self.loopParameter[i].periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.loopParameter[i].periodToAmplitude);
                                EditorGUILayout.PropertyField(loopParameterProperty.GetArrayElementAtIndex(i));
                            }
                            if (EditorGUI.EndChangeCheck()) self.OnValidate();
                        }
                        EditorGUI.indentLevel--;
                    }
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
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < 3; i++)
                    {
                        EditorGUI.BeginChangeCheck();
                        {
                            self.onceEnabled[i] = EditorGUILayout.ToggleLeft(self.axisLabel[i], self.onceEnabled[i], EditorStyles.boldLabel);
                            if (self.onceEnabled[i])
                                self.onceParameter[i].periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.onceParameter[i].periodToAmplitude);
                            EditorGUILayout.PropertyField(onceParameterProperty.GetArrayElementAtIndex(i));
                        }
                        if (EditorGUI.EndChangeCheck()) self.OnValidate();
                    }
                    EditorGUI.indentLevel--;

                    //Play Once
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();

                        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
                        {
                            if (GUILayout.Button("Play Once", GUILayout.Width(100))) self.PlayOnce();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
        #endregion
    }
}