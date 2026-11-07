using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ScenarioForge.Steps;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScenarioForge.Tests
{
    public sealed class ScenarioRunnerBehaviourTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }

            created.Clear();
        }

        ScenarioDefinition CreateScenario(params ScenarioStep[] steps)
        {
            foreach (ScenarioStep step in steps)
                step.AssignNewId();

            var definition = ScriptableObject.CreateInstance<ScenarioDefinition>();
            definition.SetContent("PlayMode test", null, steps);
            created.Add(definition);
            return definition;
        }

        ScenarioRunnerBehaviour CreateRunner(ScenarioDefinition definition)
        {
            var go = new GameObject("Scenario Runner");
            go.SetActive(false);
            created.Add(go);
            var runner = go.AddComponent<ScenarioRunnerBehaviour>();
            runner.Definition = definition;
            go.SetActive(true);
            return runner;
        }

        ScenarioSignalEmitter CreateEmitter(string signal)
        {
            var go = new GameObject("Emitter " + signal);
            created.Add(go);
            var emitter = go.AddComponent<ScenarioSignalEmitter>();
            emitter.SignalName = signal;
            return emitter;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, int maxFrames = 300)
        {
            for (int i = 0; i < maxFrames && !condition(); i++)
                yield return null;
        }

        [UnityTest]
        public IEnumerator ThreeStepScenario_RunsToCompletion_WithSignalsFromEmitters()
        {
            ScenarioDefinition definition = CreateScenario(
                new InstructionStep { Instruction = "Press confirm", RequireConfirmation = true, ConfirmSignal = "confirm" },
                new WaitForSignalStep { Signal = "door_opened" },
                new WaitStep { Duration = 0.05f });
            ScenarioRunnerBehaviour runner = CreateRunner(definition);
            ScenarioSignalEmitter confirm = CreateEmitter("confirm");
            ScenarioSignalEmitter door = CreateEmitter("door_opened");
            bool started = false, completed = false;
            var instructions = new List<string>();
            runner.OnScenarioStarted.AddListener(() => started = true);
            runner.OnScenarioCompleted.AddListener(() => completed = true);
            runner.OnInstructionChanged.AddListener(instructions.Add);

            yield return null;

            Assert.That(started, Is.True);
            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Running));
            Assert.That(runner.Runner.CurrentStepIndex, Is.EqualTo(0));

            confirm.Raise();
            yield return WaitUntil(() => runner.Runner.CurrentStepIndex == 1);
            Assert.That(runner.Runner.CurrentStepIndex, Is.EqualTo(1));

            door.Raise();
            yield return WaitUntil(() => completed);

            Assert.That(completed, Is.True);
            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Completed));
            Assert.That(runner.Runner.Results.Count, Is.EqualTo(3));
            Assert.That(instructions[0], Is.EqualTo("Press confirm"));
        }

        [UnityTest]
        public IEnumerator ReachTargetStep_CompletesWhenTargetMovesIntoTheZone()
        {
            var cart = new GameObject("Cart");
            created.Add(cart);
            cart.AddComponent<ScenarioTarget>().TargetId = "cart";
            var zone = new GameObject("Zone");
            created.Add(zone);
            zone.transform.position = new Vector3(10f, 0f, 0f);
            zone.AddComponent<ScenarioTarget>().TargetId = "zone";

            ScenarioRunnerBehaviour runner = CreateRunner(CreateScenario(
                new ReachTargetStep { ActorTargetId = "cart", DestinationTargetId = "zone", Radius = 0.5f }));
            yield return null;
            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Running));

            for (int i = 0; i < 300 && runner.Runner.State == RunnerState.Running; i++)
            {
                cart.transform.position = Vector3.MoveTowards(cart.transform.position, zone.transform.position, 0.5f);
                yield return null;
            }

            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Completed));
        }

        [UnityTest]
        public IEnumerator Emitter_WithoutRunningScenario_LogsAWarning()
        {
            ScenarioSignalEmitter emitter = CreateEmitter("orphan");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no running ScenarioRunnerBehaviour"));

            emitter.Raise();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Destroying_TheBehaviour_AbortsTheRun()
        {
            ScenarioRunnerBehaviour runner = CreateRunner(CreateScenario(new WaitForSignalStep { Signal = "never" }));
            yield return null;
            ScenarioRunner inner = runner.Runner;

            Object.Destroy(runner.gameObject);
            yield return null;

            Assert.That(inner.State, Is.EqualTo(RunnerState.Aborted));
        }
    }
}
