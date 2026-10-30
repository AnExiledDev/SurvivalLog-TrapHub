using System;

namespace TrapHub.Core;

/// <summary>
/// When the window key or the Check traps key fires. The HUD's trap button opens the window and the HUD trap panel has a
/// Check traps button, so both keys are unbound by default, and a bound key does nothing while the player types in a
/// text box.
/// </summary>
internal static class Hotkey
{
    /// <summary>The Unity key name PanelKey and CheckKey start with: none, so no key is taken from the game or other mods.</summary>
    public const string DefaultKey = "None";

    /// <summary>
    /// Whether this frame's press fires the key. The press and the typing check are only asked for when they can change
    /// the answer: an unbound key asks nothing, and typing is only checked on a press.
    /// </summary>
    public static bool Fires(bool isBound, Func<bool> isPressed, Func<bool> isTyping) => isBound && isPressed() && !isTyping();
}
