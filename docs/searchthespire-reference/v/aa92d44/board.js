// Board model for the run-start UI. DOM-free by design: node tests import
// this file directly (see wasm-parity/board-check.mjs). Rendering lives in
// board_ui.js; this file owns "what spec does this board mean".
import { VERSIONS, MAIN_VERSION, DEFAULT_UI_VERSION, resolveVersion } from "./versions/index.js";

// Resolves the table registry for a board state's pinned version, falling
// back to MAIN_VERSION for an unset/unknown one. Every table read in this
// file goes through this — see web/versions/index.js for the registry shape.
export function tablesFor(state) {
  return resolveVersion(state.version) ?? VERSIONS[MAIN_VERSION];
}

// Spec-grammar schema version, carried on share links as s=. Bump ONLY on
// breaking grammar changes; additive name-keyed keys don't count. Mirror of
// core/src/search.rs SPEC_SCHEMA_VERSION (asserted by mirror-tables-check).
export const SPEC_SCHEMA_VERSION = 1;

// Character roster, per board state's pinned version. Sourced from the
// registry (web/versions/*/gen_char_pools.js CHARACTERS, generated from
// ModelDb.AllCharacters order — see tools/extract_char_pools.py) rather than
// hand-ported here, so a second registered version can't silently diverge.
// Asserted against core/examples/mirror_tables.rs's "characters" field by
// wasm-parity/mirror-tables-check.mjs.
export function charactersFor(state) {
  return tablesFor(state).charPools.CHARACTERS;
}

// The act-1 map already pinned by the board: the act toggle or boss1
// (implies its map — BOSS1_OVERGROWTH/BOSS1_UNDERDOCKS, version-mutable
// game data, per tablesFor(state)).
function easyMapFor(state) {
  if (state.act !== null) return state.act;
  if (state.boss1 !== null) {
    return tablesFor(state).mirror.BOSS1_OVERGROWTH.some(([id]) => id === state.boss1) ? 0 : 1;
  }
  return null;
}

// Precondition per conditional option, from the decompiled event models —
// named on the slot so the player knows what to satisfy in-run.
const IF_PRECONDITIONS = {
  touchoforobas: "you still have your starter relic",
  // Same condition as Assumption::TranscendableStarterCard, in the same words:
  // one card per character (Bash, Neutralize, Dualcast, Unleash, Falling Star)
  // and the board must not describe it differently from the run viewer.
  archaictooth: "the starter card Archaic Tooth transforms is still in your deck",
  paelsclaw: "≥3 Goopy-enchantable cards in your deck",
  paelstooth: "≥5 removable cards in your deck",
  paelslegion: "you have no event pet",
  nutritioussoup: "a Basic Strike remains in your deck",
  triboomerang: "≥3 Instinct-enchantable cards in your deck",
  beautifulbracelet: "≥4 Swift-enchantable cards in your deck",
};

// One offers slot per ancient, two sections in its modal: guaranteed options
// and conditional ones. The section a pick came from picks the spec key, so the
// two keys stay distinct predicates while the board shows one control. A pick
// is one or the other — a spec naming BOTH keys for an ancient is engine-legal
// but board-inexpressible, and reconstructs to raw (it still searches).
// Helpers take the slot's act: Darv is the one ancient assignable to two acts,
// with act-gated pools (DARV_OPTIONS, act-keyed). His offers are exact under
// the engine's assume-met world, so they all sit in the guaranteed section and
// ride the plain ancientN_offers= key (core/src/parse.rs).
const guaranteedOffers = (a, s, act) => (a === "darv"
  ? tablesFor(s).mirror.DARV_OPTIONS[act]
  : tablesFor(s).mirror.ANCIENT_EVENT_OPTIONS[a] ?? []).map(([id]) => id);
const ifOffers = (a, s) => tablesFor(s).mirror.ANCIENT_IF_OPTIONS[a] ?? [];
const allOffers = (a, s, act) => [...guaranteedOffers(a, s, act), ...ifOffers(a, s)];
// "guaranteed to offer" read as "seeds where this is offered" and sent a
// reporter chasing a seed the search had deliberately skipped, so the title
// says what the filter actually promises and the note says what it leaves out.
const offerSections = (a, s, act) => [
  {
    title: "offers on any run",
    note: "seeds where this is offered however you play. seeds that offer it"
      + " only when your deck qualifies are left out, so inspect a seed to see"
      + " its full offer.",
    ids: guaranteedOffers(a, s, act),
  },
  // notes: the precondition rides along on each cell — which relic needs what is
  // the whole question in this section, and a section heading can't say it.
  { title: "offers if you qualify", ids: ifOffers(a, s), notes: IF_PRECONDITIONS },
].filter((sec) => sec.ids.length);
const offerEmit = (slotKey, act, a, v, s) =>
  guaranteedOffers(a, s, act).includes(v) ? `${slotKey}_offers=${v}`
    : ifOffers(a, s).includes(v) ? `${slotKey}_offers_if=${v}`
      : ""; // value kept from a previously selected ancient: drop the fragment
// Key and section must agree — an _offers= naming a conditional option (or vice
// versa) is a different predicate than the author wrote, so bail to raw rather
// than silently re-home it.
const offerParse = (key, act, a, v, s) =>
  (key.endsWith("_offers_if") ? ifOffers(a, s) : guaranteedOffers(a, s, act)).includes(v) ? v : undefined;
const offerHint = (a, v, s) => v && ifOffers(a, s).includes(v) && `if ${IF_PRECONDITIONS[v]}`;

// Event-slot options under the map the events section is locked to
// (effectiveEventMap: the board pin via act/boss1, else the first
// map-exclusive event already picked). Unpinned map → full list with suffixes.
function eventOptions(state) {
  const m = effectiveEventMap(state);
  return tablesFor(state).mirror.ACT1_EVENTS
    .filter(([, , map]) => m === null || map === null || map === m)
    .map(([id, name, map]) => [id, m !== null || map === null ? name
      : `${name} (${map === 0 ? "Overgrowth" : "Underdocks"})`]);
}

// Pool lookup by registry key, per board state's pinned version. `key` names
// one of the per-character tables exported by gen_char_pools.js (CHAR_*_SOLO)
// or gen_relic_pools.js (CAPSULE_RELICS, SHOP_RELICS) — both keyed by character.
// Rows (not ids) of a registry table: the flat table when c is null, one
// character's rows when it isn't. Pools and picker leaves resolve tables
// through this one path so they cannot disagree about what a key names.
const tableRows = (key, c, s) => {
  const t = tablesFor(s);
  const table = t.charPools[key] ?? t.relicPools[key] ?? t.colorless[key];
  return (c === null ? table : table[c]) ?? [];
};

const charPool = (key) => (s) => (s.char ? tableRows(key, s.char, s) : []).map(([id]) => id);

const RARITIES = ["Common", "Uncommon", "Rare"];
const nonEmpty = (leaves) => leaves.filter((l) => l.ids.length);

// Rarity from WHICH TABLE an id is in — the three per-character card tables,
// indexed by the LEAF's character.
const CARD_RARITY_TABLES = [
  ["common", "CHAR_COMMONS_SOLO"],
  ["uncommon", "CHAR_UNCOMMONS_SOLO"],
  ["rare", "CHAR_RARES_SOLO"],
];
export const charRarityLeaves = (s, c) => nonEmpty(CARD_RARITY_TABLES.map(([title, key]) => ({
  title, ids: tableRows(key, c, s).map(([id]) => id),
})));

// Rarity from A COLUMN — the widened tables (CAPSULE_RELICS,
// SHARED/CHAR_CAPSULE_RELICS, COLORLESS_SOLO all carry it at index 2).
export const rowRarityLeaves = (key) => (s, c) => nonEmpty(RARITIES.map((r) => ({
  title: r.toLowerCase(),
  ids: tableRows(key, c, s).filter((row) => row[2] === r).map(([id]) => id),
})));

// No rarity column at all: shop relics are every one Shop rarity, so their
// rows are two-column. One title-less leaf, which is how a rarity-less pool
// sits inside a character group.
export const flatLeaves = (key) => (s, c) => nonEmpty([
  { title: null, ids: tableRows(key, c, s).map(([id]) => id) },
]);
// Engine-valid pool for a direct relic pick: with a character, shared + that
// character's own; without one, shared only — shared relics need no char=
// (core/tests/shop_relics.rs shared_positions_are_character_invariant). Takes
// registry KEY NAMES and resolves through tablesFor(s) exactly like charPool:
// closing over imported tables instead would pin the picker to one engine
// version while the rest of the board follows state.version.
// MUST stay shared-only when charless — reconstruct's pool check relies on it
// to reject char relics in charless specs.
const relicPool = (sharedKey, charKey) => (s) => {
  const t = tablesFor(s).relicPools;
  return (s.char ? [...t[sharedKey], ...t[charKey][s.char]] : t[sharedKey]).map(([id]) => id);
};

// id -> the character that owns it, or null for a shared relic. Signature
// matches the registry's value-first/state-last convention (emit(v, s)).
const relicOwner = (charKey) => (id, s) => {
  const t = tablesFor(s).relicPools[charKey];
  return charactersFor(s).find((c) => t[c].some(([rid]) => rid === id)) ?? null;
};

// Charless picker blocks: a non-collapsing shared block, then one collapsible
// block per character. With a character set, undefined — the flat pool renders
// as it does today. `leavesFor` is a leaf-source FACTORY (rowRarityLeaves or
// flatLeaves), so the call site states whether this picker has a rarity axis.
// The shared block must not collapse: it is what the charless picker shows on
// open, and charfree-picker-check asserts its cells are rendered.
const relicSections = (sharedKey, charKey, leavesFor) => (s) => {
  if (s.char) return undefined;
  return [
    { title: "any character", collapsible: false, sections: leavesFor(sharedKey)(s, null) },
    ...charactersFor(s).map((c) => ({
      title: `${charName(c)} — picks the character`,
      collapsible: true,
      sections: leavesFor(charKey)(s, c),
    })),
  ].filter((g) => g.sections.length);
};

