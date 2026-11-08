using System.Collections.Generic;

namespace ScenarioForge
{
    /// <summary>
    /// Implemented by steps that reference scene targets by id, so editor validation can check that those
    /// targets exist in the open scenes.
    /// </summary>
    public interface ITargetReferencingStep
    {
        /// <summary>The target ids this step uses. Empty ids are ignored.</summary>
        IEnumerable<string> TargetIds { get; }
    }
}
