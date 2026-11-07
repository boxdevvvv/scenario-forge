using System;
using System.Collections.Generic;

namespace ScenarioForge
{
    /// <summary>
    /// Named events (<c>"door_opened"</c>) that the game raises and steps observe.
    /// </summary>
    /// <remarks>
    /// Names are case-sensitive. Leading and trailing whitespace is trimmed when a signal is raised or
    /// queried. Steps that only care about signals raised after they start should remember
    /// <see cref="GetCount"/> in <c>OnEnter</c>, or subscribe to <see cref="Raised"/>, rather than reset the counts
    /// for every other step.
    /// </remarks>
    public sealed class SignalBus
    {
        readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);

        /// <summary>Raised with the trimmed signal name every time a signal is raised.</summary>
        public event Action<string> Raised;

        /// <summary>Raises <paramref name="signal"/>: increments its count and notifies <see cref="Raised"/>.</summary>
        /// <exception cref="ArgumentException">When the name is empty or whitespace.</exception>
        public void Raise(string signal)
        {
            string name = Normalize(signal);
            if (name.Length == 0)
                throw new ArgumentException("Signal names cannot be empty. Use a name such as \"door_opened\".", nameof(signal));

            counts.TryGetValue(name, out int count);
            counts[name] = count + 1;
            Raised?.Invoke(name);
        }

        /// <summary>How many times <paramref name="signal"/> was raised since the last <see cref="ResetCounts"/>.</summary>
        public int GetCount(string signal)
        {
            string name = Normalize(signal);
            return name.Length > 0 && counts.TryGetValue(name, out int count) ? count : 0;
        }

        /// <summary>Whether <paramref name="signal"/> was raised at least once since the last <see cref="ResetCounts"/>.</summary>
        public bool WasRaised(string signal) => GetCount(signal) > 0;

        /// <summary>Sets every signal count back to zero.</summary>
        public void ResetCounts() => counts.Clear();

        static string Normalize(string signal)
        {
            if (string.IsNullOrEmpty(signal))
                return string.Empty;

            bool needsTrim = char.IsWhiteSpace(signal[0]) || char.IsWhiteSpace(signal[signal.Length - 1]);
            return needsTrim ? signal.Trim() : signal;
        }
    }
}
