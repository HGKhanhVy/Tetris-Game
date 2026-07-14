using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // PlayerSettings đã khóa Portrait cho build native, nhưng WebGL trên trình duyệt
    // điện thoại vẫn xoay ngang được — chặn bằng overlay che toàn màn hình yêu cầu
    // xoay dọc. Chỉ kích hoạt trên thiết bị mobile (desktop cửa sổ ngang vẫn chơi bình thường).
    public class PortraitLockOverlay : MonoBehaviour
    {
        static PortraitLockOverlay instance;

        GameObject overlay;
        Text messageText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null)
                return;

            var host = new GameObject("Portrait Lock Overlay");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<PortraitLockOverlay>();
        }

        void Update()
        {
            bool landscape = Application.isMobilePlatform && Screen.width > Screen.height;
            if (landscape && overlay == null)
                BuildOverlay();
            if (overlay != null && overlay.activeSelf != landscape)
                overlay.SetActive(landscape);
        }

        void BuildOverlay()
        {
            var canvasGo = new GameObject("Portrait Lock Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Chặn luôn thao tác chạm xuống game phía dưới khi đang che.
            canvasGo.AddComponent<GraphicRaycaster>();

            overlay = Ui.Panel(canvasGo.transform, "Backdrop", new Color(0.13f, 0.065f, 0.025f, 1f));
            Ui.Stretch(overlay);

            // Không dùng icon ⟳ — font VietnameseArial thiếu glyph này (vẽ ra ô trống).
            var font = RuntimeArt.LoadUiFont();
            messageText = Ui.Text(overlay.transform, "Vui lòng xoay dọc màn hình\nđể tiếp tục chơi", font, 44, new Color(1f, 0.86f, 0.60f), TextAnchor.MiddleCenter);
            var textRect = messageText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(1200f, 160f);
        }
    }
}
