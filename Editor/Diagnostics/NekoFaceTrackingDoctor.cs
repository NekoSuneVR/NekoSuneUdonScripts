using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 20)]
    internal sealed class NekoFaceTrackingDoctorAddon : INekoAddon
    {
        public string Id { get { return "face-tracking-doctor"; } }
        public string TitleKey { get { return "face.title"; } }
        public string DescriptionKey { get { return "face.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "F"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoFaceTrackingDoctorWindow.Open(); }
    }

    internal sealed class NekoFaceTrackingDoctorWindow : EditorWindow
    {
        sealed class ShapeGroup
        {
            public string Name;
            public string[] Shapes;
            public int Found;
        }

        static readonly string[] CoreV2Parameters =
        {
            "v2/EyeLeftX", "v2/EyeLeftY", "v2/EyeRightX", "v2/EyeRightY",
            "v2/EyeLidLeft", "v2/EyeLidRight", "v2/EyeSquintLeft", "v2/EyeSquintRight",
            "v2/BrowInnerUpLeft", "v2/BrowInnerUpRight", "v2/BrowOuterUpLeft", "v2/BrowOuterUpRight",
            "v2/BrowLowererLeft", "v2/BrowLowererRight", "v2/NoseSneerLeft", "v2/NoseSneerRight",
            "v2/CheekSquintLeft", "v2/CheekSquintRight", "v2/CheekPuffSuckLeft", "v2/CheekPuffSuckRight",
            "v2/JawOpen", "v2/JawX", "v2/JawZ", "v2/MouthClosed",
            "v2/MouthSmileLeft", "v2/MouthSmileRight", "v2/MouthSadLeft", "v2/MouthSadRight",
            "v2/MouthUpperUpLeft", "v2/MouthUpperUpRight", "v2/MouthLowerDownLeft", "v2/MouthLowerDownRight",
            "v2/MouthPucker", "v2/MouthFunnel", "v2/TongueOut"
        };

        GameObject _avatar;
        Component _descriptor;
        Vector2 _scroll;
        readonly HashSet<string> _blendShapes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly List<ShapeGroup> _groups = new List<ShapeGroup>();
        int _totalBlendShapes;
        int _v2ParametersFound;
        int _arkitMatches;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Face Tracking Doctor", false, 20)]
        public static void Open()
        {
            var w = GetWindow<NekoFaceTrackingDoctorWindow>(false, "Face Tracking Doctor", true);
            w.minSize = new Vector2(690f, 520f);
            w.Show();
        }

        void OnEnable()
        {
            if (_avatar == null) _avatar = NekoAvatarDiagnosticsUtil.SuggestedAvatarFromSelection();
            Scan();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Doctor", "Face Tracking", "Unified Expressions / ARKit coverage and VRCFaceTracking v2 setup");
            EditorGUI.BeginChangeCheck();
            _avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", _avatar, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();
            if (_avatar == null) { EditorGUILayout.HelpBox("Select or drop an avatar.", MessageType.Info); return; }

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Coverage", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Blendshapes discovered", _totalBlendShapes.ToString());
            EditorGUILayout.LabelField("ARKit-style shape matches", _arkitMatches.ToString());
            EditorGUILayout.LabelField("VRCFT v2 parameters already present", _v2ParametersFound + " / " + CoreV2Parameters.Length);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Height(28f))) Scan();
            using (new EditorGUI.DisabledScope(_descriptor == null))
            {
                if (GUILayout.Button("Add Core VRCFT v2 Parameters", NekoStyles.PrimaryButton, GUILayout.Height(28f))) AddV2Parameters();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("The setup button adds missing VRCFT v2 parameters as local, unsynced Floats. It does not invent facial blendshapes or silently build animation mappings: use the coverage report to see what the model actually contains, then map/animate those shapes deliberately.", MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_descriptor == null) EditorGUILayout.HelpBox("No VRChat Avatar Descriptor was found, so Expression Parameters cannot be configured.", MessageType.Error);
            for (int i = 0; i < _groups.Count; i++) DrawGroup(_groups[i]);

            GUILayout.Space(8f);
            GUILayout.Label("Core v2 parameter status", EditorStyles.boldLabel);
            HashSet<string> paramsNow = CurrentParameterNames();
            for (int i = 0; i < CoreV2Parameters.Length; i++)
                EditorGUILayout.LabelField((paramsNow.Contains(CoreV2Parameters[i]) ? "✓ " : "○ ") + CoreV2Parameters[i], NekoStyles.WrapLabel);
            EditorGUILayout.EndScrollView();
        }

        void DrawGroup(ShapeGroup group)
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(group.Name, NekoStyles.SlotName);
            GUILayout.FlexibleSpace();
            GUILayout.Label(group.Found + " / " + group.Shapes.Length, NekoStyles.Chip);
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < group.Shapes.Length; i++)
            {
                bool found = HasShape(group.Shapes[i]);
                EditorGUILayout.LabelField((found ? "✓ " : "○ ") + group.Shapes[i], NekoStyles.WrapLabel);
            }
            EditorGUILayout.EndVertical();
        }

        void Scan()
        {
            _blendShapes.Clear();
            _groups.Clear();
            _totalBlendShapes = 0;
            _arkitMatches = 0;
            _v2ParametersFound = 0;
            _descriptor = _avatar == null ? null : NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(_avatar);
            if (_avatar == null) { Repaint(); return; }

            SkinnedMeshRenderer[] renderers = _avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Mesh mesh = renderers[r] == null ? null : renderers[r].sharedMesh;
                if (mesh == null) continue;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string shape = mesh.GetBlendShapeName(i);
                    if (string.IsNullOrEmpty(shape)) continue;
                    _totalBlendShapes++;
                    _blendShapes.Add(Normalize(shape));
                }
            }

            AddGroup("Eyes", new[] { "eyeBlinkLeft", "eyeBlinkRight", "eyeLookUpLeft", "eyeLookUpRight", "eyeLookDownLeft", "eyeLookDownRight", "eyeSquintLeft", "eyeSquintRight", "eyeWideLeft", "eyeWideRight" });
            AddGroup("Brows", new[] { "browInnerUp", "browDownLeft", "browDownRight", "browOuterUpLeft", "browOuterUpRight" });
            AddGroup("Cheeks / nose", new[] { "cheekPuff", "cheekSquintLeft", "cheekSquintRight", "noseSneerLeft", "noseSneerRight" });
            AddGroup("Jaw / mouth", new[] { "jawOpen", "jawLeft", "jawRight", "jawForward", "mouthClose", "mouthFunnel", "mouthPucker", "mouthSmileLeft", "mouthSmileRight", "mouthFrownLeft", "mouthFrownRight", "mouthUpperUpLeft", "mouthUpperUpRight", "mouthLowerDownLeft", "mouthLowerDownRight", "mouthStretchLeft", "mouthStretchRight", "mouthPressLeft", "mouthPressRight" });
            AddGroup("Tongue", new[] { "tongueOut", "tongueUp", "tongueDown", "tongueLeft", "tongueRight" });

            string[] arkit52 = { "eyeBlinkLeft", "eyeBlinkRight", "eyeLookDownLeft", "eyeLookDownRight", "eyeLookInLeft", "eyeLookInRight", "eyeLookOutLeft", "eyeLookOutRight", "eyeLookUpLeft", "eyeLookUpRight", "eyeSquintLeft", "eyeSquintRight", "eyeWideLeft", "eyeWideRight", "jawForward", "jawLeft", "jawRight", "jawOpen", "mouthClose", "mouthFunnel", "mouthPucker", "mouthRight", "mouthLeft", "mouthSmileLeft", "mouthSmileRight", "mouthFrownLeft", "mouthFrownRight", "mouthDimpleLeft", "mouthDimpleRight", "mouthStretchLeft", "mouthStretchRight", "mouthRollLower", "mouthRollUpper", "mouthShrugLower", "mouthShrugUpper", "mouthPressLeft", "mouthPressRight", "mouthLowerDownLeft", "mouthLowerDownRight", "mouthUpperUpLeft", "mouthUpperUpRight", "browDownLeft", "browDownRight", "browInnerUp", "browOuterUpLeft", "browOuterUpRight", "cheekPuff", "cheekSquintLeft", "cheekSquintRight", "noseSneerLeft", "noseSneerRight", "tongueOut" };
            for (int i = 0; i < arkit52.Length; i++) if (HasShape(arkit52[i])) _arkitMatches++;

            HashSet<string> names = CurrentParameterNames();
            for (int i = 0; i < CoreV2Parameters.Length; i++) if (names.Contains(CoreV2Parameters[i])) _v2ParametersFound++;
            Repaint();
        }

        void AddGroup(string name, string[] shapes)
        {
            var group = new ShapeGroup { Name = name, Shapes = shapes };
            for (int i = 0; i < shapes.Length; i++) if (HasShape(shapes[i])) group.Found++;
            _groups.Add(group);
        }

        bool HasShape(string shape)
        {
            string wanted = Normalize(shape);
            if (_blendShapes.Contains(wanted)) return true;
            foreach (string available in _blendShapes)
            {
                if (available.EndsWith(wanted, StringComparison.OrdinalIgnoreCase) || wanted.EndsWith(available, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            char[] tmp = new char[value.Length];
            int n = 0;
            for (int i = 0; i < value.Length; i++) if (char.IsLetterOrDigit(value[i])) tmp[n++] = char.ToLowerInvariant(value[i]);
            return new string(tmp, 0, n);
        }

        HashSet<string> CurrentParameterNames()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (_descriptor == null) return result;
            List<NekoExpressionParameterInfo> parameters = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(_descriptor);
            for (int i = 0; i < parameters.Count; i++) result.Add(parameters[i].Name);
            return result;
        }

        void AddV2Parameters()
        {
            if (_descriptor == null) return;
            UnityEngine.Object asset = NekoAvatarDiagnosticsUtil.ExpressionParameters(_descriptor);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("NekoSune Face Tracking Doctor", "Assign/create a VRChat Expression Parameters asset first, then run this setup again.", "OK");
                return;
            }

            object existingValue = NekoAvatarDiagnosticsUtil.GetMember(asset, "parameters", "Parameters");
            Array existing = existingValue as Array;
            if (existing == null)
            {
                EditorUtility.DisplayDialog("NekoSune Face Tracking Doctor", "Could not read the parameters array from this SDK version.", "OK");
                return;
            }

            Type elementType = existing.GetType().GetElementType();
            if (elementType == null) return;
            HashSet<string> existingNames = CurrentParameterNames();
            var missing = new List<string>();
            for (int i = 0; i < CoreV2Parameters.Length; i++) if (!existingNames.Contains(CoreV2Parameters[i])) missing.Add(CoreV2Parameters[i]);
            if (missing.Count == 0)
            {
                EditorUtility.DisplayDialog("NekoSune Face Tracking Doctor", "All core VRCFT v2 parameters in this preset already exist.", "OK");
                return;
            }

            Array combined = Array.CreateInstance(elementType, existing.Length + missing.Count);
            for (int i = 0; i < existing.Length; i++) combined.SetValue(existing.GetValue(i), i);
            for (int i = 0; i < missing.Count; i++)
            {
                object parameter = Activator.CreateInstance(elementType);
                NekoAvatarDiagnosticsUtil.SetMember(parameter, missing[i], "name", "Name");
                NekoAvatarDiagnosticsUtil.SetMember(parameter, "Float", "valueType", "ValueType", "type", "Type");
                NekoAvatarDiagnosticsUtil.SetMember(parameter, 0f, "defaultValue", "DefaultValue");
                NekoAvatarDiagnosticsUtil.SetMember(parameter, false, "saved", "Saved");
                NekoAvatarDiagnosticsUtil.SetMember(parameter, false, "networkSynced", "NetworkSynced", "synced", "Synced");
                combined.SetValue(parameter, existing.Length + i);
            }

            Undo.RecordObject(asset, "Add NekoSune VRCFT parameters");
            if (!NekoAvatarDiagnosticsUtil.SetMember(asset, combined, "parameters", "Parameters"))
            {
                EditorUtility.DisplayDialog("NekoSune Face Tracking Doctor", "This VRChat SDK version did not allow the parameters array to be updated through the supported serialized member.", "OK");
                return;
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[NekoSune Face Tracking Doctor] Added " + missing.Count + " local unsynced VRCFT v2 Float parameter(s).");
            Scan();
        }
    }
}
