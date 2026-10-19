# Trap Hub settings

Settings live in `BepInEx\config\com.dev.survivallog.traphub.cfg`, created the first time the game starts with the mod. Edit it while the game is closed, or change the settings in game on the Mod Manager's Mods screen. Delete the file to get the defaults back. Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start.

Bait presets, the allowed bait list and each trap's own preset are not in this file. They are set in the Trap Hub window and kept in `BepInEx\config\trap-hub.json`, shared by every save. Delete that file to start over with one preset and no bait allowed.

## General

| Setting | Default | What it does |
| --- | --- | --- |
| `PanelKey` | `J` | Opens and closes the Trap Hub window. `None` turns the key off; the trap button in the tool column on the left of the screen still opens it. |

## Sounds

The sound a trap plays while it holds a catch. `Normal` plays it as the game does, `Once` plays it for 3 seconds and then stops, `Muted` never plays it. These are also in the window's Settings tab. While Quiet Traps is installed, these settings are not used.

| Setting | Default | Traps |
| --- | --- | --- |
| `LiveCaptureCage` | `Normal` | Live Capture Cage |
| `SimpleBirdNet` | `Normal` | Simple Bird Net |
| `OtherTraps` | `Normal` | Simple Mousetrap, Iron Animal Trap and Auto Trap Cage. The game gives all three the same sound, so they share one setting. |

## Diagnostics

| Setting | Default | What it does |
| --- | --- | --- |
| `LogToFile` | `false` | Also writes what Trap Hub does to `BepInEx\trap-hub.log`, which is kept between game starts. Turn it on when reporting a problem. |
