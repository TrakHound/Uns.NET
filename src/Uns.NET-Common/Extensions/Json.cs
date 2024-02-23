// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uns.Extensions
{
    public static class Json
    {
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
    }
}