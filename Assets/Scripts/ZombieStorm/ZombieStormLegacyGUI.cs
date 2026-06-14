using System;
using UnityEngine;

// Legacy immediate-mode menus, HUD, upgrade cards, and combat overlays.
public sealed partial class ZombieStormGameController
{
    // Legacy/fallback runtime UI.
    // The main menu uses Canvas and TextMeshPro, so this method skips main-menu drawing
    // when that UI is ready. OnGUI remains for lightweight runtime overlays such as the
    // HUD, upgrade choices, pause and result panels, plus fallback drawing when needed.
    private void OnGUI()
    {
        if (mainMenuUI != null && mainMenuUI.IsReady && (flowState == ZombieStormFlowState.MainMenu || IsMainMenuSettingsActive))
        {
            return;
        }

        DrawAtmosphereOverlay();
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 16;
        GUI.color = Color.white;

        if (flowState == ZombieStormFlowState.MainMenu)
        {
            DrawMainMenu();
            return;
        }

        if (flowState == ZombieStormFlowState.Settings && settingsReturnState == ZombieStormFlowState.MainMenu)
        {
            DrawSettingsPanel();
            return;
        }

        if (flowState == ZombieStormFlowState.Story)
        {
            DrawStoryPanel();
            return;
        }

        Matrix4x4 previousGuiMatrix = GUI.matrix;
        float hudScreenWidth = Screen.width / GameplayHudScale;
        GUIUtility.ScaleAroundPivot(Vector2.one * GameplayHudScale, Vector2.zero);

        if (Player != null)
        {
            DrawPlayerStatusHud(new Rect(12f, 8f, 430f, 164f));
        }

        int remain = Mathf.Max(0, Mathf.CeilToInt(runDurationSeconds - runTime));
        string timerLabel = runTime < runDurationSeconds ? "Survive " + FormatTime(remain) : "Clear " + GetLivingEnemyCount();
        DrawPanel(new Rect(hudScreenWidth - 224f, 10f, 206f, 48f), new Color(0.035f, 0.045f, 0.055f, 0.82f), new Color(1f, 0.85f, 0.25f, 0.28f));
        GUI.skin.label.fontSize = 24;
        GUI.Label(new Rect(hudScreenWidth - 206f, 18f, 190f, 32f), timerLabel);
        GUI.skin.label.fontSize = 18;

        if (Skills != null)
        {
            DrawPanel(new Rect(hudScreenWidth - 286f, 70f, 268f, 150f), new Color(0.035f, 0.045f, 0.055f, 0.74f), new Color(0.9f, 0.28f, 0.2f, 0.24f));
            GUI.skin.label.fontSize = 15;
            GUI.Label(new Rect(hudScreenWidth - 268f, 84f, 244f, 124f), Skills.GetLoadoutText());
            GUI.skin.label.fontSize = 18;
        }

        if (Time.unscaledTime < feedbackUntil)
        {
            DrawPanel(new Rect(hudScreenWidth * 0.5f - 330f, 78f, 680f, 46f), new Color(0.05f, 0.045f, 0.02f, 0.82f), new Color(1f, 0.75f, 0.18f, 0.45f));
            GUI.skin.label.fontSize = 22;
            GUI.color = new Color(1f, 0.86f, 0.25f, 1f);
            GUI.Label(new Rect(hudScreenWidth * 0.5f - 310f, 84f, 660f, 40f), feedbackText);
            GUI.skin.label.fontSize = 18;
            GUI.color = Color.white;
        }

        GUI.matrix = previousGuiMatrix;

        if (flowState == ZombieStormFlowState.LevelUp)
        {
            DrawUpgradePanel();
        }

        DrawBossBar();
        DrawEliteMarkers();
        DrawDamagePopups();
        DrawScreenFlash();

        if (flowState == ZombieStormFlowState.Paused)
        {
            DrawPausePanel();
        }
        else if (flowState == ZombieStormFlowState.Settings)
        {
            DrawSettingsPanel();
        }
        else if (flowState == ZombieStormFlowState.Results)
        {
            DrawResultsPanel();
        }
    }

