using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum ZombieStormSkillType
{
    MagicBolt,
    OrbitingKnife,
    MeteorStorm,
    FireZone,
    SummonDrone,
    ChainLightning,
    ShieldBurst,
    UltimateStorm
}

public enum ZombieStormEnemyType
{
    Grunt,
    Fast,
    Tank,
    Exploder,
    Spitter,
    Elite,
    Boss
}

public enum ZombieStormPassiveType
{
    Damage,
    FireRate,
    Area,
    MoveSpeed,
    PickupRange,
    Crit,
    MaxHealth,
    CoinGain
}

public enum ZombieStormWave
{
    Sine,
    Square,
    Triangle,
    Saw,
    Noise
}

[DefaultExecutionOrder(-100)]
public sealed class ZombieStormGameController : MonoBehaviour
{
    public static ZombieStormGameController Instance { get; private set; }

    private const string Title = "\u50f5\u5c38\u5272\u8349\u5927\u4f5c\u6218";

    [Header("Run")]
    public float runDurationSeconds = 300f;
    public int targetFrameRate = 120;

    public ZombieStormPlayer Player { get; private set; }
    public ZombieStormSkillManager Skills { get; private set; }
    public IReadOnlyList<ZombieStormEnemy> Enemies { get { return enemies; } }

    private readonly List<ZombieStormEnemy> enemies = new List<ZombieStormEnemy>(256);
    private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<ZombieStormPassiveType, int> passives = new Dictionary<ZombieStormPassiveType, int>();
    private readonly Dictionary<string, AudioClip> sfx = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> sfxLastPlayed = new Dictionary<string, float>();
    private readonly List<ZombieStormUpgradeOption> currentChoices = new List<ZombieStormUpgradeOption>(3);
    private readonly HashSet<string> choiceKeys = new HashSet<string>();
    private readonly Dictionary<string, Sprite[]> playerWalkFrames = new Dictionary<string, Sprite[]>();
    private readonly Dictionary<string, Sprite[]> effectFrames = new Dictionary<string, Sprite[]>();
    private readonly List<ZombieStormDamagePopup> damagePopups = new List<ZombieStormDamagePopup>();
    private Sprite[] playerHurtFrames = new Sprite[0];
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
    private Sprite xpSprite;
    private Sprite coinSprite;
    private Sprite fireSprite;
    private Sprite sawSprite;
    private Sprite mineSprite;
    private Sprite tileSprite;
    private Sprite ruinSprite;
    private Sprite softShadowSprite;
    private Sprite softGlowSprite;
    private Sprite bloodSplatSprite;
    private Sprite neonSignSprite;
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
    private bool leveling;
    private bool finished;
    private bool won;
    private int bossCount;
    private string feedbackText = "WASD move. Skills cast automatically. Press F for ultimate.";

    public float DamageMultiplier { get { return 1f + GetPassiveLevel(ZombieStormPassiveType.Damage) * 0.18f; } }
    public float CooldownMultiplier { get { return Mathf.Max(0.35f, 1f - GetPassiveLevel(ZombieStormPassiveType.FireRate) * 0.08f); } }
    public float AreaMultiplier { get { return 1f + GetPassiveLevel(ZombieStormPassiveType.Area) * 0.16f; } }
    public float CritChance { get { return Mathf.Clamp01(GetPassiveLevel(ZombieStormPassiveType.Crit) * 0.07f); } }
    public float CoinMultiplier { get { return 1f + GetPassiveLevel(ZombieStormPassiveType.CoinGain) * 0.2f; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        if (FindObjectOfType<ZombieStormGameController>() != null)
        {
            return;
        }

        GameObject bootstrap = new GameObject("Zombie Storm Bootstrap");
        bootstrap.AddComponent<ZombieStormGameController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Application.targetFrameRate = targetFrameRate;
        Physics2D.gravity = Vector2.zero;
        Time.timeScale = 1f;

        CreateSprites();
        BuildScene();
        CreateAudioClips();
        StartRun();
    }

    private void Update()
    {
        if (finished)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartRun();
            }

            return;
        }

        if (leveling)
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
        FollowPlayer();

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

