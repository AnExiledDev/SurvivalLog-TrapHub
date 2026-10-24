using TrapHub.Core;
using Xunit;

namespace TrapHub.Tests;

/// <summary>What the window can ask the plugin for. Anything malformed or foreign is ignored, never half-done.</summary>
public class PageCommandTests
{
    private static PageCommand? Parse(string body) => PageCommand.Parse(PageCommand.Prefix + body);

    [Fact]
    public void Each_press_is_understood()
    {
        Assert.IsType<PageCommand.Toggle>(Parse("toggle"));
        Assert.IsType<PageCommand.TakeFloor>(Parse("takefloor"));
        Assert.Equal(new PageCommand.CheckAll(false), Parse("checkall"));
        Assert.Equal(new PageCommand.RebaitOnCheck(true), Parse("checkrebait|1"));
        Assert.Equal(new PageCommand.RebaitOnCheck(false), Parse("checkrebait|0"));
        Assert.Null(Parse("checkrebait|yes"));
        Assert.Equal(new PageCommand.CheckAll(true), Parse("checkfloor"));
        Assert.Equal(new PageCommand.Rebait(12), Parse("rebait|12"));
        Assert.Equal(new PageCommand.Dismantle(-5), Parse("dismantle|-5"));
        Assert.Equal(new PageCommand.Focus(true), Parse("focus|1"));
        Assert.Equal(new PageCommand.Sound(SoundGroup.SimpleBirdNet, SoundMode.Once), Parse("sound|net|Once"));
        Assert.Equal(new PageCommand.DeletePreset("p2"), Parse("deletepreset|p2"));
    }

    [Fact]
    public void Assign_and_allow_carry_every_id()
    {
        var assign = Assert.IsType<PageCommand.Assign>(Parse("assign|p2|1,2,3"));
        var reset = Assert.IsType<PageCommand.Assign>(Parse("assign||4"));
        var allow = Assert.IsType<PageCommand.Allow>(Parse("allow|0|200,201"));

        Assert.Equal("p2", assign.PresetId);
        Assert.Equal(new long[] { 1, 2, 3 }, assign.TrapIds);
        Assert.Equal("", reset.PresetId);
        Assert.False(allow.IsAllowed);
        Assert.Equal(new[] { 200, 201 }, allow.ItemIds);
    }

    [Fact]
    public void A_preset_named_with_a_bar_still_arrives_whole()
    {
        var save = Assert.IsType<PageCommand.SavePreset>(Parse("savepreset|{\"name\":\"a|b\"}"));

        Assert.Equal("{\"name\":\"a|b\"}", save.Json);
    }

    [Theory]
    [InlineData("rebait|x")]
    [InlineData("assign|p1|")]
    [InlineData("allow|2|200")]
    [InlineData("allow|1|-3")]
    [InlineData("sound|drum|Muted")]
    [InlineData("deletepreset|")]
    [InlineData("takeall|now")]
    [InlineData("checkall|1")]
    public void A_malformed_message_is_ignored(string body)
    {
        Assert.Null(Parse(body));
    }

    [Fact]
    public void Another_mods_message_is_not_ours()
    {
        Assert.Null(PageCommand.Parse("slmod|defense|toggle"));
        Assert.Null(PageCommand.Parse(null));
    }
}
