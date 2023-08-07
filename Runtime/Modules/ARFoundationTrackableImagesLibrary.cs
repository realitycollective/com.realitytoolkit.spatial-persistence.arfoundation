// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityToolkit.SpatialPersistence.Definitions;
using UnityEngine;

namespace RealityToolkit.SpatialPersistence.ARFoundation
{
    [CreateAssetMenu(menuName = "Reality Toolkit/Spatial Persistence/CreateARFoundationTrackedImagesLibrary", fileName = "ARFoundationTrackedImagesLibraryAsset")]
    public class ARFoundationTrackableImagesLibrary : ScriptableObject
    {
        [SerializeField]
        private ARFoundationTrackedImageData[] trackedImages;

        public ARFoundationTrackedImageData[] TrackedImages => trackedImages;

        public void UpdateTrackedImages(ARFoundationTrackedImageData[] trackedImages)
        {
            this.trackedImages = trackedImages ?? new ARFoundationTrackedImageData[0];
        }
    }
}