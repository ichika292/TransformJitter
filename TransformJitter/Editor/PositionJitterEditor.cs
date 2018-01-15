using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MYB.Jitter
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PositionJitter))]
    public class PositionJitterEditor : TJitterEditor
    {
        bool childrenFolding = true;

        public override void OnInspectorGUI()
        {
            var self = target as PositionJitter;

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
                List<PositionJitter> list = self.children;
                EditorGUI.BeginDisabledGroup(true);
                if (childrenFolding = EditorGUILayout.Foldout(childrenFolding, "PositionJitter Children " + list.Count))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        self.children[i] = (PositionJitter)EditorGUILayout.ObjectField(self.children[i], typeof(PositionJitter), true);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }

            TJitterCommon(self, "Position");

            serializedObject.ApplyModifiedProperties();
        }
    }
}