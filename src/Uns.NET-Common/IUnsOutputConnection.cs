// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Uns
{
    public interface IUnsOutputConnection : IUnsConnection
    {
        Task Publish(UnsEventMessage message);
    }
}
