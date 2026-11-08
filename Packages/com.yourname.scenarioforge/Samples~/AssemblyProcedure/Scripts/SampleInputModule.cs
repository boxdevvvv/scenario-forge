using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ScenarioForge.Samples
{
    /// <summary>
    /// Adds the right UI input module for the project's input settings, so the sample works whether the
    /// project uses the Input Manager, the Input System package, or both. The sample does not depend on
    /// the Input System package: its module is looked up by name.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public sealed class SampleInputModule : MonoBehaviour
    {
        const string InputSystemModule = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        void Awake()
        {
            if (GetComponent<BaseInputModule>() != null)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
            gameObject.AddComponent<StandaloneInputModule>();
#else
            Type module = Type.GetType(InputSystemModule);
            if (module != null)
                gameObject.AddComponent(module);
            else
                Debug.LogError("The sample UI needs an input module: enable the Input Manager in Project Settings > Player > Active Input Handling, or install the Input System package.", this);
#endif
        }
    }
}
