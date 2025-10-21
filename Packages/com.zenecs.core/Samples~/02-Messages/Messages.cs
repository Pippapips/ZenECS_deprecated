using System;
using ZenECS.Core.Messaging;

namespace ZenEcsCoreSamples.Messages
{
    // Define a message
    public readonly struct DamageTaken : IMessage
    {
        public readonly int EntityId;
        public readonly int Amount;
        public DamageTaken(int entityId, int amount) { EntityId = entityId; Amount = amount; }
        public override string ToString() => $"DamageTaken(e:{EntityId}, amt:{Amount})";
    }

    public static class Program
    {
        public static void Main()
        {
            var bus = new MessageBus();

            // Subscribe (disposable)
            using var sub1 = bus.Subscribe<DamageTaken>(m =>
                Console.WriteLine($"[UI]    show - {m}"));
            using var sub2 = bus.Subscribe<DamageTaken>(m =>
                Console.WriteLine($"[Logic] apply - e:{m.EntityId} hp -= {m.Amount}"));

            // Publish a few messages
            bus.Publish(new DamageTaken(1, 10));
            bus.Publish(new DamageTaken(2, 5));
            bus.Publish(new DamageTaken(1, 7));

            // Deliver all queued messages to subscribers
            int delivered = bus.PumpAll();
            Console.WriteLine($"Delivered {delivered} messages.");

            // Show that publish during handling is safe next PumpAll
            bus.Publish(new DamageTaken(3, 99));
            Console.WriteLine("Pump again...");
            bus.PumpAll();
        }
    }
}