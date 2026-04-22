# RuleShot

Unity 2D vertical slice prototype based on `RuleShot_2D_规则枪_游戏开发文档.docx`.

## Play

Open `Assets/Scenes/SampleScene.unity` and press Play.

- A / D: move
- Space: jump
- Left Shift: dash
- Mouse left: fire the rule gun
- Q / E or mouse wheel: switch rule shots
- 1 / 2 / 3: Heavy / Light / Freeze
- R: return to the latest checkpoint

## Current Slice

- 2D neon mechanical city presentation built procedurally at runtime.
- Five connected areas: tutorial, light/wind platforming, freeze hazards, combo puzzle, final vertical shaft.
- Three rule shots: Heavy, Light, Freeze.
- Prototype interactables: boxes, pressure plates, doors, wind zones, hazards, fragile floors, moving platforms, patrol enemies, shield enemy, finish gate.

## Build

In Unity, use `Build > Build RuleShot Windows`.

Command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe" -batchmode -quit -projectPath "c:\Users\ken\My project" -executeMethod BuildRuleShot.BuildWindowsCli -logFile "c:\Users\ken\My project\Logs\ruleshot_build.log"
```

Output: `Builds/RuleShot-Windows/RuleShot.exe`
