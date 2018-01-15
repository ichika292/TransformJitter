using UnityEngine;

namespace MYB.Jitter
{
    /// <summary>
    /// TransformのPositionのX,Y,Zそれぞれを任意の波形で振幅させます。
    /// PlayOnce()を実行することで、Onceの波形を任意のタイミングでLoopの波形に加算出来ます。
    /// </summary>
    public class PositionJitter : PositionJitImpl
    {
        void OnDisable()
        {
            if (target == null) return;

            Initialize();
        }

        /// <summary>
        /// ループ再生開始
        /// </summary>
        public override void PlayLoop()
        {
            _PlayLoop(PlayState.Play, 1f);
        }

        /// <summary>
        /// ループ再生開始　振幅倍率設定あり
        /// </summary>
        public override void PlayLoop(float magnification)
        {
            _PlayLoop(PlayState.Play, magnification);
        }

        /// <summary>
        /// ループ再生フェードイン
        /// </summary>
        /// <param name="second">フェード時間</param>
        public override void FadeIn(float second)
        {
            if (playState == PlayState.Play ||
                playState == PlayState.Fadein) return;

            playState = PlayState.Fadein;
            fadeSpeed = 1f / Mathf.Max(0.01f, second);

            _PlayLoop(PlayState.Fadein, 0f);
        }

        /// <summary>
        /// ループ再生フェードアウト
        /// </summary>
        /// <param name="second">フェード時間</param>
        public override void FadeOut(float second)
        {
            if (playState == PlayState.Stop) return;

            playState = PlayState.Fadeout;
            fadeSpeed = 1f / Mathf.Max(0.01f, second);
        }

        /// <summary>
        /// 1周再生
        /// </summary>
        public override void PlayOnce()
        {
            _PlayOnce(1f);
        }

        /// <summary>
        /// 1周再生　振幅倍率設定あり
        /// </summary>
        public override void PlayOnce(float magnification)
        {
            _PlayOnce(magnification);
        }

        /// <summary>
        /// LOOP再生中、次の周期まで早送り
        /// </summary>
        /// <param name="speed">0で効果なし 1で即次周期へ</param>
        public override void MoveNext(float speed = 0.1f)
        {
            if (playState == PlayState.Stop) return;

            foreach (JitterHelper h in helperList)
                h.MoveNext(speed);
        }
    }
}