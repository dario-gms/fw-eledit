# FWEledit What's New History

This file keeps the older release notes that used to live in the README. The README now shows only the newest release notes so it stays easier to scan.

## What's New in v0.9.5.23

- Added per-client resource maps and warmer reference/list caches to make repeat opens, list switching, and package-backed description reads faster without relying on manually extracted files.
- Expanded parser, selector, reference, and inherited-icon coverage for service and recipe lists, including `Pet Bedge`, `Pet Food`, `Merge Recipe`, `NPC Hotel`, `Produce Type`, `NPC Learn Produce`, `NPC Talk Service`, `Aircraft`, `Vehicle`, and `Random Gift Bag`.
- Fixed `NPC_TALK_SERVICE.id_dialog` so it resolves against real conversation/dialog entries and opens a dialog-specific picker instead of the generic all-list item selector.
- Improved description editing reliability with normal undo behavior, `configs.pck` save confirmation, reload-safe package writes, and restored in-game update flow.
- Refined Advanced Title handling so graphic titles can be controlled separately for player/chat display while preserving text-line formatting and avoiding unintended icons on system messages.
- Added `COLOR_PLAN_CONFIG` swatches in aircraft color fields, plus clearer Yes/No and gender displays for aircraft-related fields.
- Project/app version metadata updated to `v0.9.5.23`.

---

## What's New in v0.9.5.22

- Added a built-in PCK Explorer / Importer with live 3D preview, package type filters, persistent filter selection, existing-client model highlighting, destination package selection, progress feedback, safer package updates, and detailed import audit summaries.
- Improved model, effect, mount, handhold, weapon, icon, and item-package workflows with automatic cross-PCK dependency resolution, package-aware model picking, gender-aware equipment/fashion previews, and more reliable imports for assets stored outside their original package.
- Reworked description handling so item descriptions are loaded from and saved back to `configs.pck` without requiring manually extracted files, while keeping cached browsing fast and supporting clone/delete description cleanup.
- Expanded the Advanced Title Editor with graphic title controls, chat-display toggles, improved title formatting preservation, and safer script/surface package updates.
- Added `Edit > Clear Cache`, refreshed the About window, cleaned up visible Tools menu entries, and improved equipment-field editing for percentages, using-type masks, and faster `Refine` / `Other` tab loading.
- Project/app version metadata updated to `v0.9.5.22`.

---

## What's New in v0.9.5.21

- Added the advanced Title Editor under Tools for `title_def_u.lua`, with searchable title rows, category editing, color rendering, bonus fields, live preview, and graphic-title controls.
- Added PCK-only title editing: the editor reads `title_def_u.lua` from `script.pck`, saves it back through the package update flow, creates backups, and validates/reloads the result without relying on extracted resource folders.
- Added graphic-title import from PNG/GIF/SVG into game-ready TGA assets stored through `surfaces.pck`, including automatic backup handling and preview refresh.
- Added a graphic icon selector for existing title graphics, plus an explicit `Enable graphic title` toggle so normal and graphic titles can be switched intentionally.
- Added graphic texture-size presets and custom width/height options for imported title graphics, making it easier to tune how titles scale in game while preserving aspect ratio.
- Improved the advanced title UI with more editing space, graphic thumbnails in the title list, better category handling, rendered title colors/descriptions, and higher-quality graphic previews.
- Project/app version metadata updated to `v0.9.5.21`.

---

## What's New in v0.9.5.20

- Added and polished the first `npcgen.data` editor under Tools, with NPC/monster rows, controllers, position/direction/ext fields, grouped parameters, resource rows, attach rows, save support, and tighter integration with the main editor's icon and name-color lookups.
- Added the secondary-instance launcher and replaced the old single last-folder action with recent-folder shortcuts for the last five opened client folders plus a clear-history option.
- Improved `[40] NPC Transmit Service` readability with parser/selector support for linked transmit targets, better service references, and inherited icons from NPCs that use the service.
- Added `NPC_ESSENCE.id_src_monster` parsing against `MONSTER_ESSENCE`, including inherited monster icons, hover previews, selector support, and reverse references from the source monster.
- Added `SUITE_ESSENCE` reference coverage, equipment cross-references, and inherited suite icons from the first equipment used by each suite.
- Improved `EQUIPMENT_ADDON` display for critical percentage addons and skill addons, showing real percentage values and resolving `$skill` names through the skill catalog/fallback text in both Values and the Elements list.
- Continued equipment package import/export refinement for model-only and full-structure workflows, including clearer import mode selection, copyable PathID/package dependency summaries, and safer model/package handling for equipment assets.
- Project/app version metadata updated to `v0.9.5.20`.

