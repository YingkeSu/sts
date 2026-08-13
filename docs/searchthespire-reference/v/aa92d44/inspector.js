// Seed inspector: renders inspect_seed() JSON (spec ids) into a panel,
// resolving names/art through the asset manifest.

const cap = (s) => s.charAt(0).toUpperCase() + s.slice(1);

// "AromaOfChaos" -> "Aroma Of Chaos" for display; ids stay PascalCase in data.
const spaceCamel = (s) => s.replace(/([a-z0-9])([A-Z])/g, "$1 $2");

// `describe` is set once per render (see renderInspector): the board's own
// entryFor, closed over the pinned version, so a chip's tooltip is the same
// text the picker shows for that card or relic.
let describe = null;

function chip(manifest, kind, id) {
  const e = manifest[kind]?.[id];
  const span = document.createElement("span");
  span.className = "chip";
  if (e?.img) {
    const img = document.createElement("img");
    img.src = `assets/${e.img}`;
    img.alt = "";
    span.append(img);
  }
  const title = e?.title ?? cap(id);
  span.append(title);
  const desc = describe?.(kind, id)?.desc;
  if (desc) {
    span.title = `${title}\n${desc}`;
    span.classList.add("hasdesc");
  }
  return span;
}

// A portrait-sized chip: the art carries the identity, the name confirms it.
// Used where the manifest has real art for the thing (bosses, ancients) rather
// than a room icon.
function portrait(manifest, kind, id, fallbackName) {
  const span = document.createElement("span");
  span.className = "chip bigchip";
  const e = id ? manifest[kind]?.[id] : null;
  if (e?.img) {
    const img = document.createElement("img");
    img.src = `assets/${e.img}`;
    img.alt = "";
    span.append(img);
  }
  span.append(e?.title ?? fallbackName);
  return span;
}

// Ancient offers keep the player-state branch intact: a gate that fails
// changes the option pool's WIDTH, and a different width redraws the whole
// slot rather than dropping the gated relic. So a slot either offers one
// certain relic or a handful of alternatives, and the condition picks which.
// Slots that resolve the same way under every configuration are exactly the
// ones the ancientN_offers= filters can search for.
const dropPrefix = (s) => s.replace(/^(requires|assumes): /, "");

// "if you ..." reads as the player's own condition, which is what these are.
// The engine's prose is mixed in shape: most conditions are noun phrases
// ("at least 3 cards Instinct can enchant") that need "you have", but one is
// already a clause ("you still hold your starter relic") that must not get it.
// Only the FIRST condition takes the lead-in; the rest join bare, so a pair
// reads "if you have at least 3 cards Goopy can enchant and at least 5
// removable cards" rather than repeating the stem.
function conditionLabel(assumes) {
  const parts = assumes.map(dropPrefix);
  const lead = parts[0].startsWith("you ") ? parts[0] : `you have ${parts[0]}`;
  // Two clause-shaped conditions would otherwise read "you still hold your
  // starter relic and you still hold the starter card ..."; drop whatever
  // opening words a continuation shares with the lead so the "and" joins the
  // parts that actually differ. Nothing is shared for the noun-phrase pairs
  // (Pael's two deck gates), which pass through untouched.
  const words = lead.split(" ");
  const trimShared = (part) => {
    const w = part.split(" ");
    let i = 0;
    while (i < w.length && i < words.length && w[i] === words[i]) i++;
    return i >= 2 ? w.slice(i).join(" ") : part;
  };
  return `if ${[lead, ...parts.slice(1).map(trimShared)].join(" and ")}:`;
}

// Conditions the reader has told us they will NOT meet, keyed
// `<seed>|<act>|<condition>`. Storing the unmet ones makes the default — every
// box ticked — the empty set, and that default is the honest one: a real deck
// passes these gates almost always (the filters' old both-worlds intersection
// dropped ~53% of genuinely-offered tanx/nonupeipe options for want of a world
// no real deck reaches).
const unmetConds = new Set();

// The offer as the player will actually see it, given which conditions hold.
// Per slot, the first alternative whose conditions are all met — alternatives
// arrive most-gated first and the last is ungated (`ancient_events::collapse`
// only merges when every alternative agrees, and emits an ungated one when it
// does), so this always resolves.
function resolveOffers(offers, met) {
  return offers.map((slot) => slot.find((alt) => alt.assumes.every(met)));
}

