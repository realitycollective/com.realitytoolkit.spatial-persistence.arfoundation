// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Utilities;
using RealityToolkit.SpatialPersistence.Definitions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RealityToolkit.SpatialPersistence.ARFoundation
{
    public class ARFoundationDynamicLibraryManager : MonoBehaviour
    {
        #region Private Properties
        private Queue<ARFoundationTrackedImageData> processingImagesQueue = new Queue<ARFoundationTrackedImageData>();
        private MutableRuntimeReferenceImageLibrary mutableLibrary;
        private List<ARFoundationTrackedImageData> processingImages = new List<ARFoundationTrackedImageData>();
        private ImageTrackingProcessingState m_State;
        #endregion Private Properties

        #region Public Properties
        public Action<ARFoundationTrackedImageData> OnImageLoaded;
        public Action<string> OnImageLoadFailed;
        #endregion Public Properties

        #region MonoBehaviours
        void Update()
        {
            if (processingImagesQueue.Count > 0 && processingImages.Count.Equals(0))
            {
                int imageProcessingCount = processingImagesQueue.Count;
                for (int i = 0; i < imageProcessingCount; i++)
                {
                    processingImages.Add(processingImagesQueue.Dequeue());
                }
                m_State = ImageTrackingProcessingState.AddImagesRequested;
            }

            switch (m_State)
            {
                case ImageTrackingProcessingState.AddImagesRequested:
                    {
                        if (processingImages.Count.Equals(0))
                        {
                            SetError("No images to add.");
                            break;
                        }

                        ARFoundationTrackedImageData processingImage = null;
                        try
                        {
                            foreach (var image in processingImages)
                            {
                                processingImage = image;
                                switch (image.ImageDataType)
                                {
                                    case ImageDataType.Local:
                                        AddLocalImageToLibrary(image);
                                        break;
                                    case ImageDataType.Remote:
                                        StartCoroutine(AddRemoteImageToLibrary(image));
                                        break;
                                    default:
                                        break;
                                }
                            }

                            m_State = ImageTrackingProcessingState.AddingImages;
                        }
                        catch (InvalidOperationException e)
                        {
                            SetError($"ScheduleAddImageJob for image {processingImage.Name} threw exception: {e.Message}");
                            processingImage = null;
                        }

                        if (m_State == ImageTrackingProcessingState.Error)
                        {
                            processingImages.Clear();
                        }
                        break;
                    }
                case ImageTrackingProcessingState.AddingImages:
                    {
                        // Check for completion
                        var done = true;
                        foreach (var image in processingImages)
                        {
                            if (!image.jobState.jobHandle.IsCompleted)
                            {
                                done = false;
                                break;
                            }

                            if (image.jobState.status == AddReferenceImageJobStatus.ErrorUnknown || image.jobState.status == AddReferenceImageJobStatus.ErrorInvalidImage)
                            {
                                SetError($"The image {image.Name} was not loaded due to {image.jobState.status}");
                            }
                            else
                            {
                                OnImageLoaded?.Invoke(image);
                            }
                        }

                        if (done)
                        {
                            m_State = ImageTrackingProcessingState.Done;
                        }

                        if (m_State is ImageTrackingProcessingState.Done || m_State is ImageTrackingProcessingState.Error)
                        {
                            processingImages.Clear();
                        }

                        break;
                    }
            }
        }
        #endregion MonoBehaviours

        #region Public Methods
        public void ProcessImage(MutableRuntimeReferenceImageLibrary mutableLibrary, Guid sourceGuid, Texture2D image = null, string url = "")
        {
            // Null library
            if (mutableLibrary is null)
            {
                StaticLogger.LogError("Cannot process, no library");
                return;
            }

            ProcessImage(mutableLibrary, new ARFoundationTrackedImageData() { SourceGuid = sourceGuid, Name = image?.name, Texture = image, Url = url });
        }

        public void ProcessImages(MutableRuntimeReferenceImageLibrary mutableLibrary, Guid[] sourceGuids, Texture2D[] images, string[] urls)
        {
            // Null library
            if (mutableLibrary is null)
            {
                StaticLogger.LogError("Cannot process, no library");
                return;
            }

            if (!images.Length.Equals(sourceGuids.Length))
            {
                SetError("images and guid lists do not match");
            }
            for (int i = 0; i < images.Length; i++)
            {
                ProcessImage(mutableLibrary, new ARFoundationTrackedImageData() { SourceGuid = sourceGuids[i], Texture = images[i], Url = urls[i] });
            }
        }

        public void ProcessImage(MutableRuntimeReferenceImageLibrary mutableLibrary, ARFoundationTrackedImageData image)
        {
            // Null library
            if (mutableLibrary is null)
            {
                StaticLogger.LogError("Cannot process, no library");
                return;
            }

            this.mutableLibrary = mutableLibrary;

            if (string.IsNullOrEmpty(image.Name))
            {
                image.Name = image.Texture == null ? Path.GetFileNameWithoutExtension(image.Url) : image.Texture.name;
            }

            processingImagesQueue.Enqueue(image);
        }

        public void ProcessImages(MutableRuntimeReferenceImageLibrary mutableLibrary, ARFoundationTrackedImageData[] images)
        {
            // Null library
            if (mutableLibrary is null)
            {
                StaticLogger.LogError("Cannot process, no library");
                return;
            }

            //No Images to process
            if (images is null || images.Length < 1)
            {
                return;
            }

            foreach (var image in images)
            {
                ProcessImage(mutableLibrary, image);
            }
        }
        #endregion Public Methods

        #region Private Methods
        private void SetError(string errorMessage)
        {
            m_State = ImageTrackingProcessingState.Error;
            StaticLogger.LogError($"Error: {errorMessage}");
            OnImageLoadFailed?.Invoke(errorMessage);
        }

        private IEnumerator AddRemoteImageToLibrary(ARFoundationTrackedImageData image, bool wait = true)
        {
            using (UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(image.Url))
            {
                yield return unityWebRequest.SendWebRequest();
                if (unityWebRequest.result != UnityWebRequest.Result.Success)
                {
                    SetError($"The image {image.Name} was not loaded due to {unityWebRequest.error}");
                    yield return null;
                }
                else
                {
                    image.Texture = DownloadHandlerTexture.GetContent(unityWebRequest);
                    if (image.Texture)
                    {
                        image.jobState = mutableLibrary.ScheduleAddImageWithValidationJob(image.Texture, image.Name, image.PhysicalSize > 0 ? image.PhysicalSize : 1);
                        yield return new WaitWhile(() => wait && !image.jobState.jobHandle.IsCompleted);  //Optional wait
                        
                        Destroy(image.Texture);
                    }
                }
            }
            yield return new WaitForEndOfFrame();
        }

        private void AddLocalImageToLibrary(ARFoundationTrackedImageData image)
        {
            if (!image.Texture.isReadable)
            {
                SetError($"Image {image.Name} must be readable to be added to the image library.");
                throw new InvalidOperationException();
            }

            image.jobState = mutableLibrary.ScheduleAddImageWithValidationJob(image.Texture, image.Name, image.PhysicalSize > 0 ? image.PhysicalSize : 1);
        }
        #endregion Private Methods
    }
}