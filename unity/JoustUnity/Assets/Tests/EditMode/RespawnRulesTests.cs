using System.Collections.Generic;
using Joust.Flow;
using NUnit.Framework;
using UnityEngine;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// Where a killed rider comes back. Respawning next to whatever just killed
    /// you is the cheapest way to make a life feel stolen rather than lost.
    /// </summary>
    public class RespawnRulesTests
    {
        private static readonly List<Vector2> Pads = new()
        {
            new Vector2(-12f, 6f),
            new Vector2(0f, 10f),
            new Vector2(12f, 6f),
        };

        [Test]
        public void ChoosesThePadFurthestFromTheThreat()
        {
            var pad = RespawnRules.ChooseSpawnPad(Pads, new Vector2(-12f, 6f));
            Assert.AreEqual(new Vector2(12f, 6f), pad);
        }

        [Test]
        public void ThreatOnTheRightSendsThePlayerLeft()
        {
            var pad = RespawnRules.ChooseSpawnPad(Pads, new Vector2(12f, 6f));
            Assert.AreEqual(new Vector2(-12f, 6f), pad);
        }

        [Test]
        public void WithNoThreatsAnyPadIsAcceptable()
        {
            var pad = RespawnRules.ChooseSpawnPad(Pads, null);
            Assert.Contains(pad, Pads);
        }

        [Test]
        public void ASinglePadIsAlwaysTheAnswer()
        {
            var only = new List<Vector2> { new(3f, 3f) };
            Assert.AreEqual(new Vector2(3f, 3f), RespawnRules.ChooseSpawnPad(only, new Vector2(3f, 3f)));
        }

        [Test]
        public void NoPadsYieldsTheArenaCentre()
        {
            Assert.AreEqual(Vector2.zero, RespawnRules.ChooseSpawnPad(new List<Vector2>(), null));
        }

        [Test]
        public void FallingBelowTheLavaSurfaceIsFatal()
        {
            Assert.IsTrue(RespawnRules.IsBelowLava(-0.2f));
            Assert.IsTrue(RespawnRules.IsBelowLava(-5f));
        }

        [Test]
        public void FlyingAboveTheLavaIsSurvivable()
        {
            Assert.IsFalse(RespawnRules.IsBelowLava(0.5f));
            Assert.IsFalse(RespawnRules.IsBelowLava(9f));
        }
    }
}
