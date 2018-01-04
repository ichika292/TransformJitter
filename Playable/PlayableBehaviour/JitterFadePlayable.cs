using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace MYB.Jitter
{
    public class JitterFadePlayable : PlayableBehaviour
    {
        private Jitter _jitter;
        private float _fadeinTime;
        private float _fadeoutTime;

        public void Initialize(Jitter jitter, float fadeinTime, float fadeoutTime)
        {
            _jitter = jitter;
            _fadeinTime = fadeinTime;
            _fadeoutTime = fadeoutTime;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_jitter != null)
            {
                _jitter.FadeIn(_fadeinTime);
            }
        }
        
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_jitter != null)
            {
                _jitter.FadeOut(_fadeoutTime);
            }
        }
    }
}

