# Zombie Storm

Zombie Storm is a Unity 2D survival action game inspired by horde survival games, built around readable auto-casting skills, fast upgrade choices, and escalating boss waves.

## Current Playable Loop

- Survive a five-minute city horde run.
- Move, kite enemies, and collect XP orbs.
- Pick one of three upgrades on level-up.
- Build active skills up to Lv.5, then evolve them with matching passives.
- Defeat elite enemies and three distinct boss waves.

## Controls

- `WASD`: Move
- `F`: Ultimate storm, once unlocked
- `1 / 2 / 3`: Choose level-up upgrade
- `Esc` or `P`: Pause
- `Enter`: Start or restart from menu/results

## Skills And Progression

Active skills auto-cast after being learned:

- Magic Bolt
- Fire Blades
- Regeneration
- Fire Zone
- Fire Spirit
- Ultimate Storm

Most active skills are capped at Lv.5. Fire Zone is capped at Lv.4 and Regeneration is capped at Lv.3. Follow-up upgrades now bias toward skills you already own, so a fire, fire-blade, fire-spirit, or magic build can become more coherent over a run.

## Bosses

Boss waves use different movement, health, attack rhythm, telegraphs, and rewards:

- Ravager Brute: heavy health pool, high contact damage, charge slams, radial shockwaves.
- Plague Matriarch: poison zone control, ranged toxic volleys, stronger area denial when enraged.
- Storm Revenant: fast movement, lightning strike telegraphs, fan-shaped electric barrages.

Boss skills show warning effects before they land so players can read and dodge the attack.

## Assets And Licensing

Documented external art sources are listed in `Docs/ArtSources.md`. Imported audio sources are listed separately in `Docs/AudioSources.md`.

- Kenney Topdown Shooter Pack: CC0
- Mikodrak 2D Spell Effects: CC0
- Fire bomb and burn-circle effects: imported from Pixlab24 asset downloads supplied by the project owner and verified for free and commercial project use.
- CraftPix character and boss sprites: sourced from CraftPix free asset packs and verified for commercial project use.
- Dark VFX effects: free public assets verified for personal learning and course-project use.
- Main menu cover, skill-card templates, and health potion pickup icon: AI-generated images created with ChatGPT and supplied by the project owner, documented in `Docs/ArtSources.md`.

All included assets have documented sources and verified usage terms for this course project. The GamerSounds audio clips and Dark VFX effects are used for personal learning and project exchange.

## Validation

The project builds with:

```powershell
dotnet build GameProgram.sln
```

Gameplay, UI, camera behavior, audio, and asset rendering are integrated in the Unity project.