    private void OnGUI()
    {
        DrawAtmosphereOverlay();
        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
        DrawPanel(new Rect(12f, 10f, 430f, 158f), new Color(0.035f, 0.045f, 0.055f, 0.82f), new Color(0.2f, 0.75f, 1f, 0.32f));
        GUI.Label(new Rect(24f, 18f, 760f, 28f), Title + " / Zombie Storm");
        GUI.skin.label.fontSize = 14;
        GUI.color = new Color(0.78f, 0.86f, 0.92f, 1f);
        GUI.Label(new Rect(24f, 45f, 420f, 24f), "WASD move | Auto skills | F ultimate | 1/2/3 upgrade | Enter restart");
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
        DrawPanel(new Rect(Screen.width - 224f, 10f, 206f, 48f), new Color(0.035f, 0.045f, 0.055f, 0.82f), new Color(1f, 0.85f, 0.25f, 0.28f));
        GUI.skin.label.fontSize = 24;
        GUI.Label(new Rect(Screen.width - 206f, 18f, 190f, 32f), "Survive " + FormatTime(remain));
        GUI.skin.label.fontSize = 18;

        if (Skills != null)
        {
            DrawPanel(new Rect(Screen.width - 286f, 70f, 268f, 150f), new Color(0.035f, 0.045f, 0.055f, 0.74f), new Color(0.9f, 0.28f, 0.2f, 0.24f));
            GUI.skin.label.fontSize = 15;
            GUI.Label(new Rect(Screen.width - 268f, 84f, 244f, 124f), Skills.GetLoadoutText());
            GUI.skin.label.fontSize = 18;
        }

        if (Time.unscaledTime < feedbackUntil)
        {
            DrawPanel(new Rect(Screen.width * 0.5f - 330f, 78f, 680f, 46f), new Color(0.05f, 0.045f, 0.02f, 0.82f), new Color(1f, 0.75f, 0.18f, 0.45f));
            GUI.skin.label.fontSize = 22;
            GUI.color = new Color(1f, 0.86f, 0.25f, 1f);
            GUI.Label(new Rect(Screen.width * 0.5f - 310f, 84f, 660f, 40f), feedbackText);
            GUI.skin.label.fontSize = 18;
            GUI.color = Color.white;
        }

        if (leveling)
        {
            DrawUpgradePanel();
        }

        DrawBossBar();
        DrawEliteMarkers();
        DrawDamagePopups();
        DrawScreenFlash();

        if (finished)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.74f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 36;
            GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f - 86f, 520f, 60f), won ? "Survival Victory" : "Run Failed");
            GUI.skin.label.fontSize = 20;
            int kills = Player != null ? Player.Kills : 0;
            int coins = Player != null ? Player.Coins : 0;
            int level = Player != null ? Player.Level : 1;
            GUI.Label(new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.5f - 28f, 620f, 100f), "Kills " + kills + " | Coins " + coins + " | Level " + level);
            GUI.Label(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f + 40f, 460f, 40f), "Press Enter to restart.");
            GUI.skin.label.fontSize = 18;
        }
    }

    public int GetPassiveLevel(ZombieStormPassiveType passive)
    {
        int level;
        return passives.TryGetValue(passive, out level) ? level : 0;
    }

    public void RegisterEnemy(ZombieStormEnemy enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(ZombieStormEnemy enemy)
    {
        enemies.Remove(enemy);
    }

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

    public void SpawnPlayerProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life, int pierce, Color color, float size)
    {
        GameObject projectileObject = SpawnPooled("player_bullet", CreatePlayerProjectile);
        projectileObject.transform.SetParent(worldRoot, false);
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * size;
        SpriteRenderer spriteRenderer = projectileObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetProjectileEffectSprite();
        spriteRenderer.color = color;
        ZombieStormProjectile projectile = projectileObject.GetComponent<ZombieStormProjectile>();
        projectile.Initialize(this, direction, damage, speed, life, pierce);
        PlaySfx("shoot", 0.28f, 0.055f);
    }

    public void SpawnEnemyProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life)
    {
        GameObject projectileObject = SpawnPooled("enemy_spit", CreateEnemyProjectile);
        projectileObject.transform.SetParent(worldRoot, false);
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * 0.28f;
        ZombieStormEnemyProjectile projectile = projectileObject.GetComponent<ZombieStormEnemyProjectile>();
        projectile.Initialize(this, direction, damage, speed, life);
    }

    public void SpawnAreaEffect(Vector2 position, float radius, float damage, float duration, float tickRate, Color color, string poolKey)
    {
        GameObject effectObject = SpawnPooled(poolKey, CreateAreaEffect);
        effectObject.transform.SetParent(worldRoot, false);
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer spriteRenderer = effectObject.GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = poolKey == "hit_spark" || poolKey == "lightning_flash" ? 48 : 14;
        ZombieStormAreaEffect effect = effectObject.GetComponent<ZombieStormAreaEffect>();
        effect.Initialize(this, poolKey, radius, damage, duration, tickRate);
    }

    public void SpawnHitSpark(Vector2 position, Color color, float radius = 0.36f)
    {
        SpawnAreaEffect(position, radius, 0f, 0.12f, 1f, color, "hit_spark");
        PlaySfx("hit", 0.2f + Mathf.Clamp01(radius) * 0.18f, 0.045f);
    }

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

    public void FlashScreen(float amount)
    {
        screenFlashColor = new Color(1f, 0.08f, 0.04f);
        screenFlash = Mathf.Max(screenFlash, amount);
    }

    public void FlashScreen(Color color, float amount)
    {
        screenFlashColor = color;
        screenFlash = Mathf.Max(screenFlash, amount);
    }

    public void ShakeCamera(float power, float duration)
    {
        cameraShakePower = Mathf.Max(cameraShakePower, power);
        cameraShakeTime = Mathf.Max(cameraShakeTime, duration);
    }

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

    public void OnEnemyKilled(ZombieStormEnemy enemy)
    {
        if (Player != null)
        {
            Player.Kills++;
        }

        if (enemy.Type == ZombieStormEnemyType.Elite || enemy.Type == ZombieStormEnemyType.Boss)
        {
            PlaySfx(enemy.Type == ZombieStormEnemyType.Boss ? "boss_down" : "elite_down", 0.75f, 0.1f);
        }

        int xp = enemy.Type == ZombieStormEnemyType.Boss ? 55 : enemy.Type == ZombieStormEnemyType.Elite ? 24 : enemy.Type == ZombieStormEnemyType.Tank ? 7 : enemy.Type == ZombieStormEnemyType.Spitter ? 6 : 3;
        int coins = enemy.Type == ZombieStormEnemyType.Boss ? 45 : enemy.Type == ZombieStormEnemyType.Elite ? 18 : UnityEngine.Random.value < 0.24f ? 1 : 0;
        SpawnBloodSplat(enemy.transform.position, enemy.Type == ZombieStormEnemyType.Boss ? 2.8f : enemy.Type == ZombieStormEnemyType.Elite ? 1.8f : 1.0f);
        SpawnPickup(enemy.transform.position, xp, coins);

        if (enemy.Type == ZombieStormEnemyType.Elite)
        {
            ShowFeedback("Elite down. Big XP dropped.", 2.5f);
        }

        if (enemy.Type == ZombieStormEnemyType.Boss && Player != null)
        {
            Player.Heal(24f);
            ShowFeedback("Boss defeated. The horde breaks for a moment.", 3f);
        }
    }

    public void RequestLevelUp()
    {
        if (leveling || finished)
        {
            return;
        }

        leveling = true;
        Time.timeScale = 0f;
        currentChoices.Clear();
        BuildUpgradeChoices();
        PlaySfx("level_up", 0.86f, 0.1f);
        ShowFeedback("Level up. Pick a build direction.", 2f);
    }

    public void EndRun(bool victory, string message)
    {
        if (finished)
        {
            return;
        }

        won = victory;
        finished = true;
        Time.timeScale = 0f;
        PlaySfx(victory ? "victory" : "fail", 0.9f, 0.1f);
        ShowFeedback(message, 999f);
    }

    public Sprite GetSkillSprite(ZombieStormSkillType skillType)
    {
        if (skillType == ZombieStormSkillType.OrbitingKnife)
        {
            return sawSprite;
        }

        if (skillType == ZombieStormSkillType.SummonDrone || skillType == ZombieStormSkillType.ShieldBurst)
        {
            return mineSprite;
        }

        if (skillType == ZombieStormSkillType.FireZone || skillType == ZombieStormSkillType.MeteorStorm)
        {
            return fireSprite;
        }

        return bulletSprite;
    }

    public bool HasPlayerWalkAnimation
    {
        get { return playerWalkFrames.Count > 0; }
    }

    public Sprite GetPlayerWalkFrame(string direction, int frameIndex)
    {
        Sprite[] frames;
        if (!playerWalkFrames.TryGetValue(direction, out frames) || frames == null || frames.Length == 0)
        {
            return playerSprite;
        }

        return frames[Mathf.Abs(frameIndex) % frames.Length];
    }

    public bool HasPlayerHurtAnimation
    {
        get { return playerHurtFrames != null && playerHurtFrames.Length > 0; }
    }

    public Sprite GetPlayerHurtFrame(int frameIndex)
    {
        if (!HasPlayerHurtAnimation)
        {
            return playerSprite;
        }

        return playerHurtFrames[Mathf.Abs(frameIndex) % playerHurtFrames.Length];
    }

    public Sprite GetSoftShadowSprite()
    {
        return softShadowSprite;
    }

    public Sprite GetHealthBarSprite()
    {
        return tileSprite;
    }

    public Sprite GetProjectileEffectSprite()
    {
        return projectileFxSprite != null ? projectileFxSprite : bulletSprite;
    }

    public Sprite[] GetEffectFrames(string effectKey)
    {
        if (effectFrames.Count == 0)
        {
            return null;
        }

        string sequenceKey = "spark";
        if (effectKey == "fire_pool" || effectKey == "toxic_pool" || effectKey == "meteor_blast" || effectKey == "meteor_warning")
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

        Sprite[] frames;
        if (effectFrames.TryGetValue(sequenceKey, out frames) && frames != null && frames.Length > 0)
        {
            return frames;
        }

        return null;
    }

    public static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

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
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

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
        difficultyScore = 1f;
        Time.timeScale = 1f;

        ClearActiveObjects();
        BuildEnvironment();

        GameObject playerObject = new GameObject("Pixel Survivor");
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
        mainCamera.orthographicSize = 8f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.035f, 0.04f, 0.052f);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.62f;
    }

    private void BuildEnvironment()
    {
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

    private void CreateSprites()
    {
        playerSprite = CreateSurvivorSprite();
        LoadCowboyWalkFrames();
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
        sawSprite = CreatePixelSprite(new Color(0.82f, 0.84f, 0.9f), new Color(0.2f, 0.75f, 1f), 14, true);
        mineSprite = CreatePixelSprite(new Color(0.22f, 0.22f, 0.25f), new Color(1f, 0.18f, 0.08f), 12, true);
        tileSprite = CreatePixelSprite(Color.white, Color.white, 8, false);
        ruinSprite = CreatePixelSprite(Color.white, new Color(0.06f, 0.06f, 0.08f), 12, false);
        softShadowSprite = CreateSoftDiscSprite(new Color(0f, 0f, 0f, 0.58f), 64, 1f, 0.34f);
        softGlowSprite = CreateSoftDiscSprite(new Color(1f, 1f, 1f, 0.72f), 64, 1f, 0.08f);
        bloodSplatSprite = CreateBloodSplatSprite();
        neonSignSprite = CreateNeonSignSprite();
        LoadKenneyTopdownArt();
        LoadMikodrakSpellEffects();
    }

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

    private static float NextNoise(ref uint state)
    {
        state = state * 1664525u + 1013904223u;
        return ((state >> 8) / 16777215f) * 2f - 1f;
    }

    private void UpdateDynamicDifficulty()
    {
        float timeFactor = 0.62f + runTime / 92f;
        float lowHealthMercy = Player != null && Player.Health / Player.MaxHealth < 0.42f ? 0.68f : 1f;
        float dominance = Player != null && runTime > 45f && Player.Kills > runTime * 1.15f ? 1.16f : 1f;
        difficultyScore = Mathf.Clamp(timeFactor * lowHealthMercy * dominance, 0.55f, 8f);
    }

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
            SpawnEnemy(ZombieStormEnemyType.Elite);
            ShowFeedback("Elite zombie incoming. Kill it for a reward burst.", 2.5f);
        }

        if ((bossCount == 0 && runTime >= 150f) || (bossCount == 1 && runTime >= 260f))
        {
            bossCount++;
            SpawnEnemy(ZombieStormEnemyType.Boss);
            ShowFeedback("Boss wave. Watch the phase attacks.", 3f);
        }
    }

    private ZombieStormEnemyType ChooseEnemyType()
    {
        float roll = UnityEngine.Random.value;
        bool lowHealth = Player != null && Player.Health / Player.MaxHealth < 0.28f;

        if (runTime > 95f && roll < 0.08f)
        {
            return ZombieStormEnemyType.Exploder;
        }

        if (runTime > 95f && roll < (lowHealth ? 0.08f : 0.16f))
        {
            return ZombieStormEnemyType.Spitter;
        }

        if (runTime > 70f && roll < 0.3f)
        {
            return ZombieStormEnemyType.Tank;
        }

        if (runTime > 45f && roll < (lowHealth ? 0.22f : 0.38f))
        {
            return ZombieStormEnemyType.Fast;
        }

        return ZombieStormEnemyType.Grunt;
    }

    private void SpawnEnemy(ZombieStormEnemyType enemyType)
    {
        string key = "enemy_" + enemyType;
        GameObject enemyObject = SpawnPooled(key, CreateEnemy);
        enemyObject.name = "Zombie " + enemyType;
        enemyObject.transform.SetParent(worldRoot, false);
        enemyObject.transform.position = GetOffscreenSpawnPosition();
        ZombieStormEnemy enemy = enemyObject.GetComponent<ZombieStormEnemy>();
        enemy.Initialize(this, enemyType, key, GetEnemySprite(enemyType), runTime, difficultyScore);
    }

    private Vector2 GetOffscreenSpawnPosition()
    {
        Vector2 center = Player != null ? Player.transform.position : Vector3.zero;
        Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.up;
        }

        float spawnDistance = mainCamera != null ? mainCamera.orthographicSize * 1.65f + 3f : 16f;
        return center + direction * spawnDistance;
    }

    private Sprite GetEnemySprite(ZombieStormEnemyType enemyType)
    {
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

        return zombieSprite;
    }

    private GameObject CreateEnemy()
    {
        GameObject item = new GameObject("Pooled Zombie");
        AddShadow(item.transform, new Vector3(1.4f, 0.46f, 1f), -0.08f, 17);
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 20;
        item.AddComponent<ZombieStormEnemy>();
        return item;
    }

    private GameObject CreatePlayerProjectile()
    {
        GameObject item = new GameObject("Player Bullet");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetProjectileEffectSprite();
        spriteRenderer.sortingOrder = 40;
        item.AddComponent<ZombieStormProjectile>();
        return item;
    }

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

    private GameObject CreateAreaEffect()
    {
        GameObject item = new GameObject("Area Effect");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = fireSprite;
        spriteRenderer.sortingOrder = 12;
        item.AddComponent<ZombieStormAreaEffect>();
        return item;
    }

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

    private GameObject CreateBloodSplat()
    {
        GameObject item = new GameObject("Blood Splat");
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = bloodSplatSprite;
        spriteRenderer.sortingOrder = -3;
        item.AddComponent<ZombieStormTimedPooled>();
        return item;
    }

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

    private void AddUpgradeChoice(ZombieStormUpgradeOption option)
    {
        if (option != null && choiceKeys.Add(option.Key))
        {
            currentChoices.Add(option);
        }
    }

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

    private bool TryGetRandomKnownSkill(out ZombieStormSkillType weaponType)
    {
        Array values = Enum.GetValues(typeof(ZombieStormSkillType));
        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            ZombieStormSkillType candidate = (ZombieStormSkillType)values.GetValue(i);
            if (Skills.GetSkillLevel(candidate) > 0)
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
            if (Skills.GetSkillLevel(candidate) <= 0)
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

    private ZombieStormUpgradeOption CreateUnlockSkillOption()
    {
        Array values = Enum.GetValues(typeof(ZombieStormSkillType));
        for (int guard = 0; guard < 24; guard++)
        {
            ZombieStormSkillType weaponType = (ZombieStormSkillType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            if (Skills.GetSkillLevel(weaponType) <= 0)
            {
                return ZombieStormUpgradeOption.Skill("unlock_" + weaponType, "Learn " + SkillName(weaponType), SkillSummary(weaponType), SkillAccent(weaponType), delegate { Skills.LearnSkill(weaponType); });
            }
        }

        return null;
    }

    private ZombieStormUpgradeOption CreateSkillLevelOption(ZombieStormSkillType weaponType)
    {
        int level = Skills.GetSkillLevel(weaponType);
        if (level <= 0 || level >= 5)
        {
            return null;
        }

        return ZombieStormUpgradeOption.Skill("level_" + weaponType, SkillName(weaponType) + " Lv." + (level + 1), SkillLevelSummary(weaponType, level + 1), SkillAccent(weaponType), delegate { Skills.LevelUpSkill(weaponType); });
    }

    private ZombieStormUpgradeOption CreateSkillSpecializationOption(ZombieStormSkillType weaponType)
    {
        string[] keys = SkillUpgradeKeys(weaponType);
        for (int guard = 0; guard < 18; guard++)
        {
            string key = keys[UnityEngine.Random.Range(0, keys.Length)];
            int level = Skills.GetSkillUpgradeLevel(key);
            if (level >= 3)
            {
                continue;
            }

            int nextLevel = level + 1;
            string category = SkillName(weaponType).ToUpperInvariant() + " BUILD";
            return ZombieStormUpgradeOption.Custom("special_" + key, SkillUpgradeName(key) + " Lv." + nextLevel, SkillUpgradeSummary(key, nextLevel), category, SkillAccent(weaponType), delegate { Skills.AddSkillUpgrade(key); });
        }

        return null;
    }

    private ZombieStormUpgradeOption CreatePassiveOption(ZombieStormPassiveType passive)
    {
        int level = GetPassiveLevel(passive);
        if (level >= 5)
        {
            return null;
        }

        return ZombieStormUpgradeOption.Passive("passive_" + passive, PassiveName(passive) + " Lv." + (level + 1), PassiveSummary(passive, level + 1), PassiveAccent(passive), delegate { AddPassive(passive); });
    }

    private void AddFallbackPassive(ZombieStormPassiveType passive)
    {
        ZombieStormUpgradeOption option = CreatePassiveOption(passive);
        if (option != null && choiceKeys.Add(option.Key))
        {
            currentChoices.Add(option);
        }
    }

    private void AddPassive(ZombieStormPassiveType passive)
    {
        passives[passive] = Mathf.Min(5, GetPassiveLevel(passive) + 1);
        if (passive == ZombieStormPassiveType.MaxHealth && Player != null)
        {
            Player.IncreaseMaxHealth(16f);
        }

        CheckEvolutions();
    }

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
        Time.timeScale = 1f;
        PlaySfx("upgrade", 0.9f, 0.1f);
        PlayUpgradeBurst(option);
        ShowFeedback(option.Title + " acquired.", 2.2f);
    }

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

    private void CheckEvolutions()
    {
        if (Skills == null)
        {
            return;
        }

        TryEvolve(ZombieStormSkillType.MagicBolt, ZombieStormPassiveType.FireRate, "Arcane Barrage evolved.");
        TryEvolve(ZombieStormSkillType.OrbitingKnife, ZombieStormPassiveType.MaxHealth, "Blade Halo evolved.");
        TryEvolve(ZombieStormSkillType.MeteorStorm, ZombieStormPassiveType.Area, "Judgement Meteor evolved.");
        TryEvolve(ZombieStormSkillType.ChainLightning, ZombieStormPassiveType.Crit, "Storm Chain evolved.");
        TryEvolve(ZombieStormSkillType.SummonDrone, ZombieStormPassiveType.Damage, "Drone Swarm evolved.");
    }

    private void TryEvolve(ZombieStormSkillType weapon, ZombieStormPassiveType passive, string message)
    {
        if (Skills.GetSkillLevel(weapon) >= 5 && GetPassiveLevel(passive) > 0 && !Skills.IsEvolved(weapon))
        {
            Skills.Evolve(weapon);
            ShowFeedback(message, 3f);
        }
    }

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

    private void DrawUpgradePanel()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.84f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(0.12f, 0.75f, 1f, 0.12f);
        GUI.DrawTexture(new Rect(0f, Screen.height * 0.5f - 170f, Screen.width, 360f), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.9f, 0.35f, 1f);
        GUI.skin.label.fontSize = 34;
        GUI.Label(new Rect(Screen.width * 0.5f - 188f, Screen.height * 0.5f - 184f, 460f, 48f), "LEVEL UP");
        GUI.color = new Color(0.82f, 0.9f, 1f, 1f);
        GUI.skin.label.fontSize = 16;
        GUI.Label(new Rect(Screen.width * 0.5f - 198f, Screen.height * 0.5f - 140f, 520f, 26f), "Choose one upgrade to shape this run");
        GUI.skin.label.fontSize = 18;

        float startX = Screen.width * 0.5f - 390f;
        float y = Screen.height * 0.5f - 92f;
        for (int i = 0; i < currentChoices.Count; i++)
        {
            Rect rect = new Rect(startX + i * 260f, y, 240f, 178f);
            Color edge = i == 0 ? new Color(0.2f, 0.75f, 1f, 0.72f) : i == 1 ? new Color(1f, 0.75f, 0.18f, 0.72f) : new Color(1f, 0.2f, 0.5f, 0.72f);
            edge = currentChoices[i].Accent;
            edge.a = 0.72f;
            DrawPanel(rect, new Color(0.045f, 0.055f, 0.07f, 0.98f), edge);
            GUI.color = new Color(edge.r, edge.g, edge.b, 0.16f);
            GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 46f), Texture2D.whiteTexture);
            GUI.color = edge;
            GUI.DrawTexture(new Rect(rect.x + 16f, rect.y + 18f, 30f, 30f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 20;
            GUI.Label(new Rect(rect.x + 24f, rect.y + 19f, 30f, 26f), (i + 1).ToString());
            GUI.skin.label.fontSize = 17;
            GUI.Label(new Rect(rect.x + 58f, rect.y + 17f, rect.width - 76f, 34f), currentChoices[i].Title);
            GUI.skin.label.fontSize = 12;
            GUI.color = new Color(edge.r, edge.g, edge.b, 1f);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 54f, rect.width - 36f, 18f), currentChoices[i].Category);
            GUI.skin.label.fontSize = 15;
            GUI.color = new Color(0.78f, 0.85f, 0.92f, 1f);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 72f, rect.width - 36f, 64f), currentChoices[i].Description);
            GUI.color = Color.white;
            if (GUI.Button(new Rect(rect.x + 42f, rect.y + 136f, 156f, 30f), "Pick " + (i + 1)))
            {
                ApplyUpgrade(i);
            }
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

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

    private void DrawAtmosphereOverlay()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.18f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 34f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, Screen.height - 42f, Screen.width, 42f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, 0f, 34f, Screen.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - 34f, 0f, 34f, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

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

    private void DrawBossBar()
    {
        ZombieStormEnemy boss = null;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].Type == ZombieStormEnemyType.Boss && !enemies[i].IsDead)
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
        DrawPanel(new Rect(rect.x - 10f, rect.y - 28f, rect.width + 20f, 58f), new Color(0.04f, 0.018f, 0.018f, 0.82f), new Color(1f, 0.12f, 0.08f, 0.5f));
        GUI.skin.label.fontSize = 16;
        GUI.color = new Color(1f, 0.58f, 0.42f, 1f);
        GUI.Label(new Rect(rect.x, rect.y - 24f, rect.width, 22f), "BOSS HORDE ALPHA");
        DrawBar(rect, boss.Health01, new Color(0.9f, 0.08f, 0.05f), Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaxHealth));
        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

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

    private void FollowPlayer(bool snap = false)
    {
        if (mainCamera == null || Player == null)
        {
            return;
        }

        Vector3 target = new Vector3(Player.transform.position.x, Player.transform.position.y, -10f);
        if (cameraShakeTime > 0f)
        {
            cameraShakeTime -= Time.deltaTime;
            Vector2 shake = UnityEngine.Random.insideUnitCircle * cameraShakePower;
            target += new Vector3(shake.x, shake.y, 0f);
            cameraShakePower = Mathf.Lerp(cameraShakePower, 0f, 7f * Time.deltaTime);
        }

        mainCamera.transform.position = snap ? target : Vector3.Lerp(mainCamera.transform.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

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

    private void ShowFeedback(string message, float seconds)
    {
        feedbackText = message;
        feedbackUntil = Time.unscaledTime + seconds;
    }

    private static string FormatTime(int seconds)
    {
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }

    private static string SkillName(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Magic Bolt";
            case ZombieStormSkillType.OrbitingKnife: return "Orbiting Knives";
            case ZombieStormSkillType.MeteorStorm: return "Meteor Storm";
            case ZombieStormSkillType.FireZone: return "Fire Zone";
            case ZombieStormSkillType.SummonDrone: return "Summon Drone";
            case ZombieStormSkillType.ChainLightning: return "Chain Lightning";
            case ZombieStormSkillType.ShieldBurst: return "Shield Burst";
            case ZombieStormSkillType.UltimateStorm: return "Ultimate: Full-Screen Thunder";
            default: return weapon.ToString();
        }
    }

    private static string SkillSummary(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Adds auto magic shots with a bright launch spark.";
            case ZombieStormSkillType.OrbitingKnife: return "Adds visible blades that orbit and cut nearby enemies.";
            case ZombieStormSkillType.MeteorStorm: return "Adds warning circles, then delayed impact blasts.";
            case ZombieStormSkillType.FireZone: return "Adds burning ground pools with ember flashes.";
            case ZombieStormSkillType.SummonDrone: return "Adds an AI drone that circles you and shoots targets.";
            case ZombieStormSkillType.ChainLightning: return "Adds jumping lightning with blue links between enemies.";
            case ZombieStormSkillType.ShieldBurst: return "Adds a close-range defensive shockwave trigger.";
            case ZombieStormSkillType.UltimateStorm: return "Adds one ultimate. Press F for a full-screen storm.";
            default: return "Adds another automatic skill.";
        }
    }

    private static string SkillLevelSummary(ZombieStormSkillType weapon, int nextLevel)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Lv." + nextLevel + ": faster bolts, more damage, extra pierce.";
            case ZombieStormSkillType.OrbitingKnife: return "Lv." + nextLevel + ": more blades, wider orbit, stronger ticks.";
            case ZombieStormSkillType.MeteorStorm: return "Lv." + nextLevel + ": more impacts, bigger warning circles.";
            case ZombieStormSkillType.FireZone: return "Lv." + nextLevel + ": larger pools, longer burn duration.";
            case ZombieStormSkillType.SummonDrone: return "Lv." + nextLevel + ": more drones and faster AI fire.";
            case ZombieStormSkillType.ChainLightning: return "Lv." + nextLevel + ": more jumps and stronger chain damage.";
            case ZombieStormSkillType.ShieldBurst: return "Lv." + nextLevel + ": larger defensive ring and harder hit.";
            case ZombieStormSkillType.UltimateStorm: return "Lv." + nextLevel + ": stronger F ultimate, shorter cooldown.";
            default: return "Lv." + nextLevel + ": improves this automatic skill.";
        }
    }

    private static string[] SkillUpgradeKeys(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return new[] { "magic_force", "magic_split", "magic_pierce" };
            case ZombieStormSkillType.OrbitingKnife: return new[] { "knife_blades", "knife_reach", "knife_edge" };
            case ZombieStormSkillType.MeteorStorm: return new[] { "meteor_impacts", "meteor_blast", "meteor_heat" };
            case ZombieStormSkillType.FireZone: return new[] { "fire_spread", "fire_linger", "fire_heat" };
            case ZombieStormSkillType.SummonDrone: return new[] { "drone_swarm", "drone_focus", "drone_overclock" };
            case ZombieStormSkillType.ChainLightning: return new[] { "lightning_jumps", "lightning_reach", "lightning_voltage", "lightning_tempo" };
            case ZombieStormSkillType.ShieldBurst: return new[] { "shield_radius", "shield_force", "shield_recharge" };
            case ZombieStormSkillType.UltimateStorm: return new[] { "ultimate_voltage", "ultimate_radius", "ultimate_recharge" };
            default: return new[] { "magic_force" };
        }
    }

    private static string SkillUpgradeName(string key)
    {
        switch (key)
        {
            case "magic_force": return "Focused Mana";
            case "magic_split": return "Split Casting";
            case "magic_pierce": return "Piercing Glyph";
            case "knife_blades": return "Extra Blades";
            case "knife_reach": return "Wide Orbit";
            case "knife_edge": return "Sharper Edge";
            case "meteor_impacts": return "Meteor Shower";
            case "meteor_blast": return "Impact Crater";
            case "meteor_heat": return "Molten Core";
            case "fire_spread": return "Wildfire";
            case "fire_linger": return "Lingering Flame";
            case "fire_heat": return "Hotter Burn";
            case "drone_swarm": return "Drone Bay";
            case "drone_focus": return "Targeting Lens";
            case "drone_overclock": return "Overclock";
            case "lightning_jumps": return "Forked Current";
            case "lightning_reach": return "Arc Range";
            case "lightning_voltage": return "High Voltage";
            case "lightning_tempo": return "Fast Charge";
            case "shield_radius": return "Wider Guard";
            case "shield_force": return "Repulsion Core";
            case "shield_recharge": return "Quick Recharge";
            case "ultimate_voltage": return "Storm Voltage";
            case "ultimate_radius": return "Eye of the Storm";
            case "ultimate_recharge": return "Storm Battery";
            default: return "Specialized Upgrade";
        }
    }

    private static string SkillUpgradeSummary(string key, int nextLevel)
    {
        switch (key)
        {
            case "lightning_jumps": return "Lv." + nextLevel + ": Chain Lightning jumps to one more enemy.";
            case "lightning_reach": return "Lv." + nextLevel + ": Chain Lightning links farther and flashes wider.";
            case "lightning_voltage": return "Lv." + nextLevel + ": Chain Lightning deals 18% more damage.";
            case "lightning_tempo": return "Lv." + nextLevel + ": Chain Lightning cooldown is reduced.";
            case "magic_force": return "Lv." + nextLevel + ": Magic Bolt deals more damage.";
            case "magic_split": return "Lv." + nextLevel + ": Magic Bolt can fire additional angled shots.";
            case "magic_pierce": return "Lv." + nextLevel + ": Magic Bolt pierces more enemies.";
            case "knife_blades": return "Lv." + nextLevel + ": Orbiting Knives adds another blade.";
            case "knife_reach": return "Lv." + nextLevel + ": Orbiting Knives circle farther out.";
            case "knife_edge": return "Lv." + nextLevel + ": Orbiting Knives hit harder.";
            case "meteor_impacts": return "Lv." + nextLevel + ": Meteor Storm drops one more impact.";
            case "meteor_blast": return "Lv." + nextLevel + ": Meteor blast radius grows.";
            case "meteor_heat": return "Lv." + nextLevel + ": Meteor impacts deal more damage.";
            case "fire_spread": return "Lv." + nextLevel + ": Fire Zone creates another burning pool.";
            case "fire_linger": return "Lv." + nextLevel + ": Fire Zone lasts longer.";
            case "fire_heat": return "Lv." + nextLevel + ": Fire Zone tick damage increases.";
            case "drone_swarm": return "Lv." + nextLevel + ": Summon Drone adds another drone.";
            case "drone_focus": return "Lv." + nextLevel + ": Drone bullets hit harder.";
            case "drone_overclock": return "Lv." + nextLevel + ": Drone firing cooldown is reduced.";
            case "shield_radius": return "Lv." + nextLevel + ": Shield Burst covers more space.";
            case "shield_force": return "Lv." + nextLevel + ": Shield Burst deals more damage.";
            case "shield_recharge": return "Lv." + nextLevel + ": Shield Burst cooldown is reduced.";
            case "ultimate_voltage": return "Lv." + nextLevel + ": Ultimate storm damage increases.";
            case "ultimate_radius": return "Lv." + nextLevel + ": Ultimate storm reaches farther.";
            case "ultimate_recharge": return "Lv." + nextLevel + ": Ultimate cooldown is reduced.";
            default: return "Lv." + nextLevel + ": improves this skill's behavior.";
        }
    }

    private static Color SkillAccent(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return new Color(0.45f, 0.95f, 1f, 1f);
            case ZombieStormSkillType.OrbitingKnife: return new Color(0.92f, 0.96f, 1f, 1f);
            case ZombieStormSkillType.MeteorStorm: return new Color(1f, 0.46f, 0.08f, 1f);
            case ZombieStormSkillType.FireZone: return new Color(1f, 0.28f, 0.05f, 1f);
            case ZombieStormSkillType.SummonDrone: return new Color(0.35f, 0.9f, 1f, 1f);
            case ZombieStormSkillType.ChainLightning: return new Color(0.38f, 0.72f, 1f, 1f);
            case ZombieStormSkillType.ShieldBurst: return new Color(0.78f, 0.98f, 1f, 1f);
            case ZombieStormSkillType.UltimateStorm: return new Color(0.95f, 0.86f, 1f, 1f);
            default: return new Color(0.5f, 0.9f, 1f, 1f);
        }
    }

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

    private static bool IsCityRoadCell(int x, int y)
    {
        return Mathf.Abs(x) <= 1
            || Mathf.Abs(y) <= 1
            || Mathf.Abs(Mathf.Abs(x) - 8) <= 1
            || Mathf.Abs(Mathf.Abs(y) - 8) <= 1
            || (x == -14 && y > -12 && y < 12)
            || (y == 14 && x > -12 && x < 12);
    }

    private static bool HasRoadNeighbor(int x, int y)
    {
        return IsCityRoadCell(x + 1, y)
            || IsCityRoadCell(x - 1, y)
            || IsCityRoadCell(x, y + 1)
            || IsCityRoadCell(x, y - 1);
    }

    private static float Hash01(int x, int y)
    {
        int hash = x * 73856093 ^ y * 19349663;
        hash = (hash << 13) ^ hash;
        hash = (hash * (hash * hash * 15731 + 789221) + 1376312589) & 0x7fffffff;
        return (hash % 10000) / 10000f;
    }

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

    private void AddRoadDash(Vector3 position, Vector3 scale, Color color)
    {
        GameObject dash = CreateSpriteObject("Road Paint", tileSprite, color, position, scale, -6);
        dash.transform.SetParent(worldRoot, false);
    }

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

    private void BuildCityBlockSilhouettes()
    {
        AddBuildingFootprint(new Vector2(-31f, 29f), new Vector2(13f, 7f), new Color(0.055f, 0.06f, 0.065f, 0.92f), new Color(0.2f, 0.9f, 1f, 0.3f));
        AddBuildingFootprint(new Vector2(29f, 30f), new Vector2(10f, 9f), new Color(0.065f, 0.055f, 0.06f, 0.92f), new Color(1f, 0.28f, 0.48f, 0.28f));
        AddBuildingFootprint(new Vector2(-30f, -27f), new Vector2(11f, 8f), new Color(0.06f, 0.065f, 0.055f, 0.92f), new Color(1f, 0.78f, 0.2f, 0.26f));
        AddBuildingFootprint(new Vector2(31f, -28f), new Vector2(12f, 6.5f), new Color(0.045f, 0.052f, 0.06f, 0.94f), new Color(0.25f, 0.85f, 1f, 0.24f));
        AddBuildingFootprint(new Vector2(-43f, 4f), new Vector2(8f, 18f), new Color(0.052f, 0.055f, 0.06f, 0.9f), new Color(1f, 0.22f, 0.62f, 0.22f));
        AddBuildingFootprint(new Vector2(43f, -2f), new Vector2(8.5f, 19f), new Color(0.05f, 0.057f, 0.055f, 0.9f), new Color(0.2f, 0.95f, 0.72f, 0.2f));
    }

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

    private void LoadCowboyWalkFrames()
    {
        playerWalkFrames.Clear();

        if (LoadCowboySplitFrames())
        {
            return;
        }

        string root = Path.Combine(Application.dataPath, "cowboy_walk_8dir_unity_pack", "cowboy_walk_8dir_frames");
        if (!Directory.Exists(root))
        {
            return;
        }

        string[] directions =
        {
            "walk_down",
            "walk_down_left",
            "walk_left",
            "walk_up_left",
            "walk_up",
            "walk_up_right",
            "walk_right",
            "walk_down_right"
        };

        for (int i = 0; i < directions.Length; i++)
        {
            string direction = directions[i];
            string folder = Path.Combine(root, direction);
            if (!Directory.Exists(folder))
            {
                continue;
            }

            string[] files = Directory.GetFiles(folder, "*.png");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            List<Sprite> frames = new List<Sprite>(files.Length);
            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                Sprite frame = LoadSpriteFromPng(files[fileIndex], 160f);
                if (frame != null)
                {
                    frames.Add(frame);
                }
            }

            if (frames.Count > 0)
            {
                playerWalkFrames[direction] = frames.ToArray();
            }
        }

        if (playerWalkFrames.ContainsKey("walk_down"))
        {
            playerSprite = playerWalkFrames["walk_down"][0];
        }

        StabilizeCowboyWalkFrames();
    }

    private bool LoadCowboySplitFrames()
    {
        string root = Path.Combine(Application.dataPath, "cowboy_spritesheet_transparent_split (1)");
        if (!Directory.Exists(root))
        {
            root = Path.Combine(Application.dataPath, "cowboy_spritesheet_transparent_split");
        }

        if (!Directory.Exists(root))
        {
            return false;
        }

        Sprite[] rightFrames = MakeWalkCycle(LoadCowboySplitColumns(root, 8, 4, 6));

        if (rightFrames.Length == 0)
        {
            playerWalkFrames.Clear();
            return false;
        }

        playerWalkFrames["walk_right"] = rightFrames;
        playerWalkFrames["walk_left"] = rightFrames;
        playerSprite = rightFrames[0];
        return true;
    }

    private Sprite[] LoadCowboySplitRow(string root, int row, int columns)
    {
        return LoadCowboySplitColumns(root, row, 1, columns);
    }

    private Sprite[] LoadCowboySplitColumns(string root, int row, int firstColumn, int lastColumn)
    {
        List<Sprite> frames = new List<Sprite>(lastColumn - firstColumn + 1);
        for (int column = firstColumn; column <= lastColumn; column++)
        {
            string fileName = "cowboy_spritesheet_transparent_r" + row + "_c" + column + ".png";
            string path = Path.Combine(root, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            Sprite frame = LoadSpriteFromPng(path, 160f);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        return frames.ToArray();
    }

    private Sprite[] MakeWalkCycle(Sprite[] frames)
    {
        if (frames == null || frames.Length < 3)
        {
            return frames;
        }

        return new[]
        {
            frames[0],
            frames[1],
            frames[2],
            frames[1]
        };
    }

    private void StabilizeCowboyWalkFrames()
    {
        Sprite[] rightFrames;
        playerWalkFrames.TryGetValue("walk_right", out rightFrames);

        if (rightFrames != null && rightFrames.Length > 0)
        {
            playerWalkFrames["walk_left"] = rightFrames;
            playerSprite = rightFrames[0];
        }
    }

    private void LoadScreenSelectedHurtFrames()
    {
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

    private void LoadMikodrakSpellEffects()
    {
        effectFrames.Clear();
        projectileFxSprite = null;

        string root = Path.Combine(Application.dataPath, "ExternalArt", "MikodrakSpellEffects");
        if (!Directory.Exists(root))
        {
            return;
        }

        AddEffectSequence(root, "spark", "fx1_blue_topEffect", 240f);
        AddEffectSequence(root, "fire", "fx3_fireBall", 240f);
        AddEffectSequence(root, "burst", "fx7_energyBall", 240f);
        AddEffectSequence(root, "lightning", "fx8_lighteningBall", 240f);
        AddEffectSequence(root, "explosion", "fx10_blackExplosion", 240f);

        Sprite[] projectileFrames;
        if (effectFrames.TryGetValue("burst", out projectileFrames) && projectileFrames != null && projectileFrames.Length > 0)
        {
            projectileFxSprite = projectileFrames[Mathf.Min(2, projectileFrames.Length - 1)];
        }
    }

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

    private static int CompareFrameFileNames(string left, string right)
    {
        int leftNumber = ExtractTrailingFrameNumber(Path.GetFileNameWithoutExtension(left));
        int rightNumber = ExtractTrailingFrameNumber(Path.GetFileNameWithoutExtension(right));
        int numberCompare = leftNumber.CompareTo(rightNumber);
        return numberCompare != 0 ? numberCompare : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static int ExtractTrailingFrameNumber(string name)
    {
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

    private void AddSpriteIfExists(List<Sprite> target, string path, float pixelsPerUnit, bool removeCheckerBackground)
    {
        Sprite sprite = LoadRawSpriteFromPng(path, pixelsPerUnit, removeCheckerBackground);
        if (sprite != null)
        {
            target.Add(sprite);
        }
    }

    private Sprite LoadRawSpriteFromPng(string path, float pixelsPerUnit, bool removeCheckerBackground)
    {
        if (!File.Exists(path))
        {
            return null;
        }

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

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(true, false);
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load external art: " + path + "\n" + exception.Message);
            return null;
        }
    }

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
            Debug.LogWarning("Failed to load cowboy frame: " + path + "\n" + exception.Message);
            return null;
        }
    }

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

    private static bool IsCheckerBackground(Color32 color)
    {
        if (color.a < 10)
        {
            return true;
        }

        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        int average = (color.r + color.g + color.b) / 3;
        return max - min <= 18 && average >= 150;
    }

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

    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }
}

