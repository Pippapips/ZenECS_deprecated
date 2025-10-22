#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ZenECS.Core.Binding.Util
{
    public sealed class DefaultMainThreadGate : IMainThreadGate
    {
        private readonly int _mainThreadId;
        private readonly SynchronizationContext _ctx;
        private readonly ConcurrentQueue<Action> _queue = new();
        private readonly bool _inline;

        public DefaultMainThreadGate(bool inlineWhenNoContext = true)
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _ctx = SynchronizationContext.Current;
            _inline = inlineWhenNoContext && _ctx is null; // 콘솔: 컨텍스트 없으면 인라인로 동작
            if (_ctx is null) _ctx = new SynchronizationContext();
        }

        public bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        public void Ensure()
        {
            if (!IsMainThread) throw new InvalidOperationException("Must be called on main thread.");
        }
        public void Post(Action action)
        {
            if (action == null) return;
            if (_inline) { action(); return; }
            _queue.Enqueue(action);
            _ctx.Post(_ => PumpOnce(), null);
        }
        public void Send(Action action)
        {
            if (action == null) return;
            if (IsMainThread)
            {
                action();
                return;
            }
            if (_inline || IsMainThread) { action(); return; }
            using var done = new ManualResetEventSlim(false);
            Exception? ex = null;
            _queue.Enqueue(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    done.Set();
                }
            });
            _ctx.Post(_ => PumpOnce(), null);
            done.Wait();
            if (ex != null) throw ex;
        }
        private void PumpOnce()
        {
            while (_queue.TryDequeue(out var a))
            {
                try
                {
                    a();
                }
                catch { }
            }
        }
    }
}