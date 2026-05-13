using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
    public bool IsBoss { get { return IsBossType(Type); } }
    public string DisplayName { get { return BossName(Type); } }

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

        if (IsBoss)
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

    private void UpdateBoss(Vector2 direction)
    {
        bool enraged = health < maxHealth * 0.5f;
        float moveMultiplier = Type == ZombieStormEnemyType.BruteBoss ? (enraged ? 1.55f : 1.18f) : Type == ZombieStormEnemyType.StormBoss ? (enraged ? 1.65f : 1.25f) : enraged ? 1.35f : 1f;
        transform.position += (Vector3)(direction * speed * moveMultiplier * Time.deltaTime);
        bossActionTimer -= Time.deltaTime;
        if (bossActionTimer > 0f)
        {
            return;
        }

        if (Type == ZombieStormEnemyType.PlagueBoss)
        {
            CastPlagueBossSkill(direction, enraged);
        }
        else if (Type == ZombieStormEnemyType.BruteBoss)
        {
            CastBruteBossSkill(direction, enraged);
        }
        else if (Type == ZombieStormEnemyType.StormBoss)
        {
            CastStormBossSkill(direction, enraged);
        }
        else
        {
            CastAlphaBossSkill(direction, enraged);
        }

        bossActionTimer = GetBossActionCooldown(enraged);
    }

    private void CastAlphaBossSkill(Vector2 direction, bool enraged)
    {
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
    }

    private void CastPlagueBossSkill(Vector2 direction, bool enraged)
    {
        int pools = enraged ? 10 : 7;
        for (int i = 0; i < pools; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.6f, enraged ? 5.2f : 4.2f);
            game.SpawnAreaEffect((Vector2)transform.position + offset, enraged ? 1.12f : 0.95f, 8f, enraged ? 3.1f : 2.4f, 0.45f, new Color(0.5f, 1f, 0.18f, 0.42f), "toxic_pool");
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
        transform.position += (Vector3)(direction * dashDistance);
        game.ShakeCamera(enraged ? 0.24f : 0.18f, 0.22f);
        game.SpawnAreaEffect(transform.position, enraged ? 2.2f : 1.75f, 0f, 0.24f, 1f, new Color(1f, 0.36f, 0.08f, 0.54f), "zombie_explosion");

        if (game.Player != null && Vector2.Distance(transform.position, game.Player.transform.position) < (enraged ? 2.75f : 2.2f))
        {
            game.Player.TakeDamage(enraged ? 32f : 24f);
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
        Vector2 playerPosition = game.Player != null ? (Vector2)game.Player.transform.position : (Vector2)transform.position + direction * 3f;
        for (int i = 0; i < strikes; i++)
        {
            Vector2 strikePosition = playerPosition + UnityEngine.Random.insideUnitCircle * (enraged ? 3.7f : 2.9f);
            float radius = enraged ? 1.15f : 0.9f;
            game.SpawnAreaEffect(strikePosition, radius, 0f, 0.18f, 1f, new Color(0.34f, 0.72f, 1f, 0.56f), "lightning_flash");
            if (game.Player != null && Vector2.Distance(strikePosition, game.Player.transform.position) <= radius + 0.35f)
            {
                game.Player.TakeDamage(enraged ? 18f : 12f);
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

        return enraged ? 2.15f : 3.1f;
    }

    private static bool IsBossType(ZombieStormEnemyType enemyType)
    {
        return enemyType == ZombieStormEnemyType.Boss || enemyType == ZombieStormEnemyType.PlagueBoss || enemyType == ZombieStormEnemyType.BruteBoss || enemyType == ZombieStormEnemyType.StormBoss;
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

        game.ReturnPooled(poolKey, gameObject);
    }
}
