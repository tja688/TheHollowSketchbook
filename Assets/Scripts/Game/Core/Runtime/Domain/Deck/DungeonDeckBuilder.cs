using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Rooms;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;
using Game.Core.Runs;

namespace Game.Core.Domain.Deck
{
    public sealed class DungeonDeckBuilder
    {
        private readonly RoomContentCatalog _catalog;

        public DungeonDeckBuilder()
            : this(null)
        {
        }

        public DungeonDeckBuilder(RoomContentCatalog catalog)
        {
            _catalog = catalog;
        }

        public DungeonDeck Build(RoomPlan plan, RunState run, IRng rng)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            DungeonDeck deck = new DungeonDeck();
            CardFactory cardFactory = new CardFactory(plan, _catalog, rng);

            if (plan.RoomType == RoomType.Restaurant)
            {
                deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.Food, "food", "food"));
                for (int i = 0; i < 3; i++)
                {
                    deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.Mentor, "mentor", "mentor"));
                }
                deck.Shuffle(rng);
                return deck;
            }

            AddNormalMonsters(deck, cardFactory, plan, rng);
            AddRoomSpecificCards(deck, cardFactory, plan, rng);

            int trapCount = rng.NextInt(2, 5);
            for (int i = 0; i < trapCount; i++)
            {
                deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.Trap, "trap", "trap"));
            }

            int itemCount = rng.NextInt(4, 7);
            for (int i = 0; i < itemCount; i++)
            {
                deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.Item, "item", "item"));
            }

            deck.Shuffle(rng);
            return deck;
        }

        private static void AddNormalMonsters(DungeonDeck deck, CardFactory cardFactory, RoomPlan plan, IRng rng)
        {
            IReadOnlyDictionary<int, int> counts = MonsterAllocationRule.ForNode(plan.NodeIndex).AllocateCounts(plan.LayerIndex, rng);
            foreach (KeyValuePair<int, int> pair in counts)
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    CardInstance monster = cardFactory.CreateMonster(pair.Key);
                    deck.AddToTop(monster);
                }
            }
        }

        private static void AddRoomSpecificCards(DungeonDeck deck, CardFactory cardFactory, RoomPlan plan, IRng rng)
        {
            switch (plan.RoomType)
            {
                case RoomType.Gold:
                    deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.Gold, "gold", "gold"));
                    break;
                case RoomType.Treasure:
                case RoomType.Chest:
                    deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.Chest, "chest", "chest"));
                    break;
                case RoomType.StatUpgrade:
                    deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.StatUpgrade, "stat", "stat"));
                    break;
                case RoomType.Reward:
                    deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.Chest, "chest", "chest"));
                    deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.StatUpgrade, "stat", "stat"));
                    break;
                case RoomType.Shop:
                    int productCount = rng.NextInt(4, 7);
                    for (int i = 0; i < productCount; i++)
                    {
                        deck.AddToTop(cardFactory.CreateFromCatalogOrFallback(CardType.ShopProduct, "shop-product", "shop-product"));
                    }
                    break;
                case RoomType.EliteCombat:
                    deck.AddToTop(cardFactory.CreateElite(plan.LayerIndex));
                    break;
                case RoomType.Boss:
                case RoomType.BossCombat:
                    deck.AddToTop(cardFactory.CreateBoss(plan.LayerIndex));
                    break;
            }
        }

        private sealed class CardFactory
        {
            private uint _nextId;
            private readonly RoomContentCatalog _catalog;
            private readonly IRng _rng;
            private readonly RoomPlan _plan;

            public CardFactory(RoomPlan plan, RoomContentCatalog catalog, IRng rng)
            {
                _nextId = (uint)(plan.LayerIndex * 100000 + plan.NodeIndex * 1000 + 1);
                _catalog = catalog;
                _rng = rng;
                _plan = plan;
            }

            /// <summary>
            /// Create a card instance. If a registered model exists in the catalog
            /// for the given catalogCategory, use it (resolving through ModelDb).
            /// Otherwise fall back to a synthetic ModelId under the "l0" category.
            /// Runtime uniqueness is always in CardInstanceId, never in ModelId.
            /// </summary>
            public CardInstance CreateFromCatalogOrFallback(CardType cardType, string fallbackEntry, string catalogCategory)
            {
                uint id = _nextId++;
                CardInstance card;

                if (_catalog != null && _rng != null && _catalog.TryPickRandom(catalogCategory, _rng, out ModelId registeredId))
                {
                    // Use registered content model from ModelDb
                    CardModel model = ModelDb.Get<CardModel>(registeredId);
                    card = model.CreateInstance(new CardInstanceId(id));
                }
                else
                {
                    // Fallback: synthetic ModelId for prototype/testing
                    card = new CardInstance(new CardInstanceId(id), new ModelId("l0", fallbackEntry), cardType);
                    if (cardType == CardType.Gold)
                    {
                        card.ConfigureGoldValue(20);
                    }
                    else if (cardType == CardType.Trap)
                    {
                        card.ConfigureCombatStats(3, 0, 0, contactDamageToPlayer: 2, goldOnRemoved: 0);
                    }
                }

                return card;
            }

            public CardInstance CreateMonster(int tier)
            {
                uint id = _nextId++;

                if (_catalog != null && _rng != null)
                {
                    IReadOnlyList<ModelId> tierModels = _catalog.GetAvailableMonsters(tier);
                    if (tierModels.Count > 0)
                    {
                        ModelId selectedId = tierModels[_rng.NextInt(0, tierModels.Count)];
                        CardModel model = ModelDb.Get<CardModel>(selectedId);
                        CardInstance instance = model.CreateInstance(new CardInstanceId(id));
                        instance.SetState("monsterTier", tier);
                        return instance;
                    }
                }

                // Fallback: synthetic model with tier-based stats
                CardInstance fallback = new CardInstance(
                    new CardInstanceId(id),
                    new ModelId("l0", "monster-l" + tier + "-" + id),
                    CardType.Monster);
                fallback.ConfigureCombatStats(4 + tier * 2, 1 + tier, tier / 2, 0, 10);
                fallback.SetState("monsterTier", tier);
                return fallback;
            }

            public CardInstance CreateElite(int layerIndex)
            {
                uint id = _nextId++;

                if (_catalog != null && _rng != null && _catalog.TryPickRandom("elite", _rng, out ModelId eliteId))
                {
                    CardModel model = ModelDb.Get<CardModel>(eliteId);
                    CardInstance instance = model.CreateInstance(new CardInstanceId(id));
                    instance.SetState("elite", 1);
                    return instance;
                }

                CardInstance fallback = new CardInstance(
                    new CardInstanceId(id),
                    new ModelId("l0", "elite-monster-" + id),
                    CardType.Monster);
                fallback.ConfigureCombatStats(18 + layerIndex * 3, 5 + layerIndex, 2, 0, 30);
                fallback.SetState("elite", 1);
                return fallback;
            }

            public CardInstance CreateBoss(int layerIndex)
            {
                uint id = _nextId++;

                if (_catalog != null && _rng != null && _catalog.TryPickRandom("boss", _rng, out ModelId bossId))
                {
                    CardModel model = ModelDb.Get<CardModel>(bossId);
                    CardInstance instance = model.CreateInstance(new CardInstanceId(id));
                    instance.SetState("boss", 1);
                    return instance;
                }

                CardInstance fallback = new CardInstance(
                    new CardInstanceId(id),
                    new ModelId("l0", "boss-monster-" + id),
                    CardType.Monster);
                fallback.ConfigureCombatStats(30 + layerIndex * 5, 7 + layerIndex, 3, 0, 100);
                fallback.SetState("boss", 1);
                return fallback;
            }
        }
    }
}
