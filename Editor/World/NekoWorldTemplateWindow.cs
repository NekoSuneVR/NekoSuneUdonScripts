using System.IO;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 0)]
    internal sealed class NekoWorldTemplateAddon : INekoAddon
    {
        public string Id { get { return "world-template"; } }
        public string TitleKey { get { return "template.title"; } }
        public string DescriptionKey { get { return "template.desc"; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "W"; } }
        public bool IsAvailable { get { return true; } }

        public void Open()
        {
            NekoWorldTemplateWindow.Open();
        }
    }

    internal sealed class NekoWorldTemplateWindow : EditorWindow
    {
        [MenuItem(NekoPaths.MenuRoot + "World/Template Guide", false, 10)]
        public static void Open()
        {
            var window = GetWindow<NekoWorldTemplateWindow>(false, "World Template", true);
            window.minSize = new Vector2(460f, 300f);
            window.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header(NekoLoc.T("template.ready"), "com.nekosune.worlds");

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(NekoLoc.T("template.ready.desc"), MessageType.Info);

            GUILayout.Space(8f);
            GUILayout.Label("Starter layout", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(
                "Editor/Core/                 Shared editor framework\n" +
                "Editor/Localization/         Localized editor text\n" +
                "Editor/World/                World-specific editor tools\n" +
                "Runtime/Udon/                UdonSharp/world runtime content\n" +
                "package.json                 VPM manifest",
                EditorStyles.textArea,
                GUILayout.Height(92f));

            GUILayout.Space(8f);
            GUILayout.Label("Dependencies", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("VRChat Worlds SDK", "com.vrchat.worlds");
            EditorGUILayout.LabelField("Unity", "2022.3+");

            GUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(NekoLoc.T("template.packageRoot"), GUILayout.Height(28f)))
            {
                string root = NekoPaths.ToAbsolute(NekoPaths.Root);
                if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                    EditorUtility.RevealInFinder(root);
            }

            if (GUILayout.Button(NekoLoc.T("template.runtimeRoot"), GUILayout.Height(28f)))
            {
                string runtime = NekoPaths.ToAbsolute(NekoPaths.Runtime);
                if (!string.IsNullOrEmpty(runtime) && Directory.Exists(runtime))
                    EditorUtility.RevealInFinder(runtime);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