---

## What's New in v0.9.5.19

- Added the first equipment package export/import workflow for `Equipment Essence`, focused on moving item model assets between Forsaken World clients with `.fweitem` packages.
- Added a standalone equipment package tool and integrated it into the editor through context-menu export and Tools-based import, including full-structure import and models-only import modes.
- Added import progress/result windows, copyable model PathID summaries, package dependency summaries, and safer ID generation that uses the next available item ID near the end of the target list.
- Added FWPck-based package update support with backups, package-existence detection, fallback insertion into `models.pck`, and cache invalidation after package writes.
- Improved package asset discovery for equipment models by following model dependencies such as `.ecm`, `.ski`, `.smd`, `.gfx`, textures, surfaces, normal maps, and shader-side references.
- Improved model preview startup behavior with reusable warm caches, background cache preparation, fewer blocking popups, and better handling of preview errors inside the preview flow.
- Refined model package behavior after testing cross-client imports, keeping imported model data faithful to the package instead of applying speculative Angelica 2 to Angelica 1 field remapping.
- Project/app version metadata updated to `v0.9.5.19`.

---

## What's New in v0.9.5.18

- Added the first integrated `gshop.data` editor, available from Tools and from `Goods in Game shop` references, with search, item icons, editable known fields, save support, and preservation of the unknown binary record data.
- Added GShop-specific item and gift selectors, a dedicated qshop icon picker, currency selection for Eyrda Leaf / Soul Leaf / All Leaves, and sharper shop-icon previews through the improved DDS loading path.
- Expanded `Goods in Game shop` reference support so shop entries participate in the same reference workflow as `elements.data` lists.
- Reworked reference browsing with grouped source-list tabs, automatic refresh while switching elements, smoother repainting, inherited icon resolution, and clearer nonzero/zero reference counts in the Elements list.
- Improved reference coverage for item-bearing lists such as medicine, material, skilltome, transmit roll, NPC sell service, and trade-page style tables.
- Reorganized Equipment values into gameplay-focused groups while keeping important recast and special-addon fields in Values, added parsers/selectors for equip masks, equip location, equip type, file matter/model paths, medicine/material types, and related subtype fields.
- Added Yes/No rendering and double-click toggles for common boolean fields such as transfer chance, alpha fashion equip, can decompose, can auction, and sell for bind money.
- Improved value readability with humanized field labels, large-number dot separators, better tooltip previews, and NPC-specific name coloring.
- Project/app version metadata updated to `v0.9.5.18`.

---

## What's New in v0.9.5.17

- Added a dedicated multiline bottom editor for the `desc` field in `[105] ADDON_PACKAGE_CONFIG`, making line breaks practical without using the old Description tab workflow.
- Removed the separate Description tab for `ADDON_PACKAGE_CONFIG`, keeping that list focused on the actual value-field editor.
- Preserved the selected Values-field row while switching items, so quick comparison of fields such as `desc` no longer requires reselecting the row each time.
- Added rich hover previews for reference values in the Values grid, including target item/config context, icon/title rendering, raw value, source field, and reference-count status.
- Restored an embedded `References` tab in the main editor with lazy loading, cached reference rows, automatic refresh while switching items, and double-click/Enter navigation to referenced records.
- Kept `References` as the last tab even when equipment-specific tabs are shown or hidden.
- Refined tooltip presentation with better spacing, dynamic sizing, border treatment, icon/title alignment, and a cleaner dark-theme look.
- Project/app version metadata updated to `v0.9.5.17`.

---

## What's New in v0.9.5.16

