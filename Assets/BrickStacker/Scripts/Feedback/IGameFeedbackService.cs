using UnityEngine;

namespace BrickStacker
{
    // Gameplay chi phu thuoc abstraction nay, khong biet Feel/MMF_Player ton tai.
    public interface IGameFeedbackService
    {
        void Play(GameFeedbackId id);

        void Play(GameFeedbackId id, Vector3 position, float intensity = 1f);
    }
}
