using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MYB.Jitter
{
    abstract public class Jitter : MonoBehaviour
    {
        abstract public void FadeIn(float second);
        abstract public void FadeOut(float second);
        abstract public void Initialize();
        abstract public void PlayLoop();
        abstract public void PlayLoop(float magnification);
        abstract public void PlayOnce();
        abstract public void PlayOnce(float magnification);
        abstract public void StopLoop();
    }
}
