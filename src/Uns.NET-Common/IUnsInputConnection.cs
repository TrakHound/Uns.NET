// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

namespace Uns
{
    public interface IUnsInputConnection : IUnsConnection
    {
        event UnsConnectionEventHandler EventReceived;
    }
}
