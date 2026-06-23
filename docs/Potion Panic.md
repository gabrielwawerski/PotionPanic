# Potion Panic

## Game Design Document (GDD)

Version: 0.1  
Project Type: Small Scope 3D Indie Game  
Team Size: 2 Developers  
Target Engine: Unity  
Target Duration: 4–8 Weeks  
Target Platform: PC

---

# Elevator Pitch

Potion Panic is a stylized 3D laboratory survival game where the player takes the role of an apprentice alchemist trapped inside a magical laboratory that is rapidly descending into chaos.

Disasters continuously appear throughout the laboratory. The player must gather ingredients, brew the correct potions, and resolve emergencies before the laboratory's Panic Meter reaches 100%. The accumulation of unresolved disasters spikes the panic meter until it reaches its limit and the player loses the game.

The entire game takes place inside a single room, allowing the team to focus on gameplay polish, game feel, visual feedback, and completing a finished project rather than producing large amounts of content.

---

# Vision Statement

A small but complete game that:

- Is realistic for a beginner 2-person Unity team
- Teaches core Unity systems
- Feels polished despite small scope
- Has replayability through score chasing
- Uses 3D environments without requiring open-world content

The goal is not to create a complex alchemy simulator.

The goal is to create a fun and chaotic "crisis management" game.

---

# Design Pillars

## 1. Controlled Chaos

The laboratory should always feel busy and dangerous.

The player should constantly have something important to do.

The game should create pressure without becoming confusing.

Good feeling:

> "Everything is going wrong, but I know what I need to solve next."

Bad feeling:

> "I have no idea what is happening."

---

## 2. Fast Decision Making

The challenge comes from prioritization.

The player cannot solve every problem immediately.

They must constantly decide:

- Which disaster is most dangerous?
- Which potion should I brew next?
- Can this problem wait?

The game should reward good decisions.

---

## 3. Simple Systems, High Pressure

Individual systems should remain simple.

Complexity should emerge from multiple simple systems happening at once.

Avoid:

- Complex crafting
- Large inventories
- Deep RPG mechanics

Prefer:

- Easy-to-understand mechanics
- Increasing pressure
- Faster pacing

---

# Core Gameplay Loop

1. Disaster appears.
2. Panic Meter increases.
3. Player identifies disaster.
4. Player gathers ingredients.
5. Player brews potion.
6. Player applies potion.
7. Disaster is resolved.
8. Panic decreases.
9. New disaster appears (sometimes, while fixing an ongoing disaster).
10. The difficulty slowly increases as more disasters appear.

Repeat until Panic reaches 100%.

---

# Player Experience

## Early Game

Player learns:

- Movement
- Interactions
- Ingredient collection
- Brewing
- Disaster resolution

Panic rises slowly.

---

## Mid Game

Two active disasters.

Player begins prioritizing.

Examples:

- Overheated Cauldron and Slime Leak active at the same time
- Faster escalation timers forcing quick prioritization

Player must choose which to solve first.

---

## Late Game

Three or more disasters.

Disasters escalate faster.

Panic rises aggressively.

Player survival depends on efficiency.

---

# Camera

## Fixed top-down camera

Advantages:

- Easier implementation
- Good visibility
- Minimal camera bugs
- Beginner friendly



Camera remains stationary above the room.



# Player Systems

## Movement

Controls:

- WASD movement

- Interact key



Player should feel responsive and quick.

---

## Interaction System

Single interaction button.

Used for:

- Collecting ingredients
- Using brewing station
- Applying potions
- Activating objects

---

## Inventory

MVP inventory uses one carried item slot.

The player can carry:

- one ingredient

OR

- one potion

The player cannot carry both at the same time.



This removes inventory management complexity and keeps the focus on movement, prioritization, brewing, and disaster response.

---

# Laboratory Layout

The entire game takes place inside one room.

---

## Main Areas

### Brewing Station

Central location.

Used to create potions.

---

### Ingredient Stations

Permanent ingredient sources.

Examples:

- Mushroom Shelf
- Slime Tank
- Crystal Grinder

---

### Disaster Zones

Locations where disasters may appear.

Placed around the room.

---

### UI Area

Displays:

- Panic Meter
- Score
- Active warnings

---

# Ingredients

## Blue Mushroom

Used for cooling potions.

Visual Theme:

Blue glowing fungus.

---

## Green Slime

Used for dissolving potions.

Visual Theme:

Viscous green substance.

---

## Purple Crystal Dust

Used for purification potions.

Visual Theme:

Crushed magical crystal.

---

# Potions

## Cooling Potion

Purpose:

Extinguishes fires.

Color:

Blue.

---

## Slime Dissolver

Purpose:

Removes slime outbreaks.

Color:

Green.

---

## Purification Potion

Purpose:

Removes magical contamination.

Color:

Purple.

---

