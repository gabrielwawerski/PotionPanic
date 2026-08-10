# Design Risk Fix Proposals

The strongest approach is to make the three risks reinforce each other instead of solving each with an independent feature:

- disasters create different kinds of urgency;
- room geometry makes those differences matter spatially;
- brewing becomes the shared commitment and routing point connecting them.

This preserves the current design pillars:

- one correct potion per disaster;
- one carried item;
- fixed top-down room;
- simple brewing;
- complexity emerging from simultaneous pressure.

# 1. Make disasters mechanically distinct through pressure profiles

## Recommended MVP change: different urgency curves

Do not give every disaster an elaborate bespoke mechanic.

Keep the shared rule:

> recognize disaster → fetch ingredient → brew potion → resolve

Instead, make each disaster create a different prioritization problem.

| Disaster | Identity | Initial pressure | Escalation | Decision it creates |
|---|---|---:|---:|---|
| Overheated Cauldron | Immediate threat | High | Moderate or late | Do I deal with this now? |
| Slime Leak | Growing threat | Low | Fast and severe | How long can I safely postpone this? |
| Toxic Magic Cloud | Persistent threat | Medium | Moderate | Can I tolerate this while handling something worse? |

The exact numbers should come from playtesting.

The important part is the shape of the pressure curve.

## Illustrative tuning example

These values are examples only.

| Disaster | Base | Escalation | Escalated |
|---|---:|---:|---:|
| Cauldron | 2.0 Panic/sec | 25 sec | 3.0 Panic/sec |
| Slime | 0.8 Panic/sec | 12 sec | 3.5 Panic/sec |
| Cloud | 1.4 Panic/sec | 20 sec | 2.4 Panic/sec |

The current architecture already supports this cleanly through per-disaster data such as:

- base Panic rate;
- escalation time;
- escalated Panic rate.

This means disaster differentiation can initially come from tuning rather than new code architecture.

## Why this improves prioritization

Suppose:

- Cauldron spawned 5 seconds ago.
- Slime spawned 10 seconds ago.

With identical tuning, priority is mostly determined by distance.

With different profiles:

- Cauldron may be costing much more Panic right now.
- Slime may be only seconds away from becoming much worse.

Now the player has a real tradeoff.

This directly strengthens the fast-decision-making pillar without increasing recipe complexity.

## Later: add one lightweight behavioral identity per disaster

Only if different pressure curves are not enough.

Possible post-MVP directions:

### Overheated Cauldron

- occasional Panic bursts;
- increasingly intense warning pulses;
- magical fire during a later escalation state.

### Slime Leak

- slowly spreads across floor space;
- begins affecting routing.

### Toxic Magic Cloud

- occupies an area the player prefers not to cross;
- expands gradually.

These behaviors should remain stretch or post-MVP until playtesting proves that tuning alone is insufficient.

# 2. Treat laboratory topology as a core mechanic

The laboratory should not simply contain all required stations and disaster points.

It should be deliberately built to create route decisions.

Potion Panic removes many common sources of mechanical complexity:

- combat;
- aiming;
- recipe puzzles;
- large inventories;
- camera management;
- movement abilities.

Therefore, movement through the room carries much of the gameplay.

## Recommended topology

Use:

- one brewing hub;
- three separated ingredient stations;
- several disaster locations;
- multiple intersecting routes;
- very few or no dead ends.

Do not place the brewing station perfectly in the middle by default.

A perfectly symmetrical layout risks producing the same route repeatedly:

> ingredient → center → disaster

Instead, favor an off-center hub with intersecting routes.

Conceptually:

```text
┌──────────────────────────────────────┐
│                                      │
│  Blue Ingredient          Disaster A │
│        ●                        ●     │
│         \                      /      │
│          \                    /       │
│       ● Disaster B     ● Brewer       │
│             \          /    \         │
│              \        /      \        │
│       Purple ●        ●       ● Green │
│    Ingredient     Disaster C Ingredient│
│                                      │
└──────────────────────────────────────┘
```

The exact geometry is not important.

The important property is that different combinations of:

- current player position;
- required ingredient;
- target disaster;
- brewing station;
- other active disasters;

produce different optimal routes.

## Give each placement a gameplay purpose

### Separate ingredient stations

If all ingredients are grouped together, ingredient choice has almost no spatial consequence.

A better structure is:

```text
Blue ←──── laboratory ────→ Green
               ↑
             Purple
```

Choosing a solution should also mean choosing a destination.

### Interleave disaster zones with ingredient routes

Avoid a production-line layout such as:

```text
ingredients | brewer | disasters
```

That creates repetitive movement.

Prefer overlapping routes:

```text
ingredient
    ↓
 disaster ← brewer → ingredient
    ↑           ↓
 ingredient → disaster
```

Now a newly spawned disaster can force the player to:

- reverse direction;
- take another path;
- abandon a route;
- reprioritize.

### Avoid dead-end stations

Running into a corner, interacting, turning around, and running back often becomes dead time.

Important objects should usually have at least two viable approaches or exits.

This allows the player's next objective to influence how they enter and leave an area.

## Use distance deliberately

Travel should be long enough for route choice to matter, but short enough that the player spends most of the run reacting rather than commuting.

Useful starting hypotheses for playtesting:

