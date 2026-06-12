using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Owns the survivor's moment-to-moment state: input movement, facing animation, health,
// XP/level progression, pickup magnet range, hurt cooldown, and temporary slow effects.
public sealed class ZombieStormPlayer : MonoBehaviour
{
    private const float AnimatedPlayerVisualScale = 1.55f;
    private const float FallbackPlayerVisualScale = 1.28f;
    private const float HealthBarWidth = 1.18f;
    private const float HealthBarHeight = 0.1f;
    private const float HurtAnimationDuration = 0.32f;
    private static readonly Color FrozenTint = new Color(0.48f, 0.82f, 1f, 1f);

    private ZombieStormGameController game;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer healthBarFill;
    private Transform healthBarFillTransform;
    private Vector2 lastMove = Vector2.down;
    private float hurtCooldown;
    private float hurtAnimationTimer;
    private float slowTimer;
    private float slowMultiplier = 1f;
    private float animationTimer;
    private int animationFrame;
    private int hurtAnimationFrame;
    private string facingDirection = "walk_down";

    public int Level { get; private set; }
    public float Experience { get; private set; }
    public float ExperienceToNext { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public int Kills { get; set; }
    public float PickupRange { get { return 1.35f + game.GetPassiveLevel(ZombieStormPassiveType.PickupRange) * 0.35f; } }

    // Resets a fresh run's player state, stores the controller/renderer references,
    // initializes health/XP counters, and builds the floating health bar.
    public void Initialize(ZombieStormGameController owner, SpriteRenderer renderer)
    {
        game = owner;
        spriteRenderer = renderer;
        Level = 1;
        Experience = 0f;
        ExperienceToNext = 12f;
        MaxHealth = 115f;
        Health = MaxHealth;
        Kills = 0;
        BuildHealthBar();
    }

    // Reads WASD/arrow input, applies move-speed and slow modifiers, clamps the player
    // against the arena/obstacles, advances hurt timers, and refreshes animation/health UI.
    private void Update()
    {
        if (game == null)
        {
            return;
        }

        hurtCooldown -= Time.deltaTime;
        hurtAnimationTimer -= Time.deltaTime;
        slowTimer -= Time.deltaTime;
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        float speed = 4.6f + game.GetPassiveLevel(ZombieStormPassiveType.MoveSpeed) * 0.36f;
        if (slowTimer > 0f)
        {
            speed *= slowMultiplier;
        }

        transform.position += (Vector3)(input * speed * Time.deltaTime);
        transform.position = game.ResolveObstacleCollision(transform.position, 0.34f);

        if (input.sqrMagnitude > 0.01f)
        {
            lastMove = input.normalized;
            if (Mathf.Abs(lastMove.x) > 0.08f)
            {
                facingDirection = DirectionToAnimation(lastMove);
            }

            transform.rotation = Quaternion.identity;
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        if (spriteRenderer != null)
        {
            Color targetColor = slowTimer > 0f ? FrozenTint : Color.white;
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, 6f * Time.deltaTime);
            UpdatePlayerAnimation(input.sqrMagnitude > 0.01f);
        }

        UpdateHealthBar();
    }

    // Creates the floating health bar frame and fill as child sprite objects above the survivor.
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

    // Resizes the health fill around its left edge and shifts color toward orange at low health.
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

    // Selects the current player sprite from hurt frames, walking frames, or idle fallback,
    // preserving the last horizontal facing direction when the player stops moving.
    private void UpdatePlayerAnimation(bool moving)
    {
        if (!game.HasPlayerWalkAnimation)
        {
            spriteRenderer.flipX = lastMove.x < -0.08f;
            float pulse = Mathf.Sin(Time.time * 12f) * 0.04f;
            transform.localScale = Vector3.one * (FallbackPlayerVisualScale + pulse);
            return;
        }

        transform.localScale = Vector3.one * AnimatedPlayerVisualScale;
        spriteRenderer.flipX = IsLeftFacingDirection(facingDirection);
        if (hurtAnimationTimer > 0f && game.HasPlayerHurtAnimation)
        {
            float progress = 1f - Mathf.Clamp01(hurtAnimationTimer / HurtAnimationDuration);
            hurtAnimationFrame = Mathf.Min(game.PlayerHurtFrameCount - 1, Mathf.FloorToInt(progress * game.PlayerHurtFrameCount));
            spriteRenderer.sprite = game.GetPlayerHurtFrame(hurtAnimationFrame);
            return;
        }

        if (moving)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= 0.033f)
            {
                animationTimer = 0f;
                animationFrame++;
            }

            spriteRenderer.sprite = game.GetPlayerWalkFrame(facingDirection, animationFrame);
            return;
        }

        if (game.HasPlayerIdleAnimation || game.PlayerWalkFramesAreIdle)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= 0.075f)
            {
                animationTimer = 0f;
                animationFrame++;
            }

            spriteRenderer.sprite = game.HasPlayerIdleAnimation
                ? game.GetPlayerIdleFrame(animationFrame)
                : game.GetPlayerWalkFrame(facingDirection, animationFrame);
            return;
        }

        animationTimer = 0f;
        animationFrame = 0;
        spriteRenderer.sprite = game.GetPlayerWalkFrame(facingDirection, animationFrame);
    }

    // Returns true for directional animation keys whose imported frames face left.
    private static bool IsLeftFacingDirection(string direction)
    {
        return direction == "walk_left";
    }

    // Maps the current movement vector to the walk animation bank used by the loaded player art.
    private static string DirectionToAnimation(Vector2 direction)
    {
        return direction.x < -0.08f ? "walk_left" : "walk_right";
    }

    // Adds XP, handles one or more level-ups, and asks the controller to open upgrade choices.
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

    // Applies incoming damage with brief repeat-hit mitigation, plays hurt feedback/animation,
    // and ends the run if health reaches zero.
    public void TakeDamage(float amount)
    {
        if (hurtCooldown > 0f)
        {
            amount *= 0.35f;
        }

        Health -= amount;
        hurtCooldown = 0.12f;
        hurtAnimationTimer = HurtAnimationDuration;
        hurtAnimationFrame = 0;
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

    // Applies the strongest active slow multiplier and keeps the longer remaining slow duration.
    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        slowTimer = Mathf.Max(slowTimer, duration);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = FrozenTint;
        }
    }

    // Restores health while clamping to the current maximum.
    public void Heal(float amount)
    {
        Health = Mathf.Min(MaxHealth, Health + amount);
    }

    // Raises max health and immediately grants the same amount as current health.
    public void IncreaseMaxHealth(float amount)
    {
        MaxHealth += amount;
        Heal(amount);
    }
}
