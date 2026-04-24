using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SolarSystemExperience : MonoBehaviour
{
    private Camera mainCamera;
    private AudioSource audioSource;
    private AudioClip planetTone;
    private AudioClip moonTone;

    private CelestialBody selectedBody;

    private readonly Vector3 overviewPosition = new Vector3(0f, 8f, -22f);
    private readonly Vector3 overviewLookTarget = new Vector3(0f, 1.5f, 0f);

    private Vector3 desiredLookTarget;
    private Vector3 smoothedLookTarget;
    private Vector3 cameraVelocity;
    private Vector3 lookTargetVelocity;

    private Text bodyTitleText;
    private Text factText;
    private Text hintText;
    private Button returnButton;

    private Font uiFont;
    private bool isBuilt;

    private void Awake()
    {
        SolarSystemExperience[] systems = FindObjectsOfType<SolarSystemExperience>();

        if (systems.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        BuildExperience();
    }

    private void Update()
    {
        if (!isBuilt)
        {
            return;
        }

        HandleSelectionInput();

        if (selectedBody != null && Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToOverview();
        }
    }

    private void LateUpdate()
    {
        if (!isBuilt || mainCamera == null)
        {
            return;
        }

        UpdateCamera();
    }

    private void BuildExperience()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        SetupCamera();
        SetupLighting();
        SetupAudio();
        EnsureEventSystem();
        BuildSolarSystem();
        BuildUi();
        ShowIntroMessage();

        desiredLookTarget = overviewLookTarget;
        smoothedLookTarget = overviewLookTarget;
        isBuilt = true;
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.015f, 0.025f, 0.08f);
        mainCamera.fieldOfView = 58f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 300f;
        mainCamera.transform.position = overviewPosition;
        mainCamera.transform.rotation = Quaternion.LookRotation(overviewLookTarget - overviewPosition);
    }

    private void SetupLighting()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.1f);

        Light[] lights = FindObjectsOfType<Light>();
        Light directional = null;

        foreach (Light lightSource in lights)
        {
            if (lightSource.type == LightType.Directional)
            {
                directional = lightSource;
                break;
            }
        }

        if (directional == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
        }

        directional.color = new Color(0.6f, 0.72f, 1f);
        directional.intensity = 0.3f;
        directional.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        RenderSettings.sun = directional;
    }

    private void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.2f;

        planetTone = CreateToneClip("PlanetTone", 523.25f, 0.18f);
        moonTone = CreateToneClip("MoonTone", 659.25f, 0.16f);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void BuildSolarSystem()
    {
        GameObject root = new GameObject("Solar System Root");

        CelestialBody sun = CreateBody(
            root.transform,
            "Sun",
            Vector3.zero,
            3.6f,
            new Color(1f, 0.65f, 0.15f),
            new Color(1f, 0.5f, 0.1f),
            "The Sun is our star. It gives Earth light and warmth every day.",
            false);

        SpinMotion sunSpin = sun.gameObject.AddComponent<SpinMotion>();
        sunSpin.Initialize(Vector3.up, 10f);

        Light sunLight = sun.gameObject.AddComponent<Light>();
        sunLight.type = LightType.Point;
        sunLight.range = 45f;
        sunLight.intensity = 2.4f;
        sunLight.color = new Color(1f, 0.72f, 0.35f);

        Transform earthOrbit = CreateOrbitPivot(root.transform, "Earth Orbit", Vector3.zero, new Vector3(0f, 1f, 0.08f), 10f);
        CelestialBody earth = CreateBody(
            earthOrbit,
            "Earth",
            new Vector3(9f, 0f, 0f),
            1.7f,
            new Color(0.2f, 0.5f, 1f),
            new Color(0.2f, 0.8f, 1f),
            "Earth is our home. It has blue oceans, fluffy clouds, and lots of life.",
            false);

        SpinMotion earthSpin = earth.gameObject.AddComponent<SpinMotion>();
        earthSpin.Initialize(new Vector3(0.1f, 1f, 0f), 35f);

        Transform moonOrbit = CreateOrbitPivot(earthOrbit, "Moon Orbit", earth.transform.localPosition, new Vector3(0.1f, 1f, 0f), 55f);
        CelestialBody moon = CreateBody(
            moonOrbit,
            "Moon",
            new Vector3(2.5f, 0f, 0f),
            0.65f,
            new Color(0.82f, 0.84f, 0.9f),
            new Color(0.9f, 0.92f, 1f),
            "The Moon is Earth's space buddy. It shines because sunlight bounces off it.",
            true);

        SpinMotion moonSpin = moon.gameObject.AddComponent<SpinMotion>();
        moonSpin.Initialize(Vector3.up, 18f);

        Transform marsOrbit = CreateOrbitPivot(root.transform, "Mars Orbit", Vector3.zero, new Vector3(0f, 1f, -0.04f), 6f);
        CelestialBody mars = CreateBody(
            marsOrbit,
            "Mars",
            new Vector3(-14f, 0.6f, 0f),
            1.1f,
            new Color(0.85f, 0.36f, 0.18f),
            new Color(1f, 0.42f, 0.18f),
            "Mars is called the red planet because its dusty ground looks rusty.",
            false);

        SpinMotion marsSpin = mars.gameObject.AddComponent<SpinMotion>();
        marsSpin.Initialize(new Vector3(-0.1f, 1f, 0.05f), 28f);

        CreateComet(root.transform);
    }

    private CelestialBody CreateBody(
        Transform parent,
        string bodyName,
        Vector3 localPosition,
        float scale,
        Color bodyColor,
        Color emissionColor,
        string fact,
        bool isMoon)
    {
        GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bodyObject.name = bodyName;
        bodyObject.transform.SetParent(parent, false);
        bodyObject.transform.localPosition = localPosition;
        bodyObject.transform.localScale = Vector3.one * scale;

        Renderer renderer = bodyObject.GetComponent<Renderer>();
        renderer.material = CreateLitMaterial(bodyColor, emissionColor, 0.05f, 0.45f);

        CelestialBody body = bodyObject.AddComponent<CelestialBody>();
        body.Initialize(bodyName, fact, emissionColor, isMoon);
        return body;
    }

    private Transform CreateOrbitPivot(Transform parent, string name, Vector3 localPosition, Vector3 axis, float speed)
    {
        GameObject pivotObject = new GameObject(name);
        pivotObject.transform.SetParent(parent, false);
        pivotObject.transform.localPosition = localPosition;
        pivotObject.transform.localRotation = Quaternion.identity;

        OrbitMotion orbit = pivotObject.AddComponent<OrbitMotion>();
        orbit.Initialize(axis, speed);
        return pivotObject.transform;
    }

    private void CreateComet(Transform root)
    {
        Transform cometOrbit = CreateOrbitPivot(root, "Comet Orbit", Vector3.zero, new Vector3(0.2f, 1f, 0f), 18f);
        GameObject comet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        comet.name = "Comet";
        comet.transform.SetParent(cometOrbit, false);
        comet.transform.localPosition = new Vector3(18f, 3.5f, 0f);
        comet.transform.localScale = Vector3.one * 0.4f;

        Renderer renderer = comet.GetComponent<Renderer>();
        renderer.material = CreateLitMaterial(
            new Color(0.8f, 0.95f, 1f),
            new Color(0.5f, 0.8f, 1f),
            0f,
            0.2f);

        Collider cometCollider = comet.GetComponent<Collider>();
        if (cometCollider != null)
        {
            Destroy(cometCollider);
        }

        SpinMotion cometSpin = comet.AddComponent<SpinMotion>();
        cometSpin.Initialize(new Vector3(0.2f, 1f, 0f), 40f);

        TrailRenderer trail = comet.AddComponent<TrailRenderer>();
        trail.time = 2.2f;
        trail.startWidth = 0.28f;
        trail.endWidth = 0.02f;
        trail.material = CreateTrailMaterial(new Color(0.8f, 0.95f, 1f, 0.85f));
        trail.startColor = new Color(0.85f, 0.95f, 1f, 0.9f);
        trail.endColor = new Color(0.4f, 0.7f, 1f, 0f);
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Text title = CreateText(canvas.transform, "Title", 42, FontStyle.Bold, TextAnchor.UpperCenter);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -32f);
        titleRect.sizeDelta = new Vector2(900f, 80f);
        title.text = "Interactive Solar System for Kids";
        title.color = new Color(0.92f, 0.96f, 1f);

        GameObject panelObject = new GameObject("Info Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(0.03f, 0.06f, 0.16f, 0.82f);

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.06f, 0.04f);
        panelRect.anchorMax = new Vector2(0.94f, 0.28f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        bodyTitleText = CreateText(panel.transform, "Body Title", 32, FontStyle.Bold, TextAnchor.UpperLeft);
        RectTransform bodyTitleRect = bodyTitleText.rectTransform;
        bodyTitleRect.anchorMin = new Vector2(0f, 1f);
        bodyTitleRect.anchorMax = new Vector2(1f, 1f);
        bodyTitleRect.pivot = new Vector2(0f, 1f);
        bodyTitleRect.offsetMin = new Vector2(24f, -66f);
        bodyTitleRect.offsetMax = new Vector2(-240f, -18f);

        factText = CreateText(panel.transform, "Fact Text", 24, FontStyle.Normal, TextAnchor.UpperLeft);
        RectTransform factRect = factText.rectTransform;
        factRect.anchorMin = new Vector2(0f, 0f);
        factRect.anchorMax = new Vector2(1f, 1f);
        factRect.offsetMin = new Vector2(24f, 36f);
        factRect.offsetMax = new Vector2(-240f, -74f);
        factText.horizontalOverflow = HorizontalWrapMode.Wrap;
        factText.verticalOverflow = VerticalWrapMode.Overflow;

        hintText = CreateText(panel.transform, "Hint Text", 19, FontStyle.Italic, TextAnchor.LowerLeft);
        RectTransform hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0f, 0f);
        hintRect.offsetMin = new Vector2(24f, 14f);
        hintRect.offsetMax = new Vector2(-240f, 44f);
        hintText.color = new Color(0.75f, 0.86f, 1f);

        returnButton = CreateButton(panel.transform, "Return to View");
        RectTransform buttonRect = returnButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-24f, 0f);
        buttonRect.sizeDelta = new Vector2(180f, 56f);
        returnButton.onClick.AddListener(ReturnToOverview);

        returnButton.gameObject.SetActive(false);
    }

    private void ShowIntroMessage()
    {
        bodyTitleText.text = "Welcome, space explorer!";
        bodyTitleText.color = new Color(1f, 0.93f, 0.6f);
        factText.text = "Click Earth or the Moon to zoom in, hear a cheerful space tone, and learn a fun fact. You can also click the Sun or Mars for extra exploration.";
        factText.color = new Color(0.9f, 0.95f, 1f);
        hintText.text = "Need a quick demo flow? Click Earth, then Moon, then press Return to go back to the full solar system.";
    }

    private void HandleSelectionInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return;
        }

        CelestialBody body = hit.collider.GetComponent<CelestialBody>();

        if (body == null)
        {
            return;
        }

        SelectBody(body);
    }

    private void SelectBody(CelestialBody body)
    {
        selectedBody = body;
        body.PlaySelectionEffect();
        desiredLookTarget = body.transform.position;

        bodyTitleText.text = body.BodyName;
        bodyTitleText.color = body.AccentColor;
        factText.text = body.FactText;
        factText.color = new Color(0.94f, 0.97f, 1f);
        hintText.text = "Click another world to compare it, or press Return / Esc to fly back to the main view.";

        returnButton.gameObject.SetActive(true);
        audioSource.PlayOneShot(body.IsMoon ? moonTone : planetTone);
    }

    private void ReturnToOverview()
    {
        selectedBody = null;
        desiredLookTarget = overviewLookTarget;
        returnButton.gameObject.SetActive(false);
        ShowIntroMessage();
    }

    private void UpdateCamera()
    {
        Vector3 targetPosition = selectedBody != null ? selectedBody.GetSuggestedCameraPosition() : overviewPosition;
        Vector3 targetLook = selectedBody != null ? selectedBody.transform.position : overviewLookTarget;

        mainCamera.transform.position = Vector3.SmoothDamp(
            mainCamera.transform.position,
            targetPosition,
            ref cameraVelocity,
            0.45f);

        smoothedLookTarget = Vector3.SmoothDamp(
            smoothedLookTarget,
            targetLook,
            ref lookTargetVelocity,
            0.3f);

        Vector3 lookDirection = smoothedLookTarget - mainCamera.transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, 6f * Time.deltaTime);
        }
    }

    private Material CreateLitMaterial(Color baseColor, Color emissionColor, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = baseColor;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissionColor * 0.45f);
        return material;
    }

    private Material CreateTrailMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private AudioClip CreateToneClip(string clipName, float frequency, float duration)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(Mathf.Min(time * 10f, (duration - time) * 12f));
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private Text CreateText(Transform parent, string name, int fontSize, FontStyle style, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.supportRichText = true;
        text.color = Color.white;

        return text;
    }

    private Button CreateButton(Transform parent, string label)
    {
        GameObject buttonObject = new GameObject("Return Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.39f, 0.85f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.22f, 0.49f, 0.95f, 1f);
        colors.pressedColor = new Color(0.09f, 0.27f, 0.7f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text labelText = CreateText(buttonObject.transform, "Label", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = labelText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        labelText.text = label;
        labelText.color = Color.white;

        return button;
    }
}
