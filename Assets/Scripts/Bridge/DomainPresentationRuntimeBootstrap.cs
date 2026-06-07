using System;
using Game.Content.Runtime;
using Game.Presentation.Runtime;
using UnityEngine;

namespace Game.Bridge
{
    internal static class DomainPresentationRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePresentationController()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            DomainPresentationController controller = UnityEngine.Object.FindFirstObjectByType<DomainPresentationController>();
            if (controller != null)
            {
                controller.CreateRunContext ??= seed => StarterContentRegistry.StartNewRun(seed);
                return;
            }

            if (!LooksLikePrototypeScene())
            {
                return;
            }

            GameObject runtimeRoot = new GameObject("[Runtime] DomainPresentation");
            runtimeRoot.SetActive(false);
            DomainPresentationController runtimeController = runtimeRoot.AddComponent<DomainPresentationController>();
            runtimeController.CreateRunContext = seed => StarterContentRegistry.StartNewRun(seed);
            runtimeRoot.SetActive(true);
        }

        private static bool LooksLikePrototypeScene()
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.name, "UI", StringComparison.Ordinal)
                    || candidate.name.Contains("九宫场地格", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
