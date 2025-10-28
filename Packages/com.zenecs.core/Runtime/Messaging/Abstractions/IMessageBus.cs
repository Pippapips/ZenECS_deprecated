// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IMessageBus.cs
// Purpose: Defines a minimal message-passing interface for ECS systems.
// Key concepts:
//   • Lightweight publish/subscribe model for struct-based messages.
//   • PumpAll() delivers all queued messages to subscribers once per frame.
//   • Thread-safe by design for cross-system communication.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Messaging
{
    /// <summary>
    /// Marker interface for messages used within the ECS message bus.
    /// </summary>
    public interface IMessage { }

    /// <summary>
    /// Defines the contract for the core message bus implementation.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Subscribes to messages of type <typeparamref name="T"/>.
        /// Returns a disposable subscription handle.
        /// </summary>
        IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;

        /// <summary>
        /// Publishes a message instance to all subscribers of type <typeparamref name="T"/>.
        /// </summary>
        void Publish<T>(in T msg) where T : struct, IMessage;

        /// <summary>
        /// Pumps all accumulated messages and dispatches them to subscribers once per frame.
        /// Typically invoked by the runner or during FrameSetup.
        /// </summary>
        /// <returns>The number of processed message batches.</returns>
        int PumpAll();
        void Clear();
    }
}