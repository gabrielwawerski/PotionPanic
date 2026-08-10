# Current Asset First QA

Date: 2026-08-10

## Scope

Reviewed the Task 3 Unity-native IMGUI implementation in
`CoordinationWindow.cs` and its view-model state contract.

## Static implementation findings

| Area | Result | Evidence |
| --- | --- | --- |
| Hierarchy | Implemented | Mode/status, warnings, current asset, one primary action, secondary actions, then compact row foldouts are rendered in that order. |
| Narrow layout | Implemented by code review | Below 560 pixels, target-source and secondary actions use vertical flow. The window minimum width is 430 pixels. |
| Copy and state | Implemented | The UI uses Coordinated/Manual labels, maps the internal disabled state to Manual, and exposes explicit freshness text. |
| Warnings | Implemented | Each record exposes save time, count, reason, owner, branch, task, error, and an individual confirmed reconciliation action. |
| Row interaction | Implemented | Presence, editing lease, and reservation rows expand independently of the current action target; one row detail key is retained at a time. |

## Visual and interaction acceptance blocker

The selected Current Asset First mock is not present in this checkout. A repository-wide asset search found only `Docs/public/logo.png` and `images/logo.png`; no selected mock, comparison image, or source design file exists. Therefore no 560-pixel comparison image can be produced, and typography, spacing, color, image/icon, and interaction differences against that reference cannot be classified.

The available Windows automation package also lacks the guidance API required by its own control instructions. No safe Unity Editor UI capture or keyboard-traversal run was performed. The 430, 560, and 900 pixel rendered checks and Manual/stale, Offline, WaitingForSnapshot, Live, local claim, remote claim, invalid path, empty, durable warning, and confirmation-flow interaction checks remain unperformed.

final result: blocked by the missing selected mock and unavailable compatible Unity UI automation
