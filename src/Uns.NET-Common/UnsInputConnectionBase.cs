// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uns
{
    public abstract class UnsInputConnectionBase : IUnsInputConnection
    {
        public virtual string Type { get; }

        public virtual string Id { get; }

        public virtual IEnumerable<string> Patterns { get; }


        /// <summary>
        /// Raised when the connection to the MQTT broker is established
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Raised when the connection to the MQTT broker is disconnected 
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Raised when the status of the connection to the MQTT broker has changed
        /// </summary>
        public event UnsConnectionStatusHandler ConnectionStatusChanged;


        public event UnsConnectionEventHandler EventReceived;


        public virtual Task Start() => Task.CompletedTask;

        public virtual Task Stop() => Task.CompletedTask;
    }
}
