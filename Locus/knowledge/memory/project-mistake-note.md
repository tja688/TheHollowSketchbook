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
updatedAt: 1780661167644
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
- This project currently uses `com.unity.test-framework` `1.1.33`; Unity’s own package docs for 1.1 state async tests are not supported, so EditMode tests that block on `Task` via `GetAwaiter().GetResult()` can freeze the editor when MCP auto-runs Test Runner.
- When MCP package `com.coplaydev.unity-mcp` auto-runs tests, it installs `TestRunnerNoThrottle` and drives `UnityEditor.TestTools.TestRunner.EditModeRunner.TestConsumer` on `EditorApplication.update`; any test waiting for editor-frame progress from a synchronous `[Test]` body is a high-risk deadlock.
- For this project, async/concurrency coverage in `Assets/Scripts/Game/Core/Tests/DomainP0Tests.cs` should use `[UnityTest]` plus `IEnumerator` frame pumping instead of synchronous `[Test]` wrappers around `Task` waits.
<!-- locus:body:end -->
