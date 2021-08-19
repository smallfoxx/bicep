// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System.Text.Json;
using Azure.Core;

namespace Bicep.Core.RegistryClient.Models
{
    internal partial class FsLayer
    {
        internal static FsLayer DeserializeFsLayer(JsonElement element)
        {
            Optional<string> blobSum = default;
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("blobSum"))
                {
                    blobSum = property.Value.GetString();
                    continue;
                }
            }
            return new FsLayer(blobSum.Value);
        }
    }
}