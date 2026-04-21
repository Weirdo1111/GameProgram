using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Header("One Bullet Vertical Slice")]
    [SerializeField] private int startLevel = 1;

    private void Awake()
    {
        if (FindObjectOfType<OneBulletGameController>() != null)
        {
            return;
        }

        name = "One Bullet Bootstrap";
        Application.targetFrameRate = 120;
        Time.fixedDeltaTime = 1f / 60f;
        Physics.gravity = new Vector3(0f, -20f, 0f);

        OneBulletGameController controller = gameObject.AddComponent<OneBulletGameController>();
        controller.firstLevel = Mathf.Clamp(startLevel - 1, 0, 4);
    }
}

public class OneBulletGameController : MonoBehaviour
{
    public static OneBulletGameController Instance { get; private set; }

    public int firstLevel;
    public OneBulletController bullet;
    public BulletCameraController cameraController;
    public readonly List<OneBulletEnemy> enemies = new List<OneBulletEnemy>();
    public readonly List<Transform> coverNodes = new List<Transform>();

    private const float MinX = -24f;
    private const float MaxX = 24f;
    private const float MinY = 0.8f;
    private const float MaxY = 11.5f;
    private const float MinZ = -29f;
    private const float MaxZ = 35f;

    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private GameObject levelRoot;
    private GameObject worldRoot;
    private Camera mainCamera;
    private GameState state;
    private int levelIndex;
    private int attempts;
    private int killedEnemies;
    private float levelTimer;
    private float flightLimit = 42f;
    private string statusText;

    private enum GameState
    {
        Ready,
        Flying,
        Won,
        Lost,
        Complete
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CreateMaterials();
        ConfigureCamera();
        BuildPersistentObjects();
        LoadLevel(Mathf.Clamp(firstLevel, 0, 4));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
            return;
        }

        if (state == GameState.Ready && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            LaunchBullet();
        }
        else if (state == GameState.Won && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            LoadLevel(levelIndex + 1);
        }
        else if ((state == GameState.Lost || state == GameState.Complete) && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            RestartLevel();
        }

