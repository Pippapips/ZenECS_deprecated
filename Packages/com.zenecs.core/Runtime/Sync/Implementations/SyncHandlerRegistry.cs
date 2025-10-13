#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Sync
{
    /// <summary>
    /// 기본 구현:
    /// - 스레드: 메인 스레드 사용을 전제로 한 간단한 Dictionary 기반
    /// - 필요 시 ConcurrentDictionary로 바꾸거나, 락을 추가해 다중 스레드 대응 가능
    /// </summary>
    public sealed class SyncHandlerRegistry : ISyncHandlerRegistry
    {
        // Type -> factory (Resolve 시마다 새 인스턴스 생성)
        private readonly Dictionary<Type, Func<ISyncHandler>> _factories = new();

        // Type -> singleton (항상 같은 인스턴스 반환)
        private readonly Dictionary<Type, ISyncHandler> _singletons = new();

        private bool _disposed;

        public SyncHandlerRegistry()
        {
            Infrastructure.EcsRuntimeDirectory.AttachSyncHandlerRegistry(this);
        }
        
        /// <inheritdoc />
        public void RegisterFactory<T>(Func<ISyncHandler> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            ThrowIfDisposed();

            _factories[typeof(T)] = () => factory();
            // 팩토리 등록 시 기존 싱글턴이 있었다면 교체 정책에 따라 제거할 수도 있음(여기선 그대로 둠)
        }

        /// <inheritdoc />
        public void RegisterSingleton<T>(ISyncHandler instance)
        {
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            ThrowIfDisposed();

            _singletons[typeof(T)] = instance;
            // 싱글턴 등록 시 기존 팩토리가 있어도 우선순위는 싱글턴이 먼저입니다(Resolve 구현 참고).
        }

        /// <inheritdoc />
        public ISyncHandler? Resolve(Type componentType)
        {
            if (componentType is null) throw new ArgumentNullException(nameof(componentType));
            ThrowIfDisposed();

            // 1) 싱글턴 우선
            if (_singletons.TryGetValue(componentType, out var inst))
                return inst;

            // 2) 팩토리로 생성
            if (_factories.TryGetValue(componentType, out var f))
                return f();

            // 3) 미등록 → null
            return null;
        }

        /// <inheritdoc />
        public ISyncHandler? Resolve<T>()
            => Resolve(typeof(T));

        /// <inheritdoc />
        public bool Unregister(Type componentType)
        {
            ThrowIfDisposed();
            var removed = false;

            if (_singletons.Remove(componentType, out var singleton))
            {
                (singleton as IDisposable)?.Dispose();
                removed = true;
            }

            removed |= _factories.Remove(componentType);
            return removed;
        }

        /// <inheritdoc />
        public void Clear()
        {
            ThrowIfDisposed();

            // 싱글턴 Dispose
            foreach (var s in _singletons.Values)
                (s as IDisposable)?.Dispose();

            _singletons.Clear();
            _factories.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SyncHandlerRegistry));
        }
    }
}
