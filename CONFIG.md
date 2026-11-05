# Trap Hub settings

Settings live in `BepInEx\config\com.dev.survivallog.traphub.cfg`, created the first time the game starts with the mod. Edit it while the game is closed, or change the settings in game on the Mod Manager's Mods screen. Delete the file to get the defaults back.

Bait presets, the allowed bait list and each trap's own preset are not in this file. They are set in the Trap Hub window and kept in `BepInEx\config\trap-hub.json`, shared by every save. Delete that file to start over with one preset and no bait allowed.

## General

| Setting | Default | What it does |
| --- | --- | --- |
| `PanelKey` | `None` | Opens and closes the Trap Hub window, pressed with Shift while `PanelKeyShift` is `true`. Unbound by default: the trap button in the tool column on the left of the screen opens the window. Set a key here to open it from the keyboard too. |
| `PanelKeyShift` | `true` | When `true`, the window opens with Shift and `PanelKey`, so the same key alone can be `CheckKey`. When `false`, `PanelKey` alone opens the window; if it is the same key as `CheckKey`, that key opens the window instead of checking traps. |
| `CheckKey` | `None` | Checks every trap at once: collects every catch, then baits every empty trap from its bait preset. Unbound by default: the Check traps button in the HUD's Home Traps list does the same. Set a key here to check traps from the keyboard too. |
| `RebaitOnCheck` | `true` | When `true`, Check traps also baits every trap it empties, in the same trip. When `false`, it baits only traps that are already empty. The Re-bait box beside the Check traps button in the HUD's Home Traps list sets this too. |

## Sounds

The sound a trap plays while it holds a catch. `Normal` plays it as the game does, `Once` plays it for 3 seconds and then stops, `Muted` never plays it. These are also in the window's Settings tab. While Quiet Traps is installed, these settings are not used.

| Setting | Default | Traps |
| --- | --- | --- |
| `LiveCaptureCage` | `Normal` | Live Capture Cage |
| `SimpleBirdNet` | `Normal` | Simple Bird Net |
| `OtherTraps` | `Normal` | Simple Mousetrap, Iron Animal Trap and Auto Trap Cage. The game gives all three the same sound, so they share one setting. |

## Interface

| Setting | Default | What it does |
| --- | --- | --- |
| `UiScale` | `0` | How big Trap Hub's own windows, panels and messages are drawn, in percent, from 75 to 200. At `0` they follow the UI size set in Exiled's Toolkit (its `UiScale`). |

## Diagnostics

| Setting | Default | What it does |
| --- | --- | --- |
| `LogToFile` | `false` | Also writes what Trap Hub does to `BepInEx\trap-hub.log`, which is kept between game starts. Turn it on when reporting a problem. |
