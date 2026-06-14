using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal sealed class ZombieStormBuildContent : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string ContentFolderName = "ZombieStormContent";

    private static readonly string[] Directories =
    {
        Path.Combine("ZombieStormArt", "Player", "chibi_pyromancer_idle"),
        Path.Combine("ZombieStormArt", "Player", "chibi_pyromancer_walk_right"),
        Path.Combine("ZombieStormArt", "Player", "hurt"),
        Path.Combine("ZombieStormArt", "Enemies", "chibi_zombie"),
        Path.Combine("ZombieStormArt", "Story"),
        Path.Combine("ZombieStormArt", "Effects", "IceBossOrb"),
        Path.Combine("ZombieStormArt", "Effects", "DarkVFX1", "Frames"),
        Path.Combine("ZombieStormArt", "Effects", "DarkVFX2", "Frames"),
        Path.Combine("ZombieStormArt", "Effects", "EmberBossMeteorSelected"),
        Path.Combine("ZombieStormArt", "Effects", "CraftpixPoisonExplosion10"),
        Path.Combine("ZombieStormArt", "Effects", "FoozlePixelMagic", "Fire_Ball"),
        Path.Combine("ZombieStormArt", "Effects", "FoozlePixelMagic", "Explosion"),
        Path.Combine("ZombieStormArt", "Effects", "FoozlePixelMagic", "Wind"),
        Path.Combine("ZombieStormArt", "Effects", "FoozlePixelMagic", "Tornado"),
        Path.Combine("ZombieStormArt", "Effects", "FireBomb"),
        Path.Combine("ExternalArt", "MikodrakSpellEffects", "fx1_blue_topEffect"),
        Path.Combine("ExternalArt", "MikodrakSpellEffects", "fx3_fireBall"),
        Path.Combine("ExternalArt", "MikodrakSpellEffects", "fx7_energyBall"),
        Path.Combine("ExternalArt", "MikodrakSpellEffects", "fx8_lighteningBall"),
        Path.Combine("ExternalArt", "MikodrakSpellEffects", "fx10_blackExplosion")
    };

    private static readonly string[] RequiredFiles =
    {
        Path.Combine("ZombieStormArt", "Maps", "graveyard_arena.png"),
        Path.Combine("ZombieStormArt", "Menu", "main_menu_cover.png"),
        Path.Combine("ZombieStormArt", "Player", "FireSpirit.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_chibi_fire_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_magic_bolt_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_fire_blades_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_fire_zone_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_damage_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_cooldown_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_xp_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_regeneration_template.png"),
        Path.Combine("ZombieStormArt", "UI", "skill_card_storm_template.png"),
        Path.Combine("ZombieStormArt", "UI", "player_status_card_cropped.png"),
        Path.Combine("ZombieStormArt", "UI", "health_potion.png"),
        Path.Combine("ZombieStormArt", "Effects", "GroundFire", "TekilaFire01", "Fire-01_320x160_Sheet.png")
    };

    private static readonly string[] OptionalFiles =
    {
        Path.Combine("ZombieStormArt", "UI", "result_failed.png"),
        Path.Combine("ZombieStormArt", "UI", "result_victory.png")
    };

    private static readonly string[] EnemyRoots =
    {
        "craftpix_goblin",
        "craftpix_villager",
        "craftpix_gravedigger",
        "craftpix_reaper",
        "craftpix_orc",
        "craftpix_crystal_golem",
        "craftpix_moss_golem",
        "craftpix_ember_golem"
    };

    private static readonly Dictionary<string, string[]> EnemyAnimations =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "craftpix_goblin", new[] { "Run", "Hurt", "Death" } },
            { "craftpix_villager", new[] { "Run", "Slash", "Hurt", "Death" } },
            { "craftpix_gravedigger", new[] { "Run", "Slash", "Hurt", "Death" } },
            { "craftpix_reaper", new[] { "Run", "Slash", "Hurt", "Death" } },
            { "craftpix_orc", new[] { "Run", "Throw", "Hurt", "Death" } },
            { "craftpix_crystal_golem", new[] { "Run", "Slash", "Throw", "Hurt", "Death" } },
            { "craftpix_moss_golem", new[] { "Run", "Slash", "Throw", "Hurt", "Death" } },
            { "craftpix_ember_golem", new[] { "Run", "Slash", "Throw", "Hurt", "Death" } }
        };

    private static readonly int[] GroundTileIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

    private static readonly int[] DebrisTileIds =
    {
        131, 132, 134, 156, 157, 158, 181, 182, 183, 184,
        185, 186, 197, 198, 206, 207, 208, 209, 210, 211,
        212, 213, 214, 215, 235, 236, 237, 238, 239, 240
    };

    public int callbackOrder
    {
        get { return 1000; }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureSupportedTarget(report.summary.platform);

        ForEachRequiredDirectory(delegate(string relativePath)
        {
            string source = SourcePath(relativePath);
            if (!Directory.Exists(source))
            {
                throw new BuildFailedException("Missing Zombie Storm runtime art directory: " + source);
            }

            if (Directory.GetFiles(source, "*.png", SearchOption.AllDirectories).Length == 0)
            {
                throw new BuildFailedException("Zombie Storm runtime art directory has no PNG files: " + source);
            }
        });

        ForEachRequiredFile(delegate(string relativePath)
        {
            string source = SourcePath(relativePath);
            if (!File.Exists(source))
            {
                throw new BuildFailedException("Missing Zombie Storm runtime art file: " + source);
            }
        });
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        string destinationRoot = GetDestinationRoot(report);
        if (Directory.Exists(destinationRoot))
        {
            Directory.Delete(destinationRoot, true);
        }

        int copiedFiles = 0;
        long copiedBytes = 0;

        ForEachRequiredDirectory(delegate(string relativePath)
        {
            CopyPngDirectory(relativePath, destinationRoot, ref copiedFiles, ref copiedBytes);
        });

        ForEachRequiredFile(delegate(string relativePath)
        {
            CopyFile(relativePath, destinationRoot, true, ref copiedFiles, ref copiedBytes);
        });

        for (int i = 0; i < OptionalFiles.Length; i++)
        {
            CopyFile(OptionalFiles[i], destinationRoot, false, ref copiedFiles, ref copiedBytes);
        }

        Debug.Log(
            "Zombie Storm build content: copied " + copiedFiles +
            " PNG files (" + (copiedBytes / (1024f * 1024f)).ToString("0.0") +
            " MB) to " + destinationRoot);
    }

    private static void ForEachRequiredDirectory(Action<string> action)
    {
        for (int i = 0; i < Directories.Length; i++)
        {
            action(Directories[i]);
        }

        for (int i = 0; i < EnemyRoots.Length; i++)
        {
            string enemyRoot = EnemyRoots[i];
            string[] animations = EnemyAnimations[enemyRoot];
            for (int j = 0; j < animations.Length; j++)
            {
                action(Path.Combine("ZombieStormArt", "Enemies", enemyRoot, animations[j]));
            }
        }
    }

    private static void ForEachRequiredFile(Action<string> action)
    {
        for (int i = 0; i < RequiredFiles.Length; i++)
        {
            action(RequiredFiles[i]);
        }

        string tileRoot = Path.Combine("ExternalArt", "KenneyTopdownShooter", "PNG", "Tiles");
        for (int i = 0; i < GroundTileIds.Length; i++)
        {
            action(Path.Combine(tileRoot, "tile_" + GroundTileIds[i].ToString("00") + ".png"));
        }

        for (int i = 0; i < DebrisTileIds.Length; i++)
        {
            action(Path.Combine(tileRoot, "tile_" + DebrisTileIds[i] + ".png"));
        }

        string zombieRoot = Path.Combine("ExternalArt", "KenneyTopdownShooter", "PNG", "Zombie 1");
        action(Path.Combine(zombieRoot, "zoimbie1_stand.png"));
        action(Path.Combine(zombieRoot, "zoimbie1_hold.png"));
        action(Path.Combine(zombieRoot, "zoimbie1_machine.png"));
        action(Path.Combine(zombieRoot, "zoimbie1_gun.png"));
        action(Path.Combine(zombieRoot, "zoimbie1_reload.png"));
    }

    private static void CopyPngDirectory(string relativePath, string destinationRoot, ref int copiedFiles, ref long copiedBytes)
    {
        string sourceRoot = SourcePath(relativePath);
        string[] files = Directory.GetFiles(sourceRoot, "*.png", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string suffix = files[i].Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string destination = Path.Combine(destinationRoot, relativePath, suffix);
            CopyPhysicalFile(files[i], destination, ref copiedFiles, ref copiedBytes);
        }
    }

    private static void CopyFile(string relativePath, string destinationRoot, bool required, ref int copiedFiles, ref long copiedBytes)
    {
        string source = SourcePath(relativePath);
        if (!File.Exists(source))
        {
            if (required)
            {
                throw new BuildFailedException("Missing Zombie Storm runtime art file: " + source);
            }

            return;
        }

        CopyPhysicalFile(source, Path.Combine(destinationRoot, relativePath), ref copiedFiles, ref copiedBytes);
    }

    private static void CopyPhysicalFile(string source, string destination, ref int copiedFiles, ref long copiedBytes)
    {
        string destinationDirectory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        File.Copy(source, destination, true);
        copiedFiles++;
        copiedBytes += new FileInfo(source).Length;
    }

    private static string SourcePath(string relativePath)
    {
        return Path.Combine(Application.dataPath, relativePath);
    }

    private static void EnsureSupportedTarget(BuildTarget target)
    {
        if (target != BuildTarget.StandaloneWindows &&
            target != BuildTarget.StandaloneWindows64 &&
            target != BuildTarget.StandaloneLinux64 &&
            target != BuildTarget.StandaloneOSX)
        {
            throw new BuildFailedException(
                "Zombie Storm currently supports packaged runtime art on Windows, Linux, and macOS desktop builds.");
        }
    }

    private static string GetDestinationRoot(BuildReport report)
    {
        string outputPath = report.summary.outputPath;
        if (report.summary.platform == BuildTarget.StandaloneOSX)
        {
            return Path.Combine(outputPath, "Contents", "Resources", "Data", "StreamingAssets", ContentFolderName);
        }

        string outputDirectory = Path.GetDirectoryName(outputPath);
        string playerName = Path.GetFileNameWithoutExtension(outputPath);
        return Path.Combine(outputDirectory ?? string.Empty, playerName + "_Data", "StreamingAssets", ContentFolderName);
    }
}