    // Draws the fallback immediate-mode main menu.
    private void DrawMainMenu()
    {
        DrawOverlayBackdrop(0.48f);
        Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 190f, 520f, 360f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.94f), new Color(0.2f, 0.75f, 1f, 0.58f));

        GUI.color = new Color(1f, 0.92f, 0.36f, 1f);
        GUI.skin.label.fontSize = 34;
        GUI.Label(new Rect(panel.x + 34f, panel.y + 28f, panel.width - 68f, 46f), Title);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 20;
        GUI.Label(new Rect(panel.x + 36f, panel.y + 76f, panel.width - 72f, 30f), "Zombie Storm");
        GUI.skin.label.fontSize = 15;
        GUI.color = new Color(0.78f, 0.86f, 0.92f, 1f);
        GUI.Label(new Rect(panel.x + 36f, panel.y + 116f, panel.width - 72f, 70f), "Survive five minutes, grow a coherent build, and break the city horde.");
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 198f, 300f, 38f), "Start Run"))
        {
            BeginStoryOrRun();
        }

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 248f, 300f, 34f), "Settings"))
        {
            OpenSettings(ZombieStormFlowState.MainMenu);
        }

        GUI.skin.label.fontSize = 13;
        GUI.color = new Color(0.68f, 0.76f, 0.84f, 1f);
        GUI.Label(new Rect(panel.x + 118f, panel.y + 302f, 300f, 24f), "Enter also starts a run");
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 18;
    }

    // Draws the pre-run story sequence.
    private void DrawStoryPanel()
    {
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

        Texture2D page = storyPageTextures != null && storyPageIndex >= 0 && storyPageIndex < storyPageTextures.Length ? storyPageTextures[storyPageIndex] : null;
        if (page != null)
        {
            Rect imageRect = GetScaleToFitRect(page.width, page.height, new Rect(0f, 0f, Screen.width, Screen.height));
            GUI.color = Color.white;
            GUI.DrawTexture(imageRect, page, ScaleMode.ScaleToFit, true);
        }

        Rect prompt = new Rect(Screen.width - 306f, 22f, 274f, 42f);
        GUI.color = new Color(0f, 0f, 0f, 0.58f);
        GUI.DrawTexture(prompt, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.9f, 0.62f, 1f);
        GUI.skin.label.fontSize = 18;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(prompt, "Press Space to continue");
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }

    // Fits a source rectangle inside a target rectangle without stretching it.
    private static Rect GetScaleToFitRect(float sourceWidth, float sourceHeight, Rect target)
    {
        if (sourceWidth <= 0f || sourceHeight <= 0f || target.width <= 0f || target.height <= 0f)
        {
            return target;
        }

        float scale = Mathf.Min(target.width / sourceWidth, target.height / sourceHeight);
        float width = sourceWidth * scale;
        float height = sourceHeight * scale;
        return new Rect(target.x + (target.width - width) * 0.5f, target.y + (target.height - height) * 0.5f, width, height);
    }

    // Draws the pause panel and its buttons.
    private void DrawPausePanel()
    {
        DrawOverlayBackdrop(0.68f);
        Rect panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 160f, 440f, 300f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.96f), new Color(1f, 0.78f, 0.22f, 0.58f));

        GUI.skin.label.fontSize = 34;
        GUI.color = new Color(1f, 0.86f, 0.26f, 1f);
        GUI.Label(new Rect(panel.x + 118f, panel.y + 28f, 260f, 46f), "PAUSED");
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 96f, 260f, 36f), "Resume"))
        {
            ResumeRun();
        }

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 142f, 260f, 36f), "Settings"))
        {
            OpenSettings(ZombieStormFlowState.Paused);
        }

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 188f, 260f, 36f), "Restart Run"))
        {
            StartRun();
        }

        if (GUI.Button(new Rect(panel.x + 90f, panel.y + 234f, 260f, 34f), "Main Menu"))
        {
            ReturnToMainMenu();
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws settings for volume, frame rate, and fullscreen mode.
    private void DrawSettingsPanel()
    {
        DrawOverlayBackdrop(settingsReturnState == ZombieStormFlowState.MainMenu ? 0.52f : 0.72f);
        Rect panel = new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.5f - 170f, 480f, 330f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.97f), new Color(0.2f, 0.75f, 1f, 0.58f));

        GUI.skin.label.fontSize = 30;
        GUI.color = new Color(0.74f, 0.9f, 1f, 1f);
        GUI.Label(new Rect(panel.x + 36f, panel.y + 28f, 260f, 38f), "SETTINGS");
        GUI.color = Color.white;

        GUI.skin.label.fontSize = 16;
        GUI.Label(new Rect(panel.x + 42f, panel.y + 86f, 180f, 24f), "SFX Volume");
        masterVolume = GUI.HorizontalSlider(new Rect(panel.x + 170f, panel.y + 92f, 220f, 20f), masterVolume, 0f, 1f);
        GUI.Label(new Rect(panel.x + 402f, panel.y + 86f, 48f, 24f), Mathf.RoundToInt(masterVolume * 100f).ToString() + "%");

        sfxMuted = GUI.Toggle(new Rect(panel.x + 42f, panel.y + 124f, 180f, 24f), sfxMuted, "Mute SFX");

        GUI.Label(new Rect(panel.x + 42f, panel.y + 170f, 160f, 24f), "Frame Rate");
        if (GUI.Button(new Rect(panel.x + 170f, panel.y + 166f, 70f, 30f), "60"))
        {
            SetTargetFrameRate(60);
        }

        if (GUI.Button(new Rect(panel.x + 252f, panel.y + 166f, 70f, 30f), "120"))
        {
            SetTargetFrameRate(120);
        }

        if (GUI.Button(new Rect(panel.x + 334f, panel.y + 166f, 70f, 30f), "144"))
        {
            SetTargetFrameRate(144);
        }

        GUI.color = new Color(0.72f, 0.8f, 0.88f, 1f);
        GUI.Label(new Rect(panel.x + 170f, panel.y + 204f, 220f, 24f), "Current: " + targetFrameRate + " FPS");
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 258f, 260f, 36f), "Back"))
        {
            CloseSettings();
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws the end-of-run summary panel.
    private void DrawResultsPanel()
    {
        if (won && victoryResultTexture != null)
        {
            DrawImageResultPanel(victoryResultTexture);
            return;
        }

        if (!won && failedResultTexture != null)
        {
            DrawImageResultPanel(failedResultTexture);
            return;
        }

        DrawOverlayBackdrop(0.74f);
        Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 178f, 520f, 340f);
        DrawPanel(panel, new Color(0.025f, 0.032f, 0.044f, 0.97f), won ? new Color(0.2f, 0.9f, 0.72f, 0.58f) : new Color(1f, 0.18f, 0.12f, 0.58f));

        GUI.color = won ? new Color(0.46f, 1f, 0.78f, 1f) : new Color(1f, 0.32f, 0.24f, 1f);
        GUI.skin.label.fontSize = 34;
        GUI.Label(new Rect(panel.x + 52f, panel.y + 26f, panel.width - 90f, 46f), won ? "SURVIVAL VICTORY" : "RUN FAILED");
        GUI.color = Color.white;

        GUI.skin.label.fontSize = 16;
        GUI.color = new Color(0.78f, 0.86f, 0.92f, 1f);
        GUI.Label(new Rect(panel.x + 74f, panel.y + 104f, 390f, 64f), feedbackText);
        GUI.color = Color.white;

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 204f, 300f, 36f), "Restart Run"))
        {
            StartRun();
        }

        if (GUI.Button(new Rect(panel.x + 110f, panel.y + 252f, 300f, 34f), "Main Menu"))
        {
            ReturnToMainMenu();
        }

        GUI.skin.label.fontSize = 13;
        GUI.color = new Color(0.68f, 0.76f, 0.84f, 1f);
        GUI.Label(new Rect(panel.x + 142f, panel.y + 296f, 260f, 24f), "Enter restarts | Esc returns");
        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws an illustrated result screen and keeps its buttons interactive.
    private void DrawImageResultPanel(Texture2D resultTexture)
    {
        DrawOverlayBackdrop(0.82f);
        Rect imageRect = GetCenteredTextureRect(resultTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(imageRect, resultTexture, ScaleMode.ScaleToFit);

        if (GUI.Button(RelativeRect(imageRect, 0.21f, 0.605f, 0.58f, 0.105f), GUIContent.none, GUIStyle.none))
        {
            StartRun();
        }

        if (GUI.Button(RelativeRect(imageRect, 0.21f, 0.75f, 0.58f, 0.1f), GUIContent.none, GUIStyle.none))
        {
            ReturnToMainMenu();
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws a transparent backdrop behind modal panels.
    private void DrawOverlayBackdrop(float alpha)
    {
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Draws the level-up upgrade selection panel.
    private void DrawUpgradePanel()
    {
        DrawOverlayBackdrop(0.88f);

        bool narrow = Screen.width < 780f || Screen.height < 620f;
        float sidePadding = narrow ? 18f : 28f;
        float panelWidth = Mathf.Min(Mathf.Max(320f, Screen.width - sidePadding), narrow ? 680f : 1320f);
        float panelHeight = narrow ? Mathf.Min(Screen.height - 20f, 540f) : Mathf.Min(Screen.height - 24f, 760f);
        Rect panel = new Rect(Screen.width * 0.5f - panelWidth * 0.5f, Screen.height * 0.5f - panelHeight * 0.5f, panelWidth, panelHeight);
        Color headerAccent = currentChoices.Count > 0 ? currentChoices[0].Accent : new Color(0.3f, 0.86f, 1f, 1f);

        GUI.color = new Color(0.02f, 0.05f, 0.06f, 0.5f);
        GUI.DrawTexture(new Rect(0f, panel.y + 78f, Screen.width, panel.height - 156f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(headerAccent, 0.12f);
        GUI.DrawTexture(new Rect(panel.x - 18f, panel.y - 18f, panel.width + 36f, panel.height + 36f), Texture2D.whiteTexture);
        GUI.color = new Color(0f, 0f, 0f, 0.46f);
        GUI.DrawTexture(new Rect(panel.x + 10f, panel.y + 12f, panel.width, panel.height), Texture2D.whiteTexture);

        DrawPanel(panel, new Color(0.014f, 0.019f, 0.027f, 0.98f), WithAlpha(headerAccent, 0.62f));
        GUI.color = WithAlpha(headerAccent, 0.9f);
        GUI.DrawTexture(new Rect(panel.x + 2f, panel.y + 2f, panel.width - 4f, 4f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(headerAccent, 0.18f);
        GUI.DrawTexture(new Rect(panel.x + 2f, panel.y + 6f, panel.width - 4f, narrow ? 82f : 96f), Texture2D.whiteTexture);

        DrawUpgradeHeader(panel, headerAccent, narrow);

        int choiceCount = Mathf.Max(1, currentChoices.Count);
        float gap = narrow ? 10f : 22f;
        float startX = narrow ? panel.x + 18f : panel.x + 30f;
        float startY = panel.y + (narrow ? 118f : 122f);
        float cardWidth = narrow ? panel.width - 36f : (panel.width - 60f - gap * 2f) / 3f;
        float availableCardHeight = panel.yMax - 24f - startY;
        float cardHeight = narrow ? Mathf.Clamp((availableCardHeight - gap * (choiceCount - 1)) / choiceCount, 84f, 102f) : availableCardHeight;

        for (int i = 0; i < currentChoices.Count; i++)
        {
            Rect rect = narrow
                ? new Rect(startX, startY + i * (cardHeight + gap), cardWidth, cardHeight)
                : new Rect(startX + i * (cardWidth + gap), startY, cardWidth, cardHeight);
            DrawUpgradeCard(rect, currentChoices[i], i, narrow);
        }

        GUI.skin.label.fontSize = 18;
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUI.skin.label.wordWrap = false;
        GUI.color = Color.white;
    }

    // Draws one upgrade card with icon, title, description, and pick button.
    private void DrawUpgradeCard(Rect rect, ZombieStormUpgradeOption option, int index, bool compact)
    {
        Event currentEvent = Event.current;
        bool hover = rect.Contains(currentEvent.mousePosition);
        Color accent = option.Accent;
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.8f + index * 0.7f) * 0.5f;
        Color edge = WithAlpha(accent, hover ? 1f : 0.62f);
        Color fill = hover ? new Color(0.046f, 0.058f, 0.074f, 0.99f) : new Color(0.027f, 0.035f, 0.05f, 0.98f);
        Rect drawRect = hover && !compact ? new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f) : rect;
        Texture2D cardTemplate = GetUpgradeCardTemplate(option);
        bool useTemplateArt = cardTemplate != null && !compact;

        GUI.color = new Color(0f, 0f, 0f, 0.48f);
        GUI.DrawTexture(new Rect(rect.x + 7f, rect.y + 9f, rect.width, rect.height), Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, hover ? 0.2f : 0.08f);
        GUI.DrawTexture(new Rect(drawRect.x - 5f, drawRect.y - 5f, drawRect.width + 10f, drawRect.height + 10f), Texture2D.whiteTexture);

        if (useTemplateArt)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(drawRect, cardTemplate, ScaleMode.StretchToFill, true);
            GUI.color = WithAlpha(accent, hover ? 0.12f + pulse * 0.05f : 0.04f);
            GUI.DrawTexture(new Rect(drawRect.x + 18f, drawRect.y + 18f, drawRect.width - 36f, drawRect.height - 36f), Texture2D.whiteTexture);
        }
        else
        {
            DrawPanel(drawRect, fill, edge);

            GUI.color = WithAlpha(accent, hover ? 0.3f : 0.16f);
            GUI.DrawTexture(new Rect(drawRect.x + 2f, drawRect.y + 2f, drawRect.width - 4f, compact ? 40f : 84f), Texture2D.whiteTexture);
            GUI.color = WithAlpha(accent, hover ? 1f : 0.76f);
            GUI.DrawTexture(new Rect(drawRect.x + 2f, drawRect.y + 2f, 5f, drawRect.height - 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(drawRect.x + 14f, drawRect.y + 16f, compact ? 54f : 78f, 2f), Texture2D.whiteTexture);
            GUI.color = WithAlpha(accent, hover ? 0.42f + pulse * 0.2f : 0.26f);
            GUI.DrawTexture(new Rect(drawRect.x + drawRect.width - 44f, drawRect.y + 12f, 22f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(drawRect.x + drawRect.width - 24f, drawRect.y + 12f, 2f, 22f), Texture2D.whiteTexture);
        }

        if (!useTemplateArt)
        {
            Rect hotkey = compact ? new Rect(rect.x + 14f, rect.y + 12f, 34f, 34f) : new Rect(rect.x + 18f, rect.y + 20f, 44f, 44f);
            DrawUpgradeHotkey(hotkey, index + 1, accent, hover);
        }

        if (!useTemplateArt)
        {
            Rect icon = compact ? new Rect(rect.x + rect.width - 54f, rect.y + 12f, 34f, 34f) : new Rect(rect.x + rect.width * 0.5f - 35f, rect.y + 26f, 70f, 70f);
            DrawUpgradeIcon(icon, option, accent, hover);
        }

        UpgradeCardTextLayout layout = useTemplateArt ? GetUpgradeCardTextLayout(cardTemplate) : UpgradeCardTextLayout.Default;
        float textX = useTemplateArt ? drawRect.x + drawRect.width * layout.TextInset : compact ? rect.x + 58f : rect.x + 20f;
        float textWidth = useTemplateArt ? drawRect.width * (1f - layout.TextInset * 2f) : compact ? rect.width - 122f : rect.width - 40f;
        float titleY = useTemplateArt ? drawRect.y + drawRect.height * layout.TitleY : compact ? rect.y + 10f : rect.y + 112f;

        if (useTemplateArt)
        {
            int titleFontSize = option.Title.Length > 28 ? layout.LongTitleFontSize : layout.TitleFontSize;
            GUIStyle titleStyle = CreateUpgradeCardTextStyle(titleFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, layout.TitleWrap, upgradeCardTitleFont);
            GUIStyle kindStyle = CreateUpgradeCardTextStyle(layout.KindFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, false, upgradeCardTitleFont);
            GUIStyle bodyStyle = CreateUpgradeCardTextStyle(layout.BodyFontSize, FontStyle.Normal, TextAnchor.MiddleCenter, true, upgradeCardBodyFont);
            GUIStyle buttonStyle = CreateUpgradeCardTextStyle(layout.ButtonFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, false, upgradeCardTitleFont);
            GUIStyle hotkeyStyle = CreateUpgradeCardTextStyle(11, FontStyle.Bold, TextAnchor.MiddleCenter, false, upgradeCardBodyFont);
            string cardDescription = FormatUpgradeCardDescription(option.Description);

            DrawShadowedUpgradeLabel(new Rect(drawRect.x + 22f, drawRect.y + 22f, 30f, 28f), (index + 1).ToString(), hotkeyStyle, new Color(0.95f, 0.66f, 0.28f, 0.92f), new Color(0.02f, 0f, 0f, 0.7f), new Vector2(1f, 1f));
            DrawShadowedUpgradeLabel(new Rect(drawRect.x + drawRect.width * layout.TitleInset, titleY, drawRect.width * (1f - layout.TitleInset * 2f), drawRect.height * layout.TitleHeight), option.Title, titleStyle, new Color(0.98f, 0.94f, 0.82f, 1f), new Color(0.07f, 0.015f, 0f, 0.82f), new Vector2(1.5f, 1.5f));
            DrawShadowedUpgradeLabel(new Rect(drawRect.x + drawRect.width * layout.KindInset, drawRect.y + drawRect.height * layout.KindY, drawRect.width * (1f - layout.KindInset * 2f), drawRect.height * layout.KindHeight), GetUpgradeKindLabel(option), kindStyle, new Color(1f, 0.82f, 0.52f, 1f), new Color(0.08f, 0.015f, 0f, 0.76f), new Vector2(1.1f, 1.1f));
            DrawShadowedUpgradeLabel(new Rect(textX + drawRect.width * layout.DescriptionInset, drawRect.y + drawRect.height * (layout.DescriptionY + layout.DescriptionTextOffset), textWidth - drawRect.width * layout.DescriptionInset * 2f, drawRect.height * layout.DescriptionHeight), cardDescription, bodyStyle, new Color(0.96f, 0.9f, 0.78f, 1f), new Color(0f, 0f, 0f, 0.8f), new Vector2(1f, 1f));

            if (!compact)
            {
                Rect button = new Rect(drawRect.x + drawRect.width * layout.ButtonInset, drawRect.y + drawRect.height * (layout.ButtonY + layout.ButtonTextOffset), drawRect.width * (1f - layout.ButtonInset * 2f), drawRect.height * layout.ButtonHeight);
                DrawShadowedUpgradeLabel(button, "SELECT " + (index + 1), buttonStyle, new Color(1f, 0.9f, 0.72f, 1f), new Color(0.06f, 0f, 0f, 0.86f), new Vector2(1.4f, 1.4f));
            }
        }
        else
        {
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUI.skin.label.wordWrap = true;
        GUI.skin.label.fontSize = compact ? (option.Title.Length > 24 ? 14 : 16) : (option.Title.Length > 24 ? 17 : 20);
        GUI.color = new Color(0.08f, 0.02f, 0f, 0.72f);
        GUI.Label(new Rect(textX + 2f, titleY + 2f, textWidth, compact ? 24f : 50f), option.Title);
        GUI.color = Color.white;
        GUI.Label(new Rect(textX, titleY, textWidth, compact ? 24f : 50f), option.Title);

        GUI.skin.label.fontSize = 11;
        GUI.color = accent;
        GUI.Label(new Rect(textX, compact ? rect.y + 36f : rect.y + 162f, textWidth, 18f), GetUpgradeKindLabel(option));

        GUI.skin.label.fontSize = compact ? 13 : 14;
        GUI.color = new Color(0.76f, 0.84f, 0.91f, 1f);
        GUI.Label(new Rect(textX + 4f, compact ? rect.y + 54f : rect.y + 188f, textWidth - 8f, compact ? 34f : 60f), option.Description);
        }

        if (!compact && !useTemplateArt)
        {
            DrawUpgradeEnergyTicks(new Rect(rect.x + 24f, rect.yMax - 66f, rect.width - 48f, 10f), accent, index, hover);
        }

        if (!compact && !useTemplateArt)
        {
            Rect button = new Rect(rect.x + 26f, rect.yMax - 44f, rect.width - 52f, 30f);
            DrawUpgradePickButton(button, index + 1, accent, hover);
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition))
        {
            currentEvent.Use();
            ApplyUpgrade(index);
        }

        GUI.skin.label.wordWrap = false;
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }

    // Creates a card-specific text style so upgrade art does not look like raw debug UI.
    private GUIStyle CreateUpgradeCardTextStyle(int fontSize, FontStyle fontStyle, TextAnchor alignment, bool wordWrap, Font font)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.font = font;
        style.fontSize = fontSize;
        style.fontStyle = fontStyle;
        style.alignment = alignment;
        style.wordWrap = wordWrap;
        style.clipping = TextClipping.Clip;
        style.padding = new RectOffset(0, 0, 0, 0);
        style.normal.textColor = Color.white;
        return style;
    }

    // Draws readable card text with a soft shadow instead of hard programmatic labels.
    private void DrawShadowedUpgradeLabel(Rect rect, string text, GUIStyle style, Color color, Color shadowColor, Vector2 shadowOffset)
    {
        GUI.color = shadowColor;
        GUI.Label(new Rect(rect.x + shadowOffset.x, rect.y + shadowOffset.y, rect.width, rect.height), text, style);
        GUI.color = color;
        GUI.Label(rect, text, style);
    }

    // Selects the card art template that matches the skill family.
    private Texture2D GetUpgradeCardTemplate(ZombieStormUpgradeOption option)
    {
        if (option == null || string.IsNullOrEmpty(option.Key))
        {
            return upgradeCardTemplateTexture;
        }

        string key = option.Key;
        if (IsDamageUpgradeCard(key))
        {
            return damageCardTemplateTexture != null ? damageCardTemplateTexture : upgradeCardTemplateTexture;
        }

        if (IsCooldownUpgradeCard(key))
        {
            return cooldownCardTemplateTexture != null ? cooldownCardTemplateTexture : upgradeCardTemplateTexture;
        }

        if (IsExperienceUpgradeCard(key))
        {
            return xpCardTemplateTexture != null ? xpCardTemplateTexture : upgradeCardTemplateTexture;
        }

        if (key.Contains("MagicBolt") || key.Contains("magic_"))
        {
            return magicBoltCardTemplateTexture != null ? magicBoltCardTemplateTexture : upgradeCardTemplateTexture;
        }

        if (key.Contains("OrbitingKnife") || key.Contains("knife_"))
        {
            return fireBladesCardTemplateTexture != null ? fireBladesCardTemplateTexture : upgradeCardTemplateTexture;
        }

        if (key.Contains("FireZone"))
        {
            return fireZoneCardTemplateTexture != null ? fireZoneCardTemplateTexture : upgradeCardTemplateTexture;
        }

        if (key.Contains("Regeneration") || key.Contains("regen_") || key.Contains("passive_MaxHealth"))
        {
            return regenerationCardTemplateTexture != null ? regenerationCardTemplateTexture : upgradeCardTemplateTexture;
        }

        if (key.Contains("UltimateStorm") || key.Contains("ultimate_"))
        {
            return stormCardTemplateTexture != null ? stormCardTemplateTexture : upgradeCardTemplateTexture;
        }

        return upgradeCardTemplateTexture;
    }

    // Checks whether an upgrade should use the attack-up card template.
    private static bool IsDamageUpgradeCard(string key)
    {
        return key.Contains("magic_force")
            || key.Contains("knife_edge")
            || key.Contains("drone_focus")
            || key.Contains("shield_force")
            || key.Contains("ultimate_voltage")
            || key.Contains("passive_Damage")
            || key.Contains("passive_Crit")
            || key.Contains("passive_Area");
    }

    // Checks whether an upgrade should use the cooldown / time card template.
    private static bool IsCooldownUpgradeCard(string key)
    {
        return key.Contains("drone_overclock")
            || key.Contains("shield_recharge")
            || key.Contains("ultimate_recharge")
            || key.Contains("passive_FireRate")
            || key.Contains("passive_MoveSpeed");
    }

    // Checks whether an upgrade should use the experience card template.
    private static bool IsExperienceUpgradeCard(string key)
    {
        return key.Contains("passive_PickupRange");
    }

    // Returns text slot positions for each generated card template family.
    private UpgradeCardTextLayout GetUpgradeCardTextLayout(Texture2D cardTemplate)
    {
        UpgradeCardTextLayout layout = UpgradeCardTextLayout.Default;

        if (cardTemplate == damageCardTemplateTexture)
        {
            layout.TitleInset = 0.16f;
            layout.KindInset = 0.16f;
            layout.DescriptionInset = 0.08f;
            layout.TitleY = 0.474f;
            layout.TitleHeight = 0.082f;
            layout.KindY = 0.584f;
            layout.DescriptionY = 0.664f;
            layout.DescriptionTextOffset = -0.012f;
            layout.ButtonY = 0.868f;
            layout.ButtonTextOffset = 0.004f;
        }
        else if (cardTemplate == cooldownCardTemplateTexture)
        {
            layout.TitleInset = 0.16f;
            layout.KindInset = 0.16f;
            layout.DescriptionInset = 0.08f;
            layout.TitleY = 0.406f;
            layout.TitleHeight = 0.084f;
            layout.KindY = 0.532f;
            layout.DescriptionY = 0.612f;
            layout.DescriptionTextOffset = 0f;
            layout.ButtonY = 0.858f;
            layout.ButtonTextOffset = 0f;
            layout.ButtonHeight = 0.072f;
        }
        else if (cardTemplate == xpCardTemplateTexture)
        {
            layout.TitleInset = 0.16f;
            layout.KindInset = 0.16f;
            layout.DescriptionInset = 0.08f;
            layout.TitleY = 0.448f;
            layout.TitleHeight = 0.084f;
            layout.KindY = 0.58f;
            layout.DescriptionY = 0.666f;
            layout.DescriptionTextOffset = -0.012f;
            layout.ButtonY = 0.868f;
            layout.ButtonTextOffset = 0.004f;
        }
        else if (cardTemplate == magicBoltCardTemplateTexture)
        {
            layout.TitleY = 0.405f;
            layout.KindY = 0.532f;
            layout.DescriptionY = 0.638f;
            layout.DescriptionTextOffset = -0.014f;
            layout.ButtonY = 0.872f;
            layout.ButtonTextOffset = -0.014f;
        }
        else if (cardTemplate == upgradeCardTemplateTexture)
        {
            layout.TitleY = 0.405f;
            layout.KindY = 0.532f;
            layout.DescriptionY = 0.628f;
            layout.DescriptionTextOffset = -0.012f;
            layout.ButtonY = 0.87f;
            layout.ButtonTextOffset = -0.014f;
        }
        else if (cardTemplate == fireBladesCardTemplateTexture)
        {
            layout.TitleY = 0.462f;
            layout.KindY = 0.576f;
            layout.DescriptionY = 0.664f;
            layout.DescriptionTextOffset = -0.014f;
            layout.ButtonY = 0.87f;
            layout.ButtonTextOffset = 0.018f;
        }
        else if (cardTemplate == fireZoneCardTemplateTexture)
        {
            layout.TitleY = 0.535f;
            layout.TitleHeight = 0.088f;
            layout.KindY = 0.666f;
            layout.KindHeight = 0.056f;
            layout.DescriptionY = 0.742f;
            layout.DescriptionTextOffset = 0f;
            layout.DescriptionHeight = 0.168f;
            layout.ButtonY = 0.908f;
            layout.ButtonTextOffset = 0f;
            layout.ButtonHeight = 0.07f;
        }
        else if (cardTemplate == regenerationCardTemplateTexture)
        {
            layout.TitleY = 0.46f;
            layout.KindY = 0.57f;
            layout.DescriptionY = 0.666f;
            layout.DescriptionTextOffset = -0.012f;
            layout.ButtonY = 0.878f;
            layout.ButtonTextOffset = 0.004f;
        }
        else if (cardTemplate == stormCardTemplateTexture)
        {
            layout.TextInset = 0.15f;
            layout.TitleInset = 0.13f;
            layout.KindInset = 0.16f;
            layout.DescriptionInset = 0.09f;
            layout.ButtonInset = 0.2f;
            layout.TitleY = 0.455f;
            layout.TitleHeight = 0.1f;
            layout.KindY = 0.58f;
            layout.KindHeight = 0.055f;
            layout.DescriptionY = 0.67f;
            layout.DescriptionTextOffset = -0.012f;
            layout.DescriptionHeight = 0.19f;
            layout.ButtonY = 0.862f;
            layout.ButtonTextOffset = 0.016f;
            layout.ButtonHeight = 0.07f;
            layout.TitleFontSize = 15;
            layout.LongTitleFontSize = 13;
            layout.TitleWrap = true;
            layout.KindFontSize = 11;
            layout.BodyFontSize = 14;
            layout.ButtonFontSize = 15;
        }

        return layout;
    }

    // Stores normalized card text slots so different card art templates can align independently.
    private struct UpgradeCardTextLayout
    {
        public float TextInset;
        public float TitleInset;
        public float KindInset;
        public float DescriptionInset;
        public float ButtonInset;
        public float TitleY;
        public float TitleHeight;
        public int TitleFontSize;
        public int LongTitleFontSize;
        public bool TitleWrap;
        public float KindY;
        public float KindHeight;
        public int KindFontSize;
        public float DescriptionY;
        public float DescriptionTextOffset;
        public float DescriptionHeight;
        public int BodyFontSize;
        public float ButtonY;
        public float ButtonTextOffset;
        public float ButtonHeight;
        public int ButtonFontSize;

        public static UpgradeCardTextLayout Default
        {
            get
            {
                return new UpgradeCardTextLayout
                {
                    TextInset = 0.14f,
                    TitleInset = 0.14f,
                    KindInset = 0.14f,
                    DescriptionInset = 0.07f,
                    ButtonInset = 0.18f,
                    TitleY = 0.405f,
                    TitleHeight = 0.075f,
                    TitleFontSize = 19,
                    LongTitleFontSize = 15,
                    TitleWrap = false,
                    KindY = 0.532f,
                    KindHeight = 0.058f,
                    KindFontSize = 12,
                    DescriptionY = 0.628f,
                    DescriptionTextOffset = -0.012f,
                    DescriptionHeight = 0.23f,
                    BodyFontSize = 16,
                    ButtonY = 0.87f,
                    ButtonTextOffset = -0.014f,
                    ButtonHeight = 0.078f,
                    ButtonFontSize = 16
                };
            }
        }
    }

    // Makes compact card descriptions read like two-line card copy.
    private static string FormatUpgradeCardDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        int colonIndex = description.IndexOf(": ", StringComparison.Ordinal);
        if (colonIndex > 0 && description.Length > 28)
        {
            return description.Substring(0, colonIndex + 1) + "\n" + description.Substring(colonIndex + 2);
        }

        if (description.Length <= 34)
        {
            return description;
        }

        int midpoint = description.Length / 2;
        int bestBreak = description.IndexOf(' ', midpoint);
        if (bestBreak < 0 || bestBreak > midpoint + 12)
        {
            bestBreak = description.LastIndexOf(' ', midpoint);
        }

        return bestBreak > 0 ? description.Substring(0, bestBreak) + "\n" + description.Substring(bestBreak + 1) : description;
    }

    // Draws the title area at the top of the upgrade panel.
    private void DrawUpgradeHeader(Rect panel, Color accent, bool compact)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.22f);
        GUI.DrawTexture(new Rect(panel.x + 16f, panel.y + 18f, panel.width - 32f, compact ? 84f : 94f), Texture2D.whiteTexture);

        GUI.skin.label.alignment = TextAnchor.UpperLeft;
        GUI.skin.label.wordWrap = false;
        GUI.color = WithAlpha(accent, 0.9f);
        GUI.skin.label.fontSize = compact ? 11 : 12;
        GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, 240f, 20f), "SURVIVOR UPGRADE");

        GUI.color = new Color(1f, 0.88f, 0.32f, 1f);
        GUI.skin.label.fontSize = compact ? 28 : 36;
        GUI.Label(new Rect(panel.x + 28f, panel.y + 40f, panel.width - 56f, 48f), "LEVEL UP");

        GUI.color = new Color(0.76f, 0.86f, 0.94f, 1f);
        GUI.skin.label.fontSize = compact ? 12 : 14;
        GUI.Label(new Rect(panel.x + 30f, panel.y + (compact ? 76f : 88f), panel.width - 60f, 24f), "Choose one upgrade with 1 / 2 / 3 or click a card.");

        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws the number-key hint on an upgrade card.
    private void DrawUpgradeHotkey(Rect rect, int number, Color accent, bool hover)
    {
        GUI.color = WithAlpha(accent, hover ? 0.92f : 0.68f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.015f, 0.022f, 0.03f, 0.92f);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = Mathf.RoundToInt(rect.height * 0.48f);
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(rect, number.ToString());
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws the colored icon area for an upgrade card.
    private void DrawUpgradeIcon(Rect rect, ZombieStormUpgradeOption option, Color accent, bool hover)
    {
        GUI.color = WithAlpha(accent, hover ? 0.22f : 0.14f);
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), Texture2D.whiteTexture);
        GUI.color = new Color(0.011f, 0.018f, 0.026f, 0.94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, 0.95f);
        GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + 4f, rect.yMax - 7f, rect.width - 8f, 3f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + 7f, 3f, rect.height - 14f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 7f, rect.y + 7f, 3f, rect.height - 14f), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.skin.label.fontSize = Mathf.RoundToInt(rect.height * 0.42f);
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(rect, GetUpgradeIconText(option));
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws the pick button at the bottom of an upgrade card.
    private void DrawUpgradePickButton(Rect rect, int number, Color accent, bool hover)
    {
        GUI.color = WithAlpha(accent, hover ? 0.92f : 0.58f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = hover ? new Color(0.03f, 0.045f, 0.06f, 0.88f) : new Color(0.018f, 0.026f, 0.036f, 0.88f);
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = WithAlpha(accent, hover ? 0.88f : 0.42f);
        GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 7f, 18f, rect.height - 14f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.skin.label.fontSize = 13;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(rect, "SELECT " + number);
        GUI.skin.label.alignment = TextAnchor.UpperLeft;
    }

    // Draws decorative energy ticks on an upgrade card.
    private void DrawUpgradeEnergyTicks(Rect rect, Color accent, int index, bool hover)
    {
        int tickCount = 7;
        float gap = 5f;
        float tickWidth = (rect.width - gap * (tickCount - 1)) / tickCount;
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 5.4f + index) * 0.5f;

        for (int i = 0; i < tickCount; i++)
        {
            float alpha = hover ? 0.5f + pulse * 0.4f : 0.22f + i * 0.045f;
            GUI.color = WithAlpha(accent, Mathf.Clamp01(alpha));
            GUI.DrawTexture(new Rect(rect.x + i * (tickWidth + gap), rect.y, tickWidth, rect.height), Texture2D.whiteTexture);
        }
    }

    // Returns the category label shown on an upgrade card.
    private static string GetUpgradeKindLabel(ZombieStormUpgradeOption option)
    {
        if (option.Key != null && option.Key.StartsWith("unlock_", StringComparison.Ordinal))
        {
            return "NEW ACTIVE SKILL";
        }

        if (option.Key != null && option.Key.StartsWith("level_", StringComparison.Ordinal))
        {
            return "SKILL LEVEL UP";
        }

        if (option.Key != null && option.Key.StartsWith("passive_", StringComparison.Ordinal))
        {
            return "PASSIVE STAT";
        }

        return option.Category;
    }

    // Returns the short icon text for an upgrade option.
    private static string GetUpgradeIconText(ZombieStormUpgradeOption option)
    {
        string key = option.Key ?? string.Empty;
        if (key.Contains("MagicBolt") || key.Contains("magic_"))
        {
            return "MB";
        }

        if (key.Contains("OrbitingKnife") || key.Contains("knife_"))
        {
            return "FB";
        }

        if (key.Contains("Regeneration") || key.Contains("regen_") || key.Contains("MaxHealth"))
        {
            return key.Contains("MaxHealth") ? "HP" : "RG";
        }

        if (key.Contains("FireZone") || key.Contains("fire_"))
        {
            return "FZ";
        }

        if (key.Contains("SummonDrone") || key.Contains("drone_"))
        {
            return "FS";
        }

        if (key.Contains("ShieldBurst") || key.Contains("shield_"))
        {
            return "SH";
        }

        if (key.Contains("UltimateStorm") || key.Contains("ultimate_"))
        {
            return "UL";
        }

        if (key.Contains("Damage"))
        {
            return "AT";
        }

        if (key.Contains("FireRate"))
        {
            return "AS";
        }

        if (key.Contains("MoveSpeed"))
        {
            return "MS";
        }

        if (key.Contains("PickupRange"))
        {
            return "XP";
        }

        if (key.Contains("MaxHealth"))
        {
            return "HP";
        }

        if (key.Contains("Crit"))
        {
            return "CR";
        }

        if (key.Contains("Area"))
        {
            return "AR";
        }

        return "UP";
    }

    // Draws a labeled progress bar such as health or experience.
    private void DrawBar(Rect rect, float value, Color color, string label)
    {
        value = Mathf.Clamp01(value);
        GUI.color = new Color(0.01f, 0.012f, 0.016f, 0.82f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.16f);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f) * value, rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 8f, rect.y - 2f, rect.width, rect.height + 6f), label);
    }

    // Draws the art-backed player status card with dynamic HP and XP fills.
    private void DrawPlayerStatusHud(Rect rect)
    {
        if (Player == null)
        {
            return;
        }

        if (playerStatusCardTexture == null)
        {
            DrawBar(new Rect(rect.x + 16f, rect.y + 54f, rect.width - 34f, 20f), Player.Health / Player.MaxHealth, new Color(0.92f, 0.16f, 0.12f), string.Empty);
            DrawBar(new Rect(rect.x + 16f, rect.y + 86f, rect.width - 34f, 20f), Player.Experience / Mathf.Max(1f, Player.ExperienceToNext), new Color(0.18f, 0.74f, 1f), string.Empty);
            return;
        }

        float aspect = playerStatusCardTexture.width / (float)Mathf.Max(1, playerStatusCardTexture.height);
        Rect card = new Rect(rect.x, rect.y, rect.width, rect.width / aspect);
        if (card.height > rect.height)
        {
            card.height = rect.height;
            card.width = card.height * aspect;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.28f);
        GUI.DrawTexture(new Rect(card.x + 6f, card.y + 8f, card.width, card.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(card, playerStatusCardTexture, ScaleMode.StretchToFill, true);

        Rect hpSlot = new Rect(card.x + card.width * 0.218f, card.y + card.height * 0.26f, card.width * 0.7f, card.height * 0.18f);
        Rect xpSlot = new Rect(card.x + card.width * 0.218f, card.y + card.height * 0.625f, card.width * 0.7f, card.height * 0.18f);
        DrawHudFill(hpSlot, Player.Health / Player.MaxHealth, new Color(0.95f, 0.12f, 0.08f, 0.88f), new Color(1f, 0.64f, 0.18f, 0.58f));
        DrawHudFill(xpSlot, Player.Experience / Mathf.Max(1f, Player.ExperienceToNext), new Color(0.14f, 0.58f, 1f, 0.86f), new Color(0.54f, 0.95f, 1f, 0.54f));
    }

    // Draws a soft, readable fill inside the art card's empty bar slot.
    private static void DrawHudFill(Rect slot, float value, Color fill, Color shine)
    {
        value = Mathf.Clamp01(value);
        Rect inner = new Rect(slot.x + 5f, slot.y + 5f, Mathf.Max(0f, slot.width - 10f), Mathf.Max(0f, slot.height - 10f));
        GUI.color = new Color(0f, 0f, 0f, 0.22f);
        GUI.DrawTexture(inner, Texture2D.whiteTexture);

        if (value > 0f)
        {
            Rect filled = new Rect(inner.x, inner.y, inner.width * value, inner.height);
            GUI.color = fill;
            GUI.DrawTexture(filled, Texture2D.whiteTexture);
            GUI.color = shine;
            GUI.DrawTexture(new Rect(filled.x, filled.y + 2f, filled.width, Mathf.Max(2f, filled.height * 0.32f)), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.DrawTexture(new Rect(filled.xMax - 2f, filled.y, 2f, filled.height), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }

    // Draws a screen overlay that adds combat atmosphere.
    private void DrawAtmosphereOverlay()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.18f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 34f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, Screen.height - 42f, Screen.width, 42f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0f, 0f, 34f, Screen.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - 34f, 0f, 34f, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Draws screen flash feedback for damage and impacts.
    private void DrawScreenFlash()
    {
        if (screenFlash <= 0f)
        {
            return;
        }

        GUI.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, Mathf.Clamp01(screenFlash) * 0.24f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // Draws floating damage numbers above enemies.
    private void DrawDamagePopups()
    {
        if (mainCamera == null)
        {
            return;
        }

        for (int i = 0; i < damagePopups.Count; i++)
        {
            ZombieStormDamagePopup popup = damagePopups[i];
            Vector3 screen = mainCamera.WorldToScreenPoint(popup.WorldPosition);
            if (screen.z < 0f)
            {
                continue;
            }

            GUI.skin.label.fontSize = popup.Size;
            Color color = popup.Color;
            color.a = Mathf.Clamp01(popup.TimeLeft * 2f);
            GUI.color = Color.black;
            GUI.Label(new Rect(screen.x - 17f, Screen.height - screen.y - 17f, 90f, 30f), popup.Text);
            GUI.color = color;
            GUI.Label(new Rect(screen.x - 18f, Screen.height - screen.y - 18f, 90f, 30f), popup.Text);
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws the boss name and health bar.
    private void DrawBossBar()
    {
        ZombieStormEnemy boss = null;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].IsBoss && !enemies[i].IsDead)
            {
                boss = enemies[i];
                break;
            }
        }

        if (boss == null)
        {
            return;
        }

        Rect rect = new Rect(Screen.width * 0.5f - 260f, Screen.height - 58f, 520f, 24f);
        Color accent = BossUiAccent(boss.Type);
        DrawPanel(new Rect(rect.x - 10f, rect.y - 28f, rect.width + 20f, 58f), new Color(0.04f, 0.018f, 0.018f, 0.82f), WithAlpha(accent, 0.5f));
        GUI.skin.label.fontSize = 16;
        GUI.color = accent;
        GUI.Label(new Rect(rect.x, rect.y - 24f, rect.width, 22f), "BOSS " + boss.DisplayName.ToUpperInvariant());
        DrawBar(rect, boss.Health01, accent, Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaxHealth));
        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws screen markers that make elite enemies easier to spot.
    private void DrawEliteMarkers()
    {
        if (mainCamera == null)
        {
            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            ZombieStormEnemy enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.Type != ZombieStormEnemyType.Elite)
            {
                continue;
            }

            Vector3 screen = mainCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.2f);
            if (screen.z < 0f)
            {
                continue;
            }

            GUI.skin.label.fontSize = 14;
            GUI.color = new Color(1f, 0.68f, 0.18f, 0.95f);
            GUI.Label(new Rect(screen.x - 25f, Screen.height - screen.y - 10f, 80f, 24f), "ELITE");
        }

        GUI.skin.label.fontSize = 18;
        GUI.color = Color.white;
    }

    // Draws a bordered UI panel background.
    private void DrawPanel(Rect rect, Color fill, Color edge)
    {
        GUI.color = fill;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = edge;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
