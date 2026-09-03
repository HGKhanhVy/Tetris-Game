using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // TUTORIAL TƯƠNG TÁC (thay cho hướng dẫn tĩnh cũ). Người chơi vào MỘT màn chơi thật có
    // đủ tính năng offline; đến từng mốc thì game ĐÓNG BĂNG (tutorialHold) và hiện một thẻ
    // hướng dẫn phẳng (không gỗ). Bấm "Tiếp" để chơi tiếp tới mốc sau.
    //
    // Các mốc điều khiển bằng ĐIỀU KIỆN quan sát được (poll trong Update) nên ít phụ thuộc,
    // chỉ cần 2 cờ nhỏ set ở nơi có sự kiện: ăn cụm (tutorialAnyClear) và đi bàn cờ
    // (tutorialPlayerMoved).
    public partial class BrickGameController : MonoBehaviour
    {
        bool tutorialActive;
        bool tutorialHold;      // true = đang hiện thẻ hướng dẫn -> đóng băng gameplay
        int tutorialStep;
        bool tutorialAnyClear;      // đã phá được cụm đầu tiên
        bool tutorialPlayerMoved;   // đã đi quân trên bàn cờ lần đầu

        GameObject tutorialCanvas;
        Image tutorialAccentBar;
        Text tutorialStepText;
        Text tutorialTitleText;
        Text tutorialBodyText;
        Text tutorialButtonText;

        static readonly string[] TutorialTitles =
        {
            "ĐIỀU KHIỂN KHỐI",
            "ĂN CỤM TÀI NGUYÊN",
            "LƯỢT ĐI BÀN CỜ",
            "Ô ĐẶC BIỆT",
            "DỤ QUÁI BẮT ĐỊCH",
        };

        static readonly string[] TutorialBodies =
        {
            "Kéo TRÁI / PHẢI để di chuyển khối.\nCHẠM để xoay.\nVUỐT XUỐNG để thả nhanh.",
            "Xếp các ô CÙNG MÀU dính liền nhau (từ 3 ô)\nđể phá cụm và nhận TÀI NGUYÊN.",
            "Ăn cụm GIÀY để có LƯỢT ĐI.\nChọn quân XANH rồi chạm ô sáng để đi 1 ô\ntrên bàn cờ phía trên.",
            "Bàn cờ có TƯỜNG, Ô TRỐNG, CỔNG... cản đường.\nQuan sát địa hình để đi và dụ quái đúng hướng.",
            "Bạn đi 1 ô → ĐỊCH (đỏ) chạy, QUÁI (tím) đuổi.\nDụ để QUÁI bắt được ĐỊCH → THẮNG.\nĐừng để quái bắt BẠN!",
        };

        static readonly Color[] TutorialAccents =
        {
            new Color(0.38f, 0.72f, 1.00f),
            new Color(0.42f, 0.94f, 0.58f),
            new Color(1.00f, 0.84f, 0.30f),
            new Color(1.00f, 0.58f, 0.32f),
            new Color(0.82f, 0.56f, 1.00f),
        };

        int TutorialStepCount => TutorialTitles.Length;

        // Gọi ở Start() khi vào chế độ hướng dẫn.
        void TutorialBegin()
        {
            tutorialActive = true;
            tutorialStep = 0;
            tutorialAnyClear = false;
            tutorialPlayerMoved = false;
            EnsureTutorialUi();
        }

        // Poll mỗi frame (trước cổng đóng băng): tới mốc hiện tại thì hiện thẻ.
        void TutorialTick()
        {
            if (!tutorialActive || tutorialHold || tutorialStep >= TutorialStepCount)
                return;
            if (TutorialStepReady(tutorialStep))
                ShowTutorialStep(tutorialStep);
        }

        bool TutorialStepReady(int step)
        {
            switch (step)
            {
                case 0: return true;                 // điều khiển: hiện ngay đầu
                case 1: return tutorialAnyClear;     // sau khi ăn cụm đầu tiên
                case 2: return tacticalBoard != null && tacticalBoard.MoveBank >= 1; // có lượt đi
                case 3: return tutorialPlayerMoved;  // sau khi đi bàn cờ lần đầu
                case 4: return true;                 // nối tiếp ngay sau bước 3
                default: return false;
            }
        }

        void ShowTutorialStep(int step)
        {
            EnsureTutorialUi();
            if (tutorialCanvas == null)
                return;

            Color accent = TutorialAccents[step];
            if (tutorialStepText != null) tutorialStepText.text = (step + 1) + " / " + TutorialStepCount;
            if (tutorialTitleText != null) { tutorialTitleText.text = TutorialTitles[step]; tutorialTitleText.color = accent; }
            if (tutorialBodyText != null) tutorialBodyText.text = TutorialBodies[step];
            if (tutorialAccentBar != null) tutorialAccentBar.color = accent;
            if (tutorialButtonText != null) tutorialButtonText.text = step >= TutorialStepCount - 1 ? "BẮT ĐẦU!" : "TIẾP →";

            tutorialHold = true;
            tutorialCanvas.SetActive(true);
        }

        void ContinueTutorial()
        {
            RuntimeArt.PlayUiSwitchSound();
            Haptics.Selection();
            feedbacks.Play(GameFeedbackId.TutorialStep);
            tutorialHold = false;
            if (tutorialCanvas != null)
                tutorialCanvas.SetActive(false);
            tutorialStep++;
            if (tutorialStep >= TutorialStepCount)
                tutorialActive = false; // hết hướng dẫn: chơi tiếp bình thường
        }

        void EnsureTutorialUi()
        {
            if (tutorialCanvas != null)
                return;

            var cgo = new GameObject("Tutorial Canvas");
            var cv = cgo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 26000; // trên HUD gameplay + bàn cờ isolated
            cgo.AddComponent<GraphicRaycaster>();
            tutorialCanvas = cgo;

            // Nền tối chặn chạm bàn khi đang đọc.
            var dim = Ui.Panel(cgo.transform, "Tut Dim", new Color(0.02f, 0.05f, 0.12f, 0.5f));
            Ui.Stretch(dim);

            // Vùng safe area cho thẻ.
            var safe = Ui.Panel(cgo.transform, "Tut Safe", new Color(0, 0, 0, 0));
            Ui.Stretch(safe);
            safe.GetComponent<Image>().raycastTarget = false;
            safe.AddComponent<SafeAreaFitter>();

            // Thẻ hướng dẫn nằm dải dưới màn (để vẫn thấy bàn chơi phía trên).
            var card = Ui.Panel(safe.transform, "Tut Card", new Color(0.09f, 0.14f, 0.28f, 0.98f));
            var cardImg = card.GetComponent<Image>();
            cardImg.sprite = PillSprite();
            cardImg.type = Image.Type.Sliced;
            Ui.Rect(card, new Vector2(0.10f, 0.055f), new Vector2(0.90f, 0.40f), Vector2.zero);

            // Vạch nhấn (accent) chạy dọc mép trái thẻ.
            var accentGo = Ui.Panel(card.transform, "Accent", Color.white);
            tutorialAccentBar = accentGo.GetComponent<Image>();
            tutorialAccentBar.raycastTarget = false;
            Ui.Rect(accentGo, new Vector2(0.018f, 0.14f), new Vector2(0.032f, 0.86f), Vector2.zero);

            // Số bước (góc trên-phải).
            tutorialStepText = Ui.Text(card.transform, "", RuntimeArt.LoadMenuButtonFont(), 26, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleRight);
            tutorialStepText.raycastTarget = false;
            Ui.Rect(tutorialStepText, new Vector2(0.70f, 0.78f), new Vector2(0.95f, 0.95f), Vector2.zero);

            // Tiêu đề.
            tutorialTitleText = Ui.Text(card.transform, "", RuntimeArt.LoadMenuButtonFont(), 44, Color.white, TextAnchor.MiddleLeft);
            tutorialTitleText.fontStyle = FontStyle.Bold;
            tutorialTitleText.raycastTarget = false;
            tutorialTitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Rect(tutorialTitleText, new Vector2(0.07f, 0.70f), new Vector2(0.70f, 0.95f), Vector2.zero);

            // Nội dung.
            tutorialBodyText = Ui.Text(card.transform, "", RuntimeArt.LoadMenuButtonFont(), 30, new Color(1f, 0.97f, 0.90f), TextAnchor.UpperLeft);
            tutorialBodyText.raycastTarget = false;
            tutorialBodyText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tutorialBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            Ui.Rect(tutorialBodyText, new Vector2(0.07f, 0.24f), new Vector2(0.96f, 0.66f), Vector2.zero);

            // Nút "Tiếp".
            var nextBtn = Ui.Button(card.transform, "", RuntimeArt.LoadMenuButtonFont(), 1, ContinueTutorial);
            var nbImg = nextBtn.GetComponent<Image>();
            nbImg.sprite = PillSprite();
            nbImg.type = Image.Type.Sliced;
            nbImg.color = new Color(0.20f, 0.55f, 0.95f);
            Ui.Rect(nextBtn.gameObject, new Vector2(0.68f, 0.06f), new Vector2(0.95f, 0.21f), Vector2.zero);
            tutorialButtonText = Ui.Text(nextBtn.transform, "TIẾP →", RuntimeArt.LoadMenuButtonFont(), 28, Color.white, TextAnchor.MiddleCenter);
            tutorialButtonText.fontStyle = FontStyle.Bold;
            tutorialButtonText.raycastTarget = false;
            Ui.Stretch(tutorialButtonText.gameObject);

            cgo.SetActive(false);
        }
    }
}
