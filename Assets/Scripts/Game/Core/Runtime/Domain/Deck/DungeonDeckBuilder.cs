using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Rooms;
using Game.Core.Random;
using Game.Core.Rooms;
using Game.Core.Runs;

namespace Game.Core.Domain.Deck
{
    public sealed class DungeonDeckBuilder
    {
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
            CardFactory cardFactory = new CardFactory(plan);

            if (plan.RoomType == RoomType.Restaurant)
            {
                deck.AddToTop(cardFactory.Create(CardType.Food, "food"));
                for (int i = 0; i < 3; i++)
                {
                    deck.AddToTop(cardFactory.Create(CardType.Mentor, "mentor"));
                }
                deck.Shuffle(rng);
                return deck;
            }

            AddNormalMonsters(deck, cardFactory, plan, rng);
            AddRoomSpecificCards(deck, cardFactory, plan, rng);

            int trapCount = rng.NextInt(2, 5);
            for (int i = 0; i < trapCount; i++)
            {
                CardInstance trap = cardFactory.Create(CardType.Trap, "trap");
                trap.ConfigureCombatStats(3, 0, 0, contactDamageToPlayer: 2, goldOnRemoved: 0);
                deck.AddToTop(trap);
            }

            int itemCount = rng.NextInt(4, 7);
            for (int i = 0; i < itemCount; i++)
            {
                deck.AddToTop(cardFactory.Create(CardType.Item, "item"));
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
                    CardInstance monster = cardFactory.Create(CardType.Monster, "monster-l" + pair.Key);
                    monster.ConfigureCombatStats(4 + pair.Key * 2, 1 + pair.Key, pair.Key / 2, 0, 10);
                    monster.SetState("monsterTier", pair.Key);
                    deck.AddToTop(monster);
                }
            }
        }

        private static void AddRoomSpecificCards(DungeonDeck deck, CardFactory cardFactory, RoomPlan plan, IRng rng)
        {
            switch (plan.RoomType)
            {
                case RoomType.Gold:
                    deck.AddToTop(cardFactory.Create(CardType.Gold, "gold"));
                    break;
                case RoomType.Treasure:
                case RoomType.Chest:
                    deck.AddToTop(cardFactory.Create(CardType.Chest, "chest"));
                    break;
                case RoomType.StatUpgrade:
                    deck.AddToTop(cardFactory.Create(CardType.StatUpgrade, "stat"));
                    break;
                case RoomType.Reward:
                    deck.AddToTop(cardFactory.Create(CardType.Chest, "chest"));
                    deck.AddToTop(cardFactory.Create(CardType.StatUpgrade, "stat"));
                    break;
                case RoomType.Shop:
                    int productCount = rng.NextInt(4, 7);
                    for (int i = 0; i < productCount; i++)
                    {
                        deck.AddToTop(cardFactory.Create(CardType.ShopProduct, "shop-product"));
                    }
                    break;
                case RoomType.EliteCombat:
                    CardInstance elite = cardFactory.Create(CardType.Monster, "elite-monster");
                    elite.ConfigureCombatStats(18 + plan.LayerIndex * 3, 5 + plan.LayerIndex, 2, 0, 30);
                    elite.SetState("elite", 1);
                    deck.AddToTop(elite);
                    break;
                case RoomType.Boss:
                case RoomType.BossCombat:
                    CardInstance boss = cardFactory.Create(CardType.Monster, "boss-monster");
                    boss.ConfigureCombatStats(30 + plan.LayerIndex * 5, 7 + plan.LayerIndex, 3, 0, 100);
                    boss.SetState("boss", 1);
                    deck.AddToTop(boss);
                    break;
            }
        }

        private sealed class CardFactory
        {
            private uint _nextId;

            public CardFactory(RoomPlan plan)
            {
                _nextId = (uint)(plan.LayerIndex * 100000 + plan.NodeIndex * 1000 + 1);
            }

            public CardInstance Create(CardType cardType, string modelEntryPrefix)
            {
                uint id = _nextId++;
                CardInstance card = new CardInstance(new CardInstanceId(id), new ModelId("l0", modelEntryPrefix + "-" + id), cardType);
                if (cardType == CardType.Gold)
                {
                    card.ConfigureGoldValue(20);
                }

                return card;
            }
        }
    }
}
