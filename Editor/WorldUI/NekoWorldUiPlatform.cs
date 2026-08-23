using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NekoSune.WorldUI.Editor
{
    internal static class NekoWorldUiPlatform
    {
        public static bool HasVRChatWorldSdk { get { return FindType("VRCUiShape", "VRC_UIShape", "VRCSceneDescriptor") != null; } }
        public static bool HasChilloutVR { get { return FindType("CVRCanvasWrapper", "CVRWorld") != null; } }

        public static void ApplyPlatform(GameObject root, NekoWorldUiPlatform platform, List<string> notes)
        {
            if (root == null) return;
            Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
            for (int i = 0; i < selectables.Length; i++)
            {
                Navigation nav = selectables[i].navigation;
                nav.mode = Navigation.Mode.None;
                selectables[i].navigation = nav;
            }

            if (platform == NekoWorldUiPlatform.VRChat || platform == NekoWorldUiPlatform.Both)
                ApplyVRChat(root, notes);
            if (platform == NekoWorldUiPlatform.ChilloutVR || platform == NekoWorldUiPlatform.Both)
                ApplyChilloutVR(root, notes);
        }

        static void ApplyVRChat(GameObject root, List<string> notes)
        {
            Type shape = FindType("VRCUiShape", "VRC_UIShape");
            if (shape == null)
            {
                notes.Add("VRChat Worlds SDK was not detected. The Canvas was generated, but VRC UI Shape could not be added yet.");
                return;
            }
            if (root.GetComponent(shape) == null)
            {
                Undo.AddComponent(root, shape);
                notes.Add("Added VRChat UI Shape to the generated world-space Canvas.");
            }
        }

        static void ApplyChilloutVR(GameObject root, List<string> notes)
        {
            Type wrapper = FindType("CVRCanvasWrapper");
            if (wrapper == null)
            {
                notes.Add("ChilloutVR CCK was not detected. Install CCK 3/4 later and run UI Doctor -> Fix Platform Setup to add CVR Canvas Wrapper.");
                return;
            }
            Component c = root.GetComponent(wrapper);
            if (c == null) c = Undo.AddComponent(root, wrapper);
            if (c != null)
            {
                SetMember(c, 5f, "interactionDistance", "InteractionDistance");
                EditorUtility.SetDirty(c);
                notes.Add("Added ChilloutVR CVR Canvas Wrapper with a 5m interaction distance.");
            }
        }

        public static void WireSafeActions(GameObject root, List<string> notes)
        {
            if (root == null) return;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            int wired = 0;
            int platformActions = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                NekoWorldUiAction action;
                string value;
                if (!TryParseMeta(buttons[i].gameObject.name, out action, out value)) continue;

                if (action == NekoWorldUiAction.EnableObject || action == NekoWorldUiAction.DisableObject)
                {
                    GameObject target = FindSceneObject(value);
                    if (target == null) { notes.Add("Action target '" + value + "' was not found for " + buttons[i].gameObject.name + "."); continue; }
                    UnityEventTools.AddBoolPersistentListener(buttons[i].onClick, target.SetActive, action == NekoWorldUiAction.EnableObject);
                    wired++;
                }
                else if (action == NekoWorldUiAction.PlayAudio || action == NekoWorldUiAction.StopAudio)
                {
                    GameObject target = FindSceneObject(value);
                    AudioSource audio = target == null ? null : target.GetComponent<AudioSource>();
                    if (audio == null) { notes.Add("AudioSource target '" + value + "' was not found."); continue; }
                    UnityAction callback = action == NekoWorldUiAction.PlayAudio ? (UnityAction)audio.Play : (UnityAction)audio.Stop;
                    UnityEventTools.AddPersistentListener(buttons[i].onClick, callback);
                    wired++;
                }
                else if (action == NekoWorldUiAction.ClosePage)
                {
                    UnityEventTools.AddBoolPersistentListener(buttons[i].onClick, root.SetActive, false);
                    wired++;
                }
                else if (action == NekoWorldUiAction.OpenPage)
                {
                    GameObject target = FindSceneObject(value);
                    if (target == null) { notes.Add("Page target '" + value + "' was not found."); continue; }
                    UnityEventTools.AddBoolPersistentListener(buttons[i].onClick, target.SetActive, true);
                    wired++;
                }
                else if (action == NekoWorldUiAction.TeleportPlayer || action == NekoWorldUiAction.RespawnPlayer || action == NekoWorldUiAction.RefreshJson || action == NekoWorldUiAction.AnimatorBool || action == NekoWorldUiAction.AnimatorTrigger || action == NekoWorldUiAction.ToggleObject)
                {
                    platformActions++;
                }
                else if (action == NekoWorldUiAction.OpenLinkCard)
                {
                    notes.Add("Link card kept visible as text: " + value + ". No unsupported generic browser-open API was invented.");
                }
            }
            if (wired > 0) notes.Add("Wired " + wired + " safe Unity UI action(s) automatically.");
            if (platformActions > 0) notes.Add(platformActions + " action(s) need platform runtime wiring (teleport/respawn/toggle/animator/JSON). Use the generated runtime starter pack and the Learn panel.");
        }

        public static bool TryParseMeta(string name, out NekoWorldUiAction action, out string value)
        {
            action = NekoWorldUiAction.None;
            value = "";
            if (string.IsNullOrEmpty(name) || !name.StartsWith("NUI[", StringComparison.Ordinal)) return false;
            int close = name.IndexOf(']');
            if (close < 4) return false;
            string[] parts = name.Substring(4, close - 4).Split('|');
            if (parts.Length < 2) return false;
            try { action = (NekoWorldUiAction)Enum.Parse(typeof(NekoWorldUiAction), parts[1]); } catch { return false; }
            if (parts.Length > 2) value = parts[2];
            return true;
        }

        public static Type FindType(params string[] simpleNames)
        {
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
                    Type t = types[i];
                    if (t == null) continue;
                    for (int n = 0; n < simpleNames.Length; n++)
                        if (string.Equals(t.Name, simpleNames[n], StringComparison.Ordinal) || string.Equals(t.FullName, simpleNames[n], StringComparison.Ordinal)) return t;
                }
            }
            return null;
        }

        public static bool SetMember(object target, object value, params string[] names)
        {
            if (target == null) return false;
            Type t = target.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = t.GetField(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    object converted;
                    if (TryConvert(value, f.FieldType, out converted)) { f.SetValue(target, converted); return true; }
                }
                PropertyInfo p = t.GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanWrite)
                {
                    object converted;
                    if (TryConvert(value, p.PropertyType, out converted)) { p.SetValue(target, converted, null); return true; }
                }
            }
            return false;
        }

        static bool TryConvert(object value, Type destination, out object converted)
        {
            converted = value;
            if (value == null) return !destination.IsValueType;
            if (destination.IsInstanceOfType(value)) return true;
            try
            {
                if (destination.IsEnum && value is string) { converted = Enum.Parse(destination, (string)value, true); return true; }
                converted = Convert.ChangeType(value, destination);
                return true;
            }
            catch { return false; }
        }

        static GameObject FindSceneObject(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].scene.IsValid() && string.Equals(all[i].name, name, StringComparison.OrdinalIgnoreCase)) return all[i];
            return null;
        }
    }
}