function offerLines(manifest, offers, seed, actIdx, rerender) {
  const conds = [...new Set(offers.flatMap((s) => s.flatMap((a) => a.assumes)))];
  const key = (c) => `${seed}|${actIdx}|${c}`;
  const met = (c) => !unmetConds.has(key(c));

  // One line, the whole offer: three relics, one per draw slot (or the three a
  // shuffle event serves as a unit). Enumerating the alternatives instead made
  // the reader assemble the cross-product themselves, and stating a combined
  // condition would need each gate's NEGATION, which the engine never gives.
  const nodes = [];
  for (const alt of resolveOffers(offers, met)) {
    if (!alt.options.length) {
      nodes.push(Object.assign(document.createElement("span"),
        { className: "insp-note", textContent: "(a locked slot)" }));
      continue;
    }
    nodes.push(...alt.options.map((id) => chip(manifest, "relics", id)));
  }
  const out = [line("offers:", nodes)];
  if (!conds.length) return out;

  // The conditions are facts about the player's own deck, so they are the
  // reader's to set. A ticked box beats prose here: the unticked state IS the
  // negation, with no wording to invent.
  const row = document.createElement("div");
  row.className = "insp-conds";
  row.append(Object.assign(document.createElement("span"),
    { className: "insp-note", textContent: "when you get there:" }));
  for (const c of conds) {
    const label = document.createElement("label");
    const box = document.createElement("input");
    box.type = "checkbox";
    box.checked = met(c);
    box.addEventListener("change", () => {
      if (box.checked) unmetConds.delete(key(c));
      else unmetConds.add(key(c));
      rerender();
    });
    label.append(box, dropPrefix(c));
    row.append(label);
  }
  out.push(row);
  return out;
}

function line(label, nodes) {
  const div = document.createElement("div");
  div.className = "insp-line";
  const b = document.createElement("b");
  b.textContent = label;
  div.append(b, " ", ...nodes);
  return div;
}

// A bones-granted offer's contents arrive under d.neow.bones.details[grantId]
// in the same shape the direct-offer field carries, so we wrap it into a
// pseudo-d and reuse that offer's own DETAIL_RENDERERS entry.
const BONES_DETAIL_FIELD = {
  scrollboxes: "scrollbox", smallcapsule: "capsules", largecapsule: "capsules",
  newleaf: "newleaf", kaleidoscope: "kaleido", lostcoffer: "coffer",
  heftytablet: "tablet", arcanescroll: "arcane", leafypoultice: "poultice",
};
function bonesGrantD(id, det) {
  return id === "leadpaperweight"
    ? { neow: { paperweight: det } }
    : { [BONES_DETAIL_FIELD[id]]: det };
}

const DETAIL_RENDERERS = {
  neowsbones: (d, m) => {
    const out = [line("grants:", d.neow.bones.grants.map((id) => chip(m, "relics", id)))];
    // Under presentation order the details assume the presented-first grant is
    // taken first; surface which relic that is.
    const ft = d.neow.bones.first_taken;
    if (ft && d.neow.bones.grants.length > 1) {
      const note = document.createElement("span");
      note.className = "insp-note";
      note.textContent = "(presented first)";
      out.push(line("presented 1st:", [chip(m, "relics", ft), note]));
    }
    for (const id of d.neow.bones.grants) {
      const det = d.neow.bones.details?.[id];
      const sub = det !== undefined ? DETAIL_RENDERERS[id]?.(bonesGrantD(id, det), m) ?? [] : [];
      if (!sub.length) continue; // no offered contents, or an undefined capsule-deck read (no char=, or an underivable deck edit)
      const block = document.createElement("div");
      block.className = "insp-grant";
      const head = document.createElement("div");
      head.className = "insp-line";
      head.append(chip(m, "relics", id));
      block.append(head);
      for (const s of sub) { s.classList.add("insp-detail"); block.append(s); }
      out.push(block);
    }
    out.push(line("curse:", [d.neow.bones.curse
      ? chip(m, "cards", d.neow.bones.curse)
      : document.createTextNode("(depends on reward-screen interaction)")]));
    return out;
  },
  leadpaperweight: (d, m) => [line("offers:", d.neow.paperweight.map((id) => chip(m, "cards", id)))],
  arcanescroll: (d, m) => d.arcane ? [line("the rare:", [chip(m, "cards", d.arcane)])] : [],
  heftytablet: (d, m) => d.tablet ? [line("offers rares:", d.tablet.map((id) => chip(m, "cards", id)))] : [],
  lostcoffer: (d, m) => d.coffer ? [
    line("cards:", d.coffer.cards.map((id) => chip(m, "cards", id))),
    line("potion:", [chip(m, "potions", d.coffer.potion)]),
  ] : [],
  phialholster: (d, m) => d.phial ? [line("potions:", d.phial.map((id) => chip(m, "potions", id)))] : [],
  kaleidoscope: (d, m) => d.kaleido ? [
    line("reward 1:", d.kaleido.slice(0, 3).map(([ch, id]) => chip(m, "cards", id))),
    line("reward 2:", d.kaleido.slice(3).map(([ch, id]) => chip(m, "cards", id))),
  ] : [],
  largecapsule: (d, m) => d.capsules ? [line("pulls:", d.capsules.map((id) => chip(m, "relics", id)))] : [],
  smallcapsule: (d, m) => d.capsules ? [line("pulls:", [chip(m, "relics", d.capsules[0])])] : [],
  leafypoultice: (d, m) => d.poultice ? [
    line("strike →", [chip(m, "cards", d.poultice[0])]),
    line("defend →", [chip(m, "cards", d.poultice[1])]),
  ] : [],
  newleaf: (d, m) => d.newleaf ? [line("first transform →", [chip(m, "cards", d.newleaf)])] : [],
  scrollboxes: (d, m) => d.scrollbox ? [
    line("bundle 1:", d.scrollbox[0].map((id) => chip(m, "cards", id))),
    line("bundle 2:", d.scrollbox[1].map((id) => chip(m, "cards", id))),
  ] : [],
};

