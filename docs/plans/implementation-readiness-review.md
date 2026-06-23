# Potion Panic Implementation Readiness Review

## Verdict

The current docs are ready enough to start a guided vertical-slice implementation, but they are not yet decision-complete enough for a blind handoff to another engineer.

The main indicator is that `Potion Panic - Technical Architecture.md` explicitly says it is not a strict low-level implementation plan.

## Blocking Gaps

### 1. Core gameplay numbers are still unspecified

The docs define the systems, but not the actual values or thresholds for:

- Panic rates
- Escalation timing
- Stage transition timing
- Disaster spawn cadence
- Fast-response scoring values

Relevant references:

- `Potion Panic - Technical Architecture.md` sections covering `DisasterData`, `PanicSystem`, and `ScoreSystem`
- `Potion Panic.md` difficulty scaling section

### 2. Wrong-potion behavior is still ambiguous

The docs do not lock one exact rule for wrong-potion handling.

- The GDD says wrong potion use "fails clearly or increases Panic."
- The architecture doc says a wrong-potion penalty applies "if needed."
- The architecture doc also says wrong potion use "may add" Panic.

An implementer still has to decide the exact failure rule and penalty behavior.

### 3. Difficulty progression is structurally defined but not behaviorally locked

The docs define stages and the `DisasterManager` role, but they do not specify:

- when stages advance
- how spawn delays change
- how active-disaster limits change
- whether progression is time-based, score-based, or survival-based

### 4. Run-flow structure is still open

The docs suggest game states and also suggest separate scenes, but they do not lock the runtime structure for:

- menu flow
- scene transitions
- whether main menu and gameplay are separate scenes
- where restart logic lives

An implementer would still need to choose that structure.

## What Is Ready

The docs are strong enough to begin Milestones 1-4:

- camera and movement direction are clear
- one-slot inventory and interaction flow are clear
- ingredient-to-potion and disaster-to-solution ownership are clear
- the first vertical slice loop is clear

## Recommendation

Use the current docs to implement the early vertical slice only, or add one more planning pass that locks:

- gameplay tuning values
- wrong-potion behavior
- difficulty progression rules
- run-flow and scene/state structure

## Readiness Decision

- Ready for guided implementation of the first vertical slice: `Yes`
- Ready for blind MVP implementation without additional product decisions: `No`
