using System;
using UnityEngine;
using UnityEngine.Events;

namespace ScenarioForge
{
    /// <summary>
    /// Invokes a UnityEvent when a signal is raised in the scenario, for example by a
    /// <see cref="Steps.SendSignalStep"/>. The counterpart of <see cref="ScenarioSignalEmitter"/>.
    /// </summary>
    [AddComponentMenu("Scenario Forge/Scenario Signal Listener")]
    public sealed class ScenarioSignalListener : MonoBehaviour
    {
        [SerializeField] string signalName;
        [Tooltip("The runner to listen to. Leave empty to use the first runner in the scene.")]
        [SerializeField] ScenarioRunnerBehaviour runner;
        [SerializeField] UnityEvent onSignal = new();

        ScenarioRunnerBehaviour subscribed;

        /// <summary>The signal to listen for.</summary>
        public string SignalName
        {
            get => signalName;
            set => signalName = value;
        }

        /// <summary>The runner to listen to. When <c>null</c>, the first runner in the scene is used.</summary>
        public ScenarioRunnerBehaviour Runner
        {
            get => runner;
            set
            {
                runner = value;
                if (isActiveAndEnabled)
                    Subscribe();
            }
        }

        /// <summary>Invoked every time <see cref="SignalName"/> is raised.</summary>
        public UnityEvent OnSignal => onSignal;

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            Unsubscribe();
            if (runner == null)
                runner = FindAnyObjectByType<ScenarioRunnerBehaviour>();

            if (runner == null)
            {
                Debug.LogWarning($"ScenarioSignalListener on '{name}' found no ScenarioRunnerBehaviour. Add one to the scene or assign Runner.", this);
                return;
            }

            subscribed = runner;
            subscribed.SignalRaised += HandleSignalRaised;
        }

        void Unsubscribe()
        {
            if (subscribed == null)
                return;

            subscribed.SignalRaised -= HandleSignalRaised;
            subscribed = null;
        }

        void HandleSignalRaised(string signal)
        {
            if (!string.IsNullOrWhiteSpace(signalName) && string.Equals(signalName.Trim(), signal, StringComparison.Ordinal))
                onSignal.Invoke();
        }
    }
}
