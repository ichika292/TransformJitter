using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace MYB.Jitter
{
    [System.Serializable]
    public class JitterOnceAsset : PlayableAsset
    {
        public ExposedReference<Jitter> jitter;
        public float magnification = 1f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<JitterOncePlayable>.Create(graph);
            var OncePlayable = playable.GetBehaviour();

            var timelineJitter = jitter.Resolve(graph.GetResolver());

            OncePlayable.Initialize(timelineJitter, magnification);
            return playable;
        }
    }
}
