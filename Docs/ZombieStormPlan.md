# Zombie Storm: 僵尸割草大作战 Plan

## Product Pitch

`Zombie Storm: 僵尸割草大作战` is a 2D pixel-style auto-shooting roguelite. The player only controls movement while weapons fire automatically, zombies spawn from outside the camera view, and the run escalates through experience upgrades, weapon unlocks, elite waves, boss phases, and survival victory.

## Core Loop

Move and kite -> auto-fire -> kill zombies -> collect XP and coins -> level up with three choices -> become stronger -> survive denser hordes -> defeat elites or bosses -> evolve weapons -> survive to the timer.

The MVP targets feedback every 10-20 seconds through XP orbs, coins, level-up choices, weapon unlocks, elite spawns, and boss warnings.

## MVP Scope

- Player movement with WASD only.
- Auto pistol targeting the nearest enemy.
- Zombie spawner using off-screen circular spawn points.
- Normal, fast, tank, elite, and boss zombie behaviors.
- Experience orbs with pickup radius and magnet pull.
- Level-up pause with three random choices.
- Weapon unlocks for shotgun, molotov, saw ring, lightning, and mines.
- Passive upgrades for damage, fire rate, area, move speed, pickup range, crit, max health, and coin gain.
- Simple object pools for bullets, enemies, XP, coins, and effects.
- Five-minute survival victory, death failure, timer HUD, and run summary.

## Post-MVP Roadmap

- Weapon + passive evolution recipes.
- Treasure chest rewards after elite kills.
- Permanent growth and character unlocks.
- Dynamic difficulty AI reacting to health, damage, and player dominance.
- Boss FSM with explicit phase transitions and multiple attacks.
- Pixel-art polish pass with dedicated sprites and animations.

## AI Design

- Enemy AI: direct chase, sprint chase, tank blocking, elite pressure, and boss phase logic.
- Dynamic difficulty: wave budget scales over time and reacts to player health and kill pace.
- Boss FSM: approach, summon, dash, radial shot, enraged below half health.

## Definition Of Done For First Playable

- Unity compiles with zero errors.
- Press Play starts a playable run without manual scene setup.
- The player can survive, level, choose upgrades, kill enemies, fight a boss, and win or lose.
- GitHub contains this plan, the Kanban board file, and the workflow document.
