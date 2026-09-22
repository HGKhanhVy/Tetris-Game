using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    public partial class MenuController : MonoBehaviour
    {
        // 1 VS 1 is held back for the first store release. Set to false to reopen the mode;
        // everything behind the button is left intact.
        static readonly bool IsOnlineModeLocked = true;

        const string ComingSoonMessage = "Chờ tui thời gian tới nha bà!!!";

        // Same v3 look as the name popup: blue frame, title banner, hero peeking from the corner.
        void ShowComingSoonPopup(Transform parent)
        {
            if (mapOverlay != null)
            {
                Destroy(mapOverlay);
            }
            var rootCanvas = parent.GetComponentInParent<Canvas>();
            Transform overlayParent = rootCanvas != null ? rootCanvas.rootCanvas.transform : parent;
            mapOverlay = Ui.Panel(overlayParent, "Coming Soon Overlay", new Color(0.015f, 0.045f, 0.13f, 0.80f));
            Ui.Stretch(mapOverlay);
            var ovCanvas = mapOverlay.AddComponent<Canvas>();
            ovCanvas.overrideSorting = true;
            ovCanvas.sortingOrder = 5000;
            mapOverlay.AddComponent<GraphicRaycaster>();

            const float boxW = 520f;
            float boxH = boxW * 768f / 851f;
            var box = MakeV3Image(mapOverlay.transform, "Coming Soon Box", PAUSEUI + "frame.png", new Rect(0.223f, 0.139f, 0.554f, 0.750f), true);
            box.preserveAspect = false;
            var boxRt = box.rectTransform;
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(boxW, boxH);
            Transform boxT = box.transform;

            var banner = MakeV3Image(boxT, "Coming Soon Banner", STARTUI + "frame-title-man.png", new Rect(0.130f, 0.398f, 0.740f, 0.270f), false);
            banner.preserveAspect = false;
            var bnRt = banner.rectTransform;
            bnRt.anchorMin = bnRt.anchorMax = new Vector2(0.5f, 1f);
            bnRt.pivot = new Vector2(0.5f, 0.5f);
            bnRt.sizeDelta = new Vector2(boxW * 0.62f, boxW * 0.62f / 4.12f);
            bnRt.anchoredPosition = new Vector2(0f, 4f);

            var titleLabel = Ui.Text(banner.transform, "1 VS 1", RuntimeArt.LoadMenuButtonFont(), 34, new Color(1f, 0.98f, 0.90f), TextAnchor.MiddleCenter);
            titleLabel.fontStyle = FontStyle.Bold;
            titleLabel.raycastTarget = false;
            Ui.Rect(titleLabel, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(boxW * 0.46f, 52f));
            AddV3TextEdge(titleLabel, 2.0f);

            var hero = MakeV3Image(boxT, "Coming Soon Hero", STARTUI + "icon-player.png", new Rect(0.357f, 0.316f, 0.286f, 0.422f), false);
            var heRt = hero.rectTransform;
            heRt.anchorMin = heRt.anchorMax = new Vector2(0.055f, 0.90f);
            heRt.pivot = new Vector2(0.5f, 0.5f);
            heRt.sizeDelta = new Vector2(108f, 108f);

            var message = Ui.Text(boxT, ComingSoonMessage, font, 36, new Color(0.86f, 0.94f, 1f), TextAnchor.MiddleCenter);
            message.fontStyle = FontStyle.Bold;
            message.raycastTarget = false;
            message.lineSpacing = 1.15f;
            Ui.Rect(message, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), new Vector2(boxW * 0.84f, 160f));
            AddV3TextEdge(message, 1.4f);

            var okBtn = BuildV1V1Button(boxT, STARTUI + "btn-batdau.png", new Rect(0.242f, 0.414f, 0.516f, 0.219f),
                new Vector2(0.5f, 0.185f), new Vector2(boxW * 0.50f, boxW * 0.50f / 3.536f));
            okBtn.GetComponent<Image>().preserveAspect = false;
            var okLabel = Ui.Text(okBtn.transform, "OK", RuntimeArt.LoadMenuButtonFont(), 34, new Color(1f, 0.99f, 0.94f), TextAnchor.MiddleCenter);
            okLabel.fontStyle = FontStyle.Bold;
            okLabel.raycastTarget = false;
            var okRt = okLabel.rectTransform;
            okRt.anchorMin = new Vector2(0.10f, 0f);
            okRt.anchorMax = new Vector2(0.90f, 1f);
            okRt.offsetMin = new Vector2(0f, 4f);
            okRt.offsetMax = new Vector2(0f, 4f);
            AddV3TextEdge(okLabel, 1.8f);
            okBtn.onClick.AddListener(CloseComingSoonPopup);

            var closeBtn = BuildV1V1Button(boxT, V1V1 + "btn-close.png", new Rect(0.061f, 0.069f, 0.876f, 0.888f),
                new Vector2(0.945f, 0.925f), new Vector2(64f, 64f));
            closeBtn.onClick.AddListener(CloseComingSoonPopup);
        }

        void CloseComingSoonPopup()
        {
            RuntimeArt.PlayUiSwitchSound();
            if (mapOverlay != null)
            {
                Destroy(mapOverlay);
            }
        }
    }
}