# Brewing System

## Design Goal

Brewing should be quick.

The challenge should be responding to emergencies.

Not remembering recipes.

---

## MVP Brewing

Collect ingredient.

Approach brewing station.

Press Interact.

Receive potion.

Simple and fast.

---

## Optional Upgrade

Require two ingredients per potion.

Only if MVP already feels good.

---

# Disaster System

Every disaster follows three rules.

---

## Rule 1: Easy Recognition

Player instantly understands the problem.

---

## Rule 2: Escalation

Ignoring the disaster makes it worse.

---

## Rule 3: Single Solution

One correct potion solves it.

---

# Disaster Type 1: Overheated Cauldron

### Effect

Generates heat.

Increases panic steadily.

### Escalation

~~Eventually bursts into magical fire.~~   <mark>**Stretch feature**</mark>

### Solution

Cooling Potion.

---

# Disaster Type 2: Slime Leak

### Effect

Creates expanding slime patches.

### Escalation

~~Slime spreads to nearby areas.~~   <mark>**Stretch feature**</mark>

### Solution

Slime Dissolver.

---

# Disaster Type 3: Toxic Magic Cloud

### Effect

Contaminates sections of the laboratory.

### Escalation

~~Cloud expands.~~   <mark>**Stretch feature**</mark>

### Solution

Purification Potion.

---

# Panic Meter

Represents overall laboratory stability.

Range:

0–100

---

## Panic Increases When

- ~~Time passes~~
- Disasters remain active
- Disasters escalate
- Player uses wrong solution

---

## Panic Decreases When

- Disaster solved

---

## Lose Condition

Panic reaches 100%.

Laboratory collapses.

Game Over.

---

# Difficulty Scaling

The game becomes harder over time.

---

## Stage 1

One active disaster.

---

## Stage 2

Two active disasters.

---

## Stage 3

Three active disasters.

---

## Stage 4

Higher pressure version of existing disasters.

Examples:

- Faster disaster spawn rate
- Shorter escalation timers
- Higher Panic increase rates
- More frequent overlapping disasters

New disaster variants such as Large Fire, Mutated Slime, and Corrupted Cloud are **<u>stretch features.</u>**

---

# Score System

Points awarded for:

- Solving disasters
- Fast solutions
- Long survival

Optional <u>**after**</u> MVP:

- Combo chains

Purpose:

Encourage replayability.

---

# Art Direction

## Style

Stylized Low Poly Fantasy.

References:

- Small indie games
- Cozy fantasy aesthetics
- Cartoon proportions

---

## Advantages

- Fast production
- Beginner friendly
- Lower asset requirements
- Easy optimization

---

## Visual Goals

- Strong colors
- Clear silhouettes
- Readable gameplay

Every disaster should be identifiable instantly.

---

# Audio Direction

Every major system should have audio feedback.

---

## Fire

Crackling.

---

## Slime

Bubbling.

---

## Gas Cloud

Hissing.

---

## Brewing

Liquid mixing sounds.

---

## Panic Meter

Warning alarms at high panic.

---

# Technical Architecture

The detailed runtime structure, data ownership rules, and system responsibilities live in `Potion Panic - Technical Architecture.md`.

Use this GDD for player-facing design, scope, pacing, and milestone intent.

---

# MVP Checklist

## Core Gameplay

- Player movement
- Interaction system
- Brewing station
- Ingredient collection

---

## Content

- 3 ingredients
- 3 potions
- 3 disasters

---

## UI

- Main menu
- Panic meter
- Score display
- Game over screen

---

## Polish

- Basic sounds
- Basic particles
- Basic animations

---

# Milestones

The project should be built as a playable vertical slice first, then expanded into the full MVP.

The priority is:

> Make one complete loop playable before adding more content.

---

## Milestone 1: Movement and Camera

Goal:

Create the basic player control foundation.

Deliverables:

- Fixed top-down camera

- WASD player movement

- Player collision

- Basic laboratory blockout

- Movement tuned for responsiveness

Notes:

- Mouse look is not part of the MVP.

- Movement should be camera-relative if needed.

- The laboratory does not need final art yet.

---

## Milestone 2: Interaction System

Goal:

Allow the player to interact with objects in the laboratory.

Deliverables:

- Single interact key

- Detect nearby interactable objects

- Interaction prompt

- Basic object highlighting

- Reusable interactable interface/component

Used for:

- Ingredient stations

- Brewing station

- Disaster cleanup

- Restart/menu interactions if needed

Notes:

- Keep the interaction system generic.

- Avoid special-case interaction logic inside the player controller.

---

## Milestone 3: Ingredient to Potion Loop

Goal:

Create the first complete non-disaster gameplay loop.

Deliverables:

- One ingredient station

- Brewing station

- Player can pick up one ingredient

- Player can brew one potion

- Player can carry one ingredient or one potion

