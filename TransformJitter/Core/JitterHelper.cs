﻿using UnityEngine;
using System;

namespace MYB.Jitter
{
    /// <summary>
    /// LoopとOnceのStateを管理する。
    /// (syncAxis ? 1 : 3)個インスタンス化され、同数のコルーチンが走る。
    /// </summary>
    [Serializable]
    public class JitterHelper
    {
        /// <summary>
        /// 再生中に変動するパラメータ群
        /// </summary>
        public class State
        {
            [NonSerialized]
            public JitterParameter param;

            [NonSerialized]
            public bool isProcessing;

            [NonSerialized]
            public float timer;         //周期毎にリセットするカウンタ

            [NonSerialized]
            public float curPeriod;     //周期(秒)

            [NonSerialized]
            public float nextPeriod;

            [NonSerialized]
            public float curInterval;   //次周期までの待ち時間(秒)

            [NonSerialized]
            public float curAmplitude;  //angleWeight振幅

            [NonSerialized]
            public float nextAmplitude;

            [NonSerialized]
            public float curOffset;     //angleWeight下限

            [NonSerialized]
            public float nextOffset;

            [NonSerialized]
            public float fastForward;

            public State(JitterParameter _param)
            {
                param = _param;
                nextAmplitude = param.amplitude.RandomInside();
                nextOffset = param.offset.RandomInside();
                fastForward = 0f;

                SetNextParameter();
            }

            public State SetNextParameter()
            {
                fastForward = 0f;
                SetNextPeriod();
                SetNextAmplitude();
                return this;
            }

            public void SetNextParameter(State state, bool syncPeriod, bool syncAmplitude)
            {
                timer = 0f;
                fastForward = 0f;
                if (syncPeriod)
                {
                    curPeriod = state.curPeriod;
                    nextPeriod = state.nextPeriod;
                    curInterval = state.curInterval;
                }
                else
                {
                    SetNextPeriod();
                }

                if (syncAmplitude)
                {
                    curAmplitude = state.curAmplitude;
                    nextAmplitude = state.nextAmplitude;
                    curOffset = state.curOffset;
                    nextOffset = state.nextOffset;
                }
                else
                {
                    SetNextAmplitude();
                }
            }

            void SetNextPeriod()
            {
                curPeriod = nextPeriod;
                nextPeriod = param.period.RandomInside();
                curInterval = param.interval.RandomInside();
            }

            void SetNextAmplitude()
            {
                curAmplitude = nextAmplitude;
                nextAmplitude = param.amplitude.RandomInside();
                curOffset = nextOffset;
                nextOffset = param.offset.RandomInside();
            }

            public void SetOnceParameter()
            {
                curPeriod = param.period.RandomInside();
                curInterval = param.interval.RandomInside();
                curAmplitude = param.amplitude.RandomInside();
                curOffset = param.offset.RandomInside();
            }

            /// <summary>
            /// 現在のWeightを計算
            /// </summary>
            /// <returns>weight</returns>
            public float GetCurrentWeight(AnimationCurve curve, JitterParameter _param = null)
            {
                if (curPeriod <= 0f) return curOffset;

                var easeParam = (_param == null) ? param : _param;

                float timer01 = Mathf.Clamp01(timer);
                float amp = easeParam.easingAmplitude.Evaluate(curAmplitude, nextAmplitude, timer01);
                float ofs = easeParam.easingOffset.Evaluate(curOffset, nextOffset, timer01);
                return Mathf.Clamp(curve.Evaluate(timer01) * amp + ofs, -1, 1);
            }

            public float GetCurrentPeriod()
            {
                float timer01 = Mathf.Clamp01(timer);
                return param.easingPeriod.Evaluate(curPeriod, nextPeriod, timer01);
            }

            public float GetCurrentPeriod(Easing easePeriod)
            {
                float timer01 = Mathf.Clamp01(timer);
                return easePeriod.Evaluate(curPeriod, nextPeriod, timer01);
            }

            public void Reset()
            {
                isProcessing = false;
                timer = 0f;
            }

            public State UpdateLoop(Easing easePeriod)
            {
                isProcessing = true;
                State nextState = null;

                if (timer < 1f)
                {
                    timer += Time.deltaTime / GetCurrentPeriod(easePeriod);
                    timer += (1f - timer) * fastForward;
                }
                else if (timer < 1f + curInterval)
                {
                    timer += Time.deltaTime;
                    timer += (1f + curInterval - timer) * fastForward;
                }
                else
                {
                    timer = 0f;
                    nextState = SetNextParameter();
                }
                return nextState;
            }

            public void UpdateOnce(Action callback)
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

        [NonSerialized]
        public State loopState;

        [NonSerialized]
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

        public void MoveNext(float speed)
        {
            loopState.fastForward = speed;
        }
        
        public float GetCurrentLoopWeight(AnimationCurve loopCurve, JitterParameter _param)
        {
            return loopState.GetCurrentWeight(loopCurve, _param);
        }

        public float GetCurrentOnceWeight(AnimationCurve onceCurve)
        {
            return onceState.GetCurrentWeight(onceCurve);
        }
    }
}