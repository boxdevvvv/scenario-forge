using System;
using UnityEngine;

namespace ScenarioForge.Steps
{
    /// <summary>Completes when an actor target is within a radius of a destination target.</summary>
    [Serializable]
    [ScenarioStepMenu("Spatial/Reach Target", Tooltip = "Completes when the actor is within a radius of the destination.")]
    public sealed class ReachTargetStep : ScenarioStep
    {
        [SerializeField] string actorTargetId;
        [SerializeField] string destinationTargetId;
        [SerializeField, Min(0)] float radius = 1f;

        /// <summary>Target id of the object that has to move.</summary>
        public string ActorTargetId
        {
            get => actorTargetId;
            internal set => actorTargetId = value;
        }

        /// <summary>Target id of the place to reach.</summary>
        public string DestinationTargetId
        {
            get => destinationTargetId;
            internal set => destinationTargetId = value;
        }

        /// <summary>Maximum distance, in meters, between actor and destination.</summary>
        public float Radius
        {
            get => radius;
            internal set => radius = value;
        }

        /// <inheritdoc />
        protected internal override StepStatus OnTick(ScenarioContext context, float deltaTime)
        {
            if (!context.Targets.TryGetTransform(actorTargetId, out Transform actor))
                return Fail(MissingTarget("Actor", actorTargetId));

            if (!context.Targets.TryGetTransform(destinationTargetId, out Transform destination))
                return Fail(MissingTarget("Destination", destinationTargetId));

            float sqrDistance = (actor.position - destination.position).sqrMagnitude;
            return sqrDistance <= radius * radius ? StepStatus.Succeeded : StepStatus.Running;
        }

        /// <inheritdoc />
        public override void Validate(ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(actorTargetId))
                report.Add(ValidationSeverity.Error, "Actor target is empty. Enter the Target Id of the object that has to move.", Id);

            if (string.IsNullOrWhiteSpace(destinationTargetId))
                report.Add(ValidationSeverity.Error, "Destination target is empty. Enter the Target Id of the place to reach.", Id);

            if (radius <= 0f)
                report.Add(ValidationSeverity.Error, "Radius must be greater than 0.", Id);
        }

        static string MissingTarget(string role, string id)
        {
            return string.IsNullOrWhiteSpace(id)
                ? $"{role} target id is empty. Set it in the Scenario Forge window."
                : $"{role} target '{id}' was not found. Add a ScenarioTarget with that Target Id to the scene.";
        }
    }
}
