// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Extensions;
using RealityToolkit.SpatialPersistence.Definitions;
using System;
using System.Linq;
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

        public ARFoundationTrackedImageData GetTrackedImageByGuid(Guid guid)
        {
            foreach (var trackedImage in trackedImages)
            {
                if (trackedImage.ReferenceGuid.Equals(guid))
                {
                    return trackedImage;
                }
            }
            return null;
        }

        public ARFoundationTrackedImageData GetTrackedImageByName(string name)
        {
            foreach (var trackedImage in trackedImages)
            {
                if (trackedImage.Name.Equals(name))
                {
                    return trackedImage;
                }
            }
            return null;
        }

        public void UpdateTrackedImageData(ARFoundationTrackedImageData trackedImageData)
        {
            for (int i = 0; i < trackedImages.Length; i++)
            {
                if (trackedImages[i].ReferenceGuid.Equals(trackedImageData.ReferenceGuid))
                {
                    trackedImages[i] = trackedImageData;
                    return;
                }
            }
        }

        public void AddTrackedImageData(ARFoundationTrackedImageData trackedImageData)
        {
            if (!trackedImages.Contains(trackedImageData))
            {
                trackedImages = trackedImages.AddItem(trackedImageData);
            }
        }
    }
}