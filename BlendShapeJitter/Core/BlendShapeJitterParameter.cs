using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    [System.Serializable]
    public class BlendShapeJitterParameter : BlendShapeJitterParameterBase
    {
        public float magnification;

        /// <summary>
        /// 波形生成用のプロパティ(再生中に変動しない)
        /// Loop用とOnce用の2つインスタンス化される。
        /// </summary>
        /// <param name="curve">timer→Amplitudeの変換曲線</param>
        /// <param name="loop">ループ用か外部入力用か。</param>
        public BlendShapeJitterParameter(AnimationCurve curve, bool loop) : base(curve, loop)
        {
            magnification = 1f;
            period.min = 1f;
            if (loop)
            {
                period.max = 3f;
                offset.max = 0.3f;
                easingPeriod = Easing.CubicInOut;
                easingAmplitude = Easing.CubicInOut;
                easingOffset = Easing.CubicInOut;
            }
            else
            {
                period.max = 1f;
                amplitude.min = 0.5f;
                amplitude.max = 1.0f;
                easingPeriod = Easing.None;
                easingAmplitude = Easing.None;
                easingOffset = Easing.None;
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(BlendShapeJitterParameter))]
    public class BlendShapeJitterParameterDrawer : PropertyDrawer
    {
        const int CLEARANCE_Y = 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                bool isEnabled = property.FindPropertyRelative("isEnabled").boolValue;
                bool loop = property.FindPropertyRelative("loop").boolValue;

                //各プロパティー取得
                var periodProperty = property.FindPropertyRelative("period");
                var intervalProperty = property.FindPropertyRelative("interval");
                var amplitudeProperty = property.FindPropertyRelative("amplitude");
                var offsetProperty = property.FindPropertyRelative("offset");
                var easingPeriodProperty = property.FindPropertyRelative("easingPeriod");
                var easingAmplitudeProperty = property.FindPropertyRelative("easingAmplitude");
                var easingOffsetProperty = property.FindPropertyRelative("easingOffset");

                position.height = EditorGUIUtility.singleLineHeight;
                
                if (isEnabled)
                {
                    //Period Interval
                    PutPropertyField(ref position, periodProperty);
                    PutPropertyField(ref position, intervalProperty);
                    //Amplitude Offset
                    PutPropertyField(ref position, amplitudeProperty);
                    if (loop)
                        PutPropertyField(ref position, offsetProperty);
                    //Easing
                    if (loop)
                    {
                        var tmp = (System.Enum)(Easing)easingPeriodProperty.enumValueIndex;
                        easingPeriodProperty.enumValueIndex = (int)(Easing)EditorGUI.EnumPopup(
                            position, easingPeriodProperty.displayName, tmp);

                        position.y += position.height + CLEARANCE_Y;

                        tmp = (System.Enum)(Easing)easingAmplitudeProperty.enumValueIndex;
                        easingAmplitudeProperty.enumValueIndex = (int)(Easing)EditorGUI.EnumPopup(
                            position, easingAmplitudeProperty.displayName, tmp);

                        position.y += position.height + CLEARANCE_Y;

                        tmp = (System.Enum)(Easing)easingOffsetProperty.enumValueIndex;
                        easingOffsetProperty.enumValueIndex = (int)(Easing)EditorGUI.EnumPopup(
                            position, easingOffsetProperty.displayName, tmp);
                    }
                }
            }
        }

        static void PutPropertyField(ref Rect position, SerializedProperty property)
        {
            EditorGUI.PropertyField(position, property);
            position.y += position.height + CLEARANCE_Y;
        }

        //GUI 要素の高さ
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool isEnabled = property.FindPropertyRelative("isEnabled").boolValue;
            bool loop = property.FindPropertyRelative("loop").boolValue;

            int row = isEnabled ? (loop ? 7 : 3) : 0;
            return row * (EditorGUIUtility.singleLineHeight + CLEARANCE_Y);
        }
    }
#endif
}