using System.Collections.Concurrent;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>
        /// 멀티스레드 안전 커맨드 버퍼.
        /// EndWrite(cb) 또는 Schedule(cb)로 적용 가능.
        /// </summary>
        public sealed class CommandBuffer : IJob
        {
            internal readonly ConcurrentQueue<IOp> q = new();

            internal interface IOp
            {
                void Apply(World w);
            }

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
                    w.AddInternal(e, in v);
                    Events.ComponentEvents.RaiseAdded(w, e, typeof(T));
                }
            }

            sealed class RemoveOp<T> : IOp where T : struct
            {
                readonly Entity e;

                public RemoveOp(Entity e)
                {
                    this.e = e;
                }

                public void Apply(World w)
                {
                    if (w.RemoveInternal<T>(e))
                    {
                        Events.ComponentEvents.RaiseRemoved(w, e, typeof(T));
                    }
                }
            }

            sealed class DestroyOp : IOp
            {
                readonly Entity e;

                public DestroyOp(Entity e)
                {
                    this.e = e;
                }

                public void Apply(World w) => w.DestroyEntity(e);
            }

            public void Add<T>(Entity e, in T v) where T : struct => q.Enqueue(new AddOp<T>(e, v));
            public void Remove<T>(Entity e) where T : struct => q.Enqueue(new RemoveOp<T>(e));
            public void Destroy(Entity e) => q.Enqueue(new DestroyOp(e));

            // IJob: 스케줄러와 통합
            void World.IJob.Execute(World w)
            {
                while (q.TryDequeue(out var op)) op.Apply(w);
            }
        }

        public CommandBuffer BeginWrite() => new CommandBuffer();

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
        public void Schedule(CommandBuffer cb)
        {
            if (cb != null) Schedule((IJob)cb);
        }
    }
}