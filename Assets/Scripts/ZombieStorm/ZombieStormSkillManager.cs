using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Owns the player's active skill loadout. It tracks learned skill levels, specialization
// upgrades, evolved skills, persistent helpers, cooldowns, and all automatic casts.
public sealed class ZombieStormSkillManager : MonoBehaviour
{
    private readonly Dictionary<ZombieStormSkillType, int> levels = new Dictionary<ZombieStormSkillType, int>();
    private readonly Dictionary<ZombieStormSkillType, float> cooldowns = new Dictionary<ZombieStormSkillType, float>();
    private readonly Dictionary<string, int> skillUpgrades = new Dictionary<string, int>();
    private readonly HashSet<ZombieStormSkillType> evolved = new HashSet<ZombieStormSkillType>();
    private readonly List<GameObject> orbitingObjects = new List<GameObject>();
    private readonly List<GameObject> drones = new List<GameObject>();
    private readonly List<ZombieStormPendingSkillBlast> pendingBlasts = new List<ZombieStormPendingSkillBlast>();

    private ZombieStormGameController game;
    private ZombieStormPlayer player;
    private float ultimateCooldown;
    private int fireBombAttackCounter;

    public int KnownSkillCount
    {
        get { return levels.Count; }
    }

    // Stores controller and player references used by every skill formula and spawn call.
    public void Initialize(ZombieStormGameController owner, ZombieStormPlayer survivor)
    {
        game = owner;
        player = survivor;
    }

    // Cleans up persistent skill helper objects so blades/spirits do not survive after the player.
    private void OnDestroy()
    {
        CleanupPersistentSkillObjects();
    }

    // Ticks all learned automatic skills, updates persistent blade/spirit objects, processes
    // delayed blasts, and listens for the manual ultimate key.
    private void Update()
    {
        if (game == null || player == null)
        {
            return;
        }

        TickSkill(ZombieStormSkillType.MagicBolt, CastMagicBolt);
        TickSkill(ZombieStormSkillType.Regeneration, CastRegeneration);
        TickSkill(ZombieStormSkillType.ShieldBurst, CastShieldBurst);
        UpdatePendingBlasts();
        UpdateOrbitingKnives();
        UpdateSummonDrones();
        UpdateUltimateInput();
    }

    // Adds a new skill at level 1, seeds its first cooldown, and builds persistent visuals for
    // skills that stay around the player.
    public void LearnSkill(ZombieStormSkillType weapon)
    {
        if (GetSkillLevel(weapon) > 0)
        {
            return;
        }

        levels[weapon] = 1;
        cooldowns[weapon] = weapon == ZombieStormSkillType.Regeneration ? 3f : 0.05f;
        if (weapon == ZombieStormSkillType.OrbitingKnife)
        {
            RebuildOrbitingKnives();
        }
        else if (weapon == ZombieStormSkillType.SummonDrone)
        {
            RebuildDrones();
        }
    }

    // Increases a learned skill up to its cap and rebuilds helpers whose count/radius changes.
    // Regeneration level 2 also grants the max-health bonus defined by its design.
    public void LevelUpSkill(ZombieStormSkillType weapon)
    {
        int current = GetSkillLevel(weapon);
        int maxLevel = weapon == ZombieStormSkillType.Regeneration ? 3 : weapon == ZombieStormSkillType.OrbitingKnife ? 4 : 5;
        int next = Mathf.Min(maxLevel, current + 1);
        if (next == current)
        {
            return;
        }

        levels[weapon] = next;
        if (weapon == ZombieStormSkillType.Regeneration && next == 2)
        {
            player.IncreaseMaxHealth(30f);
        }

        if (weapon == ZombieStormSkillType.OrbitingKnife)
        {
            RebuildOrbitingKnives();
        }
        else if (weapon == ZombieStormSkillType.SummonDrone)
        {
            RebuildDrones();
        }
    }

    // Returns the learned level for a skill; zero means the player does not know it yet.
    public int GetSkillLevel(ZombieStormSkillType weapon)
    {
        int level;
        return levels.TryGetValue(weapon, out level) ? level : 0;
    }

