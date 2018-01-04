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
        public bool playOnAwake = true;
        public bool sync;
        public bool overrideOnce;
        
        public List<BlendShapeJitterHelper> helperList = new List<BlendShapeJitterHelper>();
        public List<BlendShapeJitterDamper> damperList = new List<BlendShapeJitterDamper>();
        
        public BlendShapeJitterParameter loopParameter = new BlendShapeJitterParameter(PrimitiveAnimationCurve.UpDown5, true);
        public BlendShapeJitterParameter onceParameter = new BlendShapeJitterParameter(PrimitiveAnimationCurve.UpDown1, false);
        
        protected PlayState playState = PlayState.Stop;
        protected float fadeSpeed = 1f;

        //Editor用
        public bool loopGroupEnabled = true;
        public bool onceGroupEnabled = true;

        //Once再生中か否か
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
            if (skinnedMeshRenderer.sharedMesh.blendShapeCount == 0)
            {
                Debug.Log("BlendShapes not found.");
                return;
            }
        }

        protected void OnValidate()
        {
            loopParameter.AdjustParameter();
            SetMainMorphName();
            SetDampMorphName();
        }

        void Awake()
        {
            foreach (BlendShapeJitterHelper h in helperList)
                h.Initialize(this);

            foreach (BlendShapeJitterDamper d in damperList)
                d.Initialize(this);

            if (playOnAwake)
                _PlayLoop(PlayState.Play, 1f);
        }

        void Update()
        {
            UpdateLoop();
            UpdateOnce();
            UpdateFade();
            SetMorphWeight();
        }

        protected void UpdateLoop()
        {
            if (!loopGroupEnabled || playState == PlayState.Stop) return;

            foreach (BlendShapeJitterHelper h in helperList)
            {
                h.loopState.UpdateLoop();
            }
        }

        protected void UpdateOnce()
        {
            if (!onceGroupEnabled) return;

            foreach (BlendShapeJitterHelper h in helperList)
            {
                if (h.onceState.isProcessing)
                    h.onceState.UpdateOnce(StopOnce);
            }
        }

        protected void UpdateFade()
        {
            switch (playState)
            {
                case PlayState.Fadein:
                    loopParameter.magnification += Time.deltaTime * fadeSpeed;
                    if (loopParameter.magnification > 1f)
                    {
                        loopParameter.magnification = 1f;
                        playState = PlayState.Play;
                    }
                    break;
                case PlayState.Fadeout:
                    loopParameter.magnification -= Time.deltaTime * fadeSpeed;
                    if (loopParameter.magnification < 0f)
                    {
                        loopParameter.magnification = 0f;
                        playState = PlayState.Stop;
                        StopLoop();
                    }
                    break;
            }
        }

        protected void SetMorphWeight()
        {
            float? weight = null;
            foreach (BlendShapeJitterHelper h in helperList)
            {
                if (!weight.HasValue)
                {
                    //helperList[0]のweightを排他リストのweightに応じて減らす
                    weight = h.GetMorphWeight();
                    float tmp = weight.Value;
                    foreach (BlendShapeJitterDamper d in damperList)
                        tmp *= 1f - d.GetCurrentWeight();
                    h.SetMorphWeight(tmp);
                }
                else
                {
                    if (sync)
                        //helperList[0]のweight計算結果を全モーフで共有
                        h.SetMorphWeight(weight.Value);
                    else
                        h.SetMorphWeight();
                }
            }
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
        protected void GetMainMorph()
        {
            if (skinnedMeshRenderer.sharedMesh.blendShapeCount == 0) return;

            helperList.Clear();
            helperList.TrimExcess();

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

        protected void GetDampMorph()
        {
            if (skinnedMeshRenderer.sharedMesh.blendShapeCount == 0) return;

            damperList.Clear();
            damperList.TrimExcess();

            //Weight > 0fのBlendShapeを取得
            for (int index = 0; index < skinnedMeshRenderer.sharedMesh.blendShapeCount; index++)
            {
                float weight = skinnedMeshRenderer.GetBlendShapeWeight(index);
                if (weight > 0f)
                {
                    string name = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(index);
                    damperList.Add(new BlendShapeJitterDamper(this, index, name, 0.5f));
                }
            }

            if (damperList.Count() == 0)
                Debug.Log("BlendShape (Weight > 0) not found.");
        }

        protected void SetMainMorphName()
        {
            foreach (BlendShapeJitterHelper h in helperList)
                h.SetMorphName();
        }

        protected void SetDampMorphName()
        {
            foreach (BlendShapeJitterDamper d in damperList)
                d.SetMorphName();
        }

        protected void _PlayLoop(PlayState state, float magnification)
        {
            if (helperList.Count == 0) return;
            if (!loopGroupEnabled) return;

            StopLoop();

            playState = state;
            loopParameter.magnification = magnification;
        }

        /// <summary>
        /// ループ再生停止
        /// </summary>
        public override void StopLoop()
        {
            ResetAllLoopState();
            SetMorphWeight();
        }

        protected void _PlayOnce(float magnification)
        {
            if (!onceGroupEnabled) return;

            //動作中で上書き不可ならば return
            if (OnceIsProcessing && !overrideOnce) return;

            StopOnce();

            //振幅倍率
            onceParameter.magnification = magnification;

            foreach (BlendShapeJitterHelper h in helperList)
            {
                h.onceState.SetOnceParameter();
                h.onceState.isProcessing = true;
            }
        }

        /// <summary>
        /// 1周再生停止
        /// </summary>
        protected void StopOnce()
        {
            ResetAllOnceState();
            SetMorphWeight();
        }

        /// <summary>
        /// 全再生停止 & 初期化
        /// </summary>
        public override void Initialize()
        {
            ResetAllLoopState();
            ResetAllOnceState();
            SetMorphWeight();
        }

        public override void FadeIn(float second) { }
        public override void FadeOut(float second) { }
        public override void PlayLoop() { }
        public override void PlayLoop(float magnification) { }
        public override void PlayOnce() { }
        public override void PlayOnce(float magnification) { }

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