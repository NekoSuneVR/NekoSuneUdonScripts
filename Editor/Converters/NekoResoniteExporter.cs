using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 40)]
    internal sealed class NekoResoniteExporterAddon : INekoAddon
    {
        public string Id { get { return "resonite-exporter"; } }
        public string TitleKey { get { return "resonite.title"; } }
        public string DescriptionKey { get { return "resonite.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "R"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoResoniteExporterWindow.Open(); }
    }

    internal sealed class NekoResoniteExporterWindow : EditorWindow
    {
        GameObject _avatar;
        bool _building;
        string _status = "Ready";
        string _lastPath;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Export to Resonite", false, 40)]
        public static void Open()
        {
            var w = GetWindow<NekoResoniteExporterWindow>(false, "Resonite Export", true);
            w.minSize = new Vector2(620f, 360f);
            w.Show();
        }

        void OnEnable()
        {
            if (_avatar == null) _avatar = NekoAvatarDiagnosticsUtil.SuggestedAvatarFromSelection();
        }

        bool HasBackend
        {
            get
            {
                return NekoAvatarDiagnosticsUtil.FindType("nadena.dev.ndmf.platform.resonite.ResoniteBuildUI") != null &&
                       NekoAvatarDiagnosticsUtil.FindType("nadena.dev.ndmf.platform.resonite.BuildController") != null;
            }
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Exporter", "Resonite", "VRChat/Unity avatar → .resonitepackage using Modular Avatar's Resonite backend");
            _avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", _avatar, typeof(GameObject), true);

            if (!HasBackend)
            {
                EditorGUILayout.HelpBox("Modular Avatar - Resonite support was not detected. Install the experimental Modular Avatar Resonite package first; NekoSune intentionally uses its real serializer instead of inventing a separate .resonitepackage implementation.", MessageType.Warning);
                if (GUILayout.Button("Open Package Manager")) EditorApplication.ExecuteMenuItem("Window/Package Manager");
                return;
            }

            EditorGUILayout.HelpBox("This bridge runs the same Resonite build backend used by Modular Avatar's Resonite platform. Current backend support includes avatar hierarchy/common avatar info, meshes, textures/material conversion, viewpoint/visemes and PhysBone-to-Resonite dynamics where supported by that package. Animator toggle/animation conversion remains limited by the upstream experimental exporter.", MessageType.Info);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Backend", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Modular Avatar Resonite", "Detected");
            EditorGUILayout.LabelField("State", _status);
            if (!string.IsNullOrEmpty(_lastPath)) EditorGUILayout.LabelField("Last saved package", _lastPath, NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            using (new EditorGUI.DisabledScope(_building || _avatar == null))
            {
                if (GUILayout.Button(_building ? "Building…" : "Build and Save .resonitepackage", NekoStyles.PrimaryButton, GUILayout.Height(34f))) BuildAndSave();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open NDMF Console", GUILayout.Height(28f))) OpenNdmfConsole();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_lastPath)))
            {
                if (GUILayout.Button("Copy saved path", GUILayout.Height(28f))) EditorGUIUtility.systemCopyBuffer = _lastPath;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("The upstream Resonite integration is experimental and uses internal NDMF/Modular Avatar APIs. NekoSune catches API changes and falls back to opening the NDMF Console if a future package version changes the private build UI.", MessageType.None);
        }

        async void BuildAndSave()
        {
            if (_building || _avatar == null || !HasBackend) return;
            _building = true;
            _status = "Starting Modular Avatar Resonite build…";
            Repaint();

            try
            {
                Type uiType = NekoAvatarDiagnosticsUtil.FindType("nadena.dev.ndmf.platform.resonite.ResoniteBuildUI");
                object buildUi = Activator.CreateInstance(uiType, true);
                PropertyInfo avatarRoot = uiType.GetProperty("AvatarRoot", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (avatarRoot == null) throw new MissingMemberException(uiType.FullName, "AvatarRoot");
                avatarRoot.SetValue(buildUi, _avatar, null);

                MethodInfo build = uiType.GetMethod("BuildAvatar", BindingFlags.NonPublic | BindingFlags.Instance);
                if (build == null) throw new MissingMethodException(uiType.FullName, "BuildAvatar");
                object taskObject = build.Invoke(buildUi, null);
                Task task = taskObject as Task;
                if (task == null) throw new InvalidOperationException("Resonite BuildAvatar did not return a Task.");
                await task;

                Type controllerType = NekoAvatarDiagnosticsUtil.FindType("nadena.dev.ndmf.platform.resonite.BuildController");
                PropertyInfo instanceProperty = controllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                object controller = instanceProperty == null ? null : instanceProperty.GetValue(null, null);
                string tempPath = controller == null ? null : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(controller, "LastTempPath"));
                string avatarName = controller == null ? _avatar.name : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(controller, "LastAvatarName"));
                string state = controller == null ? null : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(controller, "State"));
                if (!string.IsNullOrEmpty(state)) _status = state;

                if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath)) throw new FileNotFoundException("The Resonite backend completed without a readable temporary .resonitepackage.", tempPath);
                string path = EditorUtility.SaveFilePanel("Save Resonite package", "", SafeName(string.IsNullOrEmpty(avatarName) ? _avatar.name : avatarName) + ".resonitepackage", "resonitepackage");
                if (!string.IsNullOrEmpty(path))
                {
                    File.Copy(tempPath, path, true);
                    _lastPath = path;
                    _status = "Saved successfully";
                    Debug.Log("[NekoSune Resonite Exporter] Saved " + path);
                }
                else
                {
                    _status = "Build complete; save cancelled";
                }
            }
            catch (Exception e)
            {
                Exception actual = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                _status = "Backend API changed or build failed — see Console";
                Debug.LogException(actual);
                EditorUtility.DisplayDialog("NekoSune Resonite Exporter", "The installed Modular Avatar Resonite backend could not be driven automatically. NekoSune will open the NDMF Console so you can use its native Resonite build UI.\n\n" + actual.Message, "Open NDMF Console");
                OpenNdmfConsole();
            }
            finally
            {
                _building = false;
                Repaint();
            }
        }

        static void OpenNdmfConsole()
        {
            bool opened = EditorApplication.ExecuteMenuItem("Tools/NDM Framework/NDMF Console");
            if (!opened) EditorApplication.ExecuteMenuItem("Tools/NDM Framework/NDMF Console (Experimental)");
        }

        static string SafeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Avatar";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }
    }
}