    // Returns how many times a named specialization has been chosen.
    public int GetSkillUpgradeLevel(string key)
    {
        int level;
        return skillUpgrades.TryGetValue(key, out level) ? level : 0;
    }

    // Increments a specialization up to level 3 and rebuilds any persistent helpers affected by it.
    public void AddSkillUpgrade(string key)
    {
        int next = Mathf.Min(3, GetSkillUpgradeLevel(key) + 1);
        skillUpgrades[key] = next;
        if (key == "knife_blades" || key == "knife_reach")
        {
            RebuildOrbitingKnives();
        }
        else if (key == "drone_swarm")
        {
            RebuildDrones();
        }
    }

    // Reports whether the skill has reached its passive-gated evolution state.
    public bool IsEvolved(ZombieStormSkillType weapon)
    {
        return evolved.Contains(weapon);
    }

    // Marks a skill as evolved and refreshes helpers so evolved counts/radii appear immediately.
    public void Evolve(ZombieStormSkillType weapon)
    {
        evolved.Add(weapon);
        if (weapon == ZombieStormSkillType.OrbitingKnife)
        {
            RebuildOrbitingKnives();
        }
        else if (weapon == ZombieStormSkillType.SummonDrone)
        {
            RebuildDrones();
        }
    }

    // Builds the compact HUD loadout text, including ultimate cooldown and evolved markers.
    public string GetLoadoutText()
    {
        string text = "Skills";
        int ultimateLevel = GetSkillLevel(ZombieStormSkillType.UltimateStorm);
        if (ultimateLevel > 0)
        {
            text += "   F " + Mathf.Max(0f, ultimateCooldown).ToString("0.0") + "s";
        }

        text += "\n";
        if (levels.Count == 0)
        {
            return text + "None";
        }

        foreach (KeyValuePair<ZombieStormSkillType, int> pair in levels)
        {
            if (pair.Key == ZombieStormSkillType.UltimateStorm)
            {
                text += "Ultimate Lv." + pair.Value + "\n";
                continue;
            }

            text += SkillLabel(pair.Key) + " Lv." + pair.Value + (IsEvolved(pair.Key) ? " Evolved" : "") + "\n";
        }

        return text;
    }

