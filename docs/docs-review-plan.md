# Potion Panic Docs Review and Cleanup Plan

## Review Findings

### 1. Canonical gameplay data ownership is ambiguous

The technical architecture currently defines overlapping sources of truth for gameplay mappings:

- `IngredientData.resultingPotion`
- `PotionData.solvesDisasterType`
- `DisasterData.requiredPotion`

As written, those mappings can drift apart. The docs need one authoritative ownership model.

### 2. Panic and score rules are inconsistent

The GDD says Panic decreases from fast response bonuses and consecutive successful solutions, while the technical architecture frames those ideas as scoring behavior. The docs should assign speed and combo logic to one system consistently.

### 3. Mid-game examples conflict with MVP scope

The GDD uses examples such as spreading fire and multiplying slime in the normal mid-game description, but later both docs classify those behaviors as stretch or post-MVP escalation. That creates scope confusion for implementation.

### 4. Brewing input wording is inconsistent

The GDD establishes a single interact key, but the brewing section separately says "Press Brew." The docs should describe brewing as part of the same `Interact` flow used elsewhere.

### 5. Architecture guidance is duplicated across docs

`Potion Panic.md` repeats architecture rules that already belong in the dedicated technical architecture doc. That duplication is already causing drift and should be reduced.

## Summary

Resolve the spec contradictions, keep one authoritative technical doc, and make the MVP rules implementation-safe without expanding scope.

## Key Changes

- Keep the GDD product-facing. Remove or heavily trim the duplicated architecture section in `Potion Panic.md` and replace it with a short pointer to the technical architecture doc.
- Declare one canonical gameplay mapping:
  - `IngredientData.resultingPotion` owns ingredient-to-potion mapping.
  - `DisasterData.requiredPotion` owns disaster-to-solution mapping.
  - `PotionData.solvesDisasterType` is removed from the docs or explicitly marked derived and non-authoritative.
- Normalize Panic rules:
  - Panic changes only from active disasters, resolution, escalation, and wrong-potion penalties.
  - Fast response and consecutive success affect score only in MVP.
- Normalize scope wording:
  - Replace mid-game examples that imply spreading or multiplying hazards with examples based on simultaneous active disasters and faster timers.
  - Keep spreading fire, growing slime, and expanding clouds clearly labeled post-MVP or stretch.
- Normalize input wording so brewing is always performed through the single `Interact` action.

## Test Plan

- Re-read both docs after edits and confirm each rule appears once or points to one owner.
- Verify every reference to Panic, score, brewing input, and escalation behavior is consistent across both files.
- Verify milestone descriptions still match the declared MVP boundaries.

## Assumptions

- MVP should stay as simple as the technical architecture currently suggests.
- Speed and combo bonuses are intended to be score mechanics, not Panic mechanics.
- The dedicated technical architecture doc should be the only place with detailed runtime and component structure.
