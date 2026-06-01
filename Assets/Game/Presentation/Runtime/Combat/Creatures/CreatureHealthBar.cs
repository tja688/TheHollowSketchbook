using System;
using System.Collections.Generic;
using System.Text;
using Game.Core.Entities;
using Game.Core.Powers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Combat.Creatures
{
    public sealed class CreatureHealthBar : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _blockText;
        [SerializeField] private GameObject _blockRoot;
        [SerializeField] private TextMeshProUGUI _powersText;
        [SerializeField] private GameObject _powersRoot;

        private Creature _creature;

        public void Bind(Creature creature)
        {
            _creature = creature ?? throw new ArgumentNullException(nameof(creature));
            Refresh();
        }

        public void Refresh()
        {
            if (_creature == null)
            {
                return;
            }

            int currentHp = _creature.CurrentHp;
            int maxHp = _creature.MaxHp;
            float hpPercent = maxHp > 0 ? (float)currentHp / maxHp : 0f;

            if (_hpText != null)
            {
                _hpText.text = $"{currentHp} / {maxHp}";
            }

            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = hpPercent;
                _hpFillImage.color = Color.Lerp(Color.red, Color.green, hpPercent);
            }

            if (_blockRoot != null)
            {
                bool hasBlock = _creature.Block > 0;
                _blockRoot.SetActive(hasBlock);

                if (hasBlock && _blockText != null)
                {
                    _blockText.text = _creature.Block.ToString();
                }
            }

            if (_powersRoot != null)
            {
                IReadOnlyList<PowerModel> powers = _creature.Powers;
                bool hasPowers = powers.Count > 0;
                _powersRoot.SetActive(hasPowers);

                if (hasPowers && _powersText != null)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < powers.Count; i++)
                    {
                        PowerModel power = powers[i];
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(power.Name);
                        sb.Append(": ");
                        sb.Append(power.Amount);
                    }

                    _powersText.text = sb.ToString();
                }
            }
        }
    }
}
