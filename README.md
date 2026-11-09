# Scenario Forge

<!-- Hero GIF placeholder: record the window authoring a scenario and the Play Mode debugger, then replace this comment with ![Scenario Forge](Packages/com.boxdevvvv.scenarioforge/Documentation~/images/hero.gif) -->

Scenario Forge is an open-source Unity editor tool for **step-by-step procedures**: trainings, tutorials, linear missions and safety checklists. Designers author the steps in an editor window; the game only reports facts (signals, blackboard values, positions), and a testable runtime checks that the player does things in the right order, records mistakes and reports the result.

- Unity **6.6** (6000.6) or newer
- Depends only on `com.unity.nuget.newtonsoft-json`
- MIT license

## Installation

In **Window > Package Manager**, click **+ > Install package from git URL…** and paste:

```
https://github.com/boxdevvvv/scenario-forge.git?path=/Packages/com.boxdevvvv.scenarioforge#v0.1.0
```

The package lives in a subfolder of the repository, so the `?path=` part is required, and it goes **before** the `#` version.

## Quick start

1. **Create a scenario:** **Window > Scenario Forge > Create New Scenario**, or **Assets > Create > Scenario Forge > Scenario**.
2. **Add steps** with **+ Add Step**: a searchable menu groups the step types by category. Select a step to edit it on the right. Drag to reorder; Undo works for every change.
3. **Set up the scene:** add a **Scenario Runner Behaviour** to a GameObject and assign the scenario. Mark scene objects that steps refer to with **Scenario Target** (for example `player` and `dock`).
4. **Report what happens:** call `ScenarioSignalEmitter.Raise()` from a button or trigger, write to the blackboard, or move targets.
5. **React to the scenario (optional):** **Scenario Step Events** runs UnityEvents when a step starts or ends (enable a button only during its step, show a highlight); **Scenario Signal Listener** runs a UnityEvent when a signal is raised, for example by a **Send Signal** step.
6. Press **Play**. The window shows each step's live state, the blackboard and an event log.

To see everything working, import the **Assembly Procedure** sample from the package's page in the Package Manager and open `Scenes/AssemblyProcedure.unity`.

## Built-in steps

| Step | Menu | Completes when |
| --- | --- | --- |
| Wait | Flow/Wait | `duration` seconds pass |
| Instruction | Flow/Instruction | the confirm signal is raised, or `autoAdvanceSeconds` pass |
| Wait For Signal | Signals/Wait For Signal | a signal is raised `requiredCount` times after the step starts |
| Checklist | Signals/Checklist | every item's signal is raised (in order if `ordered`; items done out of order count as mistakes) |
| Send Signal | Signals/Send Signal | immediately, after raising a signal the scene can react to |
| Reach Target | Spatial/Reach Target | the actor target is within `radius` of the destination target |
| Set Value | Blackboard/Set Value | immediately, after writing a blackboard value |
| Wait For Condition | Blackboard/Wait For Condition | a blackboard comparison is true |

Every step also has a title, an instruction, an optional timeout and a failure policy: fail the scenario, skip the step, or retry it up to `maxRetries` times.

## Scene components

| Component | Use it to |
| --- | --- |
| Scenario Runner Behaviour | run a scenario; UnityEvents for started, completed, failed and instruction changed |
| Scenario Target | give a scene object an id that steps can refer to |
| Scenario Signal Emitter | raise a signal from a UnityEvent (buttons, triggers) |
| Scenario Step Events | invoke UnityEvents when a chosen step starts or ends |
| Scenario Signal Listener | invoke a UnityEvent when a signal is raised |

## Mistakes and results

Call `context.ReportMistake("…")` (or `runner.Context.ReportMistake`) when the player does something wrong. Mistakes never fail a step; they are listed in `ScenarioRunner.Mistakes` and counted in each `StepResult.Mistakes`, next to the outcome, duration and attempts, so you can grade a run.

## Creating a custom step

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

The class must be public, `[Serializable]` and have a parameterless constructor; it then appears in the **Add Step** menu. See [Creating custom steps](Packages/com.boxdevvvv.scenarioforge/Documentation~/creating-custom-steps.md) for state, `OnEnter`/`OnExit` and renaming steps with `[MovedFrom]`.

## JSON import and export

The window's **Import** and **Export** buttons read and write JSON; **Copy** and **Paste** use the same format through the clipboard. At runtime, `ScenarioJson.TryImport(json, out var data, out var errors)` and `data.CreateDefinition()` load a scenario, for example from a learning platform. Only `ScenarioStep` types can be created from `$type`. See [JSON format](Packages/com.boxdevvvv.scenarioforge/Documentation~/json-format.md).

## Validation

The window validates as you edit (missing ids, empty signals, missing scene targets, missing step types…). Click an issue to select its step. For CI, run:

```
Unity -batchmode -projectPath . -executeMethod ScenarioForge.Editor.BatchValidation.ValidateAllScenarios
```

It exits with code 1 when any scenario has errors.

## Known limitations

- Scenarios are linear: one step runs at a time.
- References to assets inside steps are not exported to JSON.
- The sample uses URP materials and needs the uGUI package (both are in Unity's default templates).

## Roadmap

- Node graph view with branches
- Parallel steps and step groups
- Localized instructions

## Documentation

[Getting started](Packages/com.boxdevvvv.scenarioforge/Documentation~/getting-started.md) · [Custom steps](Packages/com.boxdevvvv.scenarioforge/Documentation~/creating-custom-steps.md) · [JSON format](Packages/com.boxdevvvv.scenarioforge/Documentation~/json-format.md) · [Debugging](Packages/com.boxdevvvv.scenarioforge/Documentation~/debugging.md) · [API overview](Packages/com.boxdevvvv.scenarioforge/Documentation~/api-overview.md)

## Credits and license

Created by Carlos Llanquinao. Released under the [MIT license](LICENSE.md).

Built with AI-assisted development (Claude Code); architecture and every change reviewed by the author.
