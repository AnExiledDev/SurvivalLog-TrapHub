// Trap Hub's web half. Runs in Root.html, the document every game page iframe sits in. Draws the Trap Hub window over
// all of them, styled like the game's own panels, and puts a trap button with a count badge in the HUD's tool column
// (bottom left, above the phone and bag buttons), built like the game's own buttons there. Talks to the plugin through
// vuplex.postMessage("slmod|traphub|<verb>") and is driven by window.__sltraphub.
(function () {
  if (window.__sltraphub) return;

  var PREFIX = 'slmod|traphub|';
  var HUD_CLASS = 'th-hud-btn';
  var HUD_STYLE_ID = 'th-hud-style';
  var BUSY_TIMEOUT_MS = 5000;
  var TOAST_MS = 3500;
  var MAX_CARDS = 240;
  var MAX_ADD = 30;
  var MAX_KEEP = 99;
  var SVG_NS = 'http://www.w3.org/2000/svg';
  // A cage: a top bar, three bars and a floor, on the 24-unit grid the game's HUD icons use.
  var CAGE_PATH = 'M3 3h18v3H3V3zm2 4h2v10H5V7zm5 0h2v10h-2V7zm5 0h2v10h-2V7zm4 0h2v10h-2V7zM3 18h18v3H3v-3z';
  var TABS = ['traps', 'presets', 'allowed', 'settings'];
  var TAB_WORD = { traps: 'tabTraps', presets: 'tabPresets', allowed: 'tabAllowed', settings: 'tabSettings' };
  var STATE_WORD = { armed: 'stArmed', prey: 'stPrey', nobait: 'stNoBait', halted: 'stHalted' };
  var KINDS = ['universal', 'preference', 'specialized', 'balanced', 'other'];
  var KIND_WORD = { universal: 'kUniversal', preference: 'kPreference', specialized: 'kSpecialized', balanced: 'kBalanced', other: 'kOther' };
  var SOUNDS = [['cage', 'sCage'], ['net', 'sNet'], ['other', 'sOther']];
  var MODES = [['Normal', 'mNormal'], ['Once', 'mOnce'], ['Muted', 'mMuted']];
  var AUTOS = [['off', 'autoOff'], ['nearby', 'autoNearby'], ['always', 'autoAlways']];
  // Presses the page answers itself; everything else goes to the plugin.
  var LOCAL = {
    tab: pickTab, pick: pickTrap, check: checkTrap, clearchecks: clearChecks, preset: pickPreset, newpreset: newPreset,
    stepup: function (i) { moveStep(+i, -1); }, stepdown: function (i) { moveStep(+i, 1); }, stepdel: removeStep,
    keepless: function (i) { changeKeep(+i, -1); }, keepmore: function (i) { changeKeep(+i, 1); }, stepadd: addStep,
    lastresort: function () { editDraft(function (d) { d.lastResort = !d.lastResort; }); },
    spoil: function () { editDraft(function (d) { d.spoilFirst = !d.spoilFirst; }); },
    source: function (v) { editDraft(function (d) { d.source = v; }); },
    autoset: function (v) { editDraft(function (d) { d.auto = v; }); },
    deftype: toggleDefaultType, filter: function (v) { state.filter = v; render(); },
    kind: function (v) { state.kind = state.kind === v ? '' : v; render(); }
  };
  // Presses that ask before they act: the first press arms the button, the second sends it.
  var CONFIRMED = { dismantle: true, deletepreset: true };
  // The plugin hands over the words in the game's language before every call (Texts.cs); English stands in until then.
  var ENGLISH = {
    tabTraps: 'Traps', tabPresets: 'Bait presets', tabAllowed: 'Allowed bait', tabSettings: 'Settings',
    cPrey: '{0} with prey', cNoBait: '{0} no bait', cWorn: '{0} worn', cHalted: '{0} halted',
    takeAll: 'Take all ({0})', takeFloor: 'Take this floor ({0})', rebaitAll: 'Re-bait all ({0})', floor: 'Floor {0}',
    here: 'you are here', stArmed: 'Armed', stPrey: 'Prey', stNoBait: 'No bait', stHalted: 'Halted', bait: 'Bait: {0}',
    next: 'Re-bait uses: {0}', nextNone: 'No allowed bait left for its preset', catches: '{0} catches left',
    breaks: 'Breaks on its next catch', store: 'Store: {0}', chance: 'Catch chance {0}%', queued: 'Walk queued',
    take: 'Take', rebait: 'Re-bait', dismantle: 'Dismantle', confirm: 'Sure?', preset: 'Preset',
    typeDefault: 'Type default ({0})', none: 'None', empty: 'No traps placed at home yet.', newPreset: 'New preset',
    name: 'Name', steps: 'Bait order: the first one you have enough of is used', noSteps: 'No baits yet. Add some below.',
    keep: 'Keep at least', own: 'Own {0}', notAllowed: 'Not allowed, skipped', lastResort: 'Then any allowed universal bait',
    source: 'Take bait from', srcBag: 'Bag only', srcHome: 'Bag, then home storage',
    spoilFirst: 'Use the food closest to spoiling first', auto: 'Auto re-bait', autoOff: 'Off', autoNearby: 'On my floor',
    autoAlways: 'Always', autoNote: 'Puts bait in an empty trap by itself, from this preset, without a walk.',
    defaultFor: 'Default for', save: 'Save', 'delete': 'Delete', addBait: 'Add a bait', add: 'Add', search: 'Search',
    up: 'Up', down: 'Down', remove: 'Remove', takenBy: 'Taken by: {0}',
    allowedNote: 'Trap Hub only puts bait you allow here in your traps. Everything starts blocked.', fAll: 'All',
    fOwned: 'I own', fAllowed: 'Allowed', fBlocked: 'Blocked', allowShown: 'Allow all shown', blockShown: 'Block all shown',
    allowed: 'Allowed', blocked: 'Blocked', allow: 'Allow', block: 'Block', kUniversal: 'Universal',
    kPreference: 'Preference', kSpecialized: 'Specialized', kBalanced: 'Balanced', kOther: 'Other', draws: 'Draws: {0}',
    sounds: 'Catch sounds',
    soundsNote: 'The sound a trap plays while it holds a catch. Chime once plays it for a few seconds, then goes quiet.',
    sCage: 'Live Capture Cage', sNet: 'Simple Bird Net', sOther: 'Mousetrap, Iron Animal Trap, Auto Trap Cage',
    mNormal: 'Normal', mOnce: 'Chime once', mMuted: 'Muted',
    quietTraps: 'Quiet Traps is installed, so it handles catch sounds.', keyOpen: '{0} opens and closes this window.',
    keyNone: 'No key is set; use the trap button on the left of the screen.',
    presetFile: 'Presets and allowed bait are kept in BepInEx/config/trap-hub.json, shared by every save.',
    escClose: 'Esc to close', tip: 'Your traps: catches, bait and wear',
    noSaveYet: 'Save the game once to pick presets per trap.', selected: '{0} selected', clear: 'Clear',
    assignTo: 'Bait preset for the selected traps:', pickTrap: 'Pick a trap to see its bait and catch odds.',
    durability: 'Durability {0} / {1}', odds: 'Likely catches', more: '{0} more. Search to narrow the list.',
    unsaved: 'Unsaved changes', on: 'On', off: 'Off', kinds: 'Bait kind'
  };

  var state = {
    data: { ready: false, note: '' }, isOpen: false, isLive: false, key: '', badge: 0, tab: 'traps', sel: '', checks: {},
    presetId: '', draft: null, draftOf: '', pendingNewFrom: null, filter: 'owned', kind: '', search: '', addSearch: '',
    armed: '', isTyping: false, isStale: false
  };
  var cat = { types: {}, baits: {}, prey: {}, baitList: [] };
  var ui = null;
  var toastTimer = 0;
  var isBusy = false;
  var busyTimer = 0;
  var hudVersion = 0;
  var hasReportedPageAccess = false;
  var hasReportedHudPlaced = false;

  function say(key) {
    var words = window.__sltraphubWords || ENGLISH;
    var text = words[key] || ENGLISH[key] || key;

    for (var i = 1; i < arguments.length; i++) text = text.split('{' + (i - 1) + '}').join(String(arguments[i]));

    return text;
  }

  function send(message) {
    try {
      if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
    } catch (e) {}
  }

  // One press at a time: a double click would otherwise ask twice. Every press is answered by a status line, which
  // frees the buttons; the timer frees them anyway if that answer never comes.
  function sendAction(message) {
    if (isBusy) return;

    isBusy = true;
    clearTimeout(busyTimer);
    busyTimer = setTimeout(clearBusy, BUSY_TIMEOUT_MS);
    send(message);
  }

  function clearBusy() {
    isBusy = false;
    clearTimeout(busyTimer);
  }

  function el(tag, className, text) {
    var node = document.createElement(tag);

    if (className) node.className = className;
    if (text != null) node.textContent = text;

    return node;
  }

  function put(parent) {
    for (var i = 1; i < arguments.length; i++) {
      if (arguments[i]) parent.appendChild(arguments[i]);
    }

    return parent;
  }

  function button(text, action, args, isEnabled, extraClass) {
    var node = el('button', 'th-btn' + (extraClass ? ' ' + extraClass : ''), text);
    node.setAttribute('data-act', action);

    if (args != null) node.setAttribute('data-args', String(args));
    if (isEnabled === false) node.disabled = true;

    return node;
  }

  // A row of buttons where one is the current choice.
  function segment(options, current, action, isEnabled) {
    var box = el('div', 'th-seg');

    options.forEach(function (option) {
      box.appendChild(button(say(option[1]), action, option[0], isEnabled, option[0] === current ? 'th-on' : ''));
    });

    return box;
  }

  function toggle(isOn, action, label) {
    return button(label, action, null, true, 'th-tog' + (isOn ? ' th-on' : ''));
  }

  var CSS = [
    '#th-root{position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(0,0,0,.4);font-family:"Microsoft YaHei","PingFang SC",sans-serif;color:#eee;user-select:none}',
    '#th-root *{box-sizing:border-box}',
    '.th-plate{width:min(1480px,95vw);height:min(880px,90vh);display:flex;flex-direction:column;gap:10px;padding:16px 20px;border-radius:10px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(30,30,33,.93),rgba(18,18,20,.95));box-shadow:0 10px 40px rgba(0,0,0,.6)}',
    '.th-head{display:flex;align-items:center;gap:14px}',
    '.th-title{font-size:20px;font-weight:600;letter-spacing:1px;text-shadow:0 2px 6px rgba(0,0,0,.6)}',
    '.th-sub{flex:1;display:flex;gap:8px;flex-wrap:wrap}',
    '.th-pill{font-size:12px;padding:3px 9px;border-radius:10px;background:rgba(255,255,255,.08);color:rgba(238,238,238,.8)}',
    '.th-pill.th-g{background:rgba(124,179,66,.22);color:#aed581}.th-pill.th-r{background:rgba(229,57,53,.24);color:#ff8a80}.th-pill.th-a{background:rgba(235,163,8,.22);color:#EBA308}',
    '.th-tabs{display:flex;gap:4px;border-bottom:1px solid rgba(255,255,255,.12)}',
    '.th-tab{font:inherit;font-size:14px;color:rgba(238,238,238,.65);padding:8px 16px;border:none;border-bottom:2px solid transparent;background:none;cursor:pointer}',
    '.th-tab:hover{color:#eee}.th-tab.th-on{color:#EBA308;border-bottom-color:#EBA308}',
    '.th-body{flex:1;min-height:0;display:flex;gap:10px}',
    '.th-well{min-height:0;overflow-y:auto;background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);border-radius:8px;padding:8px}',
    '.th-col{display:flex;flex-direction:column;gap:8px;min-height:0;min-width:0}',
    '.th-bar{display:flex;align-items:center;gap:8px;flex-wrap:wrap}',
    '.th-bulk{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:8px 10px;border-radius:8px;border:1px solid rgba(235,163,8,.5);background:rgba(235,163,8,.07);font-size:13px}',
    '.th-sect{font-size:12px;color:#EBA308;letter-spacing:1.5px;text-transform:uppercase;padding:8px 4px 4px;display:flex;gap:8px;align-items:center}',
    '.th-sect .th-r{margin-left:auto;text-transform:none;letter-spacing:0;color:rgba(238,238,238,.5)}',
    '.th-cards{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px}',
    '.th-card{position:relative;display:grid;grid-template-columns:44px minmax(0,1fr) auto;gap:10px;align-items:center;padding:8px 10px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent;cursor:pointer}',
    '.th-card:hover{background:rgba(255,255,255,.07)}.th-card.th-sel{border-color:rgba(235,163,8,.75);background:rgba(235,163,8,.08)}',
    '.th-card .th-chk{position:absolute;left:-1px;top:-1px;width:18px;height:18px;padding:0;font-size:11px;line-height:16px;border-radius:7px 0 5px 0}',
    '.th-card .th-chk.th-on{background:#EBA308;color:#111;border-color:#EBA308}',
    '.th-nm{font-size:14px;font-weight:600;display:flex;gap:6px;align-items:center;white-space:nowrap;overflow:hidden}',
    '.th-ln{font-size:12px;color:rgba(238,238,238,.7);display:flex;gap:6px;align-items:center;margin-top:4px;white-space:nowrap;overflow:hidden}',
    '.th-rt{display:flex;flex-direction:column;align-items:flex-end;gap:4px;font-size:12px;color:rgba(238,238,238,.55)}',
    '.th-badge{flex:none;font-size:11px;padding:2px 7px;border-radius:5px;background:rgba(255,255,255,.1);color:rgba(238,238,238,.8)}',
    '.th-b-prey{background:rgba(124,179,66,.24);color:#aed581}.th-b-nobait{background:rgba(229,57,53,.26);color:#ff8a80}.th-b-halted{background:rgba(255,255,255,.06);color:rgba(238,238,238,.5)}.th-b-worn{background:rgba(235,163,8,.22);color:#EBA308}',
    '.th-track{width:100px;height:6px;border-radius:3px;background:rgba(255,255,255,.1);overflow:hidden;flex:none}',
    '.th-fill{height:100%;border-radius:3px;background:#7cb342}.th-fill.th-a{background:#EBA308}.th-fill.th-r{background:#e53935}',
    '.th-detail{width:440px;flex:none;display:flex;flex-direction:column;gap:10px}',
    '.th-dhead{display:flex;gap:12px;align-items:center}.th-dhead .th-big{font-size:18px;font-weight:700}.th-dim{color:rgba(238,238,238,.55);font-size:12px}',
    '.th-box{border:1px solid rgba(255,255,255,.1);border-radius:8px;padding:10px 12px;background:rgba(255,255,255,.025);display:flex;flex-direction:column;gap:6px}',
    '.th-box h4{margin:0;font-size:12px;color:#EBA308;letter-spacing:1.5px;text-transform:uppercase;font-weight:600}',
    '.th-row{display:flex;align-items:center;gap:8px;font-size:13px;padding:3px 4px;border-radius:6px}',
    '.th-row .th-grow{flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    '.th-row.th-out{opacity:.45}.th-row.th-cur{background:rgba(124,179,66,.12)}',
    '.th-num{font-weight:600;color:#EBA308;font-variant-numeric:tabular-nums}',
    '.th-red{color:#ff8a80}.th-green{color:#aed581}',
    '.th-plist{width:280px;flex:none}',
    '.th-pitem{display:flex;flex-direction:column;gap:2px;padding:9px 10px;margin-bottom:4px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent;cursor:pointer}',
    '.th-pitem.th-sel{border-color:rgba(235,163,8,.75);background:rgba(235,163,8,.08)}.th-pitem b{font-size:14px}',
    '.th-editor{flex:1}',
    '.th-step{display:grid;grid-template-columns:24px 32px minmax(0,1fr) 90px 150px 150px;gap:8px;align-items:center;padding:6px 8px;border-radius:7px;background:rgba(255,255,255,.04);font-size:13px;margin-bottom:4px}',
    '.th-step.th-blocked{background:rgba(211,47,47,.08)}.th-step.th-short{box-shadow:inset 3px 0 0 #D32F2F}',
    '.th-ord{width:22px;height:22px;border-radius:50%;display:grid;place-items:center;background:rgba(235,163,8,.18);color:#EBA308;font-weight:700;font-size:12px}',
    '.th-set{display:flex;align-items:center;gap:12px;padding:8px 4px;font-size:13px;border-top:1px solid rgba(255,255,255,.06)}.th-set .th-lbl{flex:1}',
    '.th-seg{display:flex;gap:0}.th-seg .th-btn{border-radius:0}.th-seg .th-btn:first-child{border-radius:7px 0 0 7px}.th-seg .th-btn:last-child{border-radius:0 7px 7px 0}',
    '.th-btn.th-on{background:linear-gradient(180deg,rgba(235,163,8,.35),rgba(170,115,5,.35));border-color:rgba(235,163,8,.85);color:#fff}',
    '.th-inp{font:inherit;font-size:13px;color:#eee;background:rgba(0,0,0,.4);border:1px solid rgba(255,255,255,.2);border-radius:6px;padding:6px 9px;user-select:text}',
    '.th-inp:focus{outline:none;border-color:rgba(235,163,8,.75)}',
    '.th-wl{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:6px}',
    '.th-bait{display:grid;grid-template-columns:40px minmax(0,1fr) auto;gap:8px;align-items:center;padding:7px 9px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent;cursor:pointer;font-size:13px}',
    '.th-bait:hover{border-color:rgba(235,163,8,.5)}.th-bait.th-off .th-icon,.th-bait.th-off b{opacity:.45}',
    '.th-bait b{display:block;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.th-bait small{display:block;color:rgba(238,238,238,.5);font-size:11px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-lock{font-size:11px;padding:1px 7px;border-radius:5px}.th-lock.th-ok{color:#9ccc65;background:rgba(124,179,66,.15)}.th-lock.th-no{color:#ef9a9a;background:rgba(211,47,47,.15)}',
    '.th-note{font-size:12px;color:rgba(238,238,238,.55);padding:4px}',
    '.th-banner{padding:8px 12px;border-radius:7px;border:1px solid rgba(100,181,246,.6);background:rgba(100,181,246,.1);color:#bbdefb;font-size:13px}',
    '.th-lockmsg{padding:10px 14px;border-radius:7px;border:1px solid rgba(255,193,7,.75);background:rgba(255,193,7,.12);color:#EBA308;font-size:14px}',
    '.th-icon{flex:none;display:flex;align-items:center;justify-content:center;border-radius:6px;background:rgba(255,255,255,.05);overflow:hidden;color:rgba(238,238,238,.7);font-weight:600}',
    '.th-icon img{width:100%;height:100%;object-fit:contain}',
    '.th-i56{width:56px;height:56px;font-size:20px}.th-i44{width:44px;height:44px;font-size:18px}.th-i40{width:40px;height:40px;font-size:16px}.th-i30{width:30px;height:30px;font-size:13px}.th-i22{width:22px;height:22px;font-size:11px}',
    '.th-empty{padding:20px;color:rgba(238,238,238,.55);font-size:14px;text-align:center}',
    '.th-btn{font:inherit;font-size:13px;color:#dde2f0;padding:6px 11px;border-radius:7px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(58,58,60,.9),rgba(38,38,40,.92));cursor:pointer;white-space:nowrap}',
    '.th-btn:hover:not(:disabled){background:linear-gradient(180deg,rgba(72,66,48,.92),rgba(50,46,32,.95));border-color:rgba(255,193,7,.75)}',
    '.th-btn.th-danger:hover:not(:disabled),.th-btn.th-armed{background:linear-gradient(180deg,rgba(84,44,42,.92),rgba(58,30,28,.95));border-color:rgba(229,57,53,.75)}',
    '.th-btn.th-hot{border-color:rgba(124,179,66,.7)}',
    '.th-btn.th-sm{font-size:12px;padding:3px 8px}',
    '.th-btn:disabled{opacity:.4;cursor:default}',
    '.th-btn.th-x{width:30px;height:30px;padding:0;font-size:18px;line-height:28px}',
    '.th-foot{display:flex;gap:12px;font-size:13px;color:rgba(238,238,238,.7)}.th-status{flex:1}',
    '#th-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483001;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:8px;border:1px solid rgba(255,193,7,.75);background:linear-gradient(180deg,rgba(30,30,33,.94),rgba(18,18,20,.96));color:#eee;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:14px;box-shadow:0 6px 20px rgba(0,0,0,.6)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();

    var root = el('div');
    var plate = el('div', 'th-plate');
    var head = el('div', 'th-head');
    var sub = el('div', 'th-sub');
    var tabs = el('div', 'th-tabs');
    var body = el('div', 'th-body');
    var foot = el('div', 'th-foot');
    var status = el('div', 'th-status');
    var keys = el('div');
    var toast = el('div');

    root.id = 'th-root';
    toast.id = 'th-toast';
    put(head, el('div', 'th-title', 'Trap Hub'), sub, button('\u00d7', 'close', null, true, 'th-x'));
    put(foot, status, keys);
    put(plate, head, tabs, body, foot);
    root.appendChild(plate);
    document.body.appendChild(root);
    document.body.appendChild(toast);

    root.addEventListener('click', onClick);
    root.addEventListener('input', onInput);
    root.addEventListener('focusin', onFocus);
    root.addEventListener('focusout', onBlur);
    root.addEventListener('keydown', onKey);
    // Clicks inside the plate must not reach the game pages underneath; the mask itself closes nothing on purpose.
    root.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    // A broken icon path swaps to the first-letter placeholder; error events do not bubble, so capture them.
    root.addEventListener('error', function (e) {
      var img = e.target;

      if (img && img.tagName === 'IMG' && img.parentNode) img.parentNode.textContent = img.getAttribute('data-letter') || '?';
    }, true);

    ui = { root: root, sub: sub, tabs: tabs, body: body, status: status, keys: keys, toast: toast };
  }

  // The style outlives a lost window root, so a rebuild must not stack another copy of it.
  function addStyle() {
    if (document.getElementById('th-style')) return;

    var style = el('style');
    style.id = 'th-style';
    style.textContent = CSS;
    document.head.appendChild(style);
  }

  function onClick(e) {
    var target = e.target.closest('[data-act]');

    if (!target) return;

    e.stopPropagation();

    if (target.disabled) return;

    var act = target.getAttribute('data-act');
    var args = target.getAttribute('data-args');
    var armKey = act + '|' + (args || '');

    if (CONFIRMED[act] && state.armed !== armKey) {
      state.armed = armKey;
      render();

      return;
    }

    state.armed = '';

    if (LOCAL[act]) {
      LOCAL[act](args);

      return;
    }

    // Opening or shutting the window gets no status line back, so it must not hold the buttons busy.
    if (act === 'close' || act === 'toggle') {
      send(act);

      return;
    }

    if (act === 'savepreset') {
      savePreset();

      return;
    }

    sendAction(args ? act + '|' + args : act);
  }

  // Text boxes: typing pauses the game's keys, and the window is not redrawn under the cursor.
  function onFocus(e) {
    if (!isTextBox(e.target)) return;

    state.isTyping = true;
    send('focus|1');
  }

  function onBlur(e) {
    if (!isTextBox(e.target)) return;

    state.isTyping = false;
    send('focus|0');

    if (state.isStale) render();
  }

  function onKey(e) {
    if (e.key === 'Escape' && isTextBox(e.target) && e.target.blur) e.target.blur();
  }

  function onInput(e) {
    var field = e.target.getAttribute && e.target.getAttribute('data-field');

    if (field === 'name' && state.draft) {
      state.draft.name = e.target.value;
      markDirty();
    } else if (field === 'search') {
      state.search = e.target.value;
      fillBaitGrid();
    } else if (field === 'addsearch') {
      state.addSearch = e.target.value;
      fillAddList();
    }
  }

  function isTextBox(node) {
    return !!node && node.tagName === 'INPUT';
  }

  // ---------- data ----------

  function catalog(data) {
    cat = { types: {}, baits: {}, prey: {}, baitList: [] };

    ((data && data.types) || []).forEach(function (t) { cat.types[t.id] = t; });
    ((data && data.baits) || []).forEach(function (b) { cat.baits[b.id] = b; cat.baitList.push(b); });
    ((data && data.prey) || []).forEach(function (p) { cat.prey[p.id] = p; });

    if (state.isOpen) render();
  }

  function typeOf(id) { return cat.types[id] || { id: id, name: '#' + id, accepts: [], catches: [] }; }

  function baitOf(id) { return cat.baits[id] || { id: id, name: '#' + id, kind: 'other', draws: [] }; }

  function preyOf(id) { return cat.prey[id] || { id: id, name: '#' + id }; }

  function presets() { return state.data.presets || []; }

  function presetById(id) {
    var list = presets();

    for (var i = 0; i < list.length; i++) {
      if (list[i].id === id) return list[i];
    }

    return null;
  }

  function defaultPresetFor(typeId) {
    var list = presets();

    for (var i = 0; i < list.length; i++) {
      if ((list[i].types || []).indexOf(typeId) >= 0) return list[i];
    }

    return null;
  }

  function owned(itemId) { return (state.data.owned || {})[itemId] || 0; }

  function isAllowed(itemId) { return (state.data.allowed || []).indexOf(itemId) >= 0; }

  function traps() { return state.data.traps || []; }

  function trapById(id) {
    var list = traps();

    for (var i = 0; i < list.length; i++) {
      if (list[i].id === id) return list[i];
    }

    return null;
  }

  // ---------- frame ----------

  function render() {
    if (!ui || !state.isOpen) return;

    // Typing into a box must not lose the cursor to a refresh; the refresh shows when the box loses focus.
    if (state.isTyping) {
      state.isStale = true;

      return;
    }

    state.isStale = false;

    var scrolls = keepScrolls();

    renderHead();
    renderTabs();
    ui.body.textContent = '';

    if (state.tab === 'settings') renderSettings();
    else if (!state.data.ready) ui.body.appendChild(lockMessage(state.data.note));
    else if (state.tab === 'presets') renderPresets();
    else if (state.tab === 'allowed') renderAllowed();
    else renderTraps();

    restoreScrolls(scrolls);
    renderKeys();
  }

  function keepScrolls() {
    var kept = {};

    ui.body.querySelectorAll('[data-scroll]').forEach(function (node) { kept[node.getAttribute('data-scroll')] = node.scrollTop; });

    return kept;
  }

  function restoreScrolls(kept) {
    ui.body.querySelectorAll('[data-scroll]').forEach(function (node) {
      var top = kept[node.getAttribute('data-scroll')];

      if (top) node.scrollTop = top;
    });
  }

  function scroller(key, className) {
    var node = el('div', 'th-well' + (className ? ' ' + className : ''));
    node.setAttribute('data-scroll', key);

    return node;
  }

  function lockMessage(text) {
    var box = el('div', 'th-col');
    box.style.flex = '1';
    box.appendChild(el('div', 'th-lockmsg', text || ''));

    return box;
  }

  function renderHead() {
    var c = state.data.counts;
    ui.sub.textContent = '';

    if (!state.data.ready || !c) return;

    put(ui.sub,
      el('span', 'th-pill' + (c.prey ? ' th-g' : ''), say('cPrey', c.prey)),
      el('span', 'th-pill' + (c.noBait ? ' th-r' : ''), say('cNoBait', c.noBait)),
      el('span', 'th-pill' + (c.worn ? ' th-a' : ''), say('cWorn', c.worn)),
      el('span', 'th-pill', say('cHalted', c.halted)));
  }

  function renderTabs() {
    ui.tabs.textContent = '';

    TABS.forEach(function (tab) {
      var node = button(say(TAB_WORD[tab]), 'tab', tab, true, 'th-tab' + (state.tab === tab ? ' th-on' : ''));
      node.className = 'th-tab' + (state.tab === tab ? ' th-on' : '');
      ui.tabs.appendChild(node);
    });
  }

  function renderKeys() {
    ui.keys.textContent = state.key ? state.key + ' / ' + say('escClose') : say('escClose');
  }

  function pickTab(tab) {
    if (TABS.indexOf(tab) < 0) return;

    state.tab = tab;
    render();
  }

  // ---------- Traps tab ----------

  function renderTraps() {
    var data = state.data;
    var left = el('div', 'th-col');
    left.style.flex = '1';

    put(left, trapBar(data), bulkBar(), trapList(data));
    put(ui.body, left, trapDetail(trapById(state.sel)));
  }

  // The bulk buttons stay pressable with nothing to do: a press then says so.
  function trapBar(data) {
    var c = data.counts;

    return put(el('div', 'th-bar'),
      button(say('takeAll', c.take), 'takeall', null, true, 'th-hot'),
      button(say('takeFloor', c.takeFloor), 'takefloor', null, true),
      button(say('rebaitAll', c.rebait), 'rebaitall', null, true));
  }

  function checkedIds() {
    return Object.keys(state.checks).filter(function (id) { return state.checks[id] && trapById(id); });
  }

  function bulkBar() {
    var ids = checkedIds();

    if (!ids.length) return null;

    var bar = put(el('div', 'th-bulk'), el('b', '', say('selected', ids.length)), el('span', '', say('assignTo')));
    var args = '|' + ids.join(',');

    bar.appendChild(button(say('typeDefault', '...'), 'assign', args, state.data.saved));
    presets().forEach(function (p) { bar.appendChild(button(p.name, 'assign', p.id + args, state.data.saved)); });
    bar.appendChild(button(say('clear'), 'clearchecks', null, true));

    if (!state.data.saved) bar.appendChild(el('span', 'th-dim', say('noSaveYet')));

    return bar;
  }

  function trapList(data) {
    var well = scroller('traps');
    var list = traps();
    var lastFloor = null;
    var cards = null;

    if (!list.length) {
      well.appendChild(el('div', 'th-empty', say('empty')));

      return well;
    }

    list.forEach(function (trap) {
      if (trap.floor !== lastFloor) {
        lastFloor = trap.floor;

        var head = el('div', 'th-sect', say('floor', trap.floor));

        if (trap.floor === data.floor) head.appendChild(el('span', 'th-badge th-b-worn', say('here')));

        cards = el('div', 'th-cards');
        put(well, head, cards);
      }

      cards.appendChild(trapCard(trap));
    });

    return well;
  }

  function stateBadge(trap) {
    return el('span', 'th-badge th-b-' + trap.state, say(STATE_WORD[trap.state] || trap.state));
  }

  function durabilityBar(trap) {
    var track = el('span', 'th-track');
    var fill = el('span', 'th-fill' + (trap.worn ? ' th-r' : trap.dur * 2 < trap.max ? ' th-a' : ''));
    fill.style.display = 'block';
    fill.style.width = (trap.max > 0 ? Math.max(0, Math.min(100, Math.round(trap.dur * 100 / trap.max))) : 0) + '%';
    track.appendChild(fill);

    return track;
  }

  function trapCard(trap) {
    var type = typeOf(trap.type);
    var card = el('div', 'th-card' + (trap.id === state.sel ? ' th-sel' : ''));
    var main = el('div');
    var right = el('div', 'th-rt');
    var check = button(state.checks[trap.id] ? '\u2713' : '', 'check', trap.id, true, 'th-chk' + (state.checks[trap.id] ? ' th-on' : ''));
    var name = put(el('div', 'th-nm'), el('span', '', type.name), stateBadge(trap));

    card.setAttribute('data-act', 'pick');
    card.setAttribute('data-args', trap.id);

    if (trap.worn) name.appendChild(el('span', 'th-badge th-b-worn', say('breaks')));
    if (trap.queued) name.appendChild(el('span', 'th-badge th-b-prey', say('queued')));

    put(main, name, el('div', 'th-ln th-dim', trap.room), baitLine(trap),
      put(el('div', 'th-ln'), durabilityBar(trap), el('span', '', trap.catches == null ? trap.dur + ' / ' + trap.max : say('catches', trap.catches))));

    if (trap.prey) right.appendChild(icon(preyOf(trap.prey), 'th-i30'));
    if (trap.store) right.appendChild(el('span', 'th-badge', say('store', trap.store)));

    right.appendChild(el('span', '', say('chance', trap.chance)));
    put(card, check, icon(type, 'th-i44'), main, right);

    return card;
  }

  function baitLine(trap) {
    var line = el('div', 'th-ln');

    if (trap.bait) {
      var bait = baitOf(trap.bait);

      return put(line, icon(bait, 'th-i22'), el('span', '', say('bait', bait.name)));
    }

    if (!trap.canBait) return put(line, el('span', 'th-red', say('stNoBait')));
    if (!trap.next) return put(line, el('span', 'th-red', say('nextNone')));

    return put(line, icon(baitOf(trap.next), 'th-i22'), el('span', 'th-green', say('next', baitOf(trap.next).name)));
  }

  function trapDetail(trap) {
    var well = scroller('detail', 'th-detail');

    if (!trap) {
      well.appendChild(el('div', 'th-empty', say('pickTrap')));

      return well;
    }

    var type = typeOf(trap.type);
    var head = put(el('div', 'th-dhead'), icon(type, 'th-i56'),
      put(el('div', ''), el('div', 'th-big', type.name), el('div', 'th-dim', trap.room + ' \u00b7 ' + say('floor', trap.floor))));
    var isArmed = state.armed === 'dismantle|' + trap.id;
    var acts = put(el('div', 'th-bar'),
      button(say('take'), 'take', trap.id, trap.canTake && !trap.queued, 'th-hot'),
      button(say('rebait'), 'rebait', trap.id, trap.canBait),
      button(isArmed ? say('confirm') : say('dismantle'), 'dismantle', trap.id, true, 'th-danger' + (isArmed ? ' th-armed' : '')));

    head.appendChild(stateBadge(trap));
    put(well, head, acts, presetBox(trap), oddsBox(trap), wearBox(trap));

    return well;
  }

  // The trap's preset: its type's default or its own, with every step and what the player owns of it.
  function presetBox(trap) {
    var box = put(el('div', 'th-box'), el('h4', '', say('preset')));
    var pick = el('div', 'th-bar');
    var fallback = defaultPresetFor(trap.type);
    var preset = presetById(trap.preset);

    pick.appendChild(button(say('typeDefault', fallback ? fallback.name : say('none')), 'assign', '|' + trap.id, state.data.saved, 'th-sm' + (!trap.own ? ' th-on' : '')));
    presets().forEach(function (p) {
      pick.appendChild(button(p.name, 'assign', p.id + '|' + trap.id, state.data.saved, 'th-sm' + (trap.own && p.id === trap.preset ? ' th-on' : '')));
    });
    box.appendChild(pick);

    if (!state.data.saved) box.appendChild(el('div', 'th-dim', say('noSaveYet')));
    if (preset) box.appendChild(stepRows(preset, trap));
    if (trap.canBait) box.appendChild(el('div', trap.next ? 'th-green' : 'th-red', trap.next ? say('next', baitOf(trap.next).name) : say('nextNone')));

    return box;
  }

  function stepRows(preset, trap) {
    var rows = el('div', 'th-col');
    var accepts = typeOf(trap.type).accepts || [];

    rows.style.gap = '2px';

    preset.steps.forEach(function (step, i) {
      var bait = baitOf(step.item);
      var isUsable = isAllowed(step.item) && accepts.indexOf(step.item) >= 0 && owned(step.item) > step.keep;
      var row = el('div', 'th-row' + (step.item === trap.bait ? ' th-cur' : isUsable ? '' : ' th-out'));

      put(row, el('span', 'th-dim', String(i + 1)), icon(bait, 'th-i22'), el('span', 'th-grow', bait.name),
        el('span', isAllowed(step.item) ? 'th-dim' : 'th-red', isAllowed(step.item) ? say('own', owned(step.item)) : say('notAllowed')));
      rows.appendChild(row);
    });

    if (preset.lastResort) rows.appendChild(put(el('div', 'th-row th-dim'), el('span', '', '\u221e'), el('span', 'th-grow', say('lastResort'))));

    return rows;
  }

  function oddsBox(trap) {
    var box = put(el('div', 'th-box'), put(el('h4', ''), el('span', '', say('odds'))), el('div', 'th-num', say('chance', trap.chance)));

    (trap.odds || []).forEach(function (o) {
      var prey = preyOf(o.prey);

      box.appendChild(put(el('div', 'th-row'), icon(prey, 'th-i22'), el('span', 'th-grow', prey.name + (prey.rare ? ' \u2605' : '')), el('span', 'th-num', o.pct + '%')));
    });

    return box;
  }

  function wearBox(trap) {
    var box = put(el('div', 'th-box'), put(el('div', 'th-row'), durabilityBar(trap), el('span', '', say('durability', trap.dur, trap.max))));

    if (trap.catches != null) box.appendChild(el('div', trap.worn ? 'th-red' : 'th-dim', trap.worn ? say('breaks') : say('catches', trap.catches)));

    return box;
  }

  function pickTrap(id) {
    state.sel = state.sel === id ? '' : id;
    render();
  }

  function checkTrap(id) {
    state.checks[id] = !state.checks[id];
    render();
  }

  function clearChecks() {
    state.checks = {};
    render();
  }

  // ---------- Bait presets tab ----------

  function renderPresets() {
    syncDraft();

    var list = scroller('plist', 'th-plist');
    var editor = scroller('editor', 'th-editor');

    presets().forEach(function (p) {
      var item = put(el('div', 'th-pitem' + (p.id === state.presetId ? ' th-sel' : '')), el('b', '', p.name),
        el('span', 'th-dim', p.types.length ? say('defaultFor') + ': ' + p.types.map(function (t) { return typeOf(t).name; }).join(', ') : ''));

      item.setAttribute('data-act', 'preset');
      item.setAttribute('data-args', p.id);
      list.appendChild(item);
    });
    list.appendChild(button('+ ' + say('newPreset'), 'newpreset', null, true));

    if (state.draft) fillEditor(editor);

    put(ui.body, list, editor);
  }

  // The editor works on a copy, so a refresh from the game never throws away what the player is typing.
  function syncDraft() {
    if (state.pendingNewFrom) {
      var fresh = presets().filter(function (p) { return state.pendingNewFrom.indexOf(p.id) < 0; })[0];

      if (fresh) {
        state.pendingNewFrom = null;
        state.presetId = fresh.id;
        state.draft = null;
      }
    }

    if (state.draft && state.draftOf === state.presetId && (state.presetId === '' || presetById(state.presetId))) return;

    var source = presetById(state.presetId) || (state.presetId === '' && state.draft ? null : presets()[0]);

    if (!source) return;

    state.presetId = source.id;
    state.draft = copyPreset(source);
    state.draftOf = source.id;
  }

  function copyPreset(p) {
    return {
      id: p.id, name: p.name, steps: p.steps.map(function (s) { return { item: s.item, keep: s.keep }; }),
      lastResort: p.lastResort, source: p.source, spoilFirst: p.spoilFirst, auto: p.auto, types: (p.types || []).slice()
    };
  }

  function isDirty() {
    var saved = presetById(state.draftOf);

    return !saved || JSON.stringify(copyPreset(saved)) !== JSON.stringify(state.draft);
  }

  function fillEditor(editor) {
    var d = state.draft;
    var name = el('input', 'th-inp');
    var isArmed = state.armed === 'deletepreset|' + d.id;

    name.setAttribute('data-field', 'name');
    name.setAttribute('placeholder', say('name'));
    name.value = d.name;
    name.style.width = '280px';

    put(editor,
      put(el('div', 'th-bar'), name, button(say('save'), 'savepreset', null, true, 'th-hot'),
        d.id ? button(isArmed ? say('confirm') : say('delete'), 'deletepreset', d.id, true, 'th-danger' + (isArmed ? ' th-armed' : '')) : null,
        el('span', 'th-red th-dirty', isDirty() ? say('unsaved') : '')),
      el('div', 'th-sect', say('steps')));

    if (!d.steps.length) editor.appendChild(el('div', 'th-note', say('noSteps')));

    d.steps.forEach(function (step, i) { editor.appendChild(stepEditor(step, i, d.steps.length)); });

    if (d.lastResort) editor.appendChild(put(el('div', 'th-row th-dim'), el('span', 'th-ord', '\u221e'), el('span', 'th-grow', say('lastResort'))));

    put(editor, el('div', 'th-sect', say('addBait')), addPicker(),
      setting(say('lastResort'), toggle(d.lastResort, 'lastresort', say(d.lastResort ? 'on' : 'off'))),
      setting(say('source'), segment([['bag', 'srcBag'], ['home', 'srcHome']], d.source, 'source', true)),
      setting(say('spoilFirst'), toggle(d.spoilFirst, 'spoil', say(d.spoilFirst ? 'on' : 'off'))),
      setting(say('auto'), segment(AUTOS, d.auto, 'autoset', true), say('autoNote')),
      setting(say('defaultFor'), typeChips(d)));
  }

  function setting(label, control, note) {
    var lbl = put(el('div', 'th-lbl'), el('div', '', label));

    if (note) lbl.appendChild(el('div', 'th-dim', note));

    return put(el('div', 'th-set'), lbl, control);
  }

  function typeChips(d) {
    var box = el('div', 'th-bar');

    Object.keys(cat.types).forEach(function (key) {
      var type = cat.types[key];
      box.appendChild(button(type.name, 'deftype', type.id, true, 'th-sm' + (d.types.indexOf(type.id) >= 0 ? ' th-on' : '')));
    });

    return box;
  }

  function stepEditor(step, i, count) {
    var bait = baitOf(step.item);
    var allowed = isAllowed(step.item);
    var have = owned(step.item);
    var row = el('div', 'th-step' + (!allowed ? ' th-blocked' : have <= step.keep ? ' th-short' : ''));
    var label = put(el('div', ''), el('b', '', bait.name));
    var keep = put(el('div', 'th-bar'), el('span', 'th-dim', say('keep')), button('-', 'keepless', i, step.keep > 0, 'th-sm'),
      el('span', 'th-num', String(step.keep)), button('+', 'keepmore', i, step.keep < MAX_KEEP, 'th-sm'));
    var moves = put(el('div', 'th-bar'), button('\u2191', 'stepup', i, i > 0, 'th-sm'), button('\u2193', 'stepdown', i, i < count - 1, 'th-sm'),
      button(say('remove'), 'stepdel', i, true, 'th-sm th-danger'));

    if (!allowed) label.appendChild(put(el('div', 'th-red'), el('span', '', say('notAllowed') + ' '), button(say('allow'), 'allow', '1|' + step.item, true, 'th-sm')));
    else label.appendChild(el('div', 'th-dim', takenBy(step.item)));

    put(row, el('span', 'th-ord', String(i + 1)), icon(bait, 'th-i30'), label, el('span', 'th-dim', say('own', have)), keep, moves);

    return row;
  }

  function takenBy(itemId) {
    var names = Object.keys(cat.types).map(function (k) { return cat.types[k]; })
      .filter(function (t) { return (t.accepts || []).indexOf(itemId) >= 0; })
      .map(function (t) { return t.name; });

    return names.length ? say('takenBy', names.join(', ')) : '';
  }

  // A search box over every bait the game knows; owned and allowed baits come first.
  function addPicker() {
    var box = el('div', 'th-col');
    var search = el('input', 'th-inp');
    var list = el('div', 'th-col');

    search.setAttribute('data-field', 'addsearch');
    search.setAttribute('placeholder', say('search'));
    search.value = state.addSearch;
    list.id = 'th-addlist';
    put(box, search, list);
    ui.addList = list;
    fillAddList();

    return box;
  }

  function fillAddList() {
    var list = ui.addList;

    if (!list || !state.draft) return;

    var used = state.draft.steps.map(function (s) { return s.item; });
    var query = state.addSearch.trim().toLowerCase();
    var found = cat.baitList.filter(function (b) {
      return used.indexOf(b.id) < 0 && (query ? b.name.toLowerCase().indexOf(query) >= 0 : owned(b.id) > 0);
    }).sort(function (a, b) {
      return (owned(b.id) > 0) - (owned(a.id) > 0) || isAllowed(b.id) - isAllowed(a.id) || (a.name < b.name ? -1 : a.name > b.name ? 1 : 0);
    });

    list.textContent = '';
    found.slice(0, MAX_ADD).forEach(function (b) {
      list.appendChild(put(el('div', 'th-row'), icon(b, 'th-i22'), el('span', 'th-grow', b.name), el('span', 'th-dim', say('own', owned(b.id))),
        el('span', 'th-lock ' + (isAllowed(b.id) ? 'th-ok' : 'th-no'), say(isAllowed(b.id) ? 'allowed' : 'blocked')), button(say('add'), 'stepadd', b.id, true, 'th-sm')));
    });

    if (found.length > MAX_ADD) list.appendChild(el('div', 'th-note', say('more', found.length - MAX_ADD)));
  }

  function editDraft(change) {
    if (!state.draft) return;

    change(state.draft);
    render();
  }

  function markDirty() {
    var mark = ui.body.querySelector('.th-dirty');

    if (mark) mark.textContent = isDirty() ? say('unsaved') : '';
  }

  function moveStep(i, by) {
    editDraft(function (d) {
      var j = i + by;

      if (j < 0 || j >= d.steps.length) return;

      var step = d.steps[i];
      d.steps[i] = d.steps[j];
      d.steps[j] = step;
    });
  }

  function removeStep(i) {
    editDraft(function (d) { d.steps.splice(+i, 1); });
  }

  function changeKeep(i, by) {
    editDraft(function (d) {
      if (d.steps[i]) d.steps[i].keep = Math.max(0, Math.min(MAX_KEEP, d.steps[i].keep + by));
    });
  }

  // Adding a blocked bait also allows it: the player picked it by name, so it is wanted as bait.
  function addStep(itemId) {
    var id = +itemId;

    editDraft(function (d) {
      if (d.steps.every(function (s) { return s.item !== id; })) d.steps.push({ item: id, keep: 0 });
    });

    if (!isAllowed(id)) sendAction('allow|1|' + id);
  }

  function toggleDefaultType(typeId) {
    var id = +typeId;

    editDraft(function (d) {
      var at = d.types.indexOf(id);

      if (at >= 0) d.types.splice(at, 1);
      else d.types.push(id);
    });
  }

  function pickPreset(id) {
    state.presetId = id;
    state.draft = null;
    render();
  }

  function newPreset() {
    state.presetId = '';
    state.draftOf = '';
    state.draft = { id: '', name: say('newPreset'), steps: [], lastResort: true, source: 'home', spoilFirst: true, auto: 'off', types: [] };
    render();
  }

  function savePreset() {
    if (!state.draft) return;

    // A new preset gets its id from the plugin; the next view shows which one is new, and the editor moves onto it.
    if (!state.draft.id) state.pendingNewFrom = presets().map(function (p) { return p.id; });

    sendAction('savepreset|' + JSON.stringify(state.draft));
  }

  // ---------- Allowed bait tab ----------

  function renderAllowed() {
    var left = scroller('wlleft', 'th-col');
    var right = el('div', 'th-col');
    var search = el('input', 'th-inp');
    var grid = scroller('wlgrid');

    left.style.width = '250px';
    left.style.flex = 'none';
    put(left, el('div', 'th-sect', say('fAll')),
      segmentColumn([['all', 'fAll'], ['owned', 'fOwned'], ['allowed', 'fAllowed'], ['blocked', 'fBlocked']], state.filter, 'filter'),
      el('div', 'th-sect', say('kinds')),
      kindChips(),
      shownButton(say('allowShown'), '1', ''),
      shownButton(say('blockShown'), '0', 'th-danger'),
      el('div', 'th-note', say('allowedNote')));

    search.setAttribute('data-field', 'search');
    search.setAttribute('placeholder', say('search'));
    search.value = state.search;
    grid.style.flex = '1';
    put(right, search, grid);
    right.style.flex = '1';
    ui.grid = grid;
    put(ui.body, left, right);
    fillBaitGrid();
  }

  function segmentColumn(options, current, action) {
    var box = el('div', 'th-col');

    box.style.gap = '4px';
    options.forEach(function (option) {
      box.appendChild(button(say(option[1]), action, option[0], true, option[0] === current ? 'th-on' : ''));
    });

    return box;
  }

  function kindChips() {
    var box = el('div', 'th-bar');

    KINDS.forEach(function (kind) {
      box.appendChild(button(say(KIND_WORD[kind]), 'kind', kind, true, 'th-sm' + (state.kind === kind ? ' th-on' : '')));
    });

    return box;
  }

  function shownBaits() {
    var query = state.search.trim().toLowerCase();

    return cat.baitList.filter(function (b) {
      if (state.kind && b.kind !== state.kind) return false;
      if (query && b.name.toLowerCase().indexOf(query) < 0) return false;
      if (state.filter === 'owned') return owned(b.id) > 0;
      if (state.filter === 'allowed') return isAllowed(b.id);
      if (state.filter === 'blocked') return !isAllowed(b.id);

      return true;
    }).sort(function (a, b) {
      return (owned(b.id) > 0) - (owned(a.id) > 0) || (a.name < b.name ? -1 : a.name > b.name ? 1 : 0);
    });
  }

  function fillBaitGrid() {
    var grid = ui.grid;

    if (!grid) return;

    var shown = shownBaits();
    var cards = el('div', 'th-wl');

    grid.textContent = '';
    shown.slice(0, MAX_CARDS).forEach(function (b) { cards.appendChild(baitCard(b)); });
    grid.appendChild(cards);

    if (shown.length > MAX_CARDS) grid.appendChild(el('div', 'th-note', say('more', shown.length - MAX_CARDS)));

    // Allow all shown and Block all shown act on what the grid shows, including cards past the cut.
    var ids = shown.map(function (b) { return b.id; }).join(',');

    ui.body.querySelectorAll('[data-shown]').forEach(function (node) {
      node.setAttribute('data-args', node.getAttribute('data-shown') + '|' + ids);
      node.disabled = shown.length === 0;
    });
  }

  function shownButton(text, flag, extraClass) {
    var node = button(text, 'allow', flag + '|', true, extraClass);
    node.setAttribute('data-shown', flag);

    return node;
  }

  function baitCard(b) {
    var allowed = isAllowed(b.id);
    var card = el('div', 'th-bait' + (allowed ? '' : ' th-off'));
    var have = owned(b.id);
    var draws = (b.draws || []).slice(0, 3).map(function (id) { return preyOf(id).name; });
    var text = put(el('span', ''), el('b', '', b.name), el('small', '', say(KIND_WORD[b.kind] || 'kOther') + (have ? ' \u00b7 ' + say('own', have) : '')));

    if (draws.length) text.appendChild(el('small', '', say('draws', draws.join(', '))));

    card.setAttribute('data-act', 'allow');
    card.setAttribute('data-args', (allowed ? '0|' : '1|') + b.id);
    put(card, icon(b, 'th-i40'), text, el('span', 'th-lock ' + (allowed ? 'th-ok' : 'th-no'), say(allowed ? 'allowed' : 'blocked')));

    return card;
  }

  // ---------- Settings tab ----------

  function renderSettings() {
    var data = state.data;
    var well = scroller('settings');
    var sounds = data.sounds || {};
    var isElsewhere = !!data.soundsElsewhere;

    well.style.flex = '1';
    put(well, el('div', 'th-sect', say('sounds')), el('div', 'th-note', say('soundsNote')));

    if (isElsewhere) well.appendChild(el('div', 'th-banner', say('quietTraps')));

    SOUNDS.forEach(function (sound) {
      var seg = el('div', 'th-seg');

      MODES.forEach(function (mode) {
        seg.appendChild(button(say(mode[1]), 'sound', sound[0] + '|' + mode[0], !isElsewhere, sounds[sound[0]] === mode[0] ? 'th-on' : ''));
      });
      well.appendChild(setting(say(sound[1]), seg));
    });

    put(well, el('div', 'th-sect', say('tabSettings')),
      el('div', 'th-note', state.key ? say('keyOpen', state.key) : say('keyNone')),
      el('div', 'th-note', say('presetFile')));
    ui.body.appendChild(well);
  }

  // ---------- shared ----------

  function icon(item, sizeClass) {
    var box = el('div', 'th-icon ' + sizeClass);
    var letter = String(item.name || '?').charAt(0).toUpperCase();

    if (item.icon) {
      var img = el('img');
      img.setAttribute('data-letter', letter);
      img.src = item.icon;
      box.appendChild(img);
    } else {
      box.textContent = letter;
    }

    return box;
  }

  function open(data) {
    build();
    state.data = data || state.data;
    state.isOpen = true;
    state.isTyping = false;
    ui.status.textContent = '';
    ui.toast.style.display = 'none';
    ui.root.style.display = 'flex';
    render();
  }

  function setData(data) {
    state.data = data || state.data;
    render();
  }

  function close() {
    state.isOpen = false;
    state.armed = '';

    if (state.isTyping) send('focus|0');

    state.isTyping = false;

    if (ui) ui.root.style.display = 'none';
  }

  function status(text) {
    build();
    clearBusy();

    if (state.isOpen) {
      ui.status.textContent = text;

      return;
    }

    // An empty status only clears the line; as a toast it would be an empty box.
    if (!text) {
      ui.toast.style.display = 'none';

      return;
    }

    ui.toast.textContent = text;
    ui.toast.style.display = 'block';
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { ui.toast.style.display = 'none'; }, TOAST_MS);
  }

  // Whether a game with a home is loaded, and the key that opens the window ('' when none): the HUD button is only
  // shown in such a game, and both it and the window name the key.
  function live(isLive, key) {
    state.isLive = !!isLive;
    state.key = key || '';
    hudVersion++;

    if (ui && state.isOpen) renderKeys();

    patchPagesSafely();
  }

  // The number on the HUD button: traps with a catch to take or waiting for bait.
  function badge(count) {
    state.badge = Math.max(0, count | 0);
    hudVersion++;
    patchPagesSafely();
  }

  // ---------- HUD button ----------

  var HUD_CSS = [
    '.th-hud-btn{position:relative}',
    '.th-hud-btn .th-hud-badge{position:absolute;top:-6px;right:-6px;min-width:18px;height:18px;padding:0 5px;border-radius:9px;background:#e53935;color:#fff;font-size:11px;font-weight:700;line-height:18px;text-align:center;pointer-events:none;box-shadow:0 1px 4px rgba(0,0,0,.6)}'
  ].join('\n');

  // The HUD is a same-origin iframe, so its document can be reached from here; polled because the game creates and
  // drops that iframe whenever it likes.
  function patchPages() {
    var frames = document.querySelectorAll('iframe');

    for (var i = 0; i < frames.length; i++) {
      var src = frames[i].src || '';

      if (!/CoreUI1\.html/.test(src)) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc) {
        if (!hasReportedPageAccess) send('diag|cannot reach the HUD document (root ' + location.protocol + ', iframe ' + src + ')');
        hasReportedPageAccess = true;
        continue;
      }

      placeHudButton(doc);
    }
  }

  function patchPagesSafely() {
    try {
      patchPages();
    } catch (e) {}
  }

  // First in the tool column: the column is anchored at the bottom, so the first child sits on top and the game's own
  // buttons do not move. The column hides itself in build mode, and the button with it.
  function placeHudButton(doc) {
    var column = doc.querySelector('.vertical-tools');
    var existing = doc.querySelector('.' + HUD_CLASS);

    if (!column || !state.isLive) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.parentNode === column && existing.getAttribute('data-version') === String(hudVersion)) return;
    if (existing) existing.remove();

    addHudStyle(doc);
    column.insertBefore(hudButton(doc), column.firstChild);

    if (!hasReportedHudPlaced) send('diag|HUD button placed in the tool column');
    hasReportedHudPlaced = true;
  }

  function addHudStyle(doc) {
    if (doc.getElementById(HUD_STYLE_ID)) return;

    var style = doc.createElement('style');
    style.id = HUD_STYLE_ID;
    style.textContent = HUD_CSS;
    (doc.head || doc.body).appendChild(style);
  }

  // The same markup as the game's bag button: its classes, an svg icon, and the hover tip its stylesheet shows. The
  // HUD page watches its body for new buttons and tells the game where they are, so a click on this one reaches it.
  function hudButton(doc) {
    var node = doc.createElement('button');
    node.className = 'ui-btn icon-sq interactive-btn has-entry-tip ' + HUD_CLASS;
    node.setAttribute('data-interactive', '');
    node.setAttribute('data-version', String(hudVersion));
    node.appendChild(cage(doc));

    if (state.badge > 0) {
      var count = doc.createElement('span');
      count.className = 'th-hud-badge';
      count.textContent = state.badge > 99 ? '99+' : String(state.badge);
      node.appendChild(count);
    }

    node.appendChild(entryTip(doc));
    node.addEventListener('click', function (e) {
      e.stopPropagation();
      send('toggle');
    });

    return node;
  }

  function cage(doc) {
    var svg = doc.createElementNS(SVG_NS, 'svg');
    var path = doc.createElementNS(SVG_NS, 'path');

    svg.setAttribute('class', 'icon-svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    path.setAttribute('d', CAGE_PATH);
    svg.appendChild(path);

    return svg;
  }

  function entryTip(doc) {
    var tip = doc.createElement('div');
    var title = doc.createElement('div');
    var body = doc.createElement('div');

    tip.className = 'entry-tip';
    title.className = 'entry-tip-title';
    title.textContent = 'Trap Hub';
    body.className = 'entry-tip-body';
    body.textContent = say('tip');

    if (state.key) {
      var key = doc.createElement('span');
      key.className = 'entry-tip-key';
      key.textContent = state.key;
      title.appendChild(key);
    }

    tip.appendChild(title);
    tip.appendChild(body);

    return tip;
  }

  setInterval(patchPagesSafely, 400);

  window.__sltraphub = { open: open, setData: setData, close: close, status: status, live: live, catalog: catalog, badge: badge };
  send('ready');
})();
