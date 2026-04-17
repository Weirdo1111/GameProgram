using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Header("Generated Game")]
    [SerializeField] private int targetCount = 10;
    [SerializeField] private int enemyCount = 5;

    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    private void Awake()
    {
        if (FindObjectOfType<FpsGameController>() != null)
        {
            return;
        }

        Physics.gravity = new Vector3(0f, -18f, 0f);
        Physics.defaultSolverIterations = 8;
        Physics.defaultSolverVelocityIterations = 2;
        Time.fixedDeltaTime = 1f / 60f;
        Application.targetFrameRate = 120;

        CreateMaterials();

        FpsGameController controller = gameObject.AddComponent<FpsGameController>();
        controller.totalEnemies = enemyCount;
        controller.totalTargets = targetCount;

        BuildArena();
        BuildLighting();
        BuildPlayer(controller);
        BuildTargets();
        BuildEnemies();
        BuildPhysicsProps();
    }

    private void CreateMaterials()
    {
        materials["floor"] = MakeMaterial("Floor Concrete", new Color(0.24f, 0.27f, 0.28f), 0.1f, 0.55f);
        materials["wall"] = MakeMaterial("Warm Wall", new Color(0.47f, 0.43f, 0.35f), 0.05f, 0.4f);
        materials["accent"] = MakeMaterial("Hazard Yellow", new Color(1f, 0.73f, 0.16f), 0.05f, 0.35f);
        materials["crate"] = MakeMaterial("Crate Steel", new Color(0.34f, 0.26f, 0.18f), 0.15f, 0.6f);
        materials["target"] = MakeMaterial("Target Blue", new Color(0.12f, 0.64f, 0.95f), 0.15f, 0.35f);
        materials["enemy"] = MakeMaterial("Enemy Red", new Color(0.95f, 0.18f, 0.13f), 0.05f, 0.35f);
        materials["gun"] = MakeMaterial("Gun Graphite", new Color(0.04f, 0.045f, 0.05f), 0.45f, 0.22f);
        materials["muzzle"] = MakeMaterial("Muzzle Brass", new Color(0.95f, 0.69f, 0.22f), 0.3f, 0.25f);
        materials["light"] = MakeEmissiveMaterial("Neon Lime", new Color(0.5f, 1f, 0.36f), 1.5f);
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

    private void BuildArena()
    {
        CreateStaticBox("Concrete Floor", new Vector3(0f, -0.5f, 0f), new Vector3(70f, 1f, 70f), materials["floor"]);
        CreateStaticBox("North Wall", new Vector3(0f, 3f, 35f), new Vector3(70f, 7f, 1f), materials["wall"]);
        CreateStaticBox("South Wall", new Vector3(0f, 3f, -35f), new Vector3(70f, 7f, 1f), materials["wall"]);
        CreateStaticBox("East Wall", new Vector3(35f, 3f, 0f), new Vector3(1f, 7f, 70f), materials["wall"]);
        CreateStaticBox("West Wall", new Vector3(-35f, 3f, 0f), new Vector3(1f, 7f, 70f), materials["wall"]);

        CreateStaticBox("Low Cover A", new Vector3(-9f, 0.75f, 8f), new Vector3(8f, 1.5f, 2f), materials["crate"]);
        CreateStaticBox("Low Cover B", new Vector3(12f, 0.75f, -7f), new Vector3(9f, 1.5f, 2f), materials["crate"]);
        CreateStaticBox("Mid Cover", new Vector3(0f, 1.1f, -15f), new Vector3(4f, 2.2f, 5f), materials["crate"]);
        CreateStaticBox("Ramp", new Vector3(-20f, 0.45f, -12f), new Vector3(9f, 0.9f, 5f), materials["accent"])
            .transform.rotation = Quaternion.Euler(0f, 25f, -10f);

        for (int i = -2; i <= 2; i++)
        {
            CreateStaticBox("Neon Floor Strip", new Vector3(i * 9f, 0.02f, 0f), new Vector3(5f, 0.05f, 0.25f), materials["light"]);
        }
    }

    private void BuildLighting()
    {
        Light existingSun = FindObjectOfType<Light>();
        if (existingSun != null)
        {
            existingSun.name = "Arena Sun";
            existingSun.type = LightType.Directional;
            existingSun.intensity = 1.25f;
            existingSun.color = new Color(1f, 0.93f, 0.78f);
            existingSun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.11f, 0.14f, 0.15f);
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.ambientIntensity = 0.65f;

        CreatePointLight("Green Fill Light", new Vector3(0f, 8f, 0f), new Color(0.35f, 1f, 0.5f), 2.1f, 24f);
        CreatePointLight("Warm Back Light", new Vector3(-18f, 7f, 20f), new Color(1f, 0.55f, 0.25f), 1.4f, 22f);
    }

    private void BuildPlayer(FpsGameController controller)
    {
        GameObject player = new GameObject("Physics FPS Player");
        player.transform.position = new Vector3(0f, 2f, -23f);

        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 1.8f;
        capsule.radius = 0.38f;
        capsule.center = Vector3.zero;
        PhysicMaterial playerMaterial = new PhysicMaterial("Player Low Friction");
        playerMaterial.dynamicFriction = 0f;
        playerMaterial.staticFriction = 0f;
        playerMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
        capsule.material = playerMaterial;

        Rigidbody body = player.AddComponent<Rigidbody>();
        body.mass = 80f;
        body.drag = 0f;
        body.angularDrag = 0.05f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        GameObject pivot = new GameObject("Camera Pivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.55f, 0f);

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.name = "FPS Camera";
        camera.transform.SetParent(pivot.transform, false);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        camera.fieldOfView = 76f;
        camera.nearClipPlane = 0.04f;
        camera.farClipPlane = 200f;
        camera.clearFlags = CameraClearFlags.Skybox;

        Transform muzzle = CreateWeaponModel(camera.transform);

        FpsPlayerController playerController = player.AddComponent<FpsPlayerController>();
        playerController.viewCamera = camera;
        playerController.cameraPivot = pivot.transform;

        PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
        playerHealth.maxHealth = 100f;
        playerHealth.respawnPoint = player.transform.position;

        FpsWeapon weapon = player.AddComponent<FpsWeapon>();
        weapon.viewCamera = camera;
        weapon.muzzle = muzzle;
        weapon.ownerBody = body;

        controller.player = playerController;
        controller.weapon = weapon;
        controller.playerHealth = playerHealth;
    }

    private Transform CreateWeaponModel(Transform cameraTransform)
    {
        GameObject rig = new GameObject("Simple Rifle");
        rig.transform.SetParent(cameraTransform, false);
        rig.transform.localPosition = new Vector3(0.32f, -0.31f, 0.58f);
        rig.transform.localRotation = Quaternion.Euler(0f, -2f, 0f);

        CreateWeaponPart("Receiver", rig.transform, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.18f, 0.55f), materials["gun"]);
        CreateWeaponPart("Barrel", rig.transform, new Vector3(0f, 0.02f, 0.46f), new Vector3(0.12f, 0.12f, 0.56f), materials["muzzle"]);
        CreateWeaponPart("Grip", rig.transform, new Vector3(0f, -0.2f, -0.08f), new Vector3(0.16f, 0.32f, 0.14f), materials["gun"]);
        CreateWeaponPart("Sight", rig.transform, new Vector3(0f, 0.15f, 0.04f), new Vector3(0.12f, 0.08f, 0.16f), materials["muzzle"]);

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(rig.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.8f);
        return muzzle.transform;
    }

    private void CreateWeaponPart(string partName, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(part.GetComponent<Collider>());
    }

    private void BuildTargets()
    {
        for (int i = 0; i < targetCount; i++)
        {
            float x = Mathf.Lerp(-23f, 23f, i / Mathf.Max(1f, targetCount - 1f));
            float z = 12f + Mathf.Sin(i * 1.7f) * 9f;
            GameObject target = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Capsule : PrimitiveType.Cube);
            target.name = "Physics Target";
            target.transform.position = new Vector3(x, 1.35f, z);
            target.transform.localScale = i % 2 == 0 ? new Vector3(1.2f, 1.2f, 1.2f) : new Vector3(1.5f, 1.5f, 0.45f);
            target.GetComponent<Renderer>().sharedMaterial = materials["target"];

            Rigidbody body = target.AddComponent<Rigidbody>();
            body.mass = 18f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            FpsDamageable damageable = target.AddComponent<FpsDamageable>();
            damageable.maxHealth = 60f;
            damageable.scoreValue = 100;
            damageable.destroyDelay = 2.25f;
        }
    }

    private void BuildEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            float angle = i * Mathf.PI * 2f / Mathf.Max(1, enemyCount);
            Vector3 position = new Vector3(Mathf.Cos(angle) * 18f, 1.1f, Mathf.Sin(angle) * 18f + 4f);

            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Physics Chaser";
            enemy.transform.position = position;
            enemy.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
            enemy.GetComponent<Renderer>().sharedMaterial = materials["enemy"];

            Rigidbody body = enemy.AddComponent<Rigidbody>();
            body.mass = 55f;
            body.drag = 0.4f;
            body.angularDrag = 0.2f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            FpsDamageable damageable = enemy.AddComponent<FpsDamageable>();
            damageable.maxHealth = 100f;
            damageable.scoreValue = 250;
            damageable.destroyDelay = 2.8f;

            PhysicsEnemy chaser = enemy.AddComponent<PhysicsEnemy>();
            chaser.damagePerSecond = 14f;
        }
    }

    private void BuildPhysicsProps()
    {
        for (int i = 0; i < 18; i++)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Movable Physics Crate";
            crate.transform.position = new Vector3(Random.Range(-24f, 24f), 0.6f, Random.Range(-18f, 18f));
            float size = Random.Range(0.75f, 1.45f);
            crate.transform.localScale = new Vector3(size, size, size);
            crate.GetComponent<Renderer>().sharedMaterial = materials["crate"];

            Rigidbody body = crate.AddComponent<Rigidbody>();
            body.mass = size * 12f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    private GameObject CreateStaticBox(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private void CreatePointLight(string objectName, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(objectName);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }
}

public class FpsPlayerController : MonoBehaviour
{
    public Camera viewCamera;
    public Transform cameraPivot;
    public float walkSpeed = 7f;
    public float sprintSpeed = 10.5f;
    public float acceleration = 28f;
    public float airControl = 0.32f;
    public float jumpVelocity = 6.4f;
    public float lookSensitivity = 2.1f;

    private Rigidbody body;
    private float pitch;
    private bool wantsJump;
    private bool grounded;

    public bool IsGrounded
    {
        get { return grounded; }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        LockCursor(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }

        if (Input.GetMouseButtonDown(0))
        {
            LockCursor(true);
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Look();
        }

        if (Input.GetButtonDown("Jump"))
        {
            wantsJump = true;
        }
    }

    private void FixedUpdate()
    {
        grounded = CheckGrounded();
        Move();

        if (wantsJump && grounded)
        {
            Vector3 velocity = body.velocity;
            velocity.y = 0f;
            body.velocity = velocity;
            body.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
        }

        wantsJump = false;
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        transform.Rotate(Vector3.up, mouseX, Space.World);
        pitch = Mathf.Clamp(pitch - mouseY, -84f, 84f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
        Vector3 desiredDirection = transform.TransformDirection(input);
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = desiredDirection * speed;

        Vector3 currentHorizontalVelocity = new Vector3(body.velocity.x, 0f, body.velocity.z);
        Vector3 velocityDelta = targetVelocity - currentHorizontalVelocity;
        float control = grounded ? 1f : airControl;
        velocityDelta = Vector3.ClampMagnitude(velocityDelta, acceleration * control * Time.fixedDeltaTime);

        body.AddForce(velocityDelta, ForceMode.VelocityChange);
    }

    private bool CheckGrounded()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        return Physics.SphereCast(origin, 0.32f, Vector3.down, out hit, 1.05f, ~0, QueryTriggerInteraction.Ignore);
    }

    public void Teleport(Vector3 position)
    {
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = position;
        transform.rotation = Quaternion.identity;
        pitch = 0f;
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}

public class FpsWeapon : MonoBehaviour
{
    public Camera viewCamera;
    public Transform muzzle;
    public Rigidbody ownerBody;
    public float damage = 35f;
    public float range = 140f;
    public float fireRate = 0.11f;
    public float impactForce = 26f;
    public int clipSize = 18;
    public float reloadTime = 1.25f;

    private int ammo;
    private float nextFireTime;
    private bool reloading;
    private Material tracerMaterial;
    private Vector3 weaponRestPosition;
    private bool cachedWeaponRestPosition;

    public int Ammo
    {
        get { return ammo; }
    }

    public bool IsReloading
    {
        get { return reloading; }
    }

    private void Awake()
    {
        ammo = clipSize;
        tracerMaterial = new Material(Shader.Find("Sprites/Default"));
        tracerMaterial.color = new Color(1f, 0.93f, 0.28f, 1f);

    }

    private void Start()
    {
        CacheWeaponRestPosition();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime && !reloading)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (ammo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        ammo--;
        nextFireTime = Time.time + fireRate;

        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        Vector3 endPoint = ray.origin + ray.direction * range;

        if (Physics.Raycast(ray, out hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            Rigidbody hitBody = hit.rigidbody;
            if (hitBody != null)
            {
                hitBody.AddForceAtPosition(ray.direction * impactForce, hit.point, ForceMode.Impulse);
            }

            FpsDamageable damageable = hit.collider.GetComponentInParent<FpsDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, hit.point, ray.direction * impactForce, gameObject);
            }
        }

        if (ownerBody != null)
        {
            ownerBody.AddForce(-ray.direction * 0.45f, ForceMode.Impulse);
        }

        StartCoroutine(ShowTracer(muzzle != null ? muzzle.position : ray.origin, endPoint));
        StartCoroutine(WeaponKick());
    }

    private IEnumerator Reload()
    {
        if (reloading || ammo == clipSize)
        {
            yield break;
        }

        reloading = true;
        yield return new WaitForSeconds(reloadTime);
        ammo = clipSize;
        reloading = false;
    }

    private IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObject = new GameObject("Bullet Tracer");
        LineRenderer tracer = tracerObject.AddComponent<LineRenderer>();
        tracer.positionCount = 2;
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);
        tracer.startWidth = 0.035f;
        tracer.endWidth = 0.004f;
        tracer.material = tracerMaterial;
        tracer.numCapVertices = 4;

        yield return new WaitForSeconds(0.045f);
        Destroy(tracerObject);
    }

    private IEnumerator WeaponKick()
    {
        if (muzzle == null || muzzle.parent == null)
        {
            yield break;
        }

        CacheWeaponRestPosition();
        Transform rig = muzzle.parent;
        rig.localPosition = weaponRestPosition + new Vector3(0f, 0.025f, -0.09f);
        yield return new WaitForSeconds(0.045f);
        rig.localPosition = weaponRestPosition;
    }

    private void CacheWeaponRestPosition()
    {
        if (cachedWeaponRestPosition || muzzle == null || muzzle.parent == null)
        {
            return;
        }

        weaponRestPosition = muzzle.parent.localPosition;
        cachedWeaponRestPosition = true;
    }
}