    // Converts internal skill enum values into short labels that fit the HUD.
    private static string SkillLabel(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Magic Bolt";
            case ZombieStormSkillType.OrbitingKnife: return "Fire Blades";
            case ZombieStormSkillType.Regeneration: return "Regen";
            case ZombieStormSkillType.FireZone: return "Fire Zone";
            case ZombieStormSkillType.SummonDrone: return "Fire Spirit";
            case ZombieStormSkillType.ShieldBurst: return "Shield";
            case ZombieStormSkillType.UltimateStorm: return "Ultimate";
            default: return weapon.ToString();
        }
    }

    private delegate void SkillAction(int level);

    // Handles automatic skill cooldowns: ignores unlearned skills, casts ready skills,
    // and counts down the remaining cooldown for skills that are not ready yet.
    private void TickSkill(ZombieStormSkillType weapon, SkillAction action)
    {
        int level = GetSkillLevel(weapon);
        if (level <= 0)
        {
            return;
        }

        float current;
        cooldowns.TryGetValue(weapon, out current);
        current -= Time.deltaTime;
        if (current <= 0f)
        {
            action(level);
        }
        else
        {
            cooldowns[weapon] = current;
        }
    }

    // Returns the current level of a named skill upgrade for use in damage, range, and cooldown formulas.
    private int Mod(string key)
    {
        return GetSkillUpgradeLevel(key);
    }

    // Finds the nearest target and fires one or more Magic Bolt projectiles with level/upgrades
    // controlling spread, pierce, damage, projectile size, and Fire Zone attack counting.
    private void CastMagicBolt(int level)
    {
        ZombieStormEnemy target = game.FindNearestEnemy(transform.position, IsEvolved(ZombieStormSkillType.MagicBolt) ? 18f : 14f);
        if (target == null)
        {
            cooldowns[ZombieStormSkillType.MagicBolt] = 0.15f;
            return;
        }

        Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
        float targetDistance = toTarget.magnitude;
        Vector2 direction = targetDistance > 0.01f ? toTarget / targetDistance : Vector2.up;
        Vector2 origin = (Vector2)transform.position + direction * Mathf.Min(0.42f, Mathf.Max(0.12f, targetDistance * 0.42f));
        int shots = (IsEvolved(ZombieStormSkillType.MagicBolt) ? 3 : level >= 4 ? 2 : 1) + Mod("magic_split");
        float damage = GetMagicBoltBaseDamage(level);
        int pierce = (level >= 3 ? 1 : 0) + Mod("magic_pierce");
        float powerTint = Mathf.Clamp01(Mod("magic_force") * 0.18f + (IsEvolved(ZombieStormSkillType.MagicBolt) ? 0.32f : 0f));
        Color fireballColor = Color.Lerp(Color.white, new Color(1f, 0.42f, 0.12f, 1f), powerTint);
        float fireballSize = 0.78f + level * 0.055f + Mod("magic_force") * 0.06f + Mod("magic_pierce") * 0.035f + (IsEvolved(ZombieStormSkillType.MagicBolt) ? 0.12f : 0f);
        game.SpawnAreaEffect(origin, 0.52f + level * 0.035f, 0f, 0.22f, 1f, new Color(1f, 0.5f, 0.12f, 0.72f), "foozle_explosion");
        game.PlaySfx("normal_attack", 0.62f, 0.045f);
        for (int i = 0; i < shots; i++)
        {
            float spreadStep = shots <= 3 ? 9f : 7f;
            float angle = shots == 1 ? 0f : -(shots - 1) * spreadStep * 0.5f + i * spreadStep;
            game.SpawnPlayerProjectile(origin, ZombieStormGameController.Rotate(direction, angle), RollDamage(damage), 13.5f, 1.4f, pierce, fireballColor, fireballSize);
        }

        CountFireZoneAttack(origin, target.transform.position);
        float baseCooldown = IsEvolved(ZombieStormSkillType.MagicBolt) ? 0.18f : 0.62f - level * 0.055f;
        cooldowns[ZombieStormSkillType.MagicBolt] = baseCooldown * game.CooldownMultiplier;
    }

    // Applies the passive regeneration tick and refreshes its cooldown from the current level.
    private void CastRegeneration(int level)
    {
        player.Heal(1f);
        game.SpawnHitSpark(player.transform.position + Vector3.up * 0.35f, new Color(0.42f, 1f, 0.58f, 0.68f), 0.2f);
        cooldowns[ZombieStormSkillType.Regeneration] = level >= 3 ? 2f : 3f;
    }

    // Called by older projectile hooks when a fireball should immediately create Fire Zone impact.
    public void SpawnFireZoneOnFireballHit(Vector2 position)
    {
        int level = GetSkillLevel(ZombieStormSkillType.FireZone);
        if (level <= 0)
        {
            return;
        }

        SpawnFireBombImpact(position, level);
    }

    // Counts Magic Bolt attacks toward Fire Zone's throw threshold and launches a bomb at the
    // current target once enough attacks have happened.
    private void CountFireZoneAttack(Vector2 origin, Vector2 targetPosition)
    {
        int level = GetSkillLevel(ZombieStormSkillType.FireZone);
        if (level <= 0)
        {
            fireBombAttackCounter = 0;
            return;
        }

        fireBombAttackCounter++;
        int threshold = GetFireBombAttackThreshold(level);
        if (fireBombAttackCounter < threshold)
        {
            return;
        }

        fireBombAttackCounter = 0;
        Vector2 scatter = UnityEngine.Random.insideUnitCircle * 0.34f;
        float impactRadius = GetFireBombImpactRadius(level);
        float impactDamage = RollDamage(GetFireBombImpactDamage());
        float burnRadius = GetFireBombBurnRadius(level);
        float burnDamage = RollDamage(GetFireBombBurnDamage(level));
        game.SpawnFireBombProjectile(origin, targetPosition + scatter, impactDamage, impactRadius, level >= 2, burnDamage, burnRadius, GetFireBombBurnDuration(level), 0.42f);
    }

    // Creates the immediate Fire Zone impact and optional burn patch without using the arcing bomb.
    private void SpawnFireBombImpact(Vector2 position, int level)
    {
        float impactRadius = GetFireBombImpactRadius(level);
        game.SpawnAreaEffect(position, impactRadius, RollDamage(GetFireBombImpactDamage()), 0.18f, 99f, new Color(1f, 0.52f, 0.08f, 0.78f), "foozle_explosion");
        if (level >= 2)
        {
            game.SpawnAreaEffect(position, GetFireBombBurnRadius(level), RollDamage(GetFireBombBurnDamage(level)), GetFireBombBurnDuration(level), 0.42f, new Color(1f, 0.42f, 0.08f, 0.72f), game.GetRandomGroundFireEffectKey());
        }
    }

    // Calculates Magic Bolt's pre-multiplier base damage from level and force upgrades.
    private float GetMagicBoltBaseDamage(int level)
    {
        return (10f + level * 3.4f) * (1f + Mod("magic_force") * 0.16f);
    }

    // Scales Fire Zone's impact damage from the current Magic Bolt build so both skills stay linked.
    private float GetFireBombImpactDamage()
    {
        int magicBoltLevel = Mathf.Max(1, GetSkillLevel(ZombieStormSkillType.MagicBolt));
        return GetMagicBoltBaseDamage(magicBoltLevel) * 1.8f;
    }

    // Calculates Fire Zone's impact radius, including global area passive scaling.
    private float GetFireBombImpactRadius(int level)
    {
        return (1.18f + level * 0.12f) * game.AreaMultiplier * 1.3f;
    }

    // Calculates the lingering burn patch radius after the impact explosion.
    private float GetFireBombBurnRadius(int level)
    {
        return (1.05f + level * 0.12f) * game.AreaMultiplier * 0.5f;
    }

    // Calculates each burn tick before global damage/crit rolls; level 5 adds a burn bonus.
    private float GetFireBombBurnDamage(int level)
    {
        float levelFiveBonus = level >= 5 ? 1.5f : 1f;
        return (4.8f + level * 1.4f) * 1.5f * levelFiveBonus;
    }

    // Returns how long the Fire Zone burn patch stays active.
    private static float GetFireBombBurnDuration(int level)
    {
        return level >= 5 ? 6f : 5f;
    }

    // Returns how many Magic Bolt attacks are required before Fire Zone throws another bomb.
    private static int GetFireBombAttackThreshold(int level)
    {
        if (level >= 4)
        {
            return 2;
        }

        return level >= 3 ? 3 : 4;
    }

    // Fires Shield Burst only when enemies are close enough, then damages the area and starts
    // its recharge cooldown from level and recharge upgrades.
    private void CastShieldBurst(int level)
    {
        float radius = (1.35f + level * 0.24f + Mod("shield_radius") * 0.22f) * game.AreaMultiplier;
        if (CountEnemiesNear(transform.position, radius + 0.35f) <= 0)
        {
            cooldowns[ZombieStormSkillType.ShieldBurst] = 0.18f;
            return;
        }

        game.SpawnAreaEffect(transform.position, radius, RollDamage((12f + level * 4f) * (1f + Mod("shield_force") * 0.18f)), 0.18f, 99f, new Color(0.75f, 0.95f, 1f, 0.55f), "shield_burst");
        game.SpawnAreaEffect(transform.position, radius * 1.38f, 0f, 0.24f, 1f, new Color(0.4f, 0.92f, 1f, 0.32f), "shield_burst");
        game.ShakeCamera(0.06f, 0.1f);
        cooldowns[ZombieStormSkillType.ShieldBurst] = Mathf.Max(0.65f, 2.4f - level * 0.16f - Mod("shield_recharge") * 0.18f) * game.CooldownMultiplier;
    }

    // Keeps Fire Blades arranged around the player, checks enemies inside the orbit radius on a
    // shared tick cooldown, and applies blade damage plus spark feedback.
    private void UpdateOrbitingKnives()
    {
        int level = GetSkillLevel(ZombieStormSkillType.OrbitingKnife);
        if (level <= 0)
        {
            return;
        }

        float radius = (2.05f + level * 0.24f + Mod("knife_reach") * 0.3f) * game.AreaMultiplier * (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 1.32f : 1f);
        float speed = 120f + level * 28f;
        if (orbitingObjects.Count == 0)
        {
            RebuildOrbitingKnives();
        }

        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            float angle = Time.time * speed + i * (360f / Mathf.Max(1, orbitingObjects.Count));
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            orbitingObjects[i].transform.position = (Vector2)transform.position + offset;
            orbitingObjects[i].transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            float bladeScale = (0.78f + level * 0.035f + Mod("knife_edge") * 0.035f) * (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 1.08f : 1f);
            orbitingObjects[i].transform.localScale = Vector3.one * bladeScale;
        }

        float current;
        cooldowns.TryGetValue(ZombieStormSkillType.OrbitingKnife, out current);
        current -= Time.deltaTime;
        if (current > 0f)
        {
            cooldowns[ZombieStormSkillType.OrbitingKnife] = current;
            return;
        }

        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(enemy.transform.position, transform.position) <= radius + enemy.Radius)
            {
                Vector2 enemyPosition = enemy.transform.position;
                enemy.TakeDamage(RollDamage((6f + level * 1.8f) * 4f * (1f + Mod("knife_edge") * 0.18f)), (enemyPosition - (Vector2)transform.position).normalized);
                game.SpawnHitSpark(enemyPosition, new Color(1f, 0.32f, 0.16f, 0.86f), 0.28f);
                game.SpawnAreaEffect(enemyPosition, 0.34f, 0f, 0.12f, 1f, new Color(1f, 0.16f, 0.06f, 0.42f), "hit_spark");
            }
        }

        cooldowns[ZombieStormSkillType.OrbitingKnife] = 0.24f * game.CooldownMultiplier;
    }

    // Destroys and recreates Fire Blade helper objects whenever level, reach, count, or evolution changes.
    private void RebuildOrbitingKnives()
    {
        CleanupOrbitingKnives();

        int level = GetSkillLevel(ZombieStormSkillType.OrbitingKnife);
        int count = GetFireBladeCount(level) + Mod("knife_blades") + (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 3 : 0);
        float radius = (2.05f + level * 0.24f + Mod("knife_reach") * 0.3f) * game.AreaMultiplier * (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 1.32f : 1f);
        for (int i = 0; i < count; i++)
        {
            GameObject blade = new GameObject("Orbiting Skill Blade");
            blade.transform.SetParent(transform, true);
            float angle = i * (360f / Mathf.Max(1, count));
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            blade.transform.position = (Vector2)transform.position + offset;
            blade.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            blade.transform.localScale = Vector3.one * (0.78f + level * 0.035f);
            SpriteRenderer spriteRenderer = blade.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = game.GetSkillSprite(ZombieStormSkillType.OrbitingKnife);
            spriteRenderer.color = new Color(1f, 0.94f, 0.92f, 1f);
            spriteRenderer.sortingOrder = 42;
            orbitingObjects.Add(blade);
        }
    }

    // Destroys and recreates Fire Spirit helper sprites from level, swarm upgrades, and evolution.
    private void RebuildDrones()
    {
        CleanupDrones();
        int level = GetSkillLevel(ZombieStormSkillType.SummonDrone);
        int baseCount = Mathf.Max(1, level);
        int count = baseCount + Mod("drone_swarm") + (IsEvolved(ZombieStormSkillType.SummonDrone) ? 2 : 0);
        for (int i = 0; i < count; i++)
        {
            GameObject drone = new GameObject("Fire Spirit");
            drone.transform.SetParent(transform, true);
            drone.transform.localScale = Vector3.one * 0.46f;
            SpriteRenderer spriteRenderer = drone.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = game.GetSkillSprite(ZombieStormSkillType.SummonDrone);
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 34;
            drones.Add(drone);
        }
    }

    // Positions Fire Spirits in stable slots around the player and lets each one shoot at its
    // nearest enemy when the shared spirit cooldown is ready.
    private void UpdateSummonDrones()
    {
        int level = GetSkillLevel(ZombieStormSkillType.SummonDrone);
        if (level <= 0)
        {
            return;
        }

        if (drones.Count == 0)
        {
            RebuildDrones();
        }

        for (int i = 0; i < drones.Count; i++)
        {
            Vector2 desired = (Vector2)transform.position + GetFixedFireSpiritOffset(i, drones.Count);
            drones[i].transform.position = desired;
        }

        float current;
        cooldowns.TryGetValue(ZombieStormSkillType.SummonDrone, out current);
        current -= Time.deltaTime;
        if (current > 0f)
        {
            cooldowns[ZombieStormSkillType.SummonDrone] = current;
            return;
        }

        for (int i = 0; i < drones.Count; i++)
        {
            ZombieStormEnemy target = game.FindNearestEnemy(drones[i].transform.position, 9f);
            if (target == null)
            {
                continue;
            }

            Vector2 direction = ((Vector2)target.transform.position - (Vector2)drones[i].transform.position).normalized;
            Vector2 muzzle = (Vector2)drones[i].transform.position + direction * 0.26f;
            game.SpawnHitSpark(muzzle, new Color(1f, 0.45f, 0.08f, 0.82f), 0.18f);
            game.SpawnPlayerProjectile(muzzle, direction, RollDamage((7f + level * 2.4f) * (1f + Mod("drone_focus") * 0.18f)), 12f, 1.1f, 0, new Color(1f, 0.42f, 0.08f), 0.62f, true);
        }

        float levelCooldownBonus = level >= 2 ? 0.18f : 0f;
        cooldowns[ZombieStormSkillType.SummonDrone] = Mathf.Max(0.22f, 0.92f - levelCooldownBonus - Mod("drone_overclock") * 0.08f) * game.CooldownMultiplier;
    }

    // Returns the base number of Fire Blades granted by the skill level before upgrades/evolution.
    private static int GetFireBladeCount(int level)
    {
        switch (level)
        {
            case 1: return 3;
            case 2: return 5;
            case 3: return 8;
            default: return 10;
        }
    }

    // Gives each Fire Spirit a readable fixed slot around the player; small counts use hand-tuned
    // formations, larger counts fall back to an evenly spaced ring.
    private static Vector2 GetFixedFireSpiritOffset(int index, int count)
    {
        const float distanceMultiplier = 1.45f;
        count = Mathf.Max(1, count);
        Vector2 offset;
        if (count == 1)
        {
            offset = new Vector2(0.92f, 0.58f);
            return offset * distanceMultiplier;
        }

        if (count == 2)
        {
            offset = index == 0 ? new Vector2(-0.86f, 0.58f) : new Vector2(0.86f, 0.58f);
            return offset * distanceMultiplier;
        }

        if (count == 3)
        {
            switch (index)
            {
                case 0: offset = new Vector2(-0.94f, 0.58f); break;
                case 1: offset = new Vector2(0.94f, 0.58f); break;
                default: offset = new Vector2(0f, 1.02f); break;
            }

            return offset * distanceMultiplier;
        }

        if (count == 4)
        {
            switch (index)
            {
                case 0: offset = new Vector2(-0.96f, 0.58f); break;
                case 1: offset = new Vector2(0.96f, 0.58f); break;
                case 2: offset = new Vector2(-0.42f, 1.06f); break;
                default: offset = new Vector2(0.42f, 1.06f); break;
            }

            return offset * distanceMultiplier;
        }

        float angle = 90f + index * (360f / count);
        return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.52f;
    }

    // Destroys every persistent helper owned by continuous skills.
    private void CleanupPersistentSkillObjects()
    {
        CleanupOrbitingKnives();
        CleanupDrones();
    }

    // Destroys all Fire Blade helper objects and clears their tracking list.
    private void CleanupOrbitingKnives()
    {
        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            if (orbitingObjects[i] != null)
            {
                Destroy(orbitingObjects[i]);
            }
        }

        orbitingObjects.Clear();
    }

    // Destroys all Fire Spirit helper objects and clears their tracking list.
    private void CleanupDrones()
    {
        for (int i = 0; i < drones.Count; i++)
        {
            if (drones[i] != null)
            {
                Destroy(drones[i]);
            }
        }

        drones.Clear();
    }

    // Counts down ultimate cooldown, listens for F, then damages every enemy on screen and starts
    // the next ultimate recharge when the skill is learned and ready.
    private void UpdateUltimateInput()
    {
        if (ultimateCooldown > 0f)
        {
            ultimateCooldown -= Time.deltaTime;
        }

        int level = GetSkillLevel(ZombieStormSkillType.UltimateStorm);
        if (level <= 0 || ultimateCooldown > 0f || !Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            Vector2 enemyPosition = enemy.transform.position;
            enemy.TakeDamage(RollDamage((42f + level * 18f) * (1f + Mod("ultimate_voltage") * 0.18f)), (enemyPosition - (Vector2)transform.position).normalized);
            game.SpawnAreaEffect(enemyPosition, 0.62f, 0f, 0.22f, 1f, new Color(0.45f, 0.85f, 1f, 0.78f), "ultimate_spark");
        }

        float stormRadius = (7.5f + Mod("ultimate_radius") * 0.85f) * game.AreaMultiplier;
        game.SpawnAreaEffect(transform.position, stormRadius, RollDamage((25f + level * 8f) * (1f + Mod("ultimate_voltage") * 0.18f)), 0.35f, 99f, new Color(0.7f, 0.92f, 1f, 0.42f), "ultimate_storm");
        game.PlaySfx("fire_tornado", 0.92f, 0.2f);
        game.ShakeCamera(0.34f, 0.48f);
        game.FlashScreen(0.9f);
        ultimateCooldown = Mathf.Max(12f, 42f - level * 4f - Mod("ultimate_recharge") * 3.5f);
    }

    // Counts living enemies overlapping a radius; used to avoid wasting Shield Burst on empty space.
    private int CountEnemiesNear(Vector2 origin, float radius)
    {
        int count = 0;
        float radiusSquared = radius * radius;
        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && ((Vector2)enemy.transform.position - origin).sqrMagnitude <= radiusSquared)
            {
                count++;
            }
        }

        return count;
    }

    // Advances queued delayed blasts, spawns their area damage on expiry, and plays matching
    // impact feedback for meteor-style blasts.
    private void UpdatePendingBlasts()
    {
        for (int i = pendingBlasts.Count - 1; i >= 0; i--)
        {
            ZombieStormPendingSkillBlast blast = pendingBlasts[i];
            blast.Delay -= Time.deltaTime;
            if (blast.Delay > 0f)
            {
                pendingBlasts[i] = blast;
                continue;
            }

            float effectDuration = blast.Key == "meteor_blast" ? 0.36f : 0.22f;
            game.SpawnAreaEffect(blast.Position, blast.Radius, blast.Damage, effectDuration, 99f, blast.Color, blast.Key);
            if (blast.Key == "meteor_blast")
            {
                game.SpawnHitSpark(blast.Position, new Color(1f, 0.9f, 0.25f, 0.9f), blast.Radius * 0.32f);
                game.PlaySfx("boom", 0.58f, 0.08f);
                game.ShakeCamera(0.12f, 0.14f);
                game.FlashScreen(blast.Color, 0.2f);
            }

            pendingBlasts.RemoveAt(i);
        }
    }

    // Applies global skill damage and critical chance to a base damage value.
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

// Stores one pending player-skill blast that will become an area effect after a short delay.
public struct ZombieStormPendingSkillBlast
{
    public Vector2 Position;
    public float Radius;
    public float Damage;
    public float Delay;
    public Color Color;
    public string Key;
}