public sealed class ZombieStormUpgradeOption
{
    public string Key;
    public string Title;
    public string Description;
    public string Category;
    public Color Accent;
    public Action Apply;

    public static ZombieStormUpgradeOption Skill(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "ACTIVE SKILL", Accent = accent, Apply = apply };
    }

    public static ZombieStormUpgradeOption Passive(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "PASSIVE STAT", Accent = accent, Apply = apply };
    }

    public static ZombieStormUpgradeOption Custom(string key, string title, string description, string category, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = category, Accent = accent, Apply = apply };
    }
}

public struct ZombieStormDamagePopup
{
    public string Text;
    public Vector2 WorldPosition;
    public Vector2 Velocity;
    public Color Color;
    public float TimeLeft;
    public int Size;
}

public sealed class ZombieStormPlayer : MonoBehaviour
{
    private const float AnimatedPlayerVisualScale = 1.55f;
    private const float FallbackPlayerVisualScale = 1.28f;
    private const float HealthBarWidth = 1.18f;
    private const float HealthBarHeight = 0.1f;

    private ZombieStormGameController game;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer healthBarFill;
    private Transform healthBarFillTransform;
    private Vector2 lastMove = Vector2.down;
    private float hurtCooldown;
    private float hurtAnimationTimer;
    private float animationTimer;
    private int animationFrame;
    private int hurtAnimationFrame;
    private string facingDirection = "walk_down";

