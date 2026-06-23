# Potion Panic Docs Cleanup Review (Archived)

Status: Completed. This file records the cleanup pass that removed cross-doc inconsistencies before the implementation-readiness pass.

Active implementation status now lives in `implementation-readiness-review.md`.

## Purpose

Use this file as historical context for what was cleaned up.

Do not use it as the active implementation plan.

## Resolved Findings

### 1. Canonical gameplay data ownership was ambiguous

The technical architecture previously defined overlapping sources of truth for gameplay mappings:

- `IngredientData.resultingPotion`
- `PotionData.solvesDisasterType`
- `DisasterData.requiredPotion`

This was resolved by making `IngredientData.resultingPotion` and `DisasterData.requiredPotion` authoritative and treating any potion-to-disaster categorization as derived-only.

### 2. Panic and score rules were inconsistent

The GDD previously mixed Panic bonuses and score bonuses, while the technical architecture treated speed and combo ideas as scoring behavior. This was resolved by keeping speed and combo behavior in score systems and limiting MVP Panic changes to disaster-driven events and successful resolution.

### 3. Mid-game examples conflicted with MVP scope

The GDD previously used spreading and multiplying hazards as ordinary mid-game examples while later sections classified them as stretch or post-MVP. This was resolved by replacing those examples with overlapping-disaster and timer-pressure examples.

### 4. Brewing input wording was inconsistent

The GDD previously established a single interact key but separately said "Press Brew." This was resolved by documenting brewing as part of the same `Interact` flow used elsewhere.

### 5. Architecture guidance was duplicated across docs

`Potion Panic.md` previously repeated architecture rules that already belonged in the dedicated technical architecture doc. This was resolved by trimming the GDD architecture section to a pointer to the dedicated implementation doc.

### 6. Camera terminology was inconsistent

The GDD briefly mixed `top-down` and `isometric` wording. This was resolved by standardizing on `fixed top-down camera`.

## Outcome

The docs now separate responsibilities cleanly:

- `Potion Panic.md` owns player-facing design, scope, pacing, milestone intent, and locked MVP tuning.
- `Potion Panic - Technical Architecture.md` owns runtime structure, system responsibilities, and implementation-facing behavior rules.
- `implementation-readiness-review.md` owns the current readiness verdict and repo-specific handoff notes.
