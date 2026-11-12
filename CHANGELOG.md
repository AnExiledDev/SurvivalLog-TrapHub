# Changelog

## 0.6.4

- Clearer wording in the Trap Hub window, the Home Traps list, the settings and the mod's pages, in English and Chinese.
- A full Auto Trap Cage now shows as "Full" in the Trap Hub window, the same as in the Home Traps list.
- In the preset editor's list of traps using a preset, a trap picked for that preset by hand now shows "Custom".
- Messages and the Check traps tip now say "1 trap" and "1 floor" where they said "trap(s)" or "1 floors".
- Made for Survival Log 1.1.20719.

## 0.6.3

- Fixed the "Default for" row in the preset editor, where the trap names covered the label.
- Made for Survival Log 1.1.20719.

## 0.6.2

- Trap Hub now needs Exiled's Toolkit 0.19.0 or newer.
- Made for Survival Log 1.1.20719.

## 0.6.1

- The window key and the Check traps key are now unbound by default. Use the trap button and the Check traps button in the HUD, or set keys in the settings.
- The window key and the Check traps key no longer do anything while you type in a text box in any mod's window.
- Exiled's Toolkit 0.12.0 or newer is now required.
- Made for Survival Log 1.1.20151.

## 0.6.0

- New: a Re-bait traps button beside the Check traps button in the HUD's Home Traps list. It puts bait in every trap that has none, without collecting any catches. Hover over it to see which bait goes into which room's traps.
- Fixed: the Check traps tip and the open Home Traps list are no longer hidden behind the tool buttons on the left of the screen.
- Made for Survival Log 1.1.20151.

## 0.5.2

- Fixed: on Survival Log 1.1.20151, typing Trap Hub's keys in the item search no longer opens the Trap Hub window or checks your traps.
- Made for Survival Log 1.1.20151.

## 0.5.1

- With the window closed, Trap Hub's messages now show as Exiled's Toolkit toasts. Those toasts also go to the Notifications history.
- Exiled's Toolkit 0.11.0 or newer is now required.
- Made for Survival Log 1.1.19611.

## 0.5.0

- New setting: `UiScale` under Interface sets the size of Trap Hub's windows from 75% to 200%, separate from Exiled's Toolkit's UI size. At 0, the default, they use the Toolkit's size. Needs Exiled's Toolkit 0.11.0 or newer.
- Made for Survival Log 1.1.19611.

## 0.4.3

- The Trap Hub window and its pop-up messages can be made bigger or smaller with the UI size setting of Exiled's Toolkit 0.10.0 or newer. Changes show right away, and the window only grows as far as it still fits on the screen.
- The HUD button and what Trap Hub adds to the game's trap list stay at the game's size, like the buttons and text around them.
- Made for Survival Log 1.1.19611.

## 0.4.2

- Fixed: the search boxes and text boxes in Trap Hub now accept Chinese and other typing that needs an input method.
- Made for Survival Log 1.1.19611.

## 0.4.1

- Fixed a short freeze the first time Trap Hub read your traps after a save loaded. The trap list now loads a little at a time over the first few moments of play.
- While the trap list is still loading, the Trap Hub window says so.
- The game runs more smoothly with Trap Hub installed. The Trap Hub window and the HUD's trap list are now redrawn only when something in them has changed, and only the lines that changed.
- With every catch sound set to Normal, Trap Hub no longer adds any work to the sounds the game plays.

## 0.4.0

- The game's Home Traps list in the HUD gets a **Check traps** button and a **Re-bait** box next to the list's title, like the Tend Garden button and Replant box on the plants list.
- Hover over the Check traps button to see what it will do: what it collects and where, what it baits with, how many traps on how many floors, and which traps it skips and why.
- Each trap in the list gets a second line with its bait or the bait it gets next, its catch, and how many catches it has left with a wear bar. The list opens wider to fit them. A line at the top counts the catches to collect, the traps to bait and the traps about to break, with a link to the Trap Hub window.
- A full Auto Trap Cage now reads "full, collect to restart", and the English status "Deploying Defenses" now reads "Armed".
- After a check, the game's popup shows how many traps it handled ("Check x5").
- New setting `RebaitOnCheck`, on by default, sets whether Check traps also baits the traps it empties. The Re-bait box changes it too. With it off, Check traps baits only traps that were already empty.

## 0.3.0

- A new window with three tabs (Traps, Bait, Settings). Traps are grouped by room with your floor marked, and you can switch between Needs you, Armed and All or search.
- **Check all traps** collects every catch, then baits every empty trap from its bait preset, including the traps it just emptied. **This floor only**, **Collect only** and **Bait only** are next to it.
- J now checks every trap, and Shift+J opens the window (new settings `CheckKey` and `PanelKeyShift`).
- Picking a trap shows its catch odds, its bait preset, the next bait, its wear, and the Take, Re-bait and Dismantle buttons.
- The Bait tab shows the preset list, the preset editor and the allowed bait list side by side.
- Removed: picking several traps to give them a preset together, the filter and group-by side panel, and the separate Take this floor button (use This floor only).
- Updated for Survival Log 1.1.19611.

## 0.2.0

- The window has a new look, with trap cards that show catch chance and wear bars, and a side panel to filter by state, trap type and room.
- You can group the trap list by floor, room, type or state. Your floor is marked.
- With no trap picked, the window lists the traps that need you. Press one to open it.
- A trap's details show its bait preset, the bait order, the catch odds and its wear in one place.
- The allowed bait tab shows the bait you own first, quick picks (every bait in your presets, every universal bait you own) and, for each bait, what it attracts, which traps take it and which presets use it.
- The window responds faster. Picking a trap or a bait no longer redraws the whole window, and each refresh sends far less to the game's web view.
- After a game update, Mod Manager can tell you if this mod may need an update.

## 0.1.1

- With Exiled's Toolkit 0.6.0 or later, the HUD button lines up neatly with other mods' buttons.

## 0.1.0

- First release.
- A window lists every trap at home with its state, bait, wear and catch odds.
- Take all collects every catch, and Re-bait all baits every empty trap.
- Bait presets give each trap a list of baits to fall back on, and can re-bait a trap by themselves.
- An allowed bait list sets which bait the mod may use.
- Each catch sound has its own setting.
