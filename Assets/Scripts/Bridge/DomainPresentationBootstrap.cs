using Game.Content.Runtime;
using Game.Presentation.Runtime;
using UnityEngine;

namespace Game.Bridge
{
    /// <summary>
    /// Wires Content-layer run creation into the Presentation-layer DomainPresentationController.
    /// This bridge lives in Assembly-CSharp so it can reference both Game.Content and Game.Presentation
    /// without violating the assembly boundary rule (Presentation must not reference Content).
    /// Attach this to the same GameObject as DomainPresentationController, or assign the controller reference.
    /// </summary>
    public sealed class DomainPresentationBootstrap : MonoBehaviour
    {
        [SerializeField] private DomainPresentationController _controller;

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<DomainPresentationController>();
            }

            if (_controller != null)
            {
                _controller.CreateRunContext = seed => StarterContentRegistry.StartNewRun(seed);
            }
            else
            {
                Debug.LogError("[DomainPresentationBootstrap] No DomainPresentationController found.");
            }
        }
    }
}
