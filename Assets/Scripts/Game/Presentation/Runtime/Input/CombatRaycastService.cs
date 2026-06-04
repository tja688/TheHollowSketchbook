using UnityEngine;
using UnityInput = UnityEngine.Input;
using Game.Presentation.Combat.Creatures;

namespace Game.Presentation.Input
{
    /// <summary>
    /// Raycast service for combat interactions.
    /// BOUNDARY: CardView-related raycasting removed (CardView deleted).
    /// Retained: EnemyView raycasting and play area hit detection.
    /// Extend this service to add FieldCardView raycasting for the grid-based system.
    /// </summary>
    public sealed class CombatRaycastService : MonoBehaviour
    {
        [SerializeField] private LayerMask _enemyLayerMask = ~0;
        [SerializeField] private LayerMask _playAreaLayerMask = ~0;
        [SerializeField] private float _maxRaycastDistance = 100f;

        public EnemyView RaycastEnemy()
        {
            if (Camera.main == null)
            {
                return null;
            }

            Ray ray = Camera.main.ScreenPointToRay(UnityInput.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _enemyLayerMask))
            {
                EnemyView enemy = hit.collider.GetComponent<EnemyView>();
                if (enemy != null)
                {
                    return enemy;
                }

                enemy = hit.collider.GetComponentInParent<EnemyView>();
                return enemy;
            }

            return null;
        }

        public bool RaycastPlayArea(out Vector3 hitPoint)
        {
            if (Camera.main == null)
            {
                hitPoint = Vector3.zero;
                return false;
            }

            Ray ray = Camera.main.ScreenPointToRay(UnityInput.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _playAreaLayerMask))
            {
                hitPoint = hit.point;
                return true;
            }

            hitPoint = Vector3.zero;
            return false;
        }
    }
}
