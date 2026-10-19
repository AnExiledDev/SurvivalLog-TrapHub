using System.Linq;

namespace TrapHub.Core;

/// <summary>What a preset command did: the book to keep (null when nothing changed) and the line for the status bar.</summary>
public sealed record BookEdit(PresetBook? Book, string Message);

/// <summary>The window's preset, assignment and allowed-bait commands, applied to the book. Saving it is the caller's job.</summary>
public static class BookEdits
{
    public static BookEdit SavePreset(PresetBook book, string json, bool isChinese)
    {
        var edit = PresetJson.ReadEdit(json, Texts.NewPresetName.Pick(isChinese));

        if (edit == null) return new BookEdit(null, Texts.PresetBad.Pick(isChinese));

        return new BookEdit(book.Save(edit.Preset, edit.DefaultFor), Texts.PresetSaved.Say(isChinese, edit.Preset.Name));
    }

    public static BookEdit DeletePreset(PresetBook book, string presetId, bool isChinese)
    {
        var preset = book.Find(presetId);

        return preset == null ? new BookEdit(null, Texts.PresetBad.Pick(isChinese)) : new BookEdit(book.Delete(presetId), Texts.PresetDeleted.Say(isChinese, preset.Name));
    }

    /// <summary>A trap's own preset is kept per save, so a game never saved has nowhere to keep it.</summary>
    public static BookEdit Assign(PresetBook book, string? saveKey, PageCommand.Assign assign, bool isChinese)
    {
        if (saveKey == null) return new BookEdit(null, Texts.NoSave.Pick(isChinese));

        if (assign.PresetId.Length == 0) return new BookEdit(book.Assign(saveKey, assign.TrapIds, ""), Texts.AssignedDefault.Say(isChinese, assign.TrapIds.Count));

        var preset = book.Find(assign.PresetId);

        return preset == null
            ? new BookEdit(null, Texts.PresetBad.Pick(isChinese))
            : new BookEdit(book.Assign(saveKey, assign.TrapIds, preset.Id), Texts.AssignedOwn.Say(isChinese, assign.TrapIds.Count, preset.Name));
    }

    public static BookEdit Allow(PresetBook book, PageCommand.Allow allow, bool isChinese)
    {
        var count = allow.ItemIds.Distinct().Count();
        var message = (allow.IsAllowed ? Texts.AllowedCount : Texts.BlockedCount).Say(isChinese, count);

        return new BookEdit(book.Allow(allow.ItemIds, allow.IsAllowed), message);
    }
}