- Switched the editor to start in dark mode by default, matching the current FW-focused UI direction out of the box while still keeping the theme toggle available.
- Added unofficial load support groundwork for FW v773, including newer list/config handling needed to get that branch opening and browsing more safely.
- Improved `WING_ESSENCE` / newer-version parser coverage and related list handling touched by the v773 compatibility pass.
- Reworked item hover tooltips to render closer to the in-game presentation, including richer formatting, better item detail grouping, and proper icon loading inside the tooltip instead of the old generic placeholder look.
- Added mount/flying speed conversion helpers so `VEHICLE_ESSENCE` / `AIRCRAFT_ESSENCE` tooltips and value displays can show gameplay-facing speed percentages and related movement details more clearly.
- Added dedicated color-preview tooling for `COLOR_PLAN_CONFIG`, replacing the old Description-style workflow there with a color-focused preview/generator flow.
- Added `QUENCH_TOOL_ESSENCE` to `QUENCH_CONFIG` reference parsing, so `quench_config_id` now resolves correctly and `QUENCH_CONFIG` rows can show where they are used through `Refs`.
- Project/app version metadata updated to `v0.9.5.16`.

---

## What's New in v0.9.5.15

- Restored reliable item-description loading from `configs.pck` after the recent package-reader transition, including safer startup hydration so the Description tab is populated again instead of appearing empty or stale.
- Hardened `configs.pck` read extraction by automatically re-extracting when required description files are missing and falling back to the stable `spck` read path for that package when the managed package read is not enough.
- Added direct package-entry read helpers and resource-resolution plumbing used by the refreshed description-loading path, reducing dependence on stale extracted leftovers.
- Added `Copy model name` and `Copy model path` actions to the Choice Model right-click menu for quicker reuse of selected preview assets.
- Corrected race-aware model field labels for `AIRCRAFT_ESSENCE`, so `model_name_*` rows now show race names instead of only numbered slots.
- Fixed `PET_BEDGE_ESSENCE.file_icon` to use the normal image/icon picker again, while keeping `file_head_icon` and `file_self_head_icon` on the portrait-style TGA picker.
- Project/app version metadata updated to `v0.9.5.15`.

---

## What's New in v0.9.5.14

- Restored the stable `spck`-based repack path for `configs.pck` / `script.pck` saves, avoiding the package-corruption regressions seen in the recent experimental write path.
- Kept the newer `WinPCK`-based access focused on package reading/runtime lookup workflows while moving the critical save pipeline back to the proven repack toolchain.
- Hardened the standalone `FWPckUpdater` rebuild flow so package rebuilds are assembled root-by-root instead of trying to recreate multi-root packages from a single staging folder call.
- Fixed missing bundled `spck` binaries in the standard `bin\\Debug` output, so the stable repack path is actually available in local test builds without manual file copying.
- Project/app version metadata updated to `v0.9.5.14`.

---

## What's New in v0.9.5.13

- Migrated the package-backed resource workflow toward the new `WinPCK`-based reader/updater path, reducing dependency on the older `spck` extraction flow for runtime reads and package access.
- Improved direct package access when the game client keeps `.pck` files locked, allowing the editor to read package-backed data more reliably from live installs.
- Added cross-package preview asset resolution across all `.pck` files in the `resources` folder.
- Restricted cross-package fallback to exact relative-path matches only, preventing wrong models from loading because of duplicate filenames in other packages.
- Added caching for successful cross-package resolutions to speed up repeated previews.
- Added negative-cache tracking for missing cross-package references, reducing repeated slow scans on unresolved assets.
- Added package-change-aware preview cache invalidation so stale model/path results are less likely to survive after a `.pck` is replaced or rebuilt.
- Improved handling and error reporting for detached preview references that do not resolve from the original package chain.
- Improved nested `.gfx` / `.ecm` / effect-driven preview resolution so referenced assets can be merged more reliably.
- Added black-placeholder texture detection with fallback color promotion for previews that load geometry but would otherwise render fully black textures.
- Improved preview stability for assets with chained FX / GFX references.
- Reduced long preview stalls caused by repeated searches for missing files.
- Improved startup/session-restore rendering by keeping the first visible list responsive while heavier visual hydration catches up after the initial open.
- Reworked the startup progress bar so it now reflects the real loading phases instead of completing long before lists, assets, and restored UI state are actually ready.
- Fixed visible-list scroll artifacts introduced by the lightweight hydration path, preventing rows from visually “cloning” while icons are catching up during fast scrolling.
- Kept the v608 load path as the stable baseline by suppressing the compatibility warning for that validated version while preserving warnings for other older builds.
- Project/app version metadata updated to `v0.9.5.13`.