        if (state == GameState.Flying)
        {
            levelTimer += Time.deltaTime;
            if (levelTimer >= flightLimit)
            {
                FailLevel("Signal lost: flight time expired.");
            }
        }
    }

    public bool IsFlying
    {
        get { return state == GameState.Flying; }
    }

    public bool IsInsideBounds(Vector3 position)
    {
        return position.x >= MinX && position.x <= MaxX &&
               position.y >= MinY && position.y <= MaxY &&
               position.z >= MinZ && position.z <= MaxZ;
    }

    public Vector3 ClampToArena(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, MinX + 1.2f, MaxX - 1.2f);
        position.y = Mathf.Clamp(position.y, 1.2f, MaxY - 1.2f);
        position.z = Mathf.Clamp(position.z, MinZ + 1.2f, MaxZ - 1.2f);
        return position;
    }

    public Material GetMaterial(string key)
    {
        return materials[key];
    }

    public void RegisterEnemy(OneBulletEnemy enemy)
    {
        enemies.Add(enemy);
        enemy.coverNodes = coverNodes;
    }

    public void RegisterCoverNode(Vector3 position)
    {
        GameObject node = new GameObject("Cover Node");
        node.transform.SetParent(levelRoot.transform, false);
        node.transform.position = position;
        coverNodes.Add(node.transform);
    }

    public void NotifyEnemyKilled(OneBulletEnemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        killedEnemies++;
        if (killedEnemies >= enemies.Count && state == GameState.Flying)
        {
            WinLevel();
        }
    }

    public void FailLevel(string reason)
    {
        if (state != GameState.Flying)
        {
            return;
        }

        state = GameState.Lost;
        statusText = reason;
        if (bullet != null)
        {
            bullet.StopFlight();
        }
    }

    private void LaunchBullet()
    {
        attempts++;
        levelTimer = 0f;
        killedEnemies = 0;
        statusText = "Neural link active. Guide the round.";
        state = GameState.Flying;
        bullet.Launch();
    }

    private void RestartLevel()
    {
        LoadLevel(levelIndex);
    }

    private void WinLevel()
    {
        if (levelIndex >= 4)
        {
            state = GameState.Complete;
            statusText = "Vertical slice cleared. All five arenas completed.";
        }
        else
        {
            state = GameState.Won;
            statusText = "Area clear. Space or click to continue.";
        }

        if (bullet != null)
        {
            bullet.StopFlight();
        }
    }

    private void LoadLevel(int index)
    {
        if (index > 4)
        {
            index = 4;
        }

        levelIndex = index;
        killedEnemies = 0;
        levelTimer = 0f;
        statusText = "Space or click to launch the one bullet.";
        state = GameState.Ready;
        enemies.Clear();
        coverNodes.Clear();

        if (levelRoot != null)
        {
            Destroy(levelRoot);
        }

        levelRoot = new GameObject("Level " + (levelIndex + 1));
        levelRoot.transform.SetParent(worldRoot.transform, false);

        BuildArenaShell();
        BuildLevelContent(levelIndex);

        Vector3 startPosition = new Vector3(0f, 2.7f, -24f);
        bullet.ResetBullet(startPosition, Vector3.forward);
        cameraController.SetOverview(startPosition + new Vector3(0f, 0.7f, 8f));
    }

    private void CreateMaterials()
    {
        materials["floor"] = MakeMaterial("Mat Floor Graphite", new Color(0.08f, 0.1f, 0.11f), 0.05f, 0.58f);
        materials["wall"] = MakeMaterial("Mat Wall Charcoal", new Color(0.17f, 0.19f, 0.21f), 0.1f, 0.48f);
        materials["danger"] = MakeMaterial("Mat Collision Red", new Color(0.95f, 0.16f, 0.14f), 0.05f, 0.38f);
        materials["cover"] = MakeMaterial("Mat Cover Slate", new Color(0.29f, 0.35f, 0.38f), 0.18f, 0.45f);
        materials["lane"] = MakeEmissiveMaterial("Mat Route Cyan", new Color(0.1f, 0.78f, 1f), 1.7f);
        materials["bullet"] = MakeEmissiveMaterial("Mat Bullet Gold", new Color(1f, 0.82f, 0.22f), 2.4f);
        materials["enemy"] = MakeMaterial("Mat Enemy Idle", new Color(0.72f, 0.78f, 0.82f), 0.08f, 0.44f);
        materials["enemyAlert"] = MakeEmissiveMaterial("Mat Enemy Alert", new Color(1f, 0.37f, 0.12f), 1.35f);
        materials["enemyDead"] = MakeMaterial("Mat Enemy Down", new Color(0.1f, 0.1f, 0.1f), 0.05f, 0.2f);
        materials["coverNode"] = MakeEmissiveMaterial("Mat Cover Node", new Color(0.45f, 1f, 0.36f), 1.2f);
    }

    private Material MakeMaterial(string materialName, Color color, float metallic, float smoothness)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = materialName;
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    private Material MakeEmissiveMaterial(string materialName, Color color, float intensity)
    {
        Material material = MakeMaterial(materialName, color, 0f, 0.65f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * intensity);
        return material;
    }

    private void ConfigureCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.name = "One Bullet Camera";
        mainCamera.fieldOfView = 67f;
        mainCamera.nearClipPlane = 0.08f;
        mainCamera.farClipPlane = 170f;
        mainCamera.clearFlags = CameraClearFlags.Skybox;
    }

    private void BuildPersistentObjects()
    {
        worldRoot = new GameObject("One Bullet Runtime");

        GameObject bulletObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletObject.name = "Guided One Bullet";
        bulletObject.transform.SetParent(worldRoot.transform, false);
        bulletObject.transform.localScale = Vector3.one * 0.52f;
        bulletObject.GetComponent<Renderer>().sharedMaterial = materials["bullet"];
        Destroy(bulletObject.GetComponent<Collider>());

        Light bulletLight = bulletObject.AddComponent<Light>();
        bulletLight.color = new Color(1f, 0.78f, 0.22f);
        bulletLight.intensity = 2.4f;
        bulletLight.range = 6f;

        TrailRenderer trail = bulletObject.AddComponent<TrailRenderer>();
        trail.time = 1.25f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0.02f;
        trail.numCapVertices = 5;
        trail.material = materials["bullet"];

        bullet = bulletObject.AddComponent<OneBulletController>();
        bullet.game = this;
        bullet.trail = trail;

        cameraController = mainCamera.gameObject.AddComponent<BulletCameraController>();
        cameraController.target = bullet.transform;
        cameraController.bullet = bullet;
    }

    private void BuildArenaShell()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.04f, 0.055f, 0.06f);
        RenderSettings.fogDensity = 0.013f;
        RenderSettings.ambientIntensity = 0.58f;

        Light sun = FindObjectOfType<Light>();
        if (sun != null)
        {
            sun.name = "Tactical Sun";
            sun.type = LightType.Directional;
            sun.color = new Color(0.9f, 0.96f, 1f);
            sun.intensity = 1.05f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        }

        CreateHazardBox("Floor", new Vector3(0f, 0f, 3f), new Vector3(52f, 0.55f, 70f), materials["floor"]);
        CreateHazardBox("Ceiling Limit", new Vector3(0f, 12.2f, 3f), new Vector3(52f, 0.35f, 70f), materials["wall"]);
        CreateHazardBox("Left Boundary", new Vector3(MinX - 0.5f, 6f, 3f), new Vector3(1f, 12f, 70f), materials["wall"]);
        CreateHazardBox("Right Boundary", new Vector3(MaxX + 0.5f, 6f, 3f), new Vector3(1f, 12f, 70f), materials["wall"]);
        CreateHazardBox("Back Boundary", new Vector3(0f, 6f, MinZ - 0.5f), new Vector3(52f, 12f, 1f), materials["wall"]);
        CreateHazardBox("Far Boundary", new Vector3(0f, 6f, MaxZ + 0.5f), new Vector3(52f, 12f, 1f), materials["wall"]);

        CreateBox("Launch Rail", new Vector3(0f, 0.32f, -24f), new Vector3(7f, 0.1f, 1.6f), materials["lane"], false);
        CreatePointLight("Launch Glow", new Vector3(0f, 3.5f, -24f), new Color(0.1f, 0.75f, 1f), 2.2f, 12f);
        CreatePointLight("Mid Arena Glow", new Vector3(0f, 9f, 4f), new Color(0.55f, 1f, 0.65f), 1.4f, 24f);
    }

    private void BuildLevelContent(int index)
    {
        switch (index)
        {
            case 0:
                flightLimit = 28f;
                SpawnEnemy("Static Target A", OneBulletEnemy.EnemyKind.Static, new Vector3(-5f, 2.1f, 8f));
                SpawnEnemy("Static Target B", OneBulletEnemy.EnemyKind.Static, new Vector3(6f, 2.1f, 20f));
                CreateBox("Aim Lane", new Vector3(0f, 0.45f, 8f), new Vector3(2f, 0.12f, 42f), materials["lane"], false);
                break;
            case 1:
                flightLimit = 34f;
                CreateGate(0f, -2f, 5f);
                CreateGate(0f, 12f, -6f);
                SpawnEnemy("Static Target A", OneBulletEnemy.EnemyKind.Static, new Vector3(-8f, 2.1f, 7f));
                SpawnEnemy("Static Target B", OneBulletEnemy.EnemyKind.Static, new Vector3(8f, 4.2f, 16f));
                SpawnEnemy("Static Target C", OneBulletEnemy.EnemyKind.Static, new Vector3(0f, 2.1f, 27f));
                break;
            case 2:
                flightLimit = 38f;
                CreateGate(-4f, -1f, -6f);
                CreateHazardBox("Center Splitter", new Vector3(0f, 3f, 13f), new Vector3(2.8f, 5.4f, 10f), materials["danger"]);
                SpawnEnemy("Training Target", OneBulletEnemy.EnemyKind.Static, new Vector3(0f, 2.1f, 5f));
                SpawnEnemy("Reactive Target A", OneBulletEnemy.EnemyKind.Dodger, new Vector3(-9f, 2.1f, 20f));
                SpawnEnemy("Reactive Target B", OneBulletEnemy.EnemyKind.Dodger, new Vector3(9f, 2.1f, 25f));
                break;
            case 3:
                flightLimit = 42f;
                CreateCoverBlock(new Vector3(-8f, 2.4f, 12f), new Vector3(3f, 4.8f, 4f));
                CreateCoverBlock(new Vector3(9f, 2.4f, 19f), new Vector3(4f, 4.8f, 3f));
                CreateHazardBox("Low Ceiling Slab", new Vector3(0f, 7.7f, 8f), new Vector3(18f, 1f, 5f), materials["danger"]);
                SpawnEnemy("Static Target", OneBulletEnemy.EnemyKind.Static, new Vector3(0f, 2.1f, 5f));
                SpawnEnemy("Dodger A", OneBulletEnemy.EnemyKind.Dodger, new Vector3(-13f, 2.1f, 22f));
                SpawnEnemy("Dodger B", OneBulletEnemy.EnemyKind.Dodger, new Vector3(13f, 2.1f, 26f));
                SpawnEnemy("Cover Seeker", OneBulletEnemy.EnemyKind.Cover, new Vector3(0f, 2.1f, 30f));
                break;
            default:
                flightLimit = 46f;
                CreateGate(0f, -3f, 4f);
                CreateGate(0f, 10f, -5f);
                CreateCoverBlock(new Vector3(-11f, 2.3f, 16f), new Vector3(3.2f, 4.6f, 4.2f));
                CreateCoverBlock(new Vector3(11f, 2.3f, 22f), new Vector3(3.2f, 4.6f, 4.2f));
                CreateHazardBox("Final Needle A", new Vector3(-4f, 4.5f, 27f), new Vector3(2f, 8f, 2f), materials["danger"]);
                CreateHazardBox("Final Needle B", new Vector3(4f, 4.5f, 27f), new Vector3(2f, 8f, 2f), materials["danger"]);
                SpawnEnemy("Opening Target", OneBulletEnemy.EnemyKind.Static, new Vector3(-7f, 2.1f, 7f));
                SpawnEnemy("Dodger A", OneBulletEnemy.EnemyKind.Dodger, new Vector3(8f, 2.1f, 14f));
                SpawnEnemy("Cover Seeker A", OneBulletEnemy.EnemyKind.Cover, new Vector3(-12f, 2.1f, 25f));
                SpawnEnemy("Cover Seeker B", OneBulletEnemy.EnemyKind.Cover, new Vector3(12f, 2.1f, 29f));
                SpawnEnemy("Final Target", OneBulletEnemy.EnemyKind.Dodger, new Vector3(0f, 5f, 32f));
                break;
        }
    }

    private void CreateGate(float xOffset, float z, float gapX)
    {
        CreateHazardBox("Gate Left", new Vector3(-15f + xOffset, 3.2f, z), new Vector3(16f + gapX, 5.8f, 1.2f), materials["danger"]);
        CreateHazardBox("Gate Right", new Vector3(16f + xOffset, 3.2f, z), new Vector3(15f - gapX, 5.8f, 1.2f), materials["danger"]);
        CreateHazardBox("Gate Top", new Vector3(xOffset, 8.4f, z), new Vector3(12f, 2.2f, 1.2f), materials["danger"]);
    }

    private void CreateCoverBlock(Vector3 position, Vector3 scale)
    {
        CreateBox("AI Cover", position, scale, materials["cover"], true);
        RegisterCoverNode(position + new Vector3(-scale.x * 0.7f, 0.1f, 0f));
        RegisterCoverNode(position + new Vector3(scale.x * 0.7f, 0.1f, 0f));
        RegisterCoverNode(position + new Vector3(0f, 0.1f, scale.z * 0.85f));
    }

    private OneBulletEnemy SpawnEnemy(string enemyName, OneBulletEnemy.EnemyKind kind, Vector3 position)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = enemyName;
        enemy.transform.SetParent(levelRoot.transform, false);
        enemy.transform.position = position;
        enemy.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
        enemy.GetComponent<Renderer>().sharedMaterial = materials["enemy"];

        OneBulletEnemy enemyController = enemy.AddComponent<OneBulletEnemy>();
        enemyController.kind = kind;
        enemyController.game = this;
        enemyController.idleMaterial = materials["enemy"];
        enemyController.alertMaterial = materials["enemyAlert"];
        enemyController.deadMaterial = materials["enemyDead"];
        RegisterEnemy(enemyController);

        return enemyController;
    }

    private GameObject CreateHazardBox(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        return CreateBox(objectName, position, scale, material, true);
    }

    private GameObject CreateBox(string objectName, Vector3 position, Vector3 scale, Material material, bool hazard)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(levelRoot.transform, false);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;

        if (hazard)
        {
            box.AddComponent<BulletHazard>();
        }
        else
        {
            Destroy(box.GetComponent<Collider>());
        }

        return box;
    }

    private void CreatePointLight(string objectName, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(objectName);
        lightObject.transform.SetParent(levelRoot.transform, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 18;

        int remaining = Mathf.Max(0, enemies.Count - killedEnemies);
        GUI.Label(new Rect(20f, 18f, 720f, 28f), "One Bullet | Level " + (levelIndex + 1) + "/5 | Remaining: " + remaining + " | Attempts: " + attempts);
        GUI.Label(new Rect(20f, 46f, 720f, 28f), "Time: " + levelTimer.ToString("0.0") + " / " + flightLimit.ToString("0") + " | " + statusText);
        GUI.Label(new Rect(20f, 74f, 900f, 28f), "W/S steer up/down | A/D steer left/right | Shift boost | R restart | Space/click launch/continue");

        if (state == GameState.Ready || state == GameState.Won || state == GameState.Lost || state == GameState.Complete)
        {
            GUI.skin.label.fontSize = 28;
            string prompt = state == GameState.Ready ? "Launch the single guided bullet" :
                state == GameState.Won ? "Area clear" :
                state == GameState.Complete ? "Vertical slice complete" : "Trajectory failed";
            GUI.Label(new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 26f, 520f, 40f), prompt);
        }
    }
}

