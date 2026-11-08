using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ScenarioForge.Editor
{
    /// <summary>Kind of a <see cref="DebugEventLog"/> entry.</summary>
    enum DebugEventKind
    {
        Run,
        State,
        StepStarted,
        StepEnded,
        Signal,
        Mistake
    }

    /// <summary>One event of the Play Mode debugger log.</summary>
    readonly struct DebugEvent
    {
        public readonly float Time;
        public readonly DebugEventKind Kind;
        public readonly string Message;

        public DebugEvent(float time, DebugEventKind kind, string message)
        {
            Time = time;
            Kind = kind;
            Message = message;
        }

        /// <summary>"12.3s  Signal  Signal "door" raised."</summary>
        public override string ToString() => $"{Time.ToString("0.00", CultureInfo.InvariantCulture)}s  {Kind}  {Message}";
    }

    /// <summary>The latest <see cref="Capacity"/> runner events, oldest first.</summary>
    sealed class DebugEventLog
    {
        public const int Capacity = 200;

        readonly List<DebugEvent> entries = new();

        /// <summary>Changes every time the log changes, so views can skip redundant refreshes.</summary>
        public int Version { get; private set; }

        public IReadOnlyList<DebugEvent> Entries => entries;

        public void Add(float time, DebugEventKind kind, string message)
        {
            if (entries.Count == Capacity)
                entries.RemoveAt(0);
            entries.Add(new DebugEvent(time, kind, message ?? string.Empty));
            Version++;
        }

        public void Clear()
        {
            entries.Clear();
            Version++;
        }

        /// <summary>The whole log as text, one event per line, for the clipboard.</summary>
        public string ToText()
        {
            var text = new StringBuilder();
            foreach (DebugEvent entry in entries)
                text.Append(entry).Append('\n');
            return text.ToString();
        }
    }
}
