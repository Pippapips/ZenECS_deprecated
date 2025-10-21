using ZenECS.Core.Serialization;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// 스냅샷을 모두 로드(풀들까지 채움)한 뒤, 월드 전역에서 실행되는 후처리 마이그레이션.
    /// - 순서 번호(Order)로 실행 순서를 제어한다(낮은 → 높은).
    /// - 반드시 idempotent 하게 작성(여러 번 실행돼도 결과 동일).
    /// </summary>
    public interface IPostLoadMigration
    {
        int Order { get; }     // 실행 순서(낮은 값 먼저)
        void Run(World world); // 월드 전역에서 필요한 보정/리바인딩/인덱스 재구축 등
    }
}
