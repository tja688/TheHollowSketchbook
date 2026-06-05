# L0 Room Deck Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the minimal L0 room generation, dungeon deck construction, monster allocation, and grid dealing pipeline required by the project-level design.

**Architecture:** Keep the implementation in `Game.Core` pure C# and focused on domain data. Add a linear 9-node dungeon flow alongside the existing StS-style map generator, plus small deck/dealer services that create placeholder `CardInstance` objects using existing card types.

**Tech Stack:** Unity C# domain code, NUnit EditMode tests, existing `IRng`, `DungeonDeck`, `GridState`, `CardInstance`, and `ModelId` primitives.

---

### Task 1: Tests First

**Files:**
- Modify: `Assets/Scripts/Game/Core/Tests/DomainP0Tests.cs`

- [ ] **Step 1: Add failing tests**

Add tests that call the desired public API before it exists:

```csharp
[Test]
public void DungeonMapGenerator_CreatesDesignNineNodeLayer()
{
    DungeonMapGenerator generator = new DungeonMapGenerator();
    IReadOnlyList<RoomPlan> plans = generator.GenerateLayerPlans(1);

    Assert.AreEqual(9, plans.Count);
    Assert.AreEqual(RoomType.Reward, plans[0].RoomType);
    Assert.AreEqual(RoomType.Restaurant, plans[7].RoomType);
    Assert.AreEqual(RoomType.BossCombat, plans[8].RoomType);
    CollectionAssert.AreEquivalent(new[] { RoomType.Gold, RoomType.Chest, RoomType.StatUpgrade }, generator.GetChoicePoolAfterNode(1));
    CollectionAssert.Contains(generator.GetChoicePoolAfterNode(4), RoomType.Shop);
    CollectionAssert.Contains(generator.GetChoicePoolAfterNode(4), RoomType.EliteCombat);
    Assert.AreEqual(RoomType.Restaurant, generator.GetForcedNextRoomAfterNode(7));
}
```

```csharp
[Test]
public void DungeonDeckBuilder_BuildsCountsByRoomTypeAndLayer()
{
    DungeonDeckBuilder builder = new DungeonDeckBuilder();
    DungeonDeck rewardDeck = builder.Build(new RoomPlan(RoomType.Reward, 1, 1, false, false, new RngState(7)), null, new DeterministicRng(7));

    Assert.AreEqual(10, rewardDeck.Cards.Count(card => card.CardType == CardType.Monster));
    Assert.AreEqual(1, rewardDeck.Cards.Count(card => card.CardType == CardType.Chest));
    Assert.AreEqual(1, rewardDeck.Cards.Count(card => card.CardType == CardType.StatUpgrade));
    Assert.That(rewardDeck.Cards.Count(card => card.CardType == CardType.Trap), Is.InRange(2, 4));
    Assert.That(rewardDeck.Cards.Count(card => card.CardType == CardType.Item), Is.InRange(4, 6));

    DungeonDeck restaurantDeck = builder.Build(new RoomPlan(RoomType.Restaurant, 1, 8, false, false, new RngState(8)), null, new DeterministicRng(8));
    Assert.AreEqual(1, restaurantDeck.Cards.Count(card => card.CardType == CardType.Food));
    Assert.AreEqual(3, restaurantDeck.Cards.Count(card => card.CardType == CardType.Mentor));
    Assert.AreEqual(0, restaurantDeck.Cards.Count(card => card.CardType == CardType.Trap));
    Assert.AreEqual(0, restaurantDeck.Cards.Count(card => card.CardType == CardType.Item));
}
```

