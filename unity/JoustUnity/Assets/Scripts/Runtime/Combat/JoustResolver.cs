using System;

namespace Joust.Combat
{
    public enum JoustOutcome
    {
        AWins,
        BWins,
        Tie
    }

    /// <summary>
    /// Decides a lance duel purely from lance heights. Unity is Y-up, so the
    /// greater Y is the higher lance and wins. Collider geometry never decides
    /// the winner; this comparison does.
    /// </summary>
    public static class JoustResolver
    {
        public static JoustOutcome Resolve(float aLanceY, float bLanceY, float tieBand)
        {
            if (Math.Abs(aLanceY - bLanceY) <= tieBand)
            {
                return JoustOutcome.Tie;
            }

            return aLanceY > bLanceY ? JoustOutcome.AWins : JoustOutcome.BWins;
        }
    }
}
