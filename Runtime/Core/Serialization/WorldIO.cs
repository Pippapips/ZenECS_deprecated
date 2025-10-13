namespace ZenECS.Core.Serialization
{
    public static class WorldIO
    {
        public static void Save(World world, ISnapshotBackend backend, IWorldSerializer serializer)
            => serializer.Save(world, backend);

        public static void Load(World world, ISnapshotBackend backend, IWorldSerializer serializer)
        {
            serializer.Load(world, backend);
            world.RunMigrations();
        } 
    }
}