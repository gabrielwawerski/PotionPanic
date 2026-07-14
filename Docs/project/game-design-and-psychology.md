# Game Design and Player Psychology

> **Temporary location:** This research guide remains under `Docs/project/` for
> now and is intended to move to a dedicated research or reference section
> later. Its current location does not make it a source of locked Potion Panic
> requirements.

## Purpose

This document is a research and reference guide for evaluating:

- player motivation and attention
- game feel and feedback
- healthy and unhealthy engagement
- retention and replayability
- gameplay and story tradeoffs
- small-team production risk
- market communication
- prototype and playtest evidence

It is not a design specification and does not override current project
requirements.

For authoritative Potion Panic decisions, use:

- [`game-design.md`](game-design.md) for player-facing design
- [`mvp-scope.md`](mvp-scope.md) for locked scope, tuning, and milestone order
- [`technical-architecture.md`](technical-architecture.md) for implementation
  boundaries and runtime ownership

Recommendations in this guide are heuristics, not mandatory rules. Test them
against the project, audience, team, budget, and current market.

## How to Use This Guide

The document uses several kinds of statements:

- **Research finding:** tied to published research or an established external
  definition.
- **Industry guidance:** advice from a platform holder, developer, or industry
  organization.
- **Design heuristic:** useful in many projects but not universally correct.
- **Project decision:** binding only when recorded in a project's canonical
  design, scope, or architecture documents.

Treat market observations as time-sensitive. Treat psychology research as a
framework for asking better questions, not as a recipe for manipulating
players.

## Executive Summary

Strong games do not succeed because they simply "hack dopamine." That framing
is too crude and encourages shallow or manipulative design.

Games are more sustainably engaging when they repeatedly provide:

- clear goals
- meaningful action
- immediate and understandable feedback
- visible improvement
- fair challenge
- player agency
- curiosity
- an understandable fantasy
- enough variation for systems to produce new situations

Self-determination theory is a useful starting framework. It proposes that
motivation improves when experiences support **autonomy**, **competence**, and
**relatedness** [1]. Flow research adds that focused engagement is more likely
when challenge and skill are reasonably aligned, goals are clear, and feedback
is immediate.

For a small indie team, a lower-risk direction is usually a gameplay-first or
gameplay-led hybrid with a clear fantasy, a short core loop, strong feedback,
and reusable systems that can be validated before large content production.

Story can strengthen identity, emotion, and repetition. It becomes risky when
substantial authored content must exist before the core interaction is
interesting.

Ethical engagement should leave the player free to stop. High playtime alone is
not evidence of addiction. Harm is associated with impaired control, play
taking priority over other activities, and continuation despite negative
consequences [2].

## Key Findings

| Area | Main finding | Practical implication |
| --- | --- | --- |
| Motivation | Players return for competence, autonomy, curiosity, expression, belonging, status, collection, or narrative pull. | Select motivations the game can support well. |
| Retention | Durable retention comes from loops operating at different time scales. | Give reasons to continue now, finish the session, and return later. |
| Game feel | Perceived fun depends heavily on responsiveness, clarity, impact, rhythm, and feedback. | Validate feel before producing large amounts of content. |
| Mastery | Improvement is motivating when failure teaches something. | Make cause, effect, and recovery readable. |
| Ethics | Replayability does not require anxiety, obligation, or paid randomness. | Prefer mastery, curiosity, expression, and fair variation. |
| Marketability | Potential players must quickly understand the fantasy and activity. | Show actual play, readable stakes, and the core verb early. |
| Small-team viability | Systemic replayability often scales better than content-heavy production. | Favor reusable interactions over large bespoke libraries. |
| Story | Story is efficient when it reinforces the loop and fantasy. | Use narrative to add meaning, not to compensate for weak interaction. |

## Human Psychology and Game Engagement

### Attention as an Active Loop

Games hold attention through an active cycle:

1. A goal or problem appears.
2. The player acts.
3. The game responds.
4. The player updates their understanding.
5. A new or revised goal appears.

Attention is easier to sustain when the game provides:

- clear goals
- readable threats
- manageable uncertainty
- immediate feedback
- frequent opportunities to correct mistakes
- short recovery after failure
- low friction between intention and action

| Curiosity-led attention | Anxiety-led attention |
| --- | --- |
| "I want to try a better strategy." | "I must log in or lose my streak." |
| "I almost understood that pattern." | "This reward disappears tonight." |
| "I can execute this more cleanly." | "I already paid, so I have to grind." |
| "What happens if I combine these systems?" | "I will fall behind if I stop." |

