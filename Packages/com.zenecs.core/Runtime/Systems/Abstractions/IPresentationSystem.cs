namespace ZenECS.Core.Systems
{
    /// <summary>Update 이후 후처리(예: Data→View 동기화)</summary>
    public interface IPresentationSystem : ISystem
    {
        // 보간을 쓰는 시스템은 alpha 활용, 아니면 무시
        void Run(World w, float alpha);

        // 기본 구현: ISystem.Run은 alpha=1f로 위임 → 구현체는 추가 메서드 필요 없음
        void ISystem.Run(World w) => Run(w, 1f);
    }
}
