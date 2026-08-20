using UnityEngine;

namespace BrickStacker
{
    // GD v3 §15: ba Skill tiêu Energy. Id dùng luôn cho gói mạng.
    public enum OnlineSkill : byte
    {
        GarbageDrop = 1,   // §15.1 cost 4, +1 hàng rác
        LifeDrain = 2,     // §15.2 cost 4, hút 16 máu
        OverloadBlast = 3, // §15.3 cost 7, 25 dmg / phá khiên +8
        Attack = 4,        // §12 đòn Kiếm tự động (id mạng — không phải skill người chơi bấm)
        DrainHeal = 5      // §15.2 gói mạng: bên bị Hút máu báo lượng máu thực mất để bên gây hồi đúng
    }

    // GD v3 §12: đòn đánh tự động theo cụm Kiếm.
    public enum AttackTier : byte
    {
        Basic = 1, // 4–5 ô
        Strong = 2 // 6+ ô
    }

    // DTO nạp thông số Online v3 từ JSON (GD §22). Field nào có trong file sẽ ghi đè mặc định.
    [System.Serializable]
    public class OnlineConfigData
    {
        public int initialHealth = 100;
        public int maxHealth = 100;
        public int maxEnergy = 10;
        public int maxShieldCharge = 2;   // §13
        public int activeShieldHP = 16;   // §13
        public float activeShieldDuration = 4f;

        // §12 Attack tự động
        public int basicAttackDamage = 8;
        public int strongAttackDamage = 16;
        public float basicAttackWarning = 0.75f;
        public float strongAttackWarning = 1.0f;
        public float minAttackInterval = 0.5f; // §12.5 hàng đợi

        // Cụm → tài nguyên (§13 Shield, §14 Energy)
        public int shieldChargeBasic = 1;
        public int shieldChargeStrong = 2;
        public int energyBasic = 3;
        public int energyStrong = 5;

        // §15 Skill
        public int garbageDropCost = 4;
        public int garbageDropLines = 1;
        public float garbageDropWarning = 2f;
        public int lifeDrainCost = 4;
        public int lifeDrainDamage = 16;
        public float lifeDrainWarning = 0.75f;
        public int overloadCost = 7;
        public int overloadDamage = 25;
        public int overloadShieldPierce = 8;
        public float overloadWarning = 1.5f;

        public float maxMatchSeconds = 180f;
    }

    // Thông số cân bằng Online v3 (GD §11–16). Nạp override từ
    // Resources/BrickStacker/online_config.json nếu có, else dùng mặc định.
    public static class OnlineConfig
    {
        static OnlineConfigData data = new OnlineConfigData();
        static bool loaded;

        public static void EnsureLoaded()
        {
            if (loaded)
                return;
            loaded = true;
            var asset = Resources.Load<TextAsset>("BrickStacker/online_config");
            if (asset != null)
            {
                try { data = JsonUtility.FromJson<OnlineConfigData>(asset.text) ?? new OnlineConfigData(); }
                catch { data = new OnlineConfigData(); }
            }
        }

        public static int InitialHealth { get { EnsureLoaded(); return data.initialHealth; } }
        public static int MaxHealth { get { EnsureLoaded(); return data.maxHealth; } }
        public static int MaxEnergy { get { EnsureLoaded(); return data.maxEnergy; } }
        public static int MaxShieldCharge { get { EnsureLoaded(); return data.maxShieldCharge; } }
        public static int ActiveShieldHP { get { EnsureLoaded(); return data.activeShieldHP; } }
        public static float ActiveShieldDuration { get { EnsureLoaded(); return data.activeShieldDuration; } }

        public static int AttackDamage(AttackTier tier) { EnsureLoaded(); return tier == AttackTier.Strong ? data.strongAttackDamage : data.basicAttackDamage; }
        public static float AttackWarning(AttackTier tier) { EnsureLoaded(); return tier == AttackTier.Strong ? data.strongAttackWarning : data.basicAttackWarning; }
        public static float MinAttackInterval { get { EnsureLoaded(); return data.minAttackInterval; } }

        public static int ShieldChargeGain(bool strong) { EnsureLoaded(); return strong ? data.shieldChargeStrong : data.shieldChargeBasic; }
        public static int EnergyGain(bool strong) { EnsureLoaded(); return strong ? data.energyStrong : data.energyBasic; }

        public static int SkillCost(OnlineSkill s)
        {
            EnsureLoaded();
            switch (s)
            {
                case OnlineSkill.GarbageDrop: return data.garbageDropCost;
                case OnlineSkill.LifeDrain: return data.lifeDrainCost;
                case OnlineSkill.OverloadBlast: return data.overloadCost;
                default: return 0;
            }
        }

