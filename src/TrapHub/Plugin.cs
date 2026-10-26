using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using TrapHub.Adapters;
using TrapHub.Core;
using UnityEngine;

namespace TrapHub;

[BepInPlugin(Guid, "Trap Hub", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(QuietTrapsGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.traphub";
    private const string Version = "0.4.2";
    private const string QuietTrapsGuid = "com.dev.survivallog.quiettraps";
    private const string PresetFileName = "trap-hub.json";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var panelKey = Config.Bind("General", "PanelKey", KeyCode.J,
            "Key that opens and closes the Trap Hub window, used with Shift while PanelKeyShift is true (Shift+J by default). None turns the key off; the trap button in the HUD's tool column still opens the window.\n" +
            "打开和关闭 Trap Hub 窗口的按键；PanelKeyShift 为 true 时需同时按住 Shift（默认 Shift+J）。设为 None 则停用该按键，HUD 工具栏中的陷阱按钮仍可打开窗口。");
        var panelKeyShift = Config.Bind("General", "PanelKeyShift", true,
            "When true, the window opens with Shift and PanelKey together, so PanelKey alone is free for Check traps. Set it to false to open the window with PanelKey alone.\n" +
            "设为 true 时，需同时按住 Shift 和 PanelKey 才会打开窗口，单按 PanelKey 留给巡查陷阱。设为 false 则单按 PanelKey 即可打开窗口。");
        var checkKey = Config.Bind("General", "CheckKey", KeyCode.J,
            "Key that checks every trap at once: collects every catch, then baits every empty trap from its preset. J by default. None turns it off.\n" +
            "一键巡查所有陷阱的按键：先拾取所有猎物，再按预设为空陷阱布饵。默认 J。设为 None 则停用。");
        var rebaitOnCheck = Config.Bind("General", "RebaitOnCheck", true,
            "When true, Check traps also baits every trap it empties, in the same trip. When false, it baits only traps that are already empty. The Re-bait box beside the Check traps button in the HUD's trap panel sets this too.\n" +
            "设为 true 时，巡查陷阱会在同一趟中为拾取后的陷阱补饵。设为 false 时，只为本来就空着的陷阱布饵。HUD 陷阱面板中巡查按钮旁的「布饵」勾选框也会修改此项。");
        var sounds = new Dictionary<SoundGroup, ConfigEntry<string>>
        {
            [SoundGroup.LiveCaptureCage] = BindSound("LiveCaptureCage", "the Live Capture Cage", "「活捉笼」"),
            [SoundGroup.SimpleBirdNet] = BindSound("SimpleBirdNet", "the Simple Bird Net", "「简易捕鸟网」"),
            [SoundGroup.OtherTraps] = BindSound("OtherTraps", "the Simple Mousetrap, Iron Animal Trap and Auto Trap Cage (the game gives all three one sound)", "「简易捕鼠夹」、「铁制捕兽夹」和「自动诱捕笼」（游戏中这三种陷阱使用同一个声音）"),
        };
        var isLoggingToFile = Config.Bind("Diagnostics", "LogToFile", false, new ConfigDescription(
            "When true, what Trap Hub does is also written to BepInEx\\trap-hub.log, which is kept between game starts. Turn it on when reporting a problem.\n" +
            "设为 true 时，Trap Hub 的操作也会写入 BepInEx\\trap-hub.log，该文件在重启游戏后保留。报告问题时再打开。", null, "ModManager.Advanced"));
        var log = new TrapLog(Log, Path.Combine(Paths.BepInExRootPath, "trap-hub.log"));
        var harmony = new Harmony(Guid);
        var isSoundsElsewhere = IL2CPPChainloader.Instance.Plugins.ContainsKey(QuietTrapsGuid);
        var session = new TrapSession(
            new GameTraps(log.Write),
            new WebPage(log.Write),
            new PresetFileOnDisk(Path.Combine(Paths.ConfigPath, PresetFileName), log.Write),
            (group, mode) => sounds[group].Value = Sounds.ModeName(mode),
            isOn => rebaitOnCheck.Value = isOn,
            log.Write);

        OptionalLive.Watch(isLoggingToFile, isOn => log.IsFileOn = isOn);
        log.Write($"===== Trap Hub {Version} started");
        InputGate.Apply(harmony, log.Write);
        PageMessages.Apply(harmony, log.Write);

        if (isSoundsElsewhere) log.Write("Quiet Traps is installed, so it handles the catch sounds and Trap Hub's sound settings are not used");
        else SoundGate.Apply(harmony, log.Write);

        OptionalLive.Watch(() =>
        {
            session.UseControls(panelKey.Value, panelKeyShift.Value, checkKey.Value);
            log.Write($"window key {(panelKeyShift.Value ? "Shift+" : "")}{panelKey.Value}, check key {checkKey.Value}");
        }, panelKey, panelKeyShift, checkKey);

        OptionalLive.Watch(() =>
        {
            session.UseRebaitOnCheck(rebaitOnCheck.Value);
            log.Write("re-bait on check " + (rebaitOnCheck.Value ? "on" : "off"));
        }, rebaitOnCheck);

        OptionalLive.Watch(() =>
        {
            var modes = sounds.ToDictionary(pair => pair.Key, pair => Sounds.ParseMode(pair.Value.Value));

            if (!isSoundsElsewhere) SoundGate.Use(modes);

            session.UseSounds(modes, isSoundsElsewhere);
            log.Write("catch sounds: " + string.Join(", ", modes.Select(pair => $"{pair.Key} {Sounds.ModeName(pair.Value)}")));
        }, sounds.Values.Cast<ConfigEntryBase>().ToArray());

        TrapTicker.Session = session;
        TrapTicker.Log = log.Write;
        AddComponent<TrapTicker>();

        // Compiles the mod's own views and presses on a worker thread while the game starts, so the first refresh after
        // a save loads does not pay for it in one frame. Pure .NET only; it never touches the game, and a failure here
        // only means that code is compiled when first used, as before.
        Task.Run(() => Warmup.Run()).ContinueWith(task => _ = task.Exception, TaskContinuationOptions.OnlyOnFaulted);
    }

    private ConfigEntry<string> BindSound(string key, string englishTrap, string chineseTrap) =>
        Config.Bind("Sounds", key, Sounds.ModeName(SoundMode.Normal), new ConfigDescription(
            $"The sound {englishTrap} makes while it holds a catch. Normal: as the game plays it. Once: plays for 3 seconds, then stops. Muted: never plays. Not used while Quiet Traps is installed.\n" +
            $"{chineseTrap}捕获猎物后发出的声音。Normal：按游戏原样播放。Once：播放 3 秒后停止。Muted：不播放。安装了 Quiet Traps 时不生效。",
            new AcceptableValueList<string>(Sounds.ModeNames.ToArray())));
}
