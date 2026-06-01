using System;
using Game.Core.Map;
using Game.Core.Rewards;
using Game.Core.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.RunFlow
{
    public sealed class PrototypeRunRoomPanel : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private Text _primaryLabel;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private Text _secondaryLabel;

        public event Action PrimaryClicked;
        public event Action SecondaryClicked;

        public void Configure(AbstractRoom room)
        {
            if (room == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            _titleText.text = room.RoomType.ToString();
            _bodyText.text = BuildDescription(room);
            SetPrimary("Continue", true);
            SetSecondary(string.Empty, false);

            if (room is EventRoomPlaceholder)
            {
                SetPrimary("Take 50 Gold / Lose 5 HP", true);
                SetSecondary("Skip", true);
            }
            else if (room is RestSiteRoomPlaceholder)
            {
                SetPrimary("Rest +20 HP", true);
                SetSecondary("Skip", true);
            }
            else if (room is ShopRoomPlaceholder)
            {
                SetPrimary("Leave Shop", true);
            }
            else if (room is TreasureRoom)
            {
                SetPrimary("Open Treasure", true);
            }
        }

        private string BuildDescription(AbstractRoom room)
        {
            return room.RoomType switch
            {
                RoomType.Combat => "Win the combat to get rewards.",
                RoomType.Boss => "Boss fight. Win to finish the run.",
                RoomType.Event => "Placeholder event room.",
                RoomType.RestSite => "Placeholder rest site.",
                RoomType.Shop => "Shop placeholder. No purchases yet.",
                RoomType.Treasure => "Treasure room with instant rewards.",
                _ => room.RoomType.ToString()
            };
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void Awake()
        {
            _primaryButton.onClick.AddListener(() => PrimaryClicked?.Invoke());
            _secondaryButton.onClick.AddListener(() => SecondaryClicked?.Invoke());
        }

        private void SetPrimary(string label, bool visible)
        {
            _primaryButton.gameObject.SetActive(visible);
            _primaryLabel.text = label;
        }

        private void SetSecondary(string label, bool visible)
        {
            _secondaryButton.gameObject.SetActive(visible);
            _secondaryLabel.text = label;
        }
    }
}
