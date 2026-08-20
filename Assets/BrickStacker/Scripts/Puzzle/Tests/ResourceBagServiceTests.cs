using NUnit.Framework;

namespace BrickStacker.Puzzle.Tests
{
    public class ResourceBagServiceTests
    {
        [Test]
        public void OfflineBag_DrawsExactTemplateFrequencyPerBag()
        {
            var bag = new ResourceBagService(PuzzleMode.Offline, new System.Random(1));
            int move = 0, attack = 0, shield = 0, energy = 0;

            // One full offline bag is 6 + 5 + 5 = 16 draws.
            for (int i = 0; i < 16; i++)
            {
                switch (bag.Next())
                {
                    case ResourceType.Move: move++; break;
                    case ResourceType.Attack: attack++; break;
                    case ResourceType.Shield: shield++; break;
                    case ResourceType.Energy: energy++; break;
                }
            }

            Assert.AreEqual(6, move);
            Assert.AreEqual(5, attack);
            Assert.AreEqual(5, shield);
            Assert.AreEqual(0, energy); // Move is offline-only, Energy never appears
        }

        [Test]
        public void OnlineBag_NeverContainsMove()
        {
            var bag = new ResourceBagService(PuzzleMode.Online, new System.Random(2));
            for (int i = 0; i < 100; i++)
                Assert.AreNotEqual(ResourceType.Move, bag.Next());
        }
    }
}
