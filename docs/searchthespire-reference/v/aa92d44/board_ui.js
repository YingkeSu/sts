// Renders the run-start board from state; all board DOM lives here.
// Full re-render per change: the board is a few dozen click-driven elements,
// and the only text input (picker search) lives inside the modal.
import {
  SLOTS, charactersFor, slotEnabled, raresEnabled,
  KALEIDO_RARES_OPTIONS, tablesFor,
  REWARD_PICK_IDS, packageFloor,
  SHOP_PICK_IDS, EVENT_PICK_IDS, shopFloor, eventFloor, bufTarget, withPickWithin,
  BAG_PICK_IDS, bagFloor,
  charPatch, leavesOf,
} from "./board.js";
import { openPicker } from "./picker.js";
import { COPY } from "./gen_copy.js";
import { renderDesc } from "./desc.js";

// Name + rendered description + art for one pool entry, under the PINNED
// version's wording. Exported because the run viewer wants the same tooltip
// text on its card/relic chips, and a second copy would drift from the
// override + DynamicVar handling below.
export const entryFor = (manifest, poolKind, id, state) => {
  const e = manifest[poolKind][id];
  // manifest text is the CURRENT install's (beta); versions whose wording
  // diverged carry overrides in their version dir (desc_overrides.js).
  const t = tablesFor(state);
  const text = t.descOverrides?.[poolKind]?.[id] ?? e.description;
  const desc = text && renderDesc(text, t.descVars.DESC_VARS[poolKind]?.[id]);
  return { id, name: e.title ?? id, desc, img: e.img };
};

// Board language for a slot value: game art + display name where the pool has
// art (pickers, bosses), the select's option label otherwise. Shared by the
// widen chips (index.html) and the Popular tab (trending.js).
// NOTE: slot.options is a FUNCTION for most slots but a PLAIN ARRAY for the
// act slot (board.js:409) — the old index.html version called it
// unconditionally, a latent bug the widen path never hit for act. Handle both.
export function slotDisplay(slot, value, state, manifest) {
  const entry = slot.poolKind ? manifest[slot.poolKind]?.[value] : null;
  const opts = typeof slot.options === "function" ? slot.options(state) : slot.options;
  const opt = opts?.find(([id]) => id === value);
  return { img: entry?.img, name: opt?.[1] ?? entry?.title ?? value };
}

// The engine's "would this pick make the spec unsatisfiable?" probe, injected
// by index.html per render (this module stays wasm-free). Render is
// synchronous, so a module-level slot is safe. Returns the reason text or
// null; every option surface below greys a non-null answer BEFORE it can be
// picked — the engine-sourced replacement for the deleted board-side
// CURSE_BLOCKERS rule, generalized to every provable contradiction.
let probe = () => null;

