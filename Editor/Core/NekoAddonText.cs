namespace NekoSune.Avatars.Editor
{
    /// <summary>
    /// Provides English fallback labels for newly added addons while allowing normal JSON
    /// localization files to override them as translations are added.
    /// </summary>
    internal static class NekoAddonText
    {
        public static string T(string key)
        {
            string localized = NekoLoc.T(key);
            if (localized != key) return localized;

            switch (key)
            {
                case "doctor.title": return "Avatar Doctor";
                case "doctor.desc": return "Full upload preflight: descriptor, menus, parameters, animators, performance and Quest compatibility";
                case "quest.title": return "PC → Quest Assistant";
                case "quest.desc": return "Create a mobile copy, convert shaders, apply Android texture overrides and show Quest blockers";
                case "physbone.title": return "PhysBone Doctor";
                case "physbone.desc": return "Find expensive chains, unused colliders, collision-check cost and merge candidates";
                case "texture.title": return "VRAM / Texture Inspector";
                case "texture.desc": return "Sort avatar textures by memory cost and create Android-only resolution overrides";
                case "face.title": return "Face Tracking Doctor";
                case "face.desc": return "Check ARKit/Unified Expression coverage and install core VRCFaceTracking v2 parameters";
                case "exprdoctor.title": return "Expression + Animator Doctor";
                case "exprdoctor.desc": return "Audit expression menus, parameter types/budget, Animator states, transitions and Parameter Drivers";
                case "resonite.title": return "Export to Resonite";
                case "resonite.desc": return "Build a .resonitepackage through the installed Modular Avatar Resonite backend";
                case "cvr.title": return "Convert Avatar to ChilloutVR";
                case "cvr.desc": return "CCK 3/4 avatar conversion with CVR Advanced Avatar Settings, toggles, sliders and dropdowns";
                case "cvrprop.title": return "Convert to ChilloutVR Prop";
                case "cvrprop.desc": return "Turn a Unity/VRChat object into a CCK 3/4 CVR Spawnable with pickup and object-sync conversion";
                default: return key;
            }
        }
    }
}
