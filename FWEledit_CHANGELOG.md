# FWEledit - Technical Changelog

This file documents the migration and stabilization work done while adapting the editor for Forsaken World.

## 1) FW config compatibility and elements loading
- Adapted loading flow to work with `FW_X.X.X_v608.cfg`.
- Fixed list parsing robustness for FW data where some lists are empty.
- Prevented crashes when navigating empty/uninitialized lists.

## 2) Game-folder-first workflow
- Added folder-based load flow (select game root, auto-locate `elements.data`).
- `AssetManager.SetGameRootFromElements(...)` now binds runtime context to the selected game installation.
- Editor now operates with game-root context instead of only local executable resources.

## 3) PCK extraction and indexing
- Added automatic PCK extraction support using `spck` (`-fw -x`):
  - `resources/configs.pck` -> `resources/configs.pck.files`
  - `resources/surfaces.pck` -> `resources/surfaces.pck.files`
- Added resource indexing over extracted files for deterministic resource resolution.

## 4) PCK-only mode (fallback removal)
- Removed legacy fallback lookups to:
  - local `fELedit/resources`
  - executable-side `resources`
- Resource resolution is now restricted to extracted PCK content under the selected game root.

## 5) Icon pipeline updates
- Added iconset variant discovery (`iconlist_ivtrm` / `iconlist_ivtr0`, `.dds` / `.png`).
- Added icon key normalization and deterministic mapping by index.
- For FW material lists, applied deterministic path-id mapping using FW offset (`file_icon - 2958`).
- Removed heuristic icon selection logic.

## 6) Startup and missing-resource hardening
- Prevented asset loading before a valid game root is selected.
- Missing optional text resources (`language_en`, `buff_str`, `skillstr`, `item_ext_desc`) no longer crash/lock startup.
- Added safer defaults for missing optional datasets.

## 7) DDS stability fixes (load-folder crash fix)
- Hardened DDS decode path for large `iconlist_ivtr0.dds` scenarios.
- Added DXT5 boundary checks to avoid out-of-range pointer/block reads.
- Improved DDS read fallback when `sizeorpitch` is invalid/zero.
- Guarded bitmap creation against invalid raw buffers.
- Result: no crash when loading game folder (runtime stable).

## 8) Project rename / branding to FWEledit
- Solution renamed to `FWEledit.sln`.
- Project file renamed to `FWEledit.csproj`.
- Assembly metadata updated to `FWEledit`.
- Main window text/version label updated to `FWEledit`.
- README title and description updated for FW focus.

## Current status
- Folder load works in PCK-only mode.
- `elements.data` opens and list navigation is stable.
- Remaining functional gap: icon correctness/parity with paid reference editor for all items.

## 9) Usability - remembered game folder
- Added persisted user setting `LastGameFolder`.
- `File > Load...` now opens already pointed to the last successfully loaded game folder.
- Added `File > Load Last Folder` for one-click reopen without browsing.

## 10) List 0 (Added Attribute) display normalization (FW)
- Added FW-specific formatter for `EQUIPMENT_ADDON` types used in v608 private data:
  - `77`
  - `126` to `136`
  - `138` and `139`
- Added skill-token-aware output for skill-related addon types (fallback token `$skill` when unresolved).
- Added typed template labels for list 0 `type` field in right panel (example: `Lv%s + Lv%d`).
- List 0 `name` field in right panel now uses decoded display text (same resolver used in left list).
- Kept raw stored values intact; only UI presentation was adjusted.

## 11) Crash fix - loading/hover null safety
- Fixed `NullReferenceException` in `Extensions.GetItemProps(...)` when `task_items_list` is not initialized yet.
- Added guard clauses in:
  - `Extensions.GetItemProps(...)`
  - `Extensions.GetItemProps2byId(...)`
  - `MainWindow.listBox_items_CellMouseEnter(...)`
- Result: game-folder load no longer crashes due tooltip/property resolution while optional caches are still null.

## 12) List loading performance optimization (Added Attribute)
- Added cache for list 0 decoded names in `MainWindow` (`entryIndex -> displayName`).
- Added runtime cache reset hooks when:
  - opening a new `elements.data`
  - editing list 0 core fields (`id`, `name`, `type`, `num_params`, `param1..3`)
- Added `EQUIPMENT_ADDON` skill-token cache (`skillId -> resolved skill name`) to avoid repeated linear scans in `skillstr`.
- Optimized FW formatter path to resolve skill names lazily only for addon types that actually need them (`77`, `133`, `134`).
- Added `SuspendLayout/ResumeLayout` around left grid repopulation to reduce render overhead while filling rows.

## 13) FW list 0 parity update from paid-editor reference
- Added a targeted FW type mapping pass for list 0 based on real `elements.data` values from your server range (`61567..61596`).
- Corrected wrong semantic mapping that treated FW type `133` as skill-level:
  - `133` -> `All Masteries + %d`
  - `134` -> `All Resistances + %d`
- Added direct FW display mappings for:
  - `75` -> `+damage taken decreased by %d`
  - `79` -> `Increases Damage by %d`
  - `104..110` -> elemental/physical attack fixed values
  - `112..118` -> elemental/physical defense fixed values
  - `144` -> `PVE Intensity +%d`
  - `145` -> `PVE Tenacity +%d`
