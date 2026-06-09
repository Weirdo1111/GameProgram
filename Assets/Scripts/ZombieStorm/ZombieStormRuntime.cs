using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-100)]
// Main game controller for startup, scene setup, UI, spawning, and upgrade flow.
public sealed class ZombieStormGameController : MonoBehaviour
{
    private enum ZombieStormFlowState
    {
        MainMenu,
        Running,
        Paused,
        Settings,
        LevelUp,
        Results
    }

    public static ZombieStormGameController Instance { get; private set; }

    private const string Title = "\u50f5\u5c38\u5272\u8349\u5927\u4f5c\u6218";
    private const float GameplayCameraOrthographicSize = 10.5f;
    private const float GameplayHudScale = 0.8f;
    public const float EnemyDamageMultiplier = 0.75f;
    private static readonly string[] GroundFireEffectKeys = { "fire_pool_tekila_01" };

    [Header("Run")]
    public float runDurationSeconds = 300f;
    public int targetFrameRate = 120;

    public ZombieStormPlayer Player { get; private set; }
    public ZombieStormSkillManager Skills { get; private set; }
    public IReadOnlyList<ZombieStormEnemy> Enemies { get { return enemies; } }

    private readonly List<ZombieStormEnemy> enemies = new List<ZombieStormEnemy>(256);
    private readonly List<ZombieStormObstacle> obstacles = new List<ZombieStormObstacle>(32);
    private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<ZombieStormPassiveType, int> passives = new Dictionary<ZombieStormPassiveType, int>();
    private readonly Dictionary<string, AudioClip> sfx = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> sfxLastPlayed = new Dictionary<string, float>();
    private readonly List<ZombieStormUpgradeOption> currentChoices = new List<ZombieStormUpgradeOption>(3);
    private readonly HashSet<string> choiceKeys = new HashSet<string>();
    private readonly Dictionary<string, Sprite[]> playerWalkFrames = new Dictionary<string, Sprite[]>();
    private readonly Dictionary<string, Sprite[]> effectFrames = new Dictionary<string, Sprite[]>();
    private readonly List<ZombieStormDamagePopup> damagePopups = new List<ZombieStormDamagePopup>();
    private Sprite[] playerIdleFrames = new Sprite[0];
    private bool playerWalkFramesAreIdle;
    private Sprite[] playerHurtFrames = new Sprite[0];
    private Sprite[] iceBossOrbFrames = new Sprite[0];
    private Sprite[] chibiEnemyWalkFrames = new Sprite[0];
    private Sprite[] goblinRunFrames = new Sprite[0];
    private Sprite[] goblinHurtFrames = new Sprite[0];
    private Sprite[] goblinDeathFrames = new Sprite[0];
    private Sprite[] villagerRunFrames = new Sprite[0];
    private Sprite[] villagerSlashFrames = new Sprite[0];
    private Sprite[] villagerHurtFrames = new Sprite[0];
    private Sprite[] villagerDeathFrames = new Sprite[0];
    private Sprite[] gravediggerRunFrames = new Sprite[0];
    private Sprite[] gravediggerSlashFrames = new Sprite[0];
    private Sprite[] gravediggerHurtFrames = new Sprite[0];
    private Sprite[] gravediggerDeathFrames = new Sprite[0];
    private Sprite[] reaperRunFrames = new Sprite[0];
    private Sprite[] reaperSlashFrames = new Sprite[0];
    private Sprite[] reaperHurtFrames = new Sprite[0];
    private Sprite[] reaperDeathFrames = new Sprite[0];
    private Sprite[] orcRunFrames = new Sprite[0];
    private Sprite[] orcThrowFrames = new Sprite[0];
    private Sprite[] orcHurtFrames = new Sprite[0];
    private Sprite[] orcDeathFrames = new Sprite[0];
    private Sprite[] crystalGolemRunFrames = new Sprite[0];
    private Sprite[] crystalGolemSlashFrames = new Sprite[0];
    private Sprite[] crystalGolemThrowFrames = new Sprite[0];
    private Sprite[] crystalGolemHurtFrames = new Sprite[0];
    private Sprite[] crystalGolemDeathFrames = new Sprite[0];
    private Sprite[] mossGolemRunFrames = new Sprite[0];
    private Sprite[] mossGolemSlashFrames = new Sprite[0];
    private Sprite[] mossGolemThrowFrames = new Sprite[0];
    private Sprite[] mossGolemHurtFrames = new Sprite[0];
    private Sprite[] mossGolemDeathFrames = new Sprite[0];
    private Sprite[] emberGolemRunFrames = new Sprite[0];
    private Sprite[] emberGolemSlashFrames = new Sprite[0];
    private Sprite[] emberGolemThrowFrames = new Sprite[0];
    private Sprite[] emberGolemHurtFrames = new Sprite[0];
    private Sprite[] emberGolemDeathFrames = new Sprite[0];
    private readonly List<Sprite> groundSprites = new List<Sprite>();
    private readonly List<Sprite> debrisSprites = new List<Sprite>();

    private Transform worldRoot;
    private Transform poolRoot;
    private Camera mainCamera;
    private AudioSource audioSource;
    private Sprite playerSprite;
    private Sprite zombieSprite;
    private Sprite fastZombieSprite;
    private Sprite tankZombieSprite;
    private Sprite exploderSprite;
    private Sprite spitterSprite;
    private Sprite eliteSprite;
    private Sprite bossSprite;
    private Sprite bulletSprite;
    private Sprite projectileFxSprite;
    private Sprite fireSpiritSprite;
    private Sprite xpSprite;
    private Sprite coinSprite;
    private Sprite fireSprite;
    private Sprite rockSprite;
    private Sprite sawSprite;
    private Sprite orbitBladeSprite;
    private Sprite orbitRingSprite;
    private Sprite mineSprite;
    private Sprite tileSprite;
    private Sprite ruinSprite;
    private Sprite softShadowSprite;
    private Sprite softGlowSprite;
    private Sprite bloodSplatSprite;
    private Sprite neonSignSprite;
    private Sprite customArenaMapSprite;
    private Sprite mainMenuCoverSprite;
    private Sprite kenneyZombieSprite;
    private Sprite kenneyFastZombieSprite;
    private Sprite kenneyTankZombieSprite;
    private Sprite kenneyEliteZombieSprite;
    private Sprite kenneyBossSprite;

    private float runTime;
    private float spawnTimer;
    private float eliteTimer;
    private float feedbackTimer;
    private float difficultyScore = 1f;
    private float feedbackUntil;
    private float cameraShakeTime;
    private float cameraShakePower;
    private float screenFlash;
    private Color screenFlashColor = new Color(1f, 0.08f, 0.04f);
    private bool usingCustomArenaMap;
    private Vector2 customArenaHalfExtents;
    private bool leveling;
    private bool finished;
    private bool won;
    private bool firstBossDefeated;
    private int bossCount;
    private ZombieStormFlowState flowState = ZombieStormFlowState.MainMenu;
    private ZombieStormFlowState settingsReturnState = ZombieStormFlowState.MainMenu;
    private ZombieStormMainMenuUI mainMenuUI;
    private float masterVolume = 0.62f;
    private float musicVolume = 0.72f;
    private float sfxVolume = 0.9f;
    private bool sfxMuted;
    private string feedbackText = "WASD move. Skills cast automatically. Press F for ultimate.";

    public float DamageMultiplier { get { return 1f + GetPassiveLevel(ZombieStormPassiveType.Damage) * 0.18f; } }
    public float CooldownMultiplier { get { return Mathf.Max(0.35f, 1f - GetPassiveLevel(ZombieStormPassiveType.FireRate) * 0.08f); } }
    public float AreaMultiplier { get { return 1f + GetPassiveLevel(ZombieStormPassiveType.Area) * 0.16f; } }
    public float CritChance { get { return Mathf.Clamp01(GetPassiveLevel(ZombieStormPassiveType.Crit) * 0.07f); } }
    public float CoinMultiplier { get { return 1f + GetPassiveLevel(ZombieStormPassiveType.CoinGain) * 0.2f; } }
    public bool IsMainMenuActive { get { return flowState == ZombieStormFlowState.MainMenu; } }
    public bool IsMainMenuSettingsActive { get { return flowState == ZombieStormFlowState.Settings && settingsReturnState == ZombieStormFlowState.MainMenu; } }
    public float MasterVolume { get { return masterVolume; } }
    public float MusicVolume { get { return musicVolume; } }
    public float SfxVolume { get { return sfxVolume; } }
    public bool FullscreenEnabled { get { return Screen.fullScreen; } }

    // Clamps a world position inside the arena so actors cannot leave the combat area.
    public Vector2 ClampToArena(Vector2 position)
    {
        if (!usingCustomArenaMap)
        {
            return position;
        }

        const float margin = 2.2f;
        float x = Mathf.Clamp(position.x, -customArenaHalfExtents.x + margin, customArenaHalfExtents.x - margin);
        float y = Mathf.Clamp(position.y, -customArenaHalfExtents.y + margin, customArenaHalfExtents.y - margin);
        return new Vector2(x, y);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Creates the game controller automatically when the scene loads.
    private static void AutoBoot()
    {
        if (FindObjectOfType<ZombieStormGameController>() != null)
        {
            return;
        }

        GameObject bootstrap = new GameObject("Zombie Storm Bootstrap");
        bootstrap.AddComponent<ZombieStormGameController>();
    }

    // Initializes references, singleton state, settings, resources, and scene objects.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadMenuSettings();
        Application.targetFrameRate = targetFrameRate;
        Physics2D.gravity = Vector2.zero;
        Time.timeScale = 1f;

        CreateSprites();
        BuildScene();
        CreateAudioClips();
        Time.timeScale = 0f;
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
    private void Update()
    {
        if (flowState == ZombieStormFlowState.MainMenu)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartRun();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (mainMenuUI == null || !mainMenuUI.CloseTopModal())
                {
                    QuitGame();
                }
            }

            return;
        }