---

## What's New in v0.9.5.12

- Added the first built-in `tasks.data` reference pipeline, so task-linked fields can now resolve against real task IDs instead of staying as raw numbers.
- Added hierarchical task-picker support with task groups and subtasks, including parent/root context in the picker so nested task chains are easier to understand while editing.
- Added task-targeted reference resolution for task-related fields used by lists such as `DYNAMIC_INSTANCE_CONFIG` and NPC task-service tables.
- Improved task-related target-list matching by recognizing `Equipment` and `EQUIPMENT_ESSENCE` as the same logical list where needed during reference resolution.
- Broadened `elements.data` compatibility for versions below v608 by allowing older versions to load with a warning instead of a hard stop, while still blocking versions above the highest validated build.
- Hardened list-count and auto-offset validation while loading `elements.data`, reducing endless-load / misalignment failures on older configs and surfacing cleaner errors when a structure does not match.
- Kept the new task support on the lightweight path after removing the heavier task-description parsing experiment that was not worth the startup cost.
- Project/app version metadata updated to `v0.9.5.12`.

---

## What's New in v0.9.5.11

- Added direct read access for package-backed resources from `*.pck` / `*.pkx` without requiring full extraction of those packages for browsing and preview workflows.
- Improved package-read stability when the game client is open by keeping the direct managed read path and removing the experimental native WinPCK bridge that introduced preview regressions.
- Fixed 3D model preview regressions after the recent PCK-reading work, restoring reliable preview loading both from the normal editor flow and from the Choice Model window.
- Improved Choice Model preview behavior so the preview window stays tied to the picker instead of falling behind the main editor while switching models.
- Added a `Preview 3D Model` option to the Choice Model right-click menu, reusing the same preview action previously available only from Space / the bottom Preview button.
- Corrected Equipment parsing for `id_quality`, so the field now resolves against the intended quality-ID data instead of being parsed as the wrong reference type.
- Refined selection-history handling around list changes to reduce stray intermediate navigation states when moving across lists and using Back / Forward.
- Project/app version metadata updated to `v0.9.5.11`.

---

## What's New in v0.9.5.10

- Added a built-in Path Editor flow from the Choice Model window, including `Open with Path Editor`, direct `path.data` editing, manual or auto-generated PathID assignment, duplicate-ID safety checks, and backup-aware save support.
- Added support for clearing selected value fields directly from the value grid, including multi-selection clearing, keyboard delete, and proper typed normalization when a cleared field needs to fall back to `0` or an empty value.
- Added multi-field copy/paste and undo support in the value grid, including right-click actions plus `Ctrl+C`, `Ctrl+V`, `Ctrl+Z`, and sequence-aware paste behavior for repeated or downward-applied field values.
- Improved value-grid multi-selection behavior so right-click actions preserve an existing interleaved field selection instead of collapsing it to the clicked row.
- Added multi-item clone support for non-contiguous selections in the Elements list, instead of cloning only the active item.
- Added same-list duplicate-ID highlighting while editing IDs, with bold red visual feedback before save whenever the candidate ID is already used in the current list.
- Hardened item cloning and ID editing so duplicate IDs are rejected across the full dataset where uniqueness is required, including cases where another list already contains the same ID.
- Fixed cloned-item reference refresh so `Refs` counts no longer disappear for long periods after clone operations and update more predictably without waiting for a slow full rebuild.
- Improved reference-index performance by narrowing unnecessary cross-list checks and restoring smoother startup/open behavior after the recent reference and clone work.
- Restored last-list / last-item startup navigation so the editor reopens at the user's previous working position instead of always jumping back to the first list entry.
- Preserved the current value-row position after save, so saving no longer throws the right-side editor back to the top when you are working deeper in a field group.
- Fixed description dirty tracking so description edits are detected reliably, and reverting a description back to its original content now clears the dirty state correctly instead of leaving a false modified flag behind.
- Fixed the Equipment parser layout around `id_special_addon_package` / `color_*`, which exposed misplaced data and made some later fields appear under the wrong names.
- Added parser/reference/picker support for `id_identify`, `id_special_addon_package`, and `extend_identify_attr_tool_*_tool_id`, so those fields now resolve against the correct target lists instead of wrong or overly broad lookups.
- Added refine-tab parsing for `id_identify`, improving readability and edit safety for identify-scroll style equipment fields.
- Project/app version metadata updated to `v0.9.5.10`.