### Motivation

Common continuation drivers include:

- **Competence:** "I am getting better."
- **Autonomy:** "I chose this approach."
- **Curiosity:** "I want to know what happens next."
- **Mastery:** "I can perform this more cleanly."
- **Expression:** "This build, layout, or solution is mine."
- **Collection:** "I am close to completing a meaningful set."
- **Relatedness:** "I feel connected to these people or characters."
- **Status:** "My skill or achievement is recognized."
- **Narrative pull:** "I want to see how the situation develops."

Self-determination theory is useful because autonomy, competence, and
relatedness describe broad human needs rather than narrow game mechanics [1].
A reward is meaningful when it expands choice, shows improvement, or reinforces
connection—not merely because it increases a number.

### Reward Systems

Rewards work best when they are:

- understandable
- connected to player action
- proportional to the achievement
- relevant to future choices
- frequent enough to maintain rhythm
- rare enough to remain meaningful
- presented clearly enough to be noticed

A stronger reward communicates that a new option is available, a meaningful
objective was completed, useful information was gained, or mastery was shown.
Extrinsic rewards should reinforce interesting decisions rather than replace
them.

### Flow and Focused Engagement

Flow is commonly associated with:

- clear goals
- immediate feedback
- focused attention
- a sense of control
- demanding but understandable challenge
- difficulty that broadly tracks growing skill

Flow is disrupted by arbitrary difficulty spikes, unclear failure causes,
inconsistent controls, disproportionate punishment, excessive downtime,
distracting presentation, and vague goals.

> Pressure should increase the importance of decisions without making the
> situation impossible to read.

Difficulty alone does not produce flow. A difficult task can be boring when
repetitive, frustrating when opaque, or exhausting when recovery is too slow.

### Emotion and Stress

Stress is not automatically harmful inside play. Players often seek demanding
experiences when the stress feels chosen, bounded, and meaningful.

| Design pattern | Likely emotional effect |
| --- | --- |
| Safe routine with visible progress | Relaxation |
| Fair challenge with readable improvement | Confidence and pride |
| Limited information in horror | Tension and fear |
| Social comparison or ranked stakes | Excitement or stress |
| Expiring rewards and hard FOMO | Anxiety and obligation |
| Unclear failure and harsh punishment | Frustration |

## Healthy and Compulsive Engagement

### The Core Difference

Healthy engagement generally means:

- the player wants to return
- the player remains free to stop
- progress feels earned and understandable
- the game fits into normal life
- play leaves the player capable, curious, satisfied, or meaningfully moved

Compulsive or harmful engagement can involve impaired control, play taking
priority over other activities, continuation despite negative consequences,
and distress when absent.

The World Health Organization's gaming disorder definition centers on impaired
control, increasing priority given to gaming, and continuation or escalation
despite negative consequences [2]. High playtime alone is not sufficient
evidence of disorder.

> Does the design respect player agency, informed choice, time, and money?

### Engagement Mechanics by Risk Level

| Mechanic | Lower-risk use | Higher-risk failure mode |
| --- | --- | --- |
| Random rewards | Transparent, earned, non-monetized variation | Paid randomness, opaque odds, near-miss pressure |
| Procedural variation | Rule-driven, fair, learnable situations | Random noise presented as depth |
| Unlocks | Meaningful options at a reasonable pace | Artificially stretched content |
| Collections | Finite, readable, optional sets | Monetized completion pressure |
| Daily tasks | Optional, non-punitive goals | Chores, missed-value anxiety, streak pressure |
| Streaks | Private tracking with no loss | Punishment for missing days |
| Battle passes | Transparent cosmetic goals | Expiring paid grind |
| Limited events | Re-runnable or low-stakes novelty | Fear of missing unique rewards |
| Ranked ladders | Fair rules and transparent ranking | Status anxiety or manipulation |
| Gacha | Difficult to justify with real-money spending | Gambling-like pressure and opaque value |
| Social systems | Voluntary cooperation | Attendance pressure and social guilt |

Research has repeatedly found associations between loot-box spending and
problem-gambling measures [3]. Association does not establish that every
randomized system causes harm, but real money, uncertain outcomes, and
psychological pressure deserve a high standard of care.

Risk increases when several pressures are combined:

- real-money spending
- randomness or opaque odds
- limited-time availability
- power advantages
- collection completion pressure
- social comparison
- minors or vulnerable players
- near-miss presentation
- sunk-cost pressure

### Ethical Alternatives