const potionPool = (s) => {
  if (!s.char) return [];
  const { CHAR_POTIONS, SHARED_POTIONS } = tablesFor(s).potionPools;
  return [...CHAR_POTIONS[s.char], ...SHARED_POTIONS].map(([id]) => id);
};
// Kaleidoscope draws from the OTHER characters' pools (never the player's own).
const otherChars = (s) => (s.char ? charactersFor(s).filter((c) => c !== s.char) : []);
const charName = (c) => c[0].toUpperCase() + c.slice(1);

// Scroll-box "a bundle contains" trios: the chosen cards must all sit in ONE
// bundle (same-bundle subset). The three slots have no specKey of their own —
// the lowest set slot carries the whole joint `<key>=a+b+c` fragment and the
// rest emit nothing, so exactly one fragment is produced regardless of which
// slots are filled. reconstruct() routes the joint key back to slots 1→N.
const SCROLLBOX_CONTAINS = ["scrollboxCard1", "scrollboxCard2", "scrollboxCard3"];
const BONES_SCROLLBOX_CONTAINS = ["bonesScrollboxCard1", "bonesScrollboxCard2", "bonesScrollboxCard3"];
// Joint groups can have per-slot enablement (the capsule set's slot count
// tracks the pinned capsules' pull budget), and a hidden slot may still hold
// a value — treat it as absent so it neither leaks into the fragment nor
// blocks sibling pools.
const liveIds = (ids, s) => ids.filter((id) => slotEnabled(SLOTS.find((x) => x.id === id), s));
const containsEmit = (specKey, ids, self) => (v, s) => {
  const live = liveIds(ids, s);
  const set = live.map((id) => s[id]).filter(Boolean);
  return live.find((id) => s[id] !== null) === self ? `${specKey}=${set.join("+")}` : "";
};
// A bundle is 2 commons + 1 uncommon, so the trio's picks must fit one: at most
// 2 commons and at most 1 uncommon. Each slot's pool drops the sibling picks and
// the rarities already filled by them, so you can't build a never-matching set.
const containsPool = (ids, self) => (s) => {
  const commons = new Set(charPool("CHAR_COMMONS_SOLO")(s));
  const uncommons = new Set(charPool("CHAR_UNCOMMONS_SOLO")(s));
  const others = ids.filter((id) => id !== self).map((id) => s[id]).filter(Boolean);
  const commonsFull = others.filter((c) => commons.has(c)).length >= 2;
  const uncommonFull = others.some((c) => uncommons.has(c));
  return charPool("CHAR_CU_SOLO")(s).filter((id) =>
    !others.includes(id)
    && !(commonsFull && commons.has(id))
    && !(uncommonFull && uncommons.has(id)));
};

// Sibling picks already spoken for in a joint distinct group. Extracted so the
// flat pool and the sectioned pool filter against ONE definition of "taken" —
// they render the same picker and must not disagree about what's still free.
const takenBy = (ids, self) => (s) =>
  liveIds(ids, s).filter((id) => id !== self).map((id) => s[id]).filter(Boolean);

// Joint "distinct picks" pool: each slot offers its base pool minus the values
// the OTHER slots already hold (no duplicate picks across the joint fragment).
const distinctPool = (base, ids, self) => (s) => {
  const taken = takenBy(ids, self)(s);
  return base(s).filter((id) => !taken.includes(id));
};

// Same distinctness over SECTIONED pools. The grid is driven by sections, not
// by pool, so a relic already taken by a sibling would otherwise still render
// clickable. Descends one level: drops emptied leaves, then emptied groups.
const distinctSections = (base, ids, self) => (s) => {
  const sections = base(s);
  if (!sections) return undefined;
  const taken = takenBy(ids, self)(s);
  const keep = (leaf) => ({ ...leaf, ids: leaf.ids.filter((id) => !taken.includes(id)) });
  return sections
    .map((sec) => (sec.sections ? { ...sec, sections: nonEmpty(sec.sections.map(keep)) } : keep(sec)))
    .filter((sec) => (sec.sections ? sec.sections.length : sec.ids.length));
};
// Union of every OTHER character's rollable cards — Kaleidoscope draws from
// all non-player pools, so cross-character pairs like regent-Arsenal +
// ironclad-Feel-No-Pain are expressible. This pool never depended on a chosen
// character, which is why the "offers card from" picker that sat above it was
// removed: naming a card already implies its owner (kaleido_from asserts only
// "some offer is from X", and no rollable card belongs to two characters), so
// 95.6% of its uses alongside a named card filtered nothing, and its position
// taught readers they had to pick a character before the card picker worked.
const kaleidoUnionPool = (s) =>
  otherChars(s).flatMap((c) => tablesFor(s).charPools.CHAR_ROLLABLE_SOLO[c].map(([id]) => id));

const KALEIDO_DISTINCT = ["kaleidoCard1", "kaleidoCard2"];
const BONES_KALEIDO_DISTINCT = ["bonesKaleidoCard1", "bonesKaleidoCard2"];
const POULTICE_SET = ["poulticeCard1", "poulticeCard2"];
const BONES_POULTICE_SET = ["bonesPoulticeCard1", "bonesPoulticeCard2"];
const BONES_CAPSULE_SET = ["bonesCapsuleSet1", "bonesCapsuleSet2", "bonesCapsuleSet3"];
// Summed pull budget of the PINNED capsule grants: Small pulls 1 relic, Large
// pulls 2 (world/state.rs World::capsule_pulls_from). The visible "capsule pulls
// include" slot count tracks it — a third pick with only Large pinned would
// silently constrain the other grant to be Small Capsule.
const bonesCapsulePulls = (s) =>
  (bonesHas(s, "smallcapsule") ? 1 : 0) + (bonesHas(s, "largecapsule") ? 2 : 0);

// Card package: the multiset of cards that must all appear within the
// first N fight rewards. Six fixed slots; the lowest set slot carries the
// whole joint fragment (the SCROLLBOX_CONTAINS pattern) — duplicates
// allowed, they ARE the multiplicity.
export const REWARD_PICK_IDS = ["rewardPick1", "rewardPick2", "rewardPick3",
  "rewardPick4", "rewardPick5", "rewardPick6"];

export const SHOP_PICK_IDS = ["shopPick1", "shopPick2", "shopPick3",
  "shopPick4", "shopPick5", "shopPick6"];
export const EVENT_PICK_IDS = ["eventPick1", "eventPick2", "eventPick3",
  "eventPick4", "eventPick5"];

// The relic-reward deques (C/U/R): each pick must land in the top N of its
// OWN rarity's deque. 3 slots (vs shop's 6) — hunting past 3 is vanishingly
// selective.
export const BAG_PICK_IDS = ["bagPick1", "bagPick2", "bagPick3"];

// bag_relic beside a bones capsule pull compiles since niche-v2 item 3: the
// pull is a timeline choice and the bag read attaches behind both pickups
// (core/src/parse.rs routes it to the bones timeline), so the two picker
// families compose freely.

// charPool drops everything but the id (board.js:103); the per-rarity floor
// needs the rarity element the widened CAPSULE_RELICS rows carry.
const relicRarity = (s, id) =>
  (tablesFor(s).relicPools.CAPSULE_RELICS[s.char] ?? []).find(([rid]) => rid === id)?.[2] ?? null;

// Unlike shopFloor (every shop pick shares ONE deque), bag picks in different
// rarity deques don't compete: the window only has to cover the largest
// same-rarity group. max(picks sharing a rarity); 0 when nothing is picked.
// An unknown rarity (null) buckets with other unknowns — a C/U/R relic the
// engine accepts but CAPSULE_RELICS omits, since that table filters on the
// Neow-solo `allowed` flag and parse_pool_relic doesn't. No such relic exists
// in either shipped version (zero C/U/R rows are allowed=false), and grouping
// unknowns together only ever RAISES the floor, which bails reconstruct to raw
// rather than silently searching the wrong thing.
export const bagFloor = (s) => {
  const n = {};
  for (const id of BAG_PICK_IDS) {
    if (s[id]) {
      const r = relicRarity(s, s[id]);
      n[r] = (n[r] ?? 0) + 1;
    }
  }
  return Math.max(0, ...Object.values(n));
};

// Window floor = count of set picks. K distinct relics need >= K shop offers;
// K events need >= K ? rooms. No rare-pity floor (unlike packageFloor).
export const shopFloor = (s) => SHOP_PICK_IDS.filter((id) => s[id]).length;
export const eventFloor = (s) => EVENT_PICK_IDS.filter((id) => s[id]).length;

// Auto-increment target per package. Events buffer +1 for K>=2 (a lone event
// stays literal); a ? room may not resolve to an event, so the buffer makes a
// multi-event search realistically hittable. Cards/shops target the exact count.
export const bufTarget = (k) => (k === 1 ? 1 : Math.min(k + 1, 5));
const PICK_PACKAGES = [
  { ids: REWARD_PICK_IDS, withinKey: "rewardWithin", target: (k) => k },
  { ids: SHOP_PICK_IDS, withinKey: "shopWithin", target: (k) => k },
  { ids: EVENT_PICK_IDS, withinKey: "eventWithin", target: bufTarget },
  { ids: BAG_PICK_IDS, withinKey: "bagWithin", target: (_k, s) => bagFloor(s) },
];