- Updated both decoded left-list text (`GetAddon`) and right-panel type label (`GetAddonTypeDisplay`) for these FW types.

## 14) Icon mapping parity fix (PCK-first + real path table)
- Replaced heuristic material offset mapping with deterministic FW path mapping:
  - `file_icon` (from `elements.data`) -> `data/path.data` -> icon filename -> `iconlist_ivtr0` atlas lookup.
- Added/used `CacheSave.pathById` as runtime map cache (`pathId -> resource path`).
- Implemented robust `path.data` loader in `AssetManager` (`DIMP` header format, GBK path decoding).
- Updated icon resolution flow in `MainWindow` to always use list-aware resolver for `file_icon` fields (including refresh/edit/import paths).
- Added FW-compatible path-id candidate order for client variance:
  - `id + 1`, then `id`, then `id - 1`.
- Result: icons now match paid-editor reference in validated FW lists (including previously problematic samples such as item `61101` and refine talisman list).

## 15) Documentation policy
- From this point onward, every meaningful change must be appended to this changelog before closing a development cycle.
- Progress entries should include:
  - what changed
  - why it changed
  - impact/result observed

## 16) Description tab (configs.pck-driven, editable)
- Added a new right-side tab set:
  - `Values` (existing field grid)
  - `Description` (new editor panel)
- `Description` tab now reads item descriptions from `item_ext_desc.txt` located under extracted `configs.pck.files`.
- Implemented parser and in-memory mapping:
  - `itemId -> raw description string`
  - keeps order for stable write-back.
- Implemented item-linked description view:
  - selecting an item updates description editor automatically.
  - preview pane shows a legible version with color tags cleaned (`ColorClean`).
- Implemented save flow:
  - `Save Description` writes changes back to `item_ext_desc.txt` in UTF-16 format with expected header markers.
  - runtime cache (`MainWindow.item_ext_desc` / `database.item_ext_desc`) is rebuilt immediately after save so tooltips and lookups reflect updates without reload.
- Notes:
  - build lock can occur if `bin\\Debug\\FWEledit.exe` is running under debugger; validated build output via alternate `OutDir`.

## 17) Description stability + mapping correction pass
- Fixed crash while changing/searching items:
  - root cause was `Extensions.ColorClean(...)` calling `Substring(6)` on short color blocks.
  - added bounds-safe handling for blocks shorter than 7 chars.
- Improved Description tab mapping accuracy:
  - now resolves selected item id from the visible grid ID cell first.
  - fallback to list raw value only when grid cell parse fails.
  - fallback description lookup now uses `Extensions.ItemDesc(id)` when a direct map entry is not found.
- Added safe preview rendering guard (`SafeColorClean`) to prevent UI crash on malformed/partial color tags.

## 18) Description formatted preview (color-tag rendering)
- Upgraded `Description` preview from plain text box to `RichTextBox`.
- Implemented FW color-tag renderer for preview:
  - applies `^RRGGBB` tags to following text segments.
  - ignores short control markers like `^0037` when present before actual color tags.
- Kept top editor raw/legible for direct editing while bottom preview mirrors in-game styled output more closely.
- Preserved crash safety during preview updates with malformed tags.

## 19) Project baseline version set to 0.0.1
- Updated assembly version metadata to new project baseline:
  - `AssemblyVersion`: `0.0.1.0`
  - `AssemblyFileVersion`: `0.0.1.0`
  - `AssemblyInformationalVersion`: `0.0.1`
- Updated ClickOnce application version in project file to `0.0.1.0`.
- Updated runtime version label logic to prefer `AssemblyInformationalVersion`, so the UI now displays `FWEledit v0.0.1`.

## 20) Handoff packaging fix (compile/run on another machine)
- Added project-level `configs\*.cfg` copy rule (`CopyToOutputDirectory=PreserveNewest`) to avoid missing-config runtime failures after fresh compile.
- Included FW-specific config set in handoff package:
  - `FW_X.X.X_v608.cfg`
  - `references.txt`
  - `sequel_scanner.txt`
- Purpose: prevent startup/read failures caused by using an incompatible fallback config.

## 21) Workspace pipeline for real `.pck` editing/sync
- Implemented a per-client temporary workspace under `%LOCALAPPDATA%\FWEledit\workspace\<hash>`.
- Resource loading now prioritizes workspace-extracted files:
  - `configs.pck.files`
  - `surfaces.pck.files`
- Added workspace preparation flow:
  - copies `resources/configs.pck` to workspace
  - extracts it with `spck -fw -x`
  - mirrors `data/path.data` to workspace for deterministic icon/path lookups.
- Added dirty-tracking for resource edits:
  - when `item_ext_desc.txt` is saved, editor now marks `configs.pck` as changed.
- Added save-time sync/repack flow in normal `Save`:
  - keeps existing `elements.data` save behavior.
  - then repacks changed packages from workspace via `spck -fw -c`.
  - copies rebuilt `.pck` back to game `resources` with `.bak` backup.
  - supports `path.data` sync-back when it is marked dirty (future-ready for path edits).
