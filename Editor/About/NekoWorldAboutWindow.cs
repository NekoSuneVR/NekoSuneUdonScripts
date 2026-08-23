using UnityEditor;
using UnityEngine;

namespace NekoSune.Worlds.Editor
{
    internal sealed class NekoWorldAboutWindow : EditorWindow
    {
        [MenuItem(NekoPaths.MenuRoot + "World/About", false, 900)]
        public static void Open()
        {
            var w = GetWindow<NekoWorldAboutWindow>(false, "About NekoSune World Hub", true);
            w.minSize = new Vector2(480f, 320f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header("NekoSune World Hub", "Base / Template package for modular world creator addons");
            EditorGUILayout.HelpBox("This package intentionally owns the World Hub, About page, localization and addon API. World features are installed as separate addons.", MessageType.Info);
            GUILayout.Label("Recommended addons", EditorStyles.boldLabel);
            GUILayout.Label("• NekoSune World Tools — lightweight framework/template helpers\n• NekoSune Optimizer — world performance analysis plus Avatar optimization\n• NekoSune Doctors — World Doctor + Udon Network Doctor and Avatar diagnostics\n• NekoSune Converters — CCK 3/4 World conversion plus Avatar/Prop and Resonite", EditorStyles.wordWrappedLabel);
            GUILayout.Space(8f);
            if (GUILayout.Button("Open World Hub", NekoStyles.PrimaryButton)) NekoHubWindow.Open();
            if (GUILayout.Button("Open project repository")) Application.OpenURL("https://github.com/NekoSuneVR/NekoSuneUdonScripts");
        }
    }
}
