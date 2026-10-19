using System;
using System.Collections.Generic;
using System.Linq;

namespace TrapHub.Core;

/// <summary>The chance of each prey, and of nothing, on one catch roll.</summary>
public sealed record OddsResult(IReadOnlyList<(int PreyId, double Chance)> Prey, double Nothing)
{
    public double Catch => 1d - Nothing;
}

/// <summary>
/// Catch odds by the game's own formula (TrapManager.CalculateNormalizedWeights). Each prey weighs its base rate times
/// the bait's multiplier for it (times the bait talent), times the room's multiplier, times the rare-prey bonus for rare
/// prey. Nothing weighs the trap's empty weight, less the level reduction. A chance is a weight over the sum of all.
/// </summary>
public static class Odds
{
    private const float DefaultEmptyWeight = 50f;

    public static OddsResult For(TrapType type, int baitId, int roomId, TrapCatalog catalog, OddsBonus bonus)
    {
        var bait = catalog.Baits.TryGetValue(baitId, out var found) ? found : null;
        var weights = new List<(int PreyId, double Weight)>();

        foreach (var prey in type.Prey)
        {
            double weight = prey.BaseRate * (bait?.CoefficientFor(prey.PreyId) ?? 1f);

            if (baitId != 0) weight *= 1d + bonus.BaitTalent;

            weight *= catalog.RoomCoefficient(roomId, prey.PreyId);

            if (catalog.Prey.TryGetValue(prey.PreyId, out var item) && item.IsRare) weight *= Math.Max(1f, bonus.RareBonus);
            if (weight > 0) weights.Add((prey.PreyId, weight));
        }

        var empty = (1d - bonus.EmptyReduction) * (type.EmptyWeight > 0 ? type.EmptyWeight : DefaultEmptyWeight);
        var total = weights.Sum(w => w.Weight) + Math.Max(0d, empty);

        if (total <= 0) return new OddsResult(Array.Empty<(int, double)>(), 1d);

        var chances = weights.Select(w => (w.PreyId, w.Weight / total)).OrderByDescending(c => c.Item2).ToList();

        return new OddsResult(chances, Math.Max(0d, empty) / total);
    }
}
