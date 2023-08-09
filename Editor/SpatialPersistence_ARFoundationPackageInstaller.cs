// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.Editor.Utilities;
using RealityCollective.Extensions;
using RealityCollective.ServiceFramework;
using RealityCollective.ServiceFramework.Editor;
using RealityCollective.ServiceFramework.Editor.Packages;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealityToolkit.SpatialPersistence.ARFoundation.Editor
{
    [InitializeOnLoad]
    internal static class SpatialPersistence_ARFoundationPackageInstaller
    {
        private static readonly string destinationPath = Application.dataPath + "/RealityToolkit.Generated/SpatialPersistence_ARFoundation";
        private static readonly string sourcePath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(SpatialPersistence_ARFoundationPackagePathFinder)).ForwardSlashes()}{Path.DirectorySeparatorChar}{"Assets~"}");

        static SpatialPersistence_ARFoundationPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
        }

        [MenuItem(ServiceFrameworkPreferences.Editor_Menu_Keyword + "/Reality Toolkit/Packages/Install SpatialPersistence_ARFoundation Package Assets...", true)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{destinationPath}{Path.DirectorySeparatorChar}");
        }

        [MenuItem(ServiceFrameworkPreferences.Editor_Menu_Keyword + "/Reality Toolkit/Packages/Install SpatialPersistence_ARFoundation Package Assets...")]
        private static void ImportPackageAssets()
        {
            EditorPreferences.Set($"{nameof(SpatialPersistence_ARFoundationPackageInstaller)}.Assets", false);
            EditorApplication.delayCall += CheckPackage;
        }

        private static void CheckPackage()
        {
            if (!EditorPreferences.Get($"{nameof(SpatialPersistence_ARFoundationPackageInstaller)}.Assets", false))
            {
                EditorPreferences.Set($"{nameof(SpatialPersistence_ARFoundationPackageInstaller)}.Assets", AssetsInstaller.TryInstallAssets(sourcePath, destinationPath));
            }
        }
    }
}
