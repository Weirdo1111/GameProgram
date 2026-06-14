using System;
using UnityEngine;

// Upgrade choice generation, application, evolution, and display metadata.
public sealed partial class ZombieStormGameController
{
    // Reads a passive upgrade level; missing passives are treated as level zero.
    public int GetPassiveLevel(ZombieStormPassiveType passive)
    {
        int level;
        return passives.TryGetValue(passive, out level) ? level : 0;
    }

    // Builds the upgrade card choices shown when the player levels up.
    private void BuildUpgradeChoices()
    {
        choiceKeys.Clear();
        choiceFamilies.Clear();
        if (Skills != null && Skills.KnownSkillCount > 0)
        {
            AddUpgradeChoice(CreateKnownSkillOption());
        }

        int guard = 0;
        while (currentChoices.Count < 3 && guard < 80)
        {
            guard++;
            ZombieStormUpgradeOption option = CreateRandomUpgradeOption();
            AddUpgradeChoice(option);
        }

        ZombieStormPassiveType[] fallbackPassives =
        {
            ZombieStormPassiveType.Damage,
            ZombieStormPassiveType.FireRate,
            ZombieStormPassiveType.Area,
            ZombieStormPassiveType.MoveSpeed,
            ZombieStormPassiveType.MaxHealth,
            ZombieStormPassiveType.Crit
        };
        for (int i = 0; currentChoices.Count < 3 && i < fallbackPassives.Length; i++)
        {
            AddFallbackPassive(fallbackPassives[i]);
        }

        guard = 0;
        while (currentChoices.Count < 3 && guard < 80)
        {
            guard++;
            AddUpgradeChoice(CreateRandomUpgradeOption(), true);
        }

        for (int i = 0; currentChoices.Count < 3 && i < fallbackPassives.Length; i++)
        {
            AddFallbackPassive(fallbackPassives[i], true);
        }
    }

    // Adds one upgrade option to the current choice list.
    private void AddUpgradeChoice(ZombieStormUpgradeOption option)
    {
        AddUpgradeChoice(option, false);
    }

    // Adds one upgrade option, optionally relaxing same-family diversity.
    private void AddUpgradeChoice(ZombieStormUpgradeOption option, bool allowDuplicateFamily)
    {
        if (option == null || !choiceKeys.Add(option.Key))
        {
            return;
        }

        string family = GetUpgradeChoiceFamily(option.Key);
        if (!allowDuplicateFamily && !string.IsNullOrEmpty(family) && choiceFamilies.Contains(family))
        {
            choiceKeys.Remove(option.Key);
            return;
        }

        currentChoices.Add(option);
        if (!string.IsNullOrEmpty(family))
        {
            choiceFamilies.Add(family);
        }
    }

    // Groups upgrade cards by build family so one level-up choice feels varied.
    private static string GetUpgradeChoiceFamily(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (key.Contains("MagicBolt") || key.Contains("magic_"))
        {
            return "skill:MagicBolt";
        }

        if (key.Contains("OrbitingKnife") || key.Contains("knife_"))
        {
            return "skill:FireBlades";
        }

        if (key.Contains("FireZone"))
        {
            return "skill:FireZone";
        }

        if (key.Contains("SummonDrone") || key.Contains("drone_"))
        {
            return "skill:FireSpirit";
        }

        if (key.Contains("Regeneration") || key.Contains("regen_"))
        {
            return "skill:Regeneration";
        }

        if (key.Contains("UltimateStorm") || key.Contains("ultimate_"))
        {
            return "skill:UltimateStorm";
        }

        return key.StartsWith("passive_", StringComparison.Ordinal) ? key : string.Empty;
    }

