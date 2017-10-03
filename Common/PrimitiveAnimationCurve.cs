using UnityEngine;

namespace MYB.Jitter
{
    public static class PrimitiveAnimationCurve
    {
        public static AnimationCurve Zero
        {
            get {
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0, 0),
                    new Keyframe(1f, 0f, 0, 0));
            }
        }

        public static AnimationCurve UpDown5
        {
            get {
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0, 0),
                    new Keyframe(0.5f, 1f, 0, 0),
                    new Keyframe(1f, 0f, 0, 0));
            }
        }

        public static AnimationCurve UpDown1
        {
            get {
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0, 0),
                    new Keyframe(0.1f, 1f, 0, 0),
                    new Keyframe(1f, 0f, 0, 0));
            }
        }

        public static AnimationCurve UpDown25
        {
            get {
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0, 0),
                    new Keyframe(0.2f, 1f, 0, 0),
                    new Keyframe(0.5f, 1f, 0, 0),
                    new Keyframe(1f, 0f, 0, 0));
            }
        }

        public static AnimationCurve Sin
        {
            get {
                float tan = 45f * 2f * Mathf.PI * Mathf.Deg2Rad;
                return new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, tan),
                    new Keyframe(0.25f, 1f, 0f, 0f),
                    new Keyframe(0.75f, -1f, 0f, 0f),
                    new Keyframe(1f, 0f, tan, 0f));
            }
        }

        public static AnimationCurve Cos
        {
            get {
                return new AnimationCurve(
                    new Keyframe(0f, 1f, 0f, 0f),
                    new Keyframe(0.5f, -1f, 0f, 0f),
                    new Keyframe(1f, 1f, 0f, 0f));
            }
        }
    }
}