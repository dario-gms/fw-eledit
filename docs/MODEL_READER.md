# FWEledit Model Reader Documentation

Technical documentation for the embedded 3D model preview pipeline (v0.9.3.4).

---

## 1. Scope

This document describes only the in-editor model preview flow:

- `Values grid` -> model field -> context menu -> `Preview 3D Model`

Out of scope:

- icon picker internals
- save/write pipeline for `elements.data`
- external viewers

---

## 2. Architecture Overview

Main flow:

1. Resolve the model `PathID` from current value row.
2. Resolve mapped model path via `path.data` (+ neighbor offsets when applicable).
3. Load `.ecm` and resolve skin model references (`.smd` / `.ski` / `.bon`).
4. Parse `.ski` geometry/material/texture tables.
5. Resolve and decode textures (`.dds`, `.tga`, `.bmp`, `.png`, `.jpg`, `.jpeg`) with multi-path fallback.
6. Build `ModelPreviewMeshData`.
7. Render in `ModelPreviewWindow` (CPU software, OpenGL GPU, or DirectX11 GPU).

Main code files:

- `FWEledit/Services/ModelPreviewService.cs`
- `FWEledit/Services/EmbeddedModelPreviewLoaderService.cs`
- `FWEledit/Views/ModelPreviewWindow.cs`
- `FWEledit/Models/ModelPreviewMeshData.cs`
- `FWEledit/Services/PckEntryReaderService.cs`

---

## 3. UI Entry Points

Model preview actions are available only for model-like fields (`file_model*`, `file_models*`, `model_name*`, and equivalent list-specific model fields recognized by classifier logic).

Entry chain:

1. `MainWindow.ValuePickers.cs` builds right-click context menu.
2. `ValueRowPickerUiService.OpenModelPreviewForValueRow(...)` validates and loads asynchronously.
3. `ModelPreviewService` builds mesh data and opens/reuses preview window.

### 3.1 Live Preview Refresh

When preview window is open, changing selected row in values grid can auto-refresh the currently open preview window (no close/reopen), with concurrency guard to avoid overlapping loads.

---

## 4. Model File Types

### 4.1 ECM

Used fields include:

- `SkinModelPath`
- `SrcBlend`, `DestBlend`
- `EmissiveCol`, `OrgColor`
- `OuterNum`
- shader float values (`Float:` entries)

Decode attempts use multiple encodings (GBK/UTF-8/Unicode strategies).

### 4.2 SMD

Used as indirection for:

- `.ski`
- `.bon`

Resolver supports binary/text extraction heuristics and fallback scanning.

### 4.3 SKI

Expected header: `MOXBIKSA`

Parsed data:

- mesh blocks (including per-mesh texture/material index)
- vertex streams
- UVs
- triangle indices
- texture table
- material color blocks

### 4.4 BON

Bone data may be parsed for compatibility paths, but final static preview does not apply full runtime animation skinning.

---

## 5. Path Resolution and Package Fallback

Loader resolution strategy is package-first + multi-fallback:

1. extracted package folders (`*.pck.files`)
2. resolved resource root probes
3. direct `.pck/.pkx` read through `PckEntryReaderService`

Texture probing includes:

- exact SKI texture path
- `textures\` and `texture\` variants
- SKI-neighbor and parent-neighbor folders
- package fallback list (preferred package, then common model packages)
- absolute-neighbor fallback around resolved SKI location

Unicode escape fragments (`#Uxxxx`) are decoded during normalization.

---

## 6. Texture Decode and Alpha Policy

Decoded texture data is stored as ARGB pixels in `PreviewTextureData`.

Current pipeline classifies texture alpha usage into:

- `OPAQUE`
- `CUTOUT`
- `BLEND`

### 6.1 Policy by Asset Family

Loader applies conservative per-family policy to reduce classic FW issues (fully black regions or over-transparency):

- `PreferOpaque` for families where alpha is frequently auxiliary/spec data (ex: many weapons)
- `PreferCutout` for mount families with real cutout masks mixed with auxiliary alpha
- default heuristics for other assets

### 6.2 Recovery Passes

For selected cases, RGB recovery from auxiliary alpha is attempted to avoid pre-darkened output.

### 6.3 Mount Texture Pair Repair

