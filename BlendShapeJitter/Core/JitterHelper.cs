using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.BlendShapeJitter
{
    /// <summary>
    /// LoopとOnceのStateを管理する。
    /// ReorderableListで設定されたMorphと同数がインスタンス化され、同数のコルーチンが走る。
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
            public float curAmplitude;  //morphWeight振幅
            public float nextAmplitude;
            public float curOffset;     //morphWeight下限
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
            public float GetCurrentWeight()
            {
                if (curPeriod <= 0f) return curOffset;

                float timer01 = Mathf.Clamp01(timer);
                float amp = CalcBlendState(curAmplitude, nextAmplitude, timer01, param.blendNextAmplitude);
                float ofs = CalcBlendState(curOffset, nextOffset, timer01, param.blendNextAmplitude);
                float weight = Mathf.Clamp01(param.periodToAmplitude.Evaluate(timer01) * amp + ofs);
                return weight * param.magnification;
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

        public JitterImpl manager;
        public string name;
        public float weightMagnification = 100f;
        public float weight;
        public bool overrideWeight;

        protected SkinnedMeshRenderer skinnedMeshRenderer;
        int index;

        public State loopState;
        public State onceState;

        public bool isProcessing { get { return loopState.isProcessing || onceState.isProcessing; } }
        public bool OnceIsProcessing { get { return onceState.isProcessing; } }

        public JitterHelper(JitterImpl manager, int index, string name, float weightMagnification)
        {
            Initialize(manager);
            this.index = index;
            this.name = name;
            this.weightMagnification = weightMagnification;
        }

        public void Initialize(JitterImpl manager)
        {
            this.manager = manager;
            this.skinnedMeshRenderer = manager.skinnedMeshRenderer;
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

        /// <summary>
        /// morphWeightをMMD4MecanimModel.Morphに適用
        /// </summary>
        public void UpdateMorph()
        {
            if (skinnedMeshRenderer == null) return;

            index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name);
            if (index >= 0)
                skinnedMeshRenderer.SetBlendShapeWeight(index, weight * weightMagnification);
        }

        /// <summary>
        /// LoopとOnceのStateからそれぞれmorphWeightを計算
        /// </summary>
        public float SetMorphWeight()
        {
            var weight = 0f;

            if (manager.loopGroupEnabled)
                weight += loopState.GetCurrentWeight();

            if (manager.onceGroupEnabled)
                weight += onceState.GetCurrentWeight();

            this.weight = Mathf.Clamp01(weight);
            UpdateMorph();
            return weight;
        }

        /// <summary>
        /// 数値指定でweightを適用
        /// </summary>
        /// <param name="weight"></param>
        public void SetMorphWeight(float weight)
        {
            this.weight = Mathf.Clamp01(weight);
            UpdateMorph();
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

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(JitterHelper))]
    public class JitterHelperDrawer : PropertyDrawer
    {
        const int CLEARANCE_X = 4;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                //各プロパティー取得
                var nameProperty = property.FindPropertyRelative("name");
                var weightProperty = property.FindPropertyRelative("weight");
                var magnificationProperty = property.FindPropertyRelative("weightMagnification");

                //表示位置を調整
                var nameRect = new Rect(position)
                {
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
                    width = weightRect.width * 3 - CLEARANCE_X * 2
                };

                //Morph Name
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
                {
                    nameProperty.stringValue = EditorGUI.DelayedTextField(nameRect, nameProperty.stringValue);
                }
                EditorGUI.EndDisabledGroup();

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