﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MYB.Jitter
{
    public class TJitter : Jitter
    {
        public Transform target;
        public UpdateMode updateMode = UpdateMode.Reference;
        public Vector3 reference;
        public bool playOnAwake = true;
        public bool syncAxis = false;
        public bool overrideOnce;
        public float magnification;
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

        public TransformJitterAsset asset;
        
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
                    loopParameter[i].CopyFrom(loopParameter[0]);
                    onceParameter[i].CopyFrom(onceParameter[0]);
                }
            }

            foreach (JitterParameter param in loopParameter)
                param.AdjustParameter();
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

            if (!isProcessing) ResetValue();
        }

        public void StopOnce()
        {
            ResetAllOnceState();
            if (!isProcessing) ResetValue();
        }

        /// <summary>
        /// 全再生停止 & 初期化
        /// </summary>
        public override void Initialize()
        {
            ResetAllLoopState();
            ResetAllOnceState();
            ResetValue();
        }

        public void Import()
        {
            if (asset == null)
            {
                AssetNotFound();
                return;
            }

            updateMode = asset.updateMode;
            reference = asset.reference;
            playOnAwake = asset.playOnAwake;
            syncAxis = asset.syncAxis;
            overrideOnce = asset.overrideOnce;
            magnification = asset.magnification;
            loopGroupEnabled = asset.loopGroupEnabled;
            onceGroupEnabled = asset.onceGroupEnabled;

            for (int i = 0; i < 3; i++)
            {
                loopParameter[i].CopyFrom(asset.loopParameter[i], true);
                loopParameter[i].AdjustParameter();
                onceParameter[i].CopyFrom(asset.onceParameter[i], true);
                onceParameter[i].AdjustParameter();
                loopEnabled[i] = asset.loopEnabled[i];
                onceEnabled[i] = asset.onceEnabled[i];
            }

            OnValidate();
        }

        public void Export()
        {
            if (asset == null)
            {
                AssetNotFound();
                return;
            }

            asset.updateMode = updateMode;
            asset.reference = reference;
            asset.playOnAwake = playOnAwake;
            asset.syncAxis = syncAxis;
            asset.overrideOnce = overrideOnce;
            asset.magnification = magnification;
            asset.loopGroupEnabled = loopGroupEnabled;
            asset.onceGroupEnabled = onceGroupEnabled;

            for (int i = 0; i < 3; i++)
            {
                asset.loopParameter[i].CopyFrom(loopParameter[i], true);
                asset.onceParameter[i].CopyFrom(onceParameter[i], true);
                asset.loopEnabled[i] = loopEnabled[i];
                asset.onceEnabled[i] = onceEnabled[i];
            }
        }

        void AssetNotFound()
        {
            Debug.LogWarning("Asset not found. Create at Assets/Create/Jitter/TransformJitterAsset");
        }

        public virtual bool isProcessing { get; set; }
        protected virtual void ResetValue() { }

        public override void FadeIn(float second) { }
        public override void FadeOut(float second) { }
        public override void PlayLoop() { }
        public override void PlayLoop(float magnification) { }
        public override void PlayOnce() { }
        public override void PlayOnce(float magnification) { }
    }
}