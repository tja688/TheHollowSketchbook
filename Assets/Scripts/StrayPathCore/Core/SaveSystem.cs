// 整改: 2026-06-03 修复了 SaveSystem Dictionary 序列化问题 —— 使用 Newtonsoft.Json 替换 JsonUtility，并添加存档版本号机制
using System;
using System.IO;
using Newtonsoft.Json;
using StrayPathCore.Utils;
using UnityEngine;

namespace StrayPathCore.Core
{
    /// <summary>
    /// 存档系统 —— 替代 Godot 的 ConfigFile 加密持久化。
    /// 使用 Newtonsoft.Json 序列化（原生支持 Dictionary），支持可选 AES 加密。
    /// 分为 RunState（单局状态）与 AccountState（账户状态）两个独立文件。
    /// 引入 SaveFileHeader 存档版本号机制，支持未来兼容迁移。
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SaveDirectory = Application.persistentDataPath;
        private static readonly string RunSaveFile = Path.Combine(SaveDirectory, "run_save.json");
        private static readonly string AccountSaveFile = Path.Combine(SaveDirectory, "account_save.json");
        private static readonly string ConfigSaveFile = Path.Combine(SaveDirectory, "config_save.json");

        // 加密密钥（与原 Godot 项目对齐意图，防 casual tampering）
        private const string EncryptionKey = "GDSSTRDAL";
        private static readonly bool UseEncryption = false; // 开发阶段关闭，发布可开启

        // 当前存档格式版本号
        private const int CurrentSaveVersion = 1;

        // ==================== RunState ====================

        public static void SaveRunState(RunState state)
        {
            try
            {
                var wrapper = new SaveFileWrapper<RunState>
                {
                    Header = new SaveFileHeader
                    {
                        Version = CurrentSaveVersion,
                        SaveDate = DateTime.UtcNow.ToString("O"),
                        GameVersion = Application.version
                    },
                    Data = state
                };
                string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
                if (UseEncryption)
                    json = StringEncryption.Encrypt(json, EncryptionKey);
                File.WriteAllText(RunSaveFile, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 保存 RunState 失败: {ex.Message}");
            }
        }

        public static RunState LoadRunState()
        {
            try
            {
                if (!File.Exists(RunSaveFile))
                    return null;
                string json = File.ReadAllText(RunSaveFile);
                if (UseEncryption)
                    json = StringEncryption.Decrypt(json, EncryptionKey);
                var wrapper = JsonConvert.DeserializeObject<SaveFileWrapper<RunState>>(json);
                if (wrapper == null) return null;
                // 版本号检查与兼容迁移入口
                if (wrapper.Header != null && wrapper.Header.Version != CurrentSaveVersion)
                {
                    Debug.LogWarning($"[SaveSystem] 检测到旧版本存档 v{wrapper.Header.Version}，当前 v{CurrentSaveVersion}，执行兼容迁移。");
                    MigrateRunState(wrapper.Data, wrapper.Header.Version);
                }
                return wrapper.Data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 读取 RunState 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 存档版本兼容迁移入口。未来版本升级时在此扩展分支。
        /// </summary>
        private static void MigrateRunState(RunState state, int fromVersion)
        {
            if (state == null) return;
            // 示例：v1 -> v2 迁移逻辑可在此添加
            // if (fromVersion < 2) { /* 迁移字段 */ }
        }

        public static bool HasRunSave()
        {
            return File.Exists(RunSaveFile);
        }

        public static void DeleteRunSave()
        {
            try
            {
                if (File.Exists(RunSaveFile))
                    File.Delete(RunSaveFile);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 删除 RunState 失败: {ex.Message}");
            }
        }

        // ==================== AccountState ====================

        public static void SaveAccountState(AccountState state)
        {
            try
            {
                var wrapper = new SaveFileWrapper<AccountState>
                {
                    Header = new SaveFileHeader
                    {
                        Version = CurrentSaveVersion,
                        SaveDate = DateTime.UtcNow.ToString("O"),
                        GameVersion = Application.version
                    },
                    Data = state
                };
                string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
                if (UseEncryption)
                    json = StringEncryption.Encrypt(json, EncryptionKey);
                File.WriteAllText(AccountSaveFile, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 保存 AccountState 失败: {ex.Message}");
            }
        }

        public static AccountState LoadAccountState()
        {
            try
            {
                if (!File.Exists(AccountSaveFile))
                    return new AccountState();
                string json = File.ReadAllText(AccountSaveFile);
                if (UseEncryption)
                    json = StringEncryption.Decrypt(json, EncryptionKey);
                var wrapper = JsonConvert.DeserializeObject<SaveFileWrapper<AccountState>>(json);
                if (wrapper?.Data == null) return new AccountState();
                return wrapper.Data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 读取 AccountState 失败: {ex.Message}");
                return new AccountState();
            }
        }

        // ==================== Configuration ====================

        public static void SaveConfiguration(GameConfiguration config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigSaveFile, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 保存 Configuration 失败: {ex.Message}");
            }
        }

        public static GameConfiguration LoadConfiguration()
        {
            try
            {
                if (!File.Exists(ConfigSaveFile))
                    return new GameConfiguration();
                string json = File.ReadAllText(ConfigSaveFile);
                var config = JsonConvert.DeserializeObject<GameConfiguration>(json);
                return config ?? new GameConfiguration();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 读取 Configuration 失败: {ex.Message}");
                return new GameConfiguration();
            }
        }

        // ==================== 排行榜 ====================

        public static void SaveScoreboard(ScoreboardData data)
        {
            string path = Path.Combine(SaveDirectory, "scoreboard.json");
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 保存 Scoreboard 失败: {ex.Message}");
            }
        }

        public static ScoreboardData LoadScoreboard()
        {
            string path = Path.Combine(SaveDirectory, "scoreboard.json");
            try
            {
                if (!File.Exists(path))
                    return new ScoreboardData();
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ScoreboardData>(json) ?? new ScoreboardData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] 读取 Scoreboard 失败: {ex.Message}");
                return new ScoreboardData();
            }
        }
    }

    // ==================== 存档包装器（版本号 + 数据） ====================

    [Serializable]
    public class SaveFileHeader
    {
        public int Version = 1;
        public string SaveDate;
        public string GameVersion = "1.0";
    }

    [Serializable]
    public class SaveFileWrapper<T>
    {
        public SaveFileHeader Header;
        public T Data;
    }

    [Serializable]
    public class GameConfiguration
    {
        public int ResolutionIndex = 1;
        public bool VSync = true;
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.75f;
        public float SFXVolume = 0.75f;
        public string Language = "EN";
        public bool ShowRunTimer = false;
    }

    [Serializable]
    public class ScoreboardData
    {
        public System.Collections.Generic.List<ScoreEntry> Scores = new System.Collections.Generic.List<ScoreEntry>();
        public System.Collections.Generic.List<ScoreEntry> Runtimes = new System.Collections.Generic.List<ScoreEntry>();
    }
}
