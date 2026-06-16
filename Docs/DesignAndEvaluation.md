# Zombie Storm Design and Evaluation Evidence

This document summarizes the evidence that supports three assessment areas:
testing/debugging/improvement, awareness of wider issues, and clear design and
development decisions.

## Assessment Evidence Map

| Assessment area | Evidence in this project |
| --- | --- |
| Testing, debugging, and improvement | `Docs/TestingAndDebugging.md`, `Docs/TestingLogs`, successful Unity compilation, Windows build summary, startup smoke test, defect log, and regression matrix |
| Legal, ethical, social, accessibility, and security awareness | Centralized asset references, local-only settings, no network or personal-data collection, volume controls, pause support, readable warning effects, and content-safety notes |
| Design and development decisions | Modular script split, object pooling, automatic skill casting, telegraphed boss attacks, runtime content packaging, Canvas main menu, fallback UI/art, and difficulty pacing |

## Testing, Debugging, and Improvement

The project includes a dedicated [Testing and Debugging Report](TestingAndDebugging.md).
It records the testing approach, environment, verified results, defect log,
manual regression plan, and release checklist.

### Verified Evidence

| Evidence | What it proves |
| --- | --- |
| Unity script compilation | The C# scripts compile in the Unity project environment. |
| Windows standalone build | The project can be packaged as a Windows player. |
| Runtime content packaging | Required PNG runtime content is copied into StreamingAssets for builds. |
| Standalone startup smoke test | The built player starts without an immediate managed exception or process crash. |
| Scene configuration check | The active project scene and Build Settings point to `ZombieStorm.unity`. |
| Repository scan for local paths | Runtime loading no longer depends on a user-specific Downloads directory. |

### Debugging Evidence

Important debugging and improvement work already recorded in the project:

- Fixed standalone build asset loading by moving player-build content to
  `Application.streamingAssetsPath/ZombieStormContent`.
- Removed a developer-machine Downloads path from runtime resource loading.
- Split the original large runtime controller into responsibility-focused
  partial scripts such as audio, resources, upgrades, and legacy GUI.
- Renamed the template scene from `SampleScene` to `ZombieStorm` for clearer
  project presentation.
- Tuned combat audio so background music drops to 60% of the configured music
  level during active gameplay.
- Increased the Orc Thrower's post-throw interval to reduce ranged pressure.
- Clarified that the main menu uses Canvas + TextMeshPro while OnGUI remains
  for lightweight runtime overlays and fallback panels.
- Removed unused prototype art sets that were not referenced by the game.
- Updated documentation so enemies, bosses, controls, screenshots, story, and
  gameplay descriptions match the current implementation.

### Regression Plan

The project keeps a manual regression matrix for startup, player movement,
combat, skills, upgrades, enemies, bosses, audio, resources, UI, and build
packaging. Planned items are intentionally marked as planned unless they have
direct evidence. This avoids overstating unverified tests while still showing
what should be checked before a demonstration or release.

## Wider Issues

### Legal and Asset Attribution

The project keeps a centralized [Asset Credits and References](AssetReferences.md)
file. It records art, audio, fonts, screenshots, source archives, draft art, and
runtime-generated fallback visuals.

The main development decision here is traceability: each major asset group has a
local path, source or source record, project use, and attribution note. This
makes it easier to explain where assets came from and how they are used in the
game.

### Ethical and Social Considerations

Zombie Storm is a fantasy survival game with cartoon zombie combat. The design
keeps the tone non-realistic and uses stylized enemies, magic effects, and
arcade feedback instead of realistic injury or graphic violence.

The game does not include chat, accounts, telemetry, advertisements, gambling
systems, loot boxes, or real-money purchases. This reduces social and privacy
risks for a course project.

### Accessibility Considerations

Current accessibility-supporting choices:

- Keyboard controls are simple and listed in the README.
- Skills auto-cast, so the player can focus on movement and upgrade decisions.
- Boss attacks use visible warning effects before damage is applied.
- The player can pause the game.
- Settings include master, music, and sound-effect volume controls.
- Runtime music is quieter than menu music so combat feedback is easier to hear.
- The HUD uses large health/experience bars and high-contrast upgrade cards.

