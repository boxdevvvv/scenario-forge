# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Package skeleton: runtime, editor and test assemblies.
- `ScenarioDefinition`, `ScenarioStep`, `ScenarioRunner` and `ScenarioContext`: a linear runner that executes a copy of the asset, with timeouts, failure policies (fail, skip, retry), pause, skip, restart and abort.
- `Blackboard` (bool, int, float, string), `SignalBus` and scene targets (`ScenarioTarget`, `SceneTargetResolver`).
- Built-in steps: Wait, Instruction, Wait For Signal, Checklist, Reach Target, Set Blackboard Value and Blackboard Condition, each with its own validation.
- Scene components: `ScenarioRunnerBehaviour`, `ScenarioTarget` and `ScenarioSignalEmitter`.
- Scenario Forge window (`Window/Scenario Forge`, double-click on an asset, or the Inspector button): reorderable step list with search, context menu and shortcuts, a searchable Add Step menu, a details panel and Undo support for every action.
- Custom Inspector for `ScenarioDefinition` and a UI Toolkit drawer for `BlackboardValue`.
- `ScenarioValidator` with scenario and step rules, plus editor checks for missing step types and scene targets.
- Validation panel in the window (severity filters, click to select the step), issue icons on each step row and a validation summary in the Inspector.
- `ScenarioForge.Editor.BatchValidation.ValidateAllScenarios` for CI (`-executeMethod`) and the `Tools/Scenario Forge/Validate All Scenarios` menu.
- `ITargetReferencingStep`, so custom steps can have their target ids checked.
- Player mistakes: `ScenarioContext.ReportMistake`, `ScenarioRunner.Mistakes` and `MistakeReported`, and `StepResult.Mistakes`. An ordered `ChecklistStep` reports items done out of order.
- `SendSignalStep` (Signals/Send Signal) and `ScenarioRunner.SignalRaised`, so the scenario can tell the scene to react.
- Scene components `ScenarioStepEvents` (UnityEvents when a chosen step starts or ends, with a step picker in the Inspector) and `ScenarioSignalListener` (a UnityEvent per signal).
- `ScenarioRunnerBehaviour` forwards `StepStarted`, `StepEnded`, `SignalRaised` and `MistakeReported` across runs.
- JSON import and export: `ScenarioJson.Export` and `ScenarioJson.TryImport` (runtime API), `ScenarioData.CreateDefinition`, a serializer that picks the same fields as Unity, converters for `Vector2`, `Vector3`, `Quaternion` and `Color`, and a binder that only accepts `ScenarioStep` types in `$type`.
- Import (Replace or Append, one Undo group, repeated ids regenerated) and Export buttons in the window toolbar.
- Copy and paste steps through the system clipboard (Ctrl/Cmd+C, Ctrl/Cmd+V or the context menu); pasted steps get new ids.
