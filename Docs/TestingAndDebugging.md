# Zombie Storm Testing and Debugging Report

## 1. Purpose

This document records the testing strategy, debugging work, verified results, known limitations, and recommended regression coverage for Zombie Storm.

The report distinguishes between three evidence levels:

- **Verified**: supported by a Unity log, build report, player log, repository check, or current editor compilation.
- **Code reviewed**: confirmed by inspecting the implementation and configuration, but not presented as a full manual playtest.
- **Planned**: a repeatable test case that should be executed before a release or graded demonstration.

No planned test is reported as passed unless supporting evidence exists.

## 2. Test Environment

| Item | Value |
| --- | --- |
| Engine | Unity 2022.3.62f3c1 |
| Engine revision | 1623fc0bbb97 |
| Primary platform | Windows 64-bit standalone |
| Project scene | `Assets/Scenes/ZombieStorm.unity` |
| Input | Keyboard |
| UI systems | Canvas + TextMeshPro main menu; IMGUI runtime overlays and fallback UI |
| Runtime content | `StreamingAssets/ZombieStormContent` in player builds |
| Audio | Unity `AudioSource`, imported clips in `Assets/Resources/Audio` |

## 3. Validation Summary

| ID | Validation | Date | Result | Evidence |
| --- | --- | --- | --- | --- |
| VAL-001 | Unity script compilation | 2026-06-13 | Passed | Tundra build success, 657 items evaluated |
| VAL-002 | Windows standalone build | 2026-06-13 | Passed | Unity Build Report: `Result: Success` |
| VAL-003 | Runtime content packaging | 2026-06-13 | Passed | 888 PNG files, 129.4 MB, copied to StreamingAssets |
| VAL-004 | Standalone startup smoke test | 2026-06-13 | Passed with expected headless warnings | Player initialized assemblies and engine without a managed exception |
| VAL-005 | Current Unity editor compilation | 2026-06-14 | Passed | Domain reload completed after current gameplay changes |
| VAL-006 | Scene configuration | 2026-06-14 | Passed | Build Settings and default scene point to `ZombieStorm.unity` |
| VAL-007 | Repository whitespace check | 2026-06-14 | Passed | `git diff --check` reported no content errors |
| VAL-008 | External local-path scan | 2026-06-14 | Passed | No runtime reference to a user Downloads directory remains |

The detailed, privacy-safe excerpts are stored in:

- `Docs/TestingLogs/unity-compile-summary.txt`
- `Docs/TestingLogs/windows-build-summary.txt`
- `Docs/TestingLogs/player-smoke-summary.txt`

## 4. Build Verification

### 4.1 Unity compilation

The Unity compiler rebuilt the project assemblies and completed successfully:

- 657 build graph items evaluated.
- 14 items updated.
- Script compilation completed in approximately 6.49 seconds.
- `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll` were produced.
- Unity reloaded the managed assemblies successfully.
- Batch mode exited with return code 0.

This is the authoritative compilation result because Unity owns the generated project files, package references, scripting defines, and player compilation pipeline.

### 4.2 Windows player build

The Windows standalone build completed successfully:

- Target: Windows standalone.
- Result: Success.
- Total reported build pipeline duration: approximately 36.3 seconds.
- Complete Unity player build size: 75.1 MB before the external runtime PNG content is considered.
- Main executable: `ZombieStorm.exe`.
- Runtime PNG package: 888 files, 129.4 MB.

The largest Resources item in the build report was the looping background music. TextMeshPro resources, skill sounds, victory/defeat audio, and story transition audio were also included.

### 4.3 Runtime content packaging

#### Original problem

The prototype initially loaded PNG files through paths based on `Application.dataPath` and, in one case, a developer Downloads folder.

That approach worked in the Unity editor because `Application.dataPath` points to the project `Assets` folder. In a standalone player, however, it points to the generated `<Game>_Data` directory. The original path assumptions could therefore cause missing maps, animation frames, UI cards, story images, and effects after packaging.

