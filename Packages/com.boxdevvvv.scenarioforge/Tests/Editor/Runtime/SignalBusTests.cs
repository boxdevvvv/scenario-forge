using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ScenarioForge.Tests
{
    public sealed class SignalBusTests
    {
        [Test]
        public void Raise_IncrementsCountAndRaisesEvent()
        {
            var bus = new SignalBus();
            var raised = new List<string>();
            bus.Raised += raised.Add;

            bus.Raise("door_opened");
            bus.Raise("door_opened");

            Assert.That(bus.GetCount("door_opened"), Is.EqualTo(2));
            Assert.That(bus.WasRaised("door_opened"), Is.True);
            Assert.That(raised, Is.EqualTo(new[] { "door_opened", "door_opened" }));
        }

        [Test]
        public void Raise_TrimsWhitespace()
        {
            var bus = new SignalBus();
            string received = null;
            bus.Raised += name => received = name;

            bus.Raise("  door_opened \t");

            Assert.That(received, Is.EqualTo("door_opened"));
            Assert.That(bus.GetCount("door_opened"), Is.EqualTo(1));
            Assert.That(bus.GetCount(" door_opened "), Is.EqualTo(1));
        }

        [Test]
        public void Names_AreCaseSensitive()
        {
            var bus = new SignalBus();

            bus.Raise("Door");

            Assert.That(bus.WasRaised("door"), Is.False);
        }

        [Test]
        public void Raise_WithEmptyName_Throws()
        {
            var bus = new SignalBus();

            Assert.Throws<ArgumentException>(() => bus.Raise(null));
            Assert.Throws<ArgumentException>(() => bus.Raise("   "));
        }

        [Test]
        public void ResetCounts_ClearsEveryCount()
        {
            var bus = new SignalBus();
            bus.Raise("a");
            bus.Raise("b");

            bus.ResetCounts();

            Assert.That(bus.GetCount("a"), Is.Zero);
            Assert.That(bus.WasRaised("b"), Is.False);
        }

        [Test]
        public void GetCount_ForUnknownOrEmptySignal_IsZero()
        {
            var bus = new SignalBus();

            Assert.That(bus.GetCount("never"), Is.Zero);
            Assert.That(bus.GetCount(null), Is.Zero);
        }
    }
}
