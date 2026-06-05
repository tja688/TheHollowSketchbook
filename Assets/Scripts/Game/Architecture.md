# Assets/Scripts/Game Architecture

> Last updated: 2026-06-05  
> Scope: current as-built L0 foundation and project-level domain infrastructure under `Assets/Scripts/Game`.

## Current Verdict

L0 is now suitable for follow-up development of controlled L1 content that stays inside the domain contracts. The core grid, action, intent, room-generation, save/restore, lifecycle, inventory, relic, trait, and deterministic RNG paths are implemented and covered by the `Game.Core.Tests` EditMode suite.

This does not mean the whole game foundation is complete. It means the current L0-P0 domain operating system is usable as the default API surface for future content work, provided L1 work does not bypass `DomainFacade`, `GridState`, `PlayerIntent`, content contract models, or save adapters.

## Layer Model

The project uses four practical layers.

| Layer | Path | Responsibility | Ownership Rule |
|---|---|---|---|
| Foundation Infrastructure | `Core/Runtime/Actions`, `Core/Runtime/Models`, `Core/Runtime/Random`, `Core/Runtime/Saves`, `Core/Runtime/Logging`, `Core/Runtime/Hooks` | Portable low-level services: model registry, deterministic RNG, action queue, save shell, basic logging, hook primitives. | Treat as stable infrastructure. L1 content should call it, not reshape it. |
| Domain Infrastructure | `Core/Runtime/Domain` | The Hollow Sketchbook-specific rules: 3x3 grid, cards, zones, actions, intents, combat, inventory, relics, rooms, lifecycle events, invariants. | Main L0 API surface. AI content should extend through contracts, not mutate internals directly. |
| Content / Game Logic | `Content/Runtime` | Concrete cards, enemies, encounters, powers, characters, acts. Current folders are mostly placeholders. | Future L1 implementation area. It should reference `Game.Core` only. |
| Presentation | `Presentation/Runtime` | Unity-facing views, input, bootstrap, services, prototype run/combat UI. | Consumes domain events and submits intents. It should not own rules. |

## Assembly Boundaries

`Game.Core.asmdef` has `noEngineReferences: true` and no assembly references. The core layer is pure C# and should stay free of Unity APIs, `UnityEngine.Random`, frame time, scene objects, prefabs, or presentation services.

`Game.Core.Tests.asmdef` references only `Game.Core` and also has no engine references. The core test suite is therefore fast and deterministic enough for regression coverage of domain rules.

`Game.Presentation.asmdef` exists for Unity-facing code. A dedicated `Game.Content.asmdef` is still not present under `Assets/Scripts/Game/Content`; adding it remains a recommended next hardening step before broad L1 content work.

## Runtime Entry Point

The central domain entry point is `DomainFacade`.

Flow:

1. Presentation or a test creates a `PlayerIntent`.
2. `DomainFacade.PreviewIntent()` uses `IntentValidator` to return validity and highlights.
3. `DomainFacade.SubmitIntentAsync()` serializes submissions through a single gate.
4. The facade converts supported intents into `GameAction` instances and enqueues them in `ActionQueueSet`.
5. `ActionExecutor` drains the queue sequentially.
6. Each action mutates domain state through domain APIs, emits a `DomainEventBatch`, and may enqueue follow-up actions through `GameActionExecutionContext.EnqueueFollowUpActions()`.

Reentrant `SubmitIntentAsync()` calls from lifecycle hooks are rejected with `IntentRejected(SubmitIntentReentrant)`. Concurrent external submissions are serialized instead of draining the same queue in parallel.

## Intent Surface

Supported intents are defined in `PlayerIntent.cs`:

| Intent | Meaning | Counts Player Action |
|---|---|---|
| `MovePlayerIntent` | Move player card to an empty grid cell. | Yes |
| `InteractWithCardIntent` | Interact with a face-up top card on the grid. | Yes |
| `StoreItemIntent` | Move a face-up top item card from grid to inventory. | No |
| `UseItemIntent` | Use an inventory item with optional target selection. | Only when `ItemCardModel.CountsAsPlayerAction` is true |
| `ChooseOptionIntent` | Resolve an open choice session exactly once. | No |
| `ActivateRelicIntent` | Activate equipped active relic. | No |

