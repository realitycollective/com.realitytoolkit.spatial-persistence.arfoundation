// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine.XR.ARSubsystems;

namespace RealityToolkit.SpatialPersistence.ARFoundation.Extensions
{
    public static class RuntimeReferenceImageLibraryExtensions
    {
        public static Guid GetTrackedImageReferenceGuid(this MutableRuntimeReferenceImageLibrary library, string textureName)
        {
            foreach (var referenceImage in library)
            {
                if (referenceImage.name == textureName)
                {
                    return referenceImage.textureGuid;
                }
            }
            return Guid.Empty;
        }

        public static Guid GetTrackedImageReferenceGuid(this RuntimeReferenceImageLibrary library, string textureName)
        {
            int count = library.count;
            for (int i = 0; i < count; i++)
            {
                if (library[i].name == textureName)
                {
                    return library[i].textureGuid;
                }
            }
            return Guid.Empty;
        }
    }
}