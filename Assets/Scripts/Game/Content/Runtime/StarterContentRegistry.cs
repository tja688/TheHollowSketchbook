using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core;
using Game.Core.Domain;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Progression;
using Game.Core.Domain.Rooms;
using Game.Core.Entities;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;

namespace Game.Content.Runtime
{
    public static class StarterContentRegistry
    {
        internal static readonly ModelId[] CommonRelics =
        {
            StarterContentIds.Relics.LivingFlesh,
            StarterContentIds.Relics.WoodShield,
            StarterContentIds.Relics.WoodSword,
            StarterContentIds.Relics.LawWand,
            StarterContentIds.Relics.EndlessWaterBag
        };

        internal static readonly ModelId[] RareRelics =
        {
            StarterContentIds.Relics.ItemStockpile,
            StarterContentIds.Relics.BloodShield
        };

        internal static readonly ModelId[] LegendaryRelics = Array.Empty<ModelId>();

        internal static readonly ModelId[] ItemPool =
        {
            StarterContentIds.Items.HookRope,
            StarterContentIds.Items.HealingPotion,
            StarterContentIds.Items.ThrowingKnife,
            StarterContentIds.Items.ProtectionSpell,
            StarterContentIds.Items.FlipCard,
            StarterContentIds.Items.LightCard,
            StarterContentIds.Items.ViolenceCard,
            StarterContentIds.Items.FirstStrikeCard
        };

        internal static readonly ModelId[] SummonSkeletonPool =
        {
            StarterContentIds.Monsters.Skeleton,
            StarterContentIds.Monsters.ArmoredSkeleton,
            StarterContentIds.Monsters.BannerSkeleton,
            StarterContentIds.Monsters.RevengeSkeleton,
            StarterContentIds.Monsters.TrackerSkeleton,
            StarterContentIds.Monsters.AmbusherSkeleton,
            StarterContentIds.Monsters.WarSkeleton
        };

        public static RoomContentCatalog RegisterAll()
        {
            foreach (AbstractModel model in CreateModels())
            {
                if (!ModelDb.Contains(model.Id))
                {
                    ModelDb.Register(model);
                }
            }

            return BuildCatalog();
        }

        public static DomainRunFlow CreateDomainRunFlow(RoomContentCatalog catalog = null)
        {
            RoomContentCatalog resolvedCatalog = catalog ?? RegisterAll();
            return new DomainRunFlow(new DungeonMapGenerator(), new DungeonDeckBuilder(resolvedCatalog), new GridDealer());
        }

        public static DomainActionContext StartNewRun(int seed)
        {
            RoomContentCatalog catalog = RegisterAll();
            DomainRunFlow runFlow = CreateDomainRunFlow(catalog);
            DomainActionContext context = runFlow.StartNewRun(seed, StarterContentIds.PlayerHero, 8, 3, 1);
            context.ContentCatalog = catalog;
            List<DomainEvent> bootstrapEvents = new List<DomainEvent>();
            context.AcquireRelic(ModelDb.Get<RelicModel>(StarterContentIds.Relics.VillageGoodSword), bootstrapEvents);
            return context;
        }

