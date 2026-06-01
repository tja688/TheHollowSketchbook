using UnityEngine;

namespace Game.Presentation.Services
{
    public interface IFloatingTextService
    {
        void Show(Vector3 worldPosition, string text, Color color, float duration = 1f);
    }
}
