#nullable enable
using System;

namespace ZenECS.Core.Messaging
{
    public interface IMessageBus
    {
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;
        void Publish<T>(in T msg) where T : struct, IMessage;

        // 러너/프레임셋업에서 한 번 호출: 누적 메시지를 구독자에게 전달
        int PumpAll();
    }
}
