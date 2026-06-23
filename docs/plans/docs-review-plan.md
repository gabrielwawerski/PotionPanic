# Potion Panic Docs Review and Cleanup Plan

## Review Findings

### 1. Canonical gameplay data ownership was ambiguous

The technical architecture previously defined overlapping sources of truth for gameplay mappings:

- `IngredientData.resultingPotion`
- `PotionData.solvesDisasterType`
- `DisasterData.requiredPotion`

This has been resolved by making `IngredientData.resultingPotion` and `DisasterData.requiredPotion` authoritative and treating any potion-to-disaster categorization as derived-only.

### 2. Panic and score rules were inconsistent

The GDD previously mixed Panic bonuses and score bonuses, while the technical architecture treated speed and combo ideas as scoring behavior. This has been resolved by keeping speed and combo behavior in score systems and limiting MVP Panic changes to disaster-driven events and successful resolution.

### 3. Mid-game examples conflicted with MVP scope

The GDD previously used spreading and multiplying hazards as ordinary mid-game examples while later sections classified them as stretch or post-MVP. This has been resolved by replacing those examples with overlapping-disaster and timer-pressure examples.

### 4. Brewing input wording was inconsistent

The GDD previously established a single interact key but separately said "Press Brew." This has been resolved by documenting brewing as part of the same `Interact` flow used elsewhere.

### 5. Architecture guidance was duplicated across docs

`Potion Panic.md` previously repeated architecture rules that already belonged in the dedicated technical architecture doc. This has been resolved by trimming the GDD architecture section to a pointer to the dedicated implementation doc.

### 6. Camera terminology was inconsistent

The GDD briefly mixed `top-down` and `isometric` wording. This has been resolved by standardizing on `fixed top-down camera`.

## Summary

The docs now use one authoritative technical doc for runtime structure and one product-facing GDD for design and scope. The remaining job for future reviews is to preserve that separation and prevent drift.

## Key Changes

- Keep the GDD product-facing and use `Potion Panic - Technical Architecture.md` for runtime structure, data ownership, and system responsibilities.
- Keep `IngredientData.resultingPotion` as the canonical ingredient-to-potion mapping.
- Keep `DisasterData.requiredPotion` as the canonical disaster-to-solution mapping.
- Keep speed and combo behavior tied to score, not Panic.
- Keep spreading or multiplying hazard behavior labeled stretch or post-MVP.
- Keep brewing documented under the single `Interact` input flow.
- Keep camera wording standardized as `fixed top-down camera`.

## Test Plan

- Re-read `Potion Panic.md` and `Potion Panic - Technical Architecture.md` after future edits and confirm each rule either appears once or points to one owner.
- Verify references to Panic, score, brewing input, camera terminology, and escalation behavior remain consistent across both docs.
- Verify milestone descriptions still match the declared MVP boundaries.

## Assumptions

- MVP should stay as simple as the technical architecture currently suggests.
- Speed and combo bonuses are intended to be score mechanics, not Panic mechanics.
- The dedicated technical architecture doc should be the only place with detailed runtime and component structure.
