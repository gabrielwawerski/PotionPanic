# Unity Design and Implementation Thinking Guide

## Core Mental Model

Unity is not primarily “code that runs a game.”

A better mental model is:

```text
A scene-driven object composition engine
where code controls behavior attached to objects.
```

That means Unity design is not only about writing classes. It is about deciding how scene objects, prefabs, components, data assets, UI, runtime state, and systems fit together.

Think in layers:

```text
Scenes
GameObjects
Components
Prefabs
ScriptableObject data
Runtime state
Systems
UI and presentation
Save/load
```

Good Unity architecture keeps these layers from becoming tangled.

---

## Responsibilities Before Scripts

A script should not exist because “we need a script.” It should exist because a specific responsibility needs an owner.

A beginner-style `PlayerController` often becomes responsible for too much:

```text
movement
input
animation
health
damage
inventory
sound
UI updates
saving
quest triggers
```

That becomes fragile.

Prefer smaller components:

```text
PlayerInput
PlayerMovement
Health
DamageReceiver
Inventory
PlayerAnimationView
PlayerAudioFeedback
```

Each component should answer a clear question:

```text
What does this component own?
What is it allowed to change?
What should it not know about?
```

Example:

```csharp
public class Health : MonoBehaviour
{
  public int Current { get; private set; }
  public int Max { get; private set; }

  public event Action<int, int> Changed;
  public event Action Died;

  public void TakeDamage(int amount)
  {
    if (amount <= 0)
      return;

    Current = Mathf.Max(Current - amount, 0);
    Changed?.Invoke(Current, Max);

    if (Current == 0)
      Died?.Invoke();
  }
}
```

This `Health` component should not play animations, update UI, drop loot, or destroy the object. It owns health and announces changes.

---

## Logic and Presentation

Separate rules from visuals.

Bad coupling:

```csharp
public class Enemy : MonoBehaviour
{
  public Slider healthBar;

  public void TakeDamage(int damage)
  {
    health -= damage;
    healthBar.value = health;

    if (health <= 0)
    {
      animator.Play("Die");
      Destroy(gameObject);
    }
  }
}
```

This mixes combat logic, UI, animation, and lifetime handling.

Better structure:

```text
Health
- stores and changes health
- raises Changed and Died events

HealthBarView
- listens to Health
- updates UI

DeathAnimationView
- listens to death
- plays animation

LootDropper
- listens to death
- spawns loot

DestroyOnDeath
- handles object removal
```

This makes changes safer. Changing the health bar should not risk breaking combat.

---

## Composition Over Inheritance

Unity is built around composition.

Avoid deep gameplay inheritance trees unless there is a strong reason:

```text
Entity
  Character
    Enemy
      FlyingEnemy
        PoisonFlyingEnemy
```

This becomes rigid.

Prefer reusable components assembled on GameObjects:

```text
Enemy Slime
- Health
- DamageReceiver
- GroundMover
- ContactDamage
- DeathDropper
- AnimatorView

Enemy Bat
- Health
- DamageReceiver
- FlyingMover
- ContactDamage
- DeathDropper
- AnimatorView
```

The behavior comes from the component combination, not from a large class hierarchy.

Inheritance is still useful for pure C# abstractions. For scene objects, composition usually scales better.

---

## Data, Behavior, and Runtime State

Keep these concepts separate:

```text
ScriptableObject
- reusable configuration/data

MonoBehaviour
- behavior attached to a scene object or prefab

Runtime state
- current values during play
```

Good ScriptableObject use:

```csharp
[CreateAssetMenu(menuName = "Potion Panic/Potion Definition")]
public class PotionDefinition : ScriptableObject
{
  public string displayName;
  public Sprite icon;
  public int damage;
  public float radius;
}
```

Runtime behavior uses the data:

```csharp
public class PotionProjectile : MonoBehaviour
{
  [SerializeField] private PotionDefinition definition;

  public void Explode()
  {
    // Use definition.damage and definition.radius.
  }
}
```

Do not store changing runtime state in shared asset data unless the design explicitly requires it.