---

## What's New in v0.9.5.9

- Added a full title-definition pipeline backed directly by `script.pck`, so title data is now read from the real game package instead of depending on extracted development files or loose-script fallbacks.
- Added native parsing of `config/title_def_u.lua` from `script.pck`, allowing title-related fields to resolve to real title names, colors, descriptions, bonuses, and graphic-title metadata instead of raw IDs.
- Added a dedicated Title editor inside the title picker, with editable title name, color, graphic path, description, bonus lines, and live preview directly inside FWEledit.
- Added direct Apply/Save support for title editing from the picker itself, so title changes can be written back into `script.pck` without leaving the normal item-editing workflow.
- Bundled the Lua tooling needed by the editor so FWEledit can decompile, edit, and rebuild the title-definition script as part of the app's own save pipeline.
- Added automatic `script.pck` backup creation before title edits are applied, mirroring the existing safety model already used for `elements.data`, `configs.pck`, and related files.
- Fixed the `script.pck` extraction/apply flow so title saves no longer fail on valid installs because of premature extraction errors or package-apply edge cases.
- Improved the title picker UX with better dirty-state tracking, a visible Apply flow, safer navigation prompts, and fixes for false “unsaved changes” warnings while simply browsing titles.
- Expanded picker/result rendering so title rows can show accent colors, richer secondary text, and graphic-title context instead of looking like flat ID/name pairs.
- Added title-aware parsing to `RANDOM_GIFT_BAG_ESSENCE` reward fields, including reward-type handling for title rewards, so title bundles and title-box style items resolve correctly.
- Added title-aware parsing to `TITLE_PROP_CONFIG`-style fields and related title-property references, so title bonus/config rows now display with much better context.
- Improved title-related reference browsing so title-bearing fields participate more naturally in the shared reference/picker workflow.
- Continued the shift toward reading gameplay-facing metadata directly from packaged game resources, including the newer skill/title-oriented lookups that now rely on PCK-backed data rather than local extracted assets.
- Improved skill/buff/title display quality in parser-heavy fields touched by this work, so users see more game-meaningful text and less opaque raw data while editing.
- Continued parser normalization and reference-resolution cleanup for title-driven and reward-driven fields, reducing wrong-list lookups and improving consistency across pickers and rendered values.
- Project/app version metadata updated to `v0.9.5.9`.

---

## What's New in v0.9.5.8

- Added a much deeper parser pass across several FW-heavy tables, including class/race/bind restrictions, pet-related lists, model race labels, and additional item-reference fields that now render as human-readable values instead of raw IDs.
- Added and refined structured pickers for more gameplay-facing fields, including FW profession masks, race masks, bind flags, NPC sell currency types, shop item selection, and other reference-driven value editors.
- Introduced a full `NPC_SELL_SERVICE` workflow with page-aware tabs, NPC portrait icons, page item parsing, currency parsing, and a dedicated Shop Editor for editing page inventory and prices with item icons and value controls.
- Improved cross-list parsing/reference behavior so duplicate IDs, nested trade/drop-style lists, and shop/page references resolve more reliably to the intended FW records.
- Continued UI polish and usability cleanup around value editing, picker affordances, tooltip wording, and the placement/visibility of the Shop Editor action.
- Standardized additional user-facing messages into English, including save confirmation and picker error prompts.
- Project/app version metadata updated to `v0.9.5.8`.

---

## What's New in v0.9.5.7

