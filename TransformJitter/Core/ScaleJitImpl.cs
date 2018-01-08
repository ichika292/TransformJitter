using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    public class ScaleJitImpl : TJitter
    {
        public List<ScaleJitter> children = new List<ScaleJitter>();

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
                foreach (ScaleJitter child in children)
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
                reference = target.localScale;

                children = target.GetComponentsInChildren<ScaleJitter>()
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

        protected void LateUpdate()
        {
            UpdateOnce();
            if (isChild) return;

            UpdateLoop();
            UpdateFade();
            SetScale();
        }

        //target変更時に親を探す
        protected void SearchParent()
        {
            isChild = false;
            
            //target階層下から、同targetを操作対象としたScaleJitter(親)が存在するか確認
            int parent = target.GetComponentsInChildren<ScaleJitter>()
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
        protected void SetScale()
        {
            Vector3 vec = GetCurrentWeight();

            foreach (ScaleJitter child in children)
                vec += child.GetCurrentWeight();

            vec *= magnification;

            switch (updateMode)
            {
                case UpdateMode.Override:
                    target.localScale = vec;
                    break;
                case UpdateMode.Reference:
                    target.localScale = reference + vec;
                    break;
                case UpdateMode.AfterAnimation:
                    target.localScale += vec;
                    break;
            }
        }

        protected override void ResetValue()
        {
            if (Application.isPlaying)
                target.localScale = reference;
        }

        public override void FadeIn(float second) { }
        public override void FadeOut(float second) { }
        public override void PlayLoop() { }
        public override void PlayLoop(float magnification) { }
        public override void PlayOnce() { }
        public override void PlayOnce(float magnification) { }

#if UNITY_EDITOR
        //Inspector拡張クラス
        [CustomEditor(typeof(ScaleJitImpl))]
        public class ScaleJitterImplEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                EditorGUILayout.LabelField("Please attach \"ScaleJitter\" insted of this.");
            }
        }
#endif
    }
}