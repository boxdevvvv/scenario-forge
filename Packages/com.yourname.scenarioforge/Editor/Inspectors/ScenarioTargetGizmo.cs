using UnityEditor;
using UnityEngine;

namespace ScenarioForge.Editor
{
    /// <summary>Draws a marker and the target id of every <see cref="ScenarioTarget"/> in the Scene view.</summary>
    static class ScenarioTargetGizmo
    {
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        static void Draw(ScenarioTarget target, GizmoType gizmoType)
        {
            if (!target.DrawGizmo)
                return;

            Vector3 position = target.transform.position;
            bool selected = (gizmoType & GizmoType.Selected) != 0;
            Gizmos.color = selected ? Color.yellow : new Color(0.3f, 0.8f, 1f);
            Gizmos.DrawWireSphere(position, 0.25f);

            string label = string.IsNullOrEmpty(target.TargetId) ? "(no id)" : target.TargetId;
            Handles.Label(position + Vector3.up * 0.4f, label, EditorStyles.boldLabel);
        }
    }
}
