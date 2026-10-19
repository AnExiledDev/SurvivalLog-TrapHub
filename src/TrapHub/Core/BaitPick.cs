using System.Collections.Generic;
using System.Linq;

namespace TrapHub.Core;

/// <summary>The bait a trap gets: the item, the stack it comes from, and which preset step chose it (-1 for the last resort).</summary>
public sealed record BaitChoice(int ItemId, long InstanceId, int Step)
{
    public const int LastResortStep = -1;
}

/// <summary>
/// Picks the bait for one empty trap from its preset. A step is used when its item is allowed, the trap takes it, and
/// the player holds more of it than the step keeps back. Portions already promised to other traps in the same press
/// are counted as gone, through the shared reserve, so Re-bait All never plans more bait than there is.
/// </summary>
public static class BaitPick
{
    /// <summary>
    /// Whether the game would take bait in this trap now: it holds none, takes some, and would survive its next catch.
    /// A trap with a store (the Auto Trap Cage) keeps a bait store of its own that only the game's trap panel fills, so
    /// it is never baited here.
    /// </summary>
    public static bool CanBait(Trap trap, TrapType type) =>
        !type.HasStore && trap.BaitId == 0 && type.AcceptedBait.Count > 0 && (trap.PreyId <= 0 || trap.Durability - type.CatchCost > 0);

    /// <summary>Null when no step and no last resort has bait to give. The chosen stack is added to the reserve.</summary>
    public static BaitChoice? For(TrapType type, BaitPreset preset, IReadOnlySet<int> allowed, IReadOnlyList<BaitUnit> stock, TrapCatalog catalog, IDictionary<long, int> reserved)
    {
        var inScope = stock.Where(unit => preset.Source == BaitSource.BagThenHome || unit.IsInBag).ToList();
        var choice = FromSteps(type, preset, allowed, inScope, reserved) ?? (preset.LastResort ? LastResort(type, allowed, inScope, catalog, reserved) : null);

        if (choice != null) reserved[choice.InstanceId] = Reserved(reserved, choice.InstanceId) + 1;

        return choice;
    }

    /// <summary>How many of an item the player has in scope that no trap in this press has been promised yet.</summary>
    public static int Available(int itemId, IEnumerable<BaitUnit> inScope, IDictionary<long, int> reserved) =>
        inScope.Where(unit => unit.ItemId == itemId).Sum(unit => Free(unit, reserved));

    private static BaitChoice? FromSteps(TrapType type, BaitPreset preset, IReadOnlySet<int> allowed, IReadOnlyList<BaitUnit> inScope, IDictionary<long, int> reserved)
    {
        for (var step = 0; step < preset.Steps.Count; step++)
        {
            var itemId = preset.Steps[step].ItemId;

            if (!allowed.Contains(itemId) || !type.AcceptedBait.Contains(itemId)) continue;
            if (Available(itemId, inScope, reserved) <= preset.Steps[step].KeepAtLeast) continue;

            return new BaitChoice(itemId, Stack(itemId, inScope, preset.SpoilFirst, reserved), step);
        }

        return null;
    }

    /// <summary>Any allowed universal bait the trap takes, the one the player has most of first.</summary>
    private static BaitChoice? LastResort(TrapType type, IReadOnlySet<int> allowed, IReadOnlyList<BaitUnit> inScope, TrapCatalog catalog, IDictionary<long, int> reserved)
    {
        var best = inScope
            .Select(unit => unit.ItemId)
            .Distinct()
            .Where(itemId => allowed.Contains(itemId) && type.AcceptedBait.Contains(itemId) && catalog.Baits.TryGetValue(itemId, out var bait) && bait.Kind == BaitKind.Universal)
            .Select(itemId => (ItemId: itemId, Count: Available(itemId, inScope, reserved)))
            .Where(item => item.Count > 0)
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.ItemId)
            .FirstOrDefault();

        return best.Count > 0 ? new BaitChoice(best.ItemId, Stack(best.ItemId, inScope, spoilFirst: true, reserved), BaitChoice.LastResortStep) : null;
    }

    /// <summary>The bag before home, then the stack closest to spoiling (or the freshest), food that never spoils last.</summary>
    private static long Stack(int itemId, IEnumerable<BaitUnit> inScope, bool spoilFirst, IDictionary<long, int> reserved)
    {
        var stacks = inScope.Where(unit => unit.ItemId == itemId && Free(unit, reserved) > 0).OrderByDescending(unit => unit.IsInBag);
        var ordered = spoilFirst
            ? stacks.ThenBy(unit => unit.HoursLeft ?? int.MaxValue)
            : stacks.ThenByDescending(unit => unit.HoursLeft ?? int.MaxValue);

        return ordered.ThenBy(unit => unit.InstanceId).First().InstanceId;
    }

    private static int Free(BaitUnit unit, IDictionary<long, int> reserved) => unit.Count - Reserved(reserved, unit.InstanceId);

    private static int Reserved(IDictionary<long, int> reserved, long instanceId) => reserved.TryGetValue(instanceId, out var count) ? count : 0;
}