- Result:
  - editor now works against temporary extracted content,
  - and persists changes back into real game package files on save.

## 22) Clone/ID integrity + description save flow correction
- Fixed clone behavior to avoid duplicated IDs:
  - clone now performs deep copy for byte-array fields.
  - cloned items receive automatically generated unique IDs (next free ID in the list, without assuming sequential order).
- Fixed left-grid refresh triggers for lowercase field names:
  - updates now react to `id` / `name` / `file_icon` / `file_icon1` case-insensitively.
  - this resolves the issue where editing `id` in `Values` did not refresh the ID shown in the item list.
- Added ID collision protection during edits:
  - when editing `id` (single edit or Set Value), duplicate IDs are auto-adjusted to the next available free ID.
- Changed Description button semantics:
  - button now stages changes (`Stage Description`) instead of writing file immediately.
  - `item_ext_desc.txt` is now flushed only during main save actions (`File > Save` and `Save As` path flow).
  - staged changes are tracked and written together with main save pipeline, then propagated to workspace/package sync.

## 23) Hardening pass: ID validation, safe save, and error logs
- Added pre-save ID integrity validation:
  - checks each list that has an `id` field.
  - blocks save when duplicate IDs exist inside the same list.
  - also reports non-numeric IDs in ID fields.
- Added safe save pipeline for `elements.data`:
  - writes to temp file first (`*.tmp_fweledit`),
  - creates/updates `.bak`,
  - replaces target only after successful temp write.
  - avoids partially-written/corrupted output on save failures.
- Added persistent error logging:
  - file: `logs/fweledit-errors.log` (next to executable).
  - captures context, exception type/message, and stack trace for save-related failures.

## 24) Fix: name disappearing after manual ID edit
- Root cause:
  - several UI refresh paths still used case-sensitive field checks (`"Name"`/`"ID"`/`"file_icon"`),
    while FW configs commonly use lowercase (`name`, `id`).
- Fix:
  - normalized these checks to case-insensitive `string.Equals(..., OrdinalIgnoreCase)` across refresh/search/update paths.
- Result:
  - editing `id` manually no longer blanks the left-list `Name` column for items that still have valid `name` in `Values`.

## 25) Crash fix: invalid `Set Value` input on numeric fields
- Fixed a crash path when batch applying values with the placeholder text (`Set Value`) or invalid text into numeric fields (e.g. `int32` ID).
- Added guardrails in `Set Value` flow:
  - blocks placeholder/empty batch value,
  - validates value compatibility against field type before applying,
  - requires valid numeric input for `id` fields,
  - wraps `SetValue` calls with safe error handling + logging.
- Result:
  - invalid batch input now shows a user message and no longer crashes the editor.

## 26) Editing UX state model (dirty/invalid/session restore)
- Implemented visual edit-state workflow closer to paid-editor behavior:
  - valid changed fields in `Values` tab are now marked in blue.
  - invalid changes (notably duplicate/invalid `id`) are marked in red and rejected from model write.
  - changed items in left list now receive `* ` prefix.
- Added pending-change lifecycle:
  - any accepted edit marks session as dirty.
  - after successful save, dirty markers are cleared and list names return to normal.
- Added close confirmation:
  - on close with pending changes, editor prompts `Save / Discard / Cancel`.
  - `Save` executes full save pipeline; `Discard` closes without saving.
- Added session restore:
  - on startup, if previous game folder exists, editor auto-loads it.
  - restores last selected list and item ID when available.
  - new persisted user settings:
    - `LastListIndex`
    - `LastItemId`
- `Set Value` batch feature has been disabled in this mode to avoid conflict with the new direct-edit validation flow.

## 27) Session restore + description remap fixes
- Fixed startup restore of last selected item:
  - prevents state persistence from overwriting `LastItemId` during restore phase.
  - now restores using previously saved `LastListIndex` + `LastItemId` reliably.
- Added ID-change description remap:
  - when an item `id` is changed successfully, matching `item_ext_desc` entry is moved from old ID to new ID (when target ID has no explicit description yet).
  - avoids the apparent "description disappeared" behavior caused by description files being keyed by item ID.

## 28) FW-only CFG packaging and lookup
- Project configuration source has been aligned to Forsaken World only.
- Moved legacy Perfect World cfg files out of active project config folder:
  - from: sELedit\\sELedit\\configs\\PW_*.cfg
  - to: ELedit\\_legacy_pw_cfg\\
- Added FW cfg set to active project config folder:
  - FW_X.X.X_v547.cfg
  - FW_X.X.X_v595.cfg
  - FW_X.X.X_v608.cfg
  - FW_X.X.X_v610.cfg
  - FW_X.X.X_v773.cfg
  - FW_X.X.X_v834.cfg
  - FW_X.X.X_v849.cfg
- Updated cfg auto-discovery to FW-only pattern:
  - FW_*_v{version}.cfg
  - removed PW / generic wildcard pattern checks from runtime matching.
- Updated missing-config message to reflect FW-only pattern.
- Updated build content inclusion in project file:
  - FWEledit.csproj now copies configs\\FW_*.cfg (+ eferences.txt and sequel_scanner.txt) instead of configs\\*.cfg.

