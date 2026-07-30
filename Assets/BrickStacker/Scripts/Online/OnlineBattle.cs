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

    // Thông số cân bằng Online (design §11). Sau này chuyển sang config JSON §13.
    public static class OnlineConfig
    {
        public const int MaxHealth = 10;
        public const int MaxEnergy = 10;

        public const int AttackCost = 3;
        public const int AttackDamage = 2;
        public const float AttackWarning = 0.75f;

        public const int ShieldCost = 2;
        public const int ShieldBlocks = 1;

        public const int GarbageCost = 5;
        public const int GarbageLines = 1;
        public const float GarbageWarning = 2.0f;

        public const int MaxComboBonusEnergy = 3; // §6.4

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
