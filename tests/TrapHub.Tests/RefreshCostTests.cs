using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>
/// What keeps a refresh cheap: a view is not built again from a home that has not changed, and anything that did change
/// is still seen.
/// </summary>
public class RefreshCostTests
{
    private static PresetBook CarrotBook(AutoRebait auto = AutoRebait.Off) => Book(new[] { Carrot }, Preset("p1", Step(Carrot)) with { Auto = auto });

    private static FakeGame Home() => new()
    {
        Traps = { T(1, prey: Rabbit), T(2), T(3, floor: 2, type: Net, room: Attic) },
        Stock = { Bag(1, Carrot, 5) },
    };

    [Fact]
    public void A_fresh_read_of_a_home_that_has_not_changed_is_not_drawn_again()
    {
        var game = Home();
        var book = CarrotBook();
        var drawn = new DrawnFrom(isPositionDrawn: true);

        drawn.Remember(game.World, book);

        // Every read builds new lists, as the game's does, so only the items themselves can say nothing changed.
        Assert.True(drawn.IsSame(game.World, book));
    }

    [Fact]
    public void A_trap_placed_removed_or_changed_is_drawn_again()
    {
        var book = CarrotBook();

        foreach (var change in new Action<FakeGame>[]
                 {
                     game => game.Traps.Add(T(4)),
                     game => game.Traps.RemoveAt(1),
                     game => game.Traps[1] = game.Traps[1] with { BaitId = Carrot },
                     game => game.Traps[0] = game.Traps[0] with { IsTakeQueued = true },
                     game => game.Traps[2] = game.Traps[2] with { Durability = 40 },
                 })
        {
            var game = Home();
            var drawn = new DrawnFrom(isPositionDrawn: false);

            drawn.Remember(game.World, book);
            change(game);

            Assert.False(drawn.IsSame(game.World, book));
        }
    }

    [Fact]
    public void Bait_used_or_gained_a_floor_change_or_another_save_is_drawn_again()
    {
        var book = CarrotBook();

        foreach (var change in new Action<FakeGame>[]
                 {
                     game => game.Stock[0] = game.Stock[0] with { Count = 4 },
                     game => game.Stock.Add(FakeGame.Home(2, Seeds, 1)),
                     game => game.PlayerFloor = 2,
                     game => game.SaveKey = "slot-2",
                 })
        {
            var game = Home();
            var drawn = new DrawnFrom(isPositionDrawn: false);

            drawn.Remember(game.World, book);
            change(game);

            Assert.False(drawn.IsSame(game.World, book));
        }
    }

    [Fact]
    public void A_new_load_or_language_brings_a_new_catalog_and_everything_is_drawn_again()
    {
        var game = Home();
        var book = CarrotBook();
        var drawn = new DrawnFrom(isPositionDrawn: false);

        drawn.Remember(game.World, book);

        // The same names in a catalog built again: it is a new one all the same, as after a load or a language change.
        Assert.False(drawn.IsSame(game.World with { Catalog = Catalog with { } }, book));
    }

    [Fact]
    public void A_preset_change_is_drawn_again_even_when_the_home_is_the_same()
    {
        var game = Home();
        var drawn = new DrawnFrom(isPositionDrawn: false);

        drawn.Remember(game.World, CarrotBook());

        Assert.False(drawn.IsSame(game.World, CarrotBook()));
    }

    [Fact]
    public void Walking_around_redraws_only_the_view_that_shows_the_route()
    {
        var game = Home();
        var book = CarrotBook();
        var hud = new DrawnFrom(isPositionDrawn: true);
        var window = new DrawnFrom(isPositionDrawn: false);

        hud.Remember(game.World, book);
        window.Remember(game.World, book);

        var moved = game.World with { PlayerPosition = new Spot(5f, 0f, 2f) };

        Assert.False(hud.IsSame(moved, book));
        Assert.True(window.IsSame(moved, book));
    }

    [Fact]
    public void A_forgotten_view_is_drawn_again()
    {
        var game = Home();
        var book = CarrotBook();
        var drawn = new DrawnFrom(isPositionDrawn: false);

        drawn.Remember(game.World, book);
        drawn.Forget();

        Assert.False(drawn.IsSame(game.World, book));
    }

    [Fact]
    public void The_auto_pass_on_the_refreshs_own_read_baits_without_reading_the_game_again()
    {
        var game = new FakeGame { Traps = { T(1), T(2, floor: 2) }, Stock = { Bag(1, Carrot, 5) } };
        var world = game.World;

        game.ReadInstead = new TrapRead.NotReady("a read here would find nothing");

        TrapRun.Auto(game, world, CarrotBook(AutoRebait.Always), now: 0, new Dictionary<long, double>());

        Assert.Equal(new[] { 1L, 2L }, game.Baited.Select(b => b.TrapId).OrderBy(id => id));
    }

    [Fact]
    public void The_auto_pass_is_skipped_only_when_no_preset_baits_on_its_own()
    {
        Assert.False(TrapRun.HasAuto(CarrotBook(AutoRebait.Off)));
        Assert.True(TrapRun.HasAuto(CarrotBook(AutoRebait.Nearby)));
        Assert.True(TrapRun.HasAuto(Book(new[] { Carrot }, Preset("p1", Step(Carrot)), Preset("p2", Step(Carrot)) with { Auto = AutoRebait.Always })));
    }

    [Fact]
    public void The_start_up_warm_up_runs_every_view_and_press_without_failing()
    {
        Assert.True(Warmup.Run() > 0);
    }
}
