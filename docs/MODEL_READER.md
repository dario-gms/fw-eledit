# FWEledit Model Reader Documentation

Complete technical documentation for the embedded 3D model preview reader.

---

## 1. Scope

This document only covers the model reader/preview pipeline used by:

- `Values grid` -> right-click model field -> `Preview 3D Model`

Out of scope:

- icon picker internals
- model picker catalog internals
- `elements.data` save/write pipeline
- external tools

---

## 2. High-Level Architecture

Main flow:

1. Resolve `PathID` from `path.data`.
2. Read `.ecm` entity config.
3. Resolve `SkinModelPath` to `.smd` or direct `.ski`.
4. Resolve referenced `.ski` (mesh) and `.bon` (bind pose source).
5. Parse `.ski` geometry/material/texture tables.
6. Resolve and decode textures (DDS and common image formats) with multi-path fallback.
7. Build `ModelPreviewMeshData`.
8. Render in `Preview 3D Model` window (software rasterizer in current build).

Core files:

- `FWEledit/Services/ModelPreviewService.cs`
- `FWEledit/Services/EmbeddedModelPreviewLoaderService.cs`
- `FWEledit/Views/ModelPreviewWindow.cs`
- `FWEledit/Models/ModelPreviewMeshData.cs`
- `FWEledit/Services/PckEntryReaderService.cs`

---

## 3. UI Entry Points

Preview is enabled only for fields classified as model fields:

- `file_model*`
- `file_models*`
- `model_name*`

Call chain:

1. `MainWindow.ValuePickers.cs` adds `Preview 3D Model` in context menu.
2. `ValueRowPickerUiService.OpenModelPreviewForValueRow(...)` validates field and resolves the current path ID.
3. `ModelPreviewService.TryOpenPreview(...)` resolves mapped path and opens `ModelPreviewWindow`.

Common user-facing errors:

- `Invalid model PathID.`
- `Model PathID not found in path.data`
- `Model preview unavailable for this file.`
- `MODEL PREVIEW ERROR! ...`

---

## 4. File Types and Roles

### 4.1 ECM

Entity configuration source. The reader uses:

- `SkinModelPath`
- `SrcBlend`
- `DestBlend`
- `EmissiveCol`
- `OrgColor`
- `OuterNum`
- repeated `Float:` values

Text decode order:

1. `GBK`
2. `UTF-8`
3. `Unicode`

### 4.2 SMD

Descriptor that usually points to:

- `.ski`
- `.bon`

Extraction strategy:

1. length-prefixed string scan (GBK)
2. regex scan in GBK/Unicode text
3. raw byte scan for matching extensions

### 4.3 SKI

Mesh container with material and texture references.

Expected header:

- `MOXBIKSA`

Parsed data:

- mesh counts by type
- texture table
- material blocks
- vertex streams
- UVs
- triangle indices
- texture index assignment per triangle

### 4.4 BON

Bind matrices can be parsed and matched by bone name.

Important current behavior:

- BON skinning is intentionally not applied in final static preview composition to avoid severe distortion on many assets.

### 4.5 Textures

Supported extensions:

- `.dds`
- `.tga`
- `.bmp`
- `.png`
- `.jpg`
- `.jpeg`

DDS decoding:

- `FWEledit.DDSReader` -> ARGB bitmap pixels

---

## 5. Path Resolution and Fallback Strategy

The loader always works with `package + relative path`.

Primary model packages:

- `models`
- `litmodels`
- `moxing`

File read order (`TryReadModelFile`):

1. extracted package root path (`*.pck.files`)
2. generic resource resolver path
3. direct package read from `.pck/.pkx` via `PckEntryReaderService`

Texture path probing includes:

- exact texture path from SKI
- `textures\filename` and `texture\filename`
- SKI directory neighbors
- parent directory neighbors
- package fallback list: preferred package, then `models`, `litmodels`, `moxing`, `surfaces`, `configs`
- absolute-neighbor fallback around resolved SKI location

This is designed to survive client layouts with inconsistent folder conventions.

---

## 6. SKI Parsing Details

### 6.1 Validation

- minimum file size
- exact header check
- non-zero mesh count
- reject unsupported vertex stream types `2` and `3`
- strict stream boundary checks with `TryEnsureRemaining(...)`
- safety limits for extremely large meshes

### 6.2 Geometry

Per mesh:

- read `texIndex` and `matIndex`
- read vertices
- read UV (`tu`, `tv`)
- read triangle indices (`UInt16`)

Axis transform currently applied:

- `X` is flipped (`-X`)
- `Y` remains up-axis

### 6.3 Materials and Base Color

Material block:

- 16 floats
- base color inferred from first 3 values

Per-triangle base color:

- stored in `TriangleColors`
- falls back to neutral color when needed

### 6.4 Texture Assignment

Per-triangle texture index:

- stored in `TriangleTextureIndices`
- derived from mesh `texIndex`
- used by rasterizer to keep submesh texture mapping correct

---

## 7. Unicode Path Escape Support (`#Uxxxx`)

Path normalization decodes escaped Unicode fragments:

- `#U5de8#U4eba...` -> real Unicode characters

This is required for many clients that encode CJK names in path strings.

---

## 8. Preview Data Contract

`ModelPreviewMeshData` includes:

- source/mapped/absolute paths (ECM/SKI)
- `Vertices`
- `UVs`
- `Indices`
- `TriangleColors`
- `TriangleTextureIndices`
- `Textures` (`PreviewTextureData[]`)
- ECM render-related values (`SrcBlend`, `DestBlend`, `EmissiveColorArgb`, `OrgColorArgb`, `ShaderFloats`, `OuterNum`)

