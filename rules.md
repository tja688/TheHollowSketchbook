# Development Rules

These rules are hard project constraints. They apply even when a task seems small or urgent.

## Absolute Prohibitions

- Never directly modify files with the `.unity` extension under any circumstance.
- Never weaken assembly boundaries to make a task compile faster.
- Never use Presentation or Unity engine APIs from `Game.Core` or `Game.Content`.
- Never bypass Domain APIs by directly mutating card location, stack, zone, face-up state, action counter, RNG state, save DTOs, or room progression state.

## Foundation Infrastructure Governance

Foundation Infrastructure is the bottom technical foundation of the project. It is serious, stable, and controlled.

Normally, if the user did not explicitly ask to modify Foundation Infrastructure, it is not editable. If you discover a missing Foundation capability while working on another task, finish or stop the current task safely, then report a proposed Foundation change for human review. Do not opportunistically alter Foundation to unblock upper-layer work.

Foundation examples include:

- `ModelDb`, `AbstractModel`, `ModelId` and shared model identity behavior.
- `DeterministicRng` / `IRng` and deterministic randomness contracts.
- `ActionQueueSet`, `ActionExecutor`, base action scheduling semantics.
- Base save infrastructure such as `SaveManager` and cross-cutting serialization policy.
- Shared hooks, common utilities, logging, compatibility shims, and `Game.Core.asmdef` boundaries.

## Assembly Boundaries

- `Assets/Scripts/Game/Core/Game.Core.asmdef` must stay pure C# with `noEngineReferences: true`.
- `Assets/Scripts/Game/Content/Game.Content.asmdef` must reference `Game.Core` only and must stay `noEngineReferences: true`.
- `Assets/Scripts/Game/Presentation/Game.Presentation.asmdef` is the Unity-facing layer and may reference Unity-facing packages needed for views and feedback.
- Do not add `Game.Presentation` references to `Game.Core` or `Game.Content`.
- Do not add `Game.Content` references to `Game.Presentation` unless the user explicitly approves a Presentation contract change. Presentation should consume Core contracts, read models, and Domain events instead of concrete Content types.
- Do not add `UnityEngine`, DOTween, audio, VFX, prefab, scene, or `MonoBehaviour` dependencies to Core or Content.

## Domain State Rules

- Player input enters rules through `PlayerIntent`, `IntentValidator`, `DomainFacade`, and the action queue.
- Domain output to visuals is a `DomainEventBatch`; events are facts, not presentation commands.
- Card movement, flip, stack, cover, remove, zone transfer, deck redistribution, and reveal behavior must go through Domain operations.
- Player movement and player-card interaction count as player actions. Item storage, item use, item dragging, choice selection, and relic activation do not count as player actions unless the latest design docs explicitly change this through a Domain task.
- Random rule behavior must use `IRng` / `DeterministicRng`, never `System.Random`, `UnityEngine.Random`, current time, frame time, or animation timing.

## Layer Escalation

- Content work cannot add public Domain APIs without a separate Domain Infrastructure task or explicit user approval.
- Presentation work cannot change rules to fit a visual effect. It must request missing events, payloads, cue IDs, or manifest entries.
- Domain work cannot change Foundation contracts unless the user explicitly requested a Foundation change.
- If an implementation needs a lower-layer change, document the need and proposed API in `Assets/Notes` unless the user already gave a target path.
