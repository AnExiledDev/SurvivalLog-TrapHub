using System.Collections.Generic;
using System.Globalization;

namespace TrapHub.Core;

/// <summary>One piece of on-screen text in the two languages the game ships. {0}, {1} are filled in by Say.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;

    public string Say(bool isChinese, params object[] args) => string.Format(CultureInfo.InvariantCulture, Pick(isChinese), args);
}

/// <summary>A phrase about a count: English says "1 trap" and "2 traps", Chinese has one form for both. {0} is the count.</summary>
public readonly record struct Counted(Phrase One, Phrase Many)
{
    public string Say(bool isChinese, int count, params object[] more)
    {
        var args = new object[more.Length + 1];

        args[0] = count;
        more.CopyTo(args, 1);

        return (count == 1 ? One : Many).Say(isChinese, args);
    }
}

/// <summary>The words the mod puts on screen: press results, reasons, and the panel's own labels.</summary>
public static class Texts
{
    private const string Log = "BepInEx/LogOutput.log";

    public static readonly Phrase FirstPresetName = new("Any universal bait", "任意广谱诱饵");
    public static readonly Phrase NewPresetName = new("New preset", "新预设");

    public static readonly Phrase NoGame = new("Load a game first.", "请先载入游戏。");
    public static readonly Phrase NotReady = new("Can't manage traps right now: {0}.", "现在无法管理陷阱：{0}。");
    public static readonly Phrase NoWorld = new("the game world is not loaded", "游戏世界尚未载入");
    public static readonly Phrase Away = new("you are away from home", "你不在家中");
    public static readonly Phrase ReadFailed = new("the traps could not be read, see " + Log, "无法读取陷阱，详见 " + Log);
    public static readonly Phrase Gone = new("That trap is gone.", "这个陷阱已经不在了。");
    public static readonly Counted BaitedCount = new(new("Baited 1 trap.", "已为 {0} 个陷阱布饵。"), new("Baited {0} traps.", "已为 {0} 个陷阱布饵。"));
    public static readonly Counted AutoBaited = new(new("Auto re-bait: baited 1 trap.", "自动布饵：已为 {0} 个陷阱布饵。"), new("Auto re-bait: baited {0} traps.", "自动布饵：已为 {0} 个陷阱布饵。"));
    public static readonly Phrase HadNoBait = new("{0} had no allowed bait left.", "{0} 个没有可用的允许诱饵。");
    public static readonly Phrase GameRefused = new("The game refused {0}.", "游戏拒绝了 {0} 个。");
    public static readonly Phrase BaitedOne = new("{0} baited with {1}.", "{0}已放置{1}作诱饵。");
    public static readonly Phrase NothingToBait = new("Every trap that can take bait has some.", "所有可布饵的陷阱都已有诱饵。");
    public static readonly Phrase AlreadyBaited = new("{0} already has bait.", "{0}已有诱饵。");
    public static readonly Phrase CantBait = new("{0} can't take bait now.", "{0}现在无法放置诱饵。");
    public static readonly Phrase NoPreset = new("{0} has no bait preset. Pick one in the Traps tab.", "{0}没有诱饵预设。请在「陷阱」页选择一个。");
    public static readonly Phrase NoBaitLeft = new("No allowed bait left for {0} (preset {1}).", "{0}没有可用的允许诱饵（预设「{1}」）。");
    public static readonly Counted Taking = new(new("Collecting from 1 trap.", "正在从 {0} 个陷阱拾取猎物。"), new("Collecting from {0} traps on {1} floor(s).", "正在从 {0} 个陷阱拾取猎物（{1} 个楼层）。"));
    public static readonly Phrase NothingToTake = new("No catches to collect.", "没有可拾取的猎物。");
    public static readonly Phrase AlreadyTaking = new("No new catches: {0} already queued.", "没有新的猎物：{0} 个已在队列中。");
    public static readonly Phrase Dismantling = new("Dismantling {0}.", "正在拆除{0}。");
    public static readonly Phrase Refused = new("The game would not do that for {0}.", "游戏拒绝了对{0}的操作。");
    public static readonly Phrase Failed = new("Trap Hub failed, see " + Log + ".", "Trap Hub 出错，详见 " + Log + "。");

