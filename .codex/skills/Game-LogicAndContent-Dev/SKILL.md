---
name: Game-LogicAndContent-Dev
description: Use when adding or modifying TheHollowSketchbook gameplay content such as concrete cards, monsters, traps, items, relics, traits, room rewards, content registration, balance values, or content scenario tests.
---

# Game Logic And Content Dev

## Purpose

Game Logic And Content is the AI-led production layer. It implements concrete game rules and data: monsters, traps, items, relics, traits, room-derived cards, room content pools, balance values, content registration, and scenario tests.

This layer must stay inside Domain contracts. It expresses what a specific piece of content does; it does not redefine grid rules, action counting, save format, event semantics, or presentation playback.

## Editable Code Areas

- `Assets/Scripts/Game/Content/Runtime`
- `Assets/Scripts/Game/Content/Game.Content.asmdef`

## Read/Call Dependencies

- Existing Domain content contracts in `Assets/Scripts/Game/Core/Runtime/Domain/ContentContracts`
- Content-facing Domain systems such as `RoomContentCatalog`, `ModelDb`, `CardModel`, and `DomainActionContext`

Content may depend on `Game.Core` only. `Game.Content.asmdef` must remain `noEngineReferences: true`.
Do not edit Domain or Foundation files during a Content task unless the user explicitly expands the task into that lower layer and the matching lower-layer skill is used.

## Hard Rules

- Read root `rules.md` first.
- Do not directly edit `.unity` files.
- Do not use `UnityEngine`, `MonoBehaviour`, DOTween, Audio, VFX, `GameServices`, Presentation classes, prefabs, scenes, transforms, frame time, or animation timing.
- Do not mutate Domain internals directly.
- Do not add public Domain APIs during a Content task unless the user explicitly approved a Domain Infrastructure change.
- Do not invent visual cue strings or call visual services.
- Do not connect new content to legacy prototype run flow.

## Development Guidance

- Implement content as subclasses or registrations of Domain contracts such as `MonsterCardModel`, `TrapCardModel`, `ItemCardModel`, `RoomCardModel`, `RelicModel`, and `TraitModel`.
- Use stable content-template `ModelId` values. Runtime uniqueness belongs to `CardInstanceId`.
- Register content through the project’s model/content registration mechanism and room catalog conventions.
- Use Domain services and context methods for damage, movement, card removal, gold, choices, pending triggers, and room flow.
- Produce complete `DomainEvent` facts so Presentation can map them to cues.
- If a needed Domain operation, event payload, content contract, or save field is missing, write a request in `Assets/Notes` and stop or keep the current content scoped to existing APIs.
- Use `Assets/Docs/深入地牢` as design source when implementing or balancing content.

## Testing And Verification

- Add focused content tests for each new behavior when a test harness exists.
- At minimum, cover target selection, illegal target rejection, event sequence, action-count behavior, deterministic RNG, death/reward behavior, and save-relevant runtime state for stateful content.
- Prefer scenario-style tests that run without Presentation.
- Verify `Game.Content` still compiles without Unity engine references.

## Documentation Requirement

Maintain authoritative facts under:

`Assets/Docs/项目程序开发/Game-LogicAndContent`

Required documents:

- Authority facts: layer design intent, content architecture, registration patterns, content catalog notes, testing approach, and change memory.
- Design correspondence: map implemented content back to `Assets/Docs/深入地牢` design requirements, including intentional deviations and open gaps.

If this layer has no prior authority docs or the user says “重建”, build them from a full scan. For normal maintenance, update only the areas touched and append a dated change-memory entry.

## Presentation Contract For Content

Content does not “use visuals” by calling visuals. It uses visuals by emitting the right domain facts.

For each new content item, document:

- Domain events it can produce.
- Required payload fields for Presentation.
- Presentation tags if the existing contract supports them.
- Whether a new Presentation cue is needed. If yes, request it; do not implement it from Content.

## Common Failure Modes

| Rationalization | Required response |
|---|---|
| “This trap needs VFX, so call VFX here.” | Emit `DomainEvent`; request a Presentation cue if missing. |
| “I need to move a card, so I’ll set `Coord`.” | Use Domain movement APIs. |
| “A powerful item should count as a player action.” | Forbidden by current rules. |
| “No content test harness exists, so no verification.” | Add the narrowest non-Presentation test or document the blocker in `Assets/Notes`. |
