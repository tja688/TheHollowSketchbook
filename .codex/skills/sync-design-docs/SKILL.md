---
name: sync-design-docs
description: Use when the user asks to sync, refresh, import, update, or compare the designer-maintained design documents for 深入地牢.
---

# Sync Design Docs

Use the root script. Do not manually copy folders.

## Workflow

1. Run from repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ".\sync-design-docs.ps1"
```

2. Read the generated report:

```powershell
docs/design-sync/latest-sync-report.md
```

3. Inspect scoped git changes:

```powershell
git status --short -- "Assets/Docs/深入地牢" "docs/design-sync/latest-sync-report.md"
git diff --name-status -- "Assets/Docs/深入地牢" "docs/design-sync/latest-sync-report.md"
git diff -- "Assets/Docs/深入地牢" "docs/design-sync/latest-sync-report.md"
```

4. Summarize added, updated, deleted, and notable content changes for the user.

5. Stop before changing gameplay code, infrastructure, scenes, prefabs, or assets unless the user explicitly asks for follow-up implementation.

## Notes

- Source defaults to `C:\Users\jinji\Desktop\文档\MyNote\游戏开发项目\深入地牢`.
- Target defaults to `Assets\Docs\深入地牢`.
- The script preserves retained Unity `.meta` files and deletes `.meta` files for removed documents.
- The script does not commit changes.