export function renderBoard(container, state, manifest, onChange, probeUnsat) {
  probe = probeUnsat ?? (() => null);
  const set = (patch) => onChange({ ...state, ...patch });
  container.innerHTML = "";

  // --- run setup: first rail card — nearly every card/relic/potion filter
  // narrows by character, so the character pick reads as step one ---
  const setup = group(container, "run setup");
  const charPick = document.createElement("div");
  charPick.className = "artgrid";
  charPick.id = "char";
  // Display order only — charactersFor(state) order is the game's
  // character-index order (inspect_seed charIdx) and must not be reordered.
  for (const c of ["", "ironclad", "silent", "regent", "necrobinder", "defect"]) {
    const e = manifest.characters[c || "any"];
    const b = document.createElement("button");
    b.className = "artcell" + (state.char === c ? " on" : "");
    b.dataset.val = c;
    b.setAttribute("aria-pressed", String(state.char === c));
    b.title = e.description ? `${e.title}\n${e.description}` : e.title;
    const img = document.createElement("img");
    img.src = `assets/${e.img}`;
    img.alt = "";
    b.append(img, labelSpan(c ? c[0].toUpperCase() + c.slice(1) : "Any"));
    // Re-clicking the active character clears it (back to any) — the picker has
    // no other affordance for undoing a pick.
    b.addEventListener("click", () => set(charPatch(state, state.char === c ? "" : c)));
    charPick.append(b);
  }
  const asc = manifest.ui.ascension;
  const ascWrap = document.createElement("label");
  ascWrap.id = "ascension";
  ascWrap.className = "ascension" + (state.ascension > 0 ? " on" : "");
  ascWrap.title = `${asc.title}\n${asc.description}\n${COPY.ascensionNote}`;
  // The game's ascension flame, badged with the level the way NAscensionPanel
  // labels it. Lit from A1 up, because A1 is the first level that changes
  // anything the app computes.
  const flame = document.createElement("span");
  flame.className = "ascicon";
  const flameImg = document.createElement("img");
  flameImg.src = `assets/${asc.img}`;
  flameImg.alt = "";
  const lvl = document.createElement("span");
  lvl.className = "asclvl";
  lvl.textContent = String(state.ascension);
  flame.append(flameImg, lvl);
  const sel = document.createElement("select");
  sel.className = "ascsel";
  sel.setAttribute("aria-label", COPY.ascensionLabel);
  // maxAscensionAllowed = 10 (AscensionManager.cs:15).
  for (let i = 0; i <= 10; i++) {
    const o = document.createElement("option");
    o.value = String(i);
    o.textContent = i === 0 ? "none" : `A${i}`;
    o.selected = i === state.ascension;
    sel.append(o);
  }
  sel.addEventListener("change", () => set({ ascension: Number(sel.value) }));
  ascWrap.append(flame, COPY.ascensionLabel, sel);
  setup.append(charPick, ascWrap);
  if (!state.char) {
    const hint = document.createElement("span");
    hint.className = "subhint wide";
    hint.textContent = COPY.needChar;
    setup.append(hint);
  }

  // --- neow: top-level picker slots, children bracketed under parents ---
  const neow = group(container, "neow");
  for (const slot of SLOTS.filter((s) => s.kind === "picker" && !s.parent && s.group === "neow")) {
    neow.append(slotCol(slot, state, manifest, set));
  }

  // --- card package: the multiset of cards that must all appear within
  // the first N fight rewards; exact-fight pins behind a disclosure. ---
  const pkg = group(container, "card rewards", "col");
  const pkgCap = document.createElement("p");
  pkgCap.className = "subhint";
  pkgCap.textContent =
    COPY.cardRewardsCaption;
  pkg.append(pkgCap);
  pkg.append(labelSpan(SLOTS.find((s) => s.id === "rewardPick1").label),
    pickRow(REWARD_PICK_IDS, state, manifest, set));
  if (REWARD_PICK_IDS.some((id) => state[id] !== null)) {
    const floor = packageFloor(state);
    const within = document.createElement("select");
    within.dataset.slot = "rewardWithin";
    for (const n of [1, 2, 3, 4, 5, 6]) {
      const o = new Option(
        n === 1 ? "within the first fight" : `within the first ${n} fights`, String(n));
      o.disabled = n < floor;
      within.add(o);
    }
    within.className = "wide";
    // NEVER set() during render: index.html's rerender is synchronously
    // re-entrant (innerHTML = "" then re-append) and the stale outer render
    // would duplicate sections. Render the clamped value; packageEmit
    // clamps identically at compile time.
    within.value = String(Math.max(state.rewardWithin, floor));
    within.addEventListener("change", () => set({ rewardWithin: Number(within.value) }));
    const ordered = document.createElement("label");
    const obox = document.createElement("input");
    obox.type = "checkbox";
    obox.dataset.slot = "rewardOrdered";
    obox.checked = state.rewardOrdered;
    obox.addEventListener("change", () => set({ rewardOrdered: obox.checked }));
    ordered.append(obox, " in this order");
    pkg.append(within, ordered);
  }
  // Exact-fight pins (the old per-fight reward rows) behind a disclosure.
  const exact = document.createElement("details");
  exact.className = "exact";
  const exactSummary = document.createElement("summary");
  exactSummary.textContent = "exact fight";
  exact.append(exactSummary);
  // A set pin must be visible — auto-open the disclosure (render is fresh
  // each pass, so an empty details would silently hide reconstructed pins).
  exact.open = [1, 2, 3].some((n) => state[`reward${n}`] !== null);
  for (const n of [1, 2, 3]) {
    const rslot = SLOTS.find((s) => s.id === `reward${n}`);
    const row = document.createElement("div");
    row.className = "slotcol";
    row.append(labelSpan(rslot.label),
      slotEnabled(rslot, state) ? slotButton(rslot, state, manifest, set) : needCharSlot(rslot.id));
    exact.append(row);
  }
  pkg.append(exact);

  // --- relics: the two channels a run hands them out on, one panel. They stay
  // separate CONTROLS because they are different mechanisms with different
  // `within` meanings (position among shops vs position within a rarity), but
  // two panels for two lone slots was all chrome. Shop first: it is filtered in
  // 41% of sessions against the reward channel's 6.5% (telemetry, 21d).
  const shops = group(container, "relics", "col");
  shops.append(rowLabel("from a shop"));
  const shopCap = document.createElement("p");
  shopCap.className = "subhint";
  shopCap.textContent = COPY.shopsCaption;
  shops.append(shopCap);
  const srSlot = SLOTS.find((s) => s.id === "shopPick1");
  shops.append(labelSpan(srSlot.label), pickRow(SHOP_PICK_IDS, state, manifest, set));
  if (SHOP_PICK_IDS.some((id) => state[id] !== null)) {
    const floor = shopFloor(state);
    const within = document.createElement("select");
    within.dataset.slot = "shopWithin";
    for (const n of [1, 2, 3, 4, 5, 6]) {
      const o = new Option(
        n === 1 ? "in the first shop" : `in any of first ${n} shops`, String(n));
      o.disabled = n < floor;
      within.add(o);
    }
    within.className = "wide";
    within.value = String(Math.max(state.shopWithin, floor));
    within.addEventListener("change", () => set({ shopWithin: Number(within.value) }));
    shops.append(within);
  }

  // --- relic rewards: the top of each rarity's player deque (multi-pick).
  // Shares the panel above rather than opening its own.
  const bag = shops;
  bag.append(rowLabel("as a reward"));
  const bagCap = document.createElement("p");
  bagCap.className = "subhint";
  bagCap.textContent = COPY.bagCaption;
  bag.append(bagCap);
  const bagSlot = SLOTS.find((s) => s.id === "bagPick1");
  bag.append(labelSpan(bagSlot.label), pickRow(BAG_PICK_IDS, state, manifest, set));
  if (BAG_PICK_IDS.some((id) => state[id] !== null)) {
    const floor = bagFloor(state);
    const within = document.createElement("select");
    within.dataset.slot = "bagWithin";
    for (const n of [1, 2, 3, 4, 5, 6]) {
      const o = new Option(
        n === 1 ? "next of its rarity" : `in the next ${n} of its rarity`, String(n));
      o.disabled = n < floor;
      within.add(o);
    }
    within.className = "wide";
    within.value = String(Math.max(state.bagWithin, floor));
    within.addEventListener("change", () => set({ bagWithin: Number(within.value) }));
    bag.append(within);
    const capsuleHint = document.createElement("p");
    capsuleHint.className = "subhint";
    capsuleHint.textContent = COPY.bagCapsuleHint;
    bag.append(capsuleHint);
  }

  // --- events: act-1 ? rooms serve a seed-determined event queue (multi-pick) ---
  const events = group(container, "events", "col");
  const evCap = document.createElement("p");
  evCap.className = "subhint";
  const qroom = () => {
    const i = document.createElement("img");
    i.className = "qroom";
    i.src = `assets/${manifest.map.mapunknown.img}`;
    i.alt = "?";
    return i;
  };
  evCap.append(`${COPY.eventsCaption1} `, qroom(), ` ${COPY.eventsCaption2} `,
    qroom(), ` ${COPY.eventsCaption3}`);
  events.append(evCap);
  const evSlot = SLOTS.find((s) => s.id === "eventPick1");
  events.append(labelSpan(evSlot.label), pickRow(EVENT_PICK_IDS, state, manifest, set));
  if (EVENT_PICK_IDS.some((id) => state[id] !== null)) {
    const floor = eventFloor(state);
    const rec = bufTarget(floor);
    const within = document.createElement("select");
    within.dataset.slot = "eventWithin";
    for (let n = floor; n <= 5; n++) {
      const base = n === 1 ? "in your first ? room" : `within your first ${n} ? rooms`;
      const suffix = n === rec ? ` ${COPY.eventRecommended}`
        : (floor >= 2 && n === floor) ? ` ${COPY.eventExact}` : "";
      within.add(new Option(base + suffix, String(n)));
    }
    within.className = "wide";
    within.value = String(Math.max(state.eventWithin, floor));
    within.addEventListener("change", () => set({ eventWithin: Number(within.value) }));
    events.append(within);
    if (floor >= 2) {
      const buf = document.createElement("p");
      buf.className = "subhint";
      buf.textContent = COPY.eventBufferHint;
      events.append(buf);
    }
    for (const id of EVENT_PICK_IDS) {
      if (state[id] === null) continue;
      const cond = tablesFor(state).mirror.EVENT_CONDITIONS[state[id]];
      if (!cond) continue;
      const hint = document.createElement("p");
      hint.className = "subhint";
      hint.textContent = COPY.eventCondition.replace("{cond}", cond);
      events.append(hint);
    }
  }

  // --- route: act-1 map toggle + bosses (a boss1 pick implies the map) ---
  // --- ancients: top-level select slots (no art), always visible ---
  // Above route: ancients are picked more often than bosses or the act-1 map,
  // so they earn the shorter scroll.
  const ancients = group(container, "ancients", "col");
  for (const slot of SLOTS.filter((s) => !s.parent && s.group === "ancients")) {
    ancients.append(slotCol(slot, state, manifest, set));
  }

  const route = group(container, "route", "col");
  const actSlot = SLOTS.find((s) => s.id === "act");
  const actRow = document.createElement("div");
  actRow.className = "ctlrow";
  actRow.append(labelSpan("Act 1 map"));
  const toggle = document.createElement("div");
  toggle.className = "toggle";
  toggle.dataset.slot = "act";
  for (const [v, label] of [...actSlot.options, [null, "any"]]) {
    const b = document.createElement("button");
    b.textContent = label;
    b.classList.toggle("on", state.act === v);
    // Option gating: a map pick that contradicts a pinned map-implying read
    // (boss1, an act-1 event) greys with the engine's cross-map reason.
    const blocked = v !== null && state.act !== v ? probe({ act: v }) : null;
    if (blocked) {
      b.classList.add("blocked");
      b.title = blocked;
    }
    b.addEventListener("click", () => {
      if (blocked) return;
      set({ act: v });
    });
    toggle.append(b);
  }
  actRow.append(toggle);
  route.append(actRow);
  for (const slot of SLOTS.filter((s) => !s.parent && s.group === "bosses")) {
    route.append(slotCol(slot, state, manifest, set));
  }
}

