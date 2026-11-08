# API overview

Namespaces: `ScenarioForge` (core and components), `ScenarioForge.Steps` (built-in steps), `ScenarioForge.Editor` (editor only).

## Core

| Type | Members |
| --- | --- |
| `ScenarioDefinition` | `Title`, `Description`, `Steps` |
| `ScenarioStep` | `Id`, `Title`, `Instruction`, `TimeoutSeconds`, `OnFailure`, `MaxRetries`, `CanFail`; `OnEnter`, `OnTick`, `OnExit`, `Validate`, `Fail(reason)` |
| `ScenarioRunner` | `Start`, `Tick`, `Pause`, `Resume`, `Abort`, `SkipCurrentStep`, `Restart`, `Dispose`; `State`, `CurrentStepIndex`, `CurrentStep`, `CurrentStepElapsed`, `Results`, `Mistakes`, `Definition`, `Context`; events `StateChanged`, `StepStarted`, `StepEnded`, `InstructionChanged`, `SignalRaised`, `MistakeReported` |
| `ScenarioContext` | `Blackboard`, `Signals`, `Targets`, `Services`, `TotalElapsed`, `UpdateInstruction`, `ReportMistake` |
| `StepInfo` | `StepId`, `Index`, `Title`, `Instruction`, `Attempt` |
| `StepResult` | `StepId`, `Index`, `Outcome`, `Duration`, `Attempts`, `FailureReason`, `Mistakes` |
| `ScenarioMistake` | `StepId`, `StepIndex`, `Message`, `Time` |
| Enums | `RunnerState`, `StepStatus`, `StepOutcome`, `FailurePolicy` |
| `ScenarioStepMenuAttribute` | `Path`, `Tooltip` |

## Data

| Type | Members |
| --- | --- |
| `Blackboard` | `Set<T>`, `SetValue`, `TryGet<T>`, `TryGetValue`, `Has`, `Remove`, `Clear`, `Keys`, `Count`, `ValueChanged` |
| `BlackboardValue` | `FromBool/Int/Float/String`, `Type`, `TryCompare` |
| `SignalBus` | `Raise`, `GetCount`, `WasRaised`, `ResetCounts`, `Raised` |
| `ITargetResolver`, `SceneTargetResolver` | `TryGetTransform(id, out transform)`; `SceneTargetResolver.FromLoadedScenes()` |
| `IScenarioServices` | Marker interface for game services |
| `ITargetReferencingStep` | `TargetIds`, checked by editor validation |

## Validation and JSON

| Type | Members |
| --- | --- |
| `ScenarioValidator` | `Validate(definition)` |
| `ValidationReport` | `Issues`, `HasErrors`, `Add(severity, message, stepId)` |
| `ValidationIssue` | `Severity`, `Message`, `StepId`, `StepIndex` |
| `ScenarioJson` | `Export(definition)`, `TryImport(json, out data, out errors)`, `FormatVersion` |
| `ScenarioData` | `Title`, `Description`, `Steps`, `CreateDefinition()` |

## Components

`ScenarioRunnerBehaviour`, `ScenarioTarget`, `ScenarioSignalEmitter`, `ScenarioStepEvents` (with `StepEventBinding`), `ScenarioSignalListener`.

## Editor

| Type | Members |
| --- | --- |
| `ScenarioForgeWindow` | `Open()`, `Open(definition)`, `Scenario`, `SetScenario`, `SelectStep`, `AddStep(type)` |
| `BatchValidation` | `ValidateAllScenarios()` for `-executeMethod`; exits with 1 on errors |
