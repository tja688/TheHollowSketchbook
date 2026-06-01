using Game.Content;
using Game.Presentation.RunFlow;
using UnityEngine;

namespace Game.Presentation.Bootstrap
{
    public sealed class DebugCombatBootstrap : MonoBehaviour
    {
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _startOnPlay = true;
        [SerializeField] private bool _continueSavedRunIfPresent = true;

        private PrototypeRunController _runController;

        private void OnDestroy()
        {
            if (_runController != null)
            {
                Destroy(_runController.gameObject);
            }
        }

        private void Start()
        {
            if (_startOnPlay)
            {
                StartPrototypeRun();
            }
        }

        [ContextMenu("Start Prototype Run")]
        public void StartPrototypeRun()
        {
            StarterContentRegistry.RegisterAll();

            if (_runController != null)
            {
                Destroy(_runController.gameObject);
            }

            GameObject controllerGo = new GameObject("PrototypeRunController");
            _runController = controllerGo.AddComponent<PrototypeRunController>();
            _runController.StartPrototypeRun(_seed, _continueSavedRunIfPresent);
        }

        [ContextMenu("Clear Prototype Run Save")]
        public void ClearPrototypeRunSave()
        {
            new Game.Core.Saves.SaveManager(Application.persistentDataPath).DeleteCurrentRun();
        }
    }
}