## 29) Icon loading robustness for multi-client FW roots (v773 case)
- Improved spck.exe discovery:
  - no longer depends only on selected game root.
  - now also searches editor startup path and parent folders.
  - allows opening clients that do not bundle ELedit/tools/spck themselves.
- Fixed extraction retry behavior:
  - 	riedExtractConfigsPck / 	riedExtractSurfacesPck are now set only when extraction preparation succeeds.
  - failed first attempt no longer blocks future retries in same session.
- Improved iconset auto-detection:
  - still prioritizes iconlist_ivtr0 / iconlist_ivtrm.
  - now falls back to any iconlist*.txt with matching .dds/.png atlas.
  - helps FW variants that ship different iconlist base names.

## 30) Icon PathID picker for ile_icon editing (FW 608-focused)
- Added a dedicated icon picker window (IconPickerWindow) to edit icon fields directly.
- New behavior in Values grid:
  - double-click on ile_icon / ile_icon1 value opens picker.
  - picker shows icon atlas thumbnails and corresponding PathID.
  - supports search by PathID or icon filename key.
  - selection applies the chosen PathID back into the field.
- Picker selection model:
  - prefers path.data mapping (PathID -> icon key) for FW clients.
  - falls back to atlas index when path.data mapping is unavailable.
- This aligns the workflow with paid editor behavior (icon selection by image grid, writing PathID rather than manual number typing).

## 31) Icon picker performance + icon usage references
- Optimized icon picker opening speed:
  - removed synchronous full thumbnail generation during picker initialization.
  - picker now opens immediately and loads thumbnails in small timed batches (UI remains responsive).
  - thumbnail image indices are cached per icon key to avoid repeated bitmap extraction.
- Added icon usage references panel inside picker:
  - shows where the selected PathID is already used in the current list.
  - helps avoid duplicated/repeated icon choices.
- MainWindow integration updates:
  - builds per-list PathID -> [item id + name] lookup before opening picker.
  - passes lookup to picker for live reference display.

## 32) Icon picker UX clarity + thumbnail rendering fixes
- Added explicit Select Icon... button in Values tab header:
  - enabled automatically when current field is ile_icon / ile_icon1.
  - removes dependence on hidden double-click discovery.
- Added picker data cache to reduce repeated open cost:
  - icon entry mapping is reused while atlas/path dataset sizes remain unchanged.
- Improved thumbnail rendering reliability:
  - picker now attempts direct atlas crop from sourceBitmap + imageposition first.
  - falls back to existing cache image extraction path.
  - fixes cases where only blank/black placeholders were shown.

## 33) Icon picker UX placement + non-blocking load
- Removed top header Select Icon... button from Values tab.
- Added inline icon-picker button (...) inside ile_icon value cell area:
  - appears only when selected field is ile_icon / ile_icon1.
  - follows grid scroll/resize/current-row changes.
  - avoids consuming global editor space.
- Reworked icon picker loading pipeline for responsiveness:
  - picker now enqueues filtered icon entries and populates ListView in timed batches.
  - window opens immediately and progressively fills icons.
  - includes loading progress text.
- Improved thumbnail extraction reliability:
  - primary render path now crops directly from atlas using tlasIndex and atlas grid (cols, iconWidth, iconHeight).
  - fallback remains cache-based extraction when direct crop is unavailable.
- Added shared in-memory entry cache (between picker openings) to avoid rebuilding icon-path mapping every open.

## 34) Icon picker streamlined UX + faster reuse cache
- MainWindow icon workflow updates:
  - inline picker button text changed to `Select Icon`.
  - icon-usage precompute was reduced to lightweight `PathID -> count` lookup (no heavy full label list build).
- Recreated `IconPickerWindow.cs` in source and simplified layout:
  - removed right-side `Used by` panel that consumed screen space.
  - hover tooltip now shows quick info (`PathID`, icon file, `Used times`).
  - bottom status line shows selected icon info.
- Performance improvements:
  - picker now opens with placeholders and renders thumbnails progressively in background.
  - thumbnails are cached in shared static memory (`PathID -> Bitmap`) and reused on next openings.
  - icon dataset mapping is cached and rebuilt only when path dataset signature changes.
- Result:
  - substantially less freeze on repeated picker openings.
  - cleaner paid-style behavior (info on hover, no extra side panel).

## 35) Icon picker anti-freeze + inline button behavior
- Main grid icon button behavior adjusted:
  - inline icon button text switched back to `...`.
  - button now stays available on the first icon field row (`file_icon`/`file_icon1`) even when user has not clicked that row yet.
  - click opens picker for tracked icon row (not dependent on current active cell).
- Picker freeze reduction:
  - ListView population changed from full synchronous build to incremental timer batches.
  - prevents UI lock when icon dataset is large.
  - rendering starts only after list items are populated, reducing UI message flood.
- Stability:
  - resolved timer type ambiguity (`System.Windows.Forms.Timer` vs `System.Threading.Timer`).

