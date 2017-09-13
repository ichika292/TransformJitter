using UnityEngine;

namespace MYB.TransformJitter
{
    public class EyeJitter : MonoBehaviour
    {
        public Transform leftEye, rightEye;
        public float magnification = 1f;                                        //振幅倍率
        public Vector2 range = new Vector2(3f, 0.5f);                           //片振幅[deg]
        public FloatRange interval = new FloatRange(0.04f, 3f, true, false);    //移動間隔[sec]

        float timer = 0f;
        Quaternion rot, prevRotation;

        void Reset()
        {
            var anim = GetComponentInParent<Animator>();
            if(anim != null && anim.isHuman)
            {
                leftEye = anim.GetBoneTransform(HumanBodyBones.LeftEye);
                rightEye = anim.GetBoneTransform(HumanBodyBones.RightEye);
            }
            else
            {
                Debug.LogWarning("Transform not found. Please set it manually.");
            }

            interval.min = 0.5f;
            interval.max = 1.0f;
        }

        void LateUpdate()
        {
            timer -= Time.deltaTime;

            if (timer < 0f)
            {
                timer = interval.Random();
                var vec = Vector3.zero;
                vec.x = Random.Range(-range.y, range.y);
                vec.y = Random.Range(-range.x, range.x);

                rot = Quaternion.Euler(vec * magnification);
                
                if(prevRotation == leftEye.rotation)
                {
                    var maxDegreesDelta = Mathf.Max(range.x, range.y) * 2;
                    rot = Quaternion.RotateTowards(leftEye.rotation, rot, maxDegreesDelta);
                    leftEye.rotation = rot;
                    rightEye.rotation = rot;
                    prevRotation = rot;
                }
            }

            //AnimationやIKにより目が操作されているか
            if (prevRotation != leftEye.rotation)
            {
                leftEye.rotation *= rot;
                rightEye.rotation *= rot;
            }
            prevRotation = leftEye.rotation;
        }
    }
}