public class FpsDamageable : MonoBehaviour
{
    public float maxHealth = 100f;
    public int scoreValue = 100;
    public float destroyDelay = 2f;

    private float health;
    private bool dead;
    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 impulse, GameObject attacker)
    {
        if (dead)
        {
            return;
        }

        health -= amount;
        if (body != null)
        {
            body.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
        }

        if (health <= 0f)
        {
            Die(hitPoint, impulse);
        }
    }

    private void Die(Vector3 hitPoint, Vector3 impulse)
    {
        dead = true;

        if (FpsGameController.Instance != null)
        {
            FpsGameController.Instance.AddScore(scoreValue);
        }

        PhysicsEnemy enemy = GetComponent<PhysicsEnemy>();
        if (enemy != null)
        {
            enemy.enabled = false;
        }

        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.material = null;
        }

        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.constraints = RigidbodyConstraints.None;
        body.drag = 0.08f;
        body.AddTorque(Random.onUnitSphere * 12f, ForceMode.Impulse);
        body.AddForceAtPosition(impulse * 1.25f + Vector3.up * 3f, hitPoint, ForceMode.Impulse);
        Destroy(gameObject, destroyDelay);
    }
}

public class PhysicsEnemy : MonoBehaviour
{
    public float chaseForce = 34f;
    public float maxSpeed = 6f;
    public float attackRange = 1.6f;
    public float damagePerSecond = 12f;

