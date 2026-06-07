# AI Development Router

This repository uses a four-layer AI development model for `Assets/Scripts/Game`. Before any implementation, review `rules.md` in the repository root and follow the matching skill under `.codex/skills`.

## Four-Layer Model

| Layer | Purpose | Current code area | AI posture |
|---|---|---|---|
| Foundation Infrastructure | Cross-project technical foundation: model registry, deterministic RNG, action queue, base save infrastructure, logging, common IDs, low-level hooks | Technical Core areas such as `Common`, `Models`, `Random`, `Actions`, base `Saves`, `Hooks`, `Logging`, `Compatibility` | Default read/call only. Modify only when explicitly requested. |
| Domain Infrastructure | Project-specific rules grammar for 深入地牢: 3x3 grid, card instances, intents, actions, damage, room flow, deck generation, domain save state, domain events | `Assets/Scripts/Game/Core/Runtime/Domain` plus domain save/run-flow support. Project-specific legacy rule/prototype areas under Core are reviewed as Domain when migrated or rule-changed. | Human-governed stable API. Extend only through explicit infrastructure tasks. |
| Game Logic And Content | Concrete cards, monsters, traps, items, relics, traits, room content, numbers, content registration | `Assets/Scripts/Game/Content/Runtime` | AI-led content production inside Domain contracts. |
| Presentation | UI, animation, VFX, audio, camera, input feedback, views, presenters, recipes, presentation manifest/orchestrator | `Assets/Scripts/Game/Presentation` | Human-controlled visual layer. AI may work here only through presentation contracts. |

Original design documents live in `Assets/Docs/深入地牢`. Read them when the task needs design alignment, but do not treat old notes as more authoritative than current source and the latest design docs.

## Mandatory Entry Steps

1. Read `rules.md` before changing files.
2. Check `.codex/skills` and invoke the matching project skill when a task touches a layer:
   - `Foundation-Infrastructure-Dev`
   - `Domain-Infrastructure-Dev`
   - `Game-LogicAndContent-Dev`
   - `Presentation-Dev`
3. Prefer code graph tools for code discovery and impact analysis. Start with code graph context for architecture, flow, bug, or refactor questions when available.
4. If the task is exploration, analysis, audit, review, alignment, or reporting and no output path is specified, write the report under `Assets/Notes`.

## Validation Workflow

- Do not run Unity Edit Mode tests or Test Runner automation for this repository unless the user explicitly asks to restore that workflow.
- Default AI verification is compile-only: trigger Unity compilation and treat clean compilation / no compile errors as the validation gate.
- If older notes or plans mention Core/EditMode regression suites, treat them as historical unless the user explicitly re-enables them.

## Routing Rules

- If the task asks for model registry, RNG, save base, action queue, shared IDs, hooks, logging, asmdef boundaries, or cross-project technical behavior, use `Foundation-Infrastructure-Dev`.
- If the task asks for grid/card/intent/action/damage/deck/room/progression/domain event/domain save behavior, use `Domain-Infrastructure-Dev`.
- If the task touches project-specific legacy/prototype rule code under Core, treat it as Domain unless the change is purely cross-project technical foundation.
- If the task asks for a concrete card, monster, trap, item, relic, trait, room reward, content registration, balance value, or gameplay content scenario, use `Game-LogicAndContent-Dev`.
- If the task asks for UI, animation, VFX, audio, camera, input feedback, view/presenter/orchestrator/manifest/recipe work, use `Presentation-Dev`.
- If a task crosses layers, start from the lowest affected layer and keep changes minimal. Do not change Foundation or Domain as a convenience for upper-layer work; request a separate infrastructure task when needed.

## Reporting Defaults

- Reports, audits, investigation notes, impact summaries, and design alignment writeups go to `Assets/Notes` unless the user specifies another path.
- Layer authority documents go under `Assets/Docs/项目程序开发/<LayerName>` as required by the layer skill.
- Do not create or edit `.unity` scene files directly. See `rules.md`.
