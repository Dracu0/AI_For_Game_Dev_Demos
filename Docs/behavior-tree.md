# Behavior Trees: the guard, twice more

The guard from `3_FSM/FSM_Guard.unity` is rebuilt as a behavior tree in two ways. Same body (`GuardBody.cs`), same Inspector numbers, same colors, so the three versions can be compared side by side.

| Scene | Brain | Where the decisions live |
|---|---|---|
| `3_FSM/FSM_Guard.unity` | `GuardAI` | State classes with explicit transitions |
| `4_BehaviorTree/BT_Guard.unity` | `GuardBT` | A tree in code, built from `SimpleBT.cs` (about 80 lines) |
| `4_BehaviorTree/BT_Guard_UnityBehavior.unity` | Behavior Graph | Unity Behavior package graph using the nodes in `GuardNodes.cs` |

## 1. Hand-rolled tree (`BT_Guard.unity`)

`SimpleBT.cs` has the whole framework: `Status` (Success, Failure, Running), `Selector`, `Sequence`, `Condition`, `Action`.

```
Selector                                  (first branch that does not fail wins)
├─ Sequence: [player in strike range?]  -> Attack
├─ Sequence: [player spotted?]          -> Chase
├─ Sequence: [player just lost?]        -> Search
└─ Patrol
```

The tree is ticked from the root every physics step. Points worth making in class:

- **No transitions.** Priority comes from the order of the branches. Adding a behavior means adding one branch at the right height.
- **Reactive by default.** Because the root is re-evaluated every tick, a higher branch interrupts a lower one as soon as its condition becomes true.
- **Memory is still needed.** Hysteresis (two radii) and "just lost the player" need to know what ran last. `GuardBT` keeps that in one field (`_activity`). This is the job a blackboard does in bigger trees.
- **No Enter.** A leaf detects its first tick itself (`Begin`). FSMs give you that for free; trees do not.

## 2. Unity Behavior graph (`BT_Guard_UnityBehavior.unity`)

The scene's `Guard` already has `GuardGraphBody` (the body with no brain) and a `BehaviorGraphAgent`. The graph itself has to be drawn in the editor (the package has no public API to build one from code). About 5 minutes:

1. **Project window:** right-click `Assets/Scenes/4_BehaviorTree` > *Create > Behavior > Behavior Graph*. Name it `Guard Graph`.
2. Select `Guard` in the scene and drag `Guard Graph` into the **Graph** field of `BehaviorGraphAgent`.
3. Double-click `Guard Graph` to open the editor. The blackboard already has a `Self` variable.
4. Build this under the existing **On Start** node (Space or right-click > *Add Node*; the custom nodes are under *Action > Guard* and *Conditions > Guard*):

```
On Start
└ Repeat                      (Mode: Forever)
   └ Sequence
      ├ Guard patrols until it spots the player
      ├ Repeat                (Mode: Condition, condition: Guard can still see the player)
      │  └ Sequence
      │     ├ Guard chases the player
      │     └ Guard attacks the player
      └ Guard searches the area
```

5. In every Guard node and in the condition, set **[Guard]** to `Self` (click the field in the node and pick it).
6. Save the graph (Ctrl+S in the graph window) and press Play. With the Guard selected, the graph window highlights the running node.

What each node reports:

| Node | Running while | Success | Failure |
|---|---|---|---|
| Guard patrols | walking the route | player within `sightRadius` | |
| Guard chases | closing in | within `attackRange` | player beyond `loseRadius` |
| Guard attacks | player within `attackRange × 1.25` (strikes on cooldown) | player stepped out of reach | |
| Guard searches | standing still for `searchTime` | time is up, or player seen again | |
| Guard can still see the player (condition) | | true while within `loseRadius` | |

Comparison with the hand-rolled tree: this graph is **sequential, not reactive**. Each action decides when it is done, and the flow moves on, much like an FSM drawn as a tree. Making it reactive is what *Abort* / *Restart* modifiers are for: a good follow-up exercise.