Lower-risk retention can come from:

- mastery
- fair difficulty escalation
- strategy variety
- expressive customization
- visible improvement
- optional challenges
- finite collections
- non-monetized variation
- emergent interaction
- player-created goals
- replayable scenarios
- stories generated by play

> If the game remains compelling without FOMO, paid randomness, punitive
> streaks, or social guilt, its engagement is more likely to be durable and
> respectful.

## Retention Loops

| Time scale | Player question | Common design tools |
| --- | --- | --- |
| Moment-to-moment | "Does acting feel good and clear?" | controls, sound, impact, animation, readable state changes |
| Minute-to-minute | "What should I do next?" | threats, tactical choices, resources, routes, short goals |
| Session | "What am I trying to finish?" | mission, run, day, floor, boss, quota, chapter |
| Multi-session | "What am I working toward?" | unlocks, collections, upgrades, relationships, mastery goals |
| Long-term | "What kind of player or creator am I becoming?" | expertise, expression, community, challenge modes |

Not every game needs every layer. A short arcade game may rely on immediate
feel, run mastery, and score improvement. A narrative game may use story
curiosity instead of long-term mechanical progression.

### Strong Pattern

```text
Clear action → satisfying feedback → meaningful result → new decision
→ visible progress → renewed curiosity
```

### Weak or Predatory Pattern

```text
Timer → obligation → grind → temporary relief → new timer
```

Replayability can emerge from interacting systems, meaningful tradeoffs,
understandable variation, skill growth, different routes, risk/reward choices,
expressive solutions, and low retry friction.

Randomness helps only when players can interpret and respond to it. Variation
that overrides skill or obscures outcomes can reduce replay value.

## Player Motivations

Player categories are not fixed identities. Most people shift motivations
between games and sessions. Segmentation is a design lens, not a complete
description of a person.

| Motivation | Common enjoyment | Common frustration | Small-team viability |
| --- | --- | --- | --- |
| Achievement | goals, completion, visible progress | unclear or impossible requirements | High |
| Mastery | fair challenge, execution, optimization | randomness overriding skill | High |
| Exploration | secrets, spaces, discovery | empty content or excessive handholding | Medium |
| Collection | sets, unlocks, cosmetics | missable FOMO and opaque drops | High when finite |
| Competition | ranked tests and comparison | imbalance and unfair matchmaking | Low to medium |
| Social connection | cooperation and shared stories | empty populations and toxic interaction | Medium when controlled |
| Building and expression | layouts, bases, customization | restrictive systems | High |
| Roleplay | identity, choices, world consistency | shallow or contradictory fiction | Medium |
| Narrative | plot, character, mystery, emotion | pacing problems and weak writing | Medium to low unless writing is a strength |
| Relaxation | routine, safety, gentle progress | excessive pressure or punishment | High |
| Challenge | difficult tests and execution | unclear telegraphs and unfair consequences | Medium |

Small teams can often serve mastery, optimization, finite collection, tactical
problem-solving, short-session replay, cozy completion, and expression through
reusable systems.

Risk rises when the intended motivation depends on huge content volume,
constant updates, a large multiplayer population, cinematic production,
extensive bespoke animation, deep competitive balance, large rosters, or
expensive localization.

## Game Feel and Feedback

### Why Game Feel Matters

Game feel is part of how the player understands the simulation. Useful feedback
communicates:

- what happened
- why it happened
- whether it was positive or negative
- how important it was
- what changed
- what the player can do next

A mechanically sound game can feel weak when cause, impact, timing, or
consequence is unclear.

### High-Value Feedback Patterns

| Pattern | What it communicates | Common failure mode |
| --- | --- | --- |
| Brief hit pause | impact and weight | excessive interruption |
| Screen shake | force, danger, scale | discomfort and reduced readability |
| Particles | contact, magic, rarity, destruction | effects hiding important state |
| Numbers and meters | magnitude and progression | information overload |
| Sound cues | impact, danger, success, rarity | repetition and fatigue |
| Animation anticipation | what is about to happen | delayed controls |
| Squash and stretch | physicality and character | tonal mismatch or excess motion |
| Reward animation | achievement and progress | long interruption |
| UI response | confirmed input and state change | decorative delay |
| Escalating intensity | urgency and momentum | clutter without new decisions |
| Physics response | surprise and agency | unstable or unfair outcomes |
| Destruction | visible power and consequence | high production and performance cost |

Accessibility matters. Shake, flashes, contrast, motion, sound reliance, and
small text should be adjustable where they could reduce comfort or exclude
players.

