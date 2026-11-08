# Fire Safety Drill demo

A small scene for trying Scenario Forge by hand. It lives in the development project only and is not part of the package.

1. Open `FireDrillDemo.unity` and press Play. Click the Game view so it receives keyboard input.
2. Follow the instruction at the bottom of the screen. The panel on the left shows each step's state, time and mistakes.
3. At the end a report shows the grade, the time and outcome of each step, and every mistake.
4. Open `FireDrillScenario.asset` in **Window > Scenario Forge** to change the steps, then press Play again.

| Key | Action |
|---|---|
| WASD / arrows | Move |
| E | Take the extinguisher |
| Space | Spray the fire |
| Enter | Continue (raises `confirm`) |
| 1, 2, 3 | Pull the pin, aim, squeeze |
| P / N / R | Pause, skip step, restart |

## How the scene follows the scenario

The scene objects know nothing about the order of the steps. They react to the scenario and report facts back:

| Object | Component | What it does |
|---|---|---|
| Fire | `ScenarioSignalListener` | Lights the fire when the "Sound the alarm" step sends `fire_started`. |
| Scenario Reactions | `ScenarioStepEvents` | Shows the yellow highlights during "Walk to the extinguisher", "Pick up the extinguisher" and "Approach the fire"; allows taking the extinguisher only during its step; enables the 1/2/3 keys only during "Prepare the extinguisher". |
| Extinguisher | `ScenarioSignalEmitter` + `DemoPickup` | Raises `extinguisher_picked`. Trying to take it early is a mistake. |
| Fire | `DemoFire` | Writes `fire_out` to the blackboard. Spraying before the extinguisher is prepared is a mistake. |
| Scenario Runner | `DemoHud` | Raises `confirm` and the checklist signals; using the extinguisher too early is a mistake. |

The ordered checklist also reports items done out of order. Mistakes never fail a step; they lower the grade in the report.
