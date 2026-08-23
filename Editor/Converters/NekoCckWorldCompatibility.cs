using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Worlds.Editor
{
    internal enum NekoCckGeneration
    {
        Missing,
        Cck3Legacy,
        Cck4Stable,
        CompatibleUnknown
    }

    internal static class NekoCckWorldCompatibility
    {
        const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

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
                if (WorldType != null || SpawnableType != null) return NekoCckGeneration.CompatibleUnknown;
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
                    case NekoCckGeneration.CompatibleUnknown: return "Compatible CCK";
                    default: return "Not installed";
                }
            }
        }

        public static Type FindType(params string[] names)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int n = 0; n < names.Length; n++)
            {
                string wanted = names[n];
                if (string.IsNullOrEmpty(wanted)) continue;
                for (int a = 0; a < assemblies.Length; a++)
                {
                    try
                    {
                        Type t = assemblies[a].GetType(wanted, false);
                        if (t != null) return t;
                    }
                    catch { }
                }

                string swapped = wanted.StartsWith("ABI.CCK.", StringComparison.Ordinal)
                    ? "CVR.CCK." + wanted.Substring("ABI.CCK.".Length)
                    : wanted.StartsWith("CVR.CCK.", StringComparison.Ordinal)
                        ? "ABI.CCK." + wanted.Substring("CVR.CCK.".Length)
                        : null;
                if (!string.IsNullOrEmpty(swapped))
                {
                    for (int a = 0; a < assemblies.Length; a++)
                    {
                        try
                        {
                            Type t = assemblies[a].GetType(swapped, false);
                            if (t != null) return t;
                        }
                        catch { }
                    }
                }
            }
            return null;
        }

        public static Type WorldType { get { return FindType("ABI.CCK.Components.CVRWorld", "CVR.CCK.Components.CVRWorld"); } }
        public static Type SpawnableType { get { return FindType("ABI.CCK.Components.CVRSpawnable", "CVR.CCK.Components.CVRSpawnable"); } }
        public static Type PickupType { get { return FindType("ABI.CCK.Components.CVRPickupObject", "CVR.CCK.Components.CVRPickupObject"); } }
        public static Type ObjectSyncType { get { return FindType("ABI.CCK.Components.CVRObjectSync", "CVR.CCK.Components.CVRObjectSync"); } }
        public static Type MirrorType { get { return FindType("ABI.CCK.Components.CVRMirror", "CVR.CCK.Components.CVRMirror"); } }
        public static Type VideoPlayerType { get { return FindType("ABI.CCK.Components.CVRVideoPlayer", "CVR.CCK.Components.CVRVideoPlayer"); } }
        public static Type InteractableType { get { return FindType("ABI.CCK.Components.CVRInteractable", "CVR.CCK.Components.CVRInteractable"); } }
        public static Type InteractableActionType { get { return FindType("ABI.CCK.Components.CVRInteractableAction", "CVR.CCK.Components.CVRInteractableAction"); } }
        public static Type InteractableOperationType { get { return FindType("ABI.CCK.Components.CVRInteractableActionOperation", "CVR.CCK.Components.CVRInteractableActionOperation"); } }

        public static object GetMember(object target, params string[] names)
        {
            if (target == null) return null;
            Type t = target.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = t.GetField(names[i], Members);
                if (f != null) { try { return f.GetValue(target); } catch { } }
                PropertyInfo p = t.GetProperty(names[i], Members);
                if (p != null && p.GetIndexParameters().Length == 0) { try { return p.GetValue(target, null); } catch { } }
            }
            return null;
        }

        public static bool SetMember(object target, object value, params string[] names)
        {
            if (target == null) return false;
            Type t = target.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = t.GetField(names[i], Members);
                if (f != null)
                {
                    try { f.SetValue(target, ConvertFor(value, f.FieldType)); return true; } catch { }
                }
                PropertyInfo p = t.GetProperty(names[i], Members);
                if (p != null && p.CanWrite && p.GetIndexParameters().Length == 0)
                {
                    try { p.SetValue(target, ConvertFor(value, p.PropertyType), null); return true; } catch { }
                }
            }
            return false;
        }

        static object ConvertFor(object value, Type destination)
        {
            if (value == null) return null;
            if (destination.IsAssignableFrom(value.GetType())) return value;
            if (destination.IsEnum)
            {
                if (value is string) return Enum.Parse(destination, (string)value, true);
                return Enum.ToObject(destination, Convert.ToInt32(value));
            }
            return Convert.ChangeType(value, destination);
        }

        public static Component EnsureComponent(GameObject go, Type type)
        {
            if (go == null || type == null || !typeof(Component).IsAssignableFrom(type)) return null;
            Component c = go.GetComponent(type);
            if (c != null) return c;
            try { return Undo.AddComponent(go, type); }
            catch { return go.AddComponent(type); }
        }

        public static bool IsVrcOrUdon(Component component)
        {
            if (component == null) return false;
            string full = component.GetType().FullName ?? component.GetType().Name;
            return full.StartsWith("VRC.", StringComparison.Ordinal) ||
                   full.StartsWith("VRCSDK", StringComparison.Ordinal) ||
                   full.IndexOf("UdonBehaviour", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static IList AsList(object value) { return value as IList; }
    }
}
