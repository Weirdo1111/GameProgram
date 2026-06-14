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

## Validation

The project builds with:

```powershell
dotnet build GameProgram.sln
```

Gameplay, UI, camera behavior, audio, and asset rendering are integrated in the Unity project.
