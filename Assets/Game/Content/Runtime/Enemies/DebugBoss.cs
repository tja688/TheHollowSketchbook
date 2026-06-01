using System.Threading.Tasks;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Combat.Commands;
using Game.Core.Entities;
using Game.Core.Random;

namespace Game.Content
{
    public sealed class DebugBoss : EnemyModel
    {
        public override ModelId Id => new ModelId("Enemy", "DebugBoss");
        public override string Name => "Debug Boss";
        public override int MaxHp => 120;

        public override EnemyIntent BuildIntent(CombatState combat, Creature self, IRng rng)
        {
            int turn = self.GetState("BossTurn", 0) % 3;
            return turn switch
            {
                0 => new EnemyIntent { Type = EnemyIntentType.Buff, Description = "Roar" },
                1 => new EnemyIntent { Type = EnemyIntentType.Attack, Damage = 14, Description = "Slam" },
                _ => new EnemyIntent { Type = EnemyIntentType.Attack, Damage = 8, Hits = 2, Description = "Double Hit" }
            };
        }

        public override async Task ExecuteIntent(CardPlayContext ctx, Creature self, EnemyIntent intent)
        {
            if (intent.Type == EnemyIntentType.Buff)
            {
                await CreatureCmd.ApplyPower(ctx, self, Game.Core.Models.ModelDb.CreateMutable<Game.Core.Powers.PowerModel>(new ModelId("Power", "Strength")), 2);
            }
            else
            {
                for (int i = 0; i < intent.Hits; i++)
                {
                    await CreatureCmd.DealDamage(ctx, self, ctx.Combat.Players[0].Creature, intent.Damage, null);
                }
            }

            self.SetState("BossTurn", self.GetState("BossTurn", 0) + 1);
        }
    }
}
