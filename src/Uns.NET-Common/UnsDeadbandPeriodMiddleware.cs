﻿// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Uns
{
    public class UnsDeadbandPeriodMiddleware : IUnsClientMiddleware
    {
        private readonly TimeSpan _minimumPeriod;
        private readonly Dictionary<string, DateTime> _cache = new Dictionary<string, DateTime>();
        private readonly object _lock = new object();


        public string Id => "DEADBAND_PERIOD";


        public UnsDeadbandPeriodMiddleware(TimeSpan minimumPeriod)
        {
            _minimumPeriod = minimumPeriod;
        }


        public UnsEventMessage Process(UnsEventMessage e)
        {
            if (e.Content != null)
            {
                lock (_lock)
                {
                    if (_cache.ContainsKey(e.Path))
                    {
                        _cache.TryGetValue(e.Path, out var existingTimestamp);
                        if (e.Timestamp - existingTimestamp >= _minimumPeriod)
                        {
                            _cache.Remove(e.Path);
                            _cache.Add(e.Path, e.Timestamp);

                            return e;
                        }
                    }
                    else
                    {
                        _cache.Add(e.Path, e.Timestamp);
                        return e;
                    }
                }
            }

            return null;
        }
    }
}
