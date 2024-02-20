// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uns
{
    public interface IUnsConnection
    {
        string Type { get; }

        string Id { get; }

        IEnumerable<string> Patterns { get; }


        event UnsConnectionStatusHandler ConnectionStatusChanged;


        Task Start();

        Task Stop();
    }
}
