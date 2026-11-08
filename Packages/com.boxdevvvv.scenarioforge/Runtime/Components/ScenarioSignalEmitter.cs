using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// Raises a signal on a <see cref="ScenarioRunnerBehaviour"/>. Hook <see cref="Raise"/> to a button,
    /// a trigger or any UnityEvent in the Inspector.
    /// </summary>
    [AddComponentMenu("Scenario Forge/Scenario Signal Emitter")]
    public sealed class ScenarioSignalEmitter : MonoBehaviour
    {
        [SerializeField] string signalName;
        [Tooltip("The runner that receives the signal. Leave empty to use the first runner in the scene.")]
        [SerializeField] ScenarioRunnerBehaviour runner;

        /// <summary>The signal raised by <see cref="Raise"/>.</summary>
        public string SignalName
        {
            get => signalName;
            set => signalName = value;
        }

        /// <summary>The target runner. When <c>null</c>, the first runner in the scene is used.</summary>
        public ScenarioRunnerBehaviour Runner
        {
            get => runner;
            set => runner = value;
        }

        /// <summary>Raises <see cref="SignalName"/>.</summary>
        public void Raise() => RaiseSignal(signalName);

        /// <summary>Raises <paramref name="signal"/>. Useful from UnityEvents that pass a string.</summary>
        public void RaiseSignal(string signal)
        {
            if (string.IsNullOrWhiteSpace(signal))
            {
                Debug.LogWarning($"ScenarioSignalEmitter on '{name}' has no signal name. Set Signal Name in the Inspector.", this);
                return;
            }

            if (runner == null)
                runner = FindAnyObjectByType<ScenarioRunnerBehaviour>();

            if (runner == null || runner.Context == null)
            {
                Debug.LogWarning($"ScenarioSignalEmitter on '{name}' could not raise \"{signal}\": no running ScenarioRunnerBehaviour was found. Add one to the scene and start it.", this);
                return;
            }

            runner.Context.Signals.Raise(signal);
        }
    }
}
