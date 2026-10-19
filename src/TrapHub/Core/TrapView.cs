using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TrapHub.Core;

/// <summary>The settings the window shows next to the traps: catch sounds, whether Quiet Traps owns them, and the panel key.</summary>
public sealed record ViewSettings(IReadOnlyDictionary<SoundGroup, SoundMode> Sounds, bool SoundsElsewhere, string Key);

/// <summary>
/// The JSON the window draws: a count of what needs the player, every trap with its catch chance and the bait a
/// re-bait would use, the presets and the allowed list. Names of items live in the catalog, sent once (CatalogView).
/// </summary>
public static class TrapView
{
    private const int TopPrey = 3;

    public static string NotReady(string note, ViewSettings settings) => Write(json =>
    {
        json.WriteBoolean("ready", false);
        json.WriteString("note", note);
        WriteSettings(json, settings);
    });

    public static string Ready(TrapWorld world, PresetBook book, ViewSettings settings) => Write(json =>
    {
        var nextBait = NextBait(world, book);
        var rows = world.Traps
            .Where(trap => world.Catalog.Types.ContainsKey(trap.TypeId))
            .OrderBy(trap => trap.Floor)
            .ThenBy(trap => trap.RoomId)
            .ThenBy(trap => trap.Id)
            .ToList();

        json.WriteBoolean("ready", true);
        json.WriteBoolean("saved", world.SaveKey != null);
        json.WriteNumber("floor", world.PlayerFloor);
        WriteCounts(json, world, rows, nextBait);
        json.WriteStartArray("traps");

        foreach (var trap in rows) WriteTrap(json, world, book, trap, nextBait);

        json.WriteEndArray();
        json.WriteStartArray("presets");

        foreach (var preset in book.Presets) WritePreset(json, book, preset);

        json.WriteEndArray();
        json.WriteStartObject("owned");

        foreach (var (itemId, count) in Owned(world.Stock)) json.WriteNumber(itemId.ToString(CultureInfo.InvariantCulture), count);

        json.WriteEndObject();
        json.WriteStartArray("allowed");

        foreach (var itemId in book.Allowed.OrderBy(id => id)) json.WriteNumberValue(itemId);

        json.WriteEndArray();
        WriteSettings(json, settings);
    });

    /// <summary>The number on the HUD button: traps holding a catch to collect, and traps waiting for bait.</summary>
    public static int NeedsYou(TrapWorld world) => world.Traps.Count(trap =>
        world.Catalog.Types.TryGetValue(trap.TypeId, out var type) && (TakeAllRoute.HasCatch(trap) || BaitPick.CanBait(trap, type)));

    /// <summary>How many catches the trap has left before it breaks, or null for a trap that does not wear.</summary>
    public static int? CatchesLeft(Trap trap, TrapType type) => type.CatchCost > 0 ? Math.Max(0, trap.Durability) / type.CatchCost : null;

    /// <summary>A trap is worn when it breaks on its next catch or the one after.</summary>
    public static bool IsWorn(Trap trap, TrapType type) => CatchesLeft(trap, type) is <= 1;

    /// <summary>
    /// The bait Re-bait All would put in each empty trap right now, in the same order and from the same shared stock,
    /// so two traps never both show the last portion of something.
    /// </summary>
    public static IReadOnlyDictionary<long, BaitChoice?> NextBait(TrapWorld world, PresetBook book)
    {
        var reserved = new Dictionary<long, int>();
        var next = new Dictionary<long, BaitChoice?>();

        foreach (var trap in TrapRun.FloorFirst(world))
        {
            if (!world.Catalog.Types.TryGetValue(trap.TypeId, out var type) || !BaitPick.CanBait(trap, type)) continue;

            var preset = book.PresetFor(world.SaveKey, trap);

            next[trap.Id] = preset == null ? null : BaitPick.For(type, preset, book.Allowed, world.Stock, world.Catalog, reserved);
        }

        return next;
    }

