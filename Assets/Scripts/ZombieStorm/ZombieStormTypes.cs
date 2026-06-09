using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Lists the active skills the player can unlock and upgrade.
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

// Lists normal enemies, elites, and boss enemy types.
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

// Lists passive upgrades such as health, speed, pickup range, and damage.
public enum ZombieStormPassiveType
{
    Damage,
    FireRate,
    Area,
    MoveSpeed,
    PickupRange,
    Crit,
    MaxHealth,
    CoinGain
}

// Lists oscillator wave shapes used when generating simple sound effects.
public enum ZombieStormWave
{
    Sine,
    Square,
    Triangle,
    Saw,
    Noise
}

// Stores one upgrade card, including title, description, color, and action.
public sealed class ZombieStormUpgradeOption
{
    public string Key;
    public string Title;
    public string Description;
    public string Category;
    public Color Accent;
    public Action Apply;

    // Creates an upgrade option for unlocking or leveling an active skill.
    public static ZombieStormUpgradeOption Skill(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "ACTIVE SKILL", Accent = accent, Apply = apply };
    }

    // Creates an upgrade option for a passive stat.
    public static ZombieStormUpgradeOption Passive(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "PASSIVE STAT", Accent = accent, Apply = apply };
    }

    // Creates a custom upgrade option with an explicit category label.
    public static ZombieStormUpgradeOption Custom(string key, string title, string description, string category, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = category, Accent = accent, Apply = apply };
    }
}

// Stores floating damage text data, including position, color, and remaining lifetime.
public struct ZombieStormDamagePopup
{
    public string Text;
    public Vector2 WorldPosition;
    public Vector2 Velocity;
    public Color Color;
    public float TimeLeft;
    public int Size;
}
