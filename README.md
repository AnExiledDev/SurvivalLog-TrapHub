# Trap Hub

Every trap at home in one window: what each one holds, what bait it gets next, how worn it is and what it is likely to catch. Check every trap with one press, re-bait from presets with fallbacks, and pick which bait the mod may use.

Press **Check traps** in the game's Home Traps list to check every trap: collect every catch, then bait every empty trap. Press the trap button in the tool column on the left of the screen to open the window. Both can have a key too (`CheckKey` and `PanelKey`), unbound by default. The button shows how many traps hold a catch to take or wait for bait.

## Features

- **All traps at a glance**: every trap at home, grouped by room with your floor marked, with its state (armed, prey, no bait, halted), its bait, its store and how many catches it has left before it breaks.
- **Find a trap fast**: show the traps that need you, the armed ones or all, and search by trap, room or prey. The side panel lists what waits (catches to collect, traps to bait, no bait left, about to break, halted); press a line to see those traps.
- **Check all traps**: collects every catch, your floor first, nearest trap first, then baits every empty trap from its preset, the ones just emptied included. **This floor only** does the same for the floor you are on; **Collect only** and **Bait only** do one half. Each trap can also be handled on its own.
- **In the game's own trap list**: the Home Traps list in the HUD gets a Check traps button, whose tip shows what the check will collect, bait and skip, a Re-bait traps button that baits every trap with no bait without collecting anything, whose tip shows the bait each room's traps get, and a Re-bait box that turns refilling emptied traps on or off. Each trap in the list shows its bait, its catch and the catches it has left, and a line on top counts what waits, with a link to the window.
- **Bait presets**: an ordered list of baits, so when one runs out the next is used. Each step can keep a number back (keep at least 3 meat, say). If every step is out, the preset can fall back to any allowed universal bait.
- **Per trap or per type**: each trap type has a default preset, and any trap can have its own. A trap's own preset is kept per save.
- **Where bait comes from**: bag only, or bag first and then home storage, and the food closest to spoiling first.
- **Auto re-bait**: a preset can bait its empty traps by itself, off, only on your floor, or always.
- **Allowed bait**: Trap Hub only puts bait you allow in your traps. Everything starts blocked. Allow baits one at a time, or every bait the filter shows. Quick picks allow every bait in your presets, or every universal bait you own, in one press. Each bait shows what it attracts, which traps take it and which presets use it.
- **Catch odds**: each trap shows its likely catches for the bait it holds or would get, by the game's own rates, room and talents included.
- **Catch sounds**: the sound a trap plays while it holds a catch can play as normal, chime once, or stay muted, for each trap sound. When Quiet Traps is installed, it handles the sounds instead.
- **UI size**: Exiled's Toolkit's UI size setting (`UiScale`) draws the window bigger or smaller, as far as the window still fits on the screen. The HUD button and what Trap Hub adds to the game's trap list keep the game's size. Trap Hub's own `UiScale` under Interface sets a size for this mod alone; at 0, the default, it follows the Toolkit's.

Presets and allowed bait are kept in `BepInEx\config\trap-hub.json`, shared by every save. Trap Hub adds nothing to your save and does not edit game files. You can remove it at any time.

Trap Hub does what Trapline does. Use one or the other, not both.

## Requirements

- Survival Log 1.1.20719. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.19.0 or newer. Trap Hub does not load without it. It keeps Trap Hub's keys quiet while you type, shows Trap Hub's messages as toasts while the window is closed, applies settings changed on the Mod Manager's Mods screen without a restart, lays the HUD's tool buttons out four high, in columns, when there are more than four, and its UI size setting sizes the window.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. If Trapline is installed, delete `Trapline.dll` from the `BepInEx\plugins` folder.
3. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
4. Download `TrapHub-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-TrapHub/releases/latest).
5. Extract the zip into the game folder. It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\TrapHub.dll`

## Uninstall

Delete `TrapHub.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.traphub.cfg`, `BepInEx\config\trap-hub.json` and, if it exists, `BepInEx\trap-hub.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.traphub.cfg`, created the first time the game starts with the mod. The keys are `CheckKey` and `PanelKey` with `PanelKeyShift`. Both keys are unbound by default, since the HUD buttons do the same; set them to use the keyboard. They do nothing while you type in a text box. `RebaitOnCheck` sets whether Check traps refills the traps it empties. The catch sounds can also be set in the window's Settings tab. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Built against BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Trap Hub 0.4.3 started`, followed by lines starting with `hooked`. A line starting with `could not hook` means part of the mod does not work on this game version.

## Known issues

None known.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log` and, if you turned on `LogToFile`, `BepInEx\trap-hub.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx, the game's IL2CPP interop assemblies and Exiled's Toolkit, so a game install with BepInEx set up and started once is required. Those assemblies are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/TrapHub -c Release -p:GameDir="C:\path\to\Survival Log" -p:ToolkitDll="C:\path\to\ExiledToolkit.dll"
```

`GameDir` is the game folder and `ToolkitDll` is a built `ExiledToolkit.dll`. The defaults in the project file are local paths you will almost certainly need to override. The output DLL is at `src\TrapHub\bin\Release\TrapHub.dll`.

The unit tests cover the bait, preset, route, odds and window rules and need nothing from the game:

```
dotnet test tests/TrapHub.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
