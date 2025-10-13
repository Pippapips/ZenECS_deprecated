using System;
using System.Collections.Generic;

namespace ZenECS.Core.Sync.Util
{
    public sealed class Throttle
    {
        private readonly Dictionary<int, long> _last = new();
        private readonly long _min;
        public Throttle(TimeSpan min) => _min = min.Ticks;

        public bool ShouldRun(int key, long now)
        {
            if (_last.TryGetValue(key, out var last) && now - last < _min) return false;
            _last[key] = now;
            return true;
        }
    }
}