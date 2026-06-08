using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Handles persistent area effects such as fire pools and shield bursts.
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

    // Initializes the references and values this object needs at runtime.
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
        frameDuration = frames != null && frames.Length > 0 ? GetFrameDuration(poolKey, maxLife, frames.Length) : 0.05f;
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
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

    // Updates an area effect's scale, transparency, and animation frame.
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

        if (poolKey == "hit_spark" || poolKey == "lightning_flash" || poolKey == "zombie_explosion" || poolKey == "meteor_blast" || poolKey == "foozle_explosion" || poolKey == "poison_boss_blast" || poolKey == "ember_dash_blast" || poolKey == "ember_meteor_blast" || poolKey == "ember_boss_meteor")
        {
            float grow = 1f + (1f - t) * 0.55f;
            transform.localScale = initialScale * grow;
        }
    }

    // Calculates how long each effect animation frame should last.
    private static float GetFrameDuration(string key, float duration, int frameCount)
    {
        if (key == "poison_boss_blast")
        {
            return Mathf.Max(0.045f, duration / Mathf.Max(1, frameCount));
        }

        return Mathf.Clamp(duration / Mathf.Max(1, frameCount), 0.028f, 0.06f);
    }

    // Applies repeated area damage to enemies inside the radius.
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

    // Applies area damage or slow effects to the player.
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

// Shows a warning first, then creates a delayed area damage effect.
public sealed class ZombieStormDelayedAreaEffect : MonoBehaviour
{
    private ZombieStormGameController game;
    private Vector2 position;
    private Color color;
    private string poolKey;
    private float delay;
    private float radius;
    private float damage;
    private float duration;
    private float tickRate;
    private float shakePower;
    private float shakeDuration;
    private float sfxVolume;

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, Vector2 targetPosition, float waitSeconds, float areaRadius, float hitDamage, float effectDuration, float rate, Color effectColor, string key, float impactShakePower, float impactShakeDuration, float impactSfxVolume)
    {
        game = owner;
        position = targetPosition;
        delay = Mathf.Max(0f, waitSeconds);
        radius = areaRadius;
        damage = hitDamage;
        duration = effectDuration;
        tickRate = rate;
        color = effectColor;
        poolKey = key;
        shakePower = impactShakePower;
        shakeDuration = impactShakeDuration;
        sfxVolume = impactSfxVolume;
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
    private void Update()
    {
        delay -= Time.deltaTime;
        if (delay > 0f)
        {
            return;
        }

        if (game != null)
        {
            game.SpawnEnemyAreaEffect(position, radius, damage, duration, tickRate, color, poolKey);
            if (shakePower > 0f)
            {
                game.ShakeCamera(shakePower, shakeDuration);
            }

            if (sfxVolume > 0f)
            {
                game.PlaySfx("boom", sfxVolume, 0.08f);
            }
        }

        Destroy(gameObject);
    }
}

// Controls the ember boss meteor warning, fall animation, and impact blast.
public sealed class ZombieStormEmberMeteorStrike : MonoBehaviour
{
    private const int FlightFrameStart = 3;
    private const int ImpactFrameStart = 10;
    private const float FlightFrameDuration = 0.075f;
    private const float ImpactFrameDuration = 0.085f;
    private const float BurnHoldDuration = 0.38f;

