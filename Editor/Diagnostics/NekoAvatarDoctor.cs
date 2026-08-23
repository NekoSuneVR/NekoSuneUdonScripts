using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 0)]
    internal sealed class NekoAvatarDoctorAddon : INekoAddon
    {
        public string Id { get { return "avatar-doctor"; } }
        public string TitleKey { get { return "doctor.title"; } }
        public string DescriptionKey { get { return "doctor.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "✓"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoAvatarDoctorWindow.Open(); }
    }

    internal sealed class NekoAvatarDoctorWindow : EditorWindow
    {
        enum Severity { Error, Warning, Info }

        sealed class Finding
        {
            public Severity Severity;
            public string Title;
            public string Detail;
            public UnityEngine.Object Target;
        }

        GameObject _avatar;
        GameObject _questAvatar;
        Vector2 _scroll;
        readonly List<Finding> _findings = new List<Finding>();
        NekoAvatarReport _report;
        NekoRankAssessment _pc;
        NekoRankAssessment _mobile;
        int _parameterBits;
        int _parameterCount;
        long _textureBytes;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Avatar Doctor", false, 0)]
        public static void Open()
        {
            var w = GetWindow<NekoAvatarDoctorWindow>(false, "Avatar Doctor", true);
            w.minSize = new Vector2(700f, 520f);
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
            NekoStyles.HeaderBar("Doctor", "Avatar", "Preflight checks before VRChat upload");

            EditorGUI.BeginChangeCheck();
            _avatar = (GameObject)EditorGUILayout.ObjectField("PC / source avatar", _avatar, typeof(GameObject), true);
            _questAvatar = (GameObject)EditorGUILayout.ObjectField("Quest counterpart (optional)", _questAvatar, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run full preflight", NekoStyles.PrimaryButton, GUILayout.Height(30f))) Scan();
            if (GUILayout.Button("Copy report", GUILayout.Width(110f), GUILayout.Height(30f))) EditorGUIUtility.systemCopyBuffer = BuildReport();
            EditorGUILayout.EndHorizontal();

            if (_avatar == null)
            {
                EditorGUILayout.HelpBox("Drop an avatar here or select one in the Hierarchy.", MessageType.Info);
                return;
            }

            DrawSummary();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawGroup(Severity.Error, "Upload / correctness errors");
            DrawGroup(Severity.Warning, "Warnings");
            DrawGroup(Severity.Info, "Recommendations");
            EditorGUILayout.EndScrollView();
        }

        void DrawSummary()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Preflight summary", NekoStyles.SlotName);
            if (_report != null)
            {
                EditorGUILayout.LabelField("PC performance", _pc == null ? "-" : NekoLoc.T(NekoPerfTable.RankKey(_pc.Overall)));
                EditorGUILayout.LabelField("Quest / mobile performance", _mobile == null ? "-" : NekoLoc.T(NekoPerfTable.RankKey(_mobile.Overall)));
                EditorGUILayout.LabelField("Expression parameters", _parameterCount + "  ·  " + _parameterBits + " / 256 synced bits");
                EditorGUILayout.LabelField("Estimated texture memory", NekoRankAdvisor.FormatBytes(_textureBytes));
                EditorGUILayout.LabelField("Findings", Count(Severity.Error) + " errors · " + Count(Severity.Warning) + " warnings · " + Count(Severity.Info) + " recommendations");
            }
            EditorGUILayout.EndVertical();
        }

        int Count(Severity severity)
        {
            int count = 0;
            for (int i = 0; i < _findings.Count; i++) if (_findings[i].Severity == severity) count++;
            return count;
        }

        void DrawGroup(Severity severity, string title)
        {
            int count = Count(severity);
            if (count == 0) return;
            GUILayout.Space(6f);
            GUILayout.Label(title + " (" + count + ")", EditorStyles.boldLabel);
            for (int i = 0; i < _findings.Count; i++)
            {
                Finding f = _findings[i];
                if (f.Severity != severity) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(f.Title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (f.Target != null && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(58f)))
                {
                    Selection.activeObject = f.Target;
                    EditorGUIUtility.PingObject(f.Target);
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label(f.Detail, NekoStyles.WrapLabel);
                EditorGUILayout.EndVertical();
            }
        }

        void Scan()
        {
            _findings.Clear();
            _report = null;
            _pc = null;
            _mobile = null;
            _parameterBits = 0;
            _parameterCount = 0;
            _textureBytes = 0;
            if (_avatar == null) { Repaint(); return; }

            _report = NekoAvatarStats.Collect(_avatar);
            _pc = NekoRankAdvisor.Assess(_report, NekoPlatform.PC);
            _mobile = NekoRankAdvisor.Assess(_report, NekoPlatform.Mobile);

            Component descriptor = NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(_avatar);
            if (descriptor == null)
            {
                Add(Severity.Error, "Missing VRChat Avatar Descriptor", "This object cannot be uploaded as a VRChat avatar until a VRCAvatarDescriptor is present on the avatar root.", _avatar);
            }
            else
            {
                CheckDescriptor(descriptor);
                CheckExpressions(descriptor);
                CheckAnimators(descriptor);
            }

            CheckPerformance();
            CheckTexturesAndShaders();
            CheckDuplicateAndUnsupportedComponents();
            if (_questAvatar != null) CheckCrossPlatform(descriptor, _questAvatar);

            if (_findings.Count == 0) Add(Severity.Info, "No obvious preflight problems found", "Still run the VRChat SDK Build & Test / Builder validation before publishing; Avatar Doctor is an independent diagnostic pass, not a replacement for the SDK validator.", _avatar);
            Repaint();
        }

        void CheckDescriptor(Component descriptor)
        {
            Animator animator = _avatar.GetComponent<Animator>();
            if (animator == null) Add(Severity.Error, "Missing root Animator", "The avatar root has no Animator component.", _avatar);
            else if (animator.avatar == null) Add(Severity.Warning, "Animator has no Avatar rig assigned", "For humanoid avatars, verify the imported Humanoid Avatar is assigned and valid.", animator);

            object view = NekoAvatarDiagnosticsUtil.GetMember(descriptor, "ViewPosition", "viewPosition");
            if (view is Vector3)
            {
                Vector3 v = (Vector3)view;
                if (v == Vector3.zero) Add(Severity.Warning, "Viewpoint is still zero", "Set the avatar viewpoint between the eyes. A zero viewpoint usually means descriptor setup is unfinished.", descriptor);
            }
        }

        void CheckExpressions(Component descriptor)
        {
            UnityEngine.Object parametersAsset = NekoAvatarDiagnosticsUtil.ExpressionParameters(descriptor);
            UnityEngine.Object menuAsset = NekoAvatarDiagnosticsUtil.ExpressionsMenu(descriptor);
            if (parametersAsset == null) Add(Severity.Warning, "No Expression Parameters asset", "Custom toggles, puppets and OSC/VRCFT parameters need an Expression Parameters asset assigned to the descriptor.", descriptor);
            if (menuAsset == null) Add(Severity.Warning, "No Expressions Menu", "No root Expressions Menu is assigned. This is fine only if the avatar intentionally has no menu controls.", descriptor);

            List<NekoExpressionParameterInfo> parameters = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(descriptor);
            _parameterCount = parameters.Count;
            _parameterBits = NekoAvatarDiagnosticsUtil.ParameterBits(parameters);
            if (_parameterBits > 256) Add(Severity.Error, "Expression parameter memory exceeded", _parameterBits + " synced bits are configured; VRChat synchronizes at most 256 custom parameter bits.", parametersAsset);
            if (_parameterCount > 8192) Add(Severity.Error, "Too many custom expression parameters", _parameterCount + " parameters were found; VRChat limits avatars to 8192 custom Expression Parameters total.", parametersAsset);

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < parameters.Count; i++)
            {
                if (!names.Add(parameters[i].Name)) Add(Severity.Error, "Duplicate expression parameter", "Parameter '" + parameters[i].Name + "' appears more than once.", parametersAsset);
            }

            if (menuAsset != null) CheckMenuRecursive(menuAsset, NekoAvatarDiagnosticsUtil.ParameterMap(parameters), new HashSet<int>(), "Root");
        }

        void CheckMenuRecursive(object menu, Dictionary<string, NekoExpressionParameterInfo> parameters, HashSet<int> seen, string path)
        {
            if (menu == null) return;
            int id = menu.GetHashCode();
            if (!seen.Add(id)) return;
            IEnumerable controls = NekoAvatarDiagnosticsUtil.GetMember(menu, "controls", "Controls") as IEnumerable;
            if (controls == null) return;
            int count = 0;
            foreach (object control in controls)
            {
                if (control == null) continue;
                count++;
                string controlName = Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(control, "name", "Name"));
                object parameter = NekoAvatarDiagnosticsUtil.GetMember(control, "parameter", "Parameter");
                string parameterName = parameter == null ? null : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(parameter, "name", "Name"));
                object type = NekoAvatarDiagnosticsUtil.GetMember(control, "type", "Type");
                string typeName = type == null ? "Unknown" : type.ToString();

                bool needsMainParameter = typeName.IndexOf("SubMenu", StringComparison.OrdinalIgnoreCase) < 0 &&
                                          typeName.IndexOf("TwoAxis", StringComparison.OrdinalIgnoreCase) < 0 &&
                                          typeName.IndexOf("FourAxis", StringComparison.OrdinalIgnoreCase) < 0;
                if (needsMainParameter && !string.IsNullOrEmpty(parameterName) && !parameters.ContainsKey(parameterName))
                    Add(Severity.Error, "Menu references missing parameter", path + " / " + controlName + " uses '" + parameterName + "', but that name is not in Expression Parameters.", menu as UnityEngine.Object);

                IEnumerable subs = NekoAvatarDiagnosticsUtil.GetMember(control, "subParameters", "SubParameters") as IEnumerable;
                if (subs != null)
                {
                    foreach (object sub in subs)
                    {
                        string subName = sub == null ? null : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(sub, "name", "Name"));
                        if (!string.IsNullOrEmpty(subName) && !parameters.ContainsKey(subName))
                            Add(Severity.Error, "Puppet references missing parameter", path + " / " + controlName + " uses sub-parameter '" + subName + "' which is missing.", menu as UnityEngine.Object);
                    }
                }

                object subMenu = NekoAvatarDiagnosticsUtil.GetMember(control, "subMenu", "SubMenu");
                if (typeName.IndexOf("SubMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (subMenu == null) Add(Severity.Error, "Empty submenu control", path + " / " + controlName + " is a SubMenu control but has no submenu asset assigned.", menu as UnityEngine.Object);
                    else CheckMenuRecursive(subMenu, parameters, seen, path + " / " + controlName);
                }
            }
            if (count > 8) Add(Severity.Error, "Expression menu has more than 8 controls", path + " contains " + count + " controls; VRChat menus support up to 8 controls per menu.", menu as UnityEngine.Object);
        }

        void CheckAnimators(Component descriptor)
        {
            List<NekoExpressionParameterInfo> expressions = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(descriptor);
            Dictionary<string, NekoExpressionParameterInfo> exprMap = NekoAvatarDiagnosticsUtil.ParameterMap(expressions);
            List<RuntimeAnimatorController> controllers = NekoAvatarDiagnosticsUtil.FindControllers(_avatar, descriptor);
            Dictionary<string, AnimatorControllerParameterType> animatorMap = NekoAvatarDiagnosticsUtil.AnimatorParameterMap(controllers);

            foreach (KeyValuePair<string, NekoExpressionParameterInfo> pair in exprMap)
            {
                AnimatorControllerParameterType animatorType;
                if (!animatorMap.TryGetValue(pair.Key, out animatorType)) continue;
                string expected = NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(pair.Value.TypeName);
                string actual = NekoAvatarDiagnosticsUtil.AnimatorTypeName(animatorType);
                if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                    Add(Severity.Warning, "Animator / expression parameter type mismatch", pair.Key + " is " + pair.Value.TypeName + " in Expression Parameters but " + actual + " in an Animator Controller. VRChat can coerce some mismatches, but matching types is safer and clearer.", NekoAvatarDiagnosticsUtil.ExpressionParameters(descriptor));
            }

            for (int i = 0; i < controllers.Count; i++)
            {
                AnimatorController ac = controllers[i] as AnimatorController;
                if (ac == null) continue;
                CheckWriteDefaults(ac);
            }
        }

        void CheckWriteDefaults(AnimatorController controller)
        {
            bool sawOn = false, sawOff = false;
            int stateCount = 0;
            AnimatorControllerLayer[] layers = controller.layers;
            for (int l = 0; l < layers.Length; l++) ScanStateMachine(layers[l].stateMachine, ref sawOn, ref sawOff, ref stateCount);
            if (sawOn && sawOff) Add(Severity.Warning, "Mixed Write Defaults in Animator", controller.name + " mixes Write Defaults On and Off across its states. Mixed setups are easy to break when layers interact; verify this is deliberate.", controller);
        }

        void ScanStateMachine(AnimatorStateMachine machine, ref bool sawOn, ref bool sawOff, ref int stateCount)
        {
            if (machine == null) return;
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null) continue;
                stateCount++;
                if (state.writeDefaultValues) sawOn = true; else sawOff = true;
            }
            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++) ScanStateMachine(children[i].stateMachine, ref sawOn, ref sawOff, ref stateCount);
        }

        void CheckPerformance()
        {
            if (_report == null) return;
            if (_report.MeshReadWriteDisabled) Add(Severity.Error, "Mesh Read/Write disabled", "At least one avatar mesh has Read/Write disabled. Rank Advisor can enable it on model importers; VRChat treats this as an upload/performance problem.", _avatar);

            if (_pc != null && _pc.Overall >= NekoRank.Poor) Add(Severity.Warning, "PC performance rank is " + NekoLoc.T(NekoPerfTable.RankKey(_pc.Overall)), "Open Rank Advisor for the exact blocker list and targets.", _avatar);
            if (_mobile != null && _mobile.Overall >= NekoRank.Poor) Add(Severity.Warning, "Quest/mobile performance rank is " + NekoLoc.T(NekoPerfTable.RankKey(_mobile.Overall)), "Use PC → Quest Assistant and Rank Advisor to work through mobile blockers.", _avatar);
        }

        void CheckTexturesAndShaders()
        {
            List<NekoTextureInfo> textures = NekoAvatarDiagnosticsUtil.CollectTextures(_avatar);
            for (int i = 0; i < textures.Count; i++)
            {
                NekoTextureInfo t = textures[i];
                _textureBytes += t.RuntimeBytes;
                int max = Math.Max(t.Width, t.Height);
                if (max > 4096) Add(Severity.Warning, "Extremely large texture", t.Texture.name + " is " + t.Width + "×" + t.Height + ". Check whether that resolution is visually necessary.", t.Texture);
                else if (max > 2048) Add(Severity.Info, "Large texture", t.Texture.name + " is " + t.Width + "×" + t.Height + " and uses about " + NekoRankAdvisor.FormatBytes(t.RuntimeBytes) + " loaded memory. Texture Inspector can show the biggest VRAM wins.", t.Texture);
            }

            HashSet<Material> materials = NekoAvatarDiagnosticsUtil.CollectMaterials(_avatar);
            int unsupported = 0;
            foreach (Material mat in materials) if (!NekoAvatarDiagnosticsUtil.IsQuestAvatarShader(mat)) unsupported++;
            if (unsupported > 0) Add(Severity.Warning, "Quest-incompatible shaders detected", unsupported + " unique material(s) do not use VRChat/Mobile shaders. Android/Quest avatars may only use the mobile avatar shaders supplied by the VRChat SDK.", _avatar);
        }

        void CheckDuplicateAndUnsupportedComponents()
        {
            Animator[] animators = _avatar.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 4) Add(Severity.Warning, "Many Animator components", animators.Length + " Animator components were found. Extra animators count against performance and can make parameter ownership harder to reason about.", _avatar);

            int unsupportedQuest = _avatar.GetComponentsInChildren<Light>(true).Length +
                                   _avatar.GetComponentsInChildren<AudioSource>(true).Length +
                                   _avatar.GetComponentsInChildren<Camera>(true).Length +
                                   _avatar.GetComponentsInChildren<Cloth>(true).Length +
                                   _avatar.GetComponentsInChildren<Rigidbody>(true).Length +
                                   _avatar.GetComponentsInChildren<Collider>(true).Length;
            if (unsupportedQuest > 0) Add(Severity.Info, "PC-only / stripped components are present", unsupportedQuest + " Light, AudioSource, Camera, Cloth, Rigidbody or Collider components were found. Several of these are disabled or stripped for Android/Quest avatars; Quest Assistant can prepare a mobile copy.", _avatar);
        }

        void CheckCrossPlatform(Component pcDescriptor, GameObject questAvatar)
        {
            Component questDescriptor = NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(questAvatar);
            if (questDescriptor == null)
            {
                Add(Severity.Error, "Quest counterpart has no Avatar Descriptor", questAvatar.name + " cannot be compared as a VRChat avatar.", questAvatar);
                return;
            }
            List<NekoExpressionParameterInfo> pc = pcDescriptor == null ? new List<NekoExpressionParameterInfo>() : NekoAvatarDiagnosticsUtil.ReadExpressionParameters(pcDescriptor);
            List<NekoExpressionParameterInfo> quest = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(questDescriptor);
            int common = Math.Min(pc.Count, quest.Count);
            for (int i = 0; i < common; i++)
            {
                if (pc[i].Name != quest[i].Name || NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(pc[i].TypeName) != NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(quest[i].TypeName))
                {
                    Add(Severity.Error, "PC / Quest parameter order mismatch", "Entry " + i + " differs: PC='" + pc[i].Name + "' (" + pc[i].TypeName + ") vs Quest='" + quest[i].Name + "' (" + quest[i].TypeName + "). Cross-platform synced parameters should stay in the same position and type.", questAvatar);
                    break;
                }
            }
            if (pc.Count != quest.Count) Add(Severity.Warning, "PC / Quest parameter count differs", "PC has " + pc.Count + " parameters while Quest has " + quest.Count + ". The safest setup is the same Expression Parameters asset/order on both versions.", questAvatar);
        }

        void Add(Severity severity, string title, string detail, UnityEngine.Object target)
        {
            _findings.Add(new Finding { Severity = severity, Title = title, Detail = detail, Target = target });
        }

        string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune Avatar Doctor");
            sb.AppendLine("Avatar: " + (_avatar == null ? "None" : _avatar.name));
            if (_report != null)
            {
                sb.AppendLine("PC rank: " + (_pc == null ? "-" : NekoLoc.T(NekoPerfTable.RankKey(_pc.Overall))));
                sb.AppendLine("Mobile rank: " + (_mobile == null ? "-" : NekoLoc.T(NekoPerfTable.RankKey(_mobile.Overall))));
                sb.AppendLine("Expression parameters: " + _parameterCount + " / " + _parameterBits + " synced bits");
                sb.AppendLine("Texture memory: " + NekoRankAdvisor.FormatBytes(_textureBytes));
            }
            sb.AppendLine();
            for (int i = 0; i < _findings.Count; i++) sb.AppendLine("[" + _findings[i].Severity.ToString().ToUpperInvariant() + "] " + _findings[i].Title + " — " + _findings[i].Detail);
            return sb.ToString();
        }
    }
}
