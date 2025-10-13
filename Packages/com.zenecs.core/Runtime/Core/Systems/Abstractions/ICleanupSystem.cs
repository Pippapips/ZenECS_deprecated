namespace ZenECS.Core.Systems
{
    /// <summary>프레임 말 정리 단계(임시 버퍼/커맨드 정리 등)</summary>
    public interface ICleanupSystem : ISystem
    {
        void Cleanup(World w);
    }
}