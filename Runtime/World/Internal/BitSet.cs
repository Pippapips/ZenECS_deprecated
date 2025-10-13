using System.Runtime.CompilerServices;

namespace ZenECS.Core.Internal
{
    internal sealed class BitSet
    {
        private uint[] words;
        public BitSet(int bitCapacity) { words = new uint[(bitCapacity + 31) >> 5]; }
        public void EnsureCapacity(int bitCapacity)
        {
            int need = (bitCapacity + 31) >> 5;
            if (need > words.Length) System.Array.Resize(ref words, need);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            int w = index >> 5, b = index & 31;
            if (w >= words.Length) return false;
            return (words[w] & (1u << b)) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            int w = index >> 5, b = index & 31;
            EnsureCapacity(index + 1);
            if (value) words[w] |= (1u << b); else words[w] &= ~(1u << b);
        }
    }
}