`UseItemIntent` supports `ItemTargetSelection` for no target, card target, grid-cell target, card plus direction, two cards, card plus cell, and two cells. `IntentValidator` checks target existence, top-card status, face-up status, monster-only requirements, direction presence, and cell validity based on `ItemTargetMode`.

## Action Queue And Follow-Ups

`ActionQueueSet` owns action IDs and queue ordering. `ActionExecutor` drains until the queue is empty.

`GridOperationResult` carries:

| Field | Meaning |
|---|---|
| `Succeeded` | Whether the grid operation was accepted. |
| `FailureCode` | Stable rejection code for domain actions and previews. |
| `Events` | Domain events produced by the operation. |
| `FollowUpActions` | Additional `GameAction` instances that must enter the same queue drain. |

As of this update, `GameActionExecutionContext.EnqueueFollowUpActions()` is the bridge from `GridOperationResult.FollowUpActions` into the active queue. Domain actions that handle grid operation results enqueue follow-ups after collecting the result events.

## Grid And Card State

`GridState` is the only public mutation API for grid placement, movement, flipping, covering, removal, deck shuffling, and redistribution.

Core invariants:

| Invariant | Current Implementation |
|---|---|
| Grid is fixed 3x3 | `GridCoord`, `GridQueries`, and `GridCell[9]`. |
| Cell indices are 1-9 row-major | `GridCoord.FromCellIndex()` and `CellIndex`. |
| Each cell has an ordered stack | `GridCell`; top card is the last pushed card. |
| Only top cards can move, flip, store, or interact through normal APIs | Enforced by `GridState` and `IntentValidator`. |
| Card zone is explicit | `CardZone`: grid, deck, inventory, relic inventory, choice, reward, removed. |
| Removed cards are marked and leave grid coordinates | `RemoveCard()` and item consumption paths set `Zone = Removed`, clear coord/stack, and set `IsRemoved`. |
| Underlying top card flips when top card leaves a stack | `RemoveCard()` and `MoveTopCardToZone()` emit `CardFlipped(RevealAfterTopRemoved)`. |

Collection exposure has been hardened: grid cells, stacks, player inventory, and dungeon deck expose read-only wrappers rather than mutable backing lists or arrays.

## Events And Lifecycle

Domain state changes are represented as `DomainEvent` values grouped into `DomainEventBatch` by action ID and source intent.

Important event families:

| Event | Typical Producer |
|---|---|
| `CardAddedToGrid`, `CardMoved`, `CardCovered`, `CardZoneChanged` | Grid/deck/inventory movement APIs. |
| `CardFlipped`, `CardRemoved` | Grid removal/reveal APIs and item consumption. |
| `PlayerActionCommitted` | `PlayerActionCounter.Increment()`. |
| `DamageApplied`, `HealingApplied`, `GoldChanged` | Combat and reward-side domain operations. |
| `ItemStored`, `ItemUsed`, `RelicActivated` | Inventory/relic actions. |
| `ChoiceOpened`, `ChoiceResolved`, `RouteChoicesGenerated` | Choice/progression flows. |
| `IntentRejected` | Validation or execution rejection. |
| `RoomCleared`, `RunEnded` | Room/run terminal checks. |

`DomainActionContext.ProcessLifecycleAsync()` dispatches lifecycle callbacks from emitted events:

| Event | Dispatch Order |
|---|---|
| `CardFlipped` | `CardModel.OnRevealedAsync()`, then each `TraitModel.OnCardFlippedAsync()`. |
| `CardRemoved` | `CardModel.OnDestroyedAsync()`, then each `TraitModel.OnCardRemovedAsync()`. |
| Player action committed | Snapshot current face-up non-player grid observers, then call `CardModel.OnAfterPlayerActionCommittedAsync()`, then each `TraitModel.OnPlayerActionCommittedAsync()`. |

Observer snapshots prevent cards flipped during the current action from retroactively observing the same action. They start observing future player actions.

## Inventory And Relics

`PlayerInventory` stores item `CardInstance` objects and keeps them in `CardZone.PlayerInventory`. `StoreItemAction` moves items from grid to inventory through `GridState.MoveTopCardToZone()` and emits `ItemStored`.

`UseItemAction` now handles final-use consumption through domain events rather than silent direct mutation:

