namespace ZenECS.Core.Systems
{
    /// <summary>매 프레임(Update 등가) 실행되는 시스템.
    /// 데이터 접근은 반드시 World 경유(World-Gate)로만 수행.</summary>
    public interface IRunSystem : ISystem
    {
        void Run(World w);
    }
}