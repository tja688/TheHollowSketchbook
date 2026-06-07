using System.Linq;
using Game.Presentation.Runtime;
using Game.Presentation.Runtime.Playtest;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Presentation.Editor
{
    public sealed class PresentationEffectWindow : OdinEditorWindow
    {
        [MenuItem("CardDungeon/Presentation/效果面板")]
        private static void OpenWindow()
        {
            var window = GetWindow<PresentationEffectWindow>();
            window.titleContent = new GUIContent("Presentation 效果面板");
            window.minSize = new Vector2(480f, 360f);
            window.Show();
        }

        private PresentationEffectPanel _effectPanel;
        private UnityEditor.Editor _cachedEditor;
        private Tab _toolbarTab;
        private DomainPresentationController _runtimeController;
        private Vector2 _configScrollPos;
        private Vector2 _debugScrollPos;

        private enum Tab
        {
            EffectConfig,
            RuntimeDebug
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _effectPanel = LoadOrCreateEffectPanel();
            RebuildCachedEditor();
        }

        private void OnDisable()
        {
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }
        }

        protected override void OnImGUI()
        {
            DrawToolbar();

            if (_toolbarTab == Tab.EffectConfig)
            {
                DrawEffectConfigTab();
            }
            else
            {
                DrawRuntimeDebugTab();
            }
        }

        private void DrawToolbar()
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            _toolbarTab = (Tab)GUILayout.SelectionGrid((int)_toolbarTab,
                new[] { "效果配置", "运行时调试" }, 2, SirenixGUIStyles.LeftAlignedCenteredLabel);

            if (GUILayout.Button("刷新", GUILayout.Width(50f)))
            {
                RefreshReferences();
            }

            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void DrawEffectConfigTab()
        {
            _configScrollPos = EditorGUILayout.BeginScrollView(_configScrollPos);

            if (_effectPanel == null)
            {
                EditorGUILayout.HelpBox("未找到 PresentationEffectPanel 资产。点击下方按钮创建。", MessageType.Warning);
                if (GUILayout.Button("创建 PresentationEffectPanel 资产"))
                {
                    _effectPanel = CreateEffectPanelAsset();
                    RebuildCachedEditor();
                }
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.HelpBox("修改后点击「保存资产」即可在下次 StartPresentation 时生效。", MessageType.Info);

            if (_cachedEditor != null)
            {
                _cachedEditor.OnInspectorGUI();
            }

            GUILayout.Space(8f);
            SirenixEditorGUI.BeginHorizontalToolbar();

            if (GUILayout.Button("保存资产"))
            {
                EditorUtility.SetDirty(_effectPanel);
                AssetDatabase.SaveAssets();
                Debug.Log("[PresentationEffectWindow] 效果面板资产已保存。");
            }

            if (GUILayout.Button("在 Project 中定位"))
            {
                EditorGUIUtility.PingObject(_effectPanel);
                Selection.activeObject = _effectPanel;
            }

            SirenixEditorGUI.EndHorizontalToolbar();
            EditorGUILayout.EndScrollView();
        }

        private void DrawRuntimeDebugTab()
        {
            _debugScrollPos = EditorGUILayout.BeginScrollView(_debugScrollPos);

            if (_runtimeController == null)
            {
                _runtimeController = Object.FindFirstObjectByType<DomainPresentationController>();
            }

            if (_runtimeController == null)
            {
                EditorGUILayout.HelpBox("场景中未找到 DomainPresentationController。请在场景中挂载后刷新。", MessageType.Info);
                if (GUILayout.Button("刷新"))
                {
                    _runtimeController = Object.FindFirstObjectByType<DomainPresentationController>();
                }
                EditorGUILayout.EndScrollView();
                return;
            }

            SirenixEditorGUI.BeginBox("Controller 状态");
            DrawReadOnlyField("对象名称", _runtimeController.name);
            DrawReadOnlyField("运行状态", _runtimeController.IsRunning ? "运行中" : "已停止");
            SirenixEditorGUI.EndBox();

            GUILayout.Space(8f);
            SirenixEditorGUI.BeginBox("快捷操作");

            SirenixEditorGUI.BeginHorizontalToolbar();
            if (GUILayout.Button("开始演出", GUILayout.Height(28f)))
            {
                _runtimeController.StartPresentation();
            }

            if (GUILayout.Button("重新查找 Controller", GUILayout.Height(28f)))
            {
                _runtimeController = Object.FindFirstObjectByType<DomainPresentationController>();
            }
            SirenixEditorGUI.EndHorizontalToolbar();

            SirenixEditorGUI.EndBox();

            GUILayout.Space(8f);
            SirenixEditorGUI.BeginBox("场景接线提示");
            EditorGUILayout.LabelField(
                "1. 在场景中创建空对象，挂载 DomainPresentationController。\n" +
                "2. 同一对象挂载 DomainPresentationBootstrap（自动注入 Content 依赖）。\n" +
                "3. 将 Effect Panel 资产拖入 Controller 的 Effect Panel 字段。\n" +
                "4. 配置 View Roots（网格根、详情面板、玩家面板等）。\n" +
                "5. Play 模式下自动启动，或右键 Inspector → Start Presentation。",
                SirenixGUIStyles.MultiLineLabel);
            SirenixEditorGUI.EndBox();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawReadOnlyField(string label, string value)
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            GUILayout.Label(label, GUILayout.Width(100f));
            GUILayout.Label(value, SirenixGUIStyles.LeftAlignedCenteredLabel);
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void RebuildCachedEditor()
        {
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }

            if (_effectPanel != null)
            {
                _cachedEditor = UnityEditor.Editor.CreateEditor(_effectPanel);
            }
        }

        private void RefreshReferences()
        {
            _effectPanel = LoadOrCreateEffectPanel();
            RebuildCachedEditor();
            _runtimeController = Object.FindFirstObjectByType<DomainPresentationController>();
            Repaint();
        }

        private static PresentationEffectPanel LoadOrCreateEffectPanel()
        {
            var panel = Resources.Load<PresentationEffectPanel>(PresentationEffectPanel.ResourcesPath);
            if (panel != null)
            {
                return panel;
            }

            string[] guids = AssetDatabase.FindAssets("t:PresentationEffectPanel");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                panel = AssetDatabase.LoadAssetAtPath<PresentationEffectPanel>(path);
            }

            return panel;
        }

        private static PresentationEffectPanel CreateEffectPanelAsset()
        {
            string folder = "Assets/Resources/Presentation";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Presentation");
            }

            string assetPath = PresentationEffectPanel.DefaultAssetPath;
            var panel = ScriptableObject.CreateInstance<PresentationEffectPanel>();
            AssetDatabase.CreateAsset(panel, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PresentationEffectWindow] 已创建效果面板资产：{assetPath}");
            return panel;
        }
    }
}
