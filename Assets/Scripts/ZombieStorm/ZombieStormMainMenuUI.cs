using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
// Builds the standalone main menu UI using Unity UI/TextMeshPro. It owns the cover art,
// visible buttons, invisible cover hotspots, settings modal, credits modal, and hover styling.
public sealed class ZombieStormMainMenuUI : MonoBehaviour
{
    private const float FadeSpeed = 10f;
    private const float CoverArtAspectRatio = 1792f / 1024f;
    private const float CoverArtYOffset = 0f;
    private const string MasterVolumeKey = "ZombieStorm.MasterVolume";
    private const string MusicVolumeKey = "ZombieStorm.MusicVolume";
    private const string SfxVolumeKey = "ZombieStorm.SfxVolume";
    private const string FullscreenKey = "ZombieStorm.Fullscreen";

    private ZombieStormGameController controller;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private GameObject menuRoot;
    private GameObject settingsRoot;
    private GameObject creditsRoot;
    private TMP_FontAsset menuFont;
    private Sprite solidSprite;
    private Sprite backgroundFallbackSprite;
    private bool initialized;
    private bool failed;

    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Toggle fullscreenToggle;
    private TextMeshProUGUI masterValue;
    private TextMeshProUGUI musicValue;
    private TextMeshProUGUI sfxValue;
    private Image coverImage;
    private AspectRatioFitter coverAspectFitter;

    // Builds the menu the first time it is called, binds it to the game controller,
    // and refreshes the displayed background when a new cover sprite is supplied.
    public void Initialize(ZombieStormGameController owner, Sprite backgroundSprite)
    {
        controller = owner;
        if (initialized && canvas != null)
        {
            SetBackground(backgroundSprite);
            RefreshSettingsControls();
            return;
        }

        failed = false;
        try
        {
            solidSprite = CreateSolidSprite("menu_solid");
            menuFont = CreateMenuFont();
            if (menuFont == null)
            {
                failed = true;
                Debug.LogWarning("TextMeshPro Essentials are not imported yet. Using the legacy main menu until TMP resources are available.");
                enabled = false;
                return;
            }

            EnsureEventSystem();
            BuildCanvas();
            BuildMenu(backgroundSprite);
            RefreshSettingsControls();
            initialized = canvas != null && menuRoot != null && settingsRoot != null && creditsRoot != null;
        }
        catch (System.Exception exception)
        {
            initialized = false;
            failed = true;
            Debug.LogWarning("Zombie Storm main menu UI failed to initialize. Falling back to legacy menu.\n" + exception);
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }
    }

    public bool IsReady
    {
        get { return initialized && !failed && canvas != null && menuRoot != null; }
    }

    // Keeps the menu visible only while the controller is in main-menu flow, closes the top modal
    // on Escape, and smoothly fades the canvas group in/out.
    private void Update()
    {
        if (!IsReady || controller == null)
        {
            return;
        }

        bool visible = controller.IsMainMenuActive || controller.IsMainMenuSettingsActive;
        if (canvas.gameObject.activeSelf != visible)
        {
            canvas.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.unscaledDeltaTime * FadeSpeed);
        settingsRoot.SetActive(controller.IsMainMenuSettingsActive);
        if (controller.IsMainMenuSettingsActive)
        {
            creditsRoot.SetActive(false);
        }
    }

    // Closes Credits before Settings so Escape behaves like a simple modal stack.
    public bool CloseTopModal()
    {
        if (creditsRoot != null && creditsRoot.activeSelf)
        {
            creditsRoot.SetActive(false);
            return true;
        }

        return false;
    }

    // Creates the root screen-space canvas, scaler, raycaster, and canvas group used by the menu.
    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    // Assembles the full menu screen: background art, left button panel, cover-art hotspots,
    // bottom hints, settings modal, and credits modal.
    private void BuildMenu(Sprite backgroundSprite)
    {
        menuRoot = CreateRect("CommercialMainMenu", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one);

        coverImage = CreateCoverImage(menuRoot.transform, backgroundSprite != null ? backgroundSprite : GetBackgroundFallbackSprite());

        CreateCoverHotspots(menuRoot.transform);
        settingsRoot = CreateSettingsPanel(menuRoot.transform);
        creditsRoot = CreateRect("CreditsPlaceholder", menuRoot.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        settingsRoot.SetActive(false);
        creditsRoot.SetActive(false);
    }

    // Applies the supplied cover art or fallback art, then adds gradient/vignette overlays for readability.
    private void SetBackground(Sprite backgroundSprite)
    {
        if (menuRoot == null)
        {
            return;
        }

        Transform background = menuRoot.transform.Find("Background");
        if (background != null)
        {
            Image image = background.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = backgroundSprite != null ? backgroundSprite : GetBackgroundFallbackSprite();
                ApplyCoverAspect(image.sprite);
            }
        }
    }