Commonly effective mechanics include responsive movement, precise timing,
risk/reward decisions, synergies, visible escalation, meaningful upgrades,
tactical positioning, readable telegraphs, short retries, near-miss survival,
reactive environments, secrets, and optimization with visible results.

Their popularity does not mean every game should include them. A mechanic is
valuable only when it strengthens the intended experience and fits the scope.

### Feedback Hierarchy

- **Primary:** immediate danger, major success, failure, objective change
- **Secondary:** resource gain, cooldown, status change, progress update
- **Ambient:** atmosphere, world reactivity, decorative motion

When everything flashes, shakes, and makes a loud sound, nothing feels
important.

## Gameplay and Story Models

Most games are hybrids. The useful question is:

> Does narrative strengthen the core interaction, or must the player consume
> large amounts of narrative before the interaction becomes compelling?

| Model | Strengths | Weaknesses | Small-team risk |
| --- | --- | --- | --- |
| Gameplay-first | replayability, clear systems, lower marginal content cost | can feel emotionally thin | Often lower |
| Story-first | emotional impact, character, authored pacing | content-heavy and difficult to validate early | Higher unless writing is a core strength |
| Gameplay-led hybrid | reusable loop with narrative reinforcement | requires integration discipline | Strong general target |
| Story-led hybrid | authored narrative with an interactive premise | must meet both quality bars | Viable with a sharp premise |
| Sandbox or systemic | autonomy, emergence, expression | onboarding and UX complexity | Strong when scope is controlled |

Story helps when it clarifies the fantasy, makes repetition meaningful,
differentiates a familiar mechanic, motivates progression, creates memorable
characters, turns failure into continuation, or helps players retell events.

Story hurts when it delays the first meaningful interaction, requires extensive
reading before motivation exists, creates large production scope, conflicts
with replay pacing, repeatedly interrupts rhythm, or becomes the only reason
the pitch sounds interesting.

Prioritize the playable loop and support it with a focused theme, world, mood,
and limited authored story. Increase narrative investment when writing is a
team strength and narrative strengthens decisions, repetition, or progression.
Do not apply a universal percentage split.

## Genre and Market Considerations

> **Review date: July 2026.** This section is directional research, not a
> permanent forecast. Revalidate genre demand, saturation, platform visibility,
> tools, and audience expectations before major commercial decisions.

Labels such as "crowded," "high production risk," or "visually clear" are
qualitative synthesis, not measured market scores.

| Direction | Competition | Typical production risk | Small-team observation |
| --- | --- | --- | --- |
| Roguelite | Crowded | Medium | Viable when the core variation or verb is distinct. |
| Deckbuilder | Crowded | Medium | Efficient when depth comes from reusable rules. |
| Survivors-like | Crowded | Low to medium | Accessible scope, but novelty and feel are critical. |
| Horror | Moderate | Low to medium | Mood and streamer visibility can help modest projects. |
| Cozy or farming | Crowded | Often high | Content, animation, and relationship expectations can be substantial. |
| Automation or factory | Moderate | High systems and UX burden | Strong depth-to-content ratio when usability is excellent. |
| Management simulation | Moderate | Medium | Reusable systems can create longevity. |
| Survival crafting | Crowded | Very high | Dangerous unless the world and feature set are constrained. |
| Soulslike action | Crowded | High | Combat, animation, level, and balance expectations are severe. |
| Tactics | Moderate | Medium | Strong fit when rules and state are highly readable. |
| Creature collection | Crowded | Very high | Roster, animation, content, and progression expectations are dangerous. |
| Multiplayer co-op | Moderate | High | Networking, testing, matchmaking, and support multiply risk. |
| Competitive PvP | Crowded | Very high | Balance, anti-cheat, population, and live support are major burdens. |
| Mystery | Moderate | Medium | Best when the premise and interactive verb are easy to explain. |
| Narrative drama | Moderate | Medium to high | Depends heavily on writing, pacing, reviews, and word of mouth. |

Often easier to communicate visually:

- readable combat with distinct threats
- horror spaces with a clear danger
- cozy environments with visible activities
- factory or base layouts
- horde escalation
- co-op disaster situations
- large score or combo reactions
- fishing, mining, crafting, building, or cooking
- tactical boards with visible units and objectives

Often harder to communicate:

- subtle narrative drama
- humor dependent on timing or writing
- abstract systems
- minimalist mechanics
- dialogue-heavy play
- experiences dependent on late twists
- systems requiring long explanations

