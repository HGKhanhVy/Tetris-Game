using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // Vụ nổ nhỏ khi cụm tài nguyên biến mất: các tia (spark) văng ra + mờ dần, kèm 1 vòng sốc
    // (shockwave) phồng to. Tự tắt để tái dùng (pool). Toạ độ theo screen px (canvas overlay).
    public sealed class ClusterBurst : MonoBehaviour
    {
        RectTransform rt;
        Image ring;
        Image[] sparks;
        Vector2[] dirs;
        float[] speed;
        float t;
        bool playing;
        Color color;

        const float Dur = 0.42f;

        public bool IsPlaying => playing;

        // Tạo sẵn tia + vòng sốc. sprite = hình tròn mềm dùng chung.
        public void Build(int sparkCount, Sprite dot)
        {
            rt = (RectTransform)transform;
            var rng = new System.Random(GetInstanceID());

            ring = NewPiece("Ring", dot);
            ring.rectTransform.sizeDelta = new Vector2(30, 30);

            sparks = new Image[sparkCount];
            dirs = new Vector2[sparkCount];
            speed = new float[sparkCount];
            for (int i = 0; i < sparkCount; i++)
            {
                sparks[i] = NewPiece("Spark", dot);
                float ang = (i / (float)sparkCount) * 6.2831853f + (float)rng.NextDouble() * 0.5f;
                dirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                speed[i] = 120f + (float)rng.NextDouble() * 120f;
                float s = 10f + (float)rng.NextDouble() * 10f;
                sparks[i].rectTransform.sizeDelta = new Vector2(s, s);
            }
            gameObject.SetActive(false);
        }

        Image NewPiece(string name, Sprite dot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = dot;
            img.raycastTarget = false;
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        public void Play(Vector3 screenPos, Color c)
        {
            if (sparks == null)
                return;
            color = c;
            t = 0f;
            playing = true;
            rt.position = screenPos;
            transform.SetAsLastSibling();
            for (int i = 0; i < sparks.Length; i++)
                sparks[i].rectTransform.anchoredPosition = Vector2.zero;
            if (ring != null)
                ring.rectTransform.anchoredPosition = Vector2.zero;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (!playing)
                return;
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Dur);

            // Vòng sốc: phồng nhanh + mờ.
            if (ring != null)
            {
                float rs = Mathf.Lerp(20f, 140f, k);
                ring.rectTransform.sizeDelta = new Vector2(rs, rs);
                var rc = color; rc.a = (1f - k) * 0.6f; ring.color = rc;
            }

            // Tia văng ra chậm dần + mờ.
            float ease = 1f - (1f - k) * (1f - k);
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i].rectTransform.anchoredPosition = dirs[i] * (speed[i] * ease * 0.01f * 100f);
                var sc = color; sc.a = 1f - k; sparks[i].color = sc;
                float sk = Mathf.Lerp(1f, 0.3f, k);
                sparks[i].rectTransform.localScale = new Vector3(sk, sk, 1f);
            }

            if (k >= 1f)
            {
                playing = false;
                gameObject.SetActive(false);
            }
        }
    }
}