        public static int GarbageDropLines { get { EnsureLoaded(); return data.garbageDropLines; } }
        public static float GarbageDropWarning { get { EnsureLoaded(); return data.garbageDropWarning; } }
        public static int LifeDrainDamage { get { EnsureLoaded(); return data.lifeDrainDamage; } }
        public static float LifeDrainWarning { get { EnsureLoaded(); return data.lifeDrainWarning; } }
        public static int OverloadDamage { get { EnsureLoaded(); return data.overloadDamage; } }
        public static int OverloadShieldPierce { get { EnsureLoaded(); return data.overloadShieldPierce; } }
        public static float OverloadWarning { get { EnsureLoaded(); return data.overloadWarning; } }
        public static float MaxMatchSeconds { get { EnsureLoaded(); return data.maxMatchSeconds; } }
    }

    // GD v3 §14: Energy tích từ cụm Sấm sét, tiêu cho Skill.
    public class EnergySystem
    {
        public int Energy { get; private set; }
        public int Max => OnlineConfig.MaxEnergy;

        public void Reset() => Energy = 0;

        public int GainFromCluster(bool strong)
        {
            int before = Energy;
            Energy = Mathf.Min(Max, Energy + OnlineConfig.EnergyGain(strong));
            return Energy - before;
        }

        public bool CanAfford(OnlineSkill skill) => Energy >= OnlineConfig.SkillCost(skill);

        public bool TrySpend(OnlineSkill skill)
        {
            int cost = OnlineConfig.SkillCost(skill);
            if (Energy < cost)
                return false;
            Energy -= cost;
            return true;
        }
    }

    // GD v3 §13: Khiên = Shield Charge (tích, tối đa 2) + Active Shield (bấm để bật: HP 16, 4 giây).
    // §17: Máu 100.
    public class HealthSystem
    {
        public int Health { get; private set; }
        public int ShieldCharges { get; private set; }
        public int ActiveShieldHP { get; private set; }
        public float ActiveShieldTimer { get; private set; }

        public int Max => OnlineConfig.MaxHealth;
        public bool IsDead => Health <= 0;
        public bool IsShieldActive => ActiveShieldHP > 0 && ActiveShieldTimer > 0f;

        public void Reset()
        {
            Health = OnlineConfig.InitialHealth;
            ShieldCharges = 0;
            ActiveShieldHP = 0;
            ActiveShieldTimer = 0f;
        }

        // Cụm Khiên → +1/+2 charge (trần MaxShieldCharge).
        public int AddShieldCharge(bool strong)
        {
            int before = ShieldCharges;
            ShieldCharges = Mathf.Min(OnlineConfig.MaxShieldCharge, ShieldCharges + OnlineConfig.ShieldChargeGain(strong));
            return ShieldCharges - before;
        }

        // Bấm Khiên: tiêu 1 charge → bật Active Shield (HP 16, 4s). Không cộng dồn nhiều khiên.
        public bool ActivateShield()
        {
            if (ShieldCharges <= 0 || IsShieldActive)
                return false;
            ShieldCharges--;
            ActiveShieldHP = OnlineConfig.ActiveShieldHP;
            ActiveShieldTimer = OnlineConfig.ActiveShieldDuration;
            return true;
        }

        public void Tick(float dt)
        {
            if (ActiveShieldTimer > 0f)
            {
                ActiveShieldTimer -= dt;
                if (ActiveShieldTimer <= 0f)
                    ActiveShieldHP = 0; // hết thời gian → khiên biến mất
            }
        }

        // Nhận sát thương: trừ vào Active Shield trước (nếu đang bật), phần dư trừ vào máu.
        // Trả về lượng máu thực mất (để Life Drain hồi đúng lượng).
        public int TakeDamage(int damage)
        {
            damage = Mathf.Max(0, damage);
            if (IsShieldActive)
            {
                int absorbed = Mathf.Min(ActiveShieldHP, damage);
                ActiveShieldHP -= absorbed;
                if (ActiveShieldHP <= 0)
                    ActiveShieldTimer = 0f; // khiên vỡ
                damage -= absorbed;
            }
            int before = Health;
            Health = Mathf.Max(0, Health - damage);
            return before - Health;
        }

        // §15.3 Overload: phá Active Shield rồi gây sát thương xuyên.
        public int TakeOverload(int fullDamage, int pierceDamage)
        {
            if (IsShieldActive)
            {
                ActiveShieldHP = 0;
                ActiveShieldTimer = 0f;
                int before = Health;
                Health = Mathf.Max(0, Health - Mathf.Max(0, pierceDamage));
                return before - Health;
            }
            int b = Health;
            Health = Mathf.Max(0, Health - Mathf.Max(0, fullDamage));
            return b - Health;
        }

        public int Heal(int amount)
        {
            int before = Health;
            Health = Mathf.Min(Max, Health + Mathf.Max(0, amount));
            return Health - before;
        }
    }
}