A saturated genre is not automatically a poor choice. The practical question
is:

> Why should this player choose this game instead of another game they already
> understand?

Differentiation can come from a new decision structure, strong visual identity,
an unusual but understandable fantasy, a familiar verb in a new context,
reinforcing systems, better usability, a distinct social dynamic, or mechanics
that create memorable stories.

## Commercial Presentation

A strong store presentation makes these clear:

- what the player does
- what the player is trying to achieve
- what creates pressure or conflict
- how progress is visible
- what makes the game distinct
- what tone to expect

Useful elements include readable capsule art, a legible title and logo,
screenshots showing actual play, a gameplay-first trailer, accurate tags, a
concise fantasy, visible stakes, and consistent visual identity.

Steamworks guidance recommends that store trailers primarily show gameplay
from the player's perspective and notes that players often want to see the HUD
and actual interaction [4].

Common wishlist triggers include strong genre fit, an understandable fantasy,
attractive screenshots, a clear loop, distinctive art direction, a promising
demo, trusted recommendation, and social proof.

Common purchase factors include price relative to perceived value, reviews,
current mood, coherent presentation, demonstrated gameplay quality, and
technical confidence.

Common refund causes include the opening not matching the store promise, poor
performance, weak controls, confusing onboarding, severe bugs, presentation
that looks better than the interaction feels, and a loop that becomes
repetitive too quickly.

Steam's standard refund policy makes the early experience commercially
important [5]. Verify current platform policy before relying on exact limits.

### High-Risk Small-Team Projects

Use strong constraints or avoid projects requiring:

- continuous live-service operation
- large-scale competitive PvP
- extraction systems with deep backend and balance requirements
- MMO-like content and population
- large open worlds
- extensive survival-crafting content
- very large creature or item rosters
- cinematic RPG production
- animation-heavy action without matching capacity
- constant content updates
- multiplayer populations large enough to sustain basic play

Potentially efficient directions include compact roguelites, card games with a
distinct decision structure, survivors-likes with a clear hook, compact horror,
focused management or automation, readable tactics, constrained cozy-system
hybrids, mechanically integrated mysteries, and controlled co-op chaos.
Execution and differentiation matter more than category labels.

## Case Studies

Case studies identify patterns; they do not prove that copying a feature will
produce the same result.

| Game | Core loop | Useful lesson |
| --- | --- | --- |
| *Vampire Survivors* | survive, collect, choose upgrades, combine, repeat | Compress the distance between action, reward, and visible growth. |
| *Hades* | run, fight, fail, upgrade, continue relationships | Narrative can reward repetition instead of interrupting it. |
| *Stardew Valley* | plan days, gather, farm, relate, improve | Relaxation still benefits from layered goals and autonomy. |
| *Slay the Spire* | draft, route, fight, adapt | Decision density can matter more than raw content count. |
| *Balatro* | play hands, modify rules, build combinations | Familiar foundations can reduce onboarding cost. |
| *Dark Souls* / *Elden Ring* | explore, fight, fail, learn, recover | Difficulty works when causes and improvement remain readable. |
| *Celeste* | attempt, fail, retry, improve | Frequent failure can work when retry friction is low. |
| *Factorio* | build, automate, measure, optimize | Visible inefficiency creates self-directed mastery goals. |
| *Lethal Company* | explore, gather, coordinate, panic, escape | Retellable systemic moments can market themselves socially. |
| *RimWorld* | build, manage, respond, recover | Reusable systems can generate narrative. |
| *Dredge* | fish, risk, sell, upgrade, investigate | Theme can multiply the identity of a modest core verb. |
| *Papers, Please* | inspect, decide, earn, survive | Context can turn repetition into meaningful tension. |
| *Inscryption* | play, learn rules, uncover layers | Mechanics and narrative can reinforce the same curiosity. |

Broader lessons:

- Simple controls can support depth when decisions and consequences are clear.
- Story is efficient when it explains or rewards repeated play.
- Decision density is often more valuable than content that changes nothing.
- Visible inefficiency can create self-directed mastery goals.
- Retellable systemic moments can create a marketing advantage.
- Strong theme can sharpen a modest mechanic when it affects play and
  presentation.

## Practical Recommendations

### Engagement Without Predation

Prefer mastery, curiosity, fair challenge, visible improvement, meaningful
choice, expression, optional completion, readable randomization, emergent
interaction, and narrative reinforcement.

Avoid relying on hard FOMO, punitive streaks, paid randomness, artificial
grind, expiring paid value, social guilt, opaque manipulation, or misleading
odds.

