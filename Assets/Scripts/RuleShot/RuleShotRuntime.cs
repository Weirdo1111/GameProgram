using System.Collections.Generic;
using UnityEngine;

public enum RuleShotType
{
    Heavy,
    Light,
    Freeze
}

public enum RuleAffectableKind
{
    Box,
    FragileFloor,
    MovingPlatform
}

public enum RuleEnemyKind
{
    StaticGuard,
    PatrolBot,
    ShieldExecutor
}

public sealed class RuleShotGameController : MonoBehaviour
{
    public static RuleShotGameController Instance { get; private set; }

    public int firstZone;
    public RuleShotPlayerController player;
    public RuleGunController gun;

    private readonly string[] zoneNames =
    {
        "区域一：基础引导",
        "区域二：轻化平台",
        "区域三：冻结危险",
        "区域四：组合挑战",
        "区域五：终局井道"
    };

    private readonly string[] zoneObjectives =
    {
        "用重力弹让箱子压下压力板，打开第一道门。",
        "用轻化弹让箱子被风道托起，踩着它上到高平台。",
        "用冻结弹停住锯片、激光门和巡逻机械体。",
        "轻化箱子进风道，再重力压住右侧开关，同时冻结危险装置。",
        "串联移动、风道、敌人和规则弹，抵达机械井道顶部终点。"
    };

    private GameObject worldRoot;
    private Camera mainCamera;
    private Sprite pixelSprite;
    private Vector3 checkpoint;
    private int currentZone;
    private bool completed;
    private bool titleScreenActive = true;
    private float titleTimer;
    private GameObject titleRoot;
    private RuleCameraFollow cameraFollow;
    private string feedback = "A/D 移动，Space 跳跃，Shift 冲刺，鼠标左键发射规则弹。";
    private float feedbackTimer;
    private Texture2D homeScreenTexture;
    private Texture2D storeScreenTexture;
    private readonly Dictionary<string, Texture2D> homeArt = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, Texture2D> gameplayArt = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, Sprite> gameplaySprites = new Dictionary<string, Sprite>();
    private ShellScreen shellScreen = ShellScreen.Home;
    private int coins = 1200;
    private int wardrobeTab = 0;
    private int selectedStoreItem;
    private readonly string[] wardrobeTabs = { "ARMOR", "VISOR", "WEAPON" };
    private readonly int[] equippedWardrobeOptions = new int[3];
    private readonly string[][] wardrobeOptionNames =
    {
        new[] { "NIGHTRUN", "TITAN", "PHANTOM", "AERO", "NOVA" },
        new[] { "CYAN", "MAGENTA", "GOLD", "EMERALD", "CRIMSON" },
        new[] { "RIFLE", "CARBINE", "CANNON", "SMG", "LANCE" }
    };
    private readonly string[] storeNames =
    {
        "PLASMA CANNON",
        "WEAPON ATTACH",
        "ION BARREL",
        "PULSE SKIN",
        "FIELD AGENT",
        "STEALTH RIG",
        "CREDIT CACHE",
        "COIN POUCH",
        "COIN",
        "CRYSTAL",
        "ENERGY CELL",
        "POWER CORE"
    };
    private readonly int[] storePrices = { 500, 300, 300, 200, 300, 200, 200, 300, 300, 400, 200, 500 };

    private enum ShellScreen
    {
        Home,
        Store
    }

    private enum WardrobeCategory
    {
        Armor,
        Visor,
        Weapon
    }

    public Color HeavyColor { get; private set; } = new Color(1f, 0.34f, 0.12f);
    public Color LightColor { get; private set; } = new Color(0.18f, 1f, 0.68f);
    public Color FreezeColor { get; private set; } = new Color(0.36f, 0.78f, 1f);
    public Color DangerColor { get; private set; } = new Color(1f, 0.1f, 0.18f);
    public Color MetalColor { get; private set; } = new Color(0.13f, 0.16f, 0.2f);

    private void Awake()
    {
        Instance = this;
        pixelSprite = BuildPixelSprite();
    }

    private void Start()
    {
        LoadShellArt();
        ConfigureScene();
        BuildWorld();
    }

    private void Update()
    {
        titleTimer += Time.deltaTime;

        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
        }

        if (titleScreenActive)
        {
            AnimateTitleDecor();
            if (shellScreen == ShellScreen.Home && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                StartRun();
            }
            else if (shellScreen == ShellScreen.Store && Input.GetKeyDown(KeyCode.Escape))
            {
                shellScreen = ShellScreen.Home;
            }

            return;
        }

        if (player == null)
        {
            return;
        }

