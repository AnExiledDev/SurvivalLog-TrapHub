using System;
using System.IO;

namespace TrapHub.Adapters;

/// <summary>
/// trap-hub.json in BepInEx/config: the presets, the allowed bait and each save's per-trap choices. The game's save has
/// no room for a mod's data, so it lives here. A write goes to a temporary file first and is moved into place, so a
/// crash mid-write cannot leave half a file.
/// </summary>
internal sealed class PresetFileOnDisk
{
    private readonly string _path;
    private readonly Action<string> _log;

    public PresetFileOnDisk(string path, Action<string> log)
    {
        _path = path;
        _log = log;
    }

    /// <summary>The file's text, "" when there is no file yet, or null when it is there but could not be read.</summary>
    public string? Read()
    {
        try
        {
            return File.Exists(_path) ? File.ReadAllText(_path) : "";
        }
        catch (Exception ex)
        {
            _log($"could not read the presets from {_path}: {ex.Message}");

            return null;
        }
    }

    public bool Replace(string text)
    {
        var temp = _path + ".tmp";

        try
        {
            File.WriteAllText(temp, text);
            File.Move(temp, _path, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {
            _log($"could not save the presets to {_path}: {ex.Message}");

            return false;
        }
    }

    /// <summary>Keeps a file that could not be read as presets beside it, so starting over never destroys someone's work.</summary>
    public void SetAside()
    {
        try
        {
            // A new name each time, so a second bad file never overwrites the first copy.
            var copy = $"{_path}.unreadable-{DateTime.Now:yyyyMMdd-HHmmss}";

            File.Copy(_path, copy, overwrite: true);
            _log($"{_path} could not be read as presets; copied it to {copy} and started fresh");
        }
        catch (Exception ex)
        {
            _log($"could not copy the unreadable presets aside: {ex.Message}");
        }
    }
}
