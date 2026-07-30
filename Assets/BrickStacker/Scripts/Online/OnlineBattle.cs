using UnityEngine;

namespace BrickStacker
{
    // Ba kỹ năng MVP (design §11). Giá trị id dùng luôn cho gói mạng.
    public enum OnlineSkill : byte
    {
        Attack = 1,  // §7.1 cost 3, dmg 2
        Shield = 2,  // §7.2 cost 2, chặn 1 đòn
        Garbage = 3  // §7.3 cost 5, +1 hàng rác
    }

    // DTO nạp thông số Online từ JSON (design §13). Field nào có trong file sẽ ghi đè mặc định.
    [System.Serializable]
    public class OnlineConfigData
    {
        public int maxHealth = 10;
        public int maxEnergy = 10;
        public int attackCost = 3;
        public int attackDamage = 2;
        public float attackWarning = 0.75f;
        public int shieldCost = 2;
        public int shieldBlocks = 1;
        public int garbageCost = 5;
        public int garbageLines = 1;
        public float garbageWarning = 2.0f;
        public int maxComboBonusEnergy = 3;
        public float maxMatchSeconds = 180f;
    }

    // Thông số cân bằng Online (design §11). Không ghi cứng — nạp override từ
    // Resources/BrickStacker/online_config.json nếu có (design §13), else dùng mặc định.
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

        public static int MaxHealth { get { EnsureLoaded(); return data.maxHealth; } }
        public static int MaxEnergy { get { EnsureLoaded(); return data.maxEnergy; } }
        public static int AttackCost { get { EnsureLoaded(); return data.attackCost; } }
        public static int AttackDamage { get { EnsureLoaded(); return data.attackDamage; } }
        public static float AttackWarning { get { EnsureLoaded(); return data.attackWarning; } }
        public static int ShieldCost { get { EnsureLoaded(); return data.shieldCost; } }
        public static int ShieldBlocks { get { EnsureLoaded(); return data.shieldBlocks; } }
        public static int GarbageCost { get { EnsureLoaded(); return data.garbageCost; } }
        public static int GarbageLines { get { EnsureLoaded(); return data.garbageLines; } }
        public static float GarbageWarning { get { EnsureLoaded(); return data.garbageWarning; } }
        public static int MaxComboBonusEnergy { get { EnsureLoaded(); return data.maxComboBonusEnergy; } }
        public static float MaxMatchSeconds { get { EnsureLoaded(); return data.maxMatchSeconds; } }

        // §6.4: xóa 1/2/3/4 hàng → 1/3/5/8 năng lượng.
        public static int EnergyForLines(int lines)
        {
            switch (lines)
            {
                case 1: return 1;
                case 2: return 3;
                case 3: return 5;
                default: return lines >= 4 ? 8 : 0;
            }
        }

        public static int EnergyCost(OnlineSkill skill)
        {
            switch (skill)
            {
                case OnlineSkill.Attack: return AttackCost;
                case OnlineSkill.Shield: return ShieldCost;
                case OnlineSkill.Garbage: return GarbageCost;
                default: return 0;
            }
        }
    }

    // Năng lượng của người chơi (design §6.4). Tích khi xóa hàng, tiêu khi dùng kỹ năng.
    public class EnergySystem
    {
        public int Energy { get; private set; }
        public int Max => OnlineConfig.MaxEnergy;

        public void Reset() => Energy = 0;

        // Trả về lượng năng lượng thực nhận (đã kẹp trần). combo = số lần đặt khối
        // liên tiếp có xóa hàng, thưởng thêm (kẹp MaxComboBonusEnergy).
        public int GainFromLines(int lines, int combo)
        {
            int gain = OnlineConfig.EnergyForLines(lines);
            if (gain <= 0)
                return 0;
            if (combo > 1)
                gain += Mathf.Min(combo - 1, OnlineConfig.MaxComboBonusEnergy);
            int before = Energy;
            Energy = Mathf.Min(Max, Energy + gain);
            return Energy - before;
        }

        public bool CanAfford(OnlineSkill skill) => Energy >= OnlineConfig.EnergyCost(skill);

        // Tiêu năng lượng nếu đủ. Trả về true nếu thành công.
        public bool TrySpend(OnlineSkill skill)
        {
            int cost = OnlineConfig.EnergyCost(skill);
            if (Energy < cost)
                return false;
            Energy -= cost;
            return true;
        }
    }

    // Máu + khiên của người chơi (design §7.2, §8). Người chơi tự quản máu của mình;
    // khi nhận đòn tấn công từ đối thủ thì áp sát thương vào máu mình (peer chịu trách
    // nhiệm về máu của chính mình — thay cho server authority §14 vì kiến trúc P2P).
    public class HealthSystem
    {
        public int Health { get; private set; }
        public int ShieldCharges { get; private set; }
        public int Max => OnlineConfig.MaxHealth;
        public bool IsDead => Health <= 0;

        public void Reset()
        {
            Health = OnlineConfig.MaxHealth;
            ShieldCharges = 0;
        }

        public void AddShield()
        {
            ShieldCharges += OnlineConfig.ShieldBlocks;
        }

        // Nhận 1 đòn tấn công. Nếu còn khiên thì chặn (tiêu 1 lớp) và không mất máu.
        // Trả về true nếu đòn bị khiên chặn.
        public bool TakeAttack(int damage)
        {
            if (ShieldCharges > 0)
            {
                ShieldCharges--;
                return true; // chặn được
            }
            Health = Mathf.Max(0, Health - Mathf.Max(0, damage));
            return false;
        }

        public void Heal(int amount)
        {
            Health = Mathf.Clamp(Health + Mathf.Max(0, amount), 0, Max);
        }
    }
}
