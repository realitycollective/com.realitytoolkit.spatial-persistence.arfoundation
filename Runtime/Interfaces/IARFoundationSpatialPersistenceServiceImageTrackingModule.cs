// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityToolkit.SpatialPersistence.Interfaces;

namespace RealityToolkit.SpatialPersistence.ARFoundation
{
    public interface IARFoundationSpatialPersistenceServiceImageTrackingModule : ISpatialPersistenceServiceModule
    {
        /// <summary>
        /// The runtime library of images which will be detected and/or tracked in the physical environment.
        /// </summary>
        ARFoundationTrackableImagesLibrary TrackedImagesLibrary { get; }
    }
}