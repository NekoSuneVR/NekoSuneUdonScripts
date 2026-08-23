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
            var window = GetWindow<NekoHubWindow>(false, "NekoSune Worlds", true);
            window.minSize = new Vector2(440f, 340f);
            window.Show();
        }

        void OnEnable()
        {
            NekoLoc.LanguageChanged += Repaint;
        }

        void OnDisable()
        {
            NekoLoc.LanguageChanged -= Repaint;
        }

        static string Version
        {
            get
            {
                if (!string.IsNullOrEmpty(_version)) return _version;
                _version = "0.0.0";

                try
                {
                    string path = NekoPaths.ToAbsolute(NekoPaths.Root + "/package.json");
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        Match match = Regex.Match(File.ReadAllText(path), "\"version\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) _version = match.Groups[1].Value;
                    }
                }
                catch
                {
                    // Version display is cosmetic only.
                }

                return _version;
            }
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header(NekoLoc.T("hub.title"), NekoLoc.T("hub.subtitle"));
            DrawLanguagePicker();

            GUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            IList<INekoAddon> addons = NekoAddonRegistry.All;
            if (addons.Count == 0)
            {
                EditorGUILayout.HelpBox(NekoLoc.T("hub.empty"), MessageType.Info);
            }
            else
            {
                string previousCategory = null;
                for (int i = 0; i < addons.Count; i++)
                {
                    INekoAddon addon = addons[i];
                    string category = NekoLoc.T(addon.CategoryKey);
                    if (category != previousCategory)
                    {
                        GUILayout.Space(6f);
                        GUILayout.Label(category.ToUpperInvariant(), EditorStyles.miniBoldLabel);
                        previousCategory = category;
                    }

                    DrawAddon(addon);
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("NekoSune Worlds v" + Version, NekoStyles.Subtitle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(NekoLoc.T("hub.reloadLanguages"), EditorStyles.miniButton))
            {
                NekoLoc.Reload();
                NekoAddonRegistry.Refresh();
            }
            EditorGUILayout.EndHorizontal();
        }

        static void DrawLanguagePicker()
        {
            List<NekoLanguageInfo> languages = NekoLoc.Languages;
            if (languages == null || languages.Count <= 1) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(NekoLoc.T("common.language"), EditorStyles.miniLabel);

            string[] names = new string[languages.Count];
            int current = 0;
            for (int i = 0; i < languages.Count; i++)
            {
                names[i] = languages[i].Display;
                if (languages[i].Code == NekoLoc.ActiveCode) current = i;
            }

            int selected = EditorGUILayout.Popup(current, names, GUILayout.Width(190f));
            if (selected != current)
                NekoLoc.SetLanguage(languages[selected].Code);

            EditorGUILayout.EndHorizontal();
        }

        static void DrawAddon(INekoAddon addon)
        {
            EditorGUILayout.BeginHorizontal(NekoStyles.Card);

            GUILayout.Label(addon.Glyph, EditorStyles.boldLabel, GUILayout.Width(28f));
            EditorGUILayout.BeginVertical();
            GUILayout.Label(NekoLoc.T(addon.TitleKey), NekoStyles.CardTitle);
            GUILayout.Label(NekoLoc.T(addon.DescriptionKey), NekoStyles.CardDescription);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!addon.IsAvailable))
            {
                if (GUILayout.Button(NekoLoc.T("hub.open"), NekoStyles.PrimaryButton, GUILayout.Width(80f)))
                    addon.Open();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