function offerBlock(d, manifest, id, slotLabel) {
  const div = document.createElement("div");
  div.className = "insp-offer";
  div.append(line(slotLabel, [chip(manifest, "relics", id)]));
  for (const detail of DETAIL_RENDERERS[id]?.(d, manifest) ?? []) {
    detail.classList.add("insp-detail");
    div.append(detail);
  }
  return div;
}

const ACT_NAMES = [["Overgrowth", "Underdocks"], ["Hive"], ["Glory"]];

// Which acts are expanded, keyed `<seed>|<act>`. Module-level because a
// streaming search re-renders the results table (and the row's inspector with
// it) many times a second — details/open would otherwise snap shut mid-read.
const openActs = new Set();
const seenSeeds = new Set();
const actKey = (seed, i) => `${seed}|${i}`;

// Act 1 is the act a seed is usually chosen for, so it starts open — once per
// seed, at first render. After that the set is the reader's, and a re-render
// (or a fold of act 1) must not undo their choice.
function seedOpenState(seed) {
  if (seenSeeds.has(seed)) return;
  seenSeeds.add(seed);
  openActs.add(actKey(seed, 0));
}


// Room-type fallback glyph + colour (used when the game icon is unavailable).
const KIND_STYLE = {
  monster: { c: "#c0392b", t: "M" },
  elite: { c: "#8e44ad", t: "E" },
  rest: { c: "#27ae60", t: "R" },
  shop: { c: "#e1b12c", t: "$" },
  treasure: { c: "#e67e22", t: "T" },
  unknown: { c: "#7f8c8d", t: "?" },
  boss: { c: "#7b241c", t: "B" },
  ancient: { c: "#2980b9", t: "A" },
};

// Game map icons (NNormalMapPoint.IconName), extracted into assets/map/.
// Boss uses the boss-chest icon; Ancient keeps the glyph (in-game it's
// per-ancient art, not an icon).
const KIND_ICON = {
  monster: "mapmonster",
  elite: "mapelite",
  rest: "maprest",
  shop: "mapshop",
  treasure: "mapchest",
  unknown: "mapunknown",
  boss: "mapchestboss",
};

const SVG_NS = "http://www.w3.org/2000/svg";
const svgEl = (name, attrs) => {
  const e = document.createElementNS(SVG_NS, name);
  for (const [k, v] of Object.entries(attrs)) e.setAttribute(k, v);
  return e;
};

