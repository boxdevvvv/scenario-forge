using System.Collections.Generic;
using UnityEngine;

namespace ScenarioForge.Tests
{
    /// <summary>Builds in-memory scenarios for tests and destroys them afterwards.</summary>
    sealed class TestScenarios : System.IDisposable
    {
        readonly List<Object> created = new();

        public ScenarioDefinition Create(params ScenarioStep[] steps)
        {
            foreach (ScenarioStep step in steps)
            {
                if (step != null && string.IsNullOrEmpty(step.Id))
                    step.AssignNewId();
            }

            var definition = ScriptableObject.CreateInstance<ScenarioDefinition>();
            definition.name = "TestScenario";
            definition.SetContent("Test scenario", "Created by a test.", steps);
            created.Add(definition);
            return definition;
        }

        public static ProbeStep Probe(string title = null, int ticksToFinish = -1)
        {
            var step = new ProbeStep { ticksToFinish = ticksToFinish, Title = title };
            step.AssignNewId();
            return step;
        }

        public void Dispose()
        {
            foreach (Object obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            created.Clear();
        }
    }
}
