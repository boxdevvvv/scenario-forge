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
