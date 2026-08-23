using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 47)]
    internal sealed class NekoChilloutVRPropConverterAddon : INekoAddon
    {
        public string Id { get { return "cvr-prop-converter"; } }
        public string TitleKey { get { return "cvrprop.title"; } }
        public string DescriptionKey { get { return "cvrprop.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "P"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoChilloutVRPropConverterWindow.Open(); }
    }

    internal sealed class NekoChilloutVRPropConverterWindow : EditorWindow
    {
        GameObject _source;
        bool _convertPickup = true;
        bool _convertObjectSync = true;
        bool _generateAnimatorToggles = true;
        bool _stripVrcComponents = true;
        bool _addRigidbodyWhenMissing = true;
        Vector2 _scroll;
        readonly List<string> _toggleCandidates = new List<string>();
        string _status = "Ready";

        [MenuItem(NekoPaths.MenuRoot + "Avatar/ChilloutVR/Convert to Prop", false, 47)]
        public static void Open()
        {
            var w = GetWindow<NekoChilloutVRPropConverterWindow>(false, "CVR Prop Converter", true);
            w.minSize = new Vector2(700f, 540f);
            w.Show();
        }

        void OnEnable()
        {
            if (_source == null) _source = Selection.activeGameObject;
            Scan();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Prop Converter", "ChilloutVR", "Turn a Unity/VRChat object hierarchy into a real CVR Spawnable/Prop copy");
            NekoCckCompatibility.DrawStatusBox();

            EditorGUI.BeginChangeCheck();
            _source = (GameObject)EditorGUILayout.ObjectField("Source object", _source, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();

            if (!NekoCckCompatibility.Installed || NekoCckCompatibility.SpawnableType == null)
            {
                EditorGUILayout.HelpBox("Install ChilloutVR CCK 4 stable or CCK 3 legacy first. NekoSune creates real CCK components and does not emit placeholder metadata.", MessageType.Warning);
                if (GUILayout.Button("Open CCK setup documentation")) Application.OpenURL("https://docs.chilloutvr.net/cck/setup/");
                return;
            }

            if (_source == null)
            {
                EditorGUILayout.HelpBox("Select the root GameObject you want to turn into a ChilloutVR prop.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Detected source", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Renderers", _source.GetComponentsInChildren<Renderer>(true).Length.ToString());
            EditorGUILayout.LabelField("Animators", _source.GetComponentsInChildren<Animator>(true).Length.ToString());
            EditorGUILayout.LabelField("Colliders", _source.GetComponentsInChildren<Collider>(true).Length.ToString());
            EditorGUILayout.LabelField("VRChat Pickups", FindVrc("VRCPickup", "VRC_Pickup").Count.ToString());
            EditorGUILayout.LabelField("VRChat Object Sync", FindVrc("VRCObjectSync", "VRC_ObjectSync").Count.ToString());
            EditorGUILayout.LabelField("Animator toggle/value candidates", _toggleCandidates.Count.ToString());
            EditorGUILayout.LabelField("State", _status);
            EditorGUILayout.EndVertical();

            GUILayout.Label("Conversion", EditorStyles.boldLabel);
            _convertPickup = EditorGUILayout.ToggleLeft("Convert VRChat Pickup markers to CVR Pickup Object", _convertPickup);
            _convertObjectSync = EditorGUILayout.ToggleLeft("Convert VRChat Object Sync markers to CVR Object Sync", _convertObjectSync);
            _generateAnimatorToggles = EditorGUILayout.ToggleLeft("Generate CVR Interactable controls for Animator Bool toggles", _generateAnimatorToggles);
            _addRigidbodyWhenMissing = EditorGUILayout.ToggleLeft("Add Rigidbody when converting a pickup and one is missing", _addRigidbodyWhenMissing);
            _stripVrcComponents = EditorGUILayout.ToggleLeft("Strip VRChat/Udon components from the generated prop copy", _stripVrcComponents);

            EditorGUILayout.HelpBox("Meshes, materials, colliders, AudioSources, ParticleSystems and Animators are preserved. Bool Animator parameters can become Global Networked Buffered CVR Interactable toggles. The generated toggle panel is deliberately marked MOVE/STYLE ME so you can position it on the prop. Custom Udon logic still requires a CVR equivalent.", MessageType.Info);

            if (_toggleCandidates.Count > 0)
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(170f));
                GUILayout.Label("Animator values/toggles", EditorStyles.boldLabel);
                for (int i = 0; i < _toggleCandidates.Count; i++) GUILayout.Label("• " + _toggleCandidates[i], EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Height(30f))) Scan();
            if (GUILayout.Button("Copy conversion report", GUILayout.Height(30f))) EditorGUIUtility.systemCopyBuffer = BuildReport();
            using (new EditorGUI.DisabledScope(_source == null))
            {
                if (GUILayout.Button("Create ChilloutVR Prop Copy", NekoStyles.PrimaryButton, GUILayout.Height(34f))) Convert();
            }
            EditorGUILayout.EndHorizontal();
        }

        void Scan()
        {
            _toggleCandidates.Clear();
            if (_source == null) { Repaint(); return; }
            Animator[] animators = _source.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                RuntimeAnimatorController runtime = animators[i] == null ? null : animators[i].runtimeAnimatorController;
                AnimatorOverrideController over = runtime as AnimatorOverrideController;
                AnimatorController ac = (over == null ? runtime : over.runtimeAnimatorController) as AnimatorController;
                if (ac == null) continue;
                AnimatorControllerParameter[] ps = ac.parameters;
                for (int p = 0; p < ps.Length; p++)
                {
                    string item = ps[p].type + " " + ps[p].name + " — " + animators[i].gameObject.name;
                    if (!_toggleCandidates.Contains(item)) _toggleCandidates.Add(item);
                }
            }
            Repaint();
        }

        List<Component> FindVrc(params string[] names)
        {
            return _source == null ? new List<Component>() : NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_source, names);
        }

        void Convert()
        {
            try
            {
                _status = "Cloning source…"; Repaint();
                GameObject copy = Instantiate(_source, _source.transform.parent);
                copy.name = _source.name + " [ChilloutVR Prop]";
                copy.SetActive(true);
                Undo.RegisterCreatedObjectUndo(copy, "Create ChilloutVR prop copy");

                Component spawnable = NekoCckCompatibility.EnsureComponent(copy, NekoCckCompatibility.SpawnableType);
                if (spawnable == null) throw new InvalidOperationException("Could not add CVRSpawnable from the installed CCK.");

                int pickups = 0;
                int syncs = 0;
                if (_convertPickup && NekoCckCompatibility.PickupType != null)
                {
                    List<Component> src = NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(copy, "VRCPickup", "VRC_Pickup");
                    for (int i = 0; i < src.Count; i++)
                    {
                        GameObject go = src[i].gameObject;
                        if (_addRigidbodyWhenMissing && go.GetComponent<Rigidbody>() == null) Undo.AddComponent<Rigidbody>(go);
                        if (NekoCckCompatibility.EnsureComponent(go, NekoCckCompatibility.PickupType) != null) pickups++;
                    }
                }

                if (_convertObjectSync && NekoCckCompatibility.ObjectSyncType != null)
                {
                    List<Component> src = NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(copy, "VRCObjectSync", "VRC_ObjectSync");
                    for (int i = 0; i < src.Count; i++)
                        if (NekoCckCompatibility.EnsureComponent(src[i].gameObject, NekoCckCompatibility.ObjectSyncType) != null) syncs++;
                }

                int toggles = _generateAnimatorToggles ? NekoChilloutVRToggleBuilder.Generate(copy) : 0;

                int stripped = 0;
                if (_stripVrcComponents)
                {
                    Component[] all = copy.GetComponentsInChildren<Component>(true);
                    for (int i = all.Length - 1; i >= 0; i--)
                    {
                        Component c = all[i];
                        if (!NekoCckCompatibility.IsVrcComponent(c)) continue;
                        Undo.DestroyObjectImmediate(c);
                        stripped++;
                    }
                }

                EditorUtility.SetDirty(spawnable);
                Selection.activeGameObject = copy;
                EditorGUIUtility.PingObject(copy);
                _status = "Done — " + pickups + " pickup(s), " + syncs + " sync marker(s), " + toggles + " toggle(s), " + stripped + " VRC/Udon component(s) stripped";
                Debug.Log("[NekoSune CVR Prop Converter] " + _status);
            }
            catch (Exception e)
            {
                _status = "Conversion failed — see Console";
                Debug.LogException(e);
            }
            Repaint();
        }

        string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune VRChat/Unity → ChilloutVR Prop report");
            sb.AppendLine("CCK: " + NekoCckCompatibility.DisplayName);
            sb.AppendLine("Source: " + (_source == null ? "None" : _source.name));
            if (_source == null) return sb.ToString();
            sb.AppendLine("VRChat pickups: " + FindVrc("VRCPickup", "VRC_Pickup").Count);
            sb.AppendLine("VRChat object sync: " + FindVrc("VRCObjectSync", "VRC_ObjectSync").Count);
            sb.AppendLine("Animator values:");
            for (int i = 0; i < _toggleCandidates.Count; i++) sb.AppendLine("- " + _toggleCandidates[i]);
            sb.AppendLine();
            sb.AppendLine("Bool Animator values can be generated as CVR Interactable toggles. Float/Int values and custom Udon/network logic should be reviewed and wired to CVR Spawnable values/interactions as appropriate.");
            return sb.ToString();
        }
    }
}
