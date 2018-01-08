using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    /// <summary>
    /// LoopとOnceのStateを管理する。
    /// ReorderableListで設定されたMorphと同数がインスタンス化され、同数のコルーチンが走る。
    /// </summary>
    [System.Serializable]
    public class BlendShapeJitterDamper
    {
        public BlendShapeJitterImpl manager;
        public string name;
        public float weightMagnification = 1f;
        public float weight;
        public int index;

        public BlendShapeJitterDamper(BlendShapeJitterImpl manager, int index, string name, float weightMagnification)
        {
            this.index = index;
            this.name = name;
            this.weightMagnification = weightMagnification;
            Initialize(manager);
        }

        public BlendShapeJitterDamper(int index, string name, float weightMagnification)
        {
            this.index = index;
            this.name = name;
            this.weightMagnification = weightMagnification;
        }

        public BlendShapeJitterDamper Instantiate()
        {
            return new BlendShapeJitterDamper(index, "", weightMagnification);
        }

        public void Initialize(BlendShapeJitterImpl manager)
        {
            this.manager = manager;
        }

        public float GetCurrentWeight()
        {
            weight = manager.skinnedMeshRenderer.GetBlendShapeWeight(index);
            weight = Mathf.Clamp01(weight / 100f);
            return weight * weightMagnification;
        }

        public void SetMorphName(BlendShapeJitterImpl manager)
        {
            if(this.manager == null) Initialize(manager);

            var sharedMesh = this.manager.skinnedMeshRenderer.sharedMesh;
            index = Mathf.Min(index, sharedMesh.blendShapeCount - 1);
            name = (index >= 0) ? sharedMesh.GetBlendShapeName(index) : "";
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(BlendShapeJitterDamper))]
    public class JitterDampDrawer : PropertyDrawer
    {
        const int CLEARANCE_X = 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                //各プロパティー取得
                var indexProperty = property.FindPropertyRelative("index");
                var nameProperty = property.FindPropertyRelative("name");
                var weightProperty = property.FindPropertyRelative("weight");
                var magnificationProperty = property.FindPropertyRelative("weightMagnification");

                //表示位置を調整
                var indexRect = new Rect(position)
                {
                    width = 30
                };

                var nameRect = new Rect(position)
                {
                    x = position.x + indexRect.width + CLEARANCE_X,
                    width = position.width / 5f
                };

                var weightRect = new Rect(nameRect)
                {
                    x = nameRect.x + nameRect.width + CLEARANCE_X,
                    width = nameRect.width
                };

                var magnificationRect = new Rect(weightRect)
                {
                    x = weightRect.x + weightRect.width + CLEARANCE_X,
                    width = weightRect.width * 3 - CLEARANCE_X * 3 - 30
                };

                //Morph Index
                indexProperty.intValue = EditorGUI.IntField(indexRect, indexProperty.intValue);

                //Morph Name
                EditorGUI.LabelField(nameRect, nameProperty.stringValue);

                //Morph Weight
                var weight = weightProperty.floatValue * 100f;
                EditorGUI.ProgressBar(weightRect, weight / 100f, weight.ToString("F2"));

                //Weight Magnification
                magnificationProperty.floatValue = EditorGUI.FloatField(magnificationRect, magnificationProperty.floatValue);
            }
        }
    }
#endif
}