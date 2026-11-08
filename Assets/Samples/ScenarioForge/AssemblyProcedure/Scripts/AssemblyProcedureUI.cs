using UnityEngine;
using UnityEngine.UI;

namespace ScenarioForge.Samples
{
    /// <summary>
    /// The sample's progress bar, step counter and result screen. The instruction text and the button
    /// states are wired in the Inspector (On Instruction Changed and a Scenario Step Events component).
    /// </summary>
    public sealed class AssemblyProcedureUI : MonoBehaviour
    {
        [SerializeField] ScenarioRunnerBehaviour runner;
        [SerializeField] AssemblyStation station;
        [SerializeField] Text stepLabel;
        [SerializeField] RectTransform progressFill;
        [SerializeField] GameObject resultPanel;
        [SerializeField] Text resultTitle;
        [SerializeField] Text resultDetails;

        void OnEnable()
        {
            runner.StepStarted += OnStepStarted;
            runner.StepEnded += OnStepEnded;
            runner.OnScenarioCompleted.AddListener(ShowResult);
            runner.OnScenarioFailed.AddListener(ShowResult);
            resultPanel.SetActive(false);
            SetProgress(0, StepCount);
        }

        void OnDisable()
        {
            runner.StepStarted -= OnStepStarted;
            runner.StepEnded -= OnStepEnded;
            runner.OnScenarioCompleted.RemoveListener(ShowResult);
            runner.OnScenarioFailed.RemoveListener(ShowResult);
        }

        /// <summary>Resets the station and runs the scenario again. Wired to the Restart button.</summary>
        public void Restart()
        {
            resultPanel.SetActive(false);
            station.ResetStation();
            runner.StartScenario();
        }

        int StepCount => runner.Definition != null ? runner.Definition.Steps.Count : 0;

        void OnStepStarted(StepInfo info)
        {
            stepLabel.text = $"Step {info.Index + 1} of {StepCount}  ·  {info.Title}";
            SetProgress(info.Index, StepCount);
        }

        void OnStepEnded(StepResult result) => SetProgress(result.Index + 1, StepCount);

        void SetProgress(int done, int total)
        {
            float fraction = total > 0 ? Mathf.Clamp01((float)done / total) : 0f;
            progressFill.anchorMax = new Vector2(fraction, 1f);
        }

        void ShowResult()
        {
            ScenarioRunner run = runner.Runner;
            bool completed = run.State == RunnerState.Completed;
            string reason = null;
            foreach (StepResult result in run.Results)
            {
                if (result.FailureReason != null)
                    reason = result.FailureReason;
            }

            resultTitle.text = completed ? "Procedure completed" : "Procedure failed";
            resultDetails.text = $"Total time: {run.Context.TotalElapsed:0.0} s\nSteps finished: {run.Results.Count} of {StepCount}" +
                                 (reason != null ? $"\n\n{reason}" : string.Empty);
            stepLabel.text = completed ? "All steps done" : "Stopped";
            resultPanel.SetActive(true);
        }
    }
}
