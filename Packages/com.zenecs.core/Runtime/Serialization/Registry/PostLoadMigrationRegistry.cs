using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenECS.Core.Serialization
{
    public static class PostLoadMigrationRegistry
    {
        private static readonly List<IPostLoadMigration> _migs = new();
        // NEW: 등록된 마이그레이션 타입 집합
        private static readonly HashSet<Type> _migTypes = new();

        /// <summary>
        /// 마이그레이션을 레지스트리에 등록합니다.
        /// 이미 동일한 타입이 등록되어 있으면 무시하고 false를 반환합니다.
        /// </summary>
        public static bool Register(IPostLoadMigration mig)
        {
            if (mig == null) return false;
            var t = mig.GetType();
            if (_migTypes.Contains(t)) return false; // 이미 등록된 타입이면 skip

            _migTypes.Add(t);
            _migs.Add(mig);
            return true;
        }

        /// <summary>
        /// 등록 여부 확인.
        /// </summary>
        public static bool IsRegistered<T>() where T : IPostLoadMigration
            => _migTypes.Contains(typeof(T));

        /// <summary>
        /// 미등록 시에만 팩토리로 생성해 등록(중복 방지 보장).
        /// </summary>
        public static bool EnsureRegistered<T>(Func<T> factory) where T : class, IPostLoadMigration
        {
            if (IsRegistered<T>()) return false;
            var instance = factory();
            return Register(instance);
        }

        /// <summary>
        /// Order → TypeName 기준으로 안정 정렬 후 순차 실행(idempotent 가정).
        /// </summary>
        public static void RunAll(World world)
        {
            if (_migs.Count == 0) return;

            foreach (var m in _migs
                         .OrderBy(m => m.Order)
                         .ThenBy(m => m.GetType().FullName, StringComparer.Ordinal))
            {
                m.Run(world);
            }
        }

        /// <summary>
        /// 테스트/리셋용 전체 초기화.
        /// </summary>
        public static void Clear()
        {
            _migs.Clear();
            _migTypes.Clear();
        }
    }
}