// Setting a package pick raises its within window to target(K), never lowers it
// (a manually widened window survives). Non-package slots get a bare patch.
export function withPickWithin(slotId, id, state) {
  const pkg = PICK_PACKAGES.find((p) => p.ids.includes(slotId));
  if (!pkg) return { [slotId]: id };
  const next = { ...state, [slotId]: id };
  const k = pkg.ids.filter((pid) => next[pid]).length;
  // target(k, next): the count-based packages ignore the second argument; the
  // bag package needs the picks themselves, since its floor is per-rarity.
  return { [slotId]: id, [pkg.withinKey]: Math.max(state[pkg.withinKey], pkg.target(k, next)) };
}

// The act-1 map the events section is locked to: the board's own pin
// (act/boss1) if any, else the map of the first map-exclusive event picked,
// else null (unconstrained). Distinct from easyMapFor (board pin only) to
// avoid leaking event-pick state into a general map helper.
const eventMap = (id, s) =>
  tablesFor(s).mirror.ACT1_EVENTS.find(([eid]) => eid === id)?.[2] ?? null;
export function effectiveEventMap(state) {
  const pinned = easyMapFor(state);
  if (pinned !== null) return pinned;
  for (const id of EVENT_PICK_IDS) {
    if (state[id] !== null) {
      const m = eventMap(state[id], state);
      if (m !== null) return m;
    }
  }
  return null;
}

// Spec key → the slot group that jointly carries that fragment. Lets the
// widen hints map a joint fragment back to every contributing slot for
// display and removal (a single-slot fragment maps via slot.emit directly).
export const JOINT_GROUPS = {
  scrollbox_contains: SCROLLBOX_CONTAINS,
  bones_scrollbox_contains: BONES_SCROLLBOX_CONTAINS,
  kaleido_distinct: KALEIDO_DISTINCT,
  bones_kaleido_distinct: BONES_KALEIDO_DISTINCT,
  poultice_set: POULTICE_SET,
  bones_poultice_set: BONES_POULTICE_SET,
  bones_capsule_set: BONES_CAPSULE_SET,
  reward_cards: REWARD_PICK_IDS,
};
// Hard floor on the within window: one pick per reward screen (the
// engine's obtainability matching — every pick needs its own fight), and
// the total-rare floor (one shared pity offset binds all rares regardless
// of identity).
export function packageFloor(state) {
  const picks = REWARD_PICK_IDS.map((id) => state[id]).filter(Boolean);
  if (!picks.length) return 1;
  let floor = picks.length;
  const rares = new Set(charPool("CHAR_RARES_SOLO")(state));
  const r = picks.filter((p) => rares.has(p)).length;
  if (r > 0) {
    const t = tablesFor(state).derived.REWARD_RARE_FLOORS[scarcityOf(state) ? "scarcity" : "normal"];
    floor = Math.max(floor, t[r - 1] ?? Infinity);
  }
  return floor;
}

// Pool for one package pick slot: the rollable pool minus any candidate
// that would push packageFloor past the board's 1..6 within select
// (compute the floor with the candidate hypothetically in this slot —
// same shape as containsPool dropping picks that can't fit one bundle).
// Deeper packages (engine-legal to within=10) stay raw-?spec=-only, the
// board-1..6/engine-wider convention shop_within already follows.
const packagePool = (self) => (s) =>
  charPool("CHAR_ROLLABLE_SOLO")(s)
    .filter((c) => packageFloor({ ...s, [self]: c }) <= 6);

// Which characters a picker offers from. Charless it yields [], which falls
// through to the flat fallback.
const selectedChar = (s) => (s.char ? [s.char] : []);

// Sections for one picker slot, grouped by character then rarity.
// poolFn is the slot's own pool; leavesFor reads this pool's rarity for ONE
// character; chars names the characters (absent for a character-independent
// pool like colorless, which runs a single pass with char = null).
// Every leaf is `leaf source ∩ poolFn(state)`, so a floor-filtered pool
// (packagePool) or a sibling-narrowed one (distinctPool, containsPool) can
// never disagree with what the grid shows.
const pickerSections = (poolFn, leavesFor, chars) => (s) => {
  const pool = new Set(poolFn(s));
  const groups = (chars ? chars(s) : [null])
    .map((c) => ({
      c,
      leaves: nonEmpty(leavesFor(s, c).map((l) => ({
        ...l, ids: l.ids.filter((id) => pool.has(id)),
      }))),
    }))
    .filter((g) => g.leaves.length);
  if (groups.length > 1) {
    return groups.map((g) => ({ title: charName(g.c), collapsible: true, sections: g.leaves }));
  }
  const leaves = groups[0]?.leaves ?? [];
  return leaves.length > 1 ? leaves : undefined;
};

// Leaves of a slot's sections, flattening the one level of grouping. Exported
// so board_ui.js's header union and board-check's partition assert flatten the
// same way the UI renders — parallel implementations would be free to drift.
export const leavesOf = (sections) => sections.flatMap((sec) => sec.sections ?? [sec]);

const packageEmit = (self) => (v, s) => {
  const set = REWARD_PICK_IDS.map((id) => s[id]).filter(Boolean);
  if (REWARD_PICK_IDS.find((id) => s[id] !== null) !== self) return "";
  // Clamp to the floor: the render can't mutate state (set() during render
  // re-enters renderBoard and corrupts the DOM), so the select shows the
  // clamped value and the emit uses it — state.rewardWithin below the floor
  // is display-legal but never compiled.
  const within = Math.max(s.rewardWithin, packageFloor(s));
  return `reward_within=${within},reward_cards=${set.join("+")}`
    + (s.rewardOrdered ? ",reward_ordered" : "");
};

