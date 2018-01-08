using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MYB.Jitter
{
    abstract public class Jitter : MonoBehaviour
    {
        public enum PlayState
        {
            Stop,
            Fadein,
            Play,
            Fadeout,
        }

        public abstract void FadeIn(float second);
        public abstract void FadeOut(float second);
        public abstract void Initialize();
        public abstract void PlayLoop();
        public abstract void PlayLoop(float magnification);
        public abstract void PlayOnce();
        public abstract void PlayOnce(float magnification);
        public abstract void StopLoop();
    }
}
