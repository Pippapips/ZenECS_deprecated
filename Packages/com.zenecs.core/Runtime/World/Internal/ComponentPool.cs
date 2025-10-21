#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// 컴포넌트 풀 공통 인터페이스.
    /// 스냅샷/툴링에서 사용하는 최소 표면적을 유지합니다.
    /// </summary>
    internal interface IComponentPool
    {
        /// <summary>엔티티 ID까지 접근 가능하도록 용량 확보</summary>
        void EnsureCapacity(int entityId);

        /// <summary>엔티티가 이 컴포넌트를 보유하고 있는가</summary>
        bool Has(int entityId);

        /// <summary>엔티티에서 이 컴포넌트를 제거</summary>
        void Remove(int entityId, bool dataClear = true);

        /// <summary>박싱된 값 반환(없으면 null)</summary>
        object? GetBoxed(int entityId);

        /// <summary>박싱된 값 설정(없으면 추가/있으면 갱신)</summary>
        void SetBoxed(int entityId, object value);

        /// <summary>풀 내 모든 (id, boxed) 열거(존재 항목만)</summary>
        IEnumerable<(int id, object boxed)> EnumerateAll();

        /// <summary>보유 중인 항목 개수</summary>
        int Count { get; }

        /// <summary>모든 항목/표시 비트 초기화(스냅샷 로드 전)</summary>
        void ClearAll();
    }

    /// <summary>
    /// 값형(struct) 컴포넌트 전용 풀.
    /// BitSet으로 존재 여부를 관리하고, 배열을 2배씩 성장시키며 id-인덱스 직접매핑을 합니다.
    /// </summary>
    internal sealed class ComponentPool<T> : IComponentPool where T : struct
    {
        private const int DefaultInitialCapacity = 256;

        private T[] _data;       // id → T
        private BitSet _present; // id 보유 여부
        private int _count;      // 현재 존재 항목 수

        public ComponentPool(int initialCapacity = DefaultInitialCapacity)
        {
            int cap = Math.Max(1, initialCapacity);
            _data = new T[cap];
            _present = new BitSet(cap);
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            // 1) 데이터 배열이 비어 있으면 기본 크기만 확보
            if (_data == null || _data.Length == 0)
                _data = new T[DefaultInitialCapacity];

            // 2) BitSet은 "교체"하지 말고, 없으면 만들고 있으면 "늘리기"만 한다
            if (_present == null)
                _present = new BitSet(Math.Max(1, _data.Length));
            else if (_present.Length < _data.Length)
                _present.EnsureCapacity(_data.Length); // ← 비트 유지
        }

        public int Count => _count;

        /// <summary>
        /// entityId까지 접근 가능하도록 내부 배열/비트셋 용량을 2배씩 확장.
        /// </summary>
        public void EnsureCapacity(int entityId)
        {
            EnsureInitialized();
            if (entityId < _data.Length) return;

            int cap = _data.Length == 0 ? 1 : _data.Length;
            while (cap <= entityId) cap <<= 1;

            Array.Resize(ref _data, cap);
            _present.EnsureCapacity(cap); // ← 비트 유지
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            if (entityId < 0) return false;
            if (_data == null || entityId >= _data.Length) return false;
            return _present != null && _present.Get(entityId);
        }

        /// <summary>
        /// 존재하지 않으면 생성(mark present) 후 ref 반환. (쓰기 경로용)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Ref(int entityId)
        {
            EnsureCapacity(entityId);
            if (!_present.Get(entityId))
            {
                _present.Set(entityId, true);
                _count++;
            }
            return ref _data[entityId];
        }

        /// <summary>
        /// 반드시 존재해야 ref를 반환(없으면 InvalidOperationException). (고성능 읽기/쓰기 경로)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T RefExisting(int entityId)
        {
            if (!Has(entityId))
                throw new InvalidOperationException($"Component '{typeof(T).Name}' not present on entity {entityId}.");
            return ref _data[entityId];
        }

        /// <summary>
        /// 값 복사 반환(존재 안하면 기본값).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int entityId)
            => Has(entityId) ? _data[entityId] : default;

        /// <summary>
        /// 존재 값을 out으로 획득.
        /// </summary>
        public bool TryGet(int entityId, out T value)
        {
            if (Has(entityId))
            {
                value = _data[entityId];
                return true;
            }
            value = default;
            return false;
        }

        public void Remove(int entityId, bool dataClear = true)
        {
            if (!Has(entityId)) return;
            _present.Set(entityId, false);
            _count--;
            if (dataClear)
                _data[entityId] = default;
        }

        public IEnumerable<(int id, object boxed)> EnumerateAll()
        {
            // 단순 for 루프 + 비트 체크(압도적 대부분의 케이스에서 충분히 빠름)
            var data = _data; // 로컬 캐시로 JIT 최적화 도움
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                if (_present.Get(i))
                    yield return (i, (object)data[i]); // 박싱 1회
            }
        }

        public object? GetBoxed(int entityId)
            => Has(entityId) ? (object)_data[entityId] : null;

        public void SetBoxed(int entityId, object value)
        {
            EnsureInitialized();
            // 박싱 타입 체크(안전성)
            if (value is not T v)
                throw new InvalidCastException(
                    $"SetBoxed type mismatch: value is '{value?.GetType().FullName ?? "null"}' " +
                    $"but pool expects '{typeof(T).FullName}'");

            ref var r = ref Ref(entityId); // 추가/갱신 공용
            r = v;
        }

        public void ClearAll()
        {
            EnsureInitialized();
            _present.ClearAll();
            _count = 0;
        }
    }
}