    public int Level { get; private set; }
    public float Experience { get; private set; }
    public float ExperienceToNext { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public int Coins { get; private set; }
    public int Kills { get; set; }
    public float PickupRange { get { return 1.35f + game.GetPassiveLevel(ZombieStormPassiveType.PickupRange) * 0.35f; } }

    public void Initialize(ZombieStormGameController owner, SpriteRenderer renderer)
    {
        game = owner;
        spriteRenderer = renderer;
        Level = 1;
        Experience = 0f;
        ExperienceToNext = 12f;
        MaxHealth = 115f;
        Health = MaxHealth;
        Coins = 0;
        Kills = 0;
        BuildHealthBar();
    }

    private void Update()
    {
        if (game == null)
        {
            return;
        }

        hurtCooldown -= Time.deltaTime;
        hurtAnimationTimer -= Time.deltaTime;
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        float speed = 4.6f + game.GetPassiveLevel(ZombieStormPassiveType.MoveSpeed) * 0.36f;
        transform.position += (Vector3)(input * speed * Time.deltaTime);

        if (input.sqrMagnitude > 0.01f)
        {
            lastMove = input.normalized;
            if (Mathf.Abs(lastMove.x) > 0.08f)
            {
                facingDirection = DirectionToAnimation(lastMove);
            }

            if (!game.HasPlayerWalkAnimation)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(lastMove.y, lastMove.x) * Mathf.Rad2Deg - 90f);
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.white, 6f * Time.deltaTime);
            UpdatePlayerAnimation(input.sqrMagnitude > 0.01f);
        }

