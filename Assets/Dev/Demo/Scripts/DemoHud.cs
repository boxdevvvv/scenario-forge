using System.Collections.Generic;
using System.Text;
using ScenarioForge.Steps;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioForge.Demo
{
    /// <summary>
    /// The demo's screen: the step list, the current instruction, mistake messages and a final report.
    /// It also turns keys into signals and offers pause, skip and restart controls.
    /// </summary>
    public sealed class DemoHud : MonoBehaviour
    {
        const float ToastSeconds = 3f;

        [SerializeField] ScenarioRunnerBehaviour runner;
        [SerializeField] DemoPlayer player;
        [SerializeField] DemoPickup extinguisher;
        [SerializeField] DemoFire fire;

        readonly StringBuilder text = new();
        bool checklistActive;
        int currentAttempt = 1;
        string instruction = string.Empty;
        string toast;
        float toastUntil;
        GUIStyle panelStyle;
        GUIStyle instructionStyle;
        GUIStyle toastStyle;
        GUIStyle titleStyle;

        /// <summary>Shows a new instruction. Wired to the runner's On Instruction Changed event in the scene.</summary>
        public void SetInstruction(string value) => instruction = value ?? string.Empty;

        /// <summary>Enables the 1/2/3 extinguisher keys. Wired to the checklist step's events in the scene.</summary>
        public void SetChecklistActive(bool value) => checklistActive = value;

        void OnEnable()
        {
            if (runner == null)
                return;
            runner.StepStarted += HandleStepStarted;
            runner.MistakeReported += HandleMistake;
        }

        void OnDisable()
        {
            if (runner == null)
                return;
            runner.StepStarted -= HandleStepStarted;
            runner.MistakeReported -= HandleMistake;
        }

        void HandleStepStarted(StepInfo info) => currentAttempt = info.Attempt;

        void HandleMistake(ScenarioMistake mistake)
        {
            toast = mistake.Message;
            toastUntil = Time.unscaledTime + ToastSeconds;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || runner == null || runner.Runner == null)
                return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Raise("confirm");
            if (keyboard.digit1Key.wasPressedThisFrame) Operate("pull_pin");
            if (keyboard.digit2Key.wasPressedThisFrame) Operate("aim");
            if (keyboard.digit3Key.wasPressedThisFrame) Operate("squeeze");
            if (keyboard.pKey.wasPressedThisFrame) TogglePause();
            if (keyboard.nKey.wasPressedThisFrame) runner.Runner.SkipCurrentStep();
            if (keyboard.rKey.wasPressedThisFrame) Restart();
        }

        void Raise(string signal) => runner.Context?.Signals.Raise(signal);

        /// <summary>Handles an extinguisher key: raises its signal, or reports a mistake when it makes no sense yet.</summary>
        void Operate(string signal)
        {
            ScenarioContext context = runner.Context;
            if (context == null || (fire != null && fire.IsReady))
                return;

            if (extinguisher != null && !extinguisher.IsHeld)
                context.ReportMistake("Tried to operate the extinguisher without holding it.");
            else if (!checklistActive)
                context.ReportMistake("Too early: get close to the fire before preparing the extinguisher.");
            else
                context.Signals.Raise(signal);
        }

        void TogglePause()
        {
            ScenarioRunner r = runner.Runner;
            if (r.State == RunnerState.Running) r.Pause();
            else if (r.State == RunnerState.Paused) r.Resume();
        }

        void Restart()
        {
            toast = null;
            checklistActive = false;
            player?.ResetDemo();
            extinguisher?.ResetDemo();
            fire?.ResetDemo();
            runner.StartScenario();
        }

        void OnGUI()
        {
            if (runner == null || runner.Runner == null)
                return;

            EnsureStyles();
            ScenarioRunner r = runner.Runner;
            bool finished = r.State == RunnerState.Completed || r.State == RunnerState.Failed || r.State == RunnerState.Aborted;

            GUILayout.BeginArea(new Rect(12, 12, 380, Screen.height - 24), panelStyle);
            GUILayout.Label($"<b>{r.Definition.Title}</b>   <color=#{StateColor(r.State)}>{r.State}</color>");
            GUILayout.Label($"Time {r.Context.TotalElapsed:0.0}s   ·   Mistakes <color=#{(r.Mistakes.Count > 0 ? "ff6b6b" : "6fdc6f")}>{r.Mistakes.Count}</color>");
            GUILayout.Space(6);
            DrawSteps(r);
            GUILayout.Space(6);
            DrawBlackboard(r);
            GUILayout.Space(6);
            DrawButtons(r);
            GUILayout.FlexibleSpace();
            GUILayout.Label("<b>Keys</b>  WASD move · E take · Space spray\n" +
                            "Enter continue · 1 2 3 operate extinguisher\nP pause · N skip step · R restart");
            GUILayout.EndArea();

            if (finished)
                DrawReport(r);
            else
                DrawInstruction(r);

            if (!string.IsNullOrEmpty(toast) && Time.unscaledTime < toastUntil)
            {
                float width = Mathf.Min(620f, Screen.width - 420f);
                GUI.Box(new Rect(400 + (Screen.width - 412 - width) / 2f, 16, Mathf.Max(260f, width), 64), "<b>Mistake</b>\n" + toast, toastStyle);
            }
        }

        static bool TryGetResult(ScenarioRunner r, int index, out StepResult result)
        {
            for (int i = r.Results.Count - 1; i >= 0; i--)
            {
                if (r.Results[i].Index == index)
                {
                    result = r.Results[i];
                    return true;
                }
            }

            result = default;
            return false;
        }

        static string TitleOf(ScenarioRunner r, int index)
        {
            ScenarioStep step = index >= 0 && index < r.Definition.Steps.Count ? r.Definition.Steps[index] : null;
            return step != null ? step.Title : "(missing type)";
        }

        void DrawSteps(ScenarioRunner r)
        {
            for (int i = 0; i < r.Definition.Steps.Count; i++)
            {
                ScenarioStep step = r.Definition.Steps[i];
                string title = TitleOf(r, i);
                bool isCurrent = i == r.CurrentStepIndex && (r.State == RunnerState.Running || r.State == RunnerState.Paused);
                text.Clear();
                if (isCurrent)
                {
                    text.Append("<color=#ffd34d>[>] ").Append(i + 1).Append(". ").Append(title)
                        .Append("  ").Append(r.CurrentStepElapsed.ToString("0.0")).Append('s');
                    if (step != null && step.TimeoutSeconds > 0f)
                        text.Append(" / ").Append(step.TimeoutSeconds.ToString("0")).Append('s');
                    if (currentAttempt > 1)
                        text.Append("  attempt ").Append(currentAttempt);
                    text.Append("</color>");
                }
                else if (TryGetResult(r, i, out StepResult result))
                {
                    text.Append("<color=#").Append(OutcomeColor(result.Outcome)).Append(">[")
                        .Append(OutcomeMark(result.Outcome)).Append("] ").Append(i + 1).Append(". ").Append(title)
                        .Append("  ").Append(result.Duration.ToString("0.0")).Append('s');
                    if (result.Outcome != StepOutcome.Succeeded)
                        text.Append("  ").Append(result.Outcome);
                    text.Append("</color>");
                    if (result.Mistakes > 0)
                        text.Append("  <color=#ff6b6b>").Append(result.Mistakes).Append(result.Mistakes == 1 ? " mistake" : " mistakes").Append("</color>");
                }
                else
                {
                    text.Append("<color=#9a9a9a>[ ] ").Append(i + 1).Append(". ").Append(title).Append("</color>");
                }
                GUILayout.Label(text.ToString());

                if (isCurrent && r.CurrentStep is ChecklistStep checklist)
                {
                    for (int item = 0; item < checklist.Items.Count; item++)
                    {
                        string mark = item < checklist.CheckedCount ? "x" : " ";
                        GUILayout.Label($"        [{mark}] {item + 1}  {checklist.Items[item].Label}");
                    }
                }
            }
        }

        void DrawBlackboard(ScenarioRunner r)
        {
            GUILayout.Label("<b>Blackboard</b>");
            if (r.Context.Blackboard.Count == 0)
            {
                GUILayout.Label("<color=#9a9a9a>  (empty)</color>");
                return;
            }
            foreach (string key in r.Context.Blackboard.Keys)
            {
                r.Context.Blackboard.TryGetValue(key, out BlackboardValue value);
                GUILayout.Label($"  {key} = {value}");
            }
        }

        void DrawButtons(ScenarioRunner r)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Continue")) Raise("confirm");
            if (GUILayout.Button(r.State == RunnerState.Paused ? "Resume" : "Pause")) TogglePause();
            if (GUILayout.Button("Skip step")) r.SkipCurrentStep();
            if (GUILayout.Button("Restart")) Restart();
            GUILayout.EndHorizontal();
        }

        void DrawInstruction(ScenarioRunner r)
        {
            string message = r.State == RunnerState.Paused ? "<b>Paused</b>  ·  " + instruction : instruction;
            if (fire != null && fire.IsSpraying)
                message += $"\n<color=#7fd4ff>Spraying… {fire.Intensity * 100f:0}% fire left</color>";
            else if (extinguisher != null && extinguisher.IsInReach && extinguisher.IsInteractable)
                message += "\n<color=#7fd4ff>Press E to take the extinguisher</color>";

            float width = Mathf.Max(280f, Mathf.Min(720f, Screen.width - 420f));
            var rect = new Rect(400 + (Screen.width - 412 - width) / 2f, Screen.height - 130, width, 110);
            GUI.Box(rect, message, instructionStyle);
        }

        void DrawReport(ScenarioRunner r)
        {
            float width = Mathf.Max(320f, Mathf.Min(640f, Screen.width - 430f));
            float height = Mathf.Min(Screen.height - 40f, 230f + r.Results.Count * 22f + Mathf.Min(r.Mistakes.Count, 6) * 20f);
            var rect = new Rect(400 + (Screen.width - 412 - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUILayout.BeginArea(rect, panelStyle);

            int skipped = 0, retries = 0;
            string failure = null;
            foreach (StepResult result in r.Results)
            {
                if (result.Outcome == StepOutcome.Skipped) skipped++;
                retries += result.Attempts - 1;
                if (result.FailureReason != null) failure = result.FailureReason;
            }

            string grade = r.State switch
            {
                RunnerState.Failed => "<color=#ff6b6b>FAILED</color>",
                RunnerState.Aborted => "<color=#ff6b6b>ABORTED</color>",
                _ when skipped > 0 => "<color=#c0c0c0>COMPLETED WITH SKIPPED STEPS</color>",
                _ when r.Mistakes.Count == 0 && retries == 0 => "<color=#6fdc6f>EXCELLENT</color>",
                _ when r.Mistakes.Count <= 2 => "<color=#6fdc6f>PASSED</color>",
                _ => "<color=#ffd34d>PASSED, NEEDS PRACTICE</color>"
            };

            GUILayout.Label($"Drill report  ·  {grade}", titleStyle);
            GUILayout.Label($"Total time {r.Context.TotalElapsed:0.0}s   ·   Mistakes {r.Mistakes.Count}   ·   Retries {retries}   ·   Skipped {skipped}");
            if (failure != null)
                GUILayout.Label($"<color=#ff6b6b>{failure}</color>");
            GUILayout.Space(8);

            foreach (StepResult result in r.Results)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{result.Index + 1}. {TitleOf(r, result.Index)}", GUILayout.Width(width * 0.46f));
                GUILayout.Label($"<color=#{OutcomeColor(result.Outcome)}>{result.Outcome}</color>", GUILayout.Width(width * 0.18f));
                GUILayout.Label($"{result.Duration:0.0}s", GUILayout.Width(width * 0.1f));
                GUILayout.Label(result.Attempts > 1 ? $"x{result.Attempts}" : "", GUILayout.Width(width * 0.06f));
                GUILayout.Label(result.Mistakes > 0 ? $"<color=#ff6b6b>{result.Mistakes} err</color>" : "");
                GUILayout.EndHorizontal();
            }

            if (r.Mistakes.Count > 0)
            {
                GUILayout.Space(8);
                GUILayout.Label("<b>Mistakes</b>");
                for (int i = 0; i < r.Mistakes.Count && i < 6; i++)
                {
                    ScenarioMistake mistake = r.Mistakes[i];
                    string where = mistake.StepIndex >= 0 ? $"step {mistake.StepIndex + 1}" : "no step";
                    GUILayout.Label($"  {mistake.Time:0.0}s  ({where})  {mistake.Message}");
                }
                if (r.Mistakes.Count > 6)
                    GUILayout.Label($"  … and {r.Mistakes.Count - 6} more");
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Press R to run the drill again.");
            GUILayout.EndArea();
        }

        void EnsureStyles()
        {
            if (panelStyle != null)
                return;
            GUI.skin.label.richText = true;
            GUI.skin.label.wordWrap = true;
            panelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 10, 10) };
            instructionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(16, 16, 12, 12)
            };
            toastStyle = new GUIStyle(instructionStyle) { fontSize = 16 };
            toastStyle.normal.textColor = new Color(1f, 0.55f, 0.55f);
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };
        }

        static string StateColor(RunnerState state) => state switch
        {
            RunnerState.Completed => "6fdc6f",
            RunnerState.Failed or RunnerState.Aborted => "ff6b6b",
            RunnerState.Paused => "ffd34d",
            _ => "ffffff"
        };

        static string OutcomeColor(StepOutcome outcome) => outcome switch
        {
            StepOutcome.Succeeded => "6fdc6f",
            StepOutcome.Skipped => "c0c0c0",
            _ => "ff6b6b"
        };

        static char OutcomeMark(StepOutcome outcome) => outcome switch
        {
            StepOutcome.Succeeded => 'x',
            StepOutcome.Skipped => '-',
            _ => '!'
        };
    }
}
