# FWEledit Development Guide

This guide explains how to run, debug, test, and extend the project.

---

## How to Run the Project

**Requirements**
- Windows
- Visual Studio 2017+ (Community is fine)
- .NET Framework 4.5.2 targeting pack

**Steps**
1. Open `FWEledit.sln` in Visual Studio.
2. Select **Debug** or **Release**.
3. Build the solution.
4. Run `FWEledit.exe` from `FWEledit/bin/Debug/` or `Release/`.

**Run with a game folder**
- Use **File > Load...** and select the game root folder.
- The editor auto-resolves `elements.data` and resources.

---

## How to Debug

1. Run the project from Visual Studio.
2. Set breakpoints in coordinator or service classes (recommended).
3. Use **File > Load...** to open a test game folder.
4. Trigger the UI action you want to inspect.

**Recommended places to set breakpoints**
- `MainWindow*CoordinatorService`
- `*WorkflowService`
- `*CommandService`
- `Description*Service` for description pipeline

---

## How to Test

FWEledit has no formal test suite yet. Use manual testing:

**Suggested manual checks**
- Load a valid `elements.data`.
- Select lists and items, verify no exceptions.
- Edit a value and confirm dirty tracking.
- Stage and save a description.
- Run **Save** and confirm `.bak` + ZIP backups.
- Open icon and model pickers.
- Run tools: **Rules**, **Field Compare**, **Field Replace**.

**Tip:** Keep a small sample `elements.data` for faster testing.

---

## How to Create New Features

Follow the MVVM + services pattern:

1. **Add state** to a ViewModel if needed.
2. **Create services**:
   - `*CommandService` for data mutation
   - `*WorkflowService` for multi-step operations
   - `*UiService` for UI updates
3. **Create/extend a CoordinatorService** to wire everything together.
4. **Call the coordinator** from the Form event handler.

**Rule of thumb**
- Do not put logic inside the Form directly.
- Keep event handlers as one-line calls to coordinators.

---

If you need help understanding where a feature should live, check:
- `DESIGN.md`
- `ARCHITECTURE.md`
