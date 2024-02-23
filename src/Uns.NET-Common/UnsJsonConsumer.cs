// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using Uns.Extensions;

namespace Uns
{
    public class UnsJsonConsumer<TModel> : UnsConsumer<TModel> 
    { 
        public UnsJsonConsumer(string pattern) : base(pattern, ProcessFunction) { }

        private static TModel ProcessFunction(UnsEventMessage message)
        {
            if (message != null)
            {
                var json = System.Text.Encoding.UTF8.GetString(message.Content);
                return Json.Convert<TModel>(json);
            }

            return default;
        }
    }
}
