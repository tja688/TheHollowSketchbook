using System.Threading.Tasks;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Combat.Commands;
using Game.Core.Entities;
using Game.Core.Random;

namespace Game.Content
{
    public sealed class DebugElite : EnemyModel
    {
        public override ModelId Id => new ModelId("Enemy", "DebugElite");
        public override string Name => "Debug Elite";
        public override int MaxHp => 70;

        public override EnemyIntent BuildIntent(CombatState combat, Creature self, IRng rng)
        {
            bool attack = self.GetState("EliteTurn", 0) % 2 == 0;
            return attack
                ? new EnemyIntent { Type = EnemyIntentType.Attack, Damage = 12, Description = "Crush" }
                : new EnemyIntent { Type = EnemyIntentType.Buff, Description = "Fortify", Block = 8 };
        }

        public override async Task ExecuteIntent(CardPlayContext ctx, Creature self, EnemyIntent intent)
        {
            if (intent.Type == EnemyIntentType.Attack)
            {
                await CreatureCmd.DealDamage(ctx, self, ctx.Combat.Players[0].Creature, intent.Damage, null);
            }
            else
            {
                await CreatureCmd.GainBlock(ctx, self, intent.Block);
                await CreatureCmd.ApplyPower(ctx, self, Game.Core.Models.ModelDb.CreateMutable<Game.Core.Powers.PowerModel>(new ModelId("Power", "Strength")), 1);
            }

            self.SetState("EliteTurn", self.GetState("EliteTurn", 0) + 1);
        }
    }
}
