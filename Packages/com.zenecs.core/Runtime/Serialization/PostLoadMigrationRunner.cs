using System.Collections;
using System.Collections.Generic;

namespace ZenECS.Core.Serialization
{
    internal sealed class PostLoadMigrationRunner
    {
        private readonly World _world;
        private readonly IEnumerable<IPostLoadMigration> _migrations;

        public PostLoadMigrationRunner(World world, IEnumerable<IPostLoadMigration> migrations)
        {
            _world = world;
            _migrations = migrations;
        }

        public void Run()
        {
            foreach (var migration in _migrations)
            {
                migration.Run(_world);
            }
        }
    }
}