using Joust.World;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// Wrap arithmetic, tested without physics. The arena spans
    /// [-halfWidth, +halfWidth]; crossing either edge reappears at the other.
    /// </summary>
    public class ScreenWrapTests
    {
        private const float Half = 16f;

        [Test]
        public void PositionInsideBoundsIsUnchanged()
        {
            Assert.AreEqual(4f, ScreenWrapPrototype.WrapX(4f, Half), 1e-4f);
            Assert.AreEqual(-4f, ScreenWrapPrototype.WrapX(-4f, Half), 1e-4f);
            Assert.AreEqual(0f, ScreenWrapPrototype.WrapX(0f, Half), 1e-4f);
        }

        [Test]
        public void CrossingTheRightEdgeReappearsOnTheLeft()
        {
            Assert.AreEqual(-15f, ScreenWrapPrototype.WrapX(17f, Half), 1e-4f);
        }

        [Test]
        public void CrossingTheLeftEdgeReappearsOnTheRight()
        {
            Assert.AreEqual(15f, ScreenWrapPrototype.WrapX(-17f, Half), 1e-4f);
        }

        [Test]
        public void WrapIsIdempotentForAlreadyWrappedPositions()
        {
            var once = ScreenWrapPrototype.WrapX(17f, Half);
            Assert.AreEqual(once, ScreenWrapPrototype.WrapX(once, Half), 1e-4f);
        }

        [Test]
        public void PositionManyWidthsAwayStillLandsInsideBounds()
        {
            var wrapped = ScreenWrapPrototype.WrapX(200f, Half);
            Assert.GreaterOrEqual(wrapped, -Half);
            Assert.Less(wrapped, Half);
        }

        [Test]
        public void ZeroWidthArenaDoesNotDivideByZero()
        {
            Assert.AreEqual(5f, ScreenWrapPrototype.WrapX(5f, 0f), 1e-4f);
        }
    }
}
