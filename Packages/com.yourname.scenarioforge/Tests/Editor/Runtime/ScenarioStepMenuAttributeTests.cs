using NUnit.Framework;

namespace ScenarioForge.Tests
{
    public sealed class ScenarioStepMenuAttributeTests
    {
        [TestCase("Flow/Wait", "Flow", "Wait")]
        [TestCase("Custom/Machines/Press Lever", "Custom/Machines", "Press Lever")]
        [TestCase("Lonely", ScenarioStepMenuAttribute.DefaultCategory, "Lonely")]
        public void Path_IsSplitIntoCategoryAndName(string path, string category, string name)
        {
            var attribute = new ScenarioStepMenuAttribute(path);

            Assert.That(attribute.Category, Is.EqualTo(category));
            Assert.That(attribute.DisplayName, Is.EqualTo(name));
        }
    }
}
