#nullable enable
using System.Collections.Concurrent;
using ZenECS.Core.Extensions;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        // 멀티스레드 커맨드 버퍼 + 스케줄
        // var cb = world.BeginWrite();
        // cb.Add(e, new Damage { Amount = 10 });
        // cb.Remove<Stunned>(e);
        // world.Schedule(cb);     // 프레임 경계에서 world.RunScheduledJobs() 시점에 적용
        // 또는 world.EndWrite(cb); 로 즉시 적용

        /// <summary>Write scope apply policy.</summary>
        public enum ApplyMode
        {
            /// <summary>Dispose 시 스케줄러에 넣고 배리어에서 일괄 적용.</summary>
            Schedule = 0,

            /// <summary>Dispose 시 즉시 적용(메인 스레드 권장).</summary>
            Immediate = 1,
        }

        /// <summary>
        /// 멀티스레드 안전 커맨드 버퍼.
        /// EndWrite(cb) 또는 Schedule(cb)로 적용 가능.
        /// using (BeginWrite(...)) 로 자동 적용 가능.
        /// </summary>
        public sealed class CommandBuffer : IJob, System.IDisposable
        {
            internal readonly ConcurrentQueue<IOp> q = new();

            // BeginWrite에서 바인딩된다
            private World? _boundWorld;
            private ApplyMode _mode;
            private bool _disposed;

            internal void Bind(World w, ApplyMode mode)
            {
                _boundWorld = w;
                _mode = mode;
                _disposed = false;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                // using(BeginWrite)로 생성된 버퍼만 자동 적용
                var w = _boundWorld;
                _boundWorld = null;

                if (w == null) return;

                if (_mode == ApplyMode.Immediate)
                    w.EndWrite(this); // 즉시 적용
                else
                    w.Schedule(this); // 배리어에서 적용
            }

            internal interface IOp
            {
                void Apply(World w);
            } // 죽은 엔티티 방어는 각 Op 내부에서 수행

            sealed class AddOp<T> : IOp where T : struct
            {
                readonly Entity e;
                readonly T v;
                public AddOp(Entity e, in T v)
                {
                    this.e = e;
                    this.v = v;
                }
                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Add<{typeof(T).Name}>: {e} dead"); */
                        return;
                    }
                    w.AddComponentInternal(e, in v);
                    Events.ComponentEvents.RaiseAdded(w, e, typeof(T));
                }
            }

            sealed class ReplaceOp<T> : IOp where T : struct
            {
                readonly Entity e;
                readonly T v;
                public ReplaceOp(Entity e, in T v)
                {
                    this.e = e;
                    this.v = v;
                }
                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Replace<{typeof(T).Name}>: {e} dead"); */
                        return;
                    }
                    // World.Replace 경로로 훅/검증/이벤트 일치
                    w.Replace(e, in v);
                    Events.ComponentEvents.RaiseChanged(w, e, typeof(T));
                }
            }

            sealed class RemoveOp<T> : IOp where T : struct
            {
                readonly Entity e;
                public RemoveOp(Entity e) { this.e = e; }
                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Remove<{typeof(T).Name}>: {e} dead"); */
                        return;
                    }
                    if (w.RemoveComponentInternal<T>(e))
                        Events.ComponentEvents.RaiseRemoved(w, e, typeof(T));
                }
            }

            sealed class DestroyOp : IOp
            {
                readonly Entity e;
                public DestroyOp(Entity e) { this.e = e; }

                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Destroy: {e} already dead"); */
                        return;
                    }
                    w.DestroyEntity(e);
                }
            }

            // enqueue
            public void Add<T>(Entity e, in T v) where T : struct => q.Enqueue(new AddOp<T>(e, v));
            public void Replace<T>(Entity e, in T v) where T : struct => q.Enqueue(new ReplaceOp<T>(e, v));
            public void Remove<T>(Entity e) where T : struct => q.Enqueue(new RemoveOp<T>(e));
            public void Destroy(Entity e) => q.Enqueue(new DestroyOp(e));

            // IJob: 스케줄러와 통합
            void World.IJob.Execute(World w)
            {
                while (q.TryDequeue(out var op)) op.Apply(w);
            }
        }

        /// <summary>
        /// 커맨드 버퍼 쓰기 시작. using 패턴 지원:
        /// using (var cb = world.BeginWrite()) { ... } // Dispose 시 Schedule
        /// using (var cb = world.BeginWrite(ApplyMode.Immediate)) { ... } // Dispose 시 즉시 적용
        /// </summary>
        public CommandBuffer BeginWrite(ApplyMode mode = ApplyMode.Schedule)
        {
            var cb = new CommandBuffer();
            cb.Bind(this, mode);
            return cb;
        }

        /// <summary>다른 스레드에서 쌓인 명령을 즉시 적용(메인 스레드 호출 권장).</summary>
        public int EndWrite(CommandBuffer cb)
        {
            if (cb == null) return 0;
            int n = 0;
            while (cb.q.TryDequeue(out var op))
            {
                op.Apply(this);
                n++;
            }
            return n;
        }

        /// <summary>커맨드 버퍼를 스케줄러에 넣고 프레임 경계에서 적용.</summary>
        public void Schedule(CommandBuffer? cb)
        {
            if (cb != null)
            {
                Schedule((IJob)cb);
            }
        }

        // 예: 프레임-로컬/지연 커맨드 버퍼가 있다면 모두 초기화
        private void ClearAllCommandBuffers()
        {
            ClearAllScheduledJobs();
        }

        // 선택: Reset 시점에 커맨드 플러시/드롭 정책을 달리하고 싶으면 여기서 처리
        partial void OnBeforeWorldReset(bool keepCapacity)
        {
            if (!keepCapacity) RunScheduledJobs();
        }
    }
}
