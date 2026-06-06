using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Progression;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;

namespace Game.Content.Runtime
{
    internal sealed class MarkerTraitModel : TraitModel
    {
        private readonly ModelId _id;

        public MarkerTraitModel(ModelId id)
        {
            _id = id;
        }

        public override ModelId Id => _id;
    }

    internal sealed class FirstStrikeMarkerTraitModel : TraitModel
    {
        public override ModelId Id => StarterContentIds.Traits.FirstStrike;
    }

    internal sealed class ThornSkinTraitModel : TraitModel
    {
        public override ModelId Id => StarterContentIds.Traits.ThornSkin;

        public override async Task OnAfterDamageAsync(DamageContext ctx, DamageResult result)
        {
            if (ctx.TargetCard == null
                || ctx.TargetCard.CardType != CardType.Player
                || ctx.SourceCard == null
                || ctx.SourceCard.CardType == CardType.Player
                || result.HpLoss <= 0
                || ctx.Events == null)
            {
                return;
            }

            DamageInfo reflected = new DamageInfo(
                DamageSource.FromCard(ctx.TargetCard.InstanceId),
                DamageTarget.Card(ctx.SourceCard.InstanceId),
                ctx.SourceCard.Attack,
                DamageKind.Environment,
                true,
                "ThornSkin")
            {
                CanTriggerThorns = false
            };

            await ctx.Domain.Combat.ApplyDamageAsync(reflected, ctx.Events).ConfigureAwait(false);
            ctx.Domain.ResolveDeadCards(ctx.Events);
        }
    }

    internal sealed class IronSkinTraitModel : TraitModel
    {
        public override ModelId Id => StarterContentIds.Traits.IronSkin;

        public override Task OnRoomClearedAsync(RoomLifecycleContext ctx)
        {
            ctx.Domain.HealPlayer(10, ctx.Events, "trait:iron-skin");
            return Task.CompletedTask;
        }
    }

    internal sealed class VeteranTraitModel : TraitModel
    {
        public override ModelId Id => StarterContentIds.Traits.Veteran;

