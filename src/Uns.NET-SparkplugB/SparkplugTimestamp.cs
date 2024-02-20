// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

namespace Uns
{
    internal static class SparkplugTimestamp
    {
        private static readonly DateTime _epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        public static DateTime ToDateTime(ulong timestamp)
        {
            return _epochTime.AddMilliseconds(timestamp);
        }

        public static ulong ToTimestamp(DateTime d)
        {
            var x = d;
            if (d.Kind == DateTimeKind.Local) x = d.ToUniversalTime();
            var duration = x - _epochTime;
            return (ulong)duration.Ticks;
        }
    }
}
