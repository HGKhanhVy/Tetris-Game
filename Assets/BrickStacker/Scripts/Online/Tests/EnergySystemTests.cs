using NUnit.Framework;

namespace BrickStacker.Online.Tests
{
    // Assertions read the balance numbers from OnlineConfig so a tuning pass on
    // online_config.json cannot silently turn these tests red.
    public class EnergySystemTests
    {
        [Test]
        public void Reset_ClearsEnergy()
        {
            var energy = new EnergySystem();
            energy.GainFromCluster(true);

            energy.Reset();

            Assert.AreEqual(0, energy.Energy);
        }

        [Test]
        public void GainFromCluster_BasicAndStrong_AddConfiguredAmount()
        {
            var energy = new EnergySystem();
            energy.Reset();

            int basicGain = energy.GainFromCluster(false);
            Assert.AreEqual(OnlineConfig.EnergyGain(false), basicGain);
            Assert.AreEqual(basicGain, energy.Energy);

            int strongGain = energy.GainFromCluster(true);
            Assert.AreEqual(OnlineConfig.EnergyGain(true), strongGain);
            Assert.AreEqual(basicGain + strongGain, energy.Energy);
        }

        [Test]
        public void GainFromCluster_StopsAtMax_AndReportsOnlyTheRealGain()
        {
            var energy = new EnergySystem();
            energy.Reset();

            // Fill past the cap; every call before saturation must report its own delta.
            int guard = 0;
            while (energy.Energy < energy.Max && guard++ < 100)
            {
                energy.GainFromCluster(true);
            }

            Assert.AreEqual(energy.Max, energy.Energy);
            Assert.AreEqual(0, energy.GainFromCluster(true), "A saturated bar must report a gain of zero.");
            Assert.AreEqual(energy.Max, energy.Energy);
        }

        [Test]
        public void TrySpend_FailsAndKeepsEnergy_WhenTooPoor()
        {
            var energy = new EnergySystem();
            energy.Reset();

            Assert.IsFalse(energy.CanAfford(OnlineSkill.OverloadBlast));
            Assert.IsFalse(energy.TrySpend(OnlineSkill.OverloadBlast));
            Assert.AreEqual(0, energy.Energy);
        }

        [Test]
        public void TrySpend_DeductsExactlyTheSkillCost()
        {
            var energy = new EnergySystem();
            energy.Reset();

            int guard = 0;
            while (!energy.CanAfford(OnlineSkill.LifeDrain) && guard++ < 100)
            {
                energy.GainFromCluster(true);
            }

            int before = energy.Energy;
            Assert.IsTrue(energy.TrySpend(OnlineSkill.LifeDrain));
            Assert.AreEqual(before - OnlineConfig.SkillCost(OnlineSkill.LifeDrain), energy.Energy);
        }

        [Test]
        public void TrySpend_OnNonSkillId_CostsNothing()
        {
            var energy = new EnergySystem();
            energy.Reset();

            // Attack and DrainHeal are network ids, not player-spent skills.
            Assert.IsTrue(energy.TrySpend(OnlineSkill.Attack));
            Assert.AreEqual(0, energy.Energy);
        }
    }
}
