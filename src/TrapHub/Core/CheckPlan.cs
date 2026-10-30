using System;
using System.Collections.Generic;
using System.Linq;

namespace TrapHub.Core;

/// <summary>Why Check traps leaves a trap as it is.</summary>
public enum CheckSkip
{
    /// <summary>It holds a catch and the next one would break it, so it is emptied but gets no new bait.</summary>
    Breaks,

    /// <summary>It has no preset, or none of its preset's bait is left.</summary>
    NoBait,
}

/// <summary>
/// What Check traps will do if pressed now: the walks to collect, then the bait each empty trap gets. The HUD's tip
/// shows it, and the press makes the same choices (TrapRun.CheckAll), so the tip cannot promise what the press skips.
/// </summary>
/// <param name="Take">The traps to empty, in walking order.</param>
/// <param name="Bait">Each trap to bait and its bait, null when it has none to use; player's floor first.</param>
public sealed record CheckPlan(IReadOnlyList<Trap> Take, IReadOnlyList<(Trap Trap, BaitChoice? Choice)> Bait, IReadOnlyList<(Trap Trap, CheckSkip Why)> Skipped)
{
    /// <param name="onlyFloor">Null for every floor.</param>
    /// <param name="isRefill">Whether a trap still holding its catch is baited too, so it is armed again once emptied.</param>
    public static CheckPlan For(TrapWorld world, PresetBook book, Func<int, int, int> floorCost, int? onlyFloor, bool isRefill)
    {
        var take = TakeAllRoute.Plan(world.Traps, world.PlayerFloor, world.PlayerPosition, floorCost, onlyFloor);
        var next = TrapView.NextBait(world, book, trap => IsBaitWanted(trap, onlyFloor, isRefill));
        var bait = InOrder(world, next);
        var skipped = new List<(Trap, CheckSkip)>();

        foreach (var trap in TrapRun.FloorFirst(world))
        {
            if (!world.Catalog.Types.TryGetValue(trap.TypeId, out var type)) continue;

            if (next.TryGetValue(trap.Id, out var choice))
            {
                if (choice == null) skipped.Add((trap, CheckSkip.NoBait));
            }
            else if (isRefill && IsBreaking(trap, type) && take.Contains(trap))
            {
                skipped.Add((trap, CheckSkip.Breaks));
            }
        }

        return new CheckPlan(take, bait, skipped);
    }

    /// <summary>
    /// What Re-bait traps will do if pressed now (TrapRun.RebaitAll): every trap that can take bait, on every floor, a
    /// trap still holding its catch included, with the bait it gets or null when none is left. Nothing is collected.
    /// </summary>
    public static IReadOnlyList<(Trap Trap, BaitChoice? Choice)> RebaitAll(TrapWorld world, PresetBook book) => InOrder(world, TrapView.NextBait(world, book));

    /// <summary>The traps Check traps baits, besides BaitPick.CanBait: on the floor asked for, and with Re-bait off only the empty ones.</summary>
    public static bool IsBaitWanted(Trap trap, int? onlyFloor, bool isRefill) => (onlyFloor == null || trap.Floor == onlyFloor) && (isRefill || trap.PreyId <= 0);

    /// <summary>Every trap the check walks to or baits.</summary>
    public IEnumerable<Trap> Touched() => Take.Concat(Bait.Where(pair => pair.Choice != null).Select(pair => pair.Trap)).GroupBy(trap => trap.Id).Select(group => group.First());

    private static List<(Trap Trap, BaitChoice? Choice)> InOrder(TrapWorld world, IReadOnlyDictionary<long, BaitChoice?> next) =>
        TrapRun.FloorFirst(world).Where(trap => next.ContainsKey(trap.Id)).Select(trap => (trap, next[trap.Id])).ToList();

    private static bool IsBreaking(Trap trap, TrapType type) =>
        !type.HasStore && type.AcceptedBait.Count > 0 && trap.PreyId > 0 && trap.BaitId == 0 && trap.Durability - type.CatchCost <= 0;
}
