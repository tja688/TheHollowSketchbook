using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StrayPathCore.Core;
using StrayPathCore.Combat;

namespace StrayPathCore.Deck
{
    public class SpellSystem : MonoBehaviour
    {
        public static SpellSystem Instance { get; private set; }

        public List<int> AvailableSpells { get; private set; } = new List<int>();
        public int CurrentMP { get; private set; }
        public int MaxMP { get; private set; } = 3;
        public bool SpellCastThisTurn { get; private set; }

        private readonly string[] _spellNames = new string[]
        {
            "Heal", "Blast", "Draw", "Energize", "Hold", "Resist", "Wall", "Empower",
            "Fortify", "Hex", "Barrier", "Phantom", "StoneSkin", "Flare", "Rebirth", "Enrage"
        };

        private readonly string[] _spellDescriptions = new string[]
        {
            "Restore 10 HP", "Deal 15 damage to an enemy", "Draw 2 cards", "Gain 2 Energy",
            "Hold 1 card until next turn", "Gain 2 Thorns", "Gain 12 Block", "Gain 2 Power",
            "Gain 2 Toughness", "Apply Hex to enemy (Boss)", "Gain Barrier", "Create Phantom card",
            "Gain StoneSkin", "Deal 25 damage to all enemies (Boss)", "Heal to full HP", "Gain Enrage"
        };

        private readonly int[] _spellCosts = new int[] { 1, 1, 1, 1, 1, 1, 2, 2, 2, 3, 2, 2, 2, 3, 3, 2 };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(List<int> spells)
        {
            AvailableSpells = spells?.Distinct().ToList() ?? new List<int>();
            CurrentMP = MaxMP;
            SpellCastThisTurn = false;
            ResetBossSpellCastFlags();
        }

        public void AddSpell(int spellID)
        {
            if (AvailableSpells.Count >= 6) return;
            if (!AvailableSpells.Contains(spellID))
                AvailableSpells.Add(spellID);
        }

        public bool CanCastSpell(int spellID)
        {
            if (SpellCastThisTurn) return false;
            if (!AvailableSpells.Contains(spellID)) return false;
            int cost = GetSpellMPCost(spellID);
            if (CurrentMP < cost) return false;
            if (IsBossSpell(spellID) && HasBossSpellBeenCast(spellID)) return false;
            return true;
        }

        public void CastSpell(int spellID, EnemyCombatEntity target = null)
        {
            if (!CanCastSpell(spellID)) return;

            int cost = GetSpellMPCost(spellID);
            CurrentMP -= cost;
            SpellCastThisTurn = true;

            if (IsBossSpell(spellID))
            {
                var bs = GameStateManager.Instance?.BattleState;
                if (bs != null) bs.BossSpellActivated[spellID] = true;
            }

            ExecuteSpellEffect(spellID, target);

            GameEventBus.Instance.Publish(new EnergyChangedEvent
            {
                OldValue = CurrentMP + cost,
                NewValue = CurrentMP,
                Reason = "SpellCast"
            });

            GameEventBus.Instance.Publish(new SpellCastEvent
            {
                SpellID = spellID,
                TargetEnemyUID = target?.UniqueID
            });
        }

        private void ExecuteSpellEffect(int spellID, EnemyCombatEntity target)
        {
            var bs = GameStateManager.Instance?.BattleState;
            switch (spellID)
            {
                case 0:
                    GameStateManager.Instance?.HealHP(10, "Spell");
                    break;
                case 1:
                    target?.TakeDamage(15);
                    break;
                case 2:
                    DeckManager.Instance?.DrawCards(2);
                    break;
                case 3:
                    EnergyManager.Instance?.GainEnergy(2);
                    break;
                case 4:
                    if (DeckManager.Instance?.Hand.Count > 0)
                        DeckManager.Instance?.HoldCard(DeckManager.Instance.Hand[0]);
                    break;
                case 5:
                    if (bs != null) bs.CurrentHeroThorns += 2;
                    break;
                case 6:
                    if (bs != null)
                    {
                        bs.CurrentBlock += 12;
                        GameEventBus.Instance.Publish(new BlockGainedEvent { TargetUID = "hero", Amount = 12, TotalBlock = bs.CurrentBlock });
                    }
                    break;
                case 7:
                    if (bs != null) bs.CurrentHeroPower += 2;
                    break;
                case 8:
                    if (bs != null) bs.CurrentHeroToughness += 2;
                    break;
                case 9:
                    if (target != null)
                    {
                        GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
                        {
                            TargetUID = target.UniqueID,
                            EffectType = StatusEffectType.Weak,
                            Value = 3,
                            DurationType = StatusDurationType.TurnBased
                        });
                    }
                    break;
                case 10:
                    if (bs != null)
                    {
                        GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
                        {
                            TargetUID = "hero",
                            EffectType = StatusEffectType.Barrier,
                            Value = 1,
                            DurationType = StatusDurationType.ChargeBased
                        });
                    }
                    break;
                case 11:
                    DeckManager.Instance?.AddFakeCardToPlayerHand(1, false);
                    break;
                case 12:
                    if (bs != null)
                    {
                        GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
                        {
                            TargetUID = "hero",
                            EffectType = StatusEffectType.Armor,
                            Value = 2,
                            DurationType = StatusDurationType.TurnBased
                        });
                    }
                    break;
                case 13:
                    Debug.Log("[Spell] Flare deals 25 damage to all enemies");
                    break;
                case 14:
                    GameStateManager.Instance?.HealHP(999, "Rebirth");
                    break;
                case 15:
                    if (bs != null)
                    {
                        GameEventBus.Instance.Publish(new StatusEffectAppliedEvent
                        {
                            TargetUID = "hero",
                            EffectType = StatusEffectType.Power,
                            Value = 2,
                            DurationType = StatusDurationType.TurnBased
                        });
                    }
                    break;
            }
        }

        public void OnTurnStarted()
        {
            SpellCastThisTurn = false;
        }

        public void OnBattleEnded()
        {
            SpellCastThisTurn = false;
            ResetBossSpellCastFlags();
        }

        public int GetSpellMPCost(int spellID)
        {
            if (spellID < 0 || spellID >= _spellCosts.Length) return 99;
            return _spellCosts[spellID];
        }

        public string GetSpellName(int spellID)
        {
            if (spellID < 0 || spellID >= _spellNames.Length) return "Unknown";
            return _spellNames[spellID];
        }

        public string GetSpellDescription(int spellID)
        {
            if (spellID < 0 || spellID >= _spellDescriptions.Length) return "";
            return _spellDescriptions[spellID];
        }

        public bool IsBossSpell(int spellID)
        {
            return spellID == 9 || spellID == 13;
        }

        public bool HasBossSpellBeenCast(int spellID)
        {
            var bs = GameStateManager.Instance?.BattleState;
            if (bs == null) return false;
            return bs.BossSpellActivated.TryGetValue(spellID, out bool cast) && cast;
        }

        public void ResetBossSpellCastFlags()
        {
            var bs = GameStateManager.Instance?.BattleState;
            bs?.BossSpellActivated.Clear();
        }
    }
}
