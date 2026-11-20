using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using ExiledToolkit.Web;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using TrapHub.Core;
using Il2CppFloats = Il2CppSystem.Collections.Generic.List<float>;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace TrapHub.Adapters;

/// <summary>
/// The game's side of every press. Reads go through the trap manager the game's HUD and trap panel use; a press
/// calls the same handler the panel's buttons send their events to. Each call fails soft: an exception is logged once
/// per kind of call and answered as a refusal.
/// </summary>
internal sealed class GameTraps : ITrapGame
{
    private const int PickupPreyAction = 1802;
    private const int TakeEverything = -1;
    private const int RarePrey = 2;
    private const string BaitTalent = "Buff/AE_TrapBaitMultiplier";
    // The catalog is read a slice per frame, each slice stopping once it has used this much of the frame.
    private const double SliceMilliseconds = 2.0;

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    private Task<(IconIndex Icons, string Note)>? _iconScan;
    private IconIndex? _icons;
    private (IntPtr Config, bool IsChinese) _catalogKey;
    private TrapCatalog? _catalog;
    private Dictionary<string, (int Map, int Room)> _slots = new();

    private SlicedJob? _build;
    private (IntPtr Config, bool IsChinese) _buildKey;
    private TrapManager? _buildTraps;
    private TrapCatalog? _built;
    private Dictionary<string, (int Map, int Room)> _builtSlots = new();

    // Both come from the game's tables and map, so they hold until another game is loaded.
    private (IntPtr Config, IntPtr World) _loadKey;
    private readonly Dictionary<string, Spot> _positions = new();
    private readonly Dictionary<(int From, int To), int> _floorCosts = new();

    public GameTraps(Action<string> log)
    {
        _log = log;
    }

    public TrapRead Read()
    {
        try
        {
            return ReadWorld();
        }
        catch (Exception ex)
        {
            ReportOnce("read", ex);

            return new TrapRead.NotReady(Texts.ReadFailed.Pick(GameLanguage.IsChinese()));
        }
    }

    /// <summary>
    /// Puts one portion of bait in an empty trap through the handler behind the trap panel's bait list. The handler
    /// says nothing when it declines, so the trap is read again: only bait that is now in the trap counts.
    /// </summary>
    public bool SetBait(long trapId, long instanceId, int itemId)
    {
        try
        {
            var traps = Traps();
            var trap = traps?.GetTrapById(trapId);

            if (traps == null || trap == null || trap.BaitItemConfigId != 0) return false;

            traps.OnSetTrapBait(trapId, instanceId, itemId);

            var isBaited = traps.GetTrapById(trapId)?.BaitItemConfigId == itemId;

            _log(isBaited ? $"baited trap {trapId} with item {itemId}" : $"the game did not bait trap {trapId} with item {itemId}");

            return isBaited;
        }
        catch (Exception ex)
        {
            ReportOnce("set bait", ex);

            return false;
        }
    }

    /// <summary>Lines up the walk the trap panel's take button lines up: take the prey, or everything in a store.</summary>
    public bool Take(long trapId, bool fromStore)
    {
        try
        {
            var traps = Traps();

            if (traps?.GetTrapById(trapId) == null) return false;

            if (fromStore) traps!.OnStartTakeStoredPrey(trapId, TakeEverything);
            else traps!.OnStartPickupTrapPrey(trapId);

            _log($"lined up taking from trap {trapId}{(fromStore ? " (store)" : "")}");

            return true;
        }
        catch (Exception ex)
        {
            ReportOnce("take", ex);

            return false;
        }
    }

    public bool Dismantle(long trapId)
    {
        try
        {
            var traps = Traps();

            if (traps?.GetTrapById(trapId) == null) return false;

            traps!.OnStartTrapDismantle(trapId);
            _log($"lined up dismantling trap {trapId}");

            return true;
        }
        catch (Exception ex)
        {
            ReportOnce("dismantle", ex);

            return false;
        }
    }

