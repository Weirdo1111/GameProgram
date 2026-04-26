using System;
using System.Collections.Generic;
using UnityEngine;

public enum ZombieStormWeaponType
{
    Pistol,
    Shotgun,
    Molotov,
    SawRing,
    Lightning,
    Mine
}

public enum ZombieStormEnemyType
{
    Grunt,
    Fast,
    Tank,
    Exploder,
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

public sealed class ZombieStormGameController : MonoBehaviour
{
    public static ZombieStormGameController Instance { get; private set; }

    [Header("Run")]
    public float runDurationSeconds = 300f;
    public int targetFrameRate = 120;

    public ZombieStormPlayer player;
    public ZombieStormWeaponManager weapons;

    private readonly List<ZombieStormEnemy> enemies = new List<ZombieStormEnemy>();
    private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private readonly List<ZombieStormUpgradeOption> currentChoices = new List<ZombieStormUpgradeOption>();
    private readonly Dictionary<ZombieStormPassiveType, int> passives = new Dictionary<ZombieStormPassiveType, int>();

    private Transform worldRoot;
    private Transform poolRoot;
    private Camera mainCamera;
    private Sprite playerSprite;
    private Sprite[] survivorSprites;
    private Sprite zombieSprite;
    private Sprite fastZombieSprite;
    private Sprite tankZombieSprite;
    private Sprite eliteZombieSprite;
    private Sprite bossSprite;
    private Sprite bulletSprite;
    private Sprite xpSprite;
    private Sprite coinSprite;
    private Sprite fireSprite;
    private Sprite sawSprite;
    private Sprite mineSprite;
    private Sprite tileSprite;
    private Sprite ruinSprite;

    private float runTime;
    private float spawnTimer;
    private float eliteTimer = 22f;
    private float feedbackTimer;
    private float difficultyScore = 1f;
    private bool leveling;
    private bool finished;
    private bool won;
    private string feedbackText = "WASD 移动，幸存者会自动开火。";
    private float feedbackUntil;
    private int bossCount;

    public IReadOnlyList<ZombieStormEnemy> Enemies
    {
        get { return enemies; }
    }

    public float DamageMultiplier
    {
        get { return 1f + GetPassiveLevel(ZombieStormPassiveType.Damage) * 0.18f; }
    }

    public float CooldownMultiplier
    {
        get { return Mathf.Max(0.35f, 1f - GetPassiveLevel(ZombieStormPassiveType.FireRate) * 0.08f); }
    }

    public float AreaMultiplier
    {
        get { return 1f + GetPassiveLevel(ZombieStormPassiveType.Area) * 0.16f; }
    }

    public float CritChance
    {
        get { return Mathf.Clamp01(GetPassiveLevel(ZombieStormPassiveType.Crit) * 0.07f); }
    }

    public float CoinMultiplier
    {
        get { return 1f + GetPassiveLevel(ZombieStormPassiveType.CoinGain) * 0.2f; }
    }

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = targetFrameRate;
        Physics2D.gravity = Vector2.zero;
        Time.timeScale = 1f;

        CreateSprites();
        BuildScene();
        StartRun();
    }