// Top-level selects (route/ancients) render as label-left control-right rows
// that span the rail; everything else is a label-over-control column.
function slotCol(slot, state, manifest, set) {
  const col = document.createElement("div");
  const asRow = slot.kind === "select" && !slot.parent;
  col.className = asRow ? "slotrow" : "slotcol";
  if (asRow) {
    const row = document.createElement("div");
    row.className = "ctlrow";
    row.append(labelSpan(slot.label), slotSelect(slot, state, set));
    col.append(row);
  } else {
    col.append(labelSpan(artLabel(slot, state, manifest)), slot.kind === "select"
      ? slotSelect(slot, state, set)
      : slot.kind === "art"
        ? slotArt(slot, state, manifest, set)
        : slotButton(slot, state, manifest, set));
  }
  // Per-slot note under the control (e.g. the named precondition of a
  // conditional ancient offer).
  const note = slot.hint?.(state);
  if (note) {
    const p = document.createElement("p");
    p.className = "subhint";
    p.textContent = note;
    col.append(p);
  }
  const children = SLOTS.filter((s) => s.parent === slot.id && slotEnabled(s, state));
  // Slots tagged with a cluster (the bones per-grant narrowers) group into a
  // labeled sub-panel at the first member's position, so "what narrows which
  // grant" reads at a glance instead of as one flat wrap.
  const kidCols = [];
  const clusters = new Map();
  for (const c of children) {
    const col = slotCol(c, state, manifest, set);
    if (!c.cluster) {
      kidCols.push(col);
      continue;
    }
    if (!clusters.has(c.cluster)) {
      const box = document.createElement("div");
      box.className = "cluster";
      const lbl = document.createElement("span");
      lbl.className = "clusterlbl";
      lbl.textContent = c.cluster;
      const body = document.createElement("div");
      body.className = "cluster-body";
      box.append(lbl, body);
      kidCols.push(box);
      clusters.set(c.cluster, body);
    }
    clusters.get(c.cluster).append(col);
  }
  // rares isn't a SLOTS entry (numeric prefix, not a picker) but it narrows
  // the same offer: the card-back strip joins the offer's children when the
  // picked relic grants fresh card rewards.
  if (slot.id === "neowOffer" && raresEnabled(state)) {
    kidCols.push(state.neowOffer === "kaleidoscope"
      ? kaleidoRaresCol(state, set) : raresCol(state, set));
  }
  // Children that exist but are gated on the (unpicked) character get one
  // placeholder slot, so the "pick X, then narrow what it yields" flow is
  // discoverable instead of the narrowers silently not rendering.
  if (SLOTS.some((s) => s.parent === slot.id && charGated(s, state))) {
    const col = document.createElement("div");
    col.className = "slotcol";
    col.append(labelSpan("narrow further"), needCharSlot(slot.id + "Gated"));
    kidCols.push(col);
  }
  if (kidCols.length) {
    const kids = document.createElement("div");
    kids.className = "children";
    kids.append(...kidCols);
    col.append(kids);
  }
  return col;
}

