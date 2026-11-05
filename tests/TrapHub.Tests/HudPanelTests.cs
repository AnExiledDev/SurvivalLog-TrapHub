using System.Text.Json;
using ExiledToolkit.Web;
using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>The additions to the game's own HUD trap panel: the Check traps plan, each row's second line, and the controls.</summary>
public class HudPanelTests
{
    private static PresetBook CarrotBook() => Book(new[] { Carrot }, Preset("p1", Step(Carrot)));

    // 1 holds a rabbit with one catch left, 2 holds a rabbit and is fine, 3 is empty, 4 is a net with no bait it may use,
    // 5 is a full auto cage.
    private static FakeGame Home() => new()
    {
        Traps =
        {
            T(1, prey: Rabbit, dur: 10), T(2, prey: Rabbit), T(3), T(4, floor: 2, type: Net, room: Attic),
            T(5, type: AutoCage, store: 3, state: TrapState.Halted),
        },
        Stock = { Bag(1, Carrot, 5) },
    };

    private static JsonElement Hud(FakeGame game, bool isRebait) =>
        JsonDocument.Parse(HudView.Ready(game.World, CarrotBook(), game.FloorCost, isRebait, "J")).RootElement;

    private static JsonElement TrapOf(JsonElement hud, long id) => hud.GetProperty("traps").EnumerateArray().Single(t => t.GetProperty("id").GetString() == id.ToString());

    [Fact]
    public void With_rebait_on_the_plan_refills_the_traps_it_empties_except_the_one_a_catch_would_break()
    {
        var game = Home();
        var plan = CheckPlan.For(game.World, CarrotBook(), game.FloorCost, onlyFloor: null, isRefill: true);

        Assert.Equal(new[] { 1L, 2L, 5L }, plan.Take.Select(t => t.Id).OrderBy(id => id));
        Assert.Equal(new[] { 2L, 3L }, plan.Bait.Where(b => b.Choice != null).Select(b => b.Trap.Id).OrderBy(id => id));
        Assert.Contains((1L, CheckSkip.Breaks), plan.Skipped.Select(s => (s.Trap.Id, s.Why)));
        Assert.Contains((4L, CheckSkip.NoBait), plan.Skipped.Select(s => (s.Trap.Id, s.Why)));
    }

    [Fact]
    public void With_rebait_off_the_plan_baits_only_traps_already_empty_and_warns_of_no_break()
    {
        var game = Home();
        var plan = CheckPlan.For(game.World, CarrotBook(), game.FloorCost, onlyFloor: null, isRefill: false);

        Assert.Equal(new[] { 3L }, plan.Bait.Where(b => b.Choice != null).Select(b => b.Trap.Id));
        Assert.DoesNotContain(plan.Skipped, s => s.Why == CheckSkip.Breaks);
    }

