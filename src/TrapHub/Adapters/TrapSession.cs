using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using TrapHub.Core;
using UnityEngine;

namespace TrapHub.Adapters;

/// <summary>
/// The window's game side, run once a frame: its open state, the view and catalog it sends to page.js, the HUD
/// button's badge, the presses and preset edits the page asks for, and the auto re-bait pass. Page messages arrive on
/// the web view's thread, so they are queued and handled here, in this frame.
/// </summary>
internal sealed class TrapSession
{
    private const float OpenRefreshSeconds = 1f;
    // Short enough that the HUD trap panel's rows and Check traps tip are close to what a press would do.
    private const float ClosedRefreshSeconds = 2f;
    private const float AutoSeconds = 5f;

    private readonly ITrapGame _game;
    private readonly WebPage _page;
    private readonly WebKeyboard _keyboard = new();
    private readonly PresetFileOnDisk _file;
    private readonly Action<SoundGroup, SoundMode> _saveSound;
    private readonly Action<bool> _saveRebait;
    private readonly Action<string> _log;
    private readonly Dictionary<long, float> _retryAt = new();

    private PresetBook? _book;
    private KeyCode _panelKey;
    private bool _isPanelShifted;
    private KeyCode _checkKey;
    private IReadOnlyDictionary<SoundGroup, SoundMode> _sounds = new Dictionary<SoundGroup, SoundMode>();
    private bool _isSoundsElsewhere;
    private bool _isRebaitOnCheck = true;
    private bool _hasReportedPopFailure;

    private bool _isOpen;
    private string _shownView = "";
    private TrapCatalog? _sentCatalog;
    private int? _sentBadge;
    private string _sentHud = "";
    private bool? _sentLive;
    private float _nextRefreshAt;
    private float _nextAutoAt;
    private TrapWorld? _lastWorld;
    private ViewSettings? _settings;

    // What the page's HUD rows and open window were drawn from: a read that matches is not drawn again. The Check
    // traps route in the HUD starts from where the player stands, so the HUD is drawn again when they move.
    private readonly DrawnFrom _hud = new(isPositionDrawn: true);
    private readonly DrawnFrom _view = new(isPositionDrawn: false);

    public TrapSession(ITrapGame game, WebPage page, PresetFileOnDisk file, Action<SoundGroup, SoundMode> saveSound, Action<bool> saveRebait, Action<string> log)
    {
        _game = game;
        _page = page;
        _file = file;
        _saveSound = saveSound;
        _saveRebait = saveRebait;
        _log = log;
    }

    private static bool IsChinese => GameLanguage.IsChinese();

    /// <summary>Called on the main thread at start and after each settings change; the page hears the key at the next refresh.</summary>
    public void UseControls(KeyCode panelKey, bool isPanelShifted, KeyCode checkKey)
    {
        _panelKey = panelKey;
        _isPanelShifted = isPanelShifted;
        _checkKey = checkKey;
        _sentLive = null;
        _settings = null;
        ForgetDrawn();
    }

    /// <summary>How the page names the window's key, "Shift+J" when Shift is part of it; empty when there is none.</summary>
    private string PanelKeyName => _panelKey == KeyCode.None ? "" : (_isPanelShifted ? "Shift+" : "") + _panelKey;

    private string CheckKeyName => _checkKey == KeyCode.None ? "" : _checkKey.ToString();

    /// <summary>Made again only when a key or sound setting changes.</summary>
    private ViewSettings Settings => _settings ??= new ViewSettings(_sounds, _isSoundsElsewhere, PanelKeyName, CheckKeyName);

    public void UseSounds(IReadOnlyDictionary<SoundGroup, SoundMode> sounds, bool isSoundsElsewhere)
    {
        _sounds = sounds;
        _isSoundsElsewhere = isSoundsElsewhere;
        _settings = null;
        ForgetDrawn();
    }

    /// <summary>Whether Check traps also baits the traps it empties (RebaitOnCheck).</summary>
    public void UseRebaitOnCheck(bool isOn)
    {
        _isRebaitOnCheck = isOn;
        _sentHud = "";
        _hud.Forget();
        _nextRefreshAt = 0f;
    }

    /// <summary>The page may have lost what it shows, or a setting it shows changed: the next refresh draws everything again.</summary>
    private void ForgetDrawn()
    {
        _shownView = "";
        _view.Forget();
        _hud.Forget();
    }

