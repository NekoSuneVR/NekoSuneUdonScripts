using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    /// <summary>
    /// The "NekoSune" root window: lists every registered addon so features can be added
    /// to the package without touching this file.
    /// </summary>
    internal class NekoHubWindow : EditorWindow
    {
        Vector2 _scroll;
        static string _version;

        [MenuItem(NekoPaths.MenuRoot + "Hub", false, 0)]
        public static void Open()
        {
            var w = GetWindow<NekoHubWindow>(false, "NekoSune", true);
            w.minSize = new Vector2(420f, 340f);
            w.Show();
        }

        void OnEnable()
        {
            NekoLoc.LanguageChanged += Repaint;
        }

        void OnDisable()
        {
            NekoLoc.LanguageChanged -= Repaint;
        }

        public static string Version
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
                catch { /* version is cosmetic */ }
                return _version;
            }
        }

        void OnGUI()
        {
            NekoStyles.Ensure();

            NekoStyles.HeaderBar(NekoLoc.T("hub.title"), "NekoSune", NekoLoc.T("hub.subtitle"));
            DrawLanguageBar();
            NekoStyles.Rule(2f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            IList<INekoAddon> addons = NekoAddonRegistry.All;
            if (addons.Count == 0)
            {
                EditorGUILayout.HelpBox(NekoLoc.T("hub.empty"), MessageType.Info);
            }
            else
            {
                string lastCategory = null;
                for (int i = 0; i < addons.Count; i++)
                {
                    INekoAddon a = addons[i];
                    string cat = NekoLoc.T(a.CategoryKey);
                    if (cat != lastCategory)
                    {
                        GUILayout.Space(6f);
                        GUILayout.Label(cat.ToUpperInvariant(), EditorStyles.miniBoldLabel);
                        lastCategory = cat;
                    }
                    DrawAddonCard(a);
                }
            }

            GUILayout.Space(10f);
            EditorGUILayout.EndScrollView();

            NekoStyles.Rule(2f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("v" + Version, NekoStyles.Subtitle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(NekoLoc.T("hub.reloadLanguages"), EditorStyles.miniButton))
            {
                NekoLoc.Reload();
                NekoAddonRegistry.Refresh();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        void DrawLanguageBar()
        {
            List<NekoLanguageInfo> langs = NekoLoc.Languages;
            if (langs == null || langs.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(NekoLoc.T("common.language"), NekoStyles.Subtitle);

            var names = new string[langs.Count];
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

        void DrawAddonCard(INekoAddon addon)
        {
            bool available = addon.IsAvailable;

            Rect card;
            EditorGUILayout.BeginHorizontal(NekoStyles.Card);
            {
                GUILayout.Label(addon.Glyph, NekoStyles.IconBig, GUILayout.Width(34f), GUILayout.Height(38f));
                EditorGUILayout.BeginVertical();
                GUILayout.Label(NekoAddonText.T(addon.TitleKey), NekoStyles.SlotName);
                GUILayout.Label(NekoAddonText.T(addon.DescriptionKey), NekoStyles.SlotMeta);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!available))
                {
                    if (GUILayout.Button(NekoLoc.T("hub.open"), NekoStyles.PrimaryButton, GUILayout.Height(28f)))
                        addon.Open();
                }
            }
            EditorGUILayout.EndHorizontal();
            card = GUILayoutUtility.GetLastRect();

            if (!available)
            {
                EditorGUI.DrawRect(card, new Color(0f, 0f, 0f, 0.25f));
                NekoStyles.Outline(card, new Color(NekoStyles.Warn.r, NekoStyles.Warn.g, NekoStyles.Warn.b, 0.5f));
            }
        }
    }
}
