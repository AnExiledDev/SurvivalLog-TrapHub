using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TrapHub.Core;

/// <summary>
/// The JSON for the additions to the game's own trap panel in the HUD: each trap's bait, catch and wear for its row,
/// and the Check traps plan for the button's tip. Items are ids the page names from the catalog (CatalogView).
/// </summary>
public static class HudView
{
    /// <summary>Skipped traps named in the tip; the rest are counted.</summary>
    public const int NamedSkips = 3;

    public static string NotReady() => "{\"ready\":false}";

    public static string Ready(TrapWorld world, PresetBook book, Func<int, int, int> floorCost, bool isRebait, string checkKey)
    {
        var plan = CheckPlan.For(world, book, floorCost, onlyFloor: null, isRebait);
        var next = plan.Bait.ToDictionary(pair => pair.Trap.Id, pair => pair.Choice);
        var touched = plan.Touched().ToList();

        using var stream = new MemoryStream();

        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteBoolean("ready", true);
            json.WriteBoolean("rebait", isRebait);
            json.WriteString("key", checkKey);
            json.WriteStartArray("traps");

            foreach (var trap in world.Traps.Where(trap => world.Catalog.Types.ContainsKey(trap.TypeId)).OrderBy(trap => trap.Id)) WriteTrap(json, world, trap, next);

            json.WriteEndArray();
            json.WriteStartArray("take");

            foreach (var group in plan.Take.GroupBy(trap => (trap.PreyId, world.Catalog.RoomName(trap.RoomId))))
            {
                json.WriteStartObject();
                json.WriteNumber("prey", Math.Max(0, group.Key.PreyId));
                json.WriteString("room", group.Key.Item2);
                json.WriteNumber("n", group.Sum(trap => trap.StoreCount > 0 ? trap.StoreCount : 1));
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("bait");

            foreach (var group in plan.Bait.Where(pair => pair.Choice != null && pair.Trap.PreyId <= 0).GroupBy(pair => (pair.Choice!.ItemId, world.Catalog.RoomName(pair.Trap.RoomId))))
            {
                json.WriteStartObject();
                json.WriteNumber("bait", group.Key.ItemId);
                json.WriteString("room", group.Key.Item2);
                json.WriteNumber("n", group.Count());
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("refill");

            foreach (var group in plan.Bait.Where(pair => pair.Choice != null && pair.Trap.PreyId > 0).GroupBy(pair => pair.Choice!.ItemId))
            {
                json.WriteStartObject();
                json.WriteNumber("bait", group.Key);
                json.WriteNumber("n", group.Count());
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteNumber("touched", touched.Count);
            json.WriteNumber("floors", touched.Select(trap => trap.Floor).Distinct().Count());
            json.WriteNumber("preyCount", plan.Take.Count);
            json.WriteNumber("baitCount", plan.Bait.Count(pair => pair.Choice != null));
            json.WriteNumber("worn", world.Traps.Count(trap => world.Catalog.Types.TryGetValue(trap.TypeId, out var type) && TrapView.IsWorn(trap, type)));
            json.WriteStartArray("skips");

            foreach (var (trap, why) in plan.Skipped.Take(NamedSkips))
            {
                json.WriteStartObject();
                json.WriteNumber("type", trap.TypeId);
                json.WriteString("room", world.Catalog.RoomName(trap.RoomId));
                json.WriteString("why", why == CheckSkip.Breaks ? "breaks" : "nobait");
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteNumber("skipMore", Math.Max(0, plan.Skipped.Count - NamedSkips));
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>One row's second line: the bait in it or the bait it gets next (0 for none left), its catch, and its wear.</summary>
    private static void WriteTrap(Utf8JsonWriter json, TrapWorld world, Trap trap, IReadOnlyDictionary<long, BaitChoice?> next)
    {
        var type = world.Catalog.Types[trap.TypeId];

        json.WriteStartObject();
        json.WriteString("id", trap.Id.ToString(CultureInfo.InvariantCulture));
        json.WriteNumber("bait", trap.BaitId);

        if (next.TryGetValue(trap.Id, out var choice)) json.WriteNumber("next", choice?.ItemId ?? 0);

        json.WriteNumber("prey", Math.Max(0, trap.PreyId));
        json.WriteNumber("store", trap.StoreCount);
        json.WriteBoolean("halted", trap.State == TrapState.Halted);

        if (TrapView.CatchesLeft(trap, type) is { } catches)
        {
            json.WriteNumber("catches", catches);
            json.WriteNumber("total", Math.Max(1, trap.MaxDurability / type.CatchCost));
        }

        json.WriteEndObject();
    }
}
