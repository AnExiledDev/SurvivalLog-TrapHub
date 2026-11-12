using System.Collections.Generic;

namespace TrapHub.Core;

/// <summary>The game's HUD states for a trap (TrapManager.GetTrapHudStatus).</summary>
public enum TrapState
{
    Armed = 0,
    Prey = 1,
    NoBait = 2,
    Halted = 3,
}

/// <summary>The four kinds of bait the game names in each bait's effect line.</summary>
public enum BaitKind
{
    Other,
    Universal,
    Preference,
    Specialized,
    Balanced,
}

public readonly record struct Spot(float X, float Y, float Z);

/// <summary>One prey a trap type can catch, with the base weight the game rolls it with.</summary>
public sealed record PreyRate(int PreyId, float BaseRate);

/// <summary>
/// A kind of trap, from Config_Trap: what it accepts as bait, what it can catch, and what a catch costs it. A type
/// with a store (the Auto Trap Cage) keeps its catches inside and is emptied with the game's take-from-store action.
/// </summary>
public sealed record TrapType(
    int Id,
    string Name,
    string? Icon,
    bool HasStore,
    int CatchCost,
    float EmptyWeight,
    IReadOnlyList<PreyRate> Prey,
    IReadOnlySet<int> AcceptedBait);

/// <summary>A placed trap as the game holds it right now.</summary>
public sealed record Trap(
    long Id,
    int TypeId,
    int RoomId,
    int Floor,
    Spot Position,
    TrapState State,
    int PreyId,
    int BaitId,
    int StoreCount,
    int Durability,
    int MaxDurability,
    bool IsTakeQueued);

/// <summary>One stack of a bait item the player owns, in the bag or in home storage. HoursLeft is null for food that never spoils.</summary>
public sealed record BaitUnit(long InstanceId, int ItemId, int Count, bool IsInBag, int? HoursLeft);

/// <summary>A bait item, with the multiplier it gives each prey (a prey it does not name gets 1, the same as the game).</summary>
public sealed record BaitItem(int Id, string Name, string? Icon, BaitKind Kind, IReadOnlyDictionary<int, float> Coefficients)
{
    public float CoefficientFor(int preyId) => Coefficients.TryGetValue(preyId, out var coefficient) ? coefficient : 1f;
}

public sealed record PreyItem(int Id, string Name, string? Icon, bool IsRare);

/// <summary>Everything that does not change while a save is played: trap types, baits, prey, rooms and room biases.</summary>
public sealed record TrapCatalog(
    IReadOnlyDictionary<int, TrapType> Types,
    IReadOnlyDictionary<int, BaitItem> Baits,
    IReadOnlyDictionary<int, PreyItem> Prey,
    IReadOnlyDictionary<int, string> RoomNames,
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, float>> RoomBias)
{
    public float RoomCoefficient(int roomId, int preyId) =>
        RoomBias.TryGetValue(roomId, out var bias) && bias.TryGetValue(preyId, out var coefficient) ? coefficient : 1f;

    public string RoomName(int roomId) => RoomNames.TryGetValue(roomId, out var name) ? name : $"Room {roomId}";

    public string ItemName(int itemId) =>
        Baits.TryGetValue(itemId, out var bait) ? bait.Name : Prey.TryGetValue(itemId, out var prey) ? prey.Name : $"Item {itemId}";
}

/// <summary>The player's talents and levels that change catch odds.</summary>
public sealed record OddsBonus(float BaitTalent, float EmptyReduction, float RareBonus)
{
    public static readonly OddsBonus None = new(0f, 0f, 1f);
}

/// <summary>A read of the home's traps and the player's bait at one moment.</summary>
public sealed record TrapWorld(
    TrapCatalog Catalog,
    IReadOnlyList<Trap> Traps,
    IReadOnlyList<BaitUnit> Stock,
    OddsBonus Bonus,
    int PlayerFloor,
    Spot PlayerPosition,
    string? SaveKey);

/// <summary>What a read of the game found: no loaded game, a game not ready to manage traps (with why), or the traps.</summary>
public abstract record TrapRead
{
    private TrapRead()
    {
    }

    public sealed record NoGame : TrapRead;

    /// <param name="IsStillLoading">The game is fine, the mod is still reading its tables a slice per frame; ask again next frame.</param>
    public sealed record NotReady(string Reason, bool IsStillLoading = false) : TrapRead;

    public sealed record Ready(TrapWorld World) : TrapRead;
}

/// <summary>The game's side of every press. Each call fails soft and answers false when the game refused or threw.</summary>
public interface ITrapGame
{
    TrapRead Read();

    /// <summary>Puts one portion of the given bait stack in an empty trap, as the game's trap panel does.</summary>
    bool SetBait(long trapId, long instanceId, int itemId);

    /// <summary>Lines up a walk to the trap to take its prey (or everything in an Auto Trap Cage's store).</summary>
    bool Take(long trapId, bool fromStore);

    bool Dismantle(long trapId);

    /// <summary>How many floor changes the walk from one floor to another takes, as the game routes it.</summary>
    int FloorCost(int fromFloor, int toFloor);
}
