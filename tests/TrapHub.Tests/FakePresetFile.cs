using TrapHub.Core;

namespace TrapHub.Tests;

/// <summary>
/// trap-hub.json as a test needs it: there, missing or locked, and refusing a copy or a write when told to. Steps lists
/// what was done to it, in order.
/// </summary>
internal sealed class FakePresetFile : IPresetFile
{
    public StoredText Text { get; set; } = new StoredText.Missing();
    public bool CanCopy { get; init; } = true;
    public bool CanWrite { get; init; } = true;
    public List<string> Steps { get; } = new();
    public List<string> Writes { get; } = new();
    public int Reads => Steps.Count(step => step == "read");

    public string Name => "trap-hub.json";

    public StoredText Read()
    {
        Steps.Add("read");

        return Text;
    }

    public bool TryCopyAside(out string copy, out string why)
    {
        Steps.Add("copy");
        copy = Name + ".unreadable-1";
        why = CanCopy ? "" : "access denied";

        return CanCopy;
    }

    public bool TryWrite(string text, out string why)
    {
        Steps.Add("write");
        why = CanWrite ? "" : "disk full";

        if (!CanWrite) return false;

        Text = new StoredText.Found(text);
        Writes.Add(text);

        return true;
    }
}