    private void Update()
    {
        if (finished)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                RestartRun();
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
        UpdateDynamicDifficulty();
        UpdateSpawning();

        if (feedbackTimer >= 15f)
        {
            feedbackTimer = 0f;
            ShowFeedback("尸潮密度提升，继续捡经验变强。", 2.2f);
        }

        if (runTime >= runDurationSeconds)
        {
            EndRun(true, "黎明到来，你撑过了尸潮。");
        }
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 18;
        GUI.Label(new Rect(18f, 14f, 720f, 28f), "Zombie Storm: 僵尸割草大作战");
        GUI.Label(new Rect(18f, 42f, 880f, 28f), "WASD 移动 | 自动射击 | 捡经验升级三选一 | Enter 重新开始");

        if (player != null)
        {
            DrawBar(new Rect(18f, 78f, 260f, 18f), player.Health / player.MaxHealth, new Color(0.95f, 0.18f, 0.16f), "HP " + Mathf.CeilToInt(player.Health) + "/" + Mathf.CeilToInt(player.MaxHealth));
            DrawBar(new Rect(18f, 104f, 260f, 18f), player.Experience / Mathf.Max(1f, player.ExperienceToNext), new Color(0.25f, 0.75f, 1f), "Lv." + player.Level + " XP");
            GUI.Label(new Rect(18f, 130f, 520f, 28f), "Coins: " + player.Coins + "   Kills: " + player.Kills + "   Enemies: " + enemies.Count);
        }

        int remain = Mathf.Max(0, Mathf.CeilToInt(runDurationSeconds - runTime));
        GUI.Label(new Rect(Screen.width - 260f, 18f, 240f, 28f), "Survive: " + FormatTime(remain));

        if (weapons != null)
        {
            GUI.Label(new Rect(Screen.width - 360f, 50f, 340f, 84f), weapons.GetLoadoutText());
        }

        if (Time.unscaledTime < feedbackUntil)
        {
            GUI.skin.label.fontSize = 24;
            GUI.color = new Color(1f, 0.9f, 0.35f, 1f);
            GUI.Label(new Rect(Screen.width * 0.5f - 280f, 84f, 600f, 40f), feedbackText);
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 18;
        }

        if (leveling)
        {
            DrawUpgradePanel();
        }

        if (finished)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 36;
            GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f - 80f, 520f, 60f), won ? "生存胜利" : "幸存者倒下了");
            GUI.skin.label.fontSize = 20;
            GUI.Label(new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 24f, 620f, 100f), "击杀 " + (player != null ? player.Kills : 0) + " | 金币 " + (player != null ? player.Coins : 0) + " | 等级 " + (player != null ? player.Level : 1));
            GUI.Label(new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f + 40f, 560f, 40f), "按 Enter 重新开始。");
            GUI.skin.label.fontSize = 18;
        }
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

        for (int i = 0; i < 8; i++)
        {
            ZombieStormEnemy enemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead)
            {
                return enemy;
            }
        }

        return FindNearestEnemy(player != null ? player.transform.position : Vector3.zero, 999f);
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
        SpriteRenderer renderer = projectileObject.GetComponent<SpriteRenderer>();
        renderer.color = color;
        ZombieStormProjectile projectile = projectileObject.GetComponent<ZombieStormProjectile>();
        projectile.Initialize(this, direction, damage, speed, life, pierce);
    }

    public void SpawnEnemyProjectile(Vector2 position, Vector2 direction, float damage, float speed, float life)
    {
        GameObject projectileObject = SpawnPooled("enemy_spit", CreateEnemyProjectile);
        projectileObject.transform.SetParent(worldRoot, false);
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * 0.26f;
        ZombieStormEnemyProjectile projectile = projectileObject.GetComponent<ZombieStormEnemyProjectile>();
        projectile.Initialize(this, direction, damage, speed, life);
    }

    public void SpawnAreaEffect(Vector2 position, float radius, float damage, float duration, float tickRate, Color color, string poolKey)
    {
        GameObject effectObject = SpawnPooled(poolKey, CreateAreaEffect);
        effectObject.transform.SetParent(worldRoot, false);
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
        renderer.color = color;
        ZombieStormAreaEffect effect = effectObject.GetComponent<ZombieStormAreaEffect>();
        effect.Initialize(this, poolKey, radius, damage, duration, tickRate);
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
        if (player != null)
        {
            player.Kills++;
        }

        int xp = enemy.Type == ZombieStormEnemyType.Boss ? 45 : enemy.Type == ZombieStormEnemyType.Elite ? 18 : enemy.Type == ZombieStormEnemyType.Tank ? 6 : 3;
        int coins = enemy.Type == ZombieStormEnemyType.Boss ? 40 : enemy.Type == ZombieStormEnemyType.Elite ? 15 : UnityEngine.Random.value < 0.22f ? 1 : 0;
        SpawnPickup(enemy.transform.position, xp, coins);

        if (enemy.Type == ZombieStormEnemyType.Elite)
        {
            ShowFeedback("精英僵尸掉落强化能量，准备三选一。", 2.5f);
            if (player != null)
            {
                player.AddExperience(12);
            }
        }

        if (enemy.Type == ZombieStormEnemyType.Boss)
        {
            ShowFeedback("Boss 被击倒，尸潮暂时退散。", 3f);
            player.Heal(20f);
        }
    }

    public void RequestLevelUp()
    {
        if (leveling || finished)
        {
            return;
        }

        Time.timeScale = 0f;
        leveling = true;
        currentChoices.Clear();
        BuildUpgradeChoices();
        ShowFeedback("升级！选择一个构筑方向。", 2f);
    }

    public int GetPassiveLevel(ZombieStormPassiveType passive)
    {
        int level;
        return passives.TryGetValue(passive, out level) ? level : 0;
    }

    public Sprite GetWeaponSprite(ZombieStormWeaponType weapon)
    {
        if (weapon == ZombieStormWeaponType.SawRing)
        {
            return sawSprite;
        }

        if (weapon == ZombieStormWeaponType.Mine)
        {
            return mineSprite;
        }

        if (weapon == ZombieStormWeaponType.Molotov)
        {
            return fireSprite;
        }

        return bulletSprite;
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
        ShowFeedback(message, 999f);
    }

    private void StartRun()
    {
        runTime = 0f;
        spawnTimer = 0f;
        eliteTimer = 22f;
        bossCount = 0;
        finished = false;
        won = false;
        leveling = false;
        Time.timeScale = 1f;
        ClearActiveObjects();

        GameObject playerObject = new GameObject("Pixel Survivor");
        playerObject.transform.SetParent(worldRoot, false);
        playerObject.transform.position = Vector3.zero;
        SpriteRenderer playerRenderer = playerObject.AddComponent<SpriteRenderer>();
        playerRenderer.sprite = playerSprite;
        playerRenderer.sortingOrder = 20;
        playerObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
        player = playerObject.AddComponent<ZombieStormPlayer>();
        player.Initialize(this);

        ZombieStormSurvivorAnimator survivorAnimator = playerObject.AddComponent<ZombieStormSurvivorAnimator>();
        survivorAnimator.Initialize(this, playerRenderer, survivorSprites);

        weapons = playerObject.AddComponent<ZombieStormWeaponManager>();
        weapons.Initialize(this, player);
        weapons.UnlockWeapon(ZombieStormWeaponType.Pistol);

        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        ShowFeedback("第 1 波：手枪启动，边走位边捡经验。", 3f);
    }

    private void RestartRun()
    {
        StartRun();
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

        GameObject floor = CreateSpriteObject("Neon Asphalt", tileSprite, new Color(0.09f, 0.1f, 0.12f), new Vector3(0f, 0f, 4f), new Vector3(80f, 80f, 1f), 0);
        floor.transform.SetParent(worldRoot, false);

        for (int i = 0; i < 90; i++)
        {
            Vector2 position = UnityEngine.Random.insideUnitCircle * 34f;
            GameObject prop = CreateSpriteObject("Pixel Ruin", ruinSprite, UnityEngine.Random.value > 0.5f ? new Color(0.16f, 0.18f, 0.22f) : new Color(0.12f, 0.08f, 0.09f), position, new Vector3(UnityEngine.Random.Range(0.8f, 2.4f), UnityEngine.Random.Range(0.8f, 2.8f), 1f), 1);
            prop.transform.SetParent(worldRoot, false);
        }

        for (int i = -8; i <= 8; i++)
        {
            GameObject roadLine = CreateSpriteObject("Neon Road Line", tileSprite, new Color(0.05f, 0.8f, 1f, 0.35f), new Vector3(i * 4f, 0f, 2f), new Vector3(0.12f, 80f, 1f), 2);
            roadLine.transform.SetParent(worldRoot, false);
        }
    }

    private void CreateSprites()
    {
        playerSprite = CreatePixelSprite(new Color(0.3f, 0.9f, 1f), 16, true, new Color(1f, 0.95f, 0.35f));
        zombieSprite = CreatePixelSprite(new Color(0.35f, 0.95f, 0.35f), 16, true, new Color(0.08f, 0.28f, 0.08f));
        fastZombieSprite = CreatePixelSprite(new Color(0.75f, 1f, 0.25f), 16, true, new Color(0.1f, 0.36f, 0.08f));
        tankZombieSprite = CreatePixelSprite(new Color(0.58f, 0.8f, 0.35f), 16, true, new Color(0.16f, 0.28f, 0.12f));
        eliteZombieSprite = CreatePixelSprite(new Color(1f, 0.45f, 0.25f), 18, true, new Color(0.45f, 0.08f, 0.02f));
        bossSprite = CreatePixelSprite(new Color(0.95f, 0.12f, 0.12f), 24, true, new Color(0.32f, 0.02f, 0.02f));
        bulletSprite = CreatePixelSprite(new Color(1f, 0.92f, 0.22f), 8, true, Color.white);
        xpSprite = CreatePixelSprite(new Color(0.15f, 0.8f, 1f), 8, true, Color.white);
        coinSprite = CreatePixelSprite(new Color(1f, 0.75f, 0.18f), 8, true, new Color(1f, 0.95f, 0.6f));
        fireSprite = CreatePixelSprite(new Color(1f, 0.28f, 0.08f), 18, true, new Color(1f, 0.82f, 0.12f));
        sawSprite = CreatePixelSprite(new Color(0.82f, 0.84f, 0.9f), 14, true, new Color(0.2f, 0.75f, 1f));
        mineSprite = CreatePixelSprite(new Color(0.22f, 0.22f, 0.25f), 12, true, new Color(1f, 0.18f, 0.08f));
        tileSprite = CreatePixelSprite(Color.white, 8, false, Color.white);
        ruinSprite = CreatePixelSprite(Color.white, 12, false, new Color(0.06f, 0.06f, 0.08f));

        survivorSprites = LoadSurvivorSprites();
        if (survivorSprites.Length > 0)
        {
            playerSprite = survivorSprites[0];
        }
    }

    private void UpdateDynamicDifficulty()
    {
        float timeFactor = 1f + runTime / 55f;
        float healthFactor = player != null && player.Health / player.MaxHealth < 0.28f ? 0.72f : 1f;
        float dominanceFactor = player != null && player.Kills > runTime * 1.2f ? 1.22f : 1f;
        difficultyScore = timeFactor * healthFactor * dominanceFactor;
    }

    private void UpdateSpawning()
    {
        spawnTimer -= Time.deltaTime;
        eliteTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            spawnTimer = Mathf.Max(0.18f, 1.25f - runTime / 260f);
            int count = Mathf.Clamp(Mathf.RoundToInt(2f + difficultyScore * 1.25f), 2, 13);
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(PickEnemyType());
            }
        }

        if (eliteTimer <= 0f && runTime > 35f)
        {
            eliteTimer = Mathf.Max(18f, 44f - runTime * 0.04f);
            SpawnEnemy(ZombieStormEnemyType.Elite);
            ShowFeedback("精英僵尸出现：击杀可获得大量经验和金币。", 2.5f);
        }

        if ((bossCount == 0 && runTime >= 120f) || (bossCount == 1 && runTime >= 255f))
        {
            bossCount++;
            SpawnEnemy(ZombieStormEnemyType.Boss);
            ShowFeedback("Boss 波次来袭！注意冲刺和召唤。", 3.5f);
        }
    }

    private ZombieStormEnemyType PickEnemyType()
    {
        float roll = UnityEngine.Random.value;
        if (runTime > 80f && roll < 0.08f)
        {
            return ZombieStormEnemyType.Exploder;
        }

        if (runTime > 55f && roll < 0.2f)
        {
            return ZombieStormEnemyType.Tank;
        }

        if (runTime > 28f && roll < 0.42f && !(player != null && player.Health / player.MaxHealth < 0.25f))
        {
            return ZombieStormEnemyType.Fast;
        }

        return ZombieStormEnemyType.Grunt;
    }

    private void SpawnEnemy(ZombieStormEnemyType type)
    {
        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = Vector2.up;
        }

        float spawnDistance = mainCamera.orthographicSize * 1.65f + UnityEngine.Random.Range(1.5f, 4.5f);
        Vector2 spawnPos = (Vector2)player.transform.position + dir * spawnDistance;
        string poolKey = "enemy_" + type;
        GameObject enemyObject = SpawnPooled(poolKey, delegate { return CreateEnemy(type); });
        enemyObject.transform.SetParent(worldRoot, false);
        enemyObject.transform.position = spawnPos;

        ZombieStormEnemy enemy = enemyObject.GetComponent<ZombieStormEnemy>();
        enemy.Initialize(this, type, poolKey, GetEnemySprite(type), runTime, difficultyScore);
    }

    private Sprite GetEnemySprite(ZombieStormEnemyType type)
    {
        if (type == ZombieStormEnemyType.Fast)
        {
            return fastZombieSprite;
        }

        if (type == ZombieStormEnemyType.Tank || type == ZombieStormEnemyType.Exploder)
        {
            return tankZombieSprite;
        }

        if (type == ZombieStormEnemyType.Elite)
        {
            return eliteZombieSprite;
        }

        if (type == ZombieStormEnemyType.Boss)
        {
            return bossSprite;
        }

        return zombieSprite;
    }

    private GameObject CreateEnemy(ZombieStormEnemyType type)
    {
        GameObject enemyObject = new GameObject("Zombie " + type);
        SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 15;
        enemyObject.AddComponent<ZombieStormEnemy>();
        return enemyObject;
    }

    private GameObject CreatePlayerProjectile()
    {
        GameObject projectileObject = new GameObject("Pooled Player Bullet");
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = bulletSprite;
        renderer.sortingOrder = 25;
        projectileObject.AddComponent<ZombieStormProjectile>();
        return projectileObject;
    }

    private GameObject CreateEnemyProjectile()
    {
        GameObject projectileObject = new GameObject("Pooled Toxic Spit");
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = fireSprite;
        renderer.color = new Color(0.55f, 1f, 0.2f);
        renderer.sortingOrder = 24;
        projectileObject.AddComponent<ZombieStormEnemyProjectile>();
        return projectileObject;
    }

    private GameObject CreateAreaEffect()
    {
        GameObject effectObject = new GameObject("Pooled Area Effect");
        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = fireSprite;
        renderer.sortingOrder = 18;
        effectObject.AddComponent<ZombieStormAreaEffect>();
        return effectObject;
    }

    private GameObject CreateXpOrb()
    {
        GameObject orbObject = new GameObject("Pooled XP Orb");
        SpriteRenderer renderer = orbObject.AddComponent<SpriteRenderer>();
        renderer.sprite = xpSprite;
        renderer.sortingOrder = 10;
        orbObject.transform.localScale = Vector3.one * 0.34f;
        orbObject.AddComponent<ZombieStormPickup>();
        return orbObject;
    }

    private GameObject CreateCoin()
    {
        GameObject coinObject = new GameObject("Pooled Coin");
        SpriteRenderer renderer = coinObject.AddComponent<SpriteRenderer>();
        renderer.sprite = coinSprite;
        renderer.sortingOrder = 11;
        coinObject.transform.localScale = Vector3.one * 0.34f;
        coinObject.AddComponent<ZombieStormPickup>();
        return coinObject;
    }

    private void BuildUpgradeChoices()
    {
        List<ZombieStormUpgradeOption> pool = new List<ZombieStormUpgradeOption>();

        AddWeaponChoice(pool, ZombieStormWeaponType.Pistol, "手枪强化", "手枪伤害、射速和穿透提升。");
        AddWeaponChoice(pool, ZombieStormWeaponType.Shotgun, "解锁/强化霰弹枪", "扇形多弹丸，清理近距离尸潮。");
        AddWeaponChoice(pool, ZombieStormWeaponType.Molotov, "解锁/强化燃烧瓶", "随机投掷燃烧区域，持续烧伤僵尸。");
        AddWeaponChoice(pool, ZombieStormWeaponType.SawRing, "解锁/强化电锯环", "围绕自身旋转切割，适合贴身防御。");
        AddWeaponChoice(pool, ZombieStormWeaponType.Lightning, "解锁/强化雷电", "随机劈中敌人并逐级连锁。");
        AddWeaponChoice(pool, ZombieStormWeaponType.Mine, "解锁/强化地雷", "移动时在身后布置爆炸陷阱。");

        AddPassiveChoice(pool, ZombieStormPassiveType.Damage, "攻击力提升", "所有武器伤害 +18%。");
        AddPassiveChoice(pool, ZombieStormPassiveType.FireRate, "攻速芯片", "所有武器冷却 -8%。");
        AddPassiveChoice(pool, ZombieStormPassiveType.Area, "范围增幅器", "爆炸、火焰和光环范围 +16%。");
        AddPassiveChoice(pool, ZombieStormPassiveType.MoveSpeed, "疾跑鞋", "移动速度 +8%。");
        AddPassiveChoice(pool, ZombieStormPassiveType.PickupRange, "磁吸背包", "拾取范围 +18%。");
        AddPassiveChoice(pool, ZombieStormPassiveType.Crit, "暴击芯片", "暴击率 +7%，暴击造成双倍伤害。");
        AddPassiveChoice(pool, ZombieStormPassiveType.MaxHealth, "生命上限", "最大生命 +15，并立即治疗。");
        AddPassiveChoice(pool, ZombieStormPassiveType.CoinGain, "金币加成", "金币收益 +20%。");

        while (currentChoices.Count < 3 && pool.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            currentChoices.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    private void AddWeaponChoice(List<ZombieStormUpgradeOption> pool, ZombieStormWeaponType weapon, string title, string description)
    {
        int level = weapons != null ? weapons.GetWeaponLevel(weapon) : 0;
        if (level >= 5)
        {
            return;
        }

        string prefix = level == 0 ? "新武器：" : "Lv." + (level + 1) + " ";
        pool.Add(ZombieStormUpgradeOption.ForWeapon(prefix + title, description, weapon));
    }

    private void AddPassiveChoice(List<ZombieStormUpgradeOption> pool, ZombieStormPassiveType passive, string title, string description)
    {
        int level = GetPassiveLevel(passive);
        if (level >= 5)
        {
            return;
        }

        pool.Add(ZombieStormUpgradeOption.ForPassive("Lv." + (level + 1) + " " + title, description, passive));
    }

    private void ApplyUpgrade(int index)
    {
        if (index < 0 || index >= currentChoices.Count)
        {
            return;
        }

        ZombieStormUpgradeOption choice = currentChoices[index];
        if (choice.IsWeapon)
        {
            weapons.UpgradeWeapon(choice.Weapon);
            ShowFeedback(choice.Title + " 已装备。", 2.2f);
        }
        else
        {
            int level = GetPassiveLevel(choice.Passive) + 1;
            passives[choice.Passive] = level;
            if (choice.Passive == ZombieStormPassiveType.MaxHealth && player != null)
            {
                player.IncreaseMaxHealth(15f);
            }

            ShowFeedback(choice.Title + " 已生效。", 2.2f);
        }

        currentChoices.Clear();
        leveling = false;
        Time.timeScale = 1f;

        if (player != null && player.Experience >= player.ExperienceToNext)
        {
            player.TryLevelUp();
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
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 32;
        GUI.Label(new Rect(Screen.width * 0.5f - 180f, 92f, 420f, 48f), "升级三选一");
        GUI.skin.label.fontSize = 18;

        float cardWidth = 290f;
        float startX = Screen.width * 0.5f - cardWidth * 1.5f - 20f;
        for (int i = 0; i < currentChoices.Count; i++)
        {
            Rect card = new Rect(startX + i * (cardWidth + 20f), 170f, cardWidth, 230f);
            GUI.color = new Color(0.09f, 0.12f, 0.16f, 0.96f);
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = new Color(0.2f, 0.8f, 1f, 1f);
            GUI.DrawTexture(new Rect(card.x, card.y, card.width, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.skin.label.fontSize = 22;
            GUI.Label(new Rect(card.x + 18f, card.y + 22f, card.width - 36f, 54f), (i + 1) + ". " + currentChoices[i].Title);
            GUI.skin.label.fontSize = 17;
            GUI.Label(new Rect(card.x + 18f, card.y + 92f, card.width - 36f, 92f), currentChoices[i].Description);
            if (GUI.Button(new Rect(card.x + 42f, card.y + 176f, card.width - 84f, 36f), "选择 " + (i + 1)))
            {
                ApplyUpgrade(i);
            }
        }
    }

    private void DrawBar(Rect rect, float amount, Color fill, string label)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(amount), rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 8f, rect.y - 2f, rect.width, rect.height + 4f), label);
    }

    private void ShowFeedback(string text, float seconds)
    {
        feedbackText = text;
        feedbackUntil = Time.unscaledTime + seconds;
    }

    private void ClearActiveObjects()
    {
        enemies.Clear();
        if (worldRoot != null)
        {
            for (int i = worldRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = worldRoot.GetChild(i);
                if (child.name != "Neon Asphalt" && !child.name.StartsWith("Pixel Ruin") && !child.name.StartsWith("Neon Road Line"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        pools.Clear();
        if (poolRoot != null)
        {
            for (int i = poolRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(poolRoot.GetChild(i).gameObject);
            }
        }
    }

    private GameObject CreateSpriteObject(string objectName, Sprite sprite, Color color, Vector3 position, Vector3 scale, int sortingOrder)
    {
        GameObject item = new GameObject(objectName);
        item.transform.position = position;
        item.transform.localScale = scale;
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return item;
    }

    private Sprite CreatePixelSprite(Color primary, int size, bool circle, Color accent)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.47f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color color = primary;
                if (circle)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        color = Color.clear;
                    }
                    else if (distance > radius * 0.72f)
                    {
                        color = accent;
                    }
                    else if ((x + y) % 5 == 0)
                    {
                        color = Color.Lerp(primary, accent, 0.45f);
                    }
                }
                else if ((x + y) % 6 == 0)
                {
                    color = accent;
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite[] LoadSurvivorSprites()
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>("ZombieStorm/SurvivorFrames");
        Array.Sort(textures, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        Sprite[] sprites = new Sprite[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i];
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            sprites[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 300f);
        }

        return sprites;
    }

    private string FormatTime(int seconds)
    {
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
}

public sealed class ZombieStormUpgradeOption
{
    public string Title;
    public string Description;
    public bool IsWeapon;
    public ZombieStormWeaponType Weapon;
    public ZombieStormPassiveType Passive;

    public static ZombieStormUpgradeOption ForWeapon(string title, string description, ZombieStormWeaponType weapon)
    {
        return new ZombieStormUpgradeOption { Title = title, Description = description, IsWeapon = true, Weapon = weapon };
    }

    public static ZombieStormUpgradeOption ForPassive(string title, string description, ZombieStormPassiveType passive)
    {
        return new ZombieStormUpgradeOption { Title = title, Description = description, IsWeapon = false, Passive = passive };
    }
}

public sealed class ZombieStormSurvivorAnimator : MonoBehaviour
{
    private ZombieStormGameController game;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private int currentFrame;
    private int sequenceStart;
    private int sequenceEnd;
    private float frameTimer;
    private float frameRate = 10f;

    public void Initialize(ZombieStormGameController owner, SpriteRenderer renderer, Sprite[] survivorFrames)
    {
        game = owner;
        spriteRenderer = renderer;
        frames = survivorFrames ?? new Sprite[0];
        SetSequence(0, 15, 8f);
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
        {
            return;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        ZombieStormEnemy target = game != null ? game.FindNearestEnemy(transform.position, 12f) : null;
        bool attacking = target != null;
        bool moving = input.sqrMagnitude > 0.05f;

        if (target != null)
        {
            spriteRenderer.flipX = target.transform.position.x < transform.position.x;
        }
        else if (Mathf.Abs(input.x) > 0.05f)
        {
            spriteRenderer.flipX = input.x < 0f;
        }

        if (attacking)
        {
            SetSequence(80, 120, 18f);
        }
        else if (moving)
        {
            SetSequence(64, 80, 10f);
        }
        else
        {
            SetSequence(0, 15, 7f);
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame++;
            if (currentFrame > sequenceEnd)
            {
                currentFrame = sequenceStart;
            }
        }

        spriteRenderer.sprite = frames[Mathf.Clamp(currentFrame, 0, frames.Length - 1)];
    }

    private void SetSequence(int start, int end, float fps)
    {
        start = Mathf.Clamp(start, 0, Mathf.Max(0, frames.Length - 1));
        end = Mathf.Clamp(end, start, Mathf.Max(0, frames.Length - 1));
        if (sequenceStart == start && sequenceEnd == end)
        {
            return;
        }

        sequenceStart = start;
        sequenceEnd = end;
        currentFrame = sequenceStart;
        frameTimer = 0f;
        frameRate = Mathf.Max(1f, fps);
    }
}

public sealed class ZombieStormPlayer : MonoBehaviour
{
    private ZombieStormGameController game;
    private Rigidbody2D body;
    private float invulnerableUntil;

    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public int Level { get; private set; }
    public float Experience { get; private set; }
    public float ExperienceToNext { get; private set; }
    public int Coins { get; private set; }
    public int Kills { get; set; }

    public float MoveSpeed
    {
        get { return 5.1f + game.GetPassiveLevel(ZombieStormPassiveType.MoveSpeed) * 0.42f; }
    }

    public float PickupRange
    {
        get { return 1.35f + game.GetPassiveLevel(ZombieStormPassiveType.PickupRange) * 0.42f; }
    }

    public void Initialize(ZombieStormGameController owner)
    {
        game = owner;
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        MaxHealth = 100f;
        Health = MaxHealth;
        Level = 1;
        Experience = 0f;
        ExperienceToNext = 12f;
        Coins = 0;
        Kills = 0;
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        body.velocity = input * MoveSpeed;

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 target = transform.position + new Vector3(0f, 0f, -10f);
            camera.transform.position = Vector3.Lerp(camera.transform.position, target, 10f * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        if (Time.time < invulnerableUntil)
        {
            return;
        }

        Health = Mathf.Max(0f, Health - amount);
        invulnerableUntil = Time.time + 0.18f;
        if (Health <= 0f)
        {
            game.EndRun(false, "你被尸潮吞没了。");
        }
    }

    public void AddExperience(float amount)
    {
        Experience += amount;
        TryLevelUp();
    }

    public void TryLevelUp()
    {
        if (Experience >= ExperienceToNext)
        {
            Experience -= ExperienceToNext;
            Level++;
            ExperienceToNext = Mathf.Ceil(ExperienceToNext * 1.22f + 4f);
            game.RequestLevelUp();
        }
    }

    public void AddCoins(int amount)
    {
        Coins += Mathf.Max(0, amount);
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

public sealed class ZombieStormWeaponManager : MonoBehaviour
{
    private readonly Dictionary<ZombieStormWeaponType, int> weaponLevels = new Dictionary<ZombieStormWeaponType, int>();
    private readonly Dictionary<ZombieStormWeaponType, float> cooldowns = new Dictionary<ZombieStormWeaponType, float>();
    private readonly List<GameObject> sawBlades = new List<GameObject>();

    private ZombieStormGameController game;
    private ZombieStormPlayer player;
    private float mineStepDistance;
    private Vector2 lastMinePosition;

    public void Initialize(ZombieStormGameController owner, ZombieStormPlayer survivor)
    {
        game = owner;
        player = survivor;
        lastMinePosition = transform.position;
    }

    private void Update()
    {
        if (game == null || player == null)
        {
            return;
        }

        TickWeapon(ZombieStormWeaponType.Pistol, FirePistol);
        TickWeapon(ZombieStormWeaponType.Shotgun, FireShotgun);
        TickWeapon(ZombieStormWeaponType.Molotov, FireMolotov);
        TickWeapon(ZombieStormWeaponType.Lightning, FireLightning);
        TickWeapon(ZombieStormWeaponType.Mine, DropMine);
        UpdateSawRing();
    }

    public int GetWeaponLevel(ZombieStormWeaponType weapon)
    {
        int level;
        return weaponLevels.TryGetValue(weapon, out level) ? level : 0;
    }

    public void UnlockWeapon(ZombieStormWeaponType weapon)
    {
        if (!weaponLevels.ContainsKey(weapon))
        {
            weaponLevels[weapon] = 1;
            cooldowns[weapon] = 0f;
            if (weapon == ZombieStormWeaponType.SawRing)
            {
                RebuildSawRing();
            }
        }
    }

    public void UpgradeWeapon(ZombieStormWeaponType weapon)
    {
        if (!weaponLevels.ContainsKey(weapon))
        {
            UnlockWeapon(weapon);
            return;
        }

        weaponLevels[weapon] = Mathf.Min(5, weaponLevels[weapon] + 1);
        if (weapon == ZombieStormWeaponType.SawRing)
        {
            RebuildSawRing();
        }
    }

    public string GetLoadoutText()
    {
        string text = "Weapons\n";
        foreach (KeyValuePair<ZombieStormWeaponType, int> pair in weaponLevels)
        {
            text += pair.Key + " Lv." + pair.Value + "\n";
        }

        return text;
    }

    private delegate void WeaponAction(int level);

    private void TickWeapon(ZombieStormWeaponType weapon, WeaponAction action)
    {
        int level = GetWeaponLevel(weapon);
        if (level <= 0)
        {
            return;
        }

        cooldowns[weapon] = cooldowns[weapon] - Time.deltaTime;
        if (cooldowns[weapon] <= 0f)
        {
            action(level);
        }
    }

    private void FirePistol(int level)
    {
        ZombieStormEnemy target = game.FindNearestEnemy(transform.position, 14f);
        if (target == null)
        {
            cooldowns[ZombieStormWeaponType.Pistol] = 0.2f;
            return;
        }

        Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        float damage = RollDamage(10f + level * 3.5f);
        int pierce = level >= 4 ? 2 : level >= 2 ? 1 : 0;
        game.SpawnPlayerProjectile(transform.position, direction, damage, 13f, 1.4f, pierce, new Color(1f, 0.92f, 0.25f), 0.28f);
        cooldowns[ZombieStormWeaponType.Pistol] = (0.62f - level * 0.055f) * game.CooldownMultiplier;
    }

    private void FireShotgun(int level)
    {
        ZombieStormEnemy target = game.FindNearestEnemy(transform.position, 9f);
        Vector2 forward = target != null ? ((Vector2)target.transform.position - (Vector2)transform.position).normalized : UnityEngine.Random.insideUnitCircle.normalized;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector2.up;
        }

        int pelletCount = 4 + level;
        float arc = 48f + level * 5f;
        for (int i = 0; i < pelletCount; i++)
        {
            float t = pelletCount == 1 ? 0.5f : i / (float)(pelletCount - 1);
            float angle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, t);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * forward;
            game.SpawnPlayerProjectile(transform.position, direction, RollDamage(7f + level * 2f), 10.5f, 0.75f, 0, new Color(1f, 0.55f, 0.22f), 0.24f);
        }

        cooldowns[ZombieStormWeaponType.Shotgun] = (2.2f - level * 0.16f) * game.CooldownMultiplier;
    }

    private void FireMolotov(int level)
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2.5f, 6.8f);
        Vector2 position = (Vector2)transform.position + offset;
        float radius = (1.25f + level * 0.22f) * game.AreaMultiplier;
        float duration = 2.4f + level * 0.32f;
        game.SpawnAreaEffect(position, radius, RollDamage(4.2f + level * 1.3f), duration, 0.28f, new Color(1f, 0.25f, 0.05f, 0.62f), "fire_pool");
        cooldowns[ZombieStormWeaponType.Molotov] = (4.1f - level * 0.25f) * game.CooldownMultiplier;
    }

    private void FireLightning(int level)
    {
        int strikes = 1 + Mathf.FloorToInt(level * 0.8f);
        for (int i = 0; i < strikes; i++)
        {
            ZombieStormEnemy target = game.FindRandomEnemy();
            if (target != null)
            {
                target.TakeDamage(RollDamage(18f + level * 5f), (target.transform.position - transform.position).normalized);
                game.SpawnAreaEffect(target.transform.position, 0.65f * game.AreaMultiplier, 0f, 0.18f, 1f, new Color(0.25f, 0.85f, 1f, 0.78f), "lightning_flash");
            }
        }

        cooldowns[ZombieStormWeaponType.Lightning] = (3.6f - level * 0.22f) * game.CooldownMultiplier;
    }

    private void DropMine(int level)
    {
        mineStepDistance += Vector2.Distance(transform.position, lastMinePosition);
        lastMinePosition = transform.position;
        if (mineStepDistance < Mathf.Max(1.2f, 2.4f - level * 0.22f))
        {
            cooldowns[ZombieStormWeaponType.Mine] = 0.15f;
            return;
        }

        mineStepDistance = 0f;
        float radius = (1.1f + level * 0.25f) * game.AreaMultiplier;
        game.SpawnAreaEffect(transform.position, radius, RollDamage(18f + level * 4f), 3.2f, 99f, new Color(1f, 0.7f, 0.1f, 0.45f), "mine_blast");
        cooldowns[ZombieStormWeaponType.Mine] = (1.3f - level * 0.1f) * game.CooldownMultiplier;
    }

    private void UpdateSawRing()
    {
        int level = GetWeaponLevel(ZombieStormWeaponType.SawRing);
        if (level <= 0)
        {
            return;
        }

        float radius = (1.45f + level * 0.18f) * game.AreaMultiplier;
        float speed = 120f + level * 28f;
        for (int i = 0; i < sawBlades.Count; i++)
        {
            float angle = Time.time * speed + i * (360f / sawBlades.Count);
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            sawBlades[i].transform.position = (Vector2)transform.position + offset;
            sawBlades[i].transform.Rotate(0f, 0f, 360f * Time.deltaTime);
        }

        if (cooldowns.ContainsKey(ZombieStormWeaponType.SawRing))
        {
            cooldowns[ZombieStormWeaponType.SawRing] -= Time.deltaTime;
        }
        else
        {
            cooldowns[ZombieStormWeaponType.SawRing] = 0f;
        }

        if (cooldowns[ZombieStormWeaponType.SawRing] <= 0f)
        {
            IReadOnlyList<ZombieStormEnemy> enemies = game.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                ZombieStormEnemy enemy = enemies[i];
                if (enemy != null && !enemy.IsDead && Vector2.Distance(enemy.transform.position, transform.position) <= radius + enemy.Radius)
                {
                    enemy.TakeDamage(RollDamage(6f + level * 1.8f), (enemy.transform.position - transform.position).normalized);
                }
            }

            cooldowns[ZombieStormWeaponType.SawRing] = 0.24f * game.CooldownMultiplier;
        }
    }

    private void RebuildSawRing()
    {
        for (int i = 0; i < sawBlades.Count; i++)
        {
            if (sawBlades[i] != null)
            {
                Destroy(sawBlades[i]);
            }
        }

        sawBlades.Clear();
        int level = GetWeaponLevel(ZombieStormWeaponType.SawRing);
        int count = 2 + Mathf.FloorToInt(level * 0.75f);
        for (int i = 0; i < count; i++)
        {
            GameObject blade = new GameObject("Saw Ring Blade");
            blade.transform.localScale = Vector3.one * 0.38f;
            SpriteRenderer renderer = blade.AddComponent<SpriteRenderer>();
            renderer.sprite = game.GetWeaponSprite(ZombieStormWeaponType.SawRing);
            renderer.sortingOrder = 26;
            sawBlades.Add(blade);
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

public sealed class ZombieStormEnemy : MonoBehaviour
{
    private ZombieStormGameController game;
    private SpriteRenderer spriteRenderer;
    private string poolKey;
    private float health;
    private float maxHealth;
    private float speed;
    private float damagePerSecond;
    private float bossActionTimer;
    private float sprintTimer;
    private bool sprinting;

    public ZombieStormEnemyType Type { get; private set; }
    public bool IsDead { get; private set; }
    public float Radius { get; private set; }

    public void Initialize(ZombieStormGameController owner, ZombieStormEnemyType enemyType, string key, Sprite sprite, float runTime, float difficulty)
    {
        game = owner;
        Type = enemyType;
        poolKey = key;
        IsDead = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;

        float hpScale = 1f + runTime / 145f;
        Radius = 0.42f;
        speed = 1.8f;
        damagePerSecond = 9f;
        maxHealth = 22f * hpScale;
        transform.localScale = Vector3.one * 0.95f;

        if (Type == ZombieStormEnemyType.Fast)
        {
            speed = 3.45f;
            maxHealth = 15f * hpScale;
            damagePerSecond = 11f;
            Radius = 0.34f;
            transform.localScale = Vector3.one * 0.78f;
        }
        else if (Type == ZombieStormEnemyType.Tank)
        {
            speed = 1.15f;
            maxHealth = 80f * hpScale;
            damagePerSecond = 13f;
            Radius = 0.6f;
            transform.localScale = Vector3.one * 1.35f;
        }
        else if (Type == ZombieStormEnemyType.Exploder)
        {
            speed = 2.15f;
            maxHealth = 34f * hpScale;
            damagePerSecond = 5f;
            Radius = 0.48f;
            transform.localScale = Vector3.one * 1.05f;
            spriteRenderer.color = new Color(1f, 0.85f, 0.18f);
        }
        else if (Type == ZombieStormEnemyType.Elite)
        {
            speed = 2.05f;
            maxHealth = 180f * hpScale;
            damagePerSecond = 18f;
            Radius = 0.75f;
            transform.localScale = Vector3.one * 1.62f;
        }
        else if (Type == ZombieStormEnemyType.Boss)
        {
            speed = 1.25f;
            maxHealth = 900f * Mathf.Max(1f, difficulty);
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
        if (game == null || game.player == null || IsDead)
        {
            return;
        }

        Vector2 toPlayer = game.player.transform.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.01f ? toPlayer / distance : Vector2.zero;

        if (Type == ZombieStormEnemyType.Fast)
        {
            sprintTimer -= Time.deltaTime;
            if (sprintTimer <= 0f)
            {
                sprinting = !sprinting;
                sprintTimer = sprinting ? 0.65f : 1.1f;
            }
        }

        if (Type == ZombieStormEnemyType.Boss)
        {
            UpdateBoss(direction, distance);
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
                game.SpawnAreaEffect(transform.position, 2.2f, 28f, 0.22f, 99f, new Color(1f, 0.35f, 0.05f, 0.65f), "zombie_explosion");
                game.player.TakeDamage(22f);
                Die(false);
            }
            else
            {
                game.player.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }

        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    public void TakeDamage(float amount, Vector2 impulse)
    {
        if (IsDead)
        {
            return;
        }

        health -= amount;
        transform.position += (Vector3)(impulse.normalized * 0.035f);
        spriteRenderer.color = Color.Lerp(Color.white, Color.red, 0.32f);
        if (health <= 0f)
        {
            Die(true);
        }
    }

    private void UpdateBoss(Vector2 direction, float distance)
    {
        bool enraged = health < maxHealth * 0.5f;
        transform.position += (Vector3)(direction * speed * (enraged ? 1.38f : 1f) * Time.deltaTime);
        bossActionTimer -= Time.deltaTime;
        if (bossActionTimer > 0f)
        {
            return;
        }

        int action = UnityEngine.Random.Range(0, 3);
        if (action == 0)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 shotDir = Quaternion.Euler(0f, 0f, i * 30f) * Vector2.up;
                game.SpawnEnemyProjectile(transform.position, shotDir, enraged ? 16f : 10f, 4.2f, 4f);
            }
        }
        else if (action == 1)
        {
            for (int i = 0; i < (enraged ? 8 : 5); i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.8f, 3.5f);
                game.SpawnAreaEffect((Vector2)transform.position + offset, 0.95f, 8f, 2.4f, 0.45f, new Color(0.55f, 1f, 0.15f, 0.38f), "toxic_pool");
            }
        }
        else
        {
            transform.position += (Vector3)(direction * (enraged ? 3.8f : 2.5f));
        }

        bossActionTimer = enraged ? 2.2f : 3.2f;
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

        IReadOnlyList<ZombieStormEnemy> enemies = game.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            ZombieStormEnemy enemy = enemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) <= enemy.Radius + 0.16f)
            {
                enemy.TakeDamage(damage, direction);
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

        if (game.player != null && Vector2.Distance(transform.position, game.player.transform.position) <= 0.5f)
        {
            game.player.TakeDamage(damage);
            game.ReturnPooled("enemy_spit", gameObject);
        }
    }
}

public sealed class ZombieStormAreaEffect : MonoBehaviour
{
    private ZombieStormGameController game;
    private string poolKey;
    private float radius;
    private float damage;
    private float life;
    private float tickRate;
    private float tickTimer;
    private bool mineTriggered;

    public void Initialize(ZombieStormGameController owner, string key, float areaRadius, float hitDamage, float duration, float rate)
    {
        game = owner;
        poolKey = key;
        radius = areaRadius;
        damage = hitDamage;
        life = duration;
        tickRate = rate;
        tickTimer = 0f;
        mineTriggered = false;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (poolKey == "mine_blast" && !mineTriggered)
        {
            bool hasTarget = false;
            IReadOnlyList<ZombieStormEnemy> enemies = game.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                ZombieStormEnemy enemy = enemies[i];
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
            GetComponent<SpriteRenderer>().color = new Color(1f, 0.4f, 0.05f, 0.74f);
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

    private void DamageEnemies()
    {
        if (damage <= 0f)
        {
            return;
        }

        IReadOnlyList<ZombieStormEnemy> enemies = game.Enemies;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            ZombieStormEnemy enemy = enemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) <= radius + enemy.Radius)
            {
                enemy.TakeDamage(damage, (enemy.transform.position - transform.position).normalized);
            }
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
        if (game == null || game.player == null)
        {
            return;
        }

        Vector2 toPlayer = game.player.transform.position - transform.position;
        float distance = toPlayer.magnitude;
        float pickupRange = game.player.PickupRange;
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
                game.player.AddExperience(xp);
            }

            if (coins > 0)
            {
                game.player.AddCoins(coins);
            }

            game.ReturnPooled(poolKey, gameObject);
        }
    }
}