Risky:

```csharp
public class PlayerStats : ScriptableObject
{
  public int currentHealth;
}
```

Better:

```text
CharacterStatsDefinition
- max health
- base attack
- movement speed

CharacterStatsRuntime
- current health
- current modifiers
- temporary effects
```

---

## Communication Between Systems

Do not let everything talk to everything.

Fragile structure:

```text
Player -> UI
Player -> EnemySpawner
Player -> AudioManager
Enemy -> Player
Enemy -> GameManager
UI -> Player
GameManager -> Everything
```

Prefer communication styles based on distance and ownership.

### Direct References

Good for local, obvious relationships:

```text
Projectile has a Rigidbody.
HealthBarView references Health on the same object.
Door references its Animator.
EnemyAttack references its attack hitbox.
```

### Events

Good when one thing announces something and multiple systems may react:

```text
Health announces Died.
LootDropper reacts.
SoundFeedback reacts.
EnemyCounter reacts.
QuestObjective reacts.
```

The source does not need to know who is listening.

### Central Systems

Useful for broad services:

```text
AudioService
SaveSystem
SceneLoader
InputRouter
GameStateMachine
```

Avoid turning `GameManager` into a dumping ground for unrelated responsibilities.

---

## Game State and Flow

Many bugs come from unclear game state.

Avoid scattered booleans:

```csharp
bool isPaused;
bool isBrewing;
bool isInDialogue;
bool isGameOver;
bool isMenuOpen;
```

Prefer explicit states:

```csharp
public enum GameState
{
  MainMenu,
  Playing,
  Brewing,
  Paused,
  GameOver
}
```

For each state, define:

```text
allowed input
visible UI
active systems
paused systems
valid transitions
```

Example questions:

```text
Can the player move while brewing?
Can enemies spawn while paused?
Can the timer tick during dialogue?
Can combat continue during a menu?
```

A clear state model prevents accidental behavior.

---

## Smallest Playable Loop First

Do not begin with the full final architecture.

For Potion Panic, a good first loop is:

```text
player moves
one ingredient can be picked up
one cauldron can be used
one potion can be brewed
one enemy exists
potion damages enemy
enemy can die
level can restart
```

Avoid starting with:

```text
large recipe database
upgrade tree
save system
many enemy types
polished menus
full visual pipeline
complex progression
```

The key question is:

```text
What is the smallest playable version of this system?
```

Build that first. Expand after the loop works.

---

## Concrete Use Cases Before Abstractions

Do not build large generic frameworks for imagined future needs.

Usually too early:

```text
UniversalInteractableEntityAbstractFactoryManager
generic ability framework
generic quest framework
custom dependency injection framework
advanced inventory framework
```

Better early:

```text
Interactable
IngredientPickup
CauldronInteractable
DoorInteractable
PotionBrewer
```

A small interface may be enough:

```csharp
public interface IInteractable
{
  void Interact(GameObject interactor);
}
```

Build for the next few known features, not for fifty hypothetical ones.

---

## References and Dependencies

Prefer visible, intentional references.

Good:

```csharp
[SerializeField] private Health health;
[SerializeField] private Animator animator;
```

Risky as a default architecture:

```csharp
health = FindObjectOfType<Health>();
animator = GameObject.Find("EnemyAnimator").GetComponent<Animator>();
```

Good dependency sources:

```text
[SerializeField] references
GetComponent in Awake for same-object dependencies
explicit initialization from a spawner/factory
events for loose reactions
```

Validate important references early:

```csharp
private void Awake()
{
  if (health == null)
  {
    Debug.LogError($"{name} is missing Health reference.", this);
    enabled = false;
    return;
  }
}
```

Silent missing references are one of the most common Unity problems.

---

## Unity Lifecycle

Know when Unity calls each method.

```text
Awake
- self setup
- cache same-object components
- validate references

OnEnable
- subscribe to events

Start
- setup that depends on other objects being initialized

Update
- frame-based input and non-physics logic

FixedUpdate
- physics movement

OnDisable
- unsubscribe from events

OnDestroy
- cleanup
```