        internal static RoomContentCatalog BuildCatalog()
        {
            RoomContentCatalog catalog = new RoomContentCatalog();

            catalog.RegisterMonster(1, StarterContentIds.Monsters.Skeleton);
            catalog.RegisterMonster(2, StarterContentIds.Monsters.ArmoredSkeleton);
            catalog.RegisterMonster(3, StarterContentIds.Monsters.BannerSkeleton);
            catalog.RegisterMonster(3, StarterContentIds.Monsters.RevengeSkeleton);
            catalog.RegisterMonster(4, StarterContentIds.Monsters.TrackerSkeleton);
            catalog.RegisterMonster(4, StarterContentIds.Monsters.AmbusherSkeleton);
            catalog.RegisterMonster(4, StarterContentIds.Monsters.WarSkeleton);
            catalog.Register("boss", StarterContentIds.Monsters.BigSkeletonLord);

            catalog.Register("trap", StarterContentIds.Traps.Crossbow);
            catalog.Register("trap", StarterContentIds.Traps.Spike);
            catalog.Register("trap", StarterContentIds.Traps.Teleport);

            for (int i = 0; i < ItemPool.Length; i++)
            {
                catalog.Register("item", ItemPool[i]);
            }

            catalog.Register("gold", StarterContentIds.RoomCards.Gold);
            catalog.Register("stat", StarterContentIds.RoomCards.StatUpgrade);
            catalog.Register("chest", StarterContentIds.RoomCards.OrdinaryChest);
            catalog.Register("food", StarterContentIds.RoomCards.Food);
            catalog.Register("mentor", StarterContentIds.RoomCards.MentorThornSkin);
            catalog.Register("mentor", StarterContentIds.RoomCards.MentorIronSkin);
            catalog.Register("mentor", StarterContentIds.RoomCards.MentorVeteran);
            catalog.Register("shop-product", StarterContentIds.RoomCards.ShopAttack);
            catalog.Register("shop-product", StarterContentIds.RoomCards.ShopDefense);
            catalog.Register("shop-product", StarterContentIds.RoomCards.ShopMaxHp);
            catalog.Register("shop-product", StarterContentIds.RoomCards.ShopRandomItem);
            catalog.Register("shop-product", StarterContentIds.RoomCards.ShopOrdinaryChest);

            return catalog;
        }

        internal static ModelId GetRouteId(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Combat => StarterContentIds.RouteCombat,
                RoomType.Gold => StarterContentIds.RouteGold,
                RoomType.Chest => StarterContentIds.RouteChest,
                RoomType.StatUpgrade => StarterContentIds.RouteStatUpgrade,
                RoomType.Shop => StarterContentIds.RouteShop,
                RoomType.EliteCombat => StarterContentIds.RouteEliteCombat,
                RoomType.BossCombat => StarterContentIds.RouteBossCombat,
                RoomType.Reward => StarterContentIds.RouteReward,
                RoomType.Restaurant => StarterContentIds.RouteRestaurant,
                _ => new ModelId("route", roomType.ToString().ToLowerInvariant())
            };
        }