- nearby station-to-station traversal: 1.5–3 seconds;
- long room crossing: 4–6 seconds;
- full ingredient → brewer → disaster route: roughly 5–9 seconds.

These are not locked targets.

The important test is:

> Does choosing the wrong route cost enough time to matter without making movement tedious?

This should be tested during the laboratory blockout rather than postponed until final polish.

## Preserve whole-room readability

The fixed top-down camera should continue giving the player enough information to make decisions.

From almost anywhere in the room, the player should normally be able to determine:

- what disasters exist;
- roughly how urgent they are;
- where the ingredient stations are;
- where the brewer is;
- which route they want next.

The player does not need unobstructed visibility of every floor tile.

Important gameplay state should remain readable.

# 3. Keep brewing simple, but make it strategically meaningful

Do not solve the brewing problem with a minigame.

Avoid adding:

- stirring mechanics;
- timing bars;
- recipe selection;
- button sequences;
- temperature control;
- multi-step brewing.

The current design is correct that the challenge should come from responding to emergencies rather than remembering recipes.

The brewing station does not need to become a standalone game.

It needs to matter to the larger routing game.

## Recommended MVP brewing design

Keep:

> one ingredient + one interaction = matching potion

Then give brewing three properties.

## A. Brewing is a spatial commitment

The brewing station should occupy a strategically meaningful location.

When the player picks up an ingredient, the route becomes:

```text
current position
      ↓
ingredient
      ↓
brewer
      ↓
target disaster
```

The brewer therefore changes optimal routes even if the interaction itself remains simple.

This directly connects the laboratory-layout fix with the brewing fix.

## B. Give brewing a short physical action

Use a short action, roughly 0.7–1.2 seconds, with strong feedback:

- ingredient dropped in;
- fluid or color changes;
- quick bubbling reaction;
- bottle fills;
- clear sound;
- potion appears in hand.

The purpose is not to make waiting strategic.

The purpose is to give the state transition physicality and enough commitment that brewing under pressure feels different from touching a trigger collider.

Avoid long forced waits such as 3–5 seconds unless later playtesting strongly supports them.

## C. Let brewing amplify the "what next?" decision

The interesting moment should happen before the player reaches the brewer.

For example:

> I am carrying Blue. Do I finish the Cauldron solution, or has the newly escalated Slime become more important?

Because the player can carry only one ingredient or potion, changing plans already has an opportunity cost.

That means the brewer can become strategically meaningful without adding a new subsystem.

# Optional follow-up if brewing still feels pointless

Only if playtesting shows that instant brewing still feels like an unnecessary waypoint, test one additional mechanic.

## Best candidate: asynchronous one-slot brewing

Flow:

1. Put an ingredient into the brewer.
2. Brewing starts for roughly 2–3 seconds.
3. The player's hand becomes empty.
4. When finished, one potion waits at the station.
5. The brewer cannot process another ingredient until that potion is collected.

This creates two possible behaviors.

### Wait

```text
ingredient
    ↓
brewer
    ↓
wait
    ↓
potion
    ↓
disaster
```

Safe and straightforward.

### Leave

```text
ingredient
    ↓
brewer starts
    ↓
move or inspect another threat
    ↓
return when ready
```

Now brewing happens in parallel with the crisis-management loop.

This is much more aligned with Potion Panic than a timing minigame.

It also needs only a simple brewer state model:

```text
Idle
↓
Brewing
↓
Ready
↓
Idle
```

This should remain out of the first vertical slice unless instant brewing proves insufficient.

# How the three fixes combine

These fixes work best when they produce one shared decision model.

Example game state:

- Cauldron is active on the north side.
- Slime is active in the southwest.
- The player is near the Green Slime ingredient.
- The brewer is southeast of center.
- Cauldron has high current Panic output.
- Slime has lower output but will escalate in four seconds.

The player evaluates:

```text
Cauldron
high cost now
longer route

vs.

Slime
low cost now
almost escalating
short route
```

Then considers position:

```text
player → Green ingredient → brewer → Slime
```

may be much shorter than:

```text
player → Blue ingredient → brewer → Cauldron
```

That is the intended Potion Panic decision.

No recipe puzzle is required.

No combat is required.

No complicated inventory is required.

The strategic model becomes:

> urgency × distance × current commitment × future escalation

That is enough to create meaningful decision-making if the tuning and layout work.

# Recommended implementation order

## MVP

### Disasters

- Give each disaster a different Panic/escalation profile.
- Keep one-potion resolution.
- Keep behavioral escalation out initially.

### Laboratory

- Treat room topology as gameplay design.
- Separate ingredient stations.
- Interleave disaster locations.
- Use loops and cross-routes rather than dead ends.
- Place the brewer off-center rather than automatically central.
- Preserve whole-room threat readability.

### Brewing

- Keep one-input brewing.
- Add a very short physical brew action.
- Make the brewer strategically important through spatial placement.

## Only if playtesting shows insufficient depth

Add in this order:

1. one-slot asynchronous brewing;
2. lightweight disaster-specific spatial behaviors;
3. further disaster interaction mechanics.

Do not add recipe complexity, multiple-item inventory, or a full brewing minigame unless the core design itself changes.

Those systems would shift Potion Panic away from its strongest current principle:

> simple actions whose interaction creates the difficulty.
