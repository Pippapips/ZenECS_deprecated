using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Internal
{
    internal interface IComponentPool
    {
        void EnsureInitialized();
        void EnsureCapacity(int entityId);
        bool Has(int entityId);
        void Remove(int entityId);
        object GetBoxed(int entityId);
        void SetBoxed(int entityId, object value);
        IEnumerable<(int id, object boxed)> EnumerateAll();
        int Count { get; }
    }

    internal sealed class ComponentPool<T> : IComponentPool where T : struct
    {
        const int DefaultInitialCapacity = 256;

        private T[] data;
        private BitSet present;
        private int count;
        private bool inited;

        public ComponentPool(int initialCapacity = DefaultInitialCapacity)
        {
            data = new T[Math.Max(1, initialCapacity)];
            present = new BitSet(Math.Max(1, initialCapacity));
            count = 0;
            inited = true;
        }

        public void EnsureInitialized()
        {
            if (inited) return; // 멱등
            int cap = DefaultInitialCapacity;
            if (data == null || data.Length == 0)
                data = new T[cap];
            else
                cap = data.Length;

            if (present == null)
                present = new BitSet(cap);
            else
                present.EnsureCapacity(cap);

            if (count < 0) count = 0;
            inited = true;
        }

        public int Count => count;

        public void EnsureCapacity(int entityId)
        {
            EnsureInitialized();
            
            if (entityId < data.Length) return;
            int cap = data.Length;
            while (cap <= entityId) cap <<= 1;
            System.Array.Resize(ref data, cap);
            present.EnsureCapacity(cap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId) => entityId < data.Length && present.Get(entityId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Ref(int entityId)
        {
            EnsureCapacity(entityId);
            if (!present.Get(entityId))
            {
                present.Set(entityId, true);
                count++;
            }

            return ref data[entityId];
        }

        public void Remove(int entityId)
        {
            if (Has(entityId))
            {
                present.Set(entityId, false);
                count--;
            }
        }

        public T Get(int entityId) => data[entityId];

        public IEnumerable<(int id, object boxed)> EnumerateAll()
        {
            for (int i = 0; i < data.Length; i++)
                if (present.Get(i))
                    yield return (i, (object)data[i]);
        }

        public object GetBoxed(int entityId) => Has(entityId) ? (object)data[entityId] : null;

        public void SetBoxed(int entityId, object value)
        {
            ref var r = ref Ref(entityId);
            r = (T)value;
        }
    }
}