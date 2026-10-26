using System;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using GameCore.HotUpdate;
using HarmonyLib;
using TrapHub.Core;
using SoundGroup = TrapHub.Core.SoundGroup;

namespace TrapHub.Adapters;

/// <summary>
/// The catch sounds, as Quiet Traps handles them, with one more choice: a sound set to chime once plays for a few
/// seconds and is then stopped. Hooks the two FmodManager calls a catch sound goes through, and FMOD's own
/// createInstance and start, which a model's sound passes through without FmodManager. Not applied at all when Quiet
/// Traps is installed. Every hook swallows its own exceptions: a sound must never be the crash.
/// </summary>
internal static class SoundGate
{
    private const float OnceSeconds = 3f;

    // Harmony patches are static, so their state is too. FMOD sounds are created and started on Unity's main thread.
    private static readonly Dictionary<IntPtr, string?> PathsByDescription = new();
    private static readonly List<(EventInstance Instance, float StopAt)> Chiming = new();
    private static IReadOnlyDictionary<SoundGroup, SoundMode> _modes = new Dictionary<SoundGroup, SoundMode>();
    private static Action<string> _log = _ => { };
    private static Harmony? _harmony;
    private static bool _hasReportedFailure;
    private static float _now;

    // These hooks run on every sound the game makes, so they are put in only once a setting needs them, and each one
    // returns at once while no setting does.
    private static bool _isAnyMuted;
    private static bool _isAnyChanged;
    private static bool _areMuteHooksIn;
    private static bool _isStartHookIn;

    /// <summary>Hooks nothing yet: <see cref="Use"/> hooks what the chosen modes need, so with every sound Normal no sound pays for a hook.</summary>
    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;
        _harmony = harmony;
    }

    /// <summary>Called on the main thread at start and after each settings change.</summary>
    public static void Use(IReadOnlyDictionary<SoundGroup, SoundMode> modes)
    {
        _modes = modes;
        _isAnyMuted = false;
        _isAnyChanged = false;

        foreach (var mode in modes.Values)
        {
            _isAnyMuted |= mode == SoundMode.Muted;
            _isAnyChanged |= mode != SoundMode.Normal;
        }

        if (_harmony == null) return;

        if (_isAnyMuted && !_areMuteHooksIn)
        {
            _areMuteHooksIn = true;
            Hook(_harmony, typeof(FmodManager), "PlayEnv3dAction", prefix: nameof(SkipPlayIfMuted));
            Hook(_harmony, typeof(FmodManager), "PlayBattle3dAction", prefix: nameof(SkipPlayIfMuted));
            Hook(_harmony, typeof(EventDescription), nameof(EventDescription.createInstance), postfix: nameof(SilenceIfMuted));
        }

        if (_isAnyChanged && !_isStartHookIn)
        {
            _isStartHookIn = true;
            Hook(_harmony, typeof(EventInstance), nameof(EventInstance.start), prefix: nameof(SkipStartIfMuted));
        }
    }

    /// <summary>Called once a frame: stops each chime-once sound whose few seconds are up.</summary>
    public static void Tick(float now)
    {
        _now = now;

        for (var i = Chiming.Count - 1; i >= 0; i--)
        {
            if (now < Chiming[i].StopAt) continue;

            try
            {
                Chiming[i].Instance.stop(STOP_MODE.ALLOWFADEOUT);
            }
            catch (Exception ex)
            {
                ReportFailureOnce(ex);
            }

            Chiming.RemoveAt(i);
        }
    }

    private static void Hook(Harmony harmony, Type type, string method, string? prefix = null, string? postfix = null)
    {
        var name = $"{type.Name}.{method}";

        try
        {
            var original = AccessTools.Method(type, method) ?? throw new MissingMethodException(type.Name, method);

            harmony.Patch(original,
                prefix: prefix is null ? null : new HarmonyMethod(typeof(SoundGate), prefix),
                postfix: postfix is null ? null : new HarmonyMethod(typeof(SoundGate), postfix));
            _log("hooked " + name);
        }
        catch (Exception ex)
        {
            _log($"could not hook {name}, catch sound settings may not apply: {ex.Message}");
        }
    }

    /// <summary>The first argument of both FmodManager targets is the event path. False skips the sound.</summary>
    private static bool SkipPlayIfMuted(string __0)
    {
        if (!_isAnyMuted) return true;

        try
        {
            return Sounds.ModeOf(__0, _modes) != SoundMode.Muted;
        }
        catch (Exception ex)
        {
            ReportFailureOnce(ex);

            return true;
        }
    }

    private static void SilenceIfMuted(ref EventInstance __0)
    {
        if (!_isAnyMuted) return;

        try
        {
            if (Sounds.ModeOf(PathOf(__0), _modes) == SoundMode.Muted) __0.setVolume(0f);
        }
        catch (Exception ex)
        {
            ReportFailureOnce(ex);
        }
    }

    /// <summary>A muted sound is skipped and reported as started; a chime-once sound starts and is stopped by Tick.</summary>
    private static bool SkipStartIfMuted(ref EventInstance __instance, ref RESULT __result)
    {
        if (!_isAnyChanged) return true;

        try
        {
            switch (Sounds.ModeOf(PathOf(__instance), _modes))
            {
                case SoundMode.Muted:
                    __result = RESULT.OK;
                    return false;
                case SoundMode.Once:
                    Chiming.Add((__instance, _now + OnceSeconds));
                    return true;
                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            ReportFailureOnce(ex);

            return true;
        }
    }

    /// <summary>Cached per description: looking a path up asks FMOD for a string, and this runs on every sound.</summary>
    private static string? PathOf(EventInstance instance)
    {
        if (instance.getDescription(out var description) != RESULT.OK) return null;
        if (PathsByDescription.TryGetValue(description.handle, out var known)) return known;

        var path = description.getPath(out var found) == RESULT.OK ? found : null;

        PathsByDescription[description.handle] = path;

        return path;
    }

    private static void ReportFailureOnce(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log("sound check failed, further failures are not logged: " + ex);
    }
}
