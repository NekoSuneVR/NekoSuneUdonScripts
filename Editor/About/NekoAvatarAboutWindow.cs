using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    internal sealed class NekoAvatarAboutWindow : EditorWindow
    {
        [MenuItem(NekoPaths.MenuRoot + "Avatar/About", false, 900)]
        public static void Open()
        {
            var w = GetWindow<NekoAvatarAboutWindow>(false, "About NekoSune Avatar Hub", true);
            w.minSize = new Vector2(480f, 330f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("NekoSune Avatar Hub", "Base / Template package", "A lightweight host for separately installable creator addons");
            EditorGUILayout.HelpBox("This package intentionally contains the Hub, About page, localization and addon API only. Feature packages register themselves automatically when installed.", MessageType.Info);
            GUILayout.Label("Recommended addons", EditorStyles.boldLabel);
            GUILayout.Label("• NekoSune Avatar Tools — Lip Sync Studio + Rank Advisor\n• NekoSune Optimizer — Compressor, Mesh, Quest and VRAM\n• NekoSune Doctors — Avatar, PhysBone, Face and Animator diagnostics\n• NekoSune Converters — ChilloutVR CCK 3/4 + Props + Worlds + Resonite", NekoStyles.WrapLabel);
            GUILayout.Space(8f);
            if (GUILayout.Button("Open Avatar Hub", NekoStyles.PrimaryButton)) NekoHubWindow.Open();
            if (GUILayout.Button("Open project repository")) Application.OpenURL("https://github.com/NekoSuneVR/NekoSuneUdonScripts");
        }
    }
}
