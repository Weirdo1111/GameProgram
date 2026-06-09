using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Stores skill levels and automatically casts each active combat skill.
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

    // Initializes the references and values this object needs at runtime.
    public void Initialize(ZombieStormGameController owner, ZombieStormPlayer survivor)
    {
        game = owner;
        player = survivor;
    }

    // Cleans up childless helper objects when the player or skill manager is destroyed.
    private void OnDestroy()
    {
        CleanupPersistentSkillObjects();
    }

    // Advances movement, combat, animation, timers, and state changes each frame.
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

    // Learns a new skill and creates persistent skill objects when needed.
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

    // Raises a skill level and rebuilds level-dependent skill objects.
    public void LevelUpSkill(ZombieStormSkillType weapon)
    {
        int current = GetSkillLevel(weapon);
        int maxLevel = weapon == ZombieStormSkillType.Regeneration ? 3 : weapon == ZombieStormSkillType.FireZone ? 4 : 5;
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

    // Returns the current level of a skill, or zero if it has not been learned.
    public int GetSkillLevel(ZombieStormSkillType weapon)
    {
        int level;
        return levels.TryGetValue(weapon, out level) ? level : 0;
    }

    // Returns the level of a skill specialization upgrade.
    public int GetSkillUpgradeLevel(string key)
    {
        int level;
        return skillUpgrades.TryGetValue(key, out level) ? level : 0;
    }

    // Raises a skill specialization level and refreshes related skill visuals.
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

    // Checks whether a skill has evolved.
    public bool IsEvolved(ZombieStormSkillType weapon)
    {
        return evolved.Contains(weapon);
    }

    // Marks a skill as evolved and rebuilds any related skill objects.
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

    // Builds the HUD text that summarizes the current skill loadout.
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

    // Converts a skill type into a short loadout label.
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

    // Counts down a skill cooldown and casts it when ready.
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

    // Reads a specialization level used by skill formulas.
    private int Mod(string key)
    {
        return GetSkillUpgradeLevel(key);
    }

    // Fires magic fireballs with level-based count, damage, and pierce.
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
        float damage = (10f + level * 3.4f) * (1f + Mod("magic_force") * 0.16f);
        int pierce = (level >= 3 ? 1 : 0) + Mod("magic_pierce");
        float powerTint = Mathf.Clamp01(Mod("magic_force") * 0.18f + (IsEvolved(ZombieStormSkillType.MagicBolt) ? 0.32f : 0f));
        Color fireballColor = Color.Lerp(Color.white, new Color(1f, 0.42f, 0.12f, 1f), powerTint);
        float fireballSize = 0.78f + level * 0.055f + Mod("magic_force") * 0.06f + Mod("magic_pierce") * 0.035f + (IsEvolved(ZombieStormSkillType.MagicBolt) ? 0.12f : 0f);
        game.SpawnAreaEffect(origin, 0.52f + level * 0.035f, 0f, 0.22f, 1f, new Color(1f, 0.5f, 0.12f, 0.72f), "foozle_explosion");
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

    // Restores one health on a fixed timer.
    private void CastRegeneration(int level)
    {
        player.Heal(1f);
        game.SpawnHitSpark(player.transform.position + Vector3.up * 0.35f, new Color(0.42f, 1f, 0.58f, 0.68f), 0.2f);
        cooldowns[ZombieStormSkillType.Regeneration] = level >= 3 ? 2f : 3f;
    }

    // Creates the Fire Zone bomb impact at a target point.
    public void SpawnFireZoneOnFireballHit(Vector2 position)
    {
        int level = GetSkillLevel(ZombieStormSkillType.FireZone);
        if (level <= 0)
        {
            return;
        }

        SpawnFireBombImpact(position, level);
    }

    // Counts Magic Bolt attacks and throws a Fire Zone bomb when the level threshold is met.
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
        float impactRadius = (1.18f + level * 0.12f) * game.AreaMultiplier;
        float impactDamage = RollDamage(10f + level * 3.2f);
        float burnRadius = (1.05f + level * 0.12f) * game.AreaMultiplier * 0.5f;
        float burnDamage = RollDamage(4.8f + level * 1.4f);
        game.SpawnFireBombProjectile(origin, targetPosition + scatter, impactDamage, impactRadius, level >= 2, burnDamage, burnRadius, 5f, 0.42f);
    }

    // Applies the Fire Zone bomb impact immediately for compatibility with older projectile hooks.
    private void SpawnFireBombImpact(Vector2 position, int level)
    {
        float impactRadius = (1.18f + level * 0.12f) * game.AreaMultiplier;
        game.SpawnAreaEffect(position, impactRadius, RollDamage(10f + level * 3.2f), 0.18f, 99f, new Color(1f, 0.52f, 0.08f, 0.78f), "foozle_explosion");
        if (level >= 2)
        {
            game.SpawnAreaEffect(position, (1.05f + level * 0.12f) * game.AreaMultiplier * 0.5f, RollDamage(4.8f + level * 1.4f), 5f, 0.42f, new Color(1f, 0.42f, 0.08f, 0.72f), game.GetRandomGroundFireEffectKey());
        }
    }

    // Returns how many attacks Fire Zone needs before throwing a bomb.
    private static int GetFireBombAttackThreshold(int level)
    {
        if (level >= 4)
        {
            return 2;
        }

        return level >= 3 ? 3 : 4;
    }

    // Casts a shield burst that damages and knocks back nearby enemies.
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

    // Updates orbiting blade position, rotation, collision damage, and visual effects.
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
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead && Vector2.Distance(enemy.transform.position, transform.position) <= radius + enemy.Radius)
            {
                enemy.TakeDamage(RollDamage((6f + level * 1.8f) * (1f + Mod("knife_edge") * 0.18f)), ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized);
                game.SpawnHitSpark(enemy.transform.position, new Color(1f, 0.32f, 0.16f, 0.86f), 0.28f);
                game.SpawnAreaEffect(enemy.transform.position, 0.34f, 0f, 0.12f, 1f, new Color(1f, 0.16f, 0.06f, 0.42f), "hit_spark");
            }
        }

        cooldowns[ZombieStormSkillType.OrbitingKnife] = 0.24f * game.CooldownMultiplier;
    }

    // Rebuilds the orbiting blade count and glow objects from current upgrades.
    private void RebuildOrbitingKnives()
    {
        CleanupOrbitingKnives();

        int level = GetSkillLevel(ZombieStormSkillType.OrbitingKnife);
        int count = 2 + Mathf.FloorToInt(level * 0.75f) + Mod("knife_blades") + (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 3 : 0);
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

    // Rebuilds the Fire Spirit count from skill level and upgrades.
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
            drone.transform.localScale = Vector3.one * 0.56f;
            SpriteRenderer spriteRenderer = drone.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = game.GetSkillSprite(ZombieStormSkillType.SummonDrone);
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 34;
            drones.Add(drone);
        }
    }

    // Updates Fire Spirit orbit positions and auto-fires at nearby enemies.
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
            float angle = Time.time * 92f + i * (360f / Mathf.Max(1, drones.Count));
            Vector2 desired = (Vector2)transform.position + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.15f;
            drones[i].transform.position = Vector2.Lerp(drones[i].transform.position, desired, 9f * Time.deltaTime);
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

    // Destroys all persistent helper objects owned by continuous skills.
    private void CleanupPersistentSkillObjects()
    {
        CleanupOrbitingKnives();
        CleanupDrones();
    }

    // Destroys orbiting blade objects and their halo ring.
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

    // Destroys Fire Spirit helper objects.
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

    // Checks the ultimate key and casts the storm when energy is full.
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
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            enemy.TakeDamage(RollDamage((42f + level * 18f) * (1f + Mod("ultimate_voltage") * 0.18f)), ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized);
            game.SpawnAreaEffect(enemy.transform.position, 0.62f, 0f, 0.22f, 1f, new Color(0.45f, 0.85f, 1f, 0.78f), "ultimate_spark");
        }

        float stormRadius = (7.5f + Mod("ultimate_radius") * 0.85f) * game.AreaMultiplier;
        game.SpawnAreaEffect(transform.position, stormRadius, RollDamage((25f + level * 8f) * (1f + Mod("ultimate_voltage") * 0.18f)), 0.35f, 99f, new Color(0.7f, 0.92f, 1f, 0.42f), "ultimate_storm");
        game.PlaySfx("ultimate", 0.92f, 0.2f);
        game.ShakeCamera(0.34f, 0.48f);
        game.FlashScreen(0.9f);
        ultimateCooldown = Mathf.Max(12f, 42f - level * 4f - Mod("ultimate_recharge") * 3.5f);
    }

    // Finds the nearest enemy that this chain effect has not already hit.
    private ZombieStormEnemy FindNearestUnhitEnemy(Vector2 origin, float maxDistance, HashSet<ZombieStormEnemy> hit)
    {
        ZombieStormEnemy best = null;
        float bestDistance = maxDistance * maxDistance;
        IReadOnlyList<ZombieStormEnemy> activeEnemies = game.Enemies;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            ZombieStormEnemy enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead || hit.Contains(enemy))
            {
                continue;
            }

            float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }

        return best;
    }

    // Counts enemies inside a radius to evaluate skill target value.
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

    // Updates delayed blasts and creates area damage when their timers finish.
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

    // Calculates final damage from base damage and global damage multipliers.
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

// Stores delayed blast data such as warning position, timer, and damage settings.
public struct ZombieStormPendingSkillBlast
{
    public Vector2 Position;
    public float Radius;
    public float Damage;
    public float Delay;
    public Color Color;
    public string Key;
}
