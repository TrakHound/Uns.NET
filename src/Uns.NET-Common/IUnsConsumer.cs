// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;

namespace Uns
{
    public interface IUnsConsumer : IDisposable
    {
        string Id { get; }

        string Pattern { get; }

        void Push(UnsEventMessage message);
    }

    public interface IUnsConsumer<TResult> : IUnsConsumer
    {
        event EventHandler<TResult> Received;
    }
}
