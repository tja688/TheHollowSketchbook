using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public sealed class CardDungeonRenderPipelineConsoleWindow : EditorWindow
{
    private const string MenuPath = "Tools/CardDungeon Rendering/项目渲染管线综合控制台";
    private const string RetroShaderName = "CardDungeon/RetroFakeLit";
    private const string PosterizeFeatureName = "RetroPosterizeThreshold";
    private const string CompositeFeatureName = "CardDungeon Retro Composite";

    [Serializable]
    private readonly struct IntOption
    {
        public readonly int value;
        public readonly string label;

        public IntOption(int value, string label)
        {
            this.value = value;
            this.label = label;
        }
    }

    [Serializable]
    private sealed class Phase05BatchState
    {
        public float lightWrap = 0f;
        public Color shadowColor = new Color(0.105f, 0.075f, 0.052f, 1f);
        public float ambientStrength = 0.18f;
        public Color specColor = new Color(0.20f, 0.15f, 0.09f, 1f);
        public float specStrength = 0.03f;
        public float specPower = 18f;
        public int rampSteps = 4;
        public float rampStrength = 0.28f;
        public Color fogColor = new Color(0.008f, 0.006f, 0.004f, 1f);
        public float fogStart = 2.2f;
        public float fogEnd = 5.5f;
    }

    private enum ConsolePage
    {
        Overview,
        RenderScale,
        RetroFakeLit,
        Phase07Posterize,
        RetroComposite
    }

    [SerializeField] private CardDungeonRenderPipelineConsoleConfig config;
    [SerializeField] private ConsolePage currentPage;
    [SerializeField] private Phase05BatchState phase05BatchState = new Phase05BatchState();

    private readonly List<Button> navButtons = new List<Button>();
    private ScrollView contentScroll;
    private VisualElement navigationRoot;

    [MenuItem(MenuPath)]
    public static void Open()
    {
        CardDungeonRenderPipelineConsoleWindow window = GetWindow<CardDungeonRenderPipelineConsoleWindow>();
        window.titleContent = new GUIContent("渲染控制台");
        window.minSize = new Vector2(1180f, 760f);
        window.Show();
    }

    private void OnEnable()
    {
        EnsureConfig();
        PullPhase05SharedValues();
    }

    public void CreateGUI()
    {
        EnsureConfig();
        rootVisualElement.Clear();
        rootVisualElement.style.flexGrow = 1f;
        rootVisualElement.style.backgroundColor = new Color(0.10f, 0.085f, 0.07f);

        rootVisualElement.Add(BuildHeader());
        rootVisualElement.Add(BuildToolbar());
        rootVisualElement.Add(BuildBody());

        RefreshPage();
    }

    private void EnsureConfig()
    {
        config = AssetDatabase.LoadAssetAtPath<CardDungeonRenderPipelineConsoleConfig>(CardDungeonRenderPipelineConsoleConfig.AssetPath);
        if (config != null)
        {
            return;
        }

        EnsureFolder("Assets/_Project/Editor");
        EnsureFolder("Assets/_Project/Editor/Rendering");

        config = CreateInstance<CardDungeonRenderPipelineConsoleConfig>();
        config.name = "CardDungeonRenderPipelineConsoleConfig";
        config.AutoPopulate();
        AssetDatabase.CreateAsset(config, CardDungeonRenderPipelineConsoleConfig.AssetPath);
        AssetDatabase.SaveAssets();
    }

    private VisualElement BuildHeader()
    {
        VisualElement root = new VisualElement();
        root.style.paddingLeft = 16;
        root.style.paddingRight = 16;
        root.style.paddingTop = 14;
        root.style.paddingBottom = 10;
        root.style.borderBottomWidth = 1;
        root.style.borderBottomColor = new Color(0.22f, 0.18f, 0.14f);
        root.style.backgroundColor = new Color(0.13f, 0.10f, 0.08f);

        Label title = new Label("项目渲染管线综合控制台");
        title.style.fontSize = 22;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.94f, 0.88f, 0.78f);
        root.Add(title);

        Label subtitle = new Label("统一管理当前已经落地的 URP 基线、RetroFakeLit、Phase 07 暗部阈值 LUT，以及 Retro Composite 全屏风格参数。这里改动会直接写回项目资产。" );
        subtitle.style.whiteSpace = WhiteSpace.Normal;
        subtitle.style.marginTop = 6;
        subtitle.style.color = new Color(0.78f, 0.72f, 0.66f);
        root.Add(subtitle);
        return root;
    }

    private VisualElement BuildToolbar()
    {
        Toolbar toolbar = new Toolbar();
        toolbar.style.height = 34;
        toolbar.style.paddingLeft = 8;
        toolbar.style.paddingRight = 8;

        toolbar.Add(MakeToolbarButton("保存全部", SaveAllAssets));
        toolbar.Add(MakeToolbarButton("重新读取资产", () =>
        {
            EnsureConfig();
            PullPhase05SharedValues();
            RefreshPage();
        }));
        toolbar.Add(MakeToolbarButton("打开配置资产", () => PingObject(config)));
        toolbar.Add(MakeToolbarButton("Ping URP 资产", () => PingObject(config != null ? config.highFidelityPipeline : null)));
        toolbar.Add(MakeToolbarButton("Ping Renderer 资产", () => PingObject(config != null ? config.highFidelityRenderer : null)));
        toolbar.Add(MakeToolbarButton("Ping Phase07 材质", () => PingObject(config != null ? config.retroPosterizeThresholdMaterial : null)));
        toolbar.Add(MakeToolbarButton("Ping Composite 材质", () => PingObject(config != null ? config.retroCompositeMaterial : null)));

        return toolbar;
    }

    private ToolbarButton MakeToolbarButton(string text, Action onClick)
    {
        ToolbarButton button = new ToolbarButton(onClick)
        {
            text = text
        };
        return button;
    }

    private VisualElement BuildBody()
    {
        TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        splitView.style.flexGrow = 1f;

        navigationRoot = new ScrollView();
        navigationRoot.style.backgroundColor = new Color(0.12f, 0.095f, 0.08f);
        navigationRoot.style.paddingLeft = 10;
        navigationRoot.style.paddingRight = 10;
        navigationRoot.style.paddingTop = 10;
        navigationRoot.style.paddingBottom = 10;
        BuildNavigation();

        contentScroll = new ScrollView();
        contentScroll.style.flexGrow = 1f;
        contentScroll.style.paddingLeft = 18;
        contentScroll.style.paddingRight = 18;
        contentScroll.style.paddingTop = 14;
        contentScroll.style.paddingBottom = 20;
        contentScroll.style.backgroundColor = new Color(0.09f, 0.075f, 0.06f);

        splitView.Add(navigationRoot);
        splitView.Add(contentScroll);
        return splitView;
    }

    private void BuildNavigation()
    {
        navigationRoot.Clear();
        navButtons.Clear();

        navigationRoot.Add(CreateNavButton(ConsolePage.Overview, "总览", "看当前是哪几层参数在主导画面，先定位问题。"));
        navigationRoot.Add(CreateNavButton(ConsolePage.RenderScale, "低清画布 / URP 基线", "Phase 03 / 09：控制内部画布分辨率、上采样和 URP 基础设置。"));
        navigationRoot.Add(CreateNavButton(ConsolePage.RetroFakeLit, "Phase 05 / RetroFakeLit", "批量调整普通物体假光照材质的共有参数。"));
        navigationRoot.Add(CreateNavButton(ConsolePage.Phase07Posterize, "Phase 07 / 暗部阈值 LUT", "直接验证屎黄色是不是这层后处理导致的。"));
        navigationRoot.Add(CreateNavButton(ConsolePage.RetroComposite, "Phase 08 / Retro Composite", "统一管理镜头、颗粒、暗角、量化、冷暖偏色等整体风格。"));
    }

    private Button CreateNavButton(ConsolePage page, string title, string description)
    {
        Button button = new Button(() =>
        {
            currentPage = page;
            RefreshPage();
        });
        button.style.height = StyleKeyword.Auto;
        button.style.paddingLeft = 10;
        button.style.paddingRight = 10;
        button.style.paddingTop = 10;
        button.style.paddingBottom = 10;
        button.style.marginBottom = 8;
        button.style.whiteSpace = WhiteSpace.Normal;
        button.style.unityTextAlign = TextAnchor.MiddleLeft;
        button.style.borderTopLeftRadius = 6;
        button.style.borderTopRightRadius = 6;
        button.style.borderBottomLeftRadius = 6;
        button.style.borderBottomRightRadius = 6;

        VisualElement textRoot = new VisualElement();
        textRoot.style.flexDirection = FlexDirection.Column;

        Label titleLabel = new Label(title);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 14;
        titleLabel.style.color = new Color(0.96f, 0.90f, 0.80f);
        textRoot.Add(titleLabel);

        Label descriptionLabel = new Label(description);
        descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
        descriptionLabel.style.fontSize = 11;
        descriptionLabel.style.marginTop = 4;
        descriptionLabel.style.color = new Color(0.77f, 0.72f, 0.67f);
        textRoot.Add(descriptionLabel);

        button.Add(textRoot);
        navButtons.Add(button);
        return button;
    }

    private void RefreshPage()
    {
        if (contentScroll == null)
        {
            return;
        }

        UpdateNavigationStyles();
        contentScroll.Clear();

        if (config == null)
        {
            contentScroll.Add(new HelpBox("控制台配置资产不存在，无法继续。", HelpBoxMessageType.Error));
            return;
        }

        switch (currentPage)
        {
            case ConsolePage.Overview:
                BuildOverviewPage();
                break;
            case ConsolePage.RenderScale:
                BuildRenderScalePage();
                break;
            case ConsolePage.RetroFakeLit:
                BuildRetroFakeLitPage();
                break;
            case ConsolePage.Phase07Posterize:
                BuildPhase07Page();
                break;
            case ConsolePage.RetroComposite:
                BuildRetroCompositePage();
                break;
        }
    }

    private void UpdateNavigationStyles()
    {
        for (int i = 0; i < navButtons.Count; i++)
        {
            bool selected = i == (int)currentPage;
            Button button = navButtons[i];
            button.style.backgroundColor = selected
                ? new Color(0.31f, 0.22f, 0.12f)
                : new Color(0.18f, 0.14f, 0.11f);
            button.style.borderLeftColor = selected ? new Color(0.86f, 0.64f, 0.28f) : new Color(0.20f, 0.16f, 0.13f);
            button.style.borderLeftWidth = 4;
        }
    }

    private void BuildOverviewPage()
    {
        AddPageTitle("总览", "先看当前到底是哪一层在主导画面。这个页面专门用来快速回答“是不是 07 导致的”这类问题。" );

        contentScroll.Add(CreateStatsGrid(new[]
        {
            CreateStatCard("Render Scale", GetUrpFloat("m_RenderScale").ToString("0.###"), "越低越像旧画布，越高越清晰。"),
            CreateStatCard("Upscaling", GetUpscalingLabel(GetUrpInt("m_UpscalingFilter")), "决定低清画布被放大时更偏像素还是更平滑。"),
            CreateStatCard("Phase07 LUT", GetMaterialTextureName(config.retroPosterizeThresholdMaterial, "_UserLut"), "暗部会优先被这张 LUT 染色。"),
            CreateStatCard("Phase07 Contribution", GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Contribution").ToString("0.00"), "越大，暗部统一风格越强。"),
            CreateStatCard("Composite Palette", GetMaterialFloat(config.retroCompositeMaterial, "_PaletteStrength").ToString("0.00"), "越大，暗部越会被压向固定调色板。"),
            CreateStatCard("RetroFakeLit 材质数", GetRetroFakeLitMaterials().Count.ToString(), "普通物体大部分都会吃这套假光照。"),
        }));

        bool posterizeActive = IsRendererFeatureActive(PosterizeFeatureName);
        bool compositeActive = IsRendererFeatureActive(CompositeFeatureName);
        string lutName = GetMaterialTextureName(config.retroPosterizeThresholdMaterial, "_UserLut");
        float contribution = GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Contribution");
        float threshold = GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Threshold");
        float paletteStrength = GetMaterialFloat(config.retroCompositeMaterial, "_PaletteStrength");
        Color warmTint = GetMaterialColor(config.retroCompositeMaterial, "_WarmTint");

        if (posterizeActive && compositeActive && lutName.Contains("DirtyBrown", StringComparison.OrdinalIgnoreCase) && contribution >= 0.75f && threshold >= 0.45f)
        {
            contentScroll.Add(new HelpBox(
                $"当前 07 和 Composite 都处于强风格工作状态：LUT={lutName}，Contribution={contribution:0.00}，Threshold={threshold:0.00}，Composite PaletteStrength={paletteStrength:0.00}。这套组合非常容易把暗部和中暗部统一成脏棕黄调，尤其 WarmTint 还是 {warmTint}. 如果你现在看到屎黄色，这一层高度可疑。",
                HelpBoxMessageType.Warning));
        }
        else
        {
            contentScroll.Add(new HelpBox("当前参数没有明显落在“脏棕强染色”最危险区间，但你仍然可以去 Phase 07 页面直接切 LUT、调 Contribution 和 Threshold 做排查。", HelpBoxMessageType.Info));
        }

        contentScroll.Add(CreateSectionCard("当前资产接线", "这里显示控制台正在直接操作哪些资产。后续新的渲染阶段也建议继续接到这张控制表。" , section =>
        {
            section.Add(CreateObjectRow("URP Asset", config.highFidelityPipeline, "当前高保真 URP 资产。改它会影响 render scale、HDR、深度贴图等渲染基线。"));
            section.Add(CreateObjectRow("Renderer Asset", config.highFidelityRenderer, "当前 Forward Renderer。07 和 Composite 的全屏 pass 都挂在这里。"));
            section.Add(CreateObjectRow("Phase07 材质", config.retroPosterizeThresholdMaterial, "控制暗部阈值 LUT。提高强度会更统一，但更容易发黄发脏。"));
            section.Add(CreateObjectRow("Composite 材质", config.retroCompositeMaterial, "控制镜头、量化、颗粒、暗角、冷暖偏色。"));
            section.Add(CreateObjectRow("RetroFakeLit 材质文件夹", config.retroFakeLitMaterialFolder, "普通物体材质集中在这里。Phase 05 页面会批量写回这些材质。"));
        }));

        contentScroll.Add(CreateSectionCard("快速排查建议", "如果你的目标只是先确认 07 有没有把画面染坏，按下面顺序测，最快。", section =>
        {
            section.Add(CreateChecklistLabel("1. 在 `Phase 07 / 暗部阈值 LUT` 页面先关掉 `RetroPosterizeThreshold` Feature，看屎黄色是否立刻减轻。"));
            section.Add(CreateChecklistLabel("2. 不关 Feature，只把 LUT 从 DirtyBrown 切到 DarkGreen 或 CandleRed，看偏色方向是否同步变化。"));
            section.Add(CreateChecklistLabel("3. 把 Contribution 从 0.85 降到 0.35，再把 Threshold 从 0.50 降到 0.35，看中亮区域是否不再被染色。"));
            section.Add(CreateChecklistLabel("4. 如果 07 关闭后仍然黄，再去 `Phase 08 / Retro Composite` 降低 WarmTint 和 PaletteStrength。"));
        }));
    }

    private void BuildRenderScalePage()
    {
        AddPageTitle("低清画布 / URP 基线", "这一页对应 Phase 03 和 Phase 09 的基础部分。它不决定“黄不黄”，但决定整个项目到底像旧游戏还是高清 3D。" );
        contentScroll.Add(new HelpBox("原则：先用 Render Scale 快速验证，再决定以后是否上固定 960×540 RT。数值越低，像素颗粒越明显，也更能掩盖资产瑕疵；但过低会直接吃掉文字和交互可读性。", HelpBoxMessageType.Info));

        contentScroll.Add(CreateButtonRow(new[]
        {
            ("执行计划推荐值", (Action)(() =>
            {
                SetUrpFloat("m_RenderScale", 0.5f, "Apply Render Scale Preset");
                SetUrpInt("m_UpscalingFilter", 2, "Apply Upscaling Preset");
                SetUrpBool("m_SupportsHDR", true, "Apply HDR Preset");
                SetUrpBool("m_RequireDepthTexture", true, "Apply Depth Preset");
                SetUrpBool("m_RequireOpaqueTexture", false, "Apply Opaque Preset");
                SetUrpInt("m_AdditionalLightsPerObjectLimit", 4, "Apply Light Limit Preset");
                SetUrpFloat("m_ShadowDistance", 18f, "Apply Shadow Distance Preset");
                RefreshPage();
            })),
            ("更脏 640×360 倾向", (Action)(() =>
            {
                SetUrpFloat("m_RenderScale", 0.333f, "Apply Dirty Render Scale");
                SetUrpInt("m_UpscalingFilter", 2, "Apply Dirty Upscaling");
                RefreshPage();
            })),
            ("更干净 0.75", (Action)(() =>
            {
                SetUrpFloat("m_RenderScale", 0.75f, "Apply Clean Render Scale");
                SetUrpInt("m_UpscalingFilter", 2, "Apply Clean Upscaling");
                RefreshPage();
            })),
        }));

        contentScroll.Add(CreateSectionCard("低清画布", "这组控制决定你的世界先被渲染成多粗糙，再被放大到屏幕上。", section =>
        {
            section.Add(CreateFloatControl(
                "Render Scale",
                "内部渲染分辨率比例。调大：更清晰、材质瑕疵更明显；调小：更复古、噪点/像素感更强，但 UI 和细节更容易糊。",
                0.25f, 1f,
                () => GetUrpFloat("m_RenderScale"),
                value => SetUrpFloat("m_RenderScale", value, "Change Render Scale")));

            section.Add(CreatePopupIntControl(
                "Upscaling Filter",
                "低清画布放大方式。Point 最像像素点；Linear 更平滑；FSR/Auto 会更现代。想要旧卡带味，通常优先 Point。注意你当前项目序列化里存在 raw=4，所以这里同时保留一个“当前项目值 4”选项，避免我替你瞎猜。",
                () => GetUrpInt("m_UpscalingFilter"),
                value => SetUrpInt("m_UpscalingFilter", value, "Change Upscaling Filter"),
                new[]
                {
                    new IntOption(0, "0 - Auto"),
                    new IntOption(1, "1 - Linear"),
                    new IntOption(2, "2 - Point"),
                    new IntOption(3, "3 - FSR"),
                    new IntOption(4, "4 - 当前项目值")
                }));
        }));

        contentScroll.Add(CreateSectionCard("URP 基线", "这组不是主风格，但会决定后处理是否好接、灯光是否过度现代。", section =>
        {
            section.Add(CreateToggleControl(
                "HDR",
                "开着更利于 Bloom 和高亮信息点。关掉后输出更硬，但发光物会更难做出层次。",
                () => GetUrpBool("m_SupportsHDR"),
                value => SetUrpBool("m_SupportsHDR", value, "Toggle HDR")));

            section.Add(CreateToggleControl(
                "Depth Texture",
                "很多边缘暗化和空间相关效果都依赖它。开：后续扩展方便；关：少一点开销，但很多效果不容易接。",
                () => GetUrpBool("m_RequireDepthTexture"),
                value => SetUrpBool("m_RequireDepthTexture", value, "Toggle Depth Texture")));

            section.Add(CreateToggleControl(
                "Opaque Texture",
                "如果没有特别依赖，一般保持关。开大多只是多一层抓屏成本，不会直接提升你现在这套风格。",
                () => GetUrpBool("m_RequireOpaqueTexture"),
                value => SetUrpBool("m_RequireOpaqueTexture", value, "Toggle Opaque Texture")));

            section.Add(CreateIntControl(
                "Additional Lights Per Object",
                "每个物体最多吃多少附加灯。调大：局部灯影响更丰富，但更容易现代、也更费；调小：更克制、更统一。",
                0, 8,
                () => GetUrpInt("m_AdditionalLightsPerObjectLimit"),
                value => SetUrpInt("m_AdditionalLightsPerObjectLimit", value, "Change Additional Lights Limit")));

            section.Add(CreateFloatControl(
                "Shadow Distance",
                "阴影绘制距离。调大：远处也有阴影，但更容易暴露现代 3D 感；调小：更像黑场吞细节。",
                0f, 50f,
                () => GetUrpFloat("m_ShadowDistance"),
                value => SetUrpFloat("m_ShadowDistance", value, "Change Shadow Distance")));
        }));
    }

    private void BuildRetroFakeLitPage()
    {
        AddPageTitle("Phase 05 / RetroFakeLit", "这一页批量改普通物体的共有假光照参数。注意：它不会改每个材质自己的底图和 BaseColor，只改共享风格参数。" );

        List<Material> materials = GetRetroFakeLitMaterials();
        contentScroll.Add(new HelpBox($"当前共找到 {materials.Count} 个 RetroFakeLit 材质。这个页面适合统一改阴影色、环境光、量化层数、雾和硬高光，不适合统一改每个物体自己的底色。", HelpBoxMessageType.Info));
        contentScroll.Add(CreateButtonRow(new[]
        {
            ("从现有材质回读一份", (Action)(() =>
            {
                PullPhase05SharedValues();
                RefreshPage();
            })),
            ("把当前共有参数写回全部材质", (Action)(() =>
            {
                ApplyPhase05SharedValues();
                RefreshPage();
            })),
            ("Ping 材质文件夹", (Action)(() => PingObject(config.retroFakeLitMaterialFolder))),
        }));

        contentScroll.Add(CreateSectionCard("共有光照参数", "这些参数几乎决定了“现代 PBR 味”会不会被压掉。", section =>
        {
            section.Add(CreateFloatControl(
                "Light Wrap",
                "让阴影边缘更包裹。调大：更柔、更像半 Lambert；调小：更硬、更木偶、更接近现在这套落地。",
                0f, 1f,
                () => phase05BatchState.lightWrap,
                value => phase05BatchState.lightWrap = value));

            section.Add(CreateColorControl(
                "Shadow Color",
                "阴影染色。越偏棕，画面越容易脏黄；越偏冷灰/冷绿，暗部会更阴森。",
                () => phase05BatchState.shadowColor,
                value => phase05BatchState.shadowColor = value));

            section.Add(CreateFloatControl(
                "Ambient Strength",
                "最低环境亮度。调大：阴影不那么死黑，但也更容易显脏；调小：对比更强，更像黑场吞细节。",
                0f, 1f,
                () => phase05BatchState.ambientStrength,
                value => phase05BatchState.ambientStrength = value));

            section.Add(CreateColorControl(
                "Spec Color",
                "硬高光颜色。偏暖会更像烛光打蜡木；偏冷会更像潮湿金属。别太亮，否则现代感会回来。",
                () => phase05BatchState.specColor,
                value => phase05BatchState.specColor = value));

            section.Add(CreateFloatControl(
                "Spec Strength",
                "高光强度。调大：更亮更现代；调小：更钝、更旧、更像纸板和木头。",
                0f, 0.2f,
                () => phase05BatchState.specStrength,
                value => phase05BatchState.specStrength = value));

            section.Add(CreateFloatControl(
                "Spec Power",
                "高光锐度。调大：高光更尖锐、更像塑料或抛光；调小：高光更宽更旧。",
                4f, 48f,
                () => phase05BatchState.specPower,
                value => phase05BatchState.specPower = value));
        }));

        contentScroll.Add(CreateSectionCard("量化和远景雾", "这组参数决定普通物体会不会被统一成“分层旧资产”。", section =>
        {
            section.Add(CreateIntControl(
                "Ramp Steps",
                "亮度分段数量。调小：更像低阶分层、风格更重；调大：更平滑、更接近正常 3D。",
                1, 8,
                () => phase05BatchState.rampSteps,
                value => phase05BatchState.rampSteps = value));

            section.Add(CreateFloatControl(
                "Ramp Strength",
                "分段影响强度。调大：亮度台阶更明显；调小：更多只是普通光照。",
                0f, 1f,
                () => phase05BatchState.rampStrength,
                value => phase05BatchState.rampStrength = value));

            section.Add(CreateColorControl(
                "Fog Color",
                "远景吞噬色。偏棕会更尘、更闷；偏黑灰会更压抑；偏绿会更阴冷。",
                () => phase05BatchState.fogColor,
                value => phase05BatchState.fogColor = value));

            section.Add(CreateFloatControl(
                "Fog Start",
                "从多远开始被雾吞。调小：近处就开始发脏发暗；调大：近景更清楚。",
                0f, 20f,
                () => phase05BatchState.fogStart,
                value => phase05BatchState.fogStart = value));

            section.Add(CreateFloatControl(
                "Fog End",
                "到多远几乎完全被雾吞没。调小：远景更早消失；调大：空间更通透。",
                0f, 30f,
                () => phase05BatchState.fogEnd,
                value => phase05BatchState.fogEnd = value));
        }));

        contentScroll.Add(CreateSectionCard("当前材质列表（前 12 个）", "这里只是给你确认批量写回范围，不在这里逐个改。", section =>
        {
            foreach (Material material in materials.Take(12))
            {
                section.Add(CreateTinyPathLabel(AssetDatabase.GetAssetPath(material)));
            }

            if (materials.Count > 12)
            {
                section.Add(CreateTinyPathLabel($"… 其余 {materials.Count - 12} 个材质已省略"));
            }
        }));
    }

    private void BuildPhase07Page()
    {
        AddPageTitle("Phase 07 / 暗部阈值 LUT", "这就是你当前最该看的页面。它的职责是：只把暗部往统一调色板压，亮部尽量保留可读性。参数太重时，最容易把整个场景烘成脏棕黄。" );

        contentScroll.Add(CreateButtonRow(new[]
        {
            ("应用执行计划默认值", (Action)(() =>
            {
                SetPhase07Defaults();
                RefreshPage();
            })),
            ("切 LUT：DirtyBrown", (Action)(() =>
            {
                SetMaterialTexture(config.retroPosterizeThresholdMaterial, "_UserLut", config.dirtyBrownLut, "Set DirtyBrown LUT");
                RefreshPage();
            })),
            ("切 LUT：DarkGreen", (Action)(() =>
            {
                SetMaterialTexture(config.retroPosterizeThresholdMaterial, "_UserLut", config.darkGreenLut, "Set DarkGreen LUT");
                RefreshPage();
            })),
            ("切 LUT：CandleRed", (Action)(() =>
            {
                SetMaterialTexture(config.retroPosterizeThresholdMaterial, "_UserLut", config.candleRedLut, "Set CandleRed LUT");
                RefreshPage();
            })),
        }));

        contentScroll.Add(CreateSectionCard("Pass 开关", "先别急着调数值。先确定是不是这层在作祟。", section =>
        {
            section.Add(CreateToggleControl(
                "RetroPosterizeThreshold Feature",
                "总开关。关掉：07 完全不参与；打开：暗部会按阈值走 LUT。排查偏色时，这是第一开关。",
                () => IsRendererFeatureActive(PosterizeFeatureName),
                value => SetRendererFeatureActive(PosterizeFeatureName, value)));

            section.Add(CreateObjectRow("Phase07 材质", config.retroPosterizeThresholdMaterial, "这里就是 pass 直接使用的材质资产。"));
        }));

        contentScroll.Add(CreateSectionCard("核心参数", "这组参数决定有多少区域被染色、染得多重、染成什么。", section =>
        {
            section.Add(CreateObjectFieldControl(
                "User LUT",
                "暗部参考调色板。DirtyBrown 更容易把画面推向脏棕黄；DarkGreen 更阴冷；CandleRed 更偏血色烛光。",
                typeof(Texture2D),
                () => config.retroPosterizeThresholdMaterial.GetTexture("_UserLut"),
                value => SetMaterialTexture(config.retroPosterizeThresholdMaterial, "_UserLut", value as Texture2D, "Change Phase07 LUT")));

            section.Add(CreateFloatControl(
                "Contribution",
                "总影响强度。调大：LUT 更硬地接管暗部；调小：只做轻微统一。这个值过大最容易直接把场景染黄。",
                0f, 1f,
                () => GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Contribution"),
                value => SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Contribution", value, "Change Phase07 Contribution")));

            section.Add(CreateFloatControl(
                "Threshold",
                "阈值。调大：更多中亮区域也会被当成“暗部”处理；调小：只有更黑的地方受影响。它是判断“污染范围”的关键。",
                0f, 1f,
                () => GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Threshold"),
                value => SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Threshold", value, "Change Phase07 Threshold")));

            section.Add(CreateFloatControl(
                "Threshold Sharpness",
                "阈值边界硬度。调大：亮暗切换更突然、更像硬掐色；调小：过渡更柔。太高会让脏色边界特别明显。",
                1f, 24f,
                () => GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_ThresholdSharpness"),
                value => SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_ThresholdSharpness", value, "Change Phase07 Sharpness")));

            section.Add(CreateFloatControl(
                "LUT Strength",
                "LUT 自己的颜色替换强度。调大：更忠于 LUT；调小：更保留原始颜色。想只保留明暗统一、少一点偏色时，可以先降它。",
                0f, 1f,
                () => GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_LutStrength"),
                value => SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_LutStrength", value, "Change Phase07 LUT Strength")));
        }));

        contentScroll.Add(CreateSectionCard("Debug 模式", "调 07 时强烈建议先开 Compare，再开 Mask。", section =>
        {
            section.Add(CreateFloatControl(
                "Compare Debug",
                "左半原图、右半处理后。调到 1 可以直接看 07 到底改变了什么；保持 0 则正常输出。",
                0f, 1f,
                () => GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_CompareDebug"),
                value => SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_CompareDebug", value, "Change Phase07 Compare Debug")));

            section.Add(CreateFloatControl(
                "Debug Mask",
                "显示哪些区域被 07 影响。调到 1 后，白得越亮，说明那一块越被 LUT 接管。",
                0f, 1f,
                () => GetMaterialFloat(config.retroPosterizeThresholdMaterial, "_DebugMask"),
                value => SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_DebugMask", value, "Change Phase07 Debug Mask")));
        }));
    }

    private void BuildRetroCompositePage()
    {
        AddPageTitle("Phase 08 / Retro Composite", "这层是调味料，不是主菜。它负责镜头损坏、颗粒、量化、调色板、暗角和最终复古输出。如果 07 没把画面搞黄，这里通常是第二嫌疑人。" );

        contentScroll.Add(CreateButtonRow(new[]
        {
            ("应用当前落地默认值", (Action)(() =>
            {
                SetCompositeDefaults();
                RefreshPage();
            })),
            ("降低暖色污染", (Action)(() =>
            {
                SetMaterialFloat(config.retroCompositeMaterial, "_PaletteStrength", 0.18f, "Reduce Palette Strength");
                SetMaterialColor(config.retroCompositeMaterial, "_WarmTint", new Color(1.02f, 0.94f, 0.84f, 1f), "Reduce Warm Tint");
                RefreshPage();
            })),
            ("更脏更旧", (Action)(() =>
            {
                SetMaterialFloat(config.retroCompositeMaterial, "_NoiseStrength", 0.06f, "Increase Noise Strength");
                SetMaterialFloat(config.retroCompositeMaterial, "_VignetteStrength", 0.75f, "Increase Vignette Strength");
                SetMaterialFloat(config.retroCompositeMaterial, "_DitherStrength", 0.06f, "Increase Dither Strength");
                RefreshPage();
            })),
        }));

        contentScroll.Add(CreateSectionCard("Pass 开关", "如果 07 关掉以后还是黄，先关这一层再看。", section =>
        {
            section.Add(CreateToggleControl(
                "CardDungeon Retro Composite Feature",
                "总开关。关掉：镜头、暗角、量化、冷暖偏色全部不参与；打开：完整吃这层复古调味。",
                () => IsRendererFeatureActive(CompositeFeatureName),
                value => SetRendererFeatureActive(CompositeFeatureName, value)));

            section.Add(CreateObjectRow("Composite 材质", config.retroCompositeMaterial, "Renderer Feature 直接引用的材质。"));
        }));

        contentScroll.Add(CreateSectionCard("低清输出", "这组控制后处理里自己的虚拟画布和像素对齐。", section =>
        {
            section.Add(CreateIntMaterialControl(
                "Virtual Width",
                "虚拟宽度。调小：颗粒更粗、更旧；调大：更清晰。960 是当前主档。",
                config.retroCompositeMaterial, "_VirtualWidth", 320, 1920));

            section.Add(CreateIntMaterialControl(
                "Virtual Height",
                "虚拟高度。和宽度一起决定最终像素颗粒大小。540 对应 16:9 的 960×540。",
                config.retroCompositeMaterial, "_VirtualHeight", 180, 1080));

            section.Add(CreateFloatControl(
                "Pixelate",
                "像素对齐强度。1 = 强制按虚拟像素采样；0 = 基本保留原始采样。越大越有旧输出感。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_Pixelate"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_Pixelate", value, "Change Pixelate")));
        }));

        contentScroll.Add(CreateSectionCard("色调与量化", "如果画面整体偏黄、偏脏、偏死，这组优先看。", section =>
        {
            section.Add(CreateFloatControl(
                "Posterize Levels",
                "亮度层级数量。调小：层次更断、更像低位色；调大：更平滑。",
                2f, 16f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_PosterizeLevels"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_PosterizeLevels", value, "Change Posterize Levels")));

            section.Add(CreateFloatControl(
                "Posterize Strength",
                "量化影响强度。调大：亮暗分段更明显；调小：只保留轻微压缩味。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_PosterizeStrength"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_PosterizeStrength", value, "Change Posterize Strength")));

            section.Add(CreateFloatControl(
                "Palette Strength",
                "暗部调色板吸附强度。调大：暗部更统一，但也更容易全场发脏发黄；调小：保留更多原色。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_PaletteStrength"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_PaletteStrength", value, "Change Palette Strength")));

            section.Add(CreateFloatControl(
                "Palette Dark Threshold",
                "多暗以下开始往固定调色板吸。调大：更多区域被统一；调小：只抓最暗地方。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_PaletteDarkThreshold"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_PaletteDarkThreshold", value, "Change Palette Dark Threshold")));

            section.Add(CreateFloatControl(
                "Black Crush",
                "黑位压缩。调大：阴影更快掉进黑里；调小：暗部层次更多。过大容易让画面既脏又闷。",
                0f, 0.5f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_BlackCrush"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_BlackCrush", value, "Change Black Crush")));

            section.Add(CreateFloatControl(
                "Contrast",
                "整体反差。调大：更硬、更戏剧化；调小：更平、更灰。",
                0.5f, 2f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_Contrast"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_Contrast", value, "Change Contrast")));

            section.Add(CreateFloatControl(
                "Saturation",
                "整体饱和度。调大：颜色更跳；调小：更灰更旧。太高会让低清复古感被冲掉。",
                0f, 2f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_Saturation"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_Saturation", value, "Change Saturation")));

            section.Add(CreateColorControl(
                "Warm Tint",
                "亮部暖色乘色。越偏黄，烛光味越重，也越可能把你不想要的黄感抬出来。",
                () => GetMaterialColor(config.retroCompositeMaterial, "_WarmTint"),
                value => SetMaterialColor(config.retroCompositeMaterial, "_WarmTint", value, "Change Warm Tint")));

            section.Add(CreateColorControl(
                "Cold Tint",
                "暗部冷色乘色。更偏绿蓝会更阴森，也能部分对冲暖黄污染。",
                () => GetMaterialColor(config.retroCompositeMaterial, "_ColdTint"),
                value => SetMaterialColor(config.retroCompositeMaterial, "_ColdTint", value, "Change Cold Tint")));
        }));

        contentScroll.Add(CreateSectionCard("镜头 / CRT / 输出脏感", "这些效果应该轻，不然就会从“统一气质”变成“特效抢戏”。", section =>
        {
            section.Add(CreateFloatControl(
                "Chromatic Aberration",
                "色散。调大：边缘彩边更明显；调小：更稳更克制。太大最容易显廉价。",
                0f, 4f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_ChromaticAberration"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_ChromaticAberration", value, "Change Chromatic Aberration")));

            section.Add(CreateFloatControl(
                "CRT Curvature",
                "屏幕弯曲。调大：边缘更鼓、更旧电视；调小：更平直。",
                0f, 0.25f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_CrtCurvature"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_CrtCurvature", value, "Change CRT Curvature")));

            section.Add(CreateFloatControl(
                "CRT Edge Softness",
                "边缘压暗软化。调大：四边更像老屏幕边缘；调小：边界更直接。",
                0f, 0.25f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_CrtEdgeSoftness"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_CrtEdgeSoftness", value, "Change CRT Edge Softness")));

            section.Add(CreateFloatControl(
                "CRT Glow Bleed",
                "亮色横向轻微晕染。调大：高亮更糊、更像旧屏；调小：更干净。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_CrtGlowBleed"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_CrtGlowBleed", value, "Change CRT Glow Bleed")));

            section.Add(CreateFloatControl(
                "Horizontal Jitter",
                "横向轻微抖动。调大：更像不稳定旧输出；调小：更稳。太大会影响阅读。",
                0f, 4f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_HorizontalJitter"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_HorizontalJitter", value, "Change Horizontal Jitter")));

            section.Add(CreateFloatControl(
                "Scanline Strength",
                "扫描线。调大：条纹更明显；调小：更接近你设计里“不要强扫描线”的方向。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_ScanlineStrength"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_ScanlineStrength", value, "Change Scanline Strength")));

            section.Add(CreateFloatControl(
                "Noise Strength",
                "暗部噪声。调大：更脏更粗糙；调小：更平净。太大就不是颗粒，是雪花。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_NoiseStrength"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_NoiseStrength", value, "Change Noise Strength")));

            section.Add(CreateFloatControl(
                "Dither Strength",
                "最终 1/255 抖动强度。调大：量化颗粒更明显；调小：色带更平。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_DitherStrength"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_DitherStrength", value, "Change Dither Strength")));

            section.Add(CreateFloatControl(
                "Vignette Strength",
                "暗角强度。调大：边缘更黑、更聚焦中心；调小：画面更开阔。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_VignetteStrength"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_VignetteStrength", value, "Change Vignette Strength")));

            section.Add(CreateFloatControl(
                "Vignette Radius",
                "暗角开始的位置。调小：更早压暗；调大：只压最边缘。",
                0f, 1f,
                () => GetMaterialFloat(config.retroCompositeMaterial, "_VignetteRadius"),
                value => SetMaterialFloat(config.retroCompositeMaterial, "_VignetteRadius", value, "Change Vignette Radius")));
        }));
    }

    private void AddPageTitle(string title, string description)
    {
        contentScroll.Add(CreateTitleLabel(title, 24, true, new Color(0.95f, 0.90f, 0.80f)));
        Label body = new Label(description);
        body.style.whiteSpace = WhiteSpace.Normal;
        body.style.color = new Color(0.80f, 0.74f, 0.67f);
        body.style.marginBottom = 14;
        contentScroll.Add(body);
    }

    private VisualElement CreateStatsGrid(IEnumerable<VisualElement> cards)
    {
        VisualElement grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.marginBottom = 16;
        foreach (VisualElement card in cards)
        {
            grid.Add(card);
        }

        return grid;
    }

    private VisualElement CreateStatCard(string title, string value, string description)
    {
        VisualElement card = new VisualElement();
        card.style.width = 250;
        card.style.marginRight = 10;
        card.style.marginBottom = 10;
        card.style.paddingLeft = 12;
        card.style.paddingRight = 12;
        card.style.paddingTop = 10;
        card.style.paddingBottom = 10;
        card.style.backgroundColor = new Color(0.16f, 0.12f, 0.09f);
        card.style.borderTopLeftRadius = 8;
        card.style.borderTopRightRadius = 8;
        card.style.borderBottomLeftRadius = 8;
        card.style.borderBottomRightRadius = 8;
        card.style.borderLeftWidth = 3;
        card.style.borderLeftColor = new Color(0.83f, 0.62f, 0.26f);

        card.Add(CreateTitleLabel(title, 12, true, new Color(0.96f, 0.90f, 0.80f)));
        card.Add(CreateTitleLabel(value, 20, true, new Color(0.94f, 0.75f, 0.40f)));

        Label desc = new Label(description);
        desc.style.whiteSpace = WhiteSpace.Normal;
        desc.style.fontSize = 11;
        desc.style.color = new Color(0.77f, 0.72f, 0.67f);
        card.Add(desc);
        return card;
    }

    private VisualElement CreateSectionCard(string title, string description, Action<VisualElement> buildContent)
    {
        Foldout foldout = new Foldout
        {
            text = title,
            value = true
        };
        foldout.style.marginBottom = 12;
        foldout.style.paddingTop = 8;
        foldout.style.paddingBottom = 8;
        foldout.style.paddingLeft = 10;
        foldout.style.paddingRight = 10;
        foldout.style.backgroundColor = new Color(0.15f, 0.12f, 0.095f);
        foldout.style.borderTopLeftRadius = 8;
        foldout.style.borderTopRightRadius = 8;
        foldout.style.borderBottomLeftRadius = 8;
        foldout.style.borderBottomRightRadius = 8;
        foldout.style.borderLeftWidth = 3;
        foldout.style.borderLeftColor = new Color(0.56f, 0.40f, 0.18f);

        Label desc = new Label(description);
        desc.style.whiteSpace = WhiteSpace.Normal;
        desc.style.marginLeft = 4;
        desc.style.marginTop = 6;
        desc.style.marginBottom = 8;
        desc.style.color = new Color(0.79f, 0.74f, 0.69f);
        foldout.Add(desc);

        VisualElement inner = new VisualElement();
        inner.style.flexDirection = FlexDirection.Column;
        buildContent(inner);
        foldout.Add(inner);
        return foldout;
    }

    private VisualElement CreateObjectRow(string label, UnityEngine.Object value, string description)
    {
        ObjectField field = new ObjectField(label)
        {
            objectType = value != null ? value.GetType() : typeof(UnityEngine.Object),
            value = value
        };
        field.SetEnabled(false);
        return WrapControl(label, description, field);
    }

    private VisualElement CreateObjectFieldControl(string label, string description, Type objectType, Func<UnityEngine.Object> getter, Action<UnityEngine.Object> setter)
    {
        ObjectField field = new ObjectField
        {
            objectType = objectType,
            value = getter()
        };

        field.RegisterValueChangedCallback(evt => setter(evt.newValue));
        return WrapControl(label, description, field);
    }

    private VisualElement CreateFloatControl(string label, string description, float min, float max, Func<float> getter, Action<float> setter)
    {
        VisualElement fieldRoot = new VisualElement();
        fieldRoot.style.flexDirection = FlexDirection.Row;
        fieldRoot.style.alignItems = Align.Center;

        Slider slider = new Slider(min, max)
        {
            value = getter()
        };
        slider.style.flexGrow = 1f;
        slider.style.marginRight = 8;

        FloatField floatField = new FloatField
        {
            value = getter()
        };
        floatField.style.width = 90;

        bool mute = false;
        slider.RegisterValueChangedCallback(evt =>
        {
            if (mute) return;
            mute = true;
            floatField.SetValueWithoutNotify(evt.newValue);
            setter(evt.newValue);
            mute = false;
        });

        floatField.RegisterValueChangedCallback(evt =>
        {
            if (mute) return;
            float clamped = Mathf.Clamp(evt.newValue, min, max);
            mute = true;
            slider.SetValueWithoutNotify(clamped);
            floatField.SetValueWithoutNotify(clamped);
            setter(clamped);
            mute = false;
        });

        fieldRoot.Add(slider);
        fieldRoot.Add(floatField);
        return WrapControl(label, description, fieldRoot);
    }

    private VisualElement CreateIntControl(string label, string description, int min, int max, Func<int> getter, Action<int> setter)
    {
        VisualElement fieldRoot = new VisualElement();
        fieldRoot.style.flexDirection = FlexDirection.Row;
        fieldRoot.style.alignItems = Align.Center;

        SliderInt slider = new SliderInt(min, max)
        {
            value = getter()
        };
        slider.style.flexGrow = 1f;
        slider.style.marginRight = 8;

        IntegerField intField = new IntegerField
        {
            value = getter()
        };
        intField.style.width = 90;

        bool mute = false;
        slider.RegisterValueChangedCallback(evt =>
        {
            if (mute) return;
            mute = true;
            intField.SetValueWithoutNotify(evt.newValue);
            setter(evt.newValue);
            mute = false;
        });

        intField.RegisterValueChangedCallback(evt =>
        {
            if (mute) return;
            int clamped = Mathf.Clamp(evt.newValue, min, max);
            mute = true;
            slider.SetValueWithoutNotify(clamped);
            intField.SetValueWithoutNotify(clamped);
            setter(clamped);
            mute = false;
        });

        fieldRoot.Add(slider);
        fieldRoot.Add(intField);
        return WrapControl(label, description, fieldRoot);
    }

    private VisualElement CreateColorControl(string label, string description, Func<Color> getter, Action<Color> setter)
    {
        ColorField field = new ColorField
        {
            value = getter(),
            showAlpha = true
        };
        field.RegisterValueChangedCallback(evt => setter(evt.newValue));
        return WrapControl(label, description, field);
    }

    private VisualElement CreateToggleControl(string label, string description, Func<bool> getter, Action<bool> setter)
    {
        Toggle toggle = new Toggle
        {
            value = getter()
        };
        toggle.RegisterValueChangedCallback(evt => setter(evt.newValue));
        return WrapControl(label, description, toggle);
    }

    private VisualElement CreatePopupIntControl(string label, string description, Func<int> getter, Action<int> setter, IReadOnlyList<IntOption> options)
    {
        List<string> labels = options.Select(option => option.label).ToList();
        int currentValue = getter();
        int selectedIndex = 0;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].value == currentValue)
            {
                selectedIndex = i;
                break;
            }
        }

        PopupField<string> field = new PopupField<string>(labels, selectedIndex);
        field.RegisterValueChangedCallback(evt =>
        {
            int index = labels.IndexOf(evt.newValue);
            if (index >= 0)
            {
                setter(options[index].value);
            }
        });
        return WrapControl(label, description, field);
    }

    private VisualElement CreateEnumControl<TEnum>(string label, string description, Func<TEnum> getter, Action<TEnum> setter) where TEnum : Enum
    {
        EnumField field = new EnumField(getter());
        field.Init(getter());
        field.RegisterValueChangedCallback(evt => setter((TEnum)evt.newValue));
        return WrapControl(label, description, field);
    }

    private VisualElement CreateIntMaterialControl(string label, string description, Material material, string propertyName, int min, int max)
    {
        return CreateIntControl(label, description, min, max,
            () => Mathf.RoundToInt(GetMaterialFloat(material, propertyName)),
            value => SetMaterialFloat(material, propertyName, value, $"Change {propertyName}"));
    }

    private VisualElement WrapControl(string label, string description, VisualElement field)
    {
        VisualElement root = new VisualElement();
        root.style.paddingTop = 8;
        root.style.paddingBottom = 8;
        root.style.marginBottom = 4;
        root.style.borderBottomWidth = 1;
        root.style.borderBottomColor = new Color(0.22f, 0.18f, 0.14f);

        root.Add(CreateTitleLabel(label, 13, true, new Color(0.95f, 0.90f, 0.80f)));

        Label desc = new Label(description);
        desc.style.whiteSpace = WhiteSpace.Normal;
        desc.style.fontSize = 11;
        desc.style.color = new Color(0.79f, 0.73f, 0.67f);
        desc.style.marginTop = 3;
        desc.style.marginBottom = 6;
        root.Add(desc);

        root.Add(field);
        return root;
    }

    private Label CreateTitleLabel(string text, int fontSize, bool bold, Color color)
    {
        Label label = new Label(text);
        label.style.fontSize = fontSize;
        label.style.color = color;
        if (bold)
        {
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        return label;
    }

    private VisualElement CreateChecklistLabel(string text)
    {
        Label label = new Label("• " + text);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.marginBottom = 6;
        label.style.color = new Color(0.83f, 0.78f, 0.72f);
        return label;
    }

    private VisualElement CreateTinyPathLabel(string path)
    {
        Label label = new Label(path);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.fontSize = 11;
        label.style.marginBottom = 4;
        label.style.color = new Color(0.73f, 0.70f, 0.66f);
        return label;
    }

    private VisualElement CreateButtonRow((string label, Action action)[] buttons)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexWrap = Wrap.Wrap;
        row.style.marginBottom = 12;

        foreach ((string label, Action action) buttonInfo in buttons)
        {
            Button button = new Button(buttonInfo.action)
            {
                text = buttonInfo.label
            };
            button.style.marginRight = 8;
            button.style.marginBottom = 8;
            button.style.height = 28;
            row.Add(button);
        }

        return row;
    }

    private static string GetUpscalingLabel(int rawValue)
    {
        return rawValue switch
        {
            0 => "Auto",
            1 => "Linear",
            2 => "Point",
            3 => "FSR",
            4 => "当前项目值(4)",
            _ => $"Unknown({rawValue})"
        };
    }

    private float GetMaterialFloat(Material material, string propertyName)
    {
        return material != null && material.HasProperty(propertyName) ? material.GetFloat(propertyName) : 0f;
    }

    private Color GetMaterialColor(Material material, string propertyName)
    {
        return material != null && material.HasProperty(propertyName) ? material.GetColor(propertyName) : Color.white;
    }

    private string GetMaterialTextureName(Material material, string propertyName)
    {
        Texture texture = material != null && material.HasProperty(propertyName) ? material.GetTexture(propertyName) : null;
        return texture != null ? texture.name : "<none>";
    }

    private void SetMaterialFloat(Material material, string propertyName, float value, string undoName)
    {
        if (material == null || !material.HasProperty(propertyName))
        {
            return;
        }

        Undo.RecordObject(material, undoName);
        material.SetFloat(propertyName, value);
        EditorUtility.SetDirty(material);
        RepaintViews();
    }

    private void SetMaterialColor(Material material, string propertyName, Color value, string undoName)
    {
        if (material == null || !material.HasProperty(propertyName))
        {
            return;
        }

        Undo.RecordObject(material, undoName);
        material.SetColor(propertyName, value);
        EditorUtility.SetDirty(material);
        RepaintViews();
    }

    private void SetMaterialTexture(Material material, string propertyName, Texture value, string undoName)
    {
        if (material == null || !material.HasProperty(propertyName))
        {
            return;
        }

        Undo.RecordObject(material, undoName);
        material.SetTexture(propertyName, value);
        EditorUtility.SetDirty(material);
        RepaintViews();
    }

    private float GetUrpFloat(string propertyPath)
    {
        SerializedObject so = new SerializedObject(config.highFidelityPipeline);
        return so.FindProperty(propertyPath).floatValue;
    }

    private int GetUrpInt(string propertyPath)
    {
        SerializedObject so = new SerializedObject(config.highFidelityPipeline);
        return so.FindProperty(propertyPath).intValue;
    }

    private bool GetUrpBool(string propertyPath)
    {
        SerializedObject so = new SerializedObject(config.highFidelityPipeline);
        return so.FindProperty(propertyPath).boolValue;
    }

    private void SetUrpFloat(string propertyPath, float value, string undoName)
    {
        SerializedObject so = new SerializedObject(config.highFidelityPipeline);
        SerializedProperty property = so.FindProperty(propertyPath);
        Undo.RecordObject(config.highFidelityPipeline, undoName);
        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config.highFidelityPipeline);
        RepaintViews();
    }

    private void SetUrpInt(string propertyPath, int value, string undoName)
    {
        SerializedObject so = new SerializedObject(config.highFidelityPipeline);
        SerializedProperty property = so.FindProperty(propertyPath);
        Undo.RecordObject(config.highFidelityPipeline, undoName);
        property.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config.highFidelityPipeline);
        RepaintViews();
    }

    private void SetUrpBool(string propertyPath, bool value, string undoName)
    {
        SerializedObject so = new SerializedObject(config.highFidelityPipeline);
        SerializedProperty property = so.FindProperty(propertyPath);
        Undo.RecordObject(config.highFidelityPipeline, undoName);
        property.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config.highFidelityPipeline);
        RepaintViews();
    }

    private List<Material> GetRetroFakeLitMaterials()
    {
        if (config == null || config.retroFakeLitMaterialFolder == null)
        {
            return new List<Material>();
        }

        string folderPath = AssetDatabase.GetAssetPath(config.retroFakeLitMaterialFolder);
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        List<Material> materials = new List<Material>();
        foreach (string guid in guids)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material != null && material.shader != null && material.shader.name == RetroShaderName)
            {
                materials.Add(material);
            }
        }

        return materials.OrderBy(m => m.name).ToList();
    }

    private void PullPhase05SharedValues()
    {
        Material sample = GetRetroFakeLitMaterials().FirstOrDefault();
        if (sample == null)
        {
            return;
        }

        phase05BatchState.lightWrap = GetMaterialFloat(sample, "_LightWrap");
        phase05BatchState.shadowColor = GetMaterialColor(sample, "_ShadowColor");
        phase05BatchState.ambientStrength = GetMaterialFloat(sample, "_AmbientStrength");
        phase05BatchState.specColor = GetMaterialColor(sample, "_SpecColor");
        phase05BatchState.specStrength = GetMaterialFloat(sample, "_SpecStrength");
        phase05BatchState.specPower = GetMaterialFloat(sample, "_SpecPower");
        phase05BatchState.rampSteps = Mathf.RoundToInt(GetMaterialFloat(sample, "_RampSteps"));
        phase05BatchState.rampStrength = GetMaterialFloat(sample, "_RampStrength");
        phase05BatchState.fogColor = GetMaterialColor(sample, "_FogColor");
        phase05BatchState.fogStart = GetMaterialFloat(sample, "_FogStart");
        phase05BatchState.fogEnd = GetMaterialFloat(sample, "_FogEnd");
    }

    private void ApplyPhase05SharedValues()
    {
        List<Material> materials = GetRetroFakeLitMaterials();
        if (materials.Count == 0)
        {
            return;
        }

        Undo.RecordObjects(materials.ToArray(), "Apply RetroFakeLit Shared Values");
        foreach (Material material in materials)
        {
            material.SetFloat("_LightWrap", phase05BatchState.lightWrap);
            material.SetColor("_ShadowColor", phase05BatchState.shadowColor);
            material.SetFloat("_AmbientStrength", phase05BatchState.ambientStrength);
            material.SetColor("_SpecColor", phase05BatchState.specColor);
            material.SetFloat("_SpecStrength", phase05BatchState.specStrength);
            material.SetFloat("_SpecPower", phase05BatchState.specPower);
            material.SetFloat("_RampSteps", phase05BatchState.rampSteps);
            material.SetFloat("_RampStrength", phase05BatchState.rampStrength);
            material.SetColor("_FogColor", phase05BatchState.fogColor);
            material.SetFloat("_FogStart", phase05BatchState.fogStart);
            material.SetFloat("_FogEnd", phase05BatchState.fogEnd);
            EditorUtility.SetDirty(material);
        }

        RepaintViews();
    }

    private ScriptableRendererFeature FindRendererFeature(string featureName)
    {
        if (config == null || config.highFidelityRenderer == null)
        {
            return null;
        }

        string rendererPath = AssetDatabase.GetAssetPath(config.highFidelityRenderer);
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rendererPath);
        return assets.OfType<ScriptableRendererFeature>()
            .FirstOrDefault(feature => feature.name.Equals(featureName, StringComparison.OrdinalIgnoreCase) || feature.name.Contains(featureName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsRendererFeatureActive(string featureName)
    {
        ScriptableRendererFeature feature = FindRendererFeature(featureName);
        if (feature == null)
        {
            return false;
        }

        SerializedObject so = new SerializedObject(feature);
        SerializedProperty property = so.FindProperty("m_Active");
        return property != null && property.boolValue;
    }

    private void SetRendererFeatureActive(string featureName, bool value)
    {
        ScriptableRendererFeature feature = FindRendererFeature(featureName);
        if (feature == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(feature);
        SerializedProperty property = so.FindProperty("m_Active");
        if (property == null)
        {
            return;
        }

        Undo.RecordObject(feature, $"Toggle {featureName}");
        property.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(config.highFidelityRenderer);
        RepaintViews();
    }

    private void SetPhase07Defaults()
    {
        SetRendererFeatureActive(PosterizeFeatureName, true);
        SetMaterialTexture(config.retroPosterizeThresholdMaterial, "_UserLut", config.dirtyBrownLut, "Set Phase07 Default LUT");
        SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Contribution", 0.85f, "Set Phase07 Default Contribution");
        SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_Threshold", 0.50f, "Set Phase07 Default Threshold");
        SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_ThresholdSharpness", 12f, "Set Phase07 Default Sharpness");
        SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_LutStrength", 1f, "Set Phase07 Default LUT Strength");
        SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_CompareDebug", 0f, "Reset Phase07 Compare Debug");
        SetMaterialFloat(config.retroPosterizeThresholdMaterial, "_DebugMask", 0f, "Reset Phase07 Debug Mask");
    }

    private void SetCompositeDefaults()
    {
        SetRendererFeatureActive(CompositeFeatureName, true);
        SetMaterialFloat(config.retroCompositeMaterial, "_VirtualWidth", 960f, "Set Composite Virtual Width");
        SetMaterialFloat(config.retroCompositeMaterial, "_VirtualHeight", 540f, "Set Composite Virtual Height");
        SetMaterialFloat(config.retroCompositeMaterial, "_Pixelate", 1f, "Set Composite Pixelate");
        SetMaterialFloat(config.retroCompositeMaterial, "_PosterizeLevels", 8f, "Set Composite Posterize Levels");
        SetMaterialFloat(config.retroCompositeMaterial, "_PosterizeStrength", 0.42f, "Set Composite Posterize Strength");
        SetMaterialFloat(config.retroCompositeMaterial, "_PaletteStrength", 0.34f, "Set Composite Palette Strength");
        SetMaterialFloat(config.retroCompositeMaterial, "_PaletteDarkThreshold", 0.46f, "Set Composite Palette Threshold");
        SetMaterialFloat(config.retroCompositeMaterial, "_DitherStrength", 0.03f, "Set Composite Dither Strength");
        SetMaterialFloat(config.retroCompositeMaterial, "_BlackCrush", 0.10f, "Set Composite Black Crush");
        SetMaterialFloat(config.retroCompositeMaterial, "_Contrast", 1.28f, "Set Composite Contrast");
        SetMaterialFloat(config.retroCompositeMaterial, "_Saturation", 0.92f, "Set Composite Saturation");
        SetMaterialFloat(config.retroCompositeMaterial, "_VignetteStrength", 0.62f, "Set Composite Vignette Strength");
        SetMaterialFloat(config.retroCompositeMaterial, "_VignetteRadius", 0.72f, "Set Composite Vignette Radius");
        SetMaterialFloat(config.retroCompositeMaterial, "_ScanlineStrength", 0.12f, "Set Composite Scanline Strength");
        SetMaterialFloat(config.retroCompositeMaterial, "_ChromaticAberration", 0.45f, "Set Composite CA");
        SetMaterialFloat(config.retroCompositeMaterial, "_NoiseStrength", 0.04f, "Set Composite Noise Strength");
        SetMaterialFloat(config.retroCompositeMaterial, "_CrtCurvature", 0.03f, "Set Composite Curvature");
        SetMaterialFloat(config.retroCompositeMaterial, "_CrtEdgeSoftness", 0.02f, "Set Composite Edge Softness");
        SetMaterialFloat(config.retroCompositeMaterial, "_CrtGlowBleed", 0.22f, "Set Composite Glow Bleed");
        SetMaterialFloat(config.retroCompositeMaterial, "_HorizontalJitter", 0.08f, "Set Composite Jitter");
        SetMaterialColor(config.retroCompositeMaterial, "_WarmTint", new Color(1.08f, 0.88f, 0.62f, 1f), "Set Composite Warm Tint");
        SetMaterialColor(config.retroCompositeMaterial, "_ColdTint", new Color(0.10f, 0.36f, 0.32f, 1f), "Set Composite Cold Tint");
    }

    private void SaveAllAssets()
    {
        AssetDatabase.SaveAssets();
        RepaintViews();
    }

    private static void PingObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        EditorGUIUtility.PingObject(target);
        Selection.activeObject = target;
    }

    private static void RepaintViews()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
