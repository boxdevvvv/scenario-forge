# Creating custom steps

A step is a `[Serializable]` class that derives from `ScenarioStep`.

```csharp
using System;
using ScenarioForge;
using UnityEngine;

[Serializable]
[ScenarioStepMenu("Custom/Press Lever", Tooltip = "Waits until the lever reaches the target angle.")]
public sealed class PressLeverStep : ScenarioStep
{
    [SerializeField] string leverTargetId;
    [SerializeField] float targetAngle = 45f;

    protected override StepStatus OnTick(ScenarioContext context, float deltaTime)
    {
        if (!context.Targets.TryGetTransform(leverTargetId, out Transform lever))
            return Fail($"Lever target '{leverTargetId}' was not found. Add a ScenarioTarget with that id.");

        return lever.localEulerAngles.x >= targetAngle ? StepStatus.Succeeded : StepStatus.Running;
    }

    public override void Validate(ValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(leverTargetId))
            report.Add(ValidationSeverity.Error, "Lever target is empty.", Id);
    }
}
```

## Requirements to appear in the Add Step menu

- `public`, not abstract, not generic, `[Serializable]`, with a public parameterless constructor.
- `[ScenarioStepMenu("Category/Name")]` sets the menu path and tooltip. Without it, the step goes under *Other* with its class name.

## Lifecycle

| Method | When |
| --- | --- |
| `OnEnter(context)` | The step starts, and again on every retry. Reset per-attempt state here. |
| `OnTick(context, deltaTime)` | Every frame while running. Return `Running`, `Succeeded` or `Failed`. |
| `OnExit(context, outcome)` | The step ends (also on retry, skip and abort). Unsubscribe from events here. |
| `Validate(report)` | In the editor and in batch validation. Add issues with the step's `Id`. |

- Return `Fail("reason")` to fail with a message; it appears in `StepResult.FailureReason`.
- Override `CanFail` and return `false` when the step never fails on its own (the validator then notes that a timeout is its only way to fail).
- Call `context.UpdateInstruction("…")` to change the instruction while the step runs, and `context.ReportMistake("…")` to record a player mistake.
- Exceptions inside a step are caught and logged; the step fails with the exception message.

## State

The runner works on a copy of the asset, so steps may keep runtime state in fields, but mark them `[NonSerialized]` so they are not saved or exported:

```csharp
[NonSerialized] float heldSeconds;
```

Serialized fields are configuration and are the same in every run.

## Scene references

Steps live in an asset, so they cannot reference scene objects directly. Use target ids (`context.Targets`) or signals. Implement `ITargetReferencingStep` to have the editor check that your target ids exist in the open scenes.

## Game services

For custom steps that need game systems, implement `IScenarioServices` and assign it to `ScenarioRunnerBehaviour.Services` before the scenario starts; read it from `context.Services`.

## Renaming a step class

Assets store the class name. To rename or move a step without breaking existing scenarios, add `[MovedFrom]` with the old names:

```csharp
using UnityEngine.Scripting.APIUpdating;

[Serializable]
[MovedFrom(true, sourceNamespace: "MyGame.OldSteps", sourceAssembly: null, sourceClassName: "PullLeverStep")]
public sealed class PressLeverStep : ScenarioStep { … }
```

If a class was removed, the window shows the step as *Missing type* with a button to remove it.
