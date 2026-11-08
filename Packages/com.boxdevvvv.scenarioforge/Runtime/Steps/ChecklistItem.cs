using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>One entry of a <see cref="ChecklistStep"/>: a label and the signal that checks it.</summary>
    [Serializable]
    public struct ChecklistItem
    {
        [SerializeField] string label;
        [SerializeField] string signal;

        /// <summary>Creates an item.</summary>
        public ChecklistItem(string label, string signal)
        {
            this.label = label;
            this.signal = signal;
        }

        /// <summary>Text describing the item.</summary>
        public string Label => label;

        /// <summary>The signal that marks the item as done.</summary>
        public string Signal => signal;
    }
}
