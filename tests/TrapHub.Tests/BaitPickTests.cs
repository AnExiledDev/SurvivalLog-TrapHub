using TrapHub.Core;
using Xunit;
using static TrapHub.Tests.FakeGame;

namespace TrapHub.Tests;

/// <summary>Which bait an empty trap gets from its preset: allowed bait only, in the player's order, keeping back what they asked.</summary>
public class BaitPickTests
{
    private static readonly TrapType CageType = Catalog.Types[Cage];

    private static BaitChoice? Pick(BaitPreset preset, IEnumerable<int> allowed, params BaitUnit[] stock) =>
        BaitPick.For(CageType, preset, allowed.ToHashSet(), stock, Catalog, new Dictionary<long, int>());

    [Fact]
    public void The_first_step_the_player_has_enough_of_is_used()
    {
        var choice = Pick(Preset("p", Step(Meat), Step(Carrot)), new[] { Meat, Carrot }, Bag(1, Carrot, 5), Bag(2, Meat, 2));

        Assert.Equal(Meat, choice?.ItemId);
        Assert.Equal(0, choice?.Step);
    }

    [Fact]
    public void A_step_is_skipped_once_only_the_kept_amount_is_left()
    {
        var preset = Preset("p", Step(Meat, keep: 2), Step(Carrot));

        Assert.Equal(Carrot, Pick(preset, new[] { Meat, Carrot }, Bag(1, Carrot, 5), Bag(2, Meat, 2))?.ItemId);
        Assert.Equal(Meat, Pick(preset, new[] { Meat, Carrot }, Bag(1, Carrot, 5), Bag(2, Meat, 3))?.ItemId);
    }

    [Fact]
    public void Blocked_bait_never_goes_in_a_trap_even_as_the_last_resort()
    {
        var preset = Preset("p", Step(Meat)) with { LastResort = true };

        Assert.Null(Pick(preset, Array.Empty<int>(), Bag(1, Meat, 5), Bag(2, Carrot, 5)));
    }

    [Fact]
    public void The_last_resort_takes_the_allowed_universal_bait_the_player_has_most_of()
    {
        var preset = Preset("p") with { LastResort = true };
        var choice = Pick(preset, new[] { Carrot, Seeds, Meat }, Bag(1, Carrot, 2), Bag(2, Seeds, 7), Bag(3, Meat, 50));

        Assert.Equal(Seeds, choice?.ItemId);
        Assert.Equal(BaitChoice.LastResortStep, choice?.Step);
    }

    [Fact]
    public void Without_the_last_resort_a_preset_that_ran_out_gives_nothing()
    {
        Assert.Null(Pick(Preset("p", Step(Meat)), new[] { Meat, Carrot }, Bag(1, Carrot, 5)));
    }

    [Fact]
    public void A_bag_only_preset_never_reaches_into_home_storage()
    {
        var preset = Preset("p", Step(Carrot)) with { Source = BaitSource.Bag };

        Assert.Null(Pick(preset, new[] { Carrot }, Home(1, Carrot, 9)));
        Assert.Equal(2, Pick(preset, new[] { Carrot }, Home(1, Carrot, 9), Bag(2, Carrot, 1))?.InstanceId);
    }

    [Fact]
    public void Bait_comes_from_the_bag_first_then_the_stack_closest_to_spoiling()
    {
        var preset = Preset("p", Step(Carrot));

        Assert.Equal(3, Pick(preset, new[] { Carrot }, Home(1, Carrot, 9, hoursLeft: 1), Bag(2, Carrot, 9, hoursLeft: 40), Bag(3, Carrot, 9, hoursLeft: 5))?.InstanceId);
        Assert.Equal(2, Pick(preset with { SpoilFirst = false }, new[] { Carrot }, Bag(2, Carrot, 9, hoursLeft: 40), Bag(3, Carrot, 9, hoursLeft: 5))?.InstanceId);
    }

    [Fact]
    public void Of_two_stacks_with_the_same_time_left_the_older_one_spoils_first()
    {
        // Same item, same TimeLeft, but stack 2 was picked up 72 hours before stack 1. The game counts from the start
        // hour, so stack 2 is the one about to spoil. Reading TimeLeft alone ties them, and the tie goes to stack 1.
        var preset = Preset("p", Step(Meat));
        var older = Bag(2, Meat, 3, hoursLeft: BaitUnit.HoursLeftAt(startTime: 100, timeLeft: 96, hourNow: 180));
        var fresher = Bag(1, Meat, 3, hoursLeft: BaitUnit.HoursLeftAt(startTime: 172, timeLeft: 96, hourNow: 180));

        Assert.Equal(2, Pick(preset, new[] { Meat }, fresher, older)?.InstanceId);
        Assert.Equal(1, Pick(preset with { SpoilFirst = false }, new[] { Meat }, older, fresher)?.InstanceId);
    }

    [Fact]
    public void Hours_left_count_from_the_start_hour_like_the_game()
    {
        Assert.Equal(16, BaitUnit.HoursLeftAt(startTime: 100, timeLeft: 96, hourNow: 180));
        Assert.Equal(1, BaitUnit.HoursLeftAt(startTime: 100, timeLeft: 96, hourNow: 500));
        Assert.Null(BaitUnit.HoursLeftAt(startTime: 100, timeLeft: 0, hourNow: 180));
    }

    [Fact]
    public void Bait_promised_to_one_trap_is_not_promised_to_the_next()
    {
        var preset = Preset("p", Step(Meat), Step(Carrot));
        var allowed = new HashSet<int> { Meat, Carrot };
        var stock = new[] { Bag(1, Meat, 1), Bag(2, Carrot, 1) };
        var reserved = new Dictionary<long, int>();

        Assert.Equal(Meat, BaitPick.For(CageType, preset, allowed, stock, Catalog, reserved)?.ItemId);
        Assert.Equal(Carrot, BaitPick.For(CageType, preset, allowed, stock, Catalog, reserved)?.ItemId);
        Assert.Null(BaitPick.For(CageType, preset, allowed, stock, Catalog, reserved));
    }

    [Fact]
    public void Bait_the_trap_type_does_not_take_is_passed_over()
    {
        var choice = BaitPick.For(Catalog.Types[Net], Preset("p", Step(Carrot), Step(Seeds)), new HashSet<int> { Carrot, Seeds }, new[] { Bag(1, Carrot, 5), Bag(2, Seeds, 5) }, Catalog, new Dictionary<long, int>());

        Assert.Equal(Seeds, choice?.ItemId);
    }

    [Fact]
    public void A_trap_that_has_bait_breaks_on_its_next_catch_or_keeps_its_own_store_is_not_baited()
    {
        Assert.True(BaitPick.CanBait(T(1), CageType));
        Assert.False(BaitPick.CanBait(T(1, bait: Carrot), CageType));
        Assert.False(BaitPick.CanBait(T(1, prey: Rabbit, dur: 10), CageType));
        Assert.True(BaitPick.CanBait(T(1, prey: Rabbit, dur: 11), CageType));
        Assert.False(BaitPick.CanBait(T(1, type: AutoCage), Catalog.Types[AutoCage]));
    }
}
