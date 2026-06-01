using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Entities;
using Game.Core.Models;

namespace Game.Content
{
    public static class StarterContentRegistry
    {
        private static bool _isRegistered;

        public static void RegisterAll(bool force = false)
        {
            if (_isRegistered && !force)
            {
                return;
            }

            if (force)
            {
                ModelDb.Clear();
            }

            Register(new PrototypeHero());
            Register(new StrikeCard());
            Register(new DefendCard());
            Register(new BashCard());
            Register(new ZapDebugCard());
            Register(new GuardDebugCard());
            Register(new StrengthPowerModel());
            Register(new VulnerablePowerModel());
            Register(new WeakPowerModel());
            Register(new DebugCultist());
            Register(new DebugSlime());
            Register(new PrototypeEncounter());
            Register(new PrototypeAct());

            _isRegistered = true;
        }

        private static void Register(AbstractModel model)
        {
            if (!ModelDb.Contains(model.Id))
            {
                ModelDb.Register(model);
            }
        }
    }
}
