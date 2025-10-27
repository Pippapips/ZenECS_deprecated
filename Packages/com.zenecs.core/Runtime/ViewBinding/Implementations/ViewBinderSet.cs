using System.Collections.Generic;

namespace ZenECS.Core.ViewBinding
{
    public sealed class ViewBinderSet
    {
        private readonly List<IViewBinder> _list = new(4);
        private int _seq;
        public void Attach(IViewBinder b){
            if (b is IAttachOrderMarker m) m.AttachOrder = _seq++;
            int i = _list.FindIndex(x => x.Priority > b.Priority
                                         || (x.Priority==b.Priority && ((IAttachOrderMarker)x).AttachOrder > ((IAttachOrderMarker)b).AttachOrder));
            if (i<0) _list.Add(b); else _list.Insert(i,b);
        }
        public bool Detach(IViewBinder b)=>_list.Remove(b);
        public IViewBinder[] EnumerateSorted()=>_list.ToArray();
    }    
}
