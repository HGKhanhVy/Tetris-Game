using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // Chữ nổi bay lên + nảy nhẹ rồi mờ dần, tự tắt để tái dùng (pool). Dùng cho phản hồi cụm
    // trên bàn cờ Monster offline ("+2 Lượt", "Đẩy lùi!", "Khiên +1"...). Chạy theo unscaled time.
    public sealed class FloatingLabel : MonoBehaviour
    {
        Text text;
        RectTransform rt;
        Vector3 startPos;
        float t;
        float dur;
        float risePixels;
        bool playing;

        public bool IsPlaying => playing;

        void Awake()
        {
            text = GetComponent<Text>();
            rt = (RectTransform)transform;
        }

        public void Play(string message, Color color, Vector3 screenPos, float risePixels = 90f, float dur = 1.15f)
        {
            if (text == null)
            {
                text = GetComponent<Text>();
                rt = (RectTransform)transform;
            }
            text.text = message;
            text.color = color;
            startPos = screenPos;
            this.risePixels = risePixels;
            this.dur = dur;
            t = 0f;
            playing = true;
            rt.position = screenPos;
            rt.localScale = Vector3.one;
            transform.SetAsLastSibling(); // luôn nổi trên ô bàn cờ
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (!playing)
                return;

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.position = startPos + new Vector3(0f, risePixels * k, 0f);
            float pop = k < 0.15f
                ? Mathf.Lerp(0.6f, 1.18f, k / 0.15f)
                : Mathf.Lerp(1.18f, 1f, Mathf.Clamp01((k - 0.15f) / 0.35f));
            rt.localScale = new Vector3(pop, pop, 1f);
            var c = text.color;
            c.a = k < 0.6f ? 1f : Mathf.Clamp01(1f - (k - 0.6f) / 0.4f);
            text.color = c;

            if (k >= 1f)
            {
                playing = false;
                gameObject.SetActive(false);
            }
        }
    }
}
