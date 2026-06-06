---
name: Foundation-Infrastructure-Dev
description: Use when modifying or reviewing TheHollowSketchbook Foundation Infrastructure such as ModelDb, deterministic RNG, action queues, base save infrastructure, shared IDs, hooks, logging, or Core assembly boundaries.
---

# Foundation Infrastructure Dev

## Purpose

Foundation Infrastructure is the reusable technical base beneath 深入地牢. It provides stable cross-project capabilities that upper layers depend on: model identity and registry, deterministic randomness, action scheduling, base save behavior, shared hooks, logging, common IDs, and pure C# assembly boundaries.

This layer is controlled. Default posture is read-only unless the user explicitly asks for a Foundation change.

## Code Areas

- `Assets/Scripts/Game/Core/Runtime/Common`
- `Assets/Scripts/Game/Core/Runtime/Models`
- `Assets/Scripts/Game/Core/Runtime/Random`
- `Assets/Scripts/Game/Core/Runtime/Actions`
- `Assets/Scripts/Game/Core/Runtime/Saves/SaveManager.cs`
- `Assets/Scripts/Game/Core/Runtime/Hooks`
- `Assets/Scripts/Game/Core/Runtime/Logging`
- `Assets/Scripts/Game/Core/Runtime/Compatibility`
- `Assets/Scripts/Game/Core/Game.Core.asmdef`
- Tests under `Assets/Scripts/Game/Core/Tests`

Domain-specific files under `Assets/Scripts/Game/Core/Runtime/Domain` are not Foundation; use `Domain-Infrastructure-Dev` for those.

## Hard Rules

- Read root `rules.md` first.
- Do not edit Foundation unless the user explicitly requested it.
- Do not directly edit `.unity` files.
- Keep `Game.Core.asmdef` pure C# with `noEngineReferences: true`.
- Do not add `UnityEngine`, `MonoBehaviour`, DOTween, audio, VFX, prefab, scene, or Presentation dependencies.
- Do not change deterministic RNG, model identity, save compatibility, action ordering, or hook semantics as a convenience for Content or Presentation.

## Workflow For Approved Foundation Changes

1. Confirm the user explicitly asked for Foundation work.
2. Use code graph context or equivalent search to locate entry points, callers, and upper-layer consumers.
3. Identify affected Domain, Content, Presentation, save, and test behavior before editing.
4. Write or update tests before implementation when behavior changes.
5. Make the smallest compatible change.
6. Run focused tests, then the relevant Core/EditMode regression suite when available.
7. Produce an upper-layer impact report in `Assets/Notes` unless the user supplied another path.
8. Update the Foundation authority documentation.

## Documentation Requirement

Maintain authoritative facts under:

`Assets/Docs/项目程序开发/Foundation-Infrastructure`

Each fact document should include:

- Design intent: why this Foundation capability exists.
- Architecture: public contracts, ownership, dependencies, and invariants.
- Usage notes: what upper layers may call and what they must not assume.
- Verification: tests or checks that prove the contract.
- Change memory: short dated notes for significant changes.

If this is the first document for the area or the user explicitly says “重建”, rebuild from a full layer scan and replace stale content. For normal maintenance, edit only the sections affected by the current task and append a concise change-memory entry.

## Common Failure Modes

| Rationalization | Required response |
|---|---|
| “Content needs this, so I’ll add a quick Foundation helper.” | Stop and request a scoped Foundation task. |
| “Compilation is easier if Core references Unity.” | Forbidden. Preserve `noEngineReferences: true`. |
| “This random choice is only temporary.” | Use `IRng` / `DeterministicRng` or do not add it. |
| “I can change save shape now and fix callers later.” | Forbidden without explicit migration and impact review. |