#### Root cause

The asset loader treated editor source paths and deployed player paths as if they were identical.

#### Fix

- Editor: load source content from `Application.dataPath`.
- Player build: load deployed content from `Application.streamingAssetsPath/ZombieStormContent`.
- Pre-build validation checks required files and directories.
- Post-build processing copies the required PNG content into StreamingAssets.
- Direct dependencies on a developer-specific Downloads path were removed.

#### Verification

The build callback reported:

- 888 PNG files copied.
- 129.4 MB copied.
- Destination created under the built player's StreamingAssets directory.
- The final player build completed successfully.

## 5. Standalone Startup Smoke Test

The built player was launched with a Null graphics device to test startup without requiring an interactive desktop rendering session.

Observed successful stages:

1. Unity engine 2022.3.62f3c1 initialized.
2. Input System initialized.
3. Physics worker threads initialized.
4. Managed assemblies loaded.
5. The application reached normal runtime asset unloading.
6. No C# exception, missing managed assembly, or immediate process crash was recorded.

Observed warnings:

- `Sprites/Default` and `Sprites/Mask` were unsupported by the Null graphics device.
- Hardware video decoding was disabled.

These messages are expected in a headless `-nographics` run because no Direct3D device exists. They do not demonstrate a shader failure on a normal Windows graphics device. A visual player test remains part of the manual regression plan.

## 6. Debugging and Fix Log

### DBG-001: Standalone player could lose runtime art

| Field | Detail |
| --- | --- |
| Severity | High |
| Area | Resource loading / build pipeline |
| Symptom | Editor displayed art correctly, but a packaged player could fail to find PNG files |
| Root cause | Editor-only source paths were reused in a deployed build |
| Fix | Added a platform-aware runtime content root and build copy callback |
| Regression check | Windows build copied 888 PNG files and completed successfully |
| Status | Verified |

### DBG-002: Developer machine path leaked into runtime loading

| Field | Detail |
| --- | --- |
| Severity | High |
| Area | Portability |
| Symptom | A texture path depended on a local user Downloads directory |
| Root cause | A prototype asset was loaded directly from its import location |
| Fix | Moved runtime loading to project/build-owned content |
| Regression check | Repository scan finds no runtime Downloads path |
| Status | Verified |

### DBG-003: Main controller was difficult to inspect and maintain

| Field | Detail |
| --- | --- |
| Severity | Medium |
| Area | Maintainability |
| Symptom | A controller of more than 6,000 lines mixed flow, UI, audio, upgrades, and resource loading |
| Root cause | Prototype features accumulated in one class |
| Fix | Split the controller into partial files by responsibility |
| Result | Runtime 2,585 lines; Resources 1,566; Legacy GUI 1,198; Upgrades 676; Audio 205 |
| Regression check | Unity script compilation and Windows player build passed after the split |
| Status | Verified |

### DBG-004: Scene retained the template name

| Field | Detail |
| --- | --- |
| Severity | Low |
| Area | Project presentation / build configuration |
| Symptom | Unity Hierarchy and Build Settings displayed `SampleScene` |
| Root cause | The template scene had never been renamed |
| Fix | Renamed it to `ZombieStorm.unity`, retained the `.meta` GUID, and updated project settings |
| Regression check | Build Settings and the default scene both reference the new path |
| Status | Verified |

### DBG-005: Runtime music competed with combat sound effects

| Field | Detail |
| --- | --- |
| Severity | Medium |
| Area | Audio mix |
| Symptom | Menu music remained at full configured volume during combat |
| Root cause | One volume formula was used for every flow state |
| Fix | Apply a 0.70 gameplay multiplier after a run starts; restore menu volume on return |
| Special handling | Pause, upgrade, and result states retain the gameplay mix to prevent volume jumps |
| Status | Code reviewed; manual listening test planned |

### DBG-006: Orc Thrower attacked too frequently