    public static readonly Phrase PresetSaved = new("Preset {0} saved.", "预设「{0}」已保存。");
    public static readonly Phrase PresetDeleted = new("Preset {0} deleted.", "预设「{0}」已删除。");
    public static readonly Phrase PresetBad = new("That preset could not be read.", "无法读取该预设。");
    public static readonly Phrase AssignedOwn = new("{0} trap(s) now use {1}.", "{0} 个陷阱现在使用「{1}」。");
    public static readonly Phrase AssignedDefault = new("{0} trap(s) now use their type's default.", "{0} 个陷阱现在使用类型默认预设。");
    public static readonly Phrase NoSave = new("Trap choices are kept per save, and this game has no save yet. Save the game first.", "陷阱的预设选择按存档保存，而这局游戏还没有存档。请先保存游戏。");
    public static readonly Phrase NotSaved = new("trap-hub.json could not be written, so the change lasts until the game is closed. See " + Log + ".", "无法写入 trap-hub.json，更改只在关闭游戏前有效。详见 " + Log + "。");
    public static readonly Counted AllowedCount = new(new("1 bait allowed.", "已允许 {0} 种诱饵。"), new("{0} baits allowed.", "已允许 {0} 种诱饵。"));
    public static readonly Counted BlockedCount = new(new("1 bait blocked.", "已禁止 {0} 种诱饵。"), new("{0} baits blocked.", "已禁止 {0} 种诱饵。"));
    public static readonly Phrase SoundSet = new("{0}: {1}.", "{0}：{1}。");
    public static readonly Phrase SoundsElsewhere = new("Quiet Traps is installed, so it handles catch sounds.", "已安装 Quiet Traps，捕获声音由它处理。");

    public static readonly Phrase SoundCage = new("Live Capture Cage", "活捉笼");
    public static readonly Phrase SoundNet = new("Simple Bird Net", "简易捕鸟网");
    public static readonly Phrase SoundOther = new("Mousetrap, Iron Animal Trap, Auto Trap Cage", "简易捕鼠夹、铁制捕兽夹、自动诱捕笼");
    public static readonly Phrase ModeNormal = new("Normal", "正常");
    public static readonly Phrase ModeOnce = new("Chime once", "只响一次");
    public static readonly Phrase ModeMuted = new("Muted", "静音");

