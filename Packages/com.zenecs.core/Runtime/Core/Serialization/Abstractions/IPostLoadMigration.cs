using ZenECS.Core.Serialization;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// 월드 로드 직후에 실행되는 마이그레이션 스텝.
    /// - Priority: 낮은 숫자 → 먼저 실행
    /// - Run: 월드 내 레거시 컴포넌트를 최신으로 변환하고 레거시 제거
    /// </summary>
    public interface IPostLoadMigration
    {
        int Priority { get; }
        void Run(World world);
    }
}