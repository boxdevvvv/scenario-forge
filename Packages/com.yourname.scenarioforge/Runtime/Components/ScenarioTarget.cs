using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// Marks a scene object so that steps can find it by <see cref="TargetId"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Scenario Forge/Scenario Target")]
    public sealed class ScenarioTarget : MonoBehaviour
    {
        [SerializeField] string targetId;
        [SerializeField] bool drawGizmo = true;

        /// <summary>The id steps use to find this object. Unique within the loaded scenes.</summary>
        public string TargetId
        {
            get => targetId;
            set => targetId = value;
        }

        /// <summary>Whether the Scene view draws a marker with the id.</summary>
        public bool DrawGizmo => drawGizmo;
    }
}