    [Fact]
    public void Check_traps_with_rebait_off_leaves_a_trap_holding_a_catch_unbaited()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit), T(2) }, Stock = { Bag(1, Carrot, 5) } };

        var outcome = TrapRun.CheckAll(game, CarrotBook(), onlyFloor: null, isRefill: false);

        Assert.Equal(new[] { 1L }, game.Taken.Select(t => t.TrapId));
        Assert.Equal(new[] { 2L }, game.Baited.Select(b => b.TrapId));
        Assert.Equal(2, TrapMessages.CheckedCount(outcome));
    }

    [Fact]
    public void The_press_and_the_tip_pick_the_same_traps_to_bait()
    {
        foreach (var isRebait in new[] { true, false })
        {
            var planned = Home();
            var pressed = Home();
            var plan = CheckPlan.For(planned.World, CarrotBook(), planned.FloorCost, null, isRebait);

            TrapRun.CheckAll(pressed, CarrotBook(), null, isRebait);

            Assert.Equal(plan.Bait.Where(b => b.Choice != null).Select(b => b.Trap.Id).OrderBy(id => id), pressed.Baited.Select(b => b.TrapId).OrderBy(id => id));
        }
    }

    [Fact]
    public void The_count_for_the_popup_is_what_was_collected_plus_what_was_baited()
    {
        Assert.Equal(5, TrapMessages.CheckedCount(new TrapOutcome.Checked(new TrapOutcome.Taking(2, 2, 0), new TrapOutcome.Baited(3, 0, 0))));
        Assert.Equal(0, TrapMessages.CheckedCount(new TrapOutcome.NothingToTake(1)));
        Assert.Equal("Check x5", Texts.CheckPop.Say(false, 5));
    }

    [Fact]
    public void The_hud_data_gives_each_row_its_bait_catch_and_wear_and_counts_the_plan()
    {
        var hud = Hud(Home(), isRebait: true);

        Assert.True(hud.GetProperty("rebait").GetBoolean());
        Assert.Equal("J", hud.GetProperty("key").GetString());
        Assert.Equal(1, TrapOf(hud, 1).GetProperty("catches").GetInt32());
        Assert.Equal(10, TrapOf(hud, 1).GetProperty("total").GetInt32());
        Assert.False(TrapOf(hud, 1).TryGetProperty("next", out _));
        Assert.Equal(Carrot, TrapOf(hud, 3).GetProperty("next").GetInt32());
        Assert.Equal(0, TrapOf(hud, 4).GetProperty("next").GetInt32());
        Assert.True(TrapOf(hud, 5).GetProperty("halted").GetBoolean());
        Assert.False(TrapOf(hud, 5).TryGetProperty("catches", out _));
        Assert.Equal(3, hud.GetProperty("preyCount").GetInt32());
        Assert.Equal(2, hud.GetProperty("baitCount").GetInt32());
        Assert.Equal(1, hud.GetProperty("worn").GetInt32());
        Assert.Equal(1, hud.GetProperty("refill").EnumerateArray().Single().GetProperty("n").GetInt32());
        Assert.Equal(new[] { "breaks", "nobait" }, hud.GetProperty("skips").EnumerateArray().Select(s => s.GetProperty("why").GetString()).OrderBy(w => w));
        Assert.Equal(2 + 3, hud.GetProperty("take").EnumerateArray().Sum(t => t.GetProperty("n").GetInt32()));
        Assert.Equal(4, hud.GetProperty("touched").GetInt32());
    }

    [Fact]
    public void Only_three_skipped_traps_are_named_and_the_rest_counted()
    {
        var game = new FakeGame { Traps = { T(1, type: Net), T(2, type: Net), T(3, type: Net), T(4, type: Net), T(5, type: Net) } };
        var hud = Hud(game, isRebait: true);

        Assert.Equal(HudView.NamedSkips, hud.GetProperty("skips").GetArrayLength());
        Assert.Equal(2, hud.GetProperty("skipMore").GetInt32());
    }

    [Fact]
    public void Rebait_traps_baits_every_trap_that_can_take_bait_a_trap_holding_its_catch_included_and_collects_nothing()
    {
        var game = Home();

        var outcome = TrapRun.RebaitAll(game, CarrotBook());

        Assert.Empty(game.Taken);
        Assert.Equal(new[] { (2L, Carrot), (3L, Carrot) }, game.Baited.OrderBy(b => b.TrapId));
        Assert.Equal(new TrapOutcome.Baited(2, 1, 0), outcome);
    }

    [Fact]
    public void The_rebait_tip_names_the_same_traps_and_bait_the_press_uses_whether_rebait_on_check_is_on_or_off()
    {
        foreach (var isRebait in new[] { true, false })
        {
            var hud = Hud(Home(), isRebait);
            var fill = hud.GetProperty("fill").EnumerateArray().Single();

            Assert.Equal(Carrot, fill.GetProperty("bait").GetInt32());
            Assert.Equal("Pantry", fill.GetProperty("room").GetString());
            Assert.Equal(2, fill.GetProperty("n").GetInt32());
            Assert.Equal(2, hud.GetProperty("fillCount").GetInt32());
            Assert.Equal(1, hud.GetProperty("fillNone").GetInt32());
        }
    }

    [Fact]
    public void Rebait_traps_follows_the_preset_order_and_does_not_promise_more_bait_than_there_is()
    {
        // Meat first, then carrots; no meat in hand and only two carrots for three empty cages.
        var book = Book(new[] { Carrot, Meat }, Preset("p1", Step(Meat), Step(Carrot)));
        var game = new FakeGame { Traps = { T(1), T(2), T(3) }, Stock = { Bag(1, Carrot, 2) } };

        var plan = CheckPlan.RebaitAll(game.World, book);

        Assert.Equal(new int?[] { Carrot, Carrot, null }, plan.Select(p => p.Choice?.ItemId));

        var outcome = TrapRun.RebaitAll(game, book);

        Assert.Equal(new[] { (1L, Carrot), (2L, Carrot) }, game.Baited);
        Assert.Equal(new TrapOutcome.Baited(2, 1, 0), outcome);
    }

    [Fact]
    public void With_every_trap_baited_rebait_traps_has_nothing_to_do()
    {
        var game = new FakeGame { Traps = { T(1, bait: Carrot), T(2, prey: Rabbit, bait: Carrot) }, Stock = { Bag(1, Carrot, 5) } };
        var hud = Hud(game, isRebait: false);

        Assert.Empty(CheckPlan.RebaitAll(game.World, CarrotBook()));
        Assert.Equal(0, hud.GetProperty("fill").GetArrayLength());
        Assert.Equal(0, hud.GetProperty("fillCount").GetInt32());
        Assert.Equal(0, hud.GetProperty("fillNone").GetInt32());
        Assert.Equal(new TrapOutcome.NothingToBait(), TrapRun.RebaitAll(game, CarrotBook()));
        Assert.Empty(game.Baited);
    }

    private static PageScript Panel(FakeGame game, bool isRebait, string armedWords = "Deploying Defenses", string checkKey = "J", bool isToolkitFirst = true)
    {
        var page = new PageScript(isToolkitFirst);

        page.Catalog(CatalogView.Write(game.World.Catalog));
        page.OpenHud();
        page.Run($"openTrapPanel([{string.Join(",", game.Traps.Select(t => t.Id))}], {JsText.Literal(armedWords)})");
        page.Live(true, checkKey);
        page.Hud(HudView.Ready(game.World, CarrotBook(), game.FloorCost, isRebait, checkKey));

        return page;
    }

    /// <summary>The game can run Trap Hub's script before the Toolkit's: the panel's additions wait for it, then come in at once.</summary>
    [Fact]
    public void Run_before_the_toolkit_the_trap_panel_gets_its_additions_once_the_toolkit_arrives()
    {
        var page = Panel(Home(), isRebait: true, isToolkitFirst: false);

        Assert.Equal("0", page.Text("hudPage().querySelectorAll('.th-add').length"));

        page.LoadToolkit();

        Assert.Equal("1", page.Text("hudPage().querySelectorAll('.th-check-btn').length"));
        Assert.Equal("5", page.Text("hudPage().querySelectorAll('.th-sub').length"));
        Assert.True(page.Is("hudPage().getElementById('th-panel-style')"));
    }

    [Fact]
    public void The_check_button_and_rebait_box_sit_before_the_chevron_and_send_their_commands()
    {
        var page = Panel(Home(), isRebait: true);

        Assert.Equal("th-check-btn,th-fill-btn,th-rebait-chk,trap-toggle-chevron", page.Text("hudPage().querySelector('.trap-toggle-row').children.slice(1).map(function (n) { return n.classes.filter(function (c) { return c === 'th-check-btn' || c === 'th-fill-btn' || c === 'th-rebait-chk' || c === 'trap-toggle-chevron'; })[0]; }).join(',')"));
        Assert.True(page.Is("hudPage().querySelector('.th-rebait-chk').querySelector('input').checked"));

        page.Click("hudPage().querySelector('.th-check-btn')");

        Assert.Equal(new PageCommand[] { new PageCommand.CheckAll(false) }, page.Sent);

        page.Sent.Clear();
        page.Run("var box = hudPage().querySelector('.th-rebait-chk').querySelector('input'); box.checked = false; fire(box, 'change')");

        Assert.Equal(new PageCommand[] { new PageCommand.RebaitOnCheck(false) }, page.Sent);

        page.Sent.Clear();
        page.Click("hudPage().querySelector('.th-rebait-chk')");

        Assert.Empty(page.Sent);
    }

    [Fact]
    public void The_rebait_button_sends_rebait_all_and_its_tip_lists_the_bait_it_will_put_in()
    {
        var page = Panel(Home(), isRebait: false);

        page.Click("hudPage().querySelector('.th-fill-btn')");

        Assert.Equal(new PageCommand[] { new PageCommand.RebaitAll() }, page.Sent);

        var tip = page.Text("hudPage().querySelector('.th-fill-plan').textContent");

        Assert.StartsWith("Re-bait traps", tip);
        Assert.Contains("Bait with Carrot \u00b7 Pantry2", tip);
        Assert.Contains("Traps getting bait now: 2. Catches stay in the traps.", tip);
        Assert.Contains("Traps with no bait left to use: 1.", tip);
        Assert.DoesNotContain("Collect", tip);

        var idle = Panel(new FakeGame { Traps = { T(1, bait: Carrot) } }, isRebait: true);

        Assert.Contains("Nothing to re-bait", idle.Text("hudPage().querySelector('.th-fill-plan').textContent"));
    }

    [Fact]
    public void An_open_trap_list_or_a_hovered_trap_row_lifts_the_top_left_panel_above_the_tool_column()
    {
        var page = Panel(Home(), isRebait: true);
        var css = page.Text("hudPage().getElementById('th-panel-style').textContent");

        // The tool column (.vertical-tools) is at z-index 101; the game's top-left panel at 100.
        Assert.Contains(".top-left-panel:has(.trap-toggle-row.is-expanded),.top-left-panel:has(.trap-panel:hover){z-index:102}", css);
        // The game's hover filter on the row makes it a stacking context, so the hovered panel itself is raised too.
        Assert.Contains(".trap-panel:hover{z-index:1}", css);
    }

    [Fact]
    public void An_unbound_check_key_is_left_out_of_the_check_tip()
    {
        var page = Panel(Home(), isRebait: true, checkKey: "");

        Assert.Equal("0", page.Text("hudPage().querySelector('.th-plan').querySelectorAll('.entry-tip-key').length"));
        Assert.StartsWith("Check traps", page.Text("hudPage().querySelector('.th-plan').textContent"));
        Assert.DoesNotContain("None", page.Text("hudPage().querySelector('.th-plan').textContent"));
    }

    [Fact]
    public void A_bound_check_key_shows_in_the_check_tip()
    {
        var page = Panel(Home(), isRebait: true, checkKey: "J");

        Assert.Equal("J", page.Text("hudPage().querySelector('.th-plan').querySelector('.entry-tip-key').textContent"));
    }

    [Fact]
    public void The_check_tip_lists_what_the_press_will_do()
    {
        var page = Panel(Home(), isRebait: true);
        var tip = page.Text("hudPage().querySelector('.th-plan').textContent");

        Assert.Contains("Check trapsJ", tip);
        Assert.Contains("Collect Rabbit \u00b7 Pantry2", tip);
        Assert.Contains("Collect Stored catch \u00b7 Pantry3", tip);
        Assert.Contains("Bait with Carrot \u00b7 Pantry1", tip);
        Assert.Contains("RE-BAIT IS ON", tip);
        Assert.Contains("4 traps on 1 floors, this floor first", tip);
        Assert.Contains("Live Capture Cage, Pantry breaks on its next catch", tip);
        Assert.Contains("Simple Bird Net, Attic has no bait left", tip);

        var off = Panel(Home(), isRebait: false);

        Assert.DoesNotContain("RE-BAIT IS ON", off.Text("hudPage().querySelector('.th-plan').textContent"));
        Assert.False(off.Is("hudPage().querySelector('.th-rebait-chk').querySelector('input').checked"));
    }

    [Fact]
    public void Each_trap_row_gets_its_bait_catch_and_wear_and_the_list_a_summary_with_the_window_link()
    {
        var page = Panel(Home(), isRebait: true);

        Assert.Contains("Rabbit", page.Text("trapLine(0).textContent"));
        Assert.Contains("1 left", page.Text("trapLine(0).textContent"));
        Assert.True(page.Is("trapLine(0).querySelector('.th-low')"));
        Assert.Equal("10%", page.Text("trapLine(0).querySelector('i').style.width"));
        Assert.Equal("Rabbit\u00b7refill withCarrot10 left", page.Text("trapLine(1).textContent"));
        Assert.Contains("empty, will use", page.Text("trapLine(2).textContent"));
        Assert.Contains("Carrot", page.Text("trapLine(2).textContent"));
        Assert.Contains("empty, no bait left", page.Text("trapLine(3).textContent"));
        Assert.Contains("full, collect to restart", page.Text("trapLine(4).textContent"));
        Assert.Contains("Stored catch \u00d73", page.Text("trapLine(4).textContent"));

        Assert.True(page.Is("hudPage().querySelector('.trap-popover').firstChild.classList.contains('th-strip')"));
        Assert.Equal("Next check:3 prey\u00b72 to bait\u00b71 about to breakTrap Hub \u2197", page.Text("hudPage().querySelector('.th-strip').textContent"));

        page.Click("hudPage().querySelector('.th-lnk')");

        Assert.Equal(new PageCommand[] { new PageCommand.Toggle() }, page.Sent);
    }

    [Fact]
    public void The_games_battle_word_for_a_baited_trap_reads_armed_and_other_languages_are_left_alone()
    {
        var english = Panel(Home(), isRebait: true);
        var chinese = Panel(Home(), isRebait: true, armedWords: "\u5e03\u9632\u4e2d");

        Assert.Equal("Armed", english.Text("vueState().i18n_trapStatusArmed"));
        Assert.Equal("\u5e03\u9632\u4e2d", chinese.Text("vueState().i18n_trapStatusArmed"));
    }

    [Fact]
    public void The_additions_go_when_the_game_is_left_and_are_not_stacked_by_polls()
    {
        var page = Panel(Home(), isRebait: true);

        page.Poll();
        page.Poll();

        Assert.Equal("1", page.Text("hudPage().querySelectorAll('.th-check-btn').length"));
        Assert.Equal("1", page.Text("hudPage().querySelectorAll('.th-strip').length"));
        Assert.Equal("5", page.Text("hudPage().querySelectorAll('.th-sub').length"));

        page.Live(false, "J");

        Assert.Equal("0", page.Text("hudPage().querySelectorAll('.th-add').length"));
    }

    [Fact]
    public void Polls_after_the_game_is_left_add_nothing_and_the_next_game_gets_everything_back()
    {
        var page = Panel(Home(), isRebait: true);

        page.Live(false, "J");
        page.Poll();
        page.Poll();

        Assert.Equal("0", page.Text("hudPage().querySelectorAll('.th-add').length"));
        Assert.False(page.Is("hudButton()"));

        page.Live(true, "J");

        Assert.True(page.Is("hudButton()"));
        Assert.Equal("1", page.Text("hudPage().querySelectorAll('.th-check-btn').length"));
        Assert.Equal("5", page.Text("hudPage().querySelectorAll('.th-sub').length"));
    }

    [Fact]
    public void The_additions_go_when_the_traps_can_no_longer_be_read()
    {
        var page = Panel(Home(), isRebait: true);

        page.Hud(HudView.NotReady());
        page.Poll();

        Assert.Equal("0", page.Text("hudPage().querySelectorAll('.th-add').length"));
        Assert.True(page.Is("hudButton()"));
    }

    [Fact]
    public void One_trap_changing_redraws_its_own_row_line_and_leaves_the_others_as_they_are()
    {
        var game = Home();
        var page = Panel(game, isRebait: true);

        page.Run("var keptLine = trapLine(0); var changedLine = trapLine(2);");
        game.Traps[2] = game.Traps[2] with { BaitId = Carrot };
        page.Hud(HudView.Ready(game.World, CarrotBook(), game.FloorCost, isRebait: true, "J"));

        // One trap's change must not cost the web view a redraw of every row.
        Assert.True(page.Is("trapLine(0) === keptLine"));
        Assert.False(page.Is("trapLine(2) === changedLine"));
        Assert.Equal("Carrot10 left", page.Text("trapLine(2).textContent"));
    }

    [Fact]
    public void A_poll_with_nothing_new_keeps_every_row_line()
    {
        var page = Panel(Home(), isRebait: true);

        page.Run("var lines = [0, 1, 2, 3, 4].map(trapLine);");
        page.Poll();
        page.Poll();

        Assert.True(page.Is("[0, 1, 2, 3, 4].every(function (i) { return trapLine(i) === lines[i]; })"));
    }

    [Fact]
    public void A_change_of_language_redraws_every_row_line_in_the_new_words_and_names()
    {
        var game = Home();
        var page = Panel(game, isRebait: true);

        page.Catalog(CatalogView.Write(game.World.Catalog).Replace("\"Rabbit\"", "\"Hare\""));
        page.Run("window.__sltraphubWords = { hLeft: '{0} to go' }");
        page.Poll();

        Assert.Contains("Hare", page.Text("trapLine(0).textContent"));
        Assert.Contains("1 to go", page.Text("trapLine(0).textContent"));
        Assert.Contains("10 to go", page.Text("trapLine(2).textContent"));
    }
}
