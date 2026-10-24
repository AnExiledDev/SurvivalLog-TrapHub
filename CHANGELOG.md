# Changelog

## 0.3.0

- A new window: three tabs (Traps, Bait, Settings), traps grouped by room with your floor marked, a Needs you / Armed / All switch and a search box.
- **Check all traps** collects every catch, then baits every empty trap from its bait preset, the trap you just emptied included. **This floor only**, **Collect only** and **Bait only** sit beside it.
- J now checks every trap. Shift+J opens the window (new settings `CheckKey` and `PanelKeyShift`).
- A picked trap shows its catch odds, its bait preset, the next bait, its wear, and Take, Re-bait and Dismantle.
- The Bait tab holds the preset list, the preset editor and the allowed bait list side by side.
- Removed: picking several traps to give them a preset together, the filter and group-by side panel, and the separate Take this floor button (use This floor only).
- Updated for Survival Log 1.1.19611.

## 0.2.0

- A new look for the window: trap cards with catch chance and wear bars, and a side panel to filter by state, trap type and room.
- Group the trap list by floor, room, type or state. Your floor is marked.
- With no trap picked, the window lists the traps that need you. Press one to open it.
- A trap's detail shows its bait preset, the bait order, the catch odds and its wear in one place.
- The allowed bait tab shows owned bait first, quick picks (every bait in your presets, every universal bait you own) and, for each bait, what it attracts, which traps take it and which presets use it.
- The window responds faster: picking a trap or a bait no longer redraws the whole window, and each refresh sends far less to the game's web view.

- After a game update, a hook that can no longer be placed is logged as an error, so the Mod Manager can mark the mod as maybe needing an update.

## 0.1.1

- With Exiled's Toolkit 0.6.0 or later, the HUD button takes its place in the Toolkit's tidy layout of the tool buttons.

## 0.1.0

First release. A window for every trap at home: state, bait, wear and catch odds, Take all and Re-bait all, bait presets with fallbacks and auto re-bait, an allowed bait list, and a setting for each catch sound.
