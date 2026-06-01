using System.Threading.Tasks;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Combat.Commands;
using Game.Core.Entities;
using Game.Core.Random;

namespace Game.Content
{
    public sealed class DebugCultist : EnemyModel
    {
        public override ModelId Id => new ModelId("Enemy", "DebugCultist");
        public override string Name => "Debug Cultist";
        public override int MaxHp => 48;

        public override EnemyIntent BuildIntent(CombatState combat, Creature self, IRng rng)
        {
            int turnIndex = self.GetState("CultistTurn", 0);
            if (turnIndex == 0)
            {
                return new EnemyIntent
                {
                    Type = EnemyIntentType.Buff,
                    Description = "Chant"
                };
            }

            return new EnemyIntent
            {
                Type = EnemyIntentType.Attack,
                Damage = 6,
                Hits = 1,
                Description = "Strike"
            };
        }

        public override async Task ExecuteIntent(CardPlayContext ctx, Creature self, EnemyIntent intent)
        {
            int turnIndex = self.GetState("CultistTurn", 0);
            if (turnIndex == 0)
            {
                await CreatureCmd.ApplyPower(ctx, self, Game.Core.Models.ModelDb.CreateMutable<Game.Core.Powers.PowerModel>(new ModelId("Power", "Strength")), 1);
            }
            else
            {
                for (int i = 0; i < intent.Hits; i++)
                {
                    await CreatureCmd.DealDamage(ctx, self, ctx.Combat.Players[0].Creature, intent.Damage, null);
                }
            }

            self.SetState("CultistTurn", turnIndex + 1);
        }
    }
}
