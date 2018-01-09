using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    [System.Serializable]
    public class JitterParameter : JitterParameterBase
    {
        //Editor用
        public bool isXAxis;
        public bool syncAxis;

        /// <summary>
        /// 波形生成用のプロパティ(再生中に変動しない)
        /// Loop用とOnce用にそれぞれ3つずつインスタンス化される。
        /// </summary>
        /// <param name="curve">timer→Amplitudeの変換曲線</param>
        /// <param name="loop">ループ用か外部入力用か。</param>
        /// <param name="isXAxis">X軸(syncAxis時のEditor用)</param>
        public JitterParameter(AnimationCurve curve, bool loop, bool isXAxis = false) : base(curve, loop)
        {
            amplitude.minLimit = -1;
            offset.minLimit = -1;
            this.isXAxis = isXAxis;

            //パラメータ初期化
            period.min = 1f;
            if (loop)
            {
                period.max = 3f;
                offset.max = 0.3f;
                offset.min = -0.3f;
                easingPeriod = Easing.CubicInOut;
                easingAmplitude = Easing.CubicInOut;
                easingOffset = Easing.CubicInOut;
            }
            else
            {
                period.max = 1f;
                amplitude.min = -0.5f;
                amplitude.max = 0.5f;
                easingPeriod = Easing.None;
                easingAmplitude = Easing.None;
                easingOffset = Easing.None;
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
                bool isXAxis = property.FindPropertyRelative("isXAxis").boolValue;
                bool loop = property.FindPropertyRelative("loop").boolValue;
                bool syncAxis = property.FindPropertyRelative("syncAxis").boolValue;

                //各プロパティー取得
                var periodProperty = property.FindPropertyRelative("period");
                var intervalProperty = property.FindPropertyRelative("interval");
                var amplitudeProperty = property.FindPropertyRelative("amplitude");
                var offsetProperty = property.FindPropertyRelative("offset");
                var easingPeriodProperty = property.FindPropertyRelative("easingPeriod");
                var easingAmplitudeProperty = property.FindPropertyRelative("easingAmplitude");
                var easingOffsetProperty = property.FindPropertyRelative("easingOffset");

                position.height = EditorGUIUtility.singleLineHeight;
                
                if ((syncAxis && isXAxis) || (!syncAxis && isEnabled))
                {
                    //Period Interval
                    PutPropertyField(ref position, periodProperty);
                    if (!loop)
                        PutPropertyField(ref position, intervalProperty);
                    //Amplitude Offset
                    PutPropertyField(ref position, amplitudeProperty);
                    if (loop)
                        PutPropertyField(ref position, offsetProperty);
                    //Easing
                    if (loop)
                    {
                        //Easing Period
                        var tmp = (System.Enum)(Easing)easingPeriodProperty.enumValueIndex;
                        easingPeriodProperty.enumValueIndex = (int)(Easing)EditorGUI.EnumPopup(
                            position, easingPeriodProperty.displayName, tmp);

                        position.y += position.height + CLEARANCE_Y;

                        //Easing Amplitude
                        tmp = (System.Enum)(Easing)easingAmplitudeProperty.enumValueIndex;
                        easingAmplitudeProperty.enumValueIndex = (int)(Easing)EditorGUI.EnumPopup(
                            position, easingAmplitudeProperty.displayName, tmp);

                        position.y += position.height + CLEARANCE_Y;

                        //Easing Offset
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
            bool isXAxis = property.FindPropertyRelative("isXAxis").boolValue;
            bool loop = property.FindPropertyRelative("loop").boolValue;
            bool syncAxis = property.FindPropertyRelative("syncAxis").boolValue;
            
            int tmpRow = loop ? 6 : 3;
            int row = ((syncAxis && isXAxis) || (!syncAxis && isEnabled)) ? tmpRow : 0;

            return row * (EditorGUIUtility.singleLineHeight + CLEARANCE_Y);
        }
    }
#endif
}