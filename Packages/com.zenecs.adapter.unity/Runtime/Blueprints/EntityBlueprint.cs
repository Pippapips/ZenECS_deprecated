#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Infrastructure; // BlueprintData/ComponentJson

namespace ZenECS.Adapter.Unity.Blueprints
{
    [CreateAssetMenu(menuName = "ZenECS/Entity Blueprint", fileName = "EntityBlueprint")]
    public sealed class EntityBlueprint : ScriptableObject
    {
        [Header("Components (snapshot)")]
        [SerializeField] private BlueprintData _data = new();
        public BlueprintData Data => _data;

        [Header("Contexts (managed reference)")]
        [SerializeReference] private List<IContext> _contexts = new();
        [Header("Binders (managed reference)")]
        [SerializeReference] private List<IBinder> _binders = new();

        public Entity Spawn(World? world = null, bool clonePerEntity = true)
        {
            world ??= EcsKernel.World
                      ?? throw new InvalidOperationException("World is null. Initialize EcsKernel.World or pass a World.");

            // ✅ Router가 참조하는 World와 스폰 대상 world가 다르면 문제 → 강력 권고: 동일 유지
            if (!ReferenceEquals(EcsKernel.World, world))
                Debug.LogWarning("[EntityBlueprint] EcsKernel.World and Spawn(world) differ. Make sure BindingRouter/ContextRegistry use the same World.");

            var router = EcsKernel.BindingRouter  ?? throw new InvalidOperationException("BindingRouter is null.");
            var reg = EcsKernel.ContextRegistry ?? throw new InvalidOperationException("ContextRegistry is null.");

            var e = world.CreateEntity();
            _data?.ApplyTo(world, e);

            foreach (var ctx in _contexts)
            {
                if (ctx == null) continue;
                var inst = clonePerEntity ? (IContext)ShallowCopy(ctx, ctx.GetType()) : ctx;

                // ✅ 제네릭 대신 비제네릭으로 “실제 런타임 타입” 등록
                reg.Register(world, e, inst);
            }

            foreach (var b in _binders)
            {
                if (b == null) continue;
                var inst = clonePerEntity ? (IBinder)ShallowCopy(b, b.GetType()) : b;

                // Attach 시 IRequireContext<T> 검증이 이제 정상 통과
                router.Attach(e, inst);
            }

            return e;
        }

        public IEnumerable<Entity> SpawnMany(int count, World? world = null, bool clonePerEntity = true)
        {
            for (int i = 0; i < count; i++) yield return Spawn(world, clonePerEntity);
        }

        private static object ShallowCopy(object? source, Type t)
        {
            if (source == null) return null!;
            if (t.IsValueType) return source;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return source;

            object target;
            try { target = Activator.CreateInstance(t)!; }
            catch (Exception ex) { throw new InvalidOperationException($"Type '{t.FullName}' requires a public parameterless ctor.", ex); }

            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in t.GetFields(BF))
            {
                if (f.IsStatic) continue;
                var val = f.GetValue(source);
                f.SetValue(target, val);
            }
            return target;
        }
    }
}
