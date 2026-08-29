using Joust.Combat;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// OnTriggerEnter fires on both colliders of a pair, so exactly one of the
    /// two participants must own the resolution. These tests pin the ownership
    /// rule itself, independent of any physics.
    /// </summary>
    public class JoustOwnershipTests
    {
        [Test]
        public void OnlyTheLowerInstanceIdResolves()
        {
            Assert.IsTrue(JoustContact.ShouldResolve(10, 20));
            Assert.IsFalse(JoustContact.ShouldResolve(20, 10));
        }

        [Test]
        public void SelfContactNeverResolves()
        {
            Assert.IsFalse(JoustContact.ShouldResolve(10, 10));
        }

        [Test]
        public void NegativeInstanceIdsStillOrderConsistently()
        {
            Assert.IsTrue(JoustContact.ShouldResolve(-40, -10));
            Assert.IsFalse(JoustContact.ShouldResolve(-10, -40));
        }
    }
}
