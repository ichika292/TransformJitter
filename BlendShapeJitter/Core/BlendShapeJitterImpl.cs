using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    public class BlendShapeJitterImpl : Jitter
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public bool sync;
        public bool overrideOnce;
        public List<BlendShapeJitterHelper> helperList = new List<BlendShapeJitterHelper>();

        public BlendShapeJitterParameter loopParameter = new BlendShapeJitterParameter(PrimitiveAnimationCurve.UpDown5, true);
        public BlendShapeJitterParameter onceParameter = new BlendShapeJitterParameter(PrimitiveAnimationCurve.UpDown1, false);

        protected List<Coroutine> loopRoutineList = new List<Coroutine>();
        protected List<Coroutine> onceRoutineList = new List<Coroutine>();
        protected Coroutine fadeInRoutine, fadeOutRoutine;

        //Editor用
        public bool loopGroupEnabled = true;
        public bool onceGroupEnabled = true;

        //コルーチンが動作中か否か
        public bool isProcessing
        {
            get {
                bool result = false;
                foreach (BlendShapeJitterHelper h in helperList)
                {
                    if (h.isProcessing)
                    {
                        result = true;
                        break;
                    }
                }
                return result;
            }
        }

        //Onceコルーチンが動作中か否か
        public bool OnceIsProcessing
        {
            get {
                bool result = false;
                foreach (BlendShapeJitterHelper h in helperList)
                {
                    if (h.OnceIsProcessing)
                    {
                        result = true;
                        break;
                    }
                }
                return result;
            }
        }

        void Reset()
        {
            skinnedMeshRenderer = GetComponentInParent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                Debug.Log("SkinnedMeshRenderer not found.");
                return;
            }
            if(skinnedMeshRenderer.sharedMesh.blendShapeCount == 0)
            {
                Debug.Log("BlendShapes not found.");
                return;
            }
        }

        void Awake()
        {
            foreach (BlendShapeJitterHelper h in helperList)
                h.Initialize(this);
        }

        void Update()
        {
            if (!isProcessing) return;

            SetMorphWeight();
        }

        protected void SetMorphWeight()
        {
            float? weight = null;
            if (sync)
            {
                //helperList[0]のweight計算結果を全モーフで共有
                foreach (BlendShapeJitterHelper h in helperList)
                {
                    if (!weight.HasValue)
                        weight = h.SetMorphWeight();
                    else
                        h.SetMorphWeight(weight.Value);
                }
            }
            else
            {
                foreach (BlendShapeJitterHelper h in helperList)
                {
                    h.SetMorphWeight();
                }
            }
        }

        //Editor変更時
        protected void OnValidate()
        {
            loopParameter.AdjustParameter();
        }

        protected void ResetRoutineList(List<Coroutine> list)
        {
            foreach (Coroutine r in list)
                StopCoroutine(r);
            list.Clear();
        }

        protected void ResetAllLoopState()
        {
            foreach (BlendShapeJitterHelper h in helperList)
                h.ResetLoopState();
        }

        protected void ResetAllOnceState()
        {
            foreach (BlendShapeJitterHelper h in helperList)
                h.ResetOnceState();
        }

        /// <summary>
        /// MecanimModel.morphListからWeight>0のモーフを取得
        /// </summary>
        protected void SetMorph()
        {
            if (skinnedMeshRenderer.sharedMesh.blendShapeCount == 0) return;

            ResetMorph();

            //Weight > 0fのBlendShapeを取得
            for (int index = 0; index < skinnedMeshRenderer.sharedMesh.blendShapeCount; index++)
            {
                float weight = skinnedMeshRenderer.GetBlendShapeWeight(index);
                if (weight > 0f)
                {
                    string name = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(index);
                    helperList.Add(new BlendShapeJitterHelper(this, index, name, weight));
                }
            }

            if (helperList.Count() == 0)
                Debug.Log("BlendShape (Weight > 0) not found.");
        }

        /// <summary>
        /// helperListのリセット
        /// </summary>
        protected void ResetMorph()
        {
            foreach (BlendShapeJitterHelper h in helperList)
            {
                h.loopState.isProcessing = false;
                h.onceState.isProcessing = false;
                h.weight = 0f;
                h.name = "";
                h.UpdateMorph();
            }
            helperList.Clear();
            helperList.TrimExcess();
        }

        /// <summary>
        /// ループ再生用コルーチン
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected IEnumerator LoopCoroutine(BlendShapeJitterHelper.State state)
        {
            while (true)
            {
                state.isProcessing = true;
                
                state.SetNextParameter();

                state.timer = 0f;

                //loopGroupEnabled = falseの間は再生停止
                if (!loopGroupEnabled)
                {
                    state.isProcessing = false;
                    while (!loopGroupEnabled)
                    {
                        yield return null;
                    }
                    state.isProcessing = true;
                }

                //Period
                while (state.timer < 1f)
                {
                    state.timer += Time.deltaTime / state.GetCurrentPeriod();
                    yield return null;
                }

                //Interval
                float intervalTimer = 0f;
                while (intervalTimer < state.curInterval)
                {
                    intervalTimer += Time.deltaTime;
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 1周再生用コルーチン
        /// </summary>
        protected IEnumerator OnceCoroutine(BlendShapeJitterHelper.State state, System.Action callback)
        {
            if (!onceGroupEnabled) yield break;

            state.isProcessing = true;
            
            state.SetOnceParameter();

            state.timer = 0f;

            //Period
            while (state.timer < 1f)
            {
                state.timer += Time.deltaTime / state.GetCurrentPeriod();
                yield return null;
            }

            //Interval
            float intervalTimer = 0f;
            while (intervalTimer < state.curInterval)
            {
                intervalTimer += Time.deltaTime;
                yield return null;
            }

            state.timer = 0f;
            yield return null;
            state.isProcessing = false;

            if (!isProcessing) callback();
        }

        protected IEnumerator FadeInCoroutine(float sec)
        {
            sec = Mathf.Max(0.01f, sec);

            while (loopParameter.magnification < 1f)
            {
                loopParameter.magnification += Time.deltaTime / sec;
                yield return null;
            }
            loopParameter.magnification = 1f;
            fadeInRoutine = null;
        }

        protected IEnumerator FadeOutCoroutine(float sec, System.Action callback)
        {
            sec = Mathf.Max(0.01f, sec);

            while (loopParameter.magnification > 0f)
            {
                loopParameter.magnification -= Time.deltaTime / sec;
                yield return null;
            }
            loopParameter.magnification = 0f;
            fadeOutRoutine = null;
            loopGroupEnabled = false;
            callback();
        }
        
        public override void FadeIn(float second) { }
        public override void FadeOut(float second) { }
        public override void Initialize() { }
        public override void PlayLoop() { }
        public override void PlayLoop(float magnification) { }
        public override void PlayOnce() { }
        public override void PlayOnce(float magnification) { }
        public override void StopLoop() { }
        
#if UNITY_EDITOR
        [CustomEditor(typeof(BlendShapeJitterImpl))]
        public class BlendShapeJitterImplEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                EditorGUILayout.LabelField("Please attach \"BlendShapeJitter\" insted of this.");
            }
        }
#endif
    }
}