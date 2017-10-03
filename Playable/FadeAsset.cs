using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace MYB.Jitter
{
    [System.Serializable]
    public class FadeAsset : PlayableAsset
    {
        public ExposedReference<Jitter> jitter;
        public float fadeinTime = 1f;
        public float fadeoutTime = 1f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FadePlayable>.Create(graph);
            var fadePlayable = playable.GetBehaviour();

            var timelineJitter = jitter.Resolve(graph.GetResolver());

            fadePlayable.Initialize(timelineJitter, fadeinTime, fadeoutTime);
            return playable;
        }
    }
}
