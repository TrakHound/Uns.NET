// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Uns
{
    public static class Json
    {
        #region "Conversion"

        public static JsonSerializerOptions DefaultOptions
        {
            get
            {
                return new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    PropertyNameCaseInsensitive = true,
                    MaxDepth = 1000,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
            }
        }

        public static string Convert(object obj, JsonConverter converter = null, bool indented = false)
        {
            if (obj != null)
            {
                try
                {
                    if (converter != null || indented)
                    {
                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = indented,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                            PropertyNameCaseInsensitive = true,
                            MaxDepth = 1000,
                            NumberHandling = JsonNumberHandling.AllowReadingFromString
                        };

                        if (converter != null) options.Converters.Add(converter);

                        return JsonSerializer.Serialize(obj, options);
                    }
                    else
                    {
                        return JsonSerializer.Serialize(obj, DefaultOptions);
                    }
                }
                catch { }
            }

            return null;
        }

        public static byte[] ConvertToUtf8(object obj)
        {
            if (obj != null)
            {
                try
                {
                    return JsonSerializer.SerializeToUtf8Bytes(obj, DefaultOptions);
                }
                catch { }
            }

            return Array.Empty<byte>();
        }

        public static T Convert<T>(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(json, DefaultOptions);
                }
                catch { }
            }

            return default(T);
        }

        public static IEnumerable<T> Convert<T>(IEnumerable<string> jsons)
        {
            if (!jsons.IsNullOrEmpty())
            {
                var objs = new List<T>();

                foreach (var json in jsons)
                {
                    if (!string.IsNullOrEmpty(json))
                    {
                        var obj = Convert<T>(json);
                        if (obj != null) objs.Add(obj);
                    }
                }

                return objs;
            }

            return null;
        }

        public static T ConvertObject<T>(object obj, JsonConverter converter = null)
        {
            if (obj != null)
            {
                var json = Convert(obj, converter);
                return Convert<T>(json);
            }

            return default(T);
        }

        public static IEnumerable<T> ConvertList<T>(IEnumerable objs, JsonConverter converter = null)
        {
            if (objs != null)
            {
                var rObjs = new List<T>();

                foreach (var obj in objs)
                {
                    var rObj = ConvertObject<T>(obj, converter);
                    if (rObj != null) rObjs.Add(rObj);
                }

                return rObjs;
            }

            return null;
        }

        public static string ToJsonArray(object[] array)
        {
            if (array != null)
            {
                var json = "[";

                for (var i = 0; i < array.Length; i++)
                {
                    var obj = array[i];

                    if (obj == null)
                    {
                        json += "null";
                    }
                    else if (obj.GetType() == typeof(bool))
                    {
                        json += "true";
                    }
                    else if (IsNumber(obj))
                    {
                        json += obj.ToString();
                    }
                    else
                    {
                        json += $"\"{obj.ToString()}\"";
                    }

                    if (i < array.Length - 1) json += ",";
                }

                json += "]";

                return json;
            }

            return null;
        }

        private static bool IsNumber(object obj)
        {
            var t = obj.GetType();

            var b = false;
            b = t == typeof(byte);
            if (!b) b = t == typeof(short);
            if (!b) b = t == typeof(int);
            if (!b) b = t == typeof(long);
            if (!b) b = t == typeof(double);
            if (!b) b = t == typeof(decimal);

            return b;
        }

        #endregion

        #region "IO"

        public static T Read<T>(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    return Convert<T>(json);
                }
                catch { }
            }

            return default;
        }

        public static T Read<T>(string path, string node)
        {
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(node) && File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var d = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (d != null)
                    {
                        var ld = new Dictionary<string, object>();
                        foreach (var entry in d)
                        {
                            ld.Add(entry.Key.ToLower(), entry.Value);
                        }

                        var n = ld.GetValueOrDefault(node.ToLower());
                        if (n != null)
                        {
                            var nodeJson = JsonSerializer.Serialize(n, DefaultOptions);
                            if (!string.IsNullOrEmpty(nodeJson))
                            {
                                return JsonSerializer.Deserialize<T>(nodeJson, DefaultOptions);
                            }
                        }
                    }
                }
                catch { }
            }

            return default;
        }

        public static async Task<T> ReadAsync<T>(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(path);
                    return Convert<T>(json);
                }
                catch { }
            }

            return default;
        }


        public static void Write(object obj, string path)
        {
            if (obj != null && !string.IsNullOrEmpty(path))
            {
                var json = Convert(obj, indented: true);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        File.WriteAllText(path, json);
                    }
                    catch { }
                }
            }
        }

        public static async Task WriteAsync(object obj, string path, CancellationToken cancellationToken)
        {
            if (obj != null && !string.IsNullOrEmpty(path))
            {
                var json = Convert(obj, indented: true);
                if (!string.IsNullOrEmpty(json))
                {
                    await File.WriteAllTextAsync(path, json, cancellationToken);
                }
            }
        }

        #endregion
    }
}