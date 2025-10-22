// Core or Tests: InlineMainThreadGate.cs
#nullable enable
using System;

namespace ZenECS.Core.Binding.Util
{
    /// 콘솔/테스트용: Post/Send를 즉시 같은 스레드에서 실행
    public sealed class InlineMainThreadGate : IMainThreadGate
    {
        public bool IsMainThread => true;
        public void Ensure() { /* always main in this model */ }
        public void Post(Action action) { action?.Invoke(); }
        public void Send(Action action) { action?.Invoke(); }
    }
}