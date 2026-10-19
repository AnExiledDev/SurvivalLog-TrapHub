using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using TrapHub.Core;
using Il2CppFloats = Il2CppSystem.Collections.Generic.List<float>;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace TrapHub.Adapters;

/// <summary>
/// The game's side of every press. Reads go through the trap manager the game's own HUD and trap panel use; a press
/// calls the same handler the panel's buttons send their events to. Each call fails soft: an exception is logged once
/// per kind of call and answered as a refusal.
/// </summary>
internal sealed class GameTraps : ITrapGame
{
    private const int PickupPreyAction = 1802;
    private const int TakeEverything = -1;
    private const int RarePrey = 2;
    private const string BaitTalent = "Buff/AE_TrapBaitMultiplier";

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    private IconIndex? _icons;
    private (IntPtr Config, bool IsChinese) _catalogKey;
    private TrapCatalog? _catalog;
    private Dictionary<string, (int Map, int Room)> _slots = new();

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

    /// <summary>The game's own route between two floors, counted in segments; a route it cannot give falls back to counting levels.</summary>
    public int FloorCost(int fromFloor, int toFloor)
    {
        if (fromFloor == toFloor) return 0;

        try
        {
            var route = FloorRouteHelper.GetRoute(Floors.MapId(fromFloor), Floors.MapId(toFloor));

            if (route != null && route.Count > 0) return route.Count;
        }
        catch (Exception ex)
        {
            ReportOnce("floor route", ex);
        }

        return Floors.Steps(fromFloor, toFloor);
    }

