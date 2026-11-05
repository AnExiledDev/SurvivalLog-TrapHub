using ExiledToolkit.Web;
using TrapHub.Core;
using Jint;

namespace TrapHub.Tests;

/// <summary>
/// page.js exactly as the plugin embeds it, run in Jint with Exiled's Toolkit's page API (api.js, from the Toolkit's
/// source) on top of FakePage.js (Root.html, the game's HUD page and the vuplex bridge). The calls into the script are
/// the ones TrapPage makes, and what the script posts is parsed with the real PageCommand, so a test sees the commands
/// the plugin would act on.
/// </summary>
internal sealed class PageScript
{
    private readonly Engine _engine = new();

    /// <param name="isToolkitFirst">
    /// The game runs the Toolkit's script first, or with false Trap Hub's first; then the Toolkit's runs only at
    /// <see cref="LoadToolkit"/>.
    /// </param>
    public PageScript(bool isToolkitFirst = true)
    {
        _engine.Execute(Resource("TrapHub.Tests.FakePage.js"));

        if (isToolkitFirst) _engine.Execute(Resource("ExiledToolkit.api.js"));

        _engine.Execute(Resource("TrapHub.page.js"));
        Hear();
    }

    /// <summary>Every command the script sent the plugin, its ready message and diag notes aside.</summary>
    public List<PageCommand> Sent { get; } = new();

    /// <summary>The script's "diag|" notes, as the plugin logs them.</summary>
    public List<string> Notes { get; } = new();

    /// <summary>Whether the script told the plugin it is ready, which has the plugin send it everything again.</summary>
    public bool HasSaidReady { get; private set; }

    /// <summary>The Toolkit's page API arriving after Trap Hub's script.</summary>
    public void LoadToolkit() => Run(Resource("ExiledToolkit.api.js"));

    public void Run(string statements)
    {
        _engine.Execute(statements);
        Hear();
    }

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    /// <summary>One run of the Toolkit's 250 ms page check, which hands the HUD page to the script.</summary>
    public void Poll() => Run("poll()");

    public void OpenHud() => Run("openHud()");

    public void Click(string expression) => Run($"click({expression})");

    public void Live(bool isLive, string keyName) => Call($"live({(isLive ? "true" : "false")},{JsText.Literal(keyName)})");

    /// <summary>Types into the window's text box for the field ("name", "search", "addsearch"), which leaves it focused.</summary>
    public void Type(string field, string text) => Run($"type(field({JsText.Literal(field)}),{JsText.Literal(text)})");

    /// <summary>The text box loses focus, as when the player clicks elsewhere.</summary>
    public void Blur(string field) => Run($"fire(field({JsText.Literal(field)}),'focusout')");

    public void Open(string viewJson) => Call($"open({viewJson})");

    public void SetData(string viewJson) => Call($"setData({viewJson})");

    public void Catalog(string catalogJson) => Call($"catalog({catalogJson})");

    public void Hud(string hudJson) => Call($"hud({hudJson})");

    public void Badge(int count) => Call($"badge({count})");

    public void Close() => Call("close()");

    public void Status(string text) => Call($"status({JsText.Literal(text)})");

    private void Call(string expression) => Run("window.__sltraphub." + expression);

    /// <summary>Takes what the script posted since the last look.</summary>
    private void Hear()
    {
        var count = (int)_engine.Evaluate("posted.length").AsNumber();
        var messages = Enumerable.Range(0, count).Select(i => _engine.Evaluate($"posted[{i}]").AsString()).ToList();

        _engine.Execute("posted.length = 0");

        foreach (var message in messages) Take(PageCommand.Parse(message) ?? throw new InvalidOperationException("the page sent a message the plugin cannot read: " + message));
    }

    private void Take(PageCommand command)
    {
        switch (command)
        {
            case PageCommand.Ready:
                HasSaidReady = true;
                break;
            case PageCommand.Note note:
                Notes.Add(note.Text);
                break;
            default:
                Sent.Add(command);
                break;
        }
    }

    private static string Resource(string name)
    {
        using var stream = typeof(PageScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
