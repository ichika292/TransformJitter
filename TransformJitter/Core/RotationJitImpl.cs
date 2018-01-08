using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MYB.Jitter
{
    public class RotationJitImpl : TJitter
    {
        public List<RotationJitter> children = new List<RotationJitter>();

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
                foreach(RotationJitter child in children)
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
            magnification = 30f;
            SearchParent();
        }

        protected void Awake()
        {
            if (target == null) return;
                        
            if (!isChild)
            {
                reference = target.localRotation.eulerAngles;

                children = target.GetComponentsInChildren<RotationJitter>()
                    .Where(x => x.target == this.target)
                    .Where(x => x.GetInstanceID() != this.GetInstanceID())
                    .ToList();
            }

            //helperList初期化 (syncAxis ? 1 : 3)個インスタンス化
            for (int i = 0; i < (syncAxis ? 1 : 3); i++)
            {
                helperList.Add(new JitterHelper(loopParameter[i], onceParameter[i]));
            }

            if(!isChild && playOnAwake)
                _PlayLoop(PlayState.Play, 1f);
        }

        protected void LateUpdate()
        {
            UpdateOnce();
            if (isChild) return;

            UpdateLoop();
            UpdateFade();
            SetRotation();
        }

        //target変更時に親を探す
        protected void SearchParent()
        {
            isChild = false;
            
            //target階層下から、同targetを操作対象としたRotationJitter(親)が存在するか確認
            int parent = target.GetComponentsInChildren<RotationJitter>()
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
        protected void SetRotation()
        {
            Vector3 vec = GetCurrentWeight();

            foreach (RotationJitter child in children)
                vec += child.GetCurrentWeight();

            var rot = Quaternion.Euler(vec * magnification);

            switch (updateMode)
            {
                case UpdateMode.Override:
                    target.localRotation = rot;
                    break;
                case UpdateMode.Reference:
                    target.localRotation = Quaternion.Euler(reference) * rot;
                    break;
                case UpdateMode.AfterAnimation:
                    target.localRotation *= rot;
                    break;
            }
        }

        protected override void ResetValue()
        {
            if(Application.isPlaying)
                target.localRotation = Quaternion.Euler(reference);
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
                EditorGUILayout.LabelField("Please attach \"RotationJitter\" insted of this.");
            }
        }
#endif
    }
}