Common rule:

```text
Awake = self setup
Start = external setup
OnEnable = subscribe
OnDisable = unsubscribe
```

Example:

```csharp
private void OnEnable()
{
  health.Died += HandleDied;
}

private void OnDisable()
{
  health.Died -= HandleDied;
}
```

Do not subscribe to events and forget to unsubscribe.

---

## Singletons and Global Access

Singletons are convenient but can make a project brittle.

Risky pattern:

```csharp
GameManager.Instance.Player.Health.TakeDamage(5);
AudioManager.Instance.Play("Explosion");
UIManager.Instance.ShowDamageText();
SaveManager.Instance.Save();
```

A few global services can be valid:

```text
AudioService
SceneLoader
SaveSystem
InputService
```

But they should be global by design, not because passing references felt annoying.

If most scripts depend on `GameManager.Instance`, the project is probably becoming too coupled.

---

## Prefabs as Reusable Units

A prefab is a reusable unit of composition, not just a saved object.

Good prefab examples:

```text
Enemy_Slime
Enemy_Bat
PotionProjectile_Fire
Ingredient_Mushroom
Cauldron
HealthBarWorldSpace
DamagePopup
```

A good prefab has:

```text
clear purpose
required components
reasonable defaults
minimal scene-specific references
clean child hierarchy
clear naming
```

Avoid prefabs that secretly rely on one specific scene object unless that dependency is deliberate and documented.

---

## Folder Structure and Namespaces

Do not let everything fall into one folder.

Better structure:

```text
Assets/
  _Project/
    Scripts/
      Core/
      Gameplay/
        Player/
        Enemies/
        Potions/
        Ingredients/
        Interaction/
      UI/
      Audio/
      Saving/
    Prefabs/
    Scenes/
    ScriptableObjects/
    Art/
    Audio/
```

Use namespaces to clarify ownership:

```csharp
namespace PotionPanic.Gameplay.Potions
{
  public class PotionProjectile : MonoBehaviour
  {
  }
}
```

For a small project, keep this practical. Do not over-folder every tiny thing, but keep boundaries obvious.

---

## Naming

Names should describe responsibility.

Weak names:

```text
Manager
Controller
Handler
Thing
ObjectScript
NewScript
```

Better names:

```text
PotionBrewer
IngredientInventory
EnemySpawner
WaveDirector
HealthBarView
PlayerMovement
InteractionDetector
SceneTransition
```

A good name explains why the object or class exists.

---

## Avoid Update Dumps

`Update()` runs every frame. It is easy to misuse.

Bad:

```csharp
private void Update()
{
  CheckInput();
  CheckHealth();
  CheckEnemies();
  CheckInventory();
  UpdateUI();
  SaveSometimes();
  FindNearestEnemy();
}
```

Better:

```text
input components read input
movement components move
UI updates when data changes
enemy logic runs only when needed
timers use explicit timer logic
systems react to events
```

Prefer this:

```csharp
health.Changed += healthBar.SetValue;
```

Over this:

```csharp
private void Update()
{
  healthBar.value = health.Current;
}
```

Use `Update()` intentionally, not as a general dumping ground.

---

## UI as View, Not Rules

UI should display information and request actions. It should not own gameplay rules.

Bad:

```csharp
public class BrewButton : MonoBehaviour
{
  public void OnClick()
  {
    if (inventory.mushrooms >= 2 && inventory.slime >= 1)
    {
      inventory.mushrooms -= 2;
      inventory.slime -= 1;
      player.AddPotion();
    }
  }
}
```

Better:

```text
BrewButton
- asks PotionBrewer to brew a selected recipe

PotionBrewer
- validates ingredients
- consumes ingredients
- creates potion
- reports success/failure

BrewingView
- displays result
```

The button should not decide the rules.

---

## Failure Cases Before Code

Before implementing a system, ask:

```text
What if a reference is missing?
What if the object is destroyed?
What if the button is pressed twice?
What if the inventory is full?
What if the scene reloads?
What if this happens during pause?
What if two systems modify the same state?
```

