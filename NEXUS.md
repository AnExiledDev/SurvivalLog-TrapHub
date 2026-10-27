Name: Trap Hub
Summary: Every trap at home in one window, grouped by room, and one key (J) or a button in the game's own trap list that checks them all: it collects every catch, then baits every empty trap. Bait presets set an ordered list of baits with fallbacks per trap type or per trap, an allowed bait list keeps food safe, and each trap shows its wear and catch odds.

Description (BBCode):
Every trap at home in one window: what each one holds, what bait it gets next, how worn it is and what it is likely to catch. Press [font=Courier New]J[/font] to check every trap: collect every catch, then bait every empty trap. Press [font=Courier New]Shift+J[/font], or the trap button in the tool column on the left of the screen, to open the window. The button shows how many traps hold a catch to take or wait for bait.

[size=5]Main features[/size]
[list]
[*][b]All traps at a glance[/b]: every trap at home, grouped by room with your floor marked, with its state, its bait, its store and how many catches it has left before it breaks.[/*]
[*][b]Find a trap fast[/b]: show the traps that need you, the armed ones or all, and search by trap, room or prey. The side panel lists what waits; press a line to see those traps.[/*]
[*][b]Check all traps[/b] ([font=Courier New]J[/font]): collects every catch, your floor first, nearest trap first, then baits every empty trap from its preset. [b]This floor only[/b] does the same for the floor you are on; [b]Collect only[/b] and [b]Bait only[/b] do one half.[/*]
[*][b]In the game's own trap list[/b]: the Home Traps list in the HUD gets a Check traps button, whose tip shows what the check will collect, bait and skip, and a Re-bait box that turns refilling emptied traps on or off. Each trap shows its bait, its catch and the catches it has left.[/*]
[*][b]Bait presets[/b]: an ordered list of baits, so when one runs out the next is used. Each step can keep a number back, and the preset can fall back to any allowed universal bait.[/*]
[*][b]Per trap or per type[/b]: each trap type has a default preset, and any trap can have its own.[/*]
[*][b]Where bait comes from[/b]: bag only, or bag first and then home storage, and the food closest to spoiling first.[/*]
[*][b]Auto re-bait[/b]: a preset can bait its empty traps by itself, off, only on your floor, or always.[/*]
[*][b]Allowed bait[/b]: Trap Hub only puts bait you allow in your traps. Everything starts blocked. Allow every bait in your presets, or every universal bait you own, in one press. Each bait shows what it attracts, which traps take it and which presets use it.[/*]
[*][b]Catch odds[/b]: each trap shows its likely catches for its bait, by the game's own rates, room and talents included.[/*]
[*][b]Catch sounds[/b]: each trap sound can play as normal, chime once, or stay muted. When Quiet Traps is installed, it handles the sounds instead.[/*]
[*][b]UI size[/b]: with Exiled's Toolkit 0.10.0 or newer, its UI size setting draws the window bigger or smaller, as far as it still fits on the screen.[/*]
[/list]
Presets and allowed bait are kept in [font=Courier New]BepInEx\config\trap-hub.json[/font], shared by every save. Trap Hub adds nothing to your save and does not edit game files. You can remove it at any time.

Trap Hub does what Trapline does. Use one or the other, not both.

Works in English and Chinese.

[size=5]Known issues[/size]
None known.

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19611. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. With 0.10.0 or newer, its UI size setting also sizes the window. Without it, settings take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]If Trapline is installed, delete [font=Courier New]Trapline.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\TrapHub.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]TrapHub.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder. To also drop your presets, delete [font=Courier New]BepInEx\config\trap-hub.json[/font].

[size=5]Configuration[/size]
To change the keys, the catch sounds or whether Check traps refills emptied traps, edit [font=Courier New]BepInEx\config\com.dev.survivallog.traphub.cfg[/font] while the game is closed, or use the Mod Manager's Mods screen (see [url=https://github.com/AnExiledDev/SurvivalLog-TrapHub/blob/main/CONFIG.md]CONFIG.md[/url] for every setting; the cfg file explains each setting in English and Chinese). Presets and allowed bait are set in the Trap Hub window.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and, if you turned on [font=Courier New]LogToFile[/font], [font=Courier New]BepInEx\trap-hub.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-TrapHub]github.com/AnExiledDev/SurvivalLog-TrapHub[/url]

[line]
[size=5]简体中文[/size]

在一个窗口里查看家中所有陷阱：每个陷阱里有什么、下次会放什么诱饵、磨损程度，以及可能捕到什么。按 [font=Courier New]J[/font] 巡查所有陷阱：先拾取所有猎物，再为所有空陷阱布饵。按 [font=Courier New]Shift+J[/font]，或点击屏幕左侧工具栏中的陷阱按钮即可打开窗口。按钮上的数字是有猎物可取或等待放饵的陷阱数量。

[size=5]主要功能[/size]
[list]
[*][b]陷阱一览[/b]：家中所有陷阱按房间分组并标出你所在的楼层，显示状态、诱饵、储存，以及损坏前还能捕获几次。[/*]
[*][b]快速找到陷阱[/b]：可只看需要你处理的、已布饵的或全部陷阱，并按陷阱、房间或猎物搜索。侧栏列出待办事项，点一下即可查看对应的陷阱。[/*]
[*][b]巡查所有陷阱[/b]（[font=Courier New]J[/font]）：先收取所有猎物（先当前楼层，再由近及远），再按方案为所有空陷阱放饵。[b]仅本层[/b]只处理你所在的楼层；[b]仅收取[/b]和[b]仅补饵[/b]只做其中一半。[/*]
[*][b]游戏自带的陷阱列表[/b]：HUD 中的家中陷阱列表新增巡查按钮，悬停可查看这次巡查会拾取、布饵和跳过哪些陷阱；旁边的「布饵」勾选框决定是否为拾取后的陷阱补饵。每个陷阱还会显示诱饵、猎物和剩余可捕获次数。[/*]
[*][b]诱饵方案[/b]：按顺序排列的诱饵清单，一种用完就换下一种。每一步可以保留一定数量，方案还可以最后改用任意已允许的广谱诱饵。[/*]
[*][b]按陷阱或按类型[/b]：每种陷阱有默认方案，任何陷阱也可以单独指定方案。[/*]
[*][b]诱饵来源[/b]：只从背包取，或先背包再家中储物，并优先使用最快变质的食物。[/*]
[*][b]自动补饵[/b]：方案可以自动为空陷阱放饵，可选关闭、仅限当前楼层或始终开启。[/*]
[*][b]允许的诱饵[/b]：Trap Hub 只会使用你允许的诱饵。所有诱饵默认都是禁止的。可一键允许方案中的全部诱饵，或你拥有的全部广谱诱饵。每种诱饵都会显示吸引的猎物、适用的陷阱，以及使用它的方案。[/*]
[*][b]捕获概率[/b]：按游戏自身的概率显示每个陷阱可能捕到的猎物，已计入房间和天赋加成。[/*]
[*][b]捕获声音[/b]：每种陷阱声音可以正常播放、只响一次或静音。安装了 Quiet Traps 时，由它来处理声音。[/*]
[*][b]界面大小[/b]：安装 Exiled's Toolkit 0.10.0 或更新版本后，可以用它的界面大小设置放大或缩小窗口，最大放到窗口仍能完整显示在屏幕上为止。[/*]
[/list]
诱饵方案和允许的诱饵保存在 [font=Courier New]BepInEx\config\trap-hub.json[/font]，所有存档共用。Trap Hub 不会向存档添加任何内容，也不会修改游戏文件，可以随时移除。

Trap Hub 包含 Trapline 的功能，两者请只装一个。安装前如已装有 Trapline，请从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]Trapline.dll[/font]。

[size=5]已知问题[/size]
暂无。

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19611。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。安装后，在 Mod Manager 的模组界面修改的设置无需重启游戏即可生效，0.10.0 或更新版本还能用它的界面大小设置调整窗口的大小；未安装时，在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\TrapHub.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]TrapHub.dll[/font]。如需同时删除诱饵方案，再删除 [font=Courier New]BepInEx\config\trap-hub.json[/font]。

[size=5]配置[/size]
如需修改按键或捕获声音，请在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.traphub.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-TrapHub/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。修改会在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在这里留言。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\trap-hub.log[/font]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-TrapHub]github.com/AnExiledDev/SurvivalLog-TrapHub[/url]
