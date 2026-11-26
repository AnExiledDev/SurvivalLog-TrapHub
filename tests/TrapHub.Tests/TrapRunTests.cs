using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>The presses: re-bait, take and dismantle, as the player sees them through the game.</summary>
public class TrapRunTests
{
    private static PresetBook CarrotBook(AutoRebait auto = AutoRebait.Off) => Book(new[] { Carrot }, Preset("p1", Step(Carrot)) with { Auto = auto });

    [Fact]
    public void Re_bait_all_puts_bait_in_every_empty_trap_the_bait_reaches()
    {
        var game = new FakeGame { Traps = { T(1), T(2), T(3, bait: Carrot) }, Stock = { Bag(1, Carrot, 5) } };

        var outcome = TrapRun.RebaitAll(game, CarrotBook());

        Assert.Equal(new TrapOutcome.Baited(2, 0, 0), outcome);
        Assert.Equal(new[] { 1L, 2L }, game.Baited.Select(b => b.TrapId));
        Assert.Equal(3, game.Stock[0].Count);
    }

    [Fact]
    public void When_bait_is_short_the_traps_on_the_players_floor_get_it_first()
    {
        var game = new FakeGame { PlayerFloor = 2, Traps = { T(1, floor: 1), T(2, floor: 2) }, Stock = { Bag(1, Carrot, 1) } };

        var outcome = TrapRun.RebaitAll(game, CarrotBook());

        Assert.Equal(new TrapOutcome.Baited(1, 1, 0), outcome);
        Assert.Equal(2L, game.Baited.Single().TrapId);
    }

    [Fact]
    public void A_trap_the_game_refuses_is_counted_and_the_rest_still_get_bait()
    {
        var game = new FakeGame { Traps = { T(1), T(2) }, Stock = { Bag(1, Carrot, 5) }, Refusing = { 1 } };

        Assert.Equal(new TrapOutcome.Baited(1, 0, 1), TrapRun.RebaitAll(game, CarrotBook()));
    }

    [Fact]
    public void Re_bait_all_with_nothing_empty_says_so()
    {
        var game = new FakeGame { Traps = { T(1, bait: Carrot) }, Stock = { Bag(1, Carrot, 5) } };

        Assert.IsType<TrapOutcome.NothingToBait>(TrapRun.RebaitAll(game, CarrotBook()));
    }

    [Fact]
    public void Re_baiting_one_trap_names_the_trap_and_the_bait_or_why_not()
    {
        var game = new FakeGame { Traps = { T(1), T(2, bait: Carrot), T(3, type: Net) }, Stock = { Bag(1, Carrot, 1) } };
        var book = CarrotBook();

        Assert.Equal(new TrapOutcome.BaitedOne("Live Capture Cage", "Carrot"), TrapRun.Rebait(game, book, 1));
        Assert.Equal(new TrapOutcome.AlreadyBaited("Live Capture Cage"), TrapRun.Rebait(game, book, 2));
        Assert.Equal(new TrapOutcome.NoBaitLeft("Simple Bird Net", "Preset p1"), TrapRun.Rebait(game, book, 3));
        Assert.IsType<TrapOutcome.Gone>(TrapRun.Rebait(game, book, 99));
    }

    [Fact]
    public void A_trap_with_its_own_preset_uses_it_instead_of_its_types_default()
    {
        var game = new FakeGame { Traps = { T(1), T(2) }, Stock = { Bag(1, Carrot, 5), Bag(2, Meat, 5) } };
        var book = Book(new[] { Carrot, Meat }, Preset("p1", Step(Carrot)), Preset("p2", Step(Meat))).Assign("slot-1", new[] { 2L }, "p2");

        TrapRun.RebaitAll(game, book);

        Assert.Equal(new[] { (1L, Carrot), (2L, Meat) }, game.Baited);
    }

    [Fact]
    public void Auto_re_bait_on_my_floor_leaves_the_other_floors_alone()
    {
        var game = new FakeGame { PlayerFloor = 1, Traps = { T(1, floor: 1), T(2, floor: 2) }, Stock = { Bag(1, Carrot, 5) } };

        TrapRun.Auto(game, CarrotBook(AutoRebait.Nearby), now: 0, new Dictionary<long, double>());

        Assert.Equal(1L, game.Baited.Single().TrapId);
    }

    [Fact]
    public void Auto_re_bait_always_reaches_every_floor_and_off_touches_nothing()
    {
        var always = new FakeGame { PlayerFloor = 1, Traps = { T(1, floor: 1), T(2, floor: 2) }, Stock = { Bag(1, Carrot, 5) } };
        var off = new FakeGame { Traps = { T(1) }, Stock = { Bag(1, Carrot, 5) } };

        TrapRun.Auto(always, CarrotBook(AutoRebait.Always), now: 0, new Dictionary<long, double>());
        TrapRun.Auto(off, CarrotBook(AutoRebait.Off), now: 0, new Dictionary<long, double>());

        Assert.Equal(2, always.Baited.Count);
        Assert.Empty(off.Baited);
    }

