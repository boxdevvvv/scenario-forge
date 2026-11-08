using System;
using UnityEngine;
using UnityEngine.Events;

namespace ScenarioForge
{
    /// <summary>The UnityEvents that <see cref="ScenarioStepEvents"/> invokes for one step.</summary>
    [Serializable]
    public sealed class StepEventBinding
    {
        [Tooltip("The step these events belong to. Pick it from the list of the runner's scenario.")]
        [SerializeField] string stepId;
        [SerializeField] UnityEvent onStarted = new();
        [SerializeField] UnityEvent onEnded = new();

        /// <summary>Id of the step these events belong to.</summary>
        public string StepId
        {
            get => stepId;
            set => stepId = value;
        }

        /// <summary>Invoked when the step starts, and again on every retry.</summary>
        public UnityEvent OnStarted => onStarted;

        /// <summary>Invoked when the step ends, whatever the outcome (succeeded, failed, timed out, skipped or aborted).</summary>
        public UnityEvent OnEnded => onEnded;
    }
}
