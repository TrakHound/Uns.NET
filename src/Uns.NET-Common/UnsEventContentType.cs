// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

namespace Uns
{
    public enum UnsEventContentType
    {
        STRING,
        BYTE,
        INT_16,
        INT_32,
        INT_64,
        FLOAT,
        DOUBLE,
        DECIMAL,
        JSON,
        JSON_ARRAY,
        SPARKPLUG_B_METRIC,
        SPARKPLUG_B_DEVICE_BIRTH,
        SPARKPLUG_B_DEVICE_DATA,
        SPARKPLUG_B_DEVICE_DEATH,
        SPARKPLUG_B_NODE_BIRTH,
        SPARKPLUG_B_NODE_DATA,
        SPARKPLUG_B_NODE_DEATH
    }
}
