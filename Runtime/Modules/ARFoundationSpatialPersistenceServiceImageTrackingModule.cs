// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Extensions;
using RealityCollective.Utilities.Async;
using RealityToolkit.SpatialPersistence.ARFoundation.Definitions;
using RealityToolkit.SpatialPersistence.ARFoundation.Extensions;
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
        private readonly List<Guid> trackedImageIds = new List<Guid>();
        private readonly Dictionary<Guid, Guid> trackedImageReferences = new Dictionary<Guid, Guid>();
        private bool isStarted = false;
        private bool isStarting = false;

        private ARTrackedImageManager TrackedImageManager
        {
            get
            {
                if (trackedImageManager == null)
                {
                    // Get a reference to the SpatialAnchorManager component (must be on the same gameobject)
                    trackedImageManager = GameObject.FindObjectOfType<ARTrackedImageManager>();
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
                    dynamicLibraryManager = GameObject.FindObjectOfType<ARFoundationDynamicLibraryManager>();
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
                TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
                if (profile.TrackedImagesLibrary.IsNotNull() && profile.TrackedImagesLibrary.TrackedImages != null)
                {
                    DynamicLibraryManager.ProcessImages(runtimeImageLibrary, profile.TrackedImagesLibrary.TrackedImages);
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
                TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
            }
            OnSessionEnded();
        }

        /// <inheritdoc />
        public override void TryFindAnchors(params SpatialPersistenceAnchorArgs[] args)
        {
            if (args == null)
            {
                OnSpatialPersistenceError("Cannot add anchor as none were provided");
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

            foreach (var anchorArg in args)
            {
                OnFindAnchorStarted();
                AwaiterExtensions.RunCoroutine(AddToLibrary(runtimeImageLibrary, anchorArg.guid, anchorArg.texture, anchorArg.url));
            }
        }

        /// <inheritdoc />
        public override void ResetAnchors(params Guid[] ids)
        {
            //Skip for now
        }
        #endregion ISpatialPersistenceServiceModule

        #region IARFoundationImageTrackingModule implementation
        /// <inheritdoc />
        public ARFoundationTrackableImagesLibrary TrackedImagesLibrary => profile.TrackedImagesLibrary;
        #endregion IARFoundationImageTrackingModule implementation

        #region Private Methods
        private IEnumerator AddToLibrary(MutableRuntimeReferenceImageLibrary mutableLibrary, Guid anchorGuid, Texture2D texture = null, string url = "")
        {
            if (mutableLibrary is null)
            {
                OnSpatialPersistenceError($"Library is inaccessble.");
                yield return null;
            }
            if (trackedImageIds.Contains(anchorGuid))
            {
                OnAnchorLocatedError(anchorGuid, $"Anchor with Guid [{anchorGuid}] already exists.");
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

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs trackedImage)
        {
            foreach (var newImage in trackedImage.added)
            {
                if (trackedImageIds.Contains(newImage.referenceImage.guid))
                {
                    OnAnchorLocatedError(newImage.referenceImage.guid, $"Tracked Image returned but no corresponding Guid Target found, available Images [{TrackedImageManager.referenceLibrary.count}]");
                }
                else
                {
                    trackedImageIds.Add(newImage.referenceImage.guid);
                    var trackedReference = profile.TrackedImagesLibrary.GetTrackedImageByName(newImage.referenceImage.name);
                    trackedImageReferences.Add(newImage.referenceImage.guid, trackedReference.SourceGuid);
                    OnAnchorLocated(trackedImageReferences[newImage.referenceImage.guid], newImage.transform.gameObject);
                }
            }

            foreach (var updatedImage in trackedImage.updated)
            {
                if (trackedImageIds.Contains(updatedImage.referenceImage.guid))
                {
                    OnAnchorUpdated(trackedImageReferences[updatedImage.referenceImage.guid], updatedImage.transform.gameObject);
                }
                else
                {
                    OnAnchorLocatedError(updatedImage.referenceImage.guid, $"Tracked Image returned but no corresponding Guid Target found, available Images [{TrackedImageManager.referenceLibrary.count}]");
                }
            }

            foreach (var removedImage in trackedImage.removed)
            {
                if (trackedImageIds.Contains(removedImage.referenceImage.guid))
                {
                    OnAnchorDeleted(trackedImageReferences[removedImage.referenceImage.guid]);
                    trackedImageIds.Remove(removedImage.referenceImage.guid);
                }
                else
                {
                    OnAnchorLocatedError(removedImage.referenceImage.guid, $"Tracked Image returned but no corresponding Guid Target found, available Images [{TrackedImageManager.referenceLibrary.count}]");
                }
            }
        }

        private void OnImageLoaded(ARFoundationTrackedImageData data)
        {
            profile.TrackedImagesLibrary.AddTrackedImageData(data);
            OnCreateAnchorSucceeded(data.SourceGuid, null);
            OnSpatialPersistenceStatusMessage($"Image Loaded: {data.Name}, Image Count: {trackedImageManager.referenceLibrary.count}");
        }

        private void OnImageLoadFailed(string message)
        {
            OnSpatialPersistenceError(message);
        }
        #endregion Private Methods
    }
}