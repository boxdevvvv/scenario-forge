# Scenario Forge

Scenario Forge lets you author **step-by-step procedures** in an editor window and run them with a testable runtime. A scenario is an ordered list of steps; the runner executes one step at a time, checks when it is complete, applies timeouts and failure policies, and records results and player mistakes.

## How the pieces fit

| Piece | What it is |
| --- | --- |
| `ScenarioDefinition` | The asset: title, description and the list of steps. |
| `ScenarioStep` | Base class of every step. Built-in steps live in `ScenarioForge.Steps`. |
| `ScenarioRunner` | Plain C# class that runs a copy of a definition. Never modifies the asset. |
| `ScenarioContext` | State of one run: blackboard, signals, scene targets and optional game services. |
| `ScenarioRunnerBehaviour` | Scene component that creates the runner, ticks it and exposes UnityEvents. |
| Scenario Forge window | Authoring, validation, JSON import/export and the Play Mode debugger. |

The game and the scenario talk through three channels:

- **Signals:** named events (`"door_opened"`). The game raises them; steps wait for them; `SendSignalStep` raises them for the scene.
- **Blackboard:** typed values (bool, int, float, string) shared by the steps of a run.
- **Targets:** scene objects marked with `ScenarioTarget` and found by id.

## Pages

- [Getting started](getting-started.md)
- [Creating custom steps](creating-custom-steps.md)
- [JSON format](json-format.md)
- [Debugging](debugging.md)
- [API overview](api-overview.md)