| Field | Detail |
| --- | --- |
| Severity | Medium |
| Area | Enemy balance |
| Symptom | Repeated rock throws created excessive ranged pressure |
| Root cause | Post-throw random delay was 1.70-2.45 seconds |
| Fix | Increased the post-throw random delay to 2.70-3.45 seconds |
| Coverage | Updated both animated and fallback throw branches |
| Status | Code reviewed; timing playtest planned |

### DBG-007: UI implementation could be misread as entirely legacy IMGUI

| Field | Detail |
| --- | --- |
| Severity | Low |
| Area | Code clarity |
| Symptom | The `OnGUI()` comment implied all user interface screens used immediate mode |
| Root cause | Comment did not describe the Canvas/TextMeshPro main menu |
| Fix | Documented that the main menu is Canvas-based and IMGUI remains for runtime overlays/fallback |
| Status | Verified by code inspection |

### DBG-008: Unused art increased repository size

| Field | Detail |
| --- | --- |
| Severity | Medium |
| Area | Repository hygiene |
| Symptom | Three unreferenced Reaper Man animation sets added over a thousand files |
| Root cause | Prototype imports remained after the active enemy art was selected |
| Fix | Removed the three unused sets while retaining referenced enemy content |
| Regression check | Runtime resource list and Unity compilation remained valid |
| Status | Verified |

### DBG-009: Enemy documentation did not match the active spawn system

| Field | Detail |
| --- | --- |
| Severity | Low |
| Area | Documentation |
| Symptom | README listed older boss concepts instead of current golem bosses |
| Root cause | Gameplay evolved faster than the documentation |
| Fix | Documented active ordinary enemies and the current three boss waves |
| Status | Verified by comparing README with spawn selection and boss schedule code |

## 7. Functional Test Matrix

The following matrix is designed for repeatable manual regression. Items marked **Planned** are not claimed as executed.

### 7.1 Startup and navigation

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| NAV-001 | Open the project scene | `ZombieStorm` appears as the active scene | Verified |
| NAV-002 | Enter Play Mode | Main menu loads without a script compilation error | Verified |
| NAV-003 | Select Start Game | Story sequence begins when story textures are available | Planned |
| NAV-004 | Advance all story pages | Final page transitions into active gameplay | Planned |
| NAV-005 | Open and close Settings | Return to the screen that opened Settings | Planned |
| NAV-006 | Pause and resume | Time scale pauses and restores without restarting the run | Planned |
| NAV-007 | Return to main menu | Runtime objects clear and menu music volume returns | Planned |

### 7.2 Player and progression

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| PLY-001 | Move with WASD | Character moves and remains inside the arena | Planned |
| PLY-002 | Take enemy damage | Health decreases and hurt sound/feedback plays | Planned |
| PLY-003 | Collect XP orb | XP increases and pickup feedback appears | Planned |
| PLY-004 | Reach level threshold | Upgrade selection pauses gameplay | Planned |
| PLY-005 | Select cards with 1/2/3 | Correct card applies once and gameplay resumes | Planned |
| PLY-006 | Select cards by mouse | Clicked upgrade applies once | Planned |
| PLY-007 | Collect health potion | Health increases without exceeding maximum | Planned |
| PLY-008 | Unlock ultimate | `F` activates the ultimate only when available | Planned |

### 7.3 Skills

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| SKL-001 | Magic Bolt auto-cast | Projectile targets an enemy and respects cooldown | Planned |
| SKL-002 | Fire Blades | Orbiting damage applies at the intended radius | Planned |
| SKL-003 | Fire Zone | Bomb impact and optional lingering fire render correctly | Planned |
| SKL-004 | Fire Spirit | Companion attacks and follows the player | Planned |
| SKL-005 | Regeneration | Healing tick follows the displayed interval | Planned |
| SKL-006 | Ultimate Storm | Area coverage, damage, cooldown, and sound operate together | Planned |
| SKL-007 | Maximum skill level | Further choices evolve or redirect instead of exceeding the cap | Planned |

