using System;
using System.Collections.Generic;
using Game.Core.Rewards;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.RunFlow
{
    public sealed class PrototypeRewardPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private Button _buttonPrefab;
        [SerializeField] private Text _titleText;

        private readonly List<Button> _buttons = new List<Button>();

        public event Action<Reward> RewardSelected;
        public event Action<CardRewardChoice, int> CardSelected;
        public event Action<CardRewardChoice> CardSkipped;
        public event Action ContinueClicked;

        public void ShowRewards(IReadOnlyList<Reward> rewards)
        {
            ClearButtons();
            gameObject.SetActive(true);
            _titleText.text = "Rewards";

            for (int i = 0; i < rewards.Count; i++)
            {
                Reward reward = rewards[i];
                if (reward.IsResolved)
                {
                    continue;
                }

                if (reward is GoldReward gold)
                {
                    CreateButton(gold.Label, () => RewardSelected?.Invoke(gold));
                }
                else if (reward is CardRewardChoice cardReward)
                {
                    for (int choiceIndex = 0; choiceIndex < cardReward.Choices.Count; choiceIndex++)
                    {
                        int cachedIndex = choiceIndex;
                        var card = cardReward.Choices[cachedIndex];
                        CreateButton("Take Card: " + card.Name, () => CardSelected?.Invoke(cardReward, cachedIndex));
                    }

                    CreateButton("Skip Card", () => CardSkipped?.Invoke(cardReward));
                }
            }

            CreateButton("Continue", () => ContinueClicked?.Invoke());
        }

        public void Hide()
        {
            ClearButtons();
            gameObject.SetActive(false);
        }

        private void CreateButton(string label, Action onClick)
        {
            Button button = Instantiate(_buttonPrefab, _contentRoot);
            button.gameObject.SetActive(true);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }

            button.onClick.AddListener(() => onClick?.Invoke());
            _buttons.Add(button);
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null)
                {
                    Destroy(_buttons[i].gameObject);
                }
            }

            _buttons.Clear();
        }
    }
}
