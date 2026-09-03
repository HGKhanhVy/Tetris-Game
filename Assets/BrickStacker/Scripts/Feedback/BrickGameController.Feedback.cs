using UnityEngine;

namespace BrickStacker
{
    // Cau noi giua gameplay va Feel.
    // Prefab dat trong Resources thay vi dat san trong scene: BrickStackerSceneBuilder sinh lai
    // scene bang code, moi thu dat trong scene deu bi xoa moi lan "Brick Stacker/Rebuild Scenes".
    // Load + instantiate dung 1 lan luc Start roi cache, khong lookup trong gameplay loop.
    public partial class BrickGameController
    {
        const string FeedbackPrefabPath = "BrickStacker/GameFeedbacks";

        IGameFeedbackService feedbacks = NullGameFeedbackService.Instance;

        void SetupFeedbacks()
        {
            var prefab = Resources.Load<MMGameFeedbackService>(FeedbackPrefabPath);
            if (prefab == null)
            {
                // Thieu prefab khong lam vo gameplay: Null Object giu moi call site an toan.
                Debug.LogWarning("BLOCKFALL feedback prefab missing: Resources/" + FeedbackPrefabPath);
                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = "Game Feedbacks";
            feedbacks = instance;
        }
    }
}
