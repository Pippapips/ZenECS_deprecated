#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// 기본 구현:
    /// - 스레드: 메인 스레드 사용을 전제로 한 간단한 Dictionary 기반
    /// - 필요 시 ConcurrentDictionary로 바꾸거나, 락을 추가해 다중 스레드 대응 가능
    /// </summary>
    public sealed class ComponentBinderRegistry : IComponentBinderRegistry, IComponentBinderResolver
    {
        private readonly HashSet<System.Type> _registeredTypes = new();

        // Type -> factory (Resolve 시마다 새 인스턴스 생성)
        private readonly Dictionary<Type, Func<IComponentBinder>> _factories = new();

        // Type -> singleton (항상 같은 인스턴스 반환)
        private readonly Dictionary<Type, IComponentBinder> _singletons = new();

        private bool _disposed;

        public ComponentBinderRegistry()
        {
            Infrastructure.EcsRuntimeDirectory.AttachComponentBinderRegistry(this);
        }

        public void RegisterFactory<T>(Func<IComponentBinder> factory)
        {
            _registeredTypes.Add(typeof(T));
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            ThrowIfDisposed();

            _factories[typeof(T)] = () => factory();
            // 팩토리 등록 시 기존 싱글턴이 있었다면 교체 정책에 따라 제거할 수도 있음(여기선 그대로 둠)
        }

        public void RegisterSingleton<T>(IComponentBinder instance)
        {
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            ThrowIfDisposed();

            _registeredTypes.Add(typeof(T));
            _singletons[typeof(T)] = instance;
            // 싱글턴 등록 시 기존 팩토리가 있어도 우선순위는 싱글턴이 먼저입니다(Resolve 구현 참고).
        }

        public IComponentBinder? Resolve(Type componentType)
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

        public IComponentBinder? Resolve<T>()
            => Resolve(typeof(T));

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

            // 더 이상 어떤 방식으로도 등록되어 있지 않다면 집합에서 제거
            if (!_singletons.ContainsKey(componentType) && !_factories.ContainsKey(componentType))
                _registeredTypes.Remove(componentType);

            return removed;
        }

        public void Clear()
        {
            ThrowIfDisposed();

            // 싱글턴 Dispose
            foreach (var s in _singletons.Values)
                (s as IDisposable)?.Dispose();

            _singletons.Clear();
            _factories.Clear();
            _registeredTypes.Clear();
        }

        public System.Collections.Generic.IReadOnlyCollection<System.Type> RegisteredComponentTypes => _registeredTypes;

        public bool TryResolve(System.Type componentType, out IComponentBinder binder)
        {
            if (_singletons.TryGetValue(componentType, out var s))
            {
                binder = s;
                return true;
            }
            if (_factories.TryGetValue(componentType, out var f))
            {
                binder = f();
                return true;
            }
            binder = default!;
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ComponentBinderRegistry));
        }
    }
}