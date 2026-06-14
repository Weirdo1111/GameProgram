using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-100)]
// Main game controller for startup, scene setup, UI, spawning, and upgrade flow.
public sealed partial class ZombieStormGameController : MonoBehaviour
{
    private enum ZombieStormFlowState
    {
        MainMenu,
        Story,
        Running,
        Paused,
        Settings,
        LevelUp,
        Results
    }

    public static ZombieStormGameController Instance { get; private set; }

    private const string Title = "\u50f5\u5c38\u5272\u8349\u5927\u4f5c\u6218";
    private const string RuntimeContentFolder = "ZombieStormContent";
    private const float GameplayCameraOrthographicSize = 10.5f;
    private const float GameplayHudScale = 0.8f;
    public const float EnemyDamageMultiplier = 0.75f;
    private static readonly string[] GroundFireEffectKeys = { "fire_pool_tekila_01" };

    private static string RuntimeContentRoot
    {
        get
        {
#if UNITY_EDITOR
            return Application.dataPath;
#else
            return Path.Combine(Application.streamingAssetsPath, RuntimeContentFolder);
#endif
        }
    }

    [Header("Run")]
    public float runDurationSeconds = 300f;
    public int targetFrameRate = 120;

    public ZombieStormPlayer Player { get; private set; }
    public ZombieStormSkillManager Skills { get; private set; }
    public IReadOnlyList<ZombieStormEnemy> Enemies { get { return enemies; } }

    private readonly List<ZombieStormEnemy> enemies = new List<ZombieStormEnemy>(256);
    private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<ZombieStormPassiveType, int> passives = new Dictionary<ZombieStormPassiveType, int>();
    private readonly List<ZombieStormUpgradeOption> currentChoices = new List<ZombieStormUpgradeOption>(3);
    private readonly HashSet<string> choiceKeys = new HashSet<string>();
    private readonly HashSet<string> choiceFamilies = new HashSet<string>();
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
    private Sprite bonusXpSprite;
    private Sprite healthPotionSprite;
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
    private Texture2D[] storyPageTextures = new Texture2D[0];
    private Texture2D upgradeCardTemplateTexture;
    private Texture2D magicBoltCardTemplateTexture;
    private Texture2D fireBladesCardTemplateTexture;
    private Texture2D fireZoneCardTemplateTexture;
    private Texture2D damageCardTemplateTexture;
    private Texture2D cooldownCardTemplateTexture;
    private Texture2D xpCardTemplateTexture;
    private Texture2D regenerationCardTemplateTexture;
    private Texture2D stormCardTemplateTexture;
    private Texture2D playerStatusCardTexture;
    private Texture2D failedResultTexture;
    private Texture2D victoryResultTexture;
    private Font upgradeCardTitleFont;
    private Font upgradeCardBodyFont;
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
    private bool hordeSealed;
    private int storyPageIndex;
    private int healthPotionKillCounter;
    private bool healthPotionDropPending;
    private int upgradeChoicesTaken;
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
    // Ensures a controller exists even when the scene does not contain a pre-placed bootstrap object.
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

    // Runs the top-level flow state machine: menu/story input, pause/settings routing,
    // gameplay timers, spawning, upgrade shortcuts, win/loss checks, and feedback countdowns.
    private void Update()
    {
        if (flowState == ZombieStormFlowState.MainMenu)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                BeginStoryOrRun();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitGame();
            }

