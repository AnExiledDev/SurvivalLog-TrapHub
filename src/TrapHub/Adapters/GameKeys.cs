using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Show;
using UnityEngine;

namespace TrapHub.Adapters;

/// <summary>
/// The key checks only Trap Hub needs; Exiled's Toolkit holds the game's hotkeys and sends every click to the web UI
/// while the window is open. A key typed into one of the game's text boxes (the item search) is text, not one of
/// Trap Hub's keys. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class GameKeys
{
    public static void Apply(PatchKit hooks) =>
        hooks.Patch(typeof(GameKeys), typeof(HotKeyManager), "Update", postfix: nameof(NoteGameTyping), done: "Trap Hub's keys wait while you type in the game's text boxes", consequence: "typing Trap Hub's keys in the game's item search may also open the window or check traps");

    public static bool IsShiftHeld() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    /// <summary>The game is taking typed text in one of its own menus, such as the item search, so letter keys are words, not hotkeys.</summary>
    public static bool IsGameTyping { get; private set; }

    /// <summary>
    /// A postfix, so it runs on every frame, also while the Toolkit holds the game's hotkeys back. A field read and no
    /// call.
    /// </summary>
    private static void NoteGameTyping(HotKeyManager __instance) => IsGameTyping = __instance != null && __instance._isTextInputActive;
}
