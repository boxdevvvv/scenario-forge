# Portfolio recording guide

Five short clips (spec section 17); the debugger clip doubles as the README hero. Each clip shows one idea. Recordings are made in the dev project; the `Tools > Scenario Forge > Dev > Portfolio` menu prepares each shot on a throwaway copy in `Assets/Dev/Portfolio` (git-ignored), so the demo and sample assets never change.

## Setup (once)

1. **Layout:**
   - Dock the Scenario Forge window next to the Inspector, and close panels the shot does not need.
   - Make the window about 1280×720 on screen, so the text is still readable once the clip is scaled down to 960 px wide.
2. **Theme:** record in the Editor's dark theme (Unity > Settings > General > Editor Theme). If time allows, record shot 1 again in the light theme for the visual review.
3. **Clean console:** clear it before each take. No errors or warnings may be visible.
4. **Recorder:**
   - Open macOS Screenshot with **Cmd+Shift+5**, then choose **Record Selected Portion** and draw the area around the Unity window.
   - In **Options**, set Microphone to None and turn on **Show Mouse Clicks**.
5. **Pace:** move the mouse slowly and pause about half a second after each click, so viewers can follow.
6. **Save:** stop with the stop button in the menu bar. Save the `.mov` files as `01-create.mov`, `02-reorder.mov` and so on, all in one folder. Converting them to optimized GIFs is a separate step.

## Shots

### 1. Create a scenario and add steps with search (`01-create`)

1. In the Project window: **Create > Scenario Forge > Scenario**, and name it `Lockout Procedure`.
2. Double-click the scenario. The window opens with the empty state.
3. **+ Add Step**, type `instr`, and pick **Instruction**. Type a title: `Put on gloves`.
4. **+ Add Step**, type `check`, and pick **Checklist**.
5. **+ Add Step**, type `signal`, and pick **Wait For Signal**.

Delete the asset after recording.

### 2. Reorder steps by dragging (`02-reorder`)

1. **Tools > Scenario Forge > Dev > Portfolio > 2 - Reorder Shot**. This opens a copy of the Fire Drill scenario.
2. Drag the last step up to position 2. Release it and pause.
3. Press **Cmd+Z**. The step goes back to where it was, which shows that Undo works.

### 3. Validation: click the error, fix it, the error disappears (`03-validation`)

1. **Tools > Scenario Forge > Dev > Portfolio > 3 - Validation Shot**. The copy has one Wait For Signal step with an empty signal. Its row shows an error icon, and the validation panel lists the error.
2. Click the error in the validation panel. The step is selected and its details open.
3. Type a signal name, for example `alarm.pressed`. The icon and the error disappear while you type.

### 4. Play Mode debugger (`04-debugger`)

1. **Tools > Scenario Forge > Dev > Portfolio > 4 - Debugger Shot**. This opens the Fire Drill demo scene and its scenario in the window.
2. Arrange the Game view and the Scenario Forge window side by side, with the **Event Log** tab open.
3. Press Play and play the first 3–4 steps. The active row moves down with ✓ marks, the event log fills in, and a mistake (for example, spraying before picking up the extinguisher) appears in the log.
4. Optional: press **Skip Step** once.

### 5. A custom step appears in the menu (`05-custom-step`)

1. Before recording, copy the `PressLeverStep` example from `Documentation~/creating-custom-steps.md` into a new file, `Assets/Dev/Portfolio/PressLeverStep.cs`. Leave the file open in your code editor and don't save it yet.
2. Start recording with the code visible. Save the file, then switch to Unity and wait for the compile spinner. You can cut the wait later.
3. In the Scenario Forge window: **+ Add Step**, type `lever`, and pick **Custom > Press Lever**. Its fields appear in the details panel.

Clean up with **Tools > Scenario Forge > Dev > Portfolio > Delete Shot Copies**.

## Output

- **Size:** GIFs are 900 px wide (the debugger clip 1200 px), each well under 5 MB.
- **Location:** `Packages/com.boxdevvvv.scenarioforge/Documentation~/images/`, named like the recordings (`01-create.gif` … `05-custom-step.gif`). The README hero is `04-debugger.gif`.
- **Archive:** keep the original `.mov` files outside the repository.

## How the published GIFs were made

The GIFs in `Documentation~/images` were recorded without a screen recorder, so they can be redone the same way:

1. **`PortfolioRecorder`** (`Assets/Dev/Editor`) saves numbered PNG frames of chosen editor windows, read from each window's own pixels. Popups such as the Add Step search are drawn in place. It uses the Editor's internal `GUIView.GrabPixels` through reflection, so it is dev tooling only and may need changes in a future Unity version. The mouse cursor is not captured.
2. **`PortfolioAutopilot`** plays the Fire Drill demo in Play Mode for the debugger clip, including one deliberate mistake, so no keyboard input is needed.
3. [`makegif.py`](makegif.py) turns the frames into GIFs with ffmpeg (Python with `imageio-ffmpeg`, `pillow` and `numpy`). Frames that only differ by a blinking caret are dropped, pauses are shortened, and a single 256-colour palette is used so error icons stay red. Clip 5 starts with a card showing the `PressLeverStep` source.
