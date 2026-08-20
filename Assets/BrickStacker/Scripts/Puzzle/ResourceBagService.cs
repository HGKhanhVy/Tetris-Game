using System.Collections.Generic;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Weighted resource bag (GD v3 §4). Guarantees each resource appears at a controlled rate
    /// instead of pure per-cell random, so a type is never starved for too long.
    /// Offline bag: 6 Move / 5 Attack / 5 Shield. Online bag: 6 Attack / 5 Shield / 5 Energy.
    /// </summary>
    public sealed class ResourceBagService
    {
        readonly List<ResourceType> template = new List<ResourceType>();
        readonly List<ResourceType> bag = new List<ResourceType>();
        readonly System.Random rng;

        public ResourceBagService(PuzzleMode mode, System.Random rng = null)
        {
            this.rng = rng ?? new System.Random();
            BuildTemplate(mode);
            Refill();
        }

        void BuildTemplate(PuzzleMode mode)
        {
            template.Clear();
            if (mode == PuzzleMode.Offline)
            {
                Add(ResourceType.Move, 6);
                Add(ResourceType.Attack, 5);
                Add(ResourceType.Shield, 5);
            }
            else
            {
                Add(ResourceType.Attack, 6);
                Add(ResourceType.Shield, 5);
                Add(ResourceType.Energy, 5);
            }
        }

        void Add(ResourceType type, int count)
        {
            for (int i = 0; i < count; i++)
                template.Add(type);
        }

        void Refill()
        {
            bag.Clear();
            bag.AddRange(template);
            // Fisher–Yates shuffle.
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
        }

        public ResourceType Next()
        {
            if (bag.Count == 0)
                Refill();

            int last = bag.Count - 1;
            var type = bag[last];
            bag.RemoveAt(last);
            return type;
        }
    }
}
