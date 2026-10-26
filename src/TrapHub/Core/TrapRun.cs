using System;
using System.Collections.Generic;
using System.Linq;

namespace TrapHub.Core;

/// <summary>What a press did, for the status line. Names are already in the player's language.</summary>
public abstract record TrapOutcome
{
    private TrapOutcome()
    {
    }

    public sealed record NoGame : TrapOutcome;

    public sealed record NotReady(string Reason) : TrapOutcome;

    public sealed record Gone : TrapOutcome;

    /// <summary>Re-bait All, or the auto pass: how many traps got bait, how many had none to use, how many the game refused.</summary>
    public sealed record Baited(int Count, int NoBait, int RefusedCount) : TrapOutcome;

    public sealed record BaitedOne(string Trap, string Bait) : TrapOutcome;

    public sealed record NothingToBait : TrapOutcome;

    public sealed record AlreadyBaited(string Trap) : TrapOutcome;

    /// <summary>The trap takes no bait, or its next catch would break it.</summary>
    public sealed record CantBait(string Trap) : TrapOutcome;

    public sealed record NoPreset(string Trap) : TrapOutcome;

    public sealed record NoBaitLeft(string Trap, string Preset) : TrapOutcome;

    /// <summary>Walks lined up, across how many floors, and how many traps the game would not queue.</summary>
    public sealed record Taking(int Count, int Floors, int RefusedCount) : TrapOutcome;

    public sealed record NothingToTake(int AlreadyQueued) : TrapOutcome;

    public sealed record Dismantling(string Trap) : TrapOutcome;

    public sealed record Refused(string Trap) : TrapOutcome;

    /// <summary>Check traps: the catches lined up for collection, then the empty traps baited.</summary>
    public sealed record Checked(TrapOutcome Take, TrapOutcome Bait) : TrapOutcome;
}

/// <summary>The presses: re-bait, take and dismantle, each read fresh from the game and each failing soft through the port.</summary>
public static class TrapRun
{
    public static TrapOutcome RebaitAll(ITrapGame game, PresetBook book) => WithWorld(game, world => RebaitWhere(game, book, world, _ => true));

    /// <summary>
    /// The auto pass: every empty trap whose preset re-baits on its own, on the player's floor for Nearby. A trap the
    /// game refused waits a minute before it is tried again, so a trap the game will not take bait in costs nothing.
    /// </summary>
    public static TrapOutcome Auto(ITrapGame game, PresetBook book, float now, IDictionary<long, float> retryAt) =>
        WithWorld(game, world => Auto(game, world, book, now, retryAt));

    /// <summary>The auto pass on a read the caller just made, so a refresh and its auto pass share one read of the game.</summary>
    public static TrapOutcome Auto(ITrapGame game, TrapWorld world, PresetBook book, float now, IDictionary<long, float> retryAt)
    {
        const float RetrySeconds = 60f;

        return RebaitWhere(game, book, world, trap =>
        {
            var auto = book.PresetFor(world.SaveKey, trap)?.Auto ?? AutoRebait.Off;
            var isDue = auto == AutoRebait.Always || (auto == AutoRebait.Nearby && trap.Floor == world.PlayerFloor);

            return isDue && !(retryAt.TryGetValue(trap.Id, out var at) && now < at);
        }, failed => retryAt[failed] = now + RetrySeconds);
    }

    /// <summary>Whether any preset re-baits on its own; without one the auto pass has nothing to do and the game is not read for it.</summary>
    public static bool HasAuto(PresetBook book)
    {
        for (var i = 0; i < book.Presets.Count; i++)
        {
            if (book.Presets[i].Auto != AutoRebait.Off) return true;
        }

        return false;
    }

    public static TrapOutcome Rebait(ITrapGame game, PresetBook book, long trapId) => WithTrap(game, trapId, (world, trap, type) =>
    {
        if (trap.BaitId != 0) return new TrapOutcome.AlreadyBaited(type.Name);
        if (!BaitPick.CanBait(trap, type)) return new TrapOutcome.CantBait(type.Name);

        var preset = book.PresetFor(world.SaveKey, trap);

        if (preset == null) return new TrapOutcome.NoPreset(type.Name);

        var choice = BaitPick.For(type, preset, book.Allowed, world.Stock, world.Catalog, new Dictionary<long, int>());

        if (choice == null) return new TrapOutcome.NoBaitLeft(type.Name, preset.Name);

        return game.SetBait(trap.Id, choice.InstanceId, choice.ItemId)
            ? new TrapOutcome.BaitedOne(type.Name, world.Catalog.ItemName(choice.ItemId))
            : new TrapOutcome.Refused(type.Name);
    });

