---
id: kd_builtin_memory_project_mistake_note
type: memory
path: project-mistake-note.md
title: project-mistake-note
injectMode: full
summaryEnabled: false
summaryCache: Verified project pitfalls and stale-note warnings.
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1779245302335
updatedAt: 1780588467196
---

# project-mistake-note

<!-- locus:maintain-rules:start -->
- Record only verified problems, rework causes, and avoidance steps
- Prioritize recurring pitfalls, constraints, regression points, and confirmed fixes
- Keep each entry short and focused on one lesson or constraint
- Keep the list within 20 items and merge duplicates regularly
- Remove outdated issues, non-reproducible issues, and unsupported guesses
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
## Verified Pitfalls

- URP `Assets/Settings/URP-HighFidelity.asset` has a project-specific serialized `m_UpscalingFilter = 4`; do not assume standard enum meaning without checking the current Unity/URP version.
- The render scene `Assets/Tests/render.unity` now uses an active Volume profile; older notes claiming no Volume are stale.
- `Assets/Notes` contains hand-authored tuning summaries that should be condensed into project memory rather than copied verbatim.
- On Windows/Git Bash, avoid very long one-shot shell commands for bulk file generation; use `write`/`edit` per file or short scripts to avoid OS error 206 (file name or extension too long).
<!-- locus:body:end -->
