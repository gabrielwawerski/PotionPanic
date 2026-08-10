# Game Design Review

## Overall assessment

The design is strong as a small first-game/MVP design. It has unusually good scope discipline and a clear relationship between player experience, implementation scope, and technical architecture.

The main weakness is deeper: the document explains the loop better than it currently proves that the loop will remain interesting.

Potion Panic currently has this structure:

> recognize problem → fetch corresponding ingredient → brew → return → resolve

The pressure system can make that loop difficult, but the underlying actions are almost completely deterministic. That makes the project feasible, which is good, but creates the largest design risk: after the player learns the three mappings, the game may become a routing/execution exercise rather than an interesting prioritization game.

The MVP should not be expanded preemptively to solve this. The current loop is exactly the kind of design that should be prototyped first and judged through play.

## What is especially good

### The pillars actually constrain the design

The fixed camera, one-slot inventory, one solution per disaster, and concurrent threats all clearly derive from the pillars:

- controlled chaos
- fast decision making
- simple systems, high pressure

That makes the pillars useful rather than decorative.

For example:

- one carried item creates commitment and travel pressure;
- one correct potion keeps cognition focused on prioritization;
- fixed top-down visibility removes camera management;
- concurrent disasters create complexity without recipe-system complexity.

That combination is coherent.

### The one-item inventory is one of the strongest design decisions

It does substantial work for very little implementation cost.

Because the player cannot stockpile several solutions, every response forces a route and a commitment. It also prevents the optimal strategy from becoming "prepare one of everything and erase threats immediately."

This should remain.

### The example run explains the intended decision model well

The current design identifies:

- disaster age
- location
- escalation state
- current Panic pressure

as factors in deciding what to solve first.

That is a much stronger description of Potion Panic than simply saying that several disasters create pressure. It is close to describing the game's actual strategic model.

### Scope separation is clean

The project correctly distinguishes:

- game design as desired player experience;
- MVP scope as binding numbers and delivery decisions;
- runtime design as technical ownership;
- tickets and the board as current implementation state.

That separation should remain.

## Biggest design risk: the disasters are mechanically too similar

The MVP currently gives all three disasters the same base Panic rate, escalation time, escalated Panic rate, and equal spawn weighting through stages 1–3.

The game-design document says that escalation timers force the player to choose which problem matters most, but mechanically, two equally old disasters have essentially identical urgency.

Priority therefore comes mostly from:

- which one appeared first;
- which one is closer;
- what the player is already carrying;
- route length.

That can work and is appropriate for the MVP.

However, it means Overheated Cauldron, Slime Leak, and Toxic Magic Cloud are currently different answers to the same mathematical problem rather than three strategically different problems.

This should be treated as an MVP simplification rather than as fully realized disaster differentiation.

Post-MVP, small changes to pressure curves or escalation profiles could create much more depth without requiring new systems.

The locked tuning should not be changed before playtesting without evidence that it is needed.

## The laboratory layout needs more design ownership

This is probably the most important missing design area.

The document says the room contains:

- the brewing station;
- ingredient stations;
- disaster zones;
- HUD/UI;

and that the whole room should remain readable.

But in Potion Panic, space is one of the primary mechanics.

There is:

- no combat;
- no recipe puzzle;
- no large inventory;
- no aiming;
- no camera management;
- no movement abilities.

As a result, deciding where to go carries a large portion of the gameplay.

The design should eventually establish principles such as:

- ingredient stations should create meaningfully different routes;
- the brewing station should create deliberate traffic through the room;
- no important disaster should be visually hidden;
- disaster positions should sometimes make the shortest route different from the highest-priority route;
- the player should regularly cross or redirect rather than follow one circular route;
- distance should create pressure without turning into tedious walking.

Exact measurements are not needed yet.

However, laboratory topology should be treated as a core design concern rather than mainly as an art or level-layout concern.

## Brewing risks becoming a waypoint rather than gameplay

Brewing is deliberately simple:

> ingredient goes in, matching potion comes out.

That is the correct scope decision.

However, if brewing is instantaneous, the loop:

> ingredient → brewing station → disaster

can become mechanically similar to:

> ingredient → disaster

with an obligatory detour through the brewer.

That can still work if the brewing station functions as a meaningful spatial bottleneck or routing point.

The project should not solve this by adding recipe complexity or a brewing minigame.

