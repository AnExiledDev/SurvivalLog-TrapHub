using System;
using System.Globalization;
using System.IO;
using TrapHub.Core;

namespace TrapHub.Adapters;

/// <summary>
/// trap-hub.json in BepInEx/config: the presets, the allowed bait and each save's per-trap choices. The game's save has
/// no room for a mod's data, so it lives here. A write goes to a temporary file first and is moved into place, so a
/// crash mid-write cannot leave half a file.
/// </summary>
internal sealed class PresetFileOnDisk : IPresetFile
{
    private readonly string _path;
    private readonly Func<DateTime> _now;

    public PresetFileOnDisk(string path, Func<DateTime> now)
    {
        _path = path;
        _now = now;
    }

    public string Name => _path;

    /// <summary>
    /// Missing only when there is no file. File.Exists also says false for a file the mod may not read, and that file
    /// must not be taken for a missing one, or a fresh book could be saved over it.
    /// </summary>
    public StoredText Read()
    {
        try
        {
            return new StoredText.Found(File.ReadAllText(_path));
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return new StoredText.Missing();
        }
        catch (Exception ex)
        {
            return new StoredText.Failed(ex.Message);
        }
    }

    public bool TryCopyAside(out string copy, out string why)
    {
        // Named for the second it was made, and never copied over a file already there, so a second bad file cannot
        // replace the first copy.
        copy = _path + ".unreadable-" + _now().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        try
        {
            File.Copy(_path, copy, overwrite: false);
        }
        catch (Exception ex)
        {
            why = ex.Message;

            return false;
        }

        why = "";

        return true;
    }

    public bool TryWrite(string text, out string why)
    {
        var temp = _path + ".tmp";

        try
        {
            File.WriteAllText(temp, text);
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex)
        {
            why = ex.Message;

            return false;
        }

        why = "";

        return true;
    }
}
