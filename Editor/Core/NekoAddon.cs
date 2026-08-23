using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    /// <summary>Marks an addon card for automatic discovery by the Avatar Hub.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NekoAddonAttribute : Attribute
    {
        public int Order = 100;
    }

    /// <summary>
    /// Public addon contract. Any separately packaged assembly can implement this interface,
    /// decorate the class with NekoAddon, and appear in the Avatar Hub automatically.
    /// </summary>
    public interface INekoAddon
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
        public static IList<INekoAddon> All { get { if (_addons == null) Scan(); return _addons; } }
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
                catch { continue; }
                if (types == null) continue;

                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (type == null || type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(INekoAddon).IsAssignableFrom(type)) continue;
                    var attr = (NekoAddonAttribute)Attribute.GetCustomAttribute(type, typeof(NekoAddonAttribute));
                    if (attr == null) continue;
                    try { found.Add(new KeyValuePair<int, INekoAddon>(attr.Order, (INekoAddon)Activator.CreateInstance(type))); }
                    catch (Exception e) { Debug.LogWarning("[NekoSune Avatar Hub] Could not load addon " + type.FullName + ": " + e.Message); }
                }
            }

            found.Sort((x, y) => { int c = x.Key.CompareTo(y.Key); return c != 0 ? c : string.Compare(x.Value.Id, y.Value.Id, StringComparison.OrdinalIgnoreCase); });
            _addons = new List<INekoAddon>(found.Count);
            for (int i = 0; i < found.Count; i++) _addons.Add(found[i].Value);
        }
    }
}
