using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Controls player fireball movement, hits, pierce count, and fire-zone spawning.
public sealed class ZombieStormProjectile : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float life;
    private float maxLife;
    private int pierce;
    private SpriteRenderer spriteRenderer;
    private Sprite[] fireballFrames;
    private bool createsFireZoneOnKill;
    private readonly HashSet<ZombieStormEnemy> hitEnemies = new HashSet<ZombieStormEnemy>();

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, Vector2 fireDirection, float hitDamage, float moveSpeed, float seconds, int pierceCount, bool fireZoneOnKill)
    {
        game = owner;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.up;
        damage = hitDamage;
        speed = moveSpeed;
        life = seconds;
        maxLife = Mathf.Max(0.01f, seconds);
        pierce = pierceCount;
        createsFireZoneOnKill = fireZoneOnKill;
        hitEnemies.Clear();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fireballFrames = game.GetProjectileEffectFrames();
        if (spriteRenderer != null && fireballFrames != null && fireballFrames.Length > 0)
        {
            spriteRenderer.sprite = fireballFrames[0];
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
    private void Update()
    {
        UpdateFireballAnimation();
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
            if (enemy != null && !enemy.IsDead && !hitEnemies.Contains(enemy) && Vector2.Distance(transform.position, enemy.transform.position) <= enemy.Radius + 0.16f)
            {
                Vector2 hitPosition = enemy.transform.position;
                hitEnemies.Add(enemy);
                enemy.TakeDamage(damage, direction);
                game.SpawnAreaEffect(transform.position, 0.62f, 0f, 0.22f, 1f, new Color(1f, 0.56f, 0.14f, 0.78f), "foozle_explosion");
                if (createsFireZoneOnKill && game.Skills != null)
                {
                    game.Skills.SpawnFireZoneOnFireballHit(hitPosition);
                }

                pierce--;
                if (pierce < 0)
                {
                    game.ReturnPooled("player_bullet", gameObject);
                }

                return;
            }
        }
    }

    // Updates fireball scale and spin while it flies.
    private void UpdateFireballAnimation()
    {
        if (spriteRenderer == null || fireballFrames == null || fireballFrames.Length == 0)
        {
            return;
        }

        float elapsed = maxLife - life;
        int frameIndex = Mathf.Abs(Mathf.FloorToInt(elapsed / 0.045f)) % fireballFrames.Length;
        spriteRenderer.sprite = fireballFrames[frameIndex];
    }
}

// Controls thrown fire bombs, impact area damage, and optional burning ground.
public sealed class ZombieStormFireBombProjectile : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float impactDamage;
    private float impactRadius;
    private bool leavesFire;
    private float burnDamage;
    private float burnRadius;
    private float burnDuration;
    private float burnTickRate;
    private float travelTime;
    private float elapsed;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, Vector2 origin, Vector2 target, float hitDamage, float hitRadius, bool createsFire, float lingeringDamage, float lingeringRadius, float lingeringDuration, float lingeringTickRate)
    {
        game = owner;
        startPosition = origin;
        targetPosition = target;
        impactDamage = hitDamage;
        impactRadius = hitRadius;
        leavesFire = createsFire;
        burnDamage = lingeringDamage;
        burnRadius = lingeringRadius;
        burnDuration = lingeringDuration;
        burnTickRate = lingeringTickRate;
        travelTime = Mathf.Clamp(Vector2.Distance(origin, target) / 12f, 0.34f, 0.58f);
        elapsed = 0f;
        spriteRenderer = GetComponent<SpriteRenderer>();
        frames = game.GetFireBombEffectFrames();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            if (frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        transform.position = origin;
        transform.localScale = Vector3.one * 1.08f;
    }

    // Advances the throw arc and detonates once the bomb reaches its target.
    private void Update()
    {
        if (game == null)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / travelTime);
        Vector2 flatPosition = Vector2.Lerp(startPosition, targetPosition, t);
        float arc = Mathf.Sin(t * Mathf.PI) * 0.92f;
        transform.position = flatPosition + Vector2.up * arc;
        transform.localScale = Vector3.one * (1.02f + Mathf.Sin(t * Mathf.PI) * 0.18f);
        UpdateAnimation(t);

        if (t >= 1f)
        {
            Detonate();
        }
    }

    // Updates the fire bomb sprite frame and spin while it flies.
    private void UpdateAnimation(float t)
    {
        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
            spriteRenderer.sprite = frames[frameIndex];
        }

        transform.rotation = Quaternion.Euler(0f, 0f, elapsed * 320f);
    }

    // Applies impact damage and optional lingering fire, then returns the projectile to its pool.
    private void Detonate()
    {
        game.SpawnAreaEffect(targetPosition, impactRadius, impactDamage, 0.18f, 99f, new Color(1f, 0.52f, 0.08f, 0.78f), "foozle_explosion");
        if (leavesFire)
        {
            string fireKey = game.GetRandomGroundFireEffectKey();
            game.SpawnAreaEffect(targetPosition, burnRadius, burnDamage, burnDuration, burnTickRate, new Color(1f, 0.42f, 0.08f, 0.72f), fireKey);
        }

        for (int i = 0; i < 5; i++)
        {
            game.SpawnHitSpark(targetPosition + UnityEngine.Random.insideUnitCircle * impactRadius * 0.72f, new Color(1f, 0.68f, 0.12f, 0.78f), 0.14f);
        }

        game.ShakeCamera(0.045f, 0.08f);
        game.ReturnPooled("fire_bomb_projectile", gameObject);
    }
}