    public void Update()
    {
        var now = Time.unscaledTime;

        _page.KeepInstalled(now);
        SoundGate.Tick(now);

        while (PageMessages.TryTake(out var command)) HandleSafely(command);

        // The keys are asked about first and Shift only for a key that went down, so a quiet frame asks Unity little.
        // The window's key wins when both keys are the same and Shift is not part of the window's key.
        if (!_keyboard.IsClaimed)
        {
            if (_panelKey != KeyCode.None && Input.GetKeyDown(_panelKey) && InputGate.IsShiftHeld() == _isPanelShifted)
            {
                Toggle();

                return;
            }

            if (_checkKey != KeyCode.None && Input.GetKeyDown(_checkKey) && !InputGate.IsShiftHeld())
            {
                Check("check traps (key)", null);

                return;
            }

            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();

                return;
            }
        }

        if (now >= _nextRefreshAt) Refresh(now);
    }

    /// <summary>One command that throws must not cost the rest of the frame its commands or the key checks.</summary>
    private void HandleSafely(PageCommand command)
    {
        try
        {
            Handle(command);
        }
        catch (Exception ex)
        {
            _log($"page command {command} failed: {ex}");
            _page.Status(Texts.Failed.Pick(IsChinese));
        }
    }

    private void Handle(PageCommand command)
    {
        switch (command)
        {
            case PageCommand.Toggle:
                Toggle();
                break;
            case PageCommand.Close:
                Close();
                break;
            case PageCommand.Ready:
                _log("page script ready");
                _page.ForgetWords();
                _sentLive = null;
                _sentBadge = null;
                _sentCatalog = null;
                _sentHud = "";
                ForgetDrawn();
                _nextRefreshAt = 0f;
                break;
            case PageCommand.NoScript:
                _page.Install();
                break;
            case PageCommand.Note note:
                _log("page: " + note.Text);
                break;
            case PageCommand.Focus focus:
                if (focus.IsTyping) _keyboard.Claim();
                else _keyboard.Release();
                break;
            case PageCommand.TakeAll:
                Press("take all", () => TrapRun.TakeAll(_game, null));
                break;
            case PageCommand.TakeFloor:
                Press("take this floor", () => TrapRun.TakeAll(_game, _lastWorld?.PlayerFloor));
                break;
            case PageCommand.CheckAll check:
                Check(check.IsThisFloor ? "check this floor" : "check traps", check.IsThisFloor ? _lastWorld?.PlayerFloor : null);
                break;
            case PageCommand.RebaitOnCheck rebait:
                // Without Exiled's Toolkit a changed setting is not applied again on its own, so it is used here too.
                _saveRebait(rebait.IsOn);
                UseRebaitOnCheck(rebait.IsOn);
                _log("re-bait on check " + (rebait.IsOn ? "on" : "off"));
                break;
            case PageCommand.RebaitAll:
                Press("re-bait all", () => TrapRun.RebaitAll(_game, Book()));
                break;
            case PageCommand.Rebait rebait:
                Press($"re-bait {rebait.TrapId}", () => TrapRun.Rebait(_game, Book(), rebait.TrapId));
                break;
            case PageCommand.Take take:
                Press($"take {take.TrapId}", () => TrapRun.Take(_game, take.TrapId));
                break;
            case PageCommand.Dismantle dismantle:
                Press($"dismantle {dismantle.TrapId}", () => TrapRun.Dismantle(_game, dismantle.TrapId));
                break;
            case PageCommand.Assign assign:
                Keep(BookEdits.Assign(Book(), SaveSlot.CurrentKey(), assign, IsChinese));
                break;
            case PageCommand.SavePreset save:
                Keep(BookEdits.SavePreset(Book(), save.Json, IsChinese));
                break;
            case PageCommand.DeletePreset delete:
                Keep(BookEdits.DeletePreset(Book(), delete.PresetId, IsChinese));
                break;
            case PageCommand.Allow allow:
                Keep(BookEdits.Allow(Book(), allow, IsChinese));
                break;
            case PageCommand.Sound sound:
                SetSound(sound.Group, sound.Mode);
                break;
            default:
                throw new InvalidOperationException("unhandled page command " + command);
        }
    }

    /// <summary>
    /// The presets are read from trap-hub.json the first time they are needed. A file that is there but is not a preset
    /// file is copied aside before a fresh book replaces it, so nothing anyone wrote is lost.
    /// </summary>
    private PresetBook Book()
    {
        if (_book != null) return _book;

        var text = _file.Read();
        var book = string.IsNullOrWhiteSpace(text) ? null : PresetJson.ReadBook(text!);

        if (book == null && !string.IsNullOrWhiteSpace(text)) _file.SetAside();

        _book = book ?? PresetBook.First(Texts.FirstPresetName.Pick(IsChinese));
        _log($"presets: {_book.Presets.Count}, allowed bait: {_book.Allowed.Count}");

        return _book;
    }

    /// <summary>A changed book is used at once and written to disk; a failed write keeps it until the game closes, and says so.</summary>
    private void Keep(BookEdit edit)
    {
        var message = edit.Message;

        if (edit.Book != null)
        {
            _book = edit.Book;

            if (!_file.Replace(PresetJson.WriteBook(edit.Book))) message = Texts.NotSaved.Pick(IsChinese);
        }

        _log(message);
        _page.Status(message);
        Redraw();
    }

    private void SetSound(SoundGroup group, SoundMode mode)
    {
        if (_isSoundsElsewhere)
        {
            _page.Status(Texts.SoundsElsewhere.Pick(IsChinese));

            return;
        }

        // Without Exiled's Toolkit a changed setting is not applied again on its own, so the new mode is applied here too.
        _saveSound(group, mode);
        _sounds = new Dictionary<SoundGroup, SoundMode>(_sounds) { [group] = mode };
        _settings = null;
        _view.Forget();
        SoundGate.Use(_sounds);
        _page.Status(Texts.SoundSet.Say(IsChinese, Sounds.GroupName(group).Pick(IsChinese), Sounds.ModeLabel(mode).Pick(IsChinese)));
        Redraw();
    }

    /// <summary>
    /// Reads the traps once: the HUD button shows while there is a home to manage, its badge counts traps that need the
    /// player, and an open window is redrawn when its view changed. A read that throws is logged by the game adapter and
    /// read as not ready, so the window stays up with the reason instead of holding the game's input with nothing in it.
    /// </summary>
    /// <remarks>
    /// While the game's tables are still being read (a slice a frame, after a save loads or the language changes) it
    /// asks again next frame and leaves the HUD as it is. The auto pass uses the same read when it is due, and the game
    /// is read again only when the pass baited something.
    /// </remarks>
    private void Refresh(float now)
    {
        _nextRefreshAt = now + (_isOpen ? OpenRefreshSeconds : ClosedRefreshSeconds);

        var read = _game.Read();

        if (read is TrapRead.NotReady { IsStillLoading: true })
        {
            _nextRefreshAt = now;

            if (_isOpen) Show(read, isOpening: false);

            return;
        }

        if (read is TrapRead.Ready ready && now >= _nextAutoAt && RunAuto(ready.World, now)) read = _game.Read();

        var world = (read as TrapRead.Ready)?.World;

        _lastWorld = world;
        SendLive(world != null);
        SendBadge(world == null ? 0 : TrapView.NeedsYou(world));

        if (world != null) SendCatalog(world.Catalog);

        SendHudFor(world);

        if (_isOpen) Show(read, isOpening: false);
    }

    /// <summary>A read that matches the one the HUD rows were drawn from would draw the same rows, so they are not drawn again.</summary>
    private void SendHudFor(TrapWorld? world)
    {
        if (world == null)
        {
            _hud.Forget();
            SendHud(HudView.NotReady());

            return;
        }

        var book = Book();

        if (_hud.IsSame(world, book)) return;

        var hudJson = HudView.Ready(world, book, _game.FloorCost, _isRebaitOnCheck, CheckKeyName);

        SendHud(hudJson);

        if (hudJson == _sentHud) _hud.Remember(world, book);
    }

    private void SendLive(bool isLive)
    {
        if (isLive == _sentLive) return;
        if (_page.Live(isLive, PanelKeyName)) _sentLive = isLive;

        _log($"HUD button {(isLive ? "shown" : "hidden")}");
    }

    /// <summary>The HUD trap panel's rows and Check traps plan, sent only when they changed.</summary>
    private void SendHud(string hudJson)
    {
        if (hudJson == _sentHud) return;
        if (_page.Hud(hudJson)) _sentHud = hudJson;
    }

    private void SendBadge(int count)
    {
        if (count == _sentBadge) return;
        if (_page.Badge(count)) _sentBadge = count;
    }

    private void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    /// <summary>With no game loaded there is nothing to show and the window stays shut; anything else opens it.</summary>
    private void Open()
    {
        var read = _game.Read();

        if (read is TrapRead.NoGame)
        {
            _page.Status(Texts.NoGame.Pick(IsChinese));

            return;
        }

        _isOpen = true;
        InputGate.PanelOpened();
        Show(read, isOpening: true);
        _log("window opened");
    }

    private void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _keyboard.Release();
        InputGate.PanelClosed();
        _page.Close();
    }

    private void Redraw()
    {
        if (_isOpen) Show(_game.Read(), isOpening: false);
    }

    /// <summary>The catalog goes first, once per game and language, since the view names items only by id.</summary>
    private void Show(TrapRead read, bool isOpening)
    {
        string view;

        switch (read)
        {
            case TrapRead.Ready ready:
                var book = Book();

                _lastWorld = ready.World;
                SendCatalog(ready.World.Catalog);

                if (!isOpening && _view.IsSame(ready.World, book)) return;

                view = TrapView.Ready(ready.World, book, Settings);
                _view.Remember(ready.World, book);
                break;
            case TrapRead.NotReady notReady:
                _view.Forget();
                view = TrapView.NotReady(Texts.NotReady.Say(IsChinese, notReady.Reason), Settings);
                break;
            default:
                Close();
                return;
        }

        if (isOpening)
        {
            _shownView = view;
            _page.Open(view);

            return;
        }

        if (view == _shownView) return;

        _shownView = view;
        _page.Show(view);
    }

    private void SendCatalog(TrapCatalog catalog)
    {
        if (ReferenceEquals(catalog, _sentCatalog)) return;
        if (_page.Catalog(CatalogView.Write(catalog))) _sentCatalog = catalog;
    }

    /// <summary>
    /// Re-baits the traps whose preset asks for it, every few seconds, on the read the refresh just made. Only a pass
    /// that baited something says so, as a toast, so a quiet base stays quiet. True when it said so.
    /// </summary>
    private bool RunAuto(TrapWorld world, float now)
    {
        _nextAutoAt = now + AutoSeconds;

        var book = Book();

        if (!TrapRun.HasAuto(book)) return false;

        var outcome = TrapRun.Auto(_game, world, book, now, _retryAt);

        if (!TrapMessages.IsWorthShowing(outcome)) return false;

        var message = TrapMessages.Describe(outcome, IsChinese, isAuto: true);

        _log("auto: " + message);
        _page.Status(message);

        return true;
    }

    /// <summary>
    /// Check traps from any of its buttons or its key. Like the game's Tend Garden, a check that lined something up
    /// shows the game's own popup ("Check x5"), at the press rather than after the walk.
    /// </summary>
    private void Check(string name, int? onlyFloor)
    {
        var outcome = Press(name, () => TrapRun.CheckAll(_game, Book(), onlyFloor, _isRebaitOnCheck));
        var count = outcome == null ? 0 : TrapMessages.CheckedCount(outcome);

        if (count > 0) Pop(Texts.CheckPop.Say(IsChinese, count));
    }

    private void Pop(string text)
    {
        try
        {
            Ac_PopText_AddPopText.SendAction(text);
        }
        catch (Exception ex)
        {
            if (!_hasReportedPopFailure) _log("could not show the game's popup (reported once): " + ex.Message);

            _hasReportedPopFailure = true;
        }
    }

    /// <summary>
    /// Runs one press and shows what it did, then refreshes at once, so a queued badge or new count shows in the window
    /// and the HUD panel from one read. Null when the press threw.
    /// </summary>
    private TrapOutcome? Press(string name, Func<TrapOutcome> press)
    {
        string message;
        TrapOutcome? outcome = null;

        try
        {
            outcome = press();
            message = TrapMessages.Describe(outcome, IsChinese);
            _log($"{name}: {outcome} -> {message}");
        }
        catch (Exception ex)
        {
            message = Texts.Failed.Pick(IsChinese);
            _log($"{name} failed: {ex}");
        }

        _page.Status(message);
        Refresh(Time.unscaledTime);

        return outcome;
    }
}
