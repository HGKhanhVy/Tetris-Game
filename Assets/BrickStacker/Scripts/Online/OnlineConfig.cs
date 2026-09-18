using UnityEngine;

namespace BrickStacker
{
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
}
