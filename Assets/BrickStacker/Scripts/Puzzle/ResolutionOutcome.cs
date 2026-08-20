using System.Collections.Generic;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Full result of resolving one locked piece: every chain step in order (GD v3 §3.3–§3.5).
    /// Empty <see cref="Steps"/> means the placement activated nothing.
    /// </summary>
    public sealed class ResolutionOutcome
    {
        public List<ResolutionStep> Steps { get; } = new List<ResolutionStep>();

        public bool ActivatedAnything => Steps.Count > 0;

        public int ChainCount => Steps.Count;
    }
}
