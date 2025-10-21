#nullable enable
using System.Collections.Concurrent;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        // Custom Job Schedule
        // world.Schedule(new HealJob(ents, amount: 3));
        //
        // struct HealJob : World.IJob
        // {
        //     private readonly Entity[] ents; private readonly int amount;
        //     public HealJob(Entity[] ents, int amount){ this.ents = ents; this.amount = amount; }
        //     public void Execute(World w)
        //     {
        //         for (int i=0;i<ents.Length;i++)
        //             if (w.Has<Health>(ents[i]))
        //             {
        //                 ref var h = ref w.RefExisting<Health>(ents[i]);
        //                 h.Value += amount;
        //             }
        //     }
        // }

        public interface IJob
        {
            void Execute(World w);
        }

        private readonly ConcurrentQueue<IJob> jobQueue = new();

        private void Schedule(IJob? job)
        {
            if (job != null)
            {
                jobQueue.Enqueue(job);
            }
        }

        public int RunScheduledJobs()
        {
            int n = 0;
            while (jobQueue.TryDequeue(out var j))
            {
                j.Execute(this);
                n++;
            }

            return n;
        }

        private void ClearAllScheduledJobs()
        {
            jobQueue.Clear();
        }
    }
}
