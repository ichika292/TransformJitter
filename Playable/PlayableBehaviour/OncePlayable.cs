using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace MYB.Jitter
{
    public class OncePlayable : PlayableBehaviour
    {
        private Jitter _jitter;
        private float _magnification;

        private bool _alreadyPlayed = false;

        public void Initialize(Jitter jitter, float magnification)
        {
            _jitter = jitter;
            _magnification = magnification;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!_alreadyPlayed && _jitter != null)
            {
                _jitter.PlayOnce(_magnification);
                _alreadyPlayed = true;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _alreadyPlayed = false;
        }
    }
}

