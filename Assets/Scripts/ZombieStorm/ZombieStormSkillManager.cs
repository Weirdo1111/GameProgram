using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

    public int KnownSkillCount
    {
        get { return levels.Count; }
    }

    public void Initialize(ZombieStormGameController owner, ZombieStormPlayer survivor)
    {
        game = owner;
        player = survivor;
    }

    private void Update()
    {
        if (game == null || player == null)
        {
            return;
        }

        TickSkill(ZombieStormSkillType.MagicBolt, CastMagicBolt);
        TickSkill(ZombieStormSkillType.MeteorStorm, CastMeteorStorm);
        TickSkill(ZombieStormSkillType.FireZone, CastFireZone);
        TickSkill(ZombieStormSkillType.ChainLightning, CastChainLightning);
        TickSkill(ZombieStormSkillType.ShieldBurst, CastShieldBurst);
        UpdatePendingBlasts();
        UpdateOrbitingKnives();
        UpdateSummonDrones();
        UpdateUltimateInput();
    }

    public void LearnSkill(ZombieStormSkillType weapon)
    {
        if (GetSkillLevel(weapon) <= 0)
        {
            levels[weapon] = 1;
            cooldowns[weapon] = 0.05f;
            if (weapon == ZombieStormSkillType.OrbitingKnife)
            {
                RebuildOrbitingKnives();
            }
            else if (weapon == ZombieStormSkillType.SummonDrone)
            {
                RebuildDrones();
            }
        }
        else
        {
            LevelUpSkill(weapon);
        }
    }

    public void LevelUpSkill(ZombieStormSkillType weapon)
    {
        int next = Mathf.Min(5, GetSkillLevel(weapon) + 1);
        levels[weapon] = next;
        if (weapon == ZombieStormSkillType.OrbitingKnife)
        {
            RebuildOrbitingKnives();
        }
        else if (weapon == ZombieStormSkillType.SummonDrone)
        {
            RebuildDrones();
        }
    }

    public int GetSkillLevel(ZombieStormSkillType weapon)
    {
        int level;
        return levels.TryGetValue(weapon, out level) ? level : 0;
    }

    public int GetSkillUpgradeLevel(string key)
    {
        int level;
        return skillUpgrades.TryGetValue(key, out level) ? level : 0;
    }

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

    public bool IsEvolved(ZombieStormSkillType weapon)
    {
        return evolved.Contains(weapon);
    }

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

    private static string SkillLabel(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Magic Bolt";
            case ZombieStormSkillType.OrbitingKnife: return "Orbit Knives";
            case ZombieStormSkillType.MeteorStorm: return "Meteor";
            case ZombieStormSkillType.FireZone: return "Fire Zone";
            case ZombieStormSkillType.SummonDrone: return "Drone";
            case ZombieStormSkillType.ChainLightning: return "Lightning";
            case ZombieStormSkillType.ShieldBurst: return "Shield";
            case ZombieStormSkillType.UltimateStorm: return "Ultimate";
            default: return weapon.ToString();
        }
    }

    private delegate void SkillAction(int level);

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

    private int Mod(string key)
    {
        return GetSkillUpgradeLevel(key);
    }

    private void CastMagicBolt(int level)
    {
        ZombieStormEnemy target = game.FindNearestEnemy(transform.position, IsEvolved(ZombieStormSkillType.MagicBolt) ? 18f : 14f);
        if (target == null)
        {
            cooldowns[ZombieStormSkillType.MagicBolt] = 0.15f;
            return;
        }

        Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        Vector2 origin = (Vector2)transform.position + direction * 0.42f;
        int shots = (IsEvolved(ZombieStormSkillType.MagicBolt) ? 3 : level >= 4 ? 2 : 1) + Mod("magic_split");
        float damage = (10f + level * 3.4f) * (1f + Mod("magic_force") * 0.16f);
        int pierce = (level >= 3 ? 1 : 0) + Mod("magic_pierce");
        game.SpawnHitSpark(origin, new Color(0.48f, 0.95f, 1f, 0.88f), 0.24f + level * 0.025f);
        for (int i = 0; i < shots; i++)
        {
            float spreadStep = shots <= 3 ? 9f : 7f;
            float angle = shots == 1 ? 0f : -(shots - 1) * spreadStep * 0.5f + i * spreadStep;
            game.SpawnPlayerProjectile(origin, ZombieStormGameController.Rotate(direction, angle), RollDamage(damage), 13.5f, 1.4f, pierce, new Color(0.56f, 0.92f, 1f), 0.34f + level * 0.015f);
        }

        float baseCooldown = IsEvolved(ZombieStormSkillType.MagicBolt) ? 0.18f : 0.62f - level * 0.055f;
        cooldowns[ZombieStormSkillType.MagicBolt] = baseCooldown * game.CooldownMultiplier;
    }

    private void CastMeteorStorm(int level)
    {
        int impacts = 1 + Mathf.FloorToInt(level * 0.55f) + Mod("meteor_impacts") + (IsEvolved(ZombieStormSkillType.MeteorStorm) ? 2 : 0);
        for (int i = 0; i < impacts; i++)
        {
            ZombieStormEnemy target = game.FindRandomEnemy();
            Vector2 position = target != null ? (Vector2)target.transform.position : (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 5.5f;
            float radius = (0.95f + level * 0.18f + Mod("meteor_blast") * 0.2f) * game.AreaMultiplier;
            game.SpawnAreaEffect(position, radius * 1.35f, 0f, 0.48f, 1f, new Color(1f, 0.75f, 0.18f, 0.3f), "meteor_warning");
            game.SpawnAreaEffect(position, radius * 0.36f, 0f, 0.48f, 1f, new Color(1f, 0.92f, 0.3f, 0.52f), "meteor_warning");
            pendingBlasts.Add(new ZombieStormPendingSkillBlast
            {
                Position = position,
                Radius = radius,
                Damage = RollDamage((20f + level * 5.5f) * (1f + Mod("meteor_heat") * 0.16f)),
                Delay = 0.42f,
                Color = new Color(1f, 0.28f, 0.05f, 0.72f),
                Key = "meteor_blast"
            });
        }

        cooldowns[ZombieStormSkillType.MeteorStorm] = (4.2f - level * 0.24f) * game.CooldownMultiplier;
    }

    private void CastFireZone(int level)
    {
        int pools = (IsEvolved(ZombieStormSkillType.FireZone) ? 3 : 1 + level / 4) + Mod("fire_spread");
        for (int i = 0; i < pools; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2.4f, 7.0f);
            Vector2 position = (Vector2)transform.position + offset;
            float radius = (1.25f + level * 0.22f) * game.AreaMultiplier * (IsEvolved(ZombieStormSkillType.FireZone) ? 1.22f : 1f);
            float duration = 2.5f + level * 0.36f + Mod("fire_linger") * 0.55f;
            game.SpawnAreaEffect(position, radius, RollDamage((4.2f + level * 1.3f) * (1f + Mod("fire_heat") * 0.18f)), duration, 0.28f, new Color(1f, 0.25f, 0.05f, 0.62f), "fire_pool");
            for (int spark = 0; spark < 4; spark++)
            {
                game.SpawnHitSpark(position + UnityEngine.Random.insideUnitCircle * radius * 0.65f, new Color(1f, 0.68f, 0.12f, 0.78f), 0.16f);
            }
        }

        cooldowns[ZombieStormSkillType.FireZone] = (4.1f - level * 0.25f) * game.CooldownMultiplier;
    }

    private void CastChainLightning(int level)
    {
        ZombieStormEnemy current = game.FindRandomEnemy();
        if (current == null)
        {
            cooldowns[ZombieStormSkillType.ChainLightning] = 0.25f;
            return;
        }

        int jumps = 2 + level + Mod("lightning_jumps") + (IsEvolved(ZombieStormSkillType.ChainLightning) ? 4 : 0);
        float chainReach = 4.2f + level * 0.35f + Mod("lightning_reach") * 0.8f;
        float lightningDamage = (13f + level * 4f) * (1f + Mod("lightning_voltage") * 0.18f);
        game.PlaySfx("lightning", 0.48f, 0.12f);
        HashSet<ZombieStormEnemy> hit = new HashSet<ZombieStormEnemy>();
        Vector2 previous = transform.position;
        for (int i = 0; i < jumps && current != null; i++)
        {
            Vector2 currentPosition = current.transform.position;
            hit.Add(current);
            SpawnLightningSegment(previous, currentPosition, level);
            current.TakeDamage(RollDamage(lightningDamage), (currentPosition - (Vector2)transform.position).normalized);
            game.SpawnAreaEffect(currentPosition, (0.6f + Mod("lightning_reach") * 0.08f) * game.AreaMultiplier, 0f, 0.18f, 1f, new Color(0.25f, 0.85f, 1f, 0.86f), "lightning_flash");
            previous = currentPosition;
            current = FindNearestUnhitEnemy(currentPosition, chainReach, hit);
        }

        cooldowns[ZombieStormSkillType.ChainLightning] = Mathf.Max(0.82f, 3.5f - level * 0.22f - Mod("lightning_tempo") * 0.24f) * game.CooldownMultiplier;
    }

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

    private void SpawnLightningSegment(Vector2 from, Vector2 to, int level)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Clamp(Mathf.CeilToInt(distance / 0.42f), 2, 12);
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            point += UnityEngine.Random.insideUnitCircle * 0.06f;
            float radius = (0.12f + level * 0.012f + Mod("lightning_reach") * 0.01f + Mod("lightning_voltage") * 0.006f) * game.AreaMultiplier;
            game.SpawnAreaEffect(point, radius, 0f, 0.1f, 1f, new Color(0.35f, 0.9f, 1f, 0.72f), "lightning_flash");
        }
    }

    private void UpdateOrbitingKnives()
    {
        int level = GetSkillLevel(ZombieStormSkillType.OrbitingKnife);
        if (level <= 0)
        {
            return;
        }

        float radius = (1.45f + level * 0.18f + Mod("knife_reach") * 0.22f) * game.AreaMultiplier * (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 1.32f : 1f);
        float speed = 120f + level * 28f;
        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            float angle = Time.time * speed + i * (360f / Mathf.Max(1, orbitingObjects.Count));
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            orbitingObjects[i].transform.position = (Vector2)transform.position + offset;
            orbitingObjects[i].transform.Rotate(0f, 0f, 420f * Time.deltaTime);
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
                game.SpawnHitSpark(enemy.transform.position, new Color(0.9f, 0.96f, 1f, 0.72f), 0.18f);
            }
        }

        cooldowns[ZombieStormSkillType.OrbitingKnife] = 0.24f * game.CooldownMultiplier;
    }

    private void RebuildOrbitingKnives()
    {
        for (int i = 0; i < orbitingObjects.Count; i++)
        {
            if (orbitingObjects[i] != null)
            {
                Destroy(orbitingObjects[i]);
            }
        }

        orbitingObjects.Clear();
        int level = GetSkillLevel(ZombieStormSkillType.OrbitingKnife);
        int count = 2 + Mathf.FloorToInt(level * 0.75f) + Mod("knife_blades") + (IsEvolved(ZombieStormSkillType.OrbitingKnife) ? 3 : 0);
        for (int i = 0; i < count; i++)
        {
            GameObject blade = new GameObject("Orbiting Skill Blade");
            blade.transform.localScale = Vector3.one * 0.5f;
            SpriteRenderer spriteRenderer = blade.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = game.GetSkillSprite(ZombieStormSkillType.OrbitingKnife);
            spriteRenderer.color = new Color(0.9f, 0.98f, 1f, 1f);
            spriteRenderer.sortingOrder = 35;
            orbitingObjects.Add(blade);
        }
    }

    private void RebuildDrones()
    {
        for (int i = 0; i < drones.Count; i++)
        {
            if (drones[i] != null)
            {
                Destroy(drones[i]);
            }
        }

        drones.Clear();
        int level = GetSkillLevel(ZombieStormSkillType.SummonDrone);
        int count = 1 + level / 2 + Mod("drone_swarm") + (IsEvolved(ZombieStormSkillType.SummonDrone) ? 2 : 0);
        for (int i = 0; i < count; i++)
        {
            GameObject drone = new GameObject("Summoned Drone");
            drone.transform.localScale = Vector3.one * 0.56f;
            SpriteRenderer spriteRenderer = drone.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = game.GetSkillSprite(ZombieStormSkillType.SummonDrone);
            spriteRenderer.color = new Color(0.5f, 0.92f, 1f);
            spriteRenderer.sortingOrder = 34;
            drones.Add(drone);
        }
    }

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
            game.SpawnHitSpark(muzzle, new Color(0.35f, 0.9f, 1f, 0.78f), 0.16f);
            game.SpawnPlayerProjectile(muzzle, direction, RollDamage((7f + level * 2.4f) * (1f + Mod("drone_focus") * 0.18f)), 12f, 1.1f, 0, new Color(0.4f, 0.92f, 1f), 0.26f);
        }

        cooldowns[ZombieStormSkillType.SummonDrone] = Mathf.Max(0.22f, 0.92f - level * 0.06f - Mod("drone_overclock") * 0.08f) * game.CooldownMultiplier;
    }

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

            game.SpawnAreaEffect(blast.Position, blast.Radius, blast.Damage, 0.22f, 99f, blast.Color, blast.Key);
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

public struct ZombieStormPendingSkillBlast
{
    public Vector2 Position;
    public float Radius;
    public float Damage;
    public float Delay;
    public Color Color;
    public string Key;
}
