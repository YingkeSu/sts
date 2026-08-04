// Gallery modal for filling a board slot. One modal at a time; lazily
// created, reused per call.
let modal = null;

function build() {
  modal = document.createElement("div");
  modal.id = "picker";
  modal.innerHTML = `
    <div class="picker-box">
      <div class="picker-head"><span></span><button class="picker-close">×</button></div>
      <input class="picker-search" type="text" placeholder="search…">
      <div class="picker-grid"></div>
    </div>`;
  modal.addEventListener("click", (e) => { if (e.target === modal) close(); });
  modal.querySelector(".picker-close").addEventListener("click", close);
  document.addEventListener("keydown", (e) => {
    if (modal.hidden) return;
    if (e.key === "Escape") close();
    // Enter picks the first visible match — but only from the search box, so
    // Enter on a focused cell/close button keeps its native click.
    if (e.key === "Enter" && e.target === modal.querySelector(".picker-search")) {
      modal.querySelector(".picker-grid .picker-cell")?.click();
    }
  });
  document.body.append(modal);
}

function close() {
  modal.hidden = true;
}

// entries: [{ id, name, desc, img, note? }] — the flat union behind the header
// count AND the corpus the search path ranks, so the two are provably the same
// set. sections (optional): either leaves `{ title, entries, note? }` or groups
// `{ title, collapsible, sections: [leaf] }`, exactly one level deep. A group
// derives its count from its leaves at render time — a stored count would go
// stale the moment a sibling pick narrows the pool. When sections are present
// the caller passes their union as entries: a slot's pool() and its shown set
// can legitimately differ (the charless relic pickers keep pool() shared-only
// for reconstruct's benefit while showing every character's relics).
export function openPicker({ title, entries, sections, onPick }) {
  if (!modal) build();
  modal.hidden = false;
  modal.querySelector(".picker-head span").textContent =
    `${title} · ${entries.length} options`;
  const search = modal.querySelector(".picker-search");
  const grid = modal.querySelector(".picker-grid");
  // Only a picker with collapsible groups resizes as you toggle them, so only
  // that one gets a fixed height. Sizing every picker to 80vh would leave a
  // six-option boss picker mostly empty.
  modal.querySelector(".picker-box")
    .classList.toggle("anchored", (sections ?? []).some((sec) => sec.collapsible));

  // Collapsed section INDICES, not titles: the sections array is built once
  // per call and never reordered, so an index cannot collide, and a copy edit
  // to a heading cannot silently reset collapse state.
  const collapsed = new Set(
    sections?.flatMap((sec, i) => (sec.collapsible ? [i] : [])) ?? []);

  const appendCells = (list) => {
    for (const e of list) {
      const cell = document.createElement("button");
      cell.className = "picker-cell" + (e.blocked ? " blocked" : "");
      cell.title = e.blocked ?? e.desc ?? "";
      const img = document.createElement("img");
      img.src = `assets/${e.img}`;
      img.alt = "";
      img.loading = "lazy";
      const nm = document.createElement("span");
      nm.textContent = e.name;
      if (e.note) {
        const note = document.createElement("small");
        note.className = "picker-note";
        note.textContent = e.note;
        nm.append(note);
      }
      cell.append(img, nm);
      cell.addEventListener("click", () => {
        if (e.blocked) return;
        close();
        onPick(e.id);
      });
      grid.append(cell);
    }
  };

  const appendLeaf = (leaf) => {
    if (leaf.title) {
      const h = document.createElement("div");
      h.className = "picker-sect";
      h.textContent = leaf.title;
      grid.append(h);
    }
    if (leaf.note) {
      const n = document.createElement("div");
      n.className = "picker-sect-note";
      n.textContent = leaf.note;
      grid.append(n);
    }
    appendCells(leaf.entries);
  };

  const render = (filter) => {
    grid.innerHTML = "";
    const q = filter.trim().toLowerCase();
    if (q) {
      // One flat ranked list over the WHOLE union, collapsed sections
      // included. Ranking inside each leaf would hand Enter (which takes the
      // first cell) a group-local best while a name-start match sat in a
      // later group. name-start beats word-start beats contains.
      const rank = (name) => {
        const n = name.toLowerCase();
        return n.startsWith(q) ? 0 : n.split(" ").some((w) => w.startsWith(q)) ? 1 : 2;
      };
      const matches = entries.filter((e) => e.name.toLowerCase().includes(q));
      matches.sort((a, b) => rank(a.name) - rank(b.name)); // stable: pool order within rank
      appendCells(matches);
    } else {
      const list = sections ?? [{ title: null, entries }];
      // A group at index i implies every entry in `list` is a group: both
      // builders emit all-groups or all-leaves, never a mix. That is what
      // makes the .picker-group index below line up with the section index.
      list.forEach((sec, i) => {
        if (!sec.sections) { appendLeaf(sec); return; }
        const n = sec.sections.reduce((t, l) => t + l.entries.length, 0);
        const h = document.createElement(sec.collapsible ? "button" : "div");
        h.className = "picker-sect picker-group";
        h.textContent = `${sec.title} · ${n}`;
        if (sec.collapsible) {
          h.setAttribute("aria-expanded", String(!collapsed.has(i)));
          h.addEventListener("click", () => {
            if (collapsed.has(i)) collapsed.delete(i); else collapsed.add(i);
            render(search.value);
            // Re-focus the same heading, or a keyboard user lands on body
            // after every toggle.
            grid.querySelectorAll(".picker-group")[i]?.focus();
          });
        }
        grid.append(h);
        // A collapsed group renders NO cells, not hidden ones: Enter takes
        // grid.querySelector(".picker-cell"), so a hidden-but-present cell
        // would let Enter pick something invisible.
        if (!collapsed.has(i)) sec.sections.forEach(appendLeaf);
      });
    }
    if (!grid.childElementCount) {
      const none = document.createElement("div");
      none.className = "picker-empty";
      none.textContent = `nothing matches “${filter.trim()}”`;
      grid.append(none);
    }
  };
  search.value = "";
  search.oninput = () => render(search.value);
  render("");
  search.focus();
}
