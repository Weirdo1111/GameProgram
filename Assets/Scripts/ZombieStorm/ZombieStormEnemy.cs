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
