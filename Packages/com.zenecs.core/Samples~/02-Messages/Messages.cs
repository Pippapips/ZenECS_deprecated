/*
 * ZenECS Core
 * Copyright (c) 2025 Pippapips Limited
 * License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
 * SPDX-License-Identifier: MIT
 * Repository: https://github.com/pippapis/zenecs.git
 */
using System;
using ZenECS;                // Kernel
using ZenECS.Core;           // WorldConfig
using ZenECS.Core.Messaging; // IMessage

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
            // Kernel 부팅 (월드/버스 생성)
            EcsKernel.Start(new WorldConfig(), configure: (world, bus) =>
            {
                // 메시지 초기 구독/와이어링이 있으면 여기서 해도 됨
            });

            var bus = EcsKernel.Bus; // 편의 레퍼런스

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

            // Show that publish during handling is safe on next PumpAll
            bus.Publish(new DamageTaken(3, 99));
            Console.WriteLine("Pump again...");
            bus.PumpAll();

            // 종료 정리
            EcsKernel.Shutdown();
        }
    }
}