        UpdateHealthBar();
    }

    private void BuildHealthBar()
    {
        Sprite barSprite = game.GetHealthBarSprite();
        if (barSprite == null)
        {
            return;
        }

        GameObject root = new GameObject("Underfoot Health Bar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, -0.78f, 0f);

        GameObject background = new GameObject("Health Bar Back");
        background.transform.SetParent(root.transform, false);
        background.transform.localScale = new Vector3(HealthBarWidth + 0.08f, HealthBarHeight + 0.05f, 1f);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = barSprite;
        backgroundRenderer.color = new Color(0.04f, 0.03f, 0.025f, 0.86f);
        backgroundRenderer.sortingOrder = 52;

        GameObject fill = new GameObject("Health Bar Fill");
        fill.transform.SetParent(root.transform, false);
        fill.transform.localScale = new Vector3(HealthBarWidth, HealthBarHeight, 1f);
        healthBarFillTransform = fill.transform;
        healthBarFill = fill.AddComponent<SpriteRenderer>();
        healthBarFill.sprite = barSprite;
        healthBarFill.color = new Color(0.92f, 0.08f, 0.08f, 0.96f);
        healthBarFill.sortingOrder = 53;
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null || healthBarFillTransform == null)
        {
            return;
        }

        float value = MaxHealth <= 0f ? 0f : Mathf.Clamp01(Health / MaxHealth);
        float width = Mathf.Max(0.02f, HealthBarWidth * value);
        healthBarFillTransform.localScale = new Vector3(width, HealthBarHeight, 1f);
        healthBarFillTransform.localPosition = new Vector3(-HealthBarWidth * 0.5f + width * 0.5f, 0f, 0f);
        healthBarFill.color = value > 0.5f ? new Color(0.92f, 0.08f, 0.08f, 0.96f) : new Color(1f, 0.58f, 0.08f, 0.98f);
    }

    private void UpdatePlayerAnimation(bool moving)
    {
        if (!game.HasPlayerWalkAnimation)
        {
            spriteRenderer.flipX = false;
            float pulse = Mathf.Sin(Time.time * 12f) * 0.04f;
            transform.localScale = Vector3.one * (FallbackPlayerVisualScale + pulse);
            return;
        }

        transform.localScale = Vector3.one * AnimatedPlayerVisualScale;
        spriteRenderer.flipX = IsLeftFacingDirection(facingDirection);
        if (hurtAnimationTimer > 0f && game.HasPlayerHurtAnimation)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= 0.055f)
            {
                animationTimer = 0f;
                hurtAnimationFrame++;
            }

            spriteRenderer.sprite = game.GetPlayerHurtFrame(hurtAnimationFrame);
            return;
        }

        if (moving)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= 0.075f)
            {
                animationTimer = 0f;
                animationFrame++;
            }
        }
        else
        {
            animationTimer = 0f;
            animationFrame = 0;
        }

        spriteRenderer.sprite = game.GetPlayerWalkFrame(facingDirection, animationFrame);
    }

    private static bool IsLeftFacingDirection(string direction)
    {
        return direction == "walk_left";
    }

    private static string DirectionToAnimation(Vector2 direction)
    {
        return direction.x < -0.08f ? "walk_left" : "walk_right";
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Experience += amount;
        game.PlaySfx("pickup", 0.3f, 0.06f);
        while (Experience >= ExperienceToNext)
        {
            Experience -= ExperienceToNext;
            Level++;
            ExperienceToNext = Mathf.RoundToInt(ExperienceToNext * 1.28f + 8f);
            game.RequestLevelUp();
            break;
        }
    }

    public void AddCoins(int amount)
    {
        Coins += Mathf.Max(0, amount);
        game.PlaySfx("coin", 0.34f, 0.055f);
    }

    public void TakeDamage(float amount)
    {
        if (hurtCooldown > 0f)
        {
            amount *= 0.35f;
        }

        Health -= amount;
        hurtCooldown = 0.12f;
        hurtAnimationTimer = 0.24f;
        hurtAnimationFrame = 1;
        animationTimer = 0f;
        game.PlaySfx("hurt", 0.48f, 0.18f);
        game.ShakeCamera(0.08f, 0.12f);
        game.FlashScreen(0.8f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.55f, 0.55f);
        }

        if (Health <= 0f)
        {
            Health = 0f;
            game.EndRun(false, "The survivor fell to the horde.");
        }
    }

    public void Heal(float amount)
    {
        Health = Mathf.Min(MaxHealth, Health + amount);
    }

    public void IncreaseMaxHealth(float amount)
    {
        MaxHealth += amount;
        Heal(amount);
    }
}

public sealed class ZombieStormSkillManager : MonoBehaviour
{
    private readonly Dictionary<ZombieStormSkillType, int> levels = new Dictionary<ZombieStormSkillType, int>();
    private readonly Dictionary<ZombieStormSkillType, float> cooldowns = new Dictionary<ZombieStormSkillType, float>();
    private readonly Dictionary<string, int> skillUpgrades = new Dictionary<string, int>();
    private readonly HashSet<ZombieStormSkillType> evolved = new HashSet<ZombieStormSkillType>();
    private readonly List<GameObject> orbitingObjects = new List<GameObject>();
    private readonly List<GameObject> drones = new List<GameObject>();
    private readonly List<ZombieStormPendingSkillBlast> pendingBlasts = new List<ZombieStormPendingSkillBlast>();

    private ZombieStormGameController game;
    private ZombieStormPlayer player;
    private float ultimateCooldown;

    public int KnownSkillCount
    {
        get { return levels.Count; }
    }

    public void Initialize(ZombieStormGameController owner, ZombieStormPlayer survivor)
    {
        game = owner;
        player = survivor;
    }

    private void Update()
    {
        if (game == null || player == null)
        {
            return;
        }

        TickSkill(ZombieStormSkillType.MagicBolt, CastMagicBolt);
        TickSkill(ZombieStormSkillType.MeteorStorm, CastMeteorStorm);
        TickSkill(ZombieStormSkillType.FireZone, CastFireZone);
        TickSkill(ZombieStormSkillType.ChainLightning, CastChainLightning);
        TickSkill(ZombieStormSkillType.ShieldBurst, CastShieldBurst);
        UpdatePendingBlasts();
        UpdateOrbitingKnives();
        UpdateSummonDrones();
        UpdateUltimateInput();
    }

    public void LearnSkill(ZombieStormSkillType weapon)
    {
        if (GetSkillLevel(weapon) <= 0)
        {
            levels[weapon] = 1;
            cooldowns[weapon] = 0.05f;
            if (weapon == ZombieStormSkillType.OrbitingKnife)
            {
                RebuildOrbitingKnives();
            }
            else if (weapon == ZombieStormSkillType.SummonDrone)
            {
                RebuildDrones();
            }
        }
        else
        {
            LevelUpSkill(weapon);
        }
    }

