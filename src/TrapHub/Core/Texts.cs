using System.Collections.Generic;
using ExiledToolkit.Kit;

namespace TrapHub.Core;

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
    public static readonly Phrase Loading = new("the trap list is still loading", "陷阱列表仍在载入");
    public static readonly Phrase ReadFailed = new("the traps could not be read, see " + Log, "无法读取陷阱，详见 " + Log);
    public static readonly Phrase Gone = new("That trap is gone.", "这个陷阱已经不在了。");
    public static readonly Counted BaitedCount = new(new("Baited 1 trap.", "已为 {0} 个陷阱布饵。"), new("Baited {0} traps.", "已为 {0} 个陷阱布饵。"));
    public static readonly Counted AutoBaited = new(new("Auto re-bait: baited 1 trap.", "自动布饵：已为 {0} 个陷阱布饵。"), new("Auto re-bait: baited {0} traps.", "自动布饵：已为 {0} 个陷阱布饵。"));
    public static readonly Phrase HadNoBait = new("{0} trap(s) had no allowed bait left.", "{0} 个陷阱没有可用的允许诱饵。");
    public static readonly Phrase GameRefused = new("The game refused {0} trap(s).", "游戏拒绝了 {0} 个陷阱。");
    public static readonly Phrase BaitedOne = new("{0} baited with {1}.", "{0}已放置{1}作诱饵。");
    public static readonly Phrase NothingToBait = new("Every trap that can take bait has some.", "所有可布饵的陷阱都已有诱饵。");
    public static readonly Phrase AlreadyBaited = new("{0} already has bait.", "{0}已有诱饵。");
    public static readonly Phrase CantBait = new("{0} can't take bait now.", "{0}现在无法放置诱饵。");
    public static readonly Phrase NoPreset = new("{0} has no bait preset. Pick one in the Traps tab.", "{0}没有诱饵预设。请在「陷阱」页选择一个。");
    public static readonly Phrase NoBaitLeft = new("No allowed bait left for {0} (preset {1}).", "{0}没有可用的允许诱饵（预设「{1}」）。");
    public static readonly Counted Taking = new(new("Collecting from 1 trap.", "正在从 {0} 个陷阱拾取猎物。"), new("Collecting from {0} traps on {1} floor(s).", "正在从 {0} 个陷阱拾取猎物（{1} 个楼层）。"));
    public static readonly Phrase NothingToTake = new("No catches to collect.", "没有可拾取的猎物。");
    public static readonly Phrase AlreadyTaking = new("No new catches: {0} already queued.", "没有新的猎物：{0} 个已在队列中。");
    public static readonly Phrase NothingToCheck = new("Nothing to check: no catches waiting and every trap that can take bait has some.", "无需巡查：没有待拾取的猎物，所有可布饵的陷阱都已有诱饵。");
    public static readonly Phrase CheckPop = new("Check x{0}", "巡查 \u00d7{0}");
    public static readonly Phrase Dismantling = new("Dismantling {0}.", "正在拆除{0}。");
    public static readonly Phrase Refused = new("The game would not do that for {0}.", "游戏拒绝了对{0}的操作。");
    /// <summary>Who a toast is from, on its first line.</summary>
    public static readonly Phrase Source = new("Trap Hub", "陷阱中心");

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
        ("tabBait", new("Bait", "诱饵")),
        ("tabSettings", new("Settings", "设置")),
        ("nTrap", new("1 trap", "1 个陷阱")),
        ("nTraps", new("{0} traps", "{0} 个陷阱")),
        ("sumNeed", new("{0} need you", "{0} 个需要处理")),
        ("floor", new("Floor {0}", "{0} 楼")),
        ("here", new("You are here", "你在这里")),
        ("stArmed", new("Armed", "已布置")),
        ("stPrey", new("Prey", "有猎物")),
        ("stNoBait", new("No bait", "未布饵")),
        ("stHalted", new("Full", "已满")),
        ("caught", new("Caught: {0}", "已捕获：{0}")),
        ("next", new("Re-bait uses: {0}", "布饵将使用：{0}")),
        ("nextShort", new("Empty, will use {0}", "空置，将使用{0}")),
        ("nextNone", new("No allowed bait left for its preset", "该预设没有可用的允许诱饵")),
        ("catches", new("{0} catches left", "还能捕获 {0} 次")),
        ("breaks", new("Breaks on its next catch", "下次捕获后损坏")),
        ("store", new("Catches stored: {0}", "已储存的猎物：{0}")),
        ("queued", new("Walk queued", "已排队")),
        ("take", new("Take", "拾取")),
        ("rebait", new("Re-bait", "布饵")),
        ("dismantle", new("Dismantle", "拆除")),
        ("confirm", new("Sure?", "确定？")),
        ("preset", new("Bait preset", "诱饵预设")),
        ("typeDefault", new("Type default ({0})", "类型默认（{0}）")),
        ("useDefault", new("Type default", "类型默认")),
        ("ownPick", new("Custom", "自定义")),
        ("none", new("None", "无")),
        ("empty", new("No traps placed at home yet.", "家里还没有放置陷阱。")),
        ("noMatch", new("No traps match.", "没有符合条件的陷阱。")),
        ("keepN", new("keep {0}", "保留 {0}")),
        ("notTaken", new("This trap does not take it", "此陷阱不使用")),
        ("oddsNow", new("Catch odds now", "当前捕获率")),
        ("otherPrey", new("Other prey", "其他猎物")),
        ("nothing", new("Nothing", "空手而归")),
        ("wear", new("Wear", "耐久")),
        ("needsYou", new("Needs you", "需要处理")),
        ("fAll", new("All", "全部")),
        ("allFine", new("All your traps are armed and baited.", "所有陷阱都已布置并有诱饵。")),
        ("fBreaks", new("About to break", "即将损坏")),
        ("findTrap", new("Find a trap, room or prey", "查找陷阱、房间或猎物")),
        ("checkAll", new("Check all traps ({0})", "巡查所有陷阱（{0}）")),
        ("checkNote", new("Collect {0}, then bait {1} from your bait presets.", "拾取 {0} 个猎物，再按诱饵预设布饵 {1} 个。")),
        ("checkFloor", new("This floor only ({0})", "仅本层（{0}）")),
        ("takeOnly", new("Collect only ({0})", "只拾取（{0}）")),
        ("baitOnly", new("Bait only ({0})", "只布饵（{0}）")),
        ("tCollect", new("Collect", "拾取")),
        ("tBait", new("Bait", "布饵")),
        ("tNoBaitLeft", new("No bait left", "没有可用诱饵")),
        ("tNoBaitLeftSub", new("Allow more bait, or add some to its preset", "允许更多诱饵，或在预设中添加诱饵")),
        ("tBreaks", new("Breaks soon", "即将损坏")),
        ("back", new("Back", "返回")),
        ("thisTrap", new("This trap", "此陷阱")),
        ("nextBait", new("Next bait", "下一个诱饵")),
        ("inTrap", new("In the trap", "陷阱内")),
        ("noSaveYet", new("Save the game once to pick presets per trap.", "保存一次游戏后即可为每个陷阱选择预设。")),
        ("tabPresets", new("Bait presets", "诱饵预设")),
        ("typeUses", new("Each trap type uses", "各类陷阱默认使用")),
        ("newPreset", new("New preset", "新建预设")),
        ("name", new("Name", "名称")),
        ("steps", new("Bait order", "诱饵顺序")),
        ("stepsNote", new("Trap Hub uses the first bait you have more of than the number you keep.", "Trap Hub 会使用第一个数量多于保留数的诱饵。")),
        ("noSteps", new("No baits yet. Add some below.", "还没有诱饵，请在下方添加。")),
        ("keep", new("Keep", "保留")),
        ("own", new("You have {0}", "你有 {0} 个")),
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
        ("autoNote", new("Baits empty traps from this preset without you walking over to them.", "自动用此预设为空陷阱布饵，不用走过去。")),
        ("defaultFor", new("Default for", "默认用于")),
        ("save", new("Save", "保存")),
        ("delete", new("Delete", "删除")),
        ("addBait", new("Add a bait", "添加诱饵")),
        ("search", new("Search", "搜索")),
        ("usedBy", new("Traps using it", "使用此预设的陷阱")),
        ("usedNone", new("No trap uses this preset yet.", "还没有陷阱使用此预设。")),
        ("more", new("{0} more. Search to narrow the list.", "还有 {0} 个，请搜索以缩小范围。")),
        ("unsaved", new("Unsaved changes", "有未保存的更改")),
        ("tabAllowed", new("Allowed bait", "允许的诱饵")),
        ("allowedNote", new("Trap Hub only puts bait you allow here in your traps. Everything starts blocked.", "Trap Hub 只会把你在这里允许的诱饵放进陷阱。所有物品默认禁止。")),
        ("fOwned", new("I own", "我拥有的")),
        ("fAllowed", new("Allowed", "已允许")),
        ("fBlocked", new("Blocked", "已禁止")),
        ("fInPreset", new("In a preset", "在预设中")),
        ("kUniversal", new("Universal", "广谱诱饵")),
        ("kPreference", new("Preference", "偏好诱饵")),
        ("kSpecialized", new("Specialized", "专精诱饵")),
        ("kBalanced", new("Balanced", "均衡诱饵")),
        ("kOther", new("Other", "其他")),
        ("allowPresets", new("Allow everything in my presets", "允许预设中的全部诱饵")),
        ("allowUniversal", new("Allow universal bait I own", "允许我拥有的广谱诱饵")),
        ("allowShown", new("Allow all shown", "允许全部显示的")),
        ("blockShown", new("Block all shown", "禁止全部显示的")),
        ("allowed", new("Allowed", "已允许")),
        ("blocked", new("Blocked", "已禁止")),
        ("allow", new("Allow", "允许")),
        ("block", new("Block", "禁止")),
        ("noBaitMatch", new("No bait matches these filters.", "没有符合筛选条件的诱饵。")),
        ("inPresets", new("In your presets", "在你的预设中")),
        ("presetStep", new("{0}, step {1}", "{0}，第 {1} 步")),
        ("skipped", new("Skipped while blocked", "禁止期间会被跳过")),
        ("notInPresets", new("Not in any preset.", "不在任何预设中。")),
        ("drawsIn", new("Attracts", "吸引的猎物")),
        ("takenByHead", new("Traps that take it", "适用陷阱")),
        ("sounds", new("Catch sounds", "捕获声音")),
        ("soundsNote", new("The sound a trap plays while it holds a catch. Chime once plays it for a few seconds, then goes quiet.", "陷阱捕获猎物后发出的声音。「只响一次」会播放几秒后静音。")),
        ("sCage", SoundCage),
        ("sNet", SoundNet),
        ("sOther", SoundOther),
        ("mNormal", ModeNormal),
        ("mOnce", ModeOnce),
        ("mMuted", ModeMuted),
        ("quietTraps", SoundsElsewhere),
        ("keys", new("Keys", "按键")),
        ("keyCheck", new("Check traps", "巡查陷阱")),
        ("keyCheckNote", new("Collects every catch, then baits every empty trap from its bait preset.", "拾取所有猎物，再按诱饵预设为空陷阱布饵。")),
        ("keyWindow", new("Open Trap Hub", "打开 Trap Hub")),
        ("keyWindowNote", new("Change the keys in the mod config: PanelKey, PanelKeyShift and CheckKey.", "在模组配置中修改按键：PanelKey、PanelKeyShift 和 CheckKey。")),
        ("keyOff", new("Off", "未设置")),
        ("footWindow", new("this window", "此窗口")),
        ("keyNone", new("No key is set. Use the trap button on the left of the screen.", "未设置按键。请使用屏幕左侧的陷阱按钮。")),
        ("presetFile", new("Presets and allowed bait are kept in BepInEx/config/trap-hub.json, shared by every save.", "预设和允许的诱饵保存在 BepInEx/config/trap-hub.json，所有存档共用。")),
        ("escClose", new("Esc to close", "Esc 关闭")),
        ("tip", new("Your traps: catches, bait and wear", "你的陷阱：猎物、诱饵和耐久")),
        ("hEmpty", new("empty, will use", "空，将使用")),
        ("hNoBait", new("empty, no bait left", "空，没有可用诱饵")),
        ("hWornOut", new("worn out, repair or replace", "已损坏，请修理或更换")),
        ("hFull", new("full, collect to restart", "已满，拾取后继续")),
        ("hLeft", new("{0} left", "剩 {0} 次")),
        ("hStored", new("Stored catch", "储物")),
        ("hNextCheck", new("Next check:", "下次巡查：")),
        ("hPrey", new("{0} prey", "{0} 个猎物")),
        ("hToBait", new("{0} to bait", "{0} 个待布饵")),
        ("hWorn", new("{0} about to break", "{0} 个即将损坏")),
        ("hCollect", new("Collect {0}", "拾取{0}")),
        ("hBaitWith", new("Bait with {0}", "用{0}布饵")),
        ("hRebaitOn", new("RE-BAIT IS ON", "布饵已开启")),
        ("hRefill", new("Refill traps you empty", "为拾取后的陷阱补饵")),
        ("hRefillWith", new("refill with", "补饵：")),
        ("hFoot", new("{0} traps on {1} floors, starting with this floor", "{1} 个楼层的 {0} 个陷阱，从本层开始")),
        ("hIdle", new("Nothing to check: every trap is armed and baited.", "无需巡查：所有陷阱都已布置并有诱饵。")),
        ("hSkipBreaks", new("{0} in {1} breaks on its next catch, so it is emptied but not re-baited.", "{1}的{0}下次捕获就会损坏，只拾取不补饵。")),
        ("hSkipNoBait", new("{0} in {1} has no bait left, so it is skipped.", "{1}的{0}没有可用诱饵，将被跳过。")),
        ("hSkipMore", new("{0} more skipped.", "另有 {0} 个被跳过。")),
        ("hRebaitAll", new("Re-bait traps", "陷阱补饵")),
        ("hFillFoot", new("Traps getting bait now: {0}. Catches stay in the traps.", "现在布饵的陷阱：{0} 个。猎物留在陷阱里。")),
        ("hFillIdle", new("Nothing to re-bait: every trap that takes bait has some.", "无需补饵：所有能放诱饵的陷阱都已有诱饵。")),
        ("hFillNone", new("Traps with no bait left to use: {0}.", "没有可用诱饵的陷阱：{0} 个。")),
    };
}