// Picker/toggle slots, in canonical spec order. Kinds: "toggle" | "picker"
// | "art" (small pool laid out inline as game art from poolKind + options(state))
// | "select" (no manifest art: a plain <select> from options(state) →
// [[value, label], ...]; pool still validates parses).
// parent/enabledWhen: child slots render bracketed under parent and only
// compile while enabled (values persist through gating changes).
export const SLOTS = [
  { id: "act", kind: "toggle", specKey: "act", group: "act",
    options: [[0, "Overgrowth"], [1, "Underdocks"]],
    emit: (v) => `act=${v}`,
    parse: (v) => ({ 0: 0, 1: 1 })[v] },

  // One slot for whichever relic Neow's offer must contain — you pick one in
  // game, so one constraint slot. Cursed offers emit neow=N (table index),
  // bonus offers emit bonus=NAME. Constraining both at once isn't
  // board-representable (a raw ?spec= URL covers that rare need).
  { id: "neowOffer", kind: "picker", specKeys: ["neow", "bonus"], group: "neow",
    label: "offers relic", poolKind: "relics",
    pool: (s) => [...tablesFor(s).neow.CURSED_OFFERS, ...tablesFor(s).neow.BONUS_OFFERS],
    sections: (s) => [
      { title: "cursed offer", ids: tablesFor(s).neow.CURSED_OFFERS },
      { title: "bonus offers", ids: tablesFor(s).neow.BONUS_OFFERS },
    ],
    emit: (v, s) => tablesFor(s).neow.CURSED_OFFERS.includes(v)
      ? `neow=${tablesFor(s).neow.CURSED_OFFERS.indexOf(v)}`
      : `bonus=${v}`,
    parse: (v, key, s) => (key === "neow" ? tablesFor(s).neow.CURSED_OFFERS[Number(v)] : v) },

  // Both grants are asserted unordered (bones_relic=); pickup order is NOT
  // pinned. The engine evaluates every detail roll under bones_first=None =
  // the seed's presentation order (top-to-bottom pickup), so a query returns
  // every seed where playing the grants in the order that seed presents them
  // yields the outcome. Raw bones_first= (raw-spec mode) still pins for the
  // power-user "pick against presentation" case.
  { id: "bonesGrantA", kind: "picker", specKey: "bones_relic", group: "neow",
    parent: "neowOffer", label: "grants", poolKind: "relics",
    pool: (s) => tablesFor(s).neow.BONES_GRANTS,
    enabledWhen: (s) => s.neowOffer === "neowsbones",
    emit: (v) => `bones_relic=${v}`, parse: (v) => v },

  { id: "bonesGrantB", kind: "picker", specKey: "bones_relic", group: "neow",
    parent: "neowOffer", label: "and grants", poolKind: "relics",
    pool: (s) => tablesFor(s).neow.BONES_GRANTS,
    enabledWhen: (s) => s.neowOffer === "neowsbones",
    emit: (v) => `bones_relic=${v}`, parse: (v) => v },

  { id: "bonesCurse", kind: "picker", specKey: "bones_curse", group: "neow",
    parent: "neowOffer", label: "curse", poolKind: "cards",
    pool: (s) => tablesFor(s).neow.CURSES,
    // Whether a grant combination can ever yield a curse is the ENGINE's
    // call, not the board's: the compiled spec goes out as written and
    // index.html consults spec_satisfiability. Since niche-v2 (KR-3 deleted,
    // the capsules modelled) NO grant blocks the curse — sat.rs's
    // undefined-by-construction pass is gone — but the routing stays: a future
    // contradiction surfaces through the engine, never through a board copy.
    enabledWhen: (s) => s.neowOffer === "neowsbones",
    emit: (v) => `bones_curse=${v}`, parse: (v) => v },

  // Bones-granted detail narrowers (roadmap #12): narrow what a granted detail
  // relic yields. The engine positions the rewards stream in presentation order
  // (25-draw grant shuffle + any earlier-presented sibling's consumption).
  { id: "bonesTabletCard", kind: "picker", specKey: "bones_tablet_card", group: "neow",
    parent: "neowOffer", cluster: "hefty tablet", label: "offers rare", poolKind: "cards",
    pool: charPool("CHAR_RARES_SOLO"),
    sections: pickerSections(charPool("CHAR_RARES_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "heftytablet") && !!s.char,
    emit: (v) => `bones_tablet_card=${v}`, parse: (v) => v },

  { id: "bonesArcaneCard", kind: "picker", specKey: "bones_arcane_card", group: "neow",
    parent: "neowOffer", cluster: "arcane scroll", label: "the rare is", poolKind: "cards",
    pool: charPool("CHAR_RARES_SOLO"),
    sections: pickerSections(charPool("CHAR_RARES_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "arcanescroll") && !!s.char,
    emit: (v) => `bones_arcane_card=${v}`, parse: (v) => v },

  { id: "bonesPaperweightCard", kind: "picker", specKey: "bones_paperweight_card", group: "neow",
    parent: "neowOffer", cluster: "lead paperweight", label: "offers", poolKind: "cards",
    pool: (s) => tablesFor(s).colorless.COLORLESS_SOLO.map(([id]) => id),
    sections: pickerSections((s) => tablesFor(s).colorless.COLORLESS_SOLO.map(([id]) => id), rowRarityLeaves("COLORLESS_SOLO")),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "leadpaperweight"),
    emit: (v) => `bones_paperweight_card=${v}`, parse: (v) => v },

  { id: "bonesCofferCard", kind: "picker", specKey: "bones_coffer_card", group: "neow",
    parent: "neowOffer", cluster: "lost coffer", label: "offers card", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "lostcoffer") && !!s.char,
    emit: (v) => `bones_coffer_card=${v}`, parse: (v) => v },

  { id: "bonesCofferPotion", kind: "picker", specKey: "bones_coffer_potion", group: "neow",
    parent: "neowOffer", cluster: "lost coffer", label: "potion", poolKind: "potions",
    pool: potionPool,
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "lostcoffer") && !!s.char,
    emit: (v) => `bones_coffer_potion=${v}`, parse: (v) => v },

  { id: "bonesCapsuleSet1", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "capsules", label: "capsule pulls include", poolKind: "relics",
    pool: distinctPool(charPool("CAPSULE_RELICS"), BONES_CAPSULE_SET, "bonesCapsuleSet1"),
    sections: pickerSections(distinctPool(charPool("CAPSULE_RELICS"), BONES_CAPSULE_SET, "bonesCapsuleSet1"), rowRarityLeaves("CAPSULE_RELICS"), selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && !!s.char && bonesCapsulePulls(s) >= 1,
    emit: containsEmit("bones_capsule_set", BONES_CAPSULE_SET, "bonesCapsuleSet1") },

  { id: "bonesCapsuleSet2", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "capsules", label: "and includes", poolKind: "relics",
    pool: distinctPool(charPool("CAPSULE_RELICS"), BONES_CAPSULE_SET, "bonesCapsuleSet2"),
    sections: pickerSections(distinctPool(charPool("CAPSULE_RELICS"), BONES_CAPSULE_SET, "bonesCapsuleSet2"), rowRarityLeaves("CAPSULE_RELICS"), selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && !!s.char && bonesCapsulePulls(s) >= 2,
    emit: containsEmit("bones_capsule_set", BONES_CAPSULE_SET, "bonesCapsuleSet2") },

  { id: "bonesCapsuleSet3", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "capsules", label: "and includes", poolKind: "relics",
    pool: distinctPool(charPool("CAPSULE_RELICS"), BONES_CAPSULE_SET, "bonesCapsuleSet3"),
    sections: pickerSections(distinctPool(charPool("CAPSULE_RELICS"), BONES_CAPSULE_SET, "bonesCapsuleSet3"), rowRarityLeaves("CAPSULE_RELICS"), selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && !!s.char && bonesCapsulePulls(s) >= 3,
    emit: containsEmit("bones_capsule_set", BONES_CAPSULE_SET, "bonesCapsuleSet3") },

  // Bones-granted Kaleidoscope: same per-reward narrowers as the direct pick,
  // on the positioned rewards stream. Evaluated under presentation order
  // (bones_first=None). Since niche-v2, a niche-consuming sibling ahead of
  // Kaleidoscope (New Leaf or a capsule) no longer makes this undefined — its
  // read is positioned past the sibling's modelled consumption either way.
  // The only seeds that still don't match are the capsule deck model's own
  // residue: no char= (relic rewards are character-keyed), or a War
  // Paint/Whetstone pull whose deck edit the model can't derive (New Leaf's
  // own outgoing transform target, Pomander, Shears, an unpinned
  // discriminating Scroll Boxes bundle).

  { id: "bonesKaleidoCard1", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "kaleidoscope", label: "offers card", poolKind: "cards",
    pool: distinctPool(kaleidoUnionPool, BONES_KALEIDO_DISTINCT, "bonesKaleidoCard1"),
    sections: pickerSections(distinctPool(kaleidoUnionPool, BONES_KALEIDO_DISTINCT, "bonesKaleidoCard1"), charRarityLeaves, otherChars),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "kaleidoscope") && !!s.char,
    emit: containsEmit("bones_kaleido_distinct", BONES_KALEIDO_DISTINCT, "bonesKaleidoCard1") },

  { id: "bonesKaleidoCard2", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "kaleidoscope", label: "and offers card", poolKind: "cards",
    pool: distinctPool(kaleidoUnionPool, BONES_KALEIDO_DISTINCT, "bonesKaleidoCard2"),
    sections: pickerSections(distinctPool(kaleidoUnionPool, BONES_KALEIDO_DISTINCT, "bonesKaleidoCard2"), charRarityLeaves, otherChars),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "kaleidoscope") && !!s.char,
    emit: containsEmit("bones_kaleido_distinct", BONES_KALEIDO_DISTINCT, "bonesKaleidoCard2") },

  // Bones-granted Scroll Boxes: same same-bundle-subset narrowers on the
  // positioned rewards stream (bones_scrollbox_contains=a+b+c).
  { id: "bonesScrollboxCard1", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "scroll boxes", label: "a bundle contains", poolKind: "cards",
    pool: containsPool(BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard1"),
    sections: pickerSections(containsPool(BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard1"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "scrollboxes") && !!s.char,
    emit: containsEmit("bones_scrollbox_contains", BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard1") },

  { id: "bonesScrollboxCard2", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "scroll boxes", label: "and contains", poolKind: "cards",
    pool: containsPool(BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard2"),
    sections: pickerSections(containsPool(BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard2"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "scrollboxes") && !!s.char,
    emit: containsEmit("bones_scrollbox_contains", BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard2") },

  { id: "bonesScrollboxCard3", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "scroll boxes", label: "and contains", poolKind: "cards",
    pool: containsPool(BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard3"),
    sections: pickerSections(containsPool(BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard3"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "scrollboxes") && !!s.char,
    emit: containsEmit("bones_scrollbox_contains", BONES_SCROLLBOX_CONTAINS, "bonesScrollboxCard3") },

  // Bones-granted New Leaf: the direct-pick convention (player transforms a
  // Basic) plus "resolved at pickup", evaluated under presentation order.
  // Since niche-v2's N4 deck model, a capsule presented ahead of New Leaf no
  // longer makes this undefined — same residual exceptions as the
  // Kaleidoscope comment above (no char=, or a WarPaint/Whetstone pull whose
  // deck edit the model can't derive).
  { id: "bonesNewleafCard", kind: "picker", specKey: "bones_newleaf_card", group: "neow",
    parent: "neowOffer", cluster: "new leaf", label: "transforming a Basic gives", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "newleaf") && !!s.char,
    emit: (v) => `bones_newleaf_card=${v}`, parse: (v) => v },

  // Bones-granted Leafy Poultice: the transforms roll the fresh
  // `transformations` stream in any obtain context, so these carry the same
  // joint fragment under the bones_ key. Both slots share one enabledWhen, so
  // containsEmit's per-slot liveness degenerates to "both live or neither" —
  // that machinery exists for the capsule set's pull budget; don't add one here.
  { id: "bonesPoulticeCard1", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "leafy poultice", label: "poultice gives", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "leafypoultice") && !!s.char,
    emit: containsEmit("bones_poultice_set", BONES_POULTICE_SET, "bonesPoulticeCard1") },

  { id: "bonesPoulticeCard2", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "leafy poultice", label: "and also gives", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "neowsbones" && bonesHas(s, "leafypoultice") && !!s.char,
    emit: containsEmit("bones_poultice_set", BONES_POULTICE_SET, "bonesPoulticeCard2") },

  { id: "tabletCard", kind: "picker", specKey: "tablet_card", group: "neow",
    parent: "neowOffer", label: "offers rare", poolKind: "cards",
    pool: charPool("CHAR_RARES_SOLO"),
    sections: pickerSections(charPool("CHAR_RARES_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "heftytablet" && !!s.char,
    emit: (v) => `tablet_card=${v}`, parse: (v) => v },

  { id: "poulticeCard1", kind: "picker", group: "neow",
    parent: "neowOffer", label: "poultice gives", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "leafypoultice" && !!s.char,
    emit: containsEmit("poultice_set", POULTICE_SET, "poulticeCard1") },

  { id: "poulticeCard2", kind: "picker", group: "neow",
    parent: "neowOffer", label: "and also gives", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "leafypoultice" && !!s.char,
    emit: containsEmit("poultice_set", POULTICE_SET, "poulticeCard2") },

  { id: "largeRelicA", kind: "picker", specKey: "large_relic", group: "neow",
    parent: "neowOffer", label: "pulls", poolKind: "relics",
    pool: relicPool("SHARED_CAPSULE_RELICS", "CHAR_CAPSULE_RELICS"),
    sections: relicSections("SHARED_CAPSULE_RELICS", "CHAR_CAPSULE_RELICS", rowRarityLeaves),
    impliesChar: relicOwner("CHAR_CAPSULE_RELICS"),
    enabledWhen: (s) => s.neowOffer === "largecapsule",
    emit: (v) => `large_relic=${v}`, parse: (v) => v },

  { id: "largeRelicB", kind: "picker", specKey: "large_relic", group: "neow",
    parent: "neowOffer", label: "and pulls", poolKind: "relics",
    pool: relicPool("SHARED_CAPSULE_RELICS", "CHAR_CAPSULE_RELICS"),
    sections: relicSections("SHARED_CAPSULE_RELICS", "CHAR_CAPSULE_RELICS", rowRarityLeaves),
    impliesChar: relicOwner("CHAR_CAPSULE_RELICS"),
    enabledWhen: (s) => s.neowOffer === "largecapsule",
    emit: (v) => `large_relic=${v}`, parse: (v) => v },

  { id: "paperweightCard", kind: "picker", specKey: "paperweight_card", group: "neow",
    parent: "neowOffer", label: "offers", poolKind: "cards",
    pool: (s) => tablesFor(s).colorless.COLORLESS_SOLO.map(([id]) => id),
    sections: pickerSections((s) => tablesFor(s).colorless.COLORLESS_SOLO.map(([id]) => id), rowRarityLeaves("COLORLESS_SOLO")),
    enabledWhen: (s) => s.neowOffer === "leadpaperweight",
    emit: (v) => `paperweight_card=${v}`, parse: (v) => v },

  { id: "arcaneCard", kind: "picker", specKey: "arcane_card", group: "neow",
    parent: "neowOffer", label: "the rare is", poolKind: "cards",
    pool: charPool("CHAR_RARES_SOLO"),
    sections: pickerSections(charPool("CHAR_RARES_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "arcanescroll" && !!s.char,
    emit: (v) => `arcane_card=${v}`, parse: (v) => v },

  { id: "cofferCard", kind: "picker", specKey: "coffer_card", group: "neow",
    parent: "neowOffer", label: "offers card", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "lostcoffer" && !!s.char,
    emit: (v) => `coffer_card=${v}`, parse: (v) => v },

  { id: "cofferPotion", kind: "picker", specKey: "coffer_potion", group: "neow",
    parent: "neowOffer", label: "potion", poolKind: "potions",
    pool: potionPool,
    enabledWhen: (s) => s.neowOffer === "lostcoffer" && !!s.char,
    emit: (v) => `coffer_potion=${v}`, parse: (v) => v },


  // Per-reward card picks (Kaleidoscope grants 2 card rewards of 3 offers
  // each). The two slots carry ONE joint kaleido_distinct=a+b fragment (the
  // SCROLLBOX_CONTAINS pattern): the lowest set slot emits it, the other emits
  // nothing, so distinct picks across characters are expressible. Pool is the
  // union of every other character's rollables, decoupled from kaleidoFrom.
  { id: "kaleidoCard1", kind: "picker", group: "neow",
    parent: "neowOffer", label: "offers card", poolKind: "cards",
    pool: distinctPool(kaleidoUnionPool, KALEIDO_DISTINCT, "kaleidoCard1"),
    sections: pickerSections(distinctPool(kaleidoUnionPool, KALEIDO_DISTINCT, "kaleidoCard1"), charRarityLeaves, otherChars),
    enabledWhen: (s) => s.neowOffer === "kaleidoscope" && !!s.char,
    emit: containsEmit("kaleido_distinct", KALEIDO_DISTINCT, "kaleidoCard1") },

  { id: "kaleidoCard2", kind: "picker", group: "neow",
    parent: "neowOffer", label: "and offers card", poolKind: "cards",
    pool: distinctPool(kaleidoUnionPool, KALEIDO_DISTINCT, "kaleidoCard2"),
    sections: pickerSections(distinctPool(kaleidoUnionPool, KALEIDO_DISTINCT, "kaleidoCard2"), charRarityLeaves, otherChars),
    enabledWhen: (s) => s.neowOffer === "kaleidoscope" && !!s.char,
    emit: containsEmit("kaleido_distinct", KALEIDO_DISTINCT, "kaleidoCard2") },

  { id: "newleafCard", kind: "picker", specKey: "newleaf_card", group: "neow",
    parent: "neowOffer", label: "transforming a Basic gives", poolKind: "cards",
    pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "newleaf" && !!s.char,
    emit: (v) => `newleaf_card=${v}`, parse: (v) => v },

  // Up to three "a bundle contains X" narrowers: a SINGLE bundle must hold all
  // the chosen cards (same-bundle subset — the joint scrollbox_contains=a+b+c
  // predicate). No specKey: the lowest set slot carries the joint fragment (via
  // containsEmit) and the others emit nothing, so reconstruct routes the joint
  // key explicitly. Each pool excludes the sibling picks (no duplicate cards).
  { id: "scrollboxCard1", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "scroll boxes", label: "a bundle contains", poolKind: "cards",
    pool: containsPool(SCROLLBOX_CONTAINS, "scrollboxCard1"),
    sections: pickerSections(containsPool(SCROLLBOX_CONTAINS, "scrollboxCard1"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "scrollboxes" && !!s.char,
    emit: containsEmit("scrollbox_contains", SCROLLBOX_CONTAINS, "scrollboxCard1") },

  { id: "scrollboxCard2", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "scroll boxes", label: "and contains", poolKind: "cards",
    pool: containsPool(SCROLLBOX_CONTAINS, "scrollboxCard2"),
    sections: pickerSections(containsPool(SCROLLBOX_CONTAINS, "scrollboxCard2"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "scrollboxes" && !!s.char,
    emit: containsEmit("scrollbox_contains", SCROLLBOX_CONTAINS, "scrollboxCard2") },

  { id: "scrollboxCard3", kind: "picker", group: "neow",
    parent: "neowOffer", cluster: "scroll boxes", label: "and contains", poolKind: "cards",
    pool: containsPool(SCROLLBOX_CONTAINS, "scrollboxCard3"),
    sections: pickerSections(containsPool(SCROLLBOX_CONTAINS, "scrollboxCard3"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => s.neowOffer === "scrollboxes" && !!s.char,
    emit: containsEmit("scrollbox_contains", SCROLLBOX_CONTAINS, "scrollboxCard3") },

  { id: "phialPotionA", kind: "picker", specKey: "phial_potion", group: "neow",
    parent: "neowOffer", label: "grants potion", poolKind: "potions",
    pool: potionPool,
    enabledWhen: (s) => s.neowOffer === "phialholster" && !!s.char,
    emit: (v) => `phial_potion=${v}`, parse: (v) => v },

  { id: "phialPotionB", kind: "picker", specKey: "phial_potion", group: "neow",
    parent: "neowOffer", label: "and potion", poolKind: "potions",
    pool: potionPool,
    enabledWhen: (s) => s.neowOffer === "phialholster" && !!s.char,
    emit: (v) => `phial_potion=${v}`, parse: (v) => v },

  { id: "capsuleRelic", kind: "picker", specKey: "capsule_relic", group: "neow",
    parent: "neowOffer", label: "grants relic", poolKind: "relics",
    pool: relicPool("SHARED_CAPSULE_RELICS", "CHAR_CAPSULE_RELICS"),
    sections: relicSections("SHARED_CAPSULE_RELICS", "CHAR_CAPSULE_RELICS", rowRarityLeaves),
    impliesChar: relicOwner("CHAR_CAPSULE_RELICS"),
    enabledWhen: (s) => s.neowOffer === "smallcapsule",
    emit: (v) => `capsule_relic=${v}`, parse: (v) => v },

  // Act bosses: one NextItem per act in the layout walk. Act 1's pool
  // depends on the map, so a boss1 pick implies Overgrowth/Underdocks.
  { id: "boss1", kind: "art", poolKind: "bosses", specKey: "boss1", group: "bosses",
    label: "Act 1 boss", options: (s) => [
      ...tablesFor(s).mirror.BOSS1_OVERGROWTH.map(([id, name]) => [id, `${name} (Overgrowth)`]),
      ...tablesFor(s).mirror.BOSS1_UNDERDOCKS.map(([id, name]) => [id, `${name} (Underdocks)`]),
    ],
    pool: (s) => [...tablesFor(s).mirror.BOSS1_OVERGROWTH, ...tablesFor(s).mirror.BOSS1_UNDERDOCKS].map(([id]) => id),
    emit: (v) => `boss1=${v}`, parse: (v) => v },

  { id: "boss2", kind: "art", poolKind: "bosses", specKey: "boss2", group: "bosses",
    label: "Act 2 boss", options: (s) => tablesFor(s).mirror.BOSS2,
    pool: (s) => tablesFor(s).mirror.BOSS2.map(([id]) => id),
    emit: (v) => `boss2=${v}`, parse: (v) => v },

  // Act 3 has two boss slots under the DoubleBoss ascension (A10+): boss3 is
  // the first, boss3b the extra second (never the same encounter — the game
  // draws it from the pool minus the first, so the selects cross-exclude).
  { id: "boss3", kind: "art", poolKind: "bosses", specKey: "boss3", group: "bosses",
    label: "Act 3 boss", options: (s) => tablesFor(s).mirror.GLORY_BOSSES.filter(([id]) => id !== s.boss3b),
    pool: (s) => tablesFor(s).mirror.GLORY_BOSSES.map(([id]) => id).filter((id) => id !== s.boss3b),
    emit: (v) => `boss3=${v}`, parse: (v) => v },

  { id: "boss3b", kind: "art", poolKind: "bosses", specKey: "boss3b", group: "bosses",
    label: "Act 3 2nd boss (A10)", options: (s) => tablesFor(s).mirror.GLORY_BOSSES.filter(([id]) => id !== s.boss3),
    pool: (s) => tablesFor(s).mirror.GLORY_BOSSES.map(([id]) => id).filter((id) => id !== s.boss3),
    emit: (v) => `boss3b=${v}`, parse: (v) => v },

  { id: "ancient2", kind: "art", poolKind: "ancients", specKey: "ancient2", group: "ancients",
    label: "Act 2 ancient", options: (s) => tablesFor(s).mirror.ANCIENTS_ACT2,
    pool: (s) => tablesFor(s).mirror.ANCIENTS_ACT2.map(([id]) => id),
    emit: (v) => `ancient2=${v}`, parse: (v) => v },

  { id: "ancient3", kind: "art", poolKind: "ancients", specKey: "ancient3", group: "ancients",
    label: "Act 3 ancient", options: (s) => tablesFor(s).mirror.ANCIENTS_ACT3,
    pool: (s) => tablesFor(s).mirror.ANCIENTS_ACT3.map(([id]) => id),
    emit: (v) => `ancient3=${v}`, parse: (v) => v },

  // What the selected ancient offers (roadmap #13). The event's option rolls
  // are state-conditional in places, so the engine reports two grades: options
  // offered regardless of deck/pet/relic state (ancientN_offers=) and ones
  // offered if a named precondition holds (ancientN_offers_if=). Both grades
  // are sections of this one slot's modal; the section decides the key. A
  // legacy bare `nonu` fragment reconstructs into ancient3Offers=glitter.
  { id: "ancient2Offers", kind: "picker", group: "ancients",
    specKey: "ancient2_offers", specKeys: ["ancient2_offers", "ancient2_offers_if"],
    parent: "ancient2", label: "offers", poolKind: "relics",
    pool: (s) => allOffers(s.ancient2, s, 2),
    sections: (s) => offerSections(s.ancient2, s, 2),
    enabledWhen: (s) => allOffers(s.ancient2, s, 2).length > 0,
    hint: (s) => offerHint(s.ancient2, s.ancient2Offers, s),
    emit: (v, s) => offerEmit("ancient2", 2, s.ancient2, v, s),
    parse: (v, key, s) => offerParse(key, 2, s.ancient2, v, s) },

  { id: "ancient3Offers", kind: "picker", group: "ancients",
    specKey: "ancient3_offers", specKeys: ["ancient3_offers", "ancient3_offers_if"],
    parent: "ancient3", label: "offers", poolKind: "relics",
    pool: (s) => allOffers(s.ancient3, s, 3),
    sections: (s) => offerSections(s.ancient3, s, 3),
    enabledWhen: (s) => allOffers(s.ancient3, s, 3).length > 0,
    hint: (s) => offerHint(s.ancient3, s.ancient3Offers, s),
    emit: (v, s) => offerEmit("ancient3", 3, s.ancient3, v, s),
    parse: (v, key, s) => offerParse(key, 3, s.ancient3, v, s) },

  // Card-package picks: same pool for all six, duplicates allowed
  // (multiplicity). Joint emit from the lowest set slot; reconstruct
  // routes reward_cards= back to slots 1->N.
  ...REWARD_PICK_IDS.map((id, i) => {
    // One pool expression: `sections` must partition exactly what `pool`
    // offers (board-check asserts it), so they cannot be allowed to drift.
    const pool = packagePool(id);
    return {
      id, kind: "picker", group: "package",
      label: i === 0 ? "rewards have" : "and",
      poolKind: "cards",
      pool,
      sections: pickerSections(pool, charRarityLeaves, selectedChar),
      enabledWhen: (s) => !!s.char,
      emit: packageEmit(id),
      // Unreachable today — reconstructStrict matches on
      // `slot.specKeys ?? [slot.specKey]`, which is `[undefined]` for these
      // slots (board.js:1185-1186), so a string key never matches. Carried
      // verbatim anyway so this refactor is provably behaviour-preserving.
      parse: (v) => v,
    };
  }),

  // Exact-fight reward pins: reward1 is convention-free (row 1 is a forced
  // Monster row); reward2/3 assume consecutive monster fights, same
  // convention as the package window.
  { id: "reward1", kind: "picker", specKey: "reward1_card", group: "package",
    label: "fight 1 reward has", poolKind: "cards", pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => !!s.char,
    emit: (v) => `reward1_card=${v}`, parse: (v) => v },

  { id: "reward2", kind: "picker", specKey: "reward2_card", group: "package",
    label: "fight 2 reward has", poolKind: "cards", pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => !!s.char,
    emit: (v) => `reward2_card=${v}`, parse: (v) => v },

  { id: "reward3", kind: "picker", specKey: "reward3_card", group: "package",
    label: "fight 3 reward has", poolKind: "cards", pool: charPool("CHAR_ROLLABLE_SOLO"),
    sections: pickerSections(charPool("CHAR_ROLLABLE_SOLO"), charRarityLeaves, selectedChar),
    enabledWhen: (s) => !!s.char,
    emit: (v) => `reward3_card=${v}`, parse: (v) => v },

  // The fixed Shop-rarity third relic slot every merchant sells: its offer order
  // is seed+character-determined (reverse of the up_front-shuffled Shop deque).
  // Multi-pick: each relic is a clean shop_relic= fragment (the largeRelic
  // pattern); the shared window rides a trailing shop_within= pushed once by
  // compile(). distinctPool greys already-picked relics AND makes the generic
  // reconstruct loop reject duplicates via its pool check.
  ...SHOP_PICK_IDS.map((id) => ({
    id, kind: "picker", specKey: "shop_relic", group: "shops",
    label: "shop relic slot has", poolKind: "relics",
    pool: distinctPool(
      relicPool("SHARED_SHOP_RELICS", "CHAR_SHOP_RELICS"), SHOP_PICK_IDS, id),
    sections: distinctSections(
      relicSections("SHARED_SHOP_RELICS", "CHAR_SHOP_RELICS", flatLeaves), SHOP_PICK_IDS, id),
    impliesChar: relicOwner("CHAR_SHOP_RELICS"),
    emit: (v) => `shop_relic=${v}`,
    parse: (v) => v,
  })),

  // The relic-reward deques (C/U/R): "this relic is among the next N of its
  // rarity the run dispenses". Front-anchored (PullFromFront), unlike the
  // shop's reversed tail. Each pick is its own bag_relic= fragment; the shared
  // window rides a trailing bag_within= pushed once by compile().
  ...BAG_PICK_IDS.map((id) => {
    // One pool expression: `sections` must partition exactly what `pool`
    // offers (board-check asserts it), so they cannot be allowed to drift.
    const bagPool = distinctPool(charPool("CAPSULE_RELICS"), BAG_PICK_IDS, id);
    return {
      id, kind: "picker", specKey: "bag_relic", group: "bag",
      label: "relic rewards have", poolKind: "relics",
      pool: bagPool,
      // Rarity is the thing the picker must surface: each pick is checked
      // against its OWN rarity's deque, and the window floor is per-rarity.
      // The manifest carries no rarity (board_ui.js:15-24), so group by it —
      // the offerSections mechanism (board.js:69-74), rendered as headed
      // sections by picker.js.
      sections: pickerSections(bagPool, rowRarityLeaves("CAPSULE_RELICS"), selectedChar),
      enabledWhen: (s) => !!s.char,
      emit: (v) => `bag_relic=${v}`,
      parse: (v) => v,
    };
  }),

  // Act-1 ? rooms serve a seed-determined event queue, so N counts ? ROOMS.
  // Multi-pick: each event emits its own event_in<N>= (N in the key carries the
  // shared window, clamped to floor); a dedicated parse branch accumulates them.
  // effectiveEventMap locks the section to one act-1 map so a cross-map pair
  // (never co-satisfiable) can't be built. `options` drops sibling picks so the
  // "add" <select> never re-offers an already-chosen event (slotSelect reads
  // options, not pool).
  ...EVENT_PICK_IDS.map((id) => ({
    id, kind: "select", group: "events",
    label: "act 1 event",
    options: (s) => {
      const taken = EVENT_PICK_IDS.filter((pid) => pid !== id).map((pid) => s[pid]).filter(Boolean);
      return eventOptions(s).filter(([eid]) => !taken.includes(eid));
    },
    pool: (s) => eventOptions(s).map(([eid]) => eid),
    emit: (v, s) => `event_in${Math.max(s.eventWithin, eventFloor(s))}=${v}`,
    parse: (v) => v,
  })),
];

export function emptyState() {
  const s = { version: MAIN_VERSION, verPinned: false, staleVersion: null, rares: 0, rewardWithin: 1,
    rewardOrdered: false, shopWithin: 1, eventWithin: 1, bagWithin: 1, char: "", ascension: 0 };
  for (const slot of SLOTS) s[slot.id] = null;
  return s;
}

export function slotEnabled(slot, state) {
  return !slot.enabledWhen || slot.enabledWhen(state);
}

// set() patch for a character change: the new char plus a null for every
// impliesChar slot whose value isn't in its pool under the new character. Only
// impliesChar slots — every other slot keeps persist-through-gating (pinned by
// board-check's "fragment dropped, value kept" case). Exported so board-check
// can unit-test it without a DOM.
export function charPatch(state, char) {
  const next = { ...state, char };
  const patch = { char };
  for (const slot of SLOTS) {
    if (!slot.impliesChar || state[slot.id] === null) continue;
    // Pools are evaluated against `next` (every old sibling value still
    // present), so distinctPool's sibling exclusion behaves as it does at
    // render time — and since it excludes `self`, a slot is never tested
    // against a pool that omits its own value.
    if (!slot.pool(next).includes(state[slot.id])) patch[slot.id] = null;
  }
  return patch;
}

// rares=N constrains the fresh-stream card rewards granted by the Neow pick
// itself, so it only means anything when the offer slot holds a relic that
// grants them — it renders and compiles as a narrower of that slot.
const FRESH_REWARD_RELICS = ["kaleidoscope", "lostcoffer", "leadpaperweight"];
export function raresEnabled(state) {
  return FRESH_REWARD_RELICS.includes(state.neowOffer);
}

// Kaleidoscope's rares=N reads per-reward: reward 1 all rare (3) or both
// rewards all rare (6). The strip UI's 1..4 stays for coffer/paperweight.
export const KALEIDO_RARES_OPTIONS = [[0, "any"], [3, "reward 1 all rare"], [6, "both all rare"]];

export function compile(state) {
  const frags = [];
  if (state.version !== MAIN_VERSION) frags.push(`ver=${state.version}`);
  for (const slot of SLOTS) {
    const v = state[slot.id];
    if (v === null || !slotEnabled(slot, state)) continue;
    const f = slot.emit(v, state);
    if (f) frags.push(f);
  }
  // A lone shop_within= with no picks is a bare-context fragment reconstruct
  // bails on, so the push stays conditional on a set pick — but NOT on the
  // character: charless shop picks need their window too, and K picks below a
  // K floor silently search the wrong number of shops.
  if (SHOP_PICK_IDS.some((id) => state[id] !== null))
    frags.push(`shop_within=${Math.max(state.shopWithin, shopFloor(state))}`);
  if (state.char && BAG_PICK_IDS.some((id) => state[id] !== null))
    frags.push(`bag_within=${Math.max(state.bagWithin, bagFloor(state))}`);
  if (state.rares > 0 && raresEnabled(state)) frags.push(`rares=${state.rares}`);
  if (state.char) frags.push(`char=${state.char}`);
  if (scarcityOf(state)) frags.push("scarcity");
  return frags.join(",");
}

/// Scarcity is Ascension 7 (AscensionLevel.cs) and ascensions stack, so the
/// search-affecting flag is a function of the level rather than state of its
/// own. Levels below 7 compile to the exact same spec as A0: the other nine
/// ascensions change nothing the sweep can see.
export function scarcityOf(state) {
  return state.ascension >= 7;
}

// A Bones detail narrower only means something while its relic is pinned as
// one of the two grants.
export function bonesHas(state, id) {
  return state.bonesGrantA === id || state.bonesGrantB === id;
}

// Inverse of compile(): spec string → board state, or null if any fragment
// isn't board-representable (caller falls back to raw-spec mode). Three
// passes like the engine's parse_spec: version first (every table lookup
// below depends on it), then char, because char-gated pools validate
// against it.
function reconstructStrict(spec) {
  const state = emptyState();
  let sawDepth = false; // legacy reward_depth= context, consumed by `legacy`
  let depthVal = 1;
  let sawShopWithin = false;
  let sawBagWithin = false;
  let sawRewardWithin = false;
  let sawRewardOrdered = false;
  let sawVer = false;
  let legacy = null; // legacy reward_card= / reward_card_each=, mapped after the loop
  const frags = spec.split(",").filter(Boolean);

  for (const f of frags) {
    if (f.startsWith("ver=")) {
      if (sawVer) return null;
      const v = f.slice(4);
      if (!resolveVersion(v)) return null; // unknown version → raw mode
      state.version = v;
      sawVer = true;
      state.verPinned = true; // explicit ver= (even ver=MAIN) is an authoritative pin
    }
  }

  for (const f of frags) {
    if (f.startsWith("char=")) {
      const c = f.slice(5);
      if (!charactersFor(state).includes(c)) return null;
      state.char = c;
    }
  }

  outer: for (const f of frags) {
    if (f.startsWith("char=")) continue;
    if (f.startsWith("ver=")) continue; // consumed above
    // A spec is the search, not the run config: it can say scarcity but not
    // which of A7..A10 the player is on. Seven is the level that produces this
    // exact spec, and an `asc=` URL param (applied after reconstruct) refines
    // it when the link carries one.
    if (f === "scarcity") { state.ascension = Math.max(state.ascension, 7); continue; }
    if (f === "nonu") {
      // Legacy fragment: normalize into the generalized offer slot (the
      // gating sweep still enforces ancient3=nonupeipe).
      if (state.ancient3Offers !== null) return null;
      state.ancient3Offers = "glitter";
      continue;
    }
    if (f === "reward_ordered") {
      if (sawRewardOrdered) return null;
      state.rewardOrdered = true;
      sawRewardOrdered = true;
      continue;
    }
    // Legacy any-of (reward_card=) / each-of (reward_card_each=) links:
    // collect here, map onto the package slots after the loop (the depth
    // may come later in the string).
    if (f.startsWith("reward_card=") || f.startsWith("reward_card_each=")) {
      if (legacy !== null) return null;
      const each = f.startsWith("reward_card_each=");
      const v = f.slice(f.indexOf("=") + 1);
      if (!charPool("CHAR_ROLLABLE_SOLO")(state).includes(v)) return null;
      legacy = { card: v, each };
      continue;
    }
    // Joint package fragment → fill the pick slots 1→N. Duplicates ALLOWED
    // (the multiset) — unlike the scrollbox trio.
    if (f.startsWith("reward_cards=")) {
      if (state.rewardPick1 !== null) return null;
      const parts = f.slice(f.indexOf("=") + 1).split(/[+ ]/);
      const pool = charPool("CHAR_ROLLABLE_SOLO")(state);
      if (parts.length < 1 || parts.length > 6
          || !parts.every((p) => pool.includes(p))) return null;
      parts.forEach((p, i) => { state[REWARD_PICK_IDS[i]] = p; });
      continue;
    }
    // Joint "a bundle contains a+b+c" fragment → fill the trio's slots 1→N.
    const containsIds = f.startsWith("scrollbox_contains=") ? SCROLLBOX_CONTAINS
      : f.startsWith("bones_scrollbox_contains=") ? BONES_SCROLLBOX_CONTAINS : null;
    if (containsIds) {
      if (state[containsIds[0]] !== null) return null;
      const parts = f.slice(f.indexOf("=") + 1).split(/[+ ]/);
      const pool = charPool("CHAR_CU_SOLO")(state);
      const uncommons = new Set(charPool("CHAR_UNCOMMONS_SOLO")(state));
      const commons = new Set(charPool("CHAR_COMMONS_SOLO")(state));
      // ≤3 distinct pool cards that fit one bundle: ≤2 commons + ≤1 uncommon.
      if (parts.length < 1 || parts.length > containsIds.length
          || new Set(parts).size !== parts.length
          || !parts.every((p) => pool.includes(p))
          || parts.filter((p) => uncommons.has(p)).length > 1
          || parts.filter((p) => commons.has(p)).length > 2) return null;
      parts.forEach((p, i) => { state[containsIds[i]] = p; });
      continue;
    }
    // Joint kaleido distinct-group fragment → fill the two card slots 1→N.
    const kdIds = f.startsWith("kaleido_distinct=") ? KALEIDO_DISTINCT
      : f.startsWith("bones_kaleido_distinct=") ? BONES_KALEIDO_DISTINCT : null;
    if (kdIds) {
      if (state[kdIds[0]] !== null) return null;
      const parts = f.slice(f.indexOf("=") + 1).split(/[+ ]/);
      const pool = kaleidoUnionPool(state);
      if (parts.length < 1 || parts.length > 2
          || new Set(parts).size !== parts.length
          || !parts.every((p) => pool.includes(p))) return null;
      parts.forEach((p, i) => { state[kdIds[i]] = p; });
      continue;
    }
    // Joint poultice multiset fragment → fill both card slots 1→N. Duplicates
    // ALLOWED (poultice_set=X+X means BOTH transforms are X) — so no
    // `new Set(parts).size` guard, unlike the kaleido and scrollbox legs above.
    const psIds = f.startsWith("poultice_set=") ? POULTICE_SET
      : f.startsWith("bones_poultice_set=") ? BONES_POULTICE_SET : null;
    if (psIds) {
      if (state[psIds[0]] !== null) return null;
      const parts = f.slice(f.indexOf("=") + 1).split(/[+ ]/);
      const pool = charPool("CHAR_ROLLABLE_SOLO")(state);
      if (parts.length < 1 || parts.length > 2
          || !parts.every((p) => pool.includes(p))) return null;
      parts.forEach((p, i) => { state[psIds[i]] = p; });
      continue;
    }
    // Joint bones capsule-set fragment → fill the three relic slots 1→N.
    if (f.startsWith("bones_capsule_set=")) {
      if (state[BONES_CAPSULE_SET[0]] !== null) return null;
      const parts = f.slice(f.indexOf("=") + 1).split(/[+ ]/);
      const pool = charPool("CAPSULE_RELICS")(state);
      if (parts.length < 1 || parts.length > 3
          || new Set(parts).size !== parts.length
          || !parts.every((p) => pool.includes(p))) return null;
      parts.forEach((p, i) => { state[BONES_CAPSULE_SET[i]] = p; });
      continue;
    }
    const m = f.match(/^([a-z0-9_]+)=(.+)$/); // digits: ancient2/ancient3
    if (!m) return null;
    const [, key, raw] = m;
    if (key === "rares") {
      const n = Number(raw);
      // Upper bound is offer-dependent (validated after the loop): 3/6 for
      // kaleidoscope's per-reward toggle, 1..4 for the strip relics.
      if (!Number.isInteger(n) || n < 1 || n > 6 || state.rares !== 0) return null;
      state.rares = n;
      continue;
    }
    if (key === "reward_depth") {
      const n = Number(raw);
      if (!Number.isInteger(n) || n < 1 || n > 4 || sawDepth) return null;
      depthVal = n;
      sawDepth = true;
      continue;
    }
    if (key === "reward_within") {
      const n = Number(raw);
      // Engine accepts 1..=10; the board select speaks 1..6 only.
      if (!Number.isInteger(n) || n < 1 || n > 6 || sawRewardWithin) return null;
      state.rewardWithin = n;
      sawRewardWithin = true;
      continue;
    }
    if (key === "shop_within") {
      const n = Number(raw);
      // Engine accepts 1..=26; the board select speaks 1..6 only.
      if (!Number.isInteger(n) || n < 1 || n > 6 || sawShopWithin) return null;
      state.shopWithin = n;
      sawShopWithin = true;
      continue;
    }
    if (key === "bag_within") {
      const n = Number(raw);
      // Engine accepts 1..=38; the board select speaks 1..6 only.
      if (!Number.isInteger(n) || n < 1 || n > 6 || sawBagWithin) return null;
      state.bagWithin = n;
      sawBagWithin = true;
      continue;
    }
    const em = key.match(/^event_in([1-5])$/);
    if (em) {
      const n = Number(em[1]);
      const filled = EVENT_PICK_IDS.filter((id) => state[id] !== null).length;
      if (filled >= EVENT_PICK_IDS.length) return null;          // > 5 events
      if (filled > 0 && state.eventWithin !== n) return null;     // one window per package
      // eventOptions consults effectiveEventMap (reads already-filled picks),
      // so a cross-map or invalid event is simply not an option → raw. Each
      // event is validated at THIS point in the fragment walk: compile emits
      // map-pinning slots (act/boss1) before the event picks, so round-trips
      // are order-safe, but a hand-written spec pinning the map AFTER an
      // event reconstructs into an honest 0-match board (the map isn't in
      // state yet) rather than raw — a documented asymmetry, now spanning up
      // to 5 event picks whose shared map is established among the siblings.
      if (!eventOptions(state).some(([id]) => id === raw)) return null;
      if (EVENT_PICK_IDS.some((id) => state[id] === raw)) return null; // duplicate
      state[EVENT_PICK_IDS[filled]] = raw;
      state.eventWithin = n;
      continue;
    }
    for (const slot of SLOTS) {
      const keys = slot.specKeys ?? [slot.specKey];
      if (!keys.includes(key) || state[slot.id] !== null) continue;
      // state: the ancient offers slots need the ancient (emitted earlier in
      // the fragment walk) to tell a guaranteed option from a conditional one.
      const v = slot.parse(raw, key, state);
      if (v === undefined) return null;
      if (slot.pool && !slot.pool(state).includes(v)) return null;
      state[slot.id] = v;
      continue outer;
    }
    return null; // unknown key, or every slot for this key already filled
  }

  // Legacy links normalize onto the package slots (nonu precedent:
  // round-trip normalizes rather than string-identity): reward_card= →
  // [X] within depth, reward_card_each= → [X×depth] within depth. Mixing
  // legacy keys with the new reward_within= token is ambiguous → raw.
  if (legacy !== null) {
    if (state.rewardPick1 !== null || sawRewardWithin) return null;
    const n = sawDepth ? depthVal : 1;
    const picks = legacy.each ? Array(n).fill(legacy.card) : [legacy.card];
    picks.forEach((p, i) => { state[REWARD_PICK_IDS[i]] = p; });
    state.rewardWithin = n;
    sawDepth = false; // consumed
  }
  // Token-less reward_cards= (no reward_within=, no legacy key): the engine
  // defaults the window to max(#cards, floor) (search.rs::parse_spec,
  // ctx.reward_within.unwrap_or(cards.len().max(floor))) — mirror it so the
  // round-trip canonicalizes the default into an explicit token instead of
  // falling to raw. A default past the board's 1..6 select (5+ rares) is
  // board-inexpressible → raw, like an explicit reward_within=7.
  if (legacy === null && state.rewardPick1 !== null && !sawRewardWithin) {
    const n = REWARD_PICK_IDS.filter((id) => state[id] !== null).length;
    state.rewardWithin = Math.max(n, packageFloor(state));
    if (state.rewardWithin > 6) return null;
  }
  // A bare reward_depth is engine-legal context, but the board would
  // silently drop it — bail to raw instead.
  if (sawDepth) return null;
  // reward_within=/reward_ordered without picks would compile to nothing → raw.
  if ((sawRewardWithin || sawRewardOrdered) && state.rewardPick1 === null) return null;
  // A floor-violating within (incl. legacy rare-at-depth-1 links) is
  // engine-rejected or board-inexpressible → raw, never clamped: a
  // hand-written below-floor spec surfaces the engine's parse error there.
  if (state.rewardPick1 !== null && state.rewardWithin < packageFloor(state)) return null;

  // Bare shop_within is engine-legal context, but compile() only emits it
  // alongside shop_relic — bail to raw rather than silently drop it.
  if (sawShopWithin && !SHOP_PICK_IDS.some((id) => state[id] !== null)) return null;
  // Below-floor window is engine-legal but board-inexpressible (and the
  // token-less default shop_within=1 lands here when K>=2) → raw, mirroring the
  // reward below-floor bail above. Same for events.
  if (SHOP_PICK_IDS.some((id) => state[id] !== null) && state.shopWithin < shopFloor(state)) return null;
  if (EVENT_PICK_IDS.some((id) => state[id] !== null) && state.eventWithin < eventFloor(state)) return null;
  if (sawBagWithin && !BAG_PICK_IDS.some((id) => state[id] !== null)) return null;
  if (BAG_PICK_IDS.some((id) => state[id] !== null) && state.bagWithin < bagFloor(state)) return null;

  // Same reasoning for rares without a fresh-reward-granting Neow relic
  // (engine-legal, board-inexpressible — a raw URL spec still searches it).
  if (state.rares > 0 && !raresEnabled(state)) return null;
  // Offer-dependent rares bound: kaleidoscope's toggle speaks 3/6 only;
  // the coffer/paperweight strip speaks 1..4.
  if (state.rares > 0 && (state.neowOffer === "kaleidoscope"
    ? ![3, 6].includes(state.rares) : state.rares > 4)) return null;

  // A filled slot whose gate is closed (child without parent, char-gated
  // without char) — or whose emit produces nothing (an offer value that
  // isn't an option of the selected ancient) — would compile to less than
  // the input spec. Not what the author meant: bail to raw.
  for (const slot of SLOTS) {
    if (state[slot.id] === null) continue;
    if (!slotEnabled(slot, state)) return null;
    // Joint-emission slots (no specKey of their own) legitimately emit
    // nothing; for everything else an empty emit means a dropped fragment.
    if (slot.specKey && !slot.emit(state[slot.id], state)) return null;
  }
  return state;
}

// The version a stale link re-pins onto: the UI default when it resolves,
// else MAIN. Exported so the raw-mode path re-pins to the same version the
// board would have (index.html) — the two must never disagree about which
// engine a salvaged link is being read against.
export function repinVersion() {
  return VERSIONS[DEFAULT_UI_VERSION] ? DEFAULT_UI_VERSION : MAIN_VERSION;
}

// A ver= token that is unknown but still version-SHAPED (/^\d+[\d.]*$/) names
// a game version this build no longer carries tables for: a stale share link,
// not a corrupt one. Returns the token, or null when there is nothing to
// re-pin (absent, known, or not version-shaped).
export function staleVersionToken(spec) {
  const v = spec.match(/(?:^|,)ver=([^,]+)/)?.[1];
  if (!v || resolveVersion(v)) return null;
  return /^\d+[\d.]*$/.test(v) ? v : null;
}

// Wraps reconstructStrict with expired-link handling: re-pin the stale ver= to
// the current default and reconstruct against that.
//
// Three outcomes, in order — the ordering is the whole point, because two of
// them used to be conflated:
//
//  1. The re-pinned spec reconstructs whole. The link is STILL VALID; take it
//     with `staleVersion` set and drop NOTHING. This is the common case (most
//     specs survive an engine bump — only ones naming a removed thing don't).
//  2. It doesn't, but the same fragments don't reconstruct under ANY registered
//     version either. Then the failure has nothing to do with the stale stamp:
//     the spec is board-inexpressible, exactly as it would be unstamped, so
//     return null and let the caller fall to raw mode with the ver= re-pinned.
//     Salvaging here is what silently converted `rares=3,neowkaleido` (raw when
//     unstamped) into a board spec by shedding a perfectly valid fragment.
//  3. Otherwise the loss IS version-caused: greedily shed fragments and report
//     them in `droppedFragments` so the UI can name what it removed. A shared
//     search that quietly widens is worse than one that explains itself.
//
// Salvage is a greedy leave-one-out drop: try the full remaining set, and on
// failure remove whichever single fragment lets it parse; if no single removal
// helps (a multi-fragment interaction), shed the first and keep going.
// Terminates because `remaining` only shrinks. A known (or absent) ver= is
// untouched — this delegates straight through.
export function reconstruct(spec) {
  const v = staleVersionToken(spec);
  if (!v) return reconstructStrict(spec);
  const defaultVersion = repinVersion();
  const fragments = spec.split(",").filter((f) => f && !f.startsWith("ver="));
  const under = (ver, frags) => reconstructStrict([`ver=${ver}`, ...frags].join(","));

  const whole = under(defaultVersion, fragments);
  if (whole) {
    whole.staleVersion = v;
    whole.droppedFragments = [];
    return whole;
  }
  // Board-inexpressible regardless of version → raw mode, same as unstamped.
  if (!Object.keys(VERSIONS).some((ver) => under(ver, fragments))) return null;

  let remaining = fragments;
  const dropped = [];
  for (;;) {
    const st = under(defaultVersion, remaining);
    if (st) {
      st.staleVersion = v;
      st.droppedFragments = dropped;
      return st;
    }
    if (remaining.length === 0) return null; // unreachable: the empty spec always reconstructs
    const dropIdx = remaining.findIndex((_, i) =>
      under(defaultVersion, [...remaining.slice(0, i), ...remaining.slice(i + 1)]));
    const cut = dropIdx >= 0 ? dropIdx : 0;
    dropped.push(remaining[cut]);
    remaining = [...remaining.slice(0, cut), ...remaining.slice(cut + 1)];
  }
}
