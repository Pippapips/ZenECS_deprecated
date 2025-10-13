namespace ZenECS.Core.Systems
{
    /// <summary>Update 이후 후처리(예: Data→View 동기화)</summary>
    public interface ILateRunSystem : ISystem
    {
        void LateRun(World w);
    }
}