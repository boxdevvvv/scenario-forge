# Fire Safety Drill demo

A small scene for trying Scenario Forge by hand. It lives in the development project only and is not part of the package.

1. Open `FireDrillDemo.unity` and press Play. Click the Game view so it receives keyboard input.
2. Follow the instruction at the bottom of the screen. The panel on the left shows each step's state, the blackboard and the controls.
3. Open `FireDrillScenario.asset` in **Window > Scenario Forge** to see or change the steps, then press Play again.

| Key | Action |
|---|---|
| WASD / arrows | Move |
| E | Pick up the extinguisher |
| Space | Spray the fire |
| Enter | Continue (raises `confirm`) |
| 1, 2, 3 | Checklist signals `pull_pin`, `aim`, `squeeze` |
| P / N / R | Pause, skip step, restart |

The scene components only report facts: `DemoPickup` raises `extinguisher_picked` through a `ScenarioSignalEmitter`, the HUD raises the key signals, and `DemoFire` writes `fire_out` to the blackboard. The step order lives entirely in the scenario asset.