// Kaleidoscope reads rares per-reward (2 rewards × 3 rolls): a plain toggle
// over the values that make per-reward sense (any / 3 / 6).
function kaleidoRaresCol(state, set) {
  const col = document.createElement("div");
  col.className = "slotcol";
  col.append(labelSpan("rare rolls"));
  const toggle = document.createElement("div");
  toggle.className = "toggle";
  toggle.dataset.slot = "rares";
  for (const [v, label] of KALEIDO_RARES_OPTIONS) {
    const b = document.createElement("button");
    b.textContent = label;
    b.classList.toggle("on", state.rares === v);
    b.addEventListener("click", () => set({ rares: v }));
    toggle.append(b);
  }
  col.append(toggle);
  return col;
}

// Clickable card-back strip for rares=N: the Nth card sets the prefix,
// clicking the current edge clears back to 0.
function raresCol(state, set) {
  const col = document.createElement("div");
  col.className = "slotcol";
  col.append(labelSpan(state.rares > 0
    ? `first ${state.rares} roll rare` : "first N roll rare"));
  const strip = document.createElement("div");
  strip.className = "rstrip";
  strip.dataset.slot = "rares";
  for (let i = 0; i < 4; i++) {
    const cb = btn(i < state.rares ? "rare" : "any",
      () => set({ rares: i + 1 === state.rares ? 0 : i + 1 }));
    cb.className = "cardback" + (i < state.rares ? " rare" : "");
    strip.append(cb);
  }
  col.append(strip);
  return col;
}

