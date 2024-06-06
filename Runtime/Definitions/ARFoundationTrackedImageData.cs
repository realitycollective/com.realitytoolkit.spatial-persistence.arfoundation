// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine.XR.ARSubsystems;

namespace RealityToolkit.SpatialPersistence.Definitions
{
    /// <summary>
    /// ARFoundation Tracked Image Data Construct
    /// </summary>  
    [Serializable]
    public class ARFoundationTrackedImageData : TrackedImageData
    {
        public AddReferenceImageJobState jobState { get; set; }
    }
}