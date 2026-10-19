using System;
using System.Collections.Generic;
using System.Linq;

namespace TrapHub.Core;

/// <summary>Where a preset may take bait from.</summary>
public enum BaitSource
{
    Bag,
    BagThenHome,
}

/// <summary>When Trap Hub re-baits an empty trap on its own: never, while the player is on the trap's floor, or always.</summary>
public enum AutoRebait
{
    Off,
    Nearby,
    Always,
}

/// <summary>One bait in a preset's order. The step is skipped once the player has KeepAtLeast or fewer of it left.</summary>
public sealed record BaitStep(int ItemId, int KeepAtLeast);

/// <summary>
/// A named order of baits. Re-bait walks the steps in order and uses the first one the trap accepts, the player has
/// allowed and owns enough of; with LastResort on, any allowed universal bait the player owns comes after the steps.
/// </summary>
public sealed record BaitPreset(
    string Id,
    string Name,
    IReadOnlyList<BaitStep> Steps,
    bool LastResort,
    BaitSource Source,
    bool SpoilFirst,
    AutoRebait Auto);

/// <summary>
/// Every preset, the allowed bait list, which preset each trap type uses by default and the per-trap choices of each
/// save. Bait is blocked until allowed: nothing outside Allowed ever goes in a trap.
/// </summary>
public sealed record PresetBook(
    IReadOnlyList<BaitPreset> Presets,
    IReadOnlySet<int> Allowed,
    IReadOnlyDictionary<int, string> TypeDefaults,
    IReadOnlyDictionary<string, IReadOnlyDictionary<long, string>> Overrides)
{
    public const string FirstPresetId = "p1";

    private static readonly int[] KnownTrapTypes = { 1, 2, 3, 4, 5 };

    /// <summary>What a new player starts with: one preset of any universal bait for every trap type, and nothing allowed yet.</summary>
    public static PresetBook First(string firstPresetName) => new(
        new[] { new BaitPreset(FirstPresetId, firstPresetName, Array.Empty<BaitStep>(), LastResort: true, BaitSource.BagThenHome, SpoilFirst: true, AutoRebait.Off) },
        new HashSet<int>(),
        KnownTrapTypes.ToDictionary(type => type, _ => FirstPresetId),
        new Dictionary<string, IReadOnlyDictionary<long, string>>());

    public BaitPreset? Find(string? presetId) => presetId == null ? null : Presets.FirstOrDefault(preset => preset.Id == presetId);

    /// <summary>The trap's own choice in this save, else its type's default; null when neither names a preset that exists.</summary>
    public BaitPreset? PresetFor(string? saveKey, Trap trap) => Find(OverrideFor(saveKey, trap.Id)) ?? Find(TypeDefaults.TryGetValue(trap.TypeId, out var id) ? id : null);

    public string? OverrideFor(string? saveKey, long trapId) =>
        saveKey != null && Overrides.TryGetValue(saveKey, out var traps) && traps.TryGetValue(trapId, out var id) ? id : null;

    public IEnumerable<int> TypesDefaultingTo(string presetId) => TypeDefaults.Where(pair => pair.Value == presetId).Select(pair => pair.Key).OrderBy(type => type);

    /// <summary>
    /// Adds the preset, or replaces the one with its id. A preset with no id gets a new one. It becomes the default for
    /// exactly the trap types given: a type it is given takes it from any other preset, and a type it no longer lists
    /// keeps no default.
    /// </summary>
    public PresetBook Save(BaitPreset preset, IEnumerable<int> defaultFor)
    {
        var saved = string.IsNullOrEmpty(preset.Id) ? preset with { Id = NewId() } : preset;
        var presets = Presets.ToList();
        var at = presets.FindIndex(existing => existing.Id == saved.Id);

        if (at >= 0) presets[at] = saved;
        else presets.Add(saved);

        var types = defaultFor.ToHashSet();
        var defaults = TypeDefaults.Where(pair => pair.Value != saved.Id && !types.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var type in types) defaults[type] = saved.Id;

        return this with { Presets = presets, TypeDefaults = defaults };
    }

    /// <summary>Removes the preset; the trap types and traps that used it fall back to no preset and their type's default.</summary>
    public PresetBook Delete(string presetId) => this with
    {
        Presets = Presets.Where(preset => preset.Id != presetId).ToList(),
        TypeDefaults = TypeDefaults.Where(pair => pair.Value != presetId).ToDictionary(pair => pair.Key, pair => pair.Value),
        Overrides = Overrides.ToDictionary(
            save => save.Key,
            save => (IReadOnlyDictionary<long, string>)save.Value.Where(pair => pair.Value != presetId).ToDictionary(pair => pair.Key, pair => pair.Value)),
    };

    /// <summary>Sets the traps' own preset in this save; an empty preset id puts them back on their type's default.</summary>
    public PresetBook Assign(string saveKey, IEnumerable<long> trapIds, string presetId)
    {
        var overrides = Overrides.ToDictionary(save => save.Key, save => save.Value);
        var traps = overrides.TryGetValue(saveKey, out var existing) ? existing.ToDictionary(pair => pair.Key, pair => pair.Value) : new Dictionary<long, string>();

        foreach (var trapId in trapIds)
        {
            if (presetId.Length == 0) traps.Remove(trapId);
            else traps[trapId] = presetId;
        }

        overrides[saveKey] = traps;

        return this with { Overrides = overrides };
    }

    public PresetBook Allow(IEnumerable<int> itemIds, bool isAllowed)
    {
        var allowed = Allowed.ToHashSet();

        foreach (var itemId in itemIds)
        {
            if (isAllowed) allowed.Add(itemId);
            else allowed.Remove(itemId);
        }

        return this with { Allowed = allowed };
    }

    private string NewId()
    {
        var taken = Presets.Select(preset => preset.Id).ToHashSet();

        for (var n = Presets.Count + 1; ; n++)
        {
            var id = "p" + n;

            if (!taken.Contains(id)) return id;
        }
    }
}
