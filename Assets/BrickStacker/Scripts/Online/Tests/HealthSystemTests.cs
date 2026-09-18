using NUnit.Framework;

namespace BrickStacker.Online.Tests
{
    // Covers GD v3 §13 (Shield Charge + Active Shield), §15.3 (Overload pierce) and §17 (máu).
    public class HealthSystemTests
    {
        static HealthSystem FreshSystem()
        {
            var health = new HealthSystem();
            health.Reset();
            return health;
        }

        static HealthSystem SystemWithActiveShield()
        {
            var health = FreshSystem();
            health.AddShieldCharge(false);
            Assert.IsTrue(health.ActivateShield(), "Test setup expects a charge to be available.");
            return health;
        }

        [Test]
        public void Reset_StartsAtInitialHealthWithNoShield()
        {
            var health = FreshSystem();

            Assert.AreEqual(OnlineConfig.InitialHealth, health.Health);
            Assert.AreEqual(0, health.ShieldCharges);
            Assert.AreEqual(0, health.ActiveShieldHP);
            Assert.IsFalse(health.IsShieldActive);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void AddShieldCharge_StopsAtCap_AndReportsOnlyTheRealGain()
        {
            var health = FreshSystem();

            int guard = 0;
            while (health.ShieldCharges < OnlineConfig.MaxShieldCharge && guard++ < 50)
            {
                health.AddShieldCharge(false);
            }

            Assert.AreEqual(OnlineConfig.MaxShieldCharge, health.ShieldCharges);
            Assert.AreEqual(0, health.AddShieldCharge(true), "A full charge bar must report a gain of zero.");
            Assert.AreEqual(OnlineConfig.MaxShieldCharge, health.ShieldCharges);
        }

        [Test]
        public void ActivateShield_WithoutCharge_Fails()
        {
            var health = FreshSystem();

            Assert.IsFalse(health.ActivateShield());
            Assert.IsFalse(health.IsShieldActive);
        }

        [Test]
        public void ActivateShield_SpendsOneChargeAndRaisesTheShield()
        {
            var health = FreshSystem();
            health.AddShieldCharge(false);
            int before = health.ShieldCharges;

            Assert.IsTrue(health.ActivateShield());

            Assert.AreEqual(before - 1, health.ShieldCharges);
            Assert.AreEqual(OnlineConfig.ActiveShieldHP, health.ActiveShieldHP);
            Assert.IsTrue(health.IsShieldActive);
        }

        [Test]
        public void ActivateShield_WhileAlreadyUp_DoesNotStack()
        {
            var health = SystemWithActiveShield();
            health.AddShieldCharge(false);
            int charges = health.ShieldCharges;

            Assert.IsFalse(health.ActivateShield());
            Assert.AreEqual(charges, health.ShieldCharges, "A refused activation must not eat a charge.");
        }

        [Test]
        public void Tick_DropsTheShieldWhenTheTimerRunsOut()
        {
            var health = SystemWithActiveShield();

            health.Tick(OnlineConfig.ActiveShieldDuration + 0.01f);

            Assert.IsFalse(health.IsShieldActive);
            Assert.AreEqual(0, health.ActiveShieldHP);
        }

        [Test]
        public void TakeDamage_WithoutShield_ReducesHealthAndReportsTheLoss()
        {
            var health = FreshSystem();

            int lost = health.TakeDamage(30);

            Assert.AreEqual(30, lost);
            Assert.AreEqual(OnlineConfig.InitialHealth - 30, health.Health);
        }

        [Test]
        public void TakeDamage_IsAbsorbedByTheShieldBeforeHealth()
        {
            var health = SystemWithActiveShield();
            int shield = health.ActiveShieldHP;

            int lost = health.TakeDamage(shield - 1);

            Assert.AreEqual(0, lost, "A hit smaller than the shield must not reach health.");
            Assert.AreEqual(OnlineConfig.InitialHealth, health.Health);
            Assert.AreEqual(1, health.ActiveShieldHP);
        }

        [Test]
        public void TakeDamage_SpillsOverOnceTheShieldShatters()
        {
            var health = SystemWithActiveShield();
            int shield = health.ActiveShieldHP;

            int lost = health.TakeDamage(shield + 5);

            Assert.AreEqual(5, lost);
            Assert.AreEqual(OnlineConfig.InitialHealth - 5, health.Health);
            Assert.IsFalse(health.IsShieldActive, "A shattered shield must not linger on its timer.");
        }

        [Test]
        public void TakeDamage_IgnoresNegativeValues()
        {
            var health = FreshSystem();

            Assert.AreEqual(0, health.TakeDamage(-10));
            Assert.AreEqual(OnlineConfig.InitialHealth, health.Health);
        }

        [Test]
        public void TakeDamage_FloorsAtZeroAndMarksDead()
        {
            var health = FreshSystem();

            int lost = health.TakeDamage(OnlineConfig.InitialHealth + 500);

            Assert.AreEqual(OnlineConfig.InitialHealth, lost, "Reported loss must be the health actually removed.");
            Assert.AreEqual(0, health.Health);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void TakeOverload_WithShield_BreaksItAndOnlyPierceDamageLands()
        {
            var health = SystemWithActiveShield();

            int lost = health.TakeOverload(OnlineConfig.OverloadDamage, OnlineConfig.OverloadShieldPierce);

            Assert.AreEqual(OnlineConfig.OverloadShieldPierce, lost);
            Assert.AreEqual(OnlineConfig.InitialHealth - OnlineConfig.OverloadShieldPierce, health.Health);
            Assert.IsFalse(health.IsShieldActive);
        }

        [Test]
        public void TakeOverload_WithoutShield_LandsFullDamage()
        {
            var health = FreshSystem();

            int lost = health.TakeOverload(OnlineConfig.OverloadDamage, OnlineConfig.OverloadShieldPierce);

            Assert.AreEqual(OnlineConfig.OverloadDamage, lost);
            Assert.AreEqual(OnlineConfig.InitialHealth - OnlineConfig.OverloadDamage, health.Health);
        }

        [Test]
        public void Heal_StopsAtMaxAndReportsOnlyTheRealHeal()
        {
            var health = FreshSystem();
            health.TakeDamage(10);

            Assert.AreEqual(10, health.Heal(999));
            Assert.AreEqual(health.Max, health.Health);
            Assert.AreEqual(0, health.Heal(50), "Healing at full health must report zero.");
        }

        [Test]
        public void Heal_IgnoresNegativeValues()
        {
            var health = FreshSystem();
            health.TakeDamage(10);
            int before = health.Health;

            Assert.AreEqual(0, health.Heal(-25));
            Assert.AreEqual(before, health.Health);
        }
    }
}
