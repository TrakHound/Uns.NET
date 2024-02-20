// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

namespace Uns
{
    public class NamespaceConfiguration
    {
        public string Path { get; set; }

        public string Sender { get; set; }

        public NamespaceKind Kind { get; set; }

        public NamespaceType Type { get; set; }

        public UnsEventContentType ContentType { get; set; }
    }
}
