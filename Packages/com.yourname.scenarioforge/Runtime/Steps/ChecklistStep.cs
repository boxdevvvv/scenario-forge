using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>
    /// Completes when every item's signal has been raised, optionally in order. Updates the instruction with
    /// the progress, for example <c>"Check the mirrors (2/4)"</c>.
    /// </summary>
    [Serializable]
    [ScenarioStepMenu("Signals/Checklist", Tooltip = "Completes when every item's signal is raised, optionally in order.")]
    public sealed class ChecklistStep : ScenarioStep
    {
        [SerializeField] List<ChecklistItem> items = new();
        [SerializeField] bool ordered;

        [NonSerialized] bool[] done;
        [NonSerialized] int doneCount;
        [NonSerialized] ScenarioContext activeContext;
        [NonSerialized] Action<string> onSignalRaised;

        /// <summary>The items to check.</summary>
        public IReadOnlyList<ChecklistItem> Items => items;

        /// <summary>Whether items must be checked in list order. Out-of-order signals are ignored.</summary>
        public bool Ordered
        {
            get => ordered;
            internal set => ordered = value;
        }

        /// <summary>How many items are checked in the current attempt.</summary>
        public int CheckedCount => doneCount;

        internal List<ChecklistItem> MutableItems => items;

        /// <inheritdoc />
        public override bool CanFail => false;

        /// <inheritdoc />
        protected internal override void OnEnter(ScenarioContext context)
        {
            done = new bool[items.Count];
            doneCount = 0;
            activeContext = context;
            onSignalRaised ??= OnSignalRaised;
            context.Signals.Raised += onSignalRaised;
            if (items.Count > 0)
                context.UpdateInstruction(FormatProgress());
        }

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            if (items.Count == 0)
                return Fail("The checklist has no items, so it can never finish. Add at least one item.");

            return doneCount >= items.Count ? StepStatus.Succeeded : StepStatus.Running;
        }

        /// <inheritdoc />
        protected internal override void OnExit(ScenarioContext context, StepOutcome outcome)
        {
            context.Signals.Raised -= onSignalRaised;
            activeContext = null;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (items.Count == 0)
            {
                report.Add(ValidationSeverity.Error, "The checklist has no items. Add at least one item.", Id);
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < items.Count; i++)
            {
                string signal = items[i].Signal?.Trim();
                if (string.IsNullOrEmpty(signal))
                {
                    report.Add(ValidationSeverity.Error, $"Item {i + 1} has no signal, so it can never be checked. Enter its signal.", Id);
                    continue;
                }

                if (!seen.Add(signal))
                    report.Add(ValidationSeverity.Warning, $"Duplicate signal \"{signal}\": one raise checks only one item. Use a different signal per item.", Id);
            }
        }

        void OnSignalRaised(string signal)
        {
            if (activeContext == null || done == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                if (done[i])
                    continue;

                bool matches = string.Equals(items[i].Signal?.Trim(), signal, StringComparison.Ordinal);
                if (matches)
                {
                    done[i] = true;
                    doneCount++;
                    activeContext.UpdateInstruction(FormatProgress());
                    return;
                }

                if (ordered)
                    return;
            }
        }

        string FormatProgress()
        {
            string text = string.IsNullOrEmpty(Instruction) ? "Checklist" : Instruction;
            return $"{text} ({doneCount}/{items.Count})";
        }
    }
}
