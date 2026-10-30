using TrapHub.Core;
using Xunit;

namespace TrapHub.Tests;

/// <summary>The window and Check traps keys: unbound by default since HUD buttons do both, and silent while the player types.</summary>
public class HotkeyTests
{
    private int _pressAsks;
    private int _typingAsks;

    private Func<bool> Pressed(bool isPressed) => () =>
    {
        _pressAsks++;

        return isPressed;
    };

    private Func<bool> Typing(bool isTyping) => () =>
    {
        _typingAsks++;

        return isTyping;
    };

    [Fact]
    public void The_window_and_check_keys_are_unbound_by_default()
    {
        Assert.Equal("None", Hotkey.DefaultKey);
    }

    [Fact]
    public void An_unbound_key_never_fires_and_asks_for_nothing()
    {
        Assert.False(Hotkey.Fires(isBound: false, Pressed(true), Typing(false)));
        Assert.Equal(0, _pressAsks);
        Assert.Equal(0, _typingAsks);
    }

    [Fact]
    public void A_bound_key_fires_on_its_press()
    {
        Assert.True(Hotkey.Fires(isBound: true, Pressed(true), Typing(false)));
    }

    [Fact]
    public void A_bound_key_does_nothing_while_the_player_types()
    {
        Assert.False(Hotkey.Fires(isBound: true, Pressed(true), Typing(true)));
    }

    [Fact]
    public void Typing_is_only_checked_when_the_key_goes_down()
    {
        Assert.False(Hotkey.Fires(isBound: true, Pressed(false), Typing(false)));
        Assert.Equal(0, _typingAsks);
    }
}