public class OneBulletController : MonoBehaviour
{
    public OneBulletGameController game;
    public TrailRenderer trail;
    public float baseSpeed = 18f;
    public float boostSpeed = 25f;
    public float steerStrength = 1.65f;
    public float radius = 0.32f;
    public float predictionSeconds = 1.2f;

    private Vector3 direction = Vector3.forward;
    private bool flying;

    public bool IsFlying
    {
        get { return flying; }
    }

    public Vector3 Direction
    {
        get { return direction; }
    }

    public float CurrentSpeed
    {
        get { return Input.GetKey(KeyCode.LeftShift) ? boostSpeed : baseSpeed; }
    }

    public void ResetBullet(Vector3 position, Vector3 startDirection)
    {
        flying = false;
        transform.position = position;
        direction = startDirection.normalized;
        gameObject.SetActive(true);
        if (trail != null)
        {
            trail.Clear();
        }
    }

    public void Launch()
    {
        flying = true;
        if (trail != null)
        {
            trail.Clear();
        }
    }

    public void StopFlight()
    {
        flying = false;
    }

    public Vector3 PredictPosition(float seconds)
    {
        return transform.position + direction * baseSpeed * seconds;
    }

    private void FixedUpdate()
    {
        if (!flying || game == null)
        {
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 steer = new Vector3(horizontal, vertical, 0f);
        direction = (direction + steer * steerStrength * Time.fixedDeltaTime).normalized;

        if (direction.z < 0.28f)
        {
            direction.z = 0.28f;
            direction.Normalize();
        }

        Vector3 oldPosition = transform.position;
        float distance = CurrentSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = oldPosition + direction * distance;

        if (CheckCollision(oldPosition, distance, out Vector3 hitPosition))
        {
            transform.position = hitPosition;
            return;
        }

        transform.position = newPosition;
        if (!game.IsInsideBounds(transform.position))
        {
            game.FailLevel("Out of bounds: neural link severed.");
        }
    }

    private bool CheckCollision(Vector3 origin, float distance, out Vector3 hitPosition)
    {
        hitPosition = origin;
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, distance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform == transform)
            {
                continue;
            }

            OneBulletEnemy enemy = hitCollider.GetComponentInParent<OneBulletEnemy>();
            if (enemy != null)
            {
                enemy.TakeBulletHit(hits[i].point, direction);
                SpawnHitSpark(hits[i].point, game.GetMaterial("bullet"));
                continue;
            }

            if (hitCollider.GetComponentInParent<BulletHazard>() != null)
            {
                hitPosition = hits[i].point - direction * radius;
                game.FailLevel("Impact detected: the one bullet was lost.");
                SpawnHitSpark(hits[i].point, game.GetMaterial("danger"));
                return true;
            }
        }