```csharp
[Test]
public void GridDealer_DealsCombatCoverageAndRestaurantException()
{
    DungeonDeckBuilder builder = new DungeonDeckBuilder();
    GridDealer dealer = new GridDealer();

    GridState combatGrid = NewGridWithPlayer();
    DungeonDeck combatDeck = builder.Build(new RoomPlan(RoomType.Combat, 1, 2, false, false, new RngState(11)), null, new DeterministicRng(11));
    dealer.Deal(combatGrid, combatDeck, DealPolicy.CombatDefault(), new DeterministicRng(12));
    Assert.IsTrue(GridQueries.AllCoordsRowMajor().Where(coord => coord.CellIndex != 8).All(coord => combatGrid.GetStack(coord).Count >= 1));
    Assert.IsTrue(combatGrid.AllGridCards.Where(card => card.CardType != CardType.Player).All(card => !card.IsFaceUp));

    GridState restaurantGrid = NewGridWithPlayer();
    DungeonDeck restaurantDeck = builder.Build(new RoomPlan(RoomType.Restaurant, 1, 8, false, false, new RngState(13)), null, new DeterministicRng(13));
    dealer.Deal(restaurantGrid, restaurantDeck, DealPolicy.RestaurantDefault(), new DeterministicRng(14));
    Assert.AreEqual(4, restaurantGrid.AllGridCards.Count(card => card.CardType != CardType.Player));
}
```

- [ ] **Step 2: Run tests and verify RED**

Run Unity EditMode tests for `Game.Core.Tests`. Expected: compile/test failure because `DungeonMapGenerator`, `RoomPlan`, `DungeonDeckBuilder`, `GridDealer`, and policy types do not exist yet.

### Task 2: Minimal Domain Implementation

**Files:**
- Modify: `Assets/Scripts/Game/Core/Runtime/Rooms/RoomType.cs`
- Modify: `Assets/Scripts/Game/Core/Runtime/Map/MapPointType.cs`
- Create: `Assets/Scripts/Game/Core/Runtime/Domain/Rooms/RoomPlan.cs`
- Create: `Assets/Scripts/Game/Core/Runtime/Domain/Rooms/RunProgressionState.cs`
- Create: `Assets/Scripts/Game/Core/Runtime/Domain/Rooms/DungeonMapGenerator.cs`
- Create: `Assets/Scripts/Game/Core/Runtime/Domain/Deck/MonsterAllocationRule.cs`
- Create: `Assets/Scripts/Game/Core/Runtime/Domain/Deck/DungeonDeckBuilder.cs`
- Create: `Assets/Scripts/Game/Core/Runtime/Domain/Grid/GridDealer.cs`

- [ ] **Step 1: Extend room/map enums**

Add missing L0 room types while retaining existing values for compatibility.

- [ ] **Step 2: Add room progression records**

Create immutable `RoomPlan` and `RunProgressionState` classes matching the design fields.

- [ ] **Step 3: Add 9-node generator**

Generate one linear layer with fixed nodes 1, 8, 9 and expose choice pools after nodes 1-6 plus forced restaurant after node 7.

- [ ] **Step 4: Add monster allocation**

Implement the documented random range algorithm and strict total correction to `9 + layerIndex`.

- [ ] **Step 5: Add deck builder**

Build placeholder cards with deterministic instance ids, normal/elite/boss monster additions, room-specific cards, traps/items except restaurant, and final shuffle.

- [ ] **Step 6: Add grid dealer**

Deal cards face-down by default, exclude player cell 8, satisfy `AllNonPlayerCells` for combat rooms, and allow restaurant to deal only four cards.

### Task 3: Verification And Report Note

**Files:**
- Modify: `Assets/Notes/L0领域基础设施外部质检报告.md`

- [ ] **Step 1: Run targeted tests and full relevant test set**

Run Unity EditMode tests for `Game.Core.Tests`. Expected: all tests pass.

- [ ] **Step 2: Add report note**

Under section `### 2. 房间生成、地图流程、发牌管线未落地`, add a dated note stating the minimal L0 pipeline now exists and list the files/tests.

- [ ] **Step 3: Review diff**

Run `git diff --stat` and `git diff -- Assets/Scripts/Game Assets/Notes/L0领域基础设施外部质检报告.md docs/superpowers/plans/2026-06-05-l0-room-deck-pipeline.md` to confirm scope.

---

Self-review: The plan covers the approved scope, contains no placeholder requirements, keeps implementation pure Core C#, and intentionally excludes UI, real content effects, save integration, and commits.