        internal static IReadOnlyList<ModelId> GetRelicPool(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => CommonRelics,
                RelicRarity.Rare => RareRelics,
                RelicRarity.Legendary => LegendaryRelics,
                _ => Array.Empty<ModelId>()
            };
        }

        private static IEnumerable<AbstractModel> CreateModels()
        {
            yield return new MarkerTraitModel(StarterContentIds.Traits.Banner);
            yield return new MarkerTraitModel(StarterContentIds.Traits.Revenge);
            yield return new MarkerTraitModel(StarterContentIds.Traits.Aggressive);
            yield return new MarkerTraitModel(StarterContentIds.Traits.Ambush);
            yield return new MarkerTraitModel(StarterContentIds.Traits.ArmorBreak);
            yield return new MarkerTraitModel(StarterContentIds.Traits.Scatter);
            yield return new FirstStrikeMarkerTraitModel();
            yield return new ThornSkinTraitModel();
            yield return new IronSkinTraitModel();
            yield return new VeteranTraitModel();
            yield return new ViolenceTraitModel();

            yield return new SkeletonMonsterModel();
            yield return new ArmoredSkeletonModel();
            yield return new BannerSkeletonModel();
            yield return new RevengeSkeletonModel();
            yield return new TrackerSkeletonModel();
            yield return new AmbusherSkeletonModel();
            yield return new WarSkeletonModel();
            yield return new BigSkeletonLordModel();

            yield return new CrossbowTrapModel();
            yield return new SpikeTrapModel();
            yield return new TeleportTrapModel();

            yield return new HookRopeItemModel();
            yield return new HealingPotionItemModel();
            yield return new ThrowingKnifeItemModel();
            yield return new ProtectionSpellItemModel();
            yield return new FlipCardItemModel();
            yield return new LightCardItemModel();
            yield return new ViolenceCardItemModel();
            yield return new FirstStrikeCardItemModel();

            yield return new PlayerHeroCardModel();
            yield return new GoldCardModel();
            yield return new StatUpgradeCardModel();
            yield return new FoodCardModel();
            yield return new ChestCardModel(StarterContentIds.RoomCards.OrdinaryChest, ChestQuality.Ordinary);
            yield return new ChestCardModel(StarterContentIds.RoomCards.BlueChest, ChestQuality.Blue);
            yield return new ChestCardModel(StarterContentIds.RoomCards.GoldChest, ChestQuality.Gold);
            yield return new MentorCardModel(StarterContentIds.RoomCards.MentorThornSkin, StarterContentIds.Traits.ThornSkin);
            yield return new MentorCardModel(StarterContentIds.RoomCards.MentorIronSkin, StarterContentIds.Traits.IronSkin);
            yield return new MentorCardModel(StarterContentIds.RoomCards.MentorVeteran, StarterContentIds.Traits.Veteran);
            yield return new ShopProductCardModel(StarterContentIds.RoomCards.ShopAttack, ShopProductType.Attack);
            yield return new ShopProductCardModel(StarterContentIds.RoomCards.ShopDefense, ShopProductType.Defense);
            yield return new ShopProductCardModel(StarterContentIds.RoomCards.ShopMaxHp, ShopProductType.MaxHp);
            yield return new ShopProductCardModel(StarterContentIds.RoomCards.ShopRandomItem, ShopProductType.RandomItem);
            yield return new ShopProductCardModel(StarterContentIds.RoomCards.ShopOrdinaryChest, ShopProductType.OrdinaryChest);
            yield return new ActiveRelicPickupCardModel(StarterContentIds.RoomCards.ActivePickupLawWand, StarterContentIds.Relics.LawWand);
            yield return new ActiveRelicPickupCardModel(StarterContentIds.RoomCards.ActivePickupEndlessWaterBag, StarterContentIds.Relics.EndlessWaterBag);
            yield return new ActiveRelicPickupCardModel(StarterContentIds.RoomCards.ActivePickupBloodShield, StarterContentIds.Relics.BloodShield);

            yield return new LivingFleshRelicModel();
            yield return new WoodShieldRelicModel();
            yield return new WoodSwordRelicModel();
            yield return new LawWandRelicModel();
            yield return new EndlessWaterBagRelicModel();
            yield return new ItemStockpileRelicModel();
            yield return new BloodShieldRelicModel();
            yield return new VillageGoodSwordRelicModel();

            foreach (RoomType roomType in Enum.GetValues(typeof(RoomType)).Cast<RoomType>())
            {
                yield return new GenericRouteChoiceModel(GetRouteId(roomType), roomType);
            }

            yield return new PrototypeHeroCharacterModel();
            yield return new PrototypeActModel();
        }
    }

    internal static class StarterContentLogic
    {
        public static string BuildChoiceSessionId(CardInstance sourceCard, string choiceKind)
        {
            return choiceKind + ":" + sourceCard.InstanceId.Value;
        }

        public static string EncodeModelId(ModelId modelId)
        {
            return modelId.ToString();
        }

        public static ModelId DecodeModelId(string value)
        {
            int separator = value.IndexOf(':');
            return separator >= 0
                ? new ModelId(value.Substring(0, separator), value.Substring(separator + 1))
                : default;
        }

        public static CardInstance CreateCardInstance(DomainActionContext domain, ModelId modelId)
        {
            return ModelDb.Get<CardModel>(modelId).CreateInstance(new CardInstanceId(NextInstanceId(domain)));
        }

        public static uint NextInstanceId(DomainActionContext domain)
        {
            uint max = 1u;
            if (domain?.Grid != null)
            {
                foreach (CardInstance card in domain.Grid.AllKnownCards)
                {
                    if (card.InstanceId.Value >= max)
                    {
                        max = card.InstanceId.Value + 1u;
                    }
                }
            }

            if (domain?.DungeonDeck != null)
            {
                for (int i = 0; i < domain.DungeonDeck.Cards.Count; i++)
                {
                    if (domain.DungeonDeck.Cards[i].InstanceId.Value >= max)
                    {
                        max = domain.DungeonDeck.Cards[i].InstanceId.Value + 1u;
                    }
                }
            }

            if (domain?.ItemInventory != null)
            {
                for (int i = 0; i < domain.ItemInventory.Items.Count; i++)
                {
                    if (domain.ItemInventory.Items[i].InstanceId.Value >= max)
                    {
                        max = domain.ItemInventory.Items[i].InstanceId.Value + 1u;
                    }
                }
            }

            return max;
        }

        public static void AddResult(ICollection<DomainEvent> events, GridOperationResult result)
        {
            if (result == null)
            {
                return;
            }

            foreach (DomainEvent domainEvent in result.Events)
            {
                events.Add(domainEvent);
            }
        }

        public static void RemoveCard(DomainActionContext domain, CardInstance card, ICollection<DomainEvent> events, RemoveReason reason)
        {
            if (card == null || card.Zone != CardZone.Grid)
            {
                return;
            }

            AddResult(events, domain.Grid.RemoveCard(card, reason));
        }

        public static void PlaceCardOnGrid(DomainActionContext domain, CardInstance card, ICollection<DomainEvent> events, bool faceUp)
        {
            GridCoord target = FindPlacement(domain.Grid);
            GridOperationResult result = domain.Grid.IsEmpty(target)
                ? domain.Grid.AddCardToGrid(card, target, faceUp)
                : domain.Grid.CoverCellWithCard(card, target, faceUp);
            AddResult(events, result);
        }

        public static void SpawnCard(DomainActionContext domain, ModelId modelId, ICollection<DomainEvent> events, bool faceUp = true)
        {
            PlaceCardOnGrid(domain, CreateCardInstance(domain, modelId), events, faceUp);
        }

        public static GridCoord FindPlacement(GridState grid)
        {
            foreach (GridCoord coord in GridQueries.AllCoordsRowMajor())
            {
                if (coord.CellIndex != 8 && grid.IsEmpty(coord))
                {
                    return coord;
                }
            }

            return GridCoord.FromCellIndex(1);
        }

        public static bool TryMoveOrCover(GridState grid, CardInstance card, GridCoord destination, ICollection<DomainEvent> events, bool faceUp = true)
        {
            if (!destination.IsValid || destination.CellIndex == 8)
            {
                return false;
            }

            GridOperationResult result = grid.IsEmpty(destination)
                ? grid.MoveTopCardToTop(card, destination)
                : grid.CoverCellWithCard(card, destination, faceUp);
            if (!result.Succeeded)
            {
                return false;
            }

            AddResult(events, result);
            return true;
        }

        public static GridCoord Offset(GridCoord coord, GridDirection direction)
        {
            return direction switch
            {
                GridDirection.Up => new GridCoord(coord.Row - 1, coord.Col),
                GridDirection.Down => new GridCoord(coord.Row + 1, coord.Col),
                GridDirection.Left => new GridCoord(coord.Row, coord.Col - 1),
                GridDirection.Right => new GridCoord(coord.Row, coord.Col + 1),
                _ => coord
            };
        }

        public static bool IsAdjacentToPlayer(DomainActionContext domain, GridCoord coord)
        {
            return domain.Grid.PlayerCard != null
                && domain.Grid.PlayerCard.Coord.HasValue
                && GridQueries.OrthogonalNeighbors(domain.Grid.PlayerCard.Coord.Value).Contains(coord);
        }

        public static bool IsPlayerCard(CardInstance card)
        {
            return card != null && card.CardType == CardType.Player;
        }

        public static int CountOtherFaceUpBanners(DomainActionContext domain, CardInstance self)
        {
            return domain.Grid.AllGridCards.Count(card =>
                card.IsFaceUp
                && card.CardType == CardType.Monster
                && card.InstanceId != self.InstanceId
                && card.ModelId == StarterContentIds.Monsters.BannerSkeleton);
        }

        public static async Task ResolveMonsterCombatAsync(CardInteractionContext ctx, int baseAttack)
        {
            int originalAttack = ctx.TargetCard.Attack;
            int buffedAttack = baseAttack + CountOtherFaceUpBanners(ctx.Domain, ctx.TargetCard);
            ctx.TargetCard.SetAttack(buffedAttack);
            try
            {
                await ctx.Domain.Combat.ResolvePlayerVsMonsterAsync(ctx.PlayerCard, ctx.TargetCard, ctx.Events).ConfigureAwait(false);
            }
            finally
            {
                if (ctx.TargetCard.Zone == CardZone.Grid)
                {
                    ctx.TargetCard.SetAttack(originalAttack);
                }
            }
        }

        public static async Task ResolveMonsterCombatAsync(DomainActionContext domain, CardInstance player, CardInstance monster, ICollection<DomainEvent> events, int baseAttack)
        {
            int originalAttack = monster.Attack;
            int buffedAttack = baseAttack + CountOtherFaceUpBanners(domain, monster);
            monster.SetAttack(buffedAttack);
            try
            {
                await domain.Combat.ResolvePlayerVsMonsterAsync(player, monster, events).ConfigureAwait(false);
            }
            finally
            {
                if (monster.Zone == CardZone.Grid)
                {
                    monster.SetAttack(originalAttack);
                }
            }
        }

        public static bool TryStoreGeneratedItem(DomainActionContext domain, ModelId itemId, ICollection<DomainEvent> events, string reason)
        {
            if (!domain.ItemInventory.HasSpace)
            {
                return false;
            }

            CardInstance itemCard = CreateCardInstance(domain, itemId);
            InventorySlot slot = domain.ItemInventory.Store(itemCard);
            events.Add(new DomainEvent(DomainEventType.ItemStored)
            {
                CardId = itemCard.InstanceId,
                Amount = slot.Index,
                Reason = reason
            });
            return true;
        }

        public static ModelId PickRandomItem(DomainActionContext domain)
        {
            IRng rng = domain.Rng ?? new DeterministicRng(1);
            return StarterContentRegistry.ItemPool[rng.NextInt(0, StarterContentRegistry.ItemPool.Length)];
        }

        public static void RemoveOtherMentors(DomainActionContext domain, CardInstance selected, ICollection<DomainEvent> events)
        {
            List<CardInstance> mentors = domain.Grid.AllGridCards
                .Where(card => card.CardType == CardType.Mentor && card.InstanceId != selected.InstanceId)
                .ToList();

            for (int i = 0; i < mentors.Count; i++)
            {
                RemoveCard(domain, mentors[i], events, RemoveReason.Collected);
            }
        }

        public static IReadOnlyList<string> BuildRelicChoices(DomainActionContext domain, ChestQuality quality)
        {
            List<ModelId> chosen = new List<ModelId>(3);
            HashSet<ModelId> used = new HashSet<ModelId>();
            for (int i = 0; i < 3; i++)
            {
                RelicRarity rarity = RollRelicRarity(domain.Rng ?? new DeterministicRng(1), quality);
                ModelId picked = PickRelicFromPool(domain, rarity, used);
                used.Add(picked);
                chosen.Add(picked);
            }

            return chosen.Select(EncodeModelId).ToArray();
        }

        public static void ResolveRelicChoice(ChoiceResolutionContext ctx)
        {
            ModelId relicId = DecodeModelId(ctx.SelectedOptionKey);
            if (relicId.IsEmpty || !ModelDb.TryGet(relicId, out RelicModel relic))
            {
                return;
            }

            ModelId replacedActive = ctx.Domain.AcquireRelic(relic, ctx.Events);
            SpawnReplacedActiveRelic(ctx.Domain, replacedActive, ctx.Events);
            RemoveCard(ctx.Domain, ctx.SourceCard, ctx.Events, RemoveReason.Collected);
        }

        public static void SpawnReplacedActiveRelic(DomainActionContext domain, ModelId relicId, ICollection<DomainEvent> events)
        {
            if (relicId.IsEmpty)
            {
                return;
            }

            ModelId pickupId = relicId == StarterContentIds.Relics.LawWand
                ? StarterContentIds.RoomCards.ActivePickupLawWand
                : relicId == StarterContentIds.Relics.EndlessWaterBag
                    ? StarterContentIds.RoomCards.ActivePickupEndlessWaterBag
                    : relicId == StarterContentIds.Relics.BloodShield
                        ? StarterContentIds.RoomCards.ActivePickupBloodShield
                        : default;

            if (!pickupId.IsEmpty)
            {
                SpawnCard(domain, pickupId, events);
            }
        }

        private static ModelId PickRelicFromPool(DomainActionContext domain, RelicRarity rarity, HashSet<ModelId> used)
        {
            List<ModelId> available = StarterContentRegistry.GetRelicPool(rarity)
                .Where(id => !used.Contains(id))
                .Where(id => !domain.Relics.Contains(id))
                .Where(id => ModelDb.Get<RelicModel>(id).CanAppearInRelicPool)
                .ToList();

            if (available.Count == 0)
            {
                available = StarterContentRegistry.CommonRelics
                    .Concat(StarterContentRegistry.RareRelics)
                    .Concat(StarterContentRegistry.LegendaryRelics)
                    .Where(id => !used.Contains(id))
                    .Where(id => !domain.Relics.Contains(id))
                    .Where(id => ModelDb.Get<RelicModel>(id).CanAppearInRelicPool)
                    .ToList();
            }

            IRng rng = domain.Rng ?? new DeterministicRng(1);
            return available.Count > 0 ? available[rng.NextInt(0, available.Count)] : StarterContentIds.Relics.WoodSword;
        }

        private static RelicRarity RollRelicRarity(IRng rng, ChestQuality quality)
        {
            int roll = rng.NextInt(0, 100);
            return quality switch
            {
                ChestQuality.Ordinary when roll < 65 => RelicRarity.Common,
                ChestQuality.Ordinary when roll < 95 => RelicRarity.Rare,
                ChestQuality.Ordinary => RelicRarity.Legendary,
                ChestQuality.Blue when roll < 30 => RelicRarity.Common,
                ChestQuality.Blue when roll < 80 => RelicRarity.Rare,
                ChestQuality.Blue => RelicRarity.Legendary,
                ChestQuality.Gold when roll < 50 => RelicRarity.Rare,
                _ => RelicRarity.Legendary
            };
        }
    }

    internal sealed class PrototypeHeroCharacterModel : CharacterModel
    {
        public override ModelId Id => new ModelId("Character", "PrototypeHero");
        public override string Name => "兵大哥";
        public override int StartingMaxHp => 8;
    }

    internal sealed class PrototypeActModel : ActModel
    {
        public override ModelId Id => new ModelId("Act", "PrototypeAct");
        public override string Name => "Prototype Act";
    }

    internal enum ChestQuality
    {
        Ordinary,
        Blue,
        Gold
    }

    internal enum ShopProductType
    {
        Attack,
        Defense,
        MaxHp,
        RandomItem,
        OrdinaryChest
    }
}
