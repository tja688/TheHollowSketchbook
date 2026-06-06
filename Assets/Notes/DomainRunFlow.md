# Domain Run Flow

> Scope: describes the new Domain-based run pipeline that replaces the old RunManager prototype.

## Old vs New Flow

### Old (Prototype) — `Runs/RunManager.cs`
```
RunManager.StartNewRun()
  -> StandardActMapGenerator.Generate()  -> ActMap, MapPoint[]
  -> RoomFactory.CreateRoomForMapPoint() -> AbstractRoom
  -> events: RunStarted, MapChanged, RoomEntered
```
- Uses `RunState`, `ActMap`, `MapPoint`, `AbstractRoom`.
- NOT connected to `DomainActionContext`, `GridState`, or `DomainFacade`.
- Map is a tree of `MapPoint` nodes; player picks a child node to enter.
- `RunManager` still exists in the codebase for reference but should NOT be used by L1 content.

### New (Domain) — `DomainRunFlow`
```
DomainRunFlow.StartNewRun()
  -> DungeonMapGenerator.GenerateLayerPlans()  -> RoomPlan[9]
  -> DungeonDeckBuilder.Build(roomPlan)        -> DungeonDeck
  -> GridDealer.Deal(grid, deck)               -> GridState (cards face-down)
  -> DomainActionContext (ready for intents)

After Room Cleared:
  -> RoomTransitionService.GenerateAndPlaceRouteCards()
     -> 2-3 RouteChoice cards on grid (squeeze if no empty cells)
     -> RunProgressionState.PendingChoices updated

Player Selects Route (InteractWithCardIntent on RouteChoice card):
  -> RouteChoiceCardModel.OnPlayerInteractAsync()
     -> RoomTransitionService.EnterRoom()
        -> New GridState with player card carried over
        -> New DungeonDeck for next room
        -> RunProgressionState advanced to next node
```

## Key Differences

| Aspect | Old (RunManager) | New (DomainRunFlow) |
|---|---|---|
| Map structure | Tree of MapPoint | 9-node linear layer |
| Room content | AbstractRoom (opaque) | GridState + DungeonDeck + CardInstance |
| Route selection | Click child node on map | Interact with RouteChoice card on grid |
| State container | RunState (monolithic) | DomainActionContext (layered) |
| Player actions | Not modeled | DomainFacade + PlayerIntent + ActionQueue |
| Save/restore | SaveManager (full run) | DomainSaveAdapter (per-room) |
| Content boundary | No enforcement | Game.Content.asmdef -> Game.Core only |

## L1 Integration

L1 content should:
1. Register card models in `ModelDb` and `RoomContentCatalog`.
2. Create a `DomainRunFlow` instance to start runs.
3. Use `DomainFacade.SubmitIntentAsync()` for all player actions.
4. Listen to `DomainEventBatch` output for presentation.
5. Use `DomainSaveAdapter` for save/restore.

L1 content should NOT:
- Reference `RunManager`, `StandardActMapGenerator`, `RoomFactory`, or `PrototypeRunController`.
- Create `RunState` or `ActMap` objects.
- Bypass `DomainFacade` for player actions.

## Node Progression Rules

| Node | Fixed Type | Route Choices After Clear |
|---|---|---|
| 1 | Reward | 2-3 from {Gold, Chest, StatUpgrade} |
| 2 | Combat | 2-3 from {Gold, Chest, StatUpgrade} |
| 3 | Combat | 2-3 from {Gold, Chest, StatUpgrade} |
| 4 | Combat | 2-3 from {Gold, Chest, StatUpgrade, Shop, EliteCombat} |
| 5 | Combat | 2-3 from {Gold, Chest, StatUpgrade, Shop, EliteCombat} |
| 6 | Combat | 2-3 from {Gold, Chest, StatUpgrade, Shop, EliteCombat} |
| 7 | Combat | Forced: Restaurant |
| 8 | Restaurant | Forced: BossCombat (entrance card) |
| 9 | BossCombat | 1 card: next layer entrance |

## Room Transition Sequence

1. Last monster removed -> `RoomCleared` event emitted.
2. `RoomTransitionService.GenerateAndPlaceRouteCards()`:
   - Determine available room types from `DungeonMapGenerator`.
   - Create `RouteChoiceCardModel` instances, place face-up on empty cells.
   - Squeeze strategy: if no empty cells, stack on top of existing cards.
   - Update `RunProgressionState.PendingChoices`.
   - Emit `RouteChoicesGenerated` event.
3. Player interacts with a RouteChoice card (`InteractWithCardIntent`).
4. `RouteChoiceCardModel.OnPlayerInteractAsync()` calls `RoomTransitionService.EnterRoom()`.
5. `EnterRoom()`:
   - Preserve player card HP/stats.
   - Create new `GridState` with player card at cell 8.
   - Build new `DungeonDeck` for next room type.
   - Deal cards onto new grid.
   - Update `RunProgressionState` to next node.
   - Emit `RoomEntered` event.
