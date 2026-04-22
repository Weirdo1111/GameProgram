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
    private string feedback = "A/D 移动，Space 跳跃，Shift 冲刺，鼠标左键发射规则弹。";
    private float feedbackTimer;

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
        ConfigureScene();
        BuildWorld();
    }

    private void Update()
    {
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
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
        GameObject flash = CreateVisualBox("Rule Flash", position, Vector2.one * size, color, 30);
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
        return box;
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
        follow.target = player.transform;
        follow.minX = -18f;
        follow.maxX = 132f;
        follow.minY = 1.5f;
        follow.maxY = 17f;
    }

    private void BuildBackground()
    {
        CreateVisualBox("Back Sky Band", new Vector2(54f, 6f), new Vector2(190f, 32f), new Color(0.035f, 0.04f, 0.075f), -50);
        CreateVisualBox("Far Neon Grid", new Vector2(54f, -2.8f), new Vector2(190f, 0.12f), new Color(0.05f, 0.32f, 0.45f), -45);

        for (int i = 0; i < 26; i++)
        {
            float x = -28f + i * 7.2f;
            float height = 5f + (i % 5) * 1.4f;
            Color color = i % 2 == 0 ? new Color(0.055f, 0.065f, 0.1f) : new Color(0.075f, 0.055f, 0.11f);
            CreateVisualBox("Distant Building", new Vector2(x, height * 0.5f - 2f), new Vector2(4.2f, height), color, -42);
            Color signColor = i % 3 == 0 ? HeavyColor : (i % 3 == 1 ? LightColor : FreezeColor);
            CreateVisualBox("Neon Sign", new Vector2(x, height - 1f), new Vector2(2.5f, 0.18f), signColor * 0.85f, -41);
        }

        for (int i = 0; i < 12; i++)
        {
            float x = -22f + i * 14f;
            CreateVisualBox("Foreground Cable", new Vector2(x, 9.8f + Mathf.Sin(i) * 1.2f), new Vector2(10f, 0.08f), new Color(0.1f, 0.65f, 0.8f, 0.7f), -35);
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
        GameObject playerObject = CreateVisualBox("Rule Hunter", GetZoneStart(firstZone), new Vector2(0.72f, 1.16f), new Color(0.88f, 0.94f, 1f), 12);
        playerObject.AddComponent<BoxCollider2D>();
        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 3.3f;
        body.mass = 1f;

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
        GameObject box = CreateSolidBox(objectName, position, new Vector2(1.35f, 1.35f), new Color(0.52f, 0.62f, 0.7f), 8);
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
        RuleAffectable affectable = floor.AddComponent<RuleAffectable>();
        affectable.game = this;
        affectable.kind = RuleAffectableKind.FragileFloor;
    }

    private RuleDoor CreateDoor(string objectName, Vector2 position, Vector2 size)
    {
        GameObject doorObject = CreateSolidBox(objectName, position, size, new Color(0.8f, 0.14f, 0.2f), 6);
        RuleDoor door = doorObject.AddComponent<RuleDoor>();
        door.closedPosition = position;
        door.openOffset = Vector2.up * (size.y + 0.6f);
        return door;
    }

    private RulePressurePlate CreatePressurePlate(string objectName, Vector2 position, RuleDoor door, float requiredMass)
    {
        GameObject plateObject = CreateVisualBox(objectName, position, new Vector2(2.5f, 0.22f), HeavyColor, 7);
        BoxCollider2D collider = plateObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        RulePressurePlate plate = plateObject.AddComponent<RulePressurePlate>();
        plate.game = this;
        plate.targetDoor = door;
        plate.requiredMass = requiredMass;
        return plate;
    }

    private void CreateWindZone(string objectName, Vector2 position, Vector2 size)
    {
        GameObject wind = CreateVisualBox(objectName, position, size, new Color(0.08f, 0.9f, 0.65f, 0.28f), 2);
        BoxCollider2D collider = wind.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        RuleWindZone windZone = wind.AddComponent<RuleWindZone>();
        windZone.force = 46f;
        windZone.game = this;
    }

    private void CreateMovingPlatform(string objectName, Vector2 start, Vector2 end, Vector2 size, float speed)
    {
        GameObject platform = CreateSolidBox(objectName, start, size, new Color(0.18f, 0.26f, 0.32f), 5);
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
        GameObject hazard = CreateVisualBox(objectName, position, size, DangerColor, 9);
        BoxCollider2D collider = hazard.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        RuleHazard hazardController = hazard.AddComponent<RuleHazard>();
        hazardController.game = this;
        hazardController.freezeable = freezeable;
    }

    private void CreateEnemy(string objectName, RuleEnemyKind kind, Vector2 position, float leftBound, float rightBound)
    {
        GameObject enemy = CreateVisualBox(objectName, position, new Vector2(0.9f, 1.25f), new Color(0.9f, 0.92f, 0.96f), 11);
        BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        RuleEnemy enemyController = enemy.AddComponent<RuleEnemy>();
        enemyController.game = this;
        enemyController.kind = kind;
        enemyController.leftBound = leftBound;
        enemyController.rightBound = rightBound;
    }

    private void CreateFinish(Vector2 position)
    {
        GameObject finish = CreateVisualBox("Finish Gate", position, new Vector2(2.2f, 3f), new Color(1f, 0.85f, 0.18f), 10);
        BoxCollider2D collider = finish.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        RuleFinishGate gate = finish.AddComponent<RuleFinishGate>();
        gate.game = this;
    }

    private void CreateLabelBar(string objectName, Vector2 position, Color color)
    {
        CreateVisualBox(objectName, position, new Vector2(8f, 0.16f), color, 3);
    }

    private Sprite BuildPixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.name = "RuleShot Runtime Pixel";
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void OnGUI()
    {
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

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
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

    private void Start()
    {
        muzzle = game.CreateVisualBox("Rule Gun Muzzle", transform.position, new Vector2(0.9f, 0.16f), game.GetShotColor(CurrentShot), 14);
    }

    private void Update()
    {
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
            spriteRenderer.color = open ? new Color(0.2f, 0.55f, 0.38f, 0.45f) : new Color(0.8f, 0.14f, 0.2f);
        }
    }
}

public sealed class RuleWindZone : MonoBehaviour
{
    public RuleShotGameController game;
    public float force = 42f;

    private SpriteRenderer spriteRenderer;
    private float pulse;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        pulse += Time.deltaTime * 5f;
        if (spriteRenderer != null)
        {
            float alpha = 0.18f + Mathf.Sin(pulse) * 0.06f;
            spriteRenderer.color = new Color(0.08f, 0.9f, 0.65f, alpha);
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

    private Collider2D hazardCollider;
    private SpriteRenderer spriteRenderer;
    private float freezeTimer;
    private float spin;

    private void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        spin += Time.deltaTime * 140f;
        transform.rotation = Quaternion.Euler(0f, 0f, spin);
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

        if (spriteRenderer != null)
        {
            spriteRenderer.color = active ? game.DangerColor : game.FreezeColor;
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

    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private int direction = 1;
    private float frozenTimer;
    private float exposedTimer;
    private bool defeated;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
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