    private static void WriteCounts(Utf8JsonWriter json, TrapWorld world, IReadOnlyList<Trap> rows, IReadOnlyDictionary<long, BaitChoice?> nextBait)
    {
        var takeable = rows.Where(TakeAllRoute.HasCatch).ToList();

        json.WriteStartObject("counts");
        json.WriteNumber("total", rows.Count);
        json.WriteNumber("prey", rows.Count(trap => trap.PreyId > 0 || trap.StoreCount > 0));
        json.WriteNumber("noBait", rows.Count(trap => BaitPick.CanBait(trap, world.Catalog.Types[trap.TypeId])));
        json.WriteNumber("worn", rows.Count(trap => IsWorn(trap, world.Catalog.Types[trap.TypeId])));
        json.WriteNumber("halted", rows.Count(trap => trap.State == TrapState.Halted));
        json.WriteNumber("take", takeable.Count);
        json.WriteNumber("takeFloor", takeable.Count(trap => trap.Floor == world.PlayerFloor));
        json.WriteNumber("rebait", nextBait.Values.Count(choice => choice != null));
        json.WriteEndObject();
    }

    private static void WriteTrap(Utf8JsonWriter json, TrapWorld world, PresetBook book, Trap trap, IReadOnlyDictionary<long, BaitChoice?> nextBait)
    {
        var type = world.Catalog.Types[trap.TypeId];
        var ownPreset = book.Find(book.OverrideFor(world.SaveKey, trap.Id));
        var preset = ownPreset ?? book.PresetFor(world.SaveKey, trap);
        var isEmpty = nextBait.TryGetValue(trap.Id, out var next);
        var oddsBait = trap.BaitId != 0 ? trap.BaitId : next?.ItemId ?? 0;

        json.WriteStartObject();
        json.WriteString("id", trap.Id.ToString(CultureInfo.InvariantCulture));
        json.WriteNumber("type", trap.TypeId);
        json.WriteNumber("floor", trap.Floor);
        json.WriteString("room", world.Catalog.RoomName(trap.RoomId));
        json.WriteString("state", StateName(trap.State));
        json.WriteNumber("prey", trap.PreyId);
        json.WriteNumber("bait", trap.BaitId);
        json.WriteNumber("store", trap.StoreCount);
        json.WriteNumber("dur", trap.Durability);
        json.WriteNumber("max", trap.MaxDurability);

        if (CatchesLeft(trap, type) is { } catches) json.WriteNumber("catches", catches);
        else json.WriteNull("catches");

        json.WriteBoolean("worn", IsWorn(trap, type));
        json.WriteBoolean("queued", trap.IsTakeQueued);
        json.WriteBoolean("canTake", TakeAllRoute.HasCatch(trap));
        json.WriteBoolean("canBait", isEmpty);

        if (isEmpty) json.WriteNumber("next", next?.ItemId ?? 0);

        json.WriteString("preset", preset?.Id ?? "");
        json.WriteBoolean("own", ownPreset != null);
        WriteOdds(json, world, type, trap, oddsBait);
        json.WriteEndObject();
    }

