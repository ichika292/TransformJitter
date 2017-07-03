﻿using UnityEngine;

namespace MYB.TransformJitter
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
            public JitterParameter param;
            public bool isProcessing;
            public float timer;         //周期毎にリセットするカウンタ
            public float curPeriod;     //周期(秒)
            public float nextPeriod;
            public float curInterval;   //次周期までの待ち時間(秒)
            public float curAmplitude;  //angleWeight振幅
            public float nextAmplitude;
            public float curOffset;     //angleWeight下限
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
                float amp = CalcBlendState(curAmplitude, nextAmplitude, timer01, param.blendNextAmplitude);
                float ofs = CalcBlendState(curOffset, nextOffset, timer01, param.blendNextAmplitude);
                return Mathf.Clamp(curve.Evaluate(timer01) * amp + ofs, -1, 1);
            }

            public float GetCurrentPeriod()
            {
                float timer01 = Mathf.Clamp01(timer);
                return CalcBlendState(curPeriod, nextPeriod, timer01, param.blendNextPeriod);
            }

            public void Reset()
            {
                isProcessing = false;
                timer = 0f;
            }
        }
        
        public State loopState;
        public State onceState;

        public bool isProcessing { get { return loopState.isProcessing || onceState.isProcessing; } }
        public bool OnceIsProcessing { get { return onceState.isProcessing; } }

        public JitterHelper(JitterParameter loopParameter, JitterParameter onceParameter)
        {
            loopState = new State(loopParameter);
            onceState = new State(onceParameter);
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

        /// <summary>
        /// 次周期のパラメータとの補間(AmplitudeとOffset)
        /// </summary>
        static float CalcBlendState(float current, float next, float t, JitterParameter.BlendState blendState)
        {
            switch (blendState)
            {
                case JitterParameter.BlendState.Linear:
                    return Mathf.Lerp(current, next, t);
                case JitterParameter.BlendState.Curve:
                    return (next - current) * (-2 * t + 3) * t * t + current;
                default:
                    return current;
            }
        }
    }
}