Known accessibility improvements for a larger version:

- Add key rebinding.
- Add colorblind-friendly effect palettes.
- Add an option to reduce screen shake and flashing effects.
- Add scalable UI text.
- Add controller support.

### Security and Privacy Considerations

The game is offline and does not intentionally collect or transmit personal
data. Settings are stored locally through Unity `PlayerPrefs`, including volume
and fullscreen preferences.

Resource loading was improved to remove user-specific absolute paths. Build
content now uses project-owned and player-build-owned locations, which improves
portability and reduces accidental disclosure of local machine paths.

## Design and Development Decisions

### Game Structure

Zombie Storm is designed as a five-minute 2D survival action game. The short
session length makes the game easy to demonstrate in class while still allowing
enemy escalation, multiple upgrade choices, and boss encounters.

### Automatic Skill Casting

The player controls movement while learned skills cast automatically. This
decision reduces input complexity and makes the main gameplay loop clearer:
survive, collect magic orbs, level up, choose upgrades, and adapt the build.

### Upgrade System

The upgrade system offers three choices after leveling up. Choices can unlock
new skills, improve existing skills, or strengthen passive stats. Follow-up
options are biased toward owned skills so the player can build a more coherent
play style instead of receiving only unrelated upgrades.

### Enemy and Boss Design

Ordinary enemies have different roles:

- standard pursuers create constant pressure;
- fast enemies force movement;
- melee attackers punish close range;
- heavy enemies take longer to kill;
- ranged throwers pressure the player from distance;
- bosses introduce larger warning patterns and arena control.

Boss attacks are telegraphed before damage. This is both a fairness decision and
an accessibility decision: players can read the danger zone and react instead of
taking unavoidable damage.

### Difficulty Pacing

The runtime controller increases pressure over time through a dynamic difficulty
score, spawn timing, enemy selection, and scheduled boss waves. The first boss
appears early enough to show the boss system during a short play session, while
later waves introduce stronger enemy mixes and higher pressure.

### Object Pooling

The project uses pooled GameObjects for enemies, projectiles, effects, pickups,
and temporary combat feedback. This avoids repeatedly creating and destroying
large numbers of short-lived objects during horde combat, which helps runtime
performance and keeps frame pacing more stable.

### Resource Loading and Build Packaging

Prototype loading originally depended on editor-style paths. The improved design
separates editor source content from player build content:

- in the editor, assets can be loaded from project content;
- in a standalone player, external runtime images are copied to StreamingAssets;
- generated fallback art exists so missing optional images do not immediately
  break the game.

### UI Decisions

The main menu uses Canvas + TextMeshPro because it is presentation-facing and
benefits from normal Unity UI layout. Runtime overlays still use a lightweight
OnGUI fallback path for HUD, upgrade cards, pause, and result panels. This keeps
the project functional even when image-based UI assets are unavailable.

### Camera Follow

The camera follows the player with a smoothed `Vector3.Lerp` style movement and
clamps to the arena bounds. This keeps the player centered enough to read enemy
pressure while avoiding abrupt camera snapping during movement.

### Maintainability

The project was refactored from one very large runtime script into multiple
partial files grouped by responsibility. This keeps the Unity component model
simple while making the code easier to discuss:

- `ZombieStormRuntime.cs` handles core flow and shared state.
- `ZombieStormAudio.cs` handles music and sound effects.
- `ZombieStormResources.cs` handles runtime art loading.
- `ZombieStormUpgrades.cs` handles upgrade generation and application.
- `ZombieStormSkillManager.cs` handles automatic skills.
- `ZombieStormEnemy.cs` handles enemy behavior.
- `ZombieStormPlayer.cs` handles player movement, health, and XP.
- `ZombieStormLegacyGUI.cs` handles fallback runtime UI.

## Remaining Improvement Opportunities

These are reasonable next steps for a larger version of the project:

- Move more enemy, wave, and upgrade numbers into data assets for easier tuning.
- Add automated play-mode tests for level-up, pickup, and damage flow.
- Add a final visual playtest checklist with screenshots after each build.
- Add key rebinding and reduced-flash/reduced-shake settings.
- Continue reducing the size of the main runtime controller over time.
