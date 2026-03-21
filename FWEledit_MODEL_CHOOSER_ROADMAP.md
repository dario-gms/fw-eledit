# FWEledit - Model Chooser Correction Roadmap

Status base (current):
- Counts diverge from paid tool (`models/gfx/grasses`).
- Package filtering still leaks entries across categories.
- Slow first open in some runs.

Target parity (paid reference):
- `models`: 6033 entries
- `gfx`: 260 entries
- `grasses`: 223 entries
- Stable `PathID` mapping and correct `ID/Name/Uses`.

## Phase 1 - Deterministic Data Source (No Heuristics)
1. Build chooser source only from extracted package roots:
   - `models.pck.files`
   - `gfx.pck.files`
   - `grasses.pck.files`
2. Never infer package by path prefix for chooser rows.
3. For each package, enumerate only supported model extensions:
   - `.ecm`
   - `.ski` (where applicable)
4. Store rows with package-local relative path (same style as paid tool).

Deliverable:
- Raw per-package file count command and in-app count must match.

## Phase 2 - PathID Resolver Rewrite
1. Build normalized lookup from `path.data`:
   - key variants:
     - `relative`
     - `package\relative`
2. Remove package-restrictive fallback that can hide valid mappings.
3. Resolve collisions by deterministic priority:
   - exact `package\relative` match first
   - then `relative`
   - then highest `Uses` in current list
4. Keep unresolved rows as `PathID=0`, `[NOT USING]`.

Deliverable:
- Same model row selected in chooser maps to same `PathID` as paid tool in sampled cases.

## Phase 3 - Usage/ID/Name Consistency
1. Compute `Uses` by resolved path IDs from current list fields (`file_model*`, `file_models*`, `gfx_file` if applicable).
2. Capture representative `ID/Name` from first matching element in active list.
3. Guarantee no cross-item bleed when switching list selection.

Deliverable:
- `Uses` parity spot-check against paid tool on mounts list and at least 2 other lists.

## Phase 4 - Performance & Caching
1. Add package file index cache:
   - key: workspace hash + package + pck timestamp.
2. Persist lightweight index to local cache (`%LOCALAPPDATA%\FWEledit\cache\model_index.json`).
3. Rebuild index only when source `.pck` timestamp changes.
4. UI:
   - virtualized grid binding
   - debounce search filter
   - avoid full row object recreation on every filter change.

Deliverable:
- Chooser open under 1s after warm cache.

## Phase 5 - Validation Harness
1. Add debug action: export chooser rows (`package,path,pathId,id,name,uses`) to CSV.
2. Compare CSV with paid tool snapshots for:
   - first 50 rows
   - last 50 rows
   - random 100 rows.
3. Keep a known-issues list until counts and mappings close.

Deliverable:
- “Parity PASS” checklist in changelog entry.

## Quick triage checklist (when mismatch happens)
1. Confirm package extraction exists:
   - `<workspace>\resources\<pkg>.pck.files`.
2. Count files by extension in extracted folder.
3. Verify in-app package dropdown shows same count.
4. If mismatch:
   - inspect filter predicate
   - inspect normalization keys
   - inspect collision resolver.

