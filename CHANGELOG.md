# Changelog

## 0.4.0

- The game's own Home Traps list in the HUD gets a **Check traps** button and a **Re-bait** box, beside the list's title like the plants list's Tend Garden button and Replant box.
- Hover the Check traps button to see what it will do: what it collects and where, what it baits with, how many traps on how many floors, and which traps it leaves and why.
- Each trap in the list gets a second line: its bait or the bait it gets next, its catch, and how many catches it has left with a wear bar. The list opens wider to fit them, and a line on top counts the catches to collect, the traps to bait and the traps about to break, with a link to the Trap Hub window.
- A full auto trap now reads "full, collect to restart", and the English status "Deploying Defenses" reads "Armed".
- After a check, the game's popup shows how many traps it handled ("Check x5").
- New setting `RebaitOnCheck` (on): whether Check traps also baits the traps it empties. The Re-bait box sets it too. With it off, Check traps baits only traps that were already empty.

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

- After a game update, Mod Manager can tell you if this mod may need an update.

## 0.1.1

- With Exiled's Toolkit 0.6.0 or later, the HUD button lines up neatly with other mods' buttons.

## 0.1.0

- First release.
- A window lists every trap at home with its state, bait, wear and catch odds.
- Take all collects every catch, and Re-bait all baits every empty trap.
- Bait presets give each trap a bait order to fall back on, and can re-bait a trap on their own.
- An allowed bait list sets which bait the mod may use.
- Each catch sound has its own setting.