        int newZone = Mathf.Clamp(Mathf.FloorToInt((player.transform.position.x + 24f) / 32f), 0, 4);
        if (newZone != currentZone)
        {
            currentZone = newZone;
            SetCheckpoint(player.transform.position);
            ShowFeedback(zoneObjectives[currentZone], 4f);
        }
    }

    public Sprite PixelSprite
    {
        get { return pixelSprite; }
    }

    public Sprite GetGameplaySprite(string key)
    {
        Sprite sprite;
        gameplaySprites.TryGetValue(key, out sprite);
        return sprite;
    }

    public int CurrentZone
    {
        get { return currentZone; }
    }

    public string CurrentObjective
    {
        get { return completed ? "Vertical slice complete. 按 R 可重新挑战。" : zoneObjectives[currentZone]; }
    }

    public Color GetShotColor(RuleShotType shotType)
    {
        if (shotType == RuleShotType.Heavy)
        {
            return HeavyColor;
        }

        if (shotType == RuleShotType.Light)
        {
            return LightColor;
        }

        return FreezeColor;
    }

    public string GetShotName(RuleShotType shotType)
    {
        if (shotType == RuleShotType.Heavy)
        {
            return "重力弹 Heavy";
        }

        if (shotType == RuleShotType.Light)
        {
            return "轻化弹 Light";
        }

        return "冻结弹 Freeze";
    }

    public string GetShotHint(RuleShotType shotType)
    {
        if (shotType == RuleShotType.Heavy)
        {
            return "加重箱子、压开关、击碎脆弱地板。";
        }

        if (shotType == RuleShotType.Light)
        {
            return "减轻箱子，让它被风道托起或更容易推动。";
        }

        return "暂停锯片、激光门、巡逻敌人和移动机关。";
    }

    public void SpawnProjectile(Vector2 origin, Vector2 direction, RuleShotType shotType)
    {
        GameObject projectile = CreateVisualBox("Rule Projectile", origin, new Vector2(0.26f, 0.26f), GetShotColor(shotType), 18);
        RuleProjectile controller = projectile.AddComponent<RuleProjectile>();
        controller.Initialize(this, shotType, direction.normalized);
        ShowFlash(origin, GetShotColor(shotType), 0.35f);
    }

    public void ShowFeedback(string text, float seconds = 2.4f)
    {
        feedback = text;
        feedbackTimer = seconds;
    }

    public void ShowFlash(Vector2 position, Color color, float size = 0.5f)
    {
        string artKey = color.b > color.r + 0.12f ? "fx_freeze" : "fx_muzzle";
        GameObject flash = CreateGameplaySpriteBox("Rule Flash", position, Vector2.one * size * 1.8f, artKey, new Color(color.r, color.g, color.b, 0.92f), 30);
        CreateVisualChild(flash.transform, "Rule Flash Core", Vector2.zero, Vector2.one * size * 0.28f, Color.white, 31);
        flash.AddComponent<RuleAutoDestroy>().lifetime = 0.18f;
    }

    public void DamagePlayer(string reason)
    {
        if (completed)
        {
            return;
        }

        ShowFeedback(reason + "  已回到最近检查点。", 2.4f);
        RestartFromCheckpoint();
    }

    public void RestartFromCheckpoint()
    {
        if (player == null)
        {
            return;
        }

        player.ResetTo(checkpoint);
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpoint = position;
    }

    public void CompleteGame()
    {
        completed = true;
        ShowFeedback("RuleShot 初版完成：你打通了五个区域。", 999f);
    }

    public GameObject CreateVisualBox(string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject box = new GameObject(objectName);
        box.transform.SetParent(worldRoot.transform, false);
        box.transform.position = position;
        box.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
        renderer.sprite = pixelSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return box;
    }

    public GameObject CreateSolidBox(string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder = 0)
    {
        GameObject box = CreateVisualBox(objectName, position, size, color, sortingOrder);
        BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        if (ShouldDecorateSolidBox(objectName))
        {
            AddPlatformSkin(box, size, GetAccentColorForObject(objectName));
        }

        return box;
    }

    private GameObject CreateGameplaySpriteBox(string objectName, Vector2 position, Vector2 size, string artKey, Color color, int sortingOrder)
    {
        GameObject box = new GameObject(objectName);
        box.transform.SetParent(worldRoot.transform, false);
        box.transform.position = position;

        SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        ApplyArtToRenderer(renderer, artKey, size, color);
        return box;
    }

    private GameObject CreateVisualChild(Transform parent, string objectName, Vector2 localPosition, Vector2 size, Color color, int sortingOrder)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = pixelSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return child;
    }

    private GameObject CreateGameplayChild(Transform parent, string objectName, Vector2 localPosition, Vector2 size, string artKey, Color color, int sortingOrder)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        ApplyArtToRenderer(renderer, artKey, size, color);
        return child;
    }

    private void ApplyArtToRenderer(SpriteRenderer renderer, string artKey, Vector2 size, Color color)
    {
        Sprite sprite = GetGameplaySprite(artKey);
        if (sprite == null)
        {
            renderer.sprite = pixelSprite;
            renderer.color = color;
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            return;
        }

        renderer.sprite = sprite;
        renderer.color = color;
        Vector2 spriteSize = sprite.bounds.size;
        float sx = size.x / Mathf.Max(spriteSize.x, 0.01f);
        float sy = size.y / Mathf.Max(spriteSize.y, 0.01f);
        renderer.transform.localScale = new Vector3(sx, sy, 1f);
    }

    private bool ShouldDecorateSolidBox(string objectName)
    {
        return objectName.Contains("Walkway") ||
            objectName.Contains("Platform") ||
            objectName.Contains("Wall") ||
            objectName.Contains("Ledge") ||
            objectName.Contains("Floor");
    }

    private Color GetAccentColorForObject(string objectName)
    {
        if (objectName.Contains("Z1") || objectName.Contains("Heavy"))
        {
            return HeavyColor;
        }

        if (objectName.Contains("Z2") || objectName.Contains("Light") || objectName.Contains("Wind"))
        {
            return LightColor;
        }

        if (objectName.Contains("Z3") || objectName.Contains("Freeze"))
        {
            return FreezeColor;
        }

        if (objectName.Contains("Combo") || objectName.Contains("Z4"))
        {
            return new Color(1f, 0.8f, 0.18f);
        }

        if (objectName.Contains("Shaft") || objectName.Contains("Final") || objectName.Contains("Z5"))
        {
            return new Color(1f, 0.18f, 0.58f);
        }

        return new Color(0.22f, 0.86f, 0.96f);
    }

    private void AddPlatformSkin(GameObject target, Vector2 size, Color accent)
    {
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        int baseOrder = renderer.sortingOrder;
        Color baseColor = renderer.color;
        renderer.color = new Color(baseColor.r * 0.9f, baseColor.g * 0.95f, baseColor.b * 1.08f, 1f);

        CreateGameplayChild(target.transform, "Panel Surface", Vector2.zero, new Vector2(size.x, Mathf.Max(size.y, 0.72f)), "platform_tile", Color.white, baseOrder + 1);

        if (size.x >= size.y)
        {
            CreateVisualChild(target.transform, "Top Rail", new Vector2(0f, size.y * 0.42f), new Vector2(size.x * 0.94f, Mathf.Min(0.1f, size.y * 0.24f)), accent, baseOrder + 2);
            CreateVisualChild(target.transform, "Bottom Shadow", new Vector2(0f, -size.y * 0.42f), new Vector2(size.x * 0.96f, Mathf.Min(0.14f, size.y * 0.32f)), new Color(0.01f, 0.02f, 0.05f, 0.75f), baseOrder + 1);

            int lampCount = Mathf.Clamp(Mathf.RoundToInt(size.x / 2.4f), 1, 9);
            for (int i = 0; i < lampCount; i++)
            {
                float t = lampCount == 1 ? 0.5f : i / (float)(lampCount - 1);
                float x = Mathf.Lerp(-size.x * 0.38f, size.x * 0.38f, t);
                CreateVisualChild(target.transform, "Signal Lamp", new Vector2(x, size.y * 0.16f), new Vector2(0.18f, 0.06f), new Color(accent.r, accent.g, accent.b, 0.92f), baseOrder + 3);
            }
        }
        else
        {
            CreateVisualChild(target.transform, "Wall Rail", new Vector2(0f, 0f), new Vector2(Mathf.Min(size.x * 0.34f, 0.16f), size.y * 0.92f), accent, baseOrder + 2);
        }
    }

    private void BuildBackdropPanels(string artKey, float startX, int count, float step, Vector2 size, float baseY, int sortingOrder, Color tint)
    {
        for (int i = 0; i < count; i++)
        {
            float x = startX + i * step;
            float y = baseY + Mathf.Sin(i * 0.65f) * 0.35f;
            CreateGameplaySpriteBox("Backdrop " + artKey, new Vector2(x, y), size, artKey, tint, sortingOrder);
        }
    }

    private void BuildCharacterRig(GameObject actor, Color armorColor, Color visorColor, Color accentColor, int sortingBase, bool shielded)
    {
        CreateVisualChild(actor.transform, "Torso Plate", new Vector2(0f, -0.02f), new Vector2(0.62f, 0.78f), armorColor, sortingBase + 1);
        CreateVisualChild(actor.transform, "Chest Core", new Vector2(0f, 0.03f), new Vector2(0.28f, 0.18f), accentColor, sortingBase + 3);
        CreateVisualChild(actor.transform, "Helmet", new Vector2(0f, 0.34f), new Vector2(0.46f, 0.34f), armorColor * 1.08f, sortingBase + 2);
        CreateVisualChild(actor.transform, "Visor", new Vector2(0f, 0.31f), new Vector2(0.34f, 0.12f), visorColor, sortingBase + 4);
        CreateVisualChild(actor.transform, "Leg Left", new Vector2(-0.14f, -0.42f), new Vector2(0.18f, 0.42f), armorColor * 0.9f, sortingBase + 1);
        CreateVisualChild(actor.transform, "Leg Right", new Vector2(0.14f, -0.42f), new Vector2(0.18f, 0.42f), armorColor * 0.9f, sortingBase + 1);
        CreateVisualChild(actor.transform, "Arm Left", new Vector2(-0.31f, 0.02f), new Vector2(0.14f, 0.5f), armorColor * 0.92f, sortingBase + 1);
        CreateVisualChild(actor.transform, "Arm Right", new Vector2(0.31f, 0.02f), new Vector2(0.14f, 0.5f), armorColor * 0.92f, sortingBase + 1);
        CreateVisualChild(actor.transform, "Boot Glow", new Vector2(0f, -0.56f), new Vector2(0.42f, 0.05f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.82f), sortingBase + 3);
        if (shielded)
        {
            CreateVisualChild(actor.transform, "Shield Ring", new Vector2(0f, 0.04f), new Vector2(0.96f, 1.22f), new Color(1f, 0.82f, 0.22f, 0.32f), sortingBase);
        }
    }

    private void ConfigureScene()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.name = "RuleShot Camera";
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 7.4f;
        mainCamera.transform.position = new Vector3(-16f, 3.2f, -10f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.025f, 0.03f, 0.055f);
    }

    private void BuildWorld()
    {
        if (worldRoot != null)
        {
            Destroy(worldRoot);
        }

        worldRoot = new GameObject("RuleShot 2D Runtime");
        BuildBackground();
        BuildLevelGeometry();
        BuildPlayer();
        currentZone = Mathf.Clamp(firstZone, 0, 4);
        Vector3 zoneStart = GetZoneStart(currentZone);
        player.ResetTo(zoneStart);
        SetCheckpoint(zoneStart);
        ShowFeedback(zoneObjectives[currentZone], 4f);

        RuleCameraFollow follow = mainCamera.gameObject.AddComponent<RuleCameraFollow>();
        cameraFollow = follow;
        follow.target = player.transform;
        follow.minX = -18f;
        follow.maxX = 132f;
        follow.minY = 1.5f;
        follow.maxY = 17f;

        EnterTitleScreen();
    }

    private void EnterTitleScreen()
    {
        titleScreenActive = true;
        shellScreen = ShellScreen.Home;
        titleTimer = 0f;
        completed = false;
        if (player != null)
        {
            player.SetControlLocked(true);
        }

        if (gun != null)
        {
            gun.SetControlLocked(true);
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }

        mainCamera.transform.position = new Vector3(-7.5f, 4.7f, -10f);
        BuildTitleDecor();
    }

    private void StartRun()
    {
        titleScreenActive = false;
        if (titleRoot != null)
        {
            Destroy(titleRoot);
        }

        if (player != null)
        {
            player.SetControlLocked(false);
            player.ResetTo(checkpoint);
        }

        if (gun != null)
        {
            gun.SetControlLocked(false);
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled = true;
        }

        ShowFeedback("Rule gun online. Change rules, cross the city.", 3.2f);
    }

    private void BuildTitleDecor()
    {
        if (titleRoot != null)
        {
            Destroy(titleRoot);
        }

        titleRoot = new GameObject("RuleShot Title Screen Decor");
        titleRoot.transform.SetParent(worldRoot.transform, false);

        CreateTitleBox("Title Backplate", new Vector2(-7.5f, 4.9f), new Vector2(24f, 11.8f), new Color(0.015f, 0.022f, 0.045f, 0.96f), 80);
        CreateTitleBox("Title Top Rail", new Vector2(-7.5f, 10.45f), new Vector2(22f, 0.14f), FreezeColor, 84);
        CreateTitleBox("Title Bottom Rail", new Vector2(-7.5f, -0.55f), new Vector2(22f, 0.14f), LightColor, 84);
        CreateTitleBox("Title Left Rail", new Vector2(-18.4f, 4.95f), new Vector2(0.14f, 10.6f), HeavyColor, 84);
        CreateTitleBox("Title Right Rail", new Vector2(3.4f, 4.95f), new Vector2(0.14f, 10.6f), FreezeColor, 84);

        for (int i = 0; i < 18; i++)
        {
            float x = -17.4f + i * 1.15f;
            Color color = i % 3 == 0 ? HeavyColor : (i % 3 == 1 ? LightColor : FreezeColor);
            CreateTitleBox("Pixel Skyline", new Vector2(x, 0.25f + (i % 5) * 0.18f), new Vector2(0.56f, 1.2f + (i % 4) * 0.5f), new Color(color.r * 0.2f, color.g * 0.22f, color.b * 0.28f, 0.86f), 82);
            CreateTitleBox("Pixel Window", new Vector2(x, 1.55f + (i % 3) * 0.44f), new Vector2(0.32f, 0.08f), color, 85);
        }

        for (int i = 0; i < 15; i++)
        {
            float x = -17.9f + i * 1.45f;
            CreateTitleBox("Grid Vertical", new Vector2(x, 4.4f), new Vector2(0.035f, 8.8f), new Color(0.1f, 0.72f, 0.9f, 0.22f), 81);
        }

        for (int i = 0; i < 9; i++)
        {
            float y = 0.15f + i * 1.05f;
            CreateTitleBox("Grid Horizontal", new Vector2(-7.5f, y), new Vector2(21.4f, 0.035f), new Color(0.1f, 0.72f, 0.9f, 0.18f), 81);
        }

        CreateTitleBox("Rule Core", new Vector2(-14.6f, 6.55f), new Vector2(1.2f, 1.2f), HeavyColor, 86);
        CreateTitleBox("Rule Core Inner", new Vector2(-14.6f, 6.55f), new Vector2(0.62f, 0.62f), new Color(0.02f, 0.025f, 0.05f), 87);
        CreateTitleBox("Light Core", new Vector2(-12.6f, 6.55f), new Vector2(1.2f, 1.2f), LightColor, 86);
        CreateTitleBox("Light Core Inner", new Vector2(-12.6f, 6.55f), new Vector2(0.62f, 0.62f), new Color(0.02f, 0.025f, 0.05f), 87);
        CreateTitleBox("Freeze Core", new Vector2(-10.6f, 6.55f), new Vector2(1.2f, 1.2f), FreezeColor, 86);
        CreateTitleBox("Freeze Core Inner", new Vector2(-10.6f, 6.55f), new Vector2(0.62f, 0.62f), new Color(0.02f, 0.025f, 0.05f), 87);

        for (int i = 0; i < 10; i++)
        {
            float x = -16.9f + i * 2.05f;
            CreateTitleBox("Data Tick", new Vector2(x, 9.55f), new Vector2(0.6f + (i % 3) * 0.3f, 0.08f), i % 2 == 0 ? FreezeColor : LightColor, 86);
        }
    }

    private GameObject CreateTitleBox(string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject box = CreateVisualBox(objectName, position, size, color, sortingOrder);
        box.transform.SetParent(titleRoot.transform, true);
        return box;
    }

    private void AnimateTitleDecor()
    {
        if (titleRoot == null)
        {
            return;
        }

        SpriteRenderer[] renderers = titleRoot.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].name.Contains("Data Tick") || renderers[i].name.Contains("Pixel Window"))
            {
                Color color = renderers[i].color;
                color.a = 0.45f + Mathf.PingPong(titleTimer * 0.9f + i * 0.13f, 0.55f);
                renderers[i].color = color;
            }
        }
    }

    private void BuildBackground()
    {
        CreateVisualBox("Back Sky Band", new Vector2(54f, 6f), new Vector2(190f, 32f), new Color(0.035f, 0.04f, 0.075f), -50);
        CreateVisualBox("Sky Bloom", new Vector2(54f, 10.5f), new Vector2(190f, 12f), new Color(0.06f, 0.1f, 0.18f, 0.4f), -49);
        CreateVisualBox("Far Neon Grid", new Vector2(54f, -2.8f), new Vector2(190f, 0.12f), new Color(0.05f, 0.32f, 0.45f), -45);

        if (GetGameplaySprite("bg_lab_back") != null)
        {
            BuildBackdropPanels("bg_lab_back", -18f, 12, 15.6f, new Vector2(19f, 10f), 4.8f, -48, new Color(0.4f, 0.78f, 0.96f, 0.24f));
            BuildBackdropPanels("bg_lab_middle", -14f, 11, 17.2f, new Vector2(21f, 12f), 2.8f, -43, new Color(0.36f, 0.94f, 1f, 0.34f));
            BuildBackdropPanels("bg_lab_front", -10f, 10, 18.8f, new Vector2(17f, 8f), -0.25f, -37, new Color(0.7f, 0.92f, 1f, 0.24f));
        }
        else
        {
            for (int i = 0; i < 26; i++)
            {
                float x = -28f + i * 7.2f;
                float height = 5f + (i % 5) * 1.4f;
                Color color = i % 2 == 0 ? new Color(0.055f, 0.065f, 0.1f) : new Color(0.075f, 0.055f, 0.11f);
                CreateVisualBox("Distant Building", new Vector2(x, height * 0.5f - 2f), new Vector2(4.2f, height), color, -42);
                Color signColor = i % 3 == 0 ? HeavyColor : (i % 3 == 1 ? LightColor : FreezeColor);
                CreateVisualBox("Neon Sign", new Vector2(x, height - 1f), new Vector2(2.5f, 0.18f), signColor * 0.85f, -41);
            }
        }

        for (int i = 0; i < 12; i++)
        {
            float x = -22f + i * 14f;
            CreateVisualBox("Foreground Cable", new Vector2(x, 9.8f + Mathf.Sin(i) * 1.2f), new Vector2(10f, 0.08f), new Color(0.1f, 0.65f, 0.8f, 0.7f), -35);
        }

        for (int i = 0; i < 18; i++)
        {
            float x = -23f + i * 9.6f;
            float width = 2.1f + (i % 3) * 0.45f;
            float height = 2.4f + (i % 4) * 1.05f;
            CreateVisualBox("Foreground Tower", new Vector2(x, height * 0.5f - 0.85f), new Vector2(width, height), new Color(0.02f, 0.045f, 0.08f, 0.76f), -34);
            CreateVisualBox("Tower Glow", new Vector2(x, height - 0.65f), new Vector2(width * 0.7f, 0.08f), i % 2 == 0 ? FreezeColor : LightColor, -33);
        }
    }

    private void BuildLevelGeometry()
    {
        CreateSolidBox("Main Walkway A", new Vector2(-5f, -1.2f), new Vector2(45f, 1.2f), MetalColor);
        CreateSolidBox("Main Walkway B", new Vector2(42f, -1.2f), new Vector2(54f, 1.2f), MetalColor);
        CreateSolidBox("Main Walkway C", new Vector2(96f, -1.2f), new Vector2(52f, 1.2f), MetalColor);
        CreateSolidBox("Final Shaft Left Wall", new Vector2(110f, 6.4f), new Vector2(1f, 15f), MetalColor);
        CreateSolidBox("Final Shaft Right Wall", new Vector2(132f, 6.4f), new Vector2(1f, 15f), MetalColor);

        BuildZoneOne();
        BuildZoneTwo();
        BuildZoneThree();
        BuildZoneFour();
        BuildZoneFive();
    }

    private void BuildZoneOne()
    {
        CreateLabelBar("Z1 Neon Header", new Vector2(-11f, 5.8f), HeavyColor);
        RuleDoor door = CreateDoor("Door Z1", new Vector2(6f, 1.45f), new Vector2(0.9f, 4.2f));
        RulePressurePlate plate = CreatePressurePlate("Heavy Plate Z1", new Vector2(0f, -0.45f), door, 4.5f);
        RuleAffectable box = CreateRuleBox("Training Box", new Vector2(-8f, 0.2f));
        box.startingHint = "对箱子发射重力弹，它会变重并压开压力板。";
        CreateSolidBox("Zone One Ledge", new Vector2(-16f, 1.7f), new Vector2(7f, 0.55f), new Color(0.16f, 0.2f, 0.25f));
        plate.requiredHint = "需要更重的物体。Q/E 切到重力弹，射箱子。";
    }

    private void BuildZoneTwo()
    {
        CreateLabelBar("Z2 Neon Header", new Vector2(20f, 6.5f), LightColor);
        CreateSolidBox("Upper Platform Z2", new Vector2(27f, 4.4f), new Vector2(14f, 0.55f), new Color(0.16f, 0.2f, 0.25f));
        CreateSolidBox("Exit Platform Z2", new Vector2(39f, 2.9f), new Vector2(7f, 0.55f), new Color(0.16f, 0.2f, 0.25f));
        RuleAffectable box = CreateRuleBox("Light Box", new Vector2(13f, 0.2f));
        box.startingHint = "轻化箱子后，把它推到风道里当临时台阶。";
        CreateWindZone("Wind Lift Z2", new Vector2(21f, 1.55f), new Vector2(4.5f, 5.2f));
        CreateMovingPlatform("Slow Lift Platform", new Vector2(33f, 1.8f), new Vector2(33f, 4f), new Vector2(4.5f, 0.45f), 1.2f);
    }

    private void BuildZoneThree()
    {
        CreateLabelBar("Z3 Neon Header", new Vector2(54f, 6.3f), FreezeColor);
        CreateHazard("Saw A", new Vector2(49f, 0.25f), new Vector2(1.25f, 1.25f), true);
        CreateHazard("Laser Gate Z3", new Vector2(58f, 1.35f), new Vector2(0.45f, 4.1f), true);
        CreateHazard("Saw B", new Vector2(64f, 0.25f), new Vector2(1.25f, 1.25f), true);
        CreateEnemy("Patrol Freeze Tutorial", RuleEnemyKind.PatrolBot, new Vector2(70f, 0.15f), 66f, 76f);
        CreateSolidBox("Freeze Route Ledge", new Vector2(74f, 2.8f), new Vector2(10f, 0.55f), new Color(0.16f, 0.2f, 0.25f));
    }

    private void BuildZoneFour()
    {
        CreateLabelBar("Z4 Neon Header", new Vector2(86f, 7f), new Color(1f, 0.8f, 0.18f));
        RuleDoor door = CreateDoor("Door Z4", new Vector2(103f, 1.45f), new Vector2(0.9f, 4.2f));
        RulePressurePlate plate = CreatePressurePlate("Combo Plate Z4", new Vector2(98f, -0.45f), door, 4.5f);
        plate.requiredHint = "先轻化箱子通过风道，再重力压住这里。";
        RuleAffectable box = CreateRuleBox("Combo Box", new Vector2(78f, 0.2f));
        box.startingHint = "组合谜题：轻化进风道，落点正确后再重力压住开关。";
        CreateWindZone("Combo Wind", new Vector2(86f, 1.55f), new Vector2(4.5f, 5.2f));
        CreateHazard("Combo Laser", new Vector2(92f, 1.35f), new Vector2(0.45f, 4.1f), true);
        CreateSolidBox("Combo Catch Platform", new Vector2(94f, 3.6f), new Vector2(10f, 0.55f), new Color(0.16f, 0.2f, 0.25f));
        CreateFragileFloor("Fragile Floor", new Vector2(84f, 4.15f), new Vector2(5f, 0.4f));
    }

    private void BuildZoneFive()
    {
        CreateLabelBar("Z5 Neon Header", new Vector2(121f, 14.6f), new Color(1f, 0.18f, 0.58f));
        CreateSolidBox("Shaft Platform 1", new Vector2(116f, 1.6f), new Vector2(8f, 0.5f), new Color(0.16f, 0.2f, 0.25f));
        CreateSolidBox("Shaft Platform 2", new Vector2(126f, 4.2f), new Vector2(8f, 0.5f), new Color(0.16f, 0.2f, 0.25f));
        CreateSolidBox("Shaft Platform 3", new Vector2(116f, 7.1f), new Vector2(8f, 0.5f), new Color(0.16f, 0.2f, 0.25f));
        CreateSolidBox("Shaft Platform 4", new Vector2(126f, 10f), new Vector2(8f, 0.5f), new Color(0.16f, 0.2f, 0.25f));
        CreateMovingPlatform("Final Moving Platform", new Vector2(116f, 4.7f), new Vector2(126f, 7.8f), new Vector2(4.8f, 0.45f), 1.6f);
        CreateWindZone("Final Wind", new Vector2(121f, 2.6f), new Vector2(4.8f, 5.6f));
        CreateHazard("Final Laser A", new Vector2(121f, 6.2f), new Vector2(0.45f, 3.6f), true);
        CreateEnemy("Shield Executor", RuleEnemyKind.ShieldExecutor, new Vector2(126f, 4.85f), 124f, 130f);
        CreateEnemy("Final Patrol", RuleEnemyKind.PatrolBot, new Vector2(116f, 7.75f), 113f, 119f);
        CreateFinish(new Vector2(127.5f, 12.2f));
    }

    private void BuildPlayer()
    {
        GameObject playerObject = CreateVisualBox("Rule Hunter", GetZoneStart(firstZone), new Vector2(0.72f, 1.16f), new Color(0.13f, 0.17f, 0.22f), 12);
        playerObject.AddComponent<BoxCollider2D>();
        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 3.3f;
        body.mass = 1f;

        BuildCharacterRig(playerObject, new Color(0.34f, 0.42f, 0.5f), new Color(0.14f, 0.88f, 1f), new Color(1f, 0.48f, 0.2f), 12, false);

        player = playerObject.AddComponent<RuleShotPlayerController>();
        player.game = this;

        gun = playerObject.AddComponent<RuleGunController>();
        gun.game = this;
        gun.player = player;
        player.gun = gun;
    }

    private Vector3 GetZoneStart(int zone)
    {
        switch (Mathf.Clamp(zone, 0, 4))
        {
            case 1:
                return new Vector3(9.5f, 0.2f, 0f);
            case 2:
                return new Vector3(45f, 0.2f, 0f);
            case 3:
                return new Vector3(76f, 0.2f, 0f);
            case 4:
                return new Vector3(112.5f, 0.2f, 0f);
            default:
                return new Vector3(-20f, 0.2f, 0f);
        }
    }

    private RuleAffectable CreateRuleBox(string objectName, Vector2 position)
    {
        GameObject box = CreateGameplaySpriteBox(objectName, position, new Vector2(1.35f, 1.35f), "crate_box", Color.white, 8);
        BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        Rigidbody2D body = box.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 2.5f;
        body.mass = 1.2f;

        RuleAffectable affectable = box.AddComponent<RuleAffectable>();
        affectable.game = this;
        affectable.kind = RuleAffectableKind.Box;
        return affectable;
    }

    private void CreateFragileFloor(string objectName, Vector2 position, Vector2 size)
    {
        GameObject floor = CreateSolidBox(objectName, position, size, new Color(0.38f, 0.23f, 0.28f), 4);
        CreateVisualChild(floor.transform, "Fragile Crack A", new Vector2(-size.x * 0.18f, 0f), new Vector2(size.x * 0.24f, 0.05f), new Color(1f, 0.72f, 0.78f, 0.92f), 7);
        CreateVisualChild(floor.transform, "Fragile Crack B", new Vector2(size.x * 0.14f, 0.02f), new Vector2(size.x * 0.3f, 0.05f), new Color(0.7f, 0.86f, 1f, 0.88f), 7);
        RuleAffectable affectable = floor.AddComponent<RuleAffectable>();
        affectable.game = this;
        affectable.kind = RuleAffectableKind.FragileFloor;
    }

    private RuleDoor CreateDoor(string objectName, Vector2 position, Vector2 size)
    {
        GameObject doorObject = CreateGameplaySpriteBox(objectName, position, new Vector2(1.6f, size.y + 0.7f), "door_locked", Color.white, 6);
        BoxCollider2D collider = doorObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(size.x, size.y);
        RuleDoor door = doorObject.AddComponent<RuleDoor>();
        door.closedPosition = position;
        door.openOffset = Vector2.up * (size.y + 0.6f);
        door.closedSprite = GetGameplaySprite("door_locked");
        door.openSprite = GetGameplaySprite("door_open");
        CreateVisualChild(doorObject.transform, "Door Glow", new Vector2(0f, size.y * 0.36f), new Vector2(0.58f, 0.14f), DangerColor, 7);
        return door;
    }

    private RulePressurePlate CreatePressurePlate(string objectName, Vector2 position, RuleDoor door, float requiredMass)
    {
        GameObject plateObject = CreateVisualBox(objectName, position, new Vector2(2.5f, 0.22f), new Color(0.17f, 0.2f, 0.24f), 7);
        BoxCollider2D collider = plateObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        CreateVisualChild(plateObject.transform, "Plate Rail", Vector2.zero, new Vector2(2.3f, 0.1f), HeavyColor, 8);
        GameObject indicator = CreateGameplayChild(plateObject.transform, "Indicator Sprite", new Vector2(0f, 1.06f), new Vector2(0.82f, 1.06f), "switch_off", Color.white, 8);
        RulePressurePlate plate = plateObject.AddComponent<RulePressurePlate>();
        plate.game = this;
        plate.targetDoor = door;
        plate.requiredMass = requiredMass;
        plate.closedIndicator = GetGameplaySprite("switch_off");
        plate.openIndicator = GetGameplaySprite("switch_on");
        plate.indicatorRenderer = indicator.GetComponent<SpriteRenderer>();
        return plate;
    }

    private void CreateWindZone(string objectName, Vector2 position, Vector2 size)
    {
        GameObject wind = CreateVisualBox(objectName, position, size, new Color(0.08f, 0.9f, 0.65f, 0.28f), 2);
        BoxCollider2D collider = wind.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        CreateGameplayChild(wind.transform, "Wind Burst", new Vector2(0f, 0.18f), new Vector2(size.x * 0.82f, size.y * 0.84f), "fx_smoke", new Color(0.62f, 1f, 0.92f, 0.22f), 3);
        CreateGameplayChild(wind.transform, "Wind Spark", new Vector2(0f, 0.62f), new Vector2(size.x * 0.48f, size.y * 0.42f), "fx_freeze", new Color(0.44f, 1f, 0.86f, 0.16f), 4);
        RuleWindZone windZone = wind.AddComponent<RuleWindZone>();
        windZone.force = 46f;
        windZone.game = this;
    }

    private void CreateMovingPlatform(string objectName, Vector2 start, Vector2 end, Vector2 size, float speed)
    {
        GameObject platform = CreateSolidBox(objectName, start, size, new Color(0.18f, 0.26f, 0.32f), 5);
        CreateVisualChild(platform.transform, "Mover Rail", new Vector2(0f, 0f), new Vector2(size.x * 0.22f, size.y * 0.86f), FreezeColor, 8);
        RuleMovingPlatform mover = platform.AddComponent<RuleMovingPlatform>();
        mover.start = start;
        mover.end = end;
        mover.speed = speed;

        RuleAffectable affectable = platform.AddComponent<RuleAffectable>();
        affectable.game = this;
        affectable.kind = RuleAffectableKind.MovingPlatform;
        affectable.movingPlatform = mover;
    }

    private void CreateHazard(string objectName, Vector2 position, Vector2 size, bool freezeable)
    {
        bool isSaw = objectName.Contains("Saw");
        GameObject hazard = isSaw
            ? CreateGameplaySpriteBox(objectName, position, size * 1.38f, "hazard_saw", Color.white, 9)
            : CreateVisualBox(objectName, position, size, DangerColor, 9);
        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        if (!isSaw)
        {
            CreateVisualChild(hazard.transform, "Laser Core", Vector2.zero, new Vector2(size.x * 0.45f, size.y * 0.94f), new Color(1f, 0.85f, 0.92f, 0.92f), 10);
            CreateVisualChild(hazard.transform, "Laser Glow", Vector2.zero, new Vector2(size.x * 1.8f, size.y), new Color(1f, 0.18f, 0.3f, 0.24f), 8);
        }

        RuleHazard hazardController = hazard.AddComponent<RuleHazard>();
        hazardController.game = this;
        hazardController.freezeable = freezeable;
        hazardController.spinVisual = isSaw;
    }

    private void CreateEnemy(string objectName, RuleEnemyKind kind, Vector2 position, float leftBound, float rightBound)
    {
        Color armor = kind == RuleEnemyKind.ShieldExecutor ? new Color(0.42f, 0.34f, 0.18f) : new Color(0.24f, 0.3f, 0.36f);
        Color accent = kind == RuleEnemyKind.ShieldExecutor ? new Color(1f, 0.82f, 0.24f) : FreezeColor;
        GameObject enemy = CreateVisualBox(objectName, position, new Vector2(0.9f, 1.25f), new Color(0.1f, 0.12f, 0.16f), 11);
        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        BuildCharacterRig(enemy, armor, new Color(0.92f, 0.98f, 1f), accent, 11, kind == RuleEnemyKind.ShieldExecutor);

        RuleEnemy enemyController = enemy.AddComponent<RuleEnemy>();
        enemyController.game = this;
        enemyController.kind = kind;
        enemyController.leftBound = leftBound;
        enemyController.rightBound = rightBound;
    }

    private void CreateFinish(Vector2 position)
    {
        GameObject finish = CreateGameplaySpriteBox("Finish Gate", position, new Vector2(2.8f, 3.8f), "door_open", new Color(1f, 0.94f, 0.72f), 10);
        BoxCollider2D collider = finish.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        CreateGameplayChild(finish.transform, "Finish Halo", new Vector2(0f, 0f), new Vector2(2.6f, 3.1f), "fx_freeze", new Color(1f, 0.82f, 0.26f, 0.36f), 11);
        RuleFinishGate gate = finish.AddComponent<RuleFinishGate>();
        gate.game = this;
    }

    private void CreateLabelBar(string objectName, Vector2 position, Color color)
    {
        GameObject bar = CreateVisualBox(objectName, position, new Vector2(8f, 0.16f), color, 3);
        CreateVisualChild(bar.transform, "Label Glow", Vector2.zero, new Vector2(8.4f, 0.32f), new Color(color.r, color.g, color.b, 0.18f), 2);
    }

    private Sprite BuildPixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.name = "RuleShot Runtime Pixel";
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void LoadShellArt()
    {
        homeScreenTexture = Resources.Load<Texture2D>("UI/home_screen");
        storeScreenTexture = Resources.Load<Texture2D>("UI/store_screen");

        string[] dynamicHomeAssets =
        {
            "bg_sky_moon",
            "bg_city_far",
            "bg_alley_mid",
            "rain_lines",
            "neon_signs",
            "neon_glow_layers",
            "button_start_normal",
            "button_start_hover",
            "button_start_pressed",
            "button_custom_normal",
            "button_custom_hover",
            "button_custom_pressed",
            "button_store_normal",
            "button_store_hover",
            "button_store_pressed",
            "agent_home",
            "reward_panel",
            "progress_bar",
            "coin_gold",
            "chest_widget"
        };

        homeArt.Clear();
        for (int i = 0; i < dynamicHomeAssets.Length; i++)
        {
            Texture2D texture = Resources.Load<Texture2D>("Home/" + dynamicHomeAssets[i]);
            if (texture != null)
            {
                homeArt[dynamicHomeAssets[i]] = texture;
            }
        }

        string[] dynamicGameplayAssets =
        {
            "bg_lab_back",
            "bg_lab_middle",
            "bg_lab_front",
            "crate_box",
            "door_locked",
            "door_open",
            "hazard_saw",
            "switch_on",
            "switch_off",
            "platform_tile",
            "fx_muzzle",
            "fx_freeze",
            "fx_smoke"
        };

        gameplayArt.Clear();
        gameplaySprites.Clear();
        for (int i = 0; i < dynamicGameplayAssets.Length; i++)
        {
            Texture2D texture = Resources.Load<Texture2D>("Gameplay/" + dynamicGameplayAssets[i]);
            if (texture != null)
            {
                gameplayArt[dynamicGameplayAssets[i]] = texture;
                gameplaySprites[dynamicGameplayAssets[i]] = BuildRuntimeSprite(texture, dynamicGameplayAssets[i]);
            }
        }
    }

    private Sprite BuildRuntimeSprite(Texture2D texture, string spriteName)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
        sprite.name = spriteName;
        return sprite;
    }

    private void OnGUI()
    {
        if (titleScreenActive)
        {
            DrawTitleScreenGUI();
            return;
        }

        if (gun == null)
        {
            return;
        }

        GUI.color = Color.white;
        GUI.skin.label.fontSize = 18;
        GUI.Label(new Rect(18f, 16f, 760f, 28f), "RuleShot | " + zoneNames[currentZone]);
        GUI.Label(new Rect(18f, 44f, 900f, 28f), CurrentObjective);

        Color shotColor = GetShotColor(gun.CurrentShot);
        GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.82f);
        GUI.Box(new Rect(18f, Screen.height - 122f, 430f, 96f), GUIContent.none);
        GUI.color = shotColor;
        GUI.Box(new Rect(32f, Screen.height - 99f, 44f, 44f), GUIContent.none);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 17;
        GUI.Label(new Rect(90f, Screen.height - 108f, 340f, 25f), GetShotName(gun.CurrentShot));
        GUI.skin.label.fontSize = 15;
        GUI.Label(new Rect(90f, Screen.height - 82f, 340f, 25f), GetShotHint(gun.CurrentShot));
        GUI.Label(new Rect(90f, Screen.height - 58f, 340f, 25f), "Q/E 或滚轮切换 | 左键射击 | R 回检查点");

        if (feedbackTimer > 0f)
        {
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 20;
            GUI.Label(new Rect(Screen.width * 0.5f - 360f, Screen.height - 170f, 720f, 32f), feedback);
        }
    }

    private void DrawTitleScreenGUI()
    {
        if (shellScreen == ShellScreen.Store)
        {
            DrawStoreScreen();
        }
        else
        {
            DrawHomeScreen();
        }

        GUI.color = Color.white;
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    private void DrawHomeScreen()
    {
        if (HasDynamicHomeArt())
        {
            DrawDynamicHomeScreen();
            return;
        }

        if (homeScreenTexture != null)
        {
            Rect imageRect = DrawShellTextureCover(homeScreenTexture, 1376f, 768f);
            if (DrawImageHotspot(new Rect(116f, 148f, 384f, 128f), imageRect, 1376f, 768f))
            {
                StartRun();
            }

            if (DrawImageHotspot(new Rect(116f, 319f, 384f, 128f), imageRect, 1376f, 768f))
            {
                shellScreen = ShellScreen.Store;
                wardrobeTab = 0;
            }

            if (DrawImageHotspot(new Rect(116f, 489f, 384f, 128f), imageRect, 1376f, 768f))
            {
                shellScreen = ShellScreen.Store;
            }

            return;
        }

        float scale = GetShellScale();
        float ox = GetShellOffsetX(scale);
        float oy = GetShellOffsetY(scale);
        Rect canvas = new Rect(ox, oy, 1365f * scale, 768f * scale);
        DrawFill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.004f, 0.007f, 0.02f));
        DrawCyberAlley(canvas, scale);

        Rect topBar = SRect(0f, 0f, 1365f, 58f, scale, ox, oy);
        DrawFill(topBar, new Color(0.006f, 0.011f, 0.03f, 0.96f));
        DrawFill(SRect(0f, 56f, 1365f, 3f, scale, ox, oy), new Color(0.88f, 0.98f, 1f));
        DrawPixelIcon(SRect(23f, 20f, 22f, 13f, scale, ox, oy), FreezeColor);
        DrawLabel(SRect(63f, 13f, 360f, 34f, scale, ox, oy), "PIXEL_CORE_OS", 23, FreezeColor, TextAnchor.MiddleLeft, FontStyle.Bold, scale);
        DrawGem(SRect(1247f, 15f, 34f, 28f, scale, ox, oy), scale);
        DrawLabel(SRect(1292f, 12f, 90f, 36f, scale, ox, oy), coins.ToString(), 23, new Color(0.7f, 0.9f, 0.94f), TextAnchor.MiddleLeft, FontStyle.Bold, scale);
        DrawLabel(SRect(7f, 66f, 90f, 18f, scale, ox, oy), "v1.0.1", 12, new Color(0.62f, 0.68f, 0.78f), TextAnchor.MiddleLeft, FontStyle.Normal, scale);

        if (DrawNeonButton(SRect(116f, 148f, 384f, 128f, scale, ox, oy), "START MISSION", new Color(0.91f, 0.22f, 1f), scale))
        {
            StartRun();
        }

        if (DrawNeonButton(SRect(116f, 319f, 384f, 128f, scale, ox, oy), "AGENT\nCUSTOMIZATION", FreezeColor, scale))
        {
            shellScreen = ShellScreen.Store;
            wardrobeTab = 0;
        }

        if (DrawNeonButton(SRect(116f, 489f, 384f, 128f, scale, ox, oy), "CYBER STORE", new Color(0.72f, 0.29f, 1f), scale))
        {
            shellScreen = ShellScreen.Store;
        }

        DrawAgent(SRect(630f, 284f, 150f, 330f, scale, ox, oy), scale);
        DrawRewardPanel(SRect(875f, 620f, 474f, 125f, scale, ox, oy), scale);
    }

    private bool HasDynamicHomeArt()
    {
        return homeArt.ContainsKey("bg_sky_moon") &&
            homeArt.ContainsKey("bg_city_far") &&
            homeArt.ContainsKey("bg_alley_mid") &&
            homeArt.ContainsKey("button_start_normal") &&
            homeArt.ContainsKey("button_custom_normal") &&
            homeArt.ContainsKey("button_store_normal") &&
            homeArt.ContainsKey("agent_home");
    }

    private void DrawDynamicHomeScreen()
    {
        Rect canvas = GetShellCoverRect(1376f, 768f);
        DrawFill(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);
        DrawFill(canvas, new Color(0.01f, 0.015f, 0.035f));

        float s = canvas.width / 1376f;
        float t = titleTimer;

        DrawHomeTexture("bg_sky_moon", new Rect(0f, 58f, 1376f, 150f), canvas, Color.white);
        DrawHomeTexture("bg_city_far", new Rect(-20f - Mathf.PingPong(t * 2f, 10f), 138f, 1416f, 150f), canvas, new Color(0.86f, 0.9f, 1f));
        DrawHomeTexture("bg_alley_mid", new Rect(0f, 216f, 1376f, 452f), canvas, Color.white);
        DrawFill(SRect(0f, 665f, 1376f, 103f, s, canvas.x, canvas.y), new Color(0.018f, 0.035f, 0.045f, 0.72f));

        DrawHomeTexture("rain_lines", new Rect(0f, 62f + Mathf.Repeat(t * 120f, 178f) - 178f, 1376f, 534f), canvas, new Color(0.75f, 0.95f, 1f, 0.48f));
        DrawHomeTexture("rain_lines", new Rect(0f, 62f + Mathf.Repeat(t * 120f, 178f), 1376f, 534f), canvas, new Color(0.75f, 0.95f, 1f, 0.48f));

        DrawFill(SRect(0f, 0f, 1376f, 58f, s, canvas.x, canvas.y), new Color(0.006f, 0.011f, 0.03f, 0.97f));
        DrawFill(SRect(0f, 56f, 1376f, 3f, s, canvas.x, canvas.y), new Color(0.88f, 0.98f, 1f));
        DrawPixelIcon(SRect(23f, 20f, 22f, 13f, s, canvas.x, canvas.y), FreezeColor);
        DrawLabel(SRect(63f, 13f, 360f, 34f, s, canvas.x, canvas.y), "PIXEL_CORE_OS", 23, FreezeColor, TextAnchor.MiddleLeft, FontStyle.Bold, s);
        DrawGem(SRect(1247f, 15f, 34f, 28f, s, canvas.x, canvas.y), s);
        DrawLabel(SRect(1292f, 12f, 90f, 36f, s, canvas.x, canvas.y), coins.ToString(), 23, new Color(0.7f, 0.9f, 0.94f), TextAnchor.MiddleLeft, FontStyle.Bold, s);
        DrawLabel(SRect(7f, 66f, 90f, 18f, s, canvas.x, canvas.y), "v1.0.1", 12, new Color(0.62f, 0.68f, 0.78f), TextAnchor.MiddleLeft, FontStyle.Normal, s);

        if (DrawHomeImageButton(new Rect(116f, 148f, 384f, 128f), canvas, "button_start"))
        {
            StartRun();
        }

        if (DrawHomeImageButton(new Rect(116f, 319f, 384f, 128f), canvas, "button_custom"))
        {
            shellScreen = ShellScreen.Store;
            wardrobeTab = 0;
        }

        if (DrawHomeImageButton(new Rect(116f, 489f, 384f, 128f), canvas, "button_store"))
        {
            shellScreen = ShellScreen.Store;
        }

        float bob = Mathf.Sin(t * 2.4f) * 7f;
        DrawHomeTexture("agent_home", new Rect(634f, 278f + bob, 150f, 343f), canvas, Color.white);
        DrawDynamicRewardPanel(canvas);
    }

    private void DrawDynamicRewardPanel(Rect canvas)
    {
        float s = canvas.width / 1376f;
        Rect panel = SRect(875f, 620f, 474f, 125f, s, canvas.x, canvas.y);
        DrawFill(panel, new Color(0.01f, 0.025f, 0.04f, 0.92f));
        DrawFrame(panel, new Color(0.88f, 1f, 1f), 3f * s);
        DrawLabel(new Rect(panel.x + 22f * s, panel.y + 15f * s, 270f * s, 31f * s), "PLAYTIME REWARDS", 20, FreezeColor, TextAnchor.MiddleLeft, FontStyle.Bold, s);
        DrawHomeTexture("progress_bar", new Rect(899f, 678f, 293f, 34f), canvas, Color.white);
        DrawFill(SRect(904f, 685f, 145f, 20f, s, canvas.x, canvas.y), new Color(0.15f, 0.86f, 0.95f, 0.72f));
        DrawLabel(SRect(899f, 678f, 293f, 34f, s, canvas.x, canvas.y), "2h 15m / 5h", 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, s);

        float coinPulse = 1f + Mathf.Sin(titleTimer * 5f) * 0.05f;
        DrawHomeTexture("coin_gold", new Rect(1160f, 633f, 58f * coinPulse, 58f * coinPulse), canvas, Color.white);
        DrawHomeTexture("chest_widget", new Rect(1243f, 636f, 74f, 82f), canvas, new Color(1f, 1f, 1f, 0.92f + Mathf.Sin(titleTimer * 3f) * 0.08f));
        DrawLabel(SRect(1263f, 709f, 62f, 30f, s, canvas.x, canvas.y), "Coins", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, s);
    }

    private void DrawStoreScreen()
    {
        if (storeScreenTexture != null)
        {
            Rect imageRect = DrawShellTexture(storeScreenTexture, 1152f, 922f);
            bool closeLeft = DrawImageHotspot(new Rect(510f, 98f, 36f, 38f), imageRect, 1152f, 922f);
            bool closeRight = DrawImageHotspot(new Rect(1085f, 98f, 38f, 38f), imageRect, 1152f, 922f);
            bool exit = DrawImageHotspot(new Rect(864f, 854f, 288f, 68f), imageRect, 1152f, 922f);
            if (closeLeft || closeRight || exit)
            {
                shellScreen = ShellScreen.Home;
            }

            return;
        }

        float scale = GetShellScale();
        float ox = GetShellOffsetX(scale);
        float oy = GetShellOffsetY(scale);
        float contentOx = ox + 113f * scale;
        Rect canvas = new Rect(ox, oy, 1365f * scale, 768f * scale);
        DrawFill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.006f, 0.007f, 0.018f));
        DrawStoreBackground(canvas, scale);

        DrawFill(SRect(0f, 0f, 1365f, 58f, scale, ox, oy), new Color(0.006f, 0.01f, 0.028f, 0.97f));
        DrawFill(SRect(0f, 56f, 1365f, 3f, scale, ox, oy), new Color(0.92f, 0.96f, 1f));
        DrawPixelIcon(SRect(17f, 22f, 21f, 13f, scale, ox, oy), FreezeColor);
        DrawLabel(SRect(52f, 13f, 280f, 34f, scale, ox, oy), "PIXEL_CORE_OS", 21, FreezeColor, TextAnchor.MiddleLeft, FontStyle.Bold, scale);
        DrawHudChip(SRect(497f, 10f, 222f, 35f, scale, ox, oy), scale);

        DrawStorePanel(SRect(63f, 82f, 505f, 630f, scale, contentOx, oy), "AGENT WARDROBE", scale);
        DrawStorePanel(SRect(608f, 82f, 505f, 630f, scale, contentOx, oy), "CYBER STORE", scale);

        DrawWardrobeArea(scale, contentOx, oy);
        DrawStoreArea(scale, contentOx, oy);
        DrawBottomNav(scale, ox, oy);
    }

    private void DrawWardrobeArea(float scale, float ox, float oy)
    {
        int activeOption = GetEquippedWardrobeOption(wardrobeTab);
        for (int i = 0; i < 5; i++)
        {
            Rect slot = SRect(72f + i * 92f, 190f, 74f, 88f, scale, ox, oy);
            DrawSlot(slot, i == activeOption ? new Color(1f, 0.86f, 0.55f) : new Color(0.45f, 0.62f, 0.68f), scale);
            DrawWardrobeOptionIcon(slot, wardrobeTab, i, scale);
            if (GUI.Button(slot, GUIContent.none, GUIStyle.none))
            {
                equippedWardrobeOptions[wardrobeTab] = i;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            Rect labelRect = SRect(72f, 298f + i * 101f, 116f, 24f, scale, ox, oy);
            Rect iconSlot = SRect(72f, 327f + i * 101f, 84f, 84f, scale, ox, oy);
            Rect valueSlot = SRect(170f, 334f + i * 101f, 238f, 58f, scale, ox, oy);
            Rect actionSlot = SRect(424f, 334f + i * 101f, 94f, 58f, scale, ox, oy);

            bool isActiveCategory = wardrobeTab == i;
            Color accent = GetWardrobeCategoryAccent(i);
            DrawLabel(labelRect, wardrobeTabs[i], 13, isActiveCategory ? accent : new Color(0.65f, 0.76f, 0.8f), TextAnchor.MiddleLeft, FontStyle.Bold, scale);
            DrawSlot(iconSlot, isActiveCategory ? accent : new Color(0.5f, 0.58f, 0.6f), scale);
            DrawWardrobeOptionIcon(iconSlot, i, GetEquippedWardrobeOption(i), scale);

            DrawFill(valueSlot, new Color(0.05f, 0.11f, 0.14f, 0.82f));
            DrawFrame(valueSlot, new Color(0.32f, 0.55f, 0.62f), 2f * scale);
            DrawLabel(valueSlot, GetEquippedWardrobeName(i), 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, scale);

            DrawFill(actionSlot, isActiveCategory ? new Color(accent.r, accent.g, accent.b, 0.82f) : new Color(0.10f, 0.22f, 0.28f, 0.92f));
            DrawFrame(actionSlot, isActiveCategory ? new Color(0.9f, 1f, 1f) : new Color(0.32f, 0.55f, 0.62f), 2f * scale);
            DrawLabel(actionSlot, isActiveCategory ? "EDITING" : "SELECT", 14, isActiveCategory ? new Color(0.03f, 0.12f, 0.16f) : new Color(0.66f, 0.94f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
            if (GUI.Button(actionSlot, GUIContent.none, GUIStyle.none))
            {
                wardrobeTab = i;
            }
        }

        DrawAgent(SRect(228f, 310f, 168f, 245f, scale, ox, oy), scale);
        DrawFill(SRect(188f, 552f, 190f, 9f, scale, ox, oy), new Color(0.18f, 1f, 0.82f, 0.7f));

        for (int i = 0; i < wardrobeTabs.Length; i++)
        {
            Rect tab = SRect(181f + i * 85f, 619f, 59f, 59f, scale, ox, oy);
            Color accent = i == 0 ? new Color(1f, 0.72f, 0.66f) : (i == 1 ? FreezeColor : new Color(1f, 0.46f, 0.74f));
            DrawSlot(tab, wardrobeTab == i ? accent : new Color(0.24f, 0.69f, 0.82f), scale);
            DrawTabIcon(tab, i, accent, scale);
            DrawLabel(SRect(174f + i * 85f, 678f, 74f, 23f, scale, ox, oy), wardrobeTabs[i], 13, i == wardrobeTab ? accent : new Color(0.68f, 0.78f, 0.84f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
            if (GUI.Button(tab, GUIContent.none, GUIStyle.none))
            {
                wardrobeTab = i;
            }
        }
    }

    private void DrawStoreArea(float scale, float ox, float oy)
    {
        Rect reward = SRect(635f, 169f, 462f, 60f, scale, ox, oy);
        DrawFrame(reward, FreezeColor, 3f * scale);
        DrawLabel(SRect(694f, 183f, 235f, 31f, scale, ox, oy), "PLAYTIME REWARD:", 24, new Color(0.58f, 0.77f, 0.86f), TextAnchor.MiddleLeft, FontStyle.Bold, scale);
        DrawLabel(SRect(930f, 182f, 67f, 32f, scale, ox, oy), "1250", 25, new Color(0.46f, 1f, 0.65f), TextAnchor.MiddleLeft, FontStyle.Bold, scale);
        DrawCoin(SRect(1002f, 178f, 42f, 42f, scale, ox, oy), scale);

        for (int i = 0; i < storeNames.Length; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Rect slot = SRect(635f + col * 115f, 248f + row * 140f, 98f, 119f, scale, ox, oy);
            DrawSlot(slot, i == selectedStoreItem ? new Color(0.8f, 1f, 1f) : FreezeColor, scale);
            DrawItemIcon(slot, i + 3, scale);
            DrawLabel(new Rect(slot.x + 5f * scale, slot.y + 84f * scale, slot.width - 10f * scale, 28f * scale), storeNames[i], 11, new Color(0.86f, 0.95f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
            DrawLabel(SRect(635f + col * 115f, 368f + row * 140f, 98f, 28f, scale, ox, oy), storePrices[i] + " C", 16, new Color(1f, 0.78f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);

            if (GUI.Button(slot, GUIContent.none, GUIStyle.none))
            {
                selectedStoreItem = i;
            }
        }

        Rect scroll = SRect(1098f, 248f, 9f, 420f, scale, ox, oy);
        DrawFill(scroll, new Color(0.55f, 0.59f, 0.68f, 0.75f));
        DrawFill(new Rect(scroll.x, scroll.y, scroll.width, scroll.height * 0.48f), new Color(0.08f, 0.95f, 1f, 0.82f));
    }

    private void DrawBottomNav(float scale, float ox, float oy)
    {
        Rect bar = new Rect(ox, Screen.height - 80f * scale, 1365f * scale, 80f * scale);
        DrawFill(bar, new Color(0.005f, 0.012f, 0.025f, 0.98f));
        DrawFill(new Rect(bar.x, bar.y, bar.width, 3f * scale), new Color(0.86f, 1f, 1f));

        string[] labels = { "GRAVITY", "WARDROBE", "STORE", "EXIT" };
        for (int i = 0; i < 4; i++)
        {
            Rect tab = new Rect(bar.x + i * bar.width / 4f, bar.y + 4f * scale, bar.width / 4f, bar.height - 8f * scale);
            bool active = i == 1;
            if (active)
            {
                DrawFill(tab, new Color(0.03f, 0.86f, 1f, 0.82f));
                DrawFrame(tab, new Color(0.9f, 1f, 1f), 2f * scale);
            }

            DrawLabel(new Rect(tab.x, tab.y + 43f * scale, tab.width, 20f * scale), labels[i], 10, active ? new Color(0.03f, 0.12f, 0.16f) : new Color(0.55f, 0.64f, 0.72f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
            DrawNavIcon(new Rect(tab.x + tab.width * 0.5f - 16f * scale, tab.y + 13f * scale, 32f * scale, 24f * scale), i, active, scale);
            if (i == 3 && GUI.Button(tab, GUIContent.none, GUIStyle.none))
            {
                shellScreen = ShellScreen.Home;
            }
        }
    }

    private void DrawCyberAlley(Rect canvas, float scale)
    {
        DrawFill(canvas, new Color(0.025f, 0.033f, 0.065f));
        DrawFill(new Rect(canvas.x, canvas.y + 58f * scale, canvas.width, canvas.height - 58f * scale), new Color(0.04f, 0.047f, 0.08f));

        for (int i = 0; i < 13; i++)
        {
            float x = canvas.x + (i * 122f - 40f) * scale;
            float w = (92f + (i % 4) * 21f) * scale;
            float h = (460f + (i % 5) * 46f) * scale;
            float y = canvas.y + 87f * scale;
            DrawFill(new Rect(x, y, w, h), i % 2 == 0 ? new Color(0.08f, 0.11f, 0.16f) : new Color(0.10f, 0.08f, 0.18f));
            DrawFrame(new Rect(x, y, w, h), new Color(0.20f, 0.25f, 0.34f), scale);
            for (int j = 0; j < 6; j++)
            {
                DrawFill(new Rect(x + 12f * scale, y + (34f + j * 53f) * scale, w - 24f * scale, 3f * scale), new Color(0.16f, 0.70f, 0.84f, 0.26f));
            }
        }

        DrawFill(new Rect(canvas.x + 548f * scale, canvas.y + 58f * scale, 253f * scale, 642f * scale), new Color(0.10f, 0.09f, 0.20f, 0.85f));
        DrawFill(new Rect(canvas.x + 806f * scale, canvas.y + 252f * scale, 113f * scale, 395f * scale), new Color(0.45f, 0.12f, 0.54f, 0.52f));
        DrawFill(new Rect(canvas.x + 772f * scale, canvas.y + 24f * scale, 54f * scale, 196f * scale), new Color(0.28f, 0.08f, 0.43f, 0.95f));
        DrawFrame(new Rect(canvas.x + 772f * scale, canvas.y + 24f * scale, 54f * scale, 196f * scale), new Color(0.95f, 0.32f, 1f), 3f * scale);
        DrawLabel(new Rect(canvas.x + 776f * scale, canvas.y + 62f * scale, 46f * scale, 100f * scale), "101\n0K", 28, new Color(1f, 0.45f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);

        DrawFill(new Rect(canvas.x, canvas.y + 662f * scale, canvas.width, 106f * scale), new Color(0.03f, 0.06f, 0.08f));
        for (int i = 0; i < 36; i++)
        {
            float x = canvas.x + (i * 43f + Mathf.PingPong(titleTimer * 18f + i * 7f, 18f)) * scale;
            DrawFill(new Rect(x, canvas.y + (70f + (i * 37) % 560) * scale, 2f * scale, 72f * scale), new Color(0.56f, 0.90f, 1f, 0.42f));
        }
    }

    private void DrawStoreBackground(Rect canvas, float scale)
    {
        DrawFill(canvas, new Color(0.025f, 0.025f, 0.055f));
        for (int i = 0; i < 12; i++)
        {
            Rect beam = new Rect(canvas.x + i * 104f * scale, canvas.y + 58f * scale, 48f * scale, 710f * scale);
            DrawFill(beam, i % 2 == 0 ? new Color(0.10f, 0.09f, 0.16f) : new Color(0.13f, 0.10f, 0.20f));
            DrawFrame(beam, new Color(0.28f, 0.29f, 0.38f), scale);
        }

        DrawFill(new Rect(canvas.x + 679f * scale, canvas.y + 58f * scale, 7f * scale, 660f * scale), new Color(0.01f, 0.015f, 0.025f));
        DrawFill(new Rect(canvas.x + 683f * scale, canvas.y + 58f * scale, 7f * scale, 660f * scale), new Color(0.65f, 0.98f, 1f, 0.7f));
        DrawFill(new Rect(canvas.x + 0f, canvas.y + 708f * scale, canvas.width, 32f * scale), new Color(0.10f, 0.11f, 0.16f));
        for (int i = 0; i < 27; i++)
        {
            Color stripe = i % 3 == 0 ? new Color(1f, 0.25f, 0.75f) : (i % 3 == 1 ? new Color(0.58f, 1f, 0.25f) : new Color(1f, 0.75f, 0.25f));
            DrawFill(new Rect(canvas.x + i * 43f * scale, canvas.y + 718f * scale, 29f * scale, 8f * scale), stripe);
        }
    }

    private void DrawStorePanel(Rect rect, string title, float scale)
    {
        DrawFill(rect, new Color(0.02f, 0.055f, 0.075f, 0.93f));
        DrawFrame(rect, new Color(0.40f, 0.43f, 0.50f), 4f * scale);
        DrawFill(new Rect(rect.x + 16f * scale, rect.y + 9f * scale, rect.width - 32f * scale, 56f * scale), new Color(0.11f, 0.12f, 0.17f));
        DrawFrame(new Rect(rect.x + 16f * scale, rect.y + 9f * scale, rect.width - 32f * scale, 56f * scale), new Color(0.35f, 0.36f, 0.42f), 3f * scale);
        DrawLabel(new Rect(rect.x, rect.y + 12f * scale, rect.width, 46f * scale), title, 25, FreezeColor, TextAnchor.MiddleCenter, FontStyle.Bold, scale);
        Rect close = new Rect(rect.x + rect.width - 58f * scale, rect.y + 16f * scale, 34f * scale, 34f * scale);
        DrawFill(close, new Color(0.72f, 0.18f, 0.24f));
        DrawFrame(close, new Color(0.24f, 0.08f, 0.10f), 2f * scale);
        DrawLabel(close, "X", 20, new Color(0.16f, 0.04f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
        if (GUI.Button(close, GUIContent.none, GUIStyle.none))
        {
            shellScreen = ShellScreen.Home;
        }
    }

    private void DrawRewardPanel(Rect rect, float scale)
    {
        DrawFill(rect, new Color(0.01f, 0.025f, 0.04f, 0.96f));
        DrawFrame(rect, new Color(0.88f, 1f, 1f), 3f * scale);
        DrawLabel(new Rect(rect.x + 22f * scale, rect.y + 15f * scale, 270f * scale, 31f * scale), "PLAYTIME REWARDS", 20, FreezeColor, TextAnchor.MiddleLeft, FontStyle.Bold, scale);
        Rect bar = new Rect(rect.x + 24f * scale, rect.y + 59f * scale, 293f * scale, 33f * scale);
        DrawFrame(bar, new Color(0.94f, 1f, 1f), 2f * scale);
        DrawFill(new Rect(bar.x + 4f * scale, bar.y + 6f * scale, 143f * scale, bar.height - 12f * scale), FreezeColor);
        DrawLabel(bar, "2h 15m / 5h", 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, scale);
        DrawCoin(new Rect(rect.x + 316f * scale, rect.y + 50f * scale, 58f * scale, 58f * scale), scale);
        DrawChest(new Rect(rect.x + 382f * scale, rect.y + 16f * scale, 70f * scale, 64f * scale), scale);
        DrawLabel(new Rect(rect.x + 390f * scale, rect.y + 78f * scale, 62f * scale, 30f * scale), "Coins", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, scale);
    }

    private void DrawHudChip(Rect rect, float scale)
    {
        DrawFill(rect, new Color(0.03f, 0.03f, 0.035f));
        DrawFrame(rect, new Color(0.87f, 0.87f, 0.82f), 2f * scale);
        DrawLabel(new Rect(rect.x + 14f * scale, rect.y, 36f * scale, rect.height), "HP", 12, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, scale);
        for (int i = 0; i < 4; i++)
        {
            DrawFrame(new Rect(rect.x + (40f + i * 18f) * scale, rect.y + 10f * scale, 14f * scale, 14f * scale), new Color(0.65f, 0.65f, 0.65f), scale);
            if (i == 0)
            {
                DrawFill(new Rect(rect.x + (42f + i * 18f) * scale, rect.y + 12f * scale, 10f * scale, 10f * scale), new Color(1f, 0.58f, 0.64f));
            }
        }
        DrawLabel(new Rect(rect.x + 135f * scale, rect.y, 72f * scale, rect.height), "SECTOR 04", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, scale);
    }

    private bool DrawNeonButton(Rect rect, string text, Color accent, float scale)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        DrawFill(new Rect(rect.x - 7f * scale, rect.y - 7f * scale, rect.width + 14f * scale, rect.height + 14f * scale), new Color(accent.r, accent.g, accent.b, hover ? 0.22f : 0.13f));
        DrawFill(rect, new Color(0.025f, 0.038f, 0.065f, 0.95f));
        DrawFrame(rect, accent, 5f * scale);
        DrawFrame(new Rect(rect.x + 9f * scale, rect.y + 9f * scale, rect.width - 18f * scale, rect.height - 18f * scale), new Color(0.65f, 1f, 1f, 0.50f), 2f * scale);
        for (int i = 0; i < 11; i++)
        {
            DrawFill(new Rect(rect.x + 18f * scale, rect.y + (21f + i * 8f) * scale, rect.width - 36f * scale, 2f * scale), new Color(0.18f, 0.45f, 0.62f, 0.18f));
        }
        DrawLabel(rect, text, 35, new Color(0.68f, 1f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void DrawAgent(Rect rect, float scale)
    {
        float x = rect.x;
        float y = rect.y;
        float s = rect.height / 330f;
        Color armorPrimary = GetArmorPrimaryColor();
        Color armorSecondary = GetArmorSecondaryColor();
        Color armorAccent = GetArmorAccentColor();
        Color visorColor = GetVisorGlowColor();
        int weaponVariant = GetEquippedWardrobeOption((int)WardrobeCategory.Weapon);

        DrawFill(new Rect(x + 60f * s, y + 5f * s, 50f * s, 29f * s), new Color(0.80f, 0.57f, 0.47f));
        DrawFill(new Rect(x + 68f * s, y + 0f * s, 42f * s, 18f * s), armorSecondary);
        DrawFrame(new Rect(x + 66f * s, y + 0f * s, 46f * s, 19f * s), new Color(0.05f, 0.08f, 0.09f), 2.5f * s);
        DrawFill(new Rect(x + 72f * s, y + 32f * s, 62f * s, 24f * s), visorColor);
        DrawFrame(new Rect(x + 69f * s, y + 29f * s, 67f * s, 29f * s), new Color(0.05f, 0.07f, 0.09f), 4f * s);

        DrawFill(new Rect(x + 57f * s, y + 60f * s, 63f * s, 91f * s), armorPrimary);
        DrawFrame(new Rect(x + 57f * s, y + 60f * s, 63f * s, 91f * s), new Color(0.04f, 0.05f, 0.06f), 4f * s);
        DrawFill(new Rect(x + 61f * s, y + 87f * s, 18f * s, 39f * s), armorAccent);
        DrawFill(new Rect(x + 88f * s, y + 76f * s, 23f * s, 14f * s), armorAccent);

        DrawFill(new Rect(x + 17f * s, y + 70f * s, 34f * s, 72f * s), armorSecondary);
        DrawFill(new Rect(x + 108f * s, y + 74f * s, 31f * s, 84f * s), armorSecondary * 0.92f);
        DrawFill(new Rect(x + 47f * s, y + 148f * s, 30f * s, 95f * s), armorPrimary * 0.85f);
        DrawFill(new Rect(x + 87f * s, y + 148f * s, 31f * s, 99f * s), armorPrimary * 0.78f);
        DrawFill(new Rect(x + 31f * s, y + 231f * s, 43f * s, 22f * s), new Color(0.08f, 0.10f, 0.12f));
        DrawFill(new Rect(x + 84f * s, y + 240f * s, 46f * s, 21f * s), new Color(0.08f, 0.10f, 0.12f));

        DrawFill(new Rect(x + 26f * s, y + 48f * s, 28f * s, 84f * s), armorAccent * 0.7f);
        DrawFill(new Rect(x + 32f * s, y + 45f * s, 20f * s, 64f * s), armorSecondary);
        DrawGunIcon(new Rect(x + 4f * s, y + 44f * s, 48f * s, 42f * s), weaponVariant, scale);
        DrawFill(new Rect(x + 73f * s, y + 92f * s, 10f * s, 10f * s), visorColor * 0.9f);
        DrawFill(new Rect(x + 94f * s, y + 107f * s, 10f * s, 10f * s), armorAccent);
    }

    private void DrawSlot(Rect rect, Color border, float scale)
    {
        DrawFill(rect, new Color(0.04f, 0.11f, 0.14f, 0.88f));
        DrawFrame(rect, border, 2f * scale);
        DrawFill(new Rect(rect.x + 4f * scale, rect.y + 4f * scale, rect.width - 8f * scale, rect.height - 8f * scale), new Color(0.10f, 0.16f, 0.18f, 0.44f));
    }

    private void DrawItemIcon(Rect rect, int index, float scale)
    {
        int kind = index % 8;
        Rect icon = new Rect(rect.x + rect.width * 0.2f, rect.y + rect.height * 0.18f, rect.width * 0.6f, rect.height * 0.46f);
        if (kind == 0)
        {
            DrawArmorPiece(icon, 0, scale);
        }
        else if (kind == 1 || kind == 2)
        {
            DrawFill(new Rect(icon.x, icon.y + icon.height * 0.35f, icon.width, icon.height * 0.22f), kind == 1 ? new Color(0.78f, 0.36f, 1f) : new Color(1f, 0.68f, 0.25f));
            DrawFill(new Rect(icon.x + icon.width * 0.14f, icon.y + icon.height * 0.55f, icon.width * 0.72f, icon.height * 0.20f), new Color(0.18f, 0.20f, 0.24f));
        }
        else if (kind == 3)
        {
            DrawGunIcon(icon, 0, scale);
        }
        else if (kind == 4)
        {
            DrawAgent(icon, scale);
        }
        else if (kind == 5)
        {
            DrawGem(icon, scale);
        }
        else if (kind == 6)
        {
            DrawCoin(icon, scale);
        }
        else
        {
            DrawFill(icon, new Color(0.20f, 0.35f, 0.42f));
            DrawFill(new Rect(icon.x + icon.width * 0.38f, icon.y + icon.height * 0.1f, icon.width * 0.25f, icon.height * 0.8f), LightColor);
        }
    }

    private void DrawWardrobeOptionIcon(Rect rect, int categoryIndex, int optionIndex, float scale)
    {
        Rect icon = new Rect(rect.x + rect.width * 0.16f, rect.y + rect.height * 0.14f, rect.width * 0.68f, rect.height * 0.56f);
        WardrobeCategory category = (WardrobeCategory)Mathf.Clamp(categoryIndex, 0, 2);
        if (category == WardrobeCategory.Armor)
        {
            DrawArmorPiece(icon, optionIndex, scale);
            return;
        }

        if (category == WardrobeCategory.Visor)
        {
            Color visorColor = GetVisorColor(optionIndex);
            DrawFill(new Rect(icon.x, icon.y + icon.height * 0.32f, icon.width, icon.height * 0.24f), visorColor);
            DrawFrame(new Rect(icon.x - 2f * scale, icon.y + icon.height * 0.26f, icon.width + 4f * scale, icon.height * 0.34f), new Color(0.07f, 0.09f, 0.11f), 2f * scale);
            DrawFill(new Rect(icon.x + icon.width * 0.14f, icon.y + icon.height * 0.58f, icon.width * 0.72f, icon.height * 0.16f), new Color(0.18f, 0.20f, 0.24f));
            return;
        }

        DrawGunIcon(icon, optionIndex, scale);
    }

    private void DrawArmorPiece(Rect rect, int variant, float scale)
    {
        Color color = GetArmorColorByOption(variant);
        Color accent = GetArmorAccentByOption(variant);
        DrawFill(new Rect(rect.x + rect.width * 0.20f, rect.y + rect.height * 0.14f, rect.width * 0.60f, rect.height * 0.62f), color);
        DrawFill(new Rect(rect.x + rect.width * 0.06f, rect.y + rect.height * 0.24f, rect.width * 0.24f, rect.height * 0.34f), color * 0.85f);
        DrawFill(new Rect(rect.x + rect.width * 0.70f, rect.y + rect.height * 0.24f, rect.width * 0.24f, rect.height * 0.34f), color * 0.85f);
        DrawFill(new Rect(rect.x + rect.width * 0.36f, rect.y + rect.height * 0.28f, rect.width * 0.28f, rect.height * 0.16f), accent);
        DrawFrame(new Rect(rect.x + rect.width * 0.20f, rect.y + rect.height * 0.14f, rect.width * 0.60f, rect.height * 0.62f), new Color(0.08f, 0.09f, 0.1f), 2f * scale);
    }

    private void DrawGunIcon(Rect rect, int variant, float scale)
    {
        int style = Mathf.Abs(variant) % 5;
        Color body =
            style == 1 ? new Color(0.36f, 0.72f, 0.78f) :
            style == 2 ? new Color(0.68f, 0.56f, 0.38f) :
            style == 3 ? new Color(0.44f, 0.82f, 0.56f) :
            style == 4 ? new Color(0.74f, 0.30f, 0.32f) :
            new Color(0.50f, 0.58f, 0.62f);
        DrawFill(new Rect(rect.x + rect.width * 0.08f, rect.y + rect.height * 0.35f, rect.width * 0.63f, rect.height * 0.25f), body);
        DrawFill(new Rect(rect.x + rect.width * 0.62f, rect.y + rect.height * 0.42f, rect.width * 0.32f, rect.height * 0.12f), new Color(0.16f, 0.18f, 0.20f));
        DrawFill(new Rect(rect.x + rect.width * 0.33f, rect.y + rect.height * 0.58f, rect.width * 0.18f, rect.height * 0.28f), style == 2 ? new Color(0.31f, 0.21f, 0.14f) : new Color(0.25f, 0.17f, 0.15f));
        DrawFill(new Rect(rect.x + rect.width * 0.73f, rect.y + rect.height * 0.36f, rect.width * 0.10f, rect.height * 0.20f), style == 4 ? new Color(1f, 0.36f, 0.42f) : FreezeColor);
    }

    private int GetEquippedWardrobeOption(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= equippedWardrobeOptions.Length)
        {
            return 0;
        }

        return Mathf.Clamp(equippedWardrobeOptions[categoryIndex], 0, 4);
    }

    private string GetEquippedWardrobeName(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= wardrobeOptionNames.Length)
        {
            return string.Empty;
        }

        string[] options = wardrobeOptionNames[categoryIndex];
        int optionIndex = GetEquippedWardrobeOption(categoryIndex);
        return options[Mathf.Clamp(optionIndex, 0, options.Length - 1)];
    }

    private Color GetWardrobeCategoryAccent(int categoryIndex)
    {
        if (categoryIndex == (int)WardrobeCategory.Armor)
        {
            return new Color(1f, 0.72f, 0.66f);
        }

        if (categoryIndex == (int)WardrobeCategory.Visor)
        {
            return FreezeColor;
        }

        return new Color(1f, 0.46f, 0.74f);
    }

    private Color GetArmorColorByOption(int optionIndex)
    {
        int style = Mathf.Abs(optionIndex) % 5;
        if (style == 1)
        {
            return new Color(0.56f, 0.60f, 0.62f);
        }

        if (style == 2)
        {
            return new Color(0.26f, 0.30f, 0.34f);
        }

        if (style == 3)
        {
            return new Color(0.24f, 0.38f, 0.44f);
        }

        if (style == 4)
        {
            return new Color(0.31f, 0.25f, 0.36f);
        }

        return new Color(0.35f, 0.43f, 0.46f);
    }

    private Color GetArmorAccentByOption(int optionIndex)
    {
        int style = Mathf.Abs(optionIndex) % 5;
        if (style == 1)
        {
            return new Color(0.98f, 0.64f, 0.18f);
        }

        if (style == 2)
        {
            return new Color(0.96f, 0.28f, 0.34f);
        }

        if (style == 3)
        {
            return new Color(0.22f, 0.92f, 0.78f);
        }

        if (style == 4)
        {
            return new Color(0.80f, 0.48f, 1f);
        }

        return new Color(0.55f, 0.74f, 0.78f);
    }

    private Color GetArmorPrimaryColor()
    {
        return GetArmorColorByOption(GetEquippedWardrobeOption((int)WardrobeCategory.Armor));
    }

    private Color GetArmorSecondaryColor()
    {
        return Color.Lerp(GetArmorPrimaryColor(), new Color(0.82f, 0.86f, 0.88f), 0.28f);
    }

    private Color GetArmorAccentColor()
    {
        return GetArmorAccentByOption(GetEquippedWardrobeOption((int)WardrobeCategory.Armor));
    }

    private Color GetVisorColor(int optionIndex)
    {
        int style = Mathf.Abs(optionIndex) % 5;
        if (style == 1)
        {
            return new Color(0.98f, 0.34f, 1f);
        }

        if (style == 2)
        {
            return new Color(1f, 0.82f, 0.24f);
        }

        if (style == 3)
        {
            return new Color(0.28f, 1f, 0.70f);
        }

        if (style == 4)
        {
            return new Color(1f, 0.32f, 0.32f);
        }

        return FreezeColor;
    }

    private Color GetVisorGlowColor()
    {
        return GetVisorColor(GetEquippedWardrobeOption((int)WardrobeCategory.Visor));
    }

    private void DrawTabIcon(Rect rect, int index, Color color, float scale)
    {
        Rect inner = new Rect(rect.x + rect.width * 0.28f, rect.y + rect.height * 0.22f, rect.width * 0.44f, rect.height * 0.42f);
        if (index == 0)
        {
            DrawFill(inner, color);
            DrawFill(new Rect(inner.x - inner.width * 0.22f, inner.y + inner.height * 0.1f, inner.width * 0.25f, inner.height * 0.45f), color);
            DrawFill(new Rect(inner.x + inner.width * 0.97f, inner.y + inner.height * 0.1f, inner.width * 0.25f, inner.height * 0.45f), color);
        }
        else if (index == 1)
        {
            DrawFill(new Rect(inner.x - inner.width * 0.25f, inner.y + inner.height * 0.25f, inner.width * 1.5f, inner.height * 0.36f), color);
        }
        else
        {
            DrawFill(inner, color);
            DrawFill(new Rect(inner.x + inner.width * 0.72f, inner.y + inner.height * 0.28f, inner.width * 0.45f, inner.height * 0.34f), color * 0.9f);
        }
    }

    private void DrawNavIcon(Rect rect, int index, bool active, float scale)
    {
        Color color = active ? new Color(0.03f, 0.12f, 0.16f) : new Color(0.48f, 0.55f, 0.62f);
        if (index == 0)
        {
            DrawFrame(new Rect(rect.x + 7f * scale, rect.y + 4f * scale, 18f * scale, 13f * scale), color, 2f * scale);
            DrawFill(new Rect(rect.x + 15f * scale, rect.y, 3f * scale, 6f * scale), color);
        }
        else if (index == 1)
        {
            DrawTabIcon(rect, 0, color, scale);
        }
        else if (index == 2)
        {
            DrawFrame(new Rect(rect.x + 4f * scale, rect.y + 7f * scale, 24f * scale, 15f * scale), color, 2f * scale);
            DrawFill(new Rect(rect.x + 10f * scale, rect.y + 3f * scale, 12f * scale, 6f * scale), color);
        }
        else
        {
            DrawFill(new Rect(rect.x + 5f * scale, rect.y + 4f * scale, 18f * scale, 3f * scale), color);
            DrawFill(new Rect(rect.x + 20f * scale, rect.y + 1f * scale, 3f * scale, 19f * scale), color);
            DrawFill(new Rect(rect.x + 22f * scale, rect.y + 8f * scale, 8f * scale, 3f * scale), color);
        }
    }

    private void DrawGem(Rect rect, float scale)
    {
        DrawFill(new Rect(rect.x + rect.width * 0.18f, rect.y, rect.width * 0.64f, rect.height * 0.22f), new Color(0.65f, 1f, 1f));
        DrawFill(new Rect(rect.x, rect.y + rect.height * 0.22f, rect.width, rect.height * 0.30f), new Color(0.16f, 0.85f, 1f));
        DrawFill(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.52f, rect.width * 0.64f, rect.height * 0.48f), new Color(0.04f, 0.55f, 0.95f));
        DrawFrame(new Rect(rect.x + rect.width * 0.10f, rect.y + rect.height * 0.08f, rect.width * 0.80f, rect.height * 0.82f), new Color(0.43f, 1f, 1f), scale);
    }

    private void DrawCoin(Rect rect, float scale)
    {
        DrawFill(new Rect(rect.x + rect.width * 0.14f, rect.y + rect.height * 0.06f, rect.width * 0.72f, rect.height * 0.88f), new Color(0.78f, 0.43f, 0.04f));
        DrawFill(new Rect(rect.x + rect.width * 0.22f, rect.y + rect.height * 0.10f, rect.width * 0.58f, rect.height * 0.80f), new Color(1f, 0.80f, 0.17f));
        DrawFrame(new Rect(rect.x + rect.width * 0.24f, rect.y + rect.height * 0.18f, rect.width * 0.50f, rect.height * 0.64f), new Color(1f, 0.95f, 0.46f), 2f * scale);
        DrawLabel(rect, "C", 22, new Color(0.72f, 0.42f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold, scale);
    }

    private void DrawChest(Rect rect, float scale)
    {
        DrawFill(new Rect(rect.x + rect.width * 0.10f, rect.y + rect.height * 0.30f, rect.width * 0.80f, rect.height * 0.54f), new Color(0.42f, 0.20f, 0.08f));
        DrawFill(new Rect(rect.x + rect.width * 0.16f, rect.y + rect.height * 0.08f, rect.width * 0.68f, rect.height * 0.34f), new Color(0.60f, 0.32f, 0.12f));
        DrawFrame(new Rect(rect.x + rect.width * 0.10f, rect.y + rect.height * 0.20f, rect.width * 0.80f, rect.height * 0.64f), new Color(1f, 0.73f, 0.34f), 2f * scale);
        DrawFill(new Rect(rect.x + rect.width * 0.46f, rect.y + rect.height * 0.26f, rect.width * 0.12f, rect.height * 0.58f), new Color(1f, 0.76f, 0.26f));
    }

    private void DrawPixelIcon(Rect rect, Color color)
    {
        DrawFill(rect, color);
        DrawFill(new Rect(rect.x + rect.width * 0.16f, rect.y + rect.height * 0.28f, rect.width * 0.18f, rect.height * 0.18f), new Color(0.02f, 0.08f, 0.10f));
        DrawFill(new Rect(rect.x + rect.width * 0.60f, rect.y + rect.height * 0.28f, rect.width * 0.18f, rect.height * 0.18f), new Color(0.02f, 0.08f, 0.10f));
    }

    private Texture2D HomeArt(string key)
    {
        Texture2D texture;
        homeArt.TryGetValue(key, out texture);
        return texture;
    }

    private void DrawHomeTexture(string key, Rect sourceRect, Rect canvas, Color color)
    {
        Texture2D texture = HomeArt(key);
        if (texture == null)
        {
            return;
        }

        float sx = canvas.width / 1376f;
        float sy = canvas.height / 768f;
        Rect rect = new Rect(
            canvas.x + sourceRect.x * sx,
            canvas.y + sourceRect.y * sy,
            sourceRect.width * sx,
            sourceRect.height * sy);
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }

    private bool DrawHomeImageButton(Rect sourceRect, Rect canvas, string keyPrefix)
    {
        float sx = canvas.width / 1376f;
        float sy = canvas.height / 768f;
        Rect rect = new Rect(
            canvas.x + sourceRect.x * sx,
            canvas.y + sourceRect.y * sy,
            sourceRect.width * sx,
            sourceRect.height * sy);
        bool hover = rect.Contains(Event.current.mousePosition);
        string state = hover ? (Input.GetMouseButton(0) ? "pressed" : "hover") : "normal";
        Texture2D texture = HomeArt(keyPrefix + "_" + state);
        if (texture == null)
        {
            texture = HomeArt(keyPrefix + "_normal");
        }

        if (texture != null)
        {
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private Rect GetShellRect(float sourceWidth, float sourceHeight)
    {
        float scale = Mathf.Min(Screen.width / sourceWidth, Screen.height / sourceHeight);
        return new Rect(
            (Screen.width - sourceWidth * scale) * 0.5f,
            (Screen.height - sourceHeight * scale) * 0.5f,
            sourceWidth * scale,
            sourceHeight * scale);
    }

    private Rect GetShellCoverRect(float sourceWidth, float sourceHeight)
    {
        float scale = Mathf.Max(Screen.width / sourceWidth, Screen.height / sourceHeight);
        return new Rect(
            (Screen.width - sourceWidth * scale) * 0.5f,
            (Screen.height - sourceHeight * scale) * 0.5f,
            sourceWidth * scale,
            sourceHeight * scale);
    }

    private Rect DrawShellTexture(Texture2D texture, float sourceWidth, float sourceHeight)
    {
        DrawFill(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);
        Rect rect = GetShellRect(sourceWidth, sourceHeight);
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        return rect;
    }

    private Rect DrawShellTextureCover(Texture2D texture, float sourceWidth, float sourceHeight)
    {
        DrawFill(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);
        Rect rect = GetShellCoverRect(sourceWidth, sourceHeight);
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        return rect;
    }

    private bool DrawImageHotspot(Rect sourceRect, Rect imageRect, float sourceWidth, float sourceHeight)
    {
        Rect rect = new Rect(
            imageRect.x + sourceRect.x / sourceWidth * imageRect.width,
            imageRect.y + sourceRect.y / sourceHeight * imageRect.height,
            sourceRect.width / sourceWidth * imageRect.width,
            sourceRect.height / sourceHeight * imageRect.height);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void DrawFrame(Rect rect, Color color, float thickness)
    {
        DrawFill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawFill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawFill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawFill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawFill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void DrawLabel(Rect rect, string text, int fontSize, Color color, TextAnchor anchor, FontStyle fontStyle, float scale)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = anchor;
        style.fontSize = Mathf.Max(8, Mathf.RoundToInt(fontSize * scale));
        style.fontStyle = fontStyle;
        style.normal.textColor = color;
        style.wordWrap = text.Contains("\n");
        GUI.Label(rect, text, style);
    }

    private Rect SRect(float x, float y, float width, float height, float scale, float ox, float oy)
    {
        return new Rect(ox + x * scale, oy + y * scale, width * scale, height * scale);
    }

    private float GetShellScale()
    {
        return Mathf.Min(Screen.width / 1365f, Screen.height / 768f);
    }

    private float GetShellOffsetX(float scale)
    {
        return (Screen.width - 1365f * scale) * 0.5f;
    }

    private float GetShellOffsetY(float scale)
    {
        return (Screen.height - 768f * scale) * 0.5f;
    }
}

public sealed class RuleShotPlayerController : MonoBehaviour
{
    public RuleShotGameController game;
    public RuleGunController gun;
    public float moveSpeed = 7.4f;
    public float jumpSpeed = 13.2f;
    public float dashSpeed = 18f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private float dashTimer;
    private float dashCooldown;
    private bool jumpRequested;
    private bool facingRight = true;
    private bool controlsLocked;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (controlsLocked)
        {
            moveInput = 0f;
            jumpRequested = false;
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput > 0.01f)
        {
            facingRight = true;
        }
        else if (moveInput < -0.01f)
        {
            facingRight = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }

        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && dashCooldown <= 0f)
        {
            dashTimer = 0.14f;
            dashCooldown = 0.65f;
            game.ShowFlash(transform.position, new Color(0.45f, 0.9f, 1f), 0.8f);
        }

        dashCooldown -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.R))
        {
            game.RestartFromCheckpoint();
        }

        if (transform.position.y < -8f)
        {
            game.DamagePlayer("掉入机械城下层");
        }
    }

    private void FixedUpdate()
    {
        if (controlsLocked)
        {
            return;
        }

        bool grounded = IsGrounded();
        Vector2 velocity = body.velocity;

        if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            float direction = facingRight ? 1f : -1f;
            body.velocity = new Vector2(direction * dashSpeed, Mathf.Max(velocity.y, -1f));
            jumpRequested = false;
            return;
        }

        velocity.x = moveInput * moveSpeed;
        if (jumpRequested && grounded)
        {
            velocity.y = jumpSpeed;
        }

        jumpRequested = false;
        body.velocity = velocity;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
            spriteRenderer.color = grounded ? new Color(0.88f, 0.94f, 1f) : new Color(0.62f, 0.82f, 1f);
        }
    }

    public Vector2 AimFallbackDirection()
    {
        return facingRight ? Vector2.right : Vector2.left;
    }

    public void ResetTo(Vector3 position)
    {
        transform.position = position;
        body.velocity = Vector2.zero;
        dashTimer = 0f;
        dashCooldown = 0f;
    }

    public void SetControlLocked(bool locked)
    {
        controlsLocked = locked;
        moveInput = 0f;
        jumpRequested = false;
        dashTimer = 0f;
        dashCooldown = 0f;
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.simulated = !locked;
        }
    }

    private bool IsGrounded()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll((Vector2)transform.position + Vector2.down * 0.66f, new Vector2(0.58f, 0.14f), 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject == gameObject || hits[i].isTrigger)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}

public sealed class RuleGunController : MonoBehaviour
{
    public RuleShotGameController game;
    public RuleShotPlayerController player;
    public RuleShotType CurrentShot { get; private set; }

    private GameObject muzzle;
    private float fireCooldown;
    private bool controlsLocked;

    private void Start()
    {
        muzzle = game.CreateVisualBox("Rule Gun Muzzle", transform.position, new Vector2(0.9f, 0.16f), game.GetShotColor(CurrentShot), 14);
        muzzle.SetActive(!controlsLocked);
    }

    private void Update()
    {
        if (controlsLocked)
        {
            if (muzzle != null)
            {
                muzzle.SetActive(false);
            }

            return;
        }

        if (muzzle != null && !muzzle.activeSelf)
        {
            muzzle.SetActive(true);
        }

        HandleSwitching();
        fireCooldown -= Time.deltaTime;

        Vector2 direction = GetAimDirection();
        if (muzzle != null)
        {
            muzzle.transform.position = (Vector2)transform.position + direction * 0.64f + Vector2.up * 0.08f;
            muzzle.transform.right = direction;
            muzzle.GetComponent<SpriteRenderer>().color = game.GetShotColor(CurrentShot);
        }

        if (Input.GetMouseButtonDown(0) && fireCooldown <= 0f)
        {
            fireCooldown = 0.18f;
            game.SpawnProjectile((Vector2)transform.position + direction * 0.85f + Vector2.up * 0.08f, direction, CurrentShot);
        }
    }

    private void HandleSwitching()
    {
        int index = (int)CurrentShot;
        if (Input.GetKeyDown(KeyCode.E))
        {
            index++;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            index--;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.1f)
        {
            index++;
        }
        else if (scroll < -0.1f)
        {
            index--;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            index = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            index = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            index = 2;
        }

        index = (index % 3 + 3) % 3;
        RuleShotType next = (RuleShotType)index;
        if (next != CurrentShot)
        {
            CurrentShot = next;
            game.ShowFeedback(game.GetShotName(CurrentShot) + "： " + game.GetShotHint(CurrentShot), 1.7f);
        }
    }

    private Vector2 GetAimDirection()
    {
        Vector2 direction = player.AimFallbackDirection();
        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = -camera.transform.position.z;
            Vector2 mouseWorld = camera.ScreenToWorldPoint(mouse);
            Vector2 candidate = mouseWorld - (Vector2)transform.position;
            if (candidate.sqrMagnitude > 0.04f)
            {
                direction = candidate.normalized;
            }
        }

        return direction;
    }

    public void SetControlLocked(bool locked)
    {
        controlsLocked = locked;
        fireCooldown = 0f;
        if (muzzle != null)
        {
            muzzle.SetActive(!locked);
        }
    }
}

public sealed class RuleProjectile : MonoBehaviour
{
    private RuleShotGameController game;
    private RuleShotType shotType;
    private Vector2 direction;
    private float speed = 28f;
    private float lifetime = 1.4f;

    public void Initialize(RuleShotGameController owner, RuleShotType type, Vector2 initialDirection)
    {
        game = owner;
        shotType = type;
        direction = initialDirection;
        transform.right = direction;
    }

    private void Update()
    {
        float distance = speed * Time.deltaTime;
        Vector2 origin = transform.position;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance + 0.18f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.GetComponentInParent<RuleProjectile>() != null)
            {
                continue;
            }

            if (hitCollider.GetComponentInParent<RuleShotPlayerController>() != null ||
                hitCollider.GetComponentInParent<RuleWindZone>() != null ||
                hitCollider.GetComponentInParent<RulePressurePlate>() != null ||
                hitCollider.GetComponentInParent<RuleFinishGate>() != null)
            {
                continue;
            }

            transform.position = hits[i].point;
            HandleHit(hitCollider, hits[i].point);
            return;
        }

        transform.position = origin + direction * distance;
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void HandleHit(Collider2D hitCollider, Vector2 hitPoint)
    {
        RuleEnemy enemy = hitCollider.GetComponentInParent<RuleEnemy>();
        if (enemy != null)
        {
            enemy.ApplyRule(shotType, direction);
            game.ShowFlash(hitPoint, game.GetShotColor(shotType));
            Destroy(gameObject);
            return;
        }

        RuleHazard hazard = hitCollider.GetComponentInParent<RuleHazard>();
        if (hazard != null)
        {
            hazard.ApplyRule(shotType);
            game.ShowFlash(hitPoint, game.GetShotColor(shotType));
            Destroy(gameObject);
            return;
        }

        RuleAffectable affectable = hitCollider.GetComponentInParent<RuleAffectable>();
        if (affectable != null)
        {
            affectable.ApplyRule(shotType);
            game.ShowFlash(hitPoint, game.GetShotColor(shotType));
            Destroy(gameObject);
            return;
        }

        game.ShowFlash(hitPoint, new Color(0.9f, 0.95f, 1f), 0.28f);
        Destroy(gameObject);
    }
}

public sealed class RuleAffectable : MonoBehaviour
{
    public RuleShotGameController game;
    public RuleAffectableKind kind;
    public RuleMovingPlatform movingPlatform;
    public string startingHint;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float freezeTimer;
    private bool hintShown;

    public bool IsHeavy { get; private set; }
    public bool IsLight { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
            {
                if (body != null)
                {
                    body.constraints = RigidbodyConstraints2D.FreezeRotation;
                }

                if (movingPlatform != null)
                {
                    movingPlatform.SetFrozen(false);
                }

                SetColor(originalColor);
            }
        }
    }

    private void OnMouseEnter()
    {
        if (!hintShown && !string.IsNullOrEmpty(startingHint))
        {
            hintShown = true;
            game.ShowFeedback(startingHint, 3f);
        }
    }

    public void ApplyRule(RuleShotType shotType)
    {
        if (kind == RuleAffectableKind.FragileFloor)
        {
            ApplyFragileRule(shotType);
            return;
        }

        if (kind == RuleAffectableKind.MovingPlatform)
        {
            if (shotType == RuleShotType.Freeze)
            {
                Freeze(3.2f);
                game.ShowFeedback("移动平台被冻结，窗口很短，马上通过。");
            }
            return;
        }

        if (shotType == RuleShotType.Heavy)
        {
            IsHeavy = true;
            IsLight = false;
            if (body != null)
            {
                body.mass = 7.5f;
                body.gravityScale = 4.2f;
                body.drag = 0.2f;
                body.AddForce(Vector2.down * 18f, ForceMode2D.Impulse);
            }

            SetColor(game.HeavyColor);
            game.ShowFeedback("箱子变重了：可以压住压力板或砸碎脆弱地板。");
        }
        else if (shotType == RuleShotType.Light)
        {
            IsHeavy = false;
            IsLight = true;
            if (body != null)
            {
                body.mass = 0.35f;
                body.gravityScale = 0.28f;
                body.drag = 1.4f;
                body.AddForce(Vector2.up * 7f, ForceMode2D.Impulse);
            }

            SetColor(game.LightColor);
            game.ShowFeedback("箱子被轻化：风道会把它托起来，也更容易推动。");
        }
        else
        {
            Freeze(2.4f);
            game.ShowFeedback("物体被冻结了一小段时间。");
        }
    }

    private void ApplyFragileRule(RuleShotType shotType)
    {
        if (shotType == RuleShotType.Heavy)
        {
            game.ShowFeedback("重力弹击碎了脆弱地板。");
            Destroy(gameObject);
        }
        else
        {
            SetColor(shotType == RuleShotType.Light ? game.LightColor : game.FreezeColor);
            game.ShowFeedback("这块地板需要更强的压强，试试重力弹。");
        }
    }

    private void Freeze(float seconds)
    {
        freezeTimer = seconds;
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        if (movingPlatform != null)
        {
            movingPlatform.SetFrozen(true);
        }

        SetColor(game.FreezeColor);
    }

    private void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}

public sealed class RulePressurePlate : MonoBehaviour
{
    public RuleShotGameController game;
    public RuleDoor targetDoor;
    public float requiredMass = 4.5f;
    public string requiredHint;
    public SpriteRenderer indicatorRenderer;
    public Sprite closedIndicator;
    public Sprite openIndicator;

    private readonly List<Rigidbody2D> bodies = new List<Rigidbody2D>();
    private SpriteRenderer spriteRenderer;
    private bool open;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float mass = 0f;
        for (int i = bodies.Count - 1; i >= 0; i--)
        {
            if (bodies[i] == null)
            {
                bodies.RemoveAt(i);
                continue;
            }

            mass += bodies[i].mass;
        }

        bool shouldOpen = mass >= requiredMass;
        if (shouldOpen != open)
        {
            open = shouldOpen;
            targetDoor.SetOpen(open);
            game.ShowFeedback(open ? "压力板已启动，门打开了。" : "压力不足，门又关上了。");
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = open ? new Color(1f, 0.78f, 0.18f) : game.HeavyColor;
        }

        if (indicatorRenderer != null)
        {
            indicatorRenderer.sprite = open && openIndicator != null ? openIndicator : closedIndicator;
            indicatorRenderer.color = open ? Color.white : new Color(1f, 1f, 1f, 0.96f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || bodies.Contains(body))
        {
            return;
        }

        bodies.Add(body);
        if (!string.IsNullOrEmpty(requiredHint))
        {
            game.ShowFeedback(requiredHint, 2.2f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body != null)
        {
            bodies.Remove(body);
        }
    }
}

public sealed class RuleDoor : MonoBehaviour
{
    public Vector2 closedPosition;
    public Vector2 openOffset;
    public Sprite closedSprite;
    public Sprite openSprite;

    private Collider2D doorCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetOpen(bool open)
    {
        transform.position = closedPosition + (open ? openOffset : Vector2.zero);
        if (doorCollider != null)
        {
            doorCollider.enabled = !open;
        }

        if (spriteRenderer != null)
        {
            if (closedSprite != null && openSprite != null)
            {
                spriteRenderer.sprite = open ? openSprite : closedSprite;
            }

            spriteRenderer.color = open ? new Color(0.2f, 0.55f, 0.38f, 0.45f) : new Color(0.8f, 0.14f, 0.2f);
        }
    }
}

public sealed class RuleWindZone : MonoBehaviour
{
    public RuleShotGameController game;
    public float force = 42f;

    private SpriteRenderer[] spriteRenderers;
    private float pulse;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        pulse += Time.deltaTime * 5f;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            Color baseColor = spriteRenderers[i].color;
            float alpha = 0.14f + Mathf.Sin(pulse + i * 0.45f) * 0.08f;
            spriteRenderers[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null)
        {
            return;
        }

        RuleAffectable affectable = other.GetComponentInParent<RuleAffectable>();
        if (affectable != null && affectable.IsLight)
        {
            body.AddForce(Vector2.up * force);
            return;
        }

        RuleShotPlayerController player = other.GetComponentInParent<RuleShotPlayerController>();
        if (player != null)
        {
            body.AddForce(Vector2.up * (force * 0.18f));
        }
    }
}

public sealed class RuleMovingPlatform : MonoBehaviour
{
    public Vector2 start;
    public Vector2 end;
    public float speed = 1.5f;

    private float t;
    private bool frozen;

    private void Update()
    {
        if (frozen)
        {
            return;
        }

        t += Time.deltaTime * speed;
        float blend = Mathf.PingPong(t, 1f);
        transform.position = Vector2.Lerp(start, end, blend);
    }

    public void SetFrozen(bool value)
    {
        frozen = value;
    }
}

public sealed class RuleHazard : MonoBehaviour
{
    public RuleShotGameController game;
    public bool freezeable = true;
    public bool spinVisual = true;

    private Collider2D hazardCollider;
    private SpriteRenderer[] spriteRenderers;
    private float freezeTimer;
    private float spin;

    private void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
            {
                SetActiveState(true);
            }
            return;
        }

        if (spinVisual)
        {
            spin += Time.deltaTime * 140f;
            transform.rotation = Quaternion.Euler(0f, 0f, spin);
        }
    }

    public void ApplyRule(RuleShotType shotType)
    {
        if (shotType == RuleShotType.Freeze && freezeable)
        {
            freezeTimer = 3f;
            SetActiveState(false);
            game.ShowFeedback("危险机关被冻结，趁现在通过。");
        }
        else
        {
            game.ShowFeedback("这个危险物需要冻结弹才能暂停。");
        }
    }

    private void SetActiveState(bool active)
    {
        if (hazardCollider != null)
        {
            hazardCollider.enabled = active;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            Color tint = active ? game.DangerColor : game.FreezeColor;
            Color source = spriteRenderers[i].color;
            float alpha = source.a <= 0f ? 1f : source.a;
            spriteRenderers[i].color = new Color(tint.r, tint.g, tint.b, alpha);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (freezeTimer > 0f)
        {
            return;
        }

        if (other.GetComponentInParent<RuleShotPlayerController>() != null)
        {
            game.DamagePlayer("碰到危险机关");
        }
    }
}

public sealed class RuleEnemy : MonoBehaviour
{
    public RuleShotGameController game;
    public RuleEnemyKind kind;
    public float leftBound;
    public float rightBound;
    public float moveSpeed = 2.2f;

    private SpriteRenderer[] spriteRenderers;
    private Collider2D enemyCollider;
    private int direction = 1;
    private float frozenTimer;
    private float exposedTimer;
    private bool defeated;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (defeated)
        {
            return;
        }

        if (frozenTimer > 0f)
        {
            frozenTimer -= Time.deltaTime;
            if (frozenTimer <= 0f)
            {
                SetColor(kind == RuleEnemyKind.ShieldExecutor && exposedTimer <= 0f ? new Color(0.95f, 0.85f, 0.25f) : new Color(0.9f, 0.92f, 0.96f));
            }
            return;
        }

        if (exposedTimer > 0f)
        {
            exposedTimer -= Time.deltaTime;
        }

        if (kind == RuleEnemyKind.PatrolBot || kind == RuleEnemyKind.ShieldExecutor)
        {
            transform.position += Vector3.right * direction * moveSpeed * Time.deltaTime;
            if (transform.position.x > rightBound)
            {
                direction = -1;
            }
            else if (transform.position.x < leftBound)
            {
                direction = 1;
            }
        }
    }

    public void ApplyRule(RuleShotType shotType, Vector2 hitDirection)
    {
        if (defeated)
        {
            return;
        }

        if (shotType == RuleShotType.Freeze)
        {
            frozenTimer = 3.2f;
            SetColor(game.FreezeColor);
            game.ShowFeedback("敌人被冻结，可以从旁边通过或再用重力弹处理。");
            return;
        }

        if (kind == RuleEnemyKind.ShieldExecutor && exposedTimer <= 0f)
        {
            if (shotType == RuleShotType.Light)
            {
                exposedTimer = 4f;
                SetColor(game.LightColor);
                game.ShowFeedback("护盾执行者被轻化失衡，下一发可解决它。");
            }
            else
            {
                game.ShowFeedback("护盾太重，先用轻化弹让它失衡。");
            }
            return;
        }

        if (shotType == RuleShotType.Heavy || exposedTimer > 0f || kind == RuleEnemyKind.StaticGuard)
        {
            Defeat();
        }
        else
        {
            exposedTimer = 2f;
            SetColor(game.LightColor);
            game.ShowFeedback("敌人被轻化打乱节奏，但重力弹更适合终结。");
        }
    }

    private void Defeat()
    {
        defeated = true;
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        SetColor(new Color(0.12f, 0.13f, 0.14f));
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.35f, transform.localScale.z);
        game.ShowFeedback("机械敌人已失效。");
    }

    private void SetColor(Color color)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = color;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (defeated || frozenTimer > 0f)
        {
            return;
        }

        if (other.GetComponentInParent<RuleShotPlayerController>() != null)
        {
            game.DamagePlayer("被巡逻机械体拦截");
        }
    }
}

public sealed class RuleFinishGate : MonoBehaviour
{
    public RuleShotGameController game;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<RuleShotPlayerController>() != null)
        {
            game.CompleteGame();
        }
    }
}

public sealed class RuleCameraFollow : MonoBehaviour
{
    public Transform target;
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + new Vector3(3.2f, 2.2f, -10f);
        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.1f);
    }
}

public sealed class RuleAutoDestroy : MonoBehaviour
{
    public float lifetime = 0.2f;

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