    /// <summary>The words page.js draws the window, the HUD button and its tip with, by the key it looks them up by.</summary>
    public static readonly IReadOnlyList<(string Key, Phrase Phrase)> Page = new (string, Phrase)[]
    {
        ("tabTraps", new("Traps", "陷阱")),
        ("tabPresets", new("Bait presets", "诱饵预设")),
        ("tabAllowed", new("Allowed bait", "允许的诱饵")),
        ("tabSettings", new("Settings", "设置")),
        ("nTrap", new("1 trap", "1 个陷阱")),
        ("nTraps", new("{0} traps", "{0} 个陷阱")),
        ("cPrey", new("{0} with prey", "{0} 个有猎物")),
        ("cNoBait", new("{0} without bait", "{0} 个未布饵")),
        ("cWorn", new("{0} about to break", "{0} 个即将损坏")),
        ("cHalted", new("{0} halted", "{0} 个停工")),
        ("takeAll", new("Take all ({0})", "全部拾取（{0}）")),
        ("takeFloor", new("Take this floor ({0})", "拾取本层（{0}）")),
        ("rebaitAll", new("Re-bait all ({0})", "全部布饵（{0}）")),
        ("floor", new("Floor {0}", "{0} 楼")),
        ("here", new("You are here", "你在这里")),
        ("stArmed", new("Armed", "已布置")),
        ("stPrey", new("Prey", "有猎物")),
        ("stNoBait", new("No bait", "未布饵")),
        ("stHalted", new("Halted", "停工")),
        ("caught", new("Caught: {0}", "已捕获：{0}")),
        ("next", new("Re-bait uses: {0}", "布饵将使用：{0}")),
        ("nextNone", new("No allowed bait left for its preset", "该预设没有可用的允许诱饵")),
        ("catches", new("{0} catches left", "还能捕获 {0} 次")),
        ("breaks", new("Breaks on its next catch", "下次捕获后损坏")),
        ("store", new("Store: {0}", "储物：{0}")),
        ("queued", new("Walk queued", "已排队")),
        ("take", new("Take", "拾取")),
        ("rebait", new("Re-bait", "布饵")),
        ("dismantle", new("Dismantle", "拆除")),
        ("confirm", new("Sure?", "确定？")),
        ("preset", new("Bait preset", "诱饵预设")),
        ("typeDefault", new("Type default ({0})", "类型默认（{0}）")),
        ("useDefault", new("Type default", "类型默认")),
        ("ownPick", new("Picked for it", "单独指定")),
        ("none", new("None", "无")),
        ("empty", new("No traps placed at home yet.", "家里还没有放置陷阱。")),
        ("noMatch", new("No traps match these filters.", "没有符合筛选条件的陷阱。")),
        ("keepN", new("keep {0}", "保留 {0}")),
        ("notTaken", new("This trap does not take it", "此陷阱不使用")),
        ("oddsNow", new("Catch odds now", "当前捕获率")),
        ("otherPrey", new("Other prey", "其他猎物")),
        ("nothing", new("Nothing", "空手而归")),
        ("wear", new("Wear", "耐久")),
        ("needsYou", new("Needs you", "需要处理")),
        ("allFine", new("All your traps are armed and baited.", "所有陷阱都已布置并有诱饵。")),
        ("pickTrap", new("Pick a trap to see its bait and catch odds.", "选择一个陷阱以查看诱饵和捕获率。")),
        ("show", new("Show", "显示")),
        ("fAll", new("All", "全部")),
        ("fPrey", new("With prey", "有猎物")),
        ("fNoBait", new("Without bait", "未布饵")),
        ("fBreaks", new("About to break", "即将损坏")),
        ("trapType", new("Trap type", "陷阱类型")),
        ("rooms", new("Rooms", "房间")),
        ("groupBy", new("Group by", "分组方式")),
        ("gFloor", new("Floor", "楼层")),
        ("gRoom", new("Room", "房间")),
        ("gType", new("Type", "类型")),
        ("gState", new("State", "状态")),
        ("baitOwn", new("Bait you own", "你拥有的诱饵")),
        ("selected", new("{0} selected", "已选 {0} 个")),
        ("clear", new("Clear", "清除")),
        ("assignTo", new("Set bait preset:", "设置诱饵预设：")),
        ("noSaveYet", new("Save the game once to pick presets per trap.", "保存一次游戏后即可为每个陷阱选择预设。")),
        ("newPreset", new("New preset", "新建预设")),
        ("name", new("Name", "名称")),
        ("steps", new("Bait order", "诱饵顺序")),
        ("stepsNote", new("Trap Hub uses the first one you have more of than you keep.", "使用第一个数量多于保留数的诱饵。")),
        ("noSteps", new("No baits yet. Add some below.", "还没有诱饵，请在下方添加。")),
        ("keep", new("Keep", "保留")),
        ("own", new("Own {0}", "拥有 {0}")),
        ("notAllowed", new("Not allowed, skipped", "未允许，跳过")),
        ("lastResort", new("Then any allowed universal bait", "最后使用任意允许的广谱诱饵")),
        ("rules", new("Rules", "规则")),
        ("source", new("Take bait from", "诱饵来源")),
        ("srcBag", new("Bag only", "仅背包")),
        ("srcHome", new("Bag, then home storage", "先背包，再家中储物")),
        ("spoilFirst", new("Use the food closest to spoiling first", "优先使用快过期的食物")),
        ("auto", new("Auto re-bait", "自动布饵")),
        ("autoOff", new("Off", "关闭")),
        ("autoNearby", new("On my floor", "在我所在楼层")),
        ("autoAlways", new("Always", "始终")),
        ("autoNote", new("Puts bait in an empty trap by itself, from this preset, without a walk.", "自动从此预设为空陷阱放置诱饵，无需走过去。")),
        ("defaultFor", new("Default for", "默认用于")),
        ("save", new("Save", "保存")),
        ("delete", new("Delete", "删除")),
        ("addBait", new("Add a bait", "添加诱饵")),
        ("search", new("Search", "搜索")),
        ("usedBy", new("Traps using it", "使用此预设的陷阱")),
        ("usedNone", new("No trap uses this preset yet.", "还没有陷阱使用此预设。")),
        ("more", new("{0} more. Search to narrow the list.", "还有 {0} 个，请搜索以缩小范围。")),
        ("unsaved", new("Unsaved changes", "有未保存的更改")),
        ("allowedNote", new("Trap Hub only puts bait you allow here in your traps. Everything starts blocked.", "Trap Hub 只会把你在这里允许的诱饵放进陷阱。所有物品默认禁止。")),
        ("fOwned", new("I own", "我拥有的")),
        ("fAllowed", new("Allowed", "已允许")),
        ("fBlocked", new("Blocked", "已禁止")),
        ("fInPreset", new("In a preset", "在预设中")),
        ("kinds", new("Bait kind", "诱饵类型")),
        ("kUniversal", new("Universal", "广谱诱饵")),
        ("kPreference", new("Preference", "偏好诱饵")),
        ("kSpecialized", new("Specialized", "专精诱饵")),
        ("kBalanced", new("Balanced", "均衡诱饵")),
        ("kOther", new("Other", "其他")),
        ("quick", new("Quick picks", "快捷操作")),
        ("allowPresets", new("Allow everything in my presets", "允许预设中的全部诱饵")),
        ("allowUniversal", new("Allow universal bait I own", "允许我拥有的广谱诱饵")),
        ("allowShown", new("Allow all shown", "允许全部显示的")),
        ("blockShown", new("Block all shown", "禁止全部显示的")),
        ("allowed", new("Allowed", "已允许")),
        ("blocked", new("Blocked", "已禁止")),
        ("allow", new("Allow", "允许")),
        ("block", new("Block", "禁止")),
        ("sOwned", new("Owned", "已拥有")),
        ("sNotOwned", new("Not owned", "未拥有")),
        ("noBaitMatch", new("No bait matches these filters.", "没有符合筛选条件的诱饵。")),
        ("pickBait", new("Pick a bait to see where it is used.", "选择一个诱饵以查看用途。")),
        ("inPresets", new("In your presets", "在你的预设中")),
        ("presetStep", new("{0}, step {1}", "{0}，第 {1} 步")),
        ("skipped", new("Skipped while blocked", "禁止期间会被跳过")),
        ("notInPresets", new("Not in any preset.", "不在任何预设中。")),
        ("drawsIn", new("Draws in", "吸引的猎物")),
        ("takenByHead", new("Taken by", "适用陷阱")),
        ("sounds", new("Catch sounds", "捕获声音")),
        ("soundsNote", new("The sound a trap plays while it holds a catch. Chime once plays it for a few seconds, then goes quiet.", "陷阱捕获猎物后发出的声音。「只响一次」会播放几秒后静音。")),
        ("sCage", SoundCage),
        ("sNet", SoundNet),
        ("sOther", SoundOther),
        ("mNormal", ModeNormal),
        ("mOnce", ModeOnce),
        ("mMuted", ModeMuted),
        ("quietTraps", SoundsElsewhere),
        ("keyOpen", new("{0} opens and closes this window.", "按 {0} 打开或关闭此窗口。")),
        ("keyNone", new("No key is set; use the trap button on the left of the screen.", "未设置按键；请使用屏幕左侧的陷阱按钮。")),
        ("presetFile", new("Presets and allowed bait are kept in BepInEx/config/trap-hub.json, shared by every save.", "预设和允许的诱饵保存在 BepInEx/config/trap-hub.json，所有存档共用。")),
        ("hotkey", new("Hotkey", "快捷键")),
        ("escClose", new("Esc to close", "Esc 关闭")),
        ("tip", new("Your traps: catches, bait and wear", "你的陷阱：猎物、诱饵和耐久")),
    };
}
