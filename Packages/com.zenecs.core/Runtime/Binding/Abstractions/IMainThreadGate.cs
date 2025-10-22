#nullable enable
using System;
namespace ZenECS.Core.Binding.Util
{
    public interface IMainThreadGate
    {
        bool IsMainThread { get; }
        void Ensure();
        void Post(Action action);
        void Send(Action action);
    }
}
