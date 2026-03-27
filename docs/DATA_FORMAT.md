# FWEledit Data Format Guide

This document explains how FWEledit reads and writes the binary `elements.data` file used by Forsaken World.

---

## Overview

`elements.data` is a binary container that stores multiple lists (tables). Each list has:

- A list name
- An entry size (bytes per row)
- Field names and types
- A fixed number of entries

FWEledit does not hardcode the layout. Instead, it uses **versioned config files** (`configs/*.cfg`) that describe the list structure for each server version.

---

## Config‑Driven Layout

Config files define the table layout:

```
<total list count>
<conversation list index>

<list number> - <LIST_NAME>
<entry size in bytes>
<field1;field2;field3;...>
<type1;type2;type3;...>
```

Supported field types:

- `int16`, `int32`, `int64`
- `float`, `double`
- `string:<n>` (fixed length)
- `wstring:<n>` (fixed length UTF‑16)
- `byte:<n>` (raw byte array)

Each list uses this structure to decode the binary data into rows and fields.

---

## Binary Read/Write Flow

### Read
1. Read header information and list count.
2. Load the config file that matches the detected version.
3. For each list:
   - Read entry size.
   - Read the element count.
   - Read each entry according to the field types.
4. Store results in `eListCollection`.

### Write
1. Validate list integrity (field types and sizes).
2. Rebuild binary output list-by-list.
3. Write to a temp file.
4. Create `.bak` backup.
5. Replace original `elements.data`.

---

## Core Model Types

- `eListCollection`  
  Holds all lists and provides read/write methods.

- `eList`  
  Represents a single list (table):  
  `elementFields`, `elementTypes`, and `elementValues`.

- `CacheSave`  
  Runtime cache for paths, icons, themes, and temp data.

---

## Field Type Behavior

**Numeric types**
- `int16`, `int32`, `int64` are read/written in little‑endian order.
- `float`, `double` use IEEE 754.

**Strings**
- `string:<n>` reads a fixed-length byte array and trims nulls.
- `wstring:<n>` reads UTF‑16LE fixed-length and trims nulls.

**Raw bytes**
- `byte:<n>` stores an opaque fixed-length block.
- Used for unknown or packed values.

---

## List and Row Indexing

Lists are stored by index. List index is stable and is used throughout the editor:

- List index → name, fields, types
- Row index → element entry

Cross‑references and search are performed using list index + row index + field index.

---

## Known Extensions

FWEledit adds higher‑level helpers on top of raw fields:

- **Descriptions** (`item_ext_desc.txt`) are loaded and mapped by item ID.
- **Icon/model fields** use `path.data` to resolve path IDs.
- **Added attributes** decode packed numeric values into readable output.

These are UI helpers only; the underlying binary values are preserved.

---

## Safety Notes

- The editor validates IDs before save.
- Writes are atomic (`.tmp_fweledit` + `.bak`).
- A failed save does not corrupt the original file.

---

## If a Config Does Not Match

Symptoms include:
- Incorrect list lengths
- Wrong field values
- Read errors during load

Fixes:
1. Adjust the config layout (`configs/*.cfg`).
2. Verify field sizes and order.
3. Compare against a known working version.

---

This guide is intentionally high‑level. For implementation details, start with:
- `COMMON/eListCollection.cs`
- `COMMON/eList.cs`
- `COMMON/CacheSave.cs`
