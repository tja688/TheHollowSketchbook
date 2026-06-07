# Foundation Verification Workflow

更新时间：2026-06-07

## Design Intent

当前仓库把“验证代码改动是否安全”的默认路径收敛为编译通过验证，避免 Unity Edit Mode Test Runner 与 MCP 自动化测试在编辑器主线程上相互阻塞，导致卡死或崩溃。

## Architecture

- 适用范围：整个仓库，尤其是 `Assets/Scripts/Game` 下的 Core、Content、Presentation 代码改动。
- 禁用对象：Unity Test Runner 中的 Edit Mode 自动化测试、MCP `run_tests` 对 EditMode 的触发、任何等价的 AI 自动测试入口。
- 当前验证门禁：Unity 重新编译成功，且控制台没有新的编译错误。
- 历史测试资产：`Assets/Scripts/Game/Core/Tests` 与 `Assets/Scripts/Game/Content/Tests` 已移除，不再作为日常回归入口。

## Usage Notes

- AI 执行代码任务时，先做代码与调用链排查，再以编译通过作为默认验证结束条件。
- 如果旧文档、旧计划、旧复盘里仍写着“跑 EditMode 测试”，按历史信息处理，不自动执行。
- 只有用户明确要求恢复或重建测试体系时，才允许重新讨论 Test Runner 工作流。

## Verification

- 必做：触发 Unity 编译并确认无编译错误。
- 可选：使用非 Test Runner 的静态检查、代码搜索、调用链核对、非测试程序集 `dotnet build` 作为辅助证据。
- 禁止：把 Edit Mode 自动化测试结果作为默认验证门禁。

## Change Memory

- 2026-06-07：删除 `Game.Core.Tests` / `Game.Content.Tests` 的 EditMode 测试资产，仓库默认验证工作流改为“编译通过即验证”。