`PreviewTextureData` includes:

- `Name`
- `Width`, `Height`
- `Pixels` (ARGB)
- `AverageColorArgb`
- `HasTransparency`

---

## 9. Renderer Behavior (Current Build)

### 9.1 Backend UI vs Runtime Reality

UI exposes:

- `Hardware` checkbox
- backend combo: `DirectX 11`, `Vulkan`, `OpenGL`

Current runtime:

- all profiles still render through software rasterization
- backend selection changes quality/performance profile only
- status text explicitly reports CPU fallback

### 9.2 Rasterization Pipeline

1. normalize model by bounds (`center`, `radius`)
2. apply yaw/pitch camera rotation + perspective projection
3. classify triangles into opaque/transparent buckets
4. rasterize opaque first with depth buffer
5. rasterize transparent triangles after sort
6. optional wireframe overlay for visible top surfaces

### 9.3 Lighting and Color Pipeline

Per-fragment stages:

1. `ApplyShade` (gamma-aware shading via sRGB/linear conversion)
2. `ApplyEcmPostLighting` (specular/emissive/tint from ECM parameters)
3. `ApplyColorGrade` (saturation/contrast/black-level tuning)

Goal:

- avoid washed output
- better match in-game perceived contrast and warmth

### 9.4 Texture Sampling and Transparency

- UV wraps by fractional part
- bilinear filtering enabled
- near-zero alpha texels are discarded
- `HasTransparency` inferred from pixel alpha distribution

---

## 10. Camera Controls

- Drag: rotate
- `Shift + drag`: fine rotate
- Scroll: zoom
- Double-click: reset view
- `R`: reset
- `Esc`: close

Default view:

- starts from front-facing angle with calibrated yaw/pitch
- pitch clamped to prevent extreme flip behavior

---

## 11. On-Screen Diagnostics

Header:

- `Vertices: X`
- `Triangles: Y`
- `Textures: loaded/total`

Source section:

- `ECM: ...`
- `SKI: ...`
- `TEX[i]: OK/MISS ...`
- `TRI TEX[i]: count`

Interpretation:

- `Textures: n/n` + `TEX[i]: OK` confirms load/decode success
- `TRI TEX[i]` confirms triangle distribution by texture index
- `MISS` points to path resolution or decode failures

---

## 12. Dependencies

Internal:

- `AssetManager`
- `PckEntryReaderService`
- `DDSReader`

External library used by package readers:

- `Ionic.Zlib`

---

## 13. Known Limitations

1. "Hardware" mode is still CPU fallback in this build.
2. SKI mesh vertex types `2/3` are not supported.
3. BON skinning is intentionally disabled for final static preview composition.
4. GFX/particle files (`.gfx`) are not rendered.
5. Output is close to game appearance but not a full in-game shader clone.

---

## 14. Troubleshooting

### `Model PathID not found in path.data`

- verify `path.data` is loaded from the expected client
- verify field value is a valid path ID

### Model loads without textures

- inspect `TEX[i]: MISS` lines
- verify SKI texture names
- verify folder layout (`textures`, `texture`, SKI neighbor folders)
- verify file exists in extracted folders or package entry

### Texture appears mapped incorrectly

- check `TRI TEX[i]` distribution
- verify per-mesh `texIndex` parsing
- verify vertex/index stream offsets and UV reads

### Colors too dark or washed

- inspect `ApplyShade`, `ApplyEcmPostLighting`, `ApplyColorGrade`
- inspect ECM `Float:` values and emissive/org color

### Model appears distorted

- verify supported SKI stream type
- verify BON skinning was not re-enabled by mistake

---

## 15. Regression Checklist

Run before closing model-reader changes:

1. Open at least 3 different asset types (mount/creature/humanoid).
2. Confirm `Textures: n/n`.
3. Confirm expected `TEX[i]: OK` entries.
4. Confirm non-zero `TRI TEX[i]` for main textures.
5. Toggle wireframe on/off and verify stable output.
6. Test rotate/zoom/reset behavior.
7. Test Unicode paths and `#Uxxxx` decoding.
8. Test both extracted-resource and direct-PCK fallback paths.

---

## 16. Extension Targets

High-value next steps:

1. Real GPU backends (DX11/Vulkan/OpenGL) while preserving current visual parity.
2. Full runtime overlay/material blend integration.
3. Optional static animation support from `.stck`.
4. Optional support for additional SKI vertex stream types.
5. Debug overlay mode for UV/material diagnostics.

---

## 17. Code Reference Index

Preview entry and orchestration:

- `FWEledit/MainWindow.ValuePickers.cs`
- `FWEledit/Services/ValueRowPickerUiService.cs`
- `FWEledit/Services/ModelPreviewService.cs`

Model load and parse:

- `FWEledit/Services/EmbeddedModelPreviewLoaderService.cs`

Data models:

- `FWEledit/Models/ModelPreviewMeshData.cs`
- `FWEledit/Models/Vector3f.cs`

Renderer:

- `FWEledit/Views/ModelPreviewWindow.cs`

Package reader:

- `FWEledit/Services/PckEntryReaderService.cs`

DDS decoding:

- `FWEledit/DDSReader/DDS.cs`
- `FWEledit/DDSReader/DDSImage.cs`
- `FWEledit/DDSReader/Decompressor.cs`