- Added parser and picker support for skill/buff-style fields, including `SKILLMATTER_ESSENCE.id_skill`, backed by the game's `skillstr.txt` / `buff_str.txt` data instead of raw guesswork.
- Improved `SKILLMATTER_ESSENCE` value display so skill-related rows now show contextual text such as skill usage, level, cast behavior, and item type instead of bare numeric values where possible.
- Expanded portable-service parsing for `HANDY_SIMPLE_SERVICE_ESSENCE` so combined service flags render with game-style service names rather than generic placeholders.
- Reworked `proc_type` display and picker labels to use much friendlier in-game style wording such as bind/trade/sell restrictions.
- Added and refined profession-mask parsing/picking for FW-specific classes used by trade/NPC-related lists.
- Improved NPC trade parsing so nested shop/trade page references render more accurately in pickers and related value fields.
- Extended `ITEM_TRADE_PAGE_CONFIG` handling with better page-level parsing, icon resolution, profession-mask support, and reference integration.
- Improved reference browsing by grouping results into tabs per source list and tightening icon/name rendering for those grouped results.
- Continued performance work on reference/index-heavy workflows with more caching around repeated lookups and parser-heavy UI paths.
- Project/app version metadata updated to `v0.9.5.7`.

---

## What's New in v0.9.5.6

- Added human-readable parsing for `combined_services`, `combined_services2`, `combined_services3`, and `combined_services_*` fields used by portable service tables such as `HANDY_SIMPLE_SERVICE_ESSENCE`.
- Added a dedicated portable-services picker with checkbox selection, raw bitmask preview, and friendly labels for the service flags stored in those fields.
- Integrated portable-service fields into the same picker workflow as other structured values, including the inline `...` button, double-click, and right-click context actions.
- Extended value rendering and save normalization so combined-service bitmasks keep their correct `int32` storage even when high bits produce negative signed values.
- General picker affordance work from the recent UI pass now also covers these bitmask service fields consistently.
- Project/app version metadata updated to `v0.9.5.6`.

---

## What's New in v0.9.5.5

- Replaced the always-on References tab with a lighter `Show references` context action on Elements rows, opening references in a dedicated window.
- Added navigation from the references window so double-clicking or pressing Enter on a reference jumps directly to the source element.
- Restored and accelerated the `Refs` count column using a shared reference index instead of repeated full rescans.
- Added background warmup plus persisted disk caching for the reference index, so reference-heavy workflows no longer need to rebuild everything every launch.
- Added safe cache invalidation for edited IDs, names, qualities, and icons to keep cached lookups fast without showing stale data.
- Added cache layers for icon resolution, field-index lookups, and search suggestions to improve UI responsiveness across repeated navigation and search.
- Corrected reference-window icon resolution so it follows the same path/icon logic as the main Elements panel.
- Project/app version metadata updated to `v0.9.5.5`.

---

## What's New in v0.9.5.4

- Fixed multi-edit Description staging so editing descriptions for multiple selected items persists every selected item instead of only the active row.
- Rendered `item_quality` values with their quality label in the Values grid while preserving the raw numeric value for saving.
- Added friendly display and a mini picker for `gender_type`, `require_gender`, and reward gender fields (`0 = Male`, `1 = Female`, `2 = Female & Male`).
- Allowed the raw `Selected ID` / Value editor to apply the typed value with Enter, in addition to the Set button.
- Project/app version metadata updated to `v0.9.5.4`.

---

## What's New in v0.9.5.3

- Expanded ID parsing for fields that reference other lists, including broader item/reference support across parser-heavy tables.
- Fixed cross-list duplicate-ID resolution so item references prefer the correct item-bearing lists instead of unrelated lists with the same ID.
- Added NPC/Monster portrait support for `NPC_ESSENCE` and `MONSTER_ESSENCE`, resolving `file_icon` PathIDs to TGA portrait assets instead of the normal item icon atlas.
- Added a TGA portrait picker with search, thumbnails, and preview for NPC/Monster `file_icon` values.
- Rendered portrait-backed `file_icon` values with a thumbnail and resolved TGA path while preserving the raw numeric PathID for safe editing.
- Fixed portrait picker updates so choosing a TGA no longer triggers invalid `int32` validation and refreshes the Elements list icon correctly.
- Project/app version metadata updated to `v0.9.5.3`.

---

## What's New in v0.9.5.2

