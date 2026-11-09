// Trap Hub's web half. Runs in Root.html, the document every game page iframe sits in. Draws the Trap Hub window over
// all of them, styled like the game's panels, and puts a trap button with a count badge in the HUD's tool column
// (bottom left, above the phone and bag buttons), built like the game's buttons there. Talks to the plugin through
// Exiled's Toolkit's page API ("slmod|traphub|<verb>" over vuplex) and is driven by window.__sltraphub.
(function () {
  if (window.__sltraphub) return;

  var PREFIX = 'slmod|traphub|';
  var HUD_CLASS = 'th-hud-btn';
  // Trap Hub's place in the HUD's tool column when Exiled's Toolkit lays it out in columns of four.
  var DOCK_ORDER = '10';
  var HUD_PAGE = /CoreUI1\.html/;
  // The name the plugin's own UI size setting draws Trap Hub's panels at (Interface > UiScale).
  var UI_SIZE_NAME = 'TrapHub';
  var BUSY_TIMEOUT_MS = 5000;
  var MAX_ROWS = 240;
  var MAX_ADD = 30;
  var MAX_KEEP = 99;
  var MAX_NAMES = 3;
  var TOP_ODDS = 3;
  var SVG_NS = 'http://www.w3.org/2000/svg';
  // A cage: a top bar, three bars and a floor, on the 24-unit grid the game's HUD icons use.
  var CAGE_PATH = 'M3 3h18v3H3V3zm2 4h2v10H5V7zm5 0h2v10h-2V7zm5 0h2v10h-2V7zm4 0h2v10h-2V7zM3 18h18v3H3v-3z';
  var TABS = ['traps', 'bait', 'settings'];
  var TAB_WORD = { traps: 'tabTraps', bait: 'tabBait', settings: 'tabSettings' };
  var STATE_WORD = { armed: 'stArmed', prey: 'stPrey', nobait: 'stNoBait', halted: 'stHalted' };
  var SHOWS = [['needs', 'needsYou'], ['armed', 'stArmed'], ['all', 'fAll']];
  var BAIT_SHOWS = [['owned', 'fOwned'], ['allowed', 'fAllowed'], ['blocked', 'fBlocked'], ['inpreset', 'fInPreset'], ['all', 'fAll']];
  var KINDS = ['universal', 'preference', 'specialized', 'balanced', 'other'];
  var KIND_WORD = { universal: 'kUniversal', preference: 'kPreference', specialized: 'kSpecialized', balanced: 'kBalanced', other: 'kOther' };
  var SOUNDS = [['cage', 'sCage'], ['net', 'sNet'], ['other', 'sOther']];
  var MODES = [['Normal', 'mNormal'], ['Once', 'mOnce'], ['Muted', 'mMuted']];
  var AUTOS = [['off', 'autoOff'], ['nearby', 'autoNearby'], ['always', 'autoAlways']];
  // Presses the page answers itself; everything else goes to the plugin.
  var LOCAL = {
    tab: pickTab, pick: pickTrap, 'goto': gotoTrap, back: function () { pickTrap(state.sel); },
    show: function (v) { state.show = v; render(); },
    bait: pickBait, baitback: function () { pickBait(state.baitSel); },
    filter: function (v) { state.baitShow = v; render(); },
    kind: function (v) { state.kind = state.kind === v ? '' : v; render(); },
    preset: pickPreset, presetgo: function (v) { state.tab = 'bait'; pickPreset(v); }, newpreset: newPreset,
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
    tabTraps: 'Traps', tabBait: 'Bait', tabSettings: 'Settings', nTrap: '1 trap', nTraps: '{0} traps', sumNeed: '{0} need you',
    floor: 'Floor {0}', here: 'You are here', stArmed: 'Armed', stPrey: 'Prey', stNoBait: 'No bait', stHalted: 'Halted', caught: 'Caught: {0}',
    next: 'Re-bait uses: {0}', nextShort: 'Empty, next: {0}', nextNone: 'No allowed bait left for its preset', catches: '{0} catches left',
    breaks: 'Breaks on its next catch', store: 'Store: {0}', queued: 'Walk queued', take: 'Take', rebait: 'Re-bait', dismantle: 'Dismantle',
    confirm: 'Sure?', preset: 'Bait preset', typeDefault: 'Type default ({0})', useDefault: 'Type default', ownPick: 'Picked for it', none: 'None',
    empty: 'No traps placed at home yet.', noMatch: 'No traps match.', keepN: 'keep {0}', notTaken: 'This trap does not take it',
    oddsNow: 'Catch odds now', otherPrey: 'Other prey', nothing: 'Nothing', wear: 'Wear', needsYou: 'Needs you', fAll: 'All',
    allFine: 'All your traps are armed and baited.', fBreaks: 'About to break', findTrap: 'Find a trap, room or prey',
    checkAll: 'Check all traps ({0})', checkNote: 'Collect {0}, then bait {1} from your bait presets.', checkFloor: 'This floor only ({0})',
    takeOnly: 'Collect only ({0})', baitOnly: 'Bait only ({0})', tCollect: 'Collect', tBait: 'Bait', tNoBaitLeft: 'No bait left',
    tNoBaitLeftSub: 'Allow more bait, or add some to its preset', tBreaks: 'Breaks soon', back: 'Back', thisTrap: 'This trap',
    nextBait: 'Next bait', inTrap: 'In the trap', noSaveYet: 'Save the game once to pick presets per trap.', tabPresets: 'Bait presets',
    typeUses: 'Each trap type uses', newPreset: 'New preset', name: 'Name', steps: 'Bait order',
    stepsNote: 'Trap Hub uses the first one you have more of than you keep.', noSteps: 'No baits yet. Add some below.', keep: 'Keep',
    own: 'Own {0}', notAllowed: 'Not allowed, skipped', lastResort: 'Then any allowed universal bait', rules: 'Rules', source: 'Take bait from',
    srcBag: 'Bag only', srcHome: 'Bag, then home storage', spoilFirst: 'Use the food closest to spoiling first', auto: 'Auto re-bait',
    autoOff: 'Off', autoNearby: 'On my floor', autoAlways: 'Always', autoNote: 'Puts bait in an empty trap by itself, from this preset, without a walk.',
    defaultFor: 'Default for', save: 'Save', delete: 'Delete', addBait: 'Add a bait', search: 'Search', usedBy: 'Traps using it',
    usedNone: 'No trap uses this preset yet.', more: '{0} more. Search to narrow the list.', unsaved: 'Unsaved changes', tabAllowed: 'Allowed bait',
    allowedNote: 'Trap Hub only puts bait you allow here in your traps. Everything starts blocked.', fOwned: 'I own', fAllowed: 'Allowed',
    fBlocked: 'Blocked', fInPreset: 'In a preset', kUniversal: 'Universal', kPreference: 'Preference', kSpecialized: 'Specialized',
    kBalanced: 'Balanced', kOther: 'Other', allowPresets: 'Allow everything in my presets', allowUniversal: 'Allow universal bait I own',
    allowShown: 'Allow all shown', blockShown: 'Block all shown', allowed: 'Allowed', blocked: 'Blocked', allow: 'Allow', block: 'Block',
    noBaitMatch: 'No bait matches these filters.', inPresets: 'In your presets', presetStep: '{0}, step {1}', skipped: 'Skipped while blocked',
    notInPresets: 'Not in any preset.', drawsIn: 'Draws in', takenByHead: 'Taken by', sounds: 'Catch sounds',
    soundsNote: 'The sound a trap plays while it holds a catch. Chime once plays it for a few seconds, then goes quiet.', sCage: 'Live Capture Cage',
    sNet: 'Simple Bird Net', sOther: 'Mousetrap, Iron Animal Trap, Auto Trap Cage', mNormal: 'Normal', mOnce: 'Chime once', mMuted: 'Muted',
    quietTraps: 'Quiet Traps is installed, so it handles catch sounds.', keys: 'Keys', keyCheck: 'Check traps',
    keyCheckNote: 'Collects every catch, then baits every empty trap from its bait preset.', keyWindow: 'Open Trap Hub',
    keyWindowNote: 'Change the keys in the mod config: PanelKey, PanelKeyShift and CheckKey.', keyOff: 'Off', footWindow: 'this window',
    keyNone: 'No key is set; use the trap button on the left of the screen.',
    presetFile: 'Presets and allowed bait are kept in BepInEx/config/trap-hub.json, shared by every save.', escClose: 'Esc to close',
    tip: 'Your traps: catches, bait and wear',
    hEmpty: 'empty, will use', hNoBait: 'empty, no bait left', hWornOut: 'worn out, repair or replace', hFull: 'full, collect to restart', hLeft: '{0} left', hStored: 'Stored catch', hNextCheck: 'Next check:', hPrey: '{0} prey', hToBait: '{0} to bait', hWorn: '{0} about to break', hCollect: 'Collect {0}', hBaitWith: 'Bait with {0}', hRebaitOn: 'RE-BAIT IS ON', hRefill: 'Refill traps you empty', hRefillWith: 'refill with', hFoot: '{0} traps on {1} floors, this floor first', hIdle: 'Nothing to check: every trap is armed and baited.', hSkipBreaks: '{0}, {1} breaks on its next catch, so it is emptied but not re-baited.', hSkipNoBait: '{0}, {1} has no bait left, so it is skipped.', hSkipMore: '{0} more skipped.',
    hRebaitAll: 'Re-bait traps', hFillFoot: 'Traps getting bait now: {0}. Catches stay in the traps.',
    hFillIdle: 'Nothing to re-bait: every trap that takes bait has some.', hFillNone: 'Traps with no bait left to use: {0}.'
  };

  var state = {
    data: { ready: false, note: '' }, isOpen: false, isLive: false, key: '', badge: 0, tab: 'traps', sel: '', show: 'all', trapSearch: '',
    baitSel: 0, baitShow: 'owned', kind: '', search: '', presetId: '', draft: null, draftOf: '', pendingNewFrom: null, addSearch: '',
    armed: '', isTyping: false, isStale: false
  };
  var cat = { types: {}, baits: {}, prey: {}, baitList: [], typeList: [] };
  // Looked up for every row on every refresh, so they are maps built once per view rather than list scans.
  var allowedSet = {};
  var presetUses = {};
  var ui = null;
  var isBusy = false;
  var busyTimer = 0;
  // Bumped with each new catalog, so lines drawn from its names are redrawn when the names change.
  var catalogSeq = 0;
  var hasReportedHudPlaced = false;

  function say(key) {
    var words = window.__sltraphubWords || ENGLISH;
    var text = words[key] || ENGLISH[key] || key;

    for (var i = 1; i < arguments.length; i++) text = text.split('{' + (i - 1) + '}').join(String(arguments[i]));

    return text;
  }

  // A phrase with its number picked out, as in "3 need you": the number is bold, the rest is plain.
  function sayCount(key, count) {
    var parts = say(key).split('{0}');

    return put(el('span'), text(parts[0]), el('b', '', String(count)), text(parts.slice(1).join('{0}')));
  }

  function trapCount(count) {
    return count === 1 ? say('nTrap') : say('nTraps', count);
  }

  // Through the Toolkit's bridge. Nothing goes before its page API is there; the page says it is ready once it is.
  function send(message) {
    if (etk) etk.send(PREFIX, message);
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

  // A row of choices where one is on, like the game's tab strips; a choice may carry a count, and each sends its
  // value after the given prefix.
  function segment(choices, current, action, isEnabled, argsPrefix) {
    var seg = el('div', 'th-seg');

    choices.forEach(function (c) {
      seg.appendChild(put(plainButton('th-sg' + (c[0] === current ? ' th-on' : ''), c[1], action, (argsPrefix || '') + c[0], isEnabled !== false),
        c[2] != null ? el('span', 'th-n', String(c[2])) : null));
    });

    return seg;
  }

  function switchButton(isOn, action) {
    return act(el('button', 'th-tog' + (isOn ? ' th-on' : '')), action);
  }

  function tag(content, colorClass) {
    return el('span', 'th-pill' + (colorClass ? ' ' + colorClass : ''), content);
  }

  function section(title, right) {
    var head = el('div', 'th-sect', title);

    if (right) head.appendChild(right);

    return head;
  }

  function setRow(label, note, control) {
    return put(el('div', 'th-setrow'), put(el('div', 'th-lbl'), el('b', '', label), note ? el('small', '', note) : null), control);
  }

  function kv(grid, key, value) {
    put(grid, el('span', 'th-k', key), typeof value === 'string' ? el('b', '', value) : value);
  }

  function disableAll(scope, action) {
    scope.querySelectorAll('[data-act]').forEach(function (node) {
      if (node.getAttribute('data-act') === action) node.disabled = true;
    });
  }

  var CSS = [
    '#th-root{--ac:#FFC83A;--tx:#f0e8d8;--t2:#cbbfa8;--t3:#a89a82;--t4:#7a6e5a;--ln:rgba(220,188,120,.20);--ls:rgba(220,188,120,.11);--cf:rgba(70,58,44,.55);--gr:#8cc56a;--rd:#e57373;--or:#ffa726;--glow:0 0 0 1px rgba(255,200,58,.55),0 0 14px rgba(255,200,58,.22);position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(0,0,0,.45);font-family:"Noto Sans SC","Microsoft YaHei","PingFang SC",sans-serif;color:var(--tx);user-select:none;font-size:14px}',
    '#th-root *{box-sizing:border-box}',
    '#th-root button{font:inherit}',
    '.th-win{position:relative;width:min(1580px,96vw);height:min(920px,92vh);display:flex;flex-direction:column;padding:22px 26px 14px;border:1px solid rgba(220,188,120,.28);border-radius:4px;background:radial-gradient(ellipse at 15% 0%,rgba(150,100,40,.30),transparent 55%),linear-gradient(180deg,#3a3129 0%,#2b241d 55%,#241e18 100%);box-shadow:0 18px 60px rgba(0,0,0,.7),inset 0 1px 0 rgba(255,220,150,.08)}',
    '.th-win::before{content:"";position:absolute;left:24px;right:24px;top:0;height:2px;background:linear-gradient(90deg,transparent,var(--ac),transparent);opacity:.75}',
    '.th-whd{display:flex;align-items:center;gap:14px;padding-bottom:14px;border-bottom:1px solid var(--ln)}',
    '.th-logo{width:46px;height:46px;flex:none;border-radius:4px;display:flex;align-items:center;justify-content:center;background:linear-gradient(180deg,#ffd25a,#e8a91c);color:#2a1f10;box-shadow:0 2px 8px rgba(0,0,0,.4)}',
    '.th-logo svg{width:28px;height:28px;fill:currentColor}',
    '.th-ttl{font-size:26px;letter-spacing:6px;text-transform:uppercase;color:#fff;font-weight:700;line-height:1.1;text-shadow:0 2px 6px rgba(0,0,0,.5)}',
    '.th-ttl small{display:block;font-size:14px;letter-spacing:0;text-transform:none;color:var(--t3);font-weight:400;margin-top:4px;text-shadow:none}.th-ttl small b{color:var(--ac)}',
    '.th-tabs{margin-left:auto;display:flex;gap:6px}',
    '.th-tab{position:relative;padding:8px 20px;border:1px solid var(--ln);border-radius:3px;background:rgba(0,0,0,.18);color:var(--t2);font-size:15px;cursor:pointer;white-space:nowrap}',
    '.th-tab:hover{color:#fff;border-color:rgba(255,200,58,.45)}.th-tab.th-on{color:var(--ac);border-color:var(--ac);background:rgba(255,200,58,.13);box-shadow:var(--glow);font-weight:700}',
    '.th-tab .th-n{position:absolute;top:-9px;right:-9px;min-width:20px;height:20px;padding:0 6px;border-radius:10px;background:#d84a3a;color:#fff;font-size:12px;font-weight:700;line-height:20px;text-align:center}',
    '.th-x{width:36px;height:36px;flex:none;padding:0;border:1px solid var(--ln);border-radius:3px;background:rgba(0,0,0,.18);color:var(--t3);font-size:20px;line-height:32px;cursor:pointer}.th-x:hover{color:#fff;border-color:var(--ac)}',
    '.th-wbody{flex:1;min-height:0;display:flex;gap:18px;padding-top:14px}',
    '.th-main{flex:1;min-width:0;min-height:0;display:flex;flex-direction:column;gap:10px}',
    '.th-side{width:430px;flex:none;min-height:0;display:flex;flex-direction:column;gap:10px;padding:14px 16px;border:1px solid var(--ls);border-radius:3px;background:rgba(0,0,0,.18)}',
    '.th-c330{width:330px;flex:none;min-height:0;display:flex;flex-direction:column;gap:10px}.th-c330>.th-scroll{flex:1}',
    '.th-scroll{min-height:0;overflow-y:auto;padding-right:6px}',
    '.th-scroll::-webkit-scrollbar{width:6px}.th-scroll::-webkit-scrollbar-thumb{background:rgba(255,200,58,.35);border-radius:3px}',
    '.th-wfoot{display:flex;align-items:center;gap:16px;margin-top:12px;padding-top:10px;border-top:1px solid var(--ln);color:var(--t3);font-size:14px}',
    '.th-status{flex:1;min-width:0;color:var(--t2);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-kbd{display:inline-block;font-family:Consolas,monospace;font-size:13px;color:var(--tx);border:1px solid var(--ln);border-bottom-width:2px;border-radius:3px;padding:0 6px;margin-right:6px;background:rgba(0,0,0,.25)}',
    '.th-kbd.th-key{font-size:16px;padding:4px 10px;min-width:84px;text-align:center;flex:none}',
    '.th-sect{display:flex;align-items:center;gap:10px;font-size:15px;font-weight:700;letter-spacing:3px;text-transform:uppercase;color:var(--ac);margin:6px 0 8px}',
    '.th-sect::before{content:"";width:4px;height:16px;background:var(--ac);flex:none;border-radius:1px}',
    '.th-sect::after{content:"";flex:1;height:1px;background:linear-gradient(90deg,var(--ln),transparent);order:1}',
    '.th-sect .th-r{order:2;letter-spacing:0;text-transform:none;font-weight:400;font-size:13px;color:var(--t3)}',
    '.th-btn{display:inline-flex;align-items:center;justify-content:center;gap:6px;padding:8px 14px;border:1px solid var(--ln);border-radius:3px;background:linear-gradient(180deg,rgba(92,78,60,.9),rgba(66,55,42,.92));color:var(--tx);font-size:14px;cursor:pointer;white-space:nowrap}',
    '.th-btn:hover:not(:disabled){border-color:var(--ac);color:#fff}.th-btn:disabled{opacity:.4;cursor:default}',
    '.th-btn.th-sm{padding:4px 10px;font-size:13px}',
    '.th-btn.th-gold{background:linear-gradient(180deg,#ffd25a,#e8a91c);border-color:#ffd25a;color:#2a1f10;font-weight:700}.th-btn.th-gold:hover:not(:disabled){color:#000;box-shadow:var(--glow)}',
    '.th-btn.th-big{width:100%;padding:13px;font-size:18px}',
    '.th-btn.th-ghost{background:none;border-color:transparent;color:var(--t3);padding-left:0;align-self:flex-start}.th-btn.th-ghost:hover:not(:disabled){color:var(--ac);border-color:transparent}',
    '.th-btn.th-danger:hover:not(:disabled),.th-btn.th-armed{border-color:var(--rd);color:var(--rd)}',
    '.th-btn.th-ok{color:var(--gr)}.th-btn.th-no{color:var(--t3)}',
    '.th-seg{display:inline-flex;border:1px solid var(--ln);border-radius:3px;overflow:hidden;flex:none}',
    '.th-sg{padding:6px 14px;border:none;border-right:1px solid var(--ls);background:rgba(0,0,0,.18);color:var(--t2);font-size:14px;cursor:pointer;white-space:nowrap}.th-sg:last-child{border-right:none}.th-sg:hover:not(:disabled){color:#fff}.th-sg:disabled{opacity:.4;cursor:default}',
    '.th-sg.th-on{background:var(--ac);color:#2a1f10;font-weight:700}.th-sg .th-n{margin-left:6px;opacity:.7}',
    '.th-chips{display:flex;gap:6px;flex-wrap:wrap;margin-bottom:6px}',
    '.th-chip{display:inline-flex;align-items:center;gap:6px;padding:4px 10px;border-radius:3px;background:rgba(0,0,0,.2);border:1px solid var(--ln);color:var(--t2);font-size:13px;cursor:pointer;white-space:nowrap}',
    '.th-chip.th-ic{padding-left:4px}.th-chip:hover:not(:disabled){border-color:rgba(255,200,58,.6);color:#fff}.th-chip:disabled{opacity:.4;cursor:default}',
    '.th-chip.th-on{border-color:var(--ac);color:var(--ac);background:rgba(255,200,58,.12)}.th-chip .th-n{color:var(--ac);font-weight:600}',
    '.th-chip.th-add{border-style:dashed;color:var(--t3);background:none}',
    '.th-inp{font:inherit;font-size:14px;color:#fff;background:rgba(0,0,0,.35);border:1px solid var(--ln);border-radius:3px;padding:6px 10px;user-select:text}',
    '.th-inp:focus{outline:none;border-color:var(--ac)}.th-inp.th-small{font-size:13px;padding:4px 8px;width:100%}.th-inp.th-name{font-size:17px;font-weight:700;width:320px}',
    '.th-tog{width:40px;height:22px;padding:0;border-radius:11px;background:rgba(0,0,0,.35);border:1px solid var(--ln);position:relative;flex:none;cursor:pointer}',
    '.th-tog::after{content:"";position:absolute;top:3px;left:3px;width:14px;height:14px;border-radius:50%;background:var(--t4);transition:left .15s}',
    '.th-tog.th-on{background:rgba(255,200,58,.3);border-color:var(--ac)}.th-tog.th-on::after{left:21px;background:var(--ac)}',
    '.th-stepper{display:inline-flex;align-items:center;border:1px solid var(--ln);border-radius:3px;overflow:hidden;font-size:13px;flex:none}',
    '.th-stp{padding:1px 8px;color:var(--t3);background:rgba(0,0,0,.2);border:none;cursor:pointer}.th-stp:hover:not(:disabled){color:var(--ac)}.th-stp:disabled{opacity:.3;cursor:default}',
    '.th-stepper b{padding:1px 6px;color:var(--ac);font-weight:700;min-width:28px;text-align:center}',
    '.th-ib{width:20px;height:20px;padding:0;border-radius:3px;border:1px solid var(--ls);background:rgba(0,0,0,.2);color:var(--t3);font-size:11px;line-height:18px;text-align:center;cursor:pointer}',
    '.th-ib:hover:not(:disabled){border-color:var(--ac);color:var(--ac)}.th-ib:disabled{opacity:.2;cursor:default}.th-ib.th-del:hover{border-color:var(--rd);color:var(--rd)}',
    '.th-pill{display:inline-block;flex:none;padding:2px 9px;border-radius:10px;font-size:12px;white-space:nowrap;background:rgba(255,255,255,.08);color:var(--t2);font-weight:400}',
    '.th-p-ready{background:var(--ac);color:#2a1f10;font-weight:700}.th-p-urgent{background:rgba(229,115,115,.22);color:var(--rd)}.th-p-warn{background:rgba(255,167,38,.18);color:var(--or)}',
    '.th-p-good{background:rgba(140,197,106,.18);color:var(--gr)}.th-p-dim{background:rgba(255,255,255,.05);color:var(--t4)}',
    '.th-bar{display:inline-block;width:110px;height:6px;border-radius:3px;background:rgba(0,0,0,.4);overflow:hidden;flex:none}.th-bar i{display:block;height:100%;background:var(--gr)}',
    '.th-bar.th-gold i{background:var(--ac)}.th-bar.th-red i{background:var(--rd)}.th-bar.th-orange i{background:var(--or)}.th-bar.th-grey i{background:var(--t4)}.th-bar.th-wide{width:100%;height:8px}',
    '.th-tool{display:flex;align-items:center;gap:10px}.th-tool .th-inp{margin-left:auto;width:280px}',
    '.th-grp{margin-bottom:14px}',
    '.th-grp-h{display:flex;align-items:center;gap:10px;margin:0 2px 6px;font-size:13px;color:var(--t3)}.th-grp-h b{font-size:14px;letter-spacing:2px;text-transform:uppercase;color:#fff}',
    '.th-grid2{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px}',
    '.th-row{position:relative;display:flex;align-items:center;gap:12px;padding:8px 12px;border-radius:3px;background:var(--cf);border:1px solid transparent;cursor:pointer;min-height:70px}',
    '.th-row:hover{background:rgba(92,76,56,.7)}.th-row.th-sel-on{border-color:var(--ac);box-shadow:var(--glow)}',
    '.th-row.th-e-gold::before,.th-row.th-e-red::before,.th-row.th-e-or::before{content:"";position:absolute;left:0;top:6px;bottom:6px;width:3px;border-radius:2px;background:var(--ac)}',
    '.th-row.th-e-red::before{background:var(--rd)}.th-row.th-e-or::before{background:var(--or)}',
    '.th-mid{flex:1;min-width:0;display:flex;flex-direction:column;gap:4px}',
    '.th-nm{font-size:15px;color:#fff;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.th-nm small{font-size:12px;color:var(--t3);margin-left:8px}',
    '.th-meta{display:flex;align-items:center;gap:8px;font-size:13px;color:var(--t2);white-space:nowrap;overflow:hidden}',
    '.th-odds{margin-left:auto;display:flex;gap:8px;flex:none}.th-odd{display:inline-flex;align-items:center;gap:3px;font-size:12px;color:var(--t2)}',
    '.th-end{display:flex;flex-direction:column;align-items:flex-end;gap:5px;flex:none}',
    '.th-wh{font-size:13px;color:var(--t3);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-todo{display:flex;align-items:center;gap:12px;padding:9px 12px;border-radius:3px;background:var(--cf);margin-bottom:6px;cursor:pointer}.th-todo:hover{background:rgba(92,76,56,.7)}',
    '.th-todo .th-t{flex:1;min-width:0;font-size:15px;color:#fff}.th-todo .th-t small{display:block;font-size:12px;color:var(--t3);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-cnt{font-size:20px;font-weight:700;color:var(--ac);min-width:32px;text-align:right}.th-cnt.th-red{color:var(--rd)}.th-cnt.th-or{color:var(--or)}.th-cnt.th-dim{color:var(--t4)}',
    '.th-cta{margin-top:auto;display:flex;flex-direction:column;gap:8px;padding-top:10px;border-top:1px solid var(--ls)}.th-cap{font-size:13px;color:var(--t3);text-align:center}',
    '.th-subacts{display:flex;gap:8px}.th-subacts .th-btn{flex:1}',
    '.th-det-hd{display:flex;align-items:center;gap:14px;margin:4px 0 12px}.th-det-hd .th-nm{font-size:22px;font-weight:700}',
    '.th-kv{display:grid;grid-template-columns:120px minmax(0,1fr);gap:8px 12px;font-size:14px;margin-bottom:12px;align-items:center}.th-kv .th-k{color:var(--t3)}.th-kv b{font-weight:600;display:flex;gap:6px;align-items:center;flex-wrap:wrap}',
    '.th-kv .th-chips{margin:0}',
    '.th-orow{display:grid;grid-template-columns:22px minmax(0,1fr) 130px 44px;gap:8px;align-items:center;font-size:13px;padding:3px 0}.th-orow.th-nil{opacity:.55}',
    '.th-num{text-align:right;font-variant-numeric:tabular-nums}.th-ispace{width:22px}',
    '.th-nrow{display:flex;align-items:center;gap:10px;font-size:13px;padding:5px 8px;border-radius:3px}',
    '.th-nrow.th-next{background:rgba(140,197,106,.13)}.th-nrow.th-out{opacity:.45}.th-nrow.th-link{cursor:pointer}.th-nrow.th-link:hover{background:rgba(255,200,58,.10)}',
    '.th-ordn{width:16px;text-align:center;color:var(--t4);font-size:12px}',
    '.th-pcard{padding:10px 12px;border-radius:3px;background:var(--cf);border:1px solid transparent;margin-bottom:6px;cursor:pointer}.th-pcard:hover{background:rgba(92,76,56,.7)}',
    '.th-pcard.th-on{border-color:var(--ac);box-shadow:var(--glow)}.th-pcard .th-nm{font-size:16px;font-weight:700}',
    '.th-ics{display:flex;gap:3px;margin-top:6px}',
    '.th-trow{display:grid;grid-template-columns:30px minmax(0,1fr) auto;gap:8px;align-items:center;padding:5px 6px;border-bottom:1px solid var(--ls);font-size:13px;cursor:pointer}.th-trow:hover{background:rgba(255,255,255,.03)}',
    '.th-hint{font-size:12px;color:var(--t3);line-height:1.45;padding:2px}',
    '.th-edhd{display:flex;align-items:center;gap:10px;margin-bottom:10px}',
    '.th-fb{display:flex;flex-direction:column;gap:4px;min-width:0;padding:8px;border-radius:3px;background:rgba(0,0,0,.18);border:1px solid var(--ls);margin-bottom:8px}',
    '.th-it{display:grid;grid-template-columns:18px 30px minmax(0,1fr) auto;gap:8px;align-items:center;padding:5px 7px;border-radius:3px;background:var(--cf);border-left:2px solid transparent}',
    '.th-it.th-short{border-left-color:var(--rd)}.th-it.th-go{border-left-color:var(--gr)}.th-it.th-bad{opacity:.6;border-left-color:var(--rd)}',
    '.th-o{width:18px;height:18px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:rgba(255,200,58,.18);color:var(--ac);font-weight:700;font-size:11px}.th-go .th-o{background:rgba(140,197,106,.3);color:var(--gr)}',
    '.th-sinfo{min-width:0}.th-sinfo b{display:flex;gap:6px;align-items:center;font-size:13px;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-sinfo small{display:block;font-size:11px;color:var(--t3);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.th-sctl{display:flex;align-items:center;gap:8px}.th-moves{display:flex;gap:3px}',
    '.th-dash{padding:5px 8px;border:1px dashed var(--ln);border-radius:3px;color:var(--t3);font-size:12px}',
    '.th-picker{display:flex;flex-direction:column;gap:3px;padding:6px;border-radius:3px;background:rgba(0,0,0,.35);border:1px solid rgba(255,200,58,.4);margin-bottom:8px}',
    '.th-prow{display:flex;align-items:center;gap:8px;font-size:12px;padding:4px;border-radius:3px;cursor:pointer}.th-prow:hover{background:rgba(255,200,58,.12)}',
    '.th-bait{display:flex;align-items:center;gap:8px;padding:5px 8px;border-radius:3px;background:var(--cf);border:1px solid transparent;margin-bottom:4px;cursor:pointer;font-size:13px}',
    '.th-bait:hover{background:rgba(92,76,56,.7)}.th-bait.th-off .th-grow{color:var(--t3)}.th-bait.th-sel-on{border-color:var(--ac)}',
    '.th-q2{display:grid;grid-template-columns:1fr 1fr;gap:6px}.th-q2 .th-btn{white-space:normal;text-align:center;font-size:12px;padding:6px 8px}',
    '.th-pchips{display:flex;gap:6px;flex-wrap:wrap}.th-pchip{display:inline-flex;align-items:center;gap:5px;font-size:12px;padding:2px 8px 2px 2px;border-radius:3px;background:rgba(0,0,0,.2)}',
    '.th-setrow{display:flex;align-items:center;gap:14px;padding:10px 14px;border-radius:3px;background:var(--cf);margin-bottom:6px}',
    '.th-lbl{flex:1;min-width:0}.th-lbl b{display:block;font-size:14px;font-weight:600;color:#fff}.th-lbl small{display:block;font-size:12px;color:var(--t3);margin-top:2px}',
    '.th-icon{flex:none;display:flex;align-items:center;justify-content:center;border-radius:3px;background:rgba(0,0,0,.25);overflow:hidden;color:var(--t2);font-weight:600}',
    '.th-icon img{width:100%;height:100%;object-fit:contain}',
    '.th-i56{width:64px;height:64px;font-size:22px}.th-i44{width:46px;height:46px;font-size:16px}.th-i30{width:30px;height:30px;font-size:12px}.th-i22{width:22px;height:22px;font-size:11px}.th-i18{width:18px;height:18px;font-size:10px}',
    '.th-none{padding:24px;color:var(--t3);font-size:14px;text-align:center}',
    '.th-note{font-size:12px;color:var(--t3);padding:4px 2px 8px}',
    '.th-banner{padding:8px 12px;margin:6px 0;border-radius:3px;border:1px solid rgba(255,167,38,.55);background:rgba(255,167,38,.10);color:#ffcc80;font-size:13px}',
    '.th-lockmsg{padding:12px 16px;border-radius:3px;border:1px solid rgba(255,200,58,.6);background:rgba(255,200,58,.10);color:var(--ac);font-size:15px}',
    '.th-grow{flex:1;min-width:0}.th-dim{color:var(--t3);font-size:12px}.th-red{color:var(--rd)}.th-green{color:var(--gr)}.th-or{color:var(--or)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();

    var root = el('div');
    var win = el('div', 'th-win');
    var head = el('div', 'th-whd');
    var title = el('div', 'th-ttl', 'Trap Hub');
    var sum = el('small');
    var tabs = el('div', 'th-tabs');
    var body = el('div', 'th-wbody');
    var foot = el('div', 'th-wfoot');
    var status = el('div', 'th-status');
    var keys = el('div');
    var esc = el('div');

    root.id = 'th-root';
    title.appendChild(sum);
    put(head, put(el('div', 'th-logo'), cage(document)), title, tabs, plainButton('th-x', '\u00d7', 'close', null, true));
    put(foot, status, keys, esc);
    put(win, head, body, foot);
    root.appendChild(win);
    document.body.appendChild(root);

    root.addEventListener('click', onClick);
    root.addEventListener('input', onInput);
    root.addEventListener('focusin', onFocus);
    root.addEventListener('focusout', onBlur);
    root.addEventListener('keydown', onKey);
    // Clicks inside the window must not reach the game pages underneath; the mask itself closes nothing on purpose.
    root.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    // A broken icon path swaps to the first-letter placeholder; error events do not bubble, so capture them.
    root.addEventListener('error', function (e) {
      var img = e.target;

      if (img && img.tagName === 'IMG' && img.parentNode) img.parentNode.textContent = img.getAttribute('data-letter') || '?';
    }, true);

    ui = { root: root, win: win, sum: sum, tabs: tabs, body: body, status: status, keys: keys, esc: esc };
    sizeWindow();
  }

  // Drawn at the player's UI size (Exiled's Toolkit), once per window root. The full-screen mask always fills the
  // screen, so the window on it is what must fit. A window drawn before the Toolkit's page API is there is handed
  // over when the API arrives.
  function sizeWindow() {
    if (etk) etk.size(ui.root, { mod: UI_SIZE_NAME, fit: ui.win });
  }

  // One copy, which outlives a lost window root. A window drawn before the Toolkit's page API is there gets it when
  // the API arrives.
  function addStyle() {
    if (etk) etk.addStyle(document, 'th-style', CSS);
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
      fillBaitList();
    } else if (field === 'addsearch') {
      state.addSearch = e.target.value;
      fillAddList();
    } else if (field === 'trapsearch') {
      state.trapSearch = e.target.value;
      fillTrapList();
    }
  }

  function isTextBox(node) {
    return !!node && node.tagName === 'INPUT';
  }

  // ---------- data ----------

  function catalog(data) {
    cat = { types: {}, baits: {}, prey: {}, baitList: [], typeList: [] };
    catalogSeq++;

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

  // Up to a few distinct names, then how many more.
  function names(list) {
    var seen = [];

    list.forEach(function (n) {
      if (n && seen.indexOf(n) < 0) seen.push(n);
    });

    return seen.slice(0, MAX_NAMES).join(', ') + (seen.length > MAX_NAMES ? ' +' + (seen.length - MAX_NAMES) : '');
  }

  // What Check traps does to a trap: line up a walk to collect its catch, put bait in it, or both.
  function isToCollect(trap) { return !!trap.canTake && !trap.queued; }

  function isToBait(trap) { return !!trap.canBait && !!trap.next; }

  // An empty trap needs the player whatever state the game gives it: it catches nothing until it has bait.
  function needsYou(trap) {
    return trap.state !== 'armed' || !!trap.worn || !!trap.canBait || !!trap.canTake;
  }

  function needRank(trap) {
    return trap.canTake ? 0 : trap.canBait ? 1 : trap.worn ? 2 : trap.state === 'halted' ? 3 : 4;
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

    ui.rows = null;
    ui.side = null;
    ui.trapList = null;
    ui.baitList = null;
    ui.baitRows = null;
    ui.addList = null;
    renderHead();
    renderTabs();
    ui.body.textContent = '';

    if (state.tab === 'settings') renderSettings();
    else if (!state.data.ready) ui.body.appendChild(lockMessage(state.data.note));
    else if (state.tab === 'bait') renderBait();
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
    var node = el('div', 'th-scroll' + (className ? ' ' + className : ''));
    node.setAttribute('data-scroll', key);

    return node;
  }

  function lockMessage(content) {
    var box = el('div', 'th-main');
    box.appendChild(el('div', 'th-lockmsg', content || ''));

    return box;
  }

  function renderHead() {
    ui.sum.textContent = '';

    if (!state.data.ready) return;

    var need = traps().filter(needsYou).length;

    ui.sum.appendChild(text(trapCount(traps().length)));

    if (need) put(ui.sum, text(' \u00b7 '), sayCount('sumNeed', need));
  }

  function renderTabs() {
    ui.tabs.textContent = '';

    TABS.forEach(function (tab) {
      var node = act(el('button', 'th-tab' + (state.tab === tab ? ' th-on' : ''), say(TAB_WORD[tab])), 'tab', tab);

      if (tab === 'traps' && state.badge > 0) node.appendChild(el('span', 'th-n', String(state.badge)));

      ui.tabs.appendChild(node);
    });
  }

  // The footer names both keys: Check traps on its own, and the one for this window.
  function renderKeys() {
    var checkKey = state.data.checkKey || '';

    ui.keys.textContent = '';

    if (checkKey) put(ui.keys, el('span', 'th-kbd', checkKey), text(say('keyCheck')), state.key ? text(' \u00b7 ') : null);
    if (state.key) put(ui.keys, el('span', 'th-kbd', state.key), text(say('footWindow')));

    ui.esc.textContent = say('escClose');
  }

  function pickTab(tab) {
    if (TABS.indexOf(tab) < 0) return;

    state.tab = tab;
    render();
  }

  // ---------- Traps tab ----------

  function renderTraps() {
    var main = el('div', 'th-main');
    var search = el('input', 'th-inp');

    search.setAttribute('data-field', 'trapsearch');
    search.setAttribute('placeholder', say('findTrap'));
    search.value = state.trapSearch;
    ui.trapList = scroller('traps', 'th-grow');
    ui.side = el('div', 'th-side');
    put(main, put(el('div', 'th-tool'), segment(SHOWS.map(function (s) {
      return [s[0], say(s[1]), traps().filter(function (t) { return shows(t, s[0]); }).length];
    }), state.show, 'show'), search), ui.trapList);
    fillTrapList();
    fillSide();
    put(ui.body, main, ui.side);
  }

  function shows(trap, show) {
    switch (show) {
      case 'needs':
        return needsYou(trap);
      case 'armed':
        return !needsYou(trap);
      default:
        return true;
    }
  }

  function roomKey(trap) {
    return trap.floor + '|' + trap.room;
  }

  function matchesSearch(trap) {
    var query = state.trapSearch.trim().toLowerCase();

    if (!query) return true;

    var words = [typeOf(trap.type).name, trap.room || ''];

    if (trap.prey) words.push(preyOf(trap.prey).name);

    (trap.odds || []).forEach(function (o) { words.push(preyOf(o.prey).name); });

    return words.some(function (w) { return String(w).toLowerCase().indexOf(query) >= 0; });
  }

  // Rooms keep the order the plugin sends (floor, then room), so they read as the house does; inside a room the
  // traps that need the player come first.
  function fillTrapList() {
    var well = ui.trapList;

    if (!well) return;

    var shown = traps().filter(function (t) { return shows(t, state.show) && matchesSearch(t); });
    var groups = [];

    well.textContent = '';
    ui.rows = {};

    if (!traps().length) {
      well.appendChild(el('div', 'th-none', say('empty')));

      return;
    }

    if (!shown.length) {
      well.appendChild(el('div', 'th-none', state.show === 'needs' && !state.trapSearch.trim() ? say('allFine') : say('noMatch')));

      return;
    }

    shown.slice(0, MAX_ROWS).forEach(function (trap) {
      var key = roomKey(trap);
      var group = find(groups, function (g) { return g.key === key; });

      if (!group) groups.push(group = { key: key, first: trap, traps: [] });

      group.traps.push(trap);
    });

    groups.forEach(function (group) {
      var grid = el('div', 'th-grid2');

      sorted(group.traps).forEach(function (trap) { grid.appendChild(trapRow(trap)); });
      well.appendChild(put(el('div', 'th-grp'), groupHead(group), grid));
    });

    if (shown.length > MAX_ROWS) well.appendChild(el('div', 'th-note', say('more', shown.length - MAX_ROWS)));
  }

  function groupHead(group) {
    var trap = group.first;

    return put(el('div', 'th-grp-h'), el('b', '', trap.room || '?'), el('span', '', say('floor', trap.floor) + ' \u00b7 ' + trapCount(group.traps.length)),
      trap.floor === state.data.floor ? tag(say('here'), 'th-p-warn th-here') : null);
  }

  function sorted(list) {
    var keyed = list.map(function (trap, i) { return { trap: trap, i: i }; });

    keyed.sort(function (a, b) { return needRank(a.trap) - needRank(b.trap) || a.i - b.i; });

    return keyed.map(function (k) { return k.trap; });
  }

  function edgeClass(trap) {
    if (trap.state === 'prey') return ' th-e-gold';
    if (trap.state === 'nobait') return ' th-e-red';
    if (trap.worn || trap.state === 'halted') return ' th-e-or';

    return '';
  }

  function statePill(trap) {
    if (trap.state === 'prey') return tag(trap.prey ? preyOf(trap.prey).name : say('stPrey'), 'th-p-ready');
    if (trap.state === 'nobait') return tag(say('stNoBait'), 'th-p-urgent');
    if (trap.state === 'halted') return tag(say('stHalted'), 'th-p-dim');

    return tag(say('stArmed'), 'th-p-good');
  }

  function trapRow(trap) {
    var type = typeOf(trap.type);
    var row = act(el('div', 'th-row' + edgeClass(trap) + (trap.id === state.sel ? ' th-sel-on' : '')), 'pick', trap.id);
    var name = put(el('div', 'th-nm'), text(type.name), el('small', '', trap.room || '?'));
    var end = put(el('div', 'th-end'), statePill(trap), trap.worn ? tag(say('fBreaks'), 'th-p-warn') : null, trap.queued ? tag(say('queued'), 'th-p-good') : null);

    put(row, icon(type, 'th-i44'), put(el('div', 'th-mid'), name, put(baitLine(trap), topOdds(trap)), wearLine(trap)), end);
    ui.rows[trap.id] = row;

    return row;
  }

  // What is in the trap, or what a re-bait would put there.
  function baitLine(trap) {
    var line = el('div', 'th-meta');

    if (trap.prey) return put(line, el('span', 'th-green', say('caught', preyOf(trap.prey).name)));

    if (trap.bait) {
      var bait = baitOf(trap.bait);

      return put(line, icon(bait, 'th-i18'), el('span', '', bait.name));
    }

    if (trap.state === 'halted' || !trap.canBait) return line;
    if (!trap.next) return put(line, el('span', 'th-red', say('nextNone')));

    var next = baitOf(trap.next);

    return put(line, icon(next, 'th-i18'), el('span', 'th-or', say('nextShort', next.name)));
  }

  function topOdds(trap) {
    var odds = (trap.odds || []).slice(0, TOP_ODDS);

    if (!odds.length || trap.prey) return null;

    var box = el('span', 'th-odds');

    odds.forEach(function (o) { box.appendChild(put(el('span', 'th-odd'), icon(preyOf(o.prey), 'th-i18'), text(o.pct + '%'))); });

    return box;
  }

  function wearLine(trap) {
    var words = trap.catches != null ? (trap.worn ? say('breaks') : say('catches', trap.catches)) : trap.dur + ' / ' + trap.max;

    return put(el('div', 'th-meta'), durabilityBar(trap), el('span', trap.worn ? 'th-or' : 'th-dim', words));
  }

  function durabilityBar(trap, isWide) {
    var track = el('span', 'th-bar' + (isWide ? ' th-wide' : '') + (trap.worn ? ' th-red' : trap.dur * 2 < trap.max ? ' th-orange' : ''));
    var fill = el('i');

    fill.style.width = percentOf(trap.dur, trap.max) + '%';
    track.appendChild(fill);

    return track;
  }

  function percentOf(part, whole) {
    return whole > 0 ? Math.max(0, Math.min(100, Math.round(part * 100 / whole))) : 0;
  }

  // The right side: a picked trap's detail, else what needs the player and the Check traps button.
  function fillSide() {
    if (!ui.side) return;

    var trap = trapById(state.sel);

    ui.side.textContent = '';

    if (trap) trapDetail(ui.side, trap);
    else needsPanel(ui.side);
  }

  function todo(count, title, sub, icons, countClass) {
    var ics = icons && icons.length ? el('span', 'th-ics') : null;

    (icons || []).slice(0, MAX_NAMES).forEach(function (item) { ics.appendChild(icon(item, 'th-i22')); });

    return put(act(el('div', 'th-todo'), 'show', 'needs'), el('span', 'th-cnt' + (countClass ? ' ' + countClass : ''), String(count)),
      put(el('div', 'th-t'), text(title), sub ? el('small', '', sub) : null), ics);
  }

  function needsPanel(side) {
    var c = state.data.counts || {};
    var collect = traps().filter(isToCollect);
    var bait = traps().filter(isToBait);
    var noBait = traps().filter(function (t) { return t.canBait && !t.next; });
    var worn = traps().filter(function (t) { return t.worn; });
    var halted = traps().filter(function (t) { return t.state === 'halted'; });
    var floor = traps().filter(function (t) { return t.floor === state.data.floor; });
    var floorJobs = floor.filter(isToCollect).length + floor.filter(isToBait).length;
    var takeCount = c.take || 0;
    var baitCount = c.rebait || 0;
    var scroll = scroller('side', 'th-grow');
    var need = traps().filter(needsYou).length;
    var preyIcons = collect.filter(function (t) { return t.prey; }).map(function (t) { return preyOf(t.prey); });

    scroll.appendChild(section(say('needsYou'), el('span', 'th-r', trapCount(need))));

    if (collect.length) scroll.appendChild(todo(collect.length, say('tCollect'), names(preyIcons.map(function (p) { return p.name; })), preyIcons));
    if (bait.length) scroll.appendChild(todo(bait.length, say('tBait'), names(bait.map(function (t) { return roomTitle(t) + ': ' + baitOf(t.next).name; })), bait.map(function (t) { return baitOf(t.next); })));
    if (noBait.length) scroll.appendChild(todo(noBait.length, say('tNoBaitLeft'), say('tNoBaitLeftSub'), null, 'th-red'));
    if (worn.length) scroll.appendChild(todo(worn.length, say('tBreaks'), names(worn.map(function (t) { return typeOf(t.type).name + ', ' + (t.room || '?'); })), null, 'th-or'));
    if (halted.length) scroll.appendChild(todo(halted.length, say('stHalted'), names(halted.map(function (t) { return typeOf(t.type).name + ', ' + (t.room || '?'); })), null, 'th-dim'));
    if (!need) scroll.appendChild(el('div', 'th-none', say('allFine')));

    // The buttons stay pressable with nothing to do: a press then says so.
    put(side, scroll, put(el('div', 'th-cta'), button(say('checkAll', takeCount + baitCount), 'checkall', null, true, 'th-gold th-big'),
      el('div', 'th-cap', say('checkNote', takeCount, baitCount)),
      put(el('div', 'th-subacts'), button(say('checkFloor', floorJobs), 'checkfloor', null, true, 'th-sm'), button(say('takeOnly', takeCount), 'takeall', null, true, 'th-sm'),
        button(say('baitOnly', baitCount), 'rebaitall', null, true, 'th-sm'))));
  }

  function trapDetail(side, trap) {
    var type = typeOf(trap.type);
    var isArmed = state.armed === 'dismantle|' + trap.id;
    var scroll = scroller('detail', 'th-grow');
    var grid = el('div', 'th-kv');
    var main = trap.canTake ? button(say('take'), 'take', trap.id, !trap.queued, 'th-gold th-big') : button(say('rebait'), 'rebait', trap.id, !!trap.canBait, 'th-gold th-big');

    put(scroll, button('\u2039 ' + say('back'), 'back', null, true, 'th-ghost th-sm'),
      put(el('div', 'th-det-hd'), icon(type, 'th-i56'), put(el('div', 'th-mid'), el('div', 'th-nm', type.name), el('div', 'th-wh', roomTitle(trap))), statePill(trap)));

    put(scroll, section(say('oddsNow'), el('span', 'th-r', trap.chance + '%')), oddsList(trap), section(say('thisTrap')));
    kv(grid, say('inTrap'), inTrapLine(trap));
    kv(grid, say('preset'), presetChips(trap));

    if (trap.canBait) kv(grid, say('nextBait'), trap.next ? put(el('b'), icon(baitOf(trap.next), 'th-i22'), text(baitOf(trap.next).name), el('span', 'th-dim', say('own', owned(trap.next)))) : el('b', 'th-red', say('nextNone')));

    kv(grid, say('wear'), put(el('b'), durabilityBar(trap), el('span', trap.worn ? 'th-or' : 'th-dim', (trap.worn ? say('breaks') : trap.catches != null ? say('catches', trap.catches) : '') + ' (' + trap.dur + ' / ' + trap.max + ')')));

    scroll.appendChild(grid);

    if (trap.store) scroll.appendChild(el('div', 'th-note', say('store', trap.store)));

    var preset = presetById(trap.preset);

    if (preset) put(scroll, section(preset.name), stepRows(preset, trap));

    put(side, scroll, put(el('div', 'th-cta'), main,
      put(el('div', 'th-subacts'), trap.canTake ? button(say('rebait'), 'rebait', trap.id, !!trap.canBait, 'th-sm') : null,
        button(isArmed ? say('confirm') : say('dismantle'), 'dismantle', trap.id, true, 'th-sm th-danger' + (isArmed ? ' th-armed' : '')))));
  }

  function inTrapLine(trap) {
    if (trap.prey) return put(el('b'), icon(preyOf(trap.prey), 'th-i22'), el('span', 'th-green', preyOf(trap.prey).name), trap.queued ? tag(say('queued'), 'th-p-good') : null);
    if (trap.bait) return put(el('b'), icon(baitOf(trap.bait), 'th-i22'), text(baitOf(trap.bait).name));

    return el('b', 'th-dim', say('none'));
  }

  // The trap's preset: its type's default or one of its own.
  function presetChips(trap) {
    var box = el('div');
    var chips = el('div', 'th-chips');
    var fallback = defaultPresetFor(trap.type);

    chips.appendChild(chip(say('typeDefault', fallback ? fallback.name : say('none')), 'assign', '|' + trap.id, !trap.own));
    presets().forEach(function (p) { chips.appendChild(chip(p.name, 'assign', p.id + '|' + trap.id, trap.own && p.id === trap.preset)); });
    box.appendChild(chips);

    if (!state.data.saved) {
      disableAll(chips, 'assign');
      box.appendChild(el('div', 'th-dim', say('noSaveYet')));
    }

    return box;
  }

  function stepRows(preset, trap) {
    var rows = el('div');
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

    rows.appendChild(put(act(el('div', 'th-nrow th-link'), 'presetgo', preset.id), el('span', 'th-ordn', '\u203a'), el('span', 'th-grow th-dim', say('tabPresets'))));

    return rows;
  }

  // The chance of a catch at the next check, split by prey; the rest of the chance is shown as nothing.
  function oddsList(trap) {
    var odds = trap.odds || [];
    var box = el('div');
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
    var track = el('span', 'th-bar th-wide' + (extraClass ? ' th-grey' : ' th-gold'));
    var fill = el('i');

    fill.style.width = Math.max(0, Math.min(100, pct)) + '%';
    track.appendChild(fill);

    return put(el('div', 'th-orow' + extraClass), iconNode || el('span', 'th-ispace'), el('span', 'th-wh', name), track, el('span', 'th-num', pct + '%'));
  }

  // Picking only touches the rows involved and the side beside them; the list is not rebuilt.
  function pickTrap(id) {
    state.sel = state.sel === id ? '' : id;
    showPicked();
  }

  // From another tab: picks the trap and brings its row into view, clearing a filter that hides it.
  function gotoTrap(id) {
    var trap = trapById(id);

    if (!trap) return;

    state.sel = id;

    if (!shows(trap, state.show) || !matchesSearch(trap)) {
      state.show = 'all';
      state.trapSearch = '';
    }

    if (state.tab !== 'traps' || !ui.rows || !ui.rows[id]) {
      state.tab = 'traps';
      render();
    } else {
      showPicked();
    }

    var row = ui.rows && ui.rows[id];

    if (row && row.scrollIntoView) row.scrollIntoView({ block: 'nearest' });
  }

  function showPicked() {
    if (!ui.rows || !ui.side) {
      render();

      return;
    }

    Object.keys(ui.rows).forEach(function (key) { setClass(ui.rows[key], 'th-sel-on', key === state.sel); });
    fillSide();
  }

  // ---------- Bait tab ----------

  // Presets on the left, the one picked in the middle, and the allowed bait on the right.
  function renderBait() {
    syncDraft();

    var left = el('div', 'th-c330');
    var list = scroller('plist');
    var main = el('div', 'th-main');
    var editor = scroller('editor', 'th-grow');

    list.appendChild(section(say('tabPresets')));
    presets().forEach(function (p) { list.appendChild(presetCard(p)); });
    put(list, chip('+ ' + say('newPreset'), 'newpreset', null, false), section(say('typeUses')));
    cat.typeList.forEach(function (type) { list.appendChild(typeDefaultRow(type)); });
    left.appendChild(list);

    if (state.draft) fillEditor(editor);

    main.appendChild(editor);
    ui.side = el('div', 'th-side');
    fillAllowedSide();
    put(ui.body, left, main, ui.side);
  }

  function presetCard(p) {
    var icons = el('div', 'th-ics');
    var used = traps().filter(function (t) { return t.preset === p.id; }).length;
    var notes = [];

    p.steps.slice(0, 6).forEach(function (s) { icons.appendChild(icon(baitOf(s.item), 'th-i22')); });

    if ((p.types || []).length) notes.push(say('defaultFor') + ' ' + p.types.map(function (t) { return typeOf(t).name; }).join(', '));

    notes.push(trapCount(used));

    return put(act(el('div', 'th-pcard' + (p.id === state.presetId ? ' th-on' : '')), 'preset', p.id),
      el('div', 'th-nm', p.name), el('div', 'th-wh', notes.join(' \u00b7 ')), icons.firstChild ? icons : null);
  }

  // Each trap type and the preset it falls back to; a press opens that preset.
  function typeDefaultRow(type) {
    var preset = defaultPresetFor(type.id);
    var row = put(el('div', 'th-trow'), icon(type, 'th-i30'), el('span', 'th-wh', type.name), el('span', preset ? '' : 'th-dim', preset ? preset.name : say('none')));

    return preset ? act(row, 'preset', preset.id) : row;
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
    var name = el('input', 'th-inp th-name');
    var isArmed = state.armed === 'deletepreset|' + d.id;
    var steps = el('div', 'th-fb');
    var firstUsable = -1;

    name.setAttribute('data-field', 'name');
    name.setAttribute('placeholder', say('name'));
    name.value = d.name;

    d.steps.forEach(function (s, i) {
      if (firstUsable < 0 && isAllowed(s.item) && owned(s.item) > s.keep) firstUsable = i;
    });

    put(editor,
      put(el('div', 'th-edhd'), name, button(say('save'), 'savepreset', null, true, 'th-gold'),
        d.id ? button(isArmed ? say('confirm') : say('delete'), 'deletepreset', d.id, true, 'th-danger' + (isArmed ? ' th-armed' : '')) : null,
        el('span', 'th-grow'), tag(say('unsaved'), 'th-p-warn th-dirty')),
      section(say('steps'), el('span', 'th-r', say('stepsNote'))));
    markDirty(editor);

    if (!d.steps.length) steps.appendChild(el('div', 'th-note', say('noSteps')));

    d.steps.forEach(function (step, i) { steps.appendChild(stepEditor(step, i, d.steps.length, i === firstUsable)); });

    if (d.lastResort) steps.appendChild(el('div', 'th-dash', '\u221e  ' + say('lastResort')));

    put(editor, steps, section(say('addBait')), addPicker(),
      section(say('rules')),
      setRow(say('lastResort'), null, switchButton(d.lastResort, 'lastresort')),
      setRow(say('source'), null, segment([['bag', say('srcBag')], ['home', say('srcHome')]], d.source, 'source')),
      setRow(say('spoilFirst'), null, switchButton(d.spoilFirst, 'spoil')),
      setRow(say('auto'), say('autoNote'), segment(AUTOS.map(function (a) { return [a[0], say(a[1])]; }), d.auto, 'autoset')),
      setRow(say('defaultFor'), null, typeChips(d)));
    fillUses(editor);
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
    var row = el('div', 'th-it' + (!allowed ? ' th-bad' : have <= step.keep ? ' th-short' : isFirst ? ' th-go' : ''));
    var title = put(el('b'), el('span', '', bait.name), allowed ? null : tag(say('blocked'), 'th-p-urgent'));
    var info = put(el('div', 'th-sinfo'), title, el('small', have > step.keep ? '' : 'th-red', say('own', have) + ' \u00b7 ' + kindName(bait)), drawIcons(bait, 6));
    var stepper = put(el('div', 'th-stepper'), plainButton('th-stp', '-', 'keepless', i, step.keep > 0), el('b', '', String(step.keep)),
      plainButton('th-stp', '+', 'keepmore', i, step.keep < MAX_KEEP));
    var moves = put(el('div', 'th-moves'), plainButton('th-ib', '\u2191', 'stepup', i, i > 0), plainButton('th-ib', '\u2193', 'stepdown', i, i < count - 1),
      plainButton('th-ib th-del', '\u00d7', 'stepdel', i, true));
    var controls = put(el('div', 'th-sctl'), allowed ? null : button(say('allow'), 'allow', '1|' + step.item, true, 'th-sm'), el('span', 'th-dim', say('keep')), stepper, moves);

    return put(row, el('span', 'th-o', String(i + 1)), icon(bait, 'th-i30'), info, controls);
  }

  function drawIcons(bait, max) {
    var icons = el('div', 'th-ics');

    (bait.draws || []).slice(0, max).forEach(function (id) { icons.appendChild(icon(preyOf(id), 'th-i18')); });

    return icons.firstChild ? icons : null;
  }

  // A search box over every bait the game knows; owned and allowed baits come first.
  function addPicker() {
    var box = el('div', 'th-picker');
    var search = el('input', 'th-inp th-small');
    var list = el('div');

    search.setAttribute('data-field', 'addsearch');
    search.setAttribute('placeholder', say('search'));
    search.value = state.addSearch;
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
        tag(say(isAllowed(b.id) ? 'allowed' : 'blocked'), isAllowed(b.id) ? 'th-p-good' : 'th-p-urgent')));
    });

    if (!found.length) list.appendChild(el('div', 'th-dim', say('none')));
    if (found.length > MAX_ADD) list.appendChild(el('div', 'th-dim', say('more', found.length - MAX_ADD)));
  }

  // The traps the preset in the editor is on now, whether by their type's default or picked for them.
  function fillUses(well) {
    var using = traps().filter(function (t) { return t.preset === state.draft.id; });

    well.appendChild(section(say('usedBy'), el('span', 'th-r', String(using.length))));

    if (!using.length) well.appendChild(el('div', 'th-note', say('usedNone')));

    using.forEach(function (trap) {
      well.appendChild(put(act(el('div', 'th-trow'), 'goto', trap.id), icon(typeOf(trap.type), 'th-i30'),
        put(el('span', 'th-wh'), text(typeOf(trap.type).name + ' \u00b7 ' + roomTitle(trap))),
        tag(say(trap.own ? 'ownPick' : 'useDefault'), trap.own ? 'th-p-warn' : 'th-p-dim')));
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

  // ---------- Allowed bait ----------

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

  // The right side of the Bait tab: the allowed list with its filters and quick picks, or one bait picked from it.
  function fillAllowedSide() {
    var side = ui.side;
    var b = state.baitSel ? cat.baits[state.baitSel] : null;

    side.textContent = '';

    if (b) {
      baitDetail(side, b);

      return;
    }

    var search = el('input', 'th-inp th-small');
    var kindChips = el('div', 'th-chips');
    var inPresets = cat.baitList.filter(function (x) { return presetUses[x.id] && !isAllowed(x.id); }).map(function (x) { return x.id; });
    var universal = cat.baitList.filter(function (x) { return x.kind === 'universal' && owned(x.id) > 0 && !isAllowed(x.id); }).map(function (x) { return x.id; });

    search.setAttribute('data-field', 'search');
    search.setAttribute('placeholder', say('search'));
    search.value = state.search;
    KINDS.forEach(function (kind) { kindChips.appendChild(chip(say(KIND_WORD[kind]), 'kind', kind, state.kind === kind)); });
    ui.baitList = scroller('baitlist', 'th-grow');

    put(side, section(say('tabAllowed'), el('span', 'th-r', String((state.data.allowed || []).length))),
      showChips(), search, kindChips, ui.baitList,
      put(el('div', 'th-q2'),
        button(say('allowPresets') + ' (' + inPresets.length + ')', 'allow', '1|' + inPresets.join(','), inPresets.length > 0),
        button(say('allowUniversal') + ' (' + universal.length + ')', 'allow', '1|' + universal.join(','), universal.length > 0),
        shownButton(say('allowShown'), '1', ''),
        shownButton(say('blockShown'), '0', 'th-danger')),
      el('div', 'th-hint', say('allowedNote')));
    fillBaitList();
  }

  function showChips() {
    var box = el('div', 'th-chips');

    BAIT_SHOWS.forEach(function (s) {
      box.appendChild(chip(say(s[1]), 'filter', s[0], state.baitShow === s[0], cat.baitList.filter(function (b) { return baitShows(b, s[0]); }).length));
    });

    return box;
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

  // Owned bait first; Allow all shown and Block all shown act on every bait that matches, past the cut too.
  function fillBaitList() {
    var list = ui.baitList;

    if (!list) return;

    var shown = shownBaits();

    list.textContent = '';
    shown.slice(0, MAX_ROWS).forEach(function (b) { list.appendChild(baitRow(b)); });

    if (!shown.length) list.appendChild(el('div', 'th-none', say('noBaitMatch')));
    if (shown.length > MAX_ROWS) list.appendChild(el('div', 'th-note', say('more', shown.length - MAX_ROWS)));

    var ids = shown.map(function (b) { return b.id; }).join(',');

    ui.side.querySelectorAll('[data-shown]').forEach(function (node) {
      node.setAttribute('data-args', node.getAttribute('data-shown') + '|' + ids);
      node.disabled = shown.length === 0;
    });
  }

  function shownButton(content, flag, extraClass) {
    var node = button(content, 'allow', flag + '|', true, extraClass);
    node.setAttribute('data-shown', flag);

    return node;
  }

  function baitRow(b) {
    var isOn = isAllowed(b.id);
    var have = owned(b.id);

    return put(act(el('div', 'th-bait' + (isOn ? '' : ' th-off')), 'bait', b.id), icon(b, 'th-i30'),
      put(el('div', 'th-grow th-sinfo'), el('b', '', b.name), el('small', '', kindName(b) + (have ? ' \u00b7 ' + say('own', have) : ''))),
      button(say(isOn ? 'allowed' : 'blocked'), 'allow', (isOn ? '0|' : '1|') + b.id, true, 'th-sm ' + (isOn ? 'th-ok' : 'th-no')));
  }

  function pickBait(id) {
    state.baitSel = state.baitSel === +id ? 0 : +id;

    if (!ui.side || state.tab !== 'bait') {
      render();

      return;
    }

    fillAllowedSide();
  }

  // One bait: whether traps may use it, the presets that list it, what it draws in and which traps take it.
  function baitDetail(side, b) {
    var isOn = isAllowed(b.id);
    var uses = presetUses[b.id] || [];
    var scroll = scroller('baitdetail', 'th-grow');
    var takers = cat.typeList.filter(function (t) { return (t.accepts || []).indexOf(b.id) >= 0; });
    var takenChips = el('div', 'th-pchips');

    put(scroll, button('\u2039 ' + say('back'), 'baitback', null, true, 'th-ghost th-sm'),
      put(el('div', 'th-det-hd'), icon(b, 'th-i56'),
        put(el('div', 'th-mid'), el('div', 'th-nm', b.name), el('div', 'th-wh', kindName(b) + ' \u00b7 ' + say('own', owned(b.id)))),
        tag(say(isOn ? 'allowed' : 'blocked'), isOn ? 'th-p-good' : 'th-p-urgent')),
      section(say('inPresets')));

    if (!uses.length) scroll.appendChild(el('div', 'th-note', say('notInPresets')));

    uses.forEach(function (u) {
      scroll.appendChild(put(act(el('div', 'th-nrow th-link'), 'preset', u.preset.id), el('span', 'th-grow', say('presetStep', u.preset.name, u.step)),
        u.keep ? el('span', 'th-dim', say('keepN', u.keep)) : null));
    });

    if (uses.length && !isOn) scroll.appendChild(el('div', 'th-red', say('skipped')));

    if (!takers.length) takenChips.appendChild(el('span', 'th-dim', say('none')));

    takers.forEach(function (t) { takenChips.appendChild(put(el('span', 'th-pchip'), icon(t, 'th-i22'), el('span', '', t.name))); });

    put(scroll, section(say('drawsIn')), preyChips(b.draws || []), section(say('takenByHead')), takenChips);
    put(side, scroll, put(el('div', 'th-cta'), button(say(isOn ? 'block' : 'allow'), 'allow', (isOn ? '0|' : '1|') + b.id, true, isOn ? 'th-big th-danger' : 'th-big th-gold')));
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

  function keyCap(name) {
    return el('span', 'th-kbd th-key', name || say('keyOff'));
  }

  function renderSettings() {
    var data = state.data;
    var main = el('div', 'th-main');
    var well = scroller('settings', 'th-grow');
    var sounds = data.sounds || {};
    var isElsewhere = !!data.soundsElsewhere;

    well.style.maxWidth = '980px';
    put(well, section(say('keys')),
      setRow(say('keyCheck'), say('keyCheckNote'), keyCap(data.checkKey)),
      setRow(say('keyWindow'), state.key ? say('keyWindowNote') : say('keyNone'), keyCap(state.key)),
      section(say('sounds')), el('div', 'th-note', say('soundsNote')));

    if (isElsewhere) well.appendChild(el('div', 'th-banner', say('quietTraps')));

    SOUNDS.forEach(function (sound) {
      well.appendChild(setRow(say(sound[1]), null, segment(MODES.map(function (m) { return [m[0], say(m[1])]; }), sounds[sound[0]], 'sound', !isElsewhere, sound[0] + '|')));
    });

    put(well, section(say('kOther')), el('div', 'th-note', say('presetFile')));
    main.appendChild(well);
    ui.body.appendChild(main);
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

    // With the window closed the plugin shows the line as an Exiled's Toolkit toast instead.
    if (state.isOpen) ui.status.textContent = text;
  }

  // Whether a game with a home is loaded, and the key that opens the window ('' when none): the HUD button is only
  // shown in such a game, and both it and the window name the key.
  function live(isLive, key) {
    state.isLive = !!isLive;
    state.key = key || '';

    if (ui && state.isOpen) renderKeys();

    refreshHud();
  }

  // The number on the HUD button and the Traps tab: traps with a catch to take or waiting for bait.
  function badge(count) {
    state.badge = Math.max(0, count | 0);

    if (ui && state.isOpen) renderTabs();

    refreshHud();
  }

  // ---------- HUD button ----------

  // The Toolkit's page API (window.ExiledToolkit). The game can run its script after this one, so this stays null
  // until it has; the HUD button, the trap panel additions and the messages to the plugin start then.
  var etk = null;
  // The Trap Hub button in the HUD's tool column, drawn like the game's bag button. The Toolkit keeps it first in the
  // column and writes changes into it in place.
  var hudButton = null;
  // What the button was last told, so it is told again only when something changed.
  var shownHud = { isLive: null, key: null, badge: null, words: null };
  // The Toolkit's check of the HUD page, which hands that page to patchHud.
  var hudWatch = null;

  // Runs once the Toolkit's page API is there: at once when its script ran first, else when it runs.
  function startWithToolkit(api) {
    etk = api;

    // A window drawn before the Toolkit arrived gets its style and its size now.
    if (ui) {
      addStyle();
      sizeWindow();
    }

    hudButton = etk.addHudButton({
      id: HUD_CLASS, order: DOCK_ORDER, icon: CAGE_PATH, title: 'Trap Hub', badgeClass: 'th-hud-badge', shown: false,
      onClick: function () { send('toggle'); },
      onPlace: noteHudPlaced
    });
    showHudState();
    hudWatch = etk.watchPages(HUD_PAGE, patchHud, { everyCheck: true });
    send('ready');
  }

  // What the plugin sent changed what the HUD shows: the button hears it at once, and the trap panel is patched now
  // rather than at the next check.
  function refreshHud() {
    showHudState();

    if (hudWatch) hudWatch.refresh();
  }

  // The HUD page, handed over at every check, since Vue redraws the trap panel when it likes.
  function patchHud(doc) {
    showHudState();

    // Out of a game with everything already taken back out, there is nothing to put in or take out.
    if (!state.isLive && !doc.__thHasAdds) return;

    patchTrapPanel(doc);
  }

  // Shown only in a game, with the count, the key and the tip's words; each is written only when it changed.
  function showHudState() {
    if (!hudButton) return;

    var words = window.__sltraphubWords;

    if (shownHud.isLive === state.isLive && shownHud.key === state.key && shownHud.badge === state.badge && shownHud.words === words) return;

    shownHud = { isLive: state.isLive, key: state.key, badge: state.badge, words: words };
    hudButton.update({ shown: state.isLive, key: state.key, badge: state.badge, tip: say('tip') });
  }

  function noteHudPlaced() {
    if (!hasReportedHudPlaced) send('diag|HUD button placed in the tool column');
    hasReportedHudPlaced = true;
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

  // ---------- the game's trap panel ----------

  // The HUD's trap panel (CoreUI1.html, above the objectives): a toggle row that opens a list of every trap. Trap Hub
  // adds a Check traps button with its plan as the hover tip and a Re-bait box to the row, built like the game's
  // Tend Garden button and Replant box on the plants row above it; a line on top of the list with what the next check
  // does; and a second line under each trap with its bait, catch and wear. Vue owns that markup and redraws it when
  // it likes, so the additions are checked on every page poll and put back; none of them changes Vue's own nodes.
  var PANEL_STYLE_ID = 'th-panel-style';
  var GAME_ARMED_EN = 'deploying defenses';
  var JAWS_PATH = 'M1.5 11.2L4 9.8 6 12 8 10.4 10 12 12 9.8 14.5 11.2V12.6L8 14.2 1.5 12.6Z';
  var ARROW_PATH = 'M8 1.6V7.6M5.2 4.6L8 1.6 10.8 4.6';
  var DROP_PATH = 'M8 1.6V7.6M5.2 4.6L8 7.6 10.8 4.6';
  var PANEL_CSS = [
    '.trap-panel .trap-popover{max-width:340px}',
    '.trap-panel .trap-item-row{flex-wrap:wrap}',
    '.th-check-btn,.th-fill-btn{margin-left:4px;position:relative}',
    '.trap-toggle-row.is-expanded .th-check-btn .entry-tip,.trap-toggle-row.is-expanded .th-fill-btn .entry-tip{display:none}',
    // The HUD's top-left panel is a stacking context at z-index 100 and the tool column (.vertical-tools) sits at 101,
    // so nothing inside the panel can draw over the column whatever its own z-index. While the trap list is open or
    // the trap row is under the mouse, the panel is lifted just above the column.
    '.top-left-panel:has(.trap-toggle-row.is-expanded),.top-left-panel:has(.trap-panel:hover){z-index:102}',
    // A hovered toggle row gets a brightness filter, which makes it a stacking context of its own, so a tip inside it
    // would draw under any positioned element after the trap panel. A z-index on the hovered panel puts it on top.
    '.trap-panel:hover{z-index:1}',
    '.th-sub{flex-basis:100%;display:flex;align-items:center;gap:5px;padding-left:20px;font-size:9.5px;color:rgba(255,255,255,0.55);white-space:nowrap;text-shadow:1px 1px 2px black}',
    '.th-sub img,.th-strip img{width:11px;height:11px;object-fit:contain}',
    '.th-sub .th-sep{color:rgba(255,255,255,0.25)}',
    '.th-gold{color:#ffd54f}',
    '.th-red{color:#ef9a9a;font-weight:bold}',
    '.th-orange{color:#ffb74d}',
    '.th-sub .th-right{margin-left:auto;display:inline-flex;align-items:center;gap:4px}',
    '.th-wear{display:inline-block;width:26px;height:3px;border-radius:2px;background:rgba(255,255,255,0.15);overflow:hidden}',
    '.th-wear i{display:block;height:100%;background:#9ccc65}',
    '.th-wear.th-low i{background:#e57373}',
    '.th-strip{display:flex;align-items:center;gap:5px;padding:2px 4px 3px;font-size:9.5px;color:rgba(255,255,255,0.75);white-space:nowrap;text-shadow:1px 1px 2px black}',
    '.th-strip b{color:#ffd54f}',
    '.th-lnk{margin-left:auto;color:#ffd54f;border:1px solid rgba(255,193,7,0.45);border-radius:8px;padding:0 7px;cursor:pointer}',
    '.th-lnk:hover{background:rgba(255,193,7,0.16)}',
    '.entry-tip.th-plan{width:300px;max-width:330px;white-space:normal;left:-16px;transform:none;z-index:30}',
    '.th-plan-list{margin:5px 0 2px;display:grid;grid-template-columns:14px 1fr auto;gap:2px 6px;align-items:center;font-size:11px;color:#ddd}',
    '.th-plan-list img{width:13px;height:13px;object-fit:contain}',
    '.th-plan-list .th-n{color:#ffd54f;font-family:Consolas,monospace;text-align:right}',
    '.th-plan-room{color:#999}',
    '.th-plan-h{font-size:10px;color:rgba(255,193,7,0.85);letter-spacing:1px;margin-top:6px}',
    '.th-plan-foot{margin-top:5px;padding-top:4px;border-top:1px solid rgba(255,255,255,0.12);font-size:10.5px;color:#cfd3d5}',
    '.th-plan-foot b{color:#ffd54f}',
    '.th-plan-foot.th-warn{color:#ffb74d}'
  ].join('\n');

  var hudData = { ready: false };
  var hudById = {};
  // Each trap's data as sent, so a row's second line is redrawn only when its own trap changed.
  var hudKeyById = {};
  var hudSeq = 0;
  var seenWords = null;
  var wordsSeq = 0;
  var hasReportedVue = false;
  var hasReportedPanel = false;

  // What the plugin sends every couple of seconds for the panel (HudView): each trap's bait, catch and wear, and the
  // Check traps plan.
  function hud(data) {
    hudData = data && data.ready ? data : { ready: false };
    hudById = {};
    hudKeyById = {};
    (hudData.traps || []).forEach(function (t) {
      hudById[t.id] = t;
      hudKeyById[t.id] = JSON.stringify(t);
    });
    hudSeq++;
    refreshHud();
  }

  // What a row's second line is drawn from besides its trap: the catalog's names and the plugin's words, which both
  // change with the game's language.
  function lookSeq() {
    if (window.__sltraphubWords !== seenWords) {
      seenWords = window.__sltraphubWords;
      wordsSeq++;
    }

    return catalogSeq + '.' + wordsSeq;
  }

  // The HUD page's Vue state, which holds the trap rows' ids in the order the rows are drawn. Null if a game update
  // moved it, and then the rows get no second line; the button and the plan still work.
  function vueState(doc) {
    try {
      var app = doc.getElementById('app');
      var setup = app && app._vnode && app._vnode.component && app._vnode.component.setupState;

      return (setup && setup.state) || null;
    } catch (e) {
      return null;
    }
  }

  function patchTrapPanel(doc) {
    var row = doc.querySelector('.trap-toggle-row');

    if (!row || !state.isLive || !hudData.ready) {
      // Searched for only once after they were put in, not on every check while the panel is closed or still loading.
      if (doc.__thHasAdds) doc.querySelectorAll('.th-add').forEach(function (node) { node.remove(); });

      doc.__thHasAdds = false;

      return;
    }

    doc.__thHasAdds = true;

    var vue = vueState(doc);

    if (!vue && !hasReportedVue) send('diag|the HUD trap panel has no Vue state to read, so its rows get no second line');
    hasReportedVue = hasReportedVue || !vue;

    // The game's English for a baited trap waiting for prey reads as a battle term; Chinese and other words are left as they are.
    if (vue && String(vue.i18n_trapStatusArmed || '').trim().toLowerCase() === GAME_ARMED_EN) vue.i18n_trapStatusArmed = say('stArmed');

    addPanelStyle(doc);
    placeCheckControls(doc, row);

    var list = doc.querySelector('.trap-popover');

    if (list) {
      placeStrip(doc, list);
      placeRowLines(doc, list, vue);
    }

    if (!hasReportedPanel) send('diag|Check traps added to the HUD trap panel');
    hasReportedPanel = true;
  }

  // The button and box sit before the chevron, as the game's Tend Garden and Replant do on the plants row; they are
  // built once and only their tip and tick change, so a tip being read does not flicker away.
  function placeCheckControls(doc, row) {
    var button = row.querySelector('.th-check-btn');
    var fill = row.querySelector('.th-fill-btn');
    var box = row.querySelector('.th-rebait-chk');

    if (!button || !fill || !box) {
      if (button) button.remove();
      if (fill) fill.remove();
      if (box) box.remove();

      button = trapButton(doc, 'th-check-btn', ARROW_PATH, 'checkall');
      fill = trapButton(doc, 'th-fill-btn', DROP_PATH, 'rebaitall');
      box = rebaitBox(doc);

      var chevron = row.querySelector('.trap-toggle-chevron');

      row.insertBefore(button, chevron);
      row.insertBefore(fill, chevron);
      row.insertBefore(box, chevron);
    }

    if (button.getAttribute('data-version') !== String(hudSeq)) {
      var old = button.querySelector('.entry-tip');
      var oldFill = fill.querySelector('.entry-tip');

      if (old) old.remove();
      if (oldFill) oldFill.remove();

      button.appendChild(checkTip(doc));
      fill.appendChild(fillTip(doc));
      button.setAttribute('data-version', String(hudSeq));
    }

    var input = box.querySelector('input');

    if (input.checked !== !!hudData.rebait) input.checked = !!hudData.rebait;
  }

  // Check traps (an arrow out of the jaws) and Re-bait traps (an arrow into them), built like the game's Tend Garden.
  function trapButton(doc, className, arrowPath, action) {
    var node = doc.createElement('span');
    var svg = doc.createElementNS(SVG_NS, 'svg');
    var jaws = doc.createElementNS(SVG_NS, 'path');
    var arrow = doc.createElementNS(SVG_NS, 'path');

    node.className = 'mature-patrol-btn has-entry-tip ' + className + ' th-add';
    node.setAttribute('data-interactive', '');
    svg.setAttribute('viewBox', '0 0 16 16');
    svg.setAttribute('fill', 'currentColor');
    jaws.setAttribute('d', JAWS_PATH);
    arrow.setAttribute('d', arrowPath);
    arrow.setAttribute('stroke', 'currentColor');
    arrow.setAttribute('stroke-width', '1.7');
    arrow.setAttribute('stroke-linecap', 'round');
    arrow.setAttribute('fill', 'none');
    svg.appendChild(jaws);
    svg.appendChild(arrow);
    node.appendChild(svg);
    node.addEventListener('click', function (e) {
      e.stopPropagation();
      sendAction(action);
    });

    return node;
  }

  function rebaitBox(doc) {
    var label = doc.createElement('label');
    var input = doc.createElement('input');
    var words = doc.createElement('span');

    label.className = 'mature-replant-chk th-rebait-chk th-add';
    label.setAttribute('data-interactive', '');
    input.type = 'checkbox';
    words.textContent = say('rebait');
    label.appendChild(input);
    label.appendChild(words);
    // The click must not reach the toggle row, which would open or close the list.
    label.addEventListener('click', function (e) { e.stopPropagation(); });
    input.addEventListener('change', function () {
      hudData.rebait = !!input.checked;
      send('checkrebait|' + (input.checked ? '1' : '0'));
    });

    return label;
  }

  // The plan, from the same choices the press makes: what it collects and where, what it baits with, the refills when
  // Re-bait is on, how far it walks, and what it leaves.
  function checkTip(doc) {
    var tip = doc.createElement('div');
    var title = doc.createElement('div');
    var d = hudData;

    tip.className = 'entry-tip tip-below th-plan';
    title.className = 'entry-tip-title';
    title.textContent = say('keyCheck');

    if (d.key) title.appendChild(make(doc, 'span', 'entry-tip-key', d.key));

    tip.appendChild(title);

    var lines = (d.take || []).map(function (t) {
      var prey = t.prey > 0 ? preyOf(t.prey) : { name: say('hStored') };

      return planLine(doc, prey, say('hCollect', prey.name), t.room, t.n);
    })
      .concat((d.bait || []).map(function (b) { return planLine(doc, baitOf(b.bait), say('hBaitWith', baitOf(b.bait).name), b.room, b.n); }));

    if (lines.length) tip.appendChild(planList(doc, lines));

    if (d.rebait && (d.refill || []).length) {
      tip.appendChild(make(doc, 'div', 'th-plan-h', say('hRebaitOn')));
      tip.appendChild(planList(doc, d.refill.map(function (r) { return planLine(doc, baitOf(r.bait), say('hRefill'), baitOf(r.bait).name, r.n); })));
    }

    if (d.touched > 0) {
      tip.appendChild(fillFoot(doc, make(doc, 'div', 'th-plan-foot', ''), d.touched, d.floors));
    } else {
      tip.appendChild(make(doc, 'div', 'th-plan-foot', say('hIdle')));
    }

    (d.skips || []).forEach(function (s) {
      tip.appendChild(make(doc, 'div', 'th-plan-foot th-warn', say(s.why === 'breaks' ? 'hSkipBreaks' : 'hSkipNoBait', typeOf(s.type).name, s.room)));
    });

    if (d.skipMore > 0) tip.appendChild(make(doc, 'div', 'th-plan-foot th-warn', say('hSkipMore', d.skipMore)));

    return tip;
  }

  // What Re-bait traps will do, from the same choices the press makes: the bait each room's traps get, and how many
  // traps have none left. Catches are not collected.
  function fillTip(doc) {
    var tip = doc.createElement('div');
    var title = doc.createElement('div');
    var d = hudData;

    tip.className = 'entry-tip tip-below th-plan th-fill-plan';
    title.className = 'entry-tip-title';
    title.textContent = say('hRebaitAll');
    tip.appendChild(title);

    var lines = (d.fill || []).map(function (b) { return planLine(doc, baitOf(b.bait), say('hBaitWith', baitOf(b.bait).name), b.room, b.n); });

    if (lines.length) tip.appendChild(planList(doc, lines));

    tip.appendChild(make(doc, 'div', 'th-plan-foot', d.fillCount > 0 ? say('hFillFoot', d.fillCount) : say('hFillIdle')));

    if (d.fillNone > 0) tip.appendChild(make(doc, 'div', 'th-plan-foot th-warn', say('hFillNone', d.fillNone)));

    return tip;
  }

  // "5 traps on 4 floors, this floor first" with both numbers bold, whichever order the language puts them in.
  function fillFoot(doc, foot, touched, floors) {
    foot.textContent = '';

    say('hFoot').split(/(\{[01]\})/).forEach(function (part) {
      if (part === '{0}') foot.appendChild(make(doc, 'b', '', String(touched)));
      else if (part === '{1}') foot.appendChild(make(doc, 'b', '', String(floors)));
      else if (part) foot.appendChild(doc.createTextNode(part));
    });

    return foot;
  }

  function planList(doc, lines) {
    var list = make(doc, 'div', 'th-plan-list', '');

    lines.forEach(function (cells) { cells.forEach(function (cell) { list.appendChild(cell); }); });

    return list;
  }

  // One grid line: icon, words with the room greyed, count.
  function planLine(doc, item, words, room, count) {
    var middle = make(doc, 'span', '', words);

    if (room) middle.appendChild(make(doc, 'span', 'th-plan-room', ' \u00b7 ' + room));

    return [hudIcon(doc, item), middle, make(doc, 'span', 'th-n', String(count))];
  }

  // "Next check: 3 prey, 2 to bait, 1 about to break", and the way into the Trap Hub window.
  function placeStrip(doc, list) {
    var strip = list.querySelector('.th-strip');

    if (strip && strip.parentNode === list && strip.getAttribute('data-version') === String(hudSeq)) return;
    if (strip) strip.remove();

    var d = hudData;
    var link = make(doc, 'span', 'th-lnk', 'Trap Hub \u2197');

    strip = make(doc, 'div', 'th-strip th-add', say('hNextCheck'));
    strip.setAttribute('data-version', String(hudSeq));
    strip.appendChild(countPart(doc, 'hPrey', d.preyCount || 0, ''));
    strip.appendChild(make(doc, 'span', 'th-sep', '\u00b7'));
    strip.appendChild(countPart(doc, 'hToBait', d.baitCount || 0, ''));

    if (d.worn > 0) {
      strip.appendChild(make(doc, 'span', 'th-sep', '\u00b7'));
      strip.appendChild(countPart(doc, 'hWorn', d.worn, 'th-red'));
    }

    link.setAttribute('data-interactive', '');
    link.addEventListener('click', function (e) {
      e.stopPropagation();
      send('toggle');
    });
    strip.appendChild(link);
    list.insertBefore(strip, list.firstChild);
  }

  function countPart(doc, key, count, className) {
    var parts = say(key).split('{0}');
    var node = make(doc, 'span', className, parts[0]);

    node.appendChild(make(doc, 'b', '', String(count)));
    node.appendChild(doc.createTextNode(parts.slice(1).join('{0}')));

    return node;
  }

  // Rows are drawn in the order of the Vue state's trap list, so row i is that list's trap i. A line is redrawn only
  // when its own trap's data, the names or the words changed, so one trap changing does not redraw every row.
  function placeRowLines(doc, list, vue) {
    var traps = (vue && vue.traps) || [];
    var rows = list.querySelectorAll('.trap-item-row');
    var look = lookSeq();

    for (var i = 0; i < rows.length; i++) {
      var trap = traps[i];
      var data = trap ? hudById[String(trap.trapInstanceId)] : null;
      var key = data ? look + '|' + hudKeyById[data.id] : '';
      var sub = null;

      for (var c = 0; c < rows[i].children.length; c++) {
        if (rows[i].children[c].classList.contains('th-sub')) sub = rows[i].children[c];
      }

      if (sub && sub.getAttribute('data-key') === key) continue;
      if (sub) sub.remove();
      if (!data) continue;

      sub = rowLine(doc, data);
      sub.setAttribute('data-key', key);
      rows[i].appendChild(sub);
    }
  }

  // Bait (or what it gets next), catch, and catches left with a wear bar.
  function rowLine(doc, t) {
    var sub = make(doc, 'span', 'th-sub th-add', '');
    var hasNext = Object.prototype.hasOwnProperty.call(t, 'next');
    // A trap still holding its catch is not empty: its next bait goes after the catch, as a refill.
    var refill = t.prey > 0 && hasNext && t.next > 0 ? t.next : 0;

    if (t.halted) {
      sub.appendChild(make(doc, 'span', 'th-orange', say('hFull')));
    } else if (t.catches === 0) {
      sub.appendChild(make(doc, 'span', 'th-red', say('hWornOut')));
    } else if (t.bait > 0) {
      sub.appendChild(hudIcon(doc, baitOf(t.bait)));
      sub.appendChild(make(doc, 'span', '', baitOf(t.bait).name));
    } else if (refill) {
      // Shown after the catch below.
    } else if (hasNext && t.next > 0) {
      sub.appendChild(make(doc, 'span', 'th-orange', say('hEmpty')));
      sub.appendChild(hudIcon(doc, baitOf(t.next)));
      sub.appendChild(make(doc, 'span', 'th-gold', baitOf(t.next).name));
    } else if (hasNext) {
      sub.appendChild(make(doc, 'span', 'th-orange', say('hNoBait')));
    }

    if (t.prey > 0 || t.store > 0) {
      if (sub.children.length) sub.appendChild(make(doc, 'span', 'th-sep', '\u00b7'));

      if (t.prey > 0) {
        sub.appendChild(hudIcon(doc, preyOf(t.prey)));
        sub.appendChild(make(doc, 'span', 'th-gold', preyOf(t.prey).name));
      } else {
        sub.appendChild(make(doc, 'span', 'th-gold', say('hStored') + ' \u00d7' + t.store));
      }
    }

    if (refill) {
      sub.appendChild(make(doc, 'span', 'th-sep', '\u00b7'));
      sub.appendChild(make(doc, 'span', '', say('hRefillWith')));
      sub.appendChild(hudIcon(doc, baitOf(refill)));
      sub.appendChild(make(doc, 'span', 'th-gold', baitOf(refill).name));
    }

    if (t.catches != null) {
      var right = make(doc, 'span', 'th-right', '');
      var low = t.catches <= 1;
      var bar = make(doc, 'span', 'th-wear' + (low ? ' th-low' : ''), '');
      var fill = doc.createElement('i');

      fill.style.width = Math.max(0, Math.min(100, Math.round(t.catches / Math.max(1, t.total) * 100))) + '%';
      bar.appendChild(fill);
      right.appendChild(bar);
      right.appendChild(make(doc, 'span', low ? 'th-red' : '', say('hLeft', t.catches)));
      sub.appendChild(right);
    }

    return sub;
  }

  // An item's picture, or nothing when the game has none. Icon paths are relative to the root document's folder and
  // the HUD page sits in a folder of its own, so they are made absolute first.
  function hudIcon(doc, item) {
    var img = doc.createElement('img');

    if (item && item.icon) img.src = rootUrl(item.icon);
    else img.style.visibility = 'hidden';

    return img;
  }

  function rootUrl(path) {
    if (/^([a-z]+:|\/)/i.test(path)) return path;

    return String(location.href || '').replace(/[^\/]*([?#].*)?$/, '') + path;
  }

  function addPanelStyle(doc) {
    etk.addStyle(doc, PANEL_STYLE_ID, PANEL_CSS);
  }

  function make(doc, tag, className, words) {
    var node = doc.createElement(tag);

    if (className) node.className = className;
    if (words) node.textContent = words;

    return node;
  }

  window.__sltraphub = { open: open, setData: setData, close: close, status: status, live: live, catalog: catalog, badge: badge, hud: hud };
  (window.ExiledToolkitReady = window.ExiledToolkitReady || []).push(startWithToolkit);
})();
