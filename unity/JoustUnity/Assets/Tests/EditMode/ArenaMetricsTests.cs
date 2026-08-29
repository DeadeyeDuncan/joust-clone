using Joust.Core;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// The original game's logical space is 640x360 pixels, y down, with the
    /// lava surface at y=344. These tests pin the mapping into Unity's world
    /// space: y up, lava at 0, 20 pixels to the unit.
    /// </summary>
    public class ArenaMetricsTests
    {
        [Test]
        public void ArenaIsThirtyTwoUnitsWide()
        {
            Assert.AreEqual(16f, ArenaMetrics.ArenaHalfWidth, 1e-4f);
        }

        [Test]
        public void LeftEdgePixelMapsToLeftEdgeUnit()
        {
            Assert.AreEqual(-16f, ArenaMetrics.WorldX(0f), 1e-4f);
        }

        [Test]
        public void RightEdgePixelMapsToRightEdgeUnit()
        {
            Assert.AreEqual(16f, ArenaMetrics.WorldX(640f), 1e-4f);
        }

        [Test]
        public void CentrePixelMapsToOrigin()
        {
            Assert.AreEqual(0f, ArenaMetrics.WorldX(320f), 1e-4f);
        }

        [Test]
        public void LavaLineMapsToZero()
        {
            Assert.AreEqual(0f, ArenaMetrics.WorldY(344f), 1e-4f);
        }

        [Test]
        public void HigherOnScreenIsGreaterWorldY()
        {
            Assert.Greater(ArenaMetrics.WorldY(64f), ArenaMetrics.WorldY(300f));
        }

        [Test]
        public void MountFootprintIsTwoByOnePointSix()
        {
            Assert.AreEqual(2.0f, ArenaMetrics.Units(40f), 1e-4f);
            Assert.AreEqual(1.6f, ArenaMetrics.Units(32f), 1e-4f);
        }

        [Test]
        public void TuningProfileShipsFlyableDefaults()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<TuningProfile>();

            Assert.Greater(profile.gravity, 0f);
            Assert.Greater(profile.flapImpulse, 0f);
            Assert.Greater(profile.maxHorizontalSpeed, 0f);
            Assert.Greater(profile.tieBandUnits, 0f);

            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}
