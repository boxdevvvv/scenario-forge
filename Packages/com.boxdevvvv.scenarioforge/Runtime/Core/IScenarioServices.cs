namespace ScenarioForge
{
    /// <summary>
    /// Optional, game-specific services that custom steps can reach through <see cref="ScenarioContext.Services"/>.
    /// </summary>
    /// <remarks>
    /// The package never implements or calls this interface. Implement it in your game to expose
    /// your own systems (audio, vehicle input, score) to your own step types, and cast it to your type
    /// inside the step.
    /// </remarks>
    public interface IScenarioServices
    {
    }
}
