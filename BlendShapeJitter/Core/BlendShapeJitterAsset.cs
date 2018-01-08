using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace MYB.Jitter
{
    public class BlendShapeJitterAsset : ScriptableObject
    {
        public bool playOnAwake = true;
        public bool sync;
        public bool overrideOnce;
        public bool loopGroupEnabled = true;
        public bool onceGroupEnabled = true;

        public BlendShapeJitterParameter loopParameter = new BlendShapeJitterParameter(PrimitiveAnimationCurve.UpDown5, true);
        public BlendShapeJitterParameter onceParameter = new BlendShapeJitterParameter(PrimitiveAnimationCurve.UpDown1, false);

        public List<BlendShapeJitterHelper> helperList = new List<BlendShapeJitterHelper>();
        public List<BlendShapeJitterDamper> damperList = new List<BlendShapeJitterDamper>();
        
        [MenuItem("Assets/Create/Jitter/BlendShapeJitterAsset", false, 21000)]
        static BlendShapeJitterAsset CreateBlendShapeJitterAssetInstance()
        {
            var asset = CreateInstance<BlendShapeJitterAsset>();
            ProjectWindowUtil.CreateAsset(asset, "New BlendShapeJitterAsset.asset");
            AssetDatabase.Refresh();

            return asset;
        }
        
        [CanEditMultipleObjects]
        [CustomEditor(typeof(BlendShapeJitterAsset))]
        public class BlendShapeJitterAssetEditor : Editor
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
                var self = target as BlendShapeJitterAsset;

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
                        self.helperList.Add(new BlendShapeJitterHelper(-1, "", 100f));
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
                    self.damperList.Add(new BlendShapeJitterDamper(-1, "", 0.5f));
                    list.index = damperListProperty.arraySize;
                };
            }

            public override void OnInspectorGUI()
            {
                var self = target as BlendShapeJitterAsset;

                serializedObject.Update();

                playOnAwakeProperty.boolValue = EditorGUILayout.Toggle(playOnAwakeProperty.displayName, playOnAwakeProperty.boolValue);

                //sync
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                syncProperty.boolValue = EditorGUILayout.Toggle(syncProperty.displayName, syncProperty.boolValue);
                EditorGUI.EndDisabledGroup();
                overrideOnceProperty.boolValue = EditorGUILayout.Toggle(overrideOnceProperty.displayName, overrideOnceProperty.boolValue);

                //JitterParameter (Loop)
                self.loopGroupEnabled = EditorGUILayout.ToggleLeft("--- LOOP ---", self.loopGroupEnabled, EditorStyles.boldLabel);
                
                if (self.loopGroupEnabled)
                {
                    self.loopParameter.periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.loopParameter.periodToAmplitude);
                    EditorGUILayout.PropertyField(loopParameterProperty);
                }

                //JitterParameter (Once)
                self.onceGroupEnabled = EditorGUILayout.ToggleLeft("--- ONCE ---", self.onceGroupEnabled, EditorStyles.boldLabel);
                
                if (self.onceGroupEnabled)
                {
                    self.onceParameter.periodToAmplitude = EditorGUILayout.CurveField("Period to Amplitude", self.onceParameter.periodToAmplitude);
                    EditorGUILayout.PropertyField(onceParameterProperty);
                }

                //Helper List
                helperReorderableList.DoLayoutList();

                //Exclusive List
                damperReorderableList.DoLayoutList();

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}