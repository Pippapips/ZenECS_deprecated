using System.Threading;

namespace ZenECS.Core.Messaging.Channels
{
    public sealed class RingBuffer<T>
    {
        private readonly T[] buf;
        private int head; // pop index
        private int tail; // push index

        public RingBuffer(int capacity) { buf = new T[capacity]; head = tail = 0; }

        public void Push(in T item)
        {
            int t = Interlocked.Increment(ref tail) - 1;
            buf[t % buf.Length] = item; // overflow 시 오래된 데이터 덮음(간이 구현)
        }

        public bool TryPop(out T item)
        {
            int h = head;
            if (h == Volatile.Read(ref tail)) { item = default; return false; }
            item = buf[h % buf.Length];
            Interlocked.Exchange(ref head, h + 1);
            return true;
        }
    }
}