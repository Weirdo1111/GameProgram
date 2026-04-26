# GitHub Workflow For Zombie Storm

## Branching

- `main` stays stable and playable.
- Feature work uses short branches such as `zombie-storm-mvp`, `weapon-evolution`, or `boss-fsm`.
- Large art or generated asset drops should be isolated in their own commit.

## Kanban Rules

- Work starts in `Docs/KANBAN.md`.
- Move cards from `Backlog` to `Ready` only when the scope is clear.
- Move one implementation slice to `In Progress` at a time.
- Move to `Review` after local compile/play checks.
- Move to `Done` after merge or after the user accepts the result.

## Commit Rules

- Use outcome-based commit messages, for example `Build Zombie Storm MVP loop`.
- Keep planning/docs commits separate from large gameplay implementation when practical.
- Do not mix unrelated restoration, art import, and gameplay changes in the same commit unless the user explicitly asks for a single snapshot.

## Pull Request Checklist

- The run starts from `Assets/Scenes/SampleScene.unity`.
- `dotnet build Assembly-CSharp.csproj` passes or Unity compile passes.
- MVP loop is testable: move, auto-fire, kill, collect XP, level up, survive/win/lose.
- `Docs/ZombieStormPlan.md` and `Docs/KANBAN.md` reflect the current scope.

## Release Milestones

- `MVP`: five-minute survival loop with three-choice upgrades and basic boss.
- `Combat Alpha`: six weapons, elite rewards, and early weapon evolution.
- `Course Demo`: dynamic difficulty AI, Boss FSM, permanent growth, and polished presentation.
