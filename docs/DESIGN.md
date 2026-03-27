# FWEledit Design Guide (MVVM + Services)

This document explains the responsibilities of each layer and the main components in the project. It is intended for open-source contributors who may not be familiar with MVVM but need a clear map of "what does what" and "where to change what".

---

**Goals**

- Keep UI code-behind thin and predictable.
- Separate orchestration, UI manipulation, and data mutation.
- Make behaviors easy to find by name and file location.
- Allow new contributors to add features without touching MainWindow or Forms directly.

---

**Terminology**

- **View**: WinForms `Form` classes and their `.Designer.cs` layout files.
- **ViewModel**: `ViewModels/*` classes, store state and notify UI.
- **Service**: Stateless helper classes that implement one responsibility.
- **Coordinator**: Services that orchestrate multiple services for a UI event.
- **Command**: Services that mutate model data.
- **Workflow**: Services that define multi-step procedures using commands and UI services.
- **UiService**: Services that adapt data to UI controls and user interaction.

---

**Directory Map**

- `FWEledit/` is the main application.
- `FWEledit/ViewModels/` contains UI state objects.
- `FWEledit/Services/` contains all application logic.
- `FWEledit/COMMON/` contains core data models and helpers (`eListCollection`, `eList`, `CacheSave`).
- `FWEledit/Views/` contains secondary UI windows.

---

**High-Level Architecture**

1. **Form event handler** calls a **CoordinatorService**.
2. **CoordinatorService** composes UI services, workflow services, and command services.
3. **WorkflowService** defines the multi-step process and calls **CommandService**.
4. **CommandService** mutates the model (`eListCollection`, `CacheSave`, etc.).
5. **UiService** updates controls and visual state.
6. **ViewModel** holds persistent UI state and is bound to controls where possible.

This structure keeps event handlers minimal and makes it easy to locate logic by service name.

---

**Session & Core Data**

- `ISessionService` is the single source of truth for the runtime session.
- It contains the active `ListCollection`, `ConversationList`, `Database`, `AssetManager`, and other shared state.
- Do not use static session objects. Always use `ISessionService`.

Key model types:

- `eListCollection`: all lists and element values from `elements.data`.
- `eList`: a single list (table), fields and values.
- `CacheSave`: runtime cache for icons, themes, paths, etc.

---

**MainWindow Responsibilities**

MainWindow is split into partial files. Each file only wires events to coordinators.

- `MainWindow.Setup.cs` handles startup, theme, and UI wiring.
- `MainWindow.EventHandlers.*.cs` only delegates to coordinators.
- `MainWindow.Description.cs` delegates description editing and preview.
- `MainWindow.SaveNavigation.cs` delegates save and navigation persistence.

Key coordinators:

- `MainWindowNavigationCoordinatorService`: navigation state, persistence, and save workflow.
- `MainWindowDescriptionCoordinatorService`: description editing pipeline.
- `MainWindowSelectionCoordinatorService`: list/item/value selection pipeline.
- `MainWindowSearchCoordinatorService`: search and suggestions.
- `MainWindowActionsCoordinatorService`: add/clone/delete/move actions.
- `MainWindowDialogsCoordinatorService`: dialogs and tools.
- `MainWindowValuePickerCoordinatorService`: icon/model/addon pickers.
- `MainWindowSaveCoordinatorService`: save orchestration.

---

**Description Pipeline**

- Source data is `item_ext_desc` in configs or resources.
- Editing is staged and written on save.
- Services involved:
  - `Description*Service` classes handle parsing, staging, and UI updates.
  - `MainWindowDescriptionCoordinatorService` wires it all together.

---

**Navigation & Dirty Tracking**

- `MainWindowDirtyTrackingService` marks changed rows/fields.
- `NavigationStateService` and `NavigationPersistenceService` manage restore and persistence.
- `MainWindowNavigationCoordinatorService` orchestrates capture/restore/save.

---

**Search**

- Search is handled by `MainWindowSearchCoordinatorService`.
- Suggestions and navigation are in `SearchUiService` and `SearchSuggestionService`.

---

**Icon & Model Pickers**

- Icon picker is an independent `IconPickerWindow`.
- Logic now lives in `IconPickerWindowCoordinatorService`.
- Model picker is handled by `ModelPickerService` and related UI services.

---

**Secondary Windows (MVVM)**

All the following windows are now routed through coordinators:

- `ConfigWindow` via `ConfigWindowCoordinatorService`.
- `RulesWindow` via `RulesWindowCoordinatorService`.
- `ReplaceWindow` via `ReplaceWindowCoordinatorService`.
- `ClassMaskWindow` via `ClassMaskCoordinatorService`.
- `FieldCompare` via `FieldCompareCoordinatorService`.
- `FieldReplaceWindow` via `FieldReplaceWindowCoordinatorService`.
- `JoinWindow` via `JoinWindowCoordinatorService`.
- `LoseQuestWindow` via `LoseQuestWindowCoordinatorService`.
- `ReferencesWindow` via `ReferencesWindowCoordinatorService`.
- `About` via `AboutCoordinatorService`.
- `DebugWindow` via `DebugWindowCoordinatorService`.

Each event handler should only delegate to its coordinator.

---

**Naming Conventions**

- `*CoordinatorService`: orchestration layer for a window or event set.
- `*UiService`: translates model data into UI updates.
- `*CommandService`: mutates model data.
- `*WorkflowService`: multi-step processes using commands and UI services.
- `*RequestBuilderService`: builds request objects for workflows.
- `*ViewModel`: state and binding support.

---

**Where to Change Things**

Use this rule of thumb:

- **UI behavior**: go to `*UiService`.
- **Business logic**: go to `*CommandService` or `*WorkflowService`.
- **Event wiring**: go to `*CoordinatorService`.
- **State**: go to `ViewModels/*`.
- **Core data**: go to `COMMON/*` or `AssetManager`.

---

**How to Add a New Feature**

1. Add or extend a `ViewModel` if new state is required.
2. Add a `CommandService` or `WorkflowService` for new logic.
3. Add a `UiService` if controls require special updates.
4. Add a `CoordinatorService` method for the event handler.
5. Call the coordinator from the form event handler.

This keeps changes localized and avoids regressions in MainWindow.

---

**Summary**

FWEledit uses a pragmatic MVVM layout for WinForms. The goal is not to remove all code-behind, but to keep it limited to event wiring and to centralize real behavior in services. If you follow the coordinator + workflow + command + UI service structure, you will be aligned with the project's architecture and future contributors will be able to maintain your changes easily.