## 36) Icon picker compact grid + hidden render bottleneck fix
- Compact view adjustments:
  - reduced native icon cell spacing in `ListView` (closer to paid editor density).
  - spacing is now reapplied on resize and after list fill.
  - reduced label footprint for icon mode to keep thumbnails tighter.
- Render pipeline fixes:
  - removed synchronous bitmap extraction from UI thread in visible refresh path.
  - thumbnail load now resolves multiple key variants safely (`name`, `name.dds`, `name.png`).
  - shared placeholder duplication was reduced by avoiding per-item black-image inserts.
- Entry mapping robustness:
  - icon key normalization is now consistent between atlas IDs and path-ID fallback paths.
  - atlas validation now checks all valid lookup variants before accepting an entry.

## 37) Values tab anti-refresh guard during item switch
- Added a temporary UI-suppression flag while rebuilding the Values grid on item selection.
- Prevented repeated inline icon-button recalculation during row clear/refill cycles.
- Result:
  - removes the “continuous refresh” feel when clicking different items in list.
  - Description tab behavior remains unchanged.

## 38) Icon cache warmup on game-folder load
- Added icon thumbnail warmup bootstrap immediately after opening a game folder:
  - starts asynchronous preload of icon entries + thumbnails in shared picker cache.
  - uses same key-resolution logic (`name`, `name.dds`, `name.png`) for better hit rate.
- Goal:
  - avoid first-open picker stalls by preparing thumbnails before user clicks `file_icon`.

## 39) Icon warmup thread-safety fix (race condition)
- Fixed concurrent icon-cache access during warmup:
  - added synchronization in `CacheSave.images()` / `ContainsKey()`.
  - prevents background warmup thread and UI thread from mutating/reading image cache at the same time.
- Added warmup cancellation before reloading game resources:
  - stops previous warmup thread before `asm.load()` starts.
- Cleanup:
  - removed legacy duplicate icon-entry builder path from picker internals.

## 40) Atlas-first picker source parity with paid editor
- Switched iconset file preference to paid-style order:
  - `iconlist_ivtr0.dds + iconlist_ivtr0.txt` first.
  - `iconlist_ivtrm` only as fallback.
- Also aligned fallback extension preference to `.dds` before `.png`.
- Purpose:
  - keep icon order/source consistent with WinPCK iconset assets.

## 41) Icon picker UX parity pass (selection/usage/highlight)
- Picker visual updates:
  - switched icon grid background to black.
  - replaced subtle selection border with strong gold highlight.
- Interaction updates:
  - single click now selects and confirms icon immediately (no forced OK click).
- Mapping/usage fixes:
  - PathID mapping now follows stored-ID resolver logic (`+1/0/-1`) to avoid shifted icon selection.
  - `Used times` now also aggregates by resolved icon key, improving accuracy when multiple PathIDs map to the same icon.

## 42) Values flicker reduction when switching items
- Enabled double-buffer on `dataGridView_elems` and `dataGridView_item`.
- Removed redundant `PerformLayout()` during Values rebuild.
- Goal:
  - reduce visible “refresh/piscar” effect while moving between items.

## 43) Icon picker hover/selection repaint optimization
- Improved picker render smoothness by reducing full-surface redraws:
  - hover change now invalidates only old/new icon cell rectangles.
  - selection change now invalidates only affected cells.
- Added pixel-accurate drawing settings for atlas crop rendering:
  - nearest-neighbor interpolation.
  - no smoothing.
- Result:
  - better UI fluency during mouse movement over icon grid.

## 44) List-switch micro-stutter reduction (settings write debounce)
- Navigation state persistence changed from immediate disk write to debounced write:
  - `PersistNavigationState()` now updates settings in-memory and schedules a short timer.
  - disk write (`Settings.Save`) is flushed after idle delay or on close.
- Purpose:
  - remove micro-freezes while switching large lists/items.

## 45) Full list preload cache on initial load (no per-switch rebuild)
- Implemented full list display-row warmup right after `elements.data` is loaded:
  - prebuilds ID / icon bitmap / display name rows for all lists.
  - includes conversation list entries.
- `change_list` now uses cached row blocks instead of recalculating icons/names each switch.
- Cache invalidation:
  - list cache is invalidated when rows become dirty and reset after save.
- Goal:
  - make list switching near-instant, especially for large lists.

## 46) Model Path Mapping + Mount Preview Entry Point
- Added values-grid support for model fields (`file_model*`, `file_models*`):
  - each model PathID now displays as `ID | mapped_path_from_path.data`.
  - keeps stored value numeric internally (safe for save/write).
- Added inline model action on Values tab:
  - new `Preview` button appears on model rows (same inline style as icon picker button).
  - double-click on model value also triggers preview action.
- Added model resource resolution support in asset layer:
  - resource lookup/index now includes `models.pck.files`, `litmodels.pck.files`, `moxing.pck.files`.
  - on-demand extraction now attempts those model packages when path starts with model roots.
  - exposed `ResolveResourcePath(...)` for model preview resolution.
- Preview launching strategy:
  - first tries external viewer from paid editor folder (`SKIPreview_RAE.exe` and fallbacks).
  - if unavailable, falls back to opening resolved `.ecm` via OS association.

