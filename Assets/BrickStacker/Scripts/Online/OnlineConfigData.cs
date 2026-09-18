namespace BrickStacker
{
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
}
