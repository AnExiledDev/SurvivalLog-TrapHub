using System.Collections.Generic;

namespace TrapHub.Core;

/// <summary>
/// Runs every view, plan and press once on a small made-up home, so .NET compiles that code before the player needs
/// it. Meant for a background thread at start: the first real refresh after a save loads then costs the game's frame
/// only the read itself. Touches nothing outside this class; the answer is the total length of what it wrote.
/// </summary>
public static class Warmup
{
    private const int TypeId = 1;
    private const int PreyId = 100;
    private const int BaitId = 200;
    private const int RoomId = 1;

    public static int Run()
    {
        var world = World();
        var book = PresetBook.First("Warm up") with { Allowed = new HashSet<int> { BaitId } };
        var game = new Home(world);
        var settings = new ViewSettings(new Dictionary<SoundGroup, SoundMode> { [SoundGroup.OtherTraps] = SoundMode.Once }, SoundsElsewhere: false, "Shift+J", "J");
        var written = 0;

        written += CatalogView.Write(world.Catalog).Length;
        written += TrapView.Ready(world, book, settings).Length;
        written += TrapView.NotReady(Texts.NotReady.Say(false, Texts.Away.Pick(false)), settings).Length;
        written += TrapView.NeedsYou(world);
        written += HudView.Ready(world, book, game.FloorCost, isRebait: true, "J").Length;
        written += HudView.Ready(world, book, game.FloorCost, isRebait: false, "J").Length;
        written += PresetJson.ReadBook(PresetJson.WriteBook(book))?.Presets.Count ?? 0;
        written += new DrawnFrom(isPositionDrawn: true).IsSame(world, book) ? 1 : 0;
        written += TrapMessages.Describe(TrapRun.Auto(game, world, book, 0, new Dictionary<long, double>()), false, isAuto: true).Length;
        written += TrapMessages.Describe(TrapRun.CheckAll(game, book, onlyFloor: null, isRefill: true), false).Length;
        written += TrapMessages.Describe(TrapRun.TakeAll(game, onlyFloor: null), false).Length;

        return written;
    }

    private static TrapWorld World()
    {
        var catalog = new TrapCatalog(
            new Dictionary<int, TrapType>
            {
                [TypeId] = new(TypeId, "Cage", null, HasStore: false, CatchCost: 10, EmptyWeight: 50f, new[] { new PreyRate(PreyId, 50f) }, new HashSet<int> { BaitId }),
            },
            new Dictionary<int, BaitItem> { [BaitId] = new(BaitId, "Carrot", null, BaitKind.Universal, new Dictionary<int, float>()) },
            new Dictionary<int, PreyItem> { [PreyId] = new(PreyId, "Rabbit", null, IsRare: false) },
            new Dictionary<int, string> { [RoomId] = "Pantry" },
            new Dictionary<int, IReadOnlyDictionary<int, float>> { [RoomId] = new Dictionary<int, float> { [PreyId] = 2f } });
        var traps = new List<Trap>
        {
            new(1, TypeId, RoomId, 1, new Spot(1f, 0f, 0f), TrapState.Prey, PreyId, 0, 0, 10, 100, false),
            new(2, TypeId, RoomId, 1, new Spot(2f, 0f, 0f), TrapState.NoBait, 0, 0, 0, 100, 100, false),
            new(3, TypeId, RoomId, 2, new Spot(3f, 0f, 0f), TrapState.Armed, 0, BaitId, 0, 100, 100, false),
            new(4, TypeId, RoomId, -1, new Spot(4f, 0f, 0f), TrapState.Halted, 0, 0, 0, 0, 100, false),
        };
        var stock = new List<BaitUnit> { new(1, BaitId, 3, IsInBag: true, 12), new(2, BaitId, 5, IsInBag: false, null) };

        return new TrapWorld(catalog, traps, stock, OddsBonus.None, 1, new Spot(0f, 0f, 0f), "warm-up");
    }

    /// <summary>A home that refuses every press, so the presses run all the way through without changing anything.</summary>
    private sealed class Home : ITrapGame
    {
        private readonly TrapWorld _world;

        public Home(TrapWorld world)
        {
            _world = world;
        }

        public TrapRead Read() => new TrapRead.Ready(_world);

        public bool SetBait(long trapId, long instanceId, int itemId) => false;

        public bool Take(long trapId, bool fromStore) => false;

        public bool Dismantle(long trapId) => false;

        public int FloorCost(int fromFloor, int toFloor) => Floors.Steps(fromFloor, toFloor);
    }
}