    /// <summary>
    /// The game's route between two floors, counted in segments; a route it cannot give falls back to counting
    /// levels. The game builds a route from its floor tables alone, so each pair is asked once per loaded game.
    /// </summary>
    public int FloorCost(int fromFloor, int toFloor)
    {
        if (fromFloor == toFloor) return 0;
        if (_floorCosts.TryGetValue((fromFloor, toFloor), out var known)) return known;

        var cost = Floors.Steps(fromFloor, toFloor);

        try
        {
            var route = FloorRouteHelper.GetRoute(Floors.MapId(fromFloor), Floors.MapId(toFloor));

            if (route != null && route.Count > 0) cost = route.Count;
        }
        catch (Exception ex)
        {
            ReportOnce("floor route", ex);
        }

        _floorCosts[(fromFloor, toFloor)] = cost;

        return cost;
    }

    private TrapRead ReadWorld()
    {
        AreIconsIn();

        var game = LoadedGame();

        if (game == null) return new TrapRead.NoGame();

        var (world, config, agents, role) = game.Value;
        var isChinese = GameLanguage.IsChinese();
        var traps = world._TrapManager;

        if (traps == null) return new TrapRead.NotReady(Texts.NoWorld.Pick(isChinese));
        if (!IsHome(agents)) return new TrapRead.NotReady(Texts.Away.Pick(isChinese));

        var built = Catalog(config, traps, isChinese);

        if (built == null) return new TrapRead.NotReady(Texts.Loading.Pick(isChinese), IsStillLoading: true);

        var catalog = WithCatchCosts(built, config, traps);

        ForgetIfNewLoad(config, world);

        var roleId = role.InstanceId;
        var placed = ReadTraps(world, config, traps, catalog, roleId);
        var stock = ReadStock(world._ItemManager, catalog, roleId, world._GameTimeManager?.GetHour() ?? 0);
        var playerFloor = Floors.Level(agents.GetAgentFloor(roleId));

        return new TrapRead.Ready(new TrapWorld(catalog, placed, stock, ReadBonus(world), playerFloor, PositionOf(role), SaveSlot.CurrentKey()));
    }

    private void ForgetIfNewLoad(ConfigManager config, BattleLogicWorld world)
    {
        var key = (config.Pointer, world.Pointer);

        if (key == _loadKey) return;

        _loadKey = key;
        _positions.Clear();
        _floorCosts.Clear();
    }

    internal static (BattleLogicWorld World, ConfigManager Config, AgentManager Agents, LeadingRole Role)? LoadedGame()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var config = GameSingleton.Existing<ConfigManager>();
        var agents = world?._AgentManager;
        var role = agents?.GetLeadingRole();

        if (world == null || config == null || agents == null || role == null || world._GameTimeManager?.WorldStateType != WorldStateType.PostDisaster) return null;

