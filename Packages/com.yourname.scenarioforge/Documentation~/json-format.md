# JSON format

```json
{
  "formatVersion": 1,
  "scenario": {
    "title": "Forklift pre-operation check",
    "description": "…",
    "steps": [
      {
        "$type": "ScenarioForge.Steps.WaitStep, ScenarioForge.Runtime",
        "id": "3f2a9c1e5b7d4e8a9c217d4e5f6a8b90",
        "title": "Wait 2 seconds",
        "instruction": "",
        "timeoutSeconds": 0.0,
        "onFailure": "FailScenario",
        "maxRetries": 0,
        "duration": 2.0
      }
    ]
  }
}
```

## Rules

- Fields are the same ones Unity serializes: public fields and `[SerializeField]` private fields, base class first. `[NonSerialized]`, `readonly` and `UnityEngine.Object` fields are skipped.
- Enums are written as text. `Vector2`, `Vector3`, `Quaternion` and `Color` are written as objects (`{ "x": 1, "y": 2, "z": 3 }`); missing components default to 0 (`w` and alpha default to 1).
- Fields missing from the JSON keep the step's default value.
- Exporting, importing and exporting again produces the same JSON.

## Security

`$type` may only name a non-abstract `ScenarioStep` type from an assembly that is already loaded. Any other type, anywhere in the file, is rejected before anything is created, so a file cannot instantiate arbitrary types.

## Errors

`TryImport` never throws and never returns partial data. It reports, for example: invalid JSON, a missing or unsupported `formatVersion` (newer versions ask you to update the package), a missing `scenario` object, steps without `$type`, unknown or disallowed types, and wrong value types. Every bad step is listed.

## In the window

- **Export** saves the open scenario. It refuses scenarios with missing step types.
- **Import** asks *Replace* (title, description and steps) or *Append* (steps at the end). It is one Undo step. Empty or repeated ids get new ids and the status bar says how many.
- **Copy / Paste** use the same format through the clipboard. Pasted steps always get new ids.

## At runtime

```csharp
if (ScenarioJson.TryImport(json, out ScenarioData data, out IReadOnlyList<string> errors))
{
    ScenarioDefinition definition = data.CreateDefinition();
    runnerBehaviour.Definition = definition;
    runnerBehaviour.StartScenario();
}
else
{
    Debug.LogError(string.Join("\n", errors));
}
```

The definition takes the imported step objects, so create one definition per import, and destroy it when you no longer need it.
