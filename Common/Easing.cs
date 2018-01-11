using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MYB.Jitter
{
    public enum Easing
    {
        None,
        Linear,
        QuadIn,
        QuadOut,
        QuadInOut,
        CubicIn,
        CubicOut,
        CubicInOut,
        QuartIn,
        QuartOut,
        QuartInOut,
        QuintIn,
        QuintOut,
        QuintInOut,
        ElasticIn,
        ElasticOut,
        ElasticInOut,
        SinIn,
        SinOut,
        SinInOut,
    }

    static class EasingExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ease"></param>
        /// <param name="a">start value</param>
        /// <param name="b">end value</param>
        /// <param name="t">current time 0-1</param>
        /// <returns></returns>
        public static float Evaluate(this Easing ease, float a, float b, float t)
        {
            float f = 0f;
            switch (ease)
            {
                case Easing.None:
                    break;
                case Easing.Linear:
                    f = t;
                    break;
                case Easing.QuadIn:
                    f = t * t;
                    break;
                case Easing.QuadOut:
                    f = t * (2 - t);
                    break;
                case Easing.QuadInOut:
                    f = t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
                    break;
                case Easing.CubicIn:
                    f = t * t * t;
                    break;
                case Easing.CubicOut:
                    f = (--t) * t * t + 1;
                    break;
                case Easing.CubicInOut:
                    f = t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
                    break;
                case Easing.QuartIn:
                    f = t * t * t * t;
                    break;
                case Easing.QuartOut:
                    f = 1 - (--t) * t * t * t;
                    break;
                case Easing.QuartInOut:
                    f = t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;
                    break;
                case Easing.QuintIn:
                    f = t * t * t * t * t;
                    break;
                case Easing.QuintOut:
                    f = 1 + (--t) * t * t * t * t;
                    break;
                case Easing.QuintInOut:
                    f = t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;
                    break;
                case Easing.ElasticIn:
                    f = (t == 1f) ? 1f : 0.04f * t / (--t) * Mathf.Sin(25 * t);
                    break;
                case Easing.ElasticOut:
                    f = (t == 0f) ? 0f : (0.04f - 0.04f / t) * Mathf.Sin(25 * t) + 1;
                    break;
                case Easing.ElasticInOut:
                    if(t == 0.5f)
                    {
                        f = 0.5f;
                    }
                    else
                    {
                        f = (t -= 0.5f) < 0f ?
                            (0.02f + 0.01f / t) * Mathf.Sin(50 * t) :
                            (0.02f - 0.01f / t) * Mathf.Sin(50 * t) + 1;
                    }                   
                    break;
                case Easing.SinIn:
                    f = 1 + Mathf.Sin(Mathf.PI / 2 * t - Mathf.PI / 2);
                    break;
                case Easing.SinOut:
                    f = Mathf.Sin(Mathf.PI / 2 * t);
                    break;
                case Easing.SinInOut:
                    f = (1 + Mathf.Sin(Mathf.PI * t - Mathf.PI / 2)) / 2;
                    break;
                default:
                    break;
            }
            return a + (b - a) * f;
        }
    }
}