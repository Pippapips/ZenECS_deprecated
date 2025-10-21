#nullable enable
#if UNITY_EDITOR
using ZenECS.Core;
using ZenECS.Core.Extensions;
using ZenECS.Core.Infrastructure;

namespace ZenECS.EditorUtils
{
    /// <summary>에디터에서 ECS 쓰기는 항상 Mutator 경유</summary>
    public static class EcsEditorMutatorAPI
    {
        public static void Replace<T>(World w, Entity e, in T v) where T:struct => w.Replace(e, in v);
        public static void Remove<T>(World w, Entity e) where T:struct => w.Remove<T>(e);
        public static void Add<T>(World w, Entity e, in T v) where T:struct => w.Add(e, in v);
        public static void Toggle<T>(World w, Entity e, bool on) where T:struct => w.Toggle<T>(e, on);
    }
}
#endif