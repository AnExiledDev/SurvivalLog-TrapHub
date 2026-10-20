using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>The window and the HUD button as the player uses them, driven through page.js in Jint.</summary>
public class PageScriptTests
{
    private static readonly ViewSettings Settings = new(new Dictionary<SoundGroup, SoundMode>(), SoundsElsewhere: false, "J");

    private static FakeGame Home() => new()
    {
        Traps = { T(1, prey: Rabbit), T(2), T(3, floor: 2, type: Net), T(4, floor: -1, bait: Carrot) },
        Stock = { Bag(1, Carrot, 5), Bag(2, Meat, 2) },
    };

    private static PresetBook TwoPresets() => Book(new[] { Carrot }, Preset("p1", Step(Carrot)), Preset("p2", Step(Meat)));

    private static PageScript Opened(FakeGame? game = null, PresetBook? book = null)
    {
        game ??= Home();

        var page = new PageScript();

        page.Catalog(CatalogView.Write(game.World.Catalog));
        page.Open(TrapView.Ready(game.World, book ?? TwoPresets(), Settings));

        return page;
    }

    private static void ShowTab(PageScript page, string tab) => page.Click($"panelButton('tab','{tab}')");

    [Fact]
    public void The_traps_are_grouped_by_floor_and_the_players_floor_is_marked()
    {
        var page = Opened();

        Assert.Equal("Floor -1|Floor 1you are here|Floor 2", page.Text("panel().querySelectorAll('.th-sect').map(function (n) { return n.textContent; }).join('|')"));
        Assert.Equal(4, (int)double.Parse(page.Text("panelButtons('pick').length")));
    }

    [Fact]
    public void Take_all_asks_once_and_the_buttons_wait_for_the_answer()
    {
        var page = Opened();

        page.Click("panelButton('takeall')");
        page.Click("panelButton('takeall')");
        page.Click("panelButton('rebaitall')");

        Assert.Equal(new PageCommand[] { new PageCommand.TakeAll() }, page.Sent);

        page.Status("Collecting from 1 trap.");
        page.Click("panelButton('rebaitall')");

        Assert.IsType<PageCommand.RebaitAll>(page.Sent.Last());
    }

    [Fact]
    public void Dismantle_needs_a_second_press()
    {
        var page = Opened();

        page.Click("panelButton('pick','2')");
        page.Click("panelButton('dismantle','2')");

        Assert.Empty(page.Sent);
        Assert.Equal("Sure?", page.Text("panelButton('dismantle','2').textContent"));

        page.Click("panelButton('dismantle','2')");

        Assert.Equal(new PageCommand[] { new PageCommand.Dismantle(2) }, page.Sent);
    }

    [Fact]
    public void A_trap_takes_a_preset_of_its_own_from_its_detail()
    {
        var page = Opened();

        page.Click("panelButton('pick','2')");
        page.Click("panelButton('assign','p2|2')");

        Assert.Equal(new PageCommand.Assign("p2", new long[] { 2 }), page.Sent.Single(), new AssignComparer());
    }

    [Fact]
    public void Several_checked_traps_take_a_preset_together()
    {
        var page = Opened();

        page.Click("panel().querySelectorAll('.th-chk')[0]");
        page.Click("panel().querySelectorAll('.th-chk')[2]");
        page.Click("panelButton('assign','p2|2,4')");

        Assert.Equal(new PageCommand.Assign("p2", new long[] { 2, 4 }), page.Sent.Single(), new AssignComparer());
    }

    [Fact]
    public void In_a_game_never_saved_presets_cannot_be_picked_per_trap()
    {
        var game = Home();
        game.SaveKey = null;

        var page = Opened(game);

        page.Click("panelButton('pick','2')");
        page.Click("panelButton('assign','p2|2')");

        Assert.True(page.Is("panelButton('assign','p2|2').disabled"));
        Assert.Empty(page.Sent);
    }

    [Fact]
    public void An_edited_preset_is_saved_in_a_shape_the_plugin_reads_back()
    {
        var page = Opened();

        ShowTab(page, "presets");
        page.Click("panelButton('preset','p2')");
        page.Type("name", "Fox | bait");
        page.Blur("name");
        page.Click("panelButton('keepmore','0')");
        page.Click("panelButton('lastresort')");
        page.Click("panelButton('autoset','nearby')");
        page.Click("panelButton('savepreset')");

        var save = Assert.IsType<PageCommand.SavePreset>(page.Sent.Last());
        var edit = PresetJson.ReadEdit(save.Json, "x");

        Assert.NotNull(edit);
        Assert.Equal(("p2", "Fox | bait", true, AutoRebait.Nearby), (edit!.Preset.Id, edit.Preset.Name, edit.Preset.LastResort, edit.Preset.Auto));
        Assert.Equal(new[] { new BaitStep(Meat, 1) }, edit.Preset.Steps);
    }

