using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>The window and the HUD button as the player uses them, driven through page.js in Jint.</summary>
public class PageScriptTests
{
    private static readonly ViewSettings Settings = new(new Dictionary<SoundGroup, SoundMode>(), SoundsElsewhere: false, "Shift+J", "J");

    // Trap 1 holds a rabbit, 2 is empty on the player's floor, 3 is a net nothing allowed fits, 4 is baited downstairs.
    private static FakeGame Home() => new()
    {
        Traps = { T(1, prey: Rabbit, state: TrapState.Prey), T(2), T(3, floor: 2, type: Net, room: Attic), T(4, floor: -1, bait: Carrot) },
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

    private static string ListedTraps(PageScript page) => page.Text("panelButtons('pick').map(function (n) { return n.getAttribute('data-args'); }).join(',')");

    [Fact]
    public void The_traps_are_grouped_by_room_and_the_players_floor_is_marked()
    {
        var page = Opened();

        Assert.Equal("Pantry|Pantry|Attic", page.Text("panel().querySelectorAll('.th-grp-h').map(function (n) { return n.querySelector('b').textContent; }).join('|')"));
        Assert.Equal("1", page.Text("panel().querySelectorAll('.th-here').length"));
        Assert.Contains("Floor 1", page.Text("panel().querySelector('.th-here').parentNode.textContent"));
        Assert.Equal(4, (int)double.Parse(page.Text("panelButtons('pick').length")));
    }

    [Fact]
    public void The_show_choice_and_the_search_narrow_the_list()
    {
        var page = Opened();

        page.Click("panelButton('show','needs')");

        Assert.Equal("1,2,3", ListedTraps(page));

        page.Click("panelButton('show','armed')");

        Assert.Equal("4", ListedTraps(page));

        page.Click("panelButton('show','all')");
        page.Type("trapsearch", "attic");

        Assert.Equal("3", ListedTraps(page));
    }

    [Fact]
    public void Check_all_traps_names_what_it_will_do_and_sends_one_press()
    {
        var page = Opened();

        // One catch to collect, two empty traps the allowed carrots can fill; the net has nothing allowed to take.
        Assert.Equal("Check all traps (3)", page.Text("panelButton('checkall').textContent"));
        Assert.Equal("This floor only (3)", page.Text("panelButton('checkfloor').textContent"));

        page.Click("panelButton('checkall')");
        page.Click("panelButton('checkall')");
        page.Click("panelButton('checkfloor')");

        Assert.Equal(new PageCommand[] { new PageCommand.CheckAll(false) }, page.Sent);

        page.Status("Collecting from 1 trap.");
        page.Click("panelButton('checkfloor')");

        Assert.Equal(new PageCommand.CheckAll(true), page.Sent.Last());

        // Upstairs only the net waits, and nothing allowed fits it.
        var upstairs = Home();
        upstairs.PlayerFloor = 2;

        Assert.Equal("This floor only (0)", Opened(upstairs).Text("panelButton('checkfloor').textContent"));
    }

    [Fact]
    public void The_needs_list_says_what_waits_and_opens_the_traps_that_need_you()
    {
        var page = Opened();

        Assert.Contains("Rabbit", page.Text("panel().querySelector('.th-todo').textContent"));
        Assert.Equal(3, (int)double.Parse(page.Text("panel().querySelectorAll('.th-todo').length")));

        page.Click("panel().querySelectorAll('.th-todo')[0]");

        Assert.Equal("1,2,3", ListedTraps(page));
    }

    [Fact]
    public void Collect_only_and_bait_only_wait_for_the_answer_between_presses()
    {
        var page = Opened();

        page.Click("panelButton('takeall')");
        page.Click("panelButton('rebaitall')");

        Assert.Equal(new PageCommand[] { new PageCommand.TakeAll() }, page.Sent);

        page.Status("Collecting from 1 trap.");
        page.Click("panelButton('rebaitall')");

        Assert.IsType<PageCommand.RebaitAll>(page.Sent.Last());
    }

    [Fact]
    public void A_picked_trap_shows_its_detail_and_back_returns_to_the_needs_list()
    {
        var page = Opened();

        page.Click("panelButton('pick','1')");

        Assert.True(page.Is("panelButton('take','1')"));
        Assert.True(page.Is("panelButton('pick','1').className.indexOf('th-sel-on') >= 0"));
        Assert.False(page.Is("panelButton('checkall')"));

        page.Click("panelButton('back')");

        Assert.True(page.Is("panelButton('checkall')"));
        Assert.False(page.Is("panelButton('pick','1').className.indexOf('th-sel-on') >= 0"));
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

        ShowTab(page, "bait");
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

    /// <summary>
    /// The Default for row holds a whole box of trap chips. Laid out like the other setting rows, the chips took the
    /// full width and squeezed the label to nothing, so the trap names covered it.
    /// </summary>
    [Fact]
    public void The_default_for_label_keeps_its_width_beside_the_trap_chips()
    {
        var page = Opened();

        ShowTab(page, "bait");

        var css = page.Text("document.getElementById('th-style').textContent");
        const string row = "panelButtons('deftype')[0].closest('.th-setrow')";

        Assert.True(page.Is($"{row}.classList.contains('th-chiprow')"));
        Assert.Equal("Default for", page.Text($"{row}.querySelector('.th-lbl').firstChild.textContent"));
        Assert.Equal("1", page.Text("panel().querySelectorAll('.th-chiprow').length"));
        Assert.Contains(".th-setrow.th-chiprow .th-lbl{flex:none}", css);
        Assert.Contains(".th-setrow.th-chiprow .th-lbl b{white-space:nowrap}", css);
        Assert.Contains(".th-setrow.th-chiprow .th-chips{flex:1;min-width:0;margin:0}", css);
    }

    [Fact]
    public void A_refresh_while_typing_waits_until_the_box_loses_focus()
    {
        var page = Opened();

        ShowTab(page, "bait");
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

        ShowTab(page, "bait");
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
    public void Allowed_bait_starts_on_owned_bait_and_allows_one_or_all_shown()
    {
        var page = Opened();

        ShowTab(page, "bait");

        Assert.Equal(2, (int)double.Parse(page.Text("panel().querySelectorAll('.th-bait').length")));

        page.Click("panel().querySelectorAll('.th-bait')[1].querySelector('[data-act]')");
        page.Status("");
        page.Click("panelButtons('allow').filter(function (n) { return n.getAttribute('data-shown') === '1'; })[0]");

        Assert.Equal(new[] { (true, "202"), (true, "200,202") }, page.Sent.Cast<PageCommand.Allow>().Select(a => (a.IsAllowed, string.Join(",", a.ItemIds))));
    }

    [Fact]
    public void The_settings_name_both_keys_and_set_sounds_unless_quiet_traps_handles_them()
    {
        var page = Opened();

        page.Live(true, "Shift+J");
        ShowTab(page, "settings");

        Assert.Equal("J|Shift+J", page.Text("panel().querySelectorAll('.th-key').map(function (n) { return n.textContent; }).join('|')"));

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

    // ===== UI size (Exiled's Toolkit) =====

    /// <summary>A stand-in for the Toolkit's ExiledUi that records what each mod hands it.</summary>
    private const string FakeUi = "var handed = []; window.ExiledUi = { size: function (el, options) { handed.push({ el: el, options: options }); } }";

    private static void OpenWithToolkit(PageScript page)
    {
        var game = Home();

        page.Run(FakeUi);
        page.Catalog(CatalogView.Write(game.World.Catalog));
        page.Open(TrapView.Ready(game.World, TwoPresets(), Settings));
    }

    /// <summary>
    /// The mask fills the screen at any zoom, so a window measured by the mask would never be shrunk to fit; the window
    /// itself is what must fit. It is handed once, however often the window opens.
    /// </summary>
    [Fact]
    public void The_window_is_what_must_fit_the_screen()
    {
        var page = new PageScript();

        OpenWithToolkit(page);
        page.Run("window.__sltraphub.close()");
        page.Open(TrapView.Ready(Home().World, TwoPresets(), Settings));

        Assert.Equal("1", page.Text("handed.length"));
        Assert.Equal("TrapHub", page.Text("handed[0].options.mod"));
        Assert.Equal("th-root", page.Text("handed[0].el.id"));
        Assert.True(page.Is("handed[0].options.fit === document.querySelector('.th-win')"));
    }

    /// <summary>With the window closed a result line is the plugin's Toolkit toast, so the page draws no box of its own.</summary>
    [Fact]
    public void A_result_with_the_window_closed_draws_nothing_on_the_page()
    {
        var page = new PageScript();

        page.Run("window.__sltraphub.status('Rebaited 3 traps')");

        Assert.False(page.Is("document.getElementById('th-toast')"));
        Assert.False(page.Is("document.body.textContent.indexOf('Rebaited 3 traps') >= 0"));
    }

    /// <summary>The Toolkit drops a window taken out of its page, so the window drawn again in its place must be handed again.</summary>
    [Fact]
    public void A_window_drawn_again_after_the_page_lost_it_is_handed_again()
    {
        var page = new PageScript();

        OpenWithToolkit(page);
        page.Run("document.getElementById('th-root').remove()");
        page.Open(TrapView.Ready(Home().World, TwoPresets(), Settings));

        Assert.Equal("2", page.Text("handed.length"));
        Assert.True(page.Is("handed[1].el !== handed[0].el && handed[1].el === document.getElementById('th-root')"));
        Assert.True(page.Is("handed[1].options.fit === document.querySelector('.th-win')"));
    }

    /// <summary>The Toolkit's UI size script can load after its page API; the window waits in the list it reads when it starts.</summary>
    [Fact]
    public void Before_the_toolkit_has_loaded_the_window_waits_for_it()
    {
        var page = Opened();

        Assert.Equal("1", page.Text("window.__exiledUiWait.length"));
        Assert.Equal("th-root", page.Text("window.__exiledUiWait[0][0].id"));
        Assert.True(page.Is("window.__exiledUiWait[0][1].fit === document.querySelector('.th-win')"));
    }

    // ===== Load order: the game can run Trap Hub's script before the Toolkit's =====

    /// <summary>
    /// The plugin sends everything again when the page says it is ready, so the page says so only once the Toolkit's
    /// bridge is there; the HUD button then shows what the plugin sent while it waited.
    /// </summary>
    [Fact]
    public void Run_before_the_toolkit_the_page_says_ready_and_shows_the_hud_button_once_the_toolkit_arrives()
    {
        var page = new PageScript(isToolkitFirst: false);

        page.OpenHud();
        page.Live(true, "J");
        page.Badge(2);

        Assert.False(page.HasSaidReady);
        Assert.False(page.Is("hudButton()"));

        page.LoadToolkit();

        Assert.True(page.HasSaidReady);
        Assert.Equal("2", page.Text("hudButton().querySelector('.th-hud-badge').textContent"));

        page.Click("hudButton()");

        Assert.Equal(new PageCommand[] { new PageCommand.Toggle() }, page.Sent);
    }

    /// <summary>A window the plugin opened before the Toolkit's script ran gets its style and its UI size once it runs.</summary>
    [Fact]
    public void A_window_opened_before_the_toolkit_gets_its_style_and_size_when_the_toolkit_arrives()
    {
        var page = new PageScript(isToolkitFirst: false);

        page.Catalog(CatalogView.Write(Home().World.Catalog));
        page.Open(TrapView.Ready(Home().World, TwoPresets(), Settings));

        Assert.False(page.Is("document.getElementById('th-style')"));
        Assert.False(page.Is("window.__exiledUiWait"));

        page.LoadToolkit();

        Assert.True(page.Is("document.getElementById('th-style')"));
        Assert.Equal("1", page.Text("window.__exiledUiWait.length"));
        Assert.Equal("th-root", page.Text("window.__exiledUiWait[0][0].id"));
        Assert.Equal("TrapHub", page.Text("window.__exiledUiWait[0][1].mod"));
        Assert.True(page.Is("window.__exiledUiWait[0][1].fit === document.querySelector('.th-win')"));
    }
}
