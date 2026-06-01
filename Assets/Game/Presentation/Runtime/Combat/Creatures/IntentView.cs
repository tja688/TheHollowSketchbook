using System;
using System.Threading.Tasks;
using Game.Core;
using Game.Core.Entities;
using Game.Presentation.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Combat.Creatures
{
    public sealed class IntentView : MonoBehaviour
    {
        [SerializeField] private Image _typeIcon;
        [SerializeField] private TextMeshProUGUI _damageText;
        [SerializeField] private TextMeshProUGUI _hitsText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
        }

        public void ShowIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                HideIntent();
                return;
            }

            gameObject.SetActive(true);

            if (_typeIcon != null)
            {
                _typeIcon.color = GetIntentColor(intent.Type);
            }

            if (_damageText != null)
            {
                bool isAttack = intent.Type == EnemyIntentType.Attack;
                _damageText.gameObject.SetActive(isAttack);
                if (isAttack)
                {
                    _damageText.text = intent.Damage.ToString();
                }
            }

            if (_hitsText != null)
            {
                bool showHits = intent.Type == EnemyIntentType.Attack && intent.Hits > 1;
                _hitsText.gameObject.SetActive(showHits);
                if (showHits)
                {
                    _hitsText.text = $"x{intent.Hits}";
                }
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = intent.Description ?? string.Empty;
            }

            GameServices.EnsureInitialized();
            _ = FadeInAsync();
        }

        public void HideIntent()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
        }

        private async Task FadeInAsync()
        {
            if (_canvasGroup == null || GameServices.Tween == null)
            {
                return;
            }

            _canvasGroup.alpha = 0f;
            await GameServices.Tween.FadeCanvasGroup(_canvasGroup, 1f, 0.3f);
        }

        private static Color GetIntentColor(EnemyIntentType type)
        {
            switch (type)
            {
                case EnemyIntentType.Attack:
                    return new Color(0.9f, 0.2f, 0.2f);
                case EnemyIntentType.Defend:
                    return new Color(0.2f, 0.4f, 0.9f);
                case EnemyIntentType.Buff:
                    return new Color(0.2f, 0.8f, 0.2f);
                case EnemyIntentType.Debuff:
                    return new Color(0.6f, 0.2f, 0.7f);
                default:
                    return Color.gray;
            }
        }
    }
}