// map = { nodes: [[col,row,kind],...], edges: [[a,b],...] }. Row 0 (Ancient) at
// the bottom, boss at the top. Columns match the game's post-processed layout
// (Center/Spread/Straighten are ported; oracle-verified bit-exact). Nodes use
// the game's own map icons (assets/map/) over the game's node-splat backdrop,
// falling back to letter glyphs if an icon is missing from the manifest.
function renderMap(map, manifest) {
  const { nodes, edges } = map;
  const maxRow = Math.max(...nodes.map((n) => n[1]));
  const CW = 40, RH = 36, R = 11, PAD = 18;
  const w = 7 * CW + PAD * 2, h = (maxRow + 1) * RH + PAD * 2;
  const svg = svgEl("svg", { viewBox: `0 0 ${w} ${h}`, class: "insp-map" });
  svg.style.maxWidth = "100%";
  svg.style.height = "auto";
  const px = (col) => PAD + col * CW + CW / 2;
  const py = (row) => PAD + (maxRow - row) * RH + RH / 2;
  for (const [a, b] of edges) {
    svg.append(svgEl("line", {
      x1: px(nodes[a][0]), y1: py(nodes[a][1]),
      x2: px(nodes[b][0]), y2: py(nodes[b][1]),
      stroke: "#888", "stroke-width": "1.3", "stroke-dasharray": "2.5 2.5",
    }));
  }
  const bg = manifest?.map?.mapnodebackground?.img;
  for (const [col, row, kind] of nodes) {
    const g = svgEl("g", {});
    const icon = manifest?.map?.[KIND_ICON[kind]]?.img;
    if (icon) {
      const s = kind === "boss" ? 32 : 25; // boss marker reads bigger, like the game
      if (bg) {
        g.append(svgEl("image", {
          href: `assets/${bg}`, class: "map-node-bg",
          x: px(col) - (s + 6) / 2, y: py(row) - (s + 6) / 2,
          width: s + 6, height: s + 6,
        }));
      }
      g.append(svgEl("image", {
        href: `assets/${icon}`, class: "map-node-icon",
        x: px(col) - s / 2, y: py(row) - s / 2,
        width: s, height: s, preserveAspectRatio: "xMidYMid meet",
      }));
    } else {
      const st = KIND_STYLE[kind] ?? { c: "#000", t: "·" };
      g.append(svgEl("circle", { cx: px(col), cy: py(row), r: R, fill: st.c }));
      const txt = svgEl("text", {
        x: px(col), y: py(row) + 3, "text-anchor": "middle",
        "font-size": "10", fill: "#fff", "font-weight": "600",
      });
      txt.textContent = st.t;
      g.append(txt);
    }
    const title = svgEl("title", {});
    title.textContent = kind;
    g.append(title);
    svg.append(g);
  }
  return svg;
}

// One labeled block inside an act: a heading and its lines. Sections are what
// make an act scannable — the old flat list of `label: value` gave a boss the
// same visual weight as the 15th elite.
function section(title, lines) {
  const div = document.createElement("div");
  div.className = "insp-sec";
  const h = document.createElement("h5");
  h.textContent = title;
  div.append(h, ...lines);
  return div;
}

// The game draws a resolved ? room with its own icon set, distinct from the
// plain room icons — an event keeps the bare ? mark.
const UNKNOWN_ICON = {
  monster: "mapunknownmonster",
  elite: "mapunknownelite",
  treasure: "mapunknownchest",
  shop: "mapunknownshop",
  event: "mapunknown",
};

// A ? room's resolution, as the icon the game puts on that room. Kills the
// `M T $ E L` legend, which only existed in the source.
function kindChip(kind, manifest) {
  const span = document.createElement("span");
  span.className = "chip roomchip";
  const img = manifest?.map?.[UNKNOWN_ICON[kind]]?.img;
  if (img) {
    const el = document.createElement("img");
    el.src = `assets/${img}`;
    el.alt = "";
    span.append(el);
  }
  // "monster" is the engine's room-type name; the player fights a combat.
  span.append(kind === "monster" ? "combat" : kind);
  return span;
}

// How many elites this act can actually hand you: its elite ROOM count, which
// SwarmingElites (A1+) moves from 5 to 8. Not act.elites.length — that is the
// 15-long draw sequence, most of which no route can reach.
function eliteRooms(act) {
  return act.map ? act.map.nodes.filter(([, , kind]) => kind === "elite").length : 5;
}

// What a collapsed act still tells you: where it goes, who runs it, and how
// many elite ROOMS the map holds. Not act.elites.length — that is the 15-long
// pool order, identical on every seed, and printing it next to an ascension
// that changes the real count would read as a contradiction.
function actDigest(act, d, i, showSecondBoss) {
  const bits = [act.boss];
  if (i === 2 && showSecondBoss) bits.push(`+ ${d.second_boss}`);
  bits.push(cap(act.ancient));
  if (act.map) bits.push(`${eliteRooms(act)} elite rooms`);
  return bits.join(" · ");
}

