using System;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Internal
{
    public sealed class BitSet
    {
        private uint[] _words;
        public int Length => _words.Length << 5;

        public BitSet(int capacityBits)
        {
            var words = Math.Max(1, (capacityBits + 31) >> 5);
            _words = new uint[words];
        }

        public void EnsureCapacity(int capacityBits)
        {
            int needWords = (capacityBits + 31) >> 5;
            if (needWords <= _words.Length) return;

            var old = _words;
            var nw = new uint[needWords];
            Array.Copy(old, nw, old.Length); // ← 기존 비트 보존!
            _words = nw;
        }

        public void ClearAll()
        {
            Array.Clear(_words, 0, _words.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            int w = index >> 5, b = index & 31;
            if (w >= _words.Length) return false;
            return (_words[w] & (1u << b)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            int w = index >> 5, b = index & 31;
            EnsureCapacity(index + 1);
            if (value) _words[w] |= (1u << b);
            else       _words[w] &= ~(1u << b);
        }

        // --- Snapshot 직렬화/역직렬화 (uint[] 기반) ---
        public byte[] ToByteArray()
        {
            // words 전체를 그대로 직렬화 (길이는 외부에서 따로 저장하거나,
            // WorldSnapshot에서 bytes.Length로 유추 가능)
            int len = _words.Length * sizeof(uint);
            var bytes = new byte[len];
            if (len > 0) Buffer.BlockCopy(_words, 0, bytes, 0, len);
            return bytes;
        }

        public void FromByteArray(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                _words = Array.Empty<uint>();
                return;
            }
            int count = bytes.Length / sizeof(uint);
            _words = new uint[count];
            Buffer.BlockCopy(bytes, 0, _words, 0, count * sizeof(uint));
        }
    }
}
