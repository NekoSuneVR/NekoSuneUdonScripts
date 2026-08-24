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
                    string abs = NekoPaths.ToAbsolute(NekoPaths.Root + "/package.json");
                    if (abs != null && File.Exists(abs))
                    {
                        Match m = Regex.Match(File.ReadAllText(abs), "\"version\"\\s*:\\s*\"([^\"]+)\"");
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
            NekoStyles.HeaderBar("World Hub", "NekoSune", "Install addon packages and they appear here automatically");
            DrawLanguageBar();
            NekoStyles.Rule(2f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            IList<INekoAddon> addons = NekoAddonRegistry.All;
            if (addons.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No World addons are installed yet. Install NekoSune World Tools, World UI Builder, Optimizer, Doctors or Converters from the shared VCC repository.",
                    MessageType.Info);
            }
            else
            {
                string last = null;
                for (int i = 0; i < addons.Count; i++)
                {
                    INekoAddon addon = addons[i];
                    string cat = NekoLoc.T(addon.CategoryKey);
                    if (cat != last)
                    {
                        GUILayout.Space(6f);
                        GUILayout.Label(cat.ToUpperInvariant(), EditorStyles.miniBoldLabel);
                        last = cat;
                    }
                    DrawAddonCard(addon);
                }
            }

            GUILayout.Space(10f);
            EditorGUILayout.EndScrollView();
            NekoStyles.Rule(2f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("World Hub v" + Version, NekoStyles.Subtitle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("About", EditorStyles.miniButton)) NekoWorldAboutWindow.Open();
            if (GUILayout.Button("Refresh addons", EditorStyles.miniButton))
            {
                NekoAddonRegistry.Refresh();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawLanguageBar()
        {
            List<NekoLanguageInfo> langs = NekoLoc.Languages;
            if (langs == null || langs.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(NekoLoc.T("common.language"), NekoStyles.Subtitle);

            string[] names = new string[langs.Count];
            int current = 0;
            for (int i = 0; i < langs.Count; i++)
            {
                names[i] = langs[i].Display;
                if (langs[i].Code == NekoLoc.ActiveCode) current = i;
            }

            int picked = EditorGUILayout.Popup(current, names, GUILayout.Width(180f));
            if (picked != current) NekoLoc.SetLanguage(langs[picked].Code);
            EditorGUILayout.EndHorizontal();
        }

        static void DrawAddonCard(INekoAddon addon)
        {
            EditorGUILayout.BeginHorizontal(NekoStyles.Card);
            GUILayout.Label(addon.Glyph, NekoStyles.IconBig, GUILayout.Width(34f), GUILayout.Height(38f));

            EditorGUILayout.BeginVertical();
            GUILayout.Label(NekoLoc.T(addon.TitleKey), NekoStyles.SlotName);
            GUILayout.Label(NekoLoc.T(addon.DescriptionKey), NekoStyles.SlotMeta);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!addon.IsAvailable))
            {
                if (GUILayout.Button(NekoLoc.T("hub.open"), NekoStyles.PrimaryButton, GUILayout.Height(28f)))
                    addon.Open();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