// Renders a package pick row: through the highest set slot + one empty "add"
// slot (capped at pickIds.length), needchar fallback on the first slot, art
// button or <select> per slot.kind. Generalizes the reward inline block.
function pickRow(pickIds, state, manifest, set) {
  const slots = pickIds.map((id) => SLOTS.find((s) => s.id === id));
  const row = document.createElement("div");
  row.className = "pickrow";
  if (!slotEnabled(slots[0], state)) {
    // A char-gated row invites picking a character; any other gate (e.g. the
    // engine-rejected bag × bones-capsule-pull conjunction) greys the row
    // with the slot's own reason instead.
    if (charGated(slots[0], state)) {
      row.append(needCharSlot(pickIds[0]));
    } else {
      const b = document.createElement("button");
      b.className = "slot needchar";
      b.dataset.slot = pickIds[0];
      b.disabled = true;
      b.textContent = slots[0].hint?.(state) || COPY.pickCharacter;
      row.append(b);
    }
    return row;
  }
  const hi = pickIds.reduce((h, id, i) => (state[id] !== null ? i + 1 : h), 0);
  for (let i = 0; i < Math.min(hi + 1, pickIds.length); i++) {
    row.append(slots[i].kind === "select"
      ? slotSelect(slots[i], state, set)
      : slotButton(slots[i], state, manifest, set));
  }
  return row;
}