        if (flowState == ZombieStormFlowState.Settings)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseSettings();
            }

            return;
        }

        if (flowState == ZombieStormFlowState.Results)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartRun();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToMainMenu();
            }

            return;
        }

        if (flowState == ZombieStormFlowState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                ResumeRun();
            }

            return;
        }

        if (flowState == ZombieStormFlowState.Running && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            PauseRun();
            return;
        }

        if (flowState == ZombieStormFlowState.LevelUp)
        {
            HandleUpgradeHotkeys();
            return;
        }

        runTime += Time.deltaTime;
        feedbackTimer += Time.deltaTime;
        screenFlash = Mathf.Max(0f, screenFlash - Time.deltaTime * 3.2f);
        UpdateDynamicDifficulty();
        UpdateSpawning();
        UpdateDamagePopups();

        if (feedbackTimer >= 15f)
        {
            feedbackTimer = 0f;
            ShowFeedback("Horde pressure rising. Keep kiting and collect XP.", 2.2f);
        }

        if (runTime >= runDurationSeconds)
        {
            EndRun(true, "Dawn breaks. You survived the city.");
        }
    }

    // Updates camera follow after regular frame logic has moved the player.
    private void LateUpdate()
    {
        if (flowState == ZombieStormFlowState.Running)
        {
            FollowPlayer();
        }
    }

    // Draws immediate-mode UI such as HUD, upgrade cards, pause, and result panels.
    private void OnGUI()
    {
        if (mainMenuUI != null && mainMenuUI.IsReady && (flowState == ZombieStormFlowState.MainMenu || IsMainMenuSettingsActive))
        {
            return;
        }

        DrawAtmosphereOverlay();
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 16;
        GUI.color = Color.white;

        if (flowState == ZombieStormFlowState.MainMenu)
        {
            DrawMainMenu();
            return;
        }

        if (flowState == ZombieStormFlowState.Settings && settingsReturnState == ZombieStormFlowState.MainMenu)
        {
            DrawSettingsPanel();
            return;
        }

        Matrix4x4 previousGuiMatrix = GUI.matrix;
        float hudScreenWidth = Screen.width / GameplayHudScale;
        GUIUtility.ScaleAroundPivot(Vector2.one * GameplayHudScale, Vector2.zero);

        DrawPanel(new Rect(12f, 10f, 430f, 158f), new Color(0.035f, 0.045f, 0.055f, 0.82f), new Color(0.2f, 0.75f, 1f, 0.32f));
        GUI.Label(new Rect(24f, 18f, 760f, 28f), Title + " / Zombie Storm");
        GUI.skin.label.fontSize = 14;
        GUI.color = new Color(0.78f, 0.86f, 0.92f, 1f);
        GUI.Label(new Rect(24f, 45f, 420f, 24f), "WASD move | Auto skills | F ultimate | 1/2/3 upgrade | Esc/P pause");
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 18;

        if (Player != null)
        {
            DrawBar(new Rect(24f, 78f, 300f, 20f), Player.Health / Player.MaxHealth, new Color(0.92f, 0.16f, 0.12f), "HP " + Mathf.CeilToInt(Player.Health) + "/" + Mathf.CeilToInt(Player.MaxHealth));
            DrawBar(new Rect(24f, 106f, 300f, 20f), Player.Experience / Mathf.Max(1f, Player.ExperienceToNext), new Color(0.18f, 0.74f, 1f), "Lv." + Player.Level + " XP");
            GUI.skin.label.fontSize = 15;
            GUI.Label(new Rect(24f, 134f, 410f, 24f), "Coins " + Player.Coins + "    Kills " + Player.Kills + "    Horde " + enemies.Count + "    AI " + difficultyScore.ToString("0.0"));
            GUI.skin.label.fontSize = 18;
        }

        int remain = Mathf.Max(0, Mathf.CeilToInt(runDurationSeconds - runTime));
        DrawPanel(new Rect(hudScreenWidth - 224f, 10f, 206f, 48f), new Color(0.035f, 0.045f, 0.055f, 0.82f), new Color(1f, 0.85f, 0.25f, 0.28f));
        GUI.skin.label.fontSize = 24;
        GUI.Label(new Rect(hudScreenWidth - 206f, 18f, 190f, 32f), "Survive " + FormatTime(remain));
        GUI.skin.label.fontSize = 18;

        if (Skills != null)
        {
            DrawPanel(new Rect(hudScreenWidth - 286f, 70f, 268f, 150f), new Color(0.035f, 0.045f, 0.055f, 0.74f), new Color(0.9f, 0.28f, 0.2f, 0.24f));
            GUI.skin.label.fontSize = 15;
            GUI.Label(new Rect(hudScreenWidth - 268f, 84f, 244f, 124f), Skills.GetLoadoutText());
            GUI.skin.label.fontSize = 18;
        }

        if (Time.unscaledTime < feedbackUntil)
        {
            DrawPanel(new Rect(hudScreenWidth * 0.5f - 330f, 78f, 680f, 46f), new Color(0.05f, 0.045f, 0.02f, 0.82f), new Color(1f, 0.75f, 0.18f, 0.45f));
            GUI.skin.label.fontSize = 22;
            GUI.color = new Color(1f, 0.86f, 0.25f, 1f);
            GUI.Label(new Rect(hudScreenWidth * 0.5f - 310f, 84f, 660f, 40f), feedbackText);
            GUI.skin.label.fontSize = 18;
            GUI.color = Color.white;
        }

        GUI.matrix = previousGuiMatrix;

        if (flowState == ZombieStormFlowState.LevelUp)
        {
            DrawUpgradePanel();
        }

        DrawBossBar();
        DrawEliteMarkers();
        DrawDamagePopups();
        DrawScreenFlash();

        if (flowState == ZombieStormFlowState.Paused)
        {
            DrawPausePanel();
        }
        else if (flowState == ZombieStormFlowState.Settings)
        {
            DrawSettingsPanel();
        }
        else if (flowState == ZombieStormFlowState.Results)
        {
            DrawResultsPanel();
        }
    }

    // Returns the passive level value for the current state.
    public int GetPassiveLevel(ZombieStormPassiveType passive)
    {
        int level;
        return passives.TryGetValue(passive, out level) ? level : 0;
    }

    // Handles register enemy logic for ZombieStormGameController.
    public void RegisterEnemy(ZombieStormEnemy enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    // Handles unregister enemy logic for ZombieStormGameController.
    public void UnregisterEnemy(ZombieStormEnemy enemy)
    {
        enemies.Remove(enemy);
    }

    // Handles register obstacle logic for ZombieStormGameController.
    public void RegisterObstacle(ZombieStormObstacle obstacle)
    {
        if (obstacle != null && !obstacles.Contains(obstacle))
        {
            obstacles.Add(obstacle);
        }
    }

    // Handles unregister obstacle logic for ZombieStormGameController.
    public void UnregisterObstacle(ZombieStormObstacle obstacle)
    {
        obstacles.Remove(obstacle);
    }

    // Handles resolve obstacle collision logic for ZombieStormGameController.
    public Vector2 ResolveObstacleCollision(Vector2 position, float moverRadius)
    {
        position = ClampToArena(position);
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                ZombieStormObstacle obstacle = obstacles[i];
                if (obstacle == null)
                {
                    obstacles.RemoveAt(i);
                    continue;
                }

                if (!obstacle.isActiveAndEnabled || !obstacle.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector2 center = obstacle.WorldCenter;
                float minDistance = obstacle.WorldRadius + moverRadius;
                Vector2 offset = position - center;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance >= minDistance * minDistance)
                {
                    continue;
                }

                Vector2 pushDirection = sqrDistance > 0.0001f ? offset.normalized : Vector2.right;
                position = center + pushDirection * minDistance;
            }
        }

        return ClampToArena(position);
    }

    // Handles find nearest enemy logic for ZombieStormGameController.
    public ZombieStormEnemy FindNearestEnemy(Vector2 origin, float maxDistance)
    {
        ZombieStormEnemy best = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            ZombieStormEnemy enemy = enemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
            {
                enemies.RemoveAt(i);
                continue;
            }

            float sqr = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = enemy;
            }
        }

        return best;
    }

    // Handles find random enemy logic for ZombieStormGameController.
    public ZombieStormEnemy FindRandomEnemy()
    {
        if (enemies.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < 10; i++)
        {
            ZombieStormEnemy enemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
            {
                return enemy;
            }
        }

        return FindNearestEnemy(Player != null ? Player.transform.position : Vector3.zero, 999f);
    }

    // Spawns pooled and initializes its starting values.
    public GameObject SpawnPooled(string key, Func<GameObject> factory)
    {
        Queue<GameObject> queue;
        if (!pools.TryGetValue(key, out queue))
        {
            queue = new Queue<GameObject>();
            pools[key] = queue;
        }

        GameObject item = queue.Count > 0 ? queue.Dequeue() : factory();
        item.SetActive(true);
        return item;
    }

    // Handles return pooled logic for ZombieStormGameController.
    public void ReturnPooled(string key, GameObject item)
    {
        if (item == null)
        {
            return;
        }

        item.SetActive(false);
        item.transform.SetParent(poolRoot, false);

        Queue<GameObject> queue;
        if (!pools.TryGetValue(key, out queue))
        {
            queue = new Queue<GameObject>();
            pools[key] = queue;
        }

        queue.Enqueue(item);
    }

    // Spawns player projectile and initializes its starting values.
    public void SpawnPlayerProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life, int pierce, Color color, float size, bool createsFireZoneOnKill = false)
    {
        GameObject projectileObject = SpawnPooled("player_bullet", CreatePlayerProjectile);
        projectileObject.transform.SetParent(worldRoot, false);
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * size;
        SpriteRenderer spriteRenderer = projectileObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetProjectileEffectSprite();
        spriteRenderer.color = color;
        ZombieStormProjectile projectile = projectileObject.GetComponent<ZombieStormProjectile>();
        projectile.Initialize(this, direction, damage, speed, life, pierce, createsFireZoneOnKill);
        PlaySfx("shoot", 0.28f, 0.055f);
    }

    // Spawns a thrown fire bomb projectile and initializes its starting values.
    public void SpawnFireBombProjectile(Vector2 position, Vector2 targetPosition, float impactDamage, float impactRadius, bool leavesFire, float burnDamage, float burnRadius, float burnDuration, float burnTickRate)
    {
        GameObject projectileObject = SpawnPooled("fire_bomb_projectile", CreateFireBombProjectile);
        projectileObject.transform.SetParent(worldRoot, false);
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * 1.08f;
        SpriteRenderer spriteRenderer = projectileObject.GetComponent<SpriteRenderer>();
        Sprite[] frames = GetFireBombEffectFrames();
        spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : fireSprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 44;
        ZombieStormFireBombProjectile projectile = projectileObject.GetComponent<ZombieStormFireBombProjectile>();
        projectile.Initialize(this, position, targetPosition, impactDamage, impactRadius, leavesFire, burnDamage, burnRadius, burnDuration, burnTickRate);
        PlaySfx("shoot", 0.22f, 0.045f);
    }

    // Spawns enemy projectile and initializes its starting values.
    public void SpawnEnemyProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life)
    {
        SpawnEnemyProjectile(position, direction, damage, speed, life, new Color(0.5f, 1f, 0.22f, 1f), 0.44f);
    }

    // Spawns enemy projectile and initializes its starting values.
    public void SpawnEnemyProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life, Color color, float size)
    {
        SpawnEnemyProjectile(position, direction, damage, speed, life, color, size, fireSprite);
    }

    // Spawns enemy rock projectile and initializes its starting values.
    public void SpawnEnemyRockProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life)
    {
        SpawnEnemyProjectile(position, direction, damage, speed, life, new Color(0.62f, 0.54f, 0.43f, 1f), 0.48f, rockSprite);
    }

    // Spawns enemy projectile and initializes its starting values.
    private void SpawnEnemyProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life, Color color, float size, Sprite sprite)
    {
        GameObject projectileObject = SpawnPooled("enemy_spit", CreateEnemyProjectile);
        projectileObject.transform.SetParent(worldRoot, false);
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * size;
        SpriteRenderer spriteRenderer = projectileObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite != null ? sprite : fireSprite;
        spriteRenderer.color = color;
        ZombieStormEnemyProjectile projectile = projectileObject.GetComponent<ZombieStormEnemyProjectile>();
        projectile.Initialize(this, direction, damage * EnemyDamageMultiplier, speed, life, color, size);
    }

    // Spawns ice boss projectile and initializes its starting values.
    public void SpawnIceBossProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life)
    {
        GameObject projectileObject = SpawnPooled("ice_boss_orb", CreateIceBossProjectile);
        projectileObject.transform.SetParent(worldRoot, false);
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * 1.55f;
        SpriteRenderer spriteRenderer = projectileObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = iceBossOrbFrames != null && iceBossOrbFrames.Length > 0 ? iceBossOrbFrames[0] : projectileFxSprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 58;
        ZombieStormIceBossProjectile projectile = projectileObject.GetComponent<ZombieStormIceBossProjectile>();
        projectile.Initialize(this, direction, damage * EnemyDamageMultiplier, speed, life);
        SpawnHitSpark(position, new Color(0.45f, 0.9f, 1f, 0.92f), 0.58f);
    }

    // Spawns ember boss meteor strike and initializes its starting values.
    public void SpawnEmberBossMeteorStrike(Vector2 position, float damage, float radius, float fallDuration)
    {
        GameObject strikeObject = SpawnPooled("ember_meteor_strike", CreateEmberBossMeteorStrike);
        strikeObject.transform.SetParent(worldRoot, false);
        strikeObject.transform.position = position;
        ZombieStormEmberMeteorStrike strike = strikeObject.GetComponent<ZombieStormEmberMeteorStrike>();
        strike.Initialize(this, position, damage * EnemyDamageMultiplier, radius, fallDuration);
    }

    // Spawns area effect and initializes its starting values.
    public void SpawnAreaEffect(Vector2 position, float radius, float damage, float duration, float tickRate, Color color, string poolKey)
    {
        GameObject effectObject = SpawnPooled(poolKey, CreateAreaEffect);
        effectObject.transform.SetParent(worldRoot, false);
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer spriteRenderer = effectObject.GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = IsForegroundEffect(poolKey) ? 48 : 14;
        ZombieStormAreaEffect effect = effectObject.GetComponent<ZombieStormAreaEffect>();
        effect.Initialize(this, poolKey, radius, damage, duration, tickRate);
    }

    // Spawns enemy area effect and initializes its starting values.
    public void SpawnEnemyAreaEffect(Vector2 position, float radius, float damage, float duration, float tickRate, Color color, string poolKey)
    {
        GameObject effectObject = SpawnPooled(poolKey, CreateAreaEffect);
        effectObject.transform.SetParent(worldRoot, false);
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer spriteRenderer = effectObject.GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = IsForegroundEffect(poolKey) ? 48 : 14;
        ZombieStormAreaEffect effect = effectObject.GetComponent<ZombieStormAreaEffect>();
        effect.Initialize(this, poolKey, radius, damage * EnemyDamageMultiplier, duration, tickRate, true);
    }

    // Spawns delayed enemy area effect and initializes its starting values.
    public void SpawnDelayedEnemyAreaEffect(Vector2 position, float delay, float radius, float damage, float duration, float tickRate, Color color, string poolKey, float shakePower = 0f, float shakeDuration = 0f, float sfxVolume = 0f)
    {
        GameObject delayedObject = new GameObject("Delayed Enemy Area Effect");
        delayedObject.transform.SetParent(worldRoot, false);
        ZombieStormDelayedAreaEffect delayed = delayedObject.AddComponent<ZombieStormDelayedAreaEffect>();
        delayed.Initialize(this, position, delay, radius, damage, duration, tickRate, color, poolKey, shakePower, shakeDuration, sfxVolume);
    }

    // Checks whether the current value matches the foreground effect condition.
    private static bool IsForegroundEffect(string poolKey)
    {
        return poolKey == "hit_spark" || poolKey == "lightning_flash" || poolKey == "foozle_explosion" || poolKey == "poison_boss_blast" || poolKey == "ember_dash_blast" || poolKey == "ember_meteor_blast" || poolKey == "ember_boss_meteor";
    }

    // Spawns hit spark and initializes its starting values.
    public void SpawnHitSpark(Vector2 position, Color color, float radius = 0.36f)
    {
        SpawnAreaEffect(position, radius, 0f, 0.12f, 1f, color, "hit_spark");
        PlaySfx("hit", 0.2f + Mathf.Clamp01(radius) * 0.18f, 0.045f);
    }

    // Spawns damage number and initializes its starting values.
    public void SpawnDamageNumber(Vector2 position, float amount, bool critical)
    {
        if (damagePopups.Count > 80)
        {
            damagePopups.RemoveAt(0);
        }

        ZombieStormDamagePopup popup = new ZombieStormDamagePopup();
        popup.Text = Mathf.CeilToInt(amount).ToString();
        popup.WorldPosition = position + UnityEngine.Random.insideUnitCircle * 0.22f;
        popup.Velocity = new Vector2(UnityEngine.Random.Range(-0.18f, 0.18f), UnityEngine.Random.Range(0.75f, 1.05f));
        popup.Color = critical ? new Color(1f, 0.35f, 0.16f, 1f) : new Color(1f, 0.9f, 0.36f, 1f);
        popup.Size = critical ? 22 : 15;
        popup.TimeLeft = critical ? 0.82f : 0.56f;
        damagePopups.Add(popup);
    }

    // Spawns blood splat and initializes its starting values.
    public void SpawnBloodSplat(Vector2 position, float scale)
    {
        if (bloodSplatSprite == null)
        {
            return;
        }

        GameObject splat = SpawnPooled("blood_splat", CreateBloodSplat);
        splat.transform.SetParent(worldRoot, false);
        splat.transform.position = new Vector3(position.x, position.y, 0.5f);
        splat.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        splat.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.75f, 1.25f) * scale;
        SpriteRenderer renderer = splat.GetComponent<SpriteRenderer>();
        renderer.color = new Color(0.34f, 0.02f, 0.025f, UnityEngine.Random.Range(0.42f, 0.62f));
        ZombieStormTimedPooled timed = splat.GetComponent<ZombieStormTimedPooled>();
        timed.Initialize(this, "blood_splat", UnityEngine.Random.Range(16f, 26f));
    }

    // Handles flash screen logic for ZombieStormGameController.
    public void FlashScreen(float amount)
    {
        screenFlashColor = new Color(1f, 0.08f, 0.04f);
        screenFlash = Mathf.Max(screenFlash, amount);
    }

    // Handles flash screen logic for ZombieStormGameController.
    public void FlashScreen(Color color, float amount)
    {
        screenFlashColor = color;
        screenFlash = Mathf.Max(screenFlash, amount);
    }

    // Handles shake camera logic for ZombieStormGameController.
    public void ShakeCamera(float power, float duration)
    {
        cameraShakePower = Mathf.Max(cameraShakePower, power);
        cameraShakeTime = Mathf.Max(cameraShakeTime, duration);
    }

    // Spawns pickup and initializes its starting values.
    public void SpawnPickup(Vector2 position, int xp, int coins)
    {
        if (xp > 0)
        {
            GameObject xpObject = SpawnPooled("xp_orb", CreateXpOrb);
            xpObject.transform.SetParent(worldRoot, false);
            xpObject.transform.position = position + UnityEngine.Random.insideUnitCircle * 0.35f;
            xpObject.GetComponent<ZombieStormPickup>().Initialize(this, "xp_orb", xp, 0);
        }

        if (coins > 0)
        {
            GameObject coinObject = SpawnPooled("coin", CreateCoin);
            coinObject.transform.SetParent(worldRoot, false);
            coinObject.transform.position = position + UnityEngine.Random.insideUnitCircle * 0.45f;
            int finalCoins = Mathf.Max(1, Mathf.RoundToInt(coins * CoinMultiplier));
            coinObject.GetComponent<ZombieStormPickup>().Initialize(this, "coin", 0, finalCoins);
        }
    }

    // Handles on enemy killed logic for ZombieStormGameController.
    public void OnEnemyKilled(ZombieStormEnemy enemy)
    {
        if (Player != null)
        {
            Player.Kills++;
        }

        if (enemy.Type == ZombieStormEnemyType.Elite || enemy.IsBoss)
        {
            PlaySfx(enemy.IsBoss ? "boss_down" : "elite_down", 0.75f, 0.1f);
        }

        int xp = enemy.IsBoss ? BossXpReward(enemy.Type) : enemy.Type == ZombieStormEnemyType.Elite ? 24 : enemy.Type == ZombieStormEnemyType.Reaper ? 10 : enemy.Type == ZombieStormEnemyType.Tank ? 7 : enemy.Type == ZombieStormEnemyType.Gravedigger ? 8 : enemy.Type == ZombieStormEnemyType.OrcThrower ? 7 : enemy.Type == ZombieStormEnemyType.Slasher ? 6 : enemy.Type == ZombieStormEnemyType.SmallGoblin ? 3 : enemy.Type == ZombieStormEnemyType.Goblin ? 4 : enemy.Type == ZombieStormEnemyType.Spitter ? 6 : 3;
        int coins = enemy.IsBoss ? BossCoinReward(enemy.Type) : enemy.Type == ZombieStormEnemyType.Elite ? 18 : UnityEngine.Random.value < 0.24f ? 1 : 0;
        SpawnBloodSplat(enemy.transform.position, enemy.IsBoss ? 2.8f : enemy.Type == ZombieStormEnemyType.Elite ? 1.8f : 1.0f);
        SpawnPickup(enemy.transform.position, xp, coins);

        if (enemy.Type == ZombieStormEnemyType.Elite)
        {
            ShowFeedback("Elite down. Big XP dropped.", 2.5f);
        }

        if (enemy.Type == ZombieStormEnemyType.CrystalGolemBoss)
        {
            firstBossDefeated = true;
        }

        if (enemy.IsBoss && Player != null)
        {
            float bossHeal = enemy.Type == ZombieStormEnemyType.EmberTyrantBoss ? 38f : enemy.Type == ZombieStormEnemyType.MossGolemBoss ? 31f : 26f;
            Player.Heal(bossHeal);
            ShowFeedback(enemy.DisplayName + " defeated. The horde breaks for a moment.", 3f);
        }
    }

    // Handles request level up logic for ZombieStormGameController.
    public void RequestLevelUp()
    {
        if (leveling || finished || flowState != ZombieStormFlowState.Running)
        {
            return;
        }

        leveling = true;
        flowState = ZombieStormFlowState.LevelUp;
        Time.timeScale = 0f;
        currentChoices.Clear();
        BuildUpgradeChoices();
        PlaySfx("level_up", 0.86f, 0.1f);
        ShowFeedback("Level up. Pick a build direction.", 2f);
    }

    // Handles end run logic for ZombieStormGameController.
    public void EndRun(bool victory, string message)
    {
        if (finished)
        {
            return;
        }

        won = victory;
        finished = true;
        flowState = ZombieStormFlowState.Results;
        Time.timeScale = 0f;
        PlaySfx(victory ? "victory" : "fail", 0.9f, 0.1f);
        ShowFeedback(message, 999f);
    }

    // Returns the skill sprite value for the current state.
    public Sprite GetSkillSprite(ZombieStormSkillType skillType)
    {
        if (skillType == ZombieStormSkillType.MagicBolt)
        {
            return GetEffectPreviewSprite("foozle_fireball", 4, bulletSprite);
        }

        if (skillType == ZombieStormSkillType.OrbitingKnife)
        {
            return orbitBladeSprite != null ? orbitBladeSprite : sawSprite;
        }

        if (skillType == ZombieStormSkillType.SummonDrone)
        {
            return fireSpiritSprite != null ? fireSpiritSprite : GetEffectPreviewSprite("foozle_fireball", 4, fireSprite);
        }

        if (skillType == ZombieStormSkillType.ShieldBurst)
        {
            return mineSprite;
        }

        if (skillType == ZombieStormSkillType.Regeneration)
        {
            return softGlowSprite != null ? softGlowSprite : mineSprite;
        }

        if (skillType == ZombieStormSkillType.FireZone)
        {
            return GetEffectPreviewSprite("fire_bomb", 5, fireSprite);
        }

        return bulletSprite;
    }

    public bool HasPlayerWalkAnimation
    {
        get { return playerWalkFrames.Count > 0; }
    }

    public bool PlayerWalkFramesAreIdle
    {
        get { return playerWalkFramesAreIdle; }
    }

    public bool HasPlayerIdleAnimation
    {
        get { return playerIdleFrames != null && playerIdleFrames.Length > 0; }
    }

    // Returns the player walk frame value for the current state.
    public Sprite GetPlayerWalkFrame(string direction, int frameIndex)
    {
        Sprite[] frames;
        if (!playerWalkFrames.TryGetValue(direction, out frames) || frames == null || frames.Length == 0)
        {
            return playerSprite;
        }

        return frames[Mathf.Abs(frameIndex) % frames.Length];
    }

    // Returns the player idle frame value for the current state.
    public Sprite GetPlayerIdleFrame(int frameIndex)
    {
        if (!HasPlayerIdleAnimation)
        {
            return playerSprite;
        }

        return playerIdleFrames[Mathf.Abs(frameIndex) % playerIdleFrames.Length];
    }

    public bool HasPlayerHurtAnimation
    {
        get { return playerHurtFrames != null && playerHurtFrames.Length > 0; }
    }

    public int PlayerHurtFrameCount
    {
        get { return playerHurtFrames != null ? playerHurtFrames.Length : 0; }
    }

    // Returns the player hurt frame value for the current state.
    public Sprite GetPlayerHurtFrame(int frameIndex)
    {
        if (!HasPlayerHurtAnimation)
        {
            return playerSprite;
        }

        return playerHurtFrames[Mathf.Clamp(frameIndex, 0, playerHurtFrames.Length - 1)];
    }

    // Returns the soft shadow sprite value for the current state.
    public Sprite GetSoftShadowSprite()
    {
        return softShadowSprite;
    }

    // Returns the soft glow sprite value for the current state.
    public Sprite GetSoftGlowSprite()
    {
        return softGlowSprite;
    }

    // Returns the orbit ring sprite value for the current state.
    public Sprite GetOrbitRingSprite()
    {
        return orbitRingSprite != null ? orbitRingSprite : softGlowSprite;
    }

    // Returns the health bar sprite value for the current state.
    public Sprite GetHealthBarSprite()
    {
        return tileSprite;
    }

    // Returns the projectile effect sprite value for the current state.
    public Sprite GetProjectileEffectSprite()
    {
        return GetEffectPreviewSprite("foozle_fireball", 4, projectileFxSprite != null ? projectileFxSprite : bulletSprite);
    }

    // Returns the projectile effect frames value for the current state.
    public Sprite[] GetProjectileEffectFrames()
    {
        return GetEffectFrames("foozle_fireball");
    }

    // Returns the fire bomb effect sprite value for the current state.
    public Sprite GetFireBombEffectSprite()
    {
        return GetEffectPreviewSprite("fire_bomb", 5, fireSprite);
    }

    // Returns the fire bomb effect frames value for the current state.
    public Sprite[] GetFireBombEffectFrames()
    {
        return GetEffectFrames("fire_bomb");
    }

    // Returns one of the imported ground-fire effect keys.
    public string GetRandomGroundFireEffectKey()
    {
        return GroundFireEffectKeys[UnityEngine.Random.Range(0, GroundFireEffectKeys.Length)];
    }

    // Returns the ice boss orb frames value for the current state.
    public Sprite[] GetIceBossOrbFrames()
    {
        return iceBossOrbFrames;
    }

    // Returns the effect frames value for the current state.
    public Sprite[] GetEffectFrames(string effectKey)
    {
        if (effectFrames.Count == 0)
        {
            return null;
        }

        Sprite[] frames;
        if (effectFrames.TryGetValue(effectKey, out frames) && frames != null && frames.Length > 0)
        {
            return frames;
        }

        string sequenceKey = "spark";
        if (effectKey == "fire_pool" || effectKey.StartsWith("fire_pool_", StringComparison.Ordinal) || effectKey == "toxic_pool" || effectKey == "meteor_blast" || effectKey == "meteor_warning")
        {
            sequenceKey = "fire";
        }
        else if (effectKey == "lightning_flash" || effectKey == "ultimate_spark" || effectKey == "ultimate_storm")
        {
            sequenceKey = "lightning";
        }
        else if (effectKey == "zombie_explosion" || effectKey == "mine_blast")
        {
            sequenceKey = "explosion";
        }
        else if (effectKey == "shield_burst" || effectKey == "upgrade_pulse" || effectKey == "upgrade_ring")
        {
            sequenceKey = "burst";
        }

        if (effectFrames.TryGetValue(sequenceKey, out frames) && frames != null && frames.Length > 0)
        {
            return frames;
        }

        return null;
    }

    // Handles rotate logic for ZombieStormGameController.
    public static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    // Handles with alpha logic for ZombieStormGameController.
    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    // Handles play sfx logic for ZombieStormGameController.
    public void PlaySfx(string key, float volume = 1f, float minInterval = 0.02f)
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip clip;
        if (!sfx.TryGetValue(key, out clip) || clip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        float last;
        if (sfxLastPlayed.TryGetValue(key, out last) && now - last < minInterval)
        {
            return;
        }

        sfxLastPlayed[key] = now;
        audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
        audioSource.PlayOneShot(clip, sfxMuted ? 0f : Mathf.Clamp01(volume * masterVolume * sfxVolume));
    }

    // Handles request start run logic for ZombieStormGameController.
    public void RequestStartRun()
    {
        StartRun();
    }

    // Handles request open main menu settings logic for ZombieStormGameController.
    public void RequestOpenMainMenuSettings()
    {
        OpenSettings(ZombieStormFlowState.MainMenu);
    }

    // Handles request close settings logic for ZombieStormGameController.
    public void RequestCloseSettings()
    {
        CloseSettings();
    }

    // Handles request quit logic for ZombieStormGameController.
    public void RequestQuit()
    {
        QuitGame();
    }

    // Applies menu settings effects to the current game state.
    public void ApplyMenuSettings(float master, float music, float sfx, bool fullscreen)
    {
        masterVolume = Mathf.Clamp01(master);
        musicVolume = Mathf.Clamp01(music);
        sfxVolume = Mathf.Clamp01(sfx);
        sfxMuted = sfxVolume <= 0.001f;
        Screen.fullScreen = fullscreen;

        PlayerPrefs.SetFloat("ZombieStorm.MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("ZombieStorm.MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("ZombieStorm.SfxVolume", sfxVolume);
        PlayerPrefs.SetInt("ZombieStorm.Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Handles start run logic for ZombieStormGameController.
    private void StartRun()
    {
        runTime = 0f;
        spawnTimer = 1.15f;
        eliteTimer = 70f;
        feedbackTimer = 0f;
        bossCount = 0;
        leveling = false;
        finished = false;
        won = false;
        firstBossDefeated = false;
        difficultyScore = 1f;
        flowState = ZombieStormFlowState.Running;
        Time.timeScale = 1f;

        ClearActiveObjects();
        BuildEnvironment();

        GameObject playerObject = new GameObject("Pixel Survivor");
        TrySetPlayerTag(playerObject);
        playerObject.transform.SetParent(worldRoot, false);
        playerObject.transform.position = Vector3.zero;
        SpriteRenderer playerRenderer = playerObject.AddComponent<SpriteRenderer>();
        playerRenderer.sprite = playerSprite;
        playerRenderer.sortingOrder = 30;
        AddShadow(playerObject.transform, new Vector3(2.05f, 0.68f, 1f), -0.2f, 18);
        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        Player = playerObject.AddComponent<ZombieStormPlayer>();
        Player.Initialize(this, playerRenderer);

        Skills = playerObject.AddComponent<ZombieStormSkillManager>();
        Skills.Initialize(this, Player);
        Skills.LearnSkill(ZombieStormSkillType.MagicBolt);
        Skills.LearnSkill(ZombieStormSkillType.OrbitingKnife);
        Skills.LearnSkill(ZombieStormSkillType.FireZone);
        Skills.LevelUpSkill(ZombieStormSkillType.FireZone);
        Skills.LevelUpSkill(ZombieStormSkillType.FireZone);
        Skills.LevelUpSkill(ZombieStormSkillType.FireZone);

        FollowPlayer(true);
        PlaySfx("start", 0.56f, 0.1f);
        ShowFeedback("Wave 1: Magic Bolt, Fire Blades, and Fire Zone Lv.4 online. Move, kite, collect XP.", 3f);
        PlayStartupEmberMeteorPreview();
    }

    // Handles play startup ember meteor preview logic for ZombieStormGameController.
    private void PlayStartupEmberMeteorPreview()
    {
        if (Player == null)
        {
            return;
        }

        Vector2 center = Player.transform.position;
        Vector2[] offsets =
        {
            new Vector2(-5.8f, 3.2f),
            new Vector2(-3.5f, 1.4f),
            new Vector2(-1.2f, 3.6f),
            new Vector2(1.7f, 2.3f),
            new Vector2(4.4f, 3.4f),
            new Vector2(6.0f, 0.7f),
            new Vector2(3.5f, -1.7f),
            new Vector2(0.8f, -3.2f),
            new Vector2(-2.7f, -2.4f),
            new Vector2(-5.2f, -0.7f),
            new Vector2(5.2f, -3.4f),
            new Vector2(-6.8f, -3.6f),
            new Vector2(0f, 0.9f),
            new Vector2(2.6f, 4.8f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            float radius = i % 3 == 0 ? 1.25f : i % 3 == 1 ? 1.05f : 0.92f;
            SpawnEmberBossMeteorStrike(center + offsets[i], 0f, radius, 4f);
        }
    }

    // Handles pause run logic for ZombieStormGameController.
    private void PauseRun()
    {
        if (flowState != ZombieStormFlowState.Running)
        {
            return;
        }

        flowState = ZombieStormFlowState.Paused;
        Time.timeScale = 0f;
        ShowFeedback("Run paused.", 1.6f);
    }

    // Handles resume run logic for ZombieStormGameController.
    private void ResumeRun()
    {
        if (finished)
        {
            return;
        }

        flowState = ZombieStormFlowState.Running;
        Time.timeScale = 1f;
        ShowFeedback("Back to the street.", 1.6f);
    }

    // Handles open settings logic for ZombieStormGameController.
    private void OpenSettings(ZombieStormFlowState returnState)
    {
        settingsReturnState = returnState;
        flowState = ZombieStormFlowState.Settings;
        Time.timeScale = 0f;
    }

    // Handles close settings logic for ZombieStormGameController.
    private void CloseSettings()
    {
        flowState = settingsReturnState;
        Time.timeScale = flowState == ZombieStormFlowState.Running ? 1f : 0f;
    }

    // Handles return to main menu logic for ZombieStormGameController.
    private void ReturnToMainMenu()
    {
        ClearActiveObjects();
        Player = null;
        Skills = null;
        currentChoices.Clear();
        damagePopups.Clear();
        leveling = false;
        finished = false;
        won = false;
        flowState = ZombieStormFlowState.MainMenu;
        settingsReturnState = ZombieStormFlowState.MainMenu;
        Time.timeScale = 0f;
    }

    // Builds the scene objects by combining their child elements.
    private void BuildScene()
    {
        worldRoot = new GameObject("Zombie Storm Runtime").transform;
        poolRoot = new GameObject("Object Pool").transform;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.name = "Zombie Storm Camera";
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = GameplayCameraOrthographicSize;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.035f, 0.04f, 0.052f);
        SetupMainMenuUI();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
    }

    // Builds the floor, roads, obstacles, and decorative scene objects.
    private void BuildEnvironment()
    {
        usingCustomArenaMap = false;
        if (BuildCustomArenaMap())
        {
            return;
        }

        if (groundSprites.Count > 0)
        {
            BuildKenneyCityFloor();
        }
        else
        {
            BuildFallbackNeonFloor();
        }

        BuildCityBlockSilhouettes();
        BuildAtmosphericDetails();
        BuildCityDebris();
        BuildNeonAccents();
    }

    // Creates or loads sprites used by characters, skills, pickups, effects, and UI.
    private void CreateSprites()
    {
        playerSprite = CreateSurvivorSprite();
        LoadPlayerWalkFrames();
        LoadScreenSelectedHurtFrames();
        zombieSprite = CreatePixelSprite(new Color(0.32f, 0.9f, 0.32f), new Color(0.08f, 0.25f, 0.08f), 16, true);
        fastZombieSprite = CreatePixelSprite(new Color(0.75f, 1f, 0.25f), new Color(0.12f, 0.34f, 0.08f), 16, true);
        tankZombieSprite = CreatePixelSprite(new Color(0.54f, 0.72f, 0.34f), new Color(0.15f, 0.25f, 0.12f), 18, true);
        exploderSprite = CreatePixelSprite(new Color(1f, 0.76f, 0.16f), new Color(0.75f, 0.1f, 0.02f), 18, true);
        spitterSprite = CreatePixelSprite(new Color(0.45f, 1f, 0.75f), new Color(0.08f, 0.35f, 0.28f), 16, true);
        eliteSprite = CreatePixelSprite(new Color(1f, 0.42f, 0.18f), new Color(0.45f, 0.04f, 0.02f), 20, true);
        bossSprite = CreatePixelSprite(new Color(0.95f, 0.12f, 0.12f), new Color(0.3f, 0.01f, 0.01f), 24, true);
        bulletSprite = CreatePixelSprite(new Color(1f, 0.92f, 0.22f), Color.white, 8, true);
        xpSprite = CreatePixelSprite(new Color(0.12f, 0.75f, 1f), Color.white, 8, true);
        coinSprite = CreatePixelSprite(new Color(1f, 0.73f, 0.15f), new Color(1f, 0.95f, 0.55f), 8, true);
        fireSprite = CreatePixelSprite(new Color(1f, 0.28f, 0.04f), new Color(1f, 0.82f, 0.1f), 18, true);
        rockSprite = CreatePixelSprite(new Color(0.42f, 0.36f, 0.28f), new Color(0.72f, 0.66f, 0.54f), 12, true);
        sawSprite = CreatePixelSprite(new Color(0.82f, 0.84f, 0.9f), new Color(0.2f, 0.75f, 1f), 14, true);
        orbitBladeSprite = CreateOrbitingBladeSprite();
        orbitRingSprite = CreateOrbitingRingSprite();
        mineSprite = CreatePixelSprite(new Color(0.22f, 0.22f, 0.25f), new Color(1f, 0.18f, 0.08f), 12, true);
        tileSprite = CreatePixelSprite(Color.white, Color.white, 8, false);
        ruinSprite = CreatePixelSprite(Color.white, new Color(0.06f, 0.06f, 0.08f), 12, false);
        softShadowSprite = CreateSoftDiscSprite(new Color(0f, 0f, 0f, 0.58f), 64, 1f, 0.34f);
        softGlowSprite = CreateSoftDiscSprite(new Color(1f, 1f, 1f, 0.72f), 64, 1f, 0.08f);
        bloodSplatSprite = CreateBloodSplatSprite();
        neonSignSprite = CreateNeonSignSprite();
        LoadChibiEnemyWalkFrames();
        LoadCraftpixGoblinFrames();
        LoadCraftpixVillagerFrames();
        LoadCraftpixGravediggerFrames();
        LoadCraftpixReaperFrames();
        LoadCraftpixOrcFrames();
        LoadCraftpixCrystalGolemFrames();
        LoadCraftpixMossGolemFrames();
        LoadCraftpixEmberGolemFrames();
        LoadCustomArenaMap();
        LoadMainMenuCover();
        LoadFireSpiritSprite();
        LoadKenneyTopdownArt();
        LoadMikodrakSpellEffects();
        LoadIceBossOrbFrames();
    }

    // Generates sound clips for attacks, pickups, upgrades, hits, and feedback.
    private void CreateAudioClips()
    {
        sfx.Clear();
        sfx["shoot"] = CreateSynthClip("zs_shoot", 0.075f, 820f, 1180f, 0.45f, 0.08f, ZombieStormWave.Square);
        sfx["hit"] = CreateSynthClip("zs_hit", 0.07f, 190f, 82f, 0.55f, 0.42f, ZombieStormWave.Noise);
        sfx["pickup"] = CreateSynthClip("zs_pickup", 0.105f, 620f, 1240f, 0.35f, 0.02f, ZombieStormWave.Triangle);
        sfx["coin"] = CreateSynthClip("zs_coin", 0.12f, 980f, 1680f, 0.36f, 0.01f, ZombieStormWave.Sine);
        sfx["hurt"] = CreateSynthClip("zs_hurt", 0.16f, 190f, 74f, 0.62f, 0.24f, ZombieStormWave.Saw);
        sfx["level_up"] = CreateArpeggioClip("zs_level_up", new[] { 520f, 780f, 1040f, 1560f }, 0.34f, 0.48f);
        sfx["upgrade"] = CreateArpeggioClip("zs_upgrade", new[] { 440f, 660f, 990f }, 0.24f, 0.42f);
        sfx["boom"] = CreateSynthClip("zs_boom", 0.28f, 110f, 38f, 0.8f, 0.58f, ZombieStormWave.Noise);
        sfx["lightning"] = CreateSynthClip("zs_lightning", 0.16f, 1380f, 420f, 0.46f, 0.22f, ZombieStormWave.Saw);
        sfx["ultimate"] = CreateSynthClip("zs_ultimate", 0.46f, 180f, 58f, 0.74f, 0.32f, ZombieStormWave.Saw);
        sfx["elite_down"] = CreateArpeggioClip("zs_elite_down", new[] { 760f, 570f, 380f }, 0.2f, 0.48f);
        sfx["boss_down"] = CreateArpeggioClip("zs_boss_down", new[] { 360f, 540f, 720f, 1080f }, 0.42f, 0.56f);
        sfx["victory"] = CreateArpeggioClip("zs_victory", new[] { 520f, 660f, 780f, 1040f, 1320f }, 0.62f, 0.58f);
        sfx["fail"] = CreateArpeggioClip("zs_fail", new[] { 330f, 247f, 196f }, 0.42f, 0.62f);
        sfx["start"] = CreateArpeggioClip("zs_start", new[] { 330f, 495f, 660f }, 0.26f, 0.34f);
    }

    // Generates a short synthetic sound clip from frequency and wave settings.
    private AudioClip CreateSynthClip(string clipName, float duration, float startFrequency, float endFrequency, float volume, float noiseAmount, ZombieStormWave wave)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        float phase = 0f;
        uint noiseState = 22222u;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            phase += frequency / sampleRate;
            phase -= Mathf.Floor(phase);
            float envelope = Mathf.Pow(1f - t, 1.7f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 24f));
            float tone = EvaluateWave(wave, phase);
            float noise = NextNoise(ref noiseState);
            samples[i] = Mathf.Clamp((tone * (1f - noiseAmount) + noise * noiseAmount) * envelope * volume, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Generates a short arpeggio sound from a list of notes.
    private AudioClip CreateArpeggioClip(string clipName, float[] notes, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        float phase = 0f;
        int noteCount = Mathf.Max(1, notes.Length);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t * noteCount), 0, noteCount - 1);
            float noteT = (t * noteCount) - noteIndex;
            float frequency = notes[noteIndex];
            phase += frequency / sampleRate;
            phase -= Mathf.Floor(phase);
            float envelope = Mathf.Pow(1f - noteT, 1.25f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, noteT * 18f)) * Mathf.Pow(1f - t * 0.2f, 1.1f);
            samples[i] = EvaluateWave(ZombieStormWave.Triangle, phase) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Evaluates one audio sample for the selected oscillator wave shape.
    private static float EvaluateWave(ZombieStormWave wave, float phase)
    {
        if (wave == ZombieStormWave.Square)
        {
            return phase < 0.5f ? 1f : -1f;
        }

        if (wave == ZombieStormWave.Triangle)
        {
            return 1f - Mathf.Abs(phase * 4f - 2f);
        }

        if (wave == ZombieStormWave.Saw)
        {
            return phase * 2f - 1f;
        }

        if (wave == ZombieStormWave.Noise)
        {
            return 0f;
        }

        return Mathf.Sin(phase * Mathf.PI * 2f);
    }

    // Generates pseudo-random noise used to make synth sounds punchier.
    private static float NextNoise(ref uint state)
    {
        state = state * 1664525u + 1013904223u;
        return ((state >> 8) / 16777215f) * 2f - 1f;
    }

    // Scales difficulty over time so later waves spawn more dangerous enemies.
    private void UpdateDynamicDifficulty()
    {
        float timeFactor = 0.62f + runTime / 92f;
        float lowHealthMercy = Player != null && Player.Health / Player.MaxHealth < 0.42f ? 0.68f : 1f;
        float dominance = Player != null && runTime > 45f && Player.Kills > runTime * 1.15f ? 1.16f : 1f;
        difficultyScore = Mathf.Clamp(timeFactor * lowHealthMercy * dominance, 0.55f, 8f);
    }

    // Spawns regular enemies over time and triggers boss waves at scheduled moments.
    private void UpdateSpawning()
    {
        spawnTimer -= Time.deltaTime;
        eliteTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            spawnTimer = Mathf.Max(0.28f, 1.85f - runTime / 210f);
            float earlyCount = runTime < 35f ? 1f : runTime < 75f ? 1.65f : 2f + difficultyScore * 0.9f;
            int count = Mathf.Clamp(Mathf.RoundToInt(earlyCount), 1, 14);
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(ChooseEnemyType());
            }
        }

        if (eliteTimer <= 0f)
        {
            eliteTimer = Mathf.Max(34f, 58f - runTime / 14f);
            SpawnEnemy(firstBossDefeated ? ZombieStormEnemyType.Reaper : ZombieStormEnemyType.Gravedigger);
            ShowFeedback("Heavy zombie incoming. Keep your distance.", 2.5f);
        }

        if (bossCount == 0 && runTime >= 90f)
        {
            bossCount++;
            SpawnBossWave(ZombieStormEnemyType.CrystalGolemBoss);
        }
        else if (bossCount == 1 && runTime >= 185f)
        {
            bossCount++;
            SpawnBossWave(ZombieStormEnemyType.MossGolemBoss);
        }
        else if (bossCount == 2 && runTime >= 270f)
        {
            bossCount++;
            SpawnBossWave(ZombieStormEnemyType.EmberTyrantBoss);
        }
    }

    // Spawns a specific boss and shows a warning message to the player.
    private void SpawnBossWave(ZombieStormEnemyType bossType)
    {
        SpawnEnemy(bossType);
        ShowFeedback(BossWaveWarning(bossType), 3f);
    }

    // Chooses the next enemy type using time-based random weights.
    private ZombieStormEnemyType ChooseEnemyType()
    {
        float roll = UnityEngine.Random.value;
        bool lowHealth = Player != null && Player.Health / Player.MaxHealth < 0.28f;

        if (firstBossDefeated && roll < (lowHealth ? 0.08f : 0.18f))
        {
            return ZombieStormEnemyType.SmallGoblin;
        }

        if (firstBossDefeated && runTime > 82f && roll < (lowHealth ? 0.14f : 0.3f))
        {
            return ZombieStormEnemyType.Reaper;
        }

        if (runTime > 45f && roll < (lowHealth ? 0.18f : 0.34f))
        {
            return ZombieStormEnemyType.OrcThrower;
        }

        if (runTime > 58f && roll < (lowHealth ? 0.3f : 0.52f))
        {
            return ZombieStormEnemyType.Gravedigger;
        }

        if (runTime > 32f && roll < (lowHealth ? 0.24f : 0.42f))
        {
            return ZombieStormEnemyType.Slasher;
        }

        return ZombieStormEnemyType.Goblin;
    }

    // Creates an enemy offscreen and initializes its stats, sprite, and animation frames.
    private void SpawnEnemy(ZombieStormEnemyType enemyType)
    {
        enemyType = RemapRemovedBaseZombieType(enemyType);
        string key = "enemy_" + enemyType;
        GameObject enemyObject = SpawnPooled(key, CreateEnemy);
        enemyObject.name = "Zombie " + enemyType;
        enemyObject.transform.SetParent(worldRoot, false);
        enemyObject.transform.position = GetOffscreenSpawnPosition();
        ZombieStormEnemy enemy = enemyObject.GetComponent<ZombieStormEnemy>();
        Sprite[] walkFrames = GetEnemyWalkFrames(enemyType);
        bool framesFaceRight = walkFrames != chibiEnemyWalkFrames;
        enemy.Initialize(this, enemyType, key, GetEnemySprite(enemyType, walkFrames), walkFrames, GetEnemyAttackFrames(enemyType), GetEnemySpecialAttackFrames(enemyType), GetEnemyHurtFrames(enemyType), GetEnemyDeathFrames(enemyType), framesFaceRight, runTime, difficultyScore);
    }

    // Maps removed base zombie types to enemy types that still exist in the project.
    private ZombieStormEnemyType RemapRemovedBaseZombieType(ZombieStormEnemyType enemyType)
    {
        if (enemyType == ZombieStormEnemyType.Grunt ||
            enemyType == ZombieStormEnemyType.Fast ||
            enemyType == ZombieStormEnemyType.Tank ||
            enemyType == ZombieStormEnemyType.Exploder ||
            enemyType == ZombieStormEnemyType.Spitter ||
            enemyType == ZombieStormEnemyType.Elite)
        {
            if (firstBossDefeated && runTime > 82f)
            {
                return ZombieStormEnemyType.Reaper;
            }

            if (runTime > 58f)
            {
                return ZombieStormEnemyType.Gravedigger;
            }

            return ZombieStormEnemyType.Goblin;
        }

        return enemyType;
    }

    // Chooses a spawn point just outside the visible player area.
    private Vector2 GetOffscreenSpawnPosition()
    {
        Vector2 center = Player != null ? Player.transform.position : Vector3.zero;
        Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.up;
        }

        float spawnDistance = mainCamera != null ? mainCamera.orthographicSize * 1.65f + 3f : 16f;
        Vector2 spawnPosition = center + direction * spawnDistance;
        return usingCustomArenaMap ? ClampToArena(spawnPosition) : spawnPosition;
    }

    // Returns the default sprite for a given enemy type.
    private Sprite GetEnemySprite(ZombieStormEnemyType enemyType, Sprite[] walkFrames)
    {
        if (walkFrames != null && walkFrames.Length > 0)
        {
            return walkFrames[0];
        }

        if (kenneyZombieSprite != null)
        {
            if (enemyType == ZombieStormEnemyType.Fast && kenneyFastZombieSprite != null)
            {
                return kenneyFastZombieSprite;
            }

            if (enemyType == ZombieStormEnemyType.Tank && kenneyTankZombieSprite != null)
            {
                return kenneyTankZombieSprite;
            }

            if (enemyType == ZombieStormEnemyType.Elite && kenneyEliteZombieSprite != null)
            {
                return kenneyEliteZombieSprite;
            }

            if (enemyType == ZombieStormEnemyType.Boss && kenneyBossSprite != null)
            {
                return kenneyBossSprite;
            }

            if (enemyType == ZombieStormEnemyType.PlagueBoss && kenneyFastZombieSprite != null)
            {
                return kenneyFastZombieSprite;
            }

            if (enemyType == ZombieStormEnemyType.BruteBoss && kenneyTankZombieSprite != null)
            {
                return kenneyTankZombieSprite;
            }

            if (enemyType == ZombieStormEnemyType.StormBoss && kenneyEliteZombieSprite != null)
            {
                return kenneyEliteZombieSprite;
            }

            return kenneyZombieSprite;
        }

        if (enemyType == ZombieStormEnemyType.Fast)
        {
            return fastZombieSprite;
        }

        if (enemyType == ZombieStormEnemyType.Tank)
        {
            return tankZombieSprite;
        }

        if (enemyType == ZombieStormEnemyType.Exploder)
        {
            return exploderSprite;
        }

        if (enemyType == ZombieStormEnemyType.Spitter)
        {
            return spitterSprite;
        }

        if (enemyType == ZombieStormEnemyType.Elite)
        {
            return eliteSprite;
        }

        if (enemyType == ZombieStormEnemyType.Boss)
        {
            return bossSprite;
        }

        if (enemyType == ZombieStormEnemyType.PlagueBoss)
        {
            return spitterSprite;
        }

        if (enemyType == ZombieStormEnemyType.BruteBoss)
        {
            return tankZombieSprite;
        }

        if (enemyType == ZombieStormEnemyType.StormBoss)
        {
            return eliteSprite;
        }

        return zombieSprite;
    }

    // Returns walk animation frames for a given enemy type.
    private Sprite[] GetEnemyWalkFrames(ZombieStormEnemyType enemyType)
    {
        if ((enemyType == ZombieStormEnemyType.Goblin || enemyType == ZombieStormEnemyType.SmallGoblin) && goblinRunFrames != null && goblinRunFrames.Length > 0)
        {
            return goblinRunFrames;
        }

        if (enemyType == ZombieStormEnemyType.Slasher && villagerRunFrames != null && villagerRunFrames.Length > 0)
        {
            return villagerRunFrames;
        }

        if (enemyType == ZombieStormEnemyType.Gravedigger && gravediggerRunFrames != null && gravediggerRunFrames.Length > 0)
        {
            return gravediggerRunFrames;
        }

        if (enemyType == ZombieStormEnemyType.Reaper && reaperRunFrames != null && reaperRunFrames.Length > 0)
        {
            return reaperRunFrames;
        }

        if (enemyType == ZombieStormEnemyType.OrcThrower && orcRunFrames != null && orcRunFrames.Length > 0)
        {
            return orcRunFrames;
        }

        if (enemyType == ZombieStormEnemyType.CrystalGolemBoss && crystalGolemRunFrames != null && crystalGolemRunFrames.Length > 0)
        {
            return crystalGolemRunFrames;
        }

        if (enemyType == ZombieStormEnemyType.MossGolemBoss && mossGolemRunFrames != null && mossGolemRunFrames.Length > 0)
        {
            return mossGolemRunFrames;
        }

        if (enemyType == ZombieStormEnemyType.EmberTyrantBoss && emberGolemRunFrames != null && emberGolemRunFrames.Length > 0)
        {
            return emberGolemRunFrames;
        }

        if (chibiEnemyWalkFrames != null && chibiEnemyWalkFrames.Length > 0)
        {
            return chibiEnemyWalkFrames;
        }

        return null;
    }

    // Returns a preview frame for a skill effect or warning marker.
    private Sprite GetEffectPreviewSprite(string effectKey, int preferredIndex, Sprite fallback)
    {
        Sprite[] frames = GetEffectFrames(effectKey);
        if (frames == null || frames.Length == 0)
        {
            return fallback;
        }

        return frames[Mathf.Clamp(preferredIndex, 0, frames.Length - 1)];
    }

    // Returns normal attack animation frames for a given enemy type.
    private Sprite[] GetEnemyAttackFrames(ZombieStormEnemyType enemyType)
    {
        if (enemyType == ZombieStormEnemyType.Gravedigger && gravediggerSlashFrames.Length > 0)
        {
            return gravediggerSlashFrames;
        }

        if (enemyType == ZombieStormEnemyType.Reaper && reaperSlashFrames.Length > 0)
        {
            return reaperSlashFrames;
        }

        if (enemyType == ZombieStormEnemyType.OrcThrower && orcThrowFrames.Length > 0)
        {
            return orcThrowFrames;
        }

        if (enemyType == ZombieStormEnemyType.CrystalGolemBoss && crystalGolemSlashFrames.Length > 0)
        {
            return crystalGolemSlashFrames;
        }

        if (enemyType == ZombieStormEnemyType.MossGolemBoss && mossGolemSlashFrames.Length > 0)
        {
            return mossGolemSlashFrames;
        }

        if (enemyType == ZombieStormEnemyType.EmberTyrantBoss && emberGolemSlashFrames.Length > 0)
        {
            return emberGolemSlashFrames;
        }

        return enemyType == ZombieStormEnemyType.Slasher && villagerSlashFrames.Length > 0 ? villagerSlashFrames : null;
    }

    // Returns special attack animation frames for a given enemy type.
    private Sprite[] GetEnemySpecialAttackFrames(ZombieStormEnemyType enemyType)
    {
        if (enemyType == ZombieStormEnemyType.CrystalGolemBoss && crystalGolemThrowFrames.Length > 0)
        {
            return crystalGolemThrowFrames;
        }

        if (enemyType == ZombieStormEnemyType.MossGolemBoss && mossGolemThrowFrames.Length > 0)
        {
            return mossGolemThrowFrames;
        }

        return enemyType == ZombieStormEnemyType.EmberTyrantBoss && emberGolemThrowFrames.Length > 0 ? emberGolemThrowFrames : null;
    }

    // Returns hurt animation frames for a given enemy type.
    private Sprite[] GetEnemyHurtFrames(ZombieStormEnemyType enemyType)
    {
        if ((enemyType == ZombieStormEnemyType.Goblin || enemyType == ZombieStormEnemyType.SmallGoblin) && goblinHurtFrames.Length > 0)
        {
            return goblinHurtFrames;
        }

        if (enemyType == ZombieStormEnemyType.Gravedigger && gravediggerHurtFrames.Length > 0)
        {
            return gravediggerHurtFrames;
        }

        if (enemyType == ZombieStormEnemyType.Reaper && reaperHurtFrames.Length > 0)
        {
            return reaperHurtFrames;
        }

        if (enemyType == ZombieStormEnemyType.OrcThrower && orcHurtFrames.Length > 0)
        {
            return orcHurtFrames;
        }

        if (enemyType == ZombieStormEnemyType.CrystalGolemBoss && crystalGolemHurtFrames.Length > 0)
        {
            return crystalGolemHurtFrames;
        }

        if (enemyType == ZombieStormEnemyType.MossGolemBoss && mossGolemHurtFrames.Length > 0)
        {
            return mossGolemHurtFrames;
        }

        if (enemyType == ZombieStormEnemyType.EmberTyrantBoss && emberGolemHurtFrames.Length > 0)
        {
            return emberGolemHurtFrames;
        }

        return enemyType == ZombieStormEnemyType.Slasher && villagerHurtFrames.Length > 0 ? villagerHurtFrames : null;
    }

    // Returns death animation frames for a given enemy type.
    private Sprite[] GetEnemyDeathFrames(ZombieStormEnemyType enemyType)
    {
        if ((enemyType == ZombieStormEnemyType.Goblin || enemyType == ZombieStormEnemyType.SmallGoblin) && goblinDeathFrames.Length > 0)
        {
            return goblinDeathFrames;
        }

        if (enemyType == ZombieStormEnemyType.Gravedigger && gravediggerDeathFrames.Length > 0)
        {
            return gravediggerDeathFrames;
        }

        if (enemyType == ZombieStormEnemyType.Reaper && reaperDeathFrames.Length > 0)
        {
            return reaperDeathFrames;
        }

        if (enemyType == ZombieStormEnemyType.OrcThrower && orcDeathFrames.Length > 0)
        {
            return orcDeathFrames;
        }

        if (enemyType == ZombieStormEnemyType.CrystalGolemBoss && crystalGolemDeathFrames.Length > 0)
        {
            return crystalGolemDeathFrames;
        }

        if (enemyType == ZombieStormEnemyType.MossGolemBoss && mossGolemDeathFrames.Length > 0)
        {
            return mossGolemDeathFrames;
        }

        if (enemyType == ZombieStormEnemyType.EmberTyrantBoss && emberGolemDeathFrames.Length > 0)
        {
            return emberGolemDeathFrames;
        }

        return enemyType == ZombieStormEnemyType.Slasher && villagerDeathFrames.Length > 0 ? villagerDeathFrames : null;
    }

    // Creates a pooled enemy object with renderer, collider, and enemy script.
    private GameObject CreateEnemy()
    {
        GameObject item = new GameObject("Pooled Zombie");
        AddShadow(item.transform, new Vector3(1.4f, 0.46f, 1f), -0.08f, 17);
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 20;
        item.AddComponent<ZombieStormEnemy>();
        return item;
    }

    // Creates a pooled player projectile object.
    private GameObject CreatePlayerProjectile()
    {
        GameObject item = new GameObject("Player Bullet");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetProjectileEffectSprite();
        spriteRenderer.sortingOrder = 40;
        item.AddComponent<ZombieStormProjectile>();
        return item;
    }

    // Creates a pooled fire bomb projectile object.
    private GameObject CreateFireBombProjectile()
    {
        GameObject item = new GameObject("Fire Bomb");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetFireBombEffectSprite();
        spriteRenderer.sortingOrder = 44;
        item.AddComponent<ZombieStormFireBombProjectile>();
        return item;
    }

    // Creates a pooled enemy projectile object.
    private GameObject CreateEnemyProjectile()
    {
        GameObject item = new GameObject("Enemy Spit");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = fireSprite;
        spriteRenderer.color = new Color(0.45f, 1f, 0.25f);
        spriteRenderer.sortingOrder = 39;
        item.AddComponent<ZombieStormEnemyProjectile>();
        return item;
    }

    // Creates a pooled projectile for the ice boss.
    private GameObject CreateIceBossProjectile()
    {
        GameObject item = new GameObject("Ice Boss Orb");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = iceBossOrbFrames != null && iceBossOrbFrames.Length > 0 ? iceBossOrbFrames[0] : projectileFxSprite;
        spriteRenderer.sortingOrder = 58;
        item.AddComponent<ZombieStormIceBossProjectile>();
        return item;
    }

    // Creates a pooled meteor strike object for the ember boss.
    private GameObject CreateEmberBossMeteorStrike()
    {
        GameObject item = new GameObject("Ember Boss Meteor Strike");
        item.AddComponent<ZombieStormEmberMeteorStrike>();
        return item;
    }

    // Creates a pooled persistent area effect object.
    private GameObject CreateAreaEffect()
    {
        GameObject item = new GameObject("Area Effect");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = fireSprite;
        spriteRenderer.sortingOrder = 12;
        item.AddComponent<ZombieStormAreaEffect>();
        return item;
    }

    // Creates a pooled XP pickup object.
    private GameObject CreateXpOrb()
    {
        GameObject item = new GameObject("XP Orb");
        AddGlow(item.transform, new Color(0.1f, 0.8f, 1f, 0.34f), Vector3.one * 1.9f, 22);
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = xpSprite;
        spriteRenderer.sortingOrder = 24;
        item.AddComponent<ZombieStormPickup>();
        return item;
    }

    // Creates a pooled coin pickup object.
    private GameObject CreateCoin()
    {
        GameObject item = new GameObject("Coin");
        AddGlow(item.transform, new Color(1f, 0.75f, 0.08f, 0.32f), Vector3.one * 1.75f, 22);
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = coinSprite;
        spriteRenderer.sortingOrder = 24;
        item.AddComponent<ZombieStormPickup>();
        return item;
    }

    // Creates a pooled blood splat visual effect.
    private GameObject CreateBloodSplat()
    {
        GameObject item = new GameObject("Blood Splat");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = bloodSplatSprite;
        spriteRenderer.sortingOrder = -3;
        item.AddComponent<ZombieStormTimedPooled>();
        return item;
    }

    // Builds the upgrade card choices shown when the player levels up.
    private void BuildUpgradeChoices()
    {
        choiceKeys.Clear();
        if (Skills != null && Skills.KnownSkillCount > 0)
        {
            AddUpgradeChoice(CreateKnownSkillOption());
        }

        int guard = 0;
        while (currentChoices.Count < 3 && guard < 80)
        {
            guard++;
            ZombieStormUpgradeOption option = CreateRandomUpgradeOption();
            AddUpgradeChoice(option);
        }

        ZombieStormPassiveType[] fallbackPassives =
        {
            ZombieStormPassiveType.Damage,
            ZombieStormPassiveType.FireRate,
            ZombieStormPassiveType.Area,
            ZombieStormPassiveType.MoveSpeed,
            ZombieStormPassiveType.MaxHealth,
            ZombieStormPassiveType.Crit
        };
        for (int i = 0; currentChoices.Count < 3 && i < fallbackPassives.Length; i++)
        {
            AddFallbackPassive(fallbackPassives[i]);
        }
    }

    // Adds one upgrade option to the current choice list.
    private void AddUpgradeChoice(ZombieStormUpgradeOption option)
    {
        if (option != null && choiceKeys.Add(option.Key))
        {
            currentChoices.Add(option);
        }
    }

    // Creates a random valid upgrade option from skills, specializations, or passives.
    private ZombieStormUpgradeOption CreateRandomUpgradeOption()
    {
        if (Skills == null)
        {
            return null;
        }

        if (Skills.KnownSkillCount > 0 && UnityEngine.Random.value < 0.62f)
        {
            return CreateKnownSkillOption();
        }

        if (UnityEngine.Random.value < 0.46f)
        {
            return CreateUnlockSkillOption();
        }

        ZombieStormPassiveType passive = (ZombieStormPassiveType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ZombieStormPassiveType)).Length);
        return CreatePassiveOption(passive);
    }

    // Creates an upgrade option for a skill the player already knows.
    private ZombieStormUpgradeOption CreateKnownSkillOption()
    {
        for (int guard = 0; guard < 32; guard++)
        {
            ZombieStormSkillType weaponType;
            if (!TryGetRandomKnownSkill(out weaponType))
            {
                return null;
            }

            if (UnityEngine.Random.value < 0.72f)
            {
                ZombieStormUpgradeOption specialization = CreateSkillSpecializationOption(weaponType);
                if (specialization != null)
                {
                    return specialization;
                }
            }

            ZombieStormUpgradeOption levelOption = CreateSkillLevelOption(weaponType);
            if (levelOption != null)
            {
                return levelOption;
            }

            ZombieStormUpgradeOption fallbackSpecialization = CreateSkillSpecializationOption(weaponType);
            if (fallbackSpecialization != null)
            {
                return fallbackSpecialization;
            }
        }

        return null;
    }

    // Selects a known skill that can still be leveled or specialized.
    private bool TryGetRandomKnownSkill(out ZombieStormSkillType weaponType)
    {
        Array values = Enum.GetValues(typeof(ZombieStormSkillType));
        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            ZombieStormSkillType candidate = (ZombieStormSkillType)values.GetValue(i);
            if (CanOfferSkill(candidate) && IsSkillKnown(candidate))
            {
                count++;
            }
        }

        if (count == 0)
        {
            weaponType = ZombieStormSkillType.MagicBolt;
            return false;
        }

        int pick = UnityEngine.Random.Range(0, count);
        for (int i = 0; i < values.Length; i++)
        {
            ZombieStormSkillType candidate = (ZombieStormSkillType)values.GetValue(i);
            if (!CanOfferSkill(candidate) || !IsSkillKnown(candidate))
            {
                continue;
            }

            if (pick == 0)
            {
                weaponType = candidate;
                return true;
            }

            pick--;
        }

        weaponType = ZombieStormSkillType.MagicBolt;
        return false;
    }

    // Creates an upgrade option that unlocks a new skill.
    private ZombieStormUpgradeOption CreateUnlockSkillOption()
    {
        Array values = Enum.GetValues(typeof(ZombieStormSkillType));
        for (int guard = 0; guard < 24; guard++)
        {
            ZombieStormSkillType weaponType = (ZombieStormSkillType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            if (CanOfferSkill(weaponType) && !IsSkillKnown(weaponType))
            {
                return ZombieStormUpgradeOption.Custom("unlock_" + weaponType, SkillName(weaponType) + " Lv.1", SkillSummary(weaponType), "NEW ACTIVE SKILL", SkillAccent(weaponType), delegate { Skills.LearnSkill(weaponType); });
            }
        }

        return null;
    }

    // Creates an upgrade option that raises a skill level.
    private ZombieStormUpgradeOption CreateSkillLevelOption(ZombieStormSkillType weaponType)
    {
        int level = Skills.GetSkillLevel(weaponType);
        if (!CanOfferSkill(weaponType) || !IsSkillKnown(weaponType) || level >= SkillMaxLevel(weaponType))
        {
            return null;
        }

        return ZombieStormUpgradeOption.Skill("level_" + weaponType, SkillName(weaponType) + " Lv." + (level + 1), SkillLevelSummary(weaponType, level + 1), SkillAccent(weaponType), delegate { Skills.LevelUpSkill(weaponType); });
    }

    // Creates an upgrade option that improves one skill specialization.
    private ZombieStormUpgradeOption CreateSkillSpecializationOption(ZombieStormSkillType weaponType)
    {
        int skillLevel = Skills.GetSkillLevel(weaponType);
        if (!CanOfferSkill(weaponType) || !IsSkillKnown(weaponType) || skillLevel >= SkillMaxLevel(weaponType))
        {
            return null;
        }

        string[] keys = SkillUpgradeKeys(weaponType);
        if (keys.Length == 0)
        {
            return null;
        }

        for (int guard = 0; guard < 18; guard++)
        {
            string key = keys[UnityEngine.Random.Range(0, keys.Length)];
            if (Skills.GetSkillUpgradeLevel(key) >= 3)
            {
                continue;
            }

            int nextLevel = skillLevel + 1;
            string category = SkillName(weaponType).ToUpperInvariant() + " BUILD";
            return ZombieStormUpgradeOption.Custom("special_" + weaponType + "_" + key, SkillUpgradeName(key) + " Lv." + nextLevel, SkillUpgradeSummary(key, nextLevel), category, SkillAccent(weaponType), delegate { Skills.LevelUpSkill(weaponType); Skills.AddSkillUpgrade(key); });
        }

        return null;
    }

    // Checks whether the player has already learned the selected skill.
    private bool IsSkillKnown(ZombieStormSkillType weaponType)
    {
        return Skills != null && Skills.GetSkillLevel(weaponType) > 0;
    }

    // Checks whether a skill is currently allowed to appear as an upgrade card.
    private static bool CanOfferSkill(ZombieStormSkillType weaponType)
    {
        return weaponType != ZombieStormSkillType.ShieldBurst;
    }

    // Returns the highest level allowed for a skill.
    private static int SkillMaxLevel(ZombieStormSkillType weaponType)
    {
        return weaponType == ZombieStormSkillType.Regeneration ? 3 : weaponType == ZombieStormSkillType.FireZone ? 4 : 5;
    }

    // Creates an upgrade option for a passive stat.
    private ZombieStormUpgradeOption CreatePassiveOption(ZombieStormPassiveType passive)
    {
        int level = GetPassiveLevel(passive);
        if (level >= 5)
        {
            return null;
        }

        return ZombieStormUpgradeOption.Passive("passive_" + passive, PassiveName(passive) + " Lv." + (level + 1), PassiveSummary(passive, level + 1), PassiveAccent(passive), delegate { AddPassive(passive); });
    }

    // Adds a passive option when there are not enough other upgrade choices.
    private void AddFallbackPassive(ZombieStormPassiveType passive)
    {
        ZombieStormUpgradeOption option = CreatePassiveOption(passive);
        if (option != null && choiceKeys.Add(option.Key))
        {
            currentChoices.Add(option);
        }
    }

    // Raises a passive level and immediately applies its gameplay effect.
    private void AddPassive(ZombieStormPassiveType passive)
    {
        passives[passive] = Mathf.Min(5, GetPassiveLevel(passive) + 1);
        if (passive == ZombieStormPassiveType.MaxHealth && Player != null)
        {
            Player.IncreaseMaxHealth(16f);
        }

        CheckEvolutions();
    }

    // Applies the selected upgrade card and resumes the run.
    private void ApplyUpgrade(int index)
    {
        if (index < 0 || index >= currentChoices.Count)
        {
            return;
        }

        ZombieStormUpgradeOption option = currentChoices[index];
        option.Apply();
        CheckEvolutions();
        currentChoices.Clear();
        leveling = false;
        flowState = ZombieStormFlowState.Running;
        Time.timeScale = 1f;
        PlaySfx("upgrade", 0.9f, 0.1f);
        PlayUpgradeBurst(option);
        ShowFeedback(option.Title + " acquired.", 2.2f);
    }

    // Plays visual and audio feedback after an upgrade is selected.
    private void PlayUpgradeBurst(ZombieStormUpgradeOption option)
    {
        if (Player == null)
        {
            return;
        }

        Color accent = option != null ? option.Accent : new Color(0.4f, 0.9f, 1f, 1f);
        Vector2 center = Player.transform.position;
        SpawnAreaEffect(center, 1.35f, 0f, 0.28f, 1f, WithAlpha(accent, 0.42f), "upgrade_pulse");
        SpawnAreaEffect(center, 2.15f, 0f, 0.42f, 1f, WithAlpha(new Color(1f, 0.88f, 0.28f), 0.32f), "upgrade_ring");
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * UnityEngine.Random.Range(0.75f, 1.9f);
            SpawnHitSpark(center + offset, WithAlpha(accent, 0.82f), UnityEngine.Random.Range(0.16f, 0.28f));
        }

        ShakeCamera(0.1f, 0.18f);
        FlashScreen(accent, 0.46f);
    }

    // Checks whether skill and passive combinations can evolve.
    private void CheckEvolutions()
    {
        if (Skills == null)
        {
            return;
        }

        TryEvolve(ZombieStormSkillType.MagicBolt, ZombieStormPassiveType.FireRate, "Arcane Barrage evolved.");
        TryEvolve(ZombieStormSkillType.OrbitingKnife, ZombieStormPassiveType.MaxHealth, "Fire Blade Halo evolved.");
        TryEvolve(ZombieStormSkillType.SummonDrone, ZombieStormPassiveType.Damage, "Fire Spirit evolved.");
    }

    // Evolves a skill when its required passive and level conditions are met.
    private void TryEvolve(ZombieStormSkillType weapon, ZombieStormPassiveType passive, string message)
    {
        if (Skills.GetSkillLevel(weapon) >= 5 && GetPassiveLevel(passive) > 0 && !Skills.IsEvolved(weapon))
        {
            Skills.Evolve(weapon);
            ShowFeedback(message, 3f);
        }
    }

    // Handles number-key shortcuts for picking upgrade cards.
    private void HandleUpgradeHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ApplyUpgrade(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ApplyUpgrade(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ApplyUpgrade(2);
        }
    }

    // Draws the fallback immediate-mode main menu.
    private void DrawMainMenu()
    {
        DrawOverlayBackdrop(0.48f);
        Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 190f, 520f, 360f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.94f), new Color(0.2f, 0.75f, 1f, 0.58f));

        GUI.color = new Color(1f, 0.92f, 0.36f, 1f);
        GUI.skin.label.fontSize = 34;
        GUI.Label(new Rect(panel.x + 34f, panel.y + 28f, panel.width - 68f, 46f), Title);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 20;
        GUI.Label(new Rect(panel.x + 36f, panel.y + 76f, panel.width - 72f, 30f), "Zombie Storm");
        GUI.skin.label.fontSize = 15;
        GUI.color = new Color(0.78f, 0.86f, 0.92f, 1f);
        GUI.Label(new Rect(panel.x + 36f, panel.y + 116f, panel.width - 72f, 70f), "Survive five minutes, grow a coherent build, and break the city horde.");
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 198f, 300f, 38f), "Start Run"))
        {
            StartRun();
        }

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 248f, 300f, 34f), "Settings"))
        {
            OpenSettings(ZombieStormFlowState.MainMenu);
        }

        GUI.skin.label.fontSize = 13;
        GUI.color = new Color(0.68f, 0.76f, 0.84f, 1f);
        GUI.Label(new Rect(panel.x + 118f, panel.y + 302f, 300f, 24f), "Enter also starts a run");
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 18;
    }

    // Draws the pause panel and its buttons.
    private void DrawPausePanel()
    {
        DrawOverlayBackdrop(0.68f);
        Rect panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 160f, 440f, 300f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.96f), new Color(1f, 0.78f, 0.22f, 0.58f));

        GUI.skin.label.fontSize = 34;
        GUI.color = new Color(1f, 0.86f, 0.26f, 1f);
        GUI.Label(new Rect(panel.x + 118f, panel.y + 28f, 260f, 46f), "PAUSED");
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 96f, 260f, 36f), "Resume"))
        {
            ResumeRun();
        }

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 142f, 260f, 36f), "Settings"))
        {
            OpenSettings(ZombieStormFlowState.Paused);
        }

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 188f, 260f, 36f), "Restart Run"))
        {
            StartRun();
        }

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 234f, 260f, 34f), "Main Menu"))
        {
            ReturnToMainMenu();
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws settings for volume, frame rate, and fullscreen mode.
    private void DrawSettingsPanel()
    {
        DrawOverlayBackdrop(settingsReturnState == ZombieStormFlowState.MainMenu ? 0.52f : 0.72f);
        Rect panel = new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.5f - 170f, 480f, 330f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.97f), new Color(0.2f, 0.75f, 1f, 0.58f));

        GUI.skin.label.fontSize = 30;
        GUI.color = new Color(0.74f, 0.9f, 1f, 1f);
        GUI.Label(new Rect(panel.x + 36f, panel.y + 28f, 260f, 38f), "SETTINGS");
        GUI.color = Color.white;

        GUI.skin.label.fontSize = 16;
        GUI.Label(new Rect(panel.x + 42f, panel.y + 86f, 180f, 24f), "SFX Volume");
        masterVolume = GUI.HorizontalSlider(new Rect(panel.x + 170f, panel.y + 92f, 220f, 20f), masterVolume, 0f, 1f);
        GUI.Label(new Rect(panel.x + 402f, panel.y + 86f, 48f, 24f), Mathf.RoundToInt(masterVolume * 100f).ToString() + "%");

        sfxMuted = GUI.Toggle(new Rect(panel.x + 42f, panel.y + 124f, 180f, 24f), sfxMuted, "Mute SFX");

        GUI.Label(new Rect(panel.x + 42f, panel.y + 170f, 160f, 24f), "Frame Rate");
        if (GUI.Button(new Rect(panel.x + 170f, panel.y + 166f, 70f, 30f), "60"))
        {
            SetTargetFrameRate(60);
        }

        if (GUI.Button(new Rect(panel.x + 252f, panel.y + 166f, 70f, 30f), "120"))
        {
            SetTargetFrameRate(120);
        }

        if (GUI.Button(new Rect(panel.x + 334f, panel.y + 166f, 70f, 30f), "144"))
        {
            SetTargetFrameRate(144);
        }

        GUI.color = new Color(0.72f, 0.8f, 0.88f, 1f);
        GUI.Label(new Rect(panel.x + 170f, panel.y + 204f, 220f, 24f), "Current: " + targetFrameRate + " FPS");
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 258f, 260f, 36f), "Back"))
        {
            CloseSettings();
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws the end-of-run summary panel.
    private void DrawResultsPanel()
    {
        DrawOverlayBackdrop(0.74f);
        Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 178f, 520f, 340f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.97f), won ? new Color(0.2f, 0.9f, 0.72f, 0.58f) : new Color(1f, 0.18f, 0.12f, 0.58f));

        GUI.color = won ? new Color(0.46f, 1f, 0.78f, 1f) : new Color(1f, 0.32f, 0.24f, 1f);
        GUI.skin.label.fontSize = 34;
        GUI.Label(new Rect(panel.x + 52f, panel.y + 26f, panel.width - 90f, 46f), won ? "SURVIVAL VICTORY" : "RUN FAILED");
        GUI.color = Color.white;

        int kills = Player != null ? Player.Kills : 0;
        int coins = Player != null ? Player.Coins : 0;
        int level = Player != null ? Player.Level : 1;
        GUI.skin.label.fontSize = 20;
        GUI.Label(new Rect(panel.x + 74f, panel.y + 96f, 390f, 34f), "Kills " + kills + "     Coins " + coins + "     Level " + level);
        GUI.skin.label.fontSize = 16;
        GUI.color = new Color(0.78f, 0.86f, 0.92f, 1f);
        GUI.Label(new Rect(panel.x + 74f, panel.y + 136f, 390f, 46f), feedbackText);
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 204f, 300f, 36f), "Restart Run"))
        {
            StartRun();
        }

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 252f, 300f, 34f), "Main Menu"))
        {
            ReturnToMainMenu();
        }

        GUI.skin.label.fontSize = 13;
        GUI.color = new Color(0.68f, 0.76f, 0.84f, 1f);
        GUI.Label(new Rect(panel.x + 142f, panel.y + 296f, 260f, 24f), "Enter restarts | Esc returns");
        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws a transparent backdrop behind modal panels.
    private void DrawOverlayBackdrop(float alpha)
    {
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Applies the target frame rate, including the unlimited-frame option.
    private void SetTargetFrameRate(int frameRate)
    {
        targetFrameRate = frameRate;
        Application.targetFrameRate = targetFrameRate;
    }

    // Loads saved menu settings and applies them to the game.
    private void LoadMenuSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("ZombieStorm.MasterVolume", masterVolume);
        musicVolume = PlayerPrefs.GetFloat("ZombieStorm.MusicVolume", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat("ZombieStorm.SfxVolume", sfxVolume);
        sfxMuted = sfxVolume <= 0.001f;
        Screen.fullScreen = PlayerPrefs.GetInt("ZombieStorm.Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
    }

    // Creates the main menu UI and binds its buttons.
    private void SetupMainMenuUI()
    {
        if (mainMenuUI == null)
        {
            mainMenuUI = GetComponent<ZombieStormMainMenuUI>();
            if (mainMenuUI == null)
            {
                mainMenuUI = gameObject.AddComponent<ZombieStormMainMenuUI>();
            }
        }

        mainMenuUI.Initialize(this, mainMenuCoverSprite != null ? mainMenuCoverSprite : customArenaMapSprite);
    }

    // Quits the game, or exits play mode when running inside the Unity editor.
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Draws the level-up upgrade selection panel.
    private void DrawUpgradePanel()
    {
        DrawOverlayBackdrop(0.88f);

        bool narrow = Screen.width < 780f || Screen.height < 620f;
        float sidePadding = narrow ? 24f : 56f;
        float panelWidth = Mathf.Min(Mathf.Max(320f, Screen.width - sidePadding), narrow ? 640f : 1040f);
        float panelHeight = narrow ? Mathf.Min(Screen.height - 28f, 500f) : 500f;
        Rect panel = new Rect(Screen.width * 0.5f - panelWidth * 0.5f, Screen.height * 0.5f - panelHeight * 0.5f, panelWidth, panelHeight);
        Color headerAccent = currentChoices.Count > 0 ? currentChoices[0].Accent : new Color(0.3f, 0.86f, 1f, 1f);

        GUI.color = new Color(0.02f, 0.05f, 0.06f, 0.5f);
        GUI.DrawTexture(new Rect(0f, panel.y + 78f, Screen.width, panel.height - 156f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(headerAccent, 0.12f);
        GUI.DrawTexture(new Rect(panel.x - 18f, panel.y - 18f, panel.width + 36f, panel.height + 36f), Texture2D.whiteTexture);
        GUI.color = new Color(0f, 0f, 0f, 0.46f);
        GUI.DrawTexture(new Rect(panel.x + 10f, panel.y + 12f, panel.width, panel.height), Texture2D.whiteTexture);

        DrawPanel(panel, new Color(0.014f, 0.019f, 0.027f, 0.98f), WithAlpha(headerAccent, 0.62f));
        GUI.color = WithAlpha(headerAccent, 0.9f);
        GUI.DrawTexture(new Rect(panel.x + 2f, panel.y + 2f, panel.width - 4f, 4f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(headerAccent, 0.18f);
        GUI.DrawTexture(new Rect(panel.x + 2f, panel.y + 6f, panel.width - 4f, narrow ? 82f : 96f), Texture2D.whiteTexture);

        DrawUpgradeHeader(panel, headerAccent, narrow);

        int choiceCount = Mathf.Max(1, currentChoices.Count);
        float gap = narrow ? 10f : 20f;
        float startX = narrow ? panel.x + 20f : panel.x + 42f;
        float startY = panel.y + (narrow ? 126f : 142f);
        float footerHeight = narrow ? 44f : 52f;
        float cardWidth = narrow ? panel.width - 40f : (panel.width - 84f - gap * 2f) / 3f;
        float availableCardHeight = panel.yMax - footerHeight - 18f - startY;
        float cardHeight = narrow ? Mathf.Clamp((availableCardHeight - gap * (choiceCount - 1)) / choiceCount, 84f, 102f) : availableCardHeight;

        for (int i = 0; i < currentChoices.Count; i++)
        {
            Rect rect = narrow
                ? new Rect(startX, startY + i * (cardHeight + gap), cardWidth, cardHeight)
                : new Rect(startX + i * (cardWidth + gap), startY, cardWidth, cardHeight);
            DrawUpgradeCard(rect, currentChoices[i], i, narrow);
        }

        DrawUpgradeFooter(new Rect(panel.x + 20f, panel.yMax - footerHeight - 12f, panel.width - 40f, footerHeight), headerAccent, narrow);

        GUI.skin.label.fontSize = 18;
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUI.skin.label.wordWrap = false;
        GUI.color = Color.white;
    }

    // Draws one upgrade card with icon, title, description, and pick button.
    private void DrawUpgradeCard(Rect rect, ZombieStormUpgradeOption option, int index, bool compact)
    {
        Event currentEvent = Event.current;
        bool hover = rect.Contains(currentEvent.mousePosition);
        Color accent = option.Accent;
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.8f + index * 0.7f) * 0.5f;
        Color edge = WithAlpha(accent, hover ? 1f : 0.62f);
        Color fill = hover ? new Color(0.046f, 0.058f, 0.074f, 0.99f) : new Color(0.027f, 0.035f, 0.05f, 0.98f);
        Rect drawRect = hover && !compact ? new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f) : rect;

        GUI.color = new Color(0f, 0f, 0f, 0.48f);
        GUI.DrawTexture(new Rect(rect.x + 7f, rect.y + 9f, rect.width, rect.height), Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, hover ? 0.2f : 0.08f);
        GUI.DrawTexture(new Rect(drawRect.x - 5f, drawRect.y - 5f, drawRect.width + 10f, drawRect.height + 10f), Texture2D.whiteTexture);
        DrawPanel(drawRect, fill, edge);

        GUI.color = WithAlpha(accent, hover ? 0.3f : 0.16f);
        GUI.DrawTexture(new Rect(drawRect.x + 2f, drawRect.y + 2f, drawRect.width - 4f, compact ? 40f : 84f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, hover ? 1f : 0.76f);
        GUI.DrawTexture(new Rect(drawRect.x + 2f, drawRect.y + 2f, 5f, drawRect.height - 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(drawRect.x + 14f, drawRect.y + 16f, compact ? 54f : 78f, 2f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, hover ? 0.42f + pulse * 0.2f : 0.26f);
        GUI.DrawTexture(new Rect(drawRect.x + drawRect.width - 44f, drawRect.y + 12f, 22f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(drawRect.x + drawRect.width - 24f, drawRect.y + 12f, 2f, 22f), Texture2D.whiteTexture);

        Rect hotkey = compact ? new Rect(rect.x + 14f, rect.y + 12f, 34f, 34f) : new Rect(rect.x + 18f, rect.y + 20f, 44f, 44f);
        DrawUpgradeHotkey(hotkey, index + 1, accent, hover);

        Rect icon = compact ? new Rect(rect.x + rect.width - 54f, rect.y + 12f, 34f, 34f) : new Rect(rect.x + rect.width * 0.5f - 35f, rect.y + 26f, 70f, 70f);
        DrawUpgradeIcon(icon, option, accent, hover);

        float textX = compact ? rect.x + 58f : rect.x + 20f;
        float textWidth = compact ? rect.width - 122f : rect.width - 40f;
        float titleY = compact ? rect.y + 10f : rect.y + 112f;

        GUI.skin.label.wordWrap = true;
        GUI.skin.label.fontSize = compact ? (option.Title.Length > 24 ? 14 : 16) : (option.Title.Length > 24 ? 17 : 20);
        GUI.color = Color.white;
        GUI.Label(new Rect(textX, titleY, textWidth, compact ? 24f : 50f), option.Title);

        GUI.skin.label.fontSize = 11;
        GUI.color = accent;
        GUI.Label(new Rect(textX, compact ? rect.y + 36f : rect.y + 162f, textWidth, 18f), GetUpgradeKindLabel(option));

        GUI.skin.label.fontSize = compact ? 13 : 14;
        GUI.color = new Color(0.76f, 0.84f, 0.91f, 1f);
        GUI.Label(new Rect(textX, compact ? rect.y + 54f : rect.y + 188f, textWidth, compact ? 34f : 60f), option.Description);

        if (!compact)
        {
            DrawUpgradeEnergyTicks(new Rect(rect.x + 24f, rect.yMax - 66f, rect.width - 48f, 10f), accent, index, hover);
        }

        if (!compact)
        {
            Rect button = new Rect(rect.x + 26f, rect.yMax - 44f, rect.width - 52f, 30f);
            DrawUpgradePickButton(button, index + 1, accent, hover);
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition))
        {
            currentEvent.Use();
            ApplyUpgrade(index);
        }

        GUI.skin.label.wordWrap = false;
        GUI.color = Color.white;
    }

    // Draws the title area at the top of the upgrade panel.
    private void DrawUpgradeHeader(Rect panel, Color accent, bool compact)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.22f);
        GUI.DrawTexture(new Rect(panel.x + 16f, panel.y + 18f, panel.width - 32f, compact ? 84f : 94f), Texture2D.whiteTexture);

        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUI.skin.label.wordWrap = false;
        GUI.color = WithAlpha(accent, 0.9f);
        GUI.skin.label.fontSize = compact ? 11 : 12;
        GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, 240f, 20f), "SURVIVOR UPGRADE");

        GUI.color = new Color(1f, 0.88f, 0.32f, 1f);
        GUI.skin.label.fontSize = compact ? 28 : 36;
        bool showStatBlock = !compact || panel.width >= 560f;
        float titleWidth = showStatBlock ? panel.width * 0.55f : panel.width - 56f;
        GUI.Label(new Rect(panel.x + 28f, panel.y + 40f, titleWidth, 48f), "LEVEL UP");

        GUI.color = new Color(0.76f, 0.86f, 0.94f, 1f);
        GUI.skin.label.fontSize = compact ? 12 : 14;
        float promptWidth = showStatBlock ? panel.width * 0.62f : panel.width - 60f;
        GUI.Label(new Rect(panel.x + 30f, panel.y + (compact ? 76f : 88f), promptWidth, 24f), "Choose one upgrade with 1 / 2 / 3 or click a card.");

        if (showStatBlock)
        {
            string levelText = Player != null ? "RUN LV " + Player.Level : "RUN LV --";
            string coinText = Player != null ? Player.Coins + " COINS" : "-- COINS";
            Rect statRect = new Rect(panel.xMax - (compact ? 178f : 240f), panel.y + 34f, compact ? 150f : 210f, compact ? 54f : 62f);
            DrawPanel(statRect, new Color(0.012f, 0.018f, 0.026f, 0.72f), WithAlpha(accent, 0.45f));
            GUI.color = WithAlpha(accent, 0.95f);
            GUI.skin.label.fontSize = compact ? 18 : 24;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(statRect.x, statRect.y + 4f, statRect.width, statRect.height * 0.5f), levelText);
            GUI.color = new Color(0.76f, 0.84f, 0.9f, 1f);
            GUI.skin.label.fontSize = compact ? 10 : 12;
            GUI.Label(new Rect(statRect.x, statRect.y + statRect.height * 0.5f, statRect.width, 22f), coinText);
        }

        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws the number-key hint on an upgrade card.
    private void DrawUpgradeHotkey(Rect rect, int number, Color accent, bool hover)
    {
        GUI.color = WithAlpha(accent, hover ? 0.92f : 0.68f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.015f, 0.022f, 0.03f, 0.92f);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = Mathf.RoundToInt(rect.height * 0.48f);
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(rect, number.ToString());
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws the colored icon area for an upgrade card.
    private void DrawUpgradeIcon(Rect rect, ZombieStormUpgradeOption option, Color accent, bool hover)
    {
        GUI.color = WithAlpha(accent, hover ? 0.22f : 0.14f);
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), Texture2D.whiteTexture);
        GUI.color = new Color(0.011f, 0.018f, 0.026f, 0.94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, 0.95f);
        GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + 4f, rect.yMax - 7f, rect.width - 8f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + 7f, 3f, rect.height - 14f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 7f, rect.y + 7f, 3f, rect.height - 14f), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.skin.label.fontSize = Mathf.RoundToInt(rect.height * 0.42f);
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(rect, GetUpgradeIconText(option));
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws the pick button at the bottom of an upgrade card.
    private void DrawUpgradePickButton(Rect rect, int number, Color accent, bool hover)
    {
        GUI.color = WithAlpha(accent, hover ? 0.92f : 0.58f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = hover ? new Color(0.03f, 0.045f, 0.06f, 0.88f) : new Color(0.018f, 0.026f, 0.036f, 0.88f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, hover ? 0.88f : 0.42f);
        GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 7f, 18f, rect.height - 14f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 13;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(rect, "SELECT " + number);
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws decorative energy ticks on an upgrade card.
    private void DrawUpgradeEnergyTicks(Rect rect, Color accent, int index, bool hover)
    {
        int tickCount = 7;
        float gap = 5f;
        float tickWidth = (rect.width - gap * (tickCount - 1)) / tickCount;
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 5.4f + index) * 0.5f;

        for (int i = 0; i < tickCount; i++)
        {
            float alpha = hover ? 0.5f + pulse * 0.4f : 0.22f + i * 0.045f;
            GUI.color = WithAlpha(accent, Mathf.Clamp01(alpha));
            GUI.DrawTexture(new Rect(rect.x + i * (tickWidth + gap), rect.y, tickWidth, rect.height), Texture2D.whiteTexture);
        }
    }

    // Draws the footer hint and decoration for the upgrade panel.
    private void DrawUpgradeFooter(Rect rect, Color accent, bool compact)
    {
        DrawPanel(rect, new Color(0.012f, 0.017f, 0.024f, 0.86f), WithAlpha(accent, 0.28f));
        GUI.color = WithAlpha(accent, 0.7f);
        GUI.DrawTexture(new Rect(rect.x + 10f, rect.y + 9f, 3f, rect.height - 18f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 13f, rect.y + 9f, 3f, rect.height - 18f), Texture2D.whiteTexture);

        GUI.skin.label.wordWrap = false;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.skin.label.fontSize = compact ? 11 : 13;
        GUI.color = new Color(0.78f, 0.86f, 0.92f, 1f);
        GUI.Label(rect, compact ? "1 / 2 / 3  SELECT" : "BUILD CHOICE LOCKS IN IMMEDIATELY    |    1 / 2 / 3 SELECT");
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Returns the category label shown on an upgrade card.
    private static string GetUpgradeKindLabel(ZombieStormUpgradeOption option)
    {
        if (option.Key != null && option.Key.StartsWith("unlock_", StringComparison.Ordinal))
        {
            return "NEW ACTIVE SKILL";
        }

        if (option.Key != null && option.Key.StartsWith("level_", StringComparison.Ordinal))
        {
            return "SKILL LEVEL UP";
        }

        if (option.Key != null && option.Key.StartsWith("passive_", StringComparison.Ordinal))
        {
            return "PASSIVE STAT";
        }

        return option.Category;
    }

    // Returns the short icon text for an upgrade option.
    private static string GetUpgradeIconText(ZombieStormUpgradeOption option)
    {
        string key = option.Key ?? string.Empty;
        if (key.Contains("MagicBolt") || key.Contains("magic_"))
        {
            return "MB";
        }

        if (key.Contains("OrbitingKnife") || key.Contains("knife_"))
        {
            return "FB";
        }

        if (key.Contains("Regeneration") || key.Contains("regen_"))
        {
            return "RG";
        }

        if (key.Contains("FireZone") || key.Contains("fire_"))
        {
            return "FZ";
        }

        if (key.Contains("SummonDrone") || key.Contains("drone_"))
        {
            return "FS";
        }

        if (key.Contains("ShieldBurst") || key.Contains("shield_"))
        {
            return "SH";
        }

        if (key.Contains("UltimateStorm") || key.Contains("ultimate_"))
        {
            return "UL";
        }

        if (key.Contains("Damage"))
        {
            return "AT";
        }

        if (key.Contains("FireRate"))
        {
            return "AS";
        }

        if (key.Contains("MoveSpeed"))
        {
            return "MS";
        }

        if (key.Contains("PickupRange"))
        {
            return "XP";
        }

        if (key.Contains("MaxHealth"))
        {
            return "HP";
        }

        if (key.Contains("CoinGain"))
        {
            return "CN";
        }

        if (key.Contains("Crit"))
        {
            return "CR";
        }

        if (key.Contains("Area"))
        {
            return "AR";
        }

        return "UP";
    }

    // Draws a labeled progress bar such as health or experience.
    private void DrawBar(Rect rect, float value, Color color, string label)
    {
        value = Mathf.Clamp01(value);
        GUI.color = new Color(0.01f, 0.012f, 0.016f, 0.82f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.16f);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f) * value, rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 8f, rect.y - 2f, rect.width, rect.height + 6f), label);
    }

    // Draws a screen overlay that adds combat atmosphere.
    private void DrawAtmosphereOverlay()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.18f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 34f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, Screen.height - 42f, Screen.width, 42f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, 0f, 34f, Screen.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - 34f, 0f, 34f, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Draws screen flash feedback for damage and impacts.
    private void DrawScreenFlash()
    {
        if (screenFlash <= 0f)
        {
            return;
        }

        GUI.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, Mathf.Clamp01(screenFlash) * 0.24f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Draws floating damage numbers above enemies.
    private void DrawDamagePopups()
    {
        if (mainCamera == null)
        {
            return;
        }

        for (int i = 0; i < damagePopups.Count; i++)
        {
            ZombieStormDamagePopup popup = damagePopups[i];
            Vector3 screen = mainCamera.WorldToScreenPoint(popup.WorldPosition);
            if (screen.z < 0f)
            {
                continue;
            }

            GUI.skin.label.fontSize = popup.Size;
            Color color = popup.Color;
            color.a = Mathf.Clamp01(popup.TimeLeft * 2f);
            GUI.color = Color.black;
            GUI.Label(new Rect(screen.x - 17f, Screen.height - screen.y - 17f, 90f, 30f), popup.Text);
            GUI.color = color;
            GUI.Label(new Rect(screen.x - 18f, Screen.height - screen.y - 18f, 90f, 30f), popup.Text);
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws the boss name and health bar.
    private void DrawBossBar()
    {
        ZombieStormEnemy boss = null;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].IsBoss && !enemies[i].IsDead)
            {
                boss = enemies[i];
                break;
            }
        }

        if (boss == null)
        {
            return;
        }

        Rect rect = new Rect(Screen.width * 0.5f - 260f, Screen.height - 58f, 520f, 24f);
        Color accent = BossUiAccent(boss.Type);
        DrawPanel(new Rect(rect.x - 10f, rect.y - 28f, rect.width + 20f, 58f), new Color(0.04f, 0.018f, 0.018f, 0.82f), WithAlpha(accent, 0.5f));
        GUI.skin.label.fontSize = 16;
        GUI.color = accent;
        GUI.Label(new Rect(rect.x, rect.y - 24f, rect.width, 22f), "BOSS " + boss.DisplayName.ToUpperInvariant());
        DrawBar(rect, boss.Health01, accent, Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaxHealth));
        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws screen markers that make elite enemies easier to spot.
    private void DrawEliteMarkers()
    {
        if (mainCamera == null)
        {
            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            ZombieStormEnemy enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.Type != ZombieStormEnemyType.Elite)
            {
                continue;
            }

            Vector3 screen = mainCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.2f);
            if (screen.z < 0f)
            {
                continue;
            }

            GUI.skin.label.fontSize = 14;
            GUI.color = new Color(1f, 0.68f, 0.18f, 0.95f);
            GUI.Label(new Rect(screen.x - 25f, Screen.height - screen.y - 10f, 80f, 24f), "ELITE");
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws a bordered UI panel background.
    private void DrawPanel(Rect rect, Color fill, Color edge)
    {
        GUI.color = fill;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = edge;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Moves the camera toward the player and keeps it aligned for 2D view.
    private void FollowPlayer(bool snap = false)
    {
        if (mainCamera == null)
        {
            return;
        }

        Transform targetTransform = Player != null ? Player.transform : FindTaggedPlayerTransform();
        if (targetTransform == null)
        {
            return;
        }

        Vector3 target = new Vector3(targetTransform.position.x, targetTransform.position.y, -10f);
        if (usingCustomArenaMap)
        {
            float cameraHalfHeight = mainCamera.orthographicSize;
            float cameraHalfWidth = cameraHalfHeight * mainCamera.aspect;
            float maxX = Mathf.Max(0f, customArenaHalfExtents.x - cameraHalfWidth);
            float maxY = Mathf.Max(0f, customArenaHalfExtents.y - cameraHalfHeight);
            target.x = Mathf.Clamp(target.x, -maxX, maxX);
            target.y = Mathf.Clamp(target.y, -maxY, maxY);
        }

        if (cameraShakeTime > 0f)
        {
            cameraShakeTime -= Time.deltaTime;
            Vector2 shake = UnityEngine.Random.insideUnitCircle * cameraShakePower;
            target += new Vector3(shake.x, shake.y, 0f);
            cameraShakePower = Mathf.Lerp(cameraShakePower, 0f, 7f * Time.deltaTime);
        }

        Vector3 nextPosition = snap ? target : Vector3.Lerp(mainCamera.transform.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
        mainCamera.transform.position = Snap2DCameraPosition(nextPosition);
    }

    // Finds the scene object tagged as Player for camera targeting.
    private Transform FindTaggedPlayerTransform()
    {
        try
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            return taggedPlayer != null ? taggedPlayer.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    // Assigns the Player tag when that tag exists in the project.
    private static void TrySetPlayerTag(GameObject playerObject)
    {
        try
        {
            playerObject.tag = "Player";
        }
        catch (UnityException)
        {
        }
    }

    // Snaps camera coordinates to values that render 2D sprites cleanly.
    private static Vector3 Snap2DCameraPosition(Vector3 position)
    {
        const float grid = 0.01f;
        position.x = Mathf.Round(position.x / grid) * grid;
        position.y = Mathf.Round(position.y / grid) * grid;
        return position;
    }

    // Updates floating damage text position, fade, and lifetime.
    private void UpdateDamagePopups()
    {
        for (int i = damagePopups.Count - 1; i >= 0; i--)
        {
            ZombieStormDamagePopup popup = damagePopups[i];
            popup.WorldPosition += popup.Velocity * Time.deltaTime;
            popup.TimeLeft -= Time.deltaTime;
            if (popup.TimeLeft <= 0f)
            {
                damagePopups.RemoveAt(i);
            }
            else
            {
                damagePopups[i] = popup;
            }
        }
    }

    // Clears enemies, projectiles, pickups, effects, and temporary objects from the run.
    private void ClearActiveObjects()
    {
        enemies.Clear();

        if (worldRoot != null)
        {
            for (int i = worldRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(worldRoot.GetChild(i).gameObject);
            }
        }

        foreach (Queue<GameObject> queue in pools.Values)
        {
            foreach (GameObject item in queue)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
        }

        pools.Clear();
        passives.Clear();
    }

    // Shows a temporary feedback message on the screen.
    private void ShowFeedback(string message, float seconds)
    {
        feedbackText = message;
        feedbackUntil = Time.unscaledTime + seconds;
    }

    // Returns the XP reward for a defeated boss type.
    private static int BossXpReward(ZombieStormEnemyType bossType)
    {
        if (bossType == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return 66;
        }

        if (bossType == ZombieStormEnemyType.MossGolemBoss)
        {
            return 72;
        }

        if (bossType == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return 98;
        }

        if (bossType == ZombieStormEnemyType.BruteBoss)
        {
            return 62;
        }

        if (bossType == ZombieStormEnemyType.PlagueBoss)
        {
            return 70;
        }

        if (bossType == ZombieStormEnemyType.StormBoss)
        {
            return 82;
        }

        return 55;
    }

    // Returns the coin reward for a defeated boss type.
    private static int BossCoinReward(ZombieStormEnemyType bossType)
    {
        if (bossType == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return 52;
        }

        if (bossType == ZombieStormEnemyType.MossGolemBoss)
        {
            return 58;
        }

        if (bossType == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return 82;
        }

        if (bossType == ZombieStormEnemyType.BruteBoss)
        {
            return 48;
        }

        if (bossType == ZombieStormEnemyType.PlagueBoss)
        {
            return 54;
        }

        if (bossType == ZombieStormEnemyType.StormBoss)
        {
            return 66;
        }

        return 45;
    }

    // Returns the warning text displayed when a boss wave begins.
    private static string BossWaveWarning(ZombieStormEnemyType bossType)
    {
        if (bossType == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return "Crystal Colossus incoming. Dodge blade sweeps and crystal volleys.";
        }

        if (bossType == ZombieStormEnemyType.MossGolemBoss)
        {
            return "Mossbound Colossus incoming. Do not linger in corrupted ground.";
        }

        if (bossType == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return "Ember Tyrant incoming. Evade the charge and falling magma.";
        }

        if (bossType == ZombieStormEnemyType.BruteBoss)
        {
            return "Ravager Brute incoming. Keep distance from charge slams.";
        }

        if (bossType == ZombieStormEnemyType.PlagueBoss)
        {
            return "Plague Matriarch incoming. Watch poison zones and volleys.";
        }

        if (bossType == ZombieStormEnemyType.StormBoss)
        {
            return "Storm Revenant incoming. Lightning tracks your position.";
        }

        return "Horde Alpha incoming. Watch the phase attacks.";
    }

    // Returns the UI accent color for a boss type.
    private static Color BossUiAccent(ZombieStormEnemyType bossType)
    {
        if (bossType == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return new Color(0.36f, 0.92f, 1f, 1f);
        }

        if (bossType == ZombieStormEnemyType.MossGolemBoss)
        {
            return new Color(0.56f, 0.82f, 0.18f, 1f);
        }

        if (bossType == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return new Color(1f, 0.34f, 0.1f, 1f);
        }

        if (bossType == ZombieStormEnemyType.BruteBoss)
        {
            return new Color(1f, 0.38f, 0.1f, 1f);
        }

        if (bossType == ZombieStormEnemyType.PlagueBoss)
        {
            return new Color(0.58f, 1f, 0.22f, 1f);
        }

        if (bossType == ZombieStormEnemyType.StormBoss)
        {
            return new Color(0.38f, 0.78f, 1f, 1f);
        }

        return new Color(0.9f, 0.08f, 0.05f, 1f);
    }

    // Formats seconds as a minutes-and-seconds timer string.
    private static string FormatTime(int seconds)
    {
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }

    // Converts a skill enum value into the display name shown in UI.
    private static string SkillName(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Magic Bolt";
            case ZombieStormSkillType.OrbitingKnife: return "Fire Blades";
            case ZombieStormSkillType.Regeneration: return "Regeneration";
            case ZombieStormSkillType.FireZone: return "Fire Zone";
            case ZombieStormSkillType.SummonDrone: return "Fire Spirit";
            case ZombieStormSkillType.ShieldBurst: return "Shield Burst";
            case ZombieStormSkillType.UltimateStorm: return "Ultimate: Full-Screen Thunder";
            default: return weapon.ToString();
        }
    }

    // Returns a short player-facing description for a skill.
    private static string SkillSummary(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Adds auto magic shots with a bright launch spark.";
            case ZombieStormSkillType.OrbitingKnife: return "Adds orbiting fire blades that burn through nearby enemies.";
            case ZombieStormSkillType.Regeneration: return "Restores 1 health every 3 seconds.";
            case ZombieStormSkillType.FireZone: return "Every 4 Magic Bolt attacks throws a fire bomb.";
            case ZombieStormSkillType.SummonDrone: return "Summons 1 Fire Spirit that circles you and shoots fireballs.";
            case ZombieStormSkillType.ShieldBurst: return "Adds a close-range defensive shockwave trigger.";
            case ZombieStormSkillType.UltimateStorm: return "Adds one ultimate. Press F for a full-screen storm.";
            default: return "Adds another automatic skill.";
        }
    }

    // Returns the level-up description for a skill's next level.
    private static string SkillLevelSummary(ZombieStormSkillType weapon, int nextLevel)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Lv." + nextLevel + ": faster bolts, more damage, extra pierce.";
            case ZombieStormSkillType.OrbitingKnife: return "Lv." + nextLevel + ": more fire blades, wider orbit, stronger burns.";
            case ZombieStormSkillType.Regeneration: return RegenerationLevelSummary(nextLevel);
            case ZombieStormSkillType.FireZone: return FireZoneLevelSummary(nextLevel);
            case ZombieStormSkillType.SummonDrone: return FireSpiritLevelSummary(nextLevel);
            case ZombieStormSkillType.ShieldBurst: return "Lv." + nextLevel + ": larger defensive ring and harder hit.";
            case ZombieStormSkillType.UltimateStorm: return "Lv." + nextLevel + ": stronger F ultimate, shorter cooldown.";
            default: return "Lv." + nextLevel + ": improves this automatic skill.";
        }
    }

    // Returns the custom level-up description for Regeneration.
    private static string RegenerationLevelSummary(int nextLevel)
    {
        switch (nextLevel)
        {
            case 2: return "Lv.2: max health +30 and heal 30 immediately.";
            case 3: return "Lv.3: restores 1 health every 2 seconds.";
            default: return "Lv." + nextLevel + ": improves survival recovery.";
        }
    }

    // Returns the custom level-up description for Fire Zone.
    private static string FireZoneLevelSummary(int nextLevel)
    {
        switch (nextLevel)
        {
            case 2: return "Lv.2: fire bombs leave burning flames on the ground.";
            case 3: return "Lv.3: fire bombs trigger every 3 attacks.";
            case 4: return "Lv.4: fire bombs trigger every 2 attacks.";
            default: return "Lv." + nextLevel + ": throws fire bombs after repeated attacks.";
        }
    }

    // Returns the custom level-up description for Fire Spirit.
    private static string FireSpiritLevelSummary(int nextLevel)
    {
        switch (nextLevel)
        {
            case 2: return "Lv.2: Fire Spirit count becomes 2 and attack speed increases.";
            case 3: return "Lv.3: Fire Spirit count becomes 3.";
            case 4: return "Lv.4: Fire Spirit count becomes 4.";
            default: return "Lv." + nextLevel + ": adds more Fire Spirits and increases power.";
        }
    }

    // Returns specialization keys that can appear for a selected skill.
    private static string[] SkillUpgradeKeys(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return new[] { "magic_force", "magic_split", "magic_pierce" };
            case ZombieStormSkillType.OrbitingKnife: return new[] { "knife_blades", "knife_reach", "knife_edge" };
            case ZombieStormSkillType.Regeneration: return new string[0];
            case ZombieStormSkillType.FireZone: return new string[0];
            case ZombieStormSkillType.SummonDrone: return new[] { "drone_swarm", "drone_focus", "drone_overclock" };
            case ZombieStormSkillType.ShieldBurst: return new[] { "shield_radius", "shield_force", "shield_recharge" };
            case ZombieStormSkillType.UltimateStorm: return new[] { "ultimate_voltage", "ultimate_radius", "ultimate_recharge" };
            default: return new[] { "magic_force" };
        }
    }

    // Converts a specialization key into a display name.
    private static string SkillUpgradeName(string key)
    {
        switch (key)
        {
            case "magic_force": return "Focused Mana";
            case "magic_split": return "Split Casting";
            case "magic_pierce": return "Piercing Glyph";
            case "knife_blades": return "Extra Fire Blades";
            case "knife_reach": return "Wide Flame Orbit";
            case "knife_edge": return "Searing Edge";
            case "drone_swarm": return "Spirit Swarm";
            case "drone_focus": return "Focused Flame";
            case "drone_overclock": return "Spirit Overclock";
            case "shield_radius": return "Wider Guard";
            case "shield_force": return "Repulsion Core";
            case "shield_recharge": return "Quick Recharge";
            case "ultimate_voltage": return "Storm Voltage";
            case "ultimate_radius": return "Eye of the Storm";
            case "ultimate_recharge": return "Storm Battery";
            default: return "Specialized Upgrade";
        }
    }

    // Returns the effect text for a specialization level.
    private static string SkillUpgradeSummary(string key, int nextLevel)
    {
        switch (key)
        {
            case "magic_force": return "Lv." + nextLevel + ": Magic Bolt deals more damage.";
            case "magic_split": return "Lv." + nextLevel + ": Magic Bolt can fire additional angled shots.";
            case "magic_pierce": return "Lv." + nextLevel + ": Magic Bolt pierces more enemies.";
            case "knife_blades": return "Lv." + nextLevel + ": Fire Blades adds another fire blade.";
            case "knife_reach": return "Lv." + nextLevel + ": Fire Blades orbit farther out.";
            case "knife_edge": return "Lv." + nextLevel + ": Fire Blades burn harder.";
            case "drone_swarm": return "Lv." + nextLevel + ": Fire Spirit adds another Fire Spirit.";
            case "drone_focus": return "Lv." + nextLevel + ": Fire Spirit fireballs hit harder.";
            case "drone_overclock": return "Lv." + nextLevel + ": Fire Spirit fireball cooldown is reduced.";
            case "shield_radius": return "Lv." + nextLevel + ": Shield Burst covers more space.";
            case "shield_force": return "Lv." + nextLevel + ": Shield Burst deals more damage.";
            case "shield_recharge": return "Lv." + nextLevel + ": Shield Burst cooldown is reduced.";
            case "ultimate_voltage": return "Lv." + nextLevel + ": Ultimate storm damage increases.";
            case "ultimate_radius": return "Lv." + nextLevel + ": Ultimate storm reaches farther.";
            case "ultimate_recharge": return "Lv." + nextLevel + ": Ultimate cooldown is reduced.";
            default: return "Lv." + nextLevel + ": improves this skill's behavior.";
        }
    }

    // Returns the UI accent color for a skill.
    private static Color SkillAccent(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return new Color(0.45f, 0.95f, 1f, 1f);
            case ZombieStormSkillType.OrbitingKnife: return new Color(1f, 0.22f, 0.12f, 1f);
            case ZombieStormSkillType.Regeneration: return new Color(0.42f, 1f, 0.58f, 1f);
            case ZombieStormSkillType.FireZone: return new Color(1f, 0.28f, 0.05f, 1f);
            case ZombieStormSkillType.SummonDrone: return new Color(1f, 0.42f, 0.08f, 1f);
            case ZombieStormSkillType.ShieldBurst: return new Color(0.78f, 0.98f, 1f, 1f);
            case ZombieStormSkillType.UltimateStorm: return new Color(0.95f, 0.86f, 1f, 1f);
            default: return new Color(0.5f, 0.9f, 1f, 1f);
        }
    }

    // Converts a passive enum value into the UI display name.
    private static string PassiveName(ZombieStormPassiveType passive)
    {
        switch (passive)
        {
            case ZombieStormPassiveType.Damage: return "Attack Power";
            case ZombieStormPassiveType.FireRate: return "Attack Speed";
            case ZombieStormPassiveType.Area: return "Area";
            case ZombieStormPassiveType.MoveSpeed: return "Move Speed";
            case ZombieStormPassiveType.PickupRange: return "Pickup Range";
            case ZombieStormPassiveType.Crit: return "Critical Rate";
            case ZombieStormPassiveType.MaxHealth: return "Max Health";
            case ZombieStormPassiveType.CoinGain: return "Coin Gain";
            default: return passive.ToString();
        }
    }

    // Returns the effect text for a passive upgrade level.
    private static string PassiveSummary(ZombieStormPassiveType passive, int nextLevel)
    {
        switch (passive)
        {
            case ZombieStormPassiveType.Damage: return "Lv." + nextLevel + ": all skill damage increases by 18%.";
            case ZombieStormPassiveType.FireRate: return "Lv." + nextLevel + ": all skill cooldowns become shorter.";
            case ZombieStormPassiveType.Area: return "Lv." + nextLevel + ": explosions, fire, and rings grow wider.";
            case ZombieStormPassiveType.MoveSpeed: return "Lv." + nextLevel + ": player movement speed increases.";
            case ZombieStormPassiveType.PickupRange: return "Lv." + nextLevel + ": XP and coins pull from farther away.";
            case ZombieStormPassiveType.Crit: return "Lv." + nextLevel + ": higher chance to deal double damage.";
            case ZombieStormPassiveType.MaxHealth: return "Lv." + nextLevel + ": max HP increases and heals now.";
            case ZombieStormPassiveType.CoinGain: return "Lv." + nextLevel + ": coin drops are worth more.";
            default: return "Passive power increase.";
        }
    }

    // Returns the UI accent color for a passive upgrade.
    private static Color PassiveAccent(ZombieStormPassiveType passive)
    {
        switch (passive)
        {
            case ZombieStormPassiveType.Damage: return new Color(1f, 0.32f, 0.18f, 1f);
            case ZombieStormPassiveType.FireRate: return new Color(1f, 0.78f, 0.18f, 1f);
            case ZombieStormPassiveType.Area: return new Color(0.72f, 0.52f, 1f, 1f);
            case ZombieStormPassiveType.MoveSpeed: return new Color(0.42f, 1f, 0.58f, 1f);
            case ZombieStormPassiveType.PickupRange: return new Color(0.32f, 0.86f, 1f, 1f);
            case ZombieStormPassiveType.Crit: return new Color(1f, 0.42f, 0.72f, 1f);
            case ZombieStormPassiveType.MaxHealth: return new Color(0.38f, 1f, 0.38f, 1f);
            case ZombieStormPassiveType.CoinGain: return new Color(1f, 0.9f, 0.25f, 1f);
            default: return new Color(0.78f, 0.86f, 0.92f, 1f);
        }
    }

    // Creates a scene object with a SpriteRenderer and common transform settings.
    private GameObject CreateSpriteObject(string objectName, Sprite sprite, Color color, Vector3 position, Vector3 scale, int sortingOrder)
    {
        GameObject item = new GameObject(objectName);
        item.transform.position = position;
        item.transform.localScale = scale;
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        return item;
    }

    // Adds an oval shadow under an object to improve ground depth.
    private GameObject AddShadow(Transform parent, Vector3 scale, float yOffset, int sortingOrder)
    {
        if (softShadowSprite == null)
        {
            return null;
        }

        GameObject shadow = new GameObject("Soft Ground Shadow");
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = new Vector3(0f, yOffset, 0.08f);
        shadow.transform.localScale = scale;
        SpriteRenderer renderer = shadow.AddComponent<SpriteRenderer>();
        renderer.sprite = softShadowSprite;
        renderer.color = new Color(0f, 0f, 0f, 0.42f);
        renderer.sortingOrder = sortingOrder;
        return shadow;
    }

    // Adds a soft glow child object for effects and decorations.
    private GameObject AddGlow(Transform parent, Color color, Vector3 scale, int sortingOrder)
    {
        if (softGlowSprite == null)
        {
            return null;
        }

        GameObject glow = new GameObject("Soft Glow");
        glow.transform.SetParent(parent, false);
        glow.transform.localPosition = Vector3.forward * 0.06f;
        glow.transform.localScale = scale;
        SpriteRenderer renderer = glow.AddComponent<SpriteRenderer>();
        renderer.sprite = softGlowSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return glow;
    }

    // Builds a city floor using Kenney top-down assets.
    private void BuildKenneyCityFloor()
    {
        const float tileStep = 3.2f;
        const int radius = 17;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                bool road = IsCityRoadCell(x, y);
                bool curb = !road && HasRoadNeighbor(x, y);
                bool plaza = Mathf.Abs(x) <= 2 && Mathf.Abs(y) <= 2;
                Sprite sprite = groundSprites[Mathf.Abs(x * 31 + y * 17) % groundSprites.Count];
                Color color = ChooseCityTileColor(x, y, road, curb, plaza);

                GameObject tile = CreateSpriteObject("City Floor Tile", sprite, color, new Vector3(x * tileStep, y * tileStep, 4f), Vector3.one * tileStep, -8);
                tile.transform.SetParent(worldRoot, false);
            }
        }

        AddRoadMarkings(tileStep, radius);
        AddCrosswalk(new Vector2(-8f * tileStep, 0f), true);
        AddCrosswalk(new Vector2(8f * tileStep, 0f), true);
        AddCrosswalk(new Vector2(0f, -8f * tileStep), false);
        AddCrosswalk(new Vector2(0f, 8f * tileStep), false);
        AddCrosswalk(Vector2.zero, true);
        AddCrosswalk(Vector2.zero, false);

        GameObject plazaGlow = CreateSpriteObject("Last Stand Plaza Glow", softGlowSprite, new Color(0.18f, 0.75f, 1f, 0.16f), new Vector3(0f, 0f, 2.4f), new Vector3(8.5f, 8.5f, 1f), -5);
        plazaGlow.transform.SetParent(worldRoot, false);
        GameObject plazaRing = CreateSpriteObject("Last Stand Plaza Ring", tileSprite, new Color(0.8f, 0.9f, 1f, 0.28f), new Vector3(0f, 0f, 2.2f), new Vector3(7.5f, 0.08f, 1f), -4);
        plazaRing.transform.SetParent(worldRoot, false);
        GameObject plazaRingVertical = CreateSpriteObject("Last Stand Plaza Ring", tileSprite, new Color(0.8f, 0.9f, 1f, 0.22f), new Vector3(0f, 0f, 2.2f), new Vector3(0.08f, 7.5f, 1f), -4);
        plazaRingVertical.transform.SetParent(worldRoot, false);
    }

    // Chooses a floor color based on road, curb, and plaza cell data.
    private Color ChooseCityTileColor(int x, int y, bool road, bool curb, bool plaza)
    {
        float tint = Hash01(x, y) * 0.08f - 0.04f;
        Color color;
        if (plaza)
        {
            color = new Color(0.2f, 0.27f, 0.28f, 1f);
        }
        else if (road)
        {
            color = new Color(0.11f, 0.12f, 0.125f, 1f);
        }
        else if (curb)
        {
            color = new Color(0.42f, 0.43f, 0.39f, 1f);
        }
        else
        {
            color = new Color(0.27f, 0.3f, 0.27f, 1f);
        }

        color.r = Mathf.Clamp01(color.r + tint);
        color.g = Mathf.Clamp01(color.g + tint);
        color.b = Mathf.Clamp01(color.b + tint);
        return color;
    }

    // Checks whether a map grid cell belongs to a road.
    private static bool IsCityRoadCell(int x, int y)
    {
        return Mathf.Abs(x) <= 1
            || Mathf.Abs(y) <= 1
            || Mathf.Abs(Mathf.Abs(x) - 8) <= 1
            || Mathf.Abs(Mathf.Abs(y) - 8) <= 1
            || (x == -14 && y > -12 && y < 12)
            || (y == 14 && x > -12 && x < 12);
    }

    // Checks whether nearby grid cells contain a road.
    private static bool HasRoadNeighbor(int x, int y)
    {
        return IsCityRoadCell(x + 1, y)
            || IsCityRoadCell(x - 1, y)
            || IsCityRoadCell(x, y + 1)
            || IsCityRoadCell(x, y - 1);
    }

    // Generates a stable pseudo-random value from grid coordinates.
    private static float Hash01(int x, int y)
    {
        int hash = x * 73856093 ^ y * 19349663;
        hash = (hash << 13) ^ hash;
        hash = (hash * (hash * hash * 15731 + 789221) + 1376312589) & 0x7fffffff;
        return (hash % 10000) / 10000f;
    }

    // Adds road lines and markings to the city floor.
    private void AddRoadMarkings(float tileStep, int radius)
    {
        for (int i = -radius; i <= radius; i++)
        {
            if (i % 2 == 0)
            {
                AddRoadDash(new Vector3(i * tileStep, 0f, 2f), new Vector3(1.15f, 0.08f, 1f), new Color(1f, 0.82f, 0.25f, 0.58f));
                AddRoadDash(new Vector3(0f, i * tileStep, 2f), new Vector3(0.08f, 1.15f, 1f), new Color(1f, 0.82f, 0.25f, 0.58f));
            }

            AddRoadDash(new Vector3(i * tileStep, 8f * tileStep, 2f), new Vector3(0.85f, 0.06f, 1f), new Color(0.78f, 0.86f, 0.9f, 0.26f));
            AddRoadDash(new Vector3(i * tileStep, -8f * tileStep, 2f), new Vector3(0.85f, 0.06f, 1f), new Color(0.78f, 0.86f, 0.9f, 0.22f));
            AddRoadDash(new Vector3(8f * tileStep, i * tileStep, 2f), new Vector3(0.06f, 0.85f, 1f), new Color(0.78f, 0.86f, 0.9f, 0.24f));
            AddRoadDash(new Vector3(-8f * tileStep, i * tileStep, 2f), new Vector3(0.06f, 0.85f, 1f), new Color(0.78f, 0.86f, 0.9f, 0.2f));
        }
    }

    // Creates one short dashed road marking.
    private void AddRoadDash(Vector3 position, Vector3 scale, Color color)
    {
        GameObject dash = CreateSpriteObject("Road Paint", tileSprite, color, position, scale, -6);
        dash.transform.SetParent(worldRoot, false);
    }

    // Tries to load and build the custom arena map.
    private bool BuildCustomArenaMap()
    {
        if (customArenaMapSprite == null)
        {
            return false;
        }

        const float targetWidth = 58f;
        float spriteWidth = customArenaMapSprite.bounds.size.x;
        float spriteHeight = customArenaMapSprite.bounds.size.y;
        if (spriteWidth <= 0.01f || spriteHeight <= 0.01f)
        {
            return false;
        }

        float scale = targetWidth / spriteWidth;
        float targetHeight = spriteHeight * scale;
        customArenaHalfExtents = new Vector2(targetWidth * 0.5f, targetHeight * 0.5f);
        usingCustomArenaMap = true;

        GameObject map = CreateSpriteObject("Custom Graveyard Arena", customArenaMapSprite, Color.white, new Vector3(0f, 0f, 5f), Vector3.one * scale, -10);
        map.transform.SetParent(worldRoot, false);
        mainCamera.backgroundColor = new Color(0.015f, 0.018f, 0.014f);
        BuildGraveyardArenaObstacles();
        return true;
    }

    // Creates graveyard-style obstacles from the map layout.
    private void BuildGraveyardArenaObstacles()
    {
        Vector3[] circles =
        {
            new Vector3(-27.2f, 13.5f, 1.45f),
            new Vector3(-21.8f, 13.1f, 1.18f),
            new Vector3(-15.6f, 12.8f, 1.05f),
            new Vector3(-6.2f, 14.2f, 1.12f),
            new Vector3(5.6f, 14.1f, 1.1f),
            new Vector3(15.6f, 12.9f, 1.12f),
            new Vector3(21.6f, 13.1f, 1.28f),
            new Vector3(27.1f, 13.3f, 1.45f),
            new Vector3(-27.3f, 6.6f, 1.22f),
            new Vector3(-20.6f, 6.2f, 1.05f),
            new Vector3(-13.7f, 5.4f, 0.95f),
            new Vector3(13.4f, 5.5f, 0.95f),
            new Vector3(19.6f, 6.3f, 1.18f),
            new Vector3(26.5f, 6.1f, 1.34f),
            new Vector3(-27.6f, -1.4f, 1.18f),
            new Vector3(-20.8f, -1.8f, 1.02f),
            new Vector3(20.4f, -1.7f, 1.08f),
            new Vector3(27.0f, -2.0f, 1.28f),
            new Vector3(-27.1f, -8.8f, 1.28f),
            new Vector3(-21.4f, -9.5f, 1.12f),
            new Vector3(-14.2f, -10.5f, 0.98f),
            new Vector3(13.8f, -10.1f, 1.02f),
            new Vector3(20.8f, -9.4f, 1.16f),
            new Vector3(26.8f, -8.9f, 1.36f),
            new Vector3(-27.7f, -14.1f, 1.38f),
            new Vector3(-8.2f, -14.5f, 1.02f),
            new Vector3(7.6f, -14.4f, 1.02f),
            new Vector3(27.8f, -14.0f, 1.38f)
        };

        for (int i = 0; i < circles.Length; i++)
        {
            CreateMapObstacle("Graveyard Map Obstacle " + (i + 1), new Vector2(circles[i].x, circles[i].y), circles[i].z);
        }
    }

    // Creates one map obstacle and assigns its collision radius.
    private void CreateMapObstacle(string name, Vector2 position, float radius)
    {
        GameObject obstacleObject = new GameObject(name);
        obstacleObject.transform.SetParent(worldRoot, false);
        obstacleObject.transform.position = new Vector3(position.x, position.y, 0f);
        CircleCollider2D circle = obstacleObject.AddComponent<CircleCollider2D>();
        circle.radius = radius;
        circle.isTrigger = true;
        ZombieStormObstacle obstacle = obstacleObject.AddComponent<ZombieStormObstacle>();
        obstacle.radius = radius;
        obstacle.extraPadding = 0.08f;
    }

    // Adds a crosswalk decoration at the requested position.
    private void AddCrosswalk(Vector2 center, bool horizontal)
    {
        for (int i = -3; i <= 3; i++)
        {
            Vector3 position = horizontal
                ? new Vector3(center.x + i * 0.62f, center.y, 1.9f)
                : new Vector3(center.x, center.y + i * 0.62f, 1.9f);
            Vector3 scale = horizontal ? new Vector3(0.28f, 3.6f, 1f) : new Vector3(3.6f, 0.28f, 1f);
            GameObject stripe = CreateSpriteObject("Faded Crosswalk", tileSprite, new Color(0.86f, 0.9f, 0.88f, 0.2f), position, scale, -5);
            stripe.transform.SetParent(worldRoot, false);
        }
    }

    // Builds a fallback neon floor when map art is unavailable.
    private void BuildFallbackNeonFloor()
    {
        GameObject floor = CreateSpriteObject("Neon Asphalt", tileSprite, new Color(0.06f, 0.072f, 0.075f), Vector3.forward * 4f, new Vector3(110f, 110f, 1f), -8);
        floor.transform.SetParent(worldRoot, false);

        for (int i = -14; i <= 14; i++)
        {
            GameObject lineX = CreateSpriteObject("Road Line X", tileSprite, new Color(0.05f, 0.75f, 1f, 0.16f), new Vector3(i * 4f, 0f, 2f), new Vector3(0.06f, 110f, 1f), -6);
            lineX.transform.SetParent(worldRoot, false);
            GameObject lineY = CreateSpriteObject("Road Line Y", tileSprite, new Color(1f, 0.18f, 0.45f, 0.12f), new Vector3(0f, i * 4f, 2f), new Vector3(110f, 0.06f, 1f), -6);
            lineY.transform.SetParent(worldRoot, false);
        }
    }

    // Adds city building silhouettes around the arena.
    private void BuildCityBlockSilhouettes()
    {
        AddBuildingFootprint(new Vector2(-31f, 29f), new Vector2(13f, 7f), new Color(0.055f, 0.06f, 0.065f, 0.92f), new Color(0.2f, 0.9f, 1f, 0.3f));
        AddBuildingFootprint(new Vector2(29f, 30f), new Vector2(10f, 9f), new Color(0.065f, 0.055f, 0.06f, 0.92f), new Color(1f, 0.28f, 0.48f, 0.28f));
        AddBuildingFootprint(new Vector2(-30f, -27f), new Vector2(11f, 8f), new Color(0.06f, 0.065f, 0.055f, 0.92f), new Color(1f, 0.78f, 0.2f, 0.26f));
        AddBuildingFootprint(new Vector2(31f, -28f), new Vector2(12f, 6.5f), new Color(0.045f, 0.052f, 0.06f, 0.94f), new Color(0.25f, 0.85f, 1f, 0.24f));
        AddBuildingFootprint(new Vector2(-43f, 4f), new Vector2(8f, 18f), new Color(0.052f, 0.055f, 0.06f, 0.9f), new Color(1f, 0.22f, 0.62f, 0.22f));
        AddBuildingFootprint(new Vector2(43f, -2f), new Vector2(8.5f, 19f), new Color(0.05f, 0.057f, 0.055f, 0.9f), new Color(0.2f, 0.95f, 0.72f, 0.2f));
    }

    // Adds one building footprint with body and accent colors.
    private void AddBuildingFootprint(Vector2 center, Vector2 size, Color bodyColor, Color accentColor)
    {
        GameObject shadow = CreateSpriteObject("Building Shadow", softShadowSprite, new Color(0f, 0f, 0f, 0.38f), new Vector3(center.x + 0.45f, center.y - 0.45f, 2.5f), new Vector3(size.x * 1.22f, size.y * 1.22f, 1f), -5);
        shadow.transform.SetParent(worldRoot, false);
        GameObject body = CreateSpriteObject("Burned Building Footprint", tileSprite, bodyColor, new Vector3(center.x, center.y, 1.7f), new Vector3(size.x, size.y, 1f), -3);
        body.transform.SetParent(worldRoot, false);
        GameObject rimTop = CreateSpriteObject("Building Rim", tileSprite, accentColor, new Vector3(center.x, center.y + size.y * 0.5f, 1.6f), new Vector3(size.x, 0.1f, 1f), -2);
        rimTop.transform.SetParent(worldRoot, false);
        GameObject rimSide = CreateSpriteObject("Building Rim", tileSprite, new Color(accentColor.r, accentColor.g, accentColor.b, accentColor.a * 0.65f), new Vector3(center.x - size.x * 0.5f, center.y, 1.6f), new Vector3(0.1f, size.y, 1f), -2);
        rimSide.transform.SetParent(worldRoot, false);

        for (int i = 0; i < 3; i++)
        {
            float offset = (i - 1) * size.x * 0.24f;
            GameObject window = CreateSpriteObject("Dead Window Glow", tileSprite, new Color(accentColor.r, accentColor.g, accentColor.b, accentColor.a * 0.55f), new Vector3(center.x + offset, center.y + size.y * 0.12f, 1.5f), new Vector3(size.x * 0.12f, 0.22f, 1f), -1);
            window.transform.SetParent(worldRoot, false);
        }
    }

    // Adds fog, glows, and small atmosphere details.
    private void BuildAtmosphericDetails()
    {
        for (int i = 0; i < 34; i++)
        {
            Vector2 position = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(9f, 48f);
            if (position.magnitude < 7f)
            {
                position += position.normalized * 7f;
            }

            Color puddleColor = i % 3 == 0 ? new Color(0.07f, 0.18f, 0.2f, 0.2f) : new Color(0f, 0f, 0f, 0.18f);
            GameObject puddle = CreateSpriteObject("Oil Puddle", softGlowSprite, puddleColor, new Vector3(position.x, position.y, 2.1f), new Vector3(UnityEngine.Random.Range(1.5f, 3.8f), UnityEngine.Random.Range(0.45f, 1.1f), 1f), -5);
            puddle.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            puddle.transform.SetParent(worldRoot, false);
        }

        for (int i = 0; i < 46; i++)
        {
            Vector2 position = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(12f, 50f);
            GameObject crack = CreateSpriteObject("Asphalt Crack", tileSprite, new Color(0f, 0f, 0f, 0.24f), new Vector3(position.x, position.y, 2f), new Vector3(UnityEngine.Random.Range(1.2f, 3.8f), 0.05f, 1f), -4);
            crack.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 180f));
            crack.transform.SetParent(worldRoot, false);
        }
    }

    // Adds debris so the floor does not feel empty.
    private void BuildCityDebris()
    {
        int count = debrisSprites.Count > 0 ? 150 : 120;
        for (int i = 0; i < count; i++)
        {
            Vector2 position = UnityEngine.Random.insideUnitCircle * 49f;
            if (position.magnitude < 5f)
            {
                position += position.normalized * 8f;
            }

            if (debrisSprites.Count > 0)
            {
                Sprite sprite = debrisSprites[UnityEngine.Random.Range(0, debrisSprites.Count)];
                Color tint = UnityEngine.Random.value > 0.74f ? new Color(0.72f, 0.78f, 0.72f, 1f) : new Color(0.58f, 0.6f, 0.56f, 1f);
                GameObject prop = CreateSpriteObject("Street Prop", sprite, tint, new Vector3(position.x, position.y, 1f), Vector3.one * UnityEngine.Random.Range(0.95f, 1.75f), 3);
                prop.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                prop.transform.SetParent(worldRoot, false);
                AddShadow(prop.transform, new Vector3(1.15f, 0.42f, 1f), -0.18f, 2);
            }
            else
            {
                Color color = UnityEngine.Random.value > 0.5f ? new Color(0.12f, 0.14f, 0.16f) : new Color(0.14f, 0.08f, 0.1f);
                GameObject ruin = CreateSpriteObject("Pixel Ruin", ruinSprite, color, position, new Vector3(UnityEngine.Random.Range(0.7f, 2.2f), UnityEngine.Random.Range(0.7f, 2.8f), 1f), 2);
                ruin.transform.SetParent(worldRoot, false);
            }
        }
    }

    // Adds neon signs and glowing decorative accents.
    private void BuildNeonAccents()
    {
        Vector2[] anchors =
        {
            new Vector2(-31f, 24f),
            new Vector2(28f, 25f),
            new Vector2(-29f, -22f),
            new Vector2(33f, -23f),
            new Vector2(-43f, 11f),
            new Vector2(43f, -9f),
            new Vector2(-12f, 31f),
            new Vector2(13f, -31f),
            new Vector2(-23f, -2f),
            new Vector2(24f, 2f)
        };

        for (int i = 0; i < anchors.Length; i++)
        {
            Vector2 position = anchors[i] + UnityEngine.Random.insideUnitCircle * 2.2f;
            Color color = i % 3 == 0 ? new Color(0.2f, 0.9f, 1f, 0.82f) : i % 3 == 1 ? new Color(1f, 0.18f, 0.55f, 0.82f) : new Color(1f, 0.75f, 0.18f, 0.78f);
            GameObject glow = CreateSpriteObject("Neon Spill Light", softGlowSprite, new Color(color.r, color.g, color.b, 0.2f), new Vector3(position.x, position.y, 2.2f), Vector3.one * UnityEngine.Random.Range(5f, 8f), -4);
            glow.transform.SetParent(worldRoot, false);

            if (neonSignSprite != null)
            {
                GameObject sign = CreateSpriteObject("Broken Neon Sign", neonSignSprite, color, new Vector3(position.x, position.y, 1.8f), new Vector3(UnityEngine.Random.Range(1.5f, 2.6f), UnityEngine.Random.Range(0.65f, 1.05f), 1f), 5);
                sign.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-18f, 18f));
                sign.transform.SetParent(worldRoot, false);
            }
        }
    }

    // Loads player walk animation frames.
    private void LoadPlayerWalkFrames()
    {
        playerWalkFrames.Clear();
        playerWalkFramesAreIdle = false;

        if (LoadChibiPyromancerIdleFrames())
        {
            return;
        }
    }

    // Loads player idle animation frames and replaces the fallback sprite when found.
    private bool LoadChibiPyromancerIdleFrames()
    {
        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Player", "chibi_pyromancer_idle");
        if (!Directory.Exists(root))
        {
            return false;
        }

        string[] files = Directory.GetFiles(root, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 400f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count == 0)
        {
            return false;
        }

        Sprite[] idleFrames = frames.ToArray();
        playerIdleFrames = idleFrames;
        playerSprite = idleFrames[0];
        Sprite[] rightWalkFrames = LoadChibiPyromancerWalkRightFrames();
        if (rightWalkFrames != null && rightWalkFrames.Length > 0)
        {
            playerWalkFrames["walk_right"] = rightWalkFrames;
            playerWalkFrames["walk_left"] = rightWalkFrames;
            playerWalkFrames["walk_down"] = rightWalkFrames;
            playerWalkFramesAreIdle = false;
        }
        else
        {
            playerWalkFrames["walk_right"] = idleFrames;
            playerWalkFrames["walk_left"] = idleFrames;
            playerWalkFrames["walk_down"] = idleFrames;
            playerWalkFramesAreIdle = true;
        }

        return true;
    }

    // Loads player walk-right animation frames.
    private Sprite[] LoadChibiPyromancerWalkRightFrames()
    {
        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Player", "chibi_pyromancer_walk_right");
        if (!Directory.Exists(root))
        {
            return null;
        }

        string[] files = Directory.GetFiles(root, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 400f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        return frames.Count > 0 ? frames.ToArray() : null;
    }

    // Loads the hurt frames for the currently selected player art.
    private void LoadScreenSelectedHurtFrames()
    {
        if (LoadChibiPyromancerHurtFrames())
        {
            return;
        }

        string root = Path.Combine(Application.dataPath, "screen_selected");
        if (!Directory.Exists(root))
        {
            return;
        }

        int[] columns = { 2, 3, 4, 5 };
        List<Sprite> frames = new List<Sprite>(columns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            string path = Path.Combine(root, "screen_r2_c" + columns[i] + ".png");
            if (!File.Exists(path))
            {
                continue;
            }

            Sprite frame = LoadSpriteFromPng(path, 160f, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count > 0)
        {
            playerHurtFrames = frames.ToArray();
        }
    }

    // Loads player hurt animation frames used during damage feedback.
    private bool LoadChibiPyromancerHurtFrames()
    {
        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Player", "hurt");
        if (!Directory.Exists(root))
        {
            return false;
        }

        string[] files = Directory.GetFiles(root, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 400f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count == 0)
        {
            return false;
        }

        playerHurtFrames = frames.ToArray();
        return true;
    }

    // Loads Kenney top-down map and decoration assets.
    private void LoadKenneyTopdownArt()
    {
        string root = Path.Combine(Application.dataPath, "ExternalArt", "KenneyTopdownShooter", "PNG");
        if (!Directory.Exists(root))
        {
            return;
        }

        string tileRoot = Path.Combine(root, "Tiles");
        groundSprites.Clear();
        debrisSprites.Clear();

        int[] groundIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        for (int i = 0; i < groundIds.Length; i++)
        {
            AddSpriteIfExists(groundSprites, Path.Combine(tileRoot, "tile_" + groundIds[i].ToString("00") + ".png"), 64f, false);
        }

        int[] debrisIds = { 131, 132, 134, 156, 157, 158, 181, 182, 183, 184, 185, 186, 197, 198, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 235, 236, 237, 238, 239, 240 };
        for (int i = 0; i < debrisIds.Length; i++)
        {
            AddSpriteIfExists(debrisSprites, Path.Combine(tileRoot, "tile_" + debrisIds[i] + ".png"), 64f, false);
        }

        kenneyZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_stand.png"), 24f, false);
        kenneyFastZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_hold.png"), 24f, false);
        kenneyTankZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_machine.png"), 22f, false);
        kenneyEliteZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_gun.png"), 22f, false);
        kenneyBossSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_reload.png"), 18f, false);

    }

    // Loads walk frames for small enemy art.
    private void LoadChibiEnemyWalkFrames()
    {
        chibiEnemyWalkFrames = new Sprite[0];

        string folder = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "chibi_zombie");
        const float chibiPixelsPerUnit = 440f;
        Sprite[] frames = LoadEnemyFrameSequence(folder, "zombie_chibi_", chibiPixelsPerUnit, true);
        if (frames == null || frames.Length == 0)
        {
            string sheetPath = Path.Combine(folder, "zombie_chibi_walk.png");
            frames = LoadEnemyWalkSheet(sheetPath, chibiPixelsPerUnit, true, 4, 4);
        }

        if (frames != null && frames.Length > 0)
        {
            chibiEnemyWalkFrames = frames;
        }
    }

    // Loads animation frames for the villager enemy.
    private void LoadCraftpixVillagerFrames()
    {
        villagerRunFrames = new Sprite[0];
        villagerSlashFrames = new Sprite[0];
        villagerHurtFrames = new Sprite[0];
        villagerDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_villager");
        const float pixelsPerUnit = 264f;
        villagerRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        villagerSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        villagerHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        villagerDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the melee enemy.
    private void LoadCraftpixGoblinFrames()
    {
        goblinRunFrames = new Sprite[0];
        goblinHurtFrames = new Sprite[0];
        goblinDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_goblin");
        const float pixelsPerUnit = 264f;
        goblinRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        goblinHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        goblinDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the gravedigger enemy.
    private void LoadCraftpixGravediggerFrames()
    {
        gravediggerRunFrames = new Sprite[0];
        gravediggerSlashFrames = new Sprite[0];
        gravediggerHurtFrames = new Sprite[0];
        gravediggerDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_gravedigger");
        const float pixelsPerUnit = 264f;
        gravediggerRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        gravediggerSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        gravediggerHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        gravediggerDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the reaper enemy.
    private void LoadCraftpixReaperFrames()
    {
        reaperRunFrames = new Sprite[0];
        reaperSlashFrames = new Sprite[0];
        reaperHurtFrames = new Sprite[0];
        reaperDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_reaper");
        const float pixelsPerUnit = 264f;
        reaperRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        reaperSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        reaperHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        reaperDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the orc thrower enemy.
    private void LoadCraftpixOrcFrames()
    {
        orcRunFrames = new Sprite[0];
        orcThrowFrames = new Sprite[0];
        orcHurtFrames = new Sprite[0];
        orcDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_orc");
        const float pixelsPerUnit = 264f;
        orcRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        orcThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        orcHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        orcDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the crystal boss.
    private void LoadCraftpixCrystalGolemFrames()
    {
        crystalGolemRunFrames = new Sprite[0];
        crystalGolemSlashFrames = new Sprite[0];
        crystalGolemThrowFrames = new Sprite[0];
        crystalGolemHurtFrames = new Sprite[0];
        crystalGolemDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_crystal_golem");
        const float pixelsPerUnit = 230f;
        crystalGolemRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        crystalGolemSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        crystalGolemThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        crystalGolemHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        crystalGolemDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the moss boss.
    private void LoadCraftpixMossGolemFrames()
    {
        mossGolemRunFrames = new Sprite[0];
        mossGolemSlashFrames = new Sprite[0];
        mossGolemThrowFrames = new Sprite[0];
        mossGolemHurtFrames = new Sprite[0];
        mossGolemDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_moss_golem");
        const float pixelsPerUnit = 230f;
        mossGolemRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        mossGolemSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        mossGolemThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        mossGolemHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        mossGolemDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the ember boss.
    private void LoadCraftpixEmberGolemFrames()
    {
        emberGolemRunFrames = new Sprite[0];
        emberGolemSlashFrames = new Sprite[0];
        emberGolemThrowFrames = new Sprite[0];
        emberGolemHurtFrames = new Sprite[0];
        emberGolemDeathFrames = new Sprite[0];

        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Enemies", "craftpix_ember_golem");
        const float pixelsPerUnit = 230f;
        emberGolemRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        emberGolemSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        emberGolemThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        emberGolemHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        emberGolemDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads the custom map image used by arena generation.
    private void LoadCustomArenaMap()
    {
        customArenaMapSprite = null;

        string path = Path.Combine(Application.dataPath, "ZombieStormArt", "Maps", "graveyard_arena.png");
        customArenaMapSprite = LoadRawSpriteFromPng(path, 64f, false);
    }

    // Loads the main menu cover image.
    private void LoadMainMenuCover()
    {
        mainMenuCoverSprite = null;

        string path = Path.Combine(Application.dataPath, "ZombieStormArt", "Menu", "main_menu_cover.png");
        mainMenuCoverSprite = LoadRawSpriteFromPng(path, 100f, false, FilterMode.Bilinear, false);
    }

    // Loads the sprite used by the Fire Spirit summon.
    private void LoadFireSpiritSprite()
    {
        fireSpiritSprite = null;

        string path = Path.Combine(Application.dataPath, "ZombieStormArt", "Player", "FireSpirit.png");
        fireSpiritSprite = LoadRawSpriteFromPng(path, 720f, true, FilterMode.Bilinear, false, true);
    }

    // Loads spell effect frame sequences such as fire and explosions.
    private void LoadMikodrakSpellEffects()
    {
        effectFrames.Clear();
        projectileFxSprite = null;

        string root = Path.Combine(Application.dataPath, "ExternalArt", "MikodrakSpellEffects");
        if (!Directory.Exists(root))
        {
            AddDarkVfxEffectSequences();
            AddFoozlePixelMagicEffectSequences();
            AddFireZoneEffectSequences();
            return;
        }

        AddEffectSequence(root, "spark", "fx1_blue_topEffect", 240f);
        AddEffectSequence(root, "fire", "fx3_fireBall", 240f);
        AddEffectSequence(root, "burst", "fx7_energyBall", 240f);
        AddEffectSequence(root, "lightning", "fx8_lighteningBall", 240f);
        AddEffectSequence(root, "explosion", "fx10_blackExplosion", 240f);
        AddDarkVfxEffectSequences();
        AddFoozlePixelMagicEffectSequences();
        AddFireZoneEffectSequences();

        Sprite[] projectileFrames;
        if (effectFrames.TryGetValue("burst", out projectileFrames) && projectileFrames != null && projectileFrames.Length > 0)
        {
            projectileFxSprite = projectileFrames[Mathf.Min(2, projectileFrames.Length - 1)];
        }
    }

    // Loads animation frames for the ice boss orb projectile.
    private void LoadIceBossOrbFrames()
    {
        iceBossOrbFrames = new Sprite[0];

        string folder = Path.Combine(Application.dataPath, "ZombieStormArt", "Effects", "IceBossOrb");
        if (!Directory.Exists(folder))
        {
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 220f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count > 0)
        {
            iceBossOrbFrames = frames.ToArray();
        }
    }

    // Registers dark spell effect frame sequences.
    private void AddDarkVfxEffectSequences()
    {
        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Effects");
        AddEffectSequence(root, "ember_dash_blast", Path.Combine("DarkVFX1", "Frames"), 38f);
        AddEffectSequence(root, "ember_meteor_blast", Path.Combine("DarkVFX2", "Frames"), 44f);
        AddEffectSequence(root, "ember_boss_meteor", "EmberBossMeteorSelected", 180f);
        AddEffectSequence(root, "poison_boss_blast", "CraftpixPoisonExplosion10", 150f);
    }

    // Registers pixel magic effect frame sequences.
    private void AddFoozlePixelMagicEffectSequences()
    {
        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Effects", "FoozlePixelMagic");
        AddEffectSequence(root, "foozle_fireball", "Fire_Ball", 64f);
        AddEffectSequence(root, "foozle_explosion", "Explosion", 72f);
        AddEffectSequence(root, "meteor_blast", "Explosion", 82f);
        AddEffectSequence(root, "shield_burst", "Wind", 92f);
        AddEffectSequence(root, "ultimate_storm", "Tornado", 88f);
    }

    // Registers Fire Zone bomb and ground-fire effect frame sequences.
    private void AddFireZoneEffectSequences()
    {
        string root = Path.Combine(Application.dataPath, "ZombieStormArt", "Effects");
        AddEffectSequence(root, "fire_bomb", "FireBomb", 96f);
        AddEffectSheetSequence(root, "fire_pool_tekila_01", Path.Combine("GroundFire", "TekilaFire01", "Fire-01_320x160_Sheet.png"), 5, 1, 96f, true);
    }

    // Loads one effect frame sequence from a folder and stores it by key.
    private void AddEffectSequence(string root, string key, string folderName, float pixelsPerUnit)
    {
        string folder = Path.Combine(root, folderName);
        if (!Directory.Exists(folder))
        {
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite sprite = LoadRawSpriteFromPng(files[i], pixelsPerUnit, false);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        if (frames.Count > 0)
        {
            effectFrames[key] = frames.ToArray();
        }
    }

    // Loads one effect sprite sheet and stores sliced frames by key.
    private void AddEffectSheetSequence(string root, string key, string sheetName, int columns, int rows, float pixelsPerUnit, bool removeBlackBackground)
    {
        string path = Path.Combine(root, sheetName);
        if (!File.Exists(path) || columns <= 0 || rows <= 0)
        {
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return;
            }

            if (removeBlackBackground)
            {
                RemoveBlackBackground(texture);
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(false, false);
            List<Sprite> frames = new List<Sprite>(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                int sourceRow = rows - row - 1;
                for (int column = 0; column < columns; column++)
                {
                    int left = Mathf.RoundToInt(column * texture.width / (float)columns);
                    int right = Mathf.RoundToInt((column + 1) * texture.width / (float)columns);
                    int bottom = Mathf.RoundToInt(sourceRow * texture.height / (float)rows);
                    int top = Mathf.RoundToInt((sourceRow + 1) * texture.height / (float)rows);
                    Sprite frame = Sprite.Create(texture, new Rect(left, bottom, right - left, top - bottom), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                    frame.name = texture.name + "_" + (frames.Count + 1).ToString("00");
                    frames.Add(frame);
                }
            }

            if (frames.Count > 0)
            {
                effectFrames[key] = frames.ToArray();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load effect sheet: " + path + "\n" + exception.Message);
        }
    }

    // Sorts frame files by the trailing number in their names.
    private static int CompareFrameFileNames(string left, string right)
    {
        int leftNumber = ExtractTrailingFrameNumber(Path.GetFileNameWithoutExtension(left));
        int rightNumber = ExtractTrailingFrameNumber(Path.GetFileNameWithoutExtension(right));
        int numberCompare = leftNumber.CompareTo(rightNumber);
        return numberCompare != 0 ? numberCompare : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    // Extracts the trailing frame number from a file name.
    private static int ExtractTrailingFrameNumber(string name)
    {
        int frameIndex = name.IndexOf("frame", StringComparison.OrdinalIgnoreCase);
        if (frameIndex >= 0)
        {
            int scan = frameIndex + 5;
            while (scan < name.Length && (name[scan] == '-' || name[scan] == '_' || name[scan] == ' '))
            {
                scan++;
            }

            int valueFromFramePrefix = 0;
            bool foundFrameDigit = false;
            while (scan < name.Length && name[scan] >= '0' && name[scan] <= '9')
            {
                valueFromFramePrefix = valueFromFramePrefix * 10 + name[scan] - '0';
                foundFrameDigit = true;
                scan++;
            }

            if (foundFrameDigit)
            {
                return valueFromFramePrefix;
            }
        }

        int value = 0;
        int multiplier = 1;
        bool foundDigit = false;
        for (int i = name.Length - 1; i >= 0; i--)
        {
            char character = name[i];
            if (character < '0' || character > '9')
            {
                break;
            }

            foundDigit = true;
            value += (character - '0') * multiplier;
            multiplier *= 10;
        }

        return foundDigit ? value : 0;
    }

    // Loads a sprite into a list only when the source file exists.
    private void AddSpriteIfExists(List<Sprite> target, string path, float pixelsPerUnit, bool removeCheckerBackground)
    {
        Sprite sprite = LoadRawSpriteFromPng(path, pixelsPerUnit, removeCheckerBackground);
        if (sprite != null)
        {
            target.Add(sprite);
        }
    }

    // Slices enemy walk frames from a sprite sheet.
    private Sprite[] LoadEnemyWalkSheet(string path, float pixelsPerUnit, bool removeCheckerBackground, int specifiedColumns = 0, int specifiedRows = 0)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            if (removeCheckerBackground)
            {
                RemoveEdgeCheckerBackground(texture);
                CleanBackgroundFringe(texture);
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(false, false);

            bool tenFrameGrid = texture.width < texture.height * 3.2f;
            int columns = specifiedColumns > 0 ? specifiedColumns : tenFrameGrid ? 5 : 4;
            int rows = specifiedRows > 0 ? specifiedRows : tenFrameGrid ? 2 : 1;
            List<Sprite> frames = new List<Sprite>(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                int sourceRow = rows - row - 1;
                for (int column = 0; column < columns; column++)
                {
                    int left = Mathf.RoundToInt(column * texture.width / (float)columns);
                    int right = Mathf.RoundToInt((column + 1) * texture.width / (float)columns);
                    int bottom = Mathf.RoundToInt(sourceRow * texture.height / (float)rows);
                    int top = Mathf.RoundToInt((sourceRow + 1) * texture.height / (float)rows);
                    Rect rect = new Rect(left, bottom, right - left, top - bottom);
                    Sprite frame = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
                    frame.name = texture.name + "_" + (frames.Count + 1).ToString("00");
                    frames.Add(frame);
                }
            }

            return frames.ToArray();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load custom enemy walk sheet: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Loads enemy animation frames from numbered files.
    private Sprite[] LoadEnemyFrameSequence(string folder, string prefix, float pixelsPerUnit, bool removeCheckerBackground)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        List<Sprite> frames = new List<Sprite>(16);
        for (int i = 1; i <= 24; i++)
        {
            string path = Path.Combine(folder, prefix + i.ToString("00") + ".png");
            if (!File.Exists(path))
            {
                if (frames.Count > 0)
                {
                    break;
                }

                continue;
            }

            Sprite sprite = LoadRawSpriteFromPng(path, pixelsPerUnit, removeCheckerBackground);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        return frames.Count > 0 ? frames.ToArray() : null;
    }

    // Loads and sorts all enemy animation frames in a folder.
    private Sprite[] LoadEnemyFrameFolder(string folder, float pixelsPerUnit)
    {
        if (!Directory.Exists(folder))
        {
            return new Sprite[0];
        }

        string[] files = Directory.GetFiles(folder, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite sprite = LoadRawSpriteFromPng(files[i], pixelsPerUnit, false);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        return frames.ToArray();
    }

    // Loads a PNG as a sprite with optional background cleanup.
    private Sprite LoadRawSpriteFromPng(string path, float pixelsPerUnit, bool removeCheckerBackground)
    {
        return LoadRawSpriteFromPng(path, pixelsPerUnit, removeCheckerBackground, FilterMode.Bilinear, true);
    }

    // Loads a PNG as a sprite with optional background cleanup.
    private Sprite LoadRawSpriteFromPng(string path, float pixelsPerUnit, bool removeCheckerBackground, FilterMode filterMode, bool useMipMaps, bool pivotOnOpaqueCenter = false)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, useMipMaps);
            texture.filterMode = filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            if (removeCheckerBackground)
            {
                RemoveEdgeCheckerBackground(texture);
                CleanBackgroundFringe(texture);
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(useMipMaps, false);
            Vector2 pivot = pivotOnOpaqueCenter ? CalculateOpaqueCenterPivot(texture) : new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, pixelsPerUnit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load external art: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Calculates a sprite pivot from the center of its visible pixels.
    private static Vector2 CalculateOpaqueCenterPivot(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        long sumX = 0;
        long sumY = 0;
        long count = 0;

        for (int y = 0; y < texture.height; y++)
        {
            int row = y * texture.width;
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[row + x].a <= 20)
                {
                    continue;
                }

                sumX += x;
                sumY += y;
                count++;
            }
        }

        if (count == 0)
        {
            return new Vector2(0.5f, 0.5f);
        }

        return new Vector2(
            Mathf.Clamp01(sumX / (float)count / Mathf.Max(1, texture.width - 1)),
            Mathf.Clamp01(sumY / (float)count / Mathf.Max(1, texture.height - 1)));
    }

    // Loads a PNG file and converts it into a Unity sprite.
    private Sprite LoadSpriteFromPng(string path, float pixelsPerUnit, bool removeCheckerBackground = false)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            if (removeCheckerBackground)
            {
                RemoveEdgeCheckerBackground(texture);
            }

            Texture2D normalizedTexture = NormalizePlayerFrame(texture, 220, 270);
            normalizedTexture.name = Path.GetFileNameWithoutExtension(path);
            return Sprite.Create(normalizedTexture, new Rect(0f, 0f, normalizedTexture.width, normalizedTexture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load player frame: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Makes near-black sprite-sheet backgrounds transparent while preserving fire colors.
    private static void RemoveBlackBackground(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 color = pixels[i];
            if (color.a < 10)
            {
                continue;
            }

            int brightness = color.r + color.g + color.b;
            if (brightness <= 34 && color.r <= 18 && color.g <= 18 && color.b <= 18)
            {
                color.a = 0;
                pixels[i] = color;
            }
        }

        texture.SetPixels32(pixels);
    }

    // Removes checkerboard transparency background connected to image edges.
    private void RemoveEdgeCheckerBackground(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>();

        for (int x = 0; x < width; x++)
        {
            TryQueueBackgroundPixel(x, 0, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, height - 1, width, pixels, visited, queue);
        }

        for (int y = 0; y < height; y++)
        {
            TryQueueBackgroundPixel(0, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(width - 1, y, width, pixels, visited, queue);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            pixels[index].a = 0;
            int x = index % width;
            int y = index / width;
            TryQueueBackgroundPixel(x + 1, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x - 1, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, y + 1, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, y - 1, width, pixels, visited, queue);
        }

        texture.SetPixels32(pixels);
        texture.Apply(true, false);
    }

    // Cleans leftover edge colors to reduce white or gray outlines.
    private void CleanBackgroundFringe(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        Color32[] cleaned = new Color32[pixels.Length];
        Array.Copy(pixels, cleaned, pixels.Length);

        for (int pass = 0; pass < 4; pass++)
        {
            bool changed = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color32 color = pixels[index];
                    if (color.a == 0 || !TouchesTransparentPixel(x, y, width, height, pixels))
                    {
                        continue;
                    }

                    int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                    int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                    int average = (color.r + color.g + color.b) / 3;
                    int saturation = max - min;
                    if (average >= 188 && saturation <= 82)
                    {
                        cleaned[index].a = 0;
                        changed = true;
                    }
                    else if (average >= 172 && saturation <= 96)
                    {
                        cleaned[index].a = (byte)Mathf.Min(color.a, 92);
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                break;
            }

            Color32[] swap = pixels;
            pixels = cleaned;
            cleaned = swap;
            Array.Copy(pixels, cleaned, pixels.Length);
        }

        DilateTransparentPixels(texture, pixels);
    }

    // Checks whether a pixel touches transparency during edge cleanup.
    private static bool TouchesTransparentPixel(int x, int y, int width, int height, Color32[] pixels)
    {
        for (int yy = y - 1; yy <= y + 1; yy++)
        {
            for (int xx = x - 1; xx <= x + 1; xx++)
            {
                if (xx == x && yy == y)
                {
                    continue;
                }

                if (xx < 0 || xx >= width || yy < 0 || yy >= height)
                {
                    return true;
                }

                if (pixels[yy * width + xx].a < 20)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Fills transparent pixels with neighbor colors to prevent scaled texture borders.
    private void DilateTransparentPixels(Texture2D texture, Color32[] pixels)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] dilated = new Color32[pixels.Length];
        Array.Copy(pixels, dilated, pixels.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (pixels[index].a >= 20)
                {
                    continue;
                }

                Color32 replacement;
                if (TryFindOpaqueNeighborColor(x, y, width, height, pixels, out replacement))
                {
                    replacement.a = 0;
                    dilated[index] = replacement;
                }
            }
        }

        texture.SetPixels32(dilated);
        texture.Apply(false, false);
    }

    // Finds a nearby opaque pixel color for transparent edge filling.
    private static bool TryFindOpaqueNeighborColor(int x, int y, int width, int height, Color32[] pixels, out Color32 color)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int count = 0;
            for (int yy = y - radius; yy <= y + radius; yy++)
            {
                for (int xx = x - radius; xx <= x + radius; xx++)
                {
                    if (xx < 0 || xx >= width || yy < 0 || yy >= height)
                    {
                        continue;
                    }

                    Color32 sample = pixels[yy * width + xx];
                    if (sample.a < 210)
                    {
                        continue;
                    }

                    r += sample.r;
                    g += sample.g;
                    b += sample.b;
                    count++;
                }
            }

            if (count > 0)
            {
                color = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
                return true;
            }
        }

        color = new Color32(0, 0, 0, 0);
        return false;
    }

    // Adds a likely background pixel to the flood-fill queue.
    private static void TryQueueBackgroundPixel(int x, int y, int width, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        int height = pixels.Length / width;
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = y * width + x;
        if (visited[index] || !IsCheckerBackground(pixels[index]))
        {
            return;
        }

        visited[index] = true;
        queue.Enqueue(index);
    }

    // Checks whether a pixel color looks like checkerboard background.
    private static bool IsCheckerBackground(Color32 color)
    {
        if (color.a < 10)
        {
            return true;
        }

        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        int average = (color.r + color.g + color.b) / 3;
        int saturation = max - min;
        if (average >= 232)
        {
            return true;
        }

        return saturation <= 48 && average >= 168;
    }

    // Places player frames onto a consistent texture canvas size.
    private Texture2D NormalizePlayerFrame(Texture2D source, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);

        int offsetX = Mathf.RoundToInt((width - source.width) * 0.5f);
        int offsetY = 0;
        Color[] pixels = source.GetPixels();
        texture.SetPixels(offsetX, offsetY, source.width, source.height, pixels);
        texture.Apply(true, false);
        return texture;
    }

    // Programmatically draws a fallback survivor sprite.
    private Sprite CreateSurvivorSprite()
    {
        const int width = 32;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        ClearTexture(texture, Color.clear);

        Color outline = new Color(0.08f, 0.055f, 0.035f, 1f);
        Color hatDark = new Color(0.28f, 0.20f, 0.10f, 1f);
        Color hat = new Color(0.58f, 0.43f, 0.20f, 1f);
        Color hatLight = new Color(0.86f, 0.68f, 0.33f, 1f);
        Color skin = new Color(0.92f, 0.68f, 0.45f, 1f);
        Color skinShadow = new Color(0.62f, 0.36f, 0.22f, 1f);
        Color hair = new Color(0.18f, 0.11f, 0.07f, 1f);
        Color shirt = new Color(0.86f, 0.88f, 0.80f, 1f);
        Color vest = new Color(0.50f, 0.34f, 0.16f, 1f);
        Color scarf = new Color(0.72f, 0.08f, 0.06f, 1f);
        Color denim = new Color(0.12f, 0.22f, 0.34f, 1f);
        Color boot = new Color(0.20f, 0.12f, 0.07f, 1f);
        Color glove = new Color(0.22f, 0.14f, 0.08f, 1f);
        Color eye = new Color(0.08f, 0.18f, 0.32f, 1f);

        FillEllipse(texture, 16, 28, 8, 2, new Color(0f, 0f, 0f, 0.32f));

        FillRect(texture, 10, 20, 4, 7, outline);
        FillRect(texture, 18, 20, 4, 7, outline);
        FillRect(texture, 11, 20, 3, 6, denim);
        FillRect(texture, 18, 20, 3, 6, denim);
        FillRect(texture, 9, 26, 6, 3, outline);
        FillRect(texture, 17, 26, 6, 3, outline);
        FillRect(texture, 10, 26, 5, 2, boot);
        FillRect(texture, 17, 26, 5, 2, boot);

        FillRect(texture, 8, 13, 16, 10, outline);
        FillRect(texture, 9, 14, 14, 8, shirt);
        FillRect(texture, 10, 14, 4, 8, vest);
        FillRect(texture, 18, 14, 4, 8, vest);
        FillRect(texture, 14, 14, 4, 5, new Color(0.96f, 0.95f, 0.84f, 1f));
        FillRect(texture, 14, 15, 4, 3, scarf);
        FillRect(texture, 15, 18, 2, 4, new Color(0.95f, 0.72f, 0.28f, 1f));

        FillRect(texture, 5, 15, 5, 7, outline);
        FillRect(texture, 22, 15, 5, 7, outline);
        FillRect(texture, 6, 15, 3, 6, shirt);
        FillRect(texture, 23, 15, 3, 6, shirt);
        FillRect(texture, 5, 21, 4, 3, glove);
        FillRect(texture, 23, 21, 4, 3, glove);

        FillEllipse(texture, 16, 11, 8, 6, outline);
        FillEllipse(texture, 16, 11, 7, 5, skin);
        FillRect(texture, 9, 8, 3, 6, hair);
        FillRect(texture, 20, 8, 3, 6, hair);
        FillRect(texture, 11, 13, 10, 2, skinShadow);
        SetPixelSafe(texture, 12, 10, eye);
        SetPixelSafe(texture, 20, 10, eye);
        SetPixelSafe(texture, 13, 11, Color.white);
        SetPixelSafe(texture, 21, 11, Color.white);
        FillRect(texture, 15, 13, 4, 1, new Color(0.38f, 0.12f, 0.08f, 1f));

        FillEllipse(texture, 16, 6, 13, 3, outline);
        FillEllipse(texture, 16, 6, 12, 2, hatDark);
        FillRect(texture, 9, 1, 14, 7, outline);
        FillRect(texture, 10, 1, 12, 6, hat);
        FillRect(texture, 12, 2, 8, 2, hatLight);
        FillRect(texture, 8, 7, 16, 2, hatDark);
        FillRect(texture, 12, 6, 8, 1, hatLight);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 24f);
    }

    // Creates a simple pixel-art fallback sprite.
    private Sprite CreatePixelSprite(Color baseColor, Color accentColor, int size, bool character)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = Color.clear;

                if (character)
                {
                    if (distance <= radius)
                    {
                        pixel = baseColor;
                    }

                    if (distance <= radius * 0.55f && y > size * 0.45f)
                    {
                        pixel = Color.Lerp(baseColor, accentColor, 0.72f);
                    }

                    if (x < 2 || x > size - 3 || y < 2 || y > size - 3)
                    {
                        pixel.a *= 0.4f;
                    }
                }
                else
                {
                    pixel = baseColor;
                    if ((x + y) % 5 == 0)
                    {
                        pixel = Color.Lerp(baseColor, accentColor, 0.55f);
                    }
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws the red orbiting blade sprite.
    private Sprite CreateOrbitingBladeSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color glow = new Color(1f, 0.12f, 0.06f, 0.34f);
        Color edge = new Color(0.34f, 0.02f, 0.02f, 1f);
        Color steel = new Color(1f, 0.46f, 0.36f, 1f);
        Color highlight = Color.white;
        Color hilt = new Color(0.82f, 0.12f, 0.08f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x - center.x;
                float py = y - center.y;
                Color pixel = Color.clear;
                float bodyT = Mathf.InverseLerp(-24f, 22f, px);
                float bladeHalfWidth = Mathf.Lerp(6.4f, 2.1f, bodyT);
                bool bladeBody = px >= -24f && px <= 22f && Mathf.Abs(py) <= bladeHalfWidth;
                bool bladeTip = px > 22f && px <= 30f && Mathf.Abs(py) <= (30f - px) * 0.48f;
                bool bladeGlow = px >= -28f && px <= 31f && Mathf.Abs(py) <= bladeHalfWidth + 4.4f;
                bool grip = px >= -31f && px < -23f && Mathf.Abs(py) <= 8.2f;
                bool guard = px >= -24f && px <= -19f && Mathf.Abs(py) <= 12.5f;

                if (bladeGlow || (px > 22f && px <= 31f && Mathf.Abs(py) <= (31f - px) * 0.58f + 3f))
                {
                    pixel = glow;
                }

                if (bladeBody || bladeTip)
                {
                    pixel = Mathf.Abs(py) > bladeHalfWidth - 1.3f ? edge : Color.Lerp(steel, highlight, Mathf.Clamp01((py + bladeHalfWidth) / Mathf.Max(0.01f, bladeHalfWidth * 2f)));
                }

                if (grip)
                {
                    pixel = Mathf.FloorToInt(Mathf.Abs(py)) % 4 == 0 ? edge : hilt;
                }

                if (guard)
                {
                    pixel = Mathf.Abs(py) > 9.5f ? edge : new Color(1f, 0.3f, 0.18f, 1f);
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    // Programmatically draws the red energy ring around the blades.
    private Sprite CreateOrbitingRingSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.055f);
                float inner = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) / 0.025f) * 0.38f;
                float sparkle = (x + y) % 23 == 0 && d > 0.7f && d < 0.93f ? 0.2f : 0f;
                float alpha = Mathf.Clamp01(ring * 0.7f + inner + sparkle);
                texture.SetPixel(x, y, new Color(1f, 0.18f, 0.08f, alpha));
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    // Creates a soft circular sprite used for glows, shadows, and range markers.
    private Sprite CreateSoftDiscSprite(Color color, int size, float radiusScale, float centerFade)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f * radiusScale;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - d);
                alpha = Mathf.Pow(alpha, 1.7f);
                Color pixel = color;
                pixel.a *= Mathf.Lerp(centerFade, 1f, alpha) * alpha;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws the ground blood splat sprite.
    private Sprite CreateBloodSplatSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);
        Color blood = new Color(0.5f, 0.02f, 0.025f, 0.9f);
        FillEllipse(texture, 32, 32, 18, 9, blood);
        FillEllipse(texture, 22, 27, 9, 5, blood);
        FillEllipse(texture, 44, 36, 11, 6, blood);
        FillEllipse(texture, 34, 22, 5, 3, blood);
        FillEllipse(texture, 18, 39, 4, 3, blood);
        FillEllipse(texture, 51, 25, 3, 2, blood);
        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws a neon sign sprite.
    private Sprite CreateNeonSignSprite()
    {
        const int width = 64;
        const int height = 24;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);
        FillRect(texture, 2, 4, 60, 16, new Color(0f, 0f, 0f, 0.7f));
        FillRect(texture, 4, 6, 56, 2, Color.white);
        FillRect(texture, 4, 16, 56, 2, Color.white);
        FillRect(texture, 8, 10, 8, 4, Color.white);
        FillRect(texture, 21, 10, 14, 4, Color.white);
        FillRect(texture, 41, 10, 12, 4, Color.white);
        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
    }

    // Fills an entire texture with one color.
    private static void ClearTexture(Texture2D texture, Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    // Fills a rectangle area on a texture.
    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                SetPixelSafe(texture, xx, yy, color);
            }
        }
    }

    // Fills an ellipse area on a texture.
    private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        float rx = Mathf.Max(1f, radiusX);
        float ry = Mathf.Max(1f, radiusY);
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
        {
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                float dx = (x - centerX) / rx;
                float dy = (y - centerY) / ry;
                if (dx * dx + dy * dy <= 1f)
                {
                    SetPixelSafe(texture, x, y, color);
                }
            }
        }
    }

    // Writes one texture pixel only when the coordinates are inside bounds.
    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }
}