    private TrapRead ReadWorld()
    {
        var game = LoadedGame();

        if (game == null) return new TrapRead.NoGame();

        var (world, config, agents, role) = game.Value;
        var isChinese = GameLanguage.IsChinese();
        var traps = world._TrapManager;

        if (traps == null) return new TrapRead.NotReady(Texts.NoWorld.Pick(isChinese));
        if (!IsHome(agents)) return new TrapRead.NotReady(Texts.Away.Pick(isChinese));

        var catalog = Catalog(config, traps, isChinese);
        var roleId = role.InstanceId;
        var placed = ReadTraps(world, config, traps, catalog, roleId);
        var stock = ReadStock(world._ItemManager, catalog, roleId);
        var playerFloor = Floors.Level(agents.GetAgentFloor(roleId));

        return new TrapRead.Ready(new TrapWorld(catalog, placed, stock, ReadBonus(world), playerFloor, PositionOf(role), SaveSlot.CurrentKey()));
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

    private Spot SlotPosition(ConfigManager config, string slotName)
    {
        try
        {
            var position = config.GetBuildingPositionAndRotation(slotName).Item1;

            return new Spot(position.x, position.y, position.z);
        }
        catch (Exception ex)
        {
            ReportOnce("trap position", ex);

            return new Spot(0, 0, 0);
        }
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
    private List<BaitUnit> ReadStock(ItemManager? items, TrapCatalog catalog, long roleId)
    {
        var stock = new List<BaitUnit>();
        var available = items?.GetAllAvailableItems(roleId);

        for (var i = 0; available != null && i < available.Count; i++)
        {
            var item = available[i];

            if (item == null || item.ItemCount <= 0 || !catalog.Baits.ContainsKey(item.ItemConfigId)) continue;
            if (item.OwnerId != roleId && ItemManager.IsExcludedFromCostPool(item.OwnerId)) continue;

            stock.Add(new BaitUnit(item.InstanceId, item.ItemConfigId, item.ItemCount, item.OwnerId == roleId, item.TimeLeft > 0 ? item.TimeLeft : null));
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

    /// <summary>The tables only change with the language, so the catalog is built once per game and language.</summary>
    private TrapCatalog Catalog(ConfigManager config, TrapManager traps, bool isChinese)
    {
        var key = (config.Pointer, isChinese);

        if (_catalog != null && _catalogKey == key) return _catalog;

        _slots = ReadSlots(config);
        _catalog = BuildCatalog(config, traps);
        _catalogKey = key;
        _log($"read {_catalog.Types.Count} trap types, {_catalog.Baits.Count} baits, {_catalog.Prey.Count} prey, {_slots.Count} trap slots");

        return _catalog;
    }

    private TrapCatalog BuildCatalog(ConfigManager config, TrapManager traps)
    {
        var types = new Dictionary<int, TrapType>();
        var trapRows = config._Config_Trap_Dict;

        if (trapRows != null)
        {
            foreach (var row in trapRows.Values)
            {
                if (row != null) types[row.ID] = ReadType(config, traps, row);
            }
        }

        var baitIds = types.Values.SelectMany(type => type.AcceptedBait).ToHashSet();
        var preyIds = types.Values.SelectMany(type => type.Prey.Select(prey => prey.PreyId)).ToHashSet();
        var bias = ReadRoomBias(config);

        return new TrapCatalog(types, ReadBaits(config, baitIds), ReadPrey(config, preyIds), ReadRoomNames(config, bias.Keys), bias.ToDictionary(pair => pair.Key, pair => (IReadOnlyDictionary<int, float>)pair.Value.Coefficients));
    }

    private TrapType ReadType(ConfigManager config, TrapManager traps, Config_Trap row)
    {
        var name = Translate(config, row.Name) ?? NonEmpty(row.Name_Local) ?? $"Trap {row.ID}";
        var preyIds = Ints(row.Prey_Id);
        var rates = Floats(row.Base_Rate);
        var prey = preyIds.Select((id, i) => new PreyRate(id, i < rates.Count ? rates[i] : 0f)).ToList();

        return new TrapType(row.ID, name, Icons().Resolve(NonEmpty(row.WebIcon)), row.Prey_Capacity > 0, CatchCost(traps, row), row.Empty_Weight, prey, Ints(row.Bait_id).ToHashSet());
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

    private Dictionary<int, BaitItem> ReadBaits(ConfigManager config, ISet<int> baitIds)
    {
        var baits = new Dictionary<int, BaitItem>();

        foreach (var id in baitIds)
        {
            var row = config.Get_Config_TrapBait(id);
            var item = config.Get_Config_Item(id);
            var coefficients = new Dictionary<int, float>();

            if (row != null)
            {
                var prey = Ints(row.Prey_Id);
                var values = Floats(row.Bait_coefficient);

                for (var i = 0; i < prey.Count && i < values.Count; i++) coefficients[prey[i]] = values[i];
            }

            baits[id] = new BaitItem(id, ItemName(config, item, id), ItemIcon(item), BaitKinds.FromEffect(row?.Effect_Des_Local), coefficients);
        }

        return baits;
    }

    private Dictionary<int, PreyItem> ReadPrey(ConfigManager config, ISet<int> preyIds)
    {
        var prey = new Dictionary<int, PreyItem>();

        foreach (var id in preyIds)
        {
            var item = config.Get_Config_Item(id);

            prey[id] = new PreyItem(id, ItemName(config, item, id), ItemIcon(item), item != null && item.Prey_Rarity >= RarePrey);
        }

        return prey;
    }

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

            bias[row.ID] = (NonEmpty(row.Des_name_Local), coefficients);
        }

        return bias;
    }

    /// <summary>Names every room a trap slot sits in, from the map's room table, then the trap bias table's own name.</summary>
    private Dictionary<int, string> ReadRoomNames(ConfigManager config, IEnumerable<int> biasRooms)
    {
        var names = new Dictionary<int, string>();
        var roomIds = _slots.Values.Select(slot => slot.Room).Concat(biasRooms).Where(id => id > 0).Distinct();

        foreach (var id in roomIds)
        {
            var room = config.Get_Config_MapRoom(id);
            var bias = config.Get_Config_TrapRoomBias(id);
            var name = room == null ? null : Translate(config, room.Name) ?? NonEmpty(room.Name_Local);

            name ??= bias == null ? null : Translate(config, bias.Des_name_Local) ?? NonEmpty(bias.Des_name_Local);

            if (name != null) names[id] = name;
        }

        return names;
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

    private static string ItemName(ConfigManager config, Config_Item? item, int id) =>
        (item == null ? null : Translate(config, item.ItemName) ?? NonEmpty(item.ItemName_Local) ?? NonEmpty(item.ItemName)) ?? $"Item {id}";

    private string? ItemIcon(Config_Item? item) => item == null ? null : Icons().Resolve(NonEmpty(item.WebIcon) ?? NonEmpty(item.Icon));

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

    private IconIndex Icons() => _icons ??= WebIcons.Load(_log);

    private static string? Translate(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        var text = config.GetLocalTxt(key);

        return string.IsNullOrEmpty(text) || text == key ? null : text;
    }

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}