    private ZombieStormGameController game;
    private SpriteRenderer warningRingRenderer;
    private SpriteRenderer warningOuterRenderer;
    private SpriteRenderer warningCoreRenderer;
    private SpriteRenderer shadowRenderer;
    private SpriteRenderer meteorRenderer;
    private Sprite[] frames;
    private Vector2 targetPosition;
    private Vector3 skyOffset;
    private float radius;
    private float damage;
    private float fallDuration;
    private float fallTimer;
    private float impactTimer;
    private float meteorScale;
    private float warningPhase;
    private bool impacting;

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, Vector2 impactPosition, float hitDamage, float areaRadius, float secondsToImpact)
    {
        game = owner;
        targetPosition = impactPosition;
        damage = hitDamage;
        radius = Mathf.Max(0.2f, areaRadius);
        fallDuration = Mathf.Max(0.25f, secondsToImpact);
        fallTimer = 0f;
        impactTimer = 0f;
        impacting = false;
        frames = game.GetEffectFrames("ember_boss_meteor");
        skyOffset = new Vector3(UnityEngine.Random.Range(3.6f, 5.8f), UnityEngine.Random.Range(8.8f, 11.4f), 0f);
        meteorScale = UnityEngine.Random.Range(1.15f, 1.45f);
        warningPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        EnsureChildren();
        transform.position = targetPosition;
        UpdateWarning(1f);
        UpdateMeteorFlight(0f);
        game.PlaySfx("shoot", 0.1f, 0.08f);
    }

    // Creates child objects used by the meteor warning, shadow, and body.
    private void EnsureChildren()
    {
        if (warningRingRenderer == null)
        {
            GameObject warningRing = new GameObject("Meteor Warning Ring");
            warningRing.transform.SetParent(transform, false);
            warningRingRenderer = warningRing.AddComponent<SpriteRenderer>();
            warningRingRenderer.sprite = game.GetOrbitRingSprite();
            warningRingRenderer.sortingOrder = 48;
        }

        if (warningOuterRenderer == null)
        {
            GameObject warningOuter = new GameObject("Meteor Warning Outer");
            warningOuter.transform.SetParent(transform, false);
            warningOuterRenderer = warningOuter.AddComponent<SpriteRenderer>();
            warningOuterRenderer.sprite = game.GetSoftGlowSprite();
            warningOuterRenderer.sortingOrder = 46;
        }

        if (warningCoreRenderer == null)
        {
            GameObject warningCore = new GameObject("Meteor Warning Core");
            warningCore.transform.SetParent(transform, false);
            warningCoreRenderer = warningCore.AddComponent<SpriteRenderer>();
            warningCoreRenderer.sprite = game.GetSoftGlowSprite();
            warningCoreRenderer.sortingOrder = 47;
        }

        if (shadowRenderer == null)
        {
            GameObject shadow = new GameObject("Meteor Ground Shadow");
            shadow.transform.SetParent(transform, false);
            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = game.GetSoftShadowSprite();
            shadowRenderer.sortingOrder = 49;
        }

        if (meteorRenderer == null)
        {
            GameObject meteor = new GameObject("Falling Meteor");
            meteor.transform.SetParent(transform, false);
            meteorRenderer = meteor.AddComponent<SpriteRenderer>();
            meteorRenderer.sortingOrder = 61;
        }
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
    private void Update()
    {
        if (game == null)
        {
            return;
        }

        if (impacting)
        {
            UpdateImpact();
            return;
        }

        fallTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(fallTimer / fallDuration);
        UpdateWarning(1f);
        UpdateMeteorFlight(progress);
        if (progress >= 1f)
        {
            BeginImpact();
        }
    }

    // Updates the flashing warning ring before meteor impact.
    private void UpdateWarning(float fade)
    {
        float pulse = 0.5f + Mathf.Sin(Time.time * 16f + warningPhase) * 0.5f;
        float scalePulse = 1f + pulse * 0.12f;
        if (warningRingRenderer != null)
        {
            warningRingRenderer.transform.localPosition = Vector3.zero;
            warningRingRenderer.transform.localScale = Vector3.one * radius * 2.3f * scalePulse;
            warningRingRenderer.transform.rotation = Quaternion.Euler(0f, 0f, Time.time * 38f);
            warningRingRenderer.color = new Color(1f, 0.02f, 0f, (0.46f + pulse * 0.34f) * fade);
        }

        if (warningOuterRenderer != null)
        {
            warningOuterRenderer.transform.localPosition = Vector3.zero;
            warningOuterRenderer.transform.localScale = Vector3.one * radius * 3.05f * scalePulse;
            warningOuterRenderer.color = new Color(1f, 0.02f, 0f, (0.12f + pulse * 0.16f) * fade);
        }

        if (warningCoreRenderer != null)
        {
            warningCoreRenderer.transform.localPosition = Vector3.zero;
            warningCoreRenderer.transform.localScale = Vector3.one * radius * 0.95f * (1f + pulse * 0.08f);
            warningCoreRenderer.color = new Color(1f, 0.24f, 0.02f, (0.16f + pulse * 0.16f) * fade);
        }
    }

    // Animates the meteor falling toward its target point.
    private void UpdateMeteorFlight(float progress)
    {
        if (meteorRenderer == null)
        {
            return;
        }

        float eased = progress * progress * (3f - 2f * progress);
        Vector3 start = skyOffset;
        Vector3 end = new Vector3(0f, 0.16f, 0f);
        Vector3 position = Vector3.Lerp(start, end, eased);
        float arcLift = Mathf.Sin(progress * Mathf.PI) * 0.55f;
        position.y += arcLift;
        meteorRenderer.transform.localPosition = position;
        meteorRenderer.transform.localScale = Vector3.one * meteorScale * Mathf.Lerp(0.82f, 1.36f, eased);
        meteorRenderer.transform.rotation = Quaternion.Euler(0f, 0f, 42f + Mathf.Sin(Time.time * 3f + warningPhase) * 4f);
        meteorRenderer.color = Color.Lerp(new Color(1f, 1f, 1f, 0.72f), Color.white, progress);
        UpdateGroundShadow(progress);
        SetMeteorFrame(FlightFrameStart + Mathf.Abs(Mathf.FloorToInt(fallTimer / FlightFrameDuration)) % Mathf.Max(1, ImpactFrameStart - FlightFrameStart));
    }

    // Updates meteor shadow size and transparency during the fall.
    private void UpdateGroundShadow(float progress)
    {
        if (shadowRenderer == null)
        {
            return;
        }

        float eased = Mathf.SmoothStep(0f, 1f, progress);
        shadowRenderer.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        shadowRenderer.transform.localScale = new Vector3(radius * Mathf.Lerp(0.45f, 2.05f, eased), radius * Mathf.Lerp(0.16f, 0.68f, eased), 1f);
        shadowRenderer.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.04f, 0.42f, eased));
    }

    // Starts the meteor impact, applies damage, and plays feedback.
    private void BeginImpact()
    {
        impacting = true;
        impactTimer = 0f;
        if (meteorRenderer != null)
        {
            meteorRenderer.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            meteorRenderer.transform.localScale = Vector3.one * meteorScale * 1.42f;
            meteorRenderer.transform.rotation = Quaternion.identity;
            meteorRenderer.color = Color.white;
            SetMeteorFrame(ImpactFrameStart);
        }

        if (shadowRenderer != null)
        {
            shadowRenderer.color = new Color(0f, 0f, 0f, 0f);
        }

        if (damage > 0f && game.Player != null && Vector2.Distance(targetPosition, game.Player.transform.position) <= radius + 0.38f)
        {
            game.Player.TakeDamage(damage);
        }

        game.PlaySfx("boom", 0.32f, 0.08f);
        game.ShakeCamera(0.08f, 0.16f);
        game.FlashScreen(new Color(1f, 0.28f, 0.04f, 1f), 0.22f);
        for (int i = 0; i < 2; i++)
        {
            game.SpawnHitSpark(targetPosition + UnityEngine.Random.insideUnitCircle * radius * 0.65f, new Color(1f, 0.5f, 0.08f, 0.84f), UnityEngine.Random.Range(0.18f, 0.32f));
        }
    }

    // Updates the meteor impact animation and recycles the object when finished.
    private void UpdateImpact()
    {
        impactTimer += Time.deltaTime;
        float warningFade = Mathf.Clamp01(1f - impactTimer / 0.22f);
        UpdateWarning(warningFade);

        if (frames == null || frames.Length == 0)
        {
            game.ReturnPooled("ember_meteor_strike", gameObject);
            return;
        }

        int impactLength = Mathf.Max(1, frames.Length - ImpactFrameStart);
        int impactFrame = Mathf.FloorToInt(impactTimer / ImpactFrameDuration);
        if (impactFrame < impactLength)
        {
            SetMeteorFrame(ImpactFrameStart + impactFrame);
        }
        else
        {
            SetMeteorFrame(frames.Length - 1);
            if (impactTimer >= impactLength * ImpactFrameDuration + BurnHoldDuration)
            {
                game.ReturnPooled("ember_meteor_strike", gameObject);
            }
        }
    }

    // Sets the current meteor sprite frame.
    private void SetMeteorFrame(int frameIndex)
    {
        if (meteorRenderer == null || frames == null || frames.Length == 0)
        {
            return;
        }

        meteorRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
    }
}

// Returns temporary visual effects to the object pool after their lifetime ends.
public sealed class ZombieStormTimedPooled : MonoBehaviour
{
    private ZombieStormGameController game;
    private string poolKey;
    private float life;
    private float maxLife;
    private SpriteRenderer spriteRenderer;
    private Color initialColor;

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, string key, float duration)
    {
        game = owner;
        poolKey = key;
        life = duration;
        maxLife = Mathf.Max(0.01f, duration);
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
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
