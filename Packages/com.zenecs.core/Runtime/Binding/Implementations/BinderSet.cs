using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    internal sealed class BinderSet
    {
        private readonly List<IBinder> _list = new(4);
        private int _seq;
        public void Attach(IBinder b)
        {
            if (b is IAttachOrderMarker m) m.AttachOrder = _seq++;
            int i = _list.FindIndex(x => x.Priority > b.Priority
                                         || (x.Priority == b.Priority && ((IAttachOrderMarker)x).AttachOrder > ((IAttachOrderMarker)b).AttachOrder));
            if (i < 0) _list.Add(b);
            else _list.Insert(i, b);
        }
        public bool Detach(IBinder b) => _list.Remove(b);
        public IBinder[] EnumerateSorted() => _list.ToArray();
    }
}