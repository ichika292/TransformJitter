using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    public class PositionJitImpl : Jitter
    {
        public Transform target;
        public UpdateMode updateMode = UpdateMode.Reference;
        public Vector3 referencePosition;
        public bool playOnAwake = true;
        public bool syncAxis = false;
        public bool overrideOnce;
        public float magnification = 1f;
        public List<PositionJitter> children = new List<PositionJitter>();
        public List<JitterHelper> helperList = new List<JitterHelper>();

        public JitterParameter[] loopParameter =
        {
            new JitterParameter(PrimitiveAnimationCurve.Cos, true, true),
            new JitterParameter(PrimitiveAnimationCurve.Sin, true),
            new JitterParameter(PrimitiveAnimationCurve.Sin, true)
        };

        public JitterParameter[] onceParameter =
        {
            new JitterParameter(PrimitiveAnimationCurve.UpDown25, false, true),
            new JitterParameter(PrimitiveAnimationCurve.UpDown25, false),
            new JitterParameter(PrimitiveAnimationCurve.UpDown25, false)
        };
        
        protected Vector2 amplitudeMagnification = Vector2.one;    //振幅倍率 x:Loop y:Once
        protected PlayState playState = PlayState.Stop;
        protected float fadeSpeed = 1f;

        //Editor用
        public string[] axisLabel = { "--- X ---", "--- Y ---", "--- Z ---" };
        public bool loopGroupEnabled = true;
        public bool onceGroupEnabled = true;
        public bool[] loopEnabled = { true, true, true };
        public bool[] onceEnabled = { true, true, true };

        public bool isChild;

        //コルーチンが動作中か否か
        public bool isProcessing
        {
            get {
                bool result = false;
                foreach (JitterHelper h in helperList)
                {
                    if (h.isProcessing)
                    {
                        result = true;
                        break;
                    }
                }
                foreach(PositionJitter child in children)
                {
                    if (child.OnceIsProcessing)
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
                foreach (JitterHelper h in helperList)
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
            target = transform;
            SearchParent();
        }

        //Editor変更時
        protected void OnValidate()
        {
            for (int i = 0; i < 3; i++)
            {
                loopParameter[i].isEnabled = loopEnabled[i];
                onceParameter[i].isEnabled = onceEnabled[i];
                loopParameter[i].syncAxis = syncAxis;
                onceParameter[i].syncAxis = syncAxis;
            }

            if (syncAxis)
            {
                for (int i = 1; i < 3; i++)
                {
                    loopParameter[i].Copy(loopParameter[0]);
                    onceParameter[i].Copy(onceParameter[0]);
                }
            }

            foreach (JitterParameter param in loopParameter)
                param.AdjustParameter();
        }

        void Awake()
        {
            if (target == null) return;
            
            if (!isChild)
            {
                referencePosition = target.localPosition;

                children = target.GetComponentsInChildren<PositionJitter>()
                    .Where(x => x.target == this.target)
                    .Where(x => x.GetInstanceID() != this.GetInstanceID())
                    .ToList();
            }

            //helperList初期化 (syncAxis ? 1 : 3)個インスタンス化
            for (int i = 0; i < (syncAxis ? 1 : 3); i++)
            {
                helperList.Add(new JitterHelper(loopParameter[i], onceParameter[i]));
            }

            if (!isChild && playOnAwake)
                _PlayLoop(PlayState.Play, 1f);
        }

        void LateUpdate()
        {
            UpdateOnce();
            if (isChild) return;

            UpdateLoop();
            UpdateFade();
            SetPosition();
        }

        protected void UpdateLoop()
        {
            if (!loopGroupEnabled || playState == PlayState.Stop) return;

            foreach (JitterHelper h in helperList)
            {
                h.loopState.UpdateLoop();
            }
        }

        protected void UpdateOnce()
        {
            if (!onceGroupEnabled) return;

            foreach (JitterHelper h in helperList)
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
                    amplitudeMagnification.x += Time.deltaTime * fadeSpeed;
                    if (amplitudeMagnification.x > 1f)
                    {
                        amplitudeMagnification.x = 1f;
                        playState = PlayState.Play;
                    }
                    break;
                case PlayState.Fadeout:
                    amplitudeMagnification.x -= Time.deltaTime * fadeSpeed;
                    if (amplitudeMagnification.x < 0f)
                    {
                        amplitudeMagnification.x = 0f;
                        playState = PlayState.Stop;
                        StopLoop();
                    }
                    break;
            }
        }
        
        //target変更時に親を探す
        protected void SearchParent()
        {
            isChild = false;

            //target階層下から、同targetを操作対象としたPositionJitter(親)が存在するか確認
            int parent = target.GetComponentsInChildren<PositionJitter>()
                                        .Where(x => x.target == this.target)
                                        .Where(x => x.isChild == false)
                                        .Count(x => x.GetInstanceID() != this.GetInstanceID());

            isChild = parent > 0 ? true : false;

            //親が既に在る場合、ループ再生を無効
            loopGroupEnabled = !isChild;
            for (int i = 0; i < 3; i++)
                loopEnabled[i] = !isChild;
        }

        protected void ResetRoutineList(List<Coroutine> list)
        {
            foreach (Coroutine r in list)
                StopCoroutine(r);
            list.Clear();
        }

        protected void ResetAllLoopState()
        {
            foreach (JitterHelper h in helperList)
                h.ResetLoopState();
        }

        protected void ResetAllOnceState()
        {
            foreach (JitterHelper h in helperList)
                h.ResetOnceState();
        }

        //集計 & セット
        protected void SetPosition()
        {
            Vector3 vec = GetCurrentWeight();

            foreach (PositionJitter child in children)
                vec += child.GetCurrentWeight();

            vec *= magnification;

            switch (updateMode)
            {
                case UpdateMode.Override:
                    target.localPosition = vec;
                    break;
                case UpdateMode.Reference:
                    target.localPosition = referencePosition + vec;
                    break;
                case UpdateMode.AfterAnimation:
                    target.localPosition += vec;
                    break;
            }
        }

        protected void ResetPosition()
        {
            if (Application.isPlaying)
                target.localPosition = referencePosition;
        }

        protected Vector3 GetCurrentWeight()
        {
            Vector3 vec = Vector3.zero;

            //syncAxis = true の場合、X軸のパラメータ(helperList[0])から計算
            for (int i = 0; i < 3; i++)
            {
                var loopCurve = loopParameter[i].periodToAmplitude;
                var onceCurve = onceParameter[i].periodToAmplitude;

                Vector2 weight = Vector2.zero;
                if (loopGroupEnabled)
                    weight.x = helperList[syncAxis ? 0 : i].GetLoopAngle(loopCurve);
                if (onceGroupEnabled)
                    weight.y = helperList[syncAxis ? 0 : i].GetOnceAngle(onceCurve);

                Vector2 enabledFlag = new Vector2(loopEnabled[i] ? 1 : 0, onceEnabled[i] ? 1 : 0);

                vec[i] = Vector2.Dot(Vector2.Scale(weight, amplitudeMagnification), enabledFlag);
            }

            return vec;
        }

        protected void _PlayLoop(PlayState state, float magnification)
        {
            if (!loopGroupEnabled) return;

            if (isProcessing)
                StopLoop();

            playState = state;
            //振幅倍率 x:Loop
            this.amplitudeMagnification.x = magnification;
        }

        public override void StopLoop()
        {
            ResetAllLoopState();

            if (!isProcessing) ResetPosition();
        }

        protected void _PlayOnce(float magnification)
        {
            if (!onceGroupEnabled) return;

            //動作中で上書き不可ならば return
            if (OnceIsProcessing && !overrideOnce) return;

            StopOnce();

            //振幅倍率 y:Once
            this.amplitudeMagnification.y = magnification;

            foreach (JitterHelper h in helperList)
            {
                h.onceState.SetOnceParameter();
                h.onceState.isProcessing = true;
            }
        }

        public void StopOnce()
        {
            ResetAllOnceState();
            if (!isProcessing) ResetPosition();
        }

        /// <summary>
        /// 全再生停止 & 初期化
        /// </summary>
        public override void Initialize()
        {
            ResetAllLoopState();
            ResetAllOnceState();
            ResetPosition();
        }

        public override void FadeIn(float second) { }
        public override void FadeOut(float second) { }
        public override void PlayLoop() { }
        public override void PlayLoop(float magnification) { }
        public override void PlayOnce() { }
        public override void PlayOnce(float magnification) { }

#if UNITY_EDITOR
        //Inspector拡張クラス
        [CustomEditor(typeof(RotationJitImpl))]
        public class ScaleJitterImplEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                EditorGUILayout.LabelField("Please attach \"PositionJitter\" insted of this.");
            }
        }
#endif
    }
}