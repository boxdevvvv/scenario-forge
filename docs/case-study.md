# Scenario Forge — case study

Scenario Forge is a Unity 6 editor tool for step-by-step procedures (trainings, tutorials, safety checklists). Designers author the steps in an editor window; the game only reports facts (signals, blackboard values, positions), and a plain C# runner checks that the player does things in the right order, records mistakes and reports the result.

- **Stack:** Unity 6000.6, C#, UI Toolkit (editor window), `[SerializeReference]` for polymorphic steps, Newtonsoft JSON, NUnit (Unity Test Framework).
- **Size:** one UPM package with separate Runtime, Editor and Tests assemblies; 230 EditMode and 10 PlayMode tests.
- **Scope:** authoring window with type picker, drag reorder, Undo; live validation; JSON import/export; Play Mode debugger; a sample scene; documentation.

Below are three design decisions, each written as **problem → decision → result**.

---

## 1. A shared asset must never change while the scenario runs

**Problem.** A scenario is a `ScriptableObject` asset, and its steps live inside it as managed references. Steps need state while they run (a checklist remembers which items are done, a wait step counts time). In the Editor, a `ScriptableObject` is not reset when Play Mode ends: any field a step writes during a run would be **saved back into the asset on disk**. Designers would see their scenario silently change after pressing Play, and two runners using the same asset would share state.

**Decision.** The runner never executes the asset itself.

1. `ScenarioRunner.Start()` makes a copy with `Object.Instantiate(definition)`. Unity's instantiate also clones the `[SerializeReference]` graph, so every step in the copy is a new object. The runner destroys the copy when it finishes or is aborted.
2. As a second line of defence, run-time state inside a step goes in `[NonSerialized]` fields and is reset in `OnEnter`, so a step is correct even if someone runs it outside the runner.
3. The runner is plain C# (not a `MonoBehaviour`), with time passed into `Tick(deltaTime)`. A thin `ScenarioRunnerBehaviour` drives it in a scene.

**Result.**

- A test (`RunningAScenario_DoesNotModifyTheOriginalAsset`) runs a step that deliberately writes to its serialized fields. It then checks that the asset's JSON is identical before and after the run.
- Another test checks that `runner.CurrentStep` is not the same object as the asset's step.
- Because the runner is plain C#, almost all runtime behaviour is covered by fast EditMode tests (state transitions, timeouts, retries, failure policies, pause, abort) without entering Play Mode.
- The Play Mode debugger shows a visible note: *"Editing the asset does not affect the running instance"*. The window can safely stay editable while a run is in progress.

---

## 2. Importing JSON must not let a file create arbitrary types

**Problem.** Steps are polymorphic, so the JSON format must record each step's concrete type. The usual Newtonsoft answer is `TypeNameHandling`, which writes a `$type` field and creates whatever type it names. That is a known deserialization risk: a crafted file could name any type in any loaded assembly. Scenario files are meant to be shared and even loaded at runtime (for example, from an LMS), so they cannot be trusted.

There were two more problems. Newtonsoft's default rules are not Unity's rules: it reads properties, and it ignores `[SerializeField]` private fields. It also loops on Unity structs such as `Vector3.normalized`.

**Decision.**

1. A custom `ISerializationBinder` (`ScenarioStepBinder`) is the only way a `$type` becomes a `Type`. It accepts a type only if it derives from `ScenarioStep` and is neither abstract nor an open generic. Anything else is an import error with a clear message, never an instance. The binder is only used with `TypeNameHandling.Auto`, so `$type` appears only where a field is polymorphic.
2. Before deserializing, the importer pre-checks every step's `$type`. It reports **all** bad steps in one go ("Step 3: Type "System.IO.File" is not allowed…") instead of failing on the first one.
3. A custom `ContractResolver` serializes exactly what Unity serializes:
   - public fields and `[SerializeField]` private fields, walking the class hierarchy base-first;
   - skips `[NonSerialized]`, `readonly` and `const` fields, and references to `UnityEngine.Object`.
4. Explicit converters handle `Vector2`, `Vector3`, `Quaternion` and `Color`.
5. Limits: `MaxDepth = 64`, and the file has a versioned envelope (`formatVersion`). Unknown versions are rejected before anything is imported.

**Result.**

- Tests prove that a non-step type, an abstract step and an unknown type are each rejected with a readable error:
  - `TypeThatIsNotAStep_IsRejected`, `AbstractStepType_IsRejected`, `UnknownStepType_SaysHowToFixIt`;
  - `EveryBadStep_IsReported`.
- A round-trip test with every built-in step proves that export → import → export produces the same JSON. A second test checks that the JSON contains exactly the fields Unity's own serializer writes.
- The same API (`ScenarioJson.TryImport`) is used by the editor window and is available at runtime. It returns errors instead of throwing, so a bad file never breaks a build.

---

## 3. The details panel is rebuilt, never re-bound

**Problem.** The right panel of the window shows the selected step's fields with UI Toolkit `PropertyField`s bound to `steps.Array.data[i]`. Each element of that array is a `[SerializeReference]`, so the array index does not uniquely identify the object. After a reorder, an Undo, or a *Change Type…*, the same property path points to a **different** step, possibly of a different type. Re-binding existing fields in that situation risks stale bindings: fields of the old type stay visible, or edits land on the wrong step.

**Decision.** The panel is never patched. Every time the selection changes, an Undo or Redo happens (`Undo.undoRedoPerformed`), a step changes type, or the list is reordered, `StepDetailsPanel.Show` clears the panel. It then creates **new** `PropertyField`s, one per visible child of the step, and binds them again. The step's `id` stays hidden.

All edits go through `SerializedObject` with named Undo groups ("Add Step", "Move Step", "Change Step Type"…), so Undo and the rebuild always start from the same source of truth.

**Result.**

- Window tests check that selecting a step builds new fields every time (`SelectingAStep_BuildsNewDetailFieldsEachTime`) and that only the fields of the selected type are shown (`ShowsOnlyTheFieldOfTheSelectedType`).
- Edit-operation tests check that add, delete, duplicate, move and change-type each modify the asset and that one Undo reverts them.
- The cost is small: a step has a handful of fields, and the rebuild only happens on discrete events, never every frame.

---

## What I would do next

- A node-graph view for branching scenarios (the runtime model is linear on purpose for v0.1).
- Parallel steps (several active steps at once).
- Code coverage reporting in CI.

*Built with AI-assisted development (Claude Code); architecture and every change reviewed by the author.*