- UI displays currently carried item

MVP Rule:

> The player can carry one ingredient OR one potion at a time.

Example:

1. Pick up Blue Mushroom.

2. Go to Brewing Station.

3. Press Interact.

4. Blue Mushroom becomes Cooling Potion.

5. Player now carries Cooling Potion.

Notes:

- No full inventory.

- No recipe menu.

- No multi-ingredient recipes in MVP.

---

## Milestone 4: First Disaster

Goal:

Make the game technically playable with one disaster.

Deliverables:

- One disaster type: Overheated Cauldron

- Disaster spawns in the lab

- Disaster increases Panic while active

- Cooling Potion resolves the disaster

- Panic decreases when resolved

- Wrong potion fails clearly or increases Panic

Vertical Slice Example:

> Blue Mushroom → Cooling Potion → Overheated Cauldron

Notes:

- This is the first true playable version of the game.

- Do not add the other two disasters before this loop works properly.

---

## Milestone 5: Core Game Loop

Goal:

Turn the vertical slice into a repeatable game loop.

Deliverables:

- Disasters spawn repeatedly

- Panic reaches 100%

- Game Over state

- Restart run

- Basic score display

- Basic run timer or survival tracking

Notes:

- At this stage, the game should be playable from start to finish.

- The game may still have only one disaster type.

---

## Milestone 6: Full MVP Content

Goal:

Add the remaining MVP content using the existing systems.

Deliverables:

- 3 ingredients

- 3 potions

- 3 disasters

Required mappings:

| Ingredient          | Potion              | Solves              |
| ------------------- | ------------------- | ------------------- |
| Blue Mushroom       | Cooling Potion      | Overheated Cauldron |
| Green Slime         | Slime Dissolver     | Slime Leak          |
| Purple Crystal Dust | Purification Potion | Toxic Magic Cloud   |

Notes:

- Each disaster should use the same base disaster system.

- Avoid building three completely separate custom systems.

---

## Milestone 7: Difficulty and Scoring

Goal:

Add replayability and pressure.

Deliverables:

- Difficulty increases over time

- More active disasters as the run progresses

- Score awarded for resolving disasters

- Bonus points for fast response

- Optional combo bonus for consecutive correct solutions

Suggested difficulty stages:

| Stage   | Behavior                                    |
| ------- | ------------------------------------------- |
| Stage 1 | One active disaster                         |
| Stage 2 | Two active disasters                        |
| Stage 3 | Three active disasters                      |
| Stage 4 | Faster escalation and higher Panic pressure |

Notes:

- Difficulty should increase gradually.

- Avoid sudden unfair spikes.

- Panic should primarily come from active disasters, not passive time.

---

## Milestone 8: Menus and Run Flow

Goal:

Wrap the game in a complete start-to-finish structure.

Deliverables:

- Main menu

- Pause menu

- Game Over screen

- Restart button

- Quit button

- Final score display

Notes:

- The game should be playable without using the Unity editor.

- Restarting should reset the full run state correctly.

---

## Milestone 9: Audio and Visual Feedback

Goal:

Make the game readable and satisfying.

Deliverables:

- Brewing sound

- Ingredient pickup sound

- Disaster warning sounds

- Correct potion feedback

- Wrong potion feedback

- Panic warning alarm

- Basic particles for disasters

- Basic particles for resolving disasters

Notes:

- Readability is more important than visual complexity.

- Each disaster should be instantly recognizable.

---

## Milestone 10: Polish, Balancing, and Bug Fixing

Goal:

Turn the prototype into a finished small game.

Deliverables:

- Tune movement speed

- Tune disaster spawn timing

- Tune Panic increase/decrease values

- Improve UI readability

- Improve disaster visibility

- Fix major bugs

- Add final art pass where possible

- Add final audio pass where possible

Completion Rule:

> The project is done when a player can start, play, lose, see their score, restart, and play again without gameplay-breaking bugs.

---

# Scope Rules

The following features are forbidden until MVP is finished:

- Multiplayer
- Quest systems
- Dialogue systems
- Story campaign
- Crafting trees
- Skill trees
- Large inventories
- Multiple rooms
- Open world areas
- Procedural generation
- Combat systems

Any new idea should be added to a backlog document rather than implemented immediately.

---

# Future Expansion Ideas

Potential post-release features:

- Additional laboratories
- New ingredient types
- New disaster families
- Endless mode
- Daily challenge mode
- Laboratory upgrades
- Cosmetic unlocks
- Boss disasters
- Achievement system
- Disaster escalation

---

# Definition of Done

Potion Panic is considered complete when:

- A player can launch the game
- Play from start to finish
- Resolve disasters
- Lose by reaching 100% panic
- Earn a score
- Restart and play again

without bugs that prevent gameplay.

A finished small game is more valuable than a large unfinished game.