    public void LevelUpSkill(ZombieStormSkillType weapon)
    {
        int next = Mathf.Min(5, GetSkillLevel(weapon) + 1);
        levels[weapon] = next;
        if (weapon == ZombieStormSkillType.OrbitingKnife)
        {
            RebuildOrbitingKnives();
        }
        else if (weapon == ZombieStormSkillType.SummonDrone)
        {
            RebuildDrones();
        }
    }

    public int GetSkillLevel(ZombieStormSkillType weapon)
    {
        int level;
        return levels.TryGetValue(weapon, out level) ? level : 0;
    }

    public int GetSkillUpgradeLevel(string key)
    {
        int level;
        return skillUpgrades.TryGetValue(key, out level) ? level : 0;
    }

    public void AddSkillUpgrade(string key)
    {
        int next = Mathf.Min(3, GetSkillUpgradeLevel(key) + 1);
        skillUpgrades[key] = next;
        if (key == "knife_blades")
        {
            RebuildOrbitingKnives();
        }
        else if (key == "drone_swarm")
        {
            RebuildDrones();
        }
    }

    public bool IsEvolved(ZombieStormSkillType weapon)
    {
        return evolved.Contains(weapon);
    }

    public void Evolve(ZombieStormSkillType weapon)
    {
        evolved.Add(weapon);
        if (weapon == ZombieStormSkillType.OrbitingKnife)
        {
            RebuildOrbitingKnives();
        }
        else if (weapon == ZombieStormSkillType.SummonDrone)
        {
            RebuildDrones();
        }
    }

    public string GetLoadoutText()
    {
        string text = "Skills";
        int ultimateLevel = GetSkillLevel(ZombieStormSkillType.UltimateStorm);
        if (ultimateLevel > 0)
        {
            text += "   F " + Mathf.Max(0f, ultimateCooldown).ToString("0.0") + "s";
        }

        text += "\n";
        if (levels.Count == 0)
        {
            return text + "None";
        }

        foreach (KeyValuePair<ZombieStormSkillType, int> pair in levels)
        {
            if (pair.Key == ZombieStormSkillType.UltimateStorm)
            {
                text += "Ultimate Lv." + pair.Value + "\n";
                continue;
            }

            text += SkillLabel(pair.Key) + " Lv." + pair.Value + (IsEvolved(pair.Key) ? " Evolved" : "") + "\n";
        }

        return text;
    }

    private static string SkillLabel(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Magic Bolt";
            case ZombieStormSkillType.OrbitingKnife: return "Orbit Knives";
            case ZombieStormSkillType.MeteorStorm: return "Meteor";
            case ZombieStormSkillType.FireZone: return "Fire Zone";
            case ZombieStormSkillType.SummonDrone: return "Drone";
            case ZombieStormSkillType.ChainLightning: return "Lightning";
            case ZombieStormSkillType.ShieldBurst: return "Shield";
            case ZombieStormSkillType.UltimateStorm: return "Ultimate";
            default: return weapon.ToString();
        }
    }

    private delegate void SkillAction(int level);

    private void TickSkill(ZombieStormSkillType weapon, SkillAction action)
    {
        int level = GetSkillLevel(weapon);
        if (level <= 0)
        {
            return;
        }

        float current;
        cooldowns.TryGetValue(weapon, out current);
        current -= Time.deltaTime;
        if (current <= 0f)
        {
            action(level);
        }
        else
        {
            cooldowns[weapon] = current;
        }
    }

    private int Mod(string key)
    {
        return GetSkillUpgradeLevel(key);
    }

    private void CastMagicBolt(int level)
    {
        ZombieStormEnemy target = game.FindNearestEnemy(transform.position, IsEvolved(ZombieStormSkillType.MagicBolt) ? 18f : 14f);
        if (target == null)
        {
            cooldowns[ZombieStormSkillType.MagicBolt] = 0.15f;
            return;
        }

        Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        Vector2 origin = (Vector2)transform.position + direction * 0.42f;
        int shots = (IsEvolved(ZombieStormSkillType.MagicBolt) ? 3 : level >= 4 ? 2 : 1) + Mod("magic_split");
        float damage = (10f + level * 3.4f) * (1f + Mod("magic_force") * 0.16f);
        int pierce = (level >= 3 ? 1 : 0) + Mod("magic_pierce");
        game.SpawnHitSpark(origin, new Color(0.48f, 0.95f, 1f, 0.88f), 0.24f + level * 0.025f);
        for (int i = 0; i < shots; i++)
        {
            float spreadStep = shots <= 3 ? 9f : 7f;
            float angle = shots == 1 ? 0f : -(shots - 1) * spreadStep * 0.5f + i * spreadStep;
            game.SpawnPlayerProjectile(origin, ZombieStormGameController.Rotate(direction, angle), RollDamage(damage), 13.5f, 1.4f, pierce, new Color(0.56f, 0.92f, 1f), 0.34f + level * 0.015f);
        }

        float baseCooldown = IsEvolved(ZombieStormSkillType.MagicBolt) ? 0.18f : 0.62f - level * 0.055f;
        cooldowns[ZombieStormSkillType.MagicBolt] = baseCooldown * game.CooldownMultiplier;
    }

    private void CastMeteorStorm(int level)
    {
        int impacts = 1 + Mathf.FloorToInt(level * 0.55f) + Mod("meteor_impacts") + (IsEvolved(ZombieStormSkillType.MeteorStorm) ? 2 : 0);
        for (int i = 0; i < impacts; i++)
        {
            ZombieStormEnemy target = game.FindRandomEnemy();
            Vector2 position = target != null ? (Vector2)target.transform.position : (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 5.5f;
            float radius = (0.95f + level * 0.18f + Mod("meteor_blast") * 0.2f) * game.AreaMultiplier;
            game.SpawnAreaEffect(position, radius * 1.35f, 0f, 0.48f, 1f, new Color(1f, 0.75f, 0.18f, 0.3f), "meteor_warning");
            game.SpawnAreaEffect(position, radius * 0.36f, 0f, 0.48f, 1f, new Color(1f, 0.92f, 0.3f, 0.52f), "meteor_warning");
            pendingBlasts.Add(new ZombieStormPendingSkillBlast
            {
                Position = position,
                Radius = radius,
                Damage = RollDamage((20f + level * 5.5f) * (1f + Mod("meteor_heat") * 0.16f)),
                Delay = 0.42f,
                Color = new Color(1f, 0.28f, 0.05f, 0.72f),
                Key = "meteor_blast"
            });
        }

        cooldowns[ZombieStormSkillType.MeteorStorm] = (4.2f - level * 0.24f) * game.CooldownMultiplier;
    }

    private void CastFireZone(int level)
    {
        int pools = (IsEvolved(ZombieStormSkillType.FireZone) ? 3 : 1 + level / 4) + Mod("fire_spread");
        for (int i = 0; i < pools; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2.4f, 7.0f);
            Vector2 position = (Vector2)transform.position + offset;
            float radius = (1.25f + level * 0.22f) * game.AreaMultiplier * (IsEvolved(ZombieStormSkillType.FireZone) ? 1.22f : 1f);
            float duration = 2.5f + level * 0.36f + Mod("fire_linger") * 0.55f;
            game.SpawnAreaEffect(position, radius, RollDamage((4.2f + level * 1.3f) * (1f + Mod("fire_heat") * 0.18f)), duration, 0.28f, new Color(1f, 0.25f, 0.05f, 0.62f), "fire_pool");
            for (int spark = 0; spark < 4; spark++)
            {
                game.SpawnHitSpark(position + UnityEngine.Random.insideUnitCircle * radius * 0.65f, new Color(1f, 0.68f, 0.12f, 0.78f), 0.16f);
            }
        }

        cooldowns[ZombieStormSkillType.FireZone] = (4.1f - level * 0.25f) * game.CooldownMultiplier;
    }

    private void CastChainLightning(int level)
    {
        ZombieStormEnemy current = game.FindRandomEnemy();
        if (current == null)
        {
            cooldowns[ZombieStormSkillType.ChainLightning] = 0.25f;
            return;
        }

        int jumps = 2 + level + Mod("lightning_jumps") + (IsEvolved(ZombieStormSkillType.ChainLightning) ? 4 : 0);
        float chainReach = 4.2f + level * 0.35f + Mod("lightning_reach") * 0.8f;
        float lightningDamage = (13f + level * 4f) * (1f + Mod("lightning_voltage") * 0.18f);
        game.PlaySfx("lightning", 0.48f, 0.12f);
        HashSet<ZombieStormEnemy> hit = new HashSet<ZombieStormEnemy>();
        Vector2 previous = transform.position;
        for (int i = 0; i < jumps && current != null; i++)
        {
            Vector2 currentPosition = current.transform.position;
            hit.Add(current);
            SpawnLightningSegment(previous, currentPosition, level);
            current.TakeDamage(RollDamage(lightningDamage), (currentPosition - (Vector2)transform.position).normalized);
            game.SpawnAreaEffect(currentPosition, (0.6f + Mod("lightning_reach") * 0.08f) * game.AreaMultiplier, 0f, 0.18f, 1f, new Color(0.25f, 0.85f, 1f, 0.86f), "lightning_flash");
            previous = currentPosition;
            current = FindNearestUnhitEnemy(currentPosition, chainReach, hit);
        }

        cooldowns[ZombieStormSkillType.ChainLightning] = Mathf.Max(0.82f, 3.5f - level * 0.22f - Mod("lightning_tempo") * 0.24f) * game.CooldownMultiplier;
    }

    private void CastShieldBurst(int level)
    {
        float radius = (1.35f + level * 0.24f + Mod("shield_radius") * 0.22f) * game.AreaMultiplier;
        if (CountEnemiesNear(transform.position, radius + 0.35f) <= 0)
        {
            cooldowns[ZombieStormSkillType.ShieldBurst] = 0.18f;
            return;
        }

        game.SpawnAreaEffect(transform.position, radius, RollDamage((12f + level * 4f) * (1f + Mod("shield_force") * 0.18f)), 0.18f, 99f, new Color(0.75f, 0.95f, 1f, 0.55f), "shield_burst");
        game.SpawnAreaEffect(transform.position, radius * 1.38f, 0f, 0.24f, 1f, new Color(0.4f, 0.92f, 1f, 0.32f), "shield_burst");
        game.ShakeCamera(0.06f, 0.1f);
        cooldowns[ZombieStormSkillType.ShieldBurst] = Mathf.Max(0.65f, 2.4f - level * 0.16f - Mod("shield_recharge") * 0.18f) * game.CooldownMultiplier;
    }

    private void SpawnLightningSegment(Vector2 from, Vector2 to, int level)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Clamp(Mathf.CeilToInt(distance / 0.42f), 2, 12);
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            point += UnityEngine.Random.insideUnitCircle * 0.06f;
            float radius = (0.12f + level * 0.012f + Mod("lightning_reach") * 0.01f + Mod("lightning_voltage") * 0.006f) * game.AreaMultiplier;
            game.SpawnAreaEffect(point, radius, 0f, 0.1f, 1f, new Color(0.35f, 0.9f, 1f, 0.72f), "lightning_flash");
        }
    }

    private void UpdateOrbitingKnives()
    {
        int level = GetSkillLevel(ZombieStormSkillType.OrbitingKnife);
        if (level <= 0)
        {
            return;
        }

        float radius = (1.45f + level * 0.18f + Mod("knife_reach") * 0.22f) * game.AreaMultiplier * (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 1.32f : 1f);
        float speed = 120f + level * 28f;
        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            float angle = Time.time * speed + i * (360f / Mathf.Max(1, orbitingObjects.Count));
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            orbitingObjects[i].transform.position = (Vector2)transform.position + offset;
            orbitingObjects[i].transform.Rotate(0f, 0f, 420f * Time.deltaTime);
        }

        float current;
        cooldowns.TryGetValue(ZombieStormSkillType.OrbitingKnife, out current);
        current -= Time.deltaTime;
        if (current > 0f)
        {
            cooldowns[ZombieStormSkillType.OrbitingKnife] = current;
            return;
        }

        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(enemy.transform.position, transform.position) <= radius + enemy.Radius)
            {
                enemy.TakeDamage(RollDamage((6f + level * 1.8f) * (1f + Mod("knife_edge") * 0.18f)), ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized);
                game.SpawnHitSpark(enemy.transform.position, new Color(0.9f, 0.96f, 1f, 0.72f), 0.18f);
            }
        }

        cooldowns[ZombieStormSkillType.OrbitingKnife] = 0.24f * game.CooldownMultiplier;
    }

    private void RebuildOrbitingKnives()
    {
        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            if (orbitingObjects[i] != null)
            {
                Destroy(orbitingObjects[i]);
            }
        }

        orbitingObjects.Clear();
        int level = GetSkillLevel(ZombieStormSkillType.OrbitingKnife);
        int count = 2 + Mathf.FloorToInt(level * 0.75f) + Mod("knife_blades") + (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 3 : 0);
        for (int i = 0; i < count; i++)
        {
            GameObject blade = new GameObject("Orbiting Skill Blade");
            blade.transform.localScale = Vector3.one * 0.5f;
            SpriteRenderer spriteRenderer = blade.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = game.GetSkillSprite(ZombieStormSkillType.OrbitingKnife);
            spriteRenderer.color = new Color(0.9f, 0.98f, 1f, 1f);
            spriteRenderer.sortingOrder = 35;
            orbitingObjects.Add(blade);
        }
    }

    private void RebuildDrones()
    {
        for (int i = 0; i < drones.Count; i++)
        {
            if (drones[i] != null)
            {
                Destroy(drones[i]);
            }
        }

        drones.Clear();
        int level = GetSkillLevel(ZombieStormSkillType.SummonDrone);
        int count = 1 + level / 2 + Mod("drone_swarm") + (IsEvolved(ZombieStormSkillType.SummonDrone) ? 2 : 0);
        for (int i = 0; i < count; i++)
        {
            GameObject drone = new GameObject("Summoned Drone");
            drone.transform.localScale = Vector3.one * 0.56f;
            SpriteRenderer spriteRenderer = drone.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = game.GetSkillSprite(ZombieStormSkillType.SummonDrone);
            spriteRenderer.color = new Color(0.5f, 0.92f, 1f);
            spriteRenderer.sortingOrder = 34;
            drones.Add(drone);
        }
    }

    private void UpdateSummonDrones()
    {
        int level = GetSkillLevel(ZombieStormSkillType.SummonDrone);
        if (level <= 0)
        {
            return;
        }

        if (drones.Count == 0)
        {
            RebuildDrones();
        }

        for (int i = 0; i < drones.Count; i++)
        {
            float angle = Time.time * 92f + i * (360f / Mathf.Max(1, drones.Count));
            Vector2 desired = (Vector2)transform.position + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.15f;
            drones[i].transform.position = Vector2.Lerp(drones[i].transform.position, desired, 9f * Time.deltaTime);
        }

        float current;
        cooldowns.TryGetValue(ZombieStormSkillType.SummonDrone, out current);
        current -= Time.deltaTime;
        if (current > 0f)
        {
            cooldowns[ZombieStormSkillType.SummonDrone] = current;
            return;
        }

        for (int i = 0; i < drones.Count; i++)
        {
            ZombieStormEnemy target = game.FindNearestEnemy(drones[i].transform.position, 9f);
            if (target == null)
            {
                continue;
            }

            Vector2 direction = ((Vector2)target.transform.position - (Vector2)drones[i].transform.position).normalized;
            Vector2 muzzle = (Vector2)drones[i].transform.position + direction * 0.26f;
            game.SpawnHitSpark(muzzle, new Color(0.35f, 0.9f, 1f, 0.78f), 0.16f);
            game.SpawnPlayerProjectile(muzzle, direction, RollDamage((7f + level * 2.4f) * (1f + Mod("drone_focus") * 0.18f)), 12f, 1.1f, 0, new Color(0.4f, 0.92f, 1f), 0.26f);
        }

        cooldowns[ZombieStormSkillType.SummonDrone] = Mathf.Max(0.22f, 0.92f - level * 0.06f - Mod("drone_overclock") * 0.08f) * game.CooldownMultiplier;
    }

    private void UpdateUltimateInput()
    {
        if (ultimateCooldown > 0f)
        {
            ultimateCooldown -= Time.deltaTime;
        }

        int level = GetSkillLevel(ZombieStormSkillType.UltimateStorm);
        if (level <= 0 || ultimateCooldown > 0f || !Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            enemy.TakeDamage(RollDamage((42f + level * 18f) * (1f + Mod("ultimate_voltage") * 0.18f)), ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized);
            game.SpawnAreaEffect(enemy.transform.position, 0.62f, 0f, 0.22f, 1f, new Color(0.45f, 0.85f, 1f, 0.78f), "ultimate_spark");
        }

        float stormRadius = (7.5f + Mod("ultimate_radius") * 0.85f) * game.AreaMultiplier;
        game.SpawnAreaEffect(transform.position, stormRadius, RollDamage((25f + level * 8f) * (1f + Mod("ultimate_voltage") * 0.18f)), 0.35f, 99f, new Color(0.7f, 0.92f, 1f, 0.42f), "ultimate_storm");
        game.PlaySfx("ultimate", 0.92f, 0.2f);
        game.ShakeCamera(0.34f, 0.48f);
        game.FlashScreen(0.9f);
        ultimateCooldown = Mathf.Max(12f, 42f - level * 4f - Mod("ultimate_recharge") * 3.5f);
    }

    private ZombieStormEnemy FindNearestUnhitEnemy(Vector2 origin, float maxDistance, HashSet<ZombieStormEnemy> hit)
    {
        ZombieStormEnemy best = null;
        float bestDistance = maxDistance * maxDistance;
        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead || hit.Contains(enemy))
            {
                continue;
            }

            float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }

        return best;
    }

    private int CountEnemiesNear(Vector2 origin, float radius)
    {
        int count = 0;
        float radiusSquared = radius * radius;
        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && ((Vector2)enemy.transform.position - origin).sqrMagnitude <= radiusSquared)
            {
                count++;
            }
        }

        return count;
    }

    private void UpdatePendingBlasts()
    {
        for (int i = pendingBlasts.Count - 1; i >= 0; i--)
        {
            ZombieStormPendingSkillBlast blast = pendingBlasts[i];
            blast.Delay -= Time.deltaTime;
            if (blast.Delay > 0f)
            {
                pendingBlasts[i] = blast;
                continue;
            }

            game.SpawnAreaEffect(blast.Position, blast.Radius, blast.Damage, 0.22f, 99f, blast.Color, blast.Key);
            if (blast.Key == "meteor_blast")
            {
                game.SpawnHitSpark(blast.Position, new Color(1f, 0.9f, 0.25f, 0.9f), blast.Radius * 0.32f);
                game.PlaySfx("boom", 0.58f, 0.08f);
                game.ShakeCamera(0.12f, 0.14f);
                game.FlashScreen(blast.Color, 0.2f);
            }

            pendingBlasts.RemoveAt(i);
        }
    }

    private float RollDamage(float baseDamage)
    {
        float damage = baseDamage * game.DamageMultiplier;
        if (UnityEngine.Random.value < game.CritChance)
        {
            damage *= 2f;
        }

        return damage;
    }
}

