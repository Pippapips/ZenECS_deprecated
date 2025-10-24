using System;
using ZenECS.Core;
using ZenECS.Core.Extensions;
using ZenECS.Core.Infrastructure;

namespace ZenEcsCoreSamples.WorldHooks
{
    // 단순 콘솔 테스트용 샘플 (Unity에서도 동일 동작)
    public static class WorldHooksExample
    {
        class EcsLogger : EcsRuntimeOptions.ILogger
        {
            public void Info(string msg)
            {
                Console.WriteLine($"EcsLogger: {msg}");
            }
            
            public void Warn(string msg)
            {
                Console.WriteLine($"EcsLogger Warning: {msg}");
            }
            
            public void Error(string msg)
            {
                Console.WriteLine($"EcsLogger Error: {msg}");
            }
        }
        
        public static void Main()
        {
            var world = new World();

            // 로깅 정책 설정
            var ecsLogger = new EcsLogger();
            EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;
            EcsRuntimeOptions.Log = ecsLogger;

            // ---- Hook 등록 ----

            // 1️⃣ 짝수 ID 엔티티만 쓰기 허용
            var writePerm = new Func<World, Entity, Type, bool>((w, e, t) => (e.Id & 1) == 0);
            world.AddWritePermission(writePerm);

            // 2️⃣ Mana 값이 0 이상이어야 한다
            Func<Mana, bool> manaCheck = m => m.Value >= 0;
            world.AddValidator(manaCheck);

            // ---- 테스트 ----
            var e1 = world.CreateEntity(); // id=1
            var e2 = world.CreateEntity(); // id=2

            world.Add(e1, new Mana(10));  // 거부 (홀수 ID)
            world.Add(e2, new Mana(-10)); // 검증 실패
            world.Add(e2, new Mana(5));   // 정상

            // ---- 읽기 권한 테스트 ----
            world.AddReadPermission((w, e, t) => t == typeof(Mana)); // Mana만 읽기 허용

            if (world.TryRead<Mana>(e2, out var mana))
                Console.WriteLine($"Mana: {mana.Value}");
            else
                Console.WriteLine("Mana 읽기 거부됨");

            // ---- 훅 제거 ----
            world.RemoveWritePermission(writePerm);
            world.RemoveValidator(manaCheck);
            world.ClearReadPermissions();

            Console.WriteLine("모든 훅 제거 완료.");
        }
    }

    // 샘플용 컴포넌트
    public readonly struct Mana
    {
        public readonly int Value;
        public Mana(int v) => Value = v;
        public override string ToString() => Value.ToString();
    }
}