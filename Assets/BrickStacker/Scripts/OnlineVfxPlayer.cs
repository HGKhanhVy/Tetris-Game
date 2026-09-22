using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // Bắn VFX hạt của trận online (prefab world-space) lên trên giao diện.
    //
    // Giao diện chạy trên Canvas ScreenSpaceCamera — canvas được vẽ như một mặt phẳng cách camera
    // đúng planeDistance. Muốn hạt nằm ĐÈ LÊN UI thì phải đặt chúng gần camera hơn mặt phẳng đó,
    // nên ở đây quy đổi vị trí của một RectTransform sang toạ độ world ngay trước mặt canvas.
    public sealed class OnlineVfxPlayer : MonoBehaviour
    {
        [Header("Prefab (Assets/Eric VFX Studio/Free Game VFX/Prefab)")]
        [SerializeField] private ParticleSystem attackVfxPrefab;     // FX_Orange_Slash_1
        [SerializeField] private ParticleSystem overloadVfxPrefab;   // FX_LightPillar
        [SerializeField] private ParticleSystem drainHealVfxPrefab;  // FX_PinkCross_Up

        [Header("Chung")]
        [Tooltip("Phóng to/thu nhỏ toàn bộ VFX cho khớp cỡ nhân vật trên màn hình.")]
        [SerializeField] private float vfxScale = 1f;

        [Tooltip("Đặt hạt gần camera hơn mặt canvas bấy nhiêu đơn vị, để hạt nằm đè lên UI.")]
        [SerializeField] private float inFrontOfCanvas = 0.5f;

        [Tooltip("Phần loé sáng của các prefab này chỉ dài 0.2–0.3 giây nên xem gần như không kịp. " +
                 "Hạ xuống dưới 1 để chạy chậm lại cho dễ thấy; 1 = giữ đúng tốc độ gốc.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float playbackSpeed = 0.5f;

        [Tooltip("Nhân độ sáng (_Brightness) của VFX. Shader additive trên nền xanh sáng dễ bị chìm, " +
                 "tăng để hiệu ứng nổi hơn. Chỉ ghi đè theo từng renderer, không đụng vào material gốc.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float vfxIntensity = 2.5f;

        [Header("Mốc trên nhân vật (0..1 trong rect, gốc dưới-trái)")]
        [Tooltip("Rect nhân vật to hơn hình: preserveAspect chừa ~50px trống trên và dưới, và art không " +
                 "căn giữa sprite. Đo trên ảnh chụp bằng lưới 10%.")]
        [SerializeField] private Vector2 heroBodyAnchor = new Vector2(0.42f, 0.43f);
        [SerializeField] private Vector2 heroFeetAnchor = new Vector2(0.40f, 0.12f);
        [SerializeField] private Vector2 enemyBodyAnchor = new Vector2(0.55f, 0.42f);
        [SerializeField] private Vector2 enemyFeetAnchor = new Vector2(0.55f, 0.12f);

        [Header("Tấn công (FX_Orange_Slash_1)")]
        [Tooltip("Thời gian nhát chém bay từ bên đánh sang bên bị đánh. 0 = nổ ngay trên người bị đánh.")]
        [SerializeField] private float attackTravelSeconds = 0.12f;

        [Tooltip("Cộng vào góc bay để phần CONG của art quay đúng về phía đối thủ. Art vẽ cung ngược " +
                 "nên mặc định 180.")]
        [Range(-180f, 180f)]
        [SerializeField] private float attackAngleOffset = 180f;

        [Header("Cuồng nộ (FX_LightPillar)")]
        [Tooltip("Tia bắn ra từ người chơi; sau bấy nhiêu giây thì coi như tia đã chạm và đối thủ giật lùi.")]
        [SerializeField] private float overloadHitDelay = 0.08f;

        [Tooltip("World units the beam covers at scale 1 (measured in game). The beam is stretched along " +
                 "its length by distance / this, so it always reaches the target however far apart the " +
                 "fighters stand.")]
        [SerializeField] private float overloadNaturalLength = 11f;

        [Tooltip("How far past the target the beam reaches, as a multiple of the distance. >1 so it " +
                 "visibly punches through instead of stopping short at the body.")]
        [SerializeField] private float overloadOvershoot = 1.15f;

        [Tooltip("Cột sáng vốn dựng ĐỨNG; -90 xoay nó nằm ngang theo hướng bay thành luồng bắn.")]
        [Range(-180f, 180f)]
        [SerializeField] private float overloadAngleOffset = -90f;

        [Header("Giật lùi khi trúng đòn")]
        [Tooltip("Bên trúng đòn bị đẩy lùi bao nhiêu pixel theo hướng đòn đánh.")]
        [SerializeField] private float recoilDistance = 26f;

        [Tooltip("Tổng thời gian giật lùi rồi trở về chỗ cũ.")]
        [SerializeField] private float recoilSeconds = 0.18f;

        [Header("Rung khi hồi máu")]
        [Tooltip("Biên độ rung (pixel) của bên được hồi máu. 0 = tắt rung.")]
        [SerializeField] private float healShakeAmplitude = 15f;

        [Tooltip("Thời gian rung; biên độ giảm dần về 0 trong khoảng này.")]
        [SerializeField] private float healShakeSeconds = 0.5f;

        [Tooltip("Số lần lắc mỗi giây.")]
        [SerializeField] private float healShakeFrequency = 22f;

        [Header("Lớp khiên bao bọc")]
        [Tooltip("Đường kính bong bóng khiên, tính theo chiều cao rect nhân vật.")]
        [SerializeField] private float shieldBubbleSize = 0.9f;

        [Tooltip("Màu bong bóng — cùng tông xanh với khiên của chế độ offline.")]
        [SerializeField] private Color shieldColor = new Color(0.5f, 0.88f, 1f, 0.85f);

        // Mốc dự phòng cho rect không phải nhân vật đã đăng ký.
        static readonly Vector2 DefaultBodyAnchor = new Vector2(0.5f, 0.5f);
        static readonly Vector2 DefaultFeetAnchor = new Vector2(0.5f, 0f);

        // Hai shader của gói Eric (AdditiveFlow, AlphaBlendFlow) đều ra màu = tex * tint * _Brightness.
        // KHÔNG phải _Emission: property đó có trong material nhưng shader không khai báo nên
        // HasProperty trả false và bị bỏ qua — tăng nó không có tác dụng gì.
        static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

        // Số hiệu ứng được chồng cùng lúc: đòn Kiếm có thể nổ liên tiếp theo chuỗi combo.
        const int AttackPoolSize = 4;
        const int SkillPoolSize = 2;

        ParticleEffectPool attackPool;
        ParticleEffectPool overloadPool;
        ParticleEffectPool drainHealPool;

        Canvas cachedCanvas;
        RectTransform heroRect;
        RectTransform enemyRect;

        // Dùng lại mảng góc cho mỗi lần quy đổi, tránh cấp phát rác mỗi đòn đánh.
        readonly Vector3[] corners = new Vector3[4];

        // Hệ con + renderer của mỗi instance: lấy MỘT lần rồi giữ, khỏi GetComponentsInChildren mỗi đòn.
        readonly Dictionary<ParticleSystem, ParticleSystem[]> systemsCache = new Dictionary<ParticleSystem, ParticleSystem[]>();
        readonly Dictionary<ParticleSystem, ParticleSystemRenderer[]> renderersCache = new Dictionary<ParticleSystem, ParticleSystemRenderer[]>();
        MaterialPropertyBlock propertyBlock;

        // Vị trí gốc + chuyển động đang chạy (giật lùi HOẶC rung) của từng nhân vật, dùng chung cho
        // cả hai loại. Lưu gốc MỘT lần: nếu đòn thứ hai tới khi nhân vật còn đang lệch, đọc lại
        // anchoredPosition lúc đó sẽ ra vị trí đã lệch và nhân vật trôi dần khỏi bố cục. Dùng chung
        // thì bị đánh trong lúc đang rung hồi máu cũng không chồng hai chuyển động lên nhau.
        readonly Dictionary<RectTransform, Vector2> motionHome = new Dictionary<RectTransform, Vector2>();
        readonly Dictionary<RectTransform, Coroutine> motionRunning = new Dictionary<RectTransform, Coroutine>();

        // Bong bóng khiên: dựng MỘT lần rồi bật/tắt, không tạo/huỷ mỗi lần bấm khiên.
        Image shieldImage;
        RectTransform shieldRect;
        Coroutine shieldPulse;

        const float ShieldPopSeconds = 0.22f;
        const float ShieldPulseSpeed = 5f;

        void Awake()
        {
            attackPool = new ParticleEffectPool(attackVfxPrefab, transform, AttackPoolSize);
            overloadPool = new ParticleEffectPool(overloadVfxPrefab, transform, SkillPoolSize);
            drainHealPool = new ParticleEffectPool(drainHealVfxPrefab, transform, SkillPoolSize);
        }

        // Chỉ giữ canvas. KHÔNG chụp worldCamera ở đây: lúc HUD online dựng, canvas có thể còn ở
        // ScreenSpaceOverlay (worldCamera vẫn null) và cache null một lần là hỏng vĩnh viễn.
        public void Initialize(Canvas targetCanvas)
        {
            cachedCanvas = targetCanvas != null ? targetCanvas.rootCanvas : null;
        }

        // Gọi sau khi dựng hai nhân vật: để biết rect nào dùng mốc nào (thân/chân của xanh hay đỏ).
        public void RegisterCharacters(RectTransform hero, RectTransform enemy)
        {
            heroRect = hero;
            enemyRect = enemy;
        }

        Vector2 BodyAnchorFor(RectTransform target)
        {
            if (target == heroRect) return heroBodyAnchor;
            if (target == enemyRect) return enemyBodyAnchor;
            return DefaultBodyAnchor;
        }

        Vector2 FeetAnchorFor(RectTransform target)
        {
            if (target == heroRect) return heroFeetAnchor;
            if (target == enemyRect) return enemyFeetAnchor;
            return DefaultFeetAnchor;
        }

        // Đòn Kiếm: nhát chém BAY từ bên đánh sang bên bị đánh, phần cong quay về phía bị đánh,
        // rồi bên bị đánh giật lùi theo hướng đòn.
        public void PlayAttack(RectTransform target, RectTransform source)
        {
            if (!TryAim(attackPool, target, source, out Vector3 from, out Vector3 to, out Vector2 direction))
            {
                return;
            }

            ParticleSystem effect = attackPool.Get();
            if (effect == null)
            {
                return;
            }

            effect.transform.SetPositionAndRotation(from, AimRotation(direction, attackAngleOffset));
            Restart(effect);
            StartCoroutine(FlyThenRecoil(effect.transform, from, to, attackTravelSeconds, target, direction));
        }

        // Cuồng nộ: luồng tia NEO GỐC ở người chơi rồi vươn sang đối thủ. Tia không bay như nhát
        // chém: gốc của cột sáng nằm ở chân cột, nếu cho nó bay tới đối thủ thì tới nơi gốc đặt tại
        // đối thủ và thân tia chĩa ra phía sau lưng họ — tức bắn ngược. Nên đặt gốc ở bên đánh, chỉ
        // xoay theo hướng, rồi cho đối thủ giật lùi khi tia chạm tới.
        public void PlayOverload(RectTransform target, RectTransform source)
        {
            if (!TryAim(overloadPool, target, source, out Vector3 from, out Vector3 to, out Vector2 direction))
            {
                return;
            }

            ParticleSystem effect = overloadPool.Get();
            if (effect == null)
            {
                return;
            }

            // Stretch only along the beam (the pillar's local Y, which the rotation points at the
            // target) so it reaches the target without getting fatter. Works because the beam's long
            // parts are mesh particles in Hierarchy scaling mode, which honour non-uniform scale.
            float distance = Vector2.Distance(from, to);
            float lengthScale = Mathf.Max(1f, distance * overloadOvershoot / Mathf.Max(0.01f, overloadNaturalLength));
            Vector3 scale = new Vector3(vfxScale, vfxScale * lengthScale, vfxScale);

            effect.transform.SetPositionAndRotation(from, AimRotation(direction, overloadAngleOffset));
            Restart(effect, scale);
            StartCoroutine(RecoilAfter(overloadHitDelay, target, direction));
        }

        // Ngắm từ bên đánh sang bên bị đánh: điểm xuất phát, điểm trúng và hướng đòn. Không biết
        // bên đánh thì đánh tại chỗ, mặc định đẩy sang phải.
        bool TryAim(ParticleEffectPool pool, RectTransform target, RectTransform source,
            out Vector3 from, out Vector3 to, out Vector2 direction)
        {
            from = to = Vector3.zero;
            direction = Vector2.right;
            if (pool == null || !pool.HasPrefab || target == null)
            {
                return false;
            }

            if (!TryResolveWorldPoint(target, BodyAnchorFor(target), out to))
            {
                return false;
            }

            from = to;
            if (source != null && TryResolveWorldPoint(source, BodyAnchorFor(source), out Vector3 sourcePoint))
            {
                from = sourcePoint;
                Vector3 delta = to - sourcePoint;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    direction = new Vector2(delta.x, delta.y).normalized;
                }
            }
            return true;
        }

        static Quaternion AimRotation(Vector2 direction, float angleOffset)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
            return Quaternion.Euler(0f, 0f, angle);
        }

        IEnumerator RecoilAfter(float delay, RectTransform target, Vector2 direction)
        {
            for (float t = 0f; t < delay; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }
            StartRecoil(target, direction);
        }

        // Hút máu: dấu thập hồng bốc lên dưới chân bên được hồi máu, kèm nhân vật rung lên.
        public void PlayDrainHeal(RectTransform target)
        {
            if (drainHealPool == null || !drainHealPool.HasPrefab || target == null)
            {
                return;
            }

            if (!TryResolveWorldPoint(target, FeetAnchorFor(target), out Vector3 feet))
            {
                return;
            }

            ParticleSystem effect = drainHealPool.Get();
            if (effect == null)
            {
                return;
            }

            effect.transform.SetPositionAndRotation(feet, Quaternion.identity);
            Restart(effect);
            StartHealShake(target);
        }

        // Khiên: bong bóng bao trọn nhân vật, bật lên có nảy rồi phập phồng cho tới khi tắt.
        // bubbleSprite truyền từ ngoài vào để dùng chung đúng sprite khiên với chế độ offline.
        public void ShowShield(RectTransform target, Sprite bubbleSprite)
        {
            if (target == null)
            {
                return;
            }

            EnsureShieldBubble(target, bubbleSprite);
            shieldImage.gameObject.SetActive(true);
            if (shieldPulse != null)
            {
                StopCoroutine(shieldPulse);
            }
            shieldPulse = StartCoroutine(ShieldPulseRoutine(target));
        }

        public void HideShield()
        {
            if (shieldPulse != null)
            {
                StopCoroutine(shieldPulse);
                shieldPulse = null;
            }
            if (shieldImage != null)
            {
                shieldImage.gameObject.SetActive(false);
            }
        }

        void EnsureShieldBubble(RectTransform target, Sprite bubbleSprite)
        {
            if (shieldImage == null)
            {
                var go = new GameObject("Runtime Online Shield Bubble", typeof(RectTransform), typeof(Image));
                shieldRect = (RectTransform)go.transform;
                shieldImage = go.GetComponent<Image>();
                shieldImage.raycastTarget = false;
                shieldImage.preserveAspect = true;
            }

            // Là CON của nhân vật: bị giật lùi hay rung thì bong bóng đi theo, không bị bỏ lại.
            if (shieldRect.parent != target)
            {
                shieldRect.SetParent(target, false);
            }
            shieldRect.SetAsLastSibling();

            Vector2 center = BodyAnchorFor(target);
            shieldRect.anchorMin = center;
            shieldRect.anchorMax = center;
            shieldRect.pivot = new Vector2(0.5f, 0.5f);
            shieldRect.anchoredPosition = Vector2.zero;
            shieldImage.sprite = bubbleSprite;
        }

        IEnumerator ShieldPulseRoutine(RectTransform target)
        {
            for (float t = 0f; shieldImage != null && target != null; t += Time.unscaledDeltaTime)
            {
                // Tính lại cỡ mỗi khung theo rect hiện tại: bố cục đổi thì khiên vẫn bao vừa người.
                float diameter = target.rect.height * shieldBubbleSize;
                shieldRect.sizeDelta = new Vector2(diameter, diameter);

                float pop = EaseOutBack(Mathf.Clamp01(t / ShieldPopSeconds));
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * ShieldPulseSpeed);
                shieldRect.localScale = Vector3.one * (Mathf.Lerp(0.55f, 1f, pop) * (1f + 0.05f * pulse));

                Color color = shieldColor;
                color.a = shieldColor.a * (0.75f + 0.25f * pulse);
                shieldImage.color = color;
                yield return null;
            }
        }

        // Vượt quá 1 một chút rồi thu về — ra cảm giác bong bóng "bật" lên chứ không phóng to đều.
        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        // Khởi động lại TOÀN BỘ hiệu ứng từ đầu. Clear()+Play() không đủ: hệ con loé sáng chỉ dài
        // 0.2s, khi đã chạy xong mà root vẫn đang loop thì Play() coi như "đang chạy rồi" và bỏ
        // qua, nên từ đòn thứ hai trở đi không còn gì hiện ra.
        void Restart(ParticleSystem effect)
        {
            Restart(effect, Vector3.one * vfxScale);
        }

        void Restart(ParticleSystem effect, Vector3 scale)
        {
            effect.transform.localScale = scale;
            ApplyPlaybackSpeed(effect);
            ApplyIntensity(effect);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);
        }

        // Tăng _Brightness bằng MaterialPropertyBlock, KHÔNG sửa material: sửa sharedMaterial lúc chạy
        // trong Editor là ghi thẳng vào asset của gói VFX trên đĩa. Luôn đọc giá trị gốc từ material
        // nên chạy lại bao nhiêu lần cũng không cộng dồn độ sáng.
        void ApplyIntensity(ParticleSystem root)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            ParticleSystemRenderer[] renderers = RenderersOf(root);
            for (int i = 0; i < renderers.Length; i++)
            {
                ParticleSystemRenderer particleRenderer = renderers[i];
                Material material = particleRenderer.sharedMaterial;
                if (material == null || !material.HasProperty(BrightnessId))
                {
                    continue;
                }

                particleRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(BrightnessId, material.GetFloat(BrightnessId) * vfxIntensity);
                particleRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        ParticleSystem[] SystemsOf(ParticleSystem root)
        {
            if (!systemsCache.TryGetValue(root, out ParticleSystem[] systems))
            {
                systems = root.GetComponentsInChildren<ParticleSystem>(true);
                systemsCache[root] = systems;
            }
            return systems;
        }

        ParticleSystemRenderer[] RenderersOf(ParticleSystem root)
        {
            if (!renderersCache.TryGetValue(root, out ParticleSystemRenderer[] renderers))
            {
                renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
                renderersCache[root] = renderers;
            }
            return renderers;
        }

        // Bay tới đích rồi mới cho bên bị đánh giật lùi — giật đúng lúc trúng đòn thì mới ra cảm
        // giác bị đánh, giật ngay lúc bấm thì trông như hai việc rời nhau.
        IEnumerator FlyThenRecoil(Transform effectTransform, Vector3 from, Vector3 to, float travelSeconds,
            RectTransform target, Vector2 direction)
        {
            if (travelSeconds > 0f)
            {
                for (float t = 0f; t < travelSeconds; t += Time.unscaledDeltaTime)
                {
                    if (effectTransform == null)
                    {
                        break;
                    }
                    effectTransform.position = Vector3.Lerp(from, to, t / travelSeconds);
                    yield return null;
                }
            }
            if (effectTransform != null)
            {
                effectTransform.position = to;
            }

            StartRecoil(target, direction);
        }

        void StartRecoil(RectTransform target, Vector2 direction)
        {
            if (target == null || recoilDistance <= 0f || recoilSeconds <= 0f)
            {
                return;
            }

            Vector2 home = BeginMotion(target);
            motionRunning[target] = StartCoroutine(RecoilRoutine(target, home, direction));
        }

        void StartHealShake(RectTransform target)
        {
            if (target == null || healShakeAmplitude <= 0f || healShakeSeconds <= 0f)
            {
                return;
            }

            Vector2 home = BeginMotion(target);
            motionRunning[target] = StartCoroutine(ShakeRoutine(target, home));
        }

        // Lấy vị trí gốc (lưu từ lần đầu) và huỷ chuyển động đang dở của nhân vật, đặt về gốc để
        // chuyển động mới bắt đầu từ chỗ đứng thật.
        Vector2 BeginMotion(RectTransform target)
        {
            if (!motionHome.TryGetValue(target, out Vector2 home))
            {
                home = target.anchoredPosition;
                motionHome[target] = home;
            }

            if (motionRunning.TryGetValue(target, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
                target.anchoredPosition = home;
            }
            return home;
        }

        void EndMotion(RectTransform target, Vector2 home)
        {
            if (target != null)
            {
                target.anchoredPosition = home;
                motionRunning.Remove(target);
            }
        }

        // Rung hồi máu: lắc nhanh quanh chỗ đứng, biên độ tắt dần. Trục dọc lệch pha và nhỏ hơn
        // trục ngang để ra cảm giác "rùng mình" chứ không phải trượt qua lại đều đều.
        IEnumerator ShakeRoutine(RectTransform target, Vector2 home)
        {
            const float TwoPi = Mathf.PI * 2f;
            for (float t = 0f; t < healShakeSeconds; t += Time.unscaledDeltaTime)
            {
                if (target == null)
                {
                    yield break;
                }
                float falloff = 1f - t / healShakeSeconds;
                float phase = t * healShakeFrequency * TwoPi;
                Vector2 offset = new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 1.3f) * 0.5f);
                target.anchoredPosition = home + offset * (healShakeAmplitude * falloff);
                yield return null;
            }
            EndMotion(target, home);
        }

        IEnumerator RecoilRoutine(RectTransform target, Vector2 home, Vector2 direction)
        {
            Vector2 pushed = home + direction * recoilDistance;
            float half = recoilSeconds * 0.5f;

            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                if (target == null)
                {
                    yield break;
                }
                target.anchoredPosition = Vector2.Lerp(home, pushed, t / half);
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                if (target == null)
                {
                    yield break;
                }
                target.anchoredPosition = Vector2.Lerp(pushed, home, t / half);
                yield return null;
            }

            EndMotion(target, home);
        }

        // Tốc độ phải đặt cho TỪNG hệ con: mỗi hệ giữ simulationSpeed riêng, đặt mỗi ở root thì
        // các hệ loé sáng bên dưới vẫn chạy nhanh như cũ.
        void ApplyPlaybackSpeed(ParticleSystem root)
        {
            ParticleSystem[] systems = SystemsOf(root);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.simulationSpeed = playbackSpeed;
            }
        }

        // Camera giải lại mỗi lần bắn — chỉ là đọc property, không phải lookup.
        Camera ResolveCamera()
        {
            if (cachedCanvas != null && cachedCanvas.worldCamera != null)
            {
                return cachedCanvas.worldCamera;
            }
            return Camera.main;
        }

        // normalizedAnchor: (0,0) = góc dưới-trái rect, (1,1) = góc trên-phải.
        bool TryResolveWorldPoint(RectTransform target, Vector2 normalizedAnchor, out Vector3 world)
        {
            world = Vector3.zero;
            Camera cam = ResolveCamera();
            if (cam == null)
            {
                return false;
            }

            // GetWorldCorners: 0 = dưới-trái, 1 = trên-trái, 2 = trên-phải, 3 = dưới-phải.
            target.GetWorldCorners(corners);
            Vector3 bottomEdge = Vector3.Lerp(corners[0], corners[3], normalizedAnchor.x);
            Vector3 topEdge = Vector3.Lerp(corners[1], corners[2], normalizedAnchor.x);
            Vector3 anchor = Vector3.Lerp(bottomEdge, topEdge, normalizedAnchor.y);

            // Canvas ScreenSpaceCamera vẽ ở planeDistance; đặt hạt gần camera hơn bấy nhiêu thì
            // hạt nằm đè lên UI. Canvas kiểu khác thì bám sát mặt near của camera.
            bool screenSpaceCamera = cachedCanvas != null
                && cachedCanvas.renderMode == RenderMode.ScreenSpaceCamera;
            float plane = screenSpaceCamera ? cachedCanvas.planeDistance : cam.nearClipPlane + inFrontOfCanvas * 2f;
            float depth = Mathf.Max(cam.nearClipPlane + 0.01f, plane - inFrontOfCanvas);

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, anchor);
            world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            return true;
        }
    }
}
