using UnityEngine;

namespace MYB.Jitter
{
    /// <summary>
    /// LoopとOnceのStateを管理する。
    /// (syncAxis ? 1 : 3)個インスタンス化され、同数のコルーチンが走る。
    /// </summary>
    [System.Serializable]
    public class JitterHelper
    {
        /// <summary>
        /// 再生中に変動するパラメータ群
        /// </summary>
        public class State
        {
            [System.NonSerialized]
            public JitterParameter param;

            [System.NonSerialized]
            public bool isProcessing;

            [System.NonSerialized]
            public float timer;         //周期毎にリセットするカウンタ

            [System.NonSerialized]
            public float curPeriod;     //周期(秒)

            [System.NonSerialized]
            public float nextPeriod;

            [System.NonSerialized]
            public float curInterval;   //次周期までの待ち時間(秒)

            [System.NonSerialized]
            public float curAmplitude;  //angleWeight振幅

            [System.NonSerialized]
            public float nextAmplitude;

            [System.NonSerialized]
            public float curOffset;     //angleWeight下限

            [System.NonSerialized]
            public float nextOffset;

            public State(JitterParameter _param)
            {
                param = _param;
                nextAmplitude = param.amplitude.Random();
                nextOffset = param.offset.Random();

                SetNextParameter();
            }

            public void SetNextParameter()
            {
                curPeriod = nextPeriod;
                nextPeriod = param.period.Random();

                curInterval = param.interval.Random();

                curAmplitude = nextAmplitude;
                nextAmplitude = param.amplitude.Random();

                curOffset = nextOffset;
                nextOffset = param.offset.Random();
            }

            public void SetOnceParameter()
            {
                curPeriod = param.period.Random();
                curInterval = param.interval.Random();
                curAmplitude = param.amplitude.Random();
                curOffset = param.offset.Random();
            }

            /// <summary>
            /// 現在のWeightを計算
            /// </summary>
            /// <returns>weight</returns>
            public float GetCurrentWeight(AnimationCurve curve)
            {
                if (curPeriod <= 0f) return curOffset;

                float timer01 = Mathf.Clamp01(timer);
                float amp = param.easingAmplitude.Evaluate(curAmplitude, nextAmplitude, timer01);
                float ofs = param.easingOffset.Evaluate(curOffset, nextOffset, timer01);
                return Mathf.Clamp(curve.Evaluate(timer01) * amp + ofs, -1, 1);
            }

            public float GetCurrentPeriod()
            {
                float timer01 = Mathf.Clamp01(timer);
                return param.easingPeriod.Evaluate(curPeriod, nextPeriod, timer01);
            }

            public void Reset()
            {
                isProcessing = false;
                timer = 0f;
            }

            public void UpdateLoop()
            {
                isProcessing = true;

                if (timer < 1f)
                {
                    timer += Time.deltaTime / GetCurrentPeriod();
                }
                else if (timer < 1f + curInterval)
                {
                    timer += Time.deltaTime;
                }
                else
                {
                    timer = 0f;
                    SetNextParameter();
                }
            }

            public void UpdateOnce(System.Action callback)
            {
                if (timer < 1f)
                {
                    timer += Time.deltaTime / GetCurrentPeriod();
                }
                else if (timer < 1f + curInterval)
                {
                    timer += Time.deltaTime;
                }
                else
                {
                    timer = 0f;
                    isProcessing = false;
                    callback();
                }
            }
        }

        [System.NonSerialized]
        public State loopState;

        [System.NonSerialized]
        public State onceState;

        public bool isProcessing { get { return loopState.isProcessing || onceState.isProcessing; } }
        public bool OnceIsProcessing { get { return onceState.isProcessing; } }

        public JitterHelper(JitterParameter loopParameter, JitterParameter onceParameter)
        {
            loopState = new State(loopParameter);
            onceState = new State(onceParameter);
        }

        public JitterHelper Instantiate()
        {
            return new JitterHelper(loopState.param, onceState.param);
        }

        public void ResetLoopState()
        {
            loopState.Reset();
        }

        public void ResetOnceState()
        {
            onceState.Reset();
        }
        
        public float GetLoopAngle(AnimationCurve loopCurve)
        {
            return loopState.GetCurrentWeight(loopCurve);
        }

        public float GetOnceAngle(AnimationCurve onceCurve)
        {
            return onceState.GetCurrentWeight(onceCurve);
        }
    }
}