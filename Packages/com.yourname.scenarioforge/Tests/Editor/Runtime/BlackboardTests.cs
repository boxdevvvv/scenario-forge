using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ScenarioForge.Tests
{
    public sealed class BlackboardTests
    {
        [Test]
        public void SetAndTryGet_RoundTripsEverySupportedType()
        {
            var blackboard = new Blackboard();
            blackboard.Set("bool", true);
            blackboard.Set("int", 42);
            blackboard.Set("float", 1.5f);
            blackboard.Set("string", "hello");

            Assert.That(blackboard.TryGet("bool", out bool b) && b, Is.True);
            Assert.That(blackboard.TryGet("int", out int i) && i == 42, Is.True);
            Assert.That(blackboard.TryGet("float", out float f) && Math.Abs(f - 1.5f) < 1e-6f, Is.True);
            Assert.That(blackboard.TryGet("string", out string s) && s == "hello", Is.True);
            Assert.That(blackboard.Count, Is.EqualTo(4));
        }

        [Test]
        public void Set_WithUnsupportedType_ThrowsArgumentException()
        {
            var blackboard = new Blackboard();

            var exception = Assert.Throws<ArgumentException>(() => blackboard.Set("value", 1.0));

            Assert.That(exception.Message, Does.Contain("Double"));
            Assert.That(exception.Message, Does.Contain("bool, int, float or string"));
        }

        [Test]
        public void Set_WithEmptyKey_ThrowsArgumentException()
        {
            var blackboard = new Blackboard();

            Assert.Throws<ArgumentException>(() => blackboard.Set("", 1));
            Assert.Throws<ArgumentException>(() => blackboard.Set("  ", 1));
            Assert.Throws<ArgumentException>(() => blackboard.Set<int>(null, 1));
        }

        [Test]
        public void TryGet_WithWrongType_ReturnsFalseWithoutThrowing()
        {
            var blackboard = new Blackboard();
            blackboard.Set("count", 3);

            Assert.That(blackboard.TryGet("count", out float asFloat), Is.False);
            Assert.That(asFloat, Is.Zero);
            Assert.That(blackboard.TryGet("count", out string asString), Is.False);
            Assert.That(blackboard.TryGet("count", out double _), Is.False);
        }

        [Test]
        public void TryGet_WithMissingOrNullKey_ReturnsFalse()
        {
            var blackboard = new Blackboard();

            Assert.That(blackboard.TryGet("missing", out int _), Is.False);
            Assert.That(blackboard.TryGet(null, out int _), Is.False);
        }

        [Test]
        public void HasRemoveAndClear_WorkAsExpected()
        {
            var blackboard = new Blackboard();
            blackboard.Set("a", 1);
            blackboard.Set("b", 2);

            Assert.That(blackboard.Has("a"), Is.True);
            blackboard.Remove("a");
            Assert.That(blackboard.Has("a"), Is.False);

            blackboard.Clear();
            Assert.That(blackboard.Count, Is.Zero);
            Assert.That(blackboard.Has(null), Is.False);
        }

        [Test]
        public void ValueChanged_IsRaisedOnAddChangeAndRemove_ButNotForTheSameValue()
        {
            var blackboard = new Blackboard();
            var changes = new List<string>();
            blackboard.ValueChanged += changes.Add;

            blackboard.Set("door", false);
            blackboard.Set("door", false);
            blackboard.Set("door", true);
            blackboard.Set("door", 1);
            blackboard.Remove("door");
            blackboard.Remove("door");

            Assert.That(changes, Is.EqualTo(new[] { "door", "door", "door", "door" }));
        }

        [Test]
        public void Clear_RaisesValueChangedForEveryKey()
        {
            var blackboard = new Blackboard();
            blackboard.Set("a", 1);
            blackboard.Set("b", 2);
            var changes = new List<string>();
            blackboard.ValueChanged += changes.Add;

            blackboard.Clear();

            Assert.That(changes, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void SetValue_AndTryGetValue_UseTypedValues()
        {
            var blackboard = new Blackboard();
            blackboard.SetValue("speed", BlackboardValue.FromFloat(2f));

            Assert.That(blackboard.TryGetValue("speed", out BlackboardValue value), Is.True);
            Assert.That(value.Type, Is.EqualTo(BlackboardValueType.Float));
            Assert.That(value.FloatValue, Is.EqualTo(2f));
        }
    }

    public sealed class BlackboardValueTests
    {
        [TestCase(CompareOperator.Equal, 5, 5, true)]
        [TestCase(CompareOperator.Equal, 5, 6, false)]
        [TestCase(CompareOperator.NotEqual, 5, 6, true)]
        [TestCase(CompareOperator.Greater, 6, 5, true)]
        [TestCase(CompareOperator.Greater, 5, 5, false)]
        [TestCase(CompareOperator.GreaterOrEqual, 5, 5, true)]
        [TestCase(CompareOperator.Less, 4, 5, true)]
        [TestCase(CompareOperator.LessOrEqual, 6, 5, false)]
        public void TryCompare_Ints(CompareOperator op, int left, int right, bool expected)
        {
            Assert.That(BlackboardValue.FromInt(left).TryCompare(op, BlackboardValue.FromInt(right), out bool result), Is.True);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void TryCompare_FloatEquality_IsApproximate()
        {
            BlackboardValue.FromFloat(0.1f + 0.2f).TryCompare(CompareOperator.Equal, BlackboardValue.FromFloat(0.3f), out bool result);

            Assert.That(result, Is.True);
        }

        [Test]
        public void TryCompare_WithDifferentTypes_ReturnsFalse()
        {
            Assert.That(BlackboardValue.FromInt(1).TryCompare(CompareOperator.Equal, BlackboardValue.FromFloat(1f), out _), Is.False);
        }

        [Test]
        public void TryCompare_OrderingOnStrings_IsNotSupported()
        {
            Assert.That(BlackboardValue.IsOperatorSupported(BlackboardValueType.String, CompareOperator.Greater), Is.False);
            Assert.That(BlackboardValue.IsOperatorSupported(BlackboardValueType.Bool, CompareOperator.Less), Is.False);
            Assert.That(BlackboardValue.IsOperatorSupported(BlackboardValueType.String, CompareOperator.Equal), Is.True);
            Assert.That(BlackboardValue.FromString("a").TryCompare(CompareOperator.Greater, BlackboardValue.FromString("b"), out _), Is.False);
        }

        [Test]
        public void TryCompare_StringsAndBools()
        {
            BlackboardValue.FromString("ok").TryCompare(CompareOperator.Equal, BlackboardValue.FromString("ok"), out bool sameString);
            BlackboardValue.FromBool(true).TryCompare(CompareOperator.NotEqual, BlackboardValue.FromBool(false), out bool differentBool);

            Assert.That(sameString, Is.True);
            Assert.That(differentBool, Is.True);
        }

        [Test]
        public void ToString_FormatsWithInvariantCulture()
        {
            Assert.That(BlackboardValue.FromFloat(1.25f).ToString(), Is.EqualTo("1.25"));
            Assert.That(BlackboardValue.FromBool(true).ToString(), Is.EqualTo("true"));
            Assert.That(BlackboardValue.FromString(null).ToString(), Is.EqualTo(string.Empty));
        }
    }
}
