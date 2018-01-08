using UnityEngine;

namespace MYB.Jitter
{
    /// <summary>
    /// Transformの更新方法
    /// </summary>
    public enum UpdateMode
    {
        Override,
        Reference,
        AfterAnimation,
    }

    /// <summary>
    /// 波形生成用のプロパティ(再生中に変動しない)
    /// ループ用と外部入力用の2つインスタンス化される。
    /// </summary>
    public class JitterParameterBase
    {
        //次周期のパラメータとの補間(AmplitudeとOffset)
        public enum BlendState
        {
            None,
            Linear,
            Curve,
        }

        const float MIN_LIMIT_PERIOD = 0.04f;
        const float MAX_LIMIT_PERIOD = 5f;
        const float MIN_LIMIT_INTERVAL = 0f;
        const float MAX_LIMIT_INTERVAL = 5f;

        public FloatRange period;       //周期(秒)
        public FloatRange interval;     //次周期までの待ち時間(秒)
        public FloatRange amplitude;    //morphWeight振れ幅
        public FloatRange offset;       //morphWeight下限

        public AnimationCurve periodToAmplitude;    //weight = curve(timer/period) * amplitude + offset
        public BlendState blendNextPeriod;
        public BlendState blendNextAmplitude;

        //Editor用
        public bool isEnabled;
        public bool loop;
        public float preMaxAmplitude;
        public float preMinAmplitude;
        public float preMaxOffset;
        public float preMinOffset;

        /// <summary>
        /// 波形生成用のプロパティ(再生中に変動しない)
        /// ループ用と外部入力用の2つインスタンス化される。
        /// </summary>
        /// <param name="curve">timer→Amplitudeの変換曲線</param>
        /// <param name="loop">Loop用かOnce用か。</param>
        public JitterParameterBase(AnimationCurve curve, bool loop)
        {
            period = new FloatRange(MIN_LIMIT_PERIOD, MAX_LIMIT_PERIOD, true, false);
            interval = new FloatRange(MIN_LIMIT_INTERVAL, MAX_LIMIT_INTERVAL, true, false);
            amplitude = new FloatRange();
            offset = new FloatRange();
            
            isEnabled = true;
            periodToAmplitude = curve;
            this.loop = loop;
        }

        public void AdjustParameter()
        {
            //maxAmplitude変更
            if (amplitude.max != preMaxAmplitude)
            {
                offset.max = Mathf.Clamp(offset.max, offset.minLimit, offset.maxLimit - amplitude.max);
                offset.min = Mathf.Clamp(offset.min, offset.minLimit, offset.max);
            }

            //minAmplitude変更
            if (amplitude.min != preMinAmplitude)
            {
                offset.min = Mathf.Clamp(offset.min, offset.minLimit - amplitude.min, offset.max);
                offset.max = Mathf.Clamp(offset.max, offset.min, offset.maxLimit);
            }

            //maxOffset変更
            if (offset.max != preMaxOffset)
            {
                amplitude.max = Mathf.Clamp(amplitude.max, amplitude.minLimit, amplitude.maxLimit - offset.max);
                amplitude.min = Mathf.Clamp(amplitude.min, amplitude.minLimit, amplitude.max);
            }

            //minOffset変更
            if (offset.min != preMinOffset)
            {
                amplitude.min = Mathf.Clamp(amplitude.min, amplitude.minLimit - offset.min, amplitude.max);
                amplitude.max = Mathf.Clamp(amplitude.max, amplitude.min, amplitude.maxLimit);
            }

            preMaxAmplitude = amplitude.max;
            preMinAmplitude = amplitude.min;
            preMaxOffset = offset.max;
            preMinOffset = offset.min;
        }

        public void CopyFrom(JitterParameterBase input, bool CopyFully = false)
        {
            period.CopyFrom(input.period);
            interval.CopyFrom(input.interval);
            amplitude.CopyFrom(input.amplitude);
            offset.CopyFrom(input.offset);
            blendNextPeriod = input.blendNextPeriod;
            blendNextAmplitude = input.blendNextAmplitude;

            if (CopyFully)
            {
                periodToAmplitude = new AnimationCurve(input.periodToAmplitude.keys);
            }
        }
    }
}