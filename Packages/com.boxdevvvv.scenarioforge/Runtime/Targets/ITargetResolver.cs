using UnityEngine;

namespace ScenarioForge
{
    /// <summary>
    /// Resolves the text ids used by steps to scene objects. Steps reference scene objects by id because a
    /// <c>ScriptableObject</c> cannot reference objects in a scene.
    /// </summary>
    public interface ITargetResolver
    {
        /// <summary>Finds the transform registered under <paramref name="targetId"/>.</summary>
        /// <returns><c>false</c> when no live target has that id.</returns>
        bool TryGetTransform(string targetId, out Transform transform);
    }
}
