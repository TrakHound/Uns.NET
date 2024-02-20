// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Uns
{
    public class UnsDeadbandValueMiddleware : IUnsClientMiddleware
    {
        private readonly double _minimumDelta;
        private readonly Dictionary<string, double> _cache = new Dictionary<string, double>();
        private readonly object _lock = new object();


        public string Id => "DEADBAND_VALUE";


        public UnsDeadbandValueMiddleware(double minimumDelta)
        {
            _minimumDelta = minimumDelta;
        }


        public UnsEventMessage Process(UnsEventMessage e)
        {
            if (e.Content != null)
            {
                if (double.TryParse(GetValueString(e.Content), out var value))
                {
                    lock (_lock)
                    {
                        if (_cache.ContainsKey(e.Path))
                        {
                            _cache.TryGetValue(e.Path, out var existing);
                            if (Math.Abs(value - existing) >= _minimumDelta)
                            {
                                _cache.Remove(e.Path);
                                _cache.Add(e.Path, value);

                                return e;
                            }
                        }
                        else
                        {
                            _cache.Add(e.Path, value);
                            return e;
                        }
                    }
                }
            }

            return null;
        }

        private static string GetValueString(byte[] content)
        {
            try
            {
                return System.Text.Encoding.UTF8.GetString(content);
            }
            catch { }

            return null;
        }
    }
}
