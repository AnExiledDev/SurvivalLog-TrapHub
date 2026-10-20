// Trap Hub's web half. Runs in Root.html, the document every game page iframe sits in. Draws the Trap Hub window over
// all of them, styled like the game's own panels, and puts a trap button with a count badge in the HUD's tool column
// (bottom left, above the phone and bag buttons), built like the game's own buttons there. Talks to the plugin through
// vuplex.postMessage("slmod|traphub|<verb>") and is driven by window.__sltraphub.
(function () {
  if (window.__sltraphub) return;

  var PREFIX = 'slmod|traphub|';
  var HUD_CLASS = 'th-hud-btn';
  // Trap Hub's place in the HUD's tool column when Exiled's Toolkit lays it out in columns of four.
  var DOCK_ORDER = '10';
  var HUD_STYLE_ID = 'th-hud-style';
  var BUSY_TIMEOUT_MS = 5000;
  var TOAST_MS = 3500;
  var MAX_CARDS = 240;
  var MAX_ADD = 30;
  var MAX_KEEP = 99;
  var MAX_NEEDS = 30;
  var MAX_STOCK = 30;
  var SVG_NS = 'http://www.w3.org/2000/svg';
  // A cage: a top bar, three bars and a floor, on the 24-unit grid the game's HUD icons use.
  var CAGE_PATH = 'M3 3h18v3H3V3zm2 4h2v10H5V7zm5 0h2v10h-2V7zm5 0h2v10h-2V7zm4 0h2v10h-2V7zM3 18h18v3H3v-3z';
  var TABS = ['traps', 'presets', 'allowed', 'settings'];
  var TAB_WORD = { traps: 'tabTraps', presets: 'tabPresets', allowed: 'tabAllowed', settings: 'tabSettings' };
  var STATE_WORD = { armed: 'stArmed', prey: 'stPrey', nobait: 'stNoBait', halted: 'stHalted' };
  var STATE_TAG = { armed: 'th-b-blue', prey: 'th-b-green', nobait: 'th-b-red', halted: 'th-b-dim' };
  var STATE_ORDER = ['prey', 'nobait', 'halted', 'armed'];
  var SHOWS = [['all', 'fAll'], ['prey', 'fPrey'], ['nobait', 'fNoBait'], ['worn', 'fBreaks'], ['halted', 'stHalted']];
  var GROUPS = [['floor', 'gFloor'], ['room', 'gRoom'], ['type', 'gType'], ['state', 'gState']];
  var BAIT_SHOWS = [['all', 'fAll'], ['owned', 'fOwned'], ['allowed', 'fAllowed'], ['blocked', 'fBlocked'], ['inpreset', 'fInPreset']];
  var KINDS = ['universal', 'preference', 'specialized', 'balanced', 'other'];
  var KIND_WORD = { universal: 'kUniversal', preference: 'kPreference', specialized: 'kSpecialized', balanced: 'kBalanced', other: 'kOther' };
  var SOUNDS = [['cage', 'sCage'], ['net', 'sNet'], ['other', 'sOther']];
  var MODES = [['Normal', 'mNormal'], ['Once', 'mOnce'], ['Muted', 'mMuted']];
  var AUTOS = [['off', 'autoOff'], ['nearby', 'autoNearby'], ['always', 'autoAlways']];
  // Presses the page answers itself; everything else goes to the plugin.
  var LOCAL = {
    tab: pickTab, pick: pickTrap, 'goto': gotoTrap, check: checkTrap, clearchecks: clearChecks,
    show: function (v) { state.show = v; render(); },
    ftype: function (v) { state.ftype = state.ftype === +v ? 0 : +v; render(); },
    froom: function (v) { state.froom = state.froom === v ? '' : v; render(); },
    group: function (v) { state.group = v; render(); },
    bait: pickBait, baitgo: function (v) { state.tab = 'allowed'; state.baitSel = +v; render(); },
    filter: function (v) { state.baitShow = v; render(); },
    kind: function (v) { state.kind = state.kind === v ? '' : v; render(); },
    preset: pickPreset, presetgo: function (v) { state.tab = 'presets'; pickPreset(v); }, newpreset: newPreset,
    stepup: function (i) { moveStep(+i, -1); }, stepdown: function (i) { moveStep(+i, 1); }, stepdel: removeStep,
    keepless: function (i) { changeKeep(+i, -1); }, keepmore: function (i) { changeKeep(+i, 1); }, stepadd: addStep,
    lastresort: function () { editDraft(function (d) { d.lastResort = !d.lastResort; }); },
    spoil: function () { editDraft(function (d) { d.spoilFirst = !d.spoilFirst; }); },
    source: function (v) { editDraft(function (d) { d.source = v; }); },
    autoset: function (v) { editDraft(function (d) { d.auto = v; }); },
    deftype: toggleDefaultType
  };
  // Presses that ask before they act: the first press arms the button, the second sends it.
  var CONFIRMED = { dismantle: true, deletepreset: true };
  // The plugin hands over the words in the game's language before every call (Texts.cs); English stands in until then.
  var ENGLISH = {
    tabTraps: 'Traps', tabPresets: 'Bait presets', tabAllowed: 'Allowed bait', tabSettings: 'Settings', nTrap: '1 trap', nTraps: '{0} traps',
    cPrey: '{0} with prey', cNoBait: '{0} without bait', cWorn: '{0} about to break', cHalted: '{0} halted', takeAll: 'Take all ({0})',
    takeFloor: 'Take this floor ({0})', rebaitAll: 'Re-bait all ({0})', floor: 'Floor {0}', here: 'You are here', stArmed: 'Armed', stPrey: 'Prey',
    stNoBait: 'No bait', stHalted: 'Halted', caught: 'Caught: {0}', next: 'Re-bait uses: {0}', nextNone: 'No allowed bait left for its preset',
    catches: '{0} catches left', breaks: 'Breaks on its next catch', store: 'Store: {0}', queued: 'Walk queued', take: 'Take', rebait: 'Re-bait',
    dismantle: 'Dismantle', confirm: 'Sure?', preset: 'Bait preset', typeDefault: 'Type default ({0})', useDefault: 'Type default',
    ownPick: 'Picked for it', none: 'None', empty: 'No traps placed at home yet.', noMatch: 'No traps match these filters.', keepN: 'keep {0}',
    notTaken: 'This trap does not take it', oddsNow: 'Catch odds now', otherPrey: 'Other prey', nothing: 'Nothing', wear: 'Wear', needsYou: 'Needs you',
    allFine: 'All your traps are armed and baited.', pickTrap: 'Pick a trap to see its bait and catch odds.', show: 'Show', fAll: 'All',
    fPrey: 'With prey', fNoBait: 'Without bait', fBreaks: 'About to break', trapType: 'Trap type', rooms: 'Rooms', groupBy: 'Group by', gFloor: 'Floor',
    gRoom: 'Room', gType: 'Type', gState: 'State', baitOwn: 'Bait you own', selected: '{0} selected', clear: 'Clear', assignTo: 'Set bait preset:',
    noSaveYet: 'Save the game once to pick presets per trap.', newPreset: 'New preset', name: 'Name', steps: 'Bait order',
    stepsNote: 'Trap Hub uses the first one you have more of than you keep.', noSteps: 'No baits yet. Add some below.', keep: 'Keep', own: 'Own {0}',
    notAllowed: 'Not allowed, skipped', lastResort: 'Then any allowed universal bait', rules: 'Rules', source: 'Take bait from', srcBag: 'Bag only',
    srcHome: 'Bag, then home storage', spoilFirst: 'Use the food closest to spoiling first', auto: 'Auto re-bait', autoOff: 'Off',
    autoNearby: 'On my floor', autoAlways: 'Always', autoNote: 'Puts bait in an empty trap by itself, from this preset, without a walk.',
    defaultFor: 'Default for', save: 'Save', delete: 'Delete', addBait: 'Add a bait', search: 'Search', usedBy: 'Traps using it',
    usedNone: 'No trap uses this preset yet.', more: '{0} more. Search to narrow the list.', unsaved: 'Unsaved changes',
    allowedNote: 'Trap Hub only puts bait you allow here in your traps. Everything starts blocked.', fOwned: 'I own', fAllowed: 'Allowed',
    fBlocked: 'Blocked', fInPreset: 'In a preset', kinds: 'Bait kind', kUniversal: 'Universal', kPreference: 'Preference', kSpecialized: 'Specialized',
    kBalanced: 'Balanced', kOther: 'Other', quick: 'Quick picks', allowPresets: 'Allow everything in my presets',
    allowUniversal: 'Allow universal bait I own', allowShown: 'Allow all shown', blockShown: 'Block all shown', allowed: 'Allowed', blocked: 'Blocked',
    allow: 'Allow', block: 'Block', sOwned: 'Owned', sNotOwned: 'Not owned', noBaitMatch: 'No bait matches these filters.',
    pickBait: 'Pick a bait to see where it is used.', inPresets: 'In your presets', presetStep: '{0}, step {1}', skipped: 'Skipped while blocked',
    notInPresets: 'Not in any preset.', drawsIn: 'Draws in', takenByHead: 'Taken by', sounds: 'Catch sounds',
    soundsNote: 'The sound a trap plays while it holds a catch. Chime once plays it for a few seconds, then goes quiet.', sCage: 'Live Capture Cage',
    sNet: 'Simple Bird Net', sOther: 'Mousetrap, Iron Animal Trap, Auto Trap Cage', mNormal: 'Normal', mOnce: 'Chime once', mMuted: 'Muted',
    quietTraps: 'Quiet Traps is installed, so it handles catch sounds.', keyOpen: '{0} opens and closes this window.',
    keyNone: 'No key is set; use the trap button on the left of the screen.',
    presetFile: 'Presets and allowed bait are kept in BepInEx/config/trap-hub.json, shared by every save.', hotkey: 'Hotkey', escClose: 'Esc to close',
    tip: 'Your traps: catches, bait and wear'
  };

  var state = {
    data: { ready: false, note: '' }, isOpen: false, isLive: false, key: '', badge: 0, tab: 'traps', sel: '', checks: {},
    show: 'all', ftype: 0, froom: '', group: 'floor', baitSel: 0, baitShow: 'owned', kind: '', search: '',
    presetId: '', draft: null, draftOf: '', pendingNewFrom: null, addSearch: '', armed: '', isTyping: false, isStale: false
  };
  var cat = { types: {}, baits: {}, prey: {}, baitList: [], typeList: [] };
  // Looked up for every card on every refresh, so they are maps built once per view rather than list scans.
  var allowedSet = {};
  var presetUses = {};
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

  // A phrase with its number picked out, as in "3 with prey": the number is bold and coloured, the rest is plain.
  function sayCount(key, count, colorClass) {
    var parts = say(key).split('{0}');

    return put(el('span'), text(parts[0]), el('b', colorClass, String(count)), text(parts.slice(1).join('{0}')));
  }

  function trapCount(count) {
    return count === 1 ? say('nTrap') : say('nTraps', count);
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

  function el(tag, className, content) {
    var node = document.createElement(tag);

    if (className) node.className = className;
    if (content != null) node.textContent = content;

    return node;
  }

  function text(content) {
    return content ? document.createTextNode(content) : null;
  }

  function put(parent) {
    for (var i = 1; i < arguments.length; i++) {
      if (arguments[i]) parent.appendChild(arguments[i]);
    }

    return parent;
  }

  function setClass(node, name, isOn) {
    var names = String(node.className || '').split(' ').filter(function (n) { return n !== '' && n !== name; });

    if (isOn) names.push(name);

    node.className = names.join(' ');
  }

  function act(node, action, args) {
    node.setAttribute('data-act', action);

    if (args != null) node.setAttribute('data-args', String(args));

    return node;
  }

  function button(content, action, args, isEnabled, extraClass) {
    return plainButton('th-btn' + (extraClass ? ' ' + extraClass : ''), content, action, args, isEnabled);
  }

  // A button without the panel button look: the small arrows, crosses and steppers.
  function plainButton(className, content, action, args, isEnabled) {
    var node = act(el('button', className, content), action, args);

    if (isEnabled === false) node.disabled = true;

    return node;
  }

  function chip(content, action, args, isOn, count, iconItem) {
    var node = act(el('button', 'th-chip' + (isOn ? ' th-on' : '') + (iconItem ? ' th-ic' : '')), action, args);

    put(node, iconItem ? icon(iconItem, 'th-i18') : null, content ? el('span', '', content) : null, count != null ? el('span', 'th-n', String(count)) : null);

    return node;
  }

  // A row of choices where one is on, like the game's tab strips; each sends its value after the given prefix.
  function segment(choices, current, action, isEnabled, argsPrefix) {
    var seg = el('div', 'th-seg');

    choices.forEach(function (c) {
      seg.appendChild(plainButton('th-sg' + (c[0] === current ? ' th-on' : ''), c[1], action, (argsPrefix || '') + c[0], isEnabled !== false));
    });

    return seg;
  }

  function switchButton(isOn, action) {
    return act(el('button', 'th-tog' + (isOn ? ' th-on' : '')), action);
  }

  function tag(content, colorClass) {
    return el('span', 'th-badge' + (colorClass ? ' ' + colorClass : ''), content);
  }

  function section(title, right) {
    return put(el('div', 'th-sect', title), right);
  }

  // A sidebar heading: drawn like a section, kept apart so the list's group headings can be told from it.
  function caption(title) {
    return el('div', 'th-cap', title);
  }

  function setRow(label, note, control) {
    return put(el('div', 'th-setrow'), put(el('div', 'th-lbl'), el('b', '', label), note ? el('small', '', note) : null), control);
  }

  function disableAll(scope, action) {
    scope.querySelectorAll('[data-act]').forEach(function (node) {
      if (node.getAttribute('data-act') === action) node.disabled = true;
    });
  }

  var CSS = [
    '#th-root{position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(0,0,0,.45);font-family:"Noto Sans SC","Microsoft YaHei","PingFang SC",sans-serif;color:#eee;user-select:none;font-size:14px}',
    '#th-root *{box-sizing:border-box}',
    '#th-root button{font:inherit}',
    '.th-plate{width:min(1600px,96vw);height:min(940px,92vh);display:flex;flex-direction:column;gap:12px;padding:18px 22px;border-radius:10px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(30,30,33,.95),rgba(18,18,20,.97));box-shadow:0 10px 40px rgba(0,0,0,.6)}',
    '.th-head{display:flex;align-items:center;gap:14px}',
    '.th-title{font-size:22px;font-weight:700;letter-spacing:1px;text-shadow:0 2px 6px rgba(0,0,0,.6)}',
    '.th-sum{flex:1;font-size:14px;color:rgba(238,238,238,.6);display:flex;gap:6px;flex-wrap:wrap;align-items:baseline}',
    '.th-sum b{font-weight:700;color:#eee}.th-sum .th-dot{color:rgba(238,238,238,.3)}',
    '.th-cg{color:#aed581!important}.th-cr{color:#ff8a80!important}.th-ca{color:#EBA308!important}.th-cb{color:#90caf9!important}',
    '.th-tabs{display:flex;gap:4px;border-bottom:1px solid rgba(255,255,255,.14)}',
    '.th-tab{font-size:15px;color:#b8b8b8;padding:8px 18px 9px;border:1px solid transparent;border-bottom:none;border-radius:7px 7px 0 0;background:none;position:relative;top:1px;display:flex;gap:8px;align-items:center;cursor:pointer}',
    '.th-tab:hover{color:#fff}.th-tab.th-on{color:#EBA308;background:rgba(10,10,12,.72);border-color:rgba(255,255,255,.14);font-weight:600}',
    '.th-tab .th-n{font-size:11px;padding:1px 6px;border-radius:7px;background:#D32F2F;color:#fff;font-weight:700}',
    '.th-tab .th-n.th-nn{background:rgba(255,255,255,.12);color:#ccc;font-weight:600}',
    '.th-body{flex:1;min-height:0;display:flex;gap:12px}',
    '.th-well{background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);border-radius:8px;min-height:0;overflow-y:auto}',
    '.th-well::-webkit-scrollbar{width:6px}.th-well::-webkit-scrollbar-thumb{background:rgba(235,163,8,.35);border-radius:3px}',
    '.th-col{display:flex;flex-direction:column;gap:10px;min-height:0;min-width:0}',
    '.th-sect,.th-cap{font-size:12px;color:#EBA308;letter-spacing:2px;text-transform:uppercase;display:flex;align-items:center;gap:10px;padding:12px 12px 6px}',
    '.th-sect::after,.th-cap::after{content:"";flex:1;height:1px;background:linear-gradient(90deg,rgba(235,163,8,.25),transparent)}',
    '.th-cap{padding:12px 2px 6px}',
    '.th-sect .th-r{order:2;text-transform:none;letter-spacing:0;color:#888;font-size:12px}',
    '.th-sect .th-badge{letter-spacing:0;text-transform:none}',
    '.th-side{width:250px;flex:none;padding:0 10px 12px}',
    '.th-chips{display:flex;gap:6px;flex-wrap:wrap;padding:2px 2px 4px}',
    '.th-chip{display:inline-flex;align-items:center;gap:6px;padding:4px 9px;border-radius:7px;background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);color:#eee;font-size:13px;cursor:pointer;white-space:nowrap}',
    '.th-chip.th-ic{padding-left:4px}.th-chip:hover:not(:disabled){border-color:rgba(235,163,8,.6)}.th-chip:disabled{opacity:.4;cursor:default}',
    '.th-chip.th-on{border-color:#EBA308;color:#EBA308;background:rgba(235,163,8,.12)}',
    '.th-chip .th-n{color:#EBA308;font-weight:600}.th-chip.th-zero{opacity:.45}',
    '.th-seg{display:inline-flex;border:1px solid rgba(255,255,255,.14);border-radius:7px;overflow:hidden;flex:none}',
    '.th-sg{font-size:13px;padding:5px 11px;color:#b8b8b8;background:none;border:none;border-right:1px solid rgba(255,255,255,.08);cursor:pointer;white-space:nowrap}',
    '.th-sg:last-child{border-right:none}.th-sg:hover:not(:disabled){color:#fff}.th-sg.th-on{background:rgba(235,163,8,.18);color:#EBA308}.th-sg:disabled{opacity:.4;cursor:default}',
    '.th-side .th-seg{display:flex}.th-side .th-sg{flex:1;padding:5px 4px}',
    '.th-mid{flex:1;min-width:0;display:flex;flex-direction:column;gap:10px}',
    '.th-acts{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:8px 10px;border-radius:8px;border:1px solid rgba(255,255,255,.14);background:rgba(10,10,12,.72)}',
    '.th-acts.th-bulk{border-color:rgba(235,163,8,.6);background:rgba(235,163,8,.07)}.th-acts .th-grow{flex:1}',
    '.th-cards{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px;padding:0 8px 8px}',
    '.th-card{position:relative;display:flex;gap:12px;align-items:center;padding:10px 10px 10px 12px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent;cursor:pointer;min-height:96px}',
    '.th-card:hover{background:rgba(255,255,255,.07)}.th-card.th-hot{border-color:rgba(229,57,53,.5)}.th-card.th-ready{border-color:rgba(124,179,66,.5)}',
    '.th-card.th-sel{border-color:rgba(235,163,8,.8);background:rgba(235,163,8,.08)}',
    '.th-chk{position:absolute;left:4px;top:4px;width:18px;height:18px;padding:0;border-radius:4px;border:1px solid rgba(255,255,255,.35);background:rgba(0,0,0,.6);color:#111;font-size:12px;line-height:16px;font-weight:700;cursor:pointer;opacity:0;z-index:1}',
    '.th-card:hover .th-chk,.th-checking .th-chk{opacity:1}.th-chk.th-on{opacity:1;background:#EBA308;border-color:#EBA308}',
    '.th-cmain{flex:1;min-width:0;display:flex;flex-direction:column;gap:4px}',
    '.th-nm{font-size:15px;font-weight:600;display:flex;flex-wrap:wrap;gap:4px 6px;align-items:center;white-space:nowrap}',
    '.th-ln{font-size:13px;color:#b8b8b8;display:flex;gap:8px;align-items:center;white-space:nowrap;overflow:hidden;min-height:18px}',
    '.th-ln span{overflow:hidden;text-overflow:ellipsis}',
    '.th-sub{font-size:12px;color:#888;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-card .th-track{width:80px}',
    '.th-crt{display:flex;flex-direction:column;align-items:flex-end;gap:6px;flex:none}',
    '.th-pct{font-size:20px;font-weight:700;color:#EBA308;font-variant-numeric:tabular-nums;line-height:1}',
    '.th-badge{flex:none;font-size:11px;padding:2px 7px;border-radius:5px;background:rgba(255,255,255,.1);color:rgba(238,238,238,.85);white-space:nowrap;font-weight:400}',
    '.th-b-red{background:rgba(229,57,53,.28);color:#ff8a80}.th-b-amber{background:rgba(255,193,7,.22);color:#EBA308}.th-b-green{background:rgba(124,179,66,.24);color:#aed581}',
    '.th-b-blue{background:rgba(100,181,246,.2);color:#90caf9}.th-b-dim{background:rgba(255,255,255,.06);color:#888}',
    '.th-track{width:110px;height:8px;border-radius:4px;background:rgba(255,255,255,.1);overflow:hidden;flex:none}',
    '.th-fill{display:block;height:100%;border-radius:4px;background:#7cb342}.th-fill.th-amber{background:#EBA308}.th-fill.th-red{background:#e53935}.th-fill.th-grey{background:#666}',
    '.th-detail{width:440px;flex:none;padding:14px;display:flex;flex-direction:column;gap:12px}',
    '.th-detail .th-sect{padding:0 0 2px}',
    '.th-dhead{display:flex;gap:12px;align-items:center}.th-dname{flex:1;min-width:0}',
    '.th-big{font-size:20px;font-weight:700}.th-dim{color:#888;font-size:12px}',
    '.th-bar{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.th-bar .th-grow{flex:1}',
    '.th-box{border:1px solid rgba(255,255,255,.08);border-radius:8px;padding:10px 12px;background:rgba(255,255,255,.025);display:flex;flex-direction:column;gap:6px}',
    '.th-box h4{margin:0 0 2px;display:flex;align-items:center;gap:8px;font-size:12px;color:#EBA308;letter-spacing:2px;text-transform:uppercase;font-weight:600}',
    '.th-box h4 .th-hnum{margin-left:auto;letter-spacing:0;color:#eee;font-size:14px;text-transform:none}',
    '.th-nrow{display:flex;align-items:center;gap:10px;font-size:13px;padding:5px 8px;border-radius:6px}',
    '.th-nrow.th-next{background:rgba(124,179,66,.13)}.th-nrow.th-out{opacity:.45}.th-nrow .th-grow{flex:1;min-width:0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-nrow.th-link{cursor:pointer}.th-nrow.th-link:hover{background:rgba(255,255,255,.06)}',
    '.th-ordn{width:16px;text-align:center;color:#888;font-size:12px}',
    '.th-orow{display:flex;align-items:center;gap:10px;font-size:13px;padding:3px 8px}',
    '.th-orow .th-grow{flex:1;min-width:0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.th-orow .th-track{width:120px}',
    '.th-orow .th-num{width:42px;text-align:right}.th-orow.th-nil{color:#888}.th-orow.th-nil .th-num{color:#888}',
    '.th-ispace{width:22px;flex:none}',
    '.th-row{display:flex;align-items:center;gap:10px;padding:8px 10px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent;font-size:13px;cursor:pointer}',
    '.th-row:hover{background:rgba(255,255,255,.07)}.th-row .th-grow{flex:1;min-width:0}',
    '.th-rows{display:flex;flex-direction:column;gap:6px}.th-rows.th-tight{gap:2px}',
    '.th-num{font-weight:600;color:#EBA308;font-variant-numeric:tabular-nums}',
    '.th-red{color:#ff8a80}.th-green{color:#aed581}',
    '.th-btn{font-size:13px;color:#dde2f0;padding:7px 12px;border-radius:7px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(58,58,60,.9),rgba(38,38,40,.92));cursor:pointer;white-space:nowrap}',
    '.th-btn:hover:not(:disabled){background:linear-gradient(180deg,rgba(72,66,48,.92),rgba(50,46,32,.95));border-color:rgba(255,193,7,.75)}',
    '.th-btn.th-hot{border-color:rgba(235,163,8,.75);color:#EBA308}',
    '.th-btn.th-danger:hover:not(:disabled),.th-btn.th-armed{background:linear-gradient(180deg,rgba(84,44,42,.92),rgba(58,30,28,.95));border-color:rgba(229,57,53,.75);color:#ff8a80}',
    '.th-btn.th-sm{font-size:12px;padding:4px 9px}.th-btn:disabled{opacity:.4;cursor:default}',
    '.th-btn.th-x{width:32px;height:32px;padding:0;font-size:18px;line-height:30px;text-align:center}',
    '.th-btn.th-ok{border-color:rgba(124,179,66,.55);color:#aed581}.th-btn.th-no{border-color:rgba(229,57,53,.45);color:#ff8a80}',
    '.th-ib{width:20px;height:20px;padding:0;border-radius:5px;border:1px solid rgba(255,255,255,.1);background:rgba(255,255,255,.05);color:#b8b8b8;font-size:11px;line-height:18px;text-align:center;cursor:pointer}',
    '.th-ib:hover:not(:disabled){border-color:#EBA308;color:#EBA308}.th-ib:disabled{opacity:.2;cursor:default}.th-ib.th-del:hover{border-color:#e53935;color:#ff8a80}',
    '.th-tog{width:40px;height:22px;padding:0;border-radius:11px;background:rgba(255,255,255,.12);border:1px solid rgba(255,255,255,.14);position:relative;flex:none;cursor:pointer}',
    '.th-tog::after{content:"";position:absolute;top:3px;left:3px;width:14px;height:14px;border-radius:50%;background:#999;transition:left .15s}',
    '.th-tog.th-on{background:rgba(235,163,8,.35);border-color:#EBA308}.th-tog.th-on::after{left:21px;background:#EBA308}',
    '.th-stepper{display:inline-flex;align-items:center;border:1px solid rgba(255,255,255,.14);border-radius:7px;overflow:hidden;font-size:13px;flex:none}',
    '.th-stp{padding:1px 8px;color:#999;background:rgba(255,255,255,.04);border:none;cursor:pointer}.th-stp:hover:not(:disabled){color:#EBA308}.th-stp:disabled{opacity:.3;cursor:default}',
    '.th-stepper b{padding:1px 6px;color:#EBA308;font-weight:600;min-width:28px;text-align:center}',
    '.th-setrow{display:flex;align-items:center;gap:14px;padding:10px 14px;border-radius:7px;background:rgba(255,255,255,.04)}',
    '.th-lbl{flex:1;min-width:0}.th-lbl b{display:block;font-size:14px;font-weight:600}.th-lbl small{display:block;font-size:12px;color:#888;margin-top:2px}',
    '.th-note{font-size:12px;color:#888;padding:4px 12px}',
    '.th-banner{padding:8px 12px;border-radius:7px;border:1px solid rgba(100,181,246,.6);background:rgba(100,181,246,.1);color:#bbdefb;font-size:13px}',
    '.th-plist{width:270px;flex:none;display:flex;flex-direction:column;gap:10px}.th-plist .th-well{flex:1}',
    '.th-pitem{display:flex;align-items:center;gap:8px;padding:10px 12px;margin:6px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent;cursor:pointer}',
    '.th-pitem:hover{background:rgba(255,255,255,.07)}.th-pitem.th-sel{border-color:rgba(235,163,8,.75);background:rgba(235,163,8,.08)}',
    '.th-pitem .th-lbl b{font-size:15px}.th-pitem small{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-icons{display:flex;gap:2px;flex:none}',
    '.th-editor{flex:1;min-width:0;padding:12px 14px 16px;display:flex;flex-direction:column;gap:8px}',
    '.th-editor .th-sect{padding:12px 2px 2px}',
    '.th-inp{font:inherit;font-size:15px;font-weight:600;color:#fff;background:rgba(0,0,0,.5);border:1px solid rgba(255,255,255,.14);border-radius:7px;padding:6px 10px;user-select:text}',
    '.th-inp:focus{outline:none;border-color:rgba(235,163,8,.75)}.th-inp.th-small{font-size:13px;font-weight:400;padding:6px 10px;width:100%}',
    '.th-step{display:grid;grid-template-columns:20px 30px minmax(0,1fr) auto;gap:9px;align-items:center;padding:7px 9px;border-radius:7px;background:rgba(255,255,255,.04);border-left:2px solid transparent}',
    '.th-step.th-short{border-left-color:#e53935}.th-step.th-go{border-left-color:#7cb342}.th-step.th-bad{background:rgba(229,57,53,.06)}',
    '.th-ord{width:20px;height:20px;border-radius:50%;display:grid;place-items:center;background:rgba(235,163,8,.18);color:#EBA308;font-weight:700;font-size:11px}',
    '.th-sinfo{min-width:0;display:flex;flex-direction:column;gap:3px}.th-sinfo b{font-size:13px;font-weight:600;display:flex;gap:6px;align-items:center;white-space:nowrap;overflow:hidden}',
    '.th-sinfo small{display:block;font-size:11px;color:#888;line-height:1.35}',
    '.th-sctl{display:flex;align-items:center;gap:8px}.th-moves{display:flex;gap:3px}',
    '.th-dash{font-size:13px;color:#b8b8b8;padding:6px;border-radius:7px;border:1px dashed rgba(255,255,255,.18);background:none;text-align:center}',
    '.th-picker{display:flex;flex-direction:column;gap:3px;padding:6px;border-radius:7px;background:rgba(0,0,0,.35);border:1px solid rgba(235,163,8,.4)}',
    '.th-prow{display:flex;align-items:center;gap:8px;font-size:12px;padding:4px;border-radius:5px;cursor:pointer}.th-prow:hover{background:rgba(235,163,8,.12)}.th-prow .th-grow{flex:1;min-width:0}',
    '.th-uses{width:320px;flex:none;padding:0 12px 14px;display:flex;flex-direction:column;gap:6px}',
    '.th-uses .th-sect{padding:12px 0 6px}',
    '.th-settings{flex:1;padding:0 16px 16px;display:flex;flex-direction:column;gap:8px}.th-settings .th-sect{padding:14px 0 4px}',
    '.th-wl{display:grid;grid-template-columns:repeat(auto-fill,minmax(270px,1fr));gap:6px;padding:0 8px 8px}',
    '.th-bait{display:flex;gap:10px;align-items:center;padding:8px 10px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent;cursor:pointer;min-width:0}',
    '.th-bait:hover{background:rgba(255,255,255,.07)}.th-bait.th-sel{border-color:rgba(235,163,8,.8);background:rgba(235,163,8,.08)}',
    '.th-bait.th-off>.th-icon,.th-bait.th-off .th-binfo{opacity:.55}',
    '.th-binfo{flex:1;min-width:0;display:flex;flex-direction:column;gap:3px}',
    '.th-binfo b{font-size:13px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.th-binfo small{font-size:11px;color:#888;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-bait .th-btn{min-width:74px}',
    '.th-qcol{display:flex;flex-direction:column;gap:6px;padding:2px}.th-qcol .th-btn{text-align:left;white-space:normal}',
    '.th-stock{display:flex;flex-direction:column;gap:1px}',
    '.th-srow{display:flex;align-items:center;gap:8px;font-size:12px;padding:3px 4px;border-radius:5px;cursor:pointer}.th-srow:hover{background:rgba(255,255,255,.06)}',
    '.th-srow .th-grow{flex:1;min-width:0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.th-srow.th-out{opacity:.45}.th-srow .th-num{font-size:12px}',
    '.th-pchips{display:flex;flex-wrap:wrap;gap:6px}',
    '.th-pchip{display:inline-flex;align-items:center;gap:6px;padding:3px 9px 3px 3px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid rgba(255,255,255,.1);font-size:12px}',
    '.th-icon{flex:none;display:flex;align-items:center;justify-content:center;border-radius:6px;background:rgba(255,255,255,.05);overflow:hidden;color:rgba(238,238,238,.7);font-weight:600}',
    '.th-icon img{width:100%;height:100%;object-fit:contain}',
    '.th-i56{width:56px;height:56px;font-size:20px}.th-i44{width:44px;height:44px;font-size:17px}.th-i40{width:40px;height:40px;font-size:15px}.th-i30{width:30px;height:30px;font-size:12px}.th-i22{width:22px;height:22px;font-size:11px}.th-i18{width:18px;height:18px;font-size:10px;border-radius:4px}',
    '.th-empty{padding:24px;color:#888;font-size:14px;text-align:center}',
    '.th-lockmsg{padding:10px 14px;border-radius:7px;border:1px solid rgba(255,193,7,.75);background:rgba(255,193,7,.12);color:#EBA308;font-size:14px}',
    '.th-foot{display:flex;gap:12px;font-size:13px;color:rgba(238,238,238,.7);align-items:center}.th-status{flex:1}',
    '.th-kbd{font-size:12px;color:#ccc;border:1px solid #555;border-bottom-width:2px;border-radius:4px;padding:0 6px;margin:0 4px}',
    '#th-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483001;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:8px;border:1px solid rgba(255,193,7,.75);background:linear-gradient(180deg,rgba(30,30,33,.94),rgba(18,18,20,.96));color:#eee;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:14px;box-shadow:0 6px 20px rgba(0,0,0,.6)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();

    var root = el('div');
    var plate = el('div', 'th-plate');
    var head = el('div', 'th-head');
    var sum = el('div', 'th-sum');
    var tabs = el('div', 'th-tabs');
    var body = el('div', 'th-body');
    var foot = el('div', 'th-foot');
    var status = el('div', 'th-status');
    var keys = el('div');
    var toast = el('div');

    root.id = 'th-root';
    toast.id = 'th-toast';
    put(head, el('div', 'th-title', 'Trap Hub'), sum, button('\u00d7', 'close', null, true, 'th-x'));
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

    ui = { root: root, sum: sum, tabs: tabs, body: body, status: status, keys: keys, toast: toast };
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

    var action = target.getAttribute('data-act');
    var args = target.getAttribute('data-args');
    var armKey = action + '|' + (args || '');

    if (CONFIRMED[action] && state.armed !== armKey) {
      state.armed = armKey;
      render();

      return;
    }

    state.armed = '';

    if (LOCAL[action]) {
      LOCAL[action](args);

      return;
    }

    // Opening or shutting the window gets no status line back, so it must not hold the buttons busy.
    if (action === 'close' || action === 'toggle') {
      send(action);

      return;
    }

    if (action === 'savepreset') {
      savePreset();

      return;
    }

    sendAction(args ? action + '|' + args : action);
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
    cat = { types: {}, baits: {}, prey: {}, baitList: [], typeList: [] };

    ((data && data.types) || []).forEach(function (t) { cat.types[t.id] = t; cat.typeList.push(t); });
    ((data && data.baits) || []).forEach(function (b) { cat.baits[b.id] = b; cat.baitList.push(b); });
    ((data && data.prey) || []).forEach(function (p) { cat.prey[p.id] = p; });

    if (state.isOpen) render();
  }

  function useData(data) {
    state.data = data || state.data;
    allowedSet = {};
    presetUses = {};

    (state.data.allowed || []).forEach(function (id) { allowedSet[id] = true; });
    presets().forEach(function (p) {
      p.steps.forEach(function (s, i) { (presetUses[s.item] = presetUses[s.item] || []).push({ preset: p, step: i + 1, keep: s.keep }); });
    });
  }

  function typeOf(id) { return cat.types[id] || { id: id, name: '#' + id, accepts: [], catches: [] }; }

  function baitOf(id) { return cat.baits[id] || { id: id, name: '#' + id, kind: 'other', draws: [] }; }

  function preyOf(id) { return cat.prey[id] || { id: id, name: '#' + id }; }

  function presets() { return state.data.presets || []; }

  function presetById(id) { return find(presets(), function (p) { return p.id === id; }); }

  function defaultPresetFor(typeId) { return find(presets(), function (p) { return (p.types || []).indexOf(typeId) >= 0; }); }

  function owned(itemId) { return (state.data.owned || {})[itemId] || 0; }

  function isAllowed(itemId) { return !!allowedSet[itemId]; }

  function traps() { return state.data.traps || []; }

  function trapById(id) { return find(traps(), function (t) { return t.id === id; }); }

  function find(list, test) {
    for (var i = 0; i < list.length; i++) {
      if (test(list[i])) return list[i];
    }

    return null;
  }

  function byName(a, b) { return a.name < b.name ? -1 : a.name > b.name ? 1 : 0; }

  function roomTitle(trap) {
    return (trap.room || '?') + ' \u00b7 ' + say('floor', trap.floor);
  }

  function kindName(bait) {
    return say(KIND_WORD[bait.kind] || 'kOther');
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

    ui.cards = null;
    ui.detail = null;
    ui.baitCards = null;
    ui.baitDetail = null;
    ui.grid = null;
    ui.addList = null;
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

  function lockMessage(content) {
    var box = el('div', 'th-col');
    box.style.flex = '1';
    box.appendChild(el('div', 'th-lockmsg', content || ''));

    return box;
  }

  function renderHead() {
    var all = traps();
    var parts = [];

    ui.sum.textContent = '';

    if (!state.data.ready) return;

    parts.push(all.length === 1 ? el('span', '', say('nTrap')) : sayCount('nTraps', all.length, ''));

    // Counted the way the Show filters count, so the header and the sidebar never disagree.
    [['prey', 'cPrey', 'th-cg'], ['nobait', 'cNoBait', 'th-cr'], ['worn', 'cWorn', 'th-ca'], ['halted', 'cHalted', '']].forEach(function (s) {
      var count = all.filter(function (t) { return shows(t, s[0]); }).length;

      if (count) parts.push(sayCount(s[1], count, s[2]));
    });

    parts.forEach(function (part, i) {
      if (i) ui.sum.appendChild(el('span', 'th-dot', '\u00b7'));

      ui.sum.appendChild(part);
    });
  }

  function renderTabs() {
    ui.tabs.textContent = '';

    TABS.forEach(function (tab) {
      var node = act(el('button', 'th-tab' + (state.tab === tab ? ' th-on' : ''), say(TAB_WORD[tab])), 'tab', tab);

      if (tab === 'traps' && state.badge > 0) node.appendChild(el('span', 'th-n', String(state.badge)));
      if (tab === 'allowed' && state.data.ready) node.appendChild(el('span', 'th-n th-nn', String((state.data.allowed || []).length)));

      ui.tabs.appendChild(node);
    });
  }

  function renderKeys() {
    ui.keys.textContent = '';

    if (state.key) put(ui.keys, text(say('hotkey')), el('span', 'th-kbd', state.key), text('/ '));

    ui.keys.appendChild(text(say('escClose')));
  }

  function pickTab(tab) {
    if (TABS.indexOf(tab) < 0) return;

    state.tab = tab;
    render();
  }

  // ---------- Traps tab ----------

  function renderTraps() {
    var mid = el('div', 'th-mid');

    ui.acts = el('div', 'th-acts');
    ui.list = trapList();
    ui.detail = scroller('detail', 'th-detail');
    fillActs();
    fillDetail();
    put(mid, ui.acts, ui.list);
    put(ui.body, trapSide(), mid, ui.detail);
  }

  function shows(trap, show) {
    switch (show) {
      case 'prey':
        return trap.state === 'prey';
      case 'nobait':
        return trap.state === 'nobait';
      case 'worn':
        return !!trap.worn;
      case 'halted':
        return trap.state === 'halted';
      default:
        return true;
    }
  }

  function roomKey(trap) {
    return trap.floor + '|' + trap.room;
  }

  function isShown(trap) {
    return shows(trap, state.show) && (!state.ftype || trap.type === state.ftype) && (!state.froom || roomKey(trap) === state.froom);
  }

  // Filters with their counts, the trap types and rooms there are, how the list is grouped, and the bait at hand.
  function trapSide() {
    var side = scroller('side', 'th-side');
    var showChips = el('div', 'th-chips');
    var typeChips = el('div', 'th-chips');
    var roomChips = el('div', 'th-chips');
    var types = [];
    var rooms = [];

    SHOWS.forEach(function (s) {
      var count = traps().filter(function (t) { return shows(t, s[0]); }).length;
      var node = chip(say(s[1]), 'show', s[0], state.show === s[0], count);

      if (!count && state.show !== s[0]) setClass(node, 'th-zero', true);

      showChips.appendChild(node);
    });

    traps().forEach(function (t) {
      var type = find(types, function (x) { return x.id === t.type; });
      var room = find(rooms, function (x) { return x.key === roomKey(t); });

      if (!type) types.push(type = { id: t.type, count: 0 });
      if (!room) rooms.push(room = { key: roomKey(t), trap: t, count: 0 });

      type.count++;
      room.count++;
    });
    types.forEach(function (x) { typeChips.appendChild(chip(typeOf(x.id).name, 'ftype', x.id, state.ftype === x.id, x.count, typeOf(x.id))); });
    rooms.forEach(function (x) { roomChips.appendChild(chip(roomTitle(x.trap), 'froom', x.key, state.froom === x.key, x.count)); });

    put(side, caption(say('show')), showChips);

    if (types.length > 1) put(side, caption(say('trapType')), typeChips);
    if (rooms.length > 1) put(side, caption(say('rooms')), roomChips);

    put(side, caption(say('groupBy')), segment(GROUPS.map(function (g) { return [g[0], say(g[1])]; }), state.group, 'group'),
      caption(say('baitOwn')), stockList());

    return side;
  }

  // What the player has to bait with, most first; a blocked one is dimmed, and a press opens it on the Allowed tab.
  function stockList() {
    var have = state.data.owned || {};
    var list = el('div', 'th-stock');
    var ids = Object.keys(have).map(Number).filter(function (id) { return have[id] > 0 && cat.baits[id]; });

    ids.sort(function (a, b) { return have[b] - have[a] || byName(baitOf(a), baitOf(b)); });
    ids.slice(0, MAX_STOCK).forEach(function (id) {
      var bait = baitOf(id);

      list.appendChild(put(act(el('div', 'th-srow' + (isAllowed(id) ? '' : ' th-out')), 'baitgo', id), icon(bait, 'th-i22'), el('span', 'th-grow', bait.name),
        el('span', 'th-num', String(have[id]))));
    });

    if (!ids.length) list.appendChild(el('div', 'th-dim', say('none')));

    return list;
  }

  // The big buttons, or with traps checked, the presets those traps can be given together.
  function fillActs() {
    var ids = checkedIds();
    var c = state.data.counts || {};

    ui.acts.textContent = '';
    setClass(ui.acts, 'th-bulk', ids.length > 0);
    setClass(ui.list, 'th-checking', ids.length > 0);

    if (!ids.length) {
      // The bulk buttons stay pressable with nothing to do: a press then says so.
      put(ui.acts, button(say('takeAll', c.take || 0), 'takeall', null, true, 'th-hot'), button(say('takeFloor', c.takeFloor || 0), 'takefloor', null, true),
        button(say('rebaitAll', c.rebait || 0), 'rebaitall', null, true));

      return;
    }

    var list = ids.join(',');

    put(ui.acts, el('b', '', say('selected', ids.length)), el('span', 'th-dim', say('assignTo')), chip(say('useDefault'), 'assign', '|' + list, false));
    presets().forEach(function (p) { ui.acts.appendChild(chip(p.name, 'assign', p.id + '|' + list, false)); });
    put(ui.acts, el('span', 'th-grow'), button(say('clear'), 'clearchecks', null, true, 'th-sm'));

    if (!state.data.saved) {
      disableAll(ui.acts, 'assign');
      ui.acts.appendChild(el('span', 'th-dim', say('noSaveYet')));
    }
  }

  function checkedIds() {
    return Object.keys(state.checks).filter(function (id) { return state.checks[id] && trapById(id); });
  }

  function trapList() {
    var well = scroller('traps');
    var shown = traps().filter(isShown);

    ui.cards = {};

    if (!traps().length) {
      well.appendChild(el('div', 'th-empty', say('empty')));

      return well;
    }

    if (!shown.length) {
      well.appendChild(el('div', 'th-empty', say('noMatch')));

      return well;
    }

    groupsOf(shown).forEach(function (group) {
      var cards = el('div', 'th-cards');

      group.traps.forEach(function (trap) { cards.appendChild(trapCard(trap)); });
      put(well, groupHead(group), cards);
    });

    return well;
  }

  // Floor and room groups keep the order the plugin sends (floor, then room); type and state groups are sorted.
  function groupsOf(list) {
    var groups = [];

    list.forEach(function (trap) {
      var key = groupKey(trap);
      var group = find(groups, function (g) { return g.key === key; });

      if (!group) groups.push(group = { key: key, first: trap, traps: [] });

      group.traps.push(trap);
    });

    if (state.group === 'state') groups.sort(function (a, b) { return STATE_ORDER.indexOf(a.key) - STATE_ORDER.indexOf(b.key); });
    if (state.group === 'type') groups.sort(function (a, b) { return byName(typeOf(a.key), typeOf(b.key)); });

    return groups;
  }

  function groupKey(trap) {
    if (state.group === 'room') return roomKey(trap);
    if (state.group === 'type') return trap.type;
    if (state.group === 'state') return trap.state;

    return trap.floor;
  }

  function groupTitle(trap) {
    if (state.group === 'room') return roomTitle(trap);
    if (state.group === 'type') return typeOf(trap.type).name;
    if (state.group === 'state') return say(STATE_WORD[trap.state] || trap.state);

    return say('floor', trap.floor);
  }

  function groupHead(group) {
    var head = put(el('div', 'th-sect th-ghead'), el('span', 'th-gt', groupTitle(group.first)));
    var isPlace = state.group === 'floor' || state.group === 'room';

    if (isPlace && group.first.floor === state.data.floor) head.appendChild(tag(say('here'), 'th-b-amber th-here'));

    head.appendChild(el('span', 'th-r', trapCount(group.traps.length)));

    return head;
  }

  function stateTag(trap) {
    return tag(say(STATE_WORD[trap.state] || trap.state), STATE_TAG[trap.state]);
  }

  function trapCard(trap) {
    var type = typeOf(trap.type);
    var isChecked = !!state.checks[trap.id];
    var tone = trap.state === 'prey' ? ' th-ready' : trap.state === 'nobait' ? ' th-hot' : '';
    var card = act(el('div', 'th-card' + (trap.id === state.sel ? ' th-sel' : '') + tone), 'pick', trap.id);
    var check = act(el('button', 'th-chk' + (isChecked ? ' th-on' : ''), isChecked ? '\u2713' : ''), 'check', trap.id);
    var name = put(el('div', 'th-nm'), el('span', '', type.name), stateTag(trap), trap.worn ? tag(say('fBreaks'), 'th-b-amber') : null,
      trap.queued ? tag(say('queued'), 'th-b-green') : null);
    var preset = presetById(trap.preset);
    var main = put(el('div', 'th-cmain'), name, baitLine(trap), wearLine(trap), el('div', 'th-sub', (trap.room || '?') + ' \u00b7 ' + (preset ? preset.name : say('none'))));

    put(card, check, icon(type, 'th-i44'), main, cardRight(trap));
    ui.cards[trap.id] = card;

    return card;
  }

  function cardRight(trap) {
    var right = el('div', 'th-crt');

    if (trap.prey) right.appendChild(icon(preyOf(trap.prey), 'th-i40'));
    else if (trap.state === 'armed') right.appendChild(el('div', 'th-pct', trap.chance + '%'));

    if (trap.store) right.appendChild(tag(say('store', trap.store), 'th-b-blue'));

    return right;
  }

  // What is in the trap, or what a re-bait would put there.
  function baitLine(trap) {
    var line = el('div', 'th-ln');

    if (trap.prey) return put(line, el('span', 'th-green', say('caught', preyOf(trap.prey).name)));

    if (trap.bait) {
      var bait = baitOf(trap.bait);

      return put(line, icon(bait, 'th-i18'), el('span', '', bait.name));
    }

    if (trap.state === 'halted') return line;
    if (!trap.canBait) return put(line, el('span', 'th-red', say('stNoBait')));
    if (!trap.next) return put(line, el('span', 'th-red', say('nextNone')));

    var next = baitOf(trap.next);

    return put(line, icon(next, 'th-i18'), el('span', 'th-green', say('next', next.name)));
  }

  function wearLine(trap) {
    var words = trap.dur + ' / ' + trap.max;

    if (trap.catches != null && !trap.worn) words += ' \u00b7 ' + say('catches', trap.catches);

    return put(el('div', 'th-ln'), durabilityBar(trap), el('span', '', words));
  }

  function durabilityBar(trap, width) {
    var track = el('span', 'th-track');
    var fill = el('span', 'th-fill' + (trap.worn ? ' th-red' : trap.dur * 2 < trap.max ? ' th-amber' : ''));

    if (width) track.style.width = width;

    fill.style.width = percentOf(trap.dur, trap.max) + '%';
    track.appendChild(fill);

    return track;
  }

  function percentOf(part, whole) {
    return whole > 0 ? Math.max(0, Math.min(100, Math.round(part * 100 / whole))) : 0;
  }

  function fillDetail() {
    var trap = trapById(state.sel);

    ui.detail.textContent = '';

    if (trap) trapDetail(ui.detail, trap);
    else needsList(ui.detail);
  }

  function needsYou(trap) {
    return trap.state !== 'armed' || !!trap.worn;
  }

  function needRank(trap) {
    return trap.state === 'prey' ? 0 : trap.state === 'nobait' ? 1 : trap.worn ? 2 : 3;
  }

  function needTag(trap) {
    if (trap.state === 'prey') return tag(trap.prey ? preyOf(trap.prey).name : say('stPrey'), 'th-b-green');
    if (trap.state === 'nobait') return tag(say('stNoBait'), 'th-b-red');
    if (trap.worn) return tag(say('fBreaks'), 'th-b-amber');

    return stateTag(trap);
  }

  // With no trap picked: every trap waiting on the player, catches first.
  function needsList(well) {
    var waiting = traps().filter(needsYou).sort(function (a, b) { return needRank(a) - needRank(b); });
    var rows = el('div', 'th-rows');

    well.appendChild(section(say('needsYou'), waiting.length ? el('span', 'th-r', String(waiting.length)) : null));

    if (!waiting.length) well.appendChild(el('div', 'th-empty', say('allFine')));

    waiting.slice(0, MAX_NEEDS).forEach(function (trap) {
      rows.appendChild(put(act(el('div', 'th-row'), 'goto', trap.id), icon(typeOf(trap.type), 'th-i30'),
        put(el('div', 'th-grow'), el('div', '', typeOf(trap.type).name), el('div', 'th-dim', roomTitle(trap))), needTag(trap)));
    });

    put(well, rows, el('div', 'th-note', say('pickTrap')));
  }

  function trapDetail(well, trap) {
    var type = typeOf(trap.type);
    var isArmed = state.armed === 'dismantle|' + trap.id;
    var head = put(el('div', 'th-dhead'), icon(type, 'th-i56'),
      put(el('div', 'th-dname'), el('div', 'th-big', type.name), el('div', 'th-dim', roomTitle(trap))), stateTag(trap));
    var acts = put(el('div', 'th-bar'),
      button(say('take'), 'take', trap.id, trap.canTake && !trap.queued, 'th-hot'),
      button(say('rebait'), 'rebait', trap.id, trap.canBait),
      el('span', 'th-grow'),
      button(isArmed ? say('confirm') : say('dismantle'), 'dismantle', trap.id, true, 'th-danger' + (isArmed ? ' th-armed' : '')));

    put(well, head, acts, catchRow(trap), baitBox(trap), oddsBox(trap), wearBox(trap));
  }

  function catchRow(trap) {
    if (!trap.prey) return null;

    var prey = preyOf(trap.prey);

    return put(el('div', 'th-nrow th-next'), icon(prey, 'th-i30'), el('span', 'th-grow', say('caught', prey.name)), trap.queued ? tag(say('queued'), 'th-b-green') : null);
  }

  // The trap's preset: its type's default or one of its own, every step in order and what the player has of it.
  function baitBox(trap) {
    var box = put(el('div', 'th-box'), el('h4', '', say('preset')));
    var chips = el('div', 'th-chips');
    var fallback = defaultPresetFor(trap.type);
    var preset = presetById(trap.preset);

    chips.appendChild(chip(say('typeDefault', fallback ? fallback.name : say('none')), 'assign', '|' + trap.id, !trap.own));
    presets().forEach(function (p) { chips.appendChild(chip(p.name, 'assign', p.id + '|' + trap.id, trap.own && p.id === trap.preset)); });
    box.appendChild(chips);

    if (!state.data.saved) {
      disableAll(chips, 'assign');
      box.appendChild(el('div', 'th-dim', say('noSaveYet')));
    }

    if (preset) box.appendChild(stepRows(preset, trap));
    if (trap.canBait) box.appendChild(el('div', trap.next ? 'th-green' : 'th-red', trap.next ? say('next', baitOf(trap.next).name) : say('nextNone')));

    return box;
  }

  function stepRows(preset, trap) {
    var rows = el('div', 'th-rows th-tight');
    var accepts = typeOf(trap.type).accepts || [];
    var lit = trap.bait || trap.next || 0;

    preset.steps.forEach(function (step, i) {
      var bait = baitOf(step.item);
      var isTaken = accepts.indexOf(step.item) >= 0;
      var isUsable = isAllowed(step.item) && isTaken && owned(step.item) > step.keep;
      var row = el('div', 'th-nrow' + (step.item === lit ? ' th-next' : isUsable ? '' : ' th-out'));
      var why = !isAllowed(step.item) ? el('span', 'th-red', say('notAllowed')) : !isTaken ? el('span', 'th-dim', say('notTaken')) : el('span', 'th-dim', say('own', owned(step.item)));

      rows.appendChild(put(row, el('span', 'th-ordn', String(i + 1)), icon(bait, 'th-i22'), el('span', 'th-grow', bait.name),
        step.keep ? el('span', 'th-dim', say('keepN', step.keep)) : null, why));
    });

    if (preset.lastResort) rows.appendChild(put(el('div', 'th-nrow'), el('span', 'th-ordn', '\u221e'), el('span', 'th-grow th-dim', say('lastResort'))));

    return rows;
  }

  // The chance of a catch at the next check, split by prey; the rest of the chance is shown as nothing.
  function oddsBox(trap) {
    var odds = trap.odds || [];
    var box = put(el('div', 'th-box'), put(el('h4', ''), el('span', '', say('oddsNow')), el('span', 'th-hnum', trap.chance + '%')));
    var listed = 0;

    odds.forEach(function (o) {
      var prey = preyOf(o.prey);

      listed += o.pct;
      box.appendChild(oddsRow(icon(prey, 'th-i22'), prey.name + (prey.rare ? ' \u2605' : ''), o.pct, ''));
    });

    if (trap.chance - listed > 0) box.appendChild(oddsRow(null, say('otherPrey'), trap.chance - listed, ''));

    box.appendChild(oddsRow(null, say('nothing'), Math.max(0, 100 - trap.chance), ' th-nil'));

    return box;
  }

  function oddsRow(iconNode, name, pct, extraClass) {
    var track = el('span', 'th-track');
    var fill = el('span', 'th-fill' + (extraClass ? ' th-grey' : ''));

    fill.style.width = Math.max(0, Math.min(100, pct)) + '%';
    track.appendChild(fill);

    return put(el('div', 'th-orow' + extraClass), iconNode || el('span', 'th-ispace'), el('span', 'th-grow', name), track, el('span', 'th-num', pct + '%'));
  }

  function wearBox(trap) {
    var box = put(el('div', 'th-box'), put(el('h4', ''), el('span', '', say('wear')), el('span', 'th-hnum', trap.dur + ' / ' + trap.max)), durabilityBar(trap, '100%'));

    if (trap.catches != null) box.appendChild(el('div', trap.worn ? 'th-red' : 'th-dim', trap.worn ? say('breaks') : say('catches', trap.catches)));

    return box;
  }

  // Picking and checking only touch the cards involved and the panes beside them; the list is not rebuilt.
  function pickTrap(id) {
    state.sel = state.sel === id ? '' : id;
    showPicked();
  }

  // From the needs list or another tab: picks the trap and brings its card into view, clearing filters that hide it.
  function gotoTrap(id) {
    var trap = trapById(id);

    if (!trap) return;

    state.sel = id;

    if (!isShown(trap)) {
      state.show = 'all';
      state.ftype = 0;
      state.froom = '';
    }

    if (state.tab !== 'traps' || !ui.cards || !ui.cards[id]) {
      state.tab = 'traps';
      render();
    } else {
      showPicked();
    }

    var card = ui.cards && ui.cards[id];

    if (card && card.scrollIntoView) card.scrollIntoView({ block: 'nearest' });
  }

  function showPicked() {
    if (!ui.cards || !ui.detail) {
      render();

      return;
    }

    Object.keys(ui.cards).forEach(function (key) { setClass(ui.cards[key], 'th-sel', key === state.sel); });
    fillDetail();
  }

  function checkTrap(id) {
    state.checks[id] = !state.checks[id];

    var card = ui.cards && ui.cards[id];

    if (!card) {
      render();

      return;
    }

    var check = card.querySelector('.th-chk');

    setClass(check, 'th-on', state.checks[id]);
    check.textContent = state.checks[id] ? '\u2713' : '';
    fillActs();
  }

  function clearChecks() {
    state.checks = {};
    render();
  }

  // ---------- Bait presets tab ----------

  function renderPresets() {
    syncDraft();

    var side = el('div', 'th-plist');
    var list = scroller('plist');
    var editor = scroller('editor', 'th-editor');
    var uses = scroller('uses', 'th-uses');

    list.appendChild(section(say('tabPresets')));
    presets().forEach(function (p) { list.appendChild(presetItem(p)); });
    put(side, list, button('+ ' + say('newPreset'), 'newpreset', null, true));

    if (state.draft) {
      fillEditor(editor);
      fillUses(uses);
    }

    put(ui.body, side, editor, uses);
  }

  function presetItem(p) {
    var icons = el('div', 'th-icons');
    var used = traps().filter(function (t) { return t.preset === p.id; }).length;
    var notes = [];

    p.steps.slice(0, 4).forEach(function (s) { icons.appendChild(icon(baitOf(s.item), 'th-i18')); });

    if ((p.types || []).length) notes.push(say('defaultFor') + ' ' + p.types.map(function (t) { return typeOf(t).name; }).join(', '));

    notes.push(trapCount(used));

    return put(act(el('div', 'th-pitem' + (p.id === state.presetId ? ' th-sel' : '')), 'preset', p.id),
      put(el('div', 'th-lbl'), el('b', '', p.name), el('small', '', notes.join(' \u00b7 '))), icons);
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
    var steps = el('div', 'th-rows th-tight');
    var firstUsable = -1;

    name.setAttribute('data-field', 'name');
    name.setAttribute('placeholder', say('name'));
    name.value = d.name;
    name.style.width = '300px';

    d.steps.forEach(function (s, i) {
      if (firstUsable < 0 && isAllowed(s.item) && owned(s.item) > s.keep) firstUsable = i;
    });

    put(editor,
      put(el('div', 'th-bar'), name, button(say('save'), 'savepreset', null, true, 'th-hot'),
        d.id ? button(isArmed ? say('confirm') : say('delete'), 'deletepreset', d.id, true, 'th-danger' + (isArmed ? ' th-armed' : '')) : null,
        el('span', 'th-grow'), tag(say('unsaved'), 'th-b-amber th-dirty')),
      section(say('steps'), el('span', 'th-r', say('stepsNote'))));
    markDirty(editor);

    if (!d.steps.length) editor.appendChild(el('div', 'th-note', say('noSteps')));

    d.steps.forEach(function (step, i) { steps.appendChild(stepEditor(step, i, d.steps.length, i === firstUsable)); });

    if (d.lastResort) steps.appendChild(put(el('div', 'th-dash'), text('\u221e  ' + say('lastResort'))));

    put(editor, steps, section(say('addBait')), addPicker(),
      section(say('rules')),
      setRow(say('lastResort'), null, switchButton(d.lastResort, 'lastresort')),
      setRow(say('source'), null, segment([['bag', say('srcBag')], ['home', say('srcHome')]], d.source, 'source')),
      setRow(say('spoilFirst'), null, switchButton(d.spoilFirst, 'spoil')),
      setRow(say('auto'), say('autoNote'), segment(AUTOS.map(function (a) { return [a[0], say(a[1])]; }), d.auto, 'autoset')),
      setRow(say('defaultFor'), null, typeChips(d)));
  }

  function typeChips(d) {
    var box = el('div', 'th-chips');

    cat.typeList.forEach(function (type) { box.appendChild(chip(type.name, 'deftype', type.id, d.types.indexOf(type.id) >= 0, null, type)); });

    return box;
  }

  function stepEditor(step, i, count, isFirst) {
    var bait = baitOf(step.item);
    var allowed = isAllowed(step.item);
    var have = owned(step.item);
    var row = el('div', 'th-step' + (!allowed ? ' th-bad' : have <= step.keep ? ' th-short' : isFirst ? ' th-go' : ''));
    var title = put(el('b'), el('span', '', bait.name), allowed ? null : tag(say('blocked'), 'th-b-red'));
    var info = put(el('div', 'th-sinfo'), title, el('small', have > step.keep ? '' : 'th-red', say('own', have) + ' \u00b7 ' + kindName(bait)), drawIcons(bait, 6));
    var stepper = put(el('div', 'th-stepper'), plainButton('th-stp', '-', 'keepless', i, step.keep > 0), el('b', '', String(step.keep)),
      plainButton('th-stp', '+', 'keepmore', i, step.keep < MAX_KEEP));
    var moves = put(el('div', 'th-moves'), plainButton('th-ib', '\u2191', 'stepup', i, i > 0), plainButton('th-ib', '\u2193', 'stepdown', i, i < count - 1),
      plainButton('th-ib th-del', '\u00d7', 'stepdel', i, true));
    var controls = put(el('div', 'th-sctl'), allowed ? null : button(say('allow'), 'allow', '1|' + step.item, true, 'th-sm'), el('span', 'th-dim', say('keep')), stepper, moves);

    return put(row, el('span', 'th-ord', String(i + 1)), icon(bait, 'th-i30'), info, controls);
  }

  function drawIcons(bait, max) {
    var icons = el('div', 'th-icons');

    (bait.draws || []).slice(0, max).forEach(function (id) { icons.appendChild(icon(preyOf(id), 'th-i18')); });

    return icons.firstChild ? icons : null;
  }

  // A search box over every bait the game knows; owned and allowed baits come first.
  function addPicker() {
    var box = el('div', 'th-picker');
    var search = el('input', 'th-inp th-small');
    var list = el('div', 'th-col');

    list.style.gap = '2px';
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
      return (owned(b.id) > 0) - (owned(a.id) > 0) || isAllowed(b.id) - isAllowed(a.id) || byName(a, b);
    });

    list.textContent = '';
    found.slice(0, MAX_ADD).forEach(function (b) {
      list.appendChild(put(act(el('div', 'th-prow'), 'stepadd', b.id), icon(b, 'th-i22'), el('span', 'th-grow', b.name), el('span', 'th-dim', say('own', owned(b.id))),
        tag(say(isAllowed(b.id) ? 'allowed' : 'blocked'), isAllowed(b.id) ? 'th-b-green' : 'th-b-red')));
    });

    if (!found.length) list.appendChild(el('div', 'th-dim', say('none')));
    if (found.length > MAX_ADD) list.appendChild(el('div', 'th-dim', say('more', found.length - MAX_ADD)));
  }

  // The traps the preset in the editor is on now, whether by their type's default or picked for them.
  function fillUses(well) {
    var using = traps().filter(function (t) { return t.preset === state.draft.id; });

    well.appendChild(section(say('usedBy'), el('span', 'th-r', String(using.length))));

    if (!using.length) well.appendChild(el('div', 'th-empty', say('usedNone')));

    using.forEach(function (trap) {
      well.appendChild(put(act(el('div', 'th-row'), 'goto', trap.id), icon(typeOf(trap.type), 'th-i30'),
        put(el('div', 'th-grow'), el('div', '', typeOf(trap.type).name), el('div', 'th-dim', roomTitle(trap))),
        tag(say(trap.own ? 'ownPick' : 'useDefault'), trap.own ? 'th-b-amber' : 'th-b-dim')));
    });
  }

  function editDraft(change) {
    if (!state.draft) return;

    change(state.draft);
    render();
  }

  function markDirty(scope) {
    var mark = (scope || ui.body).querySelector('.th-dirty');

    if (mark) mark.style.display = isDirty() ? '' : 'none';
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
    var side = scroller('wlside', 'th-side');
    var mid = el('div', 'th-mid');
    var search = el('input', 'th-inp th-small');
    var grid = scroller('wlgrid');

    search.setAttribute('data-field', 'search');
    search.setAttribute('placeholder', say('search'));
    search.value = state.search;
    grid.style.flex = '1';
    ui.grid = grid;
    ui.baitDetail = scroller('baitdetail', 'th-detail');
    fillBaitSide(side);
    put(mid, search, grid);
    put(ui.body, side, mid, ui.baitDetail);
    fillBaitGrid();
    fillBaitDetail();
  }

  function baitShows(b, show) {
    switch (show) {
      case 'owned':
        return owned(b.id) > 0;
      case 'allowed':
        return isAllowed(b.id);
      case 'blocked':
        return !isAllowed(b.id);
      case 'inpreset':
        return !!presetUses[b.id];
      default:
        return true;
    }
  }

  function fillBaitSide(side) {
    var showChips = el('div', 'th-chips');
    var kindChips = el('div', 'th-chips');
    var inPresets = cat.baitList.filter(function (b) { return presetUses[b.id] && !isAllowed(b.id); }).map(function (b) { return b.id; });
    var universal = cat.baitList.filter(function (b) { return b.kind === 'universal' && owned(b.id) > 0 && !isAllowed(b.id); }).map(function (b) { return b.id; });

    BAIT_SHOWS.forEach(function (s) {
      var count = cat.baitList.filter(function (b) { return baitShows(b, s[0]); }).length;

      showChips.appendChild(chip(say(s[1]), 'filter', s[0], state.baitShow === s[0], count));
    });
    KINDS.forEach(function (kind) { kindChips.appendChild(chip(say(KIND_WORD[kind]), 'kind', kind, state.kind === kind)); });

    put(side, caption(say('show')), showChips, caption(say('kinds')), kindChips, caption(say('quick')),
      put(el('div', 'th-qcol'),
        button(say('allowPresets') + ' (' + inPresets.length + ')', 'allow', '1|' + inPresets.join(','), inPresets.length > 0),
        button(say('allowUniversal') + ' (' + universal.length + ')', 'allow', '1|' + universal.join(','), universal.length > 0),
        shownButton(say('allowShown'), '1', ''),
        shownButton(say('blockShown'), '0', 'th-danger')),
      el('div', 'th-note', say('allowedNote')));
  }

  function shownBaits() {
    var query = state.search.trim().toLowerCase();

    return cat.baitList.filter(function (b) {
      if (state.kind && b.kind !== state.kind) return false;
      if (query && b.name.toLowerCase().indexOf(query) < 0) return false;

      return baitShows(b, state.baitShow);
    }).sort(function (a, b) {
      return (owned(b.id) > 0) - (owned(a.id) > 0) || byName(a, b);
    });
  }

  // Owned bait first, then the rest; Allow all shown and Block all shown act on every bait that matches, past the cut too.
  function fillBaitGrid() {
    var grid = ui.grid;

    if (!grid) return;

    var shown = shownBaits();
    var room = MAX_CARDS;

    grid.textContent = '';
    ui.baitCards = {};

    [[shown.filter(function (b) { return owned(b.id) > 0; }), 'sOwned'], [shown.filter(function (b) { return !(owned(b.id) > 0); }), 'sNotOwned']].forEach(function (part) {
      if (!part[0].length || !room) return;

      var cards = el('div', 'th-wl');

      part[0].slice(0, room).forEach(function (b) { cards.appendChild(baitCard(b)); });
      room = Math.max(0, room - part[0].length);
      put(grid, section(say(part[1]), el('span', 'th-r', String(part[0].length))), cards);
    });

    if (!shown.length) grid.appendChild(el('div', 'th-empty', say('noBaitMatch')));
    if (shown.length > MAX_CARDS) grid.appendChild(el('div', 'th-note', say('more', shown.length - MAX_CARDS)));

    var ids = shown.map(function (b) { return b.id; }).join(',');

    ui.body.querySelectorAll('[data-shown]').forEach(function (node) {
      node.setAttribute('data-args', node.getAttribute('data-shown') + '|' + ids);
      node.disabled = shown.length === 0;
    });
  }

  function shownButton(content, flag, extraClass) {
    var node = button(content, 'allow', flag + '|', true, extraClass);
    node.setAttribute('data-shown', flag);

    return node;
  }

  function baitCard(b) {
    var isOn = isAllowed(b.id);
    var have = owned(b.id);
    var card = act(el('div', 'th-bait' + (isOn ? '' : ' th-off') + (b.id === state.baitSel ? ' th-sel' : '')), 'bait', b.id);
    var info = put(el('div', 'th-binfo'), el('b', '', b.name), el('small', '', kindName(b) + (have ? ' \u00b7 ' + say('own', have) : '')), drawIcons(b, 6));

    put(card, icon(b, 'th-i40'), info, button(say(isOn ? 'allowed' : 'blocked'), 'allow', (isOn ? '0|' : '1|') + b.id, true, 'th-sm ' + (isOn ? 'th-ok' : 'th-no')));
    ui.baitCards[b.id] = card;

    return card;
  }

  function pickBait(id) {
    state.baitSel = state.baitSel === +id ? 0 : +id;

    if (!ui.baitCards || !ui.baitDetail) {
      render();

      return;
    }

    Object.keys(ui.baitCards).forEach(function (key) { setClass(ui.baitCards[key], 'th-sel', +key === state.baitSel); });
    fillBaitDetail();
  }

  // One bait: whether traps may use it, the presets that list it, what it draws in and which traps take it.
  function fillBaitDetail() {
    var well = ui.baitDetail;

    if (!well) return;

    well.textContent = '';

    var b = state.baitSel ? cat.baits[state.baitSel] : null;

    if (!b) {
      put(well, el('div', 'th-empty', say('pickBait')), el('div', 'th-note', say('allowedNote')));

      return;
    }

    var isOn = isAllowed(b.id);
    var uses = presetUses[b.id] || [];
    var usesBox = put(el('div', 'th-box'), el('h4', '', say('inPresets')));
    var draws = put(el('div', 'th-box'), el('h4', '', say('drawsIn')), preyChips(b.draws || []));
    var takers = cat.typeList.filter(function (t) { return (t.accepts || []).indexOf(b.id) >= 0; });
    var takenChips = el('div', 'th-pchips');
    var takenBox = put(el('div', 'th-box'), el('h4', '', say('takenByHead')), takenChips);

    if (!uses.length) usesBox.appendChild(el('div', 'th-dim', say('notInPresets')));

    uses.forEach(function (u) {
      usesBox.appendChild(put(act(el('div', 'th-nrow th-link'), 'presetgo', u.preset.id), el('span', 'th-grow', say('presetStep', u.preset.name, u.step)),
        u.keep ? el('span', 'th-dim', say('keepN', u.keep)) : null));
    });

    if (uses.length && !isOn) usesBox.appendChild(el('div', 'th-red', say('skipped')));

    if (!takers.length) takenChips.appendChild(el('span', 'th-dim', say('none')));

    takers.forEach(function (t) { takenChips.appendChild(put(el('span', 'th-pchip'), icon(t, 'th-i22'), el('span', '', t.name))); });

    put(well,
      put(el('div', 'th-dhead'), icon(b, 'th-i56'),
        put(el('div', 'th-dname'), el('div', 'th-big', b.name), el('div', 'th-dim', kindName(b) + ' \u00b7 ' + say('own', owned(b.id)))),
        tag(say(isOn ? 'allowed' : 'blocked'), isOn ? 'th-b-green' : 'th-b-red')),
      put(el('div', 'th-bar'), button(say(isOn ? 'block' : 'allow'), 'allow', (isOn ? '0|' : '1|') + b.id, true, isOn ? 'th-danger' : 'th-hot')),
      usesBox, draws, takenBox);
  }

  function preyChips(ids) {
    var box = el('div', 'th-pchips');

    ids.forEach(function (id) {
      var prey = preyOf(id);

      box.appendChild(put(el('span', 'th-pchip'), icon(prey, 'th-i22'), el('span', '', prey.name + (prey.rare ? ' \u2605' : ''))));
    });

    if (!ids.length) box.appendChild(el('span', 'th-dim', say('none')));

    return box;
  }

  // ---------- Settings tab ----------

  function renderSettings() {
    var data = state.data;
    var well = scroller('settings', 'th-settings');
    var sounds = data.sounds || {};
    var isElsewhere = !!data.soundsElsewhere;

    put(well, section(say('sounds')), el('div', 'th-dim', say('soundsNote')));

    if (isElsewhere) well.appendChild(el('div', 'th-banner', say('quietTraps')));

    SOUNDS.forEach(function (sound) {
      well.appendChild(setRow(say(sound[1]), null, segment(MODES.map(function (m) { return [m[0], say(m[1])]; }), sounds[sound[0]], 'sound', !isElsewhere, sound[0] + '|')));
    });

    put(well, section(say('tabSettings')),
      setRow(state.key ? say('keyOpen', state.key) : say('keyNone'), say('presetFile'), null));
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
    useData(data);
    state.isOpen = true;
    state.isTyping = false;
    ui.status.textContent = '';
    ui.toast.style.display = 'none';
    ui.root.style.display = 'flex';
    render();
  }

  function setData(data) {
    useData(data);
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

  // The number on the HUD button and the Traps tab: traps with a catch to take or waiting for bait.
  function badge(count) {
    state.badge = Math.max(0, count | 0);
    hudVersion++;

    if (ui && state.isOpen) renderTabs();

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
    var node = hudButton(doc);

    node.setAttribute('data-etk-dock', DOCK_ORDER);
    column.insertBefore(node, column.firstChild);

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
