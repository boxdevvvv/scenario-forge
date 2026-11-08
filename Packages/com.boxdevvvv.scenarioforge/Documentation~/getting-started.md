# Getting started

## Install

Package Manager > **+ > Install package from git URL…**:

```
https://github.com/boxdevvvv/scenario-forge.git?path=/Packages/com.boxdevvvv.scenarioforge#v0.1.0
```

## 1. Create a scenario

Open **Window > Scenario Forge** and click **Create New Scenario**, or use **Assets > Create > Scenario Forge > Scenario**. Double-clicking a scenario asset opens it in the window.

## 2. Add and edit steps

- **+ Add Step** opens a searchable menu grouped by category. The new step goes after the selected one.
- Select a step to edit its fields on the right. The **⋮** menu has *Change Type…*, *Reset to Defaults*, *Copy*, *Duplicate* and *Delete*.
- Drag rows to reorder. Shortcuts with the list focused: Ctrl/Cmd+D duplicate, Delete, Ctrl/Cmd+C / Ctrl/Cmd+V copy and paste, Alt+↑/↓ move.
- The search field filters by title or type. Reordering is disabled while a filter is active.
- Every change supports Undo. Ctrl/Cmd+S saves the asset.

## 3. Fix validation issues

The panel at the bottom lists errors, warnings and info as you edit. Click an issue to select its step. Rows show an icon with the worst issue of the step. Scene targets are checked against the open scenes.

## 4. Set up the scene

1. Add **Scenario Runner Behaviour** to a GameObject and assign the scenario. With *Auto Start* it starts on Play.
2. Add **Scenario Target** to the objects your steps refer to and set their ids.
3. Report what happens:
   - **Scenario Signal Emitter** on a button: wire `Raise()` to `On Click`.
   - From code: `runner.Context.Signals.Raise("door_opened")`, `runner.Context.Blackboard.Set("speed", 3.5f)`.
   - Mistakes: `runner.Context.ReportMistake("Opened the valve too early.")`.
4. Show the instruction: wire **On Instruction Changed (string)** to a text component.
5. Make the scene follow the scenario:
   - **Scenario Step Events:** pick a step from the list and wire *On Started* / *On Ended*, for example to enable a button only during its step.
   - **Scenario Signal Listener:** run a UnityEvent when a signal is raised, for example by a *Send Signal* step.

## 5. Play

Press Play. With the scenario open, the window shows the debugger: the active step, results, the blackboard and an event log. See [Debugging](debugging.md).

## The sample

Import **Assembly Procedure** from the package's Samples tab. It uses every built-in step type, buttons gated by step events, a signal listener and a result screen. It needs URP and uGUI, which are in Unity's default templates.
