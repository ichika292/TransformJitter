using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MYB.Jitter
{
    public class TransformJitterAsset : ScriptableObject
    {
        public UpdateMode updateMode = UpdateMode.Reference;
        public Vector3 reference;
        public bool playOnAwake = true;
        public bool syncAxis = false;
        public bool overrideOnce;
        public float magnification;
        public List<JitterHelper> helperList = new List<JitterHelper>();

        public JitterParameter[] loopParameter =
        {
            new JitterParameter(PrimitiveAnimationCurve.Cos, true, true),
            new JitterParameter(PrimitiveAnimationCurve.Sin, true),
            new JitterParameter(PrimitiveAnimationCurve.Sin, true)
        };

        public JitterParameter[] onceParameter =
        {
            new JitterParameter(PrimitiveAnimationCurve.UpDown25, false, true),
            new JitterParameter(PrimitiveAnimationCurve.UpDown25, false),
            new JitterParameter(PrimitiveAnimationCurve.UpDown25, false)
        };

        //Editor用
        public string[] axisLabel = { "--- X ---", "--- Y ---", "--- Z ---" };
        public bool loopGroupEnabled = true;
        public bool onceGroupEnabled = true;
        public bool[] loopEnabled = { true, true, true };
        public bool[] onceEnabled = { true, true, true };

        [MenuItem("Assets/Create/Jitter/TransformJitterAsset", false, 21001)]
        static void CreateTransformJitterAssetInstance()
        {
            var asset = CreateInstance<TransformJitterAsset>();
            ProjectWindowUtil.CreateAsset(asset, "New TransformJitterAsset.asset");
            AssetDatabase.Refresh();
        }

        [CanEditMultipleObjects]
        [CustomEditor(typeof(TransformJitterAsset))]
        public class TransformJitterAssetEditor : Editor
        {
            SerializedProperty updateModeProperty;
            SerializedProperty referenceProperty;
            SerializedProperty playOnAwakeProperty;
            SerializedProperty syncAxisProperty;
            SerializedProperty overrideOnceProperty;
            SerializedProperty magnificationProperty;
            SerializedProperty loopParameterProperty;
            SerializedProperty onceParameterProperty;

            void OnEnable()
            {
                updateModeProperty = serializedObject.FindProperty("updateMode");
                referenceProperty = serializedObject.FindProperty("reference");
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
                var self = target as TransformJitterAsset;

                serializedObject.Update();
                
                //UpdateMode
                var updateMode = (UpdateMode)updateModeProperty.enumValueIndex;
                updateModeProperty.enumValueIndex = (int)(UpdateMode)EditorGUILayout.EnumPopup(
                    updateModeProperty.displayName, (System.Enum)updateMode);

                //Reference
                referenceProperty.vector3Value = EditorGUILayout.Vector3Field("Reference Rotation", referenceProperty.vector3Value);

                //PlayOnAwake
                playOnAwakeProperty.boolValue = EditorGUILayout.Toggle(playOnAwakeProperty.displayName, playOnAwakeProperty.boolValue);

                //sync Axis
                syncAxisProperty.boolValue = EditorGUILayout.Toggle(syncAxisProperty.displayName, syncAxisProperty.boolValue);
                
                overrideOnceProperty.boolValue = EditorGUILayout.Toggle(overrideOnceProperty.displayName, overrideOnceProperty.boolValue);
                
                magnificationProperty.floatValue = EditorGUILayout.FloatField(magnificationProperty.displayName, magnificationProperty.floatValue);
                
                //JitterParameter (Loop)
                self.loopGroupEnabled = EditorGUILayout.ToggleLeft("--- LOOP ---", self.loopGroupEnabled, EditorStyles.boldLabel);
                    
                if (self.loopGroupEnabled)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < 3; i++)
                    {
                        self.loopEnabled[i] = EditorGUILayout.ToggleLeft(self.axisLabel[i], self.loopEnabled[i], EditorStyles.boldLabel);
                        if (self.loopEnabled[i])
                            self.loopParameter[i].periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.loopParameter[i].periodToAmplitude);
                        EditorGUILayout.PropertyField(loopParameterProperty.GetArrayElementAtIndex(i));
                    }
                    EditorGUI.indentLevel--;
                }

                //JitterParameter (Once)
                self.onceGroupEnabled = EditorGUILayout.ToggleLeft("--- ONCE ---", self.onceGroupEnabled, EditorStyles.boldLabel);
                
                if (self.onceGroupEnabled)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < 3; i++)
                    {
                        self.onceEnabled[i] = EditorGUILayout.ToggleLeft(self.axisLabel[i], self.onceEnabled[i], EditorStyles.boldLabel);
                        if (self.onceEnabled[i])
                            self.onceParameter[i].periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.onceParameter[i].periodToAmplitude);
                        EditorGUILayout.PropertyField(onceParameterProperty.GetArrayElementAtIndex(i));
                    }
                    EditorGUI.indentLevel--;
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}