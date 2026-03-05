# Steam Category Diff

Steam Category Diff is a [Playnite](https://playnite.link/) generic plugin that compares category assignments between:

- Steam collections (from Steam user data)
- Playnite categories (for games imported by the Playnite Steam library plugin)

It shows where categories differ so you can reconcile them manually.

## Current Status

This plugin is a diff and reporting tool.

- It does not write changes back to Steam.
- It does not write changes back to Playnite.

## Features

- Detects Steam install path (registry lookup with folder picker fallback)
- Reads Steam collections from `cloud-storage-namespace-1.json`
- Reads Playnite category assignments for Steam games only
- Compares per-game category sets
- Displays results in a WPF grid with status columns
- Supports filtering by library presence and identical/different category sets
- Copies a text diff report to clipboard

## Requirements

- Windows
- Playnite (Desktop mode)
- .NET Framework 4.8 runtime
- Steam installed with at least one user profile

## How It Works

1. Steam collections are loaded from:
   - `Steam/userdata/*/config/cloudstorage/cloud-storage-namespace-1.json`
2. Playnite games are filtered to Steam plugin games using plugin ID:
   - `CB91DFC9-B977-43BF-8E70-55F46E410FAB`
3. For each Steam AppID, category sets are compared between Steam and Playnite.
4. Rows are marked as:
   - `Yes` when both sets match
   - `No` when both exist but differ
   - `--` when the game exists in only one side

## Installation (Manual)

Build the project, then copy plugin output to Playnite's Extensions folder:

- Folder name: `SteamCategoryDiff`
- Required files:
  - `SteamCategoryDiff.dll`
  - `extension.yaml`
  - any dependency DLLs from build output

Typical destination:

```text
<Playnite>/Extensions/SteamCategoryDiff/
```

Restart Playnite after copying files.

## Usage

1. In Playnite, open `Extensions` > `Steam Category Diff`.
2. The window loads data automatically.
3. Use filters:
   - `Steam only`
   - `Playnite only`
   - `Both`
   - `Show identicals`
4. Click `Reload` to refresh.
5. Click `Copy report` to place a text report on your clipboard.

## Development

### Solution Layout

- `SteamCategoryDiff.sln` - solution file
- `SteamCategoryDiff/SteamCategoryDiff.csproj` - plugin project (`net48`, WPF)
- `SteamCategoryDiff/SteamCategoryDiffPlugin.cs` - Playnite plugin entry point
- `SteamCategoryDiff/DiffViewModel.cs` - loading, diff logic, filtering, report generation
- `SteamCategoryDiff/DiffWindow.xaml` - UI
- `SteamCategoryDiff/SteamCollectionsJsonReader.cs` - Steam collections parser
- `SteamCategoryDiff/DiffModels.cs` - diff row model

### Build

From repository root:

```powershell
msbuild .\SteamCategoryDiff.sln /t:Build /p:Configuration=Release
```

Or build via Visual Studio.

### Optional Auto-Deploy on Build

`SteamCategoryDiff.csproj` includes `PlayniteExtensionDeployDir`.
If set to a valid Playnite extension folder, build output is copied there automatically after build.

## Known Limitations

- Assumes Steam collections are available in `cloud-storage-namespace-1.json`.
- Uses the largest matching cloud storage file under `userdata` when multiple profiles exist.
- Only numeric `GameId` values are treated as Steam AppIDs.
- Steam and Playnite are compared by category names (case-insensitive).
- No automatic synchronization yet.

## Troubleshooting

- If Steam path detection fails, select your Steam install directory when prompted.
- If no Steam collections file is found, verify Steam has synced collections for the account.
- If results look stale, click `Reload`.

## License

No license file is currently included.
If you plan to publish this repository, add a `LICENSE` file.
