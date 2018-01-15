using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MYB.Jitter
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RotationJitter))]
    public class RotationJitterEditor : TJitterEditor
    {
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

            TJitterCommon(self, "Rotation");

            serializedObject.ApplyModifiedProperties();
        }
    }
}