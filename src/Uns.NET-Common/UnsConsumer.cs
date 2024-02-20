// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;

namespace Uns
{
    public class UnsConsumer : UnsConsumer<UnsEventMessage> 
    { 
        public UnsConsumer(string pattern) : base(pattern, ProcessFunction) { }

        private static UnsEventMessage ProcessFunction(UnsEventMessage message)
        {
            return message;
        }
    }

    public class UnsConsumer<TResult> : IUnsConsumer<TResult>
    {
        private readonly string _id;
        private readonly string _pattern;
        private readonly Func<UnsEventMessage, TResult> _processFunction;


        public string Id => _id;

        public string Pattern => _pattern;


        public event EventHandler<TResult> Received;


        public UnsConsumer(string pattern, Func<UnsEventMessage, TResult> processFunction = null)
        {
            _id = Guid.NewGuid().ToString();
            _pattern = pattern;
            _processFunction = processFunction != null ? processFunction : DefaultProcessFunction;
        }

        public void Push(UnsEventMessage message)
        {
            if (_processFunction != null)
            {
                var result = _processFunction(message);
                if (result != null)
                {
                    if (Received != null) Received.Invoke(this, result);
                }
            }
        }


        private TResult DefaultProcessFunction(UnsEventMessage message)
        {
            if (message != null)
            {
                try
                {
                    var text = System.Text.Encoding.UTF8.GetString(message.Content);
                    var type = typeof(TResult);

                    try
                    {
                        if (type == typeof(DateTime))
                        {
                            object obj = DateTime.Parse(text);
                            return (TResult)obj;
                        }
                        else if (typeof(Enum).IsAssignableFrom(type))
                        {
                            if (Enum.TryParse(type, (string)text, true, out var result))
                            {
                                return (TResult)result;
                            }
                        }
                        else
                        {
                            return (TResult)Convert.ChangeType(text, type);
                        }
                    }
                    catch { }
                }
                catch { }
            }

            return default;
        }
    }
}