public struct ZombieStormPendingSkillBlast
{
    public Vector2 Position;
    public float Radius;
    public float Damage;
    public float Delay;
    public Color Color;
    public string Key;
}

public sealed class ZombieStormEnemy : MonoBehaviour
{
    private ZombieStormGameController game;
    private SpriteRenderer spriteRenderer;
    private string poolKey;
    private Color baseColor;
    private float health;
    private float maxHealth;
    private float speed;
    private float damagePerSecond;
    private float bossActionTimer;
    private float sprintTimer;
    private float shootTimer;
    private bool sprinting;

    public ZombieStormEnemyType Type { get; private set; }
    public bool IsDead { get; private set; }
    public float Radius { get; private set; }
    public float Health { get { return health; } }
    public float MaxHealth { get { return maxHealth; } }
    public float Health01 { get { return maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth); } }

    public void Initialize(ZombieStormGameController owner, ZombieStormEnemyType enemyType, string key, Sprite sprite, float runTime, float difficulty)
    {
        game = owner;
        Type = enemyType;
        poolKey = key;
        IsDead = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        baseColor = Color.white;
        spriteRenderer.color = baseColor;

        float hpScale = 0.82f + runTime / 165f;
        Radius = 0.42f;
        speed = 1.55f;
        damagePerSecond = 6.5f;
        maxHealth = 22f * hpScale;
        transform.localScale = Vector3.one * 0.95f;
        sprintTimer = 0.8f;
        shootTimer = UnityEngine.Random.Range(0.7f, 1.6f);

        if (Type == ZombieStormEnemyType.Fast)
        {
            speed = 3.05f;
            maxHealth = 15f * hpScale;
            damagePerSecond = 8.5f;
            Radius = 0.34f;
            transform.localScale = Vector3.one * 0.78f;
        }
        else if (Type == ZombieStormEnemyType.Tank)
        {
            speed = 1.02f;
            maxHealth = 82f * hpScale;
            damagePerSecond = 10f;
            Radius = 0.6f;
            transform.localScale = Vector3.one * 1.35f;
        }
        else if (Type == ZombieStormEnemyType.Exploder)
        {
            speed = 1.92f;
            maxHealth = 34f * hpScale;
            damagePerSecond = 5f;
            Radius = 0.5f;
            baseColor = new Color(1f, 0.85f, 0.18f);
            spriteRenderer.color = baseColor;
            transform.localScale = Vector3.one * 1.05f;
        }
        else if (Type == ZombieStormEnemyType.Spitter)
        {
            speed = 1.45f;
            maxHealth = 30f * hpScale;
            damagePerSecond = 7f;
            Radius = 0.42f;
            baseColor = new Color(0.7f, 1f, 0.75f);
            spriteRenderer.color = baseColor;
        }
        else if (Type == ZombieStormEnemyType.Elite)
        {
            speed = 1.82f;
            maxHealth = 150f * hpScale;
            damagePerSecond = 14f;
            Radius = 0.75f;
            transform.localScale = Vector3.one * 1.62f;
        }
        else if (Type == ZombieStormEnemyType.Boss)
        {
            speed = 1.22f;
            maxHealth = 920f * Mathf.Max(1f, difficulty);
            damagePerSecond = 26f;
            Radius = 1.45f;
            transform.localScale = Vector3.one * 3.1f;
            bossActionTimer = 2.5f;
        }

        health = maxHealth;
        game.RegisterEnemy(this);
    }

    private void Update()
    {
        if (game == null || game.Player == null || IsDead)
        {
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, baseColor, 10f * Time.deltaTime);
        }

        Vector2 toPlayer = (Vector2)game.Player.transform.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.01f ? toPlayer / distance : Vector2.zero;

        if (Type == ZombieStormEnemyType.Fast)
        {
            sprintTimer -= Time.deltaTime;
            if (sprintTimer <= 0f)
            {
                sprinting = !sprinting;
                sprintTimer = sprinting ? 0.55f : 1.05f;
            }
        }

        if (Type == ZombieStormEnemyType.Boss)
        {
            UpdateBoss(direction);
        }
        else if (Type == ZombieStormEnemyType.Spitter)
        {
            UpdateSpitter(direction, distance);
        }
        else
        {
            float finalSpeed = speed * (sprinting ? 1.85f : 1f);
            transform.position += (Vector3)(direction * finalSpeed * Time.deltaTime);
        }

        if (distance <= Radius + 0.45f)
        {
            if (Type == ZombieStormEnemyType.Exploder)
            {
                game.SpawnAreaEffect(transform.position, 2.2f, 30f, 0.22f, 99f, new Color(1f, 0.35f, 0.05f, 0.65f), "zombie_explosion");
                game.PlaySfx("boom", 0.54f, 0.08f);
                game.ShakeCamera(0.16f, 0.16f);
                game.Player.TakeDamage(22f);
                Die(false);
            }
            else
            {
                game.Player.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }
    }

    public void TakeDamage(float amount, Vector2 impulse)
    {
        if (IsDead)
        {
            return;
        }

        health -= amount;
        if (amount >= 8f || UnityEngine.Random.value < 0.18f)
        {
            game.SpawnDamageNumber(transform.position, amount, amount >= 30f);
        }

        transform.position += (Vector3)(impulse.normalized * 0.035f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(baseColor, Color.red, 0.55f);
        }

        if (health <= 0f)
        {
            game.SpawnHitSpark(transform.position, Type == ZombieStormEnemyType.Boss ? new Color(1f, 0.2f, 0.15f, 0.9f) : new Color(0.65f, 1f, 0.35f, 0.8f), Type == ZombieStormEnemyType.Boss ? 1.1f : 0.42f);
            Die(true);
        }
    }

    private void UpdateSpitter(Vector2 direction, float distance)
    {
        if (distance > 6.8f)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
        else if (distance < 4.2f)
        {
            transform.position -= (Vector3)(direction * speed * 0.7f * Time.deltaTime);
        }

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = 2.2f;
            game.SpawnEnemyProjectile(transform.position, direction, 10f, 4.8f, 4.2f);
        }
    }

    private void UpdateBoss(Vector2 direction)
    {
        bool enraged = health < maxHealth * 0.5f;
        transform.position += (Vector3)(direction * speed * (enraged ? 1.35f : 1f) * Time.deltaTime);
        bossActionTimer -= Time.deltaTime;
        if (bossActionTimer > 0f)
        {
            return;
        }

        int action = UnityEngine.Random.Range(0, 3);
        if (action == 0)
        {
            int shots = enraged ? 18 : 12;
            for (int i = 0; i < shots; i++)
            {
                Vector2 shotDir = ZombieStormGameController.Rotate(Vector2.up, i * (360f / shots));
                game.SpawnEnemyProjectile(transform.position, shotDir, enraged ? 16f : 10f, 4.2f, 4f);
            }
        }
        else if (action == 1)
        {
            int pools = enraged ? 8 : 5;
            for (int i = 0; i < pools; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.8f, 3.8f);
                game.SpawnAreaEffect((Vector2)transform.position + offset, 0.95f, 8f, 2.4f, 0.45f, new Color(0.55f, 1f, 0.15f, 0.38f), "toxic_pool");
            }
        }
        else
        {
            transform.position += (Vector3)(direction * (enraged ? 4.1f : 2.7f));
            game.ShakeCamera(0.11f, 0.14f);
        }

        bossActionTimer = enraged ? 2.15f : 3.1f;
    }

    private void Die(bool reward)
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        game.UnregisterEnemy(this);
        if (reward)
        {
            game.OnEnemyKilled(this);
        }

        game.ReturnPooled(poolKey, gameObject);
    }
}

