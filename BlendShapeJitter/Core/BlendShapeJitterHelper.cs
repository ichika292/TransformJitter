using System;
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
    [Serializable]
    public class BlendShapeJitterHelper
    {
        /// <summary>
        /// 再生中に変動するパラメータ群
        /// </summary>
        public class State
        {
            [NonSerialized]
            public BlendShapeJitterParameter param;

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
            public float curAmplitude;  //morphWeight振幅

            [NonSerialized]
            public float nextAmplitude;

            [NonSerialized]
            public float curOffset;     //morphWeight下限

            [NonSerialized]
            public float nextOffset;

            [NonSerialized]
            public int currentKeyframeIndex;    //AnimationCurveの現在再生中の(Timerに対応する)Keyframeのindex

            [NonSerialized]
            public float fastForward;

            public State(BlendShapeJitterParameter _param)
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
            
            public State UpdateLoop()
            {
                isProcessing = true;
                State nextState = null;

                if (timer < 1f)
                {
                    timer += Time.deltaTime / GetCurrentPeriod();
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

            /// <summary>
            /// 現在のWeightを計算
            /// </summary>
            /// <returns>weight</returns>
            public float GetCurrentWeight()
            {
                if (curPeriod <= 0f) return curOffset;
                
                float timer01 = Mathf.Clamp01(timer);
                float value = param.periodToAmplitude.Evaluate(timer01);
                var keys = param.periodToAmplitude.keys;
                int length = keys.Length;
                for (int i = currentKeyframeIndex; i < length; i++)
                {
                    if (timer > keys[i].time)
                    {
                        value = keys[i].value;
                        currentKeyframeIndex = i + 1;
                        if (currentKeyframeIndex >= length)
                            currentKeyframeIndex = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                float amp = param.easingAmplitude.Evaluate(curAmplitude, nextAmplitude, timer01);
                float ofs = param.easingOffset.Evaluate(curOffset, nextOffset, timer01);
                float weight = Mathf.Clamp01(value * amp + ofs);
                return weight * param.magnification;
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
        }

        public BlendShapeJitterImpl manager;
        public string name;
        public float weightMagnification = 100f;
        public float weight;
        public bool overrideWeight;
        public int index;
                
        public State loopState;
        public State onceState;
        
        public bool OnceIsProcessing { get { return onceState.isProcessing; } }

        public BlendShapeJitterHelper(BlendShapeJitterImpl manager, int index, string name, float weightMagnification)
        {
            Initialize(manager);
            this.index = index;
            this.name = name;
            this.weightMagnification = weightMagnification;
        }

        public BlendShapeJitterHelper(int index, string name, float weightMagnification)
        {
            this.index = index;
            this.name = name;
            this.weightMagnification = weightMagnification;
        }

        public BlendShapeJitterHelper Instantiate()
        {
            return new BlendShapeJitterHelper(index, "", weightMagnification);
        }

        public void Initialize(BlendShapeJitterImpl manager)
        {
            this.manager = manager;
            loopState = new State(manager.loopParameter);
            onceState = new State(manager.onceParameter);
        }

        public void ResetState()
        {
            ResetLoopState();
            ResetOnceState();
            weight = 0f;
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

        /// <summary>
        /// morphWeightをMMD4MecanimModel.Morphに適用
        /// </summary>
        public void UpdateMorph()
        {
            var skinnedMeshRenderer = manager.skinnedMeshRenderer;
            if (skinnedMeshRenderer == null) return;

            index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name);
            var newWeight = Mathf.Clamp(weight * weightMagnification, 0f, 100f);
            if (index >= 0)
                skinnedMeshRenderer.SetBlendShapeWeight(index, newWeight);
        }

        /// <summary>
        /// LoopとOnceのStateからそれぞれmorphWeightを計算
        /// </summary>
        public float SetMorphWeight()
        {
            this.weight = GetMorphWeight();
            UpdateMorph();
            return weight;
        }

        /// <summary>
        /// 数値指定でweightを適用
        /// </summary>
        /// <param name="weight"></param>
        public void SetMorphWeight(float weight)
        {
            this.weight = weight;
            UpdateMorph();
        }
        
        public float GetMorphWeight()
        {
            var weight = 0f;

            if (manager.loopGroupEnabled)
                weight += loopState.GetCurrentWeight();

            if (manager.onceGroupEnabled)
                weight += onceState.GetCurrentWeight();

            return Mathf.Clamp01(weight);
        }
        
        public void SetMorphName(BlendShapeJitterImpl manager)
        {
            if (this.manager == null) Initialize(manager);

            var sharedMesh = this.manager.skinnedMeshRenderer.sharedMesh;
            index = Mathf.Min(index, sharedMesh.blendShapeCount - 1);
            name = (index >= 0) ? sharedMesh.GetBlendShapeName(index) : "";
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(BlendShapeJitterHelper))]
    public class JitterHelperDrawer : PropertyDrawer
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
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                {
                    indexProperty.intValue = EditorGUI.IntField(indexRect, indexProperty.intValue);
                }
                EditorGUI.EndDisabledGroup();

                //Morph Name
                EditorGUI.LabelField(nameRect, nameProperty.stringValue);

                //Morph Weight
                var weight = weightProperty.floatValue * magnificationProperty.floatValue;
                EditorGUI.ProgressBar(weightRect, weight / 100f, weight.ToString("F2"));

                //Weight Magnification
                magnificationProperty.floatValue = EditorGUI.Slider(magnificationRect, magnificationProperty.floatValue, 0f, 100f);
            }
        }
    }
#endif
}