using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScenarioForge.Tests
{
    public sealed class SceneTargetResolverTests
    {
        readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in objects)
                Object.DestroyImmediate(go);
            objects.Clear();
        }

        ScenarioTarget CreateTarget(string name, string id)
        {
            var go = new GameObject(name);
            objects.Add(go);
            var target = go.AddComponent<ScenarioTarget>();
            target.TargetId = id;
            return target;
        }

        [Test]
        public void TryGetTransform_FindsRegisteredTargets()
        {
            ScenarioTarget dock = CreateTarget("Dock", "dock");
            var resolver = new SceneTargetResolver(new[] { dock });

            Assert.That(resolver.TryGetTransform("dock", out Transform transform), Is.True);
            Assert.That(transform, Is.SameAs(dock.transform));
            Assert.That(resolver.TryGetTransform(" dock ", out _), Is.True);
            Assert.That(resolver.TryGetTransform("missing", out _), Is.False);
            Assert.That(resolver.TryGetTransform(null, out _), Is.False);
        }

        [Test]
        public void DuplicateIds_LogAWarningNamingBothObjects_AndKeepTheFirst()
        {
            ScenarioTarget first = CreateTarget("First", "dock");
            ScenarioTarget second = CreateTarget("Second", "dock");
            LogAssert.Expect(LogType.Warning, new Regex("Duplicate ScenarioTarget id 'dock' on 'First' and 'Second'"));

            var resolver = new SceneTargetResolver(new[] { first, second });

            Assert.That(resolver.Count, Is.EqualTo(1));
            resolver.TryGetTransform("dock", out Transform transform);
            Assert.That(transform, Is.SameAs(first.transform));
        }

        [Test]
        public void TargetWithoutId_IsSkippedWithAWarning()
        {
            ScenarioTarget unnamed = CreateTarget("Unnamed", "");
            LogAssert.Expect(LogType.Warning, new Regex("has no Target Id"));

            var resolver = new SceneTargetResolver(new[] { unnamed });

            Assert.That(resolver.Count, Is.Zero);
        }

        [Test]
        public void DestroyedTarget_IsNotResolved()
        {
            ScenarioTarget target = CreateTarget("Temp", "temp");
            var resolver = new SceneTargetResolver(new[] { target });

            Object.DestroyImmediate(target.gameObject);

            Assert.That(resolver.TryGetTransform("temp", out _), Is.False);
        }

        [Test]
        public void FromLoadedScenes_IncludesInactiveTargets()
        {
            ScenarioTarget hidden = CreateTarget("Hidden", "hidden_target");
            hidden.gameObject.SetActive(false);

            SceneTargetResolver resolver = SceneTargetResolver.FromLoadedScenes();

            Assert.That(resolver.TryGetTransform("hidden_target", out _), Is.True);
        }
    }
}
