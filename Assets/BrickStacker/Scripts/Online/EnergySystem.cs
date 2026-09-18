using UnityEngine;

namespace BrickStacker
{
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
}
