using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
    private bool harmsPlayer;

    public void Initialize(ZombieStormGameController owner, string key, float areaRadius, float hitDamage, float duration, float rate, bool targetsPlayer = false)
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
        harmsPlayer = targetsPlayer;
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
            if (harmsPlayer)
            {
                DamagePlayer();
            }
            else
            {
                DamageEnemies();
            }
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

        if (poolKey == "hit_spark" || poolKey == "lightning_flash" || poolKey == "zombie_explosion" || poolKey == "meteor_blast" || poolKey == "foozle_explosion" || poolKey == "ember_dash_blast" || poolKey == "ember_meteor_blast")
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

    private void DamagePlayer()
    {
        if (damage <= 0f || game.Player == null)
        {
            return;
        }

        if (Vector2.Distance(transform.position, game.Player.transform.position) <= radius + 0.38f)
        {
            game.Player.TakeDamage(damage);
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