        return (world, config, agents, role);
    }

    private List<Trap> ReadTraps(BattleLogicWorld world, ConfigManager config, TrapManager traps, TrapCatalog catalog, long roleId)
    {
        var found = new List<Trap>();
        var placed = traps._placedTraps;

        if (placed == null) return found;

        var actions = world._ActionManager;

        foreach (var trap in placed.Values)
        {
            if (trap == null) continue;

            try
            {
                found.Add(ReadTrap(config, actions, catalog, trap, roleId));
            }
            catch (Exception ex)
            {
                ReportOnce("trap read", ex);
            }
        }

        return found;
    }

    private Trap ReadTrap(ConfigManager config, ActionManager? actions, TrapCatalog catalog, PlacedTrap trap, long roleId)
    {
        var slotName = trap.SlotNodeName ?? "";
        var slot = _slots.TryGetValue(slotName, out var known) ? known : (Map: Floors.GroundMap, Room: 0);
        var hasStore = catalog.Types.TryGetValue(trap.TrapConfigId, out var type) && type.HasStore;

        return new Trap(
            trap.InstanceId,
            trap.TrapConfigId,
            slot.Room,
            Floors.Level(slot.Map),
            SlotPosition(config, slotName),
            StateOf(trap),
            trap.HasPrey ? trap.PreyItemConfigId : 0,
            trap.BaitItemConfigId,
            hasStore ? TrapManager.GetStoreItemCount(trap) : 0,
            trap.CurrentDurability,
            trap.MaxDurability,
            actions != null && actions.HasActionInQueue(roleId, PickupPreyAction, trap.InstanceId));
    }

    /// <summary>
    /// Where a trap slot stands, from the home map's node table. A slot never moves, so each is asked once per loaded
    /// game; the game also writes an error to its log on every ask for a slot its map does not name.
    /// </summary>
    private Spot SlotPosition(ConfigManager config, string slotName)
    {
        if (_positions.TryGetValue(slotName, out var known)) return known;

        var spot = new Spot(0, 0, 0);

        try
        {
            var position = config.GetBuildingPositionAndRotation(slotName).Item1;

            spot = new Spot(position.x, position.y, position.z);
        }
        catch (Exception ex)
        {
            ReportOnce("trap position", ex);
        }

        _positions[slotName] = spot;

        return spot;
    }

    private static TrapState StateOf(PlacedTrap trap) => TrapManager.GetTrapHudStatus(trap) switch
    {
        1 => TrapState.Prey,
        2 => TrapState.NoBait,
        3 => TrapState.Halted,
        _ => TrapState.Armed,
    };

    /// <summary>
    /// The bait the player can reach: their bag and every home storage the game counts for building costs. Storage the
    /// game keeps out of that pool (a trap's own store, set-apart chests) is skipped, as the game's bait list skips it.
    /// </summary>
    private List<BaitUnit> ReadStock(ItemManager? items, TrapCatalog catalog, long roleId, int hourNow)
    {
        var stock = new List<BaitUnit>();
        var available = items?.GetAllAvailableItems(roleId);

        for (var i = 0; available != null && i < available.Count; i++)
        {
            var item = available[i];

            if (item == null || item.ItemCount <= 0 || !catalog.Baits.ContainsKey(item.ItemConfigId)) continue;
            if (item.OwnerId != roleId && ItemManager.IsExcludedFromCostPool(item.OwnerId)) continue;

            stock.Add(new BaitUnit(item.InstanceId, item.ItemConfigId, item.ItemCount, item.OwnerId == roleId, BaitUnit.HoursLeftAt(item.StartTime, item.TimeLeft, hourNow)));
        }

        return stock;
    }

    private OddsBonus ReadBonus(BattleLogicWorld world)
    {
        try
        {
            var proficiency = world._ProficiencyManager;
            var rare = proficiency?.GetRarePreyBonus() ?? 1f;

            return new OddsBonus(TrapManager.GetTalentEffectRatio(BaitTalent), proficiency?.GetTrapEmptyWeightReduction() ?? 0f, Math.Max(1f, rare));
        }
        catch (Exception ex)
        {
            ReportOnce("odds bonus", ex);

            return OddsBonus.None;
        }
    }

    /// <summary>
    /// The tables only change with the language, so the catalog is built once per game and language. Building it asks
    /// the game about every bait and prey, so it is read a slice per frame; null until it is all read.
    /// </summary>
    private TrapCatalog? Catalog(ConfigManager config, TrapManager traps, bool isChinese)
    {
        var key = (config.Pointer, isChinese);

        if (_catalog != null && _catalogKey == key) return _catalog;

        if (_build == null || _buildKey != key)
        {
            _build = new SlicedJob(CatalogSteps(config));
            _buildKey = key;
        }

        var build = _build;
        var sliceEndsAt = Stopwatch.GetTimestamp() + (long)(SliceMilliseconds * Stopwatch.Frequency / 1000.0);

        // The trap manager is passed in on every slice, so the build never holds one from an earlier read.
        _buildTraps = traps;

        try
        {
            if (!build.Advance(() => Stopwatch.GetTimestamp() >= sliceEndsAt)) return null;
        }
        catch (Exception)
        {
            _build = null;

            throw;
        }
        finally
        {
            _buildTraps = null;
        }

        _build = null;
        _slots = _builtSlots;
        _catalog = _built!;
        _catalogKey = key;
        _built = null;
        _log($"read {_catalog.Types.Count} trap types, {_catalog.Baits.Count} baits, {_catalog.Prey.Count} prey, {_slots.Count} trap slots");

        return _catalog;
    }

    /// <summary>
    /// The catalog's reads, one bait, prey or room per step. Each step reads the game's tables whole, so a step never
    /// holds a game collection open across frames. Waits first for the icon scan, since every name comes with its icon.
    /// </summary>
    private IEnumerable<bool> CatalogSteps(ConfigManager config)
    {
        while (!AreIconsIn()) yield return false;

        var slots = ReadSlots(config);
        var types = new Dictionary<int, TrapType>();
        var trapRows = config._Config_Trap_Dict;

        if (trapRows != null)
        {
            foreach (var row in trapRows.Values)
            {
                if (row != null) types[row.ID] = ReadType(config, _buildTraps!, row);
            }
        }

        yield return true;

        var bias = ReadRoomBias(config);

        yield return true;

        var baits = new Dictionary<int, BaitItem>();

        foreach (var id in types.Values.SelectMany(type => type.AcceptedBait).ToHashSet())
        {
            baits[id] = ReadBait(config, id);

            yield return true;
        }

        var prey = new Dictionary<int, PreyItem>();

        foreach (var id in types.Values.SelectMany(type => type.Prey.Select(rate => rate.PreyId)).ToHashSet())
        {
            prey[id] = ReadPrey(config, id);

            yield return true;
        }

        var names = new Dictionary<int, string>();

        foreach (var id in slots.Values.Select(slot => slot.Room).Concat(bias.Keys).Where(id => id > 0).Distinct().ToList())
        {
            var name = RoomName(config, id, bias.TryGetValue(id, out var row) ? row.Name : null);

            if (name != null) names[id] = name;

            yield return true;
        }

        _builtSlots = slots;
        _built = new TrapCatalog(types, baits, prey, names, bias.ToDictionary(pair => pair.Key, pair => (IReadOnlyDictionary<int, float>)pair.Value.Coefficients));
    }

    private TrapType ReadType(ConfigManager config, TrapManager traps, Config_Trap row)
    {
        var name = GameNames.Translate(config, row.Name) ?? GameNames.NonEmpty(row.Name_Local) ?? $"Trap {row.ID}";
        var preyIds = Ints(row.Prey_Id);
        var rates = Floats(row.Base_Rate);
        var prey = preyIds.Select((id, i) => new PreyRate(id, i < rates.Count ? rates[i] : 0f)).ToList();

        return new TrapType(row.ID, name, Icons().Resolve(GameNames.NonEmpty(row.WebIcon)), row.Prey_Capacity > 0, CatchCost(traps, row), row.Empty_Weight, prey, Ints(row.Bait_id).ToHashSet());
    }

    /// <summary>
    /// What a catch costs a trap goes down with trapping proficiency and, in endless mode, a talent, so it is player
    /// state and is read on every read (one call per trap type). The cached catalog is replaced only when a cost moved,
    /// so the page is sent a new catalog only then.
    /// </summary>
    private TrapCatalog WithCatchCosts(TrapCatalog catalog, ConfigManager config, TrapManager traps)
    {
        var rows = config._Config_Trap_Dict;

        if (rows == null) return catalog;

        Dictionary<int, TrapType>? types = null;

        foreach (var row in rows.Values)
        {
            if (row == null || !catalog.Types.TryGetValue(row.ID, out var type)) continue;

            var cost = CatchCost(traps, row);

            if (cost == type.CatchCost) continue;

            types ??= catalog.Types.ToDictionary(pair => pair.Key, pair => pair.Value);
            types[row.ID] = type with { CatchCost = cost };
        }

        if (types == null) return catalog;

        _catalog = catalog with { Types = types };

        return _catalog;
    }

    private int CatchCost(TrapManager traps, Config_Trap row)
    {
        try
        {
            return traps.GetCaptureDurabilityCost(row);
        }
        catch (Exception ex)
        {
            ReportOnce("catch cost", ex);

            return row.Capture_Cost;
        }
    }

    // The tables are read straight, not through ConfigManager.Get_Config_*: those write an error to the game's log for
    // every id they do not have.
    private BaitItem ReadBait(ConfigManager config, int id)
    {
        var row = config._Config_TrapBait_Dict is { } bait && bait.TryGetValue(id, out var found) ? found : null;
        var item = ItemRow(config, id);
        var coefficients = new Dictionary<int, float>();

        if (row != null)
        {
            var prey = Ints(row.Prey_Id);
            var values = Floats(row.Bait_coefficient);

            for (var i = 0; i < prey.Count && i < values.Count; i++) coefficients[prey[i]] = values[i];
        }

        return new BaitItem(id, ItemName(config, item, id), ItemIcon(item), BaitKinds.FromEffect(row?.Effect_Des_Local), coefficients);
    }

    private PreyItem ReadPrey(ConfigManager config, int id)
    {
        var item = ItemRow(config, id);

        return new PreyItem(id, ItemName(config, item, id), ItemIcon(item), item != null && item.Prey_Rarity >= RarePrey);
    }

    private static Config_Item? ItemRow(ConfigManager config, int id) =>
        config._Config_Item_Dict is { } items && items.TryGetValue(id, out var item) ? item : null;

    private static Dictionary<int, (string? Name, Dictionary<int, float> Coefficients)> ReadRoomBias(ConfigManager config)
    {
        var bias = new Dictionary<int, (string?, Dictionary<int, float>)>();
        var rows = config._Config_TrapRoomBias_Dict;

        if (rows == null) return bias;

        foreach (var row in rows.Values)
        {
            if (row == null) continue;

            var prey = Ints(row.Prey_Id);
            var values = Floats(row.Room_coefficient);
            var coefficients = new Dictionary<int, float>();

            for (var i = 0; i < prey.Count && i < values.Count; i++) coefficients[prey[i]] = values[i];

            bias[row.ID] = (GameNames.NonEmpty(row.Des_name_Local), coefficients);
        }

        return bias;
    }

    /// <summary>Names a room a trap slot sits in, from the map's room table, then the trap bias table's own name.</summary>
    private static string? RoomName(ConfigManager config, int id, string? biasName)
    {
        var room = config._Config_MapRoom_Dict is { } rooms && rooms.TryGetValue(id, out var found) ? found : null;
        var name = room == null ? null : GameNames.Translate(config, room.Name) ?? GameNames.NonEmpty(room.Name_Local);

        return name ?? (biasName == null ? null : GameNames.Translate(config, biasName) ?? biasName);
    }

    private static Dictionary<string, (int Map, int Room)> ReadSlots(ConfigManager config)
    {
        var slots = new Dictionary<string, (int, int)>();
        var rows = config._Config_TrapSlot_Dict;

        if (rows == null) return slots;

        foreach (var row in rows.Values)
        {
            if (row != null && !string.IsNullOrEmpty(row.Position)) slots[row.Position] = (row.Map_Id, row.Room_Id);
        }

        return slots;
    }

    private bool IsHome(AgentManager agents)
    {
        try
        {
            return agents.IsHomeMap(ExploreSearchStateManager.GetCurrentMapId());
        }
        catch (Exception ex)
        {
            ReportOnce("home check", ex);

            return false;
        }
    }

    private static Spot PositionOf(BaseAgent agent)
    {
        var transform = AgentTools.GetAgentComponent<TransformComponent>(agent);

        if (transform == null) return new Spot(0, 0, 0);

        var position = transform.Position;

        return new Spot(position.x, position.y, position.z);
    }

    private static TrapManager? Traps() => GameSingleton.Existing<BattleLogicWorld>()?._TrapManager;

    private static string ItemName(ConfigManager config, Config_Item? item, int id) => GameNames.ItemName(config, item) ?? $"Item {id}";

    private string? ItemIcon(Config_Item? item) => item == null ? null : Icons().Resolve(GameNames.NonEmpty(item.WebIcon) ?? GameNames.NonEmpty(item.Icon));

    private static List<int> Ints(Il2CppInts? list)
    {
        var values = new List<int>();

        for (var i = 0; list != null && i < list.Count; i++) values.Add(list[i]);

        return values;
    }

    private static List<float> Floats(Il2CppFloats? list)
    {
        var values = new List<float>();

        for (var i = 0; list != null && i < list.Count; i++) values.Add(list[i]);

        return values;
    }

    private IconIndex Icons() => _icons ?? IconIndex.Empty;

    /// <summary>
    /// Whether the web icons are indexed. The first call starts the scan on a worker thread (the game ships well over a
    /// thousand icon files), so it is done long before a save is loaded; what it found is logged here, on the main thread.
    /// </summary>
    private bool AreIconsIn()
    {
        if (_icons != null) return true;

        // Exiled's Toolkit walks the folder once for every mod; its task never fails.
        _iconScan ??= WebIcons.StartScan();

        if (!_iconScan.IsCompleted) return false;

        var (icons, note) = _iconScan.Result;

        _icons = icons;
        _iconScan = null;
        _log(note);

        return true;
    }

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}
