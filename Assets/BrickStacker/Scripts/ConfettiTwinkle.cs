using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // Pháo hoa/confetti trong popup CHIẾN THẮNG tự sinh động: lấp lánh (alpha), đung đưa nhẹ + xoay.
    // Tự chạy khi GameObject active (popup hiện), tắt theo popup. Không GC trong Update (§11 mobile).
    public sealed class ConfettiTwinkle : MonoBehaviour
    {
        Image image;
        RectTransform rt;
        Vector2 basePos;
        float baseAlpha;
        float phase;
        float twinkleSpeed;
        float spinSpeed;
        float swayAmp;
        float swaySpeed;

        // speedScale/swayScale < 1 = êm dịu hơn (dùng cho hiệu ứng buồn ở popup THẤT BẠI).
        public void Init(float baseAlpha, System.Random rng, float speedScale = 1f, float swayScale = 1f)
        {
            image = GetComponent<Image>();
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
            this.baseAlpha = baseAlpha;
            phase = (float)rng.NextDouble() * 6.2831853f;
            twinkleSpeed = (2.5f + (float)rng.NextDouble() * 3.5f) * speedScale;
            spinSpeed = ((float)rng.NextDouble() * 2f - 1f) * 45f * speedScale;
            swayAmp = (6f + (float)rng.NextDouble() * 12f) * swayScale;
            swaySpeed = (0.8f + (float)rng.NextDouble() * 1.4f) * speedScale;
        }

        void Update()
        {
            if (image == null)
                return;

            float t = Time.unscaledTime;
            float twinkle = 0.5f + 0.5f * Mathf.Sin(t * twinkleSpeed + phase);
            var c = image.color;
            c.a = baseAlpha * (0.35f + 0.65f * twinkle);
            image.color = c;

            rt.anchoredPosition = basePos + new Vector2(
                Mathf.Sin(t * swaySpeed + phase) * swayAmp,
                Mathf.Cos(t * swaySpeed * 0.8f + phase) * swayAmp * 0.6f);
            rt.Rotate(0f, 0f, spinSpeed * Time.unscaledDeltaTime);
        }
    }
}