Instead, Milestone 3 should specifically test whether visiting the brewing station feels like:

1. an important part of the fantasy and route planning; or
2. unnecessary walking inserted between pickup and resolution.

That distinction matters.

## Two concrete inconsistencies should be fixed

### "Three or more active disasters"

The game-design page says:

> Three or more active disasters create constant urgency.

The locked MVP cap is three, including Stage 4.

This should become either:

> Up to three active disasters create constant urgency.

or:

> Three simultaneous disasters create constant urgency.

### Core loop implies sequential spawning

The core loop currently ends with:

> A new disaster appears.

That implies:

> spawn → resolve → next spawn

But the actual design uses independent timed spawning and allows concurrent disasters.

The conceptual loop should instead communicate that disasters continue spawning while the player triages, prepares solutions, and resolves active threats.

The later example-run section already communicates this more accurately.

## Clarify what "escalation" means in the MVP

The disaster descriptions include stretch escalation concepts such as:

- the cauldron bursting into magical fire;
- slime spreading;
- the toxic cloud expanding.

The locked MVP, however, defines escalation primarily as increased Panic pressure after a timing threshold.

The design should explicitly distinguish:

### MVP escalation

- stronger visual/audio warning;
- increased Panic rate.

### Stretch or post-MVP escalation

- spreading slime;
- expanding clouds;
- magical fire;
- other behavioral effects.

Without this separation, an implementer could reasonably interpret those stretch behaviors as MVP requirements.

## Scoring is appropriate, but not currently a major decision system

The score rewards:

- disaster resolution;
- fast response;
- survival time.

That is appropriate for the MVP.

One balancing point to watch is the hard 10-second speed threshold. A resolution at 9.9 seconds earns the bonus, while one at 10.1 seconds does not.

This may feel arbitrary if the bonus is highly visible.

It should remain simple for the MVP and only be changed if playtesting shows that the threshold creates undesirable incentives.

## The wrong-potion mechanic is good

The wrong-potion rule creates a strong failure chain:

> wrong choice → potion consumed → Panic spike → another ingredient/brewing trip required → existing disasters continue ticking

This compounds pressure using systems the game already has.

The feedback requirement is important. The player must clearly understand why the penalty happened.

## The game still needs to prove its fun density

The central design assumption is that simultaneous simple jobs will generate enough interesting pressure.

That is plausible, but not guaranteed.

The key playtest questions should be:

- How long does one complete ingredient → brew → disaster trip take?
- How much of that time feels like decision-making versus simple walking?
- When two threats exist, does the player actually think about priority?
- Does changing the order meaningfully affect survival?
- How often do disasters escalate?
- Does the player recover from near-failure, or does high Panic become an inevitable death spiral?
- Does Stage 1 feel instructional or empty?
- Does Stage 4 feel chaotic but understandable?
- After learning all three mappings, is the fifth minute more interesting than the first?

These answers matter more than expanding the written design before the vertical slice exists.

## Broader identity concern

As an MVP and learning project, the current design is appropriately scoped.

As a game considered purely on its design identity, it is still fairly conventional.

Its current identity is roughly:

> Overcooked-like spatial urgency + one-room disaster triage + potion preparation

The design does not yet contain one mechanic that is unmistakably unique to Potion Panic.

That should not be solved before the vertical slice.

Adding a uniqueness mechanic now could damage the scope discipline that is currently one of the project's strongest qualities.

The better sequence is:

> build Milestones 1–5 → play the complete loop → identify what feels repetitive → add depth only to that exact weakness

The current architecture is suitable for this because it supports one reusable disaster loop rather than three unrelated systems.

## Recommended changes now

Make only four design-document changes before continuing implementation:

1. Change "three or more active disasters" to match the three-disaster cap.
2. Rewrite the final step of the core loop so spawning is clearly concurrent and time-driven rather than resolution-driven.
3. Explicitly distinguish MVP escalation feedback/tuning from behavioral stretch escalations.
4. Expand Laboratory Layout slightly to state that spatial routing and travel decisions are a primary source of gameplay pressure.

Everything else should be validated through the vertical slice rather than redesigned on paper.

## Verdict

The design is coherent and strongly disciplined for the project's size.

The main unresolved question is whether the intentionally simple loop produces enough meaningful route and priority decisions once the player has memorized all three solutions.

That should become the central playtest hypothesis for Milestones 4–7.
