using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    /// <summary>
    /// TransformのScaleのX,Y,Zそれぞれを任意の波形で振幅させます。
    /// PlayOnce()を実行することで、Onceの波形を任意のタイミングでLoopの波形に加算出来ます。
    /// </summary>
    public class ScaleJitter : ScaleJitImpl
    {
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
            _PlayLoop(PlayState.Play, 1f);
        }

        /// <summary>
        /// ループ再生開始　振幅倍率設定あり
        /// </summary>
        public override void PlayLoop(float magnification)
        {
            _PlayLoop(PlayState.Play, magnification);
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
            _PlayOnce(magnification);
        }
        #endregion
        
        #region Inspector拡張
#if UNITY_EDITOR
        [CanEditMultipleObjects]
        [CustomEditor(typeof(ScaleJitter))]
        public class ScaleJitterEditor : Editor
        {
            SerializedProperty updateModeProperty;
            SerializedProperty referenceScaleProperty;
            SerializedProperty playOnAwakeProperty;
            SerializedProperty syncAxisProperty;
            SerializedProperty overrideOnceProperty;
            SerializedProperty magnificationProperty;
            SerializedProperty loopParameterProperty;
            SerializedProperty onceParameterProperty;

            void OnEnable()
            {
                updateModeProperty = serializedObject.FindProperty("updateMode");
                referenceScaleProperty = serializedObject.FindProperty("referenceScale");
                playOnAwakeProperty = serializedObject.FindProperty("playOnAwake");
                syncAxisProperty = serializedObject.FindProperty("syncAxis");
                overrideOnceProperty = serializedObject.FindProperty("overrideOnce");
                magnificationProperty = serializedObject.FindProperty("magnification");
                loopParameterProperty = serializedObject.FindProperty("loopParameter");
                onceParameterProperty = serializedObject.FindProperty("onceParameter");
            }

            bool childrenFolding = true;

            public override void OnInspectorGUI()
            {
                var self = target as ScaleJitter;

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
                    List<ScaleJitter> list = self.children;
                    EditorGUI.BeginDisabledGroup(true);
                    if (childrenFolding = EditorGUILayout.Foldout(childrenFolding, "PositionJitter Children " + list.Count))
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            self.children[i] = (ScaleJitter)EditorGUILayout.ObjectField(self.children[i], typeof(ScaleJitter), true);
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
                    referenceScaleProperty.vector3Value = EditorGUILayout.Vector3Field(referenceScaleProperty.displayName, referenceScaleProperty.vector3Value);

                //PlayOnAwake
                if (!self.isChild)
                    playOnAwakeProperty.boolValue = EditorGUILayout.Toggle(playOnAwakeProperty.displayName, playOnAwakeProperty.boolValue);

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
                    magnificationProperty.floatValue = EditorGUILayout.FloatField(magnificationProperty.displayName, magnificationProperty.floatValue);
                
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
                        if (!self.isChild)
                        {
                            if (GUILayout.Button("Fadein", GUILayout.Width(60))) self.FadeIn(3f);
                            if (GUILayout.Button("Fadeout", GUILayout.Width(60))) self.FadeOut(3f);
                        }
                        if (GUILayout.Button("Play Once", GUILayout.Width(100))) self.PlayOnce();
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