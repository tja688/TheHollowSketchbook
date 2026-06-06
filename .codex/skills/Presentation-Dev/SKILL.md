---
name: Presentation-Dev
description: Use when adding or modifying TheHollowSketchbook Presentation work such as UI, animation, VFX, audio, camera, input adapters, views, presenters, orchestrators, recipes, cue manifests, or visual feedback.
---

# Presentation Dev

## Purpose

Presentation is the human-controlled visual and interaction layer. It turns player input into `PlayerIntent`, asks Domain for previews and submissions, and turns `DomainEventBatch` facts into UI, animation, VFX, audio, camera, and feedback.

Presentation must never become a rules layer. It displays and orchestrates outcomes; it does not decide gameplay truth.

## Code Areas

- `Assets/Scripts/Game/Presentation`
- `Assets/Scripts/Game/Presentation/Runtime`
- Future contract/input/presenter/view/recipe/manifest areas under `Assets/Scripts/Game/Presentation`
- `Assets/Scripts/Game/Presentation/Game.Presentation.asmdef`

Presentation may reference Unity-facing APIs. Core and Content may not reference Presentation. Presentation should not reference `Game.Content` unless the user explicitly approves a Presentation contract change; prefer Core contracts, read models, and Domain events.

## Hard Rules

- Read root `rules.md` first.
- Do not directly edit `.unity` files.
- Do not calculate damage, rewards, card zones, grid legality, action counts, room clear state, RNG outcomes, or save state in Presentation.
- Do not directly mutate `GridState`, `CardInstance`, `DomainActionContext`, save DTOs, or Content models from views or presenters.
- Do not make rule correctness depend on animation duration, coroutine completion, frame timing, or visual resource availability.
- Do not add Presentation references to `Game.Core` or `Game.Content`.
- Do not add `Game.Content` references to `Game.Presentation` without explicit user approval.

## Development Guidance

- Input adapters convert pointer/click/drag/UI actions into `PlayerIntent` and call `DomainFacade.PreviewIntent` or `SubmitIntentAsync`.
- Views are dumb objects: they know visual identity and play visual methods, but they do not resolve rules.
- Orchestrators and presenters consume `DomainEventBatch` and map events to cues, timelines, recipes, view data, and feedback.
- Missing visual contract, cue, view-data adapter, fallback, or manifest entry is a Presentation contract gap. Report it or implement it in Presentation, not in Content or Domain.
- Missing Domain event type or Domain event payload is a Domain request. Document the need in `Assets/Notes` unless the task explicitly includes Domain work.
- Visual failure should degrade with fallback/warning where possible; it must not corrupt Domain state.

## Presentation Contract Pattern

The desired seam is:

`Presentation input -> PlayerIntent -> DomainFacade -> DomainEventBatch -> Presentation Orchestrator -> Presenter/View/Recipe`

AI should not write content code that plays visuals. Content emits semantic facts; Presentation maps facts to visuals.

## Testing And Verification

- Test input adapters by asserting they create the intended `PlayerIntent` and do not mutate Domain state directly.
- Test orchestrator/cue resolution with fake or no-op view services where possible.
- Test missing cue/resource behavior as fallback or logged warning.
- For visual work that cannot be fully automated, include manual verification notes and screenshots/captures only when requested.

## Documentation Requirement

Maintain authoritative facts under:

`Assets/Docs/项目程序开发/Presentation`

Required documents:

- Authority facts: layer design intent, input seam, event seam, manifest/cue architecture, presenter/view responsibilities, verification, and change memory.
- Design correspondence: map presentation implementation back to `Assets/Docs/深入地牢` interaction and feedback requirements, including open cue/resource gaps.

If this layer has no prior authority docs or the user says “重建”, build them from a full scan. For normal maintenance, update only the affected sections and append a dated change-memory entry.

## Common Failure Modes

| Rationalization | Required response |
|---|---|
| “The view knows the target, so it can apply damage.” | Submit `PlayerIntent`; Domain resolves damage. |
| “Animation should finish before the rule happens.” | Domain state changes first; Presentation may gate input, not truth. |
| “No cue exists, so Content should play VFX directly.” | Add/request Presentation cue or manifest entry. |
| “Scene setup is easiest by editing `.unity` text.” | Forbidden. Use approved Unity/editor workflows only. |
