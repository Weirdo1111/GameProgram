using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Runtime art file loading and animation slicing.
public sealed partial class ZombieStormGameController
{
    // Loads player walk animation frames.
    private void LoadPlayerWalkFrames()
    {
        playerWalkFrames.Clear();
        playerWalkFramesAreIdle = false;

        if (LoadChibiPyromancerIdleFrames())
        {
            return;
        }
    }

    // Loads player idle animation frames and replaces the fallback sprite when found.
    private bool LoadChibiPyromancerIdleFrames()
    {
        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Player", "chibi_pyromancer_idle");
        if (!Directory.Exists(root))
        {
            return false;
        }

        string[] files = Directory.GetFiles(root, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 400f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count == 0)
        {
            return false;
        }

        Sprite[] idleFrames = frames.ToArray();
        playerIdleFrames = idleFrames;
        playerSprite = idleFrames[0];
        Sprite[] rightWalkFrames = LoadChibiPyromancerWalkRightFrames();
        if (rightWalkFrames != null && rightWalkFrames.Length > 0)
        {
            playerWalkFrames["walk_right"] = rightWalkFrames;
            playerWalkFrames["walk_left"] = rightWalkFrames;
            playerWalkFrames["walk_down"] = rightWalkFrames;
            playerWalkFramesAreIdle = false;
        }
        else
        {
            playerWalkFrames["walk_right"] = idleFrames;
            playerWalkFrames["walk_left"] = idleFrames;
            playerWalkFrames["walk_down"] = idleFrames;
            playerWalkFramesAreIdle = true;
        }

        return true;
    }

