using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Skill identifiers used by the upgrade system, HUD, sprite lookup, and auto-cast manager.
public enum ZombieStormSkillType
{
    MagicBolt,
    OrbitingKnife,
    Regeneration,
    FireZone,
    SummonDrone,
    ShieldBurst,
    UltimateStorm
}

// Enemy identifiers used for spawn selection, stats, animation lookup, rewards, and boss UI.
public enum ZombieStormEnemyType
{
    Grunt,
    Fast,
    Tank,
    Exploder,
    Spitter,
    Goblin,
    SmallGoblin,
    Slasher,
    Gravedigger,
    Reaper,
    OrcThrower,
    Elite,
    Boss,
    PlagueBoss,
    BruteBoss,
    StormBoss,
    CrystalGolemBoss,
    MossGolemBoss,
    EmberTyrantBoss
}

// Passive upgrade identifiers that modify global player stats or skill formulas.
public enum ZombieStormPassiveType
{
    Damage,
    FireRate,
    Area,
    MoveSpeed,
    PickupRange,
    Crit,
    MaxHealth
}

// Oscillator wave shapes used by the runtime synth that creates lightweight sound effects.
public enum ZombieStormWave
{
    Sine,
    Square,
    Triangle,
    Saw,
    Noise
}

// Data object for one level-up card: unique key, display text, category label,
// accent color, and the callback that applies the upgrade.
public sealed class ZombieStormUpgradeOption
{
    public string Key;
    public string Title;
    public string Description;
    public string Category;
    public Color Accent;
    public Action Apply;

    // Creates a standard active-skill card for unlocking a skill or raising its level.
    public static ZombieStormUpgradeOption Skill(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "ACTIVE SKILL", Accent = accent, Apply = apply };
    }

    // Creates a passive-stat card such as damage, cooldown, movement, pickup range, or health.
    public static ZombieStormUpgradeOption Passive(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "PASSIVE STAT", Accent = accent, Apply = apply };
    }

    // Creates a card with a caller-supplied category, used for skill specializations.
    public static ZombieStormUpgradeOption Custom(string key, string title, string description, string category, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = category, Accent = accent, Apply = apply };
    }
}

// Runtime state for one floating damage number drawn in OnGUI and advanced each frame.
public struct ZombieStormDamagePopup
{
    public string Text;
    public Vector2 WorldPosition;
    public Vector2 Velocity;
    public Color Color;
    public float TimeLeft;
    public int Size;
}
