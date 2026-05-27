The active base zombie animation lives in `chibi_zombie/` as numbered frame files named `zombie_chibi_01.png`, `zombie_chibi_02.png`, and so on. The loader also accepts `chibi_zombie/zombie_chibi_walk.png` as a 16-frame `4 x 4` grid, played left-to-right from the top row down.

This Q-style animation is used for the original enemy roles unless a role has its own dedicated visual set. The earlier non-Q zombie sheet has been removed from the project.

The current chibi variant contains 19 numbered left-facing video frames sampled at 0.10-second intervals and plays at the regular enemy rate of 10 fps. Its runtime orientation is inverted relative to the standard right-facing zombie and its larger display scale is intentional for readability.

`craftpix_villager/` supplies the `Slasher` enemy: a separate sword zombie with run, slash, hurt, and death sequences. It enters the spawn mix after the opening phase and deals contact damage only on its animated sword strike.

`craftpix_gravedigger/` supplies the `Gravedigger` enemy: a slower, sturdier shovel zombie whose heavy animated strike produces a wider ground-impact cue.

`craftpix_reaper/` supplies the `Reaper` enemy: a mid-to-late game curved-blade zombie with a longer, clearly animated sweeping attack range.

`craftpix_crystal_golem/` supplies the `Crystal Colossus` Boss: a large crystal-studded golem with dedicated run, blade-sweep, projectile-throw, hurt, and death animations. Its two attacks use cyan telegraphs and crystal-colored projectile impact feedback.

`craftpix_moss_golem/` supplies the `Mossbound Colossus` Boss: a moss-grown stone golem with dedicated run, slam, seed-volley, hurt, and death animations. Its strike leaves hazardous corrupted ground while its throw sends radial green projectiles outward.

`craftpix_ember_golem/` supplies the `Ember Tyrant` Boss: a volcanic stone golem with dedicated run, charge-cleave, magma-cast, hurt, and death animations. Its dash leaves burning ground, while its ranged attack marks and detonates several magma impacts.

The active Boss roster uses only these three golems: `Crystal Colossus`, `Mossbound Colossus`, and `Ember Tyrant`.