    // Loads player walk-right animation frames.
    private Sprite[] LoadChibiPyromancerWalkRightFrames()
    {
        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Player", "chibi_pyromancer_walk_right");
        if (!Directory.Exists(root))
        {
            return null;
        }

        string[] files = Directory.GetFiles(root, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 400f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        return frames.Count > 0 ? frames.ToArray() : null;
    }

    // Loads the hurt frames for the currently selected player art.
    private void LoadScreenSelectedHurtFrames()
    {
        if (LoadChibiPyromancerHurtFrames())
        {
            return;
        }

        string root = Path.Combine(RuntimeContentRoot, "screen_selected");
        if (!Directory.Exists(root))
        {
            return;
        }

        int[] columns = { 2, 3, 4, 5 };
        List<Sprite> frames = new List<Sprite>(columns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            string path = Path.Combine(root, "screen_r2_c" + columns[i] + ".png");
            if (!File.Exists(path))
            {
                continue;
            }

            Sprite frame = LoadSpriteFromPng(path, 160f, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count > 0)
        {
            playerHurtFrames = frames.ToArray();
        }
    }

    // Loads player hurt animation frames used during damage feedback.
    private bool LoadChibiPyromancerHurtFrames()
    {
        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Player", "hurt");
        if (!Directory.Exists(root))
        {
            return false;
        }

        string[] files = Directory.GetFiles(root, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 400f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count == 0)
        {
            return false;
        }

        playerHurtFrames = frames.ToArray();
        return true;
    }

    // Loads Kenney top-down map and decoration assets.
    private void LoadKenneyTopdownArt()
    {
        string root = Path.Combine(RuntimeContentRoot, "ExternalArt", "KenneyTopdownShooter", "PNG");
        if (!Directory.Exists(root))
        {
            return;
        }

        string tileRoot = Path.Combine(root, "Tiles");
        groundSprites.Clear();
        debrisSprites.Clear();

        int[] groundIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        for (int i = 0; i < groundIds.Length; i++)
        {
            AddSpriteIfExists(groundSprites, Path.Combine(tileRoot, "tile_" + groundIds[i].ToString("00") + ".png"), 64f, false);
        }

        int[] debrisIds = { 131, 132, 134, 156, 157, 158, 181, 182, 183, 184, 185, 186, 197, 198, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 235, 236, 237, 238, 239, 240 };
        for (int i = 0; i < debrisIds.Length; i++)
        {
            AddSpriteIfExists(debrisSprites, Path.Combine(tileRoot, "tile_" + debrisIds[i] + ".png"), 64f, false);
        }

        kenneyZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_stand.png"), 24f, false);
        kenneyFastZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_hold.png"), 24f, false);
        kenneyTankZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_machine.png"), 22f, false);
        kenneyEliteZombieSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_gun.png"), 22f, false);
        kenneyBossSprite = LoadRawSpriteFromPng(Path.Combine(root, "Zombie 1", "zoimbie1_reload.png"), 18f, false);

    }

    // Loads walk frames for small enemy art.
    private void LoadChibiEnemyWalkFrames()
    {
        chibiEnemyWalkFrames = new Sprite[0];

        string folder = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "chibi_zombie");
        const float chibiPixelsPerUnit = 440f;
        Sprite[] frames = LoadEnemyFrameSequence(folder, "zombie_chibi_", chibiPixelsPerUnit, true);
        if (frames == null || frames.Length == 0)
        {
            string sheetPath = Path.Combine(folder, "zombie_chibi_walk.png");
            frames = LoadEnemyWalkSheet(sheetPath, chibiPixelsPerUnit, true, 4, 4);
        }

        if (frames != null && frames.Length > 0)
        {
            chibiEnemyWalkFrames = frames;
        }
    }

    // Loads animation frames for the villager enemy.
    private void LoadCraftpixVillagerFrames()
    {
        villagerRunFrames = new Sprite[0];
        villagerSlashFrames = new Sprite[0];
        villagerHurtFrames = new Sprite[0];
        villagerDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_villager");
        const float pixelsPerUnit = 264f;
        villagerRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        villagerSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        villagerHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        villagerDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the melee enemy.
    private void LoadCraftpixGoblinFrames()
    {
        goblinRunFrames = new Sprite[0];
        goblinHurtFrames = new Sprite[0];
        goblinDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_goblin");
        const float pixelsPerUnit = 264f;
        goblinRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        goblinHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        goblinDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the gravedigger enemy.
    private void LoadCraftpixGravediggerFrames()
    {
        gravediggerRunFrames = new Sprite[0];
        gravediggerSlashFrames = new Sprite[0];
        gravediggerHurtFrames = new Sprite[0];
        gravediggerDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_gravedigger");
        const float pixelsPerUnit = 264f;
        gravediggerRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        gravediggerSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        gravediggerHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        gravediggerDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the reaper enemy.
    private void LoadCraftpixReaperFrames()
    {
        reaperRunFrames = new Sprite[0];
        reaperSlashFrames = new Sprite[0];
        reaperHurtFrames = new Sprite[0];
        reaperDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_reaper");
        const float pixelsPerUnit = 264f;
        reaperRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        reaperSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        reaperHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        reaperDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the orc thrower enemy.
    private void LoadCraftpixOrcFrames()
    {
        orcRunFrames = new Sprite[0];
        orcThrowFrames = new Sprite[0];
        orcHurtFrames = new Sprite[0];
        orcDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_orc");
        const float pixelsPerUnit = 264f;
        orcRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        orcThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        orcHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        orcDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the crystal boss.
    private void LoadCraftpixCrystalGolemFrames()
    {
        crystalGolemRunFrames = new Sprite[0];
        crystalGolemSlashFrames = new Sprite[0];
        crystalGolemThrowFrames = new Sprite[0];
        crystalGolemHurtFrames = new Sprite[0];
        crystalGolemDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_crystal_golem");
        const float pixelsPerUnit = 230f;
        crystalGolemRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        crystalGolemSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        crystalGolemThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        crystalGolemHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        crystalGolemDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the moss boss.
    private void LoadCraftpixMossGolemFrames()
    {
        mossGolemRunFrames = new Sprite[0];
        mossGolemSlashFrames = new Sprite[0];
        mossGolemThrowFrames = new Sprite[0];
        mossGolemHurtFrames = new Sprite[0];
        mossGolemDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_moss_golem");
        const float pixelsPerUnit = 230f;
        mossGolemRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        mossGolemSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        mossGolemThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        mossGolemHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        mossGolemDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads animation frames for the ember boss.
    private void LoadCraftpixEmberGolemFrames()
    {
        emberGolemRunFrames = new Sprite[0];
        emberGolemSlashFrames = new Sprite[0];
        emberGolemThrowFrames = new Sprite[0];
        emberGolemHurtFrames = new Sprite[0];
        emberGolemDeathFrames = new Sprite[0];

        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Enemies", "craftpix_ember_golem");
        const float pixelsPerUnit = 230f;
        emberGolemRunFrames = LoadEnemyFrameFolder(Path.Combine(root, "Run"), pixelsPerUnit);
        emberGolemSlashFrames = LoadEnemyFrameFolder(Path.Combine(root, "Slash"), pixelsPerUnit);
        emberGolemThrowFrames = LoadEnemyFrameFolder(Path.Combine(root, "Throw"), pixelsPerUnit);
        emberGolemHurtFrames = LoadEnemyFrameFolder(Path.Combine(root, "Hurt"), pixelsPerUnit);
        emberGolemDeathFrames = LoadEnemyFrameFolder(Path.Combine(root, "Death"), pixelsPerUnit);
    }

    // Loads the custom map image used by arena generation.
    private void LoadCustomArenaMap()
    {
        customArenaMapSprite = null;

        string path = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Maps", "graveyard_arena.png");
        customArenaMapSprite = LoadRawSpriteFromPng(path, 64f, false);
    }

    // Loads the main menu cover image.
    private void LoadMainMenuCover()
    {
        mainMenuCoverSprite = null;

        string path = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Menu", "main_menu_cover.png");
        mainMenuCoverSprite = LoadRawSpriteFromPng(path, 100f, false, FilterMode.Bilinear, false);
    }

    // Loads the story pages shown after pressing Start Run.
    private void LoadStoryPageTextures()
    {
        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Story");
        if (!Directory.Exists(root))
        {
            storyPageTextures = new Texture2D[0];
            return;
        }

        string[] files = Directory.GetFiles(root, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Texture2D> pages = new List<Texture2D>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Texture2D page = LoadTextureFromPng(files[i]);
            if (page != null)
            {
                pages.Add(page);
            }
        }

        storyPageTextures = pages.ToArray();
    }

    // Loads the chibi card art used behind upgrade option text.
    private void LoadUpgradeCardTemplate()
    {
        upgradeCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_chibi_fire_template.png"));
        magicBoltCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_magic_bolt_template.png"));
        fireBladesCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_fire_blades_template.png"));
        fireZoneCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_fire_zone_template.png"));
        damageCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_damage_template.png"));
        cooldownCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_cooldown_template.png"));
        xpCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_xp_template.png"));
        regenerationCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_regeneration_template.png"));
        stormCardTemplateTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "skill_card_storm_template.png"));
    }

    // Uses softer system fonts for card text when available.
    private void LoadUpgradeCardFonts()
    {
        upgradeCardTitleFont = CreateRuntimeFont(new[] { "Georgia", "Cambria", "Palatino Linotype", "Times New Roman" }, 24);
        upgradeCardBodyFont = CreateRuntimeFont(new[] { "Trebuchet MS", "Segoe UI Semibold", "Segoe UI", "Arial" }, 16);
    }

    // Loads the art-backed top-left player status HUD.
    private void LoadPlayerStatusCardTexture()
    {
        playerStatusCardTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "player_status_card_cropped.png"));
    }

    // Loads result screen backgrounds.
    private void LoadResultScreenTextures()
    {
        failedResultTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "result_failed.png"));
        victoryResultTexture = LoadTextureFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "result_victory.png"));
    }