- Added light/dark theme switching with persisted preference across app restarts.
- Refined dark-mode styling for tabs, grids, scrollbars, borders, headers, and editor surfaces.
- Kept the Elements list visually consistent with the item-quality color workflow.
- Fixed model preview actions so preview works outside the Models tab and refreshes when changing equipment.
- Improved selection fluidity by suppressing redraw while the right-side values panel is rebuilt.
- Reduced unnecessary preview/editor refresh work during item changes for smoother navigation.
- Project/app version metadata updated to `v0.9.5.2`.

---

## What's New in v0.9.5.1

- Added equipment-focused tabs beside Values, including Models, Refine, and Decompose groupings.
- Added item-reference resolution for equipment fields so IDs display as item names where possible.
- Added a rich item reference picker with dark list styling, icons, quality colors, list filtering, and global search.
- Added a `Selected ID` editor below the offset field with Set, increment, and decrement controls for safer raw ID editing.
- Updated model value display to show the resolved path while keeping the raw ID editable through `Selected ID`.
- Added icon and quality-colored rendering for referenced item values in the Values grid.
- Improved Description editing with formatting/color shortcut buttons and broader FW tag rendering.
- Cached item-reference lookups to improve scrolling and rendering performance in parser-heavy tabs such as Refine.
- Project/app version metadata updated to `v0.9.5.1`.

---

## What's New in v0.9.5

- Redesigned main editor layout to reduce spreadsheet density and improve scanability.
- Moved navigation controls and list selection into the left panel with cleaner panel separation.
- Restyled the Elements list, search controls, menu, progress indicator, and splitter for a calmer interface.
- Reworked the Values inspector so read-only field names are visually distinct from editable values.
- Added back/forward selection history for recently selected items.
- Added right-click actions on Elements rows for Search and Preview workflows.
- Improved Description preview rendering for FW color tags and short control markers.
- Hid technical index/type columns by default while preserving the underlying data model.
- Project/app version metadata updated to `v0.9.5`.

---

## What's New in v0.9.4.2

- Native `.gfx` support in model preview and Choice Model workflow.
- Improved model-path resolution for `file_model*`, `file_models*`, and related fields.
- Better visual parity for mounts and model variants (including package/path edge-cases).
- Choice Model UX upgrades: `.gfx`-only filtering, smoother preview flow, and focus behavior fixes.
- Fixed Choice Model instability issues (including split-container crash scenarios).
- Safer PathID assignment and persistence flow for new model paths.
- `path.data` save pipeline hardened to reduce mapping corruption and improve in-game stability.
- Project/app version metadata updated to `v0.9.4.2`.

---

## What's New in v0.9.3.1

- Embedded model preview pipeline matured and documented end-to-end (`ECM`/`SMD`/`SKI` + texture resolution diagnostics).
- Model preview renderer status/profile labels standardized in English.
- Release metadata updated to `v0.9.3.1` across project versioning and UI defaults.

---

## What's New in v0.9.2

- Full MVVM migration: UI logic and flows moved out of Forms into services/coordinators.
- MainWindow trimmed: navigation, selection, tooltips, description, save, search, and actions split into dedicated services.
- Injectable session: removed static `EditorSession`, everything now uses `SessionService`.
- Secondary windows refactored: `ConfigWindow`, `RulesWindow`, `ReplaceWindow`, `ClassMaskWindow`, `FieldCompare`, `FieldReplaceWindow`, `JoinWindow`, `LoseQuestWindow`, `ReferencesWindow`, `About`, `IconPicker`.
- sELedit naming cleanup: legacy `sELedit` identifiers and labels renamed to `FWEledit` across UI and code.
- Cleaner project structure: removed duplicate `.csproj` entries and standardized service/VM organization.

---

## What's New in v0.9.1

- Global search with auto-complete now scans **all lists** (with Enter-to-search).
- Item names and list rows are **quality-colored**, with a black list background for better contrast.
- Corrected `item_quality` color mapping and quick picker list.
- Backup system now creates **dated ZIPs** under `backup_elements`, `backup_configs`, and `backup_path`.
- Build output now always includes required tools (`spck`, `packdll`, `p2sp_4th_lib`, `Pfim`, `7za`) and the `rules` folder.
- App version and settings reset logic updated to avoid stale paths when switching installs.

---

