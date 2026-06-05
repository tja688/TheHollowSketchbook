using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Combat
{
    public sealed class CombatResolution
    {
        private readonly GridState _grid;

        public CombatResolution(GridState grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public DamageResult ApplyDamage(DamageInfo info, ICollection<DomainEvent> events)
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

            int amount = info.BaseAmount;
            int reduced = info.IgnoreDefense ? amount : Math.Max(0, amount - target.Defense);
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

            return result;
        }

        public void ResolvePlayerVsMonster(CardInstance player, CardInstance monster, ICollection<DomainEvent> events)
        {
            bool playerFirst = HasFirstStrike(player);
            bool monsterFirst = HasFirstStrike(monster);

            if (playerFirst && !monsterFirst)
            {
                // 玩家先攻
                DamageResult playerHit = ApplyDamage(new DamageInfo(
                    DamageSource.FromCard(player.InstanceId),
                    DamageTarget.Card(monster.InstanceId),
                    player.Attack,
                    DamageKind.Attack,
                    false,
                    "PlayerAttackMonster"), events);

                if (!playerHit.Killed)
                {
                    ApplyDamage(new DamageInfo(
                        DamageSource.FromCard(monster.InstanceId),
                        DamageTarget.Card(player.InstanceId),
                        monster.Attack,
                        DamageKind.Attack,
                        false,
                        "MonsterCounterAttack"), events);
                }
            }
            else if (monsterFirst && !playerFirst)
            {
                // 怪物先攻
                DamageResult monsterHit = ApplyDamage(new DamageInfo(
                    DamageSource.FromCard(monster.InstanceId),
                    DamageTarget.Card(player.InstanceId),
                    monster.Attack,
                    DamageKind.Attack,
                    false,
                    "MonsterAttackPlayer"), events);

                if (!monsterHit.Killed)
                {
                    ApplyDamage(new DamageInfo(
                        DamageSource.FromCard(player.InstanceId),
                        DamageTarget.Card(monster.InstanceId),
                        player.Attack,
                        DamageKind.Attack,
                        false,
                        "PlayerCounterAttack"), events);
                }
            }
            else
            {
                // 双方同时先攻（都有先攻）或都不先攻时，按设计文档同时受到伤害
                ApplyDamage(new DamageInfo(
                    DamageSource.FromCard(player.InstanceId),
                    DamageTarget.Card(monster.InstanceId),
                    player.Attack,
                    DamageKind.Attack,
                    false,
                    "PlayerAttackMonster"), events);

                ApplyDamage(new DamageInfo(
                    DamageSource.FromCard(monster.InstanceId),
                    DamageTarget.Card(player.InstanceId),
                    monster.Attack,
                    DamageKind.Attack,
                    false,
                    "MonsterCounterAttack"), events);
            }
        }

        public void ResolvePlayerVsTrap(CardInstance player, CardInstance trap, ICollection<DomainEvent> events)
        {
            ApplyDamage(new DamageInfo(
                DamageSource.FromCard(player.InstanceId),
                DamageTarget.Card(trap.InstanceId),
                player.Attack,
                DamageKind.Attack,
                false,
                "PlayerAttackTrap"), events);

            if (trap.ContactDamageToPlayer > 0)
            {
                ApplyDamage(new DamageInfo(
                    DamageSource.FromCard(trap.InstanceId),
                    DamageTarget.Card(player.InstanceId),
                    trap.ContactDamageToPlayer,
                    DamageKind.Trap,
                    true,
                    "TrapContactDamage"), events);
            }
        }

        private static bool HasFirstStrike(CardInstance card)
        {
            return card.GetState("firstStrike", 0) > 0;
        }
    }
}
