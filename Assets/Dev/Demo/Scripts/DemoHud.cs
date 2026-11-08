using System.Collections.Generic;
using System.Text;
using ScenarioForge.Steps;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioForge.Demo
{
    /// <summary>
    /// A debug HUD for the demo scene. It shows the step list and the current instruction,
    /// raises signals from keyboard shortcuts and offers pause, skip and restart controls.
    /// </summary>
    public sealed class DemoHud : MonoBehaviour
    {
        [SerializeField] ScenarioRunnerBehaviour runner;
        [SerializeField] DemoPlayer player;
        [SerializeField] DemoPickup extinguisher;
        [SerializeField] DemoFire fire;

        readonly Dictionary<int, StepResult> lastResults = new();
        readonly StringBuilder text = new();
        ScenarioRunner observedRunner;
        int currentAttempt = 1;
        string instruction = string.Empty;
        GUIStyle panelStyle;
        GUIStyle instructionStyle;

        /// <summary>Shows a new instruction. Wired to the runner's On Instruction Changed event in the scene.</summary>
        public void SetInstruction(string value)
        {
            instruction = value ?? string.Empty;
        }

        void Update()
        {
            ObserveRunner();
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || runner == null || runner.Runner == null)
                return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Raise("confirm");
            if (keyboard.digit1Key.wasPressedThisFrame) Raise("pull_pin");
            if (keyboard.digit2Key.wasPressedThisFrame) Raise("aim");
            if (keyboard.digit3Key.wasPressedThisFrame) Raise("squeeze");
            if (keyboard.pKey.wasPressedThisFrame) TogglePause();
            if (keyboard.nKey.wasPressedThisFrame) runner.Runner.SkipCurrentStep();
            if (keyboard.rKey.wasPressedThisFrame) Restart();
        }

        void ObserveRunner()
        {
            ScenarioRunner current = runner != null ? runner.Runner : null;
            if (current == observedRunner)
                return;

            if (observedRunner != null)
            {
                observedRunner.StepStarted -= HandleStepStarted;
                observedRunner.StepEnded -= HandleStepEnded;
            }

            observedRunner = current;
            lastResults.Clear();
            currentAttempt = 1;
            if (observedRunner != null)
            {
                observedRunner.StepStarted += HandleStepStarted;
                observedRunner.StepEnded += HandleStepEnded;
            }
        }

        void OnDestroy()
        {
            if (observedRunner == null)
                return;
            observedRunner.StepStarted -= HandleStepStarted;
            observedRunner.StepEnded -= HandleStepEnded;
        }

        void HandleStepStarted(StepInfo info) => currentAttempt = info.Attempt;

        void HandleStepEnded(StepResult result) => lastResults[result.Index] = result;

        void Raise(string signal) => runner.Context?.Signals.Raise(signal);

        void TogglePause()
        {
            ScenarioRunner r = runner.Runner;
            if (r.State == RunnerState.Running) r.Pause();
            else if (r.State == RunnerState.Paused) r.Resume();
        }

        void Restart()
        {
            player?.ResetDemo();
            extinguisher?.ResetDemo();
            fire?.ResetDemo();
            runner.StartScenario();
            ObserveRunner();
        }

        void OnGUI()
        {
            if (runner == null || runner.Runner == null)
                return;

            EnsureStyles();
            ScenarioRunner r = runner.Runner;
            GUILayout.BeginArea(new Rect(12, 12, 380, Screen.height - 24), panelStyle);
            GUILayout.Label($"<b>{r.Definition.Title}</b>   <color=#{StateColor(r.State)}>{r.State}</color>");
            GUILayout.Label($"Total time: {r.Context.TotalElapsed:0.0}s");
            GUILayout.Space(6);
            DrawSteps(r);
            GUILayout.Space(6);
            DrawBlackboard(r);
            GUILayout.Space(6);
            DrawButtons(r);
            GUILayout.FlexibleSpace();
            GUILayout.Label("<b>Keys</b>  WASD move · E pick up · Space spray\n" +
                            "Enter continue · 1 2 3 checklist\nP pause · N skip step · R restart");
            GUILayout.EndArea();

            DrawInstruction(r);
        }

        void DrawSteps(ScenarioRunner r)
        {
            IReadOnlyList<ScenarioStep> steps = r.Definition.Steps;
            for (int i = 0; i < steps.Count; i++)
            {
                ScenarioStep step = steps[i];
                string title = step != null ? step.Title : "(missing type)";
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
                else if (lastResults.TryGetValue(i, out StepResult result))
                {
                    text.Append("<color=#").Append(OutcomeColor(result.Outcome)).Append(">[")
                        .Append(OutcomeMark(result.Outcome)).Append("] ").Append(i + 1).Append(". ").Append(title)
                        .Append("  ").Append(result.Outcome).Append("</color>");
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
                        GUILayout.Label($"        [{mark}] {checklist.Items[item].Label}  <color=#9a9a9a>({checklist.Items[item].Signal})</color>");
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
            string message = r.State switch
            {
                RunnerState.Completed => "<b>Scenario completed!</b>  Press R to run it again.",
                RunnerState.Failed => "<b>Scenario failed.</b>  Press R to try again.",
                RunnerState.Paused => "<b>Paused</b>  ·  " + instruction,
                _ => instruction
            };
            if (fire != null && fire.IsSpraying)
                message += $"\n<color=#7fd4ff>Spraying… {fire.Intensity * 100f:0}% fire left</color>";
            else if (extinguisher != null && extinguisher.IsInReach)
                message += "\n<color=#7fd4ff>Press E to pick up the extinguisher</color>";

            float width = Mathf.Max(280f, Mathf.Min(720f, Screen.width - 420f));
            var rect = new Rect(400 + (Screen.width - 412 - width) / 2f, Screen.height - 130, width, 110);
            GUI.Box(rect, message, instructionStyle);
        }

        void EnsureStyles()
        {
            if (panelStyle != null)
                return;
            panelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 10, 10) };
            GUI.skin.label.richText = true;
            GUI.skin.label.wordWrap = true;
            instructionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(16, 16, 12, 12)
            };
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
