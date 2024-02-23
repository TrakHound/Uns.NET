// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System.Collections.Generic;
using Uns.Extensions;

namespace Uns
{
    public class UnsReportByExceptionMiddleware : IUnsClientMiddleware
    {
        private readonly Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();
        private readonly object _lock = new object();


        public string Id => "REPORT_BY_EXCEPTION";


        public UnsEventMessage Process(UnsEventMessage e)
        {
            lock (_lock)
            {
                _cache.TryGetValue(e.Path, out var existing);
                if (existing == null || !ObjectExtensions.ByteArraysEqual(existing, e.Content))
                {
                    _cache.Remove(e.Path);
                    _cache.Add(e.Path, e.Content);

                    return e;
                }
            }

            return null;
        }
    }
}