function slotButton(slot, state, manifest, set) {
  const b = document.createElement("button");
  b.className = "slot";
  b.dataset.slot = slot.id;
  const v = state[slot.id];
  // Slot-level gating: when EVERY option of a small pool would make the spec
  // unsatisfiable (e.g. any curse behind a New Leaf grant), grey the whole
  // slot with the engine's reason rather than opening a picker of dead
  // entries. Small pools only — probing a ~600-entry relic pool on every
  // render is not worth it; those pools gate per-entry inside the picker.
  let blocked = null;
  if (v === null) {
    const pool = slot.pool(state);
    if (pool.length > 0 && pool.length <= 16) {
      const reasons = pool.map((id) => probe({ [slot.id]: id }));
      if (reasons.every(Boolean)) blocked = reasons[0];
    }
  }
  if (blocked) {
    b.classList.add("blocked");
    b.title = blocked;
  }
  if (v !== null) {
    b.classList.add("set");
    const e = entryFor(manifest, slot.poolKind, v, state);
    const img = document.createElement("img");
    img.src = `assets/${e.img}`;
    img.alt = "";
    const nm = document.createElement("span");
    nm.textContent = e.name;
    const clear = document.createElement("span");
    clear.className = "clear";
    clear.textContent = "×";
    clear.addEventListener("click", (ev) => { ev.stopPropagation(); set({ [slot.id]: null }); });
    b.append(img, nm, clear);
  } else {
    b.textContent = "any";
  }
  b.addEventListener("click", () => {
    if (blocked) return;
    // Computed inside the handler, not in the render pass: entryFor runs
    // renderDesc per entry, and the charless relic sections are ~600 entries
    // the board would otherwise template on every re-render.
    const sortLeaves = !UNSORTED_PICKERS.has(slot.id);
    const materialize = ({ ids, notes, ...rest }) => {
      const entries = ids.map((id) => ({
        ...entryFor(manifest, slot.poolKind, id, state), note: notes?.[id],
        blocked: probe({ [slot.id]: id }),
      }));
      // Sort on the MANIFEST name, which is what the cell renders. board.js
      // sees only the generated tables' name column, and the two disagree in
      // 47 places at v0.110.1 ("One Two Punch" vs "One-Two Punch"), so a
      // board-side sort would produce a visibly unalphabetical grid.
      if (sortLeaves) entries.sort((a, b) => a.name.localeCompare(b.name));
      return { ...rest, entries };
    };
    const sections = slot.sections?.(state)?.map((sec) =>
      (sec.sections ? { ...sec, sections: sec.sections.map(materialize) } : materialize(sec)));
    openPicker({
      title: slot.label,
      // The flat total behind the header count, descending one level into
      // groups. pool(state) stays shared-only charless so reconstruct's pool
      // check keeps rejecting char relics, so counting pool() here would
      // undercount a charless relic picker.
      entries: sections
        ? leavesOf(sections).flatMap((leaf) => leaf.entries)
        : slot.pool(state).map((id) => ({
          ...entryFor(manifest, slot.poolKind, id, state),
          blocked: probe({ [slot.id]: id }),
        })),
      sections,
      onPick: (id) => {
        // Picking a character's relic charless selects that character too.
        // Merge into withPickWithin's patch rather than replacing it — a bare
        // {[slot.id]: id} would stop raising shopWithin to the pick floor.
        const owner = !state.char && slot.impliesChar?.(id, state);
        set({ ...withPickWithin(slot.id, id, state), ...(owner ? { char: owner } : {}) });
      },
    });
  });
  return b;
}

// "art" kind: pools small enough to lay out inline (bosses, kaleidoscope's
// other-character picks), so every option shows its game art instead of hiding
// behind a <select>. The leading "any" cell is the unset state; re-clicking the
// active cell also clears, mirroring the character picker.
// Boss pools are the big ones (6 act-1 bosses + an "any"); labelling every cell
// stacked the route group three rows deep per slot, so bosses render as a
// compact icon strip — the name lives in the tooltip and, once picked, in the
// slot's own label. Character pools are small enough to keep named cells.
const COMPACT_ART = new Set(["boss1", "boss2", "boss3", "boss3b", "ancient2", "ancient3"]);

// Pickers whose section order is deliberate and must not be alphabetized:
// neowOffer renders in screen order (the two positives in shuffle order,
// cursed last, as Neow.cs appends them), and the ancient offers order by
// grade. poolKind cannot express this — all three are "relics", the same as
// the relic pickers that DO want sorting.
const UNSORTED_PICKERS = new Set(["neowOffer", "ancient2Offers", "ancient3Offers"]);

