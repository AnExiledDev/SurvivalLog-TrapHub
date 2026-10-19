using TrapHub.Core;

namespace TrapHub.Tests;

/// <summary>
/// A home with traps and a bag, standing in for the game behind ITrapGame. Bait the game takes leaves the stock and sits
/// in the trap, a take lines up a walk, and any trap can be made to refuse, so a test sees what the player would.
/// </summary>
internal sealed class FakeGame : ITrapGame
{
    public const int Cage = 1;
    public const int Net = 2;
    public const int Mousetrap = 3;
    public const int AutoCage = 5;

    public const int Rabbit = 100;
    public const int Bird = 101;
    public const int Rat = 102;
    public const int Fox = 103;

    public const int Carrot = 200;
    public const int Seeds = 201;
    public const int Meat = 202;
    public const int Cheese = 203;

    public const int Pantry = 10;
    public const int Attic = 20;

    public static readonly TrapCatalog Catalog = new(
        new Dictionary<int, TrapType>
        {
            [Cage] = new(Cage, "Live Capture Cage", "Res/cage.png", HasStore: false, CatchCost: 10, EmptyWeight: 50f,
                new[] { new PreyRate(Rabbit, 30f), new PreyRate(Fox, 10f) }, new HashSet<int> { Carrot, Seeds, Meat }),
            [Net] = new(Net, "Simple Bird Net", null, HasStore: false, CatchCost: 20, EmptyWeight: 50f,
                new[] { new PreyRate(Bird, 50f) }, new HashSet<int> { Seeds }),
            [Mousetrap] = new(Mousetrap, "Simple Mousetrap", null, HasStore: false, CatchCost: 50, EmptyWeight: 50f,
                new[] { new PreyRate(Rat, 50f) }, new HashSet<int> { Cheese, Carrot }),
            [AutoCage] = new(AutoCage, "Auto Trap Cage", null, HasStore: true, CatchCost: 0, EmptyWeight: 50f,
                new[] { new PreyRate(Rabbit, 50f) }, new HashSet<int> { Carrot }),
        },
        new Dictionary<int, BaitItem>
        {
            [Carrot] = new(Carrot, "Carrot", "Res/carrot.png", BaitKind.Universal, new Dictionary<int, float>()),
            [Seeds] = new(Seeds, "Seeds", null, BaitKind.Universal, new Dictionary<int, float> { [Bird] = 2f }),
            [Meat] = new(Meat, "Meat", null, BaitKind.Preference, new Dictionary<int, float> { [Fox] = 4f, [Rabbit] = 0.5f }),
            [Cheese] = new(Cheese, "Cheese", null, BaitKind.Other, new Dictionary<int, float> { [Rat] = 2f }),
        },
        new Dictionary<int, PreyItem>
        {
            [Rabbit] = new(Rabbit, "Rabbit", null, IsRare: false),
            [Bird] = new(Bird, "Sparrow", null, IsRare: false),
            [Rat] = new(Rat, "Rat", null, IsRare: false),
            [Fox] = new(Fox, "Fox", null, IsRare: true),
        },
        new Dictionary<int, string> { [Pantry] = "Pantry", [Attic] = "Attic" },
        new Dictionary<int, IReadOnlyDictionary<int, float>> { [Attic] = new Dictionary<int, float> { [Bird] = 2f } });

    public List<Trap> Traps { get; } = new();

    public List<BaitUnit> Stock { get; } = new();

    public int PlayerFloor { get; set; } = 1;

    public string? SaveKey { get; set; } = "slot-1";

    public OddsBonus Bonus { get; set; } = OddsBonus.None;

    /// <summary>Set to make every read find no game or a game not ready, with its reason.</summary>
    public TrapRead? ReadInstead { get; set; }

    public HashSet<long> Refusing { get; } = new();

    /// <summary>Which bait went in which trap, in the order the game was asked.</summary>
    public List<(long TrapId, int ItemId)> Baited { get; } = new();

    public List<(long TrapId, bool FromStore)> Taken { get; } = new();

    public List<long> Dismantled { get; } = new();

    public static Trap T(long id, int type = Cage, int floor = 1, int room = Pantry, int prey = 0, int bait = 0, int store = 0,
        int dur = 100, int max = 100, bool queued = false, float x = 0f, TrapState state = TrapState.Armed) =>
        new(id, type, room, floor, new Spot(x, 0f, 0f), state, prey, bait, store, dur, max, queued);

    public static BaitUnit Bag(long instance, int item, int count, int? hoursLeft = null) => new(instance, item, count, IsInBag: true, hoursLeft);

    public static BaitUnit Home(long instance, int item, int count, int? hoursLeft = null) => new(instance, item, count, IsInBag: false, hoursLeft);

    public static BaitPreset Preset(string id, params BaitStep[] steps) =>
        new(id, "Preset " + id, steps, LastResort: false, BaitSource.BagThenHome, SpoilFirst: true, AutoRebait.Off);

    public static BaitStep Step(int item, int keep = 0) => new(item, keep);

    /// <summary>A book with the given presets, these baits allowed, and the first preset the default for every trap type.</summary>
    public static PresetBook Book(IEnumerable<int> allowed, params BaitPreset[] presets) => new(
        presets,
        allowed.ToHashSet(),
        new[] { Cage, Net, Mousetrap, AutoCage }.ToDictionary(type => type, _ => presets[0].Id),
        new Dictionary<string, IReadOnlyDictionary<long, string>>());

    public TrapWorld World => new(Catalog, Traps.ToList(), Stock.Where(unit => unit.Count > 0).ToList(), Bonus, PlayerFloor, new Spot(0f, 0f, 0f), SaveKey);

    public TrapRead Read() => ReadInstead ?? new TrapRead.Ready(World);

    public bool SetBait(long trapId, long instanceId, int itemId)
    {
        var at = Traps.FindIndex(trap => trap.Id == trapId);
        var stack = Stock.FindIndex(unit => unit.InstanceId == instanceId);

        if (Refusing.Contains(trapId) || at < 0 || stack < 0 || Stock[stack].ItemId != itemId || Stock[stack].Count <= 0) return false;

        Traps[at] = Traps[at] with { BaitId = itemId };
        Stock[stack] = Stock[stack] with { Count = Stock[stack].Count - 1 };
        Baited.Add((trapId, itemId));

        return true;
    }

    public bool Take(long trapId, bool fromStore)
    {
        var at = Traps.FindIndex(trap => trap.Id == trapId);

        if (Refusing.Contains(trapId) || at < 0) return false;

        Traps[at] = Traps[at] with { IsTakeQueued = true };
        Taken.Add((trapId, fromStore));

        return true;
    }

    public bool Dismantle(long trapId)
    {
        if (Refusing.Contains(trapId)) return false;

        Dismantled.Add(trapId);

        return true;
    }

    public int FloorCost(int fromFloor, int toFloor) => Floors.Steps(fromFloor, toFloor);
}