## 47) Choice Model workflow (double-click on file_models*)
- Reworked mount/model field interaction to match paid-tool flow:
  - double-click on `file_model*` / `file_models*` now opens **Choice Model** (not preview directly).
  - inline button on model row changed to `...` and opens the same chooser.
- Added model chooser window with core columns:
  - `#`, `Path`, `Name`, `ID`, `Uses`, `PathID`.
  - `Name` shows `[NOT USING]` when no element currently uses that model path.
- Added filters/search in chooser:
  - package filter (`all`, `models`, `litmodels`, `moxing`, etc., inferred from path root),
  - text search by `#`, `path`, `ID`, or `name`,
  - `Keep Only Unused` toggle.
- Added per-list usage indexing for model PathIDs:
  - counts how many entries use the same model path.
  - captures representative `ID` + `Name` for quick context.
- Added display formatting in Values tab for model IDs:
  - model field values render as `PathID | mapped_path`.
  - editor still saves only numeric PathID internally.

## 48) Model chooser UX alignment (double-click only)
- Removed inline model action button from Values tab.
- Kept model chooser entry point only on double-click over `file_model*` / `file_models*` value cell.
- Purpose:
  - match paid-tool behavior and keep Values grid cleaner.

## 49) Model PathID mapping fix (avoid wrong `surfaces` collisions)
- Added model-specific PathID resolver with safe neighbor fallback (`+1/-1`) restricted to model packages:
  - allowed packages: `models`, `litmodels`, `moxing`.
- Updated model value display mapping:
  - `file_model*` now resolves to model path even when raw ID points to adjacent non-model path.
  - display keeps original ID and shows corrected mapped path (`ID | mapped_path`).
- Updated chooser context behavior:
  - current selection now preselects resolved model PathID, not only raw ID.
  - `Uses` aggregation now counts by resolved model PathID, improving consistency with paid tool.

## 50) Version bump + Model Chooser correction roadmap
- Bumped project version baseline to `0.4.9`:
  - assembly version/file version/informational version updated.
  - app fallback display version and designer label updated.
- Added dedicated correction roadmap file for model chooser parity work:
  - file: `FWEledit_MODEL_CHOOSER_ROADMAP.md`.
  - covers phased fixes for package counts, PathID mapping, uses consistency, and performance cache.

## 51) Model chooser source + PathID lookup hardening (Phase 1/2 start)
- Enforced strict per-package file enumeration for model chooser:
  - source is now workspace `resources\<package>.pck.files` only.
  - packages limited to `models`, `gfx`, `grasses`.
  - package inference by path prefix removed from chooser rows.
- Extension filter tightened:
  - `.ecm` always allowed.
  - `.ski` allowed only for `models` package (adjust if paid tool confirms more).
- PathID lookup normalization expanded:
  - adds both `package\relative` and `relative` keys from `path.data`.
  - resolver now prioritizes `package\relative` first, then `relative`.
- Uses aggregation now includes `gfx_file` alongside `file_model*`/`file_models*`,
  and keeps resolved PathID as the aggregation key.
- Added lightweight package file cache (by workspace + package + pck timestamp)
  and debounce for chooser filter updates to reduce UI thrash.
- Impact:
  - removes cross-package leakage in chooser rows,
  - stabilizes PathID resolution order,
  - prepares for count parity validation in next step.

## 52) Model chooser package list + extracted-root fallback
- Restored paid-style package dropdown list (building/configs/.../textures) while keeping strict per-package enumeration.
- Enumeration now prefers extracted roots via `AssetManager.GetExtractedPackageRoot(...)`,
  allowing workspace extraction first but falling back to game-root extracted folders when present.
- Cache key updated to track the actual extracted root path used.
- Impact:
  - package dropdown matches paid tool,
  - avoids empty `models` list when workspace extraction is missing but game extraction exists.

## 53) Model chooser extraction visibility + large PCK timeout
- Choice Model now warns once when `models/gfx/grasses` packages are missing extraction but `.pck` exists.
- Extraction timeout for large packages (`models.pck`, `gfx.pck`, `grasses.pck`) increased to 15 minutes
  to avoid premature failure on big archives.
- Impact:
  - clearer feedback when `models` appears empty,
  - improves chance of successful extraction on large installs.

## 54) Model chooser reads PCK index directly (no extraction)
- Added direct PCK index reader for FW packages (handles `.pck` + optional `.pkx`).
- Choice Model now enumerates entries from PCK index tables instead of extracted folders.
- Uses zlib table-entry inflate to recover path strings without unpacking payloads.
- Impact:
  - aligns with paid tool behavior (no extraction required),
  - fixes empty `models` list when extraction was disabled.

## 55) Model chooser extension filter aligned to paid tool
- Choice Model now lists only `.ecm` entries (drops `.ski`).
- Impact:
  - removes inflated counts caused by `.ski`,
  - brings models list closer to paid-tool totals.

## 56) PCK index inflate via Ionic.Zlib (parity fix)
- Switched PCK index entry inflation to `Ionic.Zlib` to match paid tool’s zlib handling.
- Added `Ionic.Zlib.dll` to project dependencies for stable PCK table decoding.
- Impact:
  - restores missing entries when parsing PCK index tables,
  - improves model/gfx/grasses counts toward paid tool parity.

