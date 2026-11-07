using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScenarioForge.Tests
{
    /// <summary>Verifies that the PlayMode test assembly compiles and runs.</summary>
    public sealed class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator FramesAdvance_InPlayMode()
        {
            int startFrame = Time.frameCount;
            yield return null;
            Assert.That(Time.frameCount, Is.GreaterThan(startFrame));
        }
    }
}
