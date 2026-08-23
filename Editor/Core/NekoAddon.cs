using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NekoSune.Worlds.Editor
{
    /// <summary>
    /// Marks a world editor tool so the NekoSune Worlds Hub can discover it automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class NekoAddonAttribute : Attribute
    {
        public int Order = 100;
    }

    internal interface INekoAddon
    {
        string Id { get; }
        string TitleKey { get; }
        string DescriptionKey { get; }
        string CategoryKey { get; }
        string Glyph { get; }
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

        public static void Refresh()
        {
            _addons = null;
        }

        static void Scan()
        {
            var found = new List<KeyValuePair<int, INekoAddon>>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int a = 0; a < assemblies.Length; a++)
            {
                Type[] types;
                try
                {
                    types = assemblies[a].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                catch
                {
                    continue;
                }

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
                        var addon = (INekoAddon)Activator.CreateInstance(type);
                        found.Add(new KeyValuePair<int, INekoAddon>(attr.Order, addon));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[NekoSune Worlds] Could not instantiate addon " + type.FullName + ": " + e.Message);
                    }
                }
            }

            found.Sort((x, y) =>
            {
                int byOrder = x.Key.CompareTo(y.Key);
                return byOrder != 0
                    ? byOrder
                    : string.Compare(x.Value.Id, y.Value.Id, StringComparison.OrdinalIgnoreCase);
            });

            _addons = new List<INekoAddon>(found.Count);
            for (int i = 0; i < found.Count; i++)
                _addons.Add(found[i].Value);
        }
    }
}
