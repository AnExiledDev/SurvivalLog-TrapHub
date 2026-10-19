using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TrapHub.Core;

/// <summary>A message from the page. Everything else the page could send is ignored.</summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|traphub|";

    private PageCommand()
    {
    }

    public sealed record Toggle : PageCommand;

    public sealed record Close : PageCommand;

    public sealed record Ready : PageCommand;

    public sealed record NoScript : PageCommand;

    /// <summary>A diagnostic line from the page script, for the log.</summary>
    public sealed record Note(string Text) : PageCommand;

    /// <summary>A text box in the page gained or lost focus, so the game's keys should pause or resume.</summary>
    public sealed record Focus(bool IsTyping) : PageCommand;

    public sealed record TakeAll : PageCommand;

    public sealed record TakeFloor : PageCommand;

    public sealed record RebaitAll : PageCommand;

    public sealed record Rebait(long TrapId) : PageCommand;

    public sealed record Take(long TrapId) : PageCommand;

    public sealed record Dismantle(long TrapId) : PageCommand;

    /// <summary>An empty preset id puts the traps back on their type's default.</summary>
    public sealed record Assign(string PresetId, IReadOnlyList<long> TrapIds) : PageCommand;

    public sealed record SavePreset(string Json) : PageCommand;

    public sealed record DeletePreset(string PresetId) : PageCommand;

    public sealed record Allow(bool IsAllowed, IReadOnlyList<int> ItemIds) : PageCommand;

    public sealed record Sound(SoundGroup Group, SoundMode Mode) : PageCommand;

    public static bool IsOurs(string? message) => message != null && message.StartsWith(Prefix, System.StringComparison.Ordinal);

    public static PageCommand? Parse(string? message)
    {
        if (!IsOurs(message)) return null;

        var body = message!.Substring(Prefix.Length);

        // The preset's JSON may hold '|' in its name, so it is everything after the first one.
        if (body.StartsWith("savepreset|", System.StringComparison.Ordinal)) return new SavePreset(body.Substring("savepreset|".Length));

        if (body.StartsWith("diag|", System.StringComparison.Ordinal)) return new Note(body.Substring("diag|".Length));

        var parts = body.Split('|');

        return (parts[0], parts.Length) switch
        {
            ("toggle", 1) => new Toggle(),
            ("close", 1) => new Close(),
            ("ready", 1) => new Ready(),
            ("noscript", 1) => new NoScript(),
            ("focus", 2) when parts[1] is "0" or "1" => new Focus(parts[1] == "1"),
            ("takeall", 1) => new TakeAll(),
            ("takefloor", 1) => new TakeFloor(),
            ("rebaitall", 1) => new RebaitAll(),
            ("rebait", 2) when TryId(parts[1], out var id) => new Rebait(id),
            ("take", 2) when TryId(parts[1], out var id) => new Take(id),
            ("dismantle", 2) when TryId(parts[1], out var id) => new Dismantle(id),
            ("assign", 3) when TryIds(parts[2], out var ids) => new Assign(parts[1], ids),
            ("deletepreset", 2) when parts[1].Length > 0 => new DeletePreset(parts[1]),
            ("allow", 3) when parts[1] is "0" or "1" && TryItems(parts[2], out var items) => new Allow(parts[1] == "1", items),
            ("sound", 3) when Sounds.TryParseGroup(parts[1], out var group) => new Sound(group, Sounds.ParseMode(parts[2])),
            _ => null,
        };
    }

    private static bool TryId(string text, out long id) => long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out id);

    private static bool TryIds(string text, out IReadOnlyList<long> ids)
    {
        var parsed = new List<long>();

        foreach (var part in text.Split(',').Where(part => part.Length > 0))
        {
            if (!TryId(part, out var id))
            {
                ids = parsed;
                return false;
            }

            parsed.Add(id);
        }

        ids = parsed;

        return parsed.Count > 0;
    }

    private static bool TryItems(string text, out IReadOnlyList<int> items)
    {
        var parsed = new List<int>();

        foreach (var part in text.Split(',').Where(part => part.Length > 0))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            {
                items = parsed;
                return false;
            }

            parsed.Add(id);
        }

        items = parsed;

        return parsed.Count > 0;
    }
}
