using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum ZombieStormSkillType
{
    MagicBolt,
    OrbitingKnife,
    MeteorStorm,
    FireZone,
    SummonDrone,
    ChainLightning,
    ShieldBurst,
    UltimateStorm
}

public enum ZombieStormEnemyType
{
    Grunt,
    Fast,
    Tank,
    Exploder,
    Spitter,
    Slasher,
    Gravedigger,
    Reaper,
    Elite,
    Boss,
    PlagueBoss,
    BruteBoss,
    StormBoss,
    CrystalGolemBoss,
    MossGolemBoss,
    EmberTyrantBoss
}

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

public enum ZombieStormWave
{
    Sine,
    Square,
    Triangle,
    Saw,
    Noise
}

public sealed class ZombieStormUpgradeOption
{
    public string Key;
    public string Title;
    public string Description;
    public string Category;
    public Color Accent;
    public Action Apply;

    public static ZombieStormUpgradeOption Skill(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "ACTIVE SKILL", Accent = accent, Apply = apply };
    }

    public static ZombieStormUpgradeOption Passive(string key, string title, string description, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = "PASSIVE STAT", Accent = accent, Apply = apply };
    }

    public static ZombieStormUpgradeOption Custom(string key, string title, string description, string category, Color accent, Action apply)
    {
        return new ZombieStormUpgradeOption { Key = key, Title = title, Description = description, Category = category, Accent = accent, Apply = apply };
    }
}

public struct ZombieStormDamagePopup
{
    public string Text;
    public Vector2 WorldPosition;
    public Vector2 Velocity;
    public Color Color;
    public float TimeLeft;
    public int Size;
}