For potion brewing:

```text
Can the player brew without ingredients?
Can brewing happen during combat?
Can brewing be canceled?
Can the cauldron be used during animation?
What happens if the potion inventory is full?
```

Thinking through failure cases early prevents patchy fixes later.

---

## Debug Visibility

Add debug tools early.

Useful examples:

```text
current game state display
current player inventory display
current interactable target
enemy spawn count
current potion recipe
detection radius gizmos
state transition logs
damage logs
```

Example:

```csharp
private void OnDrawGizmosSelected()
{
  Gizmos.DrawWireSphere(transform.position, interactionRadius);
}
```

A project without visibility becomes guesswork.

---

## State Ownership

Always ask:

```text
Who owns this data?
```

Examples:

```text
Health owns current health.
IngredientInventory owns collected ingredients.
PotionBrewer owns brewing validation.
WaveDirector owns wave progression.
GameStateMachine owns current game state.
SaveSystem owns serialization.
```

Avoid multiple systems owning the same state.

Bad:

```text
UI stores selected potion.
Player stores selected potion.
PotionSystem stores selected potion.
```

Better:

```text
PotionSelection owns selected potion.
UI displays it.
Player uses it.
```

Clear ownership prevents subtle bugs.

---

## Hardcoded Rules, Configured Content

There is a balance between hardcoding everything and abstracting everything.

Bad hardcoding:

```csharp
if (ingredient.name == "Red Mushroom")
{
  damage += 10;
}
```

Better:

```csharp
public class IngredientDefinition : ScriptableObject
{
  public string displayName;
  public IngredientType type;
  public int potency;
}
```

Good rule:

```text
Hardcode rules.
Configure content.
```

Example:

```text
Rule:
A recipe requires ingredients and produces a potion.

Config:
Fire Potion requires Red Mushroom + Sulfur.
Ice Potion requires Frost Herb + Crystal Dust.
```

---

## Suggested Potion Panic System Boundaries

A sensible early architecture:

```text
Player
- PlayerMovement
- PlayerInteraction
- PlayerPotionThrower
- Health

Ingredients
- IngredientDefinition
- IngredientPickup
- IngredientInventory

Brewing
- PotionRecipe
- PotionDefinition
- PotionBrewer
- CauldronInteractable

Combat
- DamageDealer
- DamageReceiver
- Health
- EnemyMovement
- EnemyAttack

Game Flow
- GameStateMachine
- LevelTimer
- EnemySpawner
- WinLoseController

UI
- InventoryView
- BrewingView
- HealthView
- TimerView
```

This gives enough structure without creating an overengineered framework.

---

## Implementation Thinking Checklist

Before coding a feature, answer these:

```text
What should the player experience?
What data exists?
Who owns that data?
What scene objects are involved?
What components are needed?
How do those components communicate?
What should not know about what?
What failure cases exist?
What is the smallest playable version?
```

Example for brewing:

```text
Player-facing behavior:
The player picks up ingredients and uses the cauldron to brew a potion.

Data:
Ingredient counts, recipe requirements, potion result.

Ownership:
IngredientInventory owns counts.
PotionRecipe defines requirements.
PotionBrewer owns validation.

Scene objects:
Player, pickups, cauldron, UI panel, potion prefab.

Communication:
PlayerInteraction calls IInteractable.Interact().
CauldronInteractable opens BrewingView.
BrewingView asks PotionBrewer to brew.
PotionBrewer checks IngredientInventory.
Inventory emits Changed.
UI updates from the event.
```

---

## Final Architecture Rule

Good Unity design usually looks like this:

```text
small components
clear ownership
data in ScriptableObjects
runtime state in components/classes
events for reactions
Inspector references for local dependencies
explicit systems for global rules
minimal Update usage
no giant GameManager
```

Danger signs:

```text
one huge Player script
one huge GameManager
everything public
everything finds everything
UI owns game rules
scene objects secretly depend on each other
runtime state stored in shared assets
```

Aim for simple but separated. The goal is not maximum abstraction. The goal is that changing one feature does not unpredictably break five others.