    // Creates the cover image object and its aspect fitter so generated art scales without distortion.
    private Image CreateCoverImage(Transform parent, Sprite sprite)
    {
        GameObject imageObject = CreateRect("Background", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, CoverArtYOffset));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.sizeDelta = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;

        coverAspectFitter = imageObject.AddComponent<AspectRatioFitter>();
        coverAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        coverImage = image;
        ApplyCoverAspect(sprite);
        return image;
    }

    // Chooses the cover aspect ratio from the sprite when available, otherwise uses the expected art ratio.
    private void ApplyCoverAspect(Sprite sprite)
    {
        if (coverAspectFitter == null)
        {
            return;
        }

        if (sprite != null && sprite.rect.height > 0.01f)
        {
            coverAspectFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            return;
        }

        coverAspectFitter.aspectRatio = CoverArtAspectRatio;
    }

    // Creates the left-side translucent panel that holds the title and visible menu buttons.
    private RectTransform CreatePanel(Transform parent)
    {
        GameObject panelObject = CreateRect("MenuPanel", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(116f, 22f));
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560f, 720f);

        Image image = panelObject.AddComponent<Image>();
        image.sprite = solidSprite;
        image.color = new Color(0.018f, 0.023f, 0.028f, 0.86f);

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.22f, 0.12f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        Image accent = CreateImage("AccentBar", panelObject.transform, new Color(1f, 0.24f, 0.12f, 0.9f));
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 4f);

        return rect;
    }

    // Adds the title, English subtitle, and short pitch text to the menu panel.
    private void CreateTitle(RectTransform panel)
    {
        TextMeshProUGUI title = CreateText("ChineseTitle", panel, "\u50f5\u5c38\u5272\u8349\u5927\u4f5c\u6218", 56f, new Color(1f, 0.86f, 0.48f, 1f), TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(48f, -52f);
        title.rectTransform.sizeDelta = new Vector2(-96f, 76f);
        title.fontStyle = FontStyles.Bold;
        title.outlineColor = new Color(0.12f, 0.02f, 0.01f, 0.95f);
        title.outlineWidth = 0.18f;
        Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        titleShadow.effectDistance = new Vector2(4f, -4f);

        TextMeshProUGUI subtitle = CreateText("EnglishTitle", panel, "Zombie Storm", 26f, new Color(0.92f, 0.96f, 1f, 0.86f), TextAlignmentOptions.Left);
        subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitle.rectTransform.pivot = new Vector2(0f, 1f);
        subtitle.rectTransform.anchoredPosition = new Vector2(52f, -126f);
        subtitle.rectTransform.sizeDelta = new Vector2(-104f, 34f);
        subtitle.characterSpacing = 8f;

        TextMeshProUGUI pitch = CreateText("Pitch", panel, "Survive the horde, stack wild skills, and burn a path through the dead city.", 18f, new Color(0.74f, 0.8f, 0.84f, 0.9f), TextAlignmentOptions.Left);
        pitch.rectTransform.anchorMin = new Vector2(0f, 1f);
        pitch.rectTransform.anchorMax = new Vector2(1f, 1f);
        pitch.rectTransform.pivot = new Vector2(0f, 1f);
        pitch.rectTransform.anchoredPosition = new Vector2(52f, -180f);
        pitch.rectTransform.sizeDelta = new Vector2(-108f, 64f);
        pitch.enableWordWrapping = true;
    }

    // Adds the visible menu buttons and wires them to controller requests or local modal toggles.
    private void CreateButtons(RectTransform panel)
    {
        RectTransform buttonGroup = CreateRect("ButtonGroup", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(52f, -294f)).GetComponent<RectTransform>();
        buttonGroup.sizeDelta = new Vector2(430f, 330f);

        CreateStyledButton(buttonGroup, "Start Run", 0, delegate { controller.RequestStartRun(); });
        CreateStyledButton(buttonGroup, "Settings", 1, delegate { controller.RequestOpenMainMenuSettings(); });
        CreateStyledButton(buttonGroup, "Credits", 2, delegate { creditsRoot.SetActive(true); });
        CreateStyledButton(buttonGroup, "Quit", 3, delegate { controller.RequestQuit(); });

        TextMeshProUGUI hint = CreateText("EnterHint", panel, "Press Enter to start", 15f, new Color(0.7f, 0.76f, 0.8f, 0.72f), TextAlignmentOptions.Left);
        hint.rectTransform.anchorMin = new Vector2(0f, 0f);
        hint.rectTransform.anchorMax = new Vector2(1f, 0f);
        hint.rectTransform.pivot = new Vector2(0f, 0f);
        hint.rectTransform.anchoredPosition = new Vector2(54f, 44f);
        hint.rectTransform.sizeDelta = new Vector2(-108f, 24f);
    }

    // Adds transparent button hitboxes over the AI-rendered Start Game and Settings areas in the cover art.
    private void CreateCoverHotspots(Transform parent)
    {
        CreateTransparentCoverButton(parent, "StartGameHotspot", new Vector2(570f, -228f), new Vector2(500f, 128f), delegate { controller.RequestStartRun(); });
        CreateTransparentCoverButton(parent, "SettingsHotspot", new Vector2(570f, -356f), new Vector2(500f, 128f), delegate { controller.RequestOpenMainMenuSettings(); });
    }

    // Creates a transparent UI Button whose only job is to receive clicks over rendered cover text.
    private Button CreateTransparentCoverButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = solidSprite;
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        return button;
    }

    // Creates a visible menu button with background image, outline, label, click handler, and hover visual component.
    private Button CreateStyledButton(Transform parent, string label, int index, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect(label.Replace(" ", "") + "Button", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -index * 82f));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(430f, 66f);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = solidSprite;
        image.color = new Color(0.05f, 0.057f, 0.064f, 0.86f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.32f, 0.18f, 0.34f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Image strip = CreateImage("LeftAccent", buttonObject.transform, new Color(1f, 0.22f, 0.1f, 0.82f));
        RectTransform stripRect = strip.rectTransform;
        stripRect.anchorMin = new Vector2(0f, 0f);
        stripRect.anchorMax = new Vector2(0f, 1f);
        stripRect.pivot = new Vector2(0f, 0.5f);
        stripRect.anchoredPosition = Vector2.zero;
        stripRect.sizeDelta = new Vector2(5f, 0f);

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 24f, new Color(0.9f, 0.93f, 0.96f, 0.95f), TextAlignmentOptions.MidlineLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(34f, 0f);
        text.rectTransform.offsetMax = new Vector2(-24f, 0f);
        text.fontStyle = FontStyles.Bold;

        ZombieStormMenuButtonVisual visual = buttonObject.AddComponent<ZombieStormMenuButtonVisual>();
        visual.Initialize(image, outline, text);
        return button;
    }

    // Adds small footer text for version, controls, and basic menu instructions.
    private void CreateBottomInfo(Transform parent)
    {
        TextMeshProUGUI version = CreateText("Version", parent, "v0.1 Prototype", 15f, new Color(0.72f, 0.76f, 0.78f, 0.68f), TextAlignmentOptions.BottomLeft);
        version.rectTransform.anchorMin = Vector2.zero;
        version.rectTransform.anchorMax = Vector2.zero;
        version.rectTransform.pivot = Vector2.zero;
        version.rectTransform.anchoredPosition = new Vector2(32f, 24f);
        version.rectTransform.sizeDelta = new Vector2(280f, 30f);

        TextMeshProUGUI controls = CreateText("Controls", parent, "WASD Move | Auto Skills | F Ultimate", 15f, new Color(0.72f, 0.76f, 0.78f, 0.68f), TextAlignmentOptions.BottomRight);
        controls.rectTransform.anchorMin = Vector2.one;
        controls.rectTransform.anchorMax = Vector2.one;
        controls.rectTransform.pivot = Vector2.one;
        controls.rectTransform.anchoredPosition = new Vector2(-32f, 24f);
        controls.rectTransform.sizeDelta = new Vector2(520f, 30f);
    }

    // Builds the Settings modal and wires sliders/toggle so every change immediately applies to the controller.
    private GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject root = CreateModalRoot("SettingsModal", parent);
        RectTransform panel = CreateModalPanel(root.transform, new Vector2(620f, 560f));

        TextMeshProUGUI title = CreateText("Title", panel, "Settings", 38f, new Color(0.95f, 0.98f, 1f, 1f), TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(44f, -34f);
        title.rectTransform.sizeDelta = new Vector2(-88f, 52f);
        title.fontStyle = FontStyles.Bold;

        masterSlider = CreateSlider(panel, "Master Volume", 0, out masterValue);
        musicSlider = CreateSlider(panel, "Music Volume", 1, out musicValue);
        sfxSlider = CreateSlider(panel, "SFX Volume", 2, out sfxValue);
        fullscreenToggle = CreateToggle(panel, "Fullscreen", 3);

        masterSlider.onValueChanged.AddListener(delegate { SaveSettingsFromControls(); });
        musicSlider.onValueChanged.AddListener(delegate { SaveSettingsFromControls(); });
        sfxSlider.onValueChanged.AddListener(delegate { SaveSettingsFromControls(); });
        fullscreenToggle.onValueChanged.AddListener(delegate { SaveSettingsFromControls(); });

        Button back = CreateStyledButton(panel, "Back", 0, delegate { controller.RequestCloseSettings(); });
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 38f);
        backRect.sizeDelta = new Vector2(360f, 62f);
        return root;
    }

    // Builds the Credits modal that summarizes project authorship and major asset sources.
    private GameObject CreateCreditsPanel(Transform parent)
    {
        GameObject root = CreateModalRoot("CreditsModal", parent);
        RectTransform panel = CreateModalPanel(root.transform, new Vector2(560f, 420f));

        TextMeshProUGUI title = CreateText("Title", panel, "Credits", 38f, new Color(0.95f, 0.98f, 1f, 1f), TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -42f);
        title.rectTransform.sizeDelta = new Vector2(-80f, 54f);
        title.fontStyle = FontStyles.Bold;

        TextMeshProUGUI body = CreateText("Body", panel, "Zombie Storm Prototype\nDesign, code, and tuning by the project team.\nExternal art is used according to its source license.", 19f, new Color(0.78f, 0.84f, 0.88f, 0.92f), TextAlignmentOptions.Center);
        body.rectTransform.anchorMin = new Vector2(0f, 0f);
        body.rectTransform.anchorMax = new Vector2(1f, 1f);
        body.rectTransform.offsetMin = new Vector2(52f, 112f);
        body.rectTransform.offsetMax = new Vector2(-52f, -122f);
        body.enableWordWrapping = true;

        Button back = CreateStyledButton(panel, "Back", 0, delegate { creditsRoot.SetActive(false); });
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 34f);
        backRect.sizeDelta = new Vector2(320f, 60f);
        return root;
    }

    // Creates one settings row with label, slider, and numeric percent label.
    private Slider CreateSlider(RectTransform parent, string label, int index, out TextMeshProUGUI valueText)
    {
        float y = -126f - index * 86f;
        TextMeshProUGUI labelText = CreateText(label.Replace(" ", "") + "Label", parent, label, 18f, new Color(0.82f, 0.87f, 0.9f, 0.96f), TextAlignmentOptions.Left);
        labelText.rectTransform.anchorMin = new Vector2(0f, 1f);
        labelText.rectTransform.anchorMax = new Vector2(0f, 1f);
        labelText.rectTransform.pivot = new Vector2(0f, 1f);
        labelText.rectTransform.anchoredPosition = new Vector2(54f, y);
        labelText.rectTransform.sizeDelta = new Vector2(220f, 30f);

        valueText = CreateText(label.Replace(" ", "") + "Value", parent, "100%", 17f, new Color(0.95f, 0.72f, 0.42f, 0.96f), TextAlignmentOptions.Right);
        valueText.rectTransform.anchorMin = new Vector2(1f, 1f);
        valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
        valueText.rectTransform.pivot = new Vector2(1f, 1f);
        valueText.rectTransform.anchoredPosition = new Vector2(-54f, y);
        valueText.rectTransform.sizeDelta = new Vector2(90f, 30f);

        GameObject sliderObject = CreateRect(label.Replace(" ", "") + "Slider", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 44f));
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(-108f, 24f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        Image background = CreateImage("Background", sliderObject.transform, new Color(0.04f, 0.047f, 0.052f, 0.92f));
        background.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        background.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        background.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        background.rectTransform.anchoredPosition = Vector2.zero;
        background.rectTransform.sizeDelta = new Vector2(0f, 10f);

        RectTransform fillArea = CreateRect("Fill Area", sliderObject.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero).GetComponent<RectTransform>();
        fillArea.offsetMin = new Vector2(0f, 7f);
        fillArea.offsetMax = new Vector2(0f, -7f);
        Image fill = CreateImage("Fill", fillArea, new Color(1f, 0.28f, 0.12f, 0.9f));
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        RectTransform handleArea = CreateRect("Handle Slide Area", sliderObject.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero).GetComponent<RectTransform>();
        handleArea.offsetMin = new Vector2(6f, 0f);
        handleArea.offsetMax = new Vector2(-6f, 0f);
        Image handle = CreateImage("Handle", handleArea, new Color(1f, 0.82f, 0.46f, 1f));
        handle.rectTransform.sizeDelta = new Vector2(22f, 22f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        return slider;
    }

    // Creates a settings row toggle, currently used for fullscreen.
    private Toggle CreateToggle(RectTransform parent, string label, int index)
    {
        float y = -126f - index * 86f;
        GameObject toggleObject = CreateRect("FullscreenToggle", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(54f, y - 6f));
        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(-108f, 44f);

        Toggle toggle = toggleObject.AddComponent<Toggle>();
        Image box = CreateImage("Box", toggleObject.transform, new Color(0.04f, 0.047f, 0.052f, 0.94f));
        box.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        box.rectTransform.pivot = new Vector2(0f, 0.5f);
        box.rectTransform.anchoredPosition = Vector2.zero;
        box.rectTransform.sizeDelta = new Vector2(32f, 32f);
        Outline outline = box.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.35f, 0.18f, 0.48f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Image check = CreateImage("Checkmark", box.transform, new Color(1f, 0.75f, 0.32f, 1f));
        check.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        check.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        check.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        check.rectTransform.anchoredPosition = Vector2.zero;
        check.rectTransform.sizeDelta = new Vector2(18f, 18f);

        TextMeshProUGUI text = CreateText("Label", toggleObject.transform, label, 19f, new Color(0.82f, 0.87f, 0.9f, 0.96f), TextAlignmentOptions.MidlineLeft);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(48f, 0f);
        text.rectTransform.offsetMax = Vector2.zero;

        toggle.targetGraphic = box;
        toggle.graphic = check;
        return toggle;
    }

    // Creates a full-screen modal root with a dimmed click-blocking background.
    private GameObject CreateModalRoot(string name, Transform parent)
    {
        GameObject root = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one);
        Image dim = CreateImage("ModalDim", root.transform, new Color(0f, 0f, 0f, 0.48f));
        Stretch(dim.rectTransform);
        return root;
    }

    // Creates the centered modal content panel and gives it a visible border.
    private RectTransform CreateModalPanel(Transform parent, Vector2 size)
    {
        GameObject panelObject = CreateRect("Panel", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Image panel = panelObject.AddComponent<Image>();
        panel.sprite = solidSprite;
        panel.color = new Color(0.018f, 0.023f, 0.028f, 0.96f);
        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.35f, 0.18f, 0.48f);
        outline.effectDistance = new Vector2(2f, -2f);
        return rect;
    }

    // Copies current controller settings into sliders/toggle without saving them again.
    private void RefreshSettingsControls()
    {
        if (controller == null || masterSlider == null)
        {
            return;
        }

        masterSlider.SetValueWithoutNotify(controller.MasterVolume);
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicVolumeKey, controller.MusicVolume));
        sfxSlider.SetValueWithoutNotify(controller.SfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, controller.FullscreenEnabled ? 1 : 0) == 1);
        UpdateSettingValueLabels();
    }

    // Reads the current settings controls and applies/saves them through the controller.
    private void SaveSettingsFromControls()
    {
        if (controller == null || masterSlider == null)
        {
            return;
        }

        UpdateSettingValueLabels();
        PlayerPrefs.SetFloat(MasterVolumeKey, masterSlider.value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicSlider.value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxSlider.value);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        controller.ApplyMenuSettings(masterSlider.value, musicSlider.value, sfxSlider.value, fullscreenToggle.isOn);
    }

    // Updates the percent labels beside volume sliders after values change.
    private void UpdateSettingValueLabels()
    {
        masterValue.text = Mathf.RoundToInt(masterSlider.value * 100f).ToString() + "%";
        musicValue.text = Mathf.RoundToInt(musicSlider.value * 100f).ToString() + "%";
        sfxValue.text = Mathf.RoundToInt(sfxSlider.value * 100f).ToString() + "%";
    }

    // Creates a TextMeshProUGUI object with shared menu font, color, alignment, and raycast disabled.
    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        if (menuFont != null)
        {
            tmp.font = menuFont;
        }

        return tmp;
    }

    // Creates a plain UI Image used for panels, borders, overlays, and button backgrounds.
    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = solidSprite;
        image.color = color;
        return image;
    }

    // Creates a GameObject with RectTransform and applies anchors, pivot, and anchored position.
    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return gameObject;
    }

    // Resets anchors and offsets so the RectTransform fills its parent exactly.
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Uses the TMP default font if available, otherwise falls back to the bundled Liberation SDF font.
    private TMP_FontAsset CreateMenuFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    // Creates an EventSystem/InputModule if the scene does not already have one, allowing buttons to click.
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(transform, false);
    }

    // Creates and caches a simple generated fallback cover when the AI menu art is missing.
    private Sprite GetBackgroundFallbackSprite()
    {
        if (backgroundFallbackSprite != null)
        {
            return backgroundFallbackSprite;
        }

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(40, 45, 38, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        backgroundFallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return backgroundFallbackSprite;
    }

    // Creates a one-pixel solid sprite for UI images that only need flat color.
    private Sprite CreateSolidSprite(string name)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    // Creates the top/bottom gradient overlay used to darken the cover art behind text.
    private Sprite CreateVerticalGradientSprite()
    {
        const int width = 2;
        const int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "menu_top_bottom_gradient";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < height; y++)
        {
            float t = y / (height - 1f);
            float edge = Mathf.Max(1f - t, t);
            float alpha = Mathf.Lerp(0.08f, 0.46f, Mathf.InverseLerp(0.52f, 1f, edge));
            Color color = new Color(0f, 0f, 0f, alpha);
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    // Creates a radial vignette overlay that frames the menu art and improves foreground contrast.
    private Sprite CreateVignetteSprite()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "menu_vignette";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDistance = center.magnitude;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(0f, 0.62f, Mathf.InverseLerp(0.42f, 1f, distance));
                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}

// Pointer-state component for menu buttons. It changes background, outline, and label colors
// for normal, hover, and pressed states without requiring Animator assets.
public sealed class ZombieStormMenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image background;
    private Outline outline;
    private TextMeshProUGUI label;
    private bool hovering;

    // Stores the UI parts this visual controller will recolor and applies the normal state.
    public void Initialize(Image targetBackground, Outline targetOutline, TextMeshProUGUI targetLabel)
    {
        background = targetBackground;
        outline = targetOutline;
        label = targetLabel;
        ApplyVisual(false, false);
    }

    // Switches the button to hover visual state.
    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ApplyVisual(true, false);
    }

    // Restores the button to normal visual state.
    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ApplyVisual(false, false);
    }

    // Switches the button to pressed visual state.
    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyVisual(hovering, true);
    }

    // Restores the button to hover or normal visual state.
    public void OnPointerUp(PointerEventData eventData)
    {
        ApplyVisual(hovering, false);
    }

    // Applies the color palette for normal, hover, or pressed button state.
    private void ApplyVisual(bool hover, bool pressed)
    {
        if (background != null)
        {
            background.color = pressed
                ? new Color(0.14f, 0.07f, 0.052f, 0.96f)
                : hover ? new Color(0.13f, 0.088f, 0.072f, 0.94f) : new Color(0.05f, 0.057f, 0.064f, 0.86f);
        }

        if (outline != null)
        {
            outline.effectColor = pressed
                ? new Color(1f, 0.78f, 0.36f, 0.78f)
                : hover ? new Color(1f, 0.48f, 0.2f, 0.72f) : new Color(1f, 0.32f, 0.18f, 0.34f);
        }

        if (label != null)
        {
            label.color = hover ? new Color(1f, 0.94f, 0.78f, 1f) : new Color(0.9f, 0.93f, 0.96f, 0.95f);
        }

        transform.localScale = pressed ? new Vector3(0.976f, 0.976f, 1f) : Vector3.one;
    }
}