    [Fact]
    public void A_trap_auto_re_bait_could_not_fill_waits_a_minute_before_it_is_tried_again()
    {
        var game = new FakeGame { Traps = { T(1) }, Refusing = { 1 }, Stock = { Bag(1, Carrot, 5) } };
        var retryAt = new Dictionary<long, double>();
        var book = CarrotBook(AutoRebait.Always);

        TrapRun.Auto(game, book, now: 0, retryAt);
        game.Refusing.Clear();
        TrapRun.Auto(game, book, now: 30, retryAt);

        Assert.Empty(game.Baited);

        TrapRun.Auto(game, book, now: 61, retryAt);

        Assert.Single(game.Baited);
    }

    [Fact]
    public void Take_all_lines_up_every_catch_not_already_queued_and_empties_stores()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit), T(2, prey: Rabbit, queued: true), T(3), T(4, type: AutoCage, store: 3, floor: 2) } };

        var outcome = TrapRun.TakeAll(game, onlyFloor: null);

        Assert.Equal(new TrapOutcome.Taking(2, 2, 0), outcome);
        Assert.Equal(new[] { (1L, false), (4L, true) }, game.Taken);
    }

    [Fact]
    public void Take_this_floor_stays_on_the_floor()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit, floor: 1), T(2, prey: Rabbit, floor: 2) } };

        TrapRun.TakeAll(game, onlyFloor: 2);

        Assert.Equal(2L, game.Taken.Single().TrapId);
    }

    [Fact]
    public void Take_all_with_only_queued_catches_says_how_many_are_on_the_way()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit, queued: true) } };

        Assert.Equal(new TrapOutcome.NothingToTake(1), TrapRun.TakeAll(game, onlyFloor: null));
    }

    [Fact]
    public void Check_traps_collects_every_catch_then_baits_every_empty_trap_including_the_one_holding_a_catch()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit), T(2), T(3, bait: Carrot), T(4, floor: 2) }, Stock = { Bag(1, Carrot, 5) } };

        var outcome = TrapRun.CheckAll(game, CarrotBook(), onlyFloor: null);

        Assert.Equal(new TrapOutcome.Checked(new TrapOutcome.Taking(1, 1, 0), new TrapOutcome.Baited(3, 0, 0)), outcome);
        Assert.Equal(new[] { 1L }, game.Taken.Select(t => t.TrapId));
        Assert.Equal(new[] { 1L, 2L, 4L }, game.Baited.Select(b => b.TrapId).OrderBy(id => id));
        Assert.Equal("Collecting from 1 trap. Baited 3 traps.", TrapMessages.Describe(outcome, isChinese: false));
    }

    [Fact]
    public void Check_this_floor_leaves_the_other_floors_alone()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit, floor: 2), T(2, floor: 2), T(3, floor: 1) }, Stock = { Bag(1, Carrot, 5) } };

        TrapRun.CheckAll(game, CarrotBook(), onlyFloor: 1);

        Assert.Empty(game.Taken);
        Assert.Equal(new[] { 3L }, game.Baited.Select(b => b.TrapId));
    }

    [Fact]
    public void Check_traps_with_nothing_to_do_says_so_once_and_reports_only_the_half_that_acted()
    {
        var idle = new FakeGame { Traps = { T(1, bait: Carrot) }, Stock = { Bag(1, Carrot, 5) } };
        var baitOnly = new FakeGame { Traps = { T(1) }, Stock = { Bag(1, Carrot, 5) } };

        Assert.Equal(Texts.NothingToCheck.En, TrapMessages.Describe(TrapRun.CheckAll(idle, CarrotBook(), null), isChinese: false));
        Assert.Equal("Baited 1 trap.", TrapMessages.Describe(TrapRun.CheckAll(baitOnly, CarrotBook(), null), isChinese: false));
    }

    [Fact]
    public void Check_traps_does_nothing_without_a_game_ready_for_it()
    {
        var game = new FakeGame { Traps = { T(1) }, Stock = { Bag(1, Carrot, 5) }, ReadInstead = new TrapRead.NotReady("away") };

        Assert.Equal(new TrapOutcome.NotReady("away"), TrapRun.CheckAll(game, CarrotBook(), null));
        Assert.Empty(game.Baited);
    }

    [Fact]
    public void Dismantle_reports_what_the_game_did()
    {
        var game = new FakeGame { Traps = { T(1), T(2) }, Refusing = { 2 } };

        Assert.Equal(new TrapOutcome.Dismantling("Live Capture Cage"), TrapRun.Dismantle(game, 1));
        Assert.Equal(new TrapOutcome.Refused("Live Capture Cage"), TrapRun.Dismantle(game, 2));
        Assert.Equal(new[] { 1L }, game.Dismantled);
    }

    [Fact]
    public void No_press_acts_without_a_game_ready_for_it()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit) }, ReadInstead = new TrapRead.NotReady("away") };

        Assert.Equal(new TrapOutcome.NotReady("away"), TrapRun.TakeAll(game, null));
        Assert.Empty(game.Taken);

        game.ReadInstead = new TrapRead.NoGame();

        Assert.IsType<TrapOutcome.NoGame>(TrapRun.RebaitAll(game, CarrotBook()));
    }
}
