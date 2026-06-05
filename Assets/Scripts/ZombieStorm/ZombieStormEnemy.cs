using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class ZombieStormEnemy : MonoBehaviour
{
    private const float BaseZombieHealth = 22f;
    private const float BaseZombieSpeed = 1.55f;
    private const float BaseZombieMeleeStrikeDamage = 14f;
    private const float EnemyHealthMultiplier = 1.25f;

    private ZombieStormGameController game;
    private SpriteRenderer spriteRenderer;
    private Sprite[] walkFrames;
    private Sprite[] attackFrames;
    private Sprite[] specialAttackFrames;
    private Sprite[] hurtFrames;
    private Sprite[] deathFrames;
    private string poolKey;
    private Color baseColor;
    private float health;
    private float maxHealth;
    private float speed;
    private float damagePerSecond;
    private float bossActionTimer;
    private float bossTelegraphTimer;
    private float sprintTimer;
    private float shootTimer;
    private float walkAnimTime;
    private float attackAnimTime;
    private float attackAnimDuration;
    private float attackCooldown;
    private float slasherLeapRollCooldown;
    private float hurtAnimTime;
    private float hurtAnimDuration;
    private float deathAnimTime;
    private float deathAnimDuration;
    private int bossQueuedAction = -1;
    private bool bossQueuedEnraged;
    private bool sprinting;
    private bool useSideViewWalk;
    private bool walkFramesFaceRight;
    private bool slasherStrikeApplied;
    private bool slasherLeapAttack;
    private bool slasherLeapUsed;
    private float renderDepthOffset;
    private Vector2 bossQueuedDirection;
    private Vector2 slasherLeapDirection;
    private readonly List<Vector2> bossTelegraphPositions = new List<Vector2>(12);

    public ZombieStormEnemyType Type { get; private set; }
    public bool IsDead { get; private set; }
    public float Radius { get; private set; }
    public float Health { get { return health; } }
    public float MaxHealth { get { return maxHealth; } }
    public float Health01 { get { return maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth); } }
    public bool IsBoss { get { return IsBossType(Type); } }
    public string DisplayName { get { return BossName(Type); } }
    private bool UsesAnimatedMeleeAttack { get { return Type == ZombieStormEnemyType.Slasher || Type == ZombieStormEnemyType.Gravedigger || Type == ZombieStormEnemyType.Reaper; } }
    private bool UsesAnimatedEnemyArt { get { return UsesAnimatedMeleeAttack || Type == ZombieStormEnemyType.OrcThrower; } }
    private bool UsesAnimatedBossArt { get { return Type == ZombieStormEnemyType.CrystalGolemBoss || Type == ZombieStormEnemyType.MossGolemBoss || Type == ZombieStormEnemyType.EmberTyrantBoss; } }

    public void Initialize(ZombieStormGameController owner, ZombieStormEnemyType enemyType, string key, Sprite sprite, Sprite[] enemyWalkFrames, Sprite[] enemyAttackFrames, Sprite[] enemySpecialAttackFrames, Sprite[] enemyHurtFrames, Sprite[] enemyDeathFrames, bool framesFaceRight, float runTime, float difficulty)
    {
        game = owner;
        Type = enemyType;
        poolKey = key;
        IsDead = false;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.flipX = false;
        walkFrames = enemyWalkFrames;
        attackFrames = enemyAttackFrames;
        specialAttackFrames = enemySpecialAttackFrames;
        hurtFrames = enemyHurtFrames;
        deathFrames = enemyDeathFrames;
        useSideViewWalk = walkFrames != null && walkFrames.Length > 0;
        walkFramesFaceRight = framesFaceRight;
        walkAnimTime = UnityEngine.Random.value * 4f;
        attackAnimTime = 0f;
        attackAnimDuration = 0f;
        attackCooldown = UnityEngine.Random.Range(0.15f, 0.7f);
        slasherLeapRollCooldown = 0f;
        hurtAnimTime = 0f;
        hurtAnimDuration = 0f;
        deathAnimTime = 0f;
        deathAnimDuration = 0f;
        slasherStrikeApplied = false;
        slasherLeapAttack = false;
        slasherLeapUsed = false;
        slasherLeapDirection = Vector2.zero;
        renderDepthOffset = UnityEngine.Random.Range(-0.00004f, 0.00004f);
        transform.rotation = Quaternion.identity;
        baseColor = Color.white;
        spriteRenderer.color = baseColor;

        float hpScale = 0.82f + runTime / 165f;
        Radius = 0.42f;
        speed = BaseZombieSpeed;
        damagePerSecond = 6.5f;
        maxHealth = BaseZombieHealth * hpScale;
        transform.localScale = Vector3.one * 0.95f;
        sprintTimer = 0.8f;
        shootTimer = UnityEngine.Random.Range(0.7f, 1.6f);
        bossTelegraphTimer = 0f;
        bossQueuedAction = -1;
        bossQueuedDirection = Vector2.zero;
        bossTelegraphPositions.Clear();

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
        else if (Type == ZombieStormEnemyType.Goblin)
        {
            speed = BaseZombieSpeed;
            maxHealth = BaseZombieHealth * hpScale;
            damagePerSecond = 6.5f;
            Radius = 0.42f;
            transform.localScale = Vector3.one;
        }
        else if (Type == ZombieStormEnemyType.SmallGoblin)
        {
            speed = BaseZombieSpeed * 4.65f;
            maxHealth = BaseZombieHealth * 0.5f * hpScale;
            damagePerSecond = 6.5f;
            Radius = 0.3f;
            transform.localScale = Vector3.one * 0.68f;
        }
        else if (Type == ZombieStormEnemyType.Slasher)
        {
            speed = BaseZombieSpeed * 1.2f;
            maxHealth = BaseZombieHealth * 1.5f * hpScale;
            damagePerSecond = 0f;
            Radius = 0.48f;
            transform.localScale = Vector3.one * 1.05f;
        }
        else if (Type == ZombieStormEnemyType.Gravedigger)
        {
            speed = 1.34f;
            maxHealth = BaseZombieHealth * 2.5f * hpScale;
            damagePerSecond = 0f;
            Radius = 0.58f;
            transform.localScale = Vector3.one * 1.12f;
        }
        else if (Type == ZombieStormEnemyType.Reaper)
        {
            speed = 1.62f;
            maxHealth = BaseZombieHealth * hpScale;
            damagePerSecond = 0f;
            Radius = 0.55f;
            transform.localScale = Vector3.one * 1.1f;
        }
        else if (Type == ZombieStormEnemyType.OrcThrower)
        {
            speed = 1.28f;
            maxHealth = BaseZombieHealth * 1.35f * hpScale;
            damagePerSecond = 0f;
            Radius = 0.5f;
            transform.localScale = Vector3.one * 1.06f;
            shootTimer = UnityEngine.Random.Range(0.8f, 1.6f);
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
        else if (Type == ZombieStormEnemyType.PlagueBoss)
        {
            speed = 1.05f;
            maxHealth = 820f * Mathf.Max(1f, difficulty * 0.92f);
            damagePerSecond = 18f;
            Radius = 1.32f;
            baseColor = new Color(0.52f, 1f, 0.34f);
            spriteRenderer.color = baseColor;
            transform.localScale = Vector3.one * 2.9f;
            bossActionTimer = 2.1f;
        }
        else if (Type == ZombieStormEnemyType.BruteBoss)
        {
            speed = 1.38f;
            maxHealth = 1180f * Mathf.Max(1f, difficulty * 1.08f);
            damagePerSecond = 34f;
            Radius = 1.62f;
            baseColor = new Color(1f, 0.46f, 0.18f);
            spriteRenderer.color = baseColor;
            transform.localScale = Vector3.one * 3.35f;
            bossActionTimer = 1.85f;
        }
        else if (Type == ZombieStormEnemyType.StormBoss)
        {
            speed = 1.7f;
            maxHealth = 980f * Mathf.Max(1f, difficulty * 1.18f);
            damagePerSecond = 24f;
            Radius = 1.38f;
            baseColor = new Color(0.45f, 0.78f, 1f);
            spriteRenderer.color = baseColor;
            transform.localScale = Vector3.one * 3.05f;
            bossActionTimer = 1.55f;
        }
        else if (Type == ZombieStormEnemyType.CrystalGolemBoss)
        {
            speed = 1.16f;
            maxHealth = 1120f * Mathf.Max(1f, difficulty * 1.02f);
            damagePerSecond = 28f;
            Radius = 1.46f;
            transform.localScale = Vector3.one * 2.02f;
            bossActionTimer = 1.7f;
        }
        else if (Type == ZombieStormEnemyType.MossGolemBoss)
        {
            speed = 1.04f;
            maxHealth = 1260f * Mathf.Max(1f, difficulty * 1.06f);
            damagePerSecond = 30f;
            Radius = 1.52f;
            transform.localScale = Vector3.one * 2.08f;
            bossActionTimer = 1.85f;
        }
        else if (Type == ZombieStormEnemyType.EmberTyrantBoss)
        {
            speed = 1.45f;
            maxHealth = 1680f * Mathf.Max(1f, difficulty * 1.12f);
            damagePerSecond = 36f;
            Radius = 1.48f;
            transform.localScale = Vector3.one * 2.12f;
            bossActionTimer = 1.55f;
        }

        if (useSideViewWalk)
        {
            baseColor = Color.white;
            spriteRenderer.color = baseColor;
            spriteRenderer.sprite = walkFrames[Mathf.FloorToInt(walkAnimTime) % walkFrames.Length];
        }

        maxHealth *= EnemyHealthMultiplier;
        damagePerSecond *= ZombieStormGameController.EnemyDamageMultiplier;
        health = maxHealth;
        UpdateRenderDepth();
        game.RegisterEnemy(this);
    }

    private void Update()
    {
        if (game == null)
        {
            return;
        }

        if (IsDead)
        {
            UpdateDeathAnimation();
            UpdateRenderDepth();
            return;
        }

        if (game.Player == null)
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
        if (!UsesAnimatedEnemyArt)
        {
            UpdateWalkVisual(direction);
        }

        if (Type == ZombieStormEnemyType.Fast)
        {
            sprintTimer -= Time.deltaTime;
            if (sprintTimer <= 0f)
            {
                sprinting = !sprinting;
                sprintTimer = sprinting ? 0.55f : 1.05f;
            }
        }

        if (IsBoss)
        {
            UpdateBoss(direction);
        }
        else if (Type == ZombieStormEnemyType.Spitter)
        {
            UpdateSpitter(direction, distance);
        }
        else if (Type == ZombieStormEnemyType.OrcThrower)
        {
            UpdateOrcThrower(direction, distance);
        }
        else if (Type == ZombieStormEnemyType.Slasher)
        {
            UpdateAnimatedMelee(direction, distance, BaseZombieMeleeStrikeDamage, 0.68f, 0.92f, 18f, 0.42f, 0.7f, 0.68f, 0.62f, new Color(0.82f, 0.92f, 0.76f, 0.6f), false, true);
        }
        else if (Type == ZombieStormEnemyType.Gravedigger)
        {
            UpdateAnimatedMelee(direction, distance, 22f, 0.82f, 1.08f, 15f, 0.72f, 1.05f, 0.96f, 0.76f, new Color(0.77f, 0.62f, 0.3f, 0.64f), true);
        }
        else if (Type == ZombieStormEnemyType.Reaper)
        {
            UpdateAnimatedMelee(direction, distance, BaseZombieMeleeStrikeDamage * 1.5f, 1.18f, 1.42f, 16f, 0.68f, 0.94f, 1.26f, 0.98f, new Color(0.64f, 0.86f, 0.8f, 0.62f), false);
        }
        else
        {
            float finalSpeed = speed * (sprinting ? 1.85f : 1f);
            transform.position += (Vector3)(direction * finalSpeed * Time.deltaTime);
        }

        transform.position = game.ResolveObstacleCollision(transform.position, Radius);
        transform.position = ResolvePlayerSeparation(transform.position);

        Vector2 currentToPlayer = (Vector2)game.Player.transform.position - (Vector2)transform.position;
        float currentDistance = currentToPlayer.magnitude;
        if (!UsesAnimatedMeleeAttack && Type != ZombieStormEnemyType.OrcThrower && currentDistance <= Radius + 0.45f)
        {
            if (Type == ZombieStormEnemyType.Exploder)
            {
                game.SpawnAreaEffect(transform.position, 2.2f, 30f, 0.22f, 99f, new Color(1f, 0.35f, 0.05f, 0.65f), "zombie_explosion");
                game.PlaySfx("boom", 0.54f, 0.08f);
                game.ShakeCamera(0.16f, 0.16f);
                game.Player.TakeDamage(ScaledEnemyDamage(22f));
                Die(false);
            }
            else
            {
                game.Player.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }

        if (!useSideViewWalk && direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        UpdateRenderDepth();
    }

    private Vector2 ResolvePlayerSeparation(Vector2 position)
    {
        if (game == null || game.Player == null || Type == ZombieStormEnemyType.Exploder)
        {
            return position;
        }

        const float playerRadius = 0.42f;
        float minDistance = Radius + playerRadius;
        Vector2 playerPosition = game.Player.transform.position;
        Vector2 offset = position - playerPosition;
        float sqrDistance = offset.sqrMagnitude;
        if (sqrDistance >= minDistance * minDistance)
        {
            return position;
        }

        Vector2 pushDirection = sqrDistance > 0.0001f ? offset.normalized : Vector2.right;
        return playerPosition + pushDirection * minDistance;
    }

    private void UpdateWalkVisual(Vector2 direction)
    {
        if (!useSideViewWalk || spriteRenderer == null || walkFrames == null || walkFrames.Length == 0)
        {
            return;
        }

        float frameRate = IsBoss ? 8f : Type == ZombieStormEnemyType.Fast ? 13f : UsesAnimatedEnemyArt ? 12f : 10f;
        walkAnimTime += Time.deltaTime * frameRate;
        int frameIndex = Mathf.FloorToInt(walkAnimTime) % walkFrames.Length;
        spriteRenderer.sprite = walkFrames[frameIndex];
        UpdateFacing(direction);
        transform.rotation = Quaternion.identity;
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

        bool playsHurtAnimation = UsesAnimatedEnemyArt || UsesAnimatedBossArt;
        if (playsHurtAnimation && health > 0f && attackAnimTime <= 0f && bossTelegraphTimer <= 0f && hurtFrames != null && hurtFrames.Length > 0 && (!UsesAnimatedBossArt || amount >= 12f))
        {
            hurtAnimDuration = UsesAnimatedBossArt ? 0.16f : 0.2f;
            hurtAnimTime = hurtAnimDuration;
        }

        if (health <= 0f)
        {
            game.SpawnHitSpark(transform.position, IsBoss ? BossAccent(Type) : new Color(0.65f, 1f, 0.35f, 0.8f), IsBoss ? 1.1f : 0.42f);
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

    private void UpdateOrcThrower(Vector2 direction, float distance)
    {
        UpdateFacing(direction);
        transform.rotation = Quaternion.identity;

        if (attackAnimTime > 0f)
        {
            attackAnimTime -= Time.deltaTime;
            float elapsed = attackAnimDuration - Mathf.Max(0f, attackAnimTime);
            SetActionFrame(attackFrames, elapsed, attackAnimDuration);
            if (!slasherStrikeApplied && elapsed >= attackAnimDuration * 0.55f)
            {
                slasherStrikeApplied = true;
                Vector2 throwDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
                game.SpawnEnemyRockProjectile((Vector2)transform.position + throwDirection * 0.54f, throwDirection, 13f, 5.8f, 3.4f);
                game.PlaySfx("hit", 0.28f, 0.07f);
            }

            return;
        }

        if (hurtAnimTime > 0f)
        {
            hurtAnimTime -= Time.deltaTime;
            SetActionFrame(hurtFrames, hurtAnimDuration - Mathf.Max(0f, hurtAnimTime), hurtAnimDuration);
            return;
        }

        if (distance > 6.9f)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
        else if (distance < 4.2f)
        {
            transform.position -= (Vector3)(direction * speed * 0.78f * Time.deltaTime);
        }

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f && distance <= 8.5f && attackFrames != null && attackFrames.Length > 0)
        {
            attackAnimDuration = attackFrames.Length / 15f;
            attackAnimTime = attackAnimDuration;
            shootTimer = attackAnimDuration + UnityEngine.Random.Range(1.7f, 2.45f);
            slasherStrikeApplied = false;
            SetActionFrame(attackFrames, 0f, attackAnimDuration);
            return;
        }

        if (shootTimer <= 0f)
        {
            shootTimer = UnityEngine.Random.Range(1.7f, 2.45f);
            game.SpawnEnemyRockProjectile((Vector2)transform.position + direction * 0.54f, direction, 13f, 5.8f, 3.4f);
        }

        UpdateWalkVisual(direction);
    }

    private void UpdateBoss(Vector2 direction)
    {
        bool enraged = health < maxHealth * 0.5f;
        UpdateFacing(direction);
        transform.rotation = Quaternion.identity;
        if (UsesAnimatedBossArt && hurtAnimTime > 0f && bossTelegraphTimer <= 0f)
        {
            hurtAnimTime -= Time.deltaTime;
            SetActionFrame(hurtFrames, hurtAnimDuration - Mathf.Max(0f, hurtAnimTime), hurtAnimDuration);
            return;
        }

        if (bossTelegraphTimer > 0f)
        {
            bossTelegraphTimer -= Time.deltaTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(baseColor, BossAccent(Type), 0.78f + Mathf.Sin(Time.time * 22f) * 0.18f);
            }

            if (UsesAnimatedBossArt && attackAnimTime > 0f)
            {
                attackAnimTime -= Time.deltaTime;
                Sprite[] actionFrames = bossQueuedAction == 1 && specialAttackFrames != null && specialAttackFrames.Length > 0 ? specialAttackFrames : attackFrames;
                SetActionFrame(actionFrames, attackAnimDuration - Mathf.Max(0f, attackAnimTime), attackAnimDuration);
            }

            if (bossTelegraphTimer <= 0f)
            {
                ExecuteQueuedBossSkill();
                bossActionTimer = GetBossActionCooldown(bossQueuedEnraged);
                bossQueuedAction = -1;
                bossTelegraphPositions.Clear();
            }

            return;
        }

        float moveMultiplier = Type == ZombieStormEnemyType.EmberTyrantBoss ? (enraged ? 1.75f : 1.34f) : Type == ZombieStormEnemyType.BruteBoss ? (enraged ? 1.55f : 1.18f) : Type == ZombieStormEnemyType.StormBoss ? (enraged ? 1.65f : 1.25f) : enraged ? 1.35f : 1f;
        transform.position += (Vector3)(direction * speed * moveMultiplier * Time.deltaTime);

        bossActionTimer -= Time.deltaTime;
        if (bossActionTimer > 0f)
        {
            return;
        }

        BeginBossSkillTelegraph(direction, enraged);
    }

    private void BeginBossSkillTelegraph(Vector2 direction, bool enraged)
    {
        bossQueuedDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
        bossQueuedEnraged = enraged;
        bossTelegraphPositions.Clear();

        if (Type == ZombieStormEnemyType.PlagueBoss)
        {
            bossQueuedAction = 0;
            PreparePlagueBossTelegraph(enraged);
        }
        else if (Type == ZombieStormEnemyType.BruteBoss)
        {
            bossQueuedAction = 0;
            PrepareBruteBossTelegraph(bossQueuedDirection, enraged);
        }
        else if (Type == ZombieStormEnemyType.StormBoss)
        {
            bossQueuedAction = 0;
            PrepareStormBossTelegraph(enraged);
        }
        else if (Type == ZombieStormEnemyType.CrystalGolemBoss)
        {
            bossQueuedAction = UnityEngine.Random.Range(0, 2);
            PrepareCrystalGolemTelegraph(bossQueuedAction, bossQueuedDirection, enraged);
        }
        else if (Type == ZombieStormEnemyType.MossGolemBoss)
        {
            bossQueuedAction = 1;
            PrepareMossGolemTelegraph(bossQueuedAction, bossQueuedDirection, enraged);
        }
        else if (Type == ZombieStormEnemyType.EmberTyrantBoss)
        {
            bossQueuedAction = UnityEngine.Random.Range(0, 2);
            PrepareEmberTyrantTelegraph(bossQueuedAction, bossQueuedDirection, enraged);
        }
        else
        {
            bossQueuedAction = UnityEngine.Random.Range(0, 3);
            PrepareAlphaBossTelegraph(bossQueuedAction, bossQueuedDirection, enraged);
        }

        bossTelegraphTimer = GetBossTelegraphDuration();
        if (UsesAnimatedBossArt)
        {
            Sprite[] actionFrames = bossQueuedAction == 1 && specialAttackFrames != null && specialAttackFrames.Length > 0 ? specialAttackFrames : attackFrames;
            if (actionFrames != null && actionFrames.Length > 0)
            {
                attackAnimDuration = bossTelegraphTimer;
                attackAnimTime = attackAnimDuration;
                SetActionFrame(actionFrames, 0f, attackAnimDuration);
            }
        }

        game.PlaySfx(Type == ZombieStormEnemyType.StormBoss ? "lightning" : "boom", UsesAnimatedBossArt ? 0.36f : 0.22f, 0.08f);
    }

    private void ExecuteQueuedBossSkill()
    {
        if (Type == ZombieStormEnemyType.PlagueBoss)
        {
            CastPlagueBossSkill(bossQueuedDirection, bossQueuedEnraged);
        }
        else if (Type == ZombieStormEnemyType.BruteBoss)
        {
            CastBruteBossSkill(bossQueuedDirection, bossQueuedEnraged);
        }
        else if (Type == ZombieStormEnemyType.StormBoss)
        {
            CastStormBossSkill(bossQueuedDirection, bossQueuedEnraged);
        }
        else if (Type == ZombieStormEnemyType.CrystalGolemBoss)
        {
            CastCrystalGolemSkill(bossQueuedAction, bossQueuedDirection, bossQueuedEnraged);
        }
        else if (Type == ZombieStormEnemyType.MossGolemBoss)
        {
            CastMossGolemSkill(bossQueuedAction, bossQueuedDirection, bossQueuedEnraged);
        }
        else if (Type == ZombieStormEnemyType.EmberTyrantBoss)
        {
            CastEmberTyrantSkill(bossQueuedAction, bossQueuedDirection, bossQueuedEnraged);
        }
        else
        {
            CastAlphaBossSkill(bossQueuedAction, bossQueuedDirection, bossQueuedEnraged);
        }
    }

    private void UpdateAnimatedMelee(Vector2 direction, float distance, float strikeDamage, float attackRange, float hitRange, float attackFrameRate, float cooldownMin, float cooldownMax, float effectRadius, float effectOffset, Color effectColor, bool heavyStrike)
    {
        UpdateAnimatedMelee(direction, distance, strikeDamage, attackRange, hitRange, attackFrameRate, cooldownMin, cooldownMax, effectRadius, effectOffset, effectColor, heavyStrike, false);
    }

    private void UpdateAnimatedMelee(Vector2 direction, float distance, float strikeDamage, float attackRange, float hitRange, float attackFrameRate, float cooldownMin, float cooldownMax, float effectRadius, float effectOffset, Color effectColor, bool heavyStrike, bool canLeapStrike)
    {
        UpdateFacing(direction);
        transform.rotation = Quaternion.identity;
        attackCooldown -= Time.deltaTime;
        if (slasherLeapRollCooldown > 0f)
        {
            slasherLeapRollCooldown -= Time.deltaTime;
        }

        if (attackAnimTime > 0f)
        {
            attackAnimTime -= Time.deltaTime;
            float elapsed = attackAnimDuration - Mathf.Max(0f, attackAnimTime);
            if (slasherLeapAttack && elapsed < attackAnimDuration * 0.52f)
            {
                float leap01 = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, attackAnimDuration * 0.52f));
                float leapSpeed = Mathf.Lerp(speed * 3.45f, speed * 1.15f, leap01);
                transform.position += (Vector3)(slasherLeapDirection * leapSpeed * Time.deltaTime);
            }

            SetActionFrame(attackFrames, elapsed, attackAnimDuration);
            if (!slasherStrikeApplied && elapsed >= attackAnimDuration * 0.5f)
            {
                slasherStrikeApplied = true;
                Vector2 strikeDirection = slasherLeapAttack ? slasherLeapDirection : direction;
                Vector2 strikePosition = (Vector2)transform.position + strikeDirection * effectOffset;
                float finalEffectRadius = slasherLeapAttack ? effectRadius * 1.2f : effectRadius;
                float finalHitRange = slasherLeapAttack ? hitRange * 1.24f : hitRange;
                float finalStrikeDamage = slasherLeapAttack ? strikeDamage * 1.25f : strikeDamage;
                Color finalEffectColor = slasherLeapAttack ? new Color(1f, 0.95f, 0.62f, 0.68f) : effectColor;
                game.SpawnAreaEffect(strikePosition, finalEffectRadius, 0f, heavyStrike || slasherLeapAttack ? 0.2f : 0.13f, 1f, finalEffectColor, heavyStrike ? "zombie_explosion" : "hit_spark");
                game.PlaySfx(heavyStrike || slasherLeapAttack ? "boom" : "hit", heavyStrike || slasherLeapAttack ? 0.38f : 0.32f, 0.08f);
                if (heavyStrike || slasherLeapAttack)
                {
                    game.ShakeCamera(0.07f, 0.09f);
                }

                if (game.Player != null && Vector2.Distance(transform.position, game.Player.transform.position) <= Radius + finalHitRange)
                {
                    game.Player.TakeDamage(ScaledEnemyDamage(finalStrikeDamage));
                }
            }

            return;
        }

        slasherLeapAttack = false;

        if (hurtAnimTime > 0f)
        {
            hurtAnimTime -= Time.deltaTime;
            SetActionFrame(hurtFrames, hurtAnimDuration - Mathf.Max(0f, hurtAnimTime), hurtAnimDuration);
            return;
        }

        bool canStartRegularAttack = distance <= Radius + attackRange;
        bool canStartLeap = false;
        if (canLeapStrike && !slasherLeapUsed && distance <= Radius * 6f && distance > Radius + attackRange && attackCooldown <= 0f && slasherLeapRollCooldown <= 0f)
        {
            slasherLeapRollCooldown = 0.85f;
            canStartLeap = UnityEngine.Random.value < 0.3f;
        }

        if ((canStartRegularAttack || canStartLeap) && attackCooldown <= 0f && attackFrames != null && attackFrames.Length > 0)
        {
            attackAnimDuration = attackFrames.Length / attackFrameRate;
            attackAnimTime = attackAnimDuration;
            slasherLeapAttack = canStartLeap;
            if (slasherLeapAttack)
            {
                slasherLeapUsed = true;
            }

            slasherLeapDirection = direction.sqrMagnitude > 0.01f ? direction : Vector2.right;
            attackCooldown = attackAnimDuration + UnityEngine.Random.Range(cooldownMin, cooldownMax) + (slasherLeapAttack ? 0.22f : 0f);
            slasherStrikeApplied = false;
            SetActionFrame(attackFrames, 0f, attackAnimDuration);
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        UpdateWalkVisual(direction);
    }

    private void UpdateDeathAnimation()
    {
        if (deathFrames == null || deathFrames.Length == 0 || spriteRenderer == null)
        {
            game.ReturnPooled(poolKey, gameObject);
            return;
        }

        deathAnimTime -= Time.deltaTime;
        SetActionFrame(deathFrames, deathAnimDuration - Mathf.Max(0f, deathAnimTime), deathAnimDuration);
        if (deathAnimTime <= 0f)
        {
            game.ReturnPooled(poolKey, gameObject);
        }
    }

    private void SetActionFrame(Sprite[] frames, float elapsed, float duration)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
        {
            return;
        }

        float normalized = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
        int frameIndex = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(normalized * frames.Length));
        spriteRenderer.sprite = frames[frameIndex];
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.05f)
        {
            spriteRenderer.flipX = walkFramesFaceRight ? direction.x < 0f : direction.x > 0f;
        }
    }

    private void UpdateRenderDepth()
    {
        Vector3 position = transform.position;
        position.z = position.y * 0.001f + renderDepthOffset;
        transform.position = position;
    }

    private void PrepareAlphaBossTelegraph(int action, Vector2 direction, bool enraged)
    {
        if (action == 0)
        {
            int shots = enraged ? 18 : 12;
            for (int i = 0; i < shots; i++)
            {
                Vector2 shotDir = ZombieStormGameController.Rotate(Vector2.up, i * (360f / shots));
                Vector2 markerPosition = (Vector2)transform.position + shotDir * 1.65f;
                game.SpawnAreaEffect(markerPosition, 0.26f, 0f, 0.58f, 1f, new Color(1f, 0.18f, 0.12f, 0.62f), "hit_spark");
            }
        }
        else if (action == 1)
        {
            int pools = enraged ? 8 : 5;
            for (int i = 0; i < pools; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.8f, 3.8f);
                Vector2 position = (Vector2)transform.position + offset;
                bossTelegraphPositions.Add(position);
                game.SpawnAreaEffect(position, 1.05f, 0f, 0.62f, 1f, new Color(0.55f, 1f, 0.15f, 0.2f), "toxic_pool");
            }
        }
        else
        {
            PrepareDashTelegraph(direction, enraged ? 4.1f : 2.7f, new Color(1f, 0.24f, 0.12f, 0.46f));
        }
    }

    private void PreparePlagueBossTelegraph(bool enraged)
    {
        int pools = enraged ? 10 : 7;
        for (int i = 0; i < pools; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.6f, enraged ? 5.2f : 4.2f);
            Vector2 position = (Vector2)transform.position + offset;
            bossTelegraphPositions.Add(position);
            game.SpawnAreaEffect(position, enraged ? 1.25f : 1.05f, 0f, 0.64f, 1f, new Color(0.5f, 1f, 0.18f, 0.22f), "toxic_pool");
        }

        int volleys = enraged ? 5 : 3;
        for (int i = 0; i < volleys; i++)
        {
            Vector2 shotDir = ZombieStormGameController.Rotate(bossQueuedDirection, (i - volleys / 2f) * 12f);
            Vector2 markerPosition = (Vector2)transform.position + shotDir * 1.55f;
            game.SpawnAreaEffect(markerPosition, 0.24f, 0f, 0.58f, 1f, new Color(0.5f, 1f, 0.18f, 0.55f), "hit_spark");
        }
    }

    private void PrepareBruteBossTelegraph(Vector2 direction, bool enraged)
    {
        PrepareDashTelegraph(direction, enraged ? 5.6f : 4.1f, new Color(1f, 0.36f, 0.08f, 0.5f));
        Vector2 landingPosition = (Vector2)transform.position + direction * (enraged ? 5.6f : 4.1f);
        bossTelegraphPositions.Add(landingPosition);
        game.SpawnAreaEffect(landingPosition, enraged ? 2.45f : 2f, 0f, 0.68f, 1f, new Color(1f, 0.28f, 0.08f, 0.3f), "zombie_explosion");
    }

    private void PrepareStormBossTelegraph(bool enraged)
    {
        int strikes = enraged ? 6 : 4;
        Vector2 playerPosition = game.Player != null ? (Vector2)game.Player.transform.position : (Vector2)transform.position + bossQueuedDirection * 3f;
        for (int i = 0; i < strikes; i++)
        {
            Vector2 strikePosition = playerPosition + UnityEngine.Random.insideUnitCircle * (enraged ? 3.7f : 2.9f);
            bossTelegraphPositions.Add(strikePosition);
            game.SpawnAreaEffect(strikePosition, enraged ? 1.35f : 1.05f, 0f, 0.62f, 1f, new Color(0.34f, 0.72f, 1f, 0.34f), "lightning_flash");
        }

        int arcs = enraged ? 10 : 7;
        for (int i = 0; i < arcs; i++)
        {
            Vector2 shotDir = ZombieStormGameController.Rotate(bossQueuedDirection, -42f + i * (84f / Mathf.Max(1, arcs - 1)));
            Vector2 markerPosition = (Vector2)transform.position + shotDir * 1.45f;
            game.SpawnAreaEffect(markerPosition, 0.22f, 0f, 0.54f, 1f, new Color(0.34f, 0.72f, 1f, 0.58f), "lightning_flash");
        }
    }

    private void PrepareCrystalGolemTelegraph(int action, Vector2 direction, bool enraged)
    {
        Color crystal = new Color(0.36f, 0.92f, 1f, 0.46f);
        if (action == 0)
        {
            Vector2 strikePosition = (Vector2)transform.position + direction * (enraged ? 1.55f : 1.35f);
            bossTelegraphPositions.Add(strikePosition);
            game.SpawnAreaEffect(strikePosition, enraged ? 2.3f : 1.95f, 0f, 0.68f, 1f, crystal, "zombie_explosion");
            PrepareDashTelegraph(direction, 1.6f, crystal);
            return;
        }

        int shards = enraged ? 7 : 5;
        for (int i = 0; i < shards; i++)
        {
            Vector2 shotDir = ZombieStormGameController.Rotate(direction, -38f + i * (76f / Mathf.Max(1, shards - 1)));
            game.SpawnAreaEffect((Vector2)transform.position + shotDir * 1.5f, 0.3f, 0f, 0.6f, 1f, crystal, "lightning_flash");
        }
    }

    private void PrepareMossGolemTelegraph(int action, Vector2 direction, bool enraged)
    {
        Color moss = new Color(0.56f, 0.82f, 0.18f, 0.44f);
        if (action == 0)
        {
            Vector2 strikePosition = (Vector2)transform.position + direction * (enraged ? 1.7f : 1.45f);
            bossTelegraphPositions.Add(strikePosition);
            game.SpawnAreaEffect(strikePosition, enraged ? 2.5f : 2.12f, 0f, 0.72f, 1f, moss, "toxic_pool");
            return;
        }

        Vector2 targetPosition = game.Player != null ? (Vector2)game.Player.transform.position : (Vector2)transform.position + direction * 3f;
        bossTelegraphPositions.Add(targetPosition);
    }

    private void PrepareEmberTyrantTelegraph(int action, Vector2 direction, bool enraged)
    {
        Color ember = new Color(1f, 0.3f, 0.06f, 0.5f);
        if (action == 0)
        {
            float distance = enraged ? 5.4f : 4.3f;
            PrepareDashTelegraph(direction, distance, ember);
            Vector2 landingPosition = (Vector2)transform.position + direction * distance;
            bossTelegraphPositions.Add(landingPosition);
            game.SpawnAreaEffect(landingPosition, enraged ? 2.45f : 2.05f, 0f, 0.74f, 1f, ember, "ember_dash_blast");
            return;
        }

        int impacts = enraged ? 6 : 4;
        Vector2 target = game.Player != null ? (Vector2)game.Player.transform.position : (Vector2)transform.position + direction * 3f;
        for (int i = 0; i < impacts; i++)
        {
            Vector2 position = target + UnityEngine.Random.insideUnitCircle * (enraged ? 3.1f : 2.4f);
            bossTelegraphPositions.Add(position);
            game.SpawnAreaEffect(position, enraged ? 1.38f : 1.15f, 0f, 0.76f, 1f, new Color(1f, 0.32f, 0.05f, 0.38f), "meteor_warning");
        }
    }

    private void PrepareDashTelegraph(Vector2 direction, float distance, Color color)
    {
        int markers = Mathf.Clamp(Mathf.CeilToInt(distance), 3, 7);
        for (int i = 1; i <= markers; i++)
        {
            float t = i / (float)markers;
            Vector2 position = (Vector2)transform.position + direction * distance * t;
            game.SpawnAreaEffect(position, 0.42f + t * 0.28f, 0f, 0.62f, 1f, color, "zombie_explosion");
        }
    }

    private void CastAlphaBossSkill(int action, Vector2 direction, bool enraged)
    {
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
                Vector2 position = i < bossTelegraphPositions.Count ? bossTelegraphPositions[i] : (Vector2)transform.position + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.8f, 3.8f);
                game.SpawnAreaEffect(position, 0.95f, 8f, 2.4f, 0.45f, new Color(0.55f, 1f, 0.15f, 0.38f), "toxic_pool");
            }
        }
        else
        {
            transform.position += (Vector3)(direction * (enraged ? 4.1f : 2.7f));
            game.ShakeCamera(0.11f, 0.14f);
        }
    }

    private void CastPlagueBossSkill(Vector2 direction, bool enraged)
    {
        int pools = enraged ? 10 : 7;
        for (int i = 0; i < pools; i++)
        {
            Vector2 position = i < bossTelegraphPositions.Count ? bossTelegraphPositions[i] : (Vector2)transform.position + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.6f, enraged ? 5.2f : 4.2f);
            game.SpawnAreaEffect(position, enraged ? 1.12f : 0.95f, 8f, enraged ? 3.1f : 2.4f, 0.45f, new Color(0.5f, 1f, 0.18f, 0.42f), "toxic_pool");
        }

        int volleys = enraged ? 5 : 3;
        for (int i = 0; i < volleys; i++)
        {
            Vector2 shotDir = ZombieStormGameController.Rotate(direction, (i - volleys / 2f) * 12f);
            game.SpawnEnemyProjectile(transform.position, shotDir, enraged ? 14f : 10f, enraged ? 5.5f : 4.7f, 4.6f);
        }

        game.PlaySfx("boom", 0.42f, 0.08f);
    }

    private void CastBruteBossSkill(Vector2 direction, bool enraged)
    {
        float dashDistance = enraged ? 5.6f : 4.1f;
        transform.position = bossTelegraphPositions.Count > 0 ? bossTelegraphPositions[0] : (Vector2)transform.position + direction * dashDistance;
        game.ShakeCamera(enraged ? 0.24f : 0.18f, 0.22f);
        game.SpawnAreaEffect(transform.position, enraged ? 2.2f : 1.75f, 0f, 0.24f, 1f, new Color(1f, 0.36f, 0.08f, 0.54f), "zombie_explosion");

        if (game.Player != null && Vector2.Distance(transform.position, game.Player.transform.position) < (enraged ? 2.75f : 2.2f))
        {
            game.Player.TakeDamage(ScaledEnemyDamage(enraged ? 32f : 24f));
        }

        int shockwaves = enraged ? 12 : 8;
        for (int i = 0; i < shockwaves; i++)
        {
            Vector2 shotDir = ZombieStormGameController.Rotate(Vector2.up, i * (360f / shockwaves));
            game.SpawnEnemyProjectile(transform.position, shotDir, enraged ? 13f : 9f, enraged ? 5.3f : 4.2f, 2.2f);
        }

        game.PlaySfx("boom", 0.62f, 0.08f);
    }

    private void CastStormBossSkill(Vector2 direction, bool enraged)
    {
        int strikes = enraged ? 6 : 4;
        for (int i = 0; i < strikes; i++)
        {
            Vector2 strikePosition = i < bossTelegraphPositions.Count ? bossTelegraphPositions[i] : (Vector2)transform.position + direction * 3f;
            float radius = enraged ? 1.15f : 0.9f;
            game.SpawnAreaEffect(strikePosition, radius, 0f, 0.18f, 1f, new Color(0.34f, 0.72f, 1f, 0.56f), "lightning_flash");
            if (game.Player != null && Vector2.Distance(strikePosition, game.Player.transform.position) <= radius + 0.35f)
            {
                game.Player.TakeDamage(ScaledEnemyDamage(enraged ? 18f : 12f));
            }
        }

        int arcs = enraged ? 10 : 7;
        for (int i = 0; i < arcs; i++)
        {
            Vector2 shotDir = ZombieStormGameController.Rotate(direction, -42f + i * (84f / Mathf.Max(1, arcs - 1)));
            game.SpawnEnemyProjectile(transform.position, shotDir, enraged ? 12f : 8f, enraged ? 6.8f : 5.7f, 2.4f);
        }

        game.PlaySfx("lightning", 0.7f, 0.08f);
        game.ShakeCamera(0.1f, 0.12f);
    }

    private void CastCrystalGolemSkill(int action, Vector2 direction, bool enraged)
    {
        Color crystal = new Color(0.36f, 0.92f, 1f, 0.86f);
        if (action == 0)
        {
            Vector2 strikePosition = bossTelegraphPositions.Count > 0 ? bossTelegraphPositions[0] : (Vector2)transform.position + direction * 1.35f;
            float radius = enraged ? 2.15f : 1.82f;
            game.SpawnAreaEffect(strikePosition, radius, 0f, 0.22f, 1f, crystal, "zombie_explosion");
            game.ShakeCamera(enraged ? 0.2f : 0.14f, 0.18f);
            if (game.Player != null && Vector2.Distance(strikePosition, game.Player.transform.position) <= radius + 0.34f)
            {
                game.Player.TakeDamage(ScaledEnemyDamage(enraged ? 30f : 22f));
            }
        }
        else
        {
            int shards = enraged ? 7 : 5;
            for (int i = 0; i < shards; i++)
            {
                Vector2 shotDir = ZombieStormGameController.Rotate(direction, -38f + i * (76f / Mathf.Max(1, shards - 1)));
                game.SpawnEnemyProjectile(transform.position, shotDir, enraged ? 17f : 12f, enraged ? 6.3f : 5.3f, 3.2f, crystal, 0.56f);
            }
        }

        game.PlaySfx(action == 0 ? "boom" : "shoot", action == 0 ? 0.62f : 0.48f, 0.08f);
    }

    private void CastMossGolemSkill(int action, Vector2 direction, bool enraged)
    {
        Color moss = new Color(0.56f, 0.82f, 0.18f, 0.82f);
        if (action == 0)
        {
            Vector2 strikePosition = bossTelegraphPositions.Count > 0 ? bossTelegraphPositions[0] : (Vector2)transform.position + direction * 1.45f;
            float radius = enraged ? 2.34f : 2f;
            game.SpawnAreaEffect(strikePosition, radius, 0f, 0.2f, 1f, moss, "zombie_explosion");
            game.SpawnEnemyAreaEffect(strikePosition, enraged ? 1.75f : 1.45f, enraged ? 10f : 7f, enraged ? 3.4f : 2.7f, 0.48f, new Color(0.48f, 0.78f, 0.14f, 0.42f), "toxic_pool");
            game.ShakeCamera(enraged ? 0.2f : 0.15f, 0.2f);
            if (game.Player != null && Vector2.Distance(strikePosition, game.Player.transform.position) <= radius + 0.34f)
            {
                game.Player.TakeDamage(ScaledEnemyDamage(enraged ? 27f : 20f));
            }
        }
        else
        {
            Vector2 poisonPosition = game.Player != null ? (Vector2)game.Player.transform.position : bossTelegraphPositions.Count > 0 ? bossTelegraphPositions[0] : (Vector2)transform.position + direction * 3f;
            float burstRadius = enraged ? 1.65f : 1.35f;
            const float poisonDelay = 2f;
            game.SpawnAreaEffect(poisonPosition, burstRadius * 1.35f, 0f, poisonDelay, 1f, new Color(1f, 0.04f, 0.02f, 0.46f), "meteor_warning");
            game.SpawnAreaEffect(poisonPosition, burstRadius * 0.24f, 0f, poisonDelay, 1f, new Color(1f, 0.1f, 0.06f, 0.68f), "hit_spark");
            game.SpawnDelayedEnemyAreaEffect(poisonPosition, poisonDelay, burstRadius, enraged ? 24f : 18f, enraged ? 0.78f : 0.9f, 99f, new Color(0.48f, 1f, 0.14f, 0.82f), "poison_boss_blast", enraged ? 0.16f : 0.11f, 0.16f, 0.52f);
            game.SpawnDelayedEnemyAreaEffect(poisonPosition, poisonDelay, enraged ? 1.35f : 1.08f, enraged ? 10f : 7f, enraged ? 3.2f : 2.55f, 0.45f, new Color(0.42f, 0.92f, 0.12f, 0.46f), "toxic_pool");
        }

        game.PlaySfx("boom", action == 0 ? 0.64f : 0.52f, 0.08f);
    }

    private void CastEmberTyrantSkill(int action, Vector2 direction, bool enraged)
    {
        Color ember = new Color(1f, 0.3f, 0.05f, 0.88f);
        if (action == 0)
        {
            Vector2 start = transform.position;
            float distance = enraged ? 5.4f : 4.3f;
            Vector2 landingPosition = bossTelegraphPositions.Count > 0 ? bossTelegraphPositions[0] : start + direction * distance;
            transform.position = game.ClampToArena(landingPosition);
            int pools = enraged ? 4 : 3;
            for (int i = 0; i < pools; i++)
            {
                Vector2 trailPosition = Vector2.Lerp(start, transform.position, (i + 1f) / pools);
                game.SpawnEnemyAreaEffect(trailPosition, enraged ? 0.82f : 0.7f, enraged ? 8f : 6f, enraged ? 2.6f : 2.1f, 0.48f, new Color(1f, 0.22f, 0.04f, 0.46f), "fire_pool");
            }

            game.SpawnEnemyAreaEffect(transform.position, enraged ? 2.2f : 1.86f, enraged ? 32f : 24f, 0.48f, 99f, ember, "ember_dash_blast");
            game.ShakeCamera(enraged ? 0.26f : 0.19f, 0.22f);
        }
        else
        {
            for (int i = 0; i < bossTelegraphPositions.Count; i++)
            {
                Vector2 impactPosition = bossTelegraphPositions[i];
                game.SpawnEnemyAreaEffect(impactPosition, enraged ? 1.35f : 1.1f, enraged ? 24f : 18f, 0.48f, 99f, ember, "ember_meteor_blast");
                game.SpawnEnemyAreaEffect(impactPosition, enraged ? 0.92f : 0.76f, enraged ? 7f : 5f, enraged ? 2.4f : 1.9f, 0.5f, new Color(1f, 0.22f, 0.04f, 0.44f), "fire_pool");
            }

            game.ShakeCamera(enraged ? 0.2f : 0.14f, 0.18f);
        }

        game.PlaySfx("boom", action == 0 ? 0.76f : 0.66f, 0.08f);
    }

    private float GetBossTelegraphDuration()
    {
        if (Type == ZombieStormEnemyType.MossGolemBoss)
        {
            return bossQueuedEnraged ? 0.82f : 1.05f;
        }

        if (Type == ZombieStormEnemyType.BruteBoss)
        {
            return bossQueuedEnraged ? 0.46f : 0.58f;
        }

        if (Type == ZombieStormEnemyType.StormBoss)
        {
            return bossQueuedEnraged ? 0.5f : 0.62f;
        }

        if (Type == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return bossQueuedEnraged ? 0.54f : 0.72f;
        }

        if (Type == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return bossQueuedEnraged ? 0.48f : 0.64f;
        }

        return bossQueuedEnraged ? 0.52f : 0.66f;
    }

    private float GetBossActionCooldown(bool enraged)
    {
        if (Type == ZombieStormEnemyType.PlagueBoss)
        {
            return enraged ? 1.85f : 2.55f;
        }

        if (Type == ZombieStormEnemyType.BruteBoss)
        {
            return enraged ? 1.55f : 2.25f;
        }

        if (Type == ZombieStormEnemyType.StormBoss)
        {
            return enraged ? 1.35f : 2.05f;
        }

        if (Type == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return enraged ? 1.62f : 2.42f;
        }

        if (Type == ZombieStormEnemyType.MossGolemBoss)
        {
            return enraged ? 1.72f : 2.55f;
        }

        if (Type == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return enraged ? 1.28f : 1.9f;
        }

        return enraged ? 2.15f : 3.1f;
    }

    private static bool IsBossType(ZombieStormEnemyType enemyType)
    {
        return enemyType == ZombieStormEnemyType.Boss || enemyType == ZombieStormEnemyType.PlagueBoss || enemyType == ZombieStormEnemyType.BruteBoss || enemyType == ZombieStormEnemyType.StormBoss || enemyType == ZombieStormEnemyType.CrystalGolemBoss || enemyType == ZombieStormEnemyType.MossGolemBoss || enemyType == ZombieStormEnemyType.EmberTyrantBoss;
    }

    private static float ScaledEnemyDamage(float amount)
    {
        return amount * ZombieStormGameController.EnemyDamageMultiplier;
    }

    private static string BossName(ZombieStormEnemyType enemyType)
    {
        if (enemyType == ZombieStormEnemyType.PlagueBoss)
        {
            return "Plague Matriarch";
        }

        if (enemyType == ZombieStormEnemyType.BruteBoss)
        {
            return "Ravager Brute";
        }

        if (enemyType == ZombieStormEnemyType.StormBoss)
        {
            return "Storm Revenant";
        }

        if (enemyType == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return "Crystal Colossus";
        }

        if (enemyType == ZombieStormEnemyType.MossGolemBoss)
        {
            return "Mossbound Colossus";
        }

        if (enemyType == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return "Ember Tyrant";
        }

        return "Horde Alpha";
    }

    private static Color BossAccent(ZombieStormEnemyType enemyType)
    {
        if (enemyType == ZombieStormEnemyType.PlagueBoss)
        {
            return new Color(0.5f, 1f, 0.18f, 0.9f);
        }

        if (enemyType == ZombieStormEnemyType.BruteBoss)
        {
            return new Color(1f, 0.36f, 0.08f, 0.9f);
        }

        if (enemyType == ZombieStormEnemyType.StormBoss)
        {
            return new Color(0.34f, 0.72f, 1f, 0.9f);
        }

        if (enemyType == ZombieStormEnemyType.CrystalGolemBoss)
        {
            return new Color(0.36f, 0.92f, 1f, 0.92f);
        }

        if (enemyType == ZombieStormEnemyType.MossGolemBoss)
        {
            return new Color(0.56f, 0.82f, 0.18f, 0.92f);
        }

        if (enemyType == ZombieStormEnemyType.EmberTyrantBoss)
        {
            return new Color(1f, 0.34f, 0.1f, 0.94f);
        }

        return new Color(1f, 0.2f, 0.15f, 0.9f);
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

        if (deathFrames != null && deathFrames.Length > 0 && spriteRenderer != null)
        {
            deathAnimDuration = deathFrames.Length / 18f;
            deathAnimTime = deathAnimDuration;
            spriteRenderer.sprite = deathFrames[0];
            return;
        }

        game.ReturnPooled(poolKey, gameObject);
    }
}