    private static void WriteOdds(Utf8JsonWriter json, TrapWorld world, TrapType type, Trap trap, int baitId)
    {
        var odds = Odds.For(type, baitId, trap.RoomId, world.Catalog, world.Bonus);

        json.WriteNumber("chance", Percent(odds.Catch));
        json.WriteStartArray("odds");

        foreach (var (preyId, chance) in odds.Prey.Take(TopPrey))
        {
            json.WriteStartObject();
            json.WriteNumber("prey", preyId);
            json.WriteNumber("pct", Percent(chance));
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    private static void WritePreset(Utf8JsonWriter json, PresetBook book, BaitPreset preset)
    {
        using var buffer = new MemoryStream();

        using (var single = new Utf8JsonWriter(buffer))
        {
            PresetJson.WritePreset(single, preset);
        }

        using var parsed = JsonDocument.Parse(buffer.ToArray());

        json.WriteStartObject();

        foreach (var property in parsed.RootElement.EnumerateObject()) property.WriteTo(json);

        json.WriteStartArray("types");

        foreach (var type in book.TypesDefaultingTo(preset.Id)) json.WriteNumberValue(type);

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteSettings(Utf8JsonWriter json, ViewSettings settings)
    {
        json.WriteStartObject("sounds");

        foreach (var group in (SoundGroup[])Enum.GetValues(typeof(SoundGroup)))
        {
            json.WriteString(Sounds.GroupKey(group), Sounds.ModeName(settings.Sounds.TryGetValue(group, out var mode) ? mode : SoundMode.Normal));
        }

        json.WriteEndObject();
        json.WriteBoolean("soundsElsewhere", settings.SoundsElsewhere);
        json.WriteString("key", settings.Key);
    }

    private static IEnumerable<(int ItemId, int Count)> Owned(IEnumerable<BaitUnit> stock) =>
        stock.GroupBy(unit => unit.ItemId).Select(group => (group.Key, group.Sum(unit => unit.Count))).OrderBy(item => item.Key);

    private static int Percent(double chance) => (int)Math.Round(chance * 100d, MidpointRounding.AwayFromZero);

    private static string StateName(TrapState state) => state switch
    {
        TrapState.Armed => "armed",
        TrapState.Prey => "prey",
        TrapState.NoBait => "nobait",
        TrapState.Halted => "halted",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "unknown trap state"),
    };

    private static string Write(Action<Utf8JsonWriter> body)
    {
        using var stream = new MemoryStream();

        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            body(json);
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

/// <summary>The catalog as the window needs it, sent once per game and language: names, icons, what each trap takes and catches.</summary>
public static class CatalogView
{
    public static string Write(TrapCatalog catalog)
    {
        using var stream = new MemoryStream();

        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteStartArray("types");

            foreach (var type in catalog.Types.Values.OrderBy(type => type.Id)) WriteType(json, type);

            json.WriteEndArray();
            json.WriteStartArray("baits");

            foreach (var bait in catalog.Baits.Values.OrderBy(bait => bait.Id)) WriteBait(json, catalog, bait);

            json.WriteEndArray();
            json.WriteStartArray("prey");

            foreach (var prey in catalog.Prey.Values.OrderBy(prey => prey.Id))
            {
                json.WriteStartObject();
                json.WriteNumber("id", prey.Id);
                json.WriteString("name", prey.Name);
                json.WriteString("icon", prey.Icon);
                json.WriteBoolean("rare", prey.IsRare);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteType(Utf8JsonWriter json, TrapType type)
    {
        json.WriteStartObject();
        json.WriteNumber("id", type.Id);
        json.WriteString("name", type.Name);
        json.WriteString("icon", type.Icon);
        json.WriteBoolean("store", type.HasStore);
        json.WriteStartArray("accepts");

        foreach (var itemId in type.AcceptedBait.OrderBy(id => id)) json.WriteNumberValue(itemId);

        json.WriteEndArray();
        json.WriteStartArray("catches");

        foreach (var prey in type.Prey) json.WriteNumberValue(prey.PreyId);

        json.WriteEndArray();
        json.WriteEndObject();
    }

    /// <summary>A bait, with the prey it draws more than usual (a multiplier above 1), strongest first.</summary>
    private static void WriteBait(Utf8JsonWriter json, TrapCatalog catalog, BaitItem bait)
    {
        json.WriteStartObject();
        json.WriteNumber("id", bait.Id);
        json.WriteString("name", bait.Name);
        json.WriteString("icon", bait.Icon);
        json.WriteString("kind", bait.Kind.ToString().ToLowerInvariant());
        json.WriteStartArray("draws");

        foreach (var (preyId, _) in bait.Coefficients.Where(pair => pair.Value > 1f).OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key))
        {
            if (catalog.Prey.ContainsKey(preyId)) json.WriteNumberValue(preyId);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }
}
