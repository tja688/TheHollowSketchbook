# Design Document Sync Design

## Purpose

The project needs a repeatable AI-friendly workflow for importing the latest authoritative design documents from the designer-maintained folder into the Unity project. The project git repository should then expose the resulting additions, modifications, and deletions so an AI agent can react to design changes in follow-up development tasks.

## Scope

- Source: `C:\Users\jinji\Desktop\文档\MyNote\游戏开发项目\深入地牢`
- Target: `Assets\Docs\深入地牢`
- Trigger: a root-level PowerShell script plus a project `.codex` skill that tells future AI agents how to run it.
- The script does not commit changes.
- The script does not interpret design content or update game code.

## Behavior

The sync treats the source folder as authoritative for real design files.

- New source files are copied into the target.
- Changed source files overwrite the matching target files only when content differs.
- Target files that no longer exist in source are deleted.
- Unity `.meta` files are ignored when comparing source documents, but the target `.meta` for a deleted document is also deleted to avoid orphan Unity assets.
- Existing `.meta` files for retained documents are preserved.
- The script creates the target directory if it does not exist.

## Reporting

The script prints a concise summary and writes `docs/design-sync/latest-sync-report.md`.

The report includes:

- Sync timestamp.
- Source and target paths.
- Added, updated, deleted, unchanged, and deleted meta file lists.
- `git status --short -- Assets/Docs/深入地牢 docs/design-sync/latest-sync-report.md` output when git is available.

## Safety

- The script fails if the source directory does not exist.
- Paths are configurable through parameters for tests, but defaults match this project.
- The script only deletes files under the target directory.
- Empty directories left under the target are removed after file deletion.
- The script is non-destructive outside `Assets/Docs/深入地牢` and `docs/design-sync/latest-sync-report.md`.

## Testing

PowerShell tests exercise the sync against temporary directories.

The core regression case verifies:

- A new file is copied.
- A changed file is updated.
- A removed file is deleted.
- The removed file's `.meta` is deleted.
- An unchanged file's `.meta` is preserved.
- The report is written.

## Project Skill

Create `.codex/skills/sync-design-docs/SKILL.md` as a lightweight trigger skill. It should instruct future AI agents to run the root script, inspect the generated report and git diff, then stop before changing gameplay code unless the user explicitly asks for follow-up implementation.
