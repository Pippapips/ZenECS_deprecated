namespace ZenECS.Core.Serialization
{
    public interface IWorldSerializer
    {
        void Save(World world, ISnapshotBackend backend);
        void Load(World world, ISnapshotBackend backend);
    }
}