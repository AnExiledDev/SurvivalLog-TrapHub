using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>The presets, which trap types and traps use them, the allowed bait list, and the file they are kept in.</summary>
public class PresetBookTests
{
    [Fact]
    public void A_new_player_starts_with_one_universal_preset_for_every_trap_and_no_bait_allowed()
    {
        var book = PresetBook.First("Any universal bait");

        Assert.Empty(book.Allowed);
        Assert.True(book.Presets.Single().LastResort);
        Assert.Equal("Any universal bait", book.PresetFor("slot-1", T(1, type: Net))?.Name);
    }

    [Fact]
    public void A_saved_new_preset_gets_its_own_id_and_takes_the_trap_types_it_names()
    {
        var book = PresetBook.First("Any").Save(Preset("", Step(Meat)), new[] { Cage });
        var added = book.Presets.Last();

        Assert.NotEqual(PresetBook.FirstPresetId, added.Id);
        Assert.Equal(added.Id, book.PresetFor(null, T(1, type: Cage))?.Id);
        Assert.Equal(PresetBook.FirstPresetId, book.PresetFor(null, T(1, type: Net))?.Id);
    }

    [Fact]
    public void Saving_a_preset_without_a_type_it_was_default_for_leaves_that_type_with_none()
    {
        var book = PresetBook.First("Any").Save(Preset(PresetBook.FirstPresetId, Step(Carrot)), new[] { Net });

        Assert.Null(book.PresetFor(null, T(1, type: Cage)));
        Assert.Equal(PresetBook.FirstPresetId, book.PresetFor(null, T(1, type: Net))?.Id);
        Assert.Equal(Carrot, book.Presets.Single().Steps.Single().ItemId);
    }

    [Fact]
    public void A_trap_assigned_its_own_preset_keeps_it_in_that_save_only()
    {
        var book = Book(new[] { Carrot }, Preset("p1"), Preset("p2")).Assign("slot-1", new[] { 7L }, "p2");

        Assert.Equal("p2", book.PresetFor("slot-1", T(7))?.Id);
        Assert.Equal("p1", book.PresetFor("slot-2", T(7))?.Id);
        Assert.Equal("p1", book.Assign("slot-1", new[] { 7L }, "").PresetFor("slot-1", T(7))?.Id);
    }

    [Fact]
    public void Deleting_a_preset_puts_its_traps_back_on_their_types_default()
    {
        var book = Book(new[] { Carrot }, Preset("p1"), Preset("p2")).Assign("slot-1", new[] { 7L }, "p2").Delete("p2");

        Assert.Equal("p1", book.PresetFor("slot-1", T(7))?.Id);
        Assert.Null(book.OverrideFor("slot-1", 7));
    }

    [Fact]
    public void Allowing_and_blocking_change_only_the_named_baits()
    {
        var book = Book(new[] { Carrot }, Preset("p1")).Allow(new[] { Meat, Seeds }, isAllowed: true).Allow(new[] { Carrot }, isAllowed: false);

        Assert.Equal(new[] { Seeds, Meat }, book.Allowed.OrderBy(id => id));
    }

    [Fact]
    public void The_file_keeps_everything_the_player_set()
    {
        var book = Book(new[] { Carrot, Meat }, Preset("p1", Step(Meat, keep: 3)) with { Source = BaitSource.Bag, SpoilFirst = false, Auto = AutoRebait.Nearby, LastResort = true }, Preset("p2"))
            .Assign("slot-1", new[] { 7L }, "p2");

        var read = PresetJson.ReadBook(PresetJson.WriteBook(book));

        Assert.NotNull(read);
        Assert.Equal(book.Presets, read!.Presets, new PresetComparer());
        Assert.Equal(book.Allowed.OrderBy(id => id), read.Allowed.OrderBy(id => id));
        Assert.Equal(book.TypeDefaults.OrderBy(p => p.Key), read.TypeDefaults.OrderBy(p => p.Key));
        Assert.Equal("p2", read.OverrideFor("slot-1", 7));
    }

    [Fact]
    public void A_file_that_is_not_a_preset_file_is_not_read_as_an_empty_one()
    {
        Assert.Null(PresetJson.ReadBook("not json"));
        Assert.Null(PresetJson.ReadBook("{\"something\":1}"));
    }

    [Fact]
    public void The_editor_message_is_read_forgivingly()
    {
        var edit = PresetJson.ReadEdit(
            "{\"id\":\"\",\"name\":\"  \",\"steps\":[{\"item\":202,\"keep\":-4},{\"item\":202,\"keep\":1},{\"item\":0},{\"item\":200,\"keep\":123456}],\"source\":\"bag\",\"auto\":\"weird\",\"types\":[1,1,3]}",
            "New preset");

        Assert.NotNull(edit);
        Assert.Equal("New preset", edit!.Preset.Name);
        Assert.Equal(new[] { new BaitStep(Meat, 0), new BaitStep(Carrot, 9999) }, edit.Preset.Steps);
        Assert.Equal(BaitSource.Bag, edit.Preset.Source);
        Assert.Equal(AutoRebait.Off, edit.Preset.Auto);
        Assert.Equal(new[] { 1, 3 }, edit.DefaultFor);
    }

    [Fact]
    public void A_trap_choice_in_a_game_never_saved_is_refused_with_why()
    {
        var edit = BookEdits.Assign(Book(new[] { Carrot }, Preset("p1")), saveKey: null, new PageCommand.Assign("p1", new[] { 7L }), isChinese: false);

        Assert.Null(edit.Book);
        Assert.Equal(Texts.NoSave.En, edit.Message);
    }

    private sealed class PresetComparer : IEqualityComparer<BaitPreset>
    {
        public bool Equals(BaitPreset? x, BaitPreset? y) =>
            x != null && y != null && x.Id == y.Id && x.Name == y.Name && x.Steps.SequenceEqual(y.Steps) && x.LastResort == y.LastResort &&
            x.Source == y.Source && x.SpoilFirst == y.SpoilFirst && x.Auto == y.Auto;

        public int GetHashCode(BaitPreset obj) => obj.Id.GetHashCode();
    }
}