    /// <param name="onlyFloor">Null for every floor.</param>
    public static TrapOutcome TakeAll(ITrapGame game, int? onlyFloor) => WithWorld(game, world =>
    {
        var route = TakeAllRoute.Plan(world.Traps, world.PlayerFloor, world.PlayerPosition, game.FloorCost, onlyFloor);

        if (route.Count == 0) return new TrapOutcome.NothingToTake(world.Traps.Count(trap => trap.IsTakeQueued && (onlyFloor == null || trap.Floor == onlyFloor)));

        var taken = route.Where(trap => game.Take(trap.Id, HasStore(world, trap))).ToList();

        return new TrapOutcome.Taking(taken.Count, taken.Select(trap => trap.Floor).Distinct().Count(), route.Count - taken.Count);
    });

    /// <summary>
    /// Check traps, like the game's Tend Garden for the garden: every catch lined up for collection, then every empty
    /// trap baited from its preset. With isRefill, a trap still holding its catch takes bait too, so it is armed again
    /// once emptied; without it, only traps that are already empty are baited. CheckPlan shows the same choices.
    /// </summary>
    /// <param name="onlyFloor">Null for every floor.</param>
    public static TrapOutcome CheckAll(ITrapGame game, PresetBook book, int? onlyFloor, bool isRefill = true)
    {
        var take = TakeAll(game, onlyFloor);

        if (take is TrapOutcome.NoGame or TrapOutcome.NotReady) return take;

        var bait = WithWorld(game, world => RebaitWhere(game, book, world, trap => CheckPlan.IsBaitWanted(trap, onlyFloor, isRefill)));

        return new TrapOutcome.Checked(take, bait);
    }

    public static TrapOutcome Take(ITrapGame game, long trapId) => WithTrap(game, trapId, (world, trap, type) =>
    {
        if (!TakeAllRoute.HasCatch(trap)) return new TrapOutcome.NothingToTake(trap.IsTakeQueued ? 1 : 0);

        return game.Take(trap.Id, type.HasStore) ? new TrapOutcome.Taking(1, 1, 0) : new TrapOutcome.Refused(type.Name);
    });

    public static TrapOutcome Dismantle(ITrapGame game, long trapId) => WithTrap(game, trapId, (_, trap, type) =>
        game.Dismantle(trap.Id) ? new TrapOutcome.Dismantling(type.Name) : new TrapOutcome.Refused(type.Name));

    private static TrapOutcome RebaitWhere(ITrapGame game, PresetBook book, TrapWorld world, Func<Trap, bool> isWanted, Action<long>? onRefused = null)
    {
        var reserved = new Dictionary<long, int>();
        int baited = 0, noBait = 0, refused = 0;

        foreach (var trap in FloorFirst(world))
        {
            if (!world.Catalog.Types.TryGetValue(trap.TypeId, out var type) || !BaitPick.CanBait(trap, type) || !isWanted(trap)) continue;

            var preset = book.PresetFor(world.SaveKey, trap);
            var choice = preset == null ? null : BaitPick.For(type, preset, book.Allowed, world.Stock, world.Catalog, reserved);

            if (choice == null)
            {
                noBait++;
                onRefused?.Invoke(trap.Id);
                continue;
            }

            if (game.SetBait(trap.Id, choice.InstanceId, choice.ItemId))
            {
                baited++;
                continue;
            }

            refused++;
            onRefused?.Invoke(trap.Id);
        }

        return baited + noBait + refused == 0 ? new TrapOutcome.NothingToBait() : new TrapOutcome.Baited(baited, noBait, refused);
    }

    /// <summary>The player's floor first, so the bait they carry goes to the traps around them before the rest.</summary>
    public static IEnumerable<Trap> FloorFirst(TrapWorld world) =>
        world.Traps.OrderBy(trap => trap.Floor == world.PlayerFloor ? 0 : 1).ThenBy(trap => trap.Floor).ThenBy(trap => trap.Id);

    private static bool HasStore(TrapWorld world, Trap trap) => world.Catalog.Types.TryGetValue(trap.TypeId, out var type) && type.HasStore;

    private static TrapOutcome WithWorld(ITrapGame game, Func<TrapWorld, TrapOutcome> press) => game.Read() switch
    {
        TrapRead.Ready ready => press(ready.World),
        TrapRead.NotReady notReady => new TrapOutcome.NotReady(notReady.Reason),
        TrapRead.NoGame => new TrapOutcome.NoGame(),
        _ => throw new InvalidOperationException("unknown trap read"),
    };

    private static TrapOutcome WithTrap(ITrapGame game, long trapId, Func<TrapWorld, Trap, TrapType, TrapOutcome> press) => WithWorld(game, world =>
    {
        var trap = world.Traps.FirstOrDefault(found => found.Id == trapId);

        return trap != null && world.Catalog.Types.TryGetValue(trap.TypeId, out var type) ? press(world, trap, type) : new TrapOutcome.Gone();
    });
}
