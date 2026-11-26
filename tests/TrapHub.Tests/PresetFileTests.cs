using TrapHub.Adapters;
using TrapHub.Core;
using Xunit;

namespace TrapHub.Tests;

/// <summary>What the preset store counts on from its file, checked on the fake its tests use and on trap-hub.json itself.</summary>
public abstract class PresetFileContract
{
    protected abstract IPresetFile NoFileYet();

    [Fact]
    public void With_no_file_there_is_nothing_to_read() => Assert.IsType<StoredText.Missing>(NoFileYet().Read());

    [Fact]
    public void What_is_written_is_read_back_whole()
    {
        var file = NoFileYet();

        Assert.True(file.TryWrite("{\"presets\":[]}", out _));
        Assert.Equal(new StoredText.Found("{\"presets\":[]}"), file.Read());
    }

    [Fact]
    public void Copying_the_file_aside_leaves_it_as_it_was()
    {
        var file = NoFileYet();

        file.TryWrite("not presets", out _);

        Assert.True(file.TryCopyAside(out _, out _));
        Assert.Equal(new StoredText.Found("not presets"), file.Read());
    }
}

public sealed class FakePresetFileTests : PresetFileContract
{
    protected override IPresetFile NoFileYet() => new FakePresetFile();
}

/// <summary>trap-hub.json in a folder of its own, so a test can put anything at its path.</summary>
public sealed class PresetFileOnDiskTests : PresetFileContract, IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "TrapHubPresets-" + Guid.NewGuid().ToString("N"));
    private DateTime _now = new(2026, 11, 25, 12, 0, 0);

    public PresetFileOnDiskTests() => Directory.CreateDirectory(_folder);

    private string Json => Path.Combine(_folder, "trap-hub.json");

    public void Dispose() => Directory.Delete(_folder, true);

    protected override IPresetFile NoFileYet() => new PresetFileOnDisk(Json, () => _now);

    /// <summary>If it were taken for a missing file, a fresh book could be saved over it.</summary>
    [Fact]
    public void Something_there_that_cannot_be_read_is_a_failure_never_missing()
    {
        Directory.CreateDirectory(Json);

        Assert.IsType<StoredText.Failed>(NoFileYet().Read());
    }

    [Fact]
    public void A_write_that_fails_leaves_the_old_text_as_it_was()
    {
        var file = NoFileYet();

        file.TryWrite("old", out _);
        Directory.CreateDirectory(Json + ".tmp");

        Assert.False(file.TryWrite("new", out var why));
        Assert.NotEmpty(why);
        Assert.Equal(new StoredText.Found("old"), file.Read());
    }

    [Fact]
    public void A_write_leaves_no_temporary_file_behind()
    {
        NoFileYet().TryWrite("{}", out _);

        Assert.Equal(new[] { Json }, Directory.GetFileSystemEntries(_folder));
    }

    /// <summary>A second bad file in the same second must not replace the copy of the first.</summary>
    [Fact]
    public void A_copy_is_never_made_over_an_earlier_copy()
    {
        var file = NoFileYet();

        file.TryWrite("first", out _);
        file.TryCopyAside(out var first, out _);
        file.TryWrite("second", out _);

        Assert.False(file.TryCopyAside(out _, out var why));
        Assert.NotEmpty(why);
        Assert.Equal("first", File.ReadAllText(first));

        _now = _now.AddSeconds(1);

        Assert.True(file.TryCopyAside(out var next, out _));
        Assert.Equal("second", File.ReadAllText(next));
    }
}
