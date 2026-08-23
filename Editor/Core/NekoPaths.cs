using System.IO;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    /// <summary>
    /// Resolves where this package physically lives, whether it was installed through
    /// VCC/UPM (Packages/com.nekosune.avatars) or simply dropped into Assets/.
    /// </summary>
    internal static class NekoPaths
    {
        public const string PackageName = "com.nekosune.avatars";
        public const string MenuRoot    = "NekoSune/";

        static string _root;

        /// <summary>Asset-database style path to the package root, e.g. "Packages/com.nekosune.avatars".</summary>
        public static string Root
        {
            get
            {
                if (!string.IsNullOrEmpty(_root) && DirectoryExists(_root)) return _root;
                _root = Resolve();
                return _root;
            }
        }

        public static string Editor        { get { return Root + "/Editor"; } }
        public static string LanguagesDir  { get { return Editor + "/Localization/Languages"; } }

        /// <summary>Absolute on-disk path for an asset-relative path.</summary>
        public static string ToAbsolute(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (Path.IsPathRooted(assetPath)) return assetPath;
            // Application.dataPath ends with "/Assets"
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        static bool DirectoryExists(string assetPath)
        {
            string abs = ToAbsolute(assetPath);
            return !string.IsNullOrEmpty(abs) && Directory.Exists(abs);
        }

        static string Resolve()
        {
            // 1. Standard package install location.
            string guess = "Packages/" + PackageName;
            if (DirectoryExists(guess)) return guess;

            // 2. Find this very script and walk up to the folder that holds package.json
            //    (or, failing that, the folder that contains "Editor").
            string[] guids = AssetDatabase.FindAssets("NekoPaths t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(p) || !p.EndsWith("/NekoPaths.cs")) continue;

                // .../Editor/Core/NekoPaths.cs  ->  ...
                string dir = p;
                for (int up = 0; up < 6; up++)
                {
                    int slash = dir.LastIndexOf('/');
                    if (slash < 0) break;
                    dir = dir.Substring(0, slash);
                    string abs = ToAbsolute(dir);
                    if (abs != null && File.Exists(Path.Combine(abs, "package.json"))) return dir;
                    if (abs != null && Directory.Exists(Path.Combine(abs, "Editor/Localization/Languages"))) return dir;
                }
            }

            Debug.LogWarning("[NekoSune] Could not resolve the package root. Falling back to Assets/NekoSune.");
            return "Assets/NekoSune";
        }
    }
}
