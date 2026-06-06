using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Combat
{
    public sealed class CombatResolution
    {
        private GridState _grid;

        public CombatResolution(GridState grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public DomainActionContext Domain { get; set; }

        public void SetGrid(GridState grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public async Task<DamageResult> ApplyDamageAsync(DamageInfo info, ICollection<DomainEvent> events)
        {
            if (!_grid.TryGetCard(info.Target.CardId, out CardInstance target))
            {
                throw new InvalidOperationException("Damage target does not exist: " + info.Target.CardId);
            }

            if (!target.HasHitPoints)
            {
                return new DamageResult
                {
                    TargetCardId = target.InstanceId,
                    OriginalAmount = info.BaseAmount,
                    DefenseReducedAmount = 0,
                    HpLoss = 0,
                    Killed = false
                };
            }

            _grid.TryGetCard(info.Source.CardId, out CardInstance sourceCard);
            DamageContext damageCtx = new DamageContext(info, sourceCard, target, Domain, events);

            // Phase 1: BeforeDamage hooks
            await NotifyBeforeDamageAsync(damageCtx).ConfigureAwait(false);

            int amount = info.BaseAmount;

            // Phase 2: ModifyDamageDealt (source-side hooks)
            amount = await ModifyDamageDealtAsync(damageCtx, amount).ConfigureAwait(false);

            // Phase 3: Defense reduction
            int reduced = info.IgnoreDefense ? amount : Math.Max(0, amount - target.Defense);

            // Phase 4: ModifyDamageTaken (target-side hooks)
            reduced = await ModifyDamageTakenAsync(damageCtx, reduced).ConfigureAwait(false);

            int hpLoss;
            bool prevented = false;

            if (info.CanBePrevented && target.GetState("damageImmunity", 0) > 0)
            {
                target.SetState("damageImmunity", target.GetState("damageImmunity") - 1);
                hpLoss = 0;
                prevented = true;
            }
            else
            {
                hpLoss = target.ApplyHpLoss(reduced);
            }

            DamageResult result = new DamageResult
            {
                TargetCardId = target.InstanceId,
                OriginalAmount = amount,
                DefenseReducedAmount = reduced,
                HpLoss = hpLoss,
                Killed = target.HasHitPoints && !target.IsAlive,
                Prevented = prevented
            };

            events?.Add(new DomainEvent(DomainEventType.DamageApplied)
            {
                SourceCardId = info.Source.CardId,
                TargetCardId = target.InstanceId,
                Amount = hpLoss,
                SecondaryAmount = reduced,
                Reason = info.Reason + (prevented ? ":Prevented" : string.Empty)
            });

            // Phase 5: AfterDamage hooks
            await NotifyAfterDamageAsync(damageCtx, result).ConfigureAwait(false);

            return result;
        }

        public async Task ResolvePlayerVsMonsterAsync(CardInstance player, CardInstance monster, ICollection<DomainEvent> events)
        {
            bool playerFirst = HasFirstStrike(player);
            bool monsterFirst = HasFirstStrike(monster);

            if (playerFirst && !monsterFirst)
            {
                DamageResult playerHit = await ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(player.InstanceId),
                    DamageTarget.Card(monster.InstanceId),
                    player.Attack,
                    DamageKind.Attack,
                    false,
                    "PlayerAttackMonster"), events).ConfigureAwait(false);

                if (!playerHit.Killed)
                {
                    await ApplyDamageAsync(new DamageInfo(
                        DamageSource.FromCard(monster.InstanceId),
                        DamageTarget.Card(player.InstanceId),
                        monster.Attack,
                        DamageKind.Attack,
                        false,
                        "MonsterCounterAttack"), events).ConfigureAwait(false);
                }
            }
            else if (monsterFirst && !playerFirst)
            {
                DamageResult monsterHit = await ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(monster.InstanceId),
                    DamageTarget.Card(player.InstanceId),
                    monster.Attack,
                    DamageKind.Attack,
                    false,
                    "MonsterAttackPlayer"), events).ConfigureAwait(false);

                if (!monsterHit.Killed)
                {
                    await ApplyDamageAsync(new DamageInfo(
                        DamageSource.FromCard(player.InstanceId),
                        DamageTarget.Card(monster.InstanceId),
                        player.Attack,
                        DamageKind.Attack,
                        false,
                        "PlayerCounterAttack"), events).ConfigureAwait(false);
                }
            }
            else
            {
                // Both have first strike or neither has it:
                // player attacks first; monster counter-attacks only if it survives.
                DamageResult playerHit = await ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(player.InstanceId),
                    DamageTarget.Card(monster.InstanceId),
                    player.Attack,
                    DamageKind.Attack,
                    false,
                    "PlayerAttackMonster"), events).ConfigureAwait(false);

                if (!playerHit.Killed)
                {
                    await ApplyDamageAsync(new DamageInfo(
                        DamageSource.FromCard(monster.InstanceId),
                        DamageTarget.Card(player.InstanceId),
                        monster.Attack,
                        DamageKind.Attack,
                        false,
                        "MonsterCounterAttack"), events).ConfigureAwait(false);
                }
            }
        }

        public async Task ResolvePlayerVsTrapAsync(CardInstance player, CardInstance trap, ICollection<DomainEvent> events)
        {
            await ApplyDamageAsync(new DamageInfo(
                DamageSource.FromCard(player.InstanceId),
                DamageTarget.Card(trap.InstanceId),
                player.Attack,
                DamageKind.Attack,
                false,
                "PlayerAttackTrap"), events).ConfigureAwait(false);

            if (trap.ContactDamageToPlayer > 0)
            {
                await ApplyDamageAsync(new DamageInfo(
                    DamageSource.FromCard(trap.InstanceId),
                    DamageTarget.Card(player.InstanceId),
                    trap.ContactDamageToPlayer,
                    DamageKind.Trap,
                    true,
                    "TrapContactDamage"), events).ConfigureAwait(false);
            }
        }

        private async Task NotifyBeforeDamageAsync(DamageContext ctx)
        {
            if (Domain == null)
            {
                return;
            }

            await Domain.NotifyBeforeDamageAsync(ctx).ConfigureAwait(false);
        }

        private async Task NotifyAfterDamageAsync(DamageContext ctx, DamageResult result)
        {
            if (Domain == null)
            {
                return;
            }

            await Domain.NotifyAfterDamageAsync(ctx, result).ConfigureAwait(false);
        }

        private async Task<int> ModifyDamageDealtAsync(DamageContext ctx, int current)
        {
            if (Domain == null)
            {
                return current;
            }

            return await Domain.ModifyDamageDealtAsync(ctx, current).ConfigureAwait(false);
        }

        private async Task<int> ModifyDamageTakenAsync(DamageContext ctx, int current)
        {
            if (Domain == null)
            {
                return current;
            }

            return await Domain.ModifyDamageTakenAsync(ctx, current).ConfigureAwait(false);
        }

        private bool HasFirstStrike(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            if (card.GetState("firstStrike", 0) > 0)
            {
                return true;
            }

            return card.CardType == CardType.Player
                && Domain?.PlayerRunState != null
                && Domain.PlayerRunState.GetKeyword("firstStrike") > 0;
        }
    }
}
