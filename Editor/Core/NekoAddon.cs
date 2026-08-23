using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    /// <summary>
    /// Marks a class as a NekoSune addon so the Hub can list it without a hard reference.
    /// Drop a new addon file into the package, tag it, and it shows up in the menu and Hub.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class NekoAddonAttribute : Attribute
    {
        public int Order = 100;
    }

    internal interface INekoAddon
    {
        /// <summary>Stable id, used for prefs keys.</summary>
        string Id { get; }
        /// <summary>Localization key for the display name.</summary>
        string TitleKey { get; }
        /// <summary>Localization key for the one-line description.</summary>
        string DescriptionKey { get; }
        /// <summary>Localization key for the Hub category header.</summary>
        string CategoryKey { get; }
        /// <summary>Short glyph shown on the Hub card.</summary>
        string Glyph { get; }
        /// <summary>True when the addon is usable in this project (SDK present, etc.).</summary>
        bool IsAvailable { get; }
        void Open();
    }

    internal static class NekoAddonRegistry
    {
        static List<INekoAddon> _addons;

        public static IList<INekoAddon> All
        {
            get
            {
                if (_addons == null) Scan();
                return _addons;
            }
        }

        public static void Refresh() { _addons = null; }

        static void Scan()
        {
            var found = new List<KeyValuePair<int, INekoAddon>>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Type[] types;
                try { types = assemblies[a].GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch (Exception) { continue; }
                if (types == null) continue;

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(INekoAddon).IsAssignableFrom(type)) continue;

                    var attr = (NekoAddonAttribute)Attribute.GetCustomAttribute(type, typeof(NekoAddonAttribute));
                    if (attr == null) continue;

                    try
                    {
                        var inst = (INekoAddon)Activator.CreateInstance(type);
                        found.Add(new KeyValuePair<int, INekoAddon>(attr.Order, inst));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[NekoSune] Could not instantiate addon " + type.FullName + ": " + e.Message);
                    }
                }
            }

            found.Sort((x, y) =>
            {
                int c = x.Key.CompareTo(y.Key);
                return c != 0 ? c : string.Compare(x.Value.Id, y.Value.Id, StringComparison.OrdinalIgnoreCase);
            });

            _addons = new List<INekoAddon>(found.Count);
            for (int i = 0; i < found.Count; i++) _addons.Add(found[i].Value);
        }
    }
}
