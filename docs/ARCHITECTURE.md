# FWEledit Architecture

This document gives a practical overview of the system for contributors who need to understand how the editor is structured and where to make changes.

---

## 1. System Overview

FWEledit is a WinForms editor for Forsaken World `elements.data`. It uses a pragmatic MVVM approach:

- **Views (Forms)** handle UI layout and event wiring only.
- **ViewModels** hold UI state and expose properties for binding.
- **Services** implement behavior, data manipulation, and UI updates.
- **Coordinators** orchestrate multiple services for each user action.

The goal is to keep Forms thin and to centralize logic in services.

---

## 2. Data Flow

Typical flow for a UI action:

1. **Form event handler** calls a **CoordinatorService**.
2. **CoordinatorService** composes UiServices, WorkflowServices, and CommandServices.
3. **WorkflowService** drives multi-step operations (validation, preconditions, execution).
4. **CommandService** mutates model data (`eListCollection`, `CacheSave`, etc.).
5. **UiService** updates controls and visual state.
6. **ViewModel** stores UI state and notifies bindings.

This pattern makes behavior easy to find and reduces MainWindow size.

---

## 3. Main Components

**Core models**
- `eListCollection`: all lists and element values from `elements.data`.
- `eList`: a single list (table).
- `eListConversation`: conversation list model.
- `CacheSave`: runtime caches (icons, themes, paths, lists).

**Session**
- `ISessionService`: runtime session container (single source of truth).

**MainWindow**
- Split across `MainWindow.*.cs` partials.
- Uses coordinators for navigation, selection, search, save, etc.

**Secondary windows**
Each tool window has a coordinator and a small view model:
`ConfigWindow`, `RulesWindow`, `ReplaceWindow`, `ClassMaskWindow`, `FieldCompare`, `FieldReplaceWindow`, `JoinWindow`, `LoseQuestWindow`, `ReferencesWindow`, `About`, `IconPicker`.

---

## 4. Runtime Session Model

`ISessionService` contains the active runtime state, including:

- `ListCollection` (current `elements.data` lists)
- `ConversationList`
- `Database` (`CacheSave`)
- `AssetManager`
- `Xrefs`
- `ItemExtDesc` (item descriptions)

The session is injectable and must replace any static/global session usage.

---

## 5. Save Pipeline

Save is orchestrated by `MainWindowSaveUiService` + `MainWindowSaveCoordinatorService` and follows these steps:

1. **Validate IDs** (duplicate or invalid IDs across lists).
2. **Write temp file** (`.tmp_fweledit`).
3. **Create .bak** backup of `elements.data`.
4. **Replace target** with temp file atomically.
5. **Flush description changes** to `item_ext_desc.txt` if staged.
6. **Repack PCKs** when required (via `spck`).
7. **Create ZIP backups** in `backup_elements`, `backup_configs`, `backup_path`.
8. **Capture/restore navigation snapshot** to keep UI consistent.

If any step fails, the original file remains intact.

---

## 6. Resource Handling

Resources are resolved from the game root and PCK files:

- **PCK extraction** for `configs.pck` and `surfaces.pck` into a workspace.
- **Icon picker** uses DDS atlas (`iconlist_ivtr0`).
- **Model picker** reads PCK index tables without full extraction.
- **path.data** resolves model/icon path IDs consistently.

`AssetManager` and related services handle the discovery, caching, and lookup logic.

---

## 7. Extensibility Points

Recommended extension points for contributors:

1. **Config files** (`configs/*.cfg`)  
   Add new versions or fix field layouts without touching code.

2. **Services**  
   Add new `CommandService`, `WorkflowService`, `UiService`, and wire in a coordinator.

3. **Coordinators**  
   Central place to connect new UI events to logic.

4. **ViewModels**  
   Add new properties for new UI state or tool windows.

5. **GetProps** (`COMMON/GetProps/*`)  
   Tooltip and field interpretation per list type.

6. **Tools menu / dialogs**  
   New tools should be their own window + coordinator for maintainability.

---

**Rule of thumb:**
- UI wiring stays in Forms.
- Behavior goes into services.
- Multi-step logic belongs in workflows.
- State goes into view models.