public sealed class ZombieStormProjectile : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float life;
    private int pierce;

    public void Initialize(ZombieStormGameController owner, Vector2 fireDirection, float hitDamage, float moveSpeed, float seconds, int pierceCount)
    {
        game = owner;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.up;
        damage = hitDamage;
        speed = moveSpeed;
        life = seconds;
        pierce = pierceCount;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            game.ReturnPooled("player_bullet", gameObject);
            return;
        }

        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) <= enemy.Radius + 0.16f)
            {
                enemy.TakeDamage(damage, direction);
                game.SpawnHitSpark(transform.position, new Color(1f, 0.9f, 0.28f, 0.9f), 0.26f);
                pierce--;
                if (pierce < 0)
                {
                    game.ReturnPooled("player_bullet", gameObject);
                }

                return;
            }
        }
    }
}

public sealed class ZombieStormEnemyProjectile : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float life;

    public void Initialize(ZombieStormGameController owner, Vector2 fireDirection, float hitDamage, float moveSpeed, float seconds)
    {
        game = owner;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.up;
        damage = hitDamage;
        speed = moveSpeed;
        life = seconds;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            game.ReturnPooled("enemy_spit", gameObject);
            return;
        }

        if (game.Player != null && Vector2.Distance(transform.position, game.Player.transform.position) <= 0.5f)
        {
            game.Player.TakeDamage(damage);
            game.ReturnPooled("enemy_spit", gameObject);
        }
    }
}

public sealed class ZombieStormAreaEffect : MonoBehaviour
{
    private ZombieStormGameController game;
    private string poolKey;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private Color initialColor;
    private Vector3 initialScale;
    private float radius;
    private float damage;
    private float life;
    private float maxLife;
    private float tickRate;
    private float tickTimer;
    private float frameDuration;
    private bool mineTriggered;

    public void Initialize(ZombieStormGameController owner, string key, float areaRadius, float hitDamage, float duration, float rate)
    {
        game = owner;
        poolKey = key;
        radius = areaRadius;
        damage = hitDamage;
        life = duration;
        maxLife = Mathf.Max(0.01f, duration);
        tickRate = rate;
        tickTimer = 0f;
        mineTriggered = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        frames = game.GetEffectFrames(poolKey);
        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }

        initialColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        initialScale = transform.localScale;
        frameDuration = frames != null && frames.Length > 0 ? Mathf.Clamp(maxLife / frames.Length, 0.028f, 0.06f) : 0.05f;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        tickTimer -= Time.deltaTime;
        UpdateVisuals();

        if (poolKey == "mine_blast" && !mineTriggered)
        {
            bool hasTarget = false;
            IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                ZombieStormEnemy enemy = activeEnemies[i];
                if (enemy != null && !enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) <= radius + enemy.Radius)
                {
                    hasTarget = true;
                    break;
                }
            }

            if (!hasTarget)
            {
                if (life <= 0f)
                {
                    game.ReturnPooled(poolKey, gameObject);
                }

                return;
            }

            mineTriggered = true;
            life = 0.18f;
            tickTimer = 0f;
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(1f, 0.4f, 0.05f, 0.74f);
            initialColor = spriteRenderer.color;
        }

        if (tickTimer <= 0f)
        {
            tickTimer = tickRate;
            DamageEnemies();
        }

        if (life <= 0f)
        {
            game.ReturnPooled(poolKey, gameObject);
        }
    }

    private void UpdateVisuals()
    {
        float t = Mathf.Clamp01(life / maxLife);
        if (spriteRenderer != null)
        {
            if (frames != null && frames.Length > 0)
            {
                int frameIndex = Mathf.FloorToInt((maxLife - life) / frameDuration);
                if (life > 0.08f && (poolKey == "fire_pool" || poolKey == "toxic_pool" || poolKey == "ultimate_storm"))
                {
                    frameIndex %= frames.Length;
                }
                else
                {
                    frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
                }

                spriteRenderer.sprite = frames[frameIndex];
            }

            Color color = initialColor;
            color.a *= Mathf.SmoothStep(0f, 1f, t);
            spriteRenderer.color = color;
        }

        if (poolKey == "hit_spark" || poolKey == "lightning_flash" || poolKey == "zombie_explosion")
        {
            float grow = 1f + (1f - t) * 0.55f;
            transform.localScale = initialScale * grow;
        }
    }

    private void DamageEnemies()
    {
        if (damage <= 0f)
        {
            return;
        }

        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) <= radius + enemy.Radius)
            {
                enemy.TakeDamage(damage, ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized);
            }
        }
    }
}

public sealed class ZombieStormTimedPooled : MonoBehaviour
{
    private ZombieStormGameController game;
    private string poolKey;
    private float life;
    private float maxLife;
    private SpriteRenderer spriteRenderer;
    private Color initialColor;

    public void Initialize(ZombieStormGameController owner, string key, float duration)
    {
        game = owner;
        poolKey = key;
        life = duration;
        maxLife = Mathf.Max(0.01f, duration);
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (spriteRenderer != null)
        {
            Color color = initialColor;
            color.a *= Mathf.Clamp01(life / maxLife);
            spriteRenderer.color = color;
        }

        if (life <= 0f && game != null)
        {
            game.ReturnPooled(poolKey, gameObject);
        }
    }
}

public sealed class ZombieStormPickup : MonoBehaviour
{
    private ZombieStormGameController game;
    private string poolKey;
    private int xp;
    private int coins;
    private float bobOffset;

    public void Initialize(ZombieStormGameController owner, string key, int xpAmount, int coinAmount)
    {
        game = owner;
        poolKey = key;
        xp = xpAmount;
        coins = coinAmount;
        bobOffset = UnityEngine.Random.value * 10f;
    }

    private void Update()
    {
        if (game == null || game.Player == null)
        {
            return;
        }

        Vector2 toPlayer = (Vector2)game.Player.transform.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;
        float pickupRange = game.Player.PickupRange;
        if (distance < pickupRange)
        {
            float pullSpeed = Mathf.Lerp(2f, 12f, 1f - distance / pickupRange);
            transform.position += (Vector3)(toPlayer.normalized * pullSpeed * Time.deltaTime);
        }

        transform.localScale = Vector3.one * (0.32f + Mathf.Sin(Time.time * 6f + bobOffset) * 0.035f);

        if (distance < 0.34f)
        {
            if (xp > 0)
            {
                game.Player.AddExperience(xp);
            }

            if (coins > 0)
            {
                game.Player.AddCoins(coins);
            }

            game.ReturnPooled(poolKey, gameObject);
        }
    }
}
