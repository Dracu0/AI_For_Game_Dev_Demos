# AI for Game Dev: Demos

Small, readable Unity 6 (6000.3.24f1, URP 2D) demos for a game-AI course. Each scene shows one idea, with as little code as possible.

Scenes are numbered in teaching order and are all in *File > Build Profiles > Scene List*.

| # | Scene | What it shows | Scripts |
|---|---|---|---|
| 1 | `Scenes/1_Steering/Steering.unity` | Seek, Flee, Arrival, Pursue, Evade, waypoint following and NavMesh (NavMeshPlus) agents, all chasing the WASD player | `Scripts/Steering/` |
| 2 | `Scenes/2_Pathfinding/Dijkstra_VS_AStar.unity` | Grid search: Dijkstra vs A* side by side | `Scripts/Pathfinding/Grid/` |
| 2 | `Scenes/2_Pathfinding/NodeGraph_AStar.unity` | A* on a hand-placed node graph. Click anywhere: the agent walks to the nearest node by the shortest route. Disable a node in Play mode to block it | `Scripts/Pathfinding/Graph/` |
| 3 | `Scenes/3_FSM/FSM_Guard.unity` | Guard enemy as a finite state machine (Patrol, Chase, Attack, Search) | `Scripts/FSM/` |
| 4 | `Scenes/4_BehaviorTree/BT_Guard.unity` | The same guard as a hand-rolled behavior tree | `Scripts/BehaviorTree/` |
| 4 | `Scenes/4_BehaviorTree/BT_Guard_UnityBehavior.unity` | The same guard with the Unity Behavior package (graph to be drawn: see `Docs/behavior-tree.md`) | `Scripts/BehaviorTree/UnityBehavior/` |
| 5 | `Scenes/5_GOAP/GOAP_Camp.unity` | Goal-oriented action planning: a camper plans axe → wood → fire → fish → fed. Click facts in the HUD to flip them and watch it re-plan | `Scripts/GOAP/` |
| Extra | `Scenes/Extras/Boids.unity` | Flocking | `Scripts/Extras/Boids/` |
| Extra | `Scenes/Extras/AI_Platformer.unity` | Platformer enemies: jump-aware path grid, patrol, chase | `Scripts/Extras/Platformer/` |

`Scripts/Common/PlayerController2D.cs` is the top-down player used by scenes 1, 3 and 4 (WASD, `Input/Controls_TopDown.inputactions`).

## Lessons

- `Docs/lesson-fsm.md` (+ `Docs/lesson-fsm/` step files): a 2-hour live-coding lesson building the FSM guard.
- `Docs/behavior-tree.md`: the two behavior-tree versions of the guard, and how to wire the Unity Behavior graph.

## Packages

- `com.unity.ai.navigation` + [NavMeshPlus](https://github.com/h8man/NavMeshPlus) (git) for 2D NavMesh baking.
- `com.unity.behavior` for the Behavior Graph scene.
