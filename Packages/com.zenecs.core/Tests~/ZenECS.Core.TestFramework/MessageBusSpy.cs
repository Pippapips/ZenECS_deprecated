using System;
using System.Collections.Generic;
using ZenECS.Core.Messaging;

namespace ZenECS.Core.Testing
{
    /// <summary>
    /// Minimal spy that counts messages delivered by <see cref="IMessageBus"/>.
    /// </summary>
    public sealed class MessageBusSpy
    {
        private readonly IMessageBus _bus;
        private readonly Dictionary<Type, int> _counts = new();

        public MessageBusSpy(IMessageBus bus) => _bus = bus;

        public void Track<T>() where T : struct, IMessage
        {
            _bus.Subscribe<T>(_ =>
            {
                var t = typeof(T);
                _counts[t] = _counts.TryGetValue(t, out var n) ? n + 1 : 1;
            });
        }

        public int Received<T>() where T : struct
            => _counts.TryGetValue(typeof(T), out var n) ? n : 0;
    }
}