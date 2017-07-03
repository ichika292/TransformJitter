using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.BlendShapeJitter
{
    [System.Serializable]
    public class JitterParameter : JitterParameterBase
    {
        public float magnification;

        /// <summary>
        /// 波形生成用のプロパティ(再生中に変動しない)
        /// Loop用とOnce用の2つインスタンス化される。
        /// </summary>
        /// <param name="curve">timer→Amplitudeの変換曲線</param>
        /// <param name="loop">ループ用か外部入力用か。</param>
        public JitterParameter(AnimationCurve curve, bool loop) : base(curve, loop)
        {
            magnification = 1f;
            period.min = 1f;
            if (loop)
            {
                period.max = 3f;
                offset.max = 0.3f;
                blendNextPeriod = BlendState.Curve;
                blendNextAmplitude = BlendState.Curve;
            }
            else
            {
                period.max = 1f;
                amplitude.min = 0.5f;
                amplitude.max = 1.0f;
                blendNextPeriod = BlendState.None;
                blendNextAmplitude = BlendState.None;
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(JitterParameter))]
    public class JitterParameterDrawer : PropertyDrawer
    {
        const int CLEARANCE_Y = 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                bool isEnabled = property.FindPropertyRelative("isEnabled").boolValue;
                bool loop = property.FindPropertyRelative("loop").boolValue;

                //各プロパティー取得
                var curveProperty = property.FindPropertyRelative("periodToAmplitude");
                var periodProperty = property.FindPropertyRelative("period");
                var intervalProperty = property.FindPropertyRelative("interval");
                var amplitudeProperty = property.FindPropertyRelative("amplitude");
                var offsetProperty = property.FindPropertyRelative("offset");
                var blendNextPeriodProperty = property.FindPropertyRelative("blendNextPeriod");
                var blendNextAmplitudeProperty = property.FindPropertyRelative("blendNextAmplitude");

                position.height = EditorGUIUtility.singleLineHeight;

                //Curve
                if (isEnabled)
                {
                    curveProperty.animationCurveValue = EditorGUI.CurveField(position, curveProperty.displayName, curveProperty.animationCurveValue);
                    position.y += position.height + CLEARANCE_Y;
                }

                if (isEnabled)
                {
                    //Period Interval
                    PutPropertyField(ref position, periodProperty);
                    PutPropertyField(ref position, intervalProperty);
                    //Amplitude Offset
                    PutPropertyField(ref position, amplitudeProperty);
                    if (loop)
                        PutPropertyField(ref position, offsetProperty);
                    //Blend State
                    if (loop)
                    {
                        var tmp = (System.Enum)(JitterParameter.BlendState)blendNextPeriodProperty.enumValueIndex;
                        blendNextPeriodProperty.enumValueIndex = (int)(JitterParameter.BlendState)EditorGUI.EnumPopup(
                            position, blendNextPeriodProperty.displayName, tmp);

                        position.y += position.height + CLEARANCE_Y;

                        tmp = (System.Enum)(JitterParameter.BlendState)blendNextAmplitudeProperty.enumValueIndex;
                        blendNextAmplitudeProperty.enumValueIndex = (int)(JitterParameter.BlendState)EditorGUI.EnumPopup(
                            position, blendNextAmplitudeProperty.displayName, tmp);
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

            int row = isEnabled ? (loop ? 7 : 4) : 0;
            return row * (EditorGUIUtility.singleLineHeight + CLEARANCE_Y);
        }
    }
#endif
}