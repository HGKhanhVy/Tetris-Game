using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace BrickStacker
{
    // Cap (moc gameplay -> MMF_Player) gan bang Inspector trong prefab GameFeedbacks.
    [Serializable]
    public sealed class GameFeedbackBinding
    {
        [SerializeField] GameFeedbackId id = GameFeedbackId.None;
        [SerializeField] MMF_Player player;

        public GameFeedbackId Id => id;

        public MMF_Player Player => player;
    }
}
