using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MYB.Jitter
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScaleJitter))]
    public class ScaleJitterEditor : TJitterEditor
    {
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
                if (childrenFolding = EditorGUILayout.Foldout(childrenFolding, "ScaleJitter Children " + list.Count))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        self.children[i] = (ScaleJitter)EditorGUILayout.ObjectField(self.children[i], typeof(ScaleJitter), true);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }

            TJitterCommon(self, "Scale");

            serializedObject.ApplyModifiedProperties();
        }
    }
}