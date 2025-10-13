namespace ZenECS.Core.Systems
{
    /// <summary>월드/게임 시작 시 1회 실행</summary>
    public interface IInitSystem : ISystem
    {
        void Init(World w);
    }
}