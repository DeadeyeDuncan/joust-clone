namespace Joust.Core
{
    /// <summary>
    /// Conversion between the original game's logical pixel space and Unity
    /// world space.
    ///
    /// The arcade plays in 640x360 logical pixels with y increasing DOWNWARD and
    /// the lava surface at y=344. Unity is y-up. One world unit is 20 logical
    /// pixels, which makes the arena 32 units wide, a mount 2.0 x 1.6 units, and
    /// puts the lava surface at y=0.
    ///
    /// Everything positional in the game derives from these helpers, so the
    /// rebuild keeps the arcade's proportions rather than eyeballed ones.
    /// </summary>
    public static class ArenaMetrics
    {
        public const float PixelsPerUnit = 20f;
        public const float LogicalWidth = 640f;
        public const float LogicalHeight = 360f;
        public const float LogicalLavaY = 344f;

        /// <summary>Half the arena width in world units: 16.</summary>
        public static float ArenaHalfWidth => LogicalWidth / 2f / PixelsPerUnit;

        /// <summary>Logical pixel x to world x, with the arena centred on 0.</summary>
        public static float WorldX(float pixelX) => (pixelX - LogicalWidth / 2f) / PixelsPerUnit;

        /// <summary>
        /// Logical pixel y (down-positive) to world y (up-positive), with the
        /// lava surface at 0. A smaller pixel y is higher on screen and so maps
        /// to a greater world y.
        /// </summary>
        public static float WorldY(float pixelY) => (LogicalLavaY - pixelY) / PixelsPerUnit;

        /// <summary>Logical pixel length to world units.</summary>
        public static float Units(float pixels) => pixels / PixelsPerUnit;

        /// <summary>World x back to logical pixel x, for diagnostics and tools.</summary>
        public static float PixelX(float worldX) => worldX * PixelsPerUnit + LogicalWidth / 2f;
    }
}
