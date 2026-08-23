using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 15)]
    internal sealed class NekoChilloutVRWorldConverterAddon : INekoAddon
    {
        public string Id { get { return "cvr-world-converter"; } }
        public string TitleKey { get { return "cvrworld.title"; } }
        public string DescriptionKey { get { return "cvrworld.desc"; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "C"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoChilloutVRWorldConverterWindow.Open(); }
    }

    internal sealed class NekoChilloutVRWorldConverterWindow : EditorWindow
    {
        bool _convertPickups = true;
        bool _convertObjectSync = true;
        bool _convertMirrors = true;
        bool _convertStations = true;
        bool _convertVideoPlayers = true;
        bool _generateAnimatorToggles = true;
        bool _stripVrcAndUdon = true;
        Vector2 _scroll;
        readonly List<string> _warnings = new List<string>();
        readonly List<string> _toggleCandidates = new List<string>();
        string _status = "Ready";

        [MenuItem(NekoPaths.MenuRoot + "World/Convert VRChat World to ChilloutVR", false, 15)]
        public static void Open()
        {
            var w = GetWindow<NekoChilloutVRWorldConverterWindow>(false, "CVR World Converter", true);
            w.minSize = new Vector2(760f, 580f);
            w.Show();
        }

        void OnEnable() { Scan(); }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header("VRChat → ChilloutVR World Converter", "Creates a separate scene copy for CCK 3 legacy or CCK 4 stable, then converts common world setup and interactions.");

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("ChilloutVR CCK", NekoStyles.CardTitle);
            EditorGUILayout.LabelField("Detected", NekoCckWorldCompatibility.DisplayName);
            if (NekoCckWorldCompatibility.Generation == NekoCckGeneration.Cck3Legacy)
                EditorGUILayout.HelpBox("CCK 3 legacy is supported. ChilloutVR recommends CCK 4 for active development.", MessageType.Info);
            else if (NekoCckWorldCompatibility.Generation == NekoCckGeneration.Cck4Stable)
                EditorGUILayout.HelpBox("CCK 4 stable detected. Conversion targets the current component model and does not use the removed legacy upload panel.", MessageType.Info);
            EditorGUILayout.EndVertical();

            Scene scene = SceneManager.GetActiveScene();
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Source scene", NekoStyles.CardTitle);
            EditorGUILayout.LabelField("Name", scene.name);
            EditorGUILayout.LabelField("Path", string.IsNullOrEmpty(scene.path) ? "Unsaved" : scene.path);
            EditorGUILayout.LabelField("VRC Scene Descriptor", FindSceneDescriptor() == null ? "Missing" : "Detected");
            EditorGUILayout.LabelField("Animator bool toggles", _toggleCandidates.Count.ToString());
            EditorGUILayout.LabelField("Udon behaviours", FindSceneComponents("UdonBehaviour").Count.ToString());
            EditorGUILayout.LabelField("State", _status);
            EditorGUILayout.EndVertical();

            if (!NekoCckWorldCompatibility.Installed || NekoCckWorldCompatibility.WorldType == null)
            {
                EditorGUILayout.HelpBox("Install ChilloutVR CCK 4 stable or CCK 3 legacy before conversion. The converter adds real CCK components, not placeholders.", MessageType.Warning);
                if (GUILayout.Button("Open ChilloutVR CCK setup")) Application.OpenURL("https://docs.chilloutvr.net/cck/setup/");
                return;
            }

            GUILayout.Label("Conversion options", EditorStyles.boldLabel);
            _convertPickups = EditorGUILayout.ToggleLeft("VRChat Pickups → CVR Pickup Object", _convertPickups);
            _convertObjectSync = EditorGUILayout.ToggleLeft("VRChat Object Sync → CVR Object Sync", _convertObjectSync);
            _convertMirrors = EditorGUILayout.ToggleLeft("VRChat Mirrors → CVR Mirror components", _convertMirrors);
            _convertStations = EditorGUILayout.ToggleLeft("VRChat Stations → CVR Interactable sit actions", _convertStations);
            _convertVideoPlayers = EditorGUILayout.ToggleLeft("VRChat video-player markers → CVR Video Player components", _convertVideoPlayers);
            _generateAnimatorToggles = EditorGUILayout.ToggleLeft("Generate CVR Interactable controls for Animator Bool parameters", _generateAnimatorToggles);
            _stripVrcAndUdon = EditorGUILayout.ToggleLeft("Strip VRChat SDK/Udon components from the generated CVR scene copy", _stripVrcAndUdon);

            EditorGUILayout.HelpBox("Udon/UdonSharp is executable VRChat-specific logic and cannot be losslessly translated into CCK interactions. The conversion report records every detected Udon object. Simple Animator Bool controls can be generated as CVR Interactables; custom logic still needs review.", MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_toggleCandidates.Count > 0)
            {
                GUILayout.Label("Animator toggle candidates", EditorStyles.boldLabel);
                for (int i = 0; i < _toggleCandidates.Count; i++) GUILayout.Label("• " + _toggleCandidates[i], EditorStyles.wordWrappedMiniLabel);
            }
            if (_warnings.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Preflight notes", EditorStyles.boldLabel);
                for (int i = 0; i < _warnings.Count; i++) EditorGUILayout.HelpBox(_warnings[i], MessageType.Info);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Height(30f))) Scan();
            if (GUILayout.Button("Copy preflight report", GUILayout.Height(30f))) EditorGUIUtility.systemCopyBuffer = BuildReport(false);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(scene.path) || FindSceneDescriptor() == null))
            {
                if (GUILayout.Button("Create ChilloutVR Scene Copy", NekoStyles.PrimaryButton, GUILayout.Height(34f))) ConvertScene();
            }
            EditorGUILayout.EndHorizontal();
        }

        void Scan()
        {
            _warnings.Clear();
            _toggleCandidates.Clear();
            Component descriptor = FindSceneDescriptor();
            if (descriptor == null) _warnings.Add("No VRChat scene descriptor is present. Add/configure one before conversion so spawn and respawn settings can be copied.");

            List<Component> udon = FindSceneComponents("UdonBehaviour");
            if (udon.Count > 0) _warnings.Add(udon.Count + " Udon/UdonSharp behaviour(s) require manual CVR interaction/Lua/WASM equivalents after conversion.");

            Animator[] animators = FindObjectsOfType<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                RuntimeAnimatorController runtime = animators[i] == null ? null : animators[i].runtimeAnimatorController;
                AnimatorOverrideController over = runtime as AnimatorOverrideController;
                AnimatorController ac = (over == null ? runtime : over.runtimeAnimatorController) as AnimatorController;
                if (ac == null) continue;
                AnimatorControllerParameter[] parameters = ac.parameters;
                for (int p = 0; p < parameters.Length; p++)
                    if (parameters[p].type == AnimatorControllerParameterType.Bool)
                        _toggleCandidates.Add(animators[i].gameObject.name + " / " + parameters[p].name);
            }
            Repaint();
        }

        Component FindSceneDescriptor()
        {
            List<Component> descriptors = FindSceneComponents("VRCSceneDescriptor", "VRC_SceneDescriptor");
            return descriptors.Count > 0 ? descriptors[0] : null;
        }

        List<Component> FindSceneComponents(params string[] names)
        {
            var result = new List<Component>();
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Component[] all = roots[r].GetComponentsInChildren<Component>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    Component c = all[i];
                    if (c == null) continue;
                    string simple = c.GetType().Name;
                    string full = c.GetType().FullName ?? simple;
                    for (int n = 0; n < names.Length; n++)
                    {
                        if (simple == names[n] || full == names[n] || full.EndsWith("." + names[n], StringComparison.Ordinal) ||
                            (names[n] == "UdonBehaviour" && full.IndexOf("UdonBehaviour", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            result.Add(c);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        void ConvertScene()
        {
            Scene sourceScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(sourceScene.path)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets", "NekoSune");
            EnsureFolder("Assets/NekoSune", "Worlds");
            EnsureFolder("Assets/NekoSune/Worlds", "ChilloutVR");
            string copyPath = AssetDatabase.GenerateUniqueAssetPath("Assets/NekoSune/Worlds/ChilloutVR/" + SafeName(sourceScene.name) + "_CVR.unity");
            if (!AssetDatabase.CopyAsset(sourceScene.path, copyPath))
            {
                EditorUtility.DisplayDialog("NekoSune CVR World Converter", "Could not create the scene copy.", "OK");
                return;
            }

            EditorSceneManager.OpenScene(copyPath, OpenSceneMode.Single);
            Component descriptor = FindSceneDescriptor();
            if (descriptor == null) throw new InvalidOperationException("Copied scene no longer contains a VRCSceneDescriptor.");

            var report = new StringBuilder();
            report.AppendLine("NekoSune VRChat → ChilloutVR World conversion");
            report.AppendLine("CCK: " + NekoCckWorldCompatibility.DisplayName);
            report.AppendLine("Source: " + sourceScene.path);
            report.AppendLine("Converted scene: " + copyPath);
            report.AppendLine();

            GameObject worldRoot = descriptor.gameObject;
            Component cvrWorld = NekoCckWorldCompatibility.EnsureComponent(worldRoot, NekoCckWorldCompatibility.WorldType);
            if (cvrWorld == null) throw new InvalidOperationException("Could not add CVRWorld.");
            CopyWorldSettings(descriptor, cvrWorld, report);

            int pickupCount = _convertPickups ? ConvertSimpleComponent("VRCPickup", "VRC_Pickup", NekoCckWorldCompatibility.PickupType) : 0;
            int syncCount = _convertObjectSync ? ConvertSimpleComponent("VRCObjectSync", "VRC_ObjectSync", NekoCckWorldCompatibility.ObjectSyncType) : 0;
            int mirrorCount = _convertMirrors ? ConvertSimpleComponent("VRCMirrorReflection", "VRC_MirrorReflection", NekoCckWorldCompatibility.MirrorType) : 0;
            int videoCount = _convertVideoPlayers ? ConvertVideoPlayers() : 0;
            int stationCount = _convertStations ? ConvertStations(report) : 0;
            int toggleCount = _generateAnimatorToggles ? GenerateAnimatorTogglePanel(worldRoot, report) : 0;

            report.AppendLine("Converted pickups: " + pickupCount);
            report.AppendLine("Converted object sync markers: " + syncCount);
            report.AppendLine("Converted mirror markers: " + mirrorCount);
            report.AppendLine("Converted video-player markers: " + videoCount);
            report.AppendLine("Converted stations: " + stationCount);
            report.AppendLine("Generated Animator Bool controls: " + toggleCount);

            List<Component> udonBeforeStrip = FindSceneComponents("UdonBehaviour");
            if (udonBeforeStrip.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Udon/manual conversion required:");
                for (int i = 0; i < udonBeforeStrip.Count; i++) report.AppendLine("- " + HierarchyPath(udonBeforeStrip[i].transform));
            }

            int stripped = _stripVrcAndUdon ? StripVrcAndUdon(cvrWorld) : 0;
            report.AppendLine();
            report.AppendLine("VRChat/Udon components stripped from CVR copy: " + stripped);
            report.AppendLine("Review generated CVR Interactables, networking, video URLs, stations and all former Udon logic before publishing.");

            EditorUtility.SetDirty(cvrWorld);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            string reportPath = Path.ChangeExtension(copyPath, null) + "_ConversionReport.txt";
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.ImportAsset(reportPath);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(copyPath);
            _status = "Conversion complete — review the copied CVR scene and report";
            Scan();
            EditorUtility.DisplayDialog("NekoSune CVR World Converter", "Created:\n" + copyPath + "\n\nReport:\n" + reportPath, "OK");
        }

        void CopyWorldSettings(Component vrc, Component cvr, StringBuilder report)
        {
            object spawnsObject = NekoCckWorldCompatibility.GetMember(vrc, "spawns", "Spawns", "spawnPoints", "SpawnPoints");
            var spawns = new List<Transform>();
            IEnumerable sequence = spawnsObject as IEnumerable;
            if (sequence != null)
            {
                foreach (object item in sequence)
                {
                    Transform t = item as Transform;
                    GameObject go = item as GameObject;
                    if (t != null) spawns.Add(t);
                    else if (go != null) spawns.Add(go.transform);
                }
            }
            SetTransformCollection(cvr, spawns, "spawns", "Spawns", "spawnPoints", "SpawnPoints");
            report.AppendLine("Spawn points copied: " + spawns.Count);

            object camera = NekoCckWorldCompatibility.GetMember(vrc, "ReferenceCamera", "referenceCamera");
            Camera cam = camera as Camera;
            GameObject cameraGo = camera as GameObject;
            if (cam == null && cameraGo != null) cam = cameraGo.GetComponent<Camera>();
            if (cam != null) NekoCckWorldCompatibility.SetMember(cvr, cam, "referenceCamera", "ReferenceCamera");

            object respawn = NekoCckWorldCompatibility.GetMember(vrc, "RespawnHeightY", "respawnHeightY", "RespawnHeight", "respawnHeight");
            if (respawn != null) NekoCckWorldCompatibility.SetMember(cvr, respawn, "respawnHeight", "RespawnHeight");
        }

        void SetTransformCollection(Component target, List<Transform> values, params string[] names)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = target.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = type.GetField(names[i], flags);
                if (f != null)
                {
                    try
                    {
                        if (f.FieldType.IsArray) f.SetValue(target, values.ToArray());
                        else if (typeof(IList).IsAssignableFrom(f.FieldType))
                        {
                            IList list = (IList)Activator.CreateInstance(f.FieldType);
                            for (int v = 0; v < values.Count; v++) list.Add(values[v]);
                            f.SetValue(target, list);
                        }
                        return;
                    }
                    catch { }
                }
                PropertyInfo p = type.GetProperty(names[i], flags);
                if (p != null && p.CanWrite)
                {
                    try
                    {
                        if (p.PropertyType.IsArray) p.SetValue(target, values.ToArray(), null);
                        else if (typeof(IList).IsAssignableFrom(p.PropertyType))
                        {
                            IList list = (IList)Activator.CreateInstance(p.PropertyType);
                            for (int v = 0; v < values.Count; v++) list.Add(values[v]);
                            p.SetValue(target, list, null);
                        }
                        return;
                    }
                    catch { }
                }
            }
        }

        int ConvertSimpleComponent(string name1, string name2, Type cvrType)
        {
            if (cvrType == null) return 0;
            List<Component> source = FindSceneComponents(name1, name2);
            int count = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (NekoCckWorldCompatibility.EnsureComponent(source[i].gameObject, cvrType) != null) count++;
                if (cvrType == NekoCckWorldCompatibility.PickupType && source[i].GetComponent<Rigidbody>() == null)
                    Undo.AddComponent<Rigidbody>(source[i].gameObject);
            }
            return count;
        }

        int ConvertVideoPlayers()
        {
            if (NekoCckWorldCompatibility.VideoPlayerType == null) return 0;
            List<Component> all = FindSceneComponents("VRCUnityVideoPlayer", "VRCAVProVideoPlayer", "VRCVideoPlayer");
            int count = 0;
            for (int i = 0; i < all.Count; i++)
                if (NekoCckWorldCompatibility.EnsureComponent(all[i].gameObject, NekoCckWorldCompatibility.VideoPlayerType) != null) count++;
            return count;
        }

        int ConvertStations(StringBuilder report)
        {
            if (NekoCckWorldCompatibility.InteractableType == null) return 0;
            List<Component> stations = FindSceneComponents("VRCStation", "VRC_Station");
            int count = 0;
            for (int i = 0; i < stations.Count; i++)
            {
                Component station = stations[i];
                Transform enter = NekoCckWorldCompatibility.GetMember(station, "stationEnterPlayerLocation", "StationEnterPlayerLocation") as Transform;
                Transform exit = NekoCckWorldCompatibility.GetMember(station, "stationExitPlayerLocation", "StationExitPlayerLocation") as Transform;
                Component interactable = NekoCckWorldCompatibility.EnsureComponent(station.gameObject, NekoCckWorldCompatibility.InteractableType);
                if (interactable != null && TryAddInteractableOperation(interactable, "OnInteractDown", "LocalNotNetworked", "SitAtPosition", enter == null ? station.gameObject : enter.gameObject, null, exit == null ? null : exit.gameObject))
                    count++;
                else report.AppendLine("Station needs manual CVR sit setup: " + HierarchyPath(station.transform));
            }
            return count;
        }

        int GenerateAnimatorTogglePanel(GameObject worldRoot, StringBuilder report)
        {
            if (NekoCckWorldCompatibility.InteractableType == null || NekoCckWorldCompatibility.InteractableActionType == null || NekoCckWorldCompatibility.InteractableOperationType == null) return 0;
            Animator[] animators = FindObjectsOfType<Animator>(true);
            GameObject panel = new GameObject("[NekoSune CVR Animator Toggles - MOVE/STYLE ME]");
            panel.transform.SetParent(worldRoot.transform, false);
            int generated = 0;

            for (int i = 0; i < animators.Length; i++)
            {
                RuntimeAnimatorController runtime = animators[i] == null ? null : animators[i].runtimeAnimatorController;
                AnimatorOverrideController over = runtime as AnimatorOverrideController;
                AnimatorController ac = (over == null ? runtime : over.runtimeAnimatorController) as AnimatorController;
                if (ac == null) continue;
                AnimatorControllerParameter[] parameters = ac.parameters;
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (parameters[p].type != AnimatorControllerParameterType.Bool) continue;
                    GameObject control = new GameObject(animators[i].gameObject.name + " - " + parameters[p].name);
                    control.transform.SetParent(panel.transform, false);
                    control.transform.localPosition = new Vector3(0f, generated * 0.12f, 0f);
                    BoxCollider col = control.AddComponent<BoxCollider>();
                    col.size = new Vector3(1.2f, 0.1f, 0.05f);
                    TextMesh label = control.AddComponent<TextMesh>();
                    label.text = parameters[p].name;
                    label.characterSize = 0.05f;
                    label.anchor = TextAnchor.MiddleCenter;
                    Component interactable = NekoCckWorldCompatibility.EnsureComponent(control, NekoCckWorldCompatibility.InteractableType);
                    if (interactable != null && TryAddInteractableOperation(interactable, "OnInteractDown", "GlobalNetworkedBuffered", "ToggleAnimatorBoolValue", animators[i].gameObject, parameters[p].name, null))
                    {
                        NekoCckWorldCompatibility.SetMember(interactable, parameters[p].name, "tooltip", "Tooltip");
                        generated++;
                    }
                }
            }

            if (generated == 0) DestroyImmediate(panel);
            else report.AppendLine("Generated toggle panel is intentionally placed at the CVRWorld root origin. Move/style the controls before upload.");
            return generated;
        }

        bool TryAddInteractableOperation(Component interactable, string actionRegister, string executionType, string operationType, GameObject target, string parameter, GameObject secondary)
        {
            Type actionType = NekoCckWorldCompatibility.InteractableActionType;
            Type operationClass = NekoCckWorldCompatibility.InteractableOperationType;
            if (interactable == null || actionType == null || operationClass == null) return false;
            try
            {
                object action = Activator.CreateInstance(actionType);
                object operation = Activator.CreateInstance(operationClass);
                NekoCckWorldCompatibility.SetMember(action, actionRegister, "actionType", "ActionType");
                NekoCckWorldCompatibility.SetMember(action, executionType, "execType", "ExecType", "executionType", "ExecutionType");
                NekoCckWorldCompatibility.SetMember(operation, operationType, "type", "Type");
                if (!string.IsNullOrEmpty(parameter)) NekoCckWorldCompatibility.SetMember(operation, parameter, "stringVal", "StringVal", "parameterName", "ParameterName");
                if (secondary != null) NekoCckWorldCompatibility.SetMember(operation, secondary, "gameObjectVal", "GameObjectVal");

                IList targets = NekoCckWorldCompatibility.GetMember(operation, "targets", "Targets") as IList;
                if (targets != null && target != null) targets.Add(target);
                IList operations = NekoCckWorldCompatibility.GetMember(action, "operations", "Operations") as IList;
                if (operations == null) return false;
                operations.Add(operation);
                IList actions = NekoCckWorldCompatibility.GetMember(interactable, "actions", "Actions") as IList;
                if (actions == null) return false;
                actions.Add(action);
                EditorUtility.SetDirty(interactable);
                return true;
            }
            catch { return false; }
        }

        int StripVrcAndUdon(Component cvrWorld)
        {
            int count = 0;
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Component[] all = roots[r].GetComponentsInChildren<Component>(true);
                for (int i = all.Length - 1; i >= 0; i--)
                {
                    Component c = all[i];
                    if (c == null || c == cvrWorld || !NekoCckWorldCompatibility.IsVrcOrUdon(c)) continue;
                    DestroyImmediate(c);
                    count++;
                }
            }
            return count;
        }

        string BuildReport(bool converted)
        {
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune VRChat → ChilloutVR World preflight");
            sb.AppendLine("CCK: " + NekoCckWorldCompatibility.DisplayName);
            sb.AppendLine("Scene: " + SceneManager.GetActiveScene().path);
            sb.AppendLine("VRC descriptor: " + (FindSceneDescriptor() == null ? "missing" : "present"));
            sb.AppendLine("Udon behaviours: " + FindSceneComponents("UdonBehaviour").Count);
            sb.AppendLine("Animator Bool candidates: " + _toggleCandidates.Count);
            for (int i = 0; i < _toggleCandidates.Count; i++) sb.AppendLine("- " + _toggleCandidates[i]);
            sb.AppendLine();
            for (int i = 0; i < _warnings.Count; i++) sb.AppendLine("NOTE: " + _warnings[i]);
            return sb.ToString();
        }

        static string HierarchyPath(Transform t)
        {
            if (t == null) return "<missing>";
            var parts = new List<string>();
            while (t != null) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        static string SafeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "World";
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++) value = value.Replace(invalid[i], '_');
            return value;
        }
    }
}
