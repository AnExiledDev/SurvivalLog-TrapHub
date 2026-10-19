using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>Catch odds by the game's formula, the order Take All walks in, and the floors it walks between.</summary>
public class OddsAndRouteTests
{
    [Fact]
    public void Without_bait_each_prey_weighs_its_base_rate_against_the_empty_weight()
    {
        var odds = Odds.For(Catalog.Types[Net], baitId: 0, Pantry, Catalog, OddsBonus.None);

        Assert.Equal(0.5, odds.Catch, 3);
        Assert.Equal(0.5, odds.Prey.Single().Chance, 3);
    }

    [Fact]
    public void Bait_and_room_multiply_the_prey_they_favour()
    {
        var seeds = Odds.For(Catalog.Types[Net], Seeds, Pantry, Catalog, OddsBonus.None);
        var seedsInAttic = Odds.For(Catalog.Types[Net], Seeds, Attic, Catalog, OddsBonus.None);

        Assert.Equal(100d / 150d, seeds.Catch, 3);
        Assert.Equal(200d / 250d, seedsInAttic.Catch, 3);
    }

    [Fact]
    public void The_rare_bonus_and_bait_talent_only_raise_what_they_name()
    {
        var plain = Odds.For(Catalog.Types[Cage], Carrot, Pantry, Catalog, OddsBonus.None);
        var bonus = Odds.For(Catalog.Types[Cage], Carrot, Pantry, Catalog, new OddsBonus(0f, 0f, 2f));

        double Fox(OddsResult odds) => odds.Prey.Single(p => p.PreyId == FakeGame.Fox).Chance;
        double Rabbit(OddsResult odds) => odds.Prey.Single(p => p.PreyId == FakeGame.Rabbit).Chance;

        Assert.True(Fox(bonus) > Fox(plain));
        Assert.True(Rabbit(bonus) < Rabbit(plain));
        Assert.Equal(40d / 90d, plain.Catch, 3);
    }

    [Fact]
    public void Take_all_walks_the_players_floor_first_nearest_trap_to_nearest()
    {
        var traps = new[] { T(1, prey: Rabbit, floor: 2, x: 1f), T(2, prey: Rabbit, floor: 1, x: 9f), T(3, prey: Rabbit, floor: 1, x: 2f), T(4, prey: Rabbit, floor: -1) };

        var route = TakeAllRoute.Plan(traps, playerFloor: 1, new Spot(0f, 0f, 0f), Floors.Steps, onlyFloor: null);

        Assert.Equal(new[] { 3L, 2L }, route.Take(2).Select(t => t.Id));
        Assert.Equal(new long[] { 1, 4 }, route.Skip(2).Select(t => t.Id).OrderBy(id => id));
    }

    [Theory]
    [InlineData(1, 2, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(-1, 2, 2)]
    [InlineData(2, 2, 0)]
    public void The_basement_is_one_floor_change_below_the_ground_floor(int from, int to, int steps)
    {
        Assert.Equal(steps, Floors.Steps(from, to));
    }

    [Fact]
    public void Floors_sort_bottom_up_from_the_games_map_ids()
    {
        Assert.Equal(new[] { -1, 1, 2 }, new[] { Floors.UpstairsMap, Floors.GroundMap, Floors.BasementMap }.Select(Floors.Level).OrderBy(l => l));
        Assert.Equal(Floors.BasementMap, Floors.MapId(Floors.Level(Floors.BasementMap)));
    }
}
