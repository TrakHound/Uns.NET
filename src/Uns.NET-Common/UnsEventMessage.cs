// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;

namespace Uns
{
    public class UnsEventMessage
    {
        public string Path { get; set; }

        public NamespaceConfiguration Namespace { get; set; }

        public IUnsConnection Connection { get; set; }

        public UnsEventContentType ContentType { get; set; }

        public byte[] Content { get; set; }

        public DateTime Timestamp { get; set; }


        public UnsEventMessage()
        {
            Timestamp = DateTime.UtcNow;
        }

        public UnsEventMessage(string path, UnsEventContentType contentType, byte[] content, DateTime? timestamp = null)
        {
            Path = path;
            ContentType = contentType;
            Content = content;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }


        public static UnsEventContentType GetContentType(Type type)
        {
            if (type != null)
            {
                if (type == typeof(string)) return UnsEventContentType.STRING;
                if (type == typeof(byte)) return UnsEventContentType.BYTE;
                if (type == typeof(short)) return UnsEventContentType.INT_16;
                if (type == typeof(int)) return UnsEventContentType.INT_32;
                if (type == typeof(long)) return UnsEventContentType.INT_64;
                if (type == typeof(float)) return UnsEventContentType.FLOAT;
                if (type == typeof(double)) return UnsEventContentType.DOUBLE;
                if (type == typeof(decimal)) return UnsEventContentType.DECIMAL;             
            }

            return UnsEventContentType.STRING;
        }
    }
}