        return false;
    }

    private void SpawnHitSpark(Vector3 position, Material material)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.name = "Hit Spark";
        spark.transform.position = position;
        spark.transform.localScale = Vector3.one * 0.5f;
        spark.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(spark.GetComponent<Collider>());
        Destroy(spark, 0.22f);
    }
}

public class OneBulletEnemy : MonoBehaviour
{
    public enum EnemyKind
    {
        Static,
        Dodger,
        Cover
    }

    public EnemyKind kind;
    public OneBulletGameController game;
    public List<Transform> coverNodes;
    public Material idleMaterial;
    public Material alertMaterial;
    public Material deadMaterial;
    public float riskThreshold = 0.52f;
    public float moveSpeed = 7.5f;
    public float dodgeDistance = 5.2f;
    public float reactionDelay = 0.18f;

    private Renderer enemyRenderer;
    private Vector3 startPosition;
    private Vector3 desiredPosition;
    private float cooldown;
    private float reactionTimer;
    private bool alerting;
    private bool dead;

    private void Awake()
    {
        enemyRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        startPosition = transform.position;
        desiredPosition = startPosition;
    }

    private void Update()
    {
        if (dead || game == null)
        {
            return;
        }

        cooldown -= Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, desiredPosition, moveSpeed * Time.deltaTime);

