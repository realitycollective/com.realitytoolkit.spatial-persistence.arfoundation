// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.ServiceFramework.Definitions;
using RealityToolkit.SpatialPersistence.Interfaces;
using UnityEngine;

namespace RealityToolkit.SpatialPersistence.ARFoundation.Definitions
{
    /// <summary>
    /// Configuration profile for the <see cref="SpatialPersistenceService"/>.
    /// </summary>
    public class ARFoundationSpatialPersistenceServiceImageTrackingProfile : BaseServiceProfile<ISpatialPersistenceServiceModule>
    {
        [Header("Independent Tracked Images content configuration")]
        [SerializeField]
        [Tooltip("The local library of images which will be detected and/or tracked in the physical environment.")]
        private ARFoundationTrackableImagesLibrary trackedImagesLibrary;

        public ARFoundationTrackableImagesLibrary TrackedImagesLibrary => trackedImagesLibrary;
    }
}