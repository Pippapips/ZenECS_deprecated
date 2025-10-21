using System.Collections.Concurrent;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        public int FrameCount { get; set; }
        public float DeltaTime { get; set; }

        private void ResetTimingCounters()
        {
            FrameCount = 0;
            DeltaTime = 0;
        }
    }
}
