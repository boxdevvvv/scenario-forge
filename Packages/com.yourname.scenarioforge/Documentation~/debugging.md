# Debugging

## Play Mode debugger

Open the scenario in **Window > Scenario Forge** and enter Play Mode. A **PLAY MODE** bar appears under the toolbar:

- **Runner picker:** every active `ScenarioRunnerBehaviour` in the loaded scenes. It picks the one that runs the open scenario. If you pick a runner of another scenario, a notice offers to open that scenario.
- **State:** run state, scenario time and mistake count.
- **Pause / Resume, Skip Step, Restart, Abort** call the runner directly. Restart runs the scenario again; it does not reset your scene objects.
- *Editing the asset does not affect the running instance*: the runner plays a copy made when it started.

Each step row shows its state: pending (grey), active (highlighted, with elapsed time), ✓ succeeded, ✕ failed, ↷ skipped, ⏱ timed out, ■ aborted. Hover the symbol for duration, attempts, mistakes and the failure reason. The list scrolls to the active step.

The panel above the validation list has two tabs:

- **Blackboard:** key, type and value, live.
- **Event Log:** the last 200 events (runs, state changes, step starts and ends, signals, mistakes) with scenario time. **Clear** empties it; **Copy** puts it on the clipboard.

The debugger refreshes every 100 ms and disappears when Play Mode ends.

## From code

`ScenarioRunner` exposes `State`, `CurrentStepIndex`, `CurrentStep`, `CurrentStepElapsed`, `Results` and `Mistakes`, and the events `StateChanged`, `StepStarted`, `StepEnded`, `InstructionChanged`, `SignalRaised` and `MistakeReported`. `ScenarioRunnerBehaviour` forwards the step, signal and mistake events across runs, so you can subscribe once.

## Common problems

| Symptom | Cause and fix |
| --- | --- |
| A Wait For Signal step never finishes | The signal was raised before the step started; only signals raised during the step count. Gate the action with Scenario Step Events. |
| "target 'x' was not found" | No `ScenarioTarget` with that id in the loaded scenes. The validation panel warns about it in the editor. |
| A step shows *Missing type* | Its class was renamed or deleted. Add `[MovedFrom]` or remove the step. |
| Signals from an emitter do nothing | The emitter found no running `ScenarioRunnerBehaviour`; assign Runner or start the scenario first. |