## 57) Build fix - Ionic.Zlib CompressionMode
- Fixed build error by using `Ionic.Zlib.CompressionMode` instead of `System.IO.Compression.CompressionMode`.
- Impact:
  - project compiles with Ionic.Zlib PCK decoding enabled.

## 58) PCK index robustness + diagnostics
- Hardened PCK index reader to avoid `EndOfStreamException` and partial reads.
- Added dual inflate path (zlib + raw deflate fallback) for index entries.
- Added diagnostic log entries for index decode failures in `logs/fweledit-errors.log`.
- Impact:
  - more resilient PCK listing,
  - better visibility into missing entries when counts diverge.

## 59) PCK index reader rebuild (clean implementation)
- Rebuilt PCK index enumeration from scratch with a dedicated reader.
- Added zlib-header detection + raw deflate fallback on entry inflation.
- Kept `.pck + .pkx` concatenation logic but simplified read flow to avoid stream corruption.
- Impact:
  - more stable model listing,
  - groundwork for matching paid-tool counts.

## 60) Choice Model ordering + PCK entry decoding alignment
- PCK index reader now resolves entry sizes using both footer keys and validates bounds before reading.
- Entry path decoding now stops at the first null terminator for cleaner path strings.
- Choice Model list now orders paths descending (matching paid tool ordering) and uses 0-based row numbering.
- Impact:
  - row order matches paid-tool top/bottom comparisons,
  - entry parsing is stricter and less prone to corrupted paths.

## 61) Grasses parity verification notes
- Confirmed grasses list aligns with paid tool after PCK index/ordering fixes:
  - top entry: `LMS\mounts\坐骑鸟笼\精灵坐骑鸟笼.ecm` (VIP[Cbt] Chariot Horse, ID 69182)
  - bottom entry: `LMS\mounts\alaskdog\alaskdog.ecm` (VIP[Cbt] Baby Love Bear, ID 69165)
  - count: 224 entries shown as 0..223 (0-based, matches paid tool display)
- Impact:
  - grasses chooser parity achieved; ready to move to gfx/models.

## 62) Choice Model order now mirrors PCK index (reversed)
- Removed path-based sorting in the chooser and preserved PCK index order.
- Reversed the per-package entry list so newest PCK index entries appear first (paid tool behavior).
- Impact:
  - gfx ordering aligns with paid tool (new entries appear at top),
  - non-ASCII-prefixed entries land near the bottom as in the paid tool list.

## 63) GFX parity verification notes
- Confirmed gfx list aligns with paid tool after reversing PCK index order:
  - top entries: `new\冰凌翅\冰凌翅.ecm`, `new\newwings1\翅膀赤霄.ecm`, `new\newwings1\羽族紫耀羽翅.ecm`
  - bottom section stays in `models\...` as in paid tool screenshots
  - count remains 261 entries shown as 0..260 (0-based display)
- Impact:
  - gfx chooser parity achieved; ready to validate models.

## 64) Choice Model CSV export + models parity validation support
- Added Export CSV button to the Choice Model window to dump the current view:
  - columns: package,path,pathId,id,name,uses.
  - matches on-screen semantics ([NOT USING] / - for unused entries).
- Use the export to compare paid-tool lists (first/last/random rows) for models/gfx/grasses.
- Impact:
  - parity verification is now deterministic without manual screenshot diffing.

## 65) Double-click model/icon fields from any column
- Choice Model now opens when double-clicking anywhere on a model/icon field row (not just the Value column).
- Impact:
  - matches paid tool behavior for ile_model* / ile_models* fields.

## 66) Choice Model stays on selected row after open
- Suppressed initial filter debounce while the model picker is initializing.
- Impact:
  - opening the chooser no longer jumps back to the top after 1 second.

