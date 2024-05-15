// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using RealityCollective.ServiceFramework.Editor;
using RealityCollective.ServiceFramework.Editor.Packages;
using RealityCollective.Utilities.Editor;
using RealityCollective.Utilities.Extensions;
using System.IO;
using UnityEditor;

namespace RealityToolkit.SpatialPersistence.ARFoundation.Editor
{
    [InitializeOnLoad]
    internal static class SpatialPersistence_ARFoundationPackageInstaller
    {
        public const string HIDDEN_PACKAGE_ASSETS_PATH = "Assets~"; 
        public const string Editor_Menu_Keyword = "Tools/Reality Toolkit";
        private const string assetImportPath = "Assets/RealityToolkit.Generated/";
        private static readonly string destinationPath = Path.Combine(assetImportPath, "SpatialPersistence_ARFoundation");
        private static readonly string sourcePath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(SpatialPersistence_ARFoundationPackagePathFinder)).ForwardSlashes()}{Path.DirectorySeparatorChar}{HIDDEN_PACKAGE_ASSETS_PATH}");

        static SpatialPersistence_ARFoundationPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
        }

        [MenuItem(Editor_Menu_Keyword + "/Packages / Install SpatialPersistence_ARFoundation Package Assets...", true)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{destinationPath}{Path.DirectorySeparatorChar}");
        }

        [MenuItem(Editor_Menu_Keyword + "/Packages / Install SpatialPersistence_ARFoundation Package Assets...")]
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