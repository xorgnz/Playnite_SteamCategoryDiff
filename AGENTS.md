# AGENTS.md

This file defines default collaboration rules for Codex in this repository.

## Project Context
- Solution: `SteamCategoryDiff.sln`
- Main project: `SteamCategoryDiff/`
- Runtime: .NET Framework (`net48`) with WPF UI and Playnite plugin integration
- Primary language: C#

## Goals
- Keep the plugin stable for existing users.
- Prefer maintainable, testable changes over quick hacks.
- Avoid unnecessary churn in UI behavior and file formats.

## Engineering Guidelines

### Code Style
- Use clear, descriptive names for types, methods, and variables.
- Prefer small methods with single responsibility.
- Keep classes focused; avoid introducing broad utility classes unless reuse is clear.
- Use `var` when the type is obvious from the right-hand side; otherwise use explicit types.
- Keep nullable handling explicit and safe.
- Minimize public surface area; default to `private`/`internal` unless public is required.

### Architecture and Design
- Preserve existing MVVM boundaries (`DiffWindow.xaml` + `DiffViewModel.cs`).
- Keep UI concerns in views/view models and parsing logic in reader/model classes.
- Avoid adding global state; pass dependencies explicitly where practical.
- Prefer pure transformation methods for diff/category comparison logic.

### Error Handling and Logging
- Validate file paths and external inputs before use.
- Fail with actionable messages; avoid swallowing exceptions silently.
- Use targeted exception handling (catch specific exceptions where possible).

### Performance
- Optimize for clarity first, then improve hotspots shown by real usage.
- Avoid repeated full-file reads or redundant parsing in a single workflow.
- Use appropriate collections for lookup-heavy operations.

### Safety and Compatibility
- Treat Steam/Playnite data as user data: do not destructively overwrite without clear intent.
- Preserve backward compatibility for existing file structures and extension config usage.
- Prefer additive changes to models when evolving data contracts.

## Testing and Validation
- Build must succeed before completing substantial changes.
- For behavior changes, include at least one of:
  - focused unit tests (if test project exists), or
  - a short manual validation checklist in the final response.
- For parser changes, validate both expected and malformed input scenarios.

## Change Management
- Keep diffs tight and relevant to the request.
- Do not reformat unrelated files.
- Call out assumptions explicitly when requirements are ambiguous.
- If a request may cause data loss or breaking behavior, pause and confirm first.

## Response Expectations
- Provide concise summaries with:
  - what changed,
  - where it changed,
  - how it was validated,
  - any follow-up recommendations.

