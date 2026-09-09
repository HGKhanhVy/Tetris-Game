using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // Bàn tay mô phỏng thao tác cho tutorial: nhấp liên tục tại một điểm (Tap) hoặc vuốt đi vuốt
    // lại theo một đoạn (Swipe). Toạ độ nhận vào là PIXEL MÀN HÌNH — canvas tutorial không dùng
    // CanvasScaler nên 1 đơn vị canvas = 1 pixel, khỏi quy đổi qua lại.
    //
    // Ảnh gốc chỉ sang TRÁI và pivot đặt ngay đầu ngón trỏ: gán vị trí là đầu ngón nằm đúng chỗ
    // cần chạm, không phải giữa bàn tay. Hướng ngón KHÔNG cố định — mỗi lần hiện, tay tự lật /
    // xoay cho ngón trỏ trùng hướng thao tác (vuốt xuống thì chỉ xuống, chạm nửa phải màn hình
    // thì tay vươn từ trái sang, ...) để không che mất thứ đang chỉ tới.
    public class TutorialHandCue : MonoBehaviour
    {
        public enum Mode { Hidden, Tap, Swipe }

        static readonly Vector2 FingerTipPivot = new Vector2(0.05f, 0.58f);
        const float TapPeriod = 1.1f;
        const float SwipePeriod = 1.5f;
        const float ContactTime = 0.15f;   // thời điểm trong nhịp mà đầu ngón chạm mặt kính

        Image hand;
        Image ripple;
        RectTransform handRect;
        RectTransform rippleRect;

        Mode mode = Mode.Hidden;
        Vector2 pointA;
        Vector2 pointB;
        Vector2 aim = Vector2.left;
        float timer;
        float handSize;

        public void Build(Transform parent, float screenHeight)
        {
            handSize = Mathf.Clamp(screenHeight * 0.16f, 90f, 220f);

            // Vòng loang ở điểm chạm (vẽ trước để nằm dưới bàn tay).
            var rippleGo = new GameObject("Hand Ripple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rippleGo.transform.SetParent(parent, false);
            ripple = rippleGo.GetComponent<Image>();
            ripple.sprite = TutorialSprites.Ring();
            ripple.raycastTarget = false;
            rippleRect = ripple.rectTransform;
            rippleRect.sizeDelta = new Vector2(handSize * 0.78f, handSize * 0.78f);
            rippleRect.pivot = new Vector2(0.5f, 0.5f);
            rippleRect.anchorMin = rippleRect.anchorMax = Vector2.zero;

            var handGo = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handGo.transform.SetParent(parent, false);
            hand = handGo.GetComponent<Image>();
            hand.sprite = Resources.Load<Sprite>("BrickStacker/tutorial-hand");
            hand.preserveAspect = true;
            hand.raycastTarget = false;
            handRect = hand.rectTransform;
            handRect.sizeDelta = new Vector2(handSize, handSize);
            handRect.pivot = FingerTipPivot;
            handRect.anchorMin = handRect.anchorMax = Vector2.zero;

            Hide();
        }

        // Nhấn tại một điểm; hướng tay tự chọn theo vị trí điểm đó trên màn hình.
        public void ShowTap(Vector2 screenPoint)
        {
            ShowTap(screenPoint, AutoTapAim(screenPoint));
        }

        public void ShowTap(Vector2 screenPoint, Vector2 aimDirection)
        {
            mode = Mode.Tap;
            pointA = pointB = screenPoint;
            aim = Normalized(aimDirection);
            timer = 0f;
            SetVisible(true);
        }

        // Vuốt từ A tới B; ngón trỏ chỉ đúng chiều vuốt.
        public void ShowSwipe(Vector2 from, Vector2 to)
        {
            mode = Mode.Swipe;
            pointA = from;
            pointB = to;
            aim = SwipeAim(to - from);
            timer = 0f;
            SetVisible(true);
        }

        public void Hide()
        {
            mode = Mode.Hidden;
            SetVisible(false);
        }

        void SetVisible(bool visible)
        {
            if (hand != null)
            {
                hand.enabled = visible;
                hand.color = Color.white;
            }
            if (ripple != null) ripple.enabled = visible && mode == Mode.Tap;
        }

        // Tay vươn từ mép màn hình gần nhất theo chiều ngang và chúc nhẹ xuống cho tự nhiên:
        // chạm ở nửa phải thì ngón chỉ sang phải (thân tay nằm bên trái, không che chỗ cần nhìn).
        static Vector2 AutoTapAim(Vector2 point)
        {
            float dirX = point.x > Screen.width * 0.5f ? 1f : -1f;
            return new Vector2(dirX, -0.35f).normalized;
        }

        // Vuốt gần như nằm ngang thì chúc ngón xuống một chút, giống tay thật đang miết màn hình.
        static Vector2 SwipeAim(Vector2 delta)
        {
            Vector2 dir = Normalized(delta);
            if (Mathf.Abs(dir.y) < Mathf.Abs(dir.x) * 0.3f)
                dir = (dir + Vector2.down * 0.3f).normalized;
            return dir;
        }

        static Vector2 Normalized(Vector2 v)
        {
            return v.sqrMagnitude < 0.0001f ? Vector2.left : v.normalized;
        }

        // Lật ngang rồi mới xoay: xoay thẳng 180 độ sẽ thành mu bàn tay lộn ngược, còn lật ngang
        // vẫn là lòng bàn tay. Unity nhân TRS nên scale áp trước rotation -> góc lấy theo hướng
        // gốc SAU khi lật.
        void ApplyAim(float press)
        {
            bool mirrored = aim.x > 0.02f;
            Vector2 baseAim = mirrored ? Vector2.right : Vector2.left;
            handRect.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(baseAim, aim));
            handRect.localScale = new Vector3(mirrored ? -press : press, press, 1f);
        }

        void Update()
        {
            if (mode == Mode.Hidden || handRect == null)
                return;

            // Time.unscaledDeltaTime: tutorial thường dừng game (timeScale = 0) nhưng tay vẫn phải chạy.
            timer += Time.unscaledDeltaTime;

            if (mode == Mode.Tap)
            {
                float t = Mathf.Repeat(timer, TapPeriod) / TapPeriod;
                // Nhịp nhấn: thu nhỏ nhanh rồi bung về, nghỉ một quãng trước khi nhấn tiếp.
                float press = t < 0.18f ? Mathf.Lerp(1f, 0.82f, t / 0.18f)
                    : t < 0.36f ? Mathf.Lerp(0.82f, 1f, (t - 0.18f) / 0.18f)
                    : 1f;
                handRect.anchoredPosition = pointA;
                hand.color = Color.white;
                ApplyAim(press);

                // Vòng loang chỉ bung RA SAU khi đầu ngón đã chạm, để mắt đọc được nhân quả.
                float ringT = Mathf.Clamp01((t - ContactTime) / 0.5f);
                rippleRect.anchoredPosition = pointA;
                rippleRect.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.55f, ringT);
                float ringAlpha = t < ContactTime ? 0f : Mathf.Lerp(0.85f, 0f, ringT);
                ripple.color = new Color(1f, 0.92f, 0.45f, ringAlpha);
            }
            else
            {
                float t = Mathf.Repeat(timer, SwipePeriod) / SwipePeriod;
                // 0.15 chờ ở đầu -> 0.65 vuốt -> phần còn lại mờ dần rồi quay về đầu.
                float travel = Mathf.Clamp01((t - 0.15f) / 0.5f);
                travel = travel * travel * (3f - 2f * travel);   // smoothstep cho mượt
                handRect.anchoredPosition = Vector2.Lerp(pointA, pointB, travel);
                ApplyAim(1f);
                hand.color = new Color(1f, 1f, 1f, t > 0.72f ? Mathf.Lerp(1f, 0f, (t - 0.72f) / 0.28f) : 1f);
            }
        }
    }
}
