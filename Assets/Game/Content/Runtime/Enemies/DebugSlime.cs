using System.Threading.Tasks;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Combat.Commands;
using Game.Core.Entities;
using Game.Core.Random;

namespace Game.Content
{
    public sealed class DebugSlime : EnemyModel
    {
        public override ModelId Id => new ModelId("Enemy", "DebugSlime");
        public override string Name => "Debug Slime";
        public override int MaxHp => 30;

        public override EnemyIntent BuildIntent(CombatState combat, Creature self, IRng rng)
        {
            bool attack = rng.NextInt(0, 2) == 0;
            return attack
                ? new EnemyIntent
                {
                    Type = EnemyIntentType.Attack,
                    Damage = 5,
                    Description = "Tackle"
                }
                : new EnemyIntent
                {
                    Type = EnemyIntentType.Debuff,
                    Description = "Goo"
                };
        }

        public override async Task ExecuteIntent(CardPlayContext ctx, Creature self, EnemyIntent intent)
        {
            Creature player = ctx.Combat.Players[0].Creature;
            if (intent.Type == EnemyIntentType.Attack)
            {
                await CreatureCmd.DealDamage(ctx, self, player, intent.Damage, null);
                return;
            }

            await CreatureCmd.ApplyPower(ctx, player, Game.Core.Models.ModelDb.CreateMutable<Game.Core.Powers.PowerModel>(new ModelId("Power", "Weak")), 1);
        }
    }
}