export function renderInspector(container, seed, d, manifest, characterName, pickId, versionLabel,
                                opts = {}) {
  const ascension = opts.ascension ?? 0;
  // Redraw in place after a condition toggle. Cheap: `d` is already fetched, so
  // nothing re-enters wasm, and the acts' open state lives in `openActs`.
  const rerender = () =>
    renderInspector(container, seed, d, manifest, characterName, pickId, versionLabel, opts);
  // Display name -> manifest id for bosses, built by the caller off the PINNED
  // version's mirror tables. The act layout is the one part of the inspect
  // payload that speaks display names, and slugging them here would be the
  // cross-language string coupling the rest of this file avoids.
  const bossIds = opts.bossIds ?? {};
  describe = opts.describe ?? null;
  container.innerHTML = "";
  container.hidden = false;

  const h = document.createElement("h3");
  h.textContent = `Seed ${seed}` + (characterName ? ` · ${cap(characterName)}` : "")
    + (ascension > 0 ? ` · A${ascension}` : "")
    + (versionLabel ? ` · ${versionLabel}` : "");
  container.append(h);

  const neow = document.createElement("div");
  neow.className = "insp-section";
  neow.append(Object.assign(document.createElement("h4"), { textContent: "Neow" }));
  // Screen order (Neow.cs:281): the two positives the shuffle put first, then
  // the cursed option, which the game always appends last. The payload already
  // carries the bonuses in that order, so reading top to bottom here matches
  // reading top to bottom in game — which is the whole point when someone is
  // checking a seed against the real screen.
  d.neow.bonuses.forEach((id, k) =>
    neow.append(offerBlock(d, manifest, id, `option ${k + 1}:`)));
  neow.append(offerBlock(d, manifest, d.neow.cursed, `option ${d.neow.bonuses.length + 1} (cursed):`));
  container.append(neow);

  if (d.first_rewards) {
    const fr = document.createElement("div");
    fr.className = "insp-section";
    const caption = pickId
      ? `First 3 combat card rewards (after taking ${manifest.relics[pickId]?.title ?? pickId})`
      : "First 3 combat card rewards (straight hallway fights)";
    fr.append(Object.assign(document.createElement("h4"), { textContent: caption }));
    d.first_rewards.forEach((cards, i) =>
      fr.append(line(`reward ${i + 1}:`, cards.map((id) => chip(manifest, "cards", id)))));
    container.append(fr);
  } else if (d.first_rewards === null) {
    // Explicit null: a declared bones_first relic isn't granted on this
    // seed, so the pick's stream position (and thus the rewards) is undefined.
    const fr = document.createElement("div");
    fr.className = "insp-section";
    fr.append(Object.assign(document.createElement("h4"),
      { textContent: "First combat card rewards: undefined for this pickup order" }));
    container.append(fr);
  }

  if (d.shop_relics) {
    const sr = document.createElement("div");
    sr.className = "insp-section";
    sr.append(Object.assign(document.createElement("h4"),
      { textContent: "Shop relic slot (first 4 shops, before Act 3 chest)" }));
    // A null position is a relic only the (unselected) character can own —
    // the shared positions either side of it are still exact.
    sr.append(line("offers:", d.shop_relics.map((id) =>
      id === null ? document.createTextNode("(your character's relic)")
        : chip(manifest, "relics", id))));
    container.append(sr);
  }

  // The second boss only exists from A10 up, so below that the line would be
  // describing a run the player isn't on.
  const showSecondBoss = ascension >= 10;
  seedOpenState(seed);

  d.acts.forEach((act, i) => {
    const det = document.createElement("details");
    det.className = "insp-act";
    // Act 1 is the one a seed is usually chosen for; the rest stay folded until
    // asked for. A reader's own toggles outrank that default.
    const key = actKey(seed, i);
    det.open = openActs.has(key);
    det.addEventListener("toggle", () => {
      if (det.open) openActs.add(key);
      else openActs.delete(key);
    });

    const name = i === 0 ? ACT_NAMES[0][d.act1_map] : ACT_NAMES[i][0];
    const sum = document.createElement("summary");
    const title = document.createElement("b");
    title.textContent = `Act ${i + 1} · ${name}`;
    const digest = document.createElement("span");
    digest.className = "insp-digest";
    digest.textContent = actDigest(act, d, i, showSecondBoss);
    sum.append(title, digest);
    det.append(sum);

    const body = document.createElement("div");
    body.className = "insp-actbody";

    const secs = document.createElement("div");
    secs.className = "insp-secs";

    // Boss and ancient carry real portraits — they are the two things a seed is
    // usually chosen for, and the art identifies them faster than the name.
    const bossLines = [line("boss:", [portrait(manifest, "bosses", bossIds[act.boss], act.boss)])];
    if (i === 2 && showSecondBoss) {
      bossLines.push(line("2nd boss:",
        [portrait(manifest, "bosses", bossIds[d.second_boss], d.second_boss)]));
    }
    // act.ancient is already a manifest id, unlike the boss display names.
    bossLines.push(line("ancient:",
      [portrait(manifest, "ancients", act.ancient, cap(act.ancient))]));
    // What the ancient offers, with the player-state branch intact
    // (`ancient_events::offer_slots`). This replaces the pair of collapsed
    // lists this viewer used to render: a failed gate changes a pool's WIDTH,
    // which redraws the whole slot rather than dropping the gated relic, so
    // "offered no matter what" could only ever name a subset of what the
    // player sees. offerLines() renders the unbranched slots on one line and
    // gives each branch its own, so nothing is left implied.
    if (act.offers?.length) {
      bossLines.push(...offerLines(manifest, act.offers, seed, i, rerender));
    }
    secs.append(section("bosses & ancient", bossLines));

    // The pool holds 15, but an act can only hand you as many elites as it has
    // elite ROOMS (5, or 8 under SwarmingElites) — the rest of the sequence is
    // unreachable, so showing it invites planning around fights that cannot
    // happen. Numbered because the answer is positional: "what is the 3rd
    // elite" was a comma-count through a wrapped paragraph before.
    const eliteChips = act.elites.slice(0, eliteRooms(act)).map((name, k) => {
      const c = document.createElement("span");
      c.className = "chip elitechip";
      // The game's encounter class, parallel to the display name. Carried on
      // the element rather than shown: it is the join key for what the game
      // knows about this fight (title, monsters, HP, moves), and the display
      // name is tckmn's shorthand, which cannot be joined on.
      if (act.elite_ids?.[k]) c.dataset.encounter = act.elite_ids[k];
      const n = document.createElement("i");
      n.textContent = k + 1;
      c.append(n, name);
      return c;
    });
    secs.append(section("elites", [line("in order:", eliteChips)]));

    // Combats stay plain text, but each name is its own element carrying the
    // encounter id — same reason as the elite chips above.
    const combatRun = (names, ids, sep) => {
      const out = [];
      names.forEach((name, k) => {
        if (k > 0) out.push(document.createTextNode(sep));
        const s = document.createElement("span");
        s.className = "combat";
        if (ids?.[k]) s.dataset.encounter = ids[k];
        s.textContent = name;
        out.push(s);
      });
      return out;
    };
    secs.append(section("combats", [
      line("easy:", combatRun(act.easy, act.easy_ids, ", ")),
      line("hard:", combatRun(act.hard, act.hard_ids, " → ")),
    ]));

    if (act.events_shown) {
      const nodes = [];
      act.events_shown.forEach(([name, cond], k) => {
        if (k > 0) nodes.push(document.createTextNode(" · "));
        const s = document.createElement("span");
        s.textContent = spaceCamel(name);
        if (cond) {
          s.className = "insp-gated";
          s.title = `appears if ${cond} when reached`;
        }
        nodes.push(s);
      });
      secs.append(section("events & ? rooms", [
        line("event rooms serve:", nodes),
        line("? rooms resolve:", act.unknown_rooms.map((k) => kindChip(k, manifest))),
      ]));
    } else if (act.events) {
      secs.append(section("events", [
        line("queue (raw, earlier-act visits also delete entries):",
          [document.createTextNode(act.events.map(spaceCamel).join(" · "))]),
      ]));
    }

    if (act.map) {
      const mapWrap = document.createElement("div");
      mapWrap.className = "insp-map-wrap";
      mapWrap.append(renderMap(act.map, manifest));
      if (ascension >= 1) {
        const note = document.createElement("div");
        note.className = "insp-note";
        note.textContent = "8 elites per map (A1+)";
        mapWrap.append(note);
      }
      body.append(secs, mapWrap);
    } else {
      body.append(secs);
    }
    det.append(body);
    container.append(det);
  });
}
