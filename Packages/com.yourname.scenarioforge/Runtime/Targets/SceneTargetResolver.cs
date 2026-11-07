using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScenarioForge
{
    /// <summary>
    /// An <see cref="ITargetResolver"/> built from <see cref="ScenarioTarget"/> components.
    /// </summary>
    public sealed class SceneTargetResolver : ITargetResolver
    {
        readonly Dictionary<string, ScenarioTarget> targets = new(StringComparer.Ordinal);

        /// <summary>Registers the given targets. Duplicate ids log a warning and keep the first one.</summary>
        public SceneTargetResolver(IEnumerable<ScenarioTarget> sceneTargets)
        {
            if (sceneTargets == null)
                return;

            foreach (ScenarioTarget target in sceneTargets)
                Register(target);
        }

        /// <summary>The number of registered targets.</summary>
        public int Count => targets.Count;

        /// <summary>Creates a resolver from every <see cref="ScenarioTarget"/> in the loaded scenes, including inactive ones.</summary>
        public static SceneTargetResolver FromLoadedScenes()
        {
            return new SceneTargetResolver(Object.FindObjectsByType<ScenarioTarget>(FindObjectsInactive.Include));
        }

        /// <inheritdoc />
        public bool TryGetTransform(string targetId, out Transform transform)
        {
            transform = null;
            if (string.IsNullOrEmpty(targetId) || !targets.TryGetValue(targetId.Trim(), out ScenarioTarget target) || target == null)
                return false;

            transform = target.transform;
            return true;
        }

        void Register(ScenarioTarget target)
        {
            if (target == null)
                return;

            string id = target.TargetId?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"ScenarioTarget on '{target.name}' has no Target Id, so steps cannot find it. Set its Target Id in the Inspector.", target);
                return;
            }

            if (targets.TryGetValue(id, out ScenarioTarget existing) && existing != null)
            {
                Debug.LogWarning(
                    $"Duplicate ScenarioTarget id '{id}' on '{existing.name}' and '{target.name}'. Steps will use '{existing.name}'. Give each target a unique id.",
                    target);
                return;
            }

            targets[id] = target;
        }
    }
}