    private Rigidbody body;
    private Transform target;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (FpsGameController.Instance != null && FpsGameController.Instance.player != null)
        {
            target = FpsGameController.Instance.player.transform;
        }
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance < 0.01f)
        {
            return;
        }

        Vector3 direction = toTarget / distance;
        body.AddForce(direction * chaseForce, ForceMode.Acceleration);

        Vector3 flatVelocity = new Vector3(body.velocity.x, 0f, body.velocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            flatVelocity = flatVelocity.normalized * maxSpeed;
            body.velocity = new Vector3(flatVelocity.x, body.velocity.y, flatVelocity.z);
        }

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        if (distance <= attackRange && FpsGameController.Instance != null && FpsGameController.Instance.playerHealth != null)
        {
            FpsGameController.Instance.playerHealth.TakeDamage(damagePerSecond * Time.fixedDeltaTime);
        }
    }
}

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public Vector3 respawnPoint;

    private float health;
    private bool respawning;
    private FpsPlayerController controller;

    public float CurrentHealth
    {
        get { return health; }
    }

    private void Awake()
    {
        health = maxHealth;
        controller = GetComponent<FpsPlayerController>();
    }

    public void TakeDamage(float amount)
    {
        if (respawning)
        {
            return;
        }

        health = Mathf.Max(0f, health - amount);
        if (health <= 0f)
        {
            StartCoroutine(Respawn());
        }
    }

    private IEnumerator Respawn()
    {
        respawning = true;
        yield return new WaitForSeconds(0.8f);
        health = maxHealth;

        if (controller != null)
        {
            controller.Teleport(respawnPoint);
        }
        else
        {
            transform.position = respawnPoint;
        }

        respawning = false;
    }
}

