namespace ZenECS.Core.Systems
{
    /// <summary>월드 종료/씬 언로드 시 정리</summary>
    public interface IDisposeSystem : ISystem
    {
        void Dispose(World w);
    }
}