using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrapHub.Core;

/// <summary>A preset as the page's editor sends it back: the preset, and the trap types it should be the default for.</summary>
public sealed record PresetEdit(BaitPreset Preset, IReadOnlyList<int> DefaultFor);

/// <summary>
/// The preset file (trap-hub.json in BepInEx/config) and the preset editor's message, both JSON. Reading is forgiving:
/// a field that is missing or of the wrong kind takes its default, and a step naming no item is dropped.
/// </summary>
public static class PresetJson
{
    private const int MaxNameLength = 40;
    private const int MaxKeep = 9999;

    /// <summary>The book in the file, or null when the text is not a preset file at all.</summary>
    public static PresetBook? ReadBook(string text)
    {
        if (Parse(text) is not JsonObject root || root["presets"] is not JsonArray presets) return null;

        var defaults = new Dictionary<int, string>();
        var overrides = new Dictionary<string, IReadOnlyDictionary<long, string>>();

        if (root["typeDefaults"] is JsonObject typeDefaults)
        {
            foreach (var (key, value) in typeDefaults)
            {
                if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var type) && Text(value) is { Length: > 0 } id) defaults[type] = id;
            }
        }

        if (root["saves"] is JsonObject saves)
        {
            foreach (var (save, traps) in saves)
            {
                if (traps is JsonObject trapObject) overrides[save] = ReadOverrides(trapObject);
            }
        }

        return new PresetBook(
            presets.Select(node => node is JsonObject preset ? ReadPreset(preset) : null).Where(preset => preset is { Id.Length: > 0 }).Select(preset => preset!).ToList(),
            Ints(root["allowed"]).ToHashSet(),
            defaults,
            overrides);
    }

    public static string WriteBook(PresetBook book)
    {
        using var stream = new MemoryStream();

        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteNumber("version", 1);
            json.WriteStartArray("presets");

            foreach (var preset in book.Presets) WritePreset(json, preset);

            json.WriteEndArray();
            json.WriteStartArray("allowed");

            foreach (var id in book.Allowed.OrderBy(id => id)) json.WriteNumberValue(id);

            json.WriteEndArray();
            json.WriteStartObject("typeDefaults");

            foreach (var (type, id) in book.TypeDefaults.OrderBy(pair => pair.Key)) json.WriteString(type.ToString(CultureInfo.InvariantCulture), id);

            json.WriteEndObject();
            json.WriteStartObject("saves");

            foreach (var (save, traps) in book.Overrides.Where(save => save.Value.Count > 0).OrderBy(save => save.Key, StringComparer.Ordinal))
            {
                json.WriteStartObject(save);

                foreach (var (trap, id) in traps.OrderBy(pair => pair.Key)) json.WriteString(trap.ToString(CultureInfo.InvariantCulture), id);

                json.WriteEndObject();
            }

            json.WriteEndObject();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>The editor's message, or null when it is not a preset. A blank name takes the fallback name.</summary>
    public static PresetEdit? ReadEdit(string text, string fallbackName)
    {
        if (Parse(text) is not JsonObject root) return null;

        var preset = ReadPreset(root);

        if (preset == null) return null;
        if (preset.Name.Length == 0) preset = preset with { Name = fallbackName };

        return new PresetEdit(preset, Ints(root["types"]).Distinct().ToList());
    }

    public static void WritePreset(Utf8JsonWriter json, BaitPreset preset)
    {
        json.WriteStartObject();
        json.WriteString("id", preset.Id);
        json.WriteString("name", preset.Name);
        json.WriteStartArray("steps");

        foreach (var step in preset.Steps)
        {
            json.WriteStartObject();
            json.WriteNumber("item", step.ItemId);
            json.WriteNumber("keep", step.KeepAtLeast);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteBoolean("lastResort", preset.LastResort);
        json.WriteString("source", preset.Source == BaitSource.Bag ? "bag" : "home");
        json.WriteBoolean("spoilFirst", preset.SpoilFirst);
        json.WriteString("auto", AutoName(preset.Auto));
        json.WriteEndObject();
    }

    public static string AutoName(AutoRebait auto) => auto switch
    {
        AutoRebait.Off => "off",
        AutoRebait.Nearby => "nearby",
        AutoRebait.Always => "always",
        _ => throw new ArgumentOutOfRangeException(nameof(auto), auto, "unknown auto re-bait mode"),
    };

    private static BaitPreset? ReadPreset(JsonObject node)
    {
        var name = (Text(node["name"]) ?? "").Trim();
        var steps = new List<BaitStep>();

        if (node["steps"] is JsonArray stepNodes)
        {
            foreach (var step in stepNodes.OfType<JsonObject>())
            {
                var itemId = Int(step["item"]) ?? 0;

                if (itemId > 0 && steps.All(existing => existing.ItemId != itemId)) steps.Add(new BaitStep(itemId, Math.Clamp(Int(step["keep"]) ?? 0, 0, MaxKeep)));
            }
        }

        return new BaitPreset(
            Text(node["id"]) ?? "",
            name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name,
            steps,
            Bool(node["lastResort"]) ?? true,
            Text(node["source"]) == "bag" ? BaitSource.Bag : BaitSource.BagThenHome,
            Bool(node["spoilFirst"]) ?? true,
            Text(node["auto"]) switch
            {
                "nearby" => AutoRebait.Nearby,
                "always" => AutoRebait.Always,
                _ => AutoRebait.Off,
            });
    }

    private static IReadOnlyDictionary<long, string> ReadOverrides(JsonObject traps)
    {
        var read = new Dictionary<long, string>();

        foreach (var (key, value) in traps)
        {
            if (long.TryParse(key, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var trapId) && Text(value) is { Length: > 0 } id) read[trapId] = id;
        }

        return read;
    }

    private static JsonNode? Parse(string text)
    {
        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<int> Ints(JsonNode? node) =>
        node is JsonArray array ? array.Select(Int).Where(id => id is > 0).Select(id => id!.Value) : Enumerable.Empty<int>();

    private static int? Int(JsonNode? node) => node is JsonValue value && value.TryGetValue<double>(out var number) && number is >= int.MinValue and <= int.MaxValue ? (int)number : null;

    private static bool? Bool(JsonNode? node) => node is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : null;

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