Mount texture sets can run pair-repair heuristics so one partner texture can recover expected alpha behavior when pair metadata is inconsistent.

---

## 7. ModelPreviewMeshData Contract

`ModelPreviewMeshData` contains:

- resolved source paths (ECM/SKI)
- `Vertices`, `UVs`, `Indices`
- `TriangleColors`
- `TriangleTextureIndices`
- `Textures` (`PreviewTextureData[]`)
- ECM render metadata (`SrcBlend`, `DestBlend`, emissive/org colors, shader floats, `OuterNum`)

`PreviewTextureData` includes (among others):

- `Name`
- `Width`, `Height`
- `Pixels` (ARGB)
- `HasTransparency`
- `UsesAlphaAsOpacity`

---

## 8. Renderer Backends (v0.9.3.4)

Available runtime modes:

- `Software (CPU)`
- `OpenGL (GPU)`
- `DirectX11 (GPU)`

Notes:

- backend selection is persisted in user settings
- hardware on/off is persisted in user settings
- backend can fallback to CPU when initialization fails, with reason shown in status line
- Vulkan is not an active backend in current build

### 8.1 Rendering Order

Both GPU and CPU paths separate drawing by alpha behavior to reduce artifacts:

1. opaque pass
2. cutout pass
3. transparent pass (blended)

---

## 9. Camera and View Behavior

Camera controls:

- drag: rotate
- `Shift + drag`: fine rotate
- scroll: zoom
- double-click: reset
- `R`: reset view
- `Esc`: close window

Behavior changes in this version:

- camera state (yaw/pitch/zoom) is persisted across preview updates/openings
- preview window is reused and mesh is replaced in-place when possible

---

## 10. Diagnostics On Screen

Header:

- vertices / triangles / loaded textures

Source block:

- `ECM:` path
- `SKI:` path
- `TEX[i]: OK|MISS ... [OPAQUE|CUTOUT|BLEND]`
- `TRI TEX[i]: count`

This data helps identify whether issues come from:

- path resolution
- decode failures
- alpha classification mismatch

---

## 11. Settings Keys

User settings currently used by preview:

- `ModelPreviewUseHardware`
- `ModelPreviewBackend`
- `ModelPreviewCameraHasState`
- `ModelPreviewCameraYaw`
- `ModelPreviewCameraPitch`
- `ModelPreviewCameraZoom`

---

## 12. Known Limitations

1. Some Angelica materials still require deeper shader parity for perfect in-game match.
2. A subset of mounts/pets/weapons can still show alpha/classification edge cases depending on atlas authoring.
3. BON animation/runtime effects (`.gfx`, full animated material stack) are not fully reproduced.
4. Visual parity is improved but not yet a 1:1 clone of game runtime renderer.

---

## 13. Troubleshooting Quick Guide

### `Model PathID not found in path.data`

- verify list field value is valid path id
- verify correct client `path.data` is loaded

### Model with missing textures (`TEX[i]: MISS`)

- verify texture file exists in package/extracted folders
- verify SKI texture names and folder aliases (`texture`/`textures`)

### Model too dark / too transparent / black patches

- check `TEX[i]` alpha mode labels
- compare behavior between OpenGL and DirectX11 backends
- check if asset family may require different alpha policy tuning

### Live preview not updating

- open preview first from a model field
- ensure selection is on a model field row
- if window was closed, live mode is automatically disabled

---

## 14. Extension Targets

Priority next steps:

1. Improve Angelica shader/material parity for remaining problematic alpha/spec assets.
2. Optional support for additional SKI vertex stream variants where safe.
3. Optional animation playback path compatible with FW assets.
4. Add a dedicated debug overlay for per-triangle alpha/material classification.

---

## 15. Code Index

- Preview entry/menu: `FWEledit/MainWindow.ValuePickers.cs`
- Async open + live refresh: `FWEledit/Services/ValueRowPickerUiService.cs`
- Window reuse and replacement: `FWEledit/Services/ModelPreviewService.cs`
- Loader and alpha policy: `FWEledit/Services/EmbeddedModelPreviewLoaderService.cs`
- Renderers (CPU/OpenGL/DirectX11): `FWEledit/Views/ModelPreviewWindow.cs`
- Texture decode core: `FWEledit/DDSReader/*`