### Prototype Priorities

1. **Input feel:** interaction must be responsive and understandable.
2. **Core verb loop:** identify what the player does every few seconds.
3. **Decision loop:** identify what meaningful choice appears every minute.
4. **Session loop:** define what the player can complete in one sitting.
5. **Replay loop:** establish why the player would voluntarily begin again.
6. **Market hook:** test whether the game can be understood from one sentence,
   a screenshot, and a short clip.

Do not build large content libraries before these foundations are credible.

### Feature Evaluation

Before approving a feature, ask:

- Which player motivation does it support?
- What new decision does it create?
- Can the player perceive its effect?
- Does it strengthen the core loop?
- Can the team implement, test, explain, and maintain it?
- Does it require ongoing content or live support?
- Does it create accessibility or ethical concerns?
- Can it be validated cheaply?
- What is removed or delayed to make room for it?

### Market Evaluation

Before treating an idea as commercially promising, ask:

- Can a stranger identify the core activity?
- Is the fantasy understandable without several paragraphs?
- Is the differentiator visible in screenshots or clips?
- Does the genre create expectations the team cannot meet?
- Are current comparable games attracting players and reviews?
- Is the intended price credible for the content and polish?
- Does the prototype fulfill the promise made by the pitch?

## Prototype Validation

Praise is useful but weak evidence. Behavior is more reliable.

### Fun Validation

Positive evidence:

- testers replay without being asked
- testers understand feedback without explanation
- testers form a plan for the next attempt
- testers describe strategies or memorable moments
- failure produces a desire to retry
- players notice improvement
- multiple approaches appear viable
- interaction feels good before progression rewards are added

Warning signs:

- testers praise the concept but stop quickly
- players cannot explain what happened
- rewards are missed or ignored
- failure feels arbitrary
- the game needs a long explanation to sound enjoyable
- content volume is hiding a weak loop
- players repeat actions without making decisions

### Market Validation

Positive evidence:

- strangers understand the fantasy in one sentence
- screenshots communicate real gameplay
- a short trailer works without narration
- players can describe the game to someone else
- the hook is visible before detailed copy
- demo players show stronger interest after playing
- viewers mention specific mechanics

Warning signs:

- people like the art but cannot identify the activity
- the pitch requires several paragraphs
- trailer viewers cannot tell what the player controls
- the project looks interchangeable with competitors
- the strongest feature is invisible in marketing
- interest disappears after the first interaction

### Ethics Checklist

- Does the game remain enjoyable without daily pressure?
- Does missing a session punish the player?
- Are random rewards transparent?
- Is progression meaningful or merely stretched?
- Are players returning from curiosity or anxiety?
- Can players stop without losing paid value?
- Do social systems create fun or guilt?
- Are prices and probabilities understandable?
- Would the system still feel fair without monetization?
- Could a vulnerable player misunderstand the cost or probability?

### Evidence Quality

Playtest conclusions are more trustworthy when the target audience is
represented, the build is stable enough for the tested question, testers act
without constant explanation, questions avoid leading language, behavior is
recorded separately from interpretation, repeated patterns matter more than one
comment, and negative evidence is not discarded because it conflicts with the
pitch.

## Conclusion

For a small indie team seeking engaging, ethical, and commercially legible
design:

- start with a clear player fantasy
- build a satisfying interaction before large content volume
- use systems to create decisions and variation
- make feedback immediate and hierarchical
- support intentional player motivations
- use story to reinforce the experience
- avoid retention based primarily on pressure or obligation
- validate fun through behavior
- validate marketability separately from fun
- recheck market assumptions as conditions change

A strong ethical game should make players feel capable, curious, expressive,
challenged, respected, free to stop, and interested enough to return.

## Sources

Claims tied to specific research or industry guidance use numbered references.
Other statements are design heuristics or qualitative synthesis.

1. Ryan, Rigby, and Przybylski — Self-determination theory and video games: https://selfdeterminationtheory.org/SDT/documents/2006_RyanRigbyPrzybylski_MandE.pdf
2. World Health Organization — Gaming disorder FAQ: https://www.who.int/standards/classifications/frequently-asked-questions/gaming-disorder
3. Loot boxes and problem gambling review: https://pmc.ncbi.nlm.nih.gov/articles/PMC8064953/
4. Steamworks — Store trailers: https://partner.steamgames.com/doc/store/trailer
5. Steam — Refund policy: https://store.steampowered.com/steam_refunds/