// Controls enemy ranged projectiles, movement, collision, and player damage.
public sealed class ZombieStormEnemyProjectile : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float life;
    private Color hitColor;
    private float hitRadius;

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, Vector2 fireDirection, float hitDamage, float moveSpeed, float seconds, Color impactColor, float impactRadius)
    {
        game = owner;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.up;
        damage = hitDamage;
        speed = moveSpeed;
        life = seconds;
        hitColor = impactColor;
        hitRadius = impactRadius;
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
    private void Update()
    {
        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + direction * speed * Time.deltaTime;
        transform.position = endPosition;
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            game.ReturnPooled("enemy_spit", gameObject);
            return;
        }

        if (game.Player != null && DistanceToSegment(game.Player.transform.position, startPosition, endPosition) <= 0.62f)
        {
            game.SpawnHitSpark(game.Player.transform.position, hitColor, hitRadius);
            game.Player.TakeDamage(damage);
            game.ReturnPooled("enemy_spit", gameObject);
        }
    }

    // Calculates distance from a point to a line segment for trail collision.
    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
        {
            return Vector2.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }
}

// Controls the ice boss projectile flight and burst damage phases.
public sealed class ZombieStormIceBossProjectile : MonoBehaviour
{
    private const int LaunchFrameCount = 2;
    private const int BurstFrameCount = 5;
    private const float LaunchFrameDuration = 0.07f;
    private const float FlyFrameDuration = 0.055f;
    private const float BurstFrameDuration = 0.055f;
    private const float HitRadius = 0.62f;

    private ZombieStormGameController game;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float life;
    private float maxLife;
    private float burstTimer;
    private bool bursting;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, Vector2 fireDirection, float hitDamage, float moveSpeed, float seconds)
    {
        game = owner;
        direction = fireDirection.sqrMagnitude > 0.01f ? fireDirection.normalized : Vector2.up;
        damage = hitDamage;
        speed = moveSpeed;
        life = seconds;
        maxLife = Mathf.Max(0.01f, seconds);
        burstTimer = 0f;
        bursting = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        frames = game.GetIceBossOrbFrames();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            if (frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
    private void Update()
    {
        if (game == null)
        {
            return;
        }

        if (bursting)
        {
            UpdateBurstAnimation();
            return;
        }

        Vector2 startPosition = transform.position;
        Vector2 endPosition = startPosition + direction * speed * Time.deltaTime;
        transform.position = endPosition;
        life -= Time.deltaTime;
        UpdateFlightAnimation(maxLife - life);

        if (game.Player != null && DistanceToSegment(game.Player.transform.position, startPosition, endPosition) <= HitRadius)
        {
            BeginBurst(game.Player.transform.position);
            return;
        }

        if (life <= 0f)
        {
            game.ReturnPooled("ice_boss_orb", gameObject);
        }
    }

    // Updates ice projectile rotation and flicker while flying.
    private void UpdateFlightAnimation(float elapsed)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
        {
            return;
        }

        int burstStart = GetBurstStartIndex();
        if (elapsed < LaunchFrameCount * LaunchFrameDuration)
        {
            int launchFrame = Mathf.Min(LaunchFrameCount - 1, Mathf.FloorToInt(elapsed / LaunchFrameDuration));
            spriteRenderer.sprite = frames[Mathf.Min(launchFrame, frames.Length - 1)];
            return;
        }

        int flyStart = Mathf.Min(LaunchFrameCount, Mathf.Max(0, burstStart - 1));
        int flyLength = Mathf.Max(1, burstStart - flyStart);
        float flyElapsed = elapsed - LaunchFrameCount * LaunchFrameDuration;
        int flyFrame = flyStart + Mathf.Abs(Mathf.FloorToInt(flyElapsed / FlyFrameDuration)) % flyLength;
        spriteRenderer.sprite = frames[Mathf.Min(flyFrame, frames.Length - 1)];
    }

    // Switches the ice projectile into burst mode and applies area damage.
    private void BeginBurst(Vector2 hitPosition)
    {
        bursting = true;
        burstTimer = 0f;
        transform.position = hitPosition;
        transform.rotation = Quaternion.identity;

        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[GetBurstStartIndex()];
        }

        game.Player.TakeDamage(damage);
        game.Player.ApplySlow(0.6f, 3f);
        game.SpawnHitSpark(hitPosition, new Color(0.4f, 0.86f, 1f, 0.86f), 0.72f);
        game.FlashScreen(new Color(0.32f, 0.78f, 1f, 1f), 0.36f);
        game.ShakeCamera(0.11f, 0.12f);
        game.PlaySfx("hit", 0.54f, 0.08f);
    }

    // Updates ice burst animation frames and transparency.
    private void UpdateBurstAnimation()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
        {
            game.ReturnPooled("ice_boss_orb", gameObject);
            return;
        }

        burstTimer += Time.deltaTime;
        int burstStart = GetBurstStartIndex();
        int burstLength = Mathf.Max(1, frames.Length - burstStart);
        int burstFrame = Mathf.FloorToInt(burstTimer / BurstFrameDuration);
        if (burstFrame >= burstLength)
        {
            game.ReturnPooled("ice_boss_orb", gameObject);
            return;
        }

        spriteRenderer.sprite = frames[burstStart + burstFrame];
    }

    // Finds the first frame used for the ice burst animation.
    private int GetBurstStartIndex()
    {
        if (frames == null || frames.Length == 0)
        {
            return 0;
        }

        return Mathf.Clamp(frames.Length - BurstFrameCount, 0, frames.Length - 1);
    }

    // Calculates distance from a point to a line segment for trail collision.
    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
        {
            return Vector2.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }
}
