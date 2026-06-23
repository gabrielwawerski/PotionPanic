# Potion Panic Implementation Readiness

## Verdict

The docs are now decision-complete enough to implement the MVP without additional product decisions.

The authoritative docs are:

- `Potion Panic.md` for player-facing design, scope, milestones, and locked tuning targets
- `Potion Panic - Technical Architecture.md` for runtime structure, ownership, and system behavior

## Locked MVP Decisions

### 1. Run structure

- MVP uses one gameplay scene: `Laboratory.unity`
- `MainMenu`, `Playing`, `Paused`, and `GameOver` are in-scene run states
- Starting a run resets gameplay systems and spawns the first disaster after `3 seconds`
- Restart reloads `Laboratory.unity` for a clean reset

### 2. Wrong-potion behavior

- The wrong potion is consumed
- The disaster stays active
- The player immediately takes `+10 Panic`
- Wrong-potion use does not award score

### 3. Panic and disaster tuning

- Stages 1-3 use the same default disaster tuning for all three MVP disasters:
  - `1.5 Panic/sec` while active
  - escalation at `20 seconds`
  - `3.0 Panic/sec` after escalation
- Correct resolution immediately reduces Panic by `10`
- Stage 4 uses:
  - `1.875 Panic/sec` while active
  - escalation at `15 seconds`
  - `3.75 Panic/sec` after escalation

### 4. Difficulty progression

| Stage   | Run Time | Max Active Disasters | Spawn Interval |
| ------- | -------- | -------------------- | -------------- |
| Stage 1 | 0:00-0:59 | 1 | 12 seconds |
| Stage 2 | 1:00-1:59 | 2 | 10 seconds |
| Stage 3 | 2:00-2:59 | 3 | 8 seconds |
| Stage 4 | 3:00+ | 3 | 6 seconds |

Additional rules:

- Stage progression is time-based, not score-based
- If the active-disaster cap is full, no spawn backlog is queued
- Disaster selection uses equal weighting across currently enabled disaster types

### 5. Score rules

- `+100` for each resolved disaster
- `+50` if the disaster is resolved within `10 seconds` of spawning
- `+1` score for each full second survived
- Combo scoring is not part of MVP

## Repo Note

The current Unity scaffold still contains `Assets/Scenes/SampleScene.unity` as the placeholder gameplay scene, and `ProjectSettings/EditorBuildSettings.asset` currently points to that placeholder.

The first gameplay implementation pass should rename or replace that placeholder scene as `Laboratory.unity` and update build settings to match.

## Readiness Decision

- Ready for guided implementation of the first vertical slice: `Yes`
- Ready for blind MVP implementation without additional product decisions: `Yes`
- Remaining tuning flexibility belongs to Milestone 10 balancing, not to unresolved product decisions
