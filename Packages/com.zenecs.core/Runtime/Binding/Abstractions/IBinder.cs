#nullable enable
using System;

namespace ZenECS.Core.Binding
{
    public interface IBinder : IDisposable
    {
        Entity Entity { get; }
        int Priority { get; }
        void Bind(World world, Entity e);
        void Unbind();
        void Apply(); // 프레임 말(프리젠테이션 끝)에 항상 1회
    }

    internal interface IAttachOrderMarker
    {
        int AttachOrder { get; set; }
    }

    public abstract class BaseBinder : IBinder, IAttachOrderMarker
    {
        protected World? World { get; private set; }
        public Entity Entity { get; private set; }
        public virtual int Priority => 0;
        int IAttachOrderMarker.AttachOrder { get; set; }
        private bool _bound, _disposed;

        public void Bind(World w, Entity e)
        {
            if (_disposed || _bound) throw new Exception();
            World = w;
            Entity = e;
            _bound = true;
            OnBind(w, e);
        }
        
        public void Unbind()
        {
            if (!_bound) return;
            try
            {
                OnUnbind();
            }
            finally
            {
                _bound = false;
                World = null;
                Entity = default;
            }
        }
        
        public virtual void Apply() { }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Unbind();
            OnDispose();
        }
        
        protected virtual void OnBind(World w, Entity e) { }
        protected virtual void OnUnbind() { }
        protected virtual void OnDispose() { }
    }
}