    [Fact]
    public void A_refresh_while_typing_waits_until_the_box_loses_focus()
    {
        var page = Opened();

        ShowTab(page, "presets");
        page.Type("name", "Half typed");
        page.SetData(TrapView.Ready(Home().World, TwoPresets(), Settings));

        Assert.Equal("Half typed", page.Text("field('name').value"));
        Assert.Contains(page.Sent, c => c is PageCommand.Focus { IsTyping: true });

        page.Blur("name");

        Assert.IsType<PageCommand.Focus>(page.Sent.Last());
        Assert.False(((PageCommand.Focus)page.Sent.Last()).IsTyping);
    }

    [Fact]
    public void A_new_preset_is_selected_once_the_plugin_has_saved_it()
    {
        var game = Home();
        var book = TwoPresets();
        var page = Opened(game, book);

        ShowTab(page, "presets");
        page.Click("panelButton('newpreset')");
        page.Click("panelButton('savepreset')");

        var edit = PresetJson.ReadEdit(Assert.IsType<PageCommand.SavePreset>(page.Sent.Last()).Json, "x")!;
        var saved = book.Save(edit.Preset, edit.DefaultFor);

        page.Status("Saved.");
        page.SetData(TrapView.Ready(game.World, saved, Settings));

        Assert.Equal("New preset", page.Text("field('name').value"));
        Assert.True(page.Is("panelButton('deletepreset','p3')"));
    }

    [Fact]
    public void The_allowed_tab_starts_on_owned_bait_and_allows_one_or_all_shown()
    {
        var page = Opened();

        ShowTab(page, "allowed");

        Assert.Equal(2, (int)double.Parse(page.Text("panelButtons('allow').filter(function (n) { return !n.hasAttribute('data-shown'); }).length")));

        page.Click("panelButton('allow','1|202')");
        page.Status("");
        page.Click("panelButtons('allow').filter(function (n) { return n.getAttribute('data-shown') === '1'; })[0]");

        Assert.Equal(new[] { (true, "202"), (true, "200,202") }, page.Sent.Cast<PageCommand.Allow>().Select(a => (a.IsAllowed, string.Join(",", a.ItemIds))));
    }

    [Fact]
    public void Sounds_are_set_here_unless_quiet_traps_handles_them()
    {
        var page = Opened();

        ShowTab(page, "settings");
        page.Click("panelButton('sound','net|Once')");

        Assert.Equal(new PageCommand.Sound(SoundGroup.SimpleBirdNet, SoundMode.Once), page.Sent.Single());

        var elsewhere = new PageScript();
        elsewhere.Open(TrapView.Ready(Home().World, TwoPresets(), Settings with { SoundsElsewhere = true }));
        elsewhere.Click("panelButton('tab','settings')");

        Assert.True(elsewhere.Is("panelButton('sound','net|Once').disabled"));
    }

    [Fact]
    public void The_hud_button_shows_in_a_loaded_game_with_a_count_and_opens_the_window()
    {
        var page = new PageScript();

        page.OpenHud();
        page.Poll();

        Assert.False(page.Is("hudButton()"));

        page.Live(true, "J");
        page.Badge(3);

        Assert.Equal("3", page.Text("hudButton().querySelector('.th-hud-badge').textContent"));
        Assert.True(page.Is("hudPage().querySelector('.vertical-tools').firstChild === hudButton()"));
        Assert.Equal("10", page.Text("hudButton().getAttribute('data-etk-dock')"));

        page.Click("hudButton()");

        Assert.Equal(new PageCommand[] { new PageCommand.Toggle() }, page.Sent);

        page.Live(false, "J");

        Assert.False(page.Is("hudButton()"));
    }

    [Fact]
    public void The_window_speaks_the_games_language()
    {
        var page = new PageScript();

        page.Run(PageWords.Script(isChinese: true));
        page.Open(TrapView.Ready(Home().World, TwoPresets(), Settings));

        Assert.Equal(Texts.Page.Single(w => w.Key == "tabTraps").Phrase.Zh, page.Text("panelButton('tab','traps').textContent"));
    }

    private sealed class AssignComparer : IEqualityComparer<PageCommand>
    {
        public bool Equals(PageCommand? x, PageCommand? y) =>
            x is PageCommand.Assign a && y is PageCommand.Assign b && a.PresetId == b.PresetId && a.TrapIds.SequenceEqual(b.TrapIds);

        public int GetHashCode(PageCommand obj) => 0;
    }
}
