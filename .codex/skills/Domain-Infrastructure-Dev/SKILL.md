---
name: Domain-Infrastructure-Dev
description: Use when modifying or reviewing TheHollowSketchbook Domain Infrastructure such as grid, cards, intents, domain actions, damage, room flow, deck generation, domain events, progression, or domain save state.
---

# Domain Infrastructure Dev

## Purpose

Domain Infrastructure is the project-specific rules grammar for 深入地牢. It defines the 3x3 grid, card instance lifecycle, stack rules, player intents, domain actions, damage resolution, room flow, route cards, room deck generation, player progression, pending triggers, domain events, and domain save state.

This layer is stable infrastructure. Content and Presentation should call it, not duplicate or bypass it.

## Code Areas

- `Assets/Scripts/Game/Core/Runtime/Domain`
- Domain save support in `Assets/Scripts/Game/Core/Runtime/Saves/DomainSaveAdapter.cs`
- Domain save DTOs in `Assets/Scripts/Game/Core/Runtime/Saves/DomainSaveDto.cs`
- Domain tests under `Assets/Scripts/Game/Core/Tests`
- Current Domain entry point: `DomainRunFlow`, `DomainActionContext`, `DomainFacade`
- Project-specific legacy/prototype rule areas under `Assets/Scripts/Game/Core/Runtime/Map`, `Rooms`, `Runs`, `Rewards`, and `Entities` when the task migrates, replaces, or changes their gameplay semantics

Legacy prototype flow such as `RunManager`, old map generation, and prototype presentation controllers is reference-only for new Domain work unless the user explicitly asks to migrate or remove it. Do not classify project-specific rule migration in these areas as Foundation work.

## Hard Rules

- Read root `rules.md` first.
- Do not directly edit `.unity` files.
- Do not change Foundation contracts unless the user explicitly requested Foundation work.
- Keep Domain pure C# and independent from Presentation.
- All player-facing rule operations must enter through `PlayerIntent`, validation, `DomainFacade`, and the action queue.
- All visual-facing output must be domain facts in `DomainEventBatch`; do not emit presentation commands.
- Do not allow upper layers to directly set `CardInstance.Zone`, `Coord`, `StackIndex`, `IsFaceUp`, action counters, RNG state, or save DTO internals.

## Development Guidance

- Treat grid, card, action, damage, room, save, and event semantics as project law.
- Prefer intent-specific Domain APIs over exposing mutable data structures.
- Preserve deterministic behavior: same seed plus same intent sequence should produce the same domain state and event sequence.
- Route choices are grid cards selected by `InteractWithCardIntent`, not UI-only menu choices.
- Item storage/use and relic activation do not count as player actions.
- Any effect that can kill a card must flow through the unified death, removal, reward, lifecycle, and event paths.
- If a Content task reveals a missing Domain API, propose the API in `Assets/Notes`; do not smuggle the API into a content patch.

## Workflow

1. Use code graph context or equivalent search to understand the current flow and callers.
2. Compare against `Assets/Docs/深入地牢` when the task touches design rules.
3. Identify impacted Content and Presentation contracts before editing.
4. Add or update focused Domain tests first for behavior changes.
5. Implement the smallest infrastructure change that preserves invariants.
6. Run focused tests and relevant Core/EditMode tests.
7. If public API, event payloads, save DTOs, action ordering, or room flow changed, write an impact report under `Assets/Notes` unless another path is specified.
8. Update Domain authority documentation and design correspondence.

## Documentation Requirement

Maintain authoritative facts under:

`Assets/Docs/项目程序开发/Domain-Infrastructure`

Required documents:

- Authority facts: design intent, architecture, public contracts, invariants, testing, and change memory.
- Design correspondence: map relevant design requirements from `Assets/Docs/深入地牢` to current implementation files, gaps, and tests.

If this is first-time documentation or the user says “重建”, rebuild from a full scan. For normal maintenance, update only sections touched by the task and append a concise change-memory entry.

## Common Failure Modes

| Rationalization | Required response |
|---|---|
| “Just set the card field directly.” | Use or add an approved Domain operation. |
| “Route choice is easier as a UI menu.” | Use route cards and `InteractWithCardIntent`. |
| “This one item should count as an action.” | Forbidden unless latest design docs and a Domain task change the invariant. |
| “Presentation needs this event, so I’ll play VFX here.” | Emit a domain event with payload; Presentation handles playback. |
