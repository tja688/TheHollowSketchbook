using System;
using System.Collections.Generic;
using Game.Core.Rewards;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.RunFlow
{
    /// <summary>
    /// Reward panel. StS CardRewardChoice dependency removed.
    /// BOUNDARY: Updated to use ChoiceReward instead of CardRewardChoice.
    /// </summary>
    public sealed class PrototypeRewardPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private Button _buttonPrefab;
        [SerializeField] private Text _titleText;

        private readonly List<Button> _buttons = new List<Button>();

        public event Action<Reward> RewardSelected;
        public event Action<ChoiceReward, int> ChoiceSelected;
        public event Action<ChoiceReward> ChoiceSkipped;
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
                else if (reward is ChoiceReward choiceReward)
                {
                    for (int choiceIndex = 0; choiceIndex < choiceReward.Choices.Count; choiceIndex++)
                    {
                        int cachedIndex = choiceIndex;
                        string label = choiceReward.Choices[cachedIndex];
                        CreateButton("Take: " + label, () => ChoiceSelected?.Invoke(choiceReward, cachedIndex));
                    }

                    CreateButton("Skip", () => ChoiceSkipped?.Invoke(choiceReward));
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
