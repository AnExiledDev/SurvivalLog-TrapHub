using System.Reflection;
using System.Text.RegularExpressions;
using ExiledToolkit.Kit;
using TrapHub.Core;
using Xunit;

namespace TrapHub.Tests;

/// <summary>Every word on screen exists in English and Chinese, page.js knows every word the plugin sends, and the catch sounds settings.</summary>
public class TextsAndSoundsTests
{
    private static IEnumerable<Phrase> AllPhrases()
    {
        foreach (var field in typeof(Texts).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            switch (field.GetValue(null))
            {
                case Phrase phrase:
                    yield return phrase;
                    break;
                case Counted counted:
                    yield return counted.One;
                    yield return counted.Many;
                    break;
            }
        }

        foreach (var (_, phrase) in Texts.Page) yield return phrase;
    }

    private static string PageScriptText()
    {
        using var stream = typeof(TextsAndSoundsTests).Assembly.GetManifestResourceStream("TrapHub.page.js")!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    [Fact]
    public void Every_text_has_plain_english_and_a_chinese_translation()
    {
        foreach (var phrase in AllPhrases())
        {
            Assert.False(string.IsNullOrWhiteSpace(phrase.En));
            Assert.True(phrase.En.All(ch => ch is >= ' ' and <= '~'), phrase.En);
            var hasWords = Regex.Replace(phrase.En, @"\{\d\}", "").Any(char.IsLetter);

            Assert.True(!hasWords || phrase.Zh.Any(ch => ch is >= '一' and <= '鿿'), phrase.Zh);
        }
    }

    [Fact]
    public void Both_languages_fill_in_the_same_blanks()
    {
        foreach (var phrase in AllPhrases().Where(p => !p.En.StartsWith("Baited 1", StringComparison.Ordinal) && !p.En.StartsWith("Auto re-bait: baited 1", StringComparison.Ordinal) && !p.En.StartsWith("Collecting from 1", StringComparison.Ordinal) && !p.En.StartsWith("1 bait", StringComparison.Ordinal)))
        {
            Assert.True(phrase.En.Contains("{0}") == phrase.Zh.Contains("{0}"), phrase.En);
            Assert.True(phrase.En.Contains("{1}") == phrase.Zh.Contains("{1}"), phrase.En);
        }
    }

    [Fact]
    public void Page_js_falls_back_to_exactly_the_english_words_the_plugin_sends()
    {
        var script = PageScriptText();
        var block = Regex.Match(script, @"var ENGLISH = \{(.*?)\n  \};", RegexOptions.Singleline).Groups[1].Value;
        var english = Regex.Matches(block, @"'?([A-Za-z]+)'?: '((?:[^'\\]|\\.)*)'").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Replace("\\'", "'"));

        Assert.Equal(Texts.Page.Select(w => w.Key).OrderBy(k => k), english.Keys.OrderBy(k => k));

        foreach (var (key, phrase) in Texts.Page) Assert.True(phrase.En == english[key], $"{key}: \"{phrase.En}\" vs \"{english[key]}\"");
    }

    [Fact]
    public void Page_keys_are_not_listed_twice()
    {
        Assert.Equal(Texts.Page.Count, Texts.Page.Select(w => w.Key).Distinct().Count());
    }

    [Theory]
    [InlineData("event:/UI/xianjing_bushoulong", SoundMode.Muted)]
    [InlineData("event:/UI/xianjing_buniaolong", SoundMode.Once)]
    [InlineData("event:/UI/xianjing_Metalbushujia", SoundMode.Normal)]
    [InlineData("event:/UI/click", SoundMode.Normal)]
    [InlineData(null, SoundMode.Normal)]
    public void Each_catch_sound_follows_its_groups_setting_and_no_other_sound_is_touched(string? path, SoundMode expected)
    {
        var modes = new Dictionary<SoundGroup, SoundMode> { [SoundGroup.LiveCaptureCage] = SoundMode.Muted, [SoundGroup.SimpleBirdNet] = SoundMode.Once, [SoundGroup.OtherTraps] = SoundMode.Normal };

        Assert.Equal(expected, Sounds.ModeOf(path, modes));
    }

    [Theory]
    [InlineData("Muted", SoundMode.Muted)]
    [InlineData(" once ", SoundMode.Once)]
    [InlineData("loud", SoundMode.Normal)]
    [InlineData(null, SoundMode.Normal)]
    public void A_bad_sound_setting_never_silences_a_trap(string? text, SoundMode expected)
    {
        Assert.Equal(expected, Sounds.ParseMode(text));
    }

    [Fact]
    public void Bait_kinds_are_read_off_the_games_effect_line()
    {
        Assert.Equal(BaitKind.Universal, BaitKinds.FromEffect(" 广谱诱饵 \u00b7 吸引多种猎物"));
        Assert.Equal(BaitKind.Specialized, BaitKinds.FromEffect("专精诱饵"));
        Assert.Equal(BaitKind.Other, BaitKinds.FromEffect(null));
    }
}