        public override Task OnPlayerActionCommittedAsync(PlayerActionContext ctx)
        {
            if (ctx.SourceIntent is not InteractWithCardIntent interactIntent)
            {
                return Task.CompletedTask;
            }

            if (!ctx.Domain.Grid.TryGetCard(interactIntent.Target, out CardInstance targetCard) || targetCard.CardType != CardType.Monster)
            {
                return Task.CompletedTask;
            }

            int lastTarget = ctx.Domain.PlayerRunState.GetKeyword("veteranTarget");
            int currentTarget = (int)interactIntent.Target.Value;
            int nextBonus = lastTarget == currentTarget
                ? ctx.Domain.PlayerRunState.GetKeyword("veteranBonus") + 1
                : 1;

            ctx.Domain.SetPlayerKeyword("veteranTarget", currentTarget, StatModifierScope.Permanent);
            ctx.Domain.SetPlayerKeyword("veteranBonus", nextBonus, StatModifierScope.Permanent);
            ctx.Domain.RemovePlayerModifiersBySource("trait:veteran", ctx.Events, "player:attack");
            ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, nextBonus, "trait:veteran"), ctx.Events, "player:attack");
            return Task.CompletedTask;
        }
    }

    internal sealed class ViolenceTraitModel : TraitModel
    {
        public override ModelId Id => StarterContentIds.Traits.Violence;

        public override Task OnPlayerActionCommittedAsync(PlayerActionContext ctx)
        {
            if (ctx.Domain.PlayerRunState.GetKeyword("violencePending") <= 0)
            {
                return Task.CompletedTask;
            }

            if (ctx.SourceIntent is not InteractWithCardIntent interactIntent)
            {
                return Task.CompletedTask;
            }

            if (!ctx.Domain.Grid.TryGetCard(interactIntent.Target, out CardInstance targetCard) || targetCard.CardType != CardType.Monster)
            {
                return Task.CompletedTask;
            }

            ctx.Domain.RemovePlayerModifiersBySource("item:violence", ctx.Events, "player:attack");
            ctx.Domain.RemovePlayerTraitsBySource("item:violence");
            ctx.Domain.RemovePlayerKeyword("violencePending", StatModifierScope.Room);
            return Task.CompletedTask;
        }
    }

    internal abstract class StarterMonsterCardModel : MonsterCardModel
    {
        private readonly ModelId _id;
        private readonly int _level;
        private readonly int _maxHp;
        private readonly int _attack;
        private readonly int _defense;
        private readonly IReadOnlyList<ModelId> _traitIds;

        protected StarterMonsterCardModel(ModelId id, int level, int maxHp, int attack, int defense, params ModelId[] traitIds)
        {
            _id = id;
            _level = level;
            _maxHp = maxHp;
            _attack = attack;
            _defense = defense;
            _traitIds = traitIds ?? new ModelId[0];
        }

        public override ModelId Id => _id;
        public override int Level => _level;
        public override int MaxHp => _maxHp;
        public override int Attack => _attack;
        public override int Defense => _defense;
        public override IReadOnlyList<ModelId> TraitIds => _traitIds;

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            return StarterContentLogic.ResolveMonsterCombatAsync(ctx, Attack);
        }

        public override Task OnDestroyedAsync(CardDestroyedContext ctx)
        {
            List<CardInstance> revengeSkeletons = ctx.Domain.Grid.AllGridCards
                .Where(card => card.CardType == CardType.Monster
                    && card.InstanceId != ctx.Card.InstanceId
                    && card.ModelId == StarterContentIds.Monsters.RevengeSkeleton
                    && card.IsFaceUp)
                .ToList();

            for (int i = 0; i < revengeSkeletons.Count; i++)
            {
                revengeSkeletons[i].SetAttack(revengeSkeletons[i].Attack + 2);
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class SkeletonMonsterModel : StarterMonsterCardModel
    {
        public SkeletonMonsterModel() : base(StarterContentIds.Monsters.Skeleton, 1, 6, 2, 0) { }
    }

    internal sealed class ArmoredSkeletonModel : StarterMonsterCardModel
    {
        public ArmoredSkeletonModel() : base(StarterContentIds.Monsters.ArmoredSkeleton, 2, 8, 3, 1) { }
    }

    internal sealed class BannerSkeletonModel : StarterMonsterCardModel
    {
        public BannerSkeletonModel() : base(StarterContentIds.Monsters.BannerSkeleton, 3, 8, 3, 2, StarterContentIds.Traits.Banner) { }
    }

    internal sealed class RevengeSkeletonModel : StarterMonsterCardModel
    {
        public RevengeSkeletonModel() : base(StarterContentIds.Monsters.RevengeSkeleton, 3, 7, 4, 0, StarterContentIds.Traits.Revenge) { }

        public override Task OnAfterPlayerActionCommittedAsync(PlayerActionContext ctx)
        {
            int defeatedCount = ctx.Events.Count(e => e.EventType == DomainEventType.MonsterDefeated && e.CardId != ctx.ObservedCard.InstanceId);
            if (defeatedCount > 0)
            {
                ctx.ObservedCard.SetAttack(ctx.ObservedCard.Attack + (2 * defeatedCount));
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class TrackerSkeletonModel : StarterMonsterCardModel
    {
        public TrackerSkeletonModel() : base(StarterContentIds.Monsters.TrackerSkeleton, 4, 10, 5, 1, StarterContentIds.Traits.Aggressive) { }

        public override async Task OnAfterPlayerActionCommittedAsync(PlayerActionContext ctx)
        {
            int counter = ctx.ObservedCard.GetState("trackerCounter") + 1;
            if (counter < 3)
            {
                ctx.ObservedCard.SetState("trackerCounter", counter);
                return;
            }

            ctx.ObservedCard.SetState("trackerCounter", 0);
            if (!ctx.ObservedCard.Coord.HasValue || !ctx.PlayerCard.Coord.HasValue)
            {
                return;
            }

            GridCoord from = ctx.ObservedCard.Coord.Value;
            GridCoord playerCoord = ctx.PlayerCard.Coord.Value;
            if (GridQueries.OrthogonalNeighbors(from).Contains(playerCoord))
            {
                await StarterContentLogic.ResolveMonsterCombatAsync(ctx.Domain, ctx.PlayerCard, ctx.ObservedCard, ctx.Events, Attack).ConfigureAwait(false);
                return;
            }

            foreach (GridCoord destination in PreferredStepsToward(from, playerCoord))
            {
                if (!destination.IsValid || destination.CellIndex == 8 || !ctx.Domain.Grid.IsEmpty(destination))
                {
                    continue;
                }

                StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.MoveTopCardToTop(ctx.ObservedCard, destination));
                if (GridQueries.OrthogonalNeighbors(destination).Contains(playerCoord))
                {
                    await StarterContentLogic.ResolveMonsterCombatAsync(ctx.Domain, ctx.PlayerCard, ctx.ObservedCard, ctx.Events, Attack).ConfigureAwait(false);
                }

                break;
            }
        }

        private static IEnumerable<GridCoord> PreferredStepsToward(GridCoord from, GridCoord to)
        {
            int rowStep = to.Row.CompareTo(from.Row);
            if (rowStep != 0)
            {
                yield return new GridCoord(from.Row + rowStep, from.Col);
            }

            int colStep = to.Col.CompareTo(from.Col);
            if (colStep != 0)
            {
                yield return new GridCoord(from.Row, from.Col + colStep);
            }
        }
    }

    internal sealed class AmbusherSkeletonModel : StarterMonsterCardModel
    {
        public AmbusherSkeletonModel() : base(StarterContentIds.Monsters.AmbusherSkeleton, 4, 8, 4, 1, StarterContentIds.Traits.Ambush) { }

        public override async Task OnRevealedAsync(CardRevealContext ctx)
        {
            ctx.Card.SetState("firstStrike", 1);
            if (ctx.Card.Coord.HasValue && StarterContentLogic.IsAdjacentToPlayer(ctx.Domain, ctx.Card.Coord.Value))
            {
                await StarterContentLogic.ResolveMonsterCombatAsync(ctx.Domain, ctx.Domain.Grid.PlayerCard, ctx.Card, ctx.Events, Attack).ConfigureAwait(false);
            }
        }
    }

    internal sealed class WarSkeletonModel : StarterMonsterCardModel
    {
        public WarSkeletonModel() : base(StarterContentIds.Monsters.WarSkeleton, 4, 10, 4, 3, StarterContentIds.Traits.ArmorBreak) { }

        public override async Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            int beforeCount = ctx.Events.Count;
            await StarterContentLogic.ResolveMonsterCombatAsync(ctx, Attack).ConfigureAwait(false);
            bool hitPlayer = ctx.Events.Skip(beforeCount).Any(e =>
                e.EventType == DomainEventType.DamageApplied
                && e.SourceCardId == ctx.TargetCard.InstanceId
                && e.TargetCardId == ctx.PlayerCard.InstanceId);

            if (hitPlayer)
            {
                ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Defense, StatModifierScope.Room, -1, "trait:armor-break"), ctx.Events, "player:defense");
            }
        }
    }

    internal sealed class BigSkeletonLordModel : StarterMonsterCardModel
    {
        public BigSkeletonLordModel() : base(StarterContentIds.Monsters.BigSkeletonLord, 9, 50, 4, 3, StarterContentIds.Traits.Scatter) { }

        protected override void ConfigureCreatedInstance(CardInstance instance)
        {
            base.ConfigureCreatedInstance(instance);
            instance.SetState("boss", 1);
        }

        public override Task OnAfterDamageAsync(DamageContext ctx, DamageResult result)
        {
            if (ctx.TargetCard == null || ctx.TargetCard.ModelId != Id || result.HpLoss <= 0 || !ctx.TargetCard.Coord.HasValue)
            {
                return Task.CompletedTask;
            }

            int beforeHp = ctx.TargetCard.CurrentHp + result.HpLoss;
            int afterHp = ctx.TargetCard.CurrentHp;
            int crossed = ((beforeHp - 1) / 10) - ((afterHp - 1) / 10);
            if (afterHp == 0)
            {
                crossed = ((beforeHp - 1) / 10);
            }

            if (crossed <= 0)
            {
                return Task.CompletedTask;
            }

            List<GridCoord> candidates = GridQueries.OrthogonalNeighbors(ctx.TargetCard.Coord.Value)
                .Where(coord => coord.CellIndex != 8)
                .ToList();
            IRng rng = ctx.Domain.Rng ?? new DeterministicRng(1);
            for (int i = 0; i < crossed && candidates.Count > 0; i++)
            {
                GridCoord coord = candidates[rng.NextInt(0, candidates.Count)];
                CardInstance summon = StarterContentLogic.CreateCardInstance(ctx.Domain, StarterContentRegistry.SummonSkeletonPool[rng.NextInt(0, StarterContentRegistry.SummonSkeletonPool.Length)]);
                GridOperationResult resultOp = ctx.Domain.Grid.IsEmpty(coord)
                    ? ctx.Domain.Grid.AddCardToGrid(summon, coord, true)
                    : ctx.Domain.Grid.CoverCellWithCard(summon, coord, true);
                StarterContentLogic.AddResult(ctx.Events, resultOp);
            }

            return Task.CompletedTask;
        }

        public override Task OnDestroyedAsync(CardDestroyedContext ctx)
        {
            StarterContentLogic.SpawnCard(ctx.Domain, StarterContentIds.RoomCards.GoldChest, ctx.Events);
            StarterContentLogic.SpawnCard(ctx.Domain, StarterContentIds.RoomCards.Gold, ctx.Events);
            StarterContentLogic.SpawnCard(ctx.Domain, StarterContentIds.RoomCards.Gold, ctx.Events);
            StarterContentLogic.SpawnCard(ctx.Domain, StarterContentIds.RoomCards.StatUpgrade, ctx.Events);
            return Task.CompletedTask;
        }
    }

    internal sealed class CrossbowTrapModel : TrapCardModel
    {
        public override ModelId Id => StarterContentIds.Traps.Crossbow;
        public override int MaxHp => 2;

        public override async Task OnDestroyedAsync(TrapContext ctx)
        {
            if (!ctx.TrapCard.Coord.HasValue)
            {
                return;
            }

            GridCoord trapCoord = ctx.TrapCard.Coord.Value;
            for (int row = trapCoord.Row - 1; row >= 0; row--)
            {
                GridCoord coord = new GridCoord(row, trapCoord.Col);
                CardInstance topCard = ctx.Domain.Grid.GetTopCard(coord);
                if (topCard == null || !topCard.IsFaceUp)
                {
                    continue;
                }

                DamageInfo damage = new DamageInfo(
                    DamageSource.FromCard(ctx.TrapCard.InstanceId),
                    DamageTarget.Card(topCard.InstanceId),
                    6,
                    DamageKind.Trap,
                    true,
                    "CrossbowTrap");
                await ctx.ApplyDamageAsync(damage).ConfigureAwait(false);
            }

            ctx.Domain.ResolveDeadCards(ctx.Events);
        }
    }

    internal sealed class SpikeTrapModel : TrapCardModel
    {
        public override ModelId Id => StarterContentIds.Traps.Spike;
        public override int MaxHp => 4;
        public override int ContactDamageToPlayer => 2;

        public override Task OnRevealedAsync(TrapContext ctx)
        {
            ctx.Domain.PendingTriggers.Enqueue(new PendingTrigger(ctx.TrapCard.InstanceId, PendingTriggerTiming.AfterPlayerAction, ctx.ActionIndex + 1, "spike"));
            return Task.CompletedTask;
        }

        public override async Task OnPendingTriggerAsync(PendingTriggerContext ctx)
        {
            if (!ctx.Card.Coord.HasValue)
            {
                return;
            }

            IReadOnlyList<GridCoord> neighbors = GridQueries.OrthogonalNeighbors(ctx.Card.Coord.Value);
            for (int i = 0; i < neighbors.Count; i++)
            {
                CardInstance target = ctx.Domain.Grid.GetTopCard(neighbors[i]);
                if (target == null || !target.IsFaceUp)
                {
                    continue;
                }

                await ctx.ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(ctx.Card.InstanceId),
                    DamageTarget.Card(target.InstanceId),
                    6,
                    DamageKind.Trap,
                    true,
                    "SpikeTrap")).ConfigureAwait(false);
            }

            ctx.Domain.ResolveDeadCards(ctx.Events);
        }
    }

    internal sealed class TeleportTrapModel : TrapCardModel
    {
        public override ModelId Id => StarterContentIds.Traps.Teleport;
        public override int MaxHp => 1;

        public override Task OnDestroyedAsync(TrapContext ctx)
        {
            ctx.Domain.DungeonDeck ??= new Game.Core.Domain.Deck.DungeonDeck();
            IRng rng = ctx.Domain.Rng ?? new DeterministicRng(1);
            StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.ShuffleNonPlayerGridCardsIntoDeck(ctx.Domain.DungeonDeck, rng));

            List<GridCoord> empty = GridQueries.AllCoordsRowMajor().Where(coord => coord.CellIndex != 8).ToList();
            GridCoord playerTarget = empty[rng.NextInt(0, empty.Count)];
            StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.MoveCardToEmptyCell(ctx.PlayerCard, playerTarget));
            StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.RedistributeDeck(ctx.Domain.DungeonDeck, playerTarget, rng));
            StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.RevealAround(playerTarget, FlipReason.Scripted));
            return Task.CompletedTask;
        }
    }

    internal sealed class HookRopeItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.HookRope;
        public override ItemTargetMode TargetMode => ItemTargetMode.CardThenDirection;

        public override bool CanUse(ItemUseContext ctx)
        {
            UseItemIntent intent = (UseItemIntent)ctx.SourceIntent;
            if (!ctx.Domain.Grid.TryGetCard(intent.Target.PrimaryCard, out CardInstance target) || StarterContentLogic.IsPlayerCard(target) || !target.Coord.HasValue)
            {
                return false;
            }

            GridCoord destination = StarterContentLogic.Offset(target.Coord.Value, intent.Target.Direction);
            return destination.IsValid && destination.CellIndex != 8;
        }

        public override Task UseAsync(ItemUseContext ctx)
        {
            UseItemIntent intent = (UseItemIntent)ctx.SourceIntent;
            CardInstance target = ctx.Domain.Grid.GetCard(intent.Target.PrimaryCard);
            GridCoord destination = StarterContentLogic.Offset(target.Coord.Value, intent.Target.Direction);
            StarterContentLogic.TryMoveOrCover(ctx.Domain.Grid, target, destination, ctx.Events, target.IsFaceUp);
            return Task.CompletedTask;
        }
    }

    internal sealed class HealingPotionItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.HealingPotion;
        public override ItemTargetMode TargetMode => ItemTargetMode.Player;

        public override Task UseAsync(ItemUseContext ctx)
        {
            ctx.Domain.HealPlayer(10, ctx.Events, "item:healing-potion");
            return Task.CompletedTask;
        }
    }

    internal sealed class ThrowingKnifeItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.ThrowingKnife;
        public override ItemTargetMode TargetMode => ItemTargetMode.AnyCard;

        public override Task UseAsync(ItemUseContext ctx)
        {
            UseItemIntent intent = (UseItemIntent)ctx.SourceIntent;
            return ctx.ApplyDamageAsync(new DamageInfo(
                DamageSource.FromCard(ctx.ItemCard.InstanceId),
                DamageTarget.Card(intent.Target.PrimaryCard),
                6,
                DamageKind.Item,
                false,
                "ThrowingKnife"));
        }
    }

    internal sealed class ProtectionSpellItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.ProtectionSpell;
        public override ItemTargetMode TargetMode => ItemTargetMode.Player;

        public override Task UseAsync(ItemUseContext ctx)
        {
            ctx.PlayerCard.SetState("damageImmunity", ctx.PlayerCard.GetState("damageImmunity") + 1);
            return Task.CompletedTask;
        }
    }

    internal sealed class FlipCardItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.FlipCard;
        public override ItemTargetMode TargetMode => ItemTargetMode.TwoCards;

        public override bool CanUse(ItemUseContext ctx)
        {
            UseItemIntent intent = (UseItemIntent)ctx.SourceIntent;
            if (!ctx.Domain.Grid.TryGetCard(intent.Target.PrimaryCard, out CardInstance first)
                || !ctx.Domain.Grid.TryGetCard(intent.Target.SecondaryCard, out CardInstance second)
                || !first.Coord.HasValue
                || !second.Coord.HasValue)
            {
                return false;
            }

            return first.IsFaceUp && !StarterContentLogic.IsPlayerCard(first) && !second.IsFaceUp;
        }

        public override Task UseAsync(ItemUseContext ctx)
        {
            UseItemIntent intent = (UseItemIntent)ctx.SourceIntent;
            CardInstance first = ctx.Domain.Grid.GetCard(intent.Target.PrimaryCard);
            CardInstance second = ctx.Domain.Grid.GetCard(intent.Target.SecondaryCard);
            StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.SwapTopCards(first, second));

            if (second.Coord.HasValue && StarterContentLogic.IsAdjacentToPlayer(ctx.Domain, second.Coord.Value))
            {
                StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.FlipCard(second, FlipReason.Scripted));
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class LightCardItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.LightCard;
        public override ItemTargetMode TargetMode => ItemTargetMode.GridCell;

        public override Task UseAsync(ItemUseContext ctx)
        {
            UseItemIntent intent = (UseItemIntent)ctx.SourceIntent;
            StarterContentLogic.AddResult(ctx.Events, ctx.Domain.Grid.RevealAround(intent.Target.GridCell, FlipReason.Scripted));
            return Task.CompletedTask;
        }
    }

    internal sealed class ViolenceCardItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.ViolenceCard;
        public override ItemTargetMode TargetMode => ItemTargetMode.Player;

        public override Task UseAsync(ItemUseContext ctx)
        {
            ctx.Domain.RemovePlayerModifiersBySource("item:violence", ctx.Events, "player:attack");
            ctx.Domain.RemovePlayerTraitsBySource("item:violence");
            ctx.Domain.SetPlayerKeyword("violencePending", 1, StatModifierScope.Room);
            ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Room, ctx.PlayerCard.Attack, "item:violence"), ctx.Events, "player:attack");
            ctx.Domain.AddPlayerTrait(StarterContentIds.Traits.Violence, StatModifierScope.Room, "item:violence", ctx.Events);
            return Task.CompletedTask;
        }
    }

    internal sealed class FirstStrikeCardItemModel : ItemCardModel
    {
        public override ModelId Id => StarterContentIds.Items.FirstStrikeCard;
        public override ItemTargetMode TargetMode => ItemTargetMode.Player;

        public override Task UseAsync(ItemUseContext ctx)
        {
            ctx.Domain.SetPlayerKeyword("firstStrike", 1, StatModifierScope.Room);
            return Task.CompletedTask;
        }
    }

    internal sealed class GoldCardModel : CardModel
    {
        public override ModelId Id => StarterContentIds.RoomCards.Gold;
        public override CardType CardType => CardType.Gold;

        protected override void ConfigureCreatedInstance(CardInstance instance)
        {
            instance.ConfigureGoldValue(50);
        }

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            ctx.Domain.GainGold(50, ctx.Events, "GoldCard");
            StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
            return Task.CompletedTask;
        }
    }

    internal sealed class PlayerHeroCardModel : CardModel
    {
        public override ModelId Id => StarterContentIds.PlayerHero;
        public override CardType CardType => CardType.Player;

        protected override void ConfigureCreatedInstance(CardInstance instance)
        {
            instance.ConfigureCombatStats(8, 3, 1, 0, 0);
        }
    }

    internal sealed class FoodCardModel : CardModel
    {
        public override ModelId Id => StarterContentIds.RoomCards.Food;
        public override CardType CardType => CardType.Food;

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            ctx.Domain.RestorePlayerToFull(ctx.Events, "FoodCard");
            StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
            return Task.CompletedTask;
        }
    }

    internal sealed class StatUpgradeCardModel : CardModel
    {
        public override ModelId Id => StarterContentIds.RoomCards.StatUpgrade;
        public override CardType CardType => CardType.StatUpgrade;

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            ctx.Domain.OpenChoiceSession(
                StarterContentLogic.BuildChoiceSessionId(ctx.TargetCard, "stat-upgrade"),
                ctx.TargetCard,
                "StatUpgrade",
                new[] { "attack", "defense", "max-hp" },
                ctx.Events);
            return Task.CompletedTask;
        }

        public override Task OnChoiceResolvedAsync(ChoiceResolutionContext ctx)
        {
            switch (ctx.SelectedOptionKey)
            {
                case "attack":
                    ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, 1, "room:stat-upgrade"), ctx.Events, "player:attack");
                    break;
                case "defense":
                    ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Defense, StatModifierScope.Permanent, 1, "room:stat-upgrade"), ctx.Events, "player:defense");
                    break;
                default:
                    ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.MaxHp, StatModifierScope.Permanent, 2, "room:stat-upgrade"), ctx.Events, "player:max-hp");
                    ctx.Domain.HealPlayer(2, ctx.Events, "player:max-hp");
                    break;
            }

            StarterContentLogic.RemoveCard(ctx.Domain, ctx.SourceCard, ctx.Events, RemoveReason.Collected);
            return Task.CompletedTask;
        }
    }

    internal sealed class ChestCardModel : CardModel
    {
        private readonly ModelId _id;
        private readonly ChestQuality _quality;

        public ChestCardModel(ModelId id, ChestQuality quality)
        {
            _id = id;
            _quality = quality;
        }

        public override ModelId Id => _id;
        public override CardType CardType => CardType.Chest;

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            ctx.Domain.OpenChoiceSession(
                StarterContentLogic.BuildChoiceSessionId(ctx.TargetCard, "relic-choice"),
                ctx.TargetCard,
                "RelicChoice",
                StarterContentLogic.BuildRelicChoices(ctx.Domain, _quality),
                ctx.Events);
            return Task.CompletedTask;
        }

        public override Task OnChoiceResolvedAsync(ChoiceResolutionContext ctx)
        {
            StarterContentLogic.ResolveRelicChoice(ctx);
            return Task.CompletedTask;
        }
    }

    internal sealed class MentorCardModel : CardModel
    {
        private readonly ModelId _id;
        private readonly ModelId _traitId;

        public MentorCardModel(ModelId id, ModelId traitId)
        {
            _id = id;
            _traitId = traitId;
        }

        public override ModelId Id => _id;
        public override CardType CardType => CardType.Mentor;

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            ctx.Domain.AddPlayerTrait(_traitId, StatModifierScope.Permanent, "mentor:" + _traitId.Entry, ctx.Events);
            if (_traitId == StarterContentIds.Traits.IronSkin)
            {
                ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.MaxHp, StatModifierScope.Permanent, 10, "mentor:iron-skin"), ctx.Events, "player:max-hp");
                ctx.Domain.HealPlayer(10, ctx.Events, "mentor:iron-skin");
            }

            StarterContentLogic.RemoveOtherMentors(ctx.Domain, ctx.TargetCard, ctx.Events);
            StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
            return Task.CompletedTask;
        }
    }

    internal sealed class ShopProductCardModel : CardModel
    {
        private readonly ModelId _id;
        private readonly ShopProductType _productType;

        public ShopProductCardModel(ModelId id, ShopProductType productType)
        {
            _id = id;
            _productType = productType;
        }

        public override ModelId Id => _id;
        public override CardType CardType => CardType.ShopProduct;

        public override bool CanInteractWithPlayer(CardInteractionContext ctx)
        {
            int cost = GetCost();
            if (ctx.Domain.PlayerGold < cost)
            {
                return false;
            }

            return _productType != ShopProductType.RandomItem || ctx.Domain.ItemInventory.HasSpace;
        }

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            if (!ctx.Domain.TrySpendGold(GetCost(), ctx.Events, "ShopPurchase"))
            {
                return Task.CompletedTask;
            }

            switch (_productType)
            {
                case ShopProductType.Attack:
                    ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, 1, "shop:attack"), ctx.Events, "player:attack");
                    StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
                    break;
                case ShopProductType.Defense:
                    ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Defense, StatModifierScope.Permanent, 1, "shop:defense"), ctx.Events, "player:defense");
                    StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
                    break;
                case ShopProductType.MaxHp:
                    ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.MaxHp, StatModifierScope.Permanent, 2, "shop:max-hp"), ctx.Events, "player:max-hp");
                    ctx.Domain.HealPlayer(2, ctx.Events, "shop:max-hp");
                    StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
                    break;
                case ShopProductType.RandomItem:
                    if (StarterContentLogic.TryStoreGeneratedItem(ctx.Domain, StarterContentLogic.PickRandomItem(ctx.Domain), ctx.Events, "ShopRandomItem"))
                    {
                        StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
                    }
                    break;
                case ShopProductType.OrdinaryChest:
                    ctx.Domain.OpenChoiceSession(
                        StarterContentLogic.BuildChoiceSessionId(ctx.TargetCard, "shop-chest"),
                        ctx.TargetCard,
                        "RelicChoice",
                        StarterContentLogic.BuildRelicChoices(ctx.Domain, ChestQuality.Ordinary),
                        ctx.Events);
                    break;
            }

            return Task.CompletedTask;
        }

        public override Task OnChoiceResolvedAsync(ChoiceResolutionContext ctx)
        {
            StarterContentLogic.ResolveRelicChoice(ctx);
            return Task.CompletedTask;
        }

        private int GetCost()
        {
            return _productType switch
            {
                ShopProductType.RandomItem => 30,
                ShopProductType.OrdinaryChest => 160,
                _ => 80
            };
        }
    }

    internal sealed class ActiveRelicPickupCardModel : CardModel
    {
        private readonly ModelId _id;
        private readonly ModelId _relicId;

        public ActiveRelicPickupCardModel(ModelId id, ModelId relicId)
        {
            _id = id;
            _relicId = relicId;
        }

        public override ModelId Id => _id;
        public override CardType CardType => CardType.Special;

        public override Task OnPlayerInteractAsync(CardInteractionContext ctx)
        {
            RelicModel relic = ModelDb.Get<RelicModel>(_relicId);
            ModelId replaced = ctx.Domain.AcquireRelic(relic, ctx.Events);
            StarterContentLogic.RemoveCard(ctx.Domain, ctx.TargetCard, ctx.Events, RemoveReason.Collected);
            StarterContentLogic.SpawnReplacedActiveRelic(ctx.Domain, replaced, ctx.Events);
            return Task.CompletedTask;
        }
    }

    internal abstract class StarterRelicModel : RelicModel
    {
        private readonly ModelId _id;
        private readonly RelicRarity _rarity;
        private readonly RelicKind _kind;
        private readonly int _attackBonus;
        private readonly int _defenseBonus;
        private readonly int _maxHpBonus;
        private readonly bool _isInitial;

        protected StarterRelicModel(ModelId id, RelicRarity rarity, RelicKind kind, int attackBonus = 0, int defenseBonus = 0, int maxHpBonus = 0, bool isInitial = false)
        {
            _id = id;
            _rarity = rarity;
            _kind = kind;
            _attackBonus = attackBonus;
            _defenseBonus = defenseBonus;
            _maxHpBonus = maxHpBonus;
            _isInitial = isInitial;
        }

        public override ModelId Id => _id;
        public override RelicRarity Rarity => _rarity;
        public override RelicKind Kind => _kind;
        public override int AttackBonus => _attackBonus;
        public override int DefenseBonus => _defenseBonus;
        public override int MaxHpBonus => _maxHpBonus;
        public override bool IsInitialRelic => _isInitial;
    }

    internal sealed class LivingFleshRelicModel : StarterRelicModel
    {
        public LivingFleshRelicModel() : base(StarterContentIds.Relics.LivingFlesh, RelicRarity.Common, RelicKind.Passive, attackBonus: 1) { }

        public override Task OnRoomEnteredAsync(RoomLifecycleContext ctx)
        {
            int roomsEntered = ctx.Domain.PlayerRunState.GetKeyword("livingFleshRooms") + 1;
            ctx.Domain.SetPlayerKeyword("livingFleshRooms", roomsEntered, StatModifierScope.Permanent);
            if (roomsEntered % 3 == 0)
            {
                ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, 1, "relic:living-flesh:" + roomsEntered), ctx.Events, "player:attack");
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class WoodShieldRelicModel : StarterRelicModel
    {
        public WoodShieldRelicModel() : base(StarterContentIds.Relics.WoodShield, RelicRarity.Common, RelicKind.Passive, defenseBonus: 2) { }
    }

    internal sealed class WoodSwordRelicModel : StarterRelicModel
    {
        public WoodSwordRelicModel() : base(StarterContentIds.Relics.WoodSword, RelicRarity.Common, RelicKind.Passive, attackBonus: 2) { }
    }

    internal sealed class LawWandRelicModel : StarterRelicModel
    {
        public LawWandRelicModel() : base(StarterContentIds.Relics.LawWand, RelicRarity.Common, RelicKind.Active, attackBonus: 1) { }

        public override ItemTargetMode TargetMode => ItemTargetMode.AnyCardThenAnyCell;

        public override bool CanActivate(ActiveRelicContext ctx)
        {
            if (!base.CanActivate(ctx) || ctx.SourceIntent is not ActivateRelicIntent intent)
            {
                return false;
            }

            return ctx.Domain.Grid.TryGetCard(intent.Target.PrimaryCard, out CardInstance target)
                && target.CardType != CardType.Player
                && target.Coord.HasValue
                && intent.Target.GridCell.IsValid
                && intent.Target.GridCell.CellIndex != 8;
        }

        public override Task ActivateAsync(ActiveRelicContext ctx)
        {
            ActivateRelicIntent intent = (ActivateRelicIntent)ctx.SourceIntent;
            CardInstance target = ctx.Domain.Grid.GetCard(intent.Target.PrimaryCard);
            if (!target.Coord.HasValue)
            {
                return Task.CompletedTask;
            }

            GridCoord destination = intent.Target.GridCell;
            StarterContentLogic.TryMoveOrCover(ctx.Domain.Grid, target, destination, ctx.Events, target.IsFaceUp);
            return Task.CompletedTask;
        }
    }

    internal sealed class EndlessWaterBagRelicModel : StarterRelicModel
    {
        public EndlessWaterBagRelicModel() : base(StarterContentIds.Relics.EndlessWaterBag, RelicRarity.Common, RelicKind.Active, maxHpBonus: 4) { }

        public override Task ActivateAsync(ActiveRelicContext ctx)
        {
            ctx.Domain.HealPlayer(6, ctx.Events, "relic:endless-water-bag");
            return Task.CompletedTask;
        }
    }

    internal sealed class ItemStockpileRelicModel : StarterRelicModel
    {
        public ItemStockpileRelicModel() : base(StarterContentIds.Relics.ItemStockpile, RelicRarity.Rare, RelicKind.Passive) { }

        public override Task OnRoomEnteredAsync(RoomLifecycleContext ctx)
        {
            if (ctx.RoomType == RoomType.Restaurant || ctx.Domain.DungeonDeck == null)
            {
                return Task.CompletedTask;
            }

            IRng rng = ctx.Domain.Rng ?? new DeterministicRng(1);
            ctx.Domain.DungeonDeck.AddToTop(StarterContentLogic.CreateCardInstance(ctx.Domain, StarterContentLogic.PickRandomItem(ctx.Domain)));
            ctx.Domain.DungeonDeck.AddToTop(StarterContentLogic.CreateCardInstance(ctx.Domain, StarterContentLogic.PickRandomItem(ctx.Domain)));
            ctx.Domain.DungeonDeck.Shuffle(rng);
            return Task.CompletedTask;
        }
    }

    internal sealed class BloodShieldRelicModel : StarterRelicModel
    {
        public BloodShieldRelicModel() : base(StarterContentIds.Relics.BloodShield, RelicRarity.Rare, RelicKind.Active, defenseBonus: 2) { }

        public override Task ActivateAsync(ActiveRelicContext ctx)
        {
            int stack = ctx.PlayerCard.GetState("bloodShieldStacks") + 1;
            ctx.PlayerCard.SetState("bloodShieldStacks", stack);
            ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Defense, StatModifierScope.Room, 2, "relic:blood-shield:" + stack), ctx.Events, "player:defense");
            return Task.CompletedTask;
        }

        public override Task OnMonsterDefeatedAsync(MonsterDefeatedContext ctx)
        {
            if (ctx.Domain.Relics.ActiveSlot.Contains(Id))
            {
                ctx.Domain.Relics.ActiveSlot.SetUsesRemaining(ctx.Domain.Relics.ActiveSlot.MaxUsesPerRoom);
            }

            return Task.CompletedTask;
        }
    }

    internal sealed class VillageGoodSwordRelicModel : StarterRelicModel
    {
        public VillageGoodSwordRelicModel() : base(StarterContentIds.Relics.VillageGoodSword, RelicRarity.Common, RelicKind.Passive, attackBonus: 1, isInitial: true) { }

        public override Task OnMonsterDefeatedAsync(MonsterDefeatedContext ctx)
        {
            if (ctx.WasElite || ctx.WasBoss)
            {
                ctx.Domain.AddPlayerModifier(new StatModifier(PlayerStat.Attack, StatModifierScope.Permanent, 2, "relic:village-good-sword:" + ctx.DefeatedMonster.InstanceId.Value), ctx.Events, "player:attack");
            }

            return Task.CompletedTask;
        }
    }
}
