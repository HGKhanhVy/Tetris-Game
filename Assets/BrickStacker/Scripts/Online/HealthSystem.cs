using UnityEngine;

namespace BrickStacker
{
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