    // Creates a random valid upgrade option from skills, specializations, or passives.
    private ZombieStormUpgradeOption CreateRandomUpgradeOption()
    {
        if (Skills == null)
        {
            return null;
        }

        if (Skills.KnownSkillCount > 0 && UnityEngine.Random.value < 0.62f)
        {
            return CreateKnownSkillOption();
        }

        if (UnityEngine.Random.value < 0.46f)
        {
            return CreateUnlockSkillOption();
        }

        ZombieStormPassiveType passive = (ZombieStormPassiveType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ZombieStormPassiveType)).Length);
        return CreatePassiveOption(passive);
    }

    // Creates an upgrade option for a skill the player already knows.
    private ZombieStormUpgradeOption CreateKnownSkillOption()
    {
        for (int guard = 0; guard < 32; guard++)
        {
            ZombieStormSkillType weaponType;
            if (!TryGetRandomKnownSkill(out weaponType))
            {
                return null;
            }

            if (UnityEngine.Random.value < 0.72f)
            {
                ZombieStormUpgradeOption specialization = CreateSkillSpecializationOption(weaponType);
                if (specialization != null)
                {
                    return specialization;
                }
            }

            ZombieStormUpgradeOption levelOption = CreateSkillLevelOption(weaponType);
            if (levelOption != null)
            {
                return levelOption;
            }

            ZombieStormUpgradeOption fallbackSpecialization = CreateSkillSpecializationOption(weaponType);
            if (fallbackSpecialization != null)
            {
                return fallbackSpecialization;
            }
        }

        return null;
    }

    // Selects a known skill that can still be leveled or specialized.
    private bool TryGetRandomKnownSkill(out ZombieStormSkillType weaponType)
    {
        Array values = Enum.GetValues(typeof(ZombieStormSkillType));
        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            ZombieStormSkillType candidate = (ZombieStormSkillType)values.GetValue(i);
            if (CanOfferSkill(candidate) && IsSkillKnown(candidate))
            {
                count++;
            }
        }

        if (count == 0)
        {
            weaponType = ZombieStormSkillType.MagicBolt;
            return false;
        }

        int pick = UnityEngine.Random.Range(0, count);
        for (int i = 0; i < values.Length; i++)
        {
            ZombieStormSkillType candidate = (ZombieStormSkillType)values.GetValue(i);
            if (!CanOfferSkill(candidate) || !IsSkillKnown(candidate))
            {
                continue;
            }

            if (pick == 0)
            {
                weaponType = candidate;
                return true;
            }

            pick--;
        }

        weaponType = ZombieStormSkillType.MagicBolt;
        return false;
    }

    // Creates an upgrade option that unlocks a new skill.
    private ZombieStormUpgradeOption CreateUnlockSkillOption()
    {
        Array values = Enum.GetValues(typeof(ZombieStormSkillType));
        for (int guard = 0; guard < 24; guard++)
        {
            ZombieStormSkillType weaponType = (ZombieStormSkillType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            if (CanOfferSkill(weaponType) && !IsSkillKnown(weaponType))
            {
                return ZombieStormUpgradeOption.Custom("unlock_" + weaponType, SkillName(weaponType) + " Lv.1", SkillSummary(weaponType), "NEW SKILL", SkillAccent(weaponType), delegate { Skills.LearnSkill(weaponType); });
            }
        }

        return null;
    }

    // Creates an upgrade option that raises a skill level.
    private ZombieStormUpgradeOption CreateSkillLevelOption(ZombieStormSkillType weaponType)
    {
        int level = Skills.GetSkillLevel(weaponType);
        if (!CanOfferSkill(weaponType) || !IsSkillKnown(weaponType) || level >= SkillMaxLevel(weaponType))
        {
            return null;
        }

        return ZombieStormUpgradeOption.Skill("level_" + weaponType, SkillName(weaponType) + " Lv." + (level + 1), SkillLevelSummary(weaponType, level + 1), SkillAccent(weaponType), delegate { Skills.LevelUpSkill(weaponType); });
    }

    // Creates an upgrade option that improves one skill specialization.
    private ZombieStormUpgradeOption CreateSkillSpecializationOption(ZombieStormSkillType weaponType)
    {
        int skillLevel = Skills.GetSkillLevel(weaponType);
        if (!CanOfferSkill(weaponType) || !IsSkillKnown(weaponType) || skillLevel >= SkillMaxLevel(weaponType))
        {
            return null;
        }

        string[] keys = SkillUpgradeKeys(weaponType);
        if (keys.Length == 0)
        {
            return null;
        }

        for (int guard = 0; guard < 18; guard++)
        {
            string key = keys[UnityEngine.Random.Range(0, keys.Length)];
            if (!CanOfferSkillSpecialization(key) || Skills.GetSkillUpgradeLevel(key) >= 3)
            {
                continue;
            }

            int nextLevel = skillLevel + 1;
            string category = SkillName(weaponType).ToUpperInvariant() + " BUILD";
            return ZombieStormUpgradeOption.Custom("special_" + weaponType + "_" + key, SkillUpgradeName(key) + " Lv." + nextLevel, SkillUpgradeSummary(key, nextLevel), category, SkillAccent(weaponType), delegate { Skills.LevelUpSkill(weaponType); Skills.AddSkillUpgrade(key); });
        }

        return null;
    }

    // Checks whether the player has already learned the selected skill.
    private bool IsSkillKnown(ZombieStormSkillType weaponType)
    {
        return Skills != null && Skills.GetSkillLevel(weaponType) > 0;
    }

    // Checks whether a skill is currently allowed to appear as an upgrade card.
    private static bool CanOfferSkill(ZombieStormSkillType weaponType)
    {
        return weaponType != ZombieStormSkillType.ShieldBurst;
    }

    // Returns the highest level allowed for a skill.
    private static int SkillMaxLevel(ZombieStormSkillType weaponType)
    {
        return weaponType == ZombieStormSkillType.Regeneration ? 3 : weaponType == ZombieStormSkillType.OrbitingKnife ? 4 : 5;
    }

    // Creates an upgrade option for a passive stat.
    private ZombieStormUpgradeOption CreatePassiveOption(ZombieStormPassiveType passive)
    {
        if (!CanOfferPassive(passive))
        {
            return null;
        }

        int level = GetPassiveLevel(passive);
        if (level >= 5)
        {
            return null;
        }

        return ZombieStormUpgradeOption.Passive("passive_" + passive, PassiveName(passive) + " Lv." + (level + 1), PassiveSummary(passive, level + 1), PassiveAccent(passive), delegate { AddPassive(passive); });
    }

    // Checks whether a passive stat should be in the current upgrade pool.
    private bool CanOfferPassive(ZombieStormPassiveType passive)
    {
        return (passive != ZombieStormPassiveType.Damage && passive != ZombieStormPassiveType.Crit) || upgradeChoicesTaken >= 2;
    }

    // Delays direct damage upgrade cards so early choices build tools before numbers.
    private bool CanOfferSkillSpecialization(string key)
    {
        return !IsDirectDamageSpecialization(key) || upgradeChoicesTaken >= 2;
    }

    // Checks whether a specialization is mostly a direct damage increase.
    private static bool IsDirectDamageSpecialization(string key)
    {
        return key == "magic_force"
            || key == "knife_edge"
            || key == "drone_focus"
            || key == "shield_force"
            || key == "ultimate_voltage";
    }

    // Adds a passive option when there are not enough other upgrade choices.
    private void AddFallbackPassive(ZombieStormPassiveType passive)
    {
        AddFallbackPassive(passive, false);
    }

    // Adds a passive option when there are not enough other upgrade choices.
    private void AddFallbackPassive(ZombieStormPassiveType passive, bool allowDuplicateFamily)
    {
        ZombieStormUpgradeOption option = CreatePassiveOption(passive);
        AddUpgradeChoice(option, allowDuplicateFamily);
    }

    // Raises a passive level and immediately applies its gameplay effect.
    private void AddPassive(ZombieStormPassiveType passive)
    {
        passives[passive] = Mathf.Min(5, GetPassiveLevel(passive) + 1);
        if (passive == ZombieStormPassiveType.MaxHealth && Player != null)
        {
            Player.IncreaseMaxHealth(16f);
        }

        CheckEvolutions();
    }

    // Applies the selected upgrade card and resumes the run.
    private void ApplyUpgrade(int index)
    {
        if (index < 0 || index >= currentChoices.Count)
        {
            return;
        }

        ZombieStormUpgradeOption option = currentChoices[index];
        option.Apply();
        upgradeChoicesTaken++;
        CheckEvolutions();
        currentChoices.Clear();
        leveling = false;
        flowState = ZombieStormFlowState.Running;
        Time.timeScale = 1f;
        PlaySfx("upgrade", 0.9f, 0.1f);
        PlayUpgradeBurst(option);
        ShowFeedback(option.Title + " acquired.", 2.2f);
    }

    // Plays visual and audio feedback after an upgrade is selected.
    private void PlayUpgradeBurst(ZombieStormUpgradeOption option)
    {
        if (Player == null)
        {
            return;
        }

        Color accent = option != null ? option.Accent : new Color(0.4f, 0.9f, 1f, 1f);
        Vector2 center = Player.transform.position;
        SpawnAreaEffect(center, 1.35f, 0f, 0.28f, 1f, WithAlpha(accent, 0.42f), "upgrade_pulse");
        SpawnAreaEffect(center, 2.15f, 0f, 0.42f, 1f, WithAlpha(new Color(1f, 0.88f, 0.28f), 0.32f), "upgrade_ring");
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * UnityEngine.Random.Range(0.75f, 1.9f);
            SpawnHitSpark(center + offset, WithAlpha(accent, 0.82f), UnityEngine.Random.Range(0.16f, 0.28f));
        }

        ShakeCamera(0.1f, 0.18f);
        FlashScreen(accent, 0.46f);
    }

    // Checks whether skill and passive combinations can evolve.
    private void CheckEvolutions()
    {
        if (Skills == null)
        {
            return;
        }

        TryEvolve(ZombieStormSkillType.MagicBolt, ZombieStormPassiveType.FireRate, "Arcane Barrage evolved.");
        TryEvolve(ZombieStormSkillType.OrbitingKnife, ZombieStormPassiveType.MaxHealth, "Fire Blade Halo evolved.");
        TryEvolve(ZombieStormSkillType.SummonDrone, ZombieStormPassiveType.Damage, "Fire Spirit evolved.");
    }

    // Evolves a skill when its required passive and level conditions are met.
    private void TryEvolve(ZombieStormSkillType weapon, ZombieStormPassiveType passive, string message)
    {
        if (Skills.GetSkillLevel(weapon) >= 5 && GetPassiveLevel(passive) > 0 && !Skills.IsEvolved(weapon))
        {
            Skills.Evolve(weapon);
            ShowFeedback(message, 3f);
        }
    }

    // Handles number-key shortcuts for picking upgrade cards.
    private void HandleUpgradeHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ApplyUpgrade(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ApplyUpgrade(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ApplyUpgrade(2);
        }
    }

    // Converts a skill enum value into the display name shown in UI.
    private static string SkillName(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Magic Bolt";
            case ZombieStormSkillType.OrbitingKnife: return "Fire Blades";
            case ZombieStormSkillType.Regeneration: return "Regeneration";
            case ZombieStormSkillType.FireZone: return "Fire Zone";
            case ZombieStormSkillType.SummonDrone: return "Fire Spirit";
            case ZombieStormSkillType.ShieldBurst: return "Shield Burst";
            case ZombieStormSkillType.UltimateStorm: return "Full-Screen Thunder";
            default: return weapon.ToString();
        }
    }

    // Returns a short player-facing description for a skill.
    private static string SkillSummary(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Adds auto magic shots with a bright launch spark.";
            case ZombieStormSkillType.OrbitingKnife: return "Adds orbiting fire blades that burn through nearby enemies.";
            case ZombieStormSkillType.Regeneration: return "Restores 1 health every 3 seconds.";
            case ZombieStormSkillType.FireZone: return "Every 4 Magic Bolt attacks throws a fire bomb.";
            case ZombieStormSkillType.SummonDrone: return "Summons 1 Fire Spirit that circles you and shoots fireballs.";
            case ZombieStormSkillType.ShieldBurst: return "Adds a close-range defensive shockwave trigger.";
            case ZombieStormSkillType.UltimateStorm: return "Adds one ultimate. Press F for a full-screen storm.";
            default: return "Adds another automatic skill.";
        }
    }

    // Returns the level-up description for a skill's next level.
    private static string SkillLevelSummary(ZombieStormSkillType weapon, int nextLevel)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return "Lv." + nextLevel + ": faster bolts, more damage, extra pierce.";
            case ZombieStormSkillType.OrbitingKnife: return FireBladesLevelSummary(nextLevel);
            case ZombieStormSkillType.Regeneration: return RegenerationLevelSummary(nextLevel);
            case ZombieStormSkillType.FireZone: return FireZoneLevelSummary(nextLevel);
            case ZombieStormSkillType.SummonDrone: return FireSpiritLevelSummary(nextLevel);
            case ZombieStormSkillType.ShieldBurst: return "Lv." + nextLevel + ": larger defensive ring and harder hit.";
            case ZombieStormSkillType.UltimateStorm: return "Lv." + nextLevel + ": stronger F ultimate, shorter cooldown.";
            default: return "Lv." + nextLevel + ": improves this automatic skill.";
        }
    }

    // Returns the custom level-up description for Regeneration.
    private static string RegenerationLevelSummary(int nextLevel)
    {
        switch (nextLevel)
        {
            case 2: return "Lv.2: max health +30 and heal 30 immediately.";
            case 3: return "Lv.3: restores 1 health every 2 seconds.";
            default: return "Lv." + nextLevel + ": improves survival recovery.";
        }
    }

    // Returns the custom level-up description for Fire Zone.
    private static string FireZoneLevelSummary(int nextLevel)
    {
        switch (nextLevel)
        {
            case 2: return "Lv.2: fire bombs leave burning flames on the ground.";
            case 3: return "Lv.3: fire bombs trigger every 3 attacks.";
            case 4: return "Lv.4: fire bombs trigger every 2 attacks.";
            case 5: return "Lv.5: burning flames deal 50% more damage and last 1 second longer.";
            default: return "Lv." + nextLevel + ": throws fire bombs after repeated attacks.";
        }
    }

    // Returns the custom level-up description for Fire Blades.
    private static string FireBladesLevelSummary(int nextLevel)
    {
        switch (nextLevel)
        {
            case 2: return "Lv.2: Fire Blades count becomes 5.";
            case 3: return "Lv.3: Fire Blades count becomes 8.";
            case 4: return "Lv.4: Fire Blades count becomes 10.";
            default: return "Lv." + nextLevel + ": adds more fire blades.";
        }
    }

    // Returns the custom level-up description for Fire Spirit.
    private static string FireSpiritLevelSummary(int nextLevel)
    {
        switch (nextLevel)
        {
            case 2: return "Lv.2: Fire Spirit count becomes 2 and attack speed increases.";
            case 3: return "Lv.3: Fire Spirit count becomes 3.";
            case 4: return "Lv.4: Fire Spirit count becomes 4.";
            default: return "Lv." + nextLevel + ": adds more Fire Spirits and increases power.";
        }
    }

    // Returns specialization keys that can appear for a selected skill.
    private static string[] SkillUpgradeKeys(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return new[] { "magic_force", "magic_split", "magic_pierce" };
            case ZombieStormSkillType.OrbitingKnife: return new[] { "knife_blades", "knife_reach", "knife_edge" };
            case ZombieStormSkillType.Regeneration: return new string[0];
            case ZombieStormSkillType.FireZone: return new string[0];
            case ZombieStormSkillType.SummonDrone: return new[] { "drone_swarm", "drone_focus", "drone_overclock" };
            case ZombieStormSkillType.ShieldBurst: return new[] { "shield_radius", "shield_force", "shield_recharge" };
            case ZombieStormSkillType.UltimateStorm: return new[] { "ultimate_voltage", "ultimate_radius", "ultimate_recharge" };
            default: return new[] { "magic_force" };
        }
    }

    // Converts a specialization key into a display name.
    private static string SkillUpgradeName(string key)
    {
        switch (key)
        {
            case "magic_force": return "Focused Mana";
            case "magic_split": return "Split Casting";
            case "magic_pierce": return "Piercing Glyph";
            case "knife_blades": return "Extra Fire Blades";
            case "knife_reach": return "Wide Flame Orbit";
            case "knife_edge": return "Searing Edge";
            case "drone_swarm": return "Spirit Swarm";
            case "drone_focus": return "Focused Flame";
            case "drone_overclock": return "Spirit Overclock";
            case "shield_radius": return "Wider Guard";
            case "shield_force": return "Repulsion Core";
            case "shield_recharge": return "Quick Recharge";
            case "ultimate_voltage": return "Storm Voltage";
            case "ultimate_radius": return "Eye of the Storm";
            case "ultimate_recharge": return "Storm Battery";
            default: return "Specialized Upgrade";
        }
    }

    // Returns the effect text for a specialization level.
    private static string SkillUpgradeSummary(string key, int nextLevel)
    {
        switch (key)
        {
            case "magic_force": return "Lv." + nextLevel + ": Magic Bolt deals more damage.";
            case "magic_split": return "Lv." + nextLevel + ": Magic Bolt can fire additional angled shots.";
            case "magic_pierce": return "Lv." + nextLevel + ": Magic Bolt pierces more enemies.";
            case "knife_blades": return "Lv." + nextLevel + ": Fire Blades adds another fire blade.";
            case "knife_reach": return "Lv." + nextLevel + ": Fire Blades orbit farther out.";
            case "knife_edge": return "Lv." + nextLevel + ": Fire Blades burn harder.";
            case "drone_swarm": return "Lv." + nextLevel + ": Fire Spirit adds another Fire Spirit.";
            case "drone_focus": return "Lv." + nextLevel + ": Fire Spirit fireballs hit harder.";
            case "drone_overclock": return "Lv." + nextLevel + ": Fire Spirit fireball cooldown is reduced.";
            case "shield_radius": return "Lv." + nextLevel + ": Shield Burst covers more space.";
            case "shield_force": return "Lv." + nextLevel + ": Shield Burst deals more damage.";
            case "shield_recharge": return "Lv." + nextLevel + ": Shield Burst cooldown is reduced.";
            case "ultimate_voltage": return "Lv." + nextLevel + ": Ultimate storm damage increases.";
            case "ultimate_radius": return "Lv." + nextLevel + ": Ultimate storm reaches farther.";
            case "ultimate_recharge": return "Lv." + nextLevel + ": Ultimate cooldown is reduced.";
            default: return "Lv." + nextLevel + ": improves this skill's behavior.";
        }
    }

    // Returns the UI accent color for a skill.
    private static Color SkillAccent(ZombieStormSkillType weapon)
    {
        switch (weapon)
        {
            case ZombieStormSkillType.MagicBolt: return new Color(0.45f, 0.95f, 1f, 1f);
            case ZombieStormSkillType.OrbitingKnife: return new Color(1f, 0.22f, 0.12f, 1f);
            case ZombieStormSkillType.Regeneration: return new Color(0.42f, 1f, 0.58f, 1f);
            case ZombieStormSkillType.FireZone: return new Color(1f, 0.28f, 0.05f, 1f);
            case ZombieStormSkillType.SummonDrone: return new Color(1f, 0.42f, 0.08f, 1f);
            case ZombieStormSkillType.ShieldBurst: return new Color(0.78f, 0.98f, 1f, 1f);
            case ZombieStormSkillType.UltimateStorm: return new Color(0.95f, 0.86f, 1f, 1f);
            default: return new Color(0.5f, 0.9f, 1f, 1f);
        }
    }

    // Converts a passive enum value into the UI display name.
    private static string PassiveName(ZombieStormPassiveType passive)
    {
        switch (passive)
        {
            case ZombieStormPassiveType.Damage: return "Attack Power";
            case ZombieStormPassiveType.FireRate: return "Attack Speed";
            case ZombieStormPassiveType.Area: return "Area";
            case ZombieStormPassiveType.MoveSpeed: return "Move Speed";
            case ZombieStormPassiveType.PickupRange: return "Pickup Range";
            case ZombieStormPassiveType.Crit: return "Critical Rate";
            case ZombieStormPassiveType.MaxHealth: return "Max Health";
            default: return passive.ToString();
        }
    }

    // Returns the effect text for a passive upgrade level.
    private static string PassiveSummary(ZombieStormPassiveType passive, int nextLevel)
    {
        switch (passive)
        {
            case ZombieStormPassiveType.Damage: return "Lv." + nextLevel + ": all skill damage increases by 18%.";
            case ZombieStormPassiveType.FireRate: return "Lv." + nextLevel + ": all skill cooldowns become shorter.";
            case ZombieStormPassiveType.Area: return "Lv." + nextLevel + ": explosions, fire, and rings grow wider.";
            case ZombieStormPassiveType.MoveSpeed: return "Lv." + nextLevel + ": player movement speed increases.";
            case ZombieStormPassiveType.PickupRange: return "Lv." + nextLevel + ": XP orbs pull from farther away.";
            case ZombieStormPassiveType.Crit: return "Lv." + nextLevel + ": higher chance to deal double damage.";
            case ZombieStormPassiveType.MaxHealth: return "Lv." + nextLevel + ": max HP increases and heals now.";
            default: return "Passive power increase.";
        }
    }

    // Returns the UI accent color for a passive upgrade.
    private static Color PassiveAccent(ZombieStormPassiveType passive)
    {
        switch (passive)
        {
            case ZombieStormPassiveType.Damage: return new Color(1f, 0.32f, 0.18f, 1f);
            case ZombieStormPassiveType.FireRate: return new Color(1f, 0.78f, 0.18f, 1f);
            case ZombieStormPassiveType.Area: return new Color(0.72f, 0.52f, 1f, 1f);
            case ZombieStormPassiveType.MoveSpeed: return new Color(0.42f, 1f, 0.58f, 1f);
            case ZombieStormPassiveType.PickupRange: return new Color(0.32f, 0.86f, 1f, 1f);
            case ZombieStormPassiveType.Crit: return new Color(1f, 0.42f, 0.72f, 1f);
            case ZombieStormPassiveType.MaxHealth: return new Color(0.38f, 1f, 0.38f, 1f);
            default: return new Color(0.78f, 0.86f, 0.92f, 1f);
        }
    }
}
