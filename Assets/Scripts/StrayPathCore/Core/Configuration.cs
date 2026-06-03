using UnityEngine;

namespace StrayPathCore.Core
{
    /// <summary>
    /// 游戏全局配置单例 —— 管理分辨率、音量、语言、显示设置等。
    /// 启动时从 SaveSystem 加载，运行时即时保存。
    /// </summary>
    public class Configuration : MonoBehaviour
    {
        public static Configuration Instance { get; private set; }

        public GameConfiguration Data { get; private set; } = new GameConfiguration();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
            ApplyAudioSettings();
        }

        public void Load()
        {
            Data = SaveSystem.LoadConfiguration();
        }

        public void Save()
        {
            SaveSystem.SaveConfiguration(Data);
        }

        // ==================== 分辨率与显示 ====================

        public void SetResolution(int index)
        {
            Data.ResolutionIndex = Mathf.Clamp(index, 1, 15);
            ApplyResolution();
            Save();
        }

        public void ApplyResolution()
        {
            // 1~15 映射到 (分辨率 × 窗口模式)
            // Windowed=1, ExclusiveFullscreen=2, Fullscreen=3
            // 分辨率: 1920x1080(1-3), 1680x1050(4-6), 1600x900(7-9), 1440x900(10-12), 1366x768(13-15)
            int[,] resolutions = {
                {1920, 1080}, {1680, 1050}, {1600, 900}, {1440, 900}, {1366, 768}
            };
            int mode = (Data.ResolutionIndex - 1) % 3; // 0=Windowed, 1=Exclusive, 2=Fullscreen
            int resIdx = (Data.ResolutionIndex - 1) / 3;
            int w = resolutions[resIdx, 0];
            int h = resolutions[resIdx, 1];
            FullScreenMode fsMode = mode switch
            {
                1 => FullScreenMode.ExclusiveFullScreen,
                2 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.Windowed
            };
            Screen.SetResolution(w, h, fsMode);
        }

        public void SetVSync(bool enabled)
        {
            Data.VSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            Save();
        }

        // ==================== 音频 ====================

        public void SetMasterVolume(float volume)
        {
            Data.MasterVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
            Save();
        }

        public void SetMusicVolume(float volume)
        {
            Data.MusicVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
            Save();
        }

        public void SetSFXVolume(float volume)
        {
            Data.SFXVolume = Mathf.Clamp01(volume);
            ApplyAudioSettings();
            Save();
        }

        public void ApplyAudioSettings()
        {
            AudioListener.volume = Data.MasterVolume;
            // 若使用 AudioMixer，可在此设置 Mixer 参数
        }

        public static float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        }

        // ==================== 语言 ====================

        public void SetLanguage(string lang)
        {
            Data.Language = lang;
            Save();
        }

        // ==================== 其他 ====================

        public void SetShowRunTimer(bool show)
        {
            Data.ShowRunTimer = show;
            Save();
        }
    }
}
