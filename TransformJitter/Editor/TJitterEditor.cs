using UnityEngine;
using UnityEditor;

namespace MYB.Jitter
{
    public class TJitterEditor : Editor
    {
        SerializedProperty updateModeProperty;
        SerializedProperty referenceProperty;
        SerializedProperty playOnAwakeProperty;
        SerializedProperty syncPeriodProperty;
        SerializedProperty syncAmplitudeProperty;
        SerializedProperty syncEasingProperty;
        SerializedProperty overrideOnceProperty;
        SerializedProperty magnificationProperty;
        SerializedProperty loopParameterProperty;
        SerializedProperty onceParameterProperty;

        void OnEnable()
        {
            updateModeProperty = serializedObject.FindProperty("updateMode");
            referenceProperty = serializedObject.FindProperty("reference");
            playOnAwakeProperty = serializedObject.FindProperty("playOnAwake");
            syncPeriodProperty = serializedObject.FindProperty("syncPeriod");
            syncAmplitudeProperty = serializedObject.FindProperty("syncAmplitude");
            syncEasingProperty = serializedObject.FindProperty("syncEasing");
            overrideOnceProperty = serializedObject.FindProperty("overrideOnce");
            magnificationProperty = serializedObject.FindProperty("magnification");
            loopParameterProperty = serializedObject.FindProperty("loopParameter");
            onceParameterProperty = serializedObject.FindProperty("onceParameter");
        }

        protected void TJitterCommon(TJitter self, string displayName)
        {
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
                referenceProperty.vector3Value = EditorGUILayout.Vector3Field("Reference " + displayName, referenceProperty.vector3Value);

            //PlayOnAwake
            if (!self.isChild)
                playOnAwakeProperty.boolValue = EditorGUILayout.Toggle(playOnAwakeProperty.displayName, playOnAwakeProperty.boolValue);

            //sync
            EditorGUI.BeginChangeCheck();
            {
                syncPeriodProperty.boolValue = EditorGUILayout.Toggle(syncPeriodProperty.displayName, syncPeriodProperty.boolValue);

                EditorGUI.BeginDisabledGroup(!syncPeriodProperty.boolValue);
                {
                    if (!syncPeriodProperty.boolValue)
                        syncAmplitudeProperty.boolValue = false;
                    syncAmplitudeProperty.boolValue = EditorGUILayout.Toggle(syncAmplitudeProperty.displayName, syncAmplitudeProperty.boolValue);
                }
                EditorGUI.EndDisabledGroup();

                syncEasingProperty.boolValue = EditorGUILayout.Toggle(syncEasingProperty.displayName, syncEasingProperty.boolValue);
            }
            if (EditorGUI.EndChangeCheck()) self.OnValidate();

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
            }

            //Button
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
                if (!self.isChild)
                {
                    if (GUILayout.Button("Fadein")) self.FadeIn(3f);
                    if (GUILayout.Button("Fadeout")) self.FadeOut(3f);
                }
                if (GUILayout.Button("Move Next")) self.MoveNext();
                if (GUILayout.Button("Play Once")) self.PlayOnce();
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

            EditorGUILayout.BeginHorizontal();
            {
                self.asset = (TransformJitterAsset)EditorGUILayout.ObjectField(self.asset, typeof(TransformJitterAsset), false);

                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                if (GUILayout.Button("Import", GUILayout.Width(60))) self.Import();
                if (GUILayout.Button("Export", GUILayout.Width(60))) self.Export();
                EditorGUI.EndDisabledGroup();

            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
