# Zombie Storm

Zombie Storm is a Unity 2D survival action prototype inspired by horde survival games, built around readable auto-casting skills, fast upgrade choices, and escalating boss waves.

## Current Playable Loop

- Survive a five-minute city horde run.
- Move, kite enemies, collect XP and coins.
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
- Orbiting Knives
- Meteor Storm
- Fire Zone
- 火灵
- Chain Lightning
- Shield Burst
- Ultimate Storm

Each active skill is capped at Lv.5. Follow-up upgrades now bias toward skills you already own, so a lightning, fire, knife, 火灵, or magic build can become more coherent over a run.

## Bosses

Boss waves use different movement, health, attack rhythm, telegraphs, and rewards:

- Ravager Brute: heavy health pool, high contact damage, charge slams, radial shockwaves.
- Plague Matriarch: poison zone control, ranged toxic volleys, stronger area denial when enraged.
- Storm Revenant: fast movement, lightning strike telegraphs, fan-shaped electric barrages.

Boss skills show warning effects before they land so players can read and dodge the attack.

## Project Structure

Main gameplay code lives in `Assets/Scripts/ZombieStorm`:

- `ZombieStormRuntime.cs`: run flow, spawning, UI, pooling, assets, audio, rewards.
- `ZombieStormEnemy.cs`: enemy and boss behavior.
- `ZombieStormSkillManager.cs`: active skill casting and skill levels.
- `ZombieStormPlayer.cs`: player movement, health, XP, coins.
- `ZombieStormProjectiles.cs`: player and enemy projectiles.
- `ZombieStormEffects.cs`: pooled area effects and timed visuals.
- `ZombieStormPickup.cs`: XP and coin pickups.
- `ZombieStormTypes.cs`: shared enums and upgrade data types.

## Assets And Licensing

Documented external art sources are listed in `Docs/ArtSources.md`.

- Kenney Topdown Shooter Pack: CC0
- Mikodrak 2D Spell Effects: CC0

Some local prototype assets still need a final source/license audit before any commercial release. Treat the current project as a playable prototype, not a store-ready build.

## Validation

The current lightweight validation command is:

```powershell
dotnet build GameProgram.sln
```

Unity Editor playtesting is still required for full gameplay validation, timing feel, camera behavior, and asset rendering.

## Near-Term Roadmap

- Move skills, enemies, waves, and upgrades into data-driven configs.
- Complete asset source and license cleanup.
- Add persistent save data and meta progression.
- Expand public demo content with more characters, maps, bosses, and build routes.
- Replace the remaining prototype UI pass with a cohesive commercial UI style.
