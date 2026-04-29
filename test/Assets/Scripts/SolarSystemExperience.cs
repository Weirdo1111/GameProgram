using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SolarSystemExperience : MonoBehaviour
{
    private const string SolarSystemAssetFolder = "SolarSystem";

    private Camera mainCamera;
    private AudioSource ambienceSource;
    private AudioSource selectionSource;
    private AudioClip planetTone;
    private AudioClip moonTone;
    private AudioClip returnTone;
    private AudioClip ambienceLoop;

    private CelestialBody selectedBody;

    private readonly Vector3 overviewPosition = new Vector3(0f, 8f, -22f);
    private readonly Vector3 overviewLookTarget = new Vector3(0f, 1.5f, 0f);

    private Vector3 smoothedLookTarget;
    private Vector3 cameraVelocity;
    private Vector3 lookTargetVelocity;

    private Text bodyTitleText;
    private Text factText;
    private Text hintText;
    private Button returnButton;

    private Font uiFont;
    private bool isBuilt;

    private Texture2D earthDayTexture;
    private Texture2D earthCloudTexture;
    private Texture2D earthNightTexture;
    private Texture2D moonTexture;
    private Texture2D marsTexture;
    private Texture2D sunTexture;
    private Texture2D starsTexture;

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

        LoadTextures();
        SetupCamera();
        SetupLighting();
        SetupAudio();
        EnsureEventSystem();
        BuildBackdrop();
        BuildSolarSystem();
        BuildUi();
        ShowIntroMessage();

        smoothedLookTarget = overviewLookTarget;
        isBuilt = true;
    }

    private void LoadTextures()
    {
        earthDayTexture = LoadTextureFromStreamingAssets("earth_day.jpg");
        earthCloudTexture = LoadTextureFromStreamingAssets("earth_clouds.jpg");
        earthNightTexture = LoadTextureFromStreamingAssets("earth_night.jpg");
        moonTexture = LoadTextureFromStreamingAssets("moon.jpg");
        marsTexture = LoadTextureFromStreamingAssets("mars.jpg");
        sunTexture = LoadTextureFromStreamingAssets("sun.jpg");
        starsTexture = LoadTextureFromStreamingAssets("stars.jpg");
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
        mainCamera.backgroundColor = new Color(0.005f, 0.01f, 0.03f);
        mainCamera.fieldOfView = 58f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 400f;
        mainCamera.allowHDR = true;
        mainCamera.transform.position = overviewPosition;
        mainCamera.transform.rotation = Quaternion.LookRotation(overviewLookTarget - overviewPosition);
    }

    private void SetupLighting()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.08f, 0.1f, 0.16f);
        RenderSettings.ambientEquatorColor = new Color(0.03f, 0.035f, 0.06f);
        RenderSettings.ambientGroundColor = new Color(0.01f, 0.012f, 0.02f);
        RenderSettings.ambientIntensity = 0.9f;

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

        directional.color = new Color(0.68f, 0.78f, 1f);
        directional.intensity = 0.22f;
        directional.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        RenderSettings.sun = directional;
    }

    private void SetupAudio()
    {
        ambienceSource = gameObject.AddComponent<AudioSource>();
        ambienceSource.playOnAwake = false;
        ambienceSource.loop = true;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.volume = 0.08f;

        selectionSource = gameObject.AddComponent<AudioSource>();
        selectionSource.playOnAwake = false;
        selectionSource.spatialBlend = 0f;
        selectionSource.volume = 0.18f;

        ambienceLoop = CreateAmbienceClip("SpaceAmbience", 2.4f);
        planetTone = CreateSelectionClip("PlanetTone", 420f, 580f, 0.32f);
        moonTone = CreateSelectionClip("MoonTone", 620f, 880f, 0.26f);
        returnTone = CreateSelectionClip("ReturnTone", 340f, 510f, 0.22f);

        if (ambienceLoop != null)
        {
            ambienceSource.clip = ambienceLoop;
            ambienceSource.Play();
        }
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

    private void BuildBackdrop()
    {
        if (starsTexture == null)
        {
            return;
        }

        GameObject starDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        starDome.name = "Star Dome";
        starDome.transform.position = Vector3.zero;
        starDome.transform.localScale = new Vector3(-220f, 220f, 220f);

        Collider domeCollider = starDome.GetComponent<Collider>();
        if (domeCollider != null)
        {
            Destroy(domeCollider);
        }

        Renderer renderer = starDome.GetComponent<Renderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material = CreateUnlitTextureMaterial(starsTexture, new Color(0.8f, 0.86f, 1f));
        renderer.material.mainTextureScale = new Vector2(2f, 1f);
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
        ApplySunVisuals(sun.gameObject);

        SpinMotion sunSpin = sun.gameObject.AddComponent<SpinMotion>();
        sunSpin.Initialize(Vector3.up, 5f);

        Light sunLight = sun.gameObject.AddComponent<Light>();
        sunLight.type = LightType.Point;
        sunLight.range = 55f;
        sunLight.intensity = 2.8f;
        sunLight.color = new Color(1f, 0.75f, 0.45f);

        Transform earthOrbit = CreateOrbitPivot(root.transform, "Earth Orbit", Vector3.zero, new Vector3(0f, 1f, 0.08f), 8.5f);
        CelestialBody earth = CreateBody(
            earthOrbit,
            "Earth",
            new Vector3(9f, 0f, 0f),
            1.7f,
            new Color(0.2f, 0.5f, 1f),
            new Color(0.2f, 0.8f, 1f),
            "Earth is our home. It has blue oceans, white clouds, glowing night lights, and lots of life.",
            false);
        ApplyEarthVisuals(earth.gameObject);

        SpinMotion earthSpin = earth.gameObject.AddComponent<SpinMotion>();
        earthSpin.Initialize(new Vector3(0.12f, 1f, 0f), 28f);

        Transform moonOrbit = CreateOrbitPivot(earthOrbit, "Moon Orbit", earth.transform.localPosition, new Vector3(0.1f, 1f, 0f), 42f);
        CelestialBody moon = CreateBody(
            moonOrbit,
            "Moon",
            new Vector3(2.5f, 0f, 0f),
            0.65f,
            new Color(0.82f, 0.84f, 0.9f),
            new Color(0.9f, 0.92f, 1f),
            "The Moon is Earth's rocky space buddy. Its crater marks were made by ancient impacts.",
            true);
        ApplyMoonVisuals(moon.gameObject);

        SpinMotion moonSpin = moon.gameObject.AddComponent<SpinMotion>();
        moonSpin.Initialize(Vector3.up, 12f);

        Transform marsOrbit = CreateOrbitPivot(root.transform, "Mars Orbit", Vector3.zero, new Vector3(0f, 1f, -0.04f), 5.2f);
        CelestialBody mars = CreateBody(
            marsOrbit,
            "Mars",
            new Vector3(-14f, 0.6f, 0f),
            1.1f,
            new Color(0.85f, 0.36f, 0.18f),
            new Color(1f, 0.42f, 0.18f),
            "Mars is called the red planet because its surface is covered in rusty-looking dust.",
            false);
        ApplyMarsVisuals(mars.gameObject);

        SpinMotion marsSpin = mars.gameObject.AddComponent<SpinMotion>();
        marsSpin.Initialize(new Vector3(-0.1f, 1f, 0.05f), 18f);

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
        renderer.material = CreateLitMaterial(bodyColor, emissionColor, 0.02f, 0.45f);

        CelestialBody body = bodyObject.AddComponent<CelestialBody>();
        body.Initialize(bodyName, fact, emissionColor, isMoon);
        return body;
    }

    private void ApplyEarthVisuals(GameObject earthObject)
    {
        Renderer renderer = earthObject.GetComponent<Renderer>();
        Material earthMaterial = CreateTexturedMaterial(earthDayTexture, Color.white, 0.03f, 0.42f);

        if (earthNightTexture != null)
        {
            earthMaterial.EnableKeyword("_EMISSION");
            earthMaterial.SetTexture("_EmissionMap", earthNightTexture);
            earthMaterial.SetColor("_EmissionColor", new Color(0.25f, 0.38f, 0.7f) * 1.15f);
        }

        renderer.material = earthMaterial;

        if (earthCloudTexture != null)
        {
            GameObject cloudLayer = CreateShell(
                earthObject.transform,
                "Earth Clouds",
                1.03f,
                CreateTransparentMaterial(new Color(1f, 1f, 1f, 0.42f), 0.08f, earthCloudTexture));

            SpinMotion cloudSpin = cloudLayer.AddComponent<SpinMotion>();
            cloudSpin.Initialize(new Vector3(0.08f, 1f, 0f), 34f);
        }

        Material atmosphereMaterial = CreateTransparentMaterial(new Color(0.34f, 0.62f, 1f, 0.14f), 0.55f);
        atmosphereMaterial.EnableKeyword("_EMISSION");
        atmosphereMaterial.SetColor("_EmissionColor", new Color(0.18f, 0.45f, 1f) * 0.35f);
        CreateShell(earthObject.transform, "Earth Atmosphere", 1.08f, atmosphereMaterial);
    }

    private void ApplyMoonVisuals(GameObject moonObject)
    {
        Renderer renderer = moonObject.GetComponent<Renderer>();
        renderer.material = CreateTexturedMaterial(moonTexture, new Color(0.96f, 0.96f, 0.98f), 0f, 0.16f);
    }

    private void ApplyMarsVisuals(GameObject marsObject)
    {
        Renderer renderer = marsObject.GetComponent<Renderer>();
        renderer.material = CreateTexturedMaterial(marsTexture, Color.white, 0.01f, 0.22f);
    }

    private void ApplySunVisuals(GameObject sunObject)
    {
        Renderer renderer = sunObject.GetComponent<Renderer>();
        Material sunMaterial = CreateTexturedMaterial(sunTexture, Color.white, 0f, 0.18f);
        sunMaterial.EnableKeyword("_EMISSION");
        sunMaterial.SetTexture("_EmissionMap", sunTexture);
        sunMaterial.SetColor("_EmissionColor", new Color(1f, 0.56f, 0.16f) * 1.7f);
        renderer.material = sunMaterial;

        Material coronaMaterial = CreateTransparentMaterial(new Color(1f, 0.62f, 0.18f, 0.17f), 0f, sunTexture);
        coronaMaterial.EnableKeyword("_EMISSION");
        coronaMaterial.SetColor("_EmissionColor", new Color(1f, 0.52f, 0.12f) * 0.65f);

        GameObject corona = CreateShell(sunObject.transform, "Sun Corona", 1.18f, coronaMaterial);
        SpinMotion coronaSpin = corona.AddComponent<SpinMotion>();
        coronaSpin.Initialize(Vector3.up, -3f);
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
        Transform cometOrbit = CreateOrbitPivot(root, "Comet Orbit", Vector3.zero, new Vector3(0.2f, 1f, 0f), 15f);
        GameObject comet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        comet.name = "Comet";
        comet.transform.SetParent(cometOrbit, false);
        comet.transform.localPosition = new Vector3(18f, 3.5f, 0f);
        comet.transform.localScale = Vector3.one * 0.4f;

        Renderer renderer = comet.GetComponent<Renderer>();
        renderer.material = CreateLitMaterial(
            new Color(0.82f, 0.94f, 1f),
            new Color(0.5f, 0.8f, 1f),
            0f,
            0.15f);

        Collider cometCollider = comet.GetComponent<Collider>();
        if (cometCollider != null)
        {
            Destroy(cometCollider);
        }

        SpinMotion cometSpin = comet.AddComponent<SpinMotion>();
        cometSpin.Initialize(new Vector3(0.2f, 1f, 0f), 26f);

        TrailRenderer trail = comet.AddComponent<TrailRenderer>();
        trail.time = 2.4f;
        trail.startWidth = 0.28f;
        trail.endWidth = 0.02f;
        trail.material = CreateTrailMaterial(new Color(0.8f, 0.95f, 1f, 0.85f));
        trail.startColor = new Color(0.85f, 0.95f, 1f, 0.9f);
        trail.endColor = new Color(0.4f, 0.7f, 1f, 0f);
    }

    private GameObject CreateShell(Transform parent, string name, float scaleMultiplier, Material material)
    {
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = name;
        shell.transform.SetParent(parent, false);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localRotation = Quaternion.identity;
        shell.transform.localScale = Vector3.one * scaleMultiplier;

        Collider shellCollider = shell.GetComponent<Collider>();
        if (shellCollider != null)
        {
            Destroy(shellCollider);
        }

        Renderer renderer = shell.GetComponent<Renderer>();
        renderer.material = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return shell;
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
        panel.color = new Color(0.02f, 0.045f, 0.12f, 0.86f);

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
        factText.text = "Click Earth or the Moon to zoom in, hear space sounds, and explore a more realistic solar system scene. The Sun and Mars also have upgraded textures.";
        factText.color = new Color(0.9f, 0.95f, 1f);
        hintText.text = "Try Earth first to see the cloud layer and glowing night lights, then press Return to fly back.";
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

        bodyTitleText.text = body.BodyName;
        bodyTitleText.color = body.AccentColor;
        factText.text = body.FactText;
        factText.color = new Color(0.94f, 0.97f, 1f);
        hintText.text = "Click another world to compare it, or press Return / Esc to fly back to the main view.";

        returnButton.gameObject.SetActive(true);
        selectionSource.PlayOneShot(body.IsMoon ? moonTone : planetTone);
    }

    private void ReturnToOverview()
    {
        selectedBody = null;
        returnButton.gameObject.SetActive(false);
        selectionSource.PlayOneShot(returnTone);
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
        Material material = new Material(Shader.Find("Standard"));
        material.color = baseColor;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissionColor * 0.45f);
        return material;
    }

    private Material CreateTexturedMaterial(Texture2D albedoTexture, Color tint, float metallic, float smoothness)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = tint;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);

        if (albedoTexture != null)
        {
            material.SetTexture("_MainTex", albedoTexture);
        }

        return material;
    }

    private Material CreateTransparentMaterial(Color tint, float smoothness, Texture2D mainTexture = null)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = tint;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", smoothness);

        if (mainTexture != null)
        {
            material.SetTexture("_MainTex", mainTexture);
        }

        material.SetFloat("_Mode", 2f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    private Material CreateUnlitTextureMaterial(Texture2D texture, Color tint)
    {
        Shader shader = Shader.Find("Unlit/Texture");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);

        if (texture != null)
        {
            material.mainTexture = texture;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }

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

    private Texture2D LoadTextureFromStreamingAssets(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, SolarSystemAssetFolder, fileName);

        if (!File.Exists(path))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes, false);
        texture.name = Path.GetFileNameWithoutExtension(fileName);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    private AudioClip CreateAmbienceClip(string clipName, float duration)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float drone = Mathf.Sin(2f * Mathf.PI * 55f * time) * 0.45f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 110f * time) * 0.18f;
            float air = Mathf.Sin(2f * Mathf.PI * 220f * time) * 0.08f;
            samples[i] = (drone + shimmer + air) * 0.12f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSelectionClip(string clipName, float startFrequency, float endFrequency, float duration)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float time = i / (float)sampleRate;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * time);
            float harmonics = Mathf.Sin(2f * Mathf.PI * frequency * 2f * time) * 0.24f;
            samples[i] = (wave + harmonics) * envelope * 0.26f;
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