    // Loads the health potion pickup art.
    private void LoadHealthPotionSprite()
    {
        healthPotionSprite = LoadRawSpriteFromPng(Path.Combine(RuntimeContentRoot, "ZombieStormArt", "UI", "health_potion.png"), 420f, true, FilterMode.Bilinear, false, true);
    }

    // Creates a dynamic font from the first installed candidate Unity can resolve.
    private static Font CreateRuntimeFont(string[] names, int size)
    {
        for (int i = 0; i < names.Length; i++)
        {
            try
            {
                Font font = Font.CreateDynamicFontFromOSFont(names[i], size);
                if (font != null)
                {
                    return font;
                }
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    // Loads the sprite used by the Fire Spirit summon.
    private void LoadFireSpiritSprite()
    {
        fireSpiritSprite = null;

        string path = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Player", "FireSpirit.png");
        fireSpiritSprite = LoadRawSpriteFromPng(path, 720f, true, FilterMode.Bilinear, false, true);
    }

    // Loads spell effect frame sequences such as fire and explosions.
    private void LoadMikodrakSpellEffects()
    {
        effectFrames.Clear();
        projectileFxSprite = null;

        string root = Path.Combine(RuntimeContentRoot, "ExternalArt", "MikodrakSpellEffects");
        if (!Directory.Exists(root))
        {
            AddDarkVfxEffectSequences();
            AddFoozlePixelMagicEffectSequences();
            AddFireZoneEffectSequences();
            return;
        }

        AddEffectSequence(root, "spark", "fx1_blue_topEffect", 240f);
        AddEffectSequence(root, "fire", "fx3_fireBall", 240f);
        AddEffectSequence(root, "burst", "fx7_energyBall", 240f);
        AddEffectSequence(root, "lightning", "fx8_lighteningBall", 240f);
        AddEffectSequence(root, "explosion", "fx10_blackExplosion", 240f);
        AddDarkVfxEffectSequences();
        AddFoozlePixelMagicEffectSequences();
        AddFireZoneEffectSequences();

        Sprite[] projectileFrames;
        if (effectFrames.TryGetValue("burst", out projectileFrames) && projectileFrames != null && projectileFrames.Length > 0)
        {
            projectileFxSprite = projectileFrames[Mathf.Min(2, projectileFrames.Length - 1)];
        }
    }

    // Loads animation frames for the ice boss orb projectile.
    private void LoadIceBossOrbFrames()
    {
        iceBossOrbFrames = new Sprite[0];

        string folder = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Effects", "IceBossOrb");
        if (!Directory.Exists(folder))
        {
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite frame = LoadRawSpriteFromPng(files[i], 220f, false, FilterMode.Bilinear, false, true);
            if (frame != null)
            {
                frames.Add(frame);
            }
        }

        if (frames.Count > 0)
        {
            iceBossOrbFrames = frames.ToArray();
        }
    }

    // Registers dark spell effect frame sequences.
    private void AddDarkVfxEffectSequences()
    {
        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Effects");
        AddEffectSequence(root, "ember_dash_blast", Path.Combine("DarkVFX1", "Frames"), 38f);
        AddEffectSequence(root, "ember_meteor_blast", Path.Combine("DarkVFX2", "Frames"), 44f);
        AddEffectSequence(root, "ember_boss_meteor", "EmberBossMeteorSelected", 180f);
        AddEffectSequence(root, "poison_boss_blast", "CraftpixPoisonExplosion10", 150f);
    }

    // Registers pixel magic effect frame sequences.
    private void AddFoozlePixelMagicEffectSequences()
    {
        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Effects", "FoozlePixelMagic");
        AddEffectSequence(root, "foozle_fireball", "Fire_Ball", 64f);
        AddEffectSequence(root, "foozle_explosion", "Explosion", 72f);
        AddEffectSequence(root, "meteor_blast", "Explosion", 82f);
        AddEffectSequence(root, "shield_burst", "Wind", 92f);
        AddEffectSequence(root, "ultimate_storm", "Tornado", 88f);
    }

    // Registers Fire Zone bomb and ground-fire effect frame sequences.
    private void AddFireZoneEffectSequences()
    {
        string root = Path.Combine(RuntimeContentRoot, "ZombieStormArt", "Effects");
        AddEffectSequence(root, "fire_bomb", "FireBomb", 96f);
        AddEffectSheetSequence(root, "fire_pool_tekila_01", Path.Combine("GroundFire", "TekilaFire01", "Fire-01_320x160_Sheet.png"), 5, 1, 96f, true);
    }

    // Loads one effect frame sequence from a folder and stores it by key.
    private void AddEffectSequence(string root, string key, string folderName, float pixelsPerUnit)
    {
        string folder = Path.Combine(root, folderName);
        if (!Directory.Exists(folder))
        {
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite sprite = LoadRawSpriteFromPng(files[i], pixelsPerUnit, false);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        if (frames.Count > 0)
        {
            effectFrames[key] = frames.ToArray();
        }
    }

    // Loads one effect sprite sheet and stores sliced frames by key.
    private void AddEffectSheetSequence(string root, string key, string sheetName, int columns, int rows, float pixelsPerUnit, bool removeBlackBackground)
    {
        string path = Path.Combine(root, sheetName);
        if (!File.Exists(path) || columns <= 0 || rows <= 0)
        {
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return;
            }

            if (removeBlackBackground)
            {
                RemoveBlackBackground(texture);
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(false, false);
            List<Sprite> frames = new List<Sprite>(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                int sourceRow = rows - row - 1;
                for (int column = 0; column < columns; column++)
                {
                    int left = Mathf.RoundToInt(column * texture.width / (float)columns);
                    int right = Mathf.RoundToInt((column + 1) * texture.width / (float)columns);
                    int bottom = Mathf.RoundToInt(sourceRow * texture.height / (float)rows);
                    int top = Mathf.RoundToInt((sourceRow + 1) * texture.height / (float)rows);
                    Sprite frame = CreateSpriteFromTexture(
                        texture,
                        new Rect(left, bottom, right - left, top - bottom),
                        new Vector2(0.5f, 0.5f),
                        pixelsPerUnit);
                    frame.name = texture.name + "_" + (frames.Count + 1).ToString("00");
                    frames.Add(frame);
                }
            }

            if (frames.Count > 0)
            {
                effectFrames[key] = frames.ToArray();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load effect sheet: " + path + "\n" + exception.Message);
        }
    }

    // Sorts frame files by the trailing number in their names.
    private static int CompareFrameFileNames(string left, string right)
    {
        int leftNumber = ExtractTrailingFrameNumber(Path.GetFileNameWithoutExtension(left));
        int rightNumber = ExtractTrailingFrameNumber(Path.GetFileNameWithoutExtension(right));
        int numberCompare = leftNumber.CompareTo(rightNumber);
        return numberCompare != 0 ? numberCompare : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    // Extracts the trailing frame number from a file name.
    private static int ExtractTrailingFrameNumber(string name)
    {
        int frameIndex = name.IndexOf("frame", StringComparison.OrdinalIgnoreCase);
        if (frameIndex >= 0)
        {
            int scan = frameIndex + 5;
            while (scan < name.Length && (name[scan] == '-' || name[scan] == '_' || name[scan] == ' '))
            {
                scan++;
            }

            int valueFromFramePrefix = 0;
            bool foundFrameDigit = false;
            while (scan < name.Length && name[scan] >= '0' && name[scan] <= '9')
            {
                valueFromFramePrefix = valueFromFramePrefix * 10 + name[scan] - '0';
                foundFrameDigit = true;
                scan++;
            }

            if (foundFrameDigit)
            {
                return valueFromFramePrefix;
            }
        }

        int value = 0;
        int multiplier = 1;
        bool foundDigit = false;
        for (int i = name.Length - 1; i >= 0; i--)
        {
            char character = name[i];
            if (character < '0' || character > '9')
            {
                break;
            }

            foundDigit = true;
            value += (character - '0') * multiplier;
            multiplier *= 10;
        }

        return foundDigit ? value : 0;
    }

    // Loads a sprite into a list only when the source file exists.
    private void AddSpriteIfExists(List<Sprite> target, string path, float pixelsPerUnit, bool removeCheckerBackground)
    {
        Sprite sprite = LoadRawSpriteFromPng(path, pixelsPerUnit, removeCheckerBackground);
        if (sprite != null)
        {
            target.Add(sprite);
        }
    }

    // Slices enemy walk frames from a sprite sheet.
    private Sprite[] LoadEnemyWalkSheet(string path, float pixelsPerUnit, bool removeCheckerBackground, int specifiedColumns = 0, int specifiedRows = 0)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            if (removeCheckerBackground)
            {
                RemoveEdgeCheckerBackground(texture);
                CleanBackgroundFringe(texture);
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(false, false);

            bool tenFrameGrid = texture.width < texture.height * 3.2f;
            int columns = specifiedColumns > 0 ? specifiedColumns : tenFrameGrid ? 5 : 4;
            int rows = specifiedRows > 0 ? specifiedRows : tenFrameGrid ? 2 : 1;
            List<Sprite> frames = new List<Sprite>(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                int sourceRow = rows - row - 1;
                for (int column = 0; column < columns; column++)
                {
                    int left = Mathf.RoundToInt(column * texture.width / (float)columns);
                    int right = Mathf.RoundToInt((column + 1) * texture.width / (float)columns);
                    int bottom = Mathf.RoundToInt(sourceRow * texture.height / (float)rows);
                    int top = Mathf.RoundToInt((sourceRow + 1) * texture.height / (float)rows);
                    Rect rect = new Rect(left, bottom, right - left, top - bottom);
                    Sprite frame = CreateSpriteFromTexture(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
                    frame.name = texture.name + "_" + (frames.Count + 1).ToString("00");
                    frames.Add(frame);
                }
            }

            return frames.ToArray();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load custom enemy walk sheet: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Loads enemy animation frames from numbered files.
    private Sprite[] LoadEnemyFrameSequence(string folder, string prefix, float pixelsPerUnit, bool removeCheckerBackground)
    {
        if (!Directory.Exists(folder))
        {
            return null;
        }

        List<Sprite> frames = new List<Sprite>(16);
        for (int i = 1; i <= 24; i++)
        {
            string path = Path.Combine(folder, prefix + i.ToString("00") + ".png");
            if (!File.Exists(path))
            {
                if (frames.Count > 0)
                {
                    break;
                }

                continue;
            }

            Sprite sprite = LoadRawSpriteFromPng(path, pixelsPerUnit, removeCheckerBackground);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        return frames.Count > 0 ? frames.ToArray() : null;
    }

    // Loads and sorts all enemy animation frames in a folder.
    private Sprite[] LoadEnemyFrameFolder(string folder, float pixelsPerUnit)
    {
        if (!Directory.Exists(folder))
        {
            return new Sprite[0];
        }

        string[] files = Directory.GetFiles(folder, "*.png");
        Array.Sort(files, CompareFrameFileNames);
        List<Sprite> frames = new List<Sprite>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            Sprite sprite = LoadRawSpriteFromPng(files[i], pixelsPerUnit, false);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        return frames.ToArray();
    }

    // Loads a PNG as a sprite with optional background cleanup.
    private Sprite LoadRawSpriteFromPng(string path, float pixelsPerUnit, bool removeCheckerBackground)
    {
        return LoadRawSpriteFromPng(path, pixelsPerUnit, removeCheckerBackground, FilterMode.Bilinear, true);
    }

    // Loads a PNG as a UI texture.
    private Texture2D LoadTextureFromPng(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(false, false);
            return texture;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load UI texture: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Returns the largest centered rect that preserves a texture's aspect ratio.
    private static Rect GetCenteredTextureRect(Texture2D texture)
    {
        if (texture == null || texture.height <= 0)
        {
            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        float textureAspect = texture.width / (float)texture.height;
        float screenAspect = Screen.width / (float)Mathf.Max(1, Screen.height);
        float width = Screen.width;
        float height = Screen.height;
        if (screenAspect > textureAspect)
        {
            width = height * textureAspect;
        }
        else
        {
            height = width / textureAspect;
        }

        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    // Converts normalized coordinates inside a parent rect to an absolute GUI rect.
    private static Rect RelativeRect(Rect parent, float x, float y, float width, float height)
    {
        return new Rect(parent.x + parent.width * x, parent.y + parent.height * y, parent.width * width, parent.height * height);
    }

    // Loads a PNG as a sprite with optional background cleanup.
    private Sprite LoadRawSpriteFromPng(string path, float pixelsPerUnit, bool removeCheckerBackground, FilterMode filterMode, bool useMipMaps, bool pivotOnOpaqueCenter = false)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, useMipMaps);
            texture.filterMode = filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            if (removeCheckerBackground)
            {
                RemoveEdgeCheckerBackground(texture);
                CleanBackgroundFringe(texture);
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.Apply(useMipMaps, false);
            Vector2 pivot = pivotOnOpaqueCenter ? CalculateOpaqueCenterPivot(texture) : new Vector2(0.5f, 0.5f);
            return CreateSpriteFromTexture(texture, pivot, pixelsPerUnit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load external art: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Loads a PNG file and converts it into a Unity sprite.
    private Sprite LoadSpriteFromPng(string path, float pixelsPerUnit, bool removeCheckerBackground = false)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            if (!ImageConversion.LoadImage(texture, bytes))
            {
                return null;
            }

            if (removeCheckerBackground)
            {
                RemoveEdgeCheckerBackground(texture);
            }

            Texture2D normalizedTexture = NormalizePlayerFrame(texture, 220, 270);
            normalizedTexture.name = Path.GetFileNameWithoutExtension(path);
            return CreateSpriteFromTexture(normalizedTexture, new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load player frame: " + path + "\n" + exception.Message);
            return null;
        }
    }

}
