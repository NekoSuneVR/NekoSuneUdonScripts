using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Worlds.Editor
{
    internal sealed class NekoHubWindow : EditorWindow
    {
        Vector2 _scroll;
        static string _version;

        [MenuItem(NekoPaths.MenuRoot + "World/Hub", false, 0)]
        public static void Open()
        {
            var w = GetWindow<NekoHubWindow>(false, "NekoSune World Hub", true);
            w.minSize = new Vector2(440f, 360f);
            w.Show();
        }

        void OnEnable() { NekoLoc.LanguageChanged += Repaint; NekoAddonRegistry.Refresh(); }
        void OnDisable() { NekoLoc.LanguageChanged -= Repaint; }

        static string Version
        {
            get
            {
                if (!string.IsNullOrEmpty(_version)) return _version;
                _version = "0.0.0";
                try
                {
                    string p = NekoPaths.ToAbsolute(NekoPaths.Root + "/package.json");
                    if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    {
                        Match m = Regex.Match(File.ReadAllText(p), "\"version\"\\s*:\\s*\"([^\"]+)\"");
                        if (m.Success) _version = m.Groups[1].Value;
                    }
                }
                catch { }
                return _version;
            }
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header("NekoSune World Hub", "Install World addons and they appear here automatically");
            DrawLanguagePicker();
            GUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            IList<INekoAddon> addons = NekoAddonRegistry.All;
            if (addons.Count == 0)
                EditorGUILayout.HelpBox("No World addons are installed yet. Install NekoSune World Tools, Optimizer, Doctors or Converters from the shared VCC repository.", MessageType.Info);
            else
            {
                string last = null;
                for (int i = 0; i < addons.Count; i++)
                {
                    INekoAddon addon = addons[i];
                    string category = NekoLoc.T(addon.CategoryKey);
                    if (category != last) { GUILayout.Space(6f); GUILayout.Label(category.ToUpperInvariant(), EditorStyles.miniBoldLabel); last = category; }
                    DrawAddon(addon);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("World Hub v" + Version, NekoStyles.Subtitle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("About", EditorStyles.miniButton)) NekoWorldAboutWindow.Open();
            if (GUILayout.Button("Refresh addons", EditorStyles.miniButton)) { NekoAddonRegistry.Refresh(); Repaint(); }
            EditorGUILayout.EndHorizontal();
        }

        static void DrawLanguagePicker()
        {
            List<NekoLanguageInfo> languages = NekoLoc.Languages;
            if (languages == null || languages.Count <= 1) return;
            EditorGUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label(NekoLoc.T("common.language"), EditorStyles.miniLabel);
            string[] names = new string[languages.Count]; int current = 0;
            for (int i = 0; i < languages.Count; i++) { names[i] = languages[i].Display; if (languages[i].Code == NekoLoc.ActiveCode) current = i; }
            int selected = EditorGUILayout.Popup(current, names, GUILayout.Width(190f));
            if (selected != current) NekoLoc.SetLanguage(languages[selected].Code);
            EditorGUILayout.EndHorizontal();
        }

        static void DrawAddon(INekoAddon addon)
        {
            EditorGUILayout.BeginHorizontal(NekoStyles.Card);
            GUILayout.Label(addon.Glyph, EditorStyles.boldLabel, GUILayout.Width(28f));
            EditorGUILayout.BeginVertical(); GUILayout.Label(NekoLoc.T(addon.TitleKey), NekoStyles.CardTitle); GUILayout.Label(NekoLoc.T(addon.DescriptionKey), NekoStyles.CardDescription); EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!addon.IsAvailable)) if (GUILayout.Button(NekoLoc.T("hub.open"), NekoStyles.PrimaryButton, GUILayout.Width(80f))) addon.Open();
            EditorGUILayout.EndHorizontal();
        }
    }
}