function slotArt(slot, state, manifest, set) {
  const compact = COMPACT_ART.has(slot.id);
  const grid = document.createElement("div");
  grid.className = "artgrid" + (compact ? " compact" : "");
  grid.dataset.slot = slot.id;
  const cur = state[slot.id] ?? "";
  const cell = (val, name, tip, img) => {
    const b = document.createElement("button");
    b.className = "artcell" + (cur === val ? " on" : "") + (img ? "" : " noart");
    b.dataset.val = val;
    b.setAttribute("aria-pressed", String(cur === val));
    b.title = tip ?? name;
    // Option gating: the "any" cell and the currently-selected cell stay
    // clickable (they clear/relax, never tighten), everything else asks the
    // engine whether picking it could ever match.
    const blocked = val && cur !== val ? probe({ [slot.id]: val }) : null;
    if (blocked) {
      b.classList.add("blocked");
      b.title = blocked;
    }
    if (img) {
      const i = document.createElement("img");
      i.src = `assets/${img}`;
      i.alt = "";
      b.append(i);
    }
    if (!compact || !img) b.append(labelSpan(name));
    b.addEventListener("click", () => {
      if (blocked) return;
      set({ [slot.id]: cur === val ? null : val || null });
    });
    grid.append(b);
  };
  cell("", "any", null, null);
  for (const [id, label] of slot.options(state)) {
    const e = manifest[slot.poolKind][id];
    // The option label carries context the title doesn't (a boss's act-1 map,
    // which the pick implies) — keep it as the tooltip.
    cell(id, e.title ?? id, label, e.img);
  }
  return grid;
}

// A compact art slot hides its cell names, so the slot label carries the pick.
function artLabel(slot, state, manifest) {
  const cur = state[slot.id];
  if (!COMPACT_ART.has(slot.id) || !cur) return slot.label;
  return `${slot.label} · ${manifest[slot.poolKind][cur]?.title ?? cur}`;
}

// "select" kind: a plain labeled <select> ("any" = empty = slot unset).
// Child selects render only while their parent enables them; top-level ones
// (ancients) are always visible.
function slotSelect(slot, state, set) {
  const sel = document.createElement("select");
  sel.dataset.slot = slot.id;
  sel.add(new Option("any", ""));
  for (const [v, label] of slot.options(state)) {
    const o = new Option(label, v);
    // Option gating (the current value stays selectable — it's already set).
    if (v !== state[slot.id]) {
      const blocked = probe({ [slot.id]: v });
      if (blocked) {
        o.disabled = true;
        o.title = blocked;
      }
    }
    sel.add(o);
  }
  sel.value = state[slot.id] ?? "";
  sel.addEventListener("change", () => set(withPickWithin(slot.id, sel.value || null, state)));
  return sel;
}

// Enabled-but-for-the-character: probing enabledWhen with a hypothetical
// char distinguishes "needs a character" from "wrong parent value".
const charGated = (slot, state) =>
  !state.char && !slotEnabled(slot, state) &&
  slotEnabled(slot, { ...state, char: charactersFor(state)[0] });

function needCharSlot(slotId) {
  const b = document.createElement("button");
  b.className = "slot needchar";
  b.dataset.slot = slotId;
  b.textContent = COPY.pickCharacter;
  b.addEventListener("click", () =>
    document.querySelector("#char .artcell[data-val='ironclad']").focus());
  return b;
}

const btn = (text, fn) => {
  const b = document.createElement("button");
  b.textContent = text;
  b.addEventListener("click", fn);
  return b;
};
const labelSpan = (text) => {
  const s = document.createElement("span");
  s.className = "slotlbl";
  s.textContent = text;
  return s;
};
// Divider inside a panel that holds more than one thing: names the channel the
// controls under it belong to, so one panel can carry two mechanisms without
// the reader having to infer where one ends.
const rowLabel = (text) => {
  const s = document.createElement("div");
  s.className = "rowlbl";
  s.textContent = text;
  return s;
};
function group(container, title, bodyCls) {
  const g = document.createElement("div");
  g.className = "grp";
  const h = document.createElement("h5");
  h.textContent = title;
  g.append(h);
  container.append(g);
  const body = document.createElement("div");
  body.className = "grp-body" + (bodyCls ? " " + bodyCls : "");
  g.append(body);
  return body;
}
