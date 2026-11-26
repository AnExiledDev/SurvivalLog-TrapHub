using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>
/// trap-hub.json and the presets in it: what is read, what is written, and that presets the mod could not read are
/// never saved over.
/// </summary>
public class PresetStoreTests
{
    private static readonly StoredText Locked = new StoredText.Failed("in use by another program");
    private static readonly PresetBook Saved = Book(new[] { Carrot }, Preset("p1", Step(Carrot)), Preset("p2"));
    private static readonly StoredText SavedFile = new StoredText.Found(PresetJson.WriteBook(Saved));
    private static readonly StoredText NotPresets = new StoredText.Found("not json {");

    private readonly List<string> _log = new();
    private double _now;

    private PresetStore Store(FakePresetFile file) => new(file, () => "First", () => _now, _log.Add);

    /// <summary>A change the player makes in the window: a new preset.</summary>
    private static PresetBook NewPresetIn(PresetBook book) => book.Save(Preset("", Step(Carrot)), Array.Empty<int>());

    [Fact]
    public void A_saved_file_gives_back_its_presets()
    {
        var store = Store(new FakePresetFile { Text = SavedFile });

        Assert.Equal(new[] { "p1", "p2" }, store.Book().Presets.Select(preset => preset.Id));
        Assert.True(store.IsRead);
    }

    [Fact]
    public void With_no_file_yet_nothing_is_copied_aside_and_the_first_change_is_saved()
    {
        var file = new FakePresetFile();
        var store = Store(file);
        var changed = NewPresetIn(store.Book());

        Assert.Equal(KeepResult.Saved, store.Keep(changed));
        Assert.Equal(new[] { "read", "write" }, file.Steps);
        Assert.Equal(PresetJson.WriteBook(changed), Assert.Single(file.Writes));
    }

    [Fact]
    public void While_another_program_has_the_file_open_it_is_read_again_only_every_10_seconds()
    {
        var file = new FakePresetFile { Text = Locked };
        var store = Store(file);
        var standIn = store.Book();

        _now = 9;

        Assert.Same(standIn, store.Book());
        Assert.Equal(1, file.Reads);
        Assert.False(store.IsRead);

        file.Text = SavedFile;
        _now = 10;

        Assert.Equal(2, store.Book().Presets.Count);
        Assert.Equal(2, file.Reads);
        Assert.True(store.IsRead);
    }

    [Fact]
    public void A_problem_that_lasts_is_logged_once()
    {
        var file = new FakePresetFile { Text = Locked };
        var store = Store(file);

        foreach (var at in new double[] { 0, 10, 20 })
        {
            _now = at;
            store.Book();
        }

        Assert.Equal(3, file.Reads);
        Assert.Contains("in use by another program", Assert.Single(_log));
    }

    [Fact]
    public void A_change_made_while_the_file_could_not_be_read_is_not_written()
    {
        var file = new FakePresetFile { Text = Locked };
        var store = Store(file);

        Assert.Equal(KeepResult.NotRead, store.Keep(NewPresetIn(store.Book())));
        Assert.Empty(file.Writes);
        Assert.False(store.IsRead);
    }

    [Fact]
    public void Retry_reads_the_file_at_once_and_only_until_the_presets_are_read()
    {
        var file = new FakePresetFile { Text = Locked };
        var store = Store(file);

        store.Book();
        file.Text = SavedFile;
        store.Retry();

        Assert.Equal(2, store.Book().Presets.Count);

        store.Retry();

        Assert.Equal(2, file.Reads);
    }

    [Fact]
    public void A_file_that_is_not_a_preset_file_is_copied_aside_then_replaced_with_a_fresh_book()
    {
        var file = new FakePresetFile { Text = NotPresets };
        var store = Store(file);
        var book = store.Book();

        Assert.Equal(new[] { "read", "copy", "write" }, file.Steps);
        Assert.Equal(PresetJson.WriteBook(book), Assert.Single(file.Writes));
        Assert.Equal("First", Assert.Single(book.Presets).Name);
        Assert.Contains(_log, line => line.Contains("trap-hub.json.unreadable-1"));
    }

    [Fact]
    public void A_file_that_is_not_a_preset_file_and_cannot_be_copied_aside_is_never_written_over()
    {
        var file = new FakePresetFile { Text = NotPresets, CanCopy = false };
        var store = Store(file);

        Assert.Equal(KeepResult.NotRead, store.Keep(NewPresetIn(store.Book())));
        Assert.Empty(file.Writes);
        Assert.False(store.IsRead);
        Assert.Contains("access denied", Assert.Single(_log));
    }

    [Fact]
    public void A_change_that_cannot_be_written_is_used_until_the_game_closes()
    {
        var file = new FakePresetFile { Text = SavedFile, CanWrite = false };
        var store = Store(file);
        var changed = NewPresetIn(store.Book());

        Assert.Equal(KeepResult.NotSaved, store.Keep(changed));
        Assert.Same(changed, store.Book());
    }
}
