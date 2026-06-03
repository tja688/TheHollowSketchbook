using System.Collections.Generic;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Deck;
using StrayPathCore.Relic;
using UnityEngine;

namespace StrayPathCore.EventNodes
{
    /// <summary>
    /// 老者(Old Man Will)系统 —— 开局启动礼物发放。
    /// </summary>
    public class OldManSystem : MonoBehaviour
    {
        public static OldManSystem Instance { get; private set; }

        public int SelectedGiftID { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void GenerateGift()
        {
            // 诅咒10: 老者礼物失效
            var curse = CurseSystem.Instance;
            if (curse != null && curse.HasCurse(10))
            {
                SelectedGiftID = 0; // Nothing
                return;
            }

            SelectedGiftID = Random.Range(1, 7);
        }

        public void AcceptGift()
        {
            var run = GameStateManager.Instance.CurrentRun;
            switch (SelectedGiftID)
            {
                case 1: // Show stance: MaxHP+5, HP回满
                    GameStateManager.Instance.SetMaxHP(run.MaxHP + 5, true);
                    break;
                case 2: // Take pouch: +50金币
                    GameStateManager.Instance.AddGold(50, "oldman");
                    break;
                case 3: // Take relic: 随机遗物(1-23, 34)
                    var pool = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 34 };
                    int relicID = pool[Random.Range(0, pool.Count)];
                    RelicManager.Instance?.GiveRelicToHero(relicID);
                    break;
                case 4: // Show technique: 随机升级1张卡
                    if (run.DeckCards.Count > 0)
                    {
                        var card = run.DeckCards[Random.Range(0, run.DeckCards.Count)];
                        card.IsUpgraded = true;
                    }
                    break;
                case 5: // Listen: 获得1张随机卡
                    // 简化：给予一张Common卡
                    var newCard = new CardRuntime { CardID = Random.Range(11, 20), CopyCount = 1 };
                    GameStateManager.Instance.AddCardToDeck(newCard);
                    break;
                case 6: // Envision: 交换1张卡
                    // 由UI层打开交换面板
                    break;
            }
        }
    }
}