### 7.4 Ordinary enemies

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| ENM-001 | Goblin pursuit | Moves directly toward the player and uses contact damage | Code reviewed |
| ENM-002 | Small Goblin pursuit | Moves faster, has lower health, appears after first boss | Code reviewed |
| ENM-003 | Slasher attack | Uses animated strike and may perform one leap attack | Code reviewed |
| ENM-004 | Gravedigger attack | Slow heavy strike has greater damage and reach | Code reviewed |
| ENM-005 | Reaper attack | Long-reach melee strike and larger XP reward work | Code reviewed |
| ENM-006 | Orc Thrower spacing | Approaches from far range and retreats when too close | Code reviewed |
| ENM-007 | Orc Thrower cadence | Delay after a throw is 2.70-3.45 seconds plus animation time | Code reviewed |
| ENM-008 | Enemy death | Death animation, sound, blood, XP, and pooling complete once | Planned |

### 7.5 Bosses

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| BOS-001 | First boss timing | Crystal Colossus appears near 75 seconds | Code reviewed |
| BOS-002 | Second boss timing | Mossbound Colossus appears near 185 seconds | Code reviewed |
| BOS-003 | Third boss timing | Ember Tyrant appears near 270 seconds | Code reviewed |
| BOS-004 | Telegraph readability | Warning effect appears before damaging action | Code reviewed |
| BOS-005 | Crystal Colossus | Slam and ice shard fan match telegraphs | Planned |
| BOS-006 | Mossbound Colossus | Corrupted ground and delayed poison restrict movement | Planned |
| BOS-007 | Ember Tyrant | Charge, fire trail, volleys, and meteor barrage resolve correctly | Planned |
| BOS-008 | Enrage threshold | Boss becomes faster and more aggressive below half health | Code reviewed |
| BOS-009 | Boss defeat | Reward, heal, feedback, and progression state update once | Planned |

### 7.6 Audio

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| AUD-001 | Application startup | Background music begins and loops | Code reviewed |
| AUD-002 | Gameplay transition | Music changes to 70% of the configured menu level | Code reviewed |
| AUD-003 | Pause/upgrade/results | Music does not jump back to full menu level | Code reviewed |
| AUD-004 | Return to menu | Music restores to the configured menu level | Code reviewed |
| AUD-005 | Settings sliders | Master, music, and SFX values persist through PlayerPrefs | Code reviewed |
| AUD-006 | Enemy death variation | One of two death clips is selected randomly | Code reviewed |
| AUD-007 | Victory and defeat | Correct result sound plays once | Planned |

### 7.7 UI and presentation

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| UI-001 | Main menu assets available | Canvas/TextMeshPro menu is used | Code reviewed |
| UI-002 | Main menu assets unavailable | Legacy fallback prevents an unusable menu | Code reviewed |
| UI-003 | Runtime HUD | Health, timer, skills, and boss health remain readable | Planned |
| UI-004 | Upgrade cards | Three cards render without overlap at supported resolution | Planned |
| UI-005 | Result screen | Victory/defeat result appears and accepts restart/menu input | Planned |
| UI-006 | README screenshots | All four relative image links render on GitHub | Repository check |

### 7.8 Packaging and portability

| ID | Test | Expected result | State |
| --- | --- | --- | --- |
| PKG-001 | Required source validation | Missing required files stop the build with a useful error | Code reviewed |
| PKG-002 | StreamingAssets copy | Required runtime PNG files are copied after build | Verified |
| PKG-003 | Clean machine launch | Player does not depend on project `Assets` or user Downloads | Verified by path design; clean-machine run planned |
| PKG-004 | Standalone startup | Player reaches initialization without a managed exception | Verified |
| PKG-005 | Visual standalone launch | Sprites, UI, audio, and story render on a normal GPU | Planned |

## 8. Static and Repository Checks

### 8.1 Checks performed