1. Emits `ItemUsed`.
2. Executes `ItemCardModel.UseAsync()`.
3. If `CountsAsPlayerAction` is true, increments the player action counter and notifies after-action observers.
4. Decrements `usesRemaining`.
5. If depleted, removes the card from inventory, tracks it in the domain card lookup, marks it removed, emits `CardZoneChanged(Removed)`, and emits `CardRemoved(Consumed)`.
6. Processes lifecycle callbacks and terminal checks.

`RelicInventory` supports passive relic IDs and one active relic slot. Damage hooks and active relic activation are routed through `DomainActionContext` and content contract methods on `RelicModel`.

## Combat And Damage Hooks

`CombatResolution` owns pure domain damage resolution. It operates on `CardInstance` combat stats and delegates extensibility to `DomainActionContext`.

Supported baseline behavior includes:

| Capability | Notes |
|---|---|
| Player vs monster | Uses attack, defense, counterattack, death removal, gold reward. |
| Player vs trap | Trap contact damage ignores player defense. |
| First strike | One or both sides can attack first through runtime state. |
| Damage prevention | Runtime `damageImmunity` can prevent damage. |
| Relic hooks | Before/after damage and damage dealt/taken modifiers. |
| Trait damage hooks | Source, target, and field observer trait modifiers are invoked. |

## Rooms, Decks, And Progression

The minimal L0 room pipeline exists:

| Component | Responsibility |
|---|---|
| `DungeonMapGenerator` | Generates the design-specific nine-node layer sequence and choice pools. |
| `RoomPlan` | Captures room type, layer/node indices, elite/boss flags, and RNG state. |
| `RunProgressionState` | Tracks current layer/node/room type and pending room choices. |
| `DungeonDeckBuilder` | Builds room card pools by room type and layer. |
| `MonsterAllocationRule` | Allocates monster tiers/counts while matching layer target counts. |
| `GridDealer` | Deals deck cards into the grid with combat and restaurant policies. |

Known policy currently covered by tests:

| Rule | Status |
|---|---|
| Each layer has 9 nodes | Covered. |
| Node 1 is reward, node 8 restaurant, node 9 boss combat | Covered. |
| Early and mid-layer choice pools differ | Covered. |
| Monster count matches `9 + layerIndex` target | Covered. |
| Combat rooms cover all non-player grid cells | Covered. |
| Restaurant deals exactly 4 non-player cards | Covered. |

## Save And Restore

`DomainSaveAdapter` captures and restores the current room-level domain aggregate.

Currently preserved:

| State | Status |
|---|---|
| Grid card instances, positions, face state, stack order, combat stats, runtime state | Implemented. |
| Action counter | Implemented. |
| Player gold | Implemented. |
| Item inventory card instances | Implemented. |
| Relic inventory and active relic uses | Implemented. |
| Dungeon deck order | Implemented. |
| Choice sessions | Implemented. |
| Run progression and pending room choices | Implemented. |
| RNG state | Implemented. |

Restore uses `DomainActionContext.ReplaceGrid()` so `CombatResolution` points at the restored grid instead of the constructor-time grid. Non-grid cards are restored before reattaching grid, deck, inventory, choice, and removed-zone ownership to avoid losing card instances outside the grid.

Still not modeled as first-class save objects: pending trigger queue, full player stat layers, trait inventories, and future content-specific state beyond `CardInstance` runtime state.

## Invariant Validation

`DomainInvariantValidator` supports grid-only validation and broader `DomainActionContext` validation.

Current checks include:

| Check | Scope |
|---|---|
| Exactly one player card on a valid combat grid | Grid. |
| Grid card `Zone`, `Coord`, `StackIndex`, and removed flags match placement | Grid. |
| Cross-zone duplicate `CardInstanceId` among grid, deck, inventory, and removed cards | Domain context. |
| Container-specific expected zones | Domain context. |
| Removed cards do not retain grid coordinates | Domain context. |

This validator is a safety net, not a replacement for using domain APIs.

## Testing Status

The current core regression suite is `Assets/Scripts/Game/Core/Tests/DomainP0Tests.cs` with 33 `[Test]` cases.

Covered areas include:

