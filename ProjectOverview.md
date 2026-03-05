# Steam Category Diff (Playnite Plugin)

## Purpose

This project implements a **Playnite plugin** that compares **Steam library collections (categories)** with **Playnite categories** for Steam games. The goal is to help the user identify mismatches between the two systems so they can keep them synchronized.

Steam and Playnite both allow games to be organized into categories, but they store this information in different places and formats. Over time these category assignments drift apart. This tool provides a **diff-style comparison** so the user can see:

- Categories that exist **in Steam but not in Playnite**
- Categories that exist **in Playnite but not in Steam**

The plugin does **not automatically synchronize categories** yet. It is currently a **diagnostic and reporting tool**.

---

## Environment

This project targets:

- **.NET Framework 4.8**
- **WPF UI**
- **Playnite plugin API**

The plugin is deployed by copying the compiled DLL into the Playnite:

```
Extensions/
  SteamCategoryDiff/
```

folder.

Playnite loads the plugin automatically at startup.

---

## Key Technologies

- **C#**
- **WPF (XAML)**
- **Playnite SDK**
- **Newtonsoft.Json**
- Steam configuration files

---

## How Steam Stores Collections

Modern Steam clients store user collections in:

```
Steam/userdata/<steamid>/config/cloudstorage/cloud-storage-namespace-1.json
```

This file contains entries like:

```
["user-collections.<id>", {
  "value": "{ \"name\": \"Collection Name\", \"added\": [appid, ...], \"removed\": [...] }"
}]
```

Important characteristics:

- Each entry represents a **collection**
- The `"value"` field contains **JSON encoded as a string**
- `added` contains Steam **AppIDs**
- `removed` removes entries from the collection

The plugin parses this file and converts it into:

```
AppID -> set of Steam category names
```

---

## How Playnite Stores Categories

Playnite categories are available through the Playnite API:

```
api.Database.Games
api.Database.Categories
```

Each game has:

```
Game.PluginId
Game.GameId
Game.CategoryIds
```

Only games belonging to the **Steam library plugin** are considered.

The Steam plugin ID is:

```
CB91DFC9-B977-43BF-8E70-55F46E410FAB
```

Game.GameId corresponds to the **Steam AppID**.

The plugin converts Playnite data into:

```
AppID -> set of Playnite category names
```

---

## Diff Logic

The plugin computes the union of all AppIDs from both systems.

For each game:

```
steamOnly = SteamCategories - PlayniteCategories
playniteOnly = PlayniteCategories - SteamCategories
```

Each row in the UI shows:

- Game name
- AppID
- Categories only in Steam
- Categories only in Playnite

---

## UI Architecture

The UI uses a simple MVVM pattern:

```
SteamCategoryDiffPlugin
    ↓
DiffWindow (WPF view)
    ↓
DiffViewModel (logic)
```

### Plugin Entry

`SteamCategoryDiffPlugin`

- Registers a menu item under **Extensions**
- Opens the diff window

### Window

`DiffWindow.xaml`

Displays:

- a table of diff rows
- status text
- controls such as reload and copy report

### ViewModel

`DiffViewModel`

Responsibilities:

- locate Steam install
- load Steam collections
- read Playnite categories
- compute diffs
- populate the UI

---

## Key Classes

### DiffViewModel

Core logic:

- reads Steam collections JSON
- reads Playnite game categories
- generates `DiffItem` objects

### DiffItem

Represents a single row:

```
Name
AppId
SteamOnly (list)
PlayniteOnly (list)
```

### SteamCollectionsJsonReader

Parses Steam’s:

```
cloud-storage-namespace-1.json
```

and returns:

```
Dictionary<string, HashSet<string>>
```

mapping AppID → category names.

---

## Current Features

- Detect Steam install location
- Parse Steam collections JSON
- Read Playnite categories
- Compute differences
- Display results in a grid
- Copy a text report to clipboard

---

## Planned Improvements

Possible future features:

- Sync **Steam → Playnite**
- Sync **Playnite → Steam**
- Ignore categories by rule
- Filter by category
- Export diff report to file
- Detect multiple Steam profiles
- Incremental refresh instead of full reload

---

## Important Constraints

- Steam must not be writing the collections file during parsing
- Only **numeric GameIds** are treated as Steam AppIDs
- The plugin currently assumes **one Steam account**

---

## Development Notes

The plugin is intended to be developed iteratively with AI-assisted coding tools. Code should prioritize:

- clarity
- defensive parsing
- robust error logging
- minimal assumptions about Steam file formats

Steam file formats occasionally change, so parsing logic should be tolerant of structure differences.

---

## Summary

Steam Category Diff is a **Playnite extension** that helps maintain consistency between:

```
Steam collections
and
Playnite categories
```

by computing and displaying a clear diff of category assignments per Steam game.
