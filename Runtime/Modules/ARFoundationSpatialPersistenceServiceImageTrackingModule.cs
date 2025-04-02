// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Utilities.Extensions;
using RealityCollective.Utilities.Async;
using RealityToolkit.SpatialPersistence.ARFoundation.Definitions;
using RealityToolkit.SpatialPersistence.Definitions;
using RealityToolkit.SpatialPersistence.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RealityToolkit.SpatialPersistence.ARFoundation
{
    [System.Runtime.InteropServices.Guid("94813E3D-1625-4836-8589-210A11252830")]
    public class ARFoundationSpatialPersistenceServiceImageTrackingModule : BaseSpatialPersistenceServiceModule, IARFoundationSpatialPersistenceServiceImageTrackingModule
    {
        #region Private Variables
        private readonly ARFoundationSpatialPersistenceServiceImageTrackingProfile profile;
        private ARTrackedImageManager trackedImageManager;
        private ARFoundationDynamicLibraryManager dynamicLibraryManager;
        private MutableRuntimeReferenceImageLibrary runtimeImageLibrary;
        private readonly List<Guid> trackedImageIds = new();
        private readonly Dictionary<Guid, Guid> trackedImageReferences = new Dictionary<Guid, Guid>();
        private bool isStarted = false;
        private bool isStarting = false;

        private bool HasValidTrackingProfile => profile.IsNotNull() && TrackedImagesLibrary.IsNotNull();

        private ARTrackedImageManager TrackedImageManager
        {
            get
            {
                if (trackedImageManager == null)
                {
                    // Get a reference to the SpatialAnchorManager component (must be on the same gameobject)
#if UNITY_2023_1_OR_NEWER
                    trackedImageManager = GameObject.FindFirstObjectByType<ARTrackedImageManager>();
#else                    
                    trackedImageManager = GameObject.FindObjectOfType<ARTrackedImageManager>();
#endif
                    if (trackedImageManager.IsNull())
                    {
                        var message = $"Unable to locate the {typeof(ARTrackedImageManager)} in the scene, service cannot initialize";
                        OnSpatialPersistenceError(message);
                    }
                }
                return trackedImageManager;
            }
        }

        private ARFoundationDynamicLibraryManager DynamicLibraryManager
        {
            get
            {
                if (dynamicLibraryManager == null)
                {
                    // Get a reference to the SpatialAnchorManager component (must be on the same gameobject)
#if UNITY_2023_1_OR_NEWER
                    dynamicLibraryManager = GameObject.FindFirstObjectByType<ARFoundationDynamicLibraryManager>();
#else                    
                    dynamicLibraryManager = GameObject.FindObjectOfType<ARFoundationDynamicLibraryManager>();
#endif
                    if (dynamicLibraryManager.IsNull())
                    {
                        if (TrackedImageManager.IsNull())
                        {
                            var message = $"Unable to locate the {typeof(ARTrackedImageManager)} in the scene, service cannot initialize";
                            OnSpatialPersistenceError(message);
                        }
                        dynamicLibraryManager = TrackedImageManager.gameObject.AddComponent<ARFoundationDynamicLibraryManager>();
                    }
                }
                return dynamicLibraryManager;
            }
        }

        #endregion Private Variables

        public ARFoundationSpatialPersistenceServiceImageTrackingModule(string name, uint priority, ARFoundationSpatialPersistenceServiceImageTrackingProfile profile, ISpatialPersistenceService parentService)
            : base(name, priority, profile, parentService)
        {
            this.profile = profile;
        }

        #region ISpatialPersistenceServiceModule
        /// <inheritdoc />
        public override bool IsRunning => !TrackedImageManager.IsNull() && isStarted;

        /// <inheritdoc />
        public override SpatialPersistenceTrackingType TrackingType => SpatialPersistenceTrackingType.ImageTracking;

        /// <inheritdoc />
        public override Task StartSpatialPersistenceModule()
        {
            if (TrackedImageManager is null)
            {
                var message = $"Unable to start the ARFoundation Spatial Persistence module as the {nameof(ARTrackedImageManager)} is not defined in the scene";
                OnSpatialPersistenceError(message);
                return Task.FromException(new ArgumentNullException(nameof(ARTrackedImageManager), message));
            }

            if (TrackedImageManager.referenceLibrary is null)
            {
                var message = $"Unable to start the ARFoundation Spatial Persistence module as the {nameof(ARTrackedImageManager)} has no Reference Image Library defined\nA default Image library is required for the Component to start in Unity";
                OnSpatialPersistenceError(message);
                return Task.FromException(new ArgumentNullException($"{nameof(ARTrackedImageManager)} - Serialized Library", message));
            }
            if (!IsRunning && !isStarting)
            {
                isStarting = true;
                runtimeImageLibrary = TrackedImageManager.referenceLibrary as MutableRuntimeReferenceImageLibrary;
                DynamicLibraryManager.OnImageLoaded += OnImageLoaded;
                DynamicLibraryManager.OnImageLoadFailed += OnImageLoadFailed;

#if ARFOUNDATION_6
                TrackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
#else
                TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
#endif
                if (HasValidTrackingProfile && TrackedImagesLibrary.TrackedImages != null)
                {
                    DynamicLibraryManager.ProcessImages(runtimeImageLibrary, TrackedImagesLibrary.TrackedImages);
                }
                isStarted = true;
                isStarting = false;
                OnSessionStarted();
                return Task.CompletedTask;
            }
            return Task.FromException(new ArgumentNullException(nameof(ARTrackedImageManager)));
        }

        /// <inheritdoc />
        public override void StopSpatialPersistenceModule()
        {
            if (TrackedImageManager != null)
            {
#if ARFOUNDATION_6
                TrackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
#else
                TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
#endif
            }
            OnSessionEnded();
        }

        /// <inheritdoc />
        public override void TryFindAnchors(params SpatialPersistenceSearchArgs[] searchCriteria)
        {
            if (searchCriteria == null)
            {
                OnSpatialPersistenceError("Cannot add anchors as no search criteria was provided");
                return;
            }
            if (!IsRunning)
            {
                StartSpatialPersistenceModule();
            }
            if (!IsRunning)
            {
                OnSpatialPersistenceError($"[{nameof(TryFindAnchors)}] - Cannot find anchors as the Image Tracking Service is not started, check configuration");
                return;
            }

            foreach (var searchArg in searchCriteria)
            {
                if (searchArg.spatialPersistenceTrackingType != TrackingType || searchArg.spatialPersistenceTrackingType == SpatialPersistenceTrackingType.Any)
                {
                    // Skip if the search criteria is not valid for this type of module
                    OnSpatialPersistenceStatusMessage($"Cannot add anchor as is not an Image Tracking type - [{searchArg.spatialPersistenceTrackingType}]");
                    return;
                }
                if (!searchArg.IsValid)
                {
                    OnSpatialPersistenceError("Cannot add anchor as no id/Texture was provided");
                    continue;
                }
                OnFindAnchorStarted();
                AwaiterExtensions.RunCoroutine(AddToLibrary(runtimeImageLibrary, searchArg.anchorID, searchArg.texture, searchArg.url));
            }
        }

        /// <inheritdoc />
        public override void ResetAnchors(params string[] ids)
        {
            //Skip for now
        }
        #endregion ISpatialPersistenceServiceModule

        #region IARFoundationImageTrackingModule implementation
        /// <inheritdoc />
        public ARFoundationTrackableImagesLibrary TrackedImagesLibrary => profile.TrackedImagesLibrary;
        #endregion IARFoundationImageTrackingModule implementation

        #region Private Methods
        private IEnumerator AddToLibrary(MutableRuntimeReferenceImageLibrary mutableLibrary, string anchorID, Texture2D texture = null, string url = "")
        {
            var anchorGuid = new Guid(anchorID);
            if (string.IsNullOrEmpty(anchorID) || anchorGuid == Guid.Empty)
            {
                OnSpatialPersistenceError($"Invalid Anchor ID provided [{anchorID}]");
                yield return null;
            }

            if (mutableLibrary is null)
            {
                OnSpatialPersistenceError($"Library is inaccessible.");
                yield return null;
            }
            if (trackedImageIds.Contains(anchorGuid))
            {
                OnAnchorLocatedError(anchorID, $"Anchor with Guid [{anchorID}] already exists.");
                yield return null;
            }
            if (texture.IsNull() && string.IsNullOrEmpty(url))
            {
                OnSpatialPersistenceError($"No Source Image texture or URL provided, nothing to add.");
                yield return null;
            }
            if (texture.IsNotNull() && !texture.isReadable)
            {
                OnSpatialPersistenceError($"Unable to add image [{texture?.name}] to library as the texture is not readable\nSet Read/Write on the source image.");
                yield return null;
            }

            dynamicLibraryManager.ProcessImage(mutableLibrary, anchorGuid, texture, url);
        }

#if ARFOUNDATION_6
        private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> trackedImage)
#else
        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs trackedImage)
