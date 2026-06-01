using UnityEngine;

namespace Game.Presentation.Services
{
    public static class GameServices
    {
        public static ITweenService Tween { get; private set; }
        public static IAudioService Audio { get; private set; }
        public static IVfxService Vfx { get; private set; }
        public static IFloatingTextService FloatingText { get; private set; }

        public static void Initialize(ITweenService tween, IAudioService audio, IVfxService vfx, IFloatingTextService floatingText)
        {
            Tween = tween;
            Audio = audio;
            Vfx = vfx;
            FloatingText = floatingText;
        }

        public static void EnsureInitialized()
        {
            if (Tween == null)
            {
                var host = new GameObject("[GameServices]");
                Object.DontDestroyOnLoad(host);
                var tween = host.AddComponent<CoroutineTweenService>();
                var audio = host.AddComponent<UnityAudioService>();
                var vfx = host.AddComponent<SimpleVfxService>();
                var floating = host.AddComponent<FloatingTextService>();
                Initialize(tween, audio, vfx, floating);
            }
        }
    }
}