public class FpsGameController : MonoBehaviour
{
    public static FpsGameController Instance { get; private set; }

    public FpsPlayerController player;
    public FpsWeapon weapon;
    public PlayerHealth playerHealth;
    public int totalTargets;
    public int totalEnemies;

    private int score;
    private int destroyedObjects;

    private void Awake()
    {
        Instance = this;
    }

    public void AddScore(int amount)
    {
        score += amount;
        destroyedObjects++;
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 18;

        string ammoText = weapon != null && weapon.IsReloading ? "Reloading..." : weapon != null ? weapon.Ammo + " / " + weapon.clipSize : "--";
        string healthText = playerHealth != null ? Mathf.CeilToInt(playerHealth.CurrentHealth).ToString() : "--";

        GUI.Label(new Rect(20f, 18f, 420f, 28f), "Score: " + score + "   Destroyed: " + destroyedObjects + " / " + (totalTargets + totalEnemies));
        GUI.Label(new Rect(20f, 46f, 420f, 28f), "Health: " + healthText + "   Ammo: " + ammoText);
        GUI.Label(new Rect(20f, 74f, 700f, 28f), "WASD move | Shift sprint | Space jump | Mouse aim | Left click shoot | R reload | Esc unlock mouse");

        float size = 18f;
        Rect crosshair = new Rect(Screen.width * 0.5f - size * 0.5f, Screen.height * 0.5f - size * 0.5f, size, size);
        GUI.Label(crosshair, "+");

        if (playerHealth != null && playerHealth.CurrentHealth <= 0f)
        {
            GUI.skin.label.fontSize = 30;
            GUI.Label(new Rect(Screen.width * 0.5f - 130f, Screen.height * 0.5f + 50f, 300f, 40f), "Respawning...");
        }
    }
}
