using System;
namespace MyDay.Windows.Core
{
    // Explicit xorshift32 sequence so daily layouts can be reproduced on another OS.
    public sealed class MiniGameRandom
    {
        private uint state;
        public MiniGameRandom(int seed){state=unchecked((uint)seed);if(state==0)state=0x6d2b79f5;}
        public double NextDouble(){state^=state<<13;state^=state>>17;state^=state<<5;return state/4294967296.0;}
        public int Next(int maximum){return (int)(NextDouble()*maximum);}
        public static int DailySeed(DateTime day){return unchecked((day.Year*10000+day.Month*100+day.Day)*397^0x53414e47);}
    }
}
