# Game design

Version: 1.0
Project Type: Small-scope 3D indie game
Team Size: 2 developers
Target Engine: Unity
Target Platform: PC

Use this doc for player-facing design, feel, readability, and content intent.
Use [`mvp-scope.md`](mvp-scope.md) for locked milestone and tuning decisions.
Use [`technical-architecture.md`](technical-architecture.md) for runtime
structure.

## Game at a glance

| The player is... | The game asks them to... | The game ends when... |
| --- | --- | --- |
| An apprentice alchemist in one chaotic laboratory | identify disasters, gather one matching ingredient, brew its potion, and resolve the threat | the Panic Meter reaches 100% |

The design owns player experience and content intent. It does not set locked
numbers, milestone order, or runtime component boundaries.

## Elevator Pitch

Potion Panic is a stylized 3D laboratory survival game where the player takes
the role of an apprentice alchemist trapped inside a magical laboratory that is
rapidly descending into chaos.

Disasters continuously appear throughout the laboratory. The player must gather
ingredients, brew the correct potions, and resolve emergencies before the
laboratory's Panic Meter reaches 100%.

The entire game takes place inside a single room so the team can focus on
gameplay polish, game feel, visual feedback, and finishing a complete small
game.

## Vision

Potion Panic should:

- stay realistic for a beginner two-person Unity team
- teach core Unity systems
- feel polished despite small scope
- create replayability through score chasing
- use a 3D environment without demanding large content production

The goal is not to create a deep alchemy simulator. The goal is to create a fun
crisis-management game.

## Design Pillars

### Controlled chaos

The laboratory should always feel busy and dangerous, but never unreadable.

Good feeling:

> Everything is going wrong, but I know what I need to solve next.

Bad feeling:

> I have no idea what is happening.

### Fast decision making

The challenge comes from prioritization, not from complex crafting. The player
must constantly choose what to solve first.

### Simple systems, high pressure

Individual systems stay simple. Complexity comes from several simple systems
happening at once.

Avoid:

- deep crafting
- large inventories
- RPG progression

Prefer:

- readable mechanics
- increasing pressure
- fast pacing

## Core Gameplay Loop

1. A disaster appears.
2. Panic increases while it stays unresolved.
3. The player identifies the disaster.
4. The player gathers the matching ingredient.
5. The player brews the matching potion.
6. The player applies the potion to the disaster.
7. The disaster resolves.
8. Panic drops and score increases.
9. A new disaster appears.

Repeat until Panic reaches 100%.

## Player Experience

### Early game

The player learns:

- movement
- interaction
- ingredient collection
- brewing
- disaster resolution

Panic rises slowly.

### Mid game

Two active disasters create prioritization pressure.

Example:

- an Overheated Cauldron and a Slime Leak are both active
- escalation timers force the player to choose which problem matters most

### Late game

Three or more active disasters create constant urgency. Survival depends on
efficiency and choosing the highest-risk problem first.

## Camera

The intended MVP camera is fixed top-down.

Benefits:

- easier implementation
- strong room visibility
- fewer camera bugs
- beginner-friendly development

The player challenge should come from gameplay pressure, not from camera
management.

## Player interaction

### Movement

Core controls:

- WASD movement
- one interact key

The player should feel responsive and quick.

### Interaction

The same interact action should support:

- collecting ingredients
- using the brewing station
- applying potions
- activating world objects

### Inventory

The MVP inventory uses one carried item slot.

The player can carry:

- one ingredient
- or one potion

The player cannot carry both at the same time.

This keeps the focus on movement, prioritization, brewing, and disaster
response.

## Content and space

### Laboratory layout

The entire game takes place in one room.

Main areas:

- brewing station
- ingredient stations
- disaster zones
- UI/HUD

The room should be readable from the fixed top-down camera at all times.

### Ingredient and potion set

| Ingredient | Potion | Solves |
| --- | --- | --- |
| Blue Mushroom | Cooling Potion | Overheated Cauldron |
| Green Slime | Slime Dissolver | Slime Leak |
| Purple Crystal Dust | Purification Potion | Toxic Magic Cloud |

### Ingredient themes

- Blue Mushroom: glowing blue fungus
- Green Slime: viscous green substance
- Purple Crystal Dust: crushed magical crystal

### Potion themes

- Cooling Potion: blue
- Slime Dissolver: green
- Purification Potion: purple

### Brewing

Brewing should be quick. The challenge should be responding to emergencies, not
remembering recipes.

MVP brewing:

1. Collect one ingredient.
2. Go to the brewing station.
3. Press Interact.
4. Receive the resulting potion.

Not part of MVP:

- recipe menus
- multi-ingredient brewing
- brewing minigames

## Pressure and reward

### Disaster design rules

Every disaster should follow three rules:

1. The player can recognize it instantly.
2. Ignoring it increases pressure.
3. One correct potion solves it.

### Overheated Cauldron

- effect: generates heat and raises Panic steadily
- solution: Cooling Potion
- stretch escalation: bursts into magical fire

### Slime Leak

- effect: creates expanding slime pressure
- solution: Slime Dissolver
- stretch escalation: active spreading behavior

### Toxic Magic Cloud

- effect: contaminates part of the laboratory
- solution: Purification Potion
- stretch escalation: active expansion behavior

### Panic meter

The Panic Meter represents overall laboratory stability on a `0-100` scale.

Panic should rise because of unresolved disasters, escalations, and wrong
solutions. It should drop when the player resolves disasters correctly.

### Scoring intent

Scoring exists to reward efficient crisis management and replayability.

The design intent is:

- reward correct disaster resolution
- reward fast responses
- reward survival time
- avoid deep combo systems before the base loop is proven

See [`mvp-scope.md`](mvp-scope.md) for the locked numbers.

## Presentation intent

### Art direction

Style target:

- stylized low-poly fantasy

Why this works:

- faster production
- beginner-friendly art direction
- lower asset cost
- easier readability

Visual goals:

- strong colors
- clear silhouettes
- instantly readable disasters

### Audio direction

Every major system should have readable feedback.

Examples:

- fire: crackling
- slime: bubbling
- gas cloud: hissing
- brewing: liquid mixing
- panic meter: warning alarms at high Panic

## Design Boundary

Potion Panic is a finished small game target, not a platform for feature creep.

Do not let the design drift into:

- multiplayer
- open-world exploration
- RPG systems
- large crafting trees
- several-room progression
- story-heavy systems

If a new idea does not help the current milestone become playable, put it on
the [board](../board.md) instead of expanding the MVP.

## Related pages

- [MVP Scope](mvp-scope.md)
- [Potion Panic Runtime Contract](technical-architecture.md)
- [Unity Presentation Workflows](../guides/unity/presentation-workflows.md)
- [Design Research](../research/game-design-and-psychology.md)
