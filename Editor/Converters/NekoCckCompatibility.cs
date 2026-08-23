using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    internal enum NekoCckGeneration
    {
        Missing,
        Cck3Legacy,
        Cck4Stable,
        CompatibleUnknown
    }

    /// <summary>
    /// Runtime/reflection compatibility layer for ChilloutVR CCK 3 legacy and CCK 4 stable.
    /// The avatar package never takes a hard assembly dependency on either CCK generation.
    /// </summary>
    internal static class NekoCckCompatibility
    {
        public static NekoCckGeneration Generation
        {
            get
            {
#if CVR_CCK_4_OR_NEWER
                return NekoCckGeneration.Cck4Stable;
#elif CVR_CCK_EXISTS
                return NekoCckGeneration.Cck3Legacy;
#else
                if (AssetDatabase.IsValidFolder("Assets/CVR.CCK")) return NekoCckGeneration.Cck4Stable;
                if (AssetDatabase.IsValidFolder("Assets/ABI.CCK")) return NekoCckGeneration.Cck3Legacy;
                if (FindType("ABI.CCK.Components.CVRAvatar", "CVR.CCK.Components.CVRAvatar") != null)
                    return NekoCckGeneration.CompatibleUnknown;
                return NekoCckGeneration.Missing;
#endif
            }
        }

        public static bool Installed { get { return Generation != NekoCckGeneration.Missing; } }

        public static string DisplayName
        {
            get
            {
                switch (Generation)
                {
                    case NekoCckGeneration.Cck4Stable: return "CCK 4 (stable)";
                    case NekoCckGeneration.Cck3Legacy: return "CCK 3 (legacy)";
                    case NekoCckGeneration.CompatibleUnknown: return "Compatible CCK (generation unknown)";
                    default: return "Not installed";
                }
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                Type t = FindType("ABI.CCK.Components.CVRAvatar", "CVR.CCK.Components.CVRAvatar",
                                  "ABI.CCK.Components.CVRWorld", "CVR.CCK.Components.CVRWorld");
                if (t == null || t.Assembly == null) return "";
                try
                {
                    AssemblyName n = t.Assembly.GetName();
                    return n.Version == null ? "" : n.Version.ToString();
                }
                catch { return ""; }
            }
        }

        public static Type FindType(params string[] names)
        {
            Type t = NekoAvatarDiagnosticsUtil.FindType(names);
            if (t != null) return t;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i])) continue;
                string swapped = names[i].StartsWith("ABI.CCK.", StringComparison.Ordinal)
                    ? "CVR.CCK." + names[i].Substring("ABI.CCK.".Length)
                    : names[i].StartsWith("CVR.CCK.", StringComparison.Ordinal)
                        ? "ABI.CCK." + names[i].Substring("CVR.CCK.".Length)
                        : null;
                if (string.IsNullOrEmpty(swapped)) continue;
                t = NekoAvatarDiagnosticsUtil.FindType(swapped);
                if (t != null) return t;
            }
            return null;
        }

        public static Type AssetInfoType { get { return FindType("ABI.CCK.Components.CVRAssetInfo", "CVR.CCK.Components.CVRAssetInfo"); } }
        public static Type AvatarType { get { return FindType("ABI.CCK.Components.CVRAvatar", "CVR.CCK.Components.CVRAvatar"); } }
        public static Type WorldType { get { return FindType("ABI.CCK.Components.CVRWorld", "CVR.CCK.Components.CVRWorld"); } }
        public static Type SpawnableType { get { return FindType("ABI.CCK.Components.CVRSpawnable", "CVR.CCK.Components.CVRSpawnable"); } }
        public static Type PickupType { get { return FindType("ABI.CCK.Components.CVRPickupObject", "CVR.CCK.Components.CVRPickupObject"); } }
        public static Type ObjectSyncType { get { return FindType("ABI.CCK.Components.CVRObjectSync", "CVR.CCK.Components.CVRObjectSync"); } }
        public static Type InteractableType { get { return FindType("ABI.CCK.Components.CVRInteractable", "CVR.CCK.Components.CVRInteractable"); } }
        public static Type InteractableActionType { get { return FindType("ABI.CCK.Components.CVRInteractableAction", "CVR.CCK.Components.CVRInteractableAction"); } }
        public static Type InteractableOperationType { get { return FindType("ABI.CCK.Components.CVRInteractableActionOperation", "CVR.CCK.Components.CVRInteractableActionOperation"); } }

        public static Component EnsureComponent(GameObject root, Type type)
        {
            if (root == null || type == null || !typeof(Component).IsAssignableFrom(type)) return null;
            Component c = root.GetComponent(type);
            if (c != null) return c;
            try { return Undo.AddComponent(root, type); }
            catch { return root.AddComponent(type); }
        }

        public static bool IsVrcComponent(Component c)
        {
            if (c == null) return false;
            string full = c.GetType().FullName ?? c.GetType().Name;
            return full.StartsWith("VRC.", StringComparison.Ordinal) ||
                   full.StartsWith("VRCSDK", StringComparison.Ordinal) ||
                   full.IndexOf("UdonBehaviour", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void DrawStatusBox()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("ChilloutVR CCK", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Detected generation", DisplayName);
            string version = AssemblyVersion;
            if (!string.IsNullOrEmpty(version)) EditorGUILayout.LabelField("Assembly version", version);
            if (Generation == NekoCckGeneration.Cck3Legacy)
                EditorGUILayout.HelpBox("CCK 3 is supported as a legacy target. ChilloutVR recommends CCK 4 for active development.", MessageType.Info);
            else if (Generation == NekoCckGeneration.Cck4Stable)
                EditorGUILayout.HelpBox("CCK 4 stable detected. NekoSune uses the current CCK component model and does not rely on the removed legacy upload panel.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }
    }
}
