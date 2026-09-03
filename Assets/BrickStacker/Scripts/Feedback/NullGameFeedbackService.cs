using UnityEngine;

namespace BrickStacker
{
    // Null Object: dung khi prefab feedback chua duoc tao hoac chua gan.
    // Call site khong can null-check; gameplay chay binh thuong, chi mat phan hieu ung.
    public sealed class NullGameFeedbackService : IGameFeedbackService
    {
        public static readonly IGameFeedbackService Instance = new NullGameFeedbackService();

        NullGameFeedbackService()
        {
        }

        public void Play(GameFeedbackId id)
        {
        }

        public void Play(GameFeedbackId id, Vector3 position, float intensity = 1f)
        {
        }
    }
}