            return;
        }

        if (flowState == ZombieStormFlowState.Story)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                AdvanceStoryPage();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToMainMenu();
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
        if (runTime < runDurationSeconds)
        {
            UpdateSpawning();
        }
        else if (!hordeSealed)
        {
            hordeSealed = true;
            spawnTimer = float.MaxValue;
            eliteTimer = float.MaxValue;
            ShowFeedback("The horde stops. Clear every remaining enemy.", 3f);
        }

        UpdateDamagePopups();

        if (feedbackTimer >= 15f)
        {
            feedbackTimer = 0f;
            ShowFeedback("Horde pressure rising. Keep kiting and collect XP.", 2.2f);
        }

        if (hordeSealed && GetLivingEnemyCount() == 0)
        {
            EndRun(true, "Dawn breaks. You cleared the city.");
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

    // Adds an enemy to the active list so skills, collision, rewards, and UI can find it.
    public void RegisterEnemy(ZombieStormEnemy enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    // Removes an enemy from the active list when it dies, despawns, or returns to the pool.
    public void UnregisterEnemy(ZombieStormEnemy enemy)
    {
        enemies.Remove(enemy);
    }

    // Finds the closest living enemy inside a maximum distance from the given world position.
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

    // Counts active enemies, pruning stale references so clear-stage victory cannot hang.
    private int GetLivingEnemyCount()
    {
        int count = 0;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            ZombieStormEnemy enemy = enemies[i];
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
            {
                enemies.RemoveAt(i);
                continue;
            }

            count++;
        }

        return count;
    }

    // Fetches an inactive pooled object or creates a new one, then activates it for reuse.
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

    // Deactivates an object, parents it under the pool root, and queues it for later reuse.
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

    // Spawns a player fireball and configures its damage, speed, lifetime, pierce count,
    // visual color, size, and optional Fire Zone effect when it kills an enemy.
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
    }

    // Spawns the arcing Fire Zone bomb and passes the impact damage plus all lingering
    // ground-fire settings that must be applied when the bomb reaches its target.
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
        PlaySfx("fire_bomb", 0.7f, 0.08f);
    }

    // Spawns the standard enemy projectile used by ranged enemies and some boss attacks.
    public void SpawnEnemyProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life)
    {
        SpawnEnemyProjectile(position, direction, damage, speed, life, new Color(0.5f, 1f, 0.22f, 1f), 0.44f);
    }

    // Spawns an enemy projectile with caller-selected color and size.
    public void SpawnEnemyProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life, Color color, float size)
    {
        SpawnEnemyProjectile(position, direction, damage, speed, life, color, size, fireSprite);
    }

    // Spawns the Orc Thrower's rock projectile using the shared enemy projectile behavior.
    public void SpawnEnemyRockProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life)
    {
        SpawnEnemyProjectile(position, direction, damage, speed, life, new Color(0.62f, 0.54f, 0.43f, 1f), 0.48f, rockSprite);
    }

    // Internal enemy projectile factory that applies the chosen sprite, tint, scale,
    // damage, travel speed, lifetime, and the global enemy damage multiplier.
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

    // Spawns an animated crystal orb for the ice boss and initializes its combat values.
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

    // Creates one Ember Tyrant meteor strike, including its warning, falling animation,
    // impact radius, damage, and fall duration.
    public void SpawnEmberBossMeteorStrike(Vector2 position, float damage, float radius, float fallDuration)
    {
        GameObject strikeObject = SpawnPooled("ember_meteor_strike", CreateEmberBossMeteorStrike);
        strikeObject.transform.SetParent(worldRoot, false);
        strikeObject.transform.position = position;
        ZombieStormEmberMeteorStrike strike = strikeObject.GetComponent<ZombieStormEmberMeteorStrike>();
        strike.Initialize(this, position, damage * EnemyDamageMultiplier, radius, fallDuration);
    }

    // Spawns a player-owned or neutral area effect that can repeatedly damage enemies
    // while displaying either an imported animation or a configured static sprite.
    public void SpawnAreaEffect(Vector2 position, float radius, float damage, float duration, float tickRate, Color color, string poolKey)
    {
        GameObject effectObject = SpawnPooled(poolKey, CreateAreaEffect);
        effectObject.transform.SetParent(worldRoot, false);
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer spriteRenderer = effectObject.GetComponent<SpriteRenderer>();
        ConfigureAreaEffectSprite(spriteRenderer, poolKey);
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = IsForegroundEffect(poolKey) ? 48 : 14;
        ZombieStormAreaEffect effect = effectObject.GetComponent<ZombieStormAreaEffect>();
        effect.Initialize(this, poolKey, radius, damage, duration, tickRate);
    }

    // Spawns an enemy-owned area effect that damages the player and renders above
    // ordinary world sprites so dangerous ground attacks remain visible.
    public void SpawnEnemyAreaEffect(Vector2 position, float radius, float damage, float duration, float tickRate, Color color, string poolKey)
    {
        GameObject effectObject = SpawnPooled(poolKey, CreateAreaEffect);
        effectObject.transform.SetParent(worldRoot, false);
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer spriteRenderer = effectObject.GetComponent<SpriteRenderer>();
        ConfigureAreaEffectSprite(spriteRenderer, poolKey);
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = IsForegroundEffect(poolKey) ? 48 : 14;
        ZombieStormAreaEffect effect = effectObject.GetComponent<ZombieStormAreaEffect>();
        effect.Initialize(this, poolKey, radius, damage * EnemyDamageMultiplier, duration, tickRate, true);
    }

    // Creates a lightweight delayed attack timer. When the warning delay expires, it
    // spawns the enemy area effect and optionally triggers camera shake and sound.
    public void SpawnDelayedEnemyAreaEffect(Vector2 position, float delay, float radius, float damage, float duration, float tickRate, Color color, string poolKey, float shakePower = 0f, float shakeDuration = 0f, float sfxVolume = 0f)
    {
        GameObject delayedObject = new GameObject("Delayed Enemy Area Effect");
        delayedObject.transform.SetParent(worldRoot, false);
        ZombieStormDelayedAreaEffect delayed = delayedObject.AddComponent<ZombieStormDelayedAreaEffect>();
        delayed.Initialize(this, position, delay, radius, damage, duration, tickRate, color, poolKey, shakePower, shakeDuration, sfxVolume);
    }

    // Returns true for impact and warning effects that must render in the foreground.
    private static bool IsForegroundEffect(string poolKey)
    {
        return poolKey == "hit_spark" || poolKey == "lightning_flash" || poolKey == "foozle_explosion" || poolKey == "meteor_warning" || poolKey == "poison_boss_blast" || poolKey == "ember_dash_blast" || poolKey == "ember_meteor_blast" || poolKey == "ember_boss_meteor";
    }

    // Chooses static sprites for area effects that should not use animated sheet frames.
    private void ConfigureAreaEffectSprite(SpriteRenderer spriteRenderer, string poolKey)
    {
        if (poolKey == "meteor_warning" && orbitRingSprite != null)
        {
            spriteRenderer.sprite = orbitRingSprite;
            return;
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = fireSprite;
        }
    }

    // Spawns a short, non-damaging spark or glow used for hits, pickups, heals, and impacts.
    public void SpawnHitSpark(Vector2 position, Color color, float radius = 0.36f)
    {
        SpawnAreaEffect(position, radius, 0f, 0.12f, 1f, color, "hit_spark");
        PlaySfx("hit", 0.2f + Mathf.Clamp01(radius) * 0.18f, 0.045f);
    }

    // Adds a floating damage number to the HUD list and removes the oldest entry when
    // the configured popup limit has been reached.
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

    // Places a temporary blood decal with randomized rotation and scale at the hit position.
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

    // Starts the default red screen flash used when the player takes damage.
    public void FlashScreen(float amount)
    {
        screenFlashColor = new Color(1f, 0.08f, 0.04f);
        screenFlash = Mathf.Max(screenFlash, amount);
    }

    // Starts a screen flash with a caller-selected color and intensity/duration value.
    public void FlashScreen(Color color, float amount)
    {
        screenFlashColor = color;
        screenFlash = Mathf.Max(screenFlash, amount);
    }

    // Starts camera shake while preserving any stronger or longer shake already in progress.
    public void ShakeCamera(float power, float duration)
    {
        cameraShakePower = Mathf.Max(cameraShakePower, power);
        cameraShakeTime = Mathf.Max(cameraShakeTime, duration);
    }

    // Spawns the normal XP pickup and, when requested, an additional bonus-XP pickup.
    public void SpawnPickup(Vector2 position, int xp, int bonusXp)
    {
        if (xp > 0)
        {
            GameObject xpObject = SpawnPooled("xp_orb", CreateXpOrb);
            xpObject.transform.SetParent(worldRoot, false);
            xpObject.transform.position = position + UnityEngine.Random.insideUnitCircle * 0.35f;
            xpObject.GetComponent<ZombieStormPickup>().Initialize(this, "xp_orb", xp, 0f);
        }

        if (bonusXp > 0)
        {
            GameObject bonusXpObject = SpawnPooled("bonus_xp_orb", CreateBonusXpOrb);
            bonusXpObject.transform.SetParent(worldRoot, false);
            bonusXpObject.transform.position = position + UnityEngine.Random.insideUnitCircle * 0.45f;
            bonusXpObject.GetComponent<ZombieStormPickup>().Initialize(this, "bonus_xp_orb", bonusXp, 0f);
        }
    }

    // Spawns a healing potion pickup.
    private void SpawnHealthPotion(Vector2 position, float healAmount)
    {
        GameObject potionObject = SpawnPooled("health_potion", CreateHealthPotion);
        potionObject.transform.SetParent(worldRoot, false);
        potionObject.transform.position = position + UnityEngine.Random.insideUnitCircle * 0.42f;
        potionObject.GetComponent<ZombieStormPickup>().Initialize(this, "health_potion", 0, healAmount);
    }

    // Processes every consequence of an enemy death: kill count, score feedback, blood,
    // XP rewards, potion drops, boss rewards, healing, and temporary messages.
    public void OnEnemyKilled(ZombieStormEnemy enemy)
    {
        if (Player != null)
        {
            Player.Kills++;
        }

        string deathSfx = UnityEngine.Random.value < 0.5f ? "enemy_death" : "enemy_death_alt";
        PlaySfx(deathSfx, enemy.IsBoss ? 0.84f : 0.58f, 0.045f);
        if (enemy.Type == ZombieStormEnemyType.Elite || enemy.IsBoss)
        {
            PlaySfx(enemy.IsBoss ? "boss_down" : "elite_down", 0.75f, 0.1f);
        }

        int xp = enemy.IsBoss ? BossXpReward(enemy.Type) : enemy.Type == ZombieStormEnemyType.Elite ? 24 : enemy.Type == ZombieStormEnemyType.Reaper ? 10 : enemy.Type == ZombieStormEnemyType.Tank ? 7 : enemy.Type == ZombieStormEnemyType.Gravedigger ? 8 : enemy.Type == ZombieStormEnemyType.OrcThrower ? 7 : enemy.Type == ZombieStormEnemyType.Slasher ? 6 : enemy.Type == ZombieStormEnemyType.SmallGoblin ? 3 : enemy.Type == ZombieStormEnemyType.Goblin ? 4 : enemy.Type == ZombieStormEnemyType.Spitter ? 6 : 3;
        int bonusXp = enemy.IsBoss ? BossBonusXpReward(enemy.Type) : enemy.Type == ZombieStormEnemyType.Elite ? 18 : UnityEngine.Random.value < 0.24f ? 1 : 0;
        SpawnBloodSplat(enemy.transform.position, enemy.IsBoss ? 2.8f : enemy.Type == ZombieStormEnemyType.Elite ? 1.8f : 1.0f);
        SpawnPickup(enemy.transform.position, xp, bonusXp);
        TryDropHealthPotion(enemy.transform.position);

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

    // Rolls a health potion drop on the kill after each five-kill streak.
    private void TryDropHealthPotion(Vector2 position)
    {
        if (healthPotionDropPending)
        {
            healthPotionDropPending = false;
            healthPotionKillCounter = 0;
            if (UnityEngine.Random.value < 0.5f)
            {
                SpawnHealthPotion(position, 30f);
            }

            return;
        }

        healthPotionKillCounter++;
        if (healthPotionKillCounter >= 5)
        {
            healthPotionDropPending = true;
        }
    }

    // Pauses active gameplay and opens the upgrade-choice state after the player levels up.
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

    // Ends the current run, records victory or defeat, freezes gameplay, and prepares
    // the appropriate result screen and feedback.
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

    // Returns the best available icon for a skill, preferring imported art and falling
    // back to generated sprites when a project asset is unavailable.
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

    // Returns the walk-animation frame for the requested direction and frame index,
    // wrapping the index so callers can advance animation time without checking bounds.
    public Sprite GetPlayerWalkFrame(string direction, int frameIndex)
    {
        Sprite[] frames;
        if (!playerWalkFrames.TryGetValue(direction, out frames) || frames == null || frames.Length == 0)
        {
            return playerSprite;
        }

        return frames[Mathf.Abs(frameIndex) % frames.Length];
    }

    // Returns the idle-animation frame for the requested direction and frame index,
    // falling back to the matching walk frame when no dedicated idle art is available.
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

    // Returns the hurt-animation frame for the requested direction and frame index,
    // with directional and walk-frame fallbacks when hurt art is incomplete.
    public Sprite GetPlayerHurtFrame(int frameIndex)
    {
        if (!HasPlayerHurtAnimation)
        {
            return playerSprite;
        }

        return playerHurtFrames[Mathf.Clamp(frameIndex, 0, playerHurtFrames.Length - 1)];
    }

    // Returns the reusable soft oval sprite used beneath characters and world objects.
    public Sprite GetSoftShadowSprite()
    {
        return softShadowSprite;
    }

    // Returns the reusable soft circular sprite used by lights, impacts, and aura effects.
    public Sprite GetSoftGlowSprite()
    {
        return softGlowSprite;
    }

    // Returns the generated energy-ring sprite displayed around orbiting Fire Blades.
    public Sprite GetOrbitRingSprite()
    {
        return orbitRingSprite != null ? orbitRingSprite : softGlowSprite;
    }

    // Returns the sprite used to draw simple world-space health bar fills.
    public Sprite GetHealthBarSprite()
    {
        return tileSprite;
    }

    // Returns the first frame of a registered projectile effect, or null when that
    // effect key has not been loaded.
    public Sprite GetProjectileEffectSprite()
    {
        return GetEffectPreviewSprite("foozle_fireball", 4, projectileFxSprite != null ? projectileFxSprite : bulletSprite);
    }

    // Returns all animation frames registered for a projectile effect key.
    public Sprite[] GetProjectileEffectFrames()
    {
        return GetEffectFrames("foozle_fireball");
    }

    // Returns the first imported Fire Zone bomb frame for use as its initial sprite.
    public Sprite GetFireBombEffectSprite()
    {
        return GetEffectPreviewSprite("fire_bomb", 5, fireSprite);
    }

    // Returns the complete imported animation used by the thrown Fire Zone bomb.
    public Sprite[] GetFireBombEffectFrames()
    {
        return GetEffectFrames("fire_bomb");
    }

    // Returns one of the imported ground-fire effect keys.
    public string GetRandomGroundFireEffectKey()
    {
        return GroundFireEffectKeys[UnityEngine.Random.Range(0, GroundFireEffectKeys.Length)];
    }

    // Returns the imported frame sequence used by the crystal boss's orb projectile.
    public Sprite[] GetIceBossOrbFrames()
    {
        return iceBossOrbFrames;
    }

    // Looks up an effect animation by key and returns an empty array when it is missing,
    // allowing callers to safely fall back to static visuals.
    public Sprite[] GetEffectFrames(string effectKey)
    {
        if (effectKey == "meteor_warning")
        {
            return null;
        }

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
        if (effectKey == "fire_pool" || effectKey.StartsWith("fire_pool_", StringComparison.Ordinal) || effectKey == "toxic_pool" || effectKey == "meteor_blast")
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

    // Rotates a 2D vector by the supplied angle in degrees.
    public static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    // Returns a copy of a color with its alpha channel replaced by the supplied value.
    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    // Receives the main-menu Start command and begins the story or gameplay flow.
    public void RequestStartRun()
    {
        BeginStoryOrRun();
    }

    // Opens the settings modal from the main menu without changing the run state.
    public void RequestOpenMainMenuSettings()
    {
        OpenSettings(ZombieStormFlowState.MainMenu);
    }

    // Closes the current settings modal and returns to the screen that opened it.
    public void RequestCloseSettings()
    {
        CloseSettings();
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
        UpdateMusicVolume();
    }

    // Starts the story sequence when available, otherwise starts gameplay immediately.
    private void BeginStoryOrRun()
    {
        if (storyPageTextures != null && storyPageTextures.Length > 0)
        {
            storyPageIndex = 0;
            flowState = ZombieStormFlowState.Story;
            settingsReturnState = ZombieStormFlowState.MainMenu;
            Time.timeScale = 0f;
            PlaySfx("story_transition", 0.62f, 0.08f);
            return;
        }

        StartRun();
    }

    // Advances the story sequence and starts the run after the final page.
    private void AdvanceStoryPage()
    {
        storyPageIndex++;
        if (storyPageTextures == null || storyPageIndex >= storyPageTextures.Length)
        {
            StartRun();
            return;
        }

        PlaySfx("story_transition", 0.62f, 0.08f);
    }

    // Resets all run-specific state, clears old pooled objects, creates the player and
    // skill manager, restores normal time, and enters active gameplay.
    private void StartRun()
    {
        runTime = 0f;
        spawnTimer = 1.35f;
        eliteTimer = 78f;
        feedbackTimer = 0f;
        bossCount = 0;
        leveling = false;
        finished = false;
        won = false;
        firstBossDefeated = false;
        hordeSealed = false;
        healthPotionKillCounter = 0;
        healthPotionDropPending = false;
        upgradeChoicesTaken = 0;
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

        FollowPlayer(true);
        PlaySfx("start", 0.56f, 0.1f);
        ShowFeedback("Wave 1: Magic Bolt online. Move, kite, collect XP.", 3f);
    }

    // Spawns the wide, non-clustered meteor barrage used by the third boss.
    public void SpawnFullArenaEmberMeteorBarrage(float damage, float radiusMultiplier, float fallDurationOffset)
    {
        Vector2 halfExtents = usingCustomArenaMap ? customArenaHalfExtents : new Vector2(22f, 12f);
        halfExtents = new Vector2(Mathf.Max(12f, halfExtents.x - 3f), Mathf.Max(7f, halfExtents.y - 2f));
        Vector2[] normalizedPositions =
        {
            new Vector2(-0.88f, 0.78f),
            new Vector2(-0.46f, 0.86f),
            new Vector2(0.02f, 0.72f),
            new Vector2(0.48f, 0.88f),
            new Vector2(0.88f, 0.74f),
            new Vector2(-0.74f, 0.28f),
            new Vector2(-0.18f, 0.36f),
            new Vector2(0.36f, 0.18f),
            new Vector2(0.76f, 0.34f),
            new Vector2(-0.9f, -0.2f),
            new Vector2(-0.42f, -0.42f),
            new Vector2(0.12f, -0.28f),
            new Vector2(0.62f, -0.48f),
            new Vector2(-0.68f, -0.82f),
            new Vector2(0.82f, -0.78f),
            new Vector2(-0.12f, 0.02f),
            new Vector2(0.9f, -0.08f),
            new Vector2(-0.28f, -0.9f),
            new Vector2(0.36f, 0.62f)
        };

        for (int i = 0; i < normalizedPositions.Length; i++)
        {
            float radius = (i % 4 == 0 ? 1.32f : i % 4 == 1 ? 1.12f : i % 4 == 2 ? 0.98f : 0.86f) * radiusMultiplier;
            float fallDuration = 3.25f + fallDurationOffset + (i % 5) * 0.16f;
            Vector2 position = new Vector2(normalizedPositions[i].x * halfExtents.x, normalizedPositions[i].y * halfExtents.y);
            SpawnEmberBossMeteorStrike(ClampToArena(position), damage, radius, fallDuration);
        }
    }

    // Pauses an active run by changing the flow state and setting the game time scale to zero.
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

    // Leaves the pause screen, restores active gameplay, and resumes normal game time.
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

    // Opens settings from the pause screen while remembering that gameplay is already paused.
    private void OpenSettings(ZombieStormFlowState returnState)
    {
        settingsReturnState = returnState;
        flowState = ZombieStormFlowState.Settings;
        Time.timeScale = 0f;
    }

    // Closes settings and returns either to the pause screen or the main menu.
    private void CloseSettings()
    {
        flowState = settingsReturnState;
        Time.timeScale = flowState == ZombieStormFlowState.Running ? 1f : 0f;
    }

    // Abandons the current run, clears temporary objects, restores normal time, and
    // returns to the main-menu state.
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
        storyPageIndex = 0;
        flowState = ZombieStormFlowState.MainMenu;
        settingsReturnState = ZombieStormFlowState.MainMenu;
        Time.timeScale = 0f;
    }

    // Creates the runtime-owned camera, roots, map, pooled object containers, menu UI,
    // event system, resources, and other scene objects required by the game.
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

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.pitch = 1f;
    }

    // Builds the floor, roads, and decorative scene objects.
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
        bonusXpSprite = CreatePixelSprite(new Color(1f, 0.73f, 0.15f), new Color(1f, 0.95f, 0.55f), 8, true);
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
        LoadStoryPageTextures();
        LoadUpgradeCardTemplate();
        LoadUpgradeCardFonts();
        LoadPlayerStatusCardTexture();
        LoadResultScreenTextures();
        LoadHealthPotionSprite();
        LoadFireSpiritSprite();
        LoadKenneyTopdownArt();
        LoadMikodrakSpellEffects();
        LoadIceBossOrbFrames();
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
            spawnTimer = Mathf.Max(0.28f, 1.85f - runTime / 210f) / GetEarlyGameReliefScale();
            float earlyCount = runTime < 35f ? 1f : runTime < 75f ? 1.65f : 2f + difficultyScore * 0.9f;
            if (!firstBossDefeated)
            {
                earlyCount *= runTime < 75f ? 0.94f : 0.88f;
            }

            int count = Mathf.Clamp(Mathf.RoundToInt(earlyCount), 1, 14);
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(ChooseEnemyType());
            }
        }

        if (eliteTimer <= 0f)
        {
            eliteTimer = Mathf.Max(34f, 58f - runTime / 14f) / GetEarlyGameReliefScale();
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

    // Gives the player a little room to build before the first boss is defeated.
    private float GetEarlyGameReliefScale()
    {
        return firstBossDefeated ? 1f : 0.82f;
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

        if (runTime > 45f && roll < (lowHealth ? 0.15f : 0.3f))
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
        enemy.Initialize(this, enemyType, key, GetEnemySprite(enemyType, walkFrames), walkFrames, GetEnemyAttackFrames(enemyType), GetEnemySpecialAttackFrames(enemyType), GetEnemyHurtFrames(enemyType), GetEnemyDeathFrames(enemyType), framesFaceRight, runTime, difficultyScore, firstBossDefeated ? 1f : 0.9f);
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

    // Creates a pooled bonus XP pickup object.
    private GameObject CreateBonusXpOrb()
    {
        GameObject item = new GameObject("Bonus XP Orb");
        AddGlow(item.transform, new Color(1f, 0.75f, 0.08f, 0.32f), Vector3.one * 1.75f, 22);
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = bonusXpSprite;
        spriteRenderer.sortingOrder = 24;
        item.AddComponent<ZombieStormPickup>();
        return item;
    }

    // Creates a pooled health potion pickup object.
    private GameObject CreateHealthPotion()
    {
        GameObject item = new GameObject("Health Potion");
        AddGlow(item.transform, new Color(1f, 0.12f, 0.2f, 0.34f), Vector3.one * 1.9f, 22);
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = healthPotionSprite != null ? healthPotionSprite : fireSprite;
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

    // Returns the bonus XP reward for a defeated boss type.
    private static int BossBonusXpReward(ZombieStormEnemyType bossType)
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
        return true;
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
}