| Area | Examples |
|---|---|
| Grid basics | Coordinates, stacks, top-card behavior, reveal after removal. |
| Movement and interaction | Player movement, invalid interactions, monster defeat, room clear. |
| Combat | Defense, traps, first strike, simultaneous death, prevention, player defeat. |
| Collections and invariants | Read-only views, cross-zone duplicate detection. |
| Inventory and item use | Store item, target selection, lifecycle consumption events, countable item action commit. |
| Choice sessions | Missing session, valid resolution, duplicate rejection. |
| Save/restore | Grid, combat reference replacement, deck, inventory, choices, progression, RNG. |
| Hooks | Relic damage modifiers, trait flip/action/remove lifecycle. |
| Queue safety | Concurrent submit serialization, reentrant submit rejection, follow-up action enqueue path. |
| Room pipeline | Map generation, deck counts, monster allocation, grid dealing. |

`dotnet build Game.Core.Tests.csproj` currently succeeds with 0 warnings and 0 errors. Unity EditMode execution is the authoritative test runner for `[Test]` execution.

## Development Rules For L1 Content

L1 content should follow these constraints:

| Rule | Reason |
|---|---|
| Register content through `ModelDb` and derive from `CardModel`, `MonsterCardModel`, `TrapCardModel`, `ItemCardModel`, `RelicModel`, or `TraitModel`. | Keeps content behind stable contracts. |
| Submit player-facing operations through `DomainFacade.SubmitIntentAsync()`. | Preserves validation, action ordering, event batches, and reentrancy rules. |
| Use `GridState` operations for grid mutation. | Preserves zone, stack, reveal, removal, and event invariants. |
| Use `ItemTargetSelection` and `ItemTargetMode` for item targeting. | Avoids ad hoc target parsing. |
| Use `DomainEvent` output for presentation. | Keeps presentation reactive and rule-free. |
| Use `DomainActionContext.Rng` or injected `IRng` for randomness. | Preserves deterministic replay/save behavior. |
| Put temporary card state in `CardInstance` runtime state unless a first-class infrastructure type is needed. | Avoids uncontrolled fields in core entities. |

L1 content must not directly mutate `GridCell`, backing collections, `CardInstance.Zone`, `CardInstance.Coord`, `CardInstance.StackIndex`, or presentation objects to make rules happen.

## Remaining Gaps

These are known and should not be described as complete:

| Gap | Impact | Suggested Priority |
|---|---|---|
| No `Game.Content.asmdef` under `Assets/Scripts/Game/Content` | Content layer is not yet compile-time constrained to Core-only references. | High before broad L1. |
| No full player stat layer such as `PlayerRunState` / `StatModifier` | Permanent growth, room temporary buffs, relic stat modifiers, and character identity may tempt direct stat mutation. | High for relic/character work. |
| No first-class pending trigger queue | Delayed effects can be represented through runtime state and lifecycle hooks, but not yet as saveable scheduled trigger objects. | Medium-high for complex traps. |
| No formal domain command/query service facade for content authors | Content can still see rich domain objects; conventions rely on review and tests. | Medium. |
| Content folders are mostly empty | L0 is infrastructure-ready, not content-complete. | Expected for next phase. |
| Presentation contract is event-based but not fully specified per visual payload | Presentation can consume events, but exact animation/audio mapping remains human-designed. | Medium. |

## Readiness Assessment

L0-P0 infrastructure is usable for the next phase if development proceeds with guardrails:

| Area | Readiness |
|---|---|
| Core pure C# boundary | Ready. |
| 3x3 grid domain API | Ready. |
| Intent validation and domain facade submission | Ready for current supported intents. |
| Action queue serialization and follow-up bridge | Ready. |
| Domain events and lifecycle callbacks | Ready for current card, trait, item, relic paths. |
| Basic combat and damage hooks | Ready for simple-to-moderate L1 content. |
| Room/deck/deal P0 pipeline | Ready for prototype room generation. |
| Save/restore for current runtime state | Ready for current modeled state. |
| Broad content production | Conditionally ready after adding `Game.Content.asmdef` and agreeing on player stat/trigger scope. |

The practical recommendation is to start L1 with a small vertical slice of content that uses the existing contracts, while immediately adding missing compile-time content boundaries and player stat infrastructure before implementing relic-heavy or delayed-trigger-heavy systems.