## 67) Model chooser icons + faster startup load
- Added item icon column to the Choice Model list (uses representative item's ile_icon).
- Deferred full list display warmup so initial loading screen stays responsive.
- Impact:
  - model picker now matches paid tool icon visibility,
  - startup load no longer stalls on list cache prebuild.

## 68) Support model_name_* fields in Choice Model
- Treated model_name_1..6 as model fields (same chooser behavior as file_models).
- Impact:
  - aircraft mounts now open the model picker on double-click.

## 69) Missing-icon placeholder matches paid tool
- Replaced black blank icons with the QMark stamp for missing/unknown icons.
- Applied in list view and Choice Model icon column.
- Impact:
  - empty icon slots now match paid tool visual behavior.

## 70) Custom no_icon.png placeholder
- Added 
o_icon.png resource and switched missing-icon fallback to it.
- Impact:
  - empty icon slots show the new stamp instead of a black square.

## 71) Added Attribute type picker
- Double-clicking the 	ype field in Added Attribute now opens a type picker.
- Picker lists all type IDs up to the max used in list 0, with readable labels.
- Impact:
  - users can choose addon types without memorizing IDs.

## 72) Added Attribute type picker reduces Not using spam
- Type picker now lists only types that exist in list 0 or are declared in ddon_table.txt.
- Unknown types without hints are suppressed instead of filling the list with placeholders.
- Impact:
  - chooser is closer to paid tool (less noise, still complete for known types).

## 73) Added Attribute type picker translation layer + button contrast
- Added translation lookup order for addon type hints:
  - `addon_table_en.txt`, then `addon_table_pt.txt`, then `addon_table.txt`.
  - looks in both app `resources\data` and game `data` directories.
- If only Chinese hints are available, they are ignored to avoid unreadable lists.
- Fixed OK/Cancel button styling so text stays visible on dark backgrounds.
- Impact:
  - picker can show English/Portuguese labels when a translation file is present,
  - buttons are readable across themes.

## 74) Added Attribute type picker opens at current type
- Type picker now reads the raw `type` value from the selected element (not the display text).
- Impact:
  - double-clicking `type` highlights the correct option immediately, matching paid tool behavior.

## 75) Type picker now scrolls to the selected type on open
- Ensured the picker re-selects and scrolls to the current type after the window is shown.
- Impact:
  - the list opens focused on the active type, not at the top.

## 76) Added Attribute shows correct type/name text (prefers raw data + addon_table)
- List 0 display now uses the raw `name` field when present instead of recomputing via localization.
- Type display in the Values grid prefers `addon_table_*` hints before legacy mappings.
- Impact:
  - Added Attribute names/types align with paid tool for entries like 7056 (Skill %s Lv%d) and Crit Chance.

## 77) Added Attribute now uses paid-tool localization file if available
- Localization loader now falls back to the paid tool `language_en.txt` when game resources don't ship it.
- The Added Attribute list display goes back to computed strings (type/params) instead of raw CN names.
- Impact:
  - English addon names/types align with paid tool when the paid tool localization file is present.

## 78) Mirror paid-tool language file into local resources
- When the paid tool `language_en.txt` is found, it is copied into the editor `resources\data` folder.
- Impact:
  - Added Attribute stays in English even without the paid tool nearby,
  - simplifies handoff/packaging for testers.

## 79) Version label now reflects actual build
- Updated `AssemblyInformationalVersion` so the UI version label matches the compiled build.
- Impact:
  - avoids confusion when validating fixes (no more showing 0.6.8 after rebuilding).

## 80) FW Added Attribute crit/skill types mapping corrected
- Added FW-specific handling for types `81`, `84`–`88`:
  - `81` now maps to `Skill %s Lv%d` (same as type `77`).
  - `84` Crit Chance, `85` Crit Damage, `86` Crit Dodge, `87` Crit Defense, `88` Movement Speed.
- Percentage formatting now matches paid tool (`%.1f%%` for crit types, `%.2f` for movement speed).
- Impact:
  - entries like ID 7056 and Crit Chance now match paid tool wording and values.

## 81) FW elemental resistances mapping corrected
- Types `21`–`27` now map to FW elemental resistances:
  - `21` Physical, `22` Earth, `23` Water, `24` Fire, `25` Wind, `26` Light, `27` Darkness.
- These no longer show PW-only labels like “Metal/Wood Resistance”.
- Impact:
  - entries such as ID 3145 now display `Wind Resistance +%d` like the paid tool.

## 82) FW Attack/Defense/Evasion mapping corrected
- Added FW overrides for:
  - `3` Attack, `4` Defense, `90` Evasion.
- Prevents PW inherited labels like “Magic Attack” and “Slaying Level”.
- Impact:
  - entries like ID 51 (Attack) and ID 54 (Evasion) now match the paid tool.

## 83) FW Health/Mana and percent mapping corrected
- Added FW overrides for:
  - `1` Health, `2` Mana.
  - `37` Health %, `39` Attack %.
- Impact:
  - IDs like 1608 now show `Health +100` instead of max attack,
  - percent-based entries (e.g. 8542/8543) align with the paid tool.

## 84) FW Added Attribute mitigation/accuracy types mapped
- Added FW overrides for:
  - `89` Accuracy +%d
  - `99` Reduces drop rate of Wrath orbs when hit by +%.1f%%
  - `141` Decreases direct damage by %d
  - `142` Decreases reflected damage by %.1f%% plus %d
  - `143` Reduces Damage over Time by %.1f%%
- The addon type now prefers the list `type` field over `addon_table.txt` when present, avoiding PW mismatches.
- Impact:
  - IDs like 8572/8569/8570/8571/8865 display the same text as the paid tool.

## 85) FW Added Attribute type list aligned to paid tool
- Added a full type range (0â€“145) for the Added Attribute chooser so the list matches the paid tool (unused types show `Not using X`).
- For Added Attribute types, the chooser now prefers FW display mappings and, when unmapped, derives a label from the first matching entry.
- Corrected mana-cost type formats (`123`â€“`125`) to use integer percent (`-%d%%`) to match FW data.
- Removed duplicate addon-type cases to avoid ambiguous handling.

## 86) FW Added Attribute percent params decoded in Values grid
- Added decoding of float-packed params for percentage-based addon types in list 0 (`param1/param2/param3`).
- The Values grid now shows human floats like `0.05` instead of raw int bits (e.g., `1028443341`).
- Editing float values writes back the packed int, while large raw ints are still accepted.
