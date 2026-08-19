# Project overview

These pages hold accepted Potion Panic decisions that remain true across tasks.
Read them before proposing scope or runtime changes.

## Choose by question

| You need to know...                                       | Open                                                                 |
|-----------------------------------------------------------|----------------------------------------------------------------------|
| What the player should experience and why                 | [Game Design](game-design.md)                                        |
| What the MVP includes, defers, and delivers in what order | [MVP Scope](mvp-scope.md)                                            |
| Which target runtime data and components own behavior     | [Runtime Design](technical-architecture.md)                          |
| What concrete artifacts each MVP milestone must deliver   | [MVP Deliverables](mvp-deliverables.md)                              |
| How general Unity concepts support those decisions        | [Unity Runtime Foundations](../guides/unity/runtime-architecture.md) |

## Reading order

1. Start with Game Design to understand the player-facing target.
2. Read MVP Scope before approving or starting feature work.
3. Read Runtime Design before adding or reshaping a gameplay system.
4. Read MVP Deliverables before decomposing milestone work into tickets.

The project documents describe accepted targets even when implementation has not
reached them. Each page must label current repository state separately from the
target it defines.

## Changing project truth

Propose a change in a ticket and use an implementation plan when the change
crosses several systems or acceptance gates. Update the owning project page only
after the decision is accepted. Keep temporary reasoning and execution notes out
of evergreen specifications.

## Related pages

- [Project Setup](../onboarding/getting-started.md)
- [Daily Workflow](../collaboration/team-workflow.md)
- [Unity Guides](../guides/unity/)
- [Documentation Atlas](../ATLAS.md)
