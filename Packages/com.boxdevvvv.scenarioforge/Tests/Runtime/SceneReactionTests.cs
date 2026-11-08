using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ScenarioForge.Steps;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScenarioForge.Tests
{
    public sealed class SceneReactionTests
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
            definition.SetContent("Scene reaction test", null, steps);
            created.Add(definition);
            return definition;
        }

        ScenarioRunnerBehaviour CreateRunner(ScenarioDefinition definition, out GameObject go)
        {
            go = new GameObject("Scenario Runner");
            go.SetActive(false);
            created.Add(go);
            var runner = go.AddComponent<ScenarioRunnerBehaviour>();
            runner.Definition = definition;
            return runner;
        }

        T AddInactive<T>(string name, out GameObject go) where T : Component
        {
            go = new GameObject(name);
            go.SetActive(false);
            created.Add(go);
            return go.AddComponent<T>();
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, int maxFrames = 300)
        {
            for (int i = 0; i < maxFrames && !condition(); i++)
                yield return null;
        }

        [UnityTest]
        public IEnumerator StepEvents_InvokeStartedAndEnded_OnlyForTheBoundStep()
        {
            var first = new InstructionStep { Instruction = "Go", ConfirmSignal = "confirm" };
            var second = new WaitForSignalStep { Signal = "picked" };
            ScenarioRunnerBehaviour runner = CreateRunner(CreateScenario(first, second), out GameObject runnerGo);
            ScenarioStepEvents events = AddInactive<ScenarioStepEvents>("Step Events", out GameObject eventsGo);
            events.Runner = runner;
            var log = new List<string>();
            var binding = new StepEventBinding { StepId = second.Id };
            binding.OnStarted.AddListener(() => log.Add("started"));
            binding.OnEnded.AddListener(() => log.Add("ended"));
            events.Bindings.Add(binding);
            eventsGo.SetActive(true);
            runnerGo.SetActive(true);

            yield return null;
            Assert.That(log, Is.Empty, "The first step is not bound.");

            runner.Context.Signals.Raise("confirm");
            yield return WaitUntil(() => log.Count == 1);
            Assert.That(log, Is.EqualTo(new[] { "started" }));

            runner.Context.Signals.Raise("picked");
            yield return WaitUntil(() => log.Count == 2);
            Assert.That(log, Is.EqualTo(new[] { "started", "ended" }));
            Assert.That(runner.Runner.State, Is.EqualTo(RunnerState.Completed));
        }

        [UnityTest]
        public IEnumerator StepEvents_InvokeEnded_WhenTheStepIsSkipped()
        {
            var only = new WaitForSignalStep { Signal = "never" };
            ScenarioRunnerBehaviour runner = CreateRunner(CreateScenario(only), out GameObject runnerGo);
            ScenarioStepEvents events = AddInactive<ScenarioStepEvents>("Step Events", out GameObject eventsGo);
            int ended = 0;
            var binding = new StepEventBinding { StepId = only.Id };
            binding.OnEnded.AddListener(() => ended++);
            events.Bindings.Add(binding);
            runnerGo.SetActive(true);
            eventsGo.SetActive(true);

            yield return null;
            runner.Runner.SkipCurrentStep();

            Assert.That(ended, Is.EqualTo(1), "With no runner assigned, the first runner in the scene is used.");
        }

        [UnityTest]
        public IEnumerator StepEvents_InvokeEnded_WhenTheScenarioIsStartedAgain()
        {
            var only = new WaitForSignalStep { Signal = "never" };
            ScenarioRunnerBehaviour runner = CreateRunner(CreateScenario(only), out GameObject runnerGo);
            ScenarioStepEvents events = AddInactive<ScenarioStepEvents>("Step Events", out GameObject eventsGo);
            var log = new List<string>();
            var binding = new StepEventBinding { StepId = only.Id };
            binding.OnStarted.AddListener(() => log.Add("started"));
            binding.OnEnded.AddListener(() => log.Add("ended"));
            events.Bindings.Add(binding);
            runnerGo.SetActive(true);
            eventsGo.SetActive(true);
            yield return null;

            runner.StartScenario();

            Assert.That(log, Is.EqualTo(new[] { "started", "ended", "started" }));
        }

        [UnityTest]
        public IEnumerator SignalListener_ReactsToSendSignalStep_AndToEmitters()
        {
            ScenarioRunnerBehaviour runner = CreateRunner(
                CreateScenario(new SendSignalStep { Signal = "fire_started" }, new WaitForSignalStep { Signal = "never" }),
                out GameObject runnerGo);
            ScenarioSignalListener listener = AddInactive<ScenarioSignalListener>("Listener", out GameObject listenerGo);
            listener.Runner = runner;
            listener.SignalName = " fire_started ";
            int calls = 0;
            listener.OnSignal.AddListener(() => calls++);
            listenerGo.SetActive(true);
            runnerGo.SetActive(true);

            yield return WaitUntil(() => calls == 1);
            Assert.That(calls, Is.EqualTo(1));

            ScenarioSignalEmitter emitter = AddInactive<ScenarioSignalEmitter>("Emitter", out GameObject emitterGo);
            emitterGo.SetActive(true);
            emitter.Runner = runner;
            emitter.RaiseSignal("fire_started");
            emitter.RaiseSignal("other");

            Assert.That(calls, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SignalListener_KeepsWorkingAfterTheScenarioIsStartedAgain()
        {
            ScenarioRunnerBehaviour runner = CreateRunner(CreateScenario(new WaitForSignalStep { Signal = "never" }), out GameObject runnerGo);
            ScenarioSignalListener listener = AddInactive<ScenarioSignalListener>("Listener", out GameObject listenerGo);
            listener.SignalName = "ping";
            int calls = 0;
            listener.OnSignal.AddListener(() => calls++);
            runnerGo.SetActive(true);
            listenerGo.SetActive(true);
            yield return null;

            runner.StartScenario();
            runner.Context.Signals.Raise("ping");

            Assert.That(calls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Behaviour_ForwardsMistakes()
        {
            ScenarioRunnerBehaviour runner = CreateRunner(CreateScenario(new WaitForSignalStep { Signal = "never" }), out GameObject runnerGo);
            var mistakes = new List<ScenarioMistake>();
            runner.MistakeReported += mistakes.Add;
            runnerGo.SetActive(true);
            yield return null;

            runner.Context.ReportMistake("Sprayed too early");

            Assert.That(mistakes, Has.Count.EqualTo(1));
            Assert.That(mistakes[0].StepIndex, Is.EqualTo(0));
        }
    }
}
