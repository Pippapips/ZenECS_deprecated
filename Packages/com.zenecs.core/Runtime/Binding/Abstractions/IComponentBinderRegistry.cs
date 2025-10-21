#nullable enable
using System;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// 컴포넌트 타입 ↔ IComponentBinder 매핑 레지스트리.
    /// - Factory: Resolve 때마다 새 핸들러 인스턴스 생성
    /// - Singleton: 항상 동일 인스턴스 반환(핸들러가 무상태일 때만 권장)
    /// </summary>
    public interface IComponentBinderRegistry : IDisposable
    {
        /// <summary>해당 컴포넌트 타입의 핸들러를 생성할 팩토리를 등록합니다.</summary>
        void RegisterFactory<T>(Func<IComponentBinder> factory);

        /// <summary>해당 컴포넌트 타입의 핸들러 싱글턴 인스턴스를 등록합니다.</summary>
        void RegisterSingleton<T>(IComponentBinder instance);

        /// <summary>타입으로 핸들러를 조회(필요 시 생성)합니다. 없으면 null.</summary>
        IComponentBinder? Resolve(Type componentType);

        /// <summary>제네릭으로 핸들러를 조회(필요 시 생성)합니다. 없으면 null.</summary>
        IComponentBinder? Resolve<T>();

        /// <summary>등록된 항목을 제거합니다(팩토리/싱글턴 둘 중 있는 것을 제거). 성공 시 true.</summary>
        bool Unregister(Type componentType);

        /// <summary>팩토리/싱글턴을 모두 초기화합니다. 싱글턴이 IDisposable이면 Dispose 호출.</summary>
        void Clear();
    }
}