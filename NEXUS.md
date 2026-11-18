Name: Trap Hub
Summary: See every trap at home in one window, grouped by room, and check them all with one button in the game's trap list. It collects every catch, then baits every empty trap. Bait presets give each trap type, or a single trap, an ordered list of baits to fall back on, an allowed bait list keeps your food safe, and each trap shows its wear and catch odds.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/1-save-safe.png[/img][/center]

See every trap at home in one window: what each one holds, what bait it gets next, how worn it is and what it is likely to catch. Press [b]Check traps[/b] in the game's Home Traps list to check every trap. It collects every catch, then baits every empty trap. Press the trap button in the tool column on the left of the screen to open the window. You can also set a key for each (CheckKey and PanelKey in the config). Both are unbound by default, and they do nothing while you type in a text box. The trap button shows how many traps have a catch to collect or are waiting for bait.

[size=5]Main features[/size]
[list]
[*][b]All traps at a glance[/b]: every trap at home, grouped by room with your floor marked. Each one shows its state, its bait, the catches it has stored and how many catches it has left before it breaks.[/*]
[*][b]Find a trap fast[/b]: show the traps that need you, the armed ones or all of them, and search by trap, room or prey. The side panel lists what needs doing. Press a line to see those traps.[/*]
[*][b]Check all traps[/b]: collects every catch, starting on your floor with the nearest trap, then baits every empty trap from its preset. [b]This floor only[/b] does the same for the floor you are on. [b]Collect only[/b] and [b]Bait only[/b] do just one of the two.[/*]
[*][b]In the game's trap list[/b]: the Home Traps list in the HUD gets three new controls. The Check traps button shows what the check will collect, bait and skip when you hover over it. The Re-bait traps button baits every trap that has no bait without collecting anything. The Re-bait box turns refilling emptied traps on or off. Each trap in the list shows its bait, its catch and the catches it has left.[/*]
[*][b]Bait presets[/b]: a preset is an ordered list of baits, so when one runs out the next one is used. Each step can keep some back, and the preset can fall back to any allowed universal bait.[/*]
[*][b]Per trap or per type[/b]: each trap type has a default preset, and any trap can have its own.[/*]
[*][b]Where bait comes from[/b]: bag only, or bag first and then home storage. You can also use the food closest to spoiling first.[/*]
[*][b]Auto re-bait[/b]: a preset can bait its empty traps without you walking to them. Set it to Off, On my floor or Always.[/*]
[*][b]Allowed bait[/b]: Trap Hub only puts bait you allow in your traps. Everything starts blocked. Allow every bait in your presets, or every universal bait you own, in one press. Each bait shows what it attracts, which traps take it and which presets use it.[/*]
[*][b]Catch odds[/b]: each trap shows what it is likely to catch with its bait. The odds use the game's catch rates, including room and talent bonuses.[/*]
[*][b]Catch sounds[/b]: each trap sound can play normally, chime once or stay muted. When Quiet Traps is installed, it handles the sounds instead.[/*]
[*][b]UI size[/b]: Exiled's Toolkit's UI size setting makes the window bigger or smaller, up to the largest size that still fits on the screen. Trap Hub's UiScale setting can set a size for this mod alone.[/*]
[/list]
Presets and allowed bait are kept in [font=Courier New]BepInEx\config\trap-hub.json[/font], shared by every save. Trap Hub adds nothing to your save and does not edit game files. You can remove it at any time.

Trap Hub does everything Trapline does, so install only one of them.

Works in English and Chinese.

[size=5]Known issues[/size]
None known.

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.20719. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 or newer. Trap Hub does not load without it. The Toolkit turns off Trap Hub's keys while you type, shows Trap Hub's messages as toasts while the window is closed, and applies settings you change on the Mod Manager's Mods screen without a restart. Its UI size setting also sets the size of the window.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]. With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]If Trapline is installed, delete [font=Courier New]Trapline.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.[/*]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] the same way: download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\TrapHub.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]TrapHub.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder. To also delete your presets, delete [font=Courier New]BepInEx\config\trap-hub.json[/font].

[size=5]Configuration[/size]
To change the keys, the catch sounds or whether Check traps refills emptied traps, edit [font=Courier New]BepInEx\config\com.dev.survivallog.traphub.cfg[/font] while the game is closed, or use the Mod Manager's Mods screen. [url=https://github.com/AnExiledDev/SurvivalLog-TrapHub/blob/main/CONFIG.md]CONFIG.md[/url] lists every setting, and the cfg file explains each one in English and Chinese. Presets and allowed bait are set in the Trap Hub window.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi in the [url=https://discord.gg/pDrQtqbZgb]unofficial modding Discord for Survival Log[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and, if you turned on [font=Courier New]LogToFile[/font], [font=Courier New]BepInEx\trap-hub.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord. You can find me in the [url=https://discord.gg/pDrQtqbZgb]unofficial modding Discord[/url] and the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-TrapHub]github.com/AnExiledDev/SurvivalLog-TrapHub[/url]

[line]
[size=5]简体中文[/size]

在一个窗口里查看家中所有陷阱：每个陷阱里有什么、下次会放什么诱饵、磨损程度，以及可能捕到什么。点游戏家中陷阱列表里的「巡查陷阱」按钮即可巡查所有陷阱：先拾取所有猎物，再为所有空陷阱布饵。点击屏幕左侧工具栏中的陷阱按钮即可打开窗口。两者也可以设置按键（配置中的 CheckKey 和 PanelKey），默认不绑定。在输入框里打字时，这些按键不起作用。陷阱按钮上的数字是有猎物可拾取或在等待布饵的陷阱数量。

[size=5]主要功能[/size]
[list]
[*][b]陷阱一览[/b]：家中所有陷阱按房间分组，并标出你所在的楼层。每个陷阱都会显示状态、诱饵、储存的猎物，以及损坏前还能捕获几次。[/*]
[*][b]快速找到陷阱[/b]：可以只看需要处理的、已布置的或全部陷阱，也可以按陷阱、房间或猎物搜索。侧栏列出要处理的事，点一下就能看到对应的陷阱。[/*]
[*][b]巡查所有陷阱[/b]：先拾取所有猎物（从你所在楼层最近的陷阱开始），再按预设为所有空陷阱布饵。「仅本层」只处理你所在的楼层。「只拾取」和「只布饵」只做其中一步。[/*]
[*][b]游戏的陷阱列表[/b]：HUD 中的家中陷阱列表多了三个控件。鼠标移到「巡查陷阱」按钮上，可以看到这次巡查会拾取、布饵和跳过哪些陷阱。「陷阱补饵」按钮只为没有诱饵的陷阱布饵，不拾取猎物。旁边的「布饵」勾选框决定是否为拾取后的陷阱补饵。列表里每个陷阱还会显示诱饵、猎物和剩余可捕获次数。[/*]
[*][b]诱饵预设[/b]：预设是按顺序排列的诱饵清单，一种用完就换下一种。每一步都可以保留一定数量，预设还可以在最后改用任意已允许的广谱诱饵。[/*]
[*][b]按陷阱或按类型[/b]：每种陷阱有默认预设，任何陷阱也可以单独选择预设。[/*]
[*][b]诱饵来源[/b]：只从背包取，或先背包再家中储物。还可以优先使用最快变质的食物。[/*]
[*][b]自动布饵[/b]：预设可以自动为空陷阱布饵，不用你走过去。可选「关闭」、「在我所在楼层」或「始终」。[/*]
[*][b]允许的诱饵[/b]：Trap Hub 只会使用你允许的诱饵。所有诱饵默认都是禁止的。可以一键允许预设中的全部诱饵，或你拥有的全部广谱诱饵。每种诱饵都会显示吸引的猎物、适用的陷阱，以及使用它的预设。[/*]
[*][b]捕获概率[/b]：按游戏的捕获率显示每个陷阱可能捕到的猎物，已计入房间和天赋加成。[/*]
[*][b]捕获声音[/b]：每种陷阱声音可以正常播放、只响一次或静音。安装了 Quiet Traps 时，由它来处理声音。[/*]
[*][b]界面大小[/b]：可以用 Exiled's Toolkit 的界面大小设置放大或缩小窗口，最大放到窗口仍能完整显示在屏幕上为止。也可以用 Trap Hub 的 UiScale 设置单独调整本模组的大小。[/*]
[/list]
诱饵预设和允许的诱饵保存在 [font=Courier New]BepInEx\config\trap-hub.json[/font]，所有存档共用。Trap Hub 不会向存档添加任何内容，也不会修改游戏文件，可以随时移除。

Trap Hub 包含 Trapline 的功能，两者请只装一个。安装前如已装有 Trapline，请从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]Trapline.dll[/font]。

[size=5]已知问题[/size]
暂无。

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.20719。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 或更新版本。没有它本模组不会加载。打字时，它让 Trap Hub 的按键不起作用。窗口关闭时，它把 Trap Hub 的消息显示为弹出通知。在 Mod Manager 的「模组」界面修改的设置，也靠它无需重启即可生效。它的界面大小设置还能调整窗口的大小。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]。装了它，可以在游戏里它的「模组」界面修改每项设置，并看到每项的说明，不用打开配置文件。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]用同样的方式安装 [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url]：下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\TrapHub.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]TrapHub.dll[/font]。如需同时删除诱饵预设，再删除 [font=Courier New]BepInEx\config\trap-hub.json[/font]。

[size=5]配置[/size]
如需修改按键、捕获声音，或巡查陷阱时是否为拾取后的陷阱补饵，请在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.traphub.cfg[/font]，或在 Mod Manager 的「模组」界面修改。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-TrapHub/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。诱饵预设和允许的诱饵在 Trap Hub 窗口里设置。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/pDrQtqbZgb]Survival Log 非官方模组 Discord[/url] 打个招呼，或在这里留言。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font]，如果打开了 [font=Courier New]LogToFile[/font]，也请附上 [font=Courier New]BepInEx\trap-hub.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/pDrQtqbZgb]Survival Log 非官方模组 Discord[/url] 和 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-TrapHub]github.com/AnExiledDev/SurvivalLog-TrapHub[/url]
