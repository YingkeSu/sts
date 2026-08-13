// Popular-tab renderer, board_ui.js-style: render-only, data in / callbacks
// out. The reconstruct() gate is sanitization layer 2 (see the design spec):
// an entry renders ONLY if its spec round-trips into board state, and always
// from that state (char + filter chips) — never as raw text. Unparseable or
// schema-stranded specs drop silently.
//
// MAX_ENTRIES, the reconstruct gate and the <button> row stay here: the Saved
// list is uncapped, ungated (it renders a `raw spec` chip instead of dropping),
// and cannot use a <button> row because a delete control cannot legally nest
// inside one. Only the chips are shared.
import { reconstruct } from "./board.js";
import { specChips } from "./chips.js";

const MAX_ENTRIES = 10;

// entries: [{spec, sessions}] from /api/trending. Returns how many rendered
// (0 => caller shows the placeholder). onPick(state) gets the reconstructed
// board state of the clicked entry.
export function renderPopular(container, entries, manifest, onPick) {
  container.replaceChildren();
  let shown = 0;
  for (const { spec, sessions } of entries) {
    if (shown >= MAX_ENTRIES) break;
    const st = reconstruct(spec);
    if (!st) continue;
    shown++;
    const row = document.createElement("button");
    row.className = "popentry";
    const count = document.createElement("span");
    count.className = "popcount";
    count.textContent = sessions === 1 ? "1 search" : `${sessions} searches`;
    row.append(specChips(st, manifest), count);
    row.addEventListener("click", () => onPick(st));
    container.append(row);
  }
  return shown;
}
