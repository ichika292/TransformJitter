using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    public class PositionJitImpl : TJitter
    {
        public List<PositionJitter> children = new List<PositionJitter>();

        //コルーチンが動作中か否か
        public override bool isProcessing
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

        protected void Reset()
        {
            target = transform;
            magnification = 1f;
            SearchParent();
        }

        protected void Awake()
        {
            if (target == null) return;
            
            if (!isChild)
            {
                reference = target.localPosition;

                children = target.GetComponentsInChildren<PositionJitter>()
                    .Where(x => x.target == this.target)
                    .Where(x => x.GetInstanceID() != this.GetInstanceID())
                    .ToList();
            }

            //helperList初期化 1or3個インスタンス化
            for (int i = 0; i < (syncPeriod && syncAmplitude ? 1 : 3); i++)
            {
                helperList.Add(new JitterHelper(loopParameter[i], onceParameter[i]));
            }

            if (!isChild && playOnAwake)
                _PlayLoop(PlayState.Play, 1f);
        }

        void LateUpdate()
        {
            if (target == null) return;

            UpdateOnce();
            if (isChild) return;

            UpdateLoop();
            UpdateFade();
            SetPosition();
        }

        //target変更時に親を探す
        public void SearchParent()
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
                    target.localPosition = reference + vec;
                    break;
                case UpdateMode.AfterAnimation:
                    target.localPosition += vec;
                    break;
            }
        }

        protected override void ResetValue()
        {
            if (Application.isPlaying)
                target.localPosition = reference;
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