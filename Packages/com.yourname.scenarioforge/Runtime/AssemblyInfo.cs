using System.Runtime.CompilerServices;

// The editor tooling and the test assemblies need access to step lifecycle
// methods (OnEnter/OnTick/OnExit) and to id assignment, which are internal by design.
[assembly: InternalsVisibleTo("ScenarioForge.Editor")]
[assembly: InternalsVisibleTo("ScenarioForge.Editor.Tests")]
[assembly: InternalsVisibleTo("ScenarioForge.Runtime.Tests")]
