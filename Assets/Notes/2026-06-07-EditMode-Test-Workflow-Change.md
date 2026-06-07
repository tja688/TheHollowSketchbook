# EditMode Test Workflow Change

更新时间：2026-06-07

## Root Cause

- `Assets/Scripts/Game/Core/Tests/DomainP0Tests.cs`
- `Assets/Scripts/Game/Core/Tests/DomainBatch3Tests.cs`
- `Assets/Scripts/Game/Core/Tests/DomainBatch4Tests.cs`
- `Assets/Scripts/Game/Content/Tests/StarterContentRegistryTests.cs`

这些测试文件都存在普通 `[Test]` 包裹异步 Domain 调用、再通过 `asyncTest().GetAwaiter().GetResult()` 同步等待的模式。

- 项目当前使用 `com.unity.test-framework` `1.1.33`。
- MCP 自动化测试入口会驱动 Unity EditMode Test Runner 的 `EditorApplication.update` 消费流程。
- 当测试本体同步阻塞、但完成条件又依赖编辑器帧推进时，会形成高风险死锁，表现为 Test Runner 卡死、Unity 崩溃或 MCP 调用悬挂。

## Decision

- 删除 `Assets/Scripts/Game/Core/Tests` 下的全部 EditMode 测试文件与 `Game.Core.Tests.asmdef`。
- 删除 `Assets/Scripts/Game/Content/Tests` 下的全部 EditMode 测试文件与 `Game.Content.Tests.asmdef`。
- 仓库默认验证工作流调整为“编译通过即验证”。

## AI Guardrails

- 不进入 Unity Test Runner 执行 Edit Mode 自动化测试。
- 不通过 MCP `run_tests` 触发 Edit Mode 测试。
- 如果历史文档仍提到 EditMode 回归套件，视为旧流程，不自动执行。

## Related Docs Updated

- `AGENTS.md`
- `.codex/skills/Foundation-Infrastructure-Dev/SKILL.md`
- `.codex/skills/Domain-Infrastructure-Dev/SKILL.md`
- `Assets/Notes/归档/项目开发规范.md`
- `Assets/Docs/项目程序开发/Foundation-Infrastructure/Verification-Workflow.md`