        if (kind == EnemyKind.Static || !game.IsFlying || game.bullet == null || !game.bullet.IsFlying)
        {
            return;
        }

        float risk = EvaluateRisk(transform.position);
        if (risk > riskThreshold && cooldown <= 0f && !alerting)
        {
            alerting = true;
            reactionTimer = reactionDelay;
            SetMaterial(alertMaterial);
        }

        if (alerting)
        {
            reactionTimer -= Time.deltaTime;
            if (reactionTimer <= 0f)
            {
                alerting = false;
                cooldown = 1.2f;
                if (kind == EnemyKind.Cover)
                {
                    MoveToCover();
                }
                else
                {
                    DodgeSideways();
                }
            }
        }

        if (Vector3.Distance(transform.position, desiredPosition) < 0.08f && !alerting)
        {
            SetMaterial(idleMaterial);
        }
    }

    public void TakeBulletHit(Vector3 hitPoint, Vector3 bulletDirection)
    {
        if (dead)
        {
            return;
        }

        dead = true;
        SetMaterial(deadMaterial);
        transform.rotation = Quaternion.LookRotation(Vector3.up, -bulletDirection);
        StartCoroutine(DeathRoutine(hitPoint));
        game.NotifyEnemyKilled(this);
    }

    private IEnumerator DeathRoutine(Vector3 hitPoint)
    {
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.name = "Enemy Hit Burst";
        burst.transform.position = hitPoint;
        burst.transform.localScale = Vector3.one * 1.2f;
        burst.GetComponent<Renderer>().sharedMaterial = deadMaterial;
        Destroy(burst.GetComponent<Collider>());

        yield return new WaitForSeconds(0.22f);
        Destroy(burst);
        Destroy(gameObject, 0.35f);
    }

    private float EvaluateRisk(Vector3 position)
    {
        OneBulletController bullet = game.bullet;
        Vector3 toEnemy = position - bullet.transform.position;
        float projected = Vector3.Dot(toEnemy, bullet.Direction);
        if (projected < 0f)
        {
            return 0f;
        }

        float maxDistance = bullet.baseSpeed * bullet.predictionSeconds;
        Vector3 closest = bullet.transform.position + bullet.Direction * Mathf.Clamp(projected, 0f, maxDistance);
        float distance = Vector3.Distance(closest, position);
        float distanceScore = Mathf.Clamp01(1f - distance / 4.2f);
        float aimScore = Mathf.Clamp01(Vector3.Dot(bullet.Direction, toEnemy.normalized));
        return distanceScore * 0.76f + aimScore * 0.24f;
    }

    private void DodgeSideways()
    {
        OneBulletController bullet = game.bullet;
        Vector3 side = Vector3.Cross(Vector3.up, bullet.Direction).normalized;
        Vector3 left = game.ClampToArena(startPosition + side * dodgeDistance);
        Vector3 right = game.ClampToArena(startPosition - side * dodgeDistance);
        desiredPosition = EvaluateRisk(left) < EvaluateRisk(right) ? left : right;
    }

    private void MoveToCover()
    {
        if (coverNodes == null || coverNodes.Count == 0)
        {
            DodgeSideways();
            return;
        }

        Transform bestNode = coverNodes[0];
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < coverNodes.Count; i++)
        {
            Vector3 nodePosition = coverNodes[i].position;
            float riskSafety = 1f - EvaluateRisk(nodePosition);
            float travelCost = Vector3.Distance(transform.position, nodePosition) * 0.035f;
            float score = riskSafety - travelCost;
            if (score > bestScore)
            {
                bestScore = score;
                bestNode = coverNodes[i];
            }
        }

        desiredPosition = game.ClampToArena(bestNode.position);
    }

    private void SetMaterial(Material material)
    {
        if (enemyRenderer != null && material != null)
        {
            enemyRenderer.sharedMaterial = material;
        }
    }
}

public class BulletCameraController : MonoBehaviour
{
    public Transform target;
    public OneBulletController bullet;
    public float followDistance = 7f;
    public float followHeight = 2.6f;
    public float smoothTime = 0.08f;

    private Vector3 velocity;
    private Vector3 overviewFocus;

    public void SetOverview(Vector3 focus)
    {
        overviewFocus = focus;
        transform.position = focus + new Vector3(0f, 9.5f, -17f);
        transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
    }

    private void LateUpdate()
    {
        if (target == null || bullet == null)
        {
            return;
        }

        Vector3 focus = bullet.IsFlying ? target.position + bullet.Direction * 3f : overviewFocus;
        Vector3 desired;
        if (bullet.IsFlying)
        {
            desired = target.position - bullet.Direction * followDistance + Vector3.up * followHeight;
        }
        else
        {
            desired = overviewFocus + new Vector3(0f, 9.5f, -17f);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        Quaternion desiredRotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * 12f);
    }
}

public class BulletHazard : MonoBehaviour
{
}
