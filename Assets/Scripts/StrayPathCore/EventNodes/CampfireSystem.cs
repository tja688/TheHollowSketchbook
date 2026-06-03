using StrayPathCore.Core;
using StrayPathCore.Deck;
using StrayPathCore.Relic;
using UnityEngine;

namespace StrayPathCore.EventNodes
{
    /// <summary>
    /// 营地系统 —— 二选一：休息恢复HP 或 燃烧删卡。
    /// </summary>
    public class CampfireSystem : MonoBehaviour
    {
        public static CampfireSystem Instance { get; private set; }

        public bool HasRecovered { get; private set; }
        public bool HasBurned { get; private set; }
        public int ExtraBurnCharges { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            HasRecovered = false;
            HasBurned = false;
            ExtraBurnCharges = 0;

            // 遗物77: Amulet of the Void 额外Burn次数
            var relic77 = GameStateManager.Instance.GetRelic(77);
            if (relic77 != null) ExtraBurnCharges = relic77.CurrentCharges;

            // 遗物301: Sliced Carrot 自动恢复
            var relic301 = GameStateManager.Instance.GetRelic(301);
            if (relic301 != null && relic301.CurrentCharges > 0)
            {
                var run = GameStateManager.Instance.CurrentRun;
                if (run.CurrentHP <= run.MaxHP - 5)
                {
                    GameStateManager.Instance.HealHP(5, "relic_301");
                    relic301.CurrentCharges--;
                }
            }
        }

        public void Recover()
        {
            if (HasRecovered) return;
            var run = GameStateManager.Instance.CurrentRun;
            float multiplier = 0.4f;

            // 诅咒5: 恢复量降至35%
            var curse = CurseSystem.Instance;
            if (curse != null) multiplier = curse.GetCampfireHealMultiplier();

            int healAmount = Mathf.RoundToInt(run.MaxHP * multiplier);
            GameStateManager.Instance.HealHP(healAmount, "campfire");

            // 遗物17: Rune of Growth 额外效果
            if (GameStateManager.Instance.HasRelic(17))
            {
                GameStateManager.Instance.SetMaxHP(run.MaxHP + 1);
            }

            // 遗物80: Incense 额外恢复
            if (GameStateManager.Instance.HasRelic(80))
            {
                GameStateManager.Instance.HealHP(20, "relic_80");
                GameStateManager.Instance.SetMaxMP(run.MaxMP + 1);
            }

            // 遗物95: 额外恢复25%
            if (GameStateManager.Instance.HasRelic(95))
            {
                GameStateManager.Instance.HealHP(Mathf.RoundToInt(run.MaxHP * 0.25f), "relic_95");
            }

            HasRecovered = true;
        }

        public void BurnCard(CardRuntime card)
        {
            if (HasBurned && ExtraBurnCharges <= 0) return;
            DeckManager.Instance?.BurnCard(card);

            if (HasBurned)
            {
                ExtraBurnCharges--;
                var relic77 = GameStateManager.Instance.GetRelic(77);
                if (relic77 != null) relic77.CurrentCharges = ExtraBurnCharges;
            }
            else
            {
                HasBurned = true;
            }
        }

        public bool CanBurnAgain()
        {
            return !HasBurned || ExtraBurnCharges > 0;
        }
    }
}
