using System.Text.Json;
using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>What the window shows about the traps: what needs the player, what a re-bait would use, odds and wear.</summary>
public class TrapViewTests
{
    private static readonly ViewSettings Settings = new(new Dictionary<SoundGroup, SoundMode> { [SoundGroup.SimpleBirdNet] = SoundMode.Muted }, SoundsElsewhere: false, "J");

    private static JsonElement View(FakeGame game, PresetBook book) => JsonDocument.Parse(TrapView.Ready(game.World, book, Settings)).RootElement;

    private static JsonElement Trap(JsonElement view, long id) => view.GetProperty("traps").EnumerateArray().Single(t => t.GetProperty("id").GetString() == id.ToString());

    private static PresetBook CarrotBook() => Book(new[] { Carrot }, Preset("p1", Step(Carrot)));

    [Fact]
    public void The_counts_say_what_needs_the_player()
    {
        var game = new FakeGame
        {
            PlayerFloor = 2,
            Traps = { T(1, prey: Rabbit), T(2, prey: Rabbit, floor: 2), T(3, prey: Rabbit, queued: true), T(4), T(5, bait: Carrot, state: TrapState.Halted) },
            Stock = { Bag(1, Carrot, 9) },
        };
        var counts = View(game, CarrotBook()).GetProperty("counts");

        Assert.Equal(3, counts.GetProperty("prey").GetInt32());
        Assert.Equal(2, counts.GetProperty("take").GetInt32());
        Assert.Equal(1, counts.GetProperty("takeFloor").GetInt32());
        Assert.Equal(4, counts.GetProperty("noBait").GetInt32());
        Assert.Equal(4, counts.GetProperty("rebait").GetInt32());
        Assert.Equal(1, counts.GetProperty("halted").GetInt32());
    }

    [Fact]
    public void Two_empty_traps_never_both_show_the_last_carrot()
    {
        var game = new FakeGame { Traps = { T(1), T(2) }, Stock = { Bag(1, Carrot, 1) } };
        var view = View(game, CarrotBook());

        Assert.Equal(Carrot, Trap(view, 1).GetProperty("next").GetInt32());
        Assert.Equal(0, Trap(view, 2).GetProperty("next").GetInt32());
        Assert.Equal(1, view.GetProperty("counts").GetProperty("rebait").GetInt32());
    }

    [Fact]
    public void A_baited_trap_shows_no_next_bait()
    {
        var view = View(new FakeGame { Traps = { T(1, bait: Carrot) } }, CarrotBook());

        Assert.False(Trap(view, 1).GetProperty("canBait").GetBoolean());
        Assert.False(Trap(view, 1).TryGetProperty("next", out _));
    }

    [Fact]
    public void Wear_is_shown_as_catches_left_and_a_trap_about_to_break_is_worn()
    {
        var view = View(new FakeGame { Traps = { T(1, dur: 35), T(2, dur: 15), T(3, type: AutoCage) } }, CarrotBook());

        Assert.Equal(3, Trap(view, 1).GetProperty("catches").GetInt32());
        Assert.False(Trap(view, 1).GetProperty("worn").GetBoolean());
        Assert.True(Trap(view, 2).GetProperty("worn").GetBoolean());
        Assert.Equal(JsonValueKind.Null, Trap(view, 3).GetProperty("catches").ValueKind);
    }

    [Fact]
    public void The_odds_follow_the_bait_a_re_bait_would_use()
    {
        var game = new FakeGame { Traps = { T(1) }, Stock = { Bag(1, Meat, 3) } };
        var withMeat = Trap(View(game, Book(new[] { Meat }, Preset("p1", Step(Meat)))), 1);
        var withNothing = Trap(View(game, Book(Array.Empty<int>(), Preset("p1", Step(Meat)))), 1);

        Assert.Equal(Fox, withMeat.GetProperty("odds")[0].GetProperty("prey").GetInt32());
        Assert.Equal(Rabbit, withNothing.GetProperty("odds")[0].GetProperty("prey").GetInt32());
    }

    [Fact]
    public void A_trap_shows_whether_its_preset_is_its_own_or_its_types()
    {
        var game = new FakeGame { Traps = { T(1), T(2) } };
        var view = View(game, Book(new[] { Carrot }, Preset("p1"), Preset("p2")).Assign("slot-1", new[] { 2L }, "p2"));

        Assert.Equal(("p1", false), (Trap(view, 1).GetProperty("preset").GetString(), Trap(view, 1).GetProperty("own").GetBoolean()));
        Assert.Equal(("p2", true), (Trap(view, 2).GetProperty("preset").GetString(), Trap(view, 2).GetProperty("own").GetBoolean()));
    }

    [Fact]
    public void The_settings_ride_along_with_the_view()
    {
        var view = View(new FakeGame(), CarrotBook());

        Assert.Equal("Muted", view.GetProperty("sounds").GetProperty("net").GetString());
        Assert.Equal("Normal", view.GetProperty("sounds").GetProperty("cage").GetString());
        Assert.Equal("J", view.GetProperty("key").GetString());
    }

    [Fact]
    public void The_hud_badge_counts_catches_to_take_and_traps_waiting_for_bait()
    {
        var game = new FakeGame { Traps = { T(1, prey: Rabbit, dur: 5), T(2), T(3, bait: Carrot), T(4, prey: Rabbit, queued: true, dur: 5) } };

        Assert.Equal(2, TrapView.NeedsYou(game.World));
    }

    [Fact]
    public void The_catalog_lists_what_each_bait_draws_strongest_first()
    {
        var catalog = JsonDocument.Parse(CatalogView.Write(Catalog)).RootElement;
        var meat = catalog.GetProperty("baits").EnumerateArray().Single(b => b.GetProperty("id").GetInt32() == Meat);

        Assert.Equal("preference", meat.GetProperty("kind").GetString());
        Assert.Equal(new[] { Fox }, meat.GetProperty("draws").EnumerateArray().Select(p => p.GetInt32()));
    }
}
