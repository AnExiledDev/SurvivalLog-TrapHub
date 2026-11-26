using UnityEngine;

namespace TrapHub.Adapters;

/// <summary>
/// Whether Shift is held, for the window's key and the check key. Exiled's Toolkit holds the game's hotkeys and sends
/// every click to the web UI while the window is open, and its typing check covers the game's text boxes.
/// </summary>
internal static class GameKeys
{
    public static bool IsShiftHeld() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
}
