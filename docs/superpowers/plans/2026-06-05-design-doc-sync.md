# Design Document Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a root-level AI-triggerable tool that syncs authoritative design docs into `Assets/Docs/深入地牢` and reports the exact project git changes.

**Architecture:** A focused PowerShell script performs content-based recursive sync from source to target and writes a markdown report. A small `.codex` skill acts as the AI-facing trigger and delegates real work to the script.

**Tech Stack:** PowerShell 7+, git CLI, Codex-style project skill markdown.

---

### Task 1: Script Regression Test

**Files:**
- Create: `tests/SyncDesignDocs.Tests.ps1`

- [ ] **Step 1: Write the failing test**

```powershell
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot 'sync-design-docs.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('design-sync-test-' + [System.Guid]::NewGuid().ToString('N'))
$source = Join-Path $tempRoot 'source'
$target = Join-Path $tempRoot 'target'
$report = Join-Path $tempRoot 'latest-sync-report.md'

New-Item -ItemType Directory -Path $source, $target | Out-Null
New-Item -ItemType Directory -Path (Join-Path $source '归档'), (Join-Path $target '归档') | Out-Null
Set-Content -LiteralPath (Join-Path $source '新增.md') -Value 'new from source' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $source '变更.md') -Value 'updated from source' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $source '保留.md') -Value 'same content' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $source '归档\子文档.md') -Value 'nested source' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '变更.md') -Value 'old target' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '保留.md') -Value 'same content' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '保留.md.meta') -Value 'keep this meta' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '删除.md') -Value 'remove me' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '删除.md.meta') -Value 'remove orphan meta' -Encoding UTF8

try {
    & pwsh -NoProfile -ExecutionPolicy Bypass -File $scriptPath -SourcePath $source -TargetPath $target -ReportPath $report | Out-Null

    if (-not (Test-Path -LiteralPath (Join-Path $target '新增.md'))) { throw 'Expected added file to be copied.' }
    if ((Get-Content -LiteralPath (Join-Path $target '变更.md') -Raw).Trim() -ne 'updated from source') { throw 'Expected changed file to be updated.' }
    if (Test-Path -LiteralPath (Join-Path $target '删除.md')) { throw 'Expected removed target file to be deleted.' }
    if (Test-Path -LiteralPath (Join-Path $target '删除.md.meta')) { throw 'Expected deleted target file meta to be deleted.' }
    if (-not (Test-Path -LiteralPath (Join-Path $target '保留.md.meta'))) { throw 'Expected retained target file meta to be preserved.' }
    if (-not (Test-Path -LiteralPath (Join-Path $target '归档\子文档.md'))) { throw 'Expected nested file to be copied.' }
    if (-not (Test-Path -LiteralPath $report)) { throw 'Expected sync report to be written.' }
    if (-not ((Get-Content -LiteralPath $report -Raw) -match 'Added')) { throw 'Expected report to include Added section.' }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File tests/SyncDesignDocs.Tests.ps1`
Expected: FAIL because `sync-design-docs.ps1` does not exist yet.

### Task 2: Sync Script

**Files:**
- Create: `sync-design-docs.ps1`

- [ ] **Step 1: Implement minimal script**

Create `sync-design-docs.ps1` with parameters for source, target, and report paths. Recursively compare non-`.meta` source files by relative path and SHA256 hash, copy added/changed files, delete target files missing from source, delete their paired `.meta`, remove empty directories, write a markdown report, and print the summary.

- [ ] **Step 2: Run regression test**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File tests/SyncDesignDocs.Tests.ps1`
Expected: PASS with no output.

### Task 3: Codex Trigger Skill

**Files:**
- Create: `.codex/skills/sync-design-docs/SKILL.md`

- [ ] **Step 1: Create project skill**

The skill frontmatter uses `name: sync-design-docs` and a trigger-focused description. The body tells the AI to run `pwsh -NoProfile -ExecutionPolicy Bypass -File .\sync-design-docs.ps1`, read `docs/design-sync/latest-sync-report.md`, inspect `git diff -- Assets/Docs/深入地牢 docs/design-sync/latest-sync-report.md`, and stop unless follow-up implementation is requested.

- [ ] **Step 2: Verify skill file**

Run: `git diff -- .codex/skills/sync-design-docs/SKILL.md`
Expected: frontmatter and workflow are present.

### Task 4: Final Verification

**Files:**
- Modify: `docs/design-sync/latest-sync-report.md` generated by running the script against the real source and target.

- [ ] **Step 1: Run real sync**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File .\sync-design-docs.ps1`
Expected: script exits 0 and prints the change summary.

- [ ] **Step 2: Inspect project changes**

Run: `git status --short -- sync-design-docs.ps1 tests/SyncDesignDocs.Tests.ps1 .codex/skills/sync-design-docs docs/superpowers docs/design-sync Assets/Docs/深入地牢`
Expected: only intended script, test, skill, spec/plan, report, and synced doc changes appear.

- [ ] **Step 3: Do not commit automatically**

The user did not explicitly request a commit. Leave changes unstaged.
