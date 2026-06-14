using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Runtime art loading, texture processing, animation slicing, and fallback sprite generation.
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
                    Sprite frame = Sprite.Create(texture, new Rect(left, bottom, right - left, top - bottom), new Vector2(0.5f, 0.5f), pixelsPerUnit);
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
                    Sprite frame = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
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
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, pixelsPerUnit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load external art: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Calculates a sprite pivot from the center of its visible pixels.
    private static Vector2 CalculateOpaqueCenterPivot(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        long sumX = 0;
        long sumY = 0;
        long count = 0;

        for (int y = 0; y < texture.height; y++)
        {
            int row = y * texture.width;
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[row + x].a <= 20)
                {
                    continue;
                }

                sumX += x;
                sumY += y;
                count++;
            }
        }

        if (count == 0)
        {
            return new Vector2(0.5f, 0.5f);
        }

        return new Vector2(
            Mathf.Clamp01(sumX / (float)count / Mathf.Max(1, texture.width - 1)),
            Mathf.Clamp01(sumY / (float)count / Mathf.Max(1, texture.height - 1)));
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
            return Sprite.Create(normalizedTexture, new Rect(0f, 0f, normalizedTexture.width, normalizedTexture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load player frame: " + path + "\n" + exception.Message);
            return null;
        }
    }

    // Makes near-black sprite-sheet backgrounds transparent while preserving fire colors.
    private static void RemoveBlackBackground(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 color = pixels[i];
            if (color.a < 10)
            {
                continue;
            }

            int brightness = color.r + color.g + color.b;
            if (brightness <= 34 && color.r <= 18 && color.g <= 18 && color.b <= 18)
            {
                color.a = 0;
                pixels[i] = color;
            }
        }

        texture.SetPixels32(pixels);
    }

    // Removes checkerboard transparency background connected to image edges.
    private void RemoveEdgeCheckerBackground(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>();

        for (int x = 0; x < width; x++)
        {
            TryQueueBackgroundPixel(x, 0, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, height - 1, width, pixels, visited, queue);
        }

        for (int y = 0; y < height; y++)
        {
            TryQueueBackgroundPixel(0, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(width - 1, y, width, pixels, visited, queue);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            pixels[index].a = 0;
            int x = index % width;
            int y = index / width;
            TryQueueBackgroundPixel(x + 1, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x - 1, y, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, y + 1, width, pixels, visited, queue);
            TryQueueBackgroundPixel(x, y - 1, width, pixels, visited, queue);
        }

        texture.SetPixels32(pixels);
        texture.Apply(true, false);
    }

    // Cleans leftover edge colors to reduce white or gray outlines.
    private void CleanBackgroundFringe(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        Color32[] cleaned = new Color32[pixels.Length];
        Array.Copy(pixels, cleaned, pixels.Length);

        for (int pass = 0; pass < 4; pass++)
        {
            bool changed = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color32 color = pixels[index];
                    if (color.a == 0 || !TouchesTransparentPixel(x, y, width, height, pixels))
                    {
                        continue;
                    }

                    int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                    int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                    int average = (color.r + color.g + color.b) / 3;
                    int saturation = max - min;
                    if (average >= 188 && saturation <= 82)
                    {
                        cleaned[index].a = 0;
                        changed = true;
                    }
                    else if (average >= 172 && saturation <= 96)
                    {
                        cleaned[index].a = (byte)Mathf.Min(color.a, 92);
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                break;
            }

            Color32[] swap = pixels;
            pixels = cleaned;
            cleaned = swap;
            Array.Copy(pixels, cleaned, pixels.Length);
        }

        DilateTransparentPixels(texture, pixels);
    }

    // Checks whether a pixel touches transparency during edge cleanup.
    private static bool TouchesTransparentPixel(int x, int y, int width, int height, Color32[] pixels)
    {
        for (int yy = y - 1; yy <= y + 1; yy++)
        {
            for (int xx = x - 1; xx <= x + 1; xx++)
            {
                if (xx == x && yy == y)
                {
                    continue;
                }

                if (xx < 0 || xx >= width || yy < 0 || yy >= height)
                {
                    return true;
                }

                if (pixels[yy * width + xx].a < 20)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Fills transparent pixels with neighbor colors to prevent scaled texture borders.
    private void DilateTransparentPixels(Texture2D texture, Color32[] pixels)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] dilated = new Color32[pixels.Length];
        Array.Copy(pixels, dilated, pixels.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (pixels[index].a >= 20)
                {
                    continue;
                }

                Color32 replacement;
                if (TryFindOpaqueNeighborColor(x, y, width, height, pixels, out replacement))
                {
                    replacement.a = 0;
                    dilated[index] = replacement;
                }
            }
        }

        texture.SetPixels32(dilated);
        texture.Apply(false, false);
    }

    // Finds a nearby opaque pixel color for transparent edge filling.
    private static bool TryFindOpaqueNeighborColor(int x, int y, int width, int height, Color32[] pixels, out Color32 color)
    {
        for (int radius = 1; radius <= 3; radius++)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int count = 0;
            for (int yy = y - radius; yy <= y + radius; yy++)
            {
                for (int xx = x - radius; xx <= x + radius; xx++)
                {
                    if (xx < 0 || xx >= width || yy < 0 || yy >= height)
                    {
                        continue;
                    }

                    Color32 sample = pixels[yy * width + xx];
                    if (sample.a < 210)
                    {
                        continue;
                    }

                    r += sample.r;
                    g += sample.g;
                    b += sample.b;
                    count++;
                }
            }

            if (count > 0)
            {
                color = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
                return true;
            }
        }

        color = new Color32(0, 0, 0, 0);
        return false;
    }

    // Adds a likely background pixel to the flood-fill queue.
    private static void TryQueueBackgroundPixel(int x, int y, int width, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        int height = pixels.Length / width;
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = y * width + x;
        if (visited[index] || !IsCheckerBackground(pixels[index]))
        {
            return;
        }

        visited[index] = true;
        queue.Enqueue(index);
    }

    // Checks whether a pixel color looks like checkerboard background.
    private static bool IsCheckerBackground(Color32 color)
    {
        if (color.a < 10)
        {
            return true;
        }

        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        int average = (color.r + color.g + color.b) / 3;
        int saturation = max - min;
        if (average >= 232)
        {
            return true;
        }

        return saturation <= 48 && average >= 168;
    }

    // Places player frames onto a consistent texture canvas size.
    private Texture2D NormalizePlayerFrame(Texture2D source, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);

        int offsetX = Mathf.RoundToInt((width - source.width) * 0.5f);
        int offsetY = 0;
        Color[] pixels = source.GetPixels();
        texture.SetPixels(offsetX, offsetY, source.width, source.height, pixels);
        texture.Apply(true, false);
        return texture;
    }

    // Programmatically draws a fallback survivor sprite.
    private Sprite CreateSurvivorSprite()
    {
        const int width = 32;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        ClearTexture(texture, Color.clear);

        Color outline = new Color(0.08f, 0.055f, 0.035f, 1f);
        Color hatDark = new Color(0.28f, 0.20f, 0.10f, 1f);
        Color hat = new Color(0.58f, 0.43f, 0.20f, 1f);
        Color hatLight = new Color(0.86f, 0.68f, 0.33f, 1f);
        Color skin = new Color(0.92f, 0.68f, 0.45f, 1f);
        Color skinShadow = new Color(0.62f, 0.36f, 0.22f, 1f);
        Color hair = new Color(0.18f, 0.11f, 0.07f, 1f);
        Color shirt = new Color(0.86f, 0.88f, 0.80f, 1f);
        Color vest = new Color(0.50f, 0.34f, 0.16f, 1f);
        Color scarf = new Color(0.72f, 0.08f, 0.06f, 1f);
        Color denim = new Color(0.12f, 0.22f, 0.34f, 1f);
        Color boot = new Color(0.20f, 0.12f, 0.07f, 1f);
        Color glove = new Color(0.22f, 0.14f, 0.08f, 1f);
        Color eye = new Color(0.08f, 0.18f, 0.32f, 1f);

        FillEllipse(texture, 16, 28, 8, 2, new Color(0f, 0f, 0f, 0.32f));

        FillRect(texture, 10, 20, 4, 7, outline);
        FillRect(texture, 18, 20, 4, 7, outline);
        FillRect(texture, 11, 20, 3, 6, denim);
        FillRect(texture, 18, 20, 3, 6, denim);
        FillRect(texture, 9, 26, 6, 3, outline);
        FillRect(texture, 17, 26, 6, 3, outline);
        FillRect(texture, 10, 26, 5, 2, boot);
        FillRect(texture, 17, 26, 5, 2, boot);

        FillRect(texture, 8, 13, 16, 10, outline);
        FillRect(texture, 9, 14, 14, 8, shirt);
        FillRect(texture, 10, 14, 4, 8, vest);
        FillRect(texture, 18, 14, 4, 8, vest);
        FillRect(texture, 14, 14, 4, 5, new Color(0.96f, 0.95f, 0.84f, 1f));
        FillRect(texture, 14, 15, 4, 3, scarf);
        FillRect(texture, 15, 18, 2, 4, new Color(0.95f, 0.72f, 0.28f, 1f));

        FillRect(texture, 5, 15, 5, 7, outline);
        FillRect(texture, 22, 15, 5, 7, outline);
        FillRect(texture, 6, 15, 3, 6, shirt);
        FillRect(texture, 23, 15, 3, 6, shirt);
        FillRect(texture, 5, 21, 4, 3, glove);
        FillRect(texture, 23, 21, 4, 3, glove);

        FillEllipse(texture, 16, 11, 8, 6, outline);
        FillEllipse(texture, 16, 11, 7, 5, skin);
        FillRect(texture, 9, 8, 3, 6, hair);
        FillRect(texture, 20, 8, 3, 6, hair);
        FillRect(texture, 11, 13, 10, 2, skinShadow);
        SetPixelSafe(texture, 12, 10, eye);
        SetPixelSafe(texture, 20, 10, eye);
        SetPixelSafe(texture, 13, 11, Color.white);
        SetPixelSafe(texture, 21, 11, Color.white);
        FillRect(texture, 15, 13, 4, 1, new Color(0.38f, 0.12f, 0.08f, 1f));

        FillEllipse(texture, 16, 6, 13, 3, outline);
        FillEllipse(texture, 16, 6, 12, 2, hatDark);
        FillRect(texture, 9, 1, 14, 7, outline);
        FillRect(texture, 10, 1, 12, 6, hat);
        FillRect(texture, 12, 2, 8, 2, hatLight);
        FillRect(texture, 8, 7, 16, 2, hatDark);
        FillRect(texture, 12, 6, 8, 1, hatLight);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 24f);
    }

    // Creates a simple pixel-art fallback sprite.
    private Sprite CreatePixelSprite(Color baseColor, Color accentColor, int size, bool character)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = Color.clear;

                if (character)
                {
                    if (distance <= radius)
                    {
                        pixel = baseColor;
                    }

                    if (distance <= radius * 0.55f && y > size * 0.45f)
                    {
                        pixel = Color.Lerp(baseColor, accentColor, 0.72f);
                    }

                    if (x < 2 || x > size - 3 || y < 2 || y > size - 3)
                    {
                        pixel.a *= 0.4f;
                    }
                }
                else
                {
                    pixel = baseColor;
                    if ((x + y) % 5 == 0)
                    {
                        pixel = Color.Lerp(baseColor, accentColor, 0.55f);
                    }
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws the red orbiting blade sprite.
    private Sprite CreateOrbitingBladeSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color glow = new Color(1f, 0.12f, 0.06f, 0.34f);
        Color edge = new Color(0.34f, 0.02f, 0.02f, 1f);
        Color steel = new Color(1f, 0.46f, 0.36f, 1f);
        Color highlight = Color.white;
        Color hilt = new Color(0.82f, 0.12f, 0.08f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x - center.x;
                float py = y - center.y;
                Color pixel = Color.clear;
                float bodyT = Mathf.InverseLerp(-24f, 22f, px);
                float bladeHalfWidth = Mathf.Lerp(6.4f, 2.1f, bodyT);
                bool bladeBody = px >= -24f && px <= 22f && Mathf.Abs(py) <= bladeHalfWidth;
                bool bladeTip = px > 22f && px <= 30f && Mathf.Abs(py) <= (30f - px) * 0.48f;
                bool bladeGlow = px >= -28f && px <= 31f && Mathf.Abs(py) <= bladeHalfWidth + 4.4f;
                bool grip = px >= -31f && px < -23f && Mathf.Abs(py) <= 8.2f;
                bool guard = px >= -24f && px <= -19f && Mathf.Abs(py) <= 12.5f;

                if (bladeGlow || (px > 22f && px <= 31f && Mathf.Abs(py) <= (31f - px) * 0.58f + 3f))
                {
                    pixel = glow;
                }

                if (bladeBody || bladeTip)
                {
                    pixel = Mathf.Abs(py) > bladeHalfWidth - 1.3f ? edge : Color.Lerp(steel, highlight, Mathf.Clamp01((py + bladeHalfWidth) / Mathf.Max(0.01f, bladeHalfWidth * 2f)));
                }

                if (grip)
                {
                    pixel = Mathf.FloorToInt(Mathf.Abs(py)) % 4 == 0 ? edge : hilt;
                }

                if (guard)
                {
                    pixel = Mathf.Abs(py) > 9.5f ? edge : new Color(1f, 0.3f, 0.18f, 1f);
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    // Programmatically draws the red energy ring around the blades.
    private Sprite CreateOrbitingRingSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.055f);
                float inner = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) / 0.025f) * 0.38f;
                float sparkle = (x + y) % 23 == 0 && d > 0.7f && d < 0.93f ? 0.2f : 0f;
                float alpha = Mathf.Clamp01(ring * 0.7f + inner + sparkle);
                texture.SetPixel(x, y, new Color(1f, 0.18f, 0.08f, alpha));
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    // Creates a soft circular sprite used for glows, shadows, and range markers.
    private Sprite CreateSoftDiscSprite(Color color, int size, float radiusScale, float centerFade)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f * radiusScale;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - d);
                alpha = Mathf.Pow(alpha, 1.7f);
                Color pixel = color;
                pixel.a *= Mathf.Lerp(centerFade, 1f, alpha) * alpha;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws the ground blood splat sprite.
    private Sprite CreateBloodSplatSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);
        Color blood = new Color(0.5f, 0.02f, 0.025f, 0.9f);
        FillEllipse(texture, 32, 32, 18, 9, blood);
        FillEllipse(texture, 22, 27, 9, 5, blood);
        FillEllipse(texture, 44, 36, 11, 6, blood);
        FillEllipse(texture, 34, 22, 5, 3, blood);
        FillEllipse(texture, 18, 39, 4, 3, blood);
        FillEllipse(texture, 51, 25, 3, 2, blood);
        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // Programmatically draws a neon sign sprite.
    private Sprite CreateNeonSignSprite()
    {
        const int width = 64;
        const int height = 24;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        ClearTexture(texture, Color.clear);
        FillRect(texture, 2, 4, 60, 16, new Color(0f, 0f, 0f, 0.7f));
        FillRect(texture, 4, 6, 56, 2, Color.white);
        FillRect(texture, 4, 16, 56, 2, Color.white);
        FillRect(texture, 8, 10, 8, 4, Color.white);
        FillRect(texture, 21, 10, 14, 4, Color.white);
        FillRect(texture, 41, 10, 12, 4, Color.white);
        texture.Apply(true, false);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
    }

    // Fills an entire texture with one color.
    private static void ClearTexture(Texture2D texture, Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    // Fills a rectangle area on a texture.
    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                SetPixelSafe(texture, xx, yy, color);
            }
        }
    }

    // Fills an ellipse area on a texture.
    private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        float rx = Mathf.Max(1f, radiusX);
        float ry = Mathf.Max(1f, radiusY);
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
        {
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                float dx = (x - centerX) / rx;
                float dy = (y - centerY) / ry;
                if (dx * dx + dy * dy <= 1f)
                {
                    SetPixelSafe(texture, x, y, color);
                }
            }
        }
    }

    // Writes one texture pixel only when the coordinates are inside bounds.
    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }
}
