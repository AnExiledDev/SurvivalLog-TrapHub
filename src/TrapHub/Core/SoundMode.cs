using System;
using System.Collections.Generic;

namespace TrapHub.Core;

/// <summary>What a trap's catch sound does: play as the game plays it, play a few seconds then go quiet, or never play.</summary>
public enum SoundMode
{
    Normal,
    Once,
    Muted,
}

/// <summary>The three groups of catch sounds the settings name, by the game's FMOD event paths.</summary>
public enum SoundGroup
{
    LiveCaptureCage,
    SimpleBirdNet,
    OtherTraps,
}

public static class Sounds
{
    public static readonly IReadOnlyList<string> ModeNames = new[] { "Normal", "Once", "Muted" };

    /// <summary>A config or page value as a mode; anything unknown is Normal, so a bad value never silences a trap.</summary>
    public static SoundMode ParseMode(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "once" => SoundMode.Once,
        "muted" => SoundMode.Muted,
        _ => SoundMode.Normal,
    };

    public static string ModeName(SoundMode mode) => mode switch
    {
        SoundMode.Normal => "Normal",
        SoundMode.Once => "Once",
        SoundMode.Muted => "Muted",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "unknown sound mode"),
    };

    public static bool TryParseGroup(string? text, out SoundGroup group)
    {
        group = text switch
        {
            "cage" => SoundGroup.LiveCaptureCage,
            "net" => SoundGroup.SimpleBirdNet,
            "other" => SoundGroup.OtherTraps,
            _ => (SoundGroup)(-1),
        };

        return Enum.IsDefined(typeof(SoundGroup), group);
    }

    public static string GroupKey(SoundGroup group) => group switch
    {
        SoundGroup.LiveCaptureCage => "cage",
        SoundGroup.SimpleBirdNet => "net",
        SoundGroup.OtherTraps => "other",
        _ => throw new ArgumentOutOfRangeException(nameof(group), group, "unknown sound group"),
    };

    public static Phrase GroupName(SoundGroup group) => group switch
    {
        SoundGroup.LiveCaptureCage => Texts.SoundCage,
        SoundGroup.SimpleBirdNet => Texts.SoundNet,
        SoundGroup.OtherTraps => Texts.SoundOther,
        _ => throw new ArgumentOutOfRangeException(nameof(group), group, "unknown sound group"),
    };

    public static Phrase ModeLabel(SoundMode mode) => mode switch
    {
        SoundMode.Normal => Texts.ModeNormal,
        SoundMode.Once => Texts.ModeOnce,
        SoundMode.Muted => Texts.ModeMuted,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "unknown sound mode"),
    };

    /// <summary>What to do with an FMOD event path: its group's mode, or Normal for any sound that is not a trap's catch sound.</summary>
    public static SoundMode ModeOf(string? path, IReadOnlyDictionary<SoundGroup, SoundMode> modes) =>
        GroupOf(path) is { } group && modes.TryGetValue(group, out var mode) ? mode : SoundMode.Normal;

    /// <summary>The group an FMOD event path belongs to, or null when it is not a trap's catch sound.</summary>
    public static SoundGroup? GroupOf(string? path) => path switch
    {
        "event:/UI/xianjing_bushoulong" => SoundGroup.LiveCaptureCage,
        "event:/UI/xianjing_buniaolong" => SoundGroup.SimpleBirdNet,
        "event:/UI/xianjing_bushujia02" or "event:/UI/xianjing_Metalbushujia" => SoundGroup.OtherTraps,
        _ => null,
    };
}
