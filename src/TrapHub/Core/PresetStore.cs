using System;

namespace TrapHub.Core;

/// <summary>
/// The file the presets are kept in. Each call says what went wrong instead of logging it, so the store can log a
/// problem once while it lasts.
/// </summary>
public interface IPresetFile
{
    /// <summary>Where the presets are kept, for the log.</summary>
    string Name { get; }

    StoredText Read();

    /// <summary>Copies the file beside itself under a new name each time, never over a file already there.</summary>
    bool TryCopyAside(out string copy, out string why);

    /// <summary>Replaces the file whole, so a failed write leaves the old text as it was.</summary>
    bool TryWrite(string text, out string why);
}

/// <summary>What reading the file found. Failed is not Missing: presets that are there but unread must not be written over.</summary>
public abstract record StoredText
{
    private StoredText()
    {
    }

    public sealed record Missing : StoredText;

    public sealed record Found(string Text) : StoredText;

    public sealed record Failed(string Why) : StoredText;
}

/// <summary>What became of a change made in the window.</summary>
public enum KeepResult
{
    Saved,

    /// <summary>The change is used, but only until the game closes: the file could not be written.</summary>
    NotSaved,

    /// <summary>The change is dropped: it was made on the stand-in, not the player's presets.</summary>
    NotRead,
}

/// <summary>
/// The presets, read from their file the first time they are needed. A file that is not a preset file is copied aside
/// and replaced by a fresh book, so nothing anyone wrote is lost and the next start does not copy it again. While the
/// file cannot be read, or cannot be copied aside, a fresh book stands in for the HUD and the presses, the problem is
/// logged once, and nothing is written over the file.
/// </summary>
public sealed class PresetStore
{
    // Nothing tells the mod when another program lets go of the file, so it is tried again after a while: rarely
    // enough that the HUD refresh does not read it every two seconds, soon enough that auto re-bait gets the real
    // presets back without the player doing anything. Texts.PresetsNotRead names this number.
    private const double RetrySeconds = 10;

    private readonly IPresetFile _file;
    private readonly Func<string> _firstPresetName;
    private readonly Func<double> _now;
    private readonly Action<string> _log;

    private PresetBook? _book;
    private PresetBook? _standIn;
    private double _retryAt;
    private string _problem = "";

    public PresetStore(IPresetFile file, Func<string> firstPresetName, Func<double> now, Action<string> log)
    {
        _file = file;
        _firstPresetName = firstPresetName;
        _now = now;
        _log = log;
    }

    /// <summary>False while the stand-in is used. The window then says why instead of showing it, so nothing is changed on it.</summary>
    public bool IsRead => _book != null;

    /// <summary>The player's presets, or the stand-in while the file cannot be read.</summary>
    public PresetBook Book()
    {
        if (_book != null) return _book;
        if (_standIn != null && _now() < _retryAt) return _standIn;

        return Load();
    }

    /// <summary>Tries the file at once if it could not be read, for the player's presses and for opening the window.</summary>
    public void Retry()
    {
        if (_book == null) Load();
    }

    /// <summary>A change made on the stand-in is dropped: writing it would replace the presets that could not be read.</summary>
    public KeepResult Keep(PresetBook changed)
    {
        if (_book == null) return KeepResult.NotRead;

        _book = changed;

        return Write(changed) ? KeepResult.Saved : KeepResult.NotSaved;
    }

    private PresetBook Load()
    {
        var read = _file.Read();

        if (read is StoredText.Failed failed) return StandIn($"could not read the presets from {_file.Name}: {failed.Why}");

        var text = read is StoredText.Found found ? found.Text : "";

        if (string.IsNullOrWhiteSpace(text)) return Use(PresetBook.First(_firstPresetName()));
        if (PresetJson.ReadBook(text) is { } saved) return Use(saved);
        if (!_file.TryCopyAside(out var copy, out var why)) return StandIn($"{_file.Name} could not be read as presets or copied aside, so it is left as it is: {why}");

        _log($"{_file.Name} could not be read as presets; copied it to {copy} and started fresh");

        var fresh = Use(PresetBook.First(_firstPresetName()));

        Write(fresh);

        return fresh;
    }

    private PresetBook Use(PresetBook book)
    {
        _book = book;

        _log($"presets: {book.Presets.Count}, allowed bait: {book.Allowed.Count}");

        return book;
    }

    /// <summary>The same stand-in each time, so the HUD is not drawn again for it, and a problem that lasts is logged once.</summary>
    private PresetBook StandIn(string problem)
    {
        if (problem != _problem) _log(problem);

        _problem = problem;
        _retryAt = _now() + RetrySeconds;

        return _standIn ??= PresetBook.First(_firstPresetName());
    }

    private bool Write(PresetBook book)
    {
        if (_file.TryWrite(PresetJson.WriteBook(book), out var why)) return true;

        _log($"could not save the presets to {_file.Name}: {why}");

        return false;
    }
}
