using System.Text.Json;
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

    private static PageScript Panel(FakeGame game, bool isRebait, string armedWords = "Deploying Defenses")
    {
        var page = new PageScript();

        page.Catalog(CatalogView.Write(game.World.Catalog));
        page.OpenHud();
        page.Run($"openTrapPanel([{string.Join(",", game.Traps.Select(t => t.Id))}], {JsText.Literal(armedWords)})");
        page.Live(true, "J");
        page.Hud(HudView.Ready(game.World, CarrotBook(), game.FloorCost, isRebait, "J"));

        return page;
    }

    [Fact]
    public void The_check_button_and_rebait_box_sit_before_the_chevron_and_send_their_commands()
    {
        var page = Panel(Home(), isRebait: true);

        Assert.Equal("th-check-btn,th-rebait-chk,trap-toggle-chevron", page.Text("hudPage().querySelector('.trap-toggle-row').children.slice(1).map(function (n) { return n.classes.filter(function (c) { return c === 'th-check-btn' || c === 'th-rebait-chk' || c === 'trap-toggle-chevron'; })[0]; }).join(',')"));
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
}
