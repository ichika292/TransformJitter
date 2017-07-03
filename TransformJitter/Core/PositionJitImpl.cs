﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.TransformJitter
{
    public class PositionJitImpl : MonoBehaviour
    {
        public Transform target;
        public Vector3 referencePosition;
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

        protected Animator anim;
        protected Vector2 amplitudeMagnification = Vector2.one;    //振幅倍率 x:Loop y:Once
        protected List<Coroutine> loopRoutineList = new List<Coroutine>();
        protected List<Coroutine> onceRoutineList = new List<Coroutine>();
        protected Coroutine fadeInRoutine, fadeOutRoutine;

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

        void Awake()
        {
            if (target == null) return;

            anim = GetComponentInParent<Animator>();

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
        }

        void LateUpdate()
        {
            if (isChild) return;
            if (!isProcessing) return;

            SetUserRotation();
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

            foreach (JitterParameter param in loopParameter)
                param.AdjustParameter();
        }

        //target変更時に親を探す
        protected void SearchParent()
        {
            isChild = false;

            //target階層下から、同Boneを操作対象としたBoneJitter(親)が存在するか確認
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

        //EulerAngle集計 & セット
        protected void SetUserRotation()
        {
            Vector3 vec = GetCurrentWeight();

            foreach (PositionJitter child in children)
                vec += child.GetCurrentWeight();

            vec *= magnification;

            if (anim != null)
            {
                if (anim.runtimeAnimatorController == null)
                    target.localPosition = referencePosition + vec;
                else
                    target.localPosition += vec;
            }
            else
            {
                target.localPosition = referencePosition + vec;
            }
        }

        protected void ResetPosition()
        {
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

        /// <summary>
        /// ループ再生用コルーチン
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected IEnumerator LoopCoroutine(JitterHelper.State state)
        {
            while (true)
            {
                state.isProcessing = true;

                state.SetNextParameter();

                state.timer = 0f;

                //各軸isEnabled = falseの間は再生停止
                var param = state.param;
                if (!param.isEnabled && !param.isXAxis && param.syncAxis)
                {
                    state.isProcessing = false;
                    while (!param.isEnabled)
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
            }
        }

        /// <summary>
        /// 1周再生用コルーチン
        /// </summary>
        protected IEnumerator OnceCoroutine(JitterHelper.State state)
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
        }

        protected IEnumerator FadeInCoroutine(float sec)
        {
            sec = Mathf.Max(0.01f, sec);

            while (amplitudeMagnification.x < 1f)
            {
                amplitudeMagnification.x += Time.deltaTime / sec;
                yield return null;
            }
            amplitudeMagnification.x = 1f;
            fadeInRoutine = null;
        }

        protected IEnumerator FadeOutCoroutine(float sec, System.Action callback)
        {
            sec = Mathf.Max(0.01f, sec);

            while (amplitudeMagnification.x > 0f)
            {
                amplitudeMagnification.x -= Time.deltaTime / sec;
                yield return null;
            }
            amplitudeMagnification.x = 0f;
            fadeOutRoutine = null;
            loopGroupEnabled = false;
            callback();
        }

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