#endif
        {
            foreach (var newImage in trackedImage.added)
            {
                if (trackedImageIds.Contains(newImage.referenceImage.guid))
                {
                    OnAnchorLocatedError(newImage.referenceImage.guid.ToString(), $"Tracked Image returned but no corresponding Guid Target found, available Images [{TrackedImageManager.referenceLibrary.count}]");
                }
                else
                {
                    trackedImageIds.Add(newImage.referenceImage.guid);
                    var trackedReference = TrackedImagesLibrary.GetTrackedImageByName(newImage.referenceImage.name);
                    if (trackedReference != null)
                    {
                        trackedImageReferences.TryAdd(newImage.referenceImage.guid, trackedReference.SourceGuid);
                        OnAnchorLocated(trackedImageReferences[newImage.referenceImage.guid].ToString(), newImage.transform.gameObject);
                    }
                    else
                    {
                        OnAnchorLocated(newImage.referenceImage.guid.ToString(), newImage.transform.gameObject);
                    }
                }
            }

            foreach (var updatedImage in trackedImage.updated)
            {
                if (trackedImageIds.Contains(updatedImage.referenceImage.guid))
                {
                    Guid refGuid = updatedImage.referenceImage.guid;
                    if (trackedImageReferences.TryGetValue(updatedImage.referenceImage.guid, out var trackedGuid))
                    {
                        refGuid = trackedGuid;
                    }
                    OnAnchorUpdated(refGuid.ToString(), updatedImage.transform.gameObject);
                }
                else
                {
                    OnAnchorLocatedError(updatedImage.referenceImage.guid.ToString(), $"Tracked Image returned but no corresponding Guid Target found, available Images [{TrackedImageManager.referenceLibrary.count}]");
                }
            }

#if ARFOUNDATION_6
            foreach (var (id, removedImage) in trackedImage.removed)
#else
            foreach (var removedImage in trackedImage.removed)
#endif
            {
                if (trackedImageIds.Contains(removedImage.referenceImage.guid))
                {
                    Guid refGuid = removedImage.referenceImage.guid;
                    if (trackedImageReferences.TryGetValue(removedImage.referenceImage.guid, out var trackedGuid))
                    {
                        refGuid = trackedGuid;
                    }
                    OnAnchorDeleted(refGuid.ToString());
                    trackedImageIds.Remove(removedImage.referenceImage.guid);
                }
                else
                {
                    OnAnchorLocatedError(removedImage.referenceImage.guid.ToString(), $"Tracked Image returned but no corresponding Guid Target found, available Images [{TrackedImageManager.referenceLibrary.count}]");
                }
            }
        }

        private void OnImageLoaded(ARFoundationTrackedImageData data)
        {
            if (HasValidTrackingProfile && TrackedImagesLibrary.GetTrackedImageByName(data.Name) == null)
            {
                TrackedImagesLibrary.AddTrackedImageData(data);
            }
            OnCreateAnchorSucceeded(data.SourceGuid.ToString(), null);
            OnSpatialPersistenceStatusMessage($"Image Loaded: {data.Name}, Image Count: {trackedImageManager.referenceLibrary.count}");
        }

        private void OnImageLoadFailed(string message)
        {
            OnSpatialPersistenceError(message);
        }
        #endregion Private Methods
    }
}