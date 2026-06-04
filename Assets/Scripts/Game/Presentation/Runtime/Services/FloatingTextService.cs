using System.Collections;
using UnityEngine;

namespace Game.Presentation.Services
{
    public sealed class FloatingTextService : MonoBehaviour, IFloatingTextService
    {
        [SerializeField] private GameObject _floatingTextPrefab;

        public void Show(Vector3 worldPosition, string text, Color color, float duration = 1f)
        {
            // Prototype: use UI world-space canvas or simple mesh text
            // For now, just log
            Debug.Log($"[FloatingText] {text} at {worldPosition}");
        }
    }
}
