using NUnit.Framework;

namespace ScenarioForge.Tests
{
    /// <summary>Verifies that the EditMode test assembly compiles and runs.</summary>
    public sealed class EditModeSmokeTests
    {
        [Test]
        public void PackageName_IsLowercase()
        {
            Assert.That(PackageInfo.PackageName, Is.EqualTo(PackageInfo.PackageName.ToLowerInvariant()));
        }
    }
}