- Confirmed the only enabled build scene is `Assets/Scenes/ZombieStorm.unity`.
- Confirmed the scene GUID was preserved during rename.
- Confirmed no `SampleScene` path remains in Assets or ProjectSettings.
- Confirmed no user Downloads path remains in runtime code.
- Confirmed build-only content uses StreamingAssets.
- Confirmed the Orc Thrower cooldown was changed in both execution branches.
- Confirmed gameplay music uses a single named multiplier.
- Confirmed README image paths point to tracked project files.
- Ran `git diff --check` with no whitespace errors.

### 8.2 Logging policy

Runtime logging is intentionally limited:

- Build content copy results are logged once per build.
- Missing optional UI/resources produce warnings with the failed path.
- Main menu initialization failure reports a warning and uses a fallback.
- Per-frame combat does not emit debug logs, avoiding console spam and unnecessary allocation.

## 9. Tooling Note: Generated C# Project Files

Unity is the authoritative build system for this project.

The solution and `.csproj` files are generated IDE artifacts. After the main controller was split into new partial files, an older generated `Assembly-CSharp.csproj` did not immediately list every new source file. Running `dotnet build GameProgram.sln` against that stale file produced missing-member errors even though Unity compiled the same source successfully.

Recommended procedure:

1. Open the project in Unity.
2. Regenerate project files from the configured external tools integration when needed.
3. Use Unity compilation or a Unity batch-mode build for release validation.
4. Do not treat a stale generated `.csproj` as stronger evidence than Unity's compiler output.

## 10. Known Limitations and Residual Risk

- No dedicated Unity Test Framework test assembly currently covers gameplay logic.
- Most combat verification is integration-level because behaviors depend on frame timing, pooled GameObjects, sprites, and Unity lifecycle methods.
- The headless smoke test cannot validate visual correctness.
- A complete five-minute manual run should be repeated after major balance changes.
- Multiple resolutions and aspect ratios require visual regression checks.
- Audio loudness is subjective and should be checked with headphones and speakers.
- Randomized spawn selection and upgrade choices require several runs to cover naturally.

## 11. Recommended Automated Tests

### Edit Mode candidates

- Upgrade level caps and evolution prerequisites.
- Passive multiplier formulas.
- Boss XP and healing rewards.
- Enemy type remapping.
- Boss warning text and display names.
- Arena clamping.
- Cooldown range boundaries.
- Required build-content manifest validation.

### Play Mode candidates

- Starting a run creates exactly one player and skill manager.
- Level-up pauses time and selecting a card resumes it.
- Returning to menu clears pooled active objects.
- Enemy death rewards are emitted once.
- Player projectiles respect pierce and lifetime.
- Boss telegraphs precede damage.
- Audio state multiplier changes on run/menu transitions.
- Packaged runtime content can load a representative map, story image, enemy frame, and UI card.

## 12. Release Acceptance Checklist

Before presenting or publishing a new build:

- [ ] Unity Console has no compile errors.
- [ ] Main menu opens and Settings values persist.
- [ ] Story advances through every page.
- [ ] A new run starts with movement and Magic Bolt active.
- [ ] At least one upgrade is selected by keyboard and mouse.
- [ ] Every ordinary enemy type is observed.
- [ ] Orc Thrower cadence feels readable after the one-second increase.
- [ ] All three bosses spawn and complete each attack pattern.
- [ ] Victory and defeat flows both return to a playable state.
- [ ] Gameplay music is quieter than menu music without masking SFX.
- [ ] Windows player builds successfully.
- [ ] StreamingAssets contains the expected runtime content folder.
- [ ] Standalone player launches on a normal graphics device.
- [ ] No missing texture, audio, or file-path warning appears.
- [ ] README screenshots and testing report render on GitHub.

## 13. Conclusion

The project has verified Unity compilation, a successful Windows build, a functioning runtime-content packaging pipeline, and a successful standalone startup smoke test. The highest-risk portability issue, editor-only asset paths, has been addressed and verified through the build output.

The remaining quality work is primarily systematic manual regression and future automated gameplay coverage rather than a known build blocker.
