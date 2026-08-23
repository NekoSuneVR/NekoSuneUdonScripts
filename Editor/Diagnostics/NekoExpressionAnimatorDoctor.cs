using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 25)]
    internal sealed class NekoExpressionAnimatorDoctorAddon : INekoAddon
    {
        public string Id { get { return "expression-animator-doctor"; } }
        public string TitleKey { get { return "exprdoctor.title"; } }
        public string DescriptionKey { get { return "exprdoctor.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "A"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoExpressionAnimatorDoctorWindow.Open(); }
    }

    internal sealed class NekoExpressionAnimatorDoctorWindow : EditorWindow
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
        Component _descriptor;
        List<NekoExpressionParameterInfo> _expressionParameters = new List<NekoExpressionParameterInfo>();
        List<RuntimeAnimatorController> _controllers = new List<RuntimeAnimatorController>();
        readonly List<Finding> _findings = new List<Finding>();
        readonly HashSet<string> _menuParameters = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _animatorParameters = new HashSet<string>(StringComparer.Ordinal);
        Vector2 _scroll;
        int _menuCount;
        int _controlCount;
        int _stateCount;
        int _transitionCount;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Expression and Animator Doctor", false, 25)]
        public static void Open()
        {
            var w = GetWindow<NekoExpressionAnimatorDoctorWindow>(false, "Expression Doctor", true);
            w.minSize = new Vector2(720f, 540f);
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
            NekoStyles.HeaderBar("Doctor", "Expressions", "Expression Menu, parameter and Animator consistency checks");
            EditorGUI.BeginChangeCheck();
            _avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", _avatar, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();
            if (_avatar == null) { EditorGUILayout.HelpBox("Select or drop an avatar.", MessageType.Info); return; }

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Expression / Animator summary", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Expression Parameters", _expressionParameters.Count + " · " + NekoAvatarDiagnosticsUtil.ParameterBits(_expressionParameters) + " synced bits");
            EditorGUILayout.LabelField("Menus / controls", _menuCount + " / " + _controlCount);
            EditorGUILayout.LabelField("Animator Controllers", _controllers.Count.ToString());
            EditorGUILayout.LabelField("States / transitions", _stateCount + " / " + _transitionCount);
            EditorGUILayout.LabelField("Findings", Count(Severity.Error) + " errors · " + Count(Severity.Warning) + " warnings · " + Count(Severity.Info) + " info");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", NekoStyles.PrimaryButton, GUILayout.Height(28f))) Scan();
            if (GUILayout.Button("Copy report", GUILayout.Width(110f), GUILayout.Height(28f))) EditorGUIUtility.systemCopyBuffer = BuildReport();
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawFindings(Severity.Error, "Errors");
            DrawFindings(Severity.Warning, "Warnings");
            DrawFindings(Severity.Info, "Information");
            EditorGUILayout.EndScrollView();
        }

        int Count(Severity severity)
        {
            int n = 0;
            for (int i = 0; i < _findings.Count; i++) if (_findings[i].Severity == severity) n++;
            return n;
        }

        void DrawFindings(Severity severity, string heading)
        {
            int count = Count(severity);
            if (count == 0) return;
            GUILayout.Space(5f);
            GUILayout.Label(heading + " (" + count + ")", EditorStyles.boldLabel);
            for (int i = 0; i < _findings.Count; i++)
            {
                Finding f = _findings[i];
                if (f.Severity != severity) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(f.Title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (f.Target != null && GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(52f)))
                {
                    Selection.activeObject = f.Target;
                    EditorGUIUtility.PingObject(f.Target);
                    AssetDatabase.OpenAsset(f.Target);
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label(f.Detail, NekoStyles.WrapLabel);
                EditorGUILayout.EndVertical();
            }
        }

        void Scan()
        {
            _findings.Clear();
            _menuParameters.Clear();
            _animatorParameters.Clear();
            _expressionParameters.Clear();
            _controllers.Clear();
            _menuCount = _controlCount = _stateCount = _transitionCount = 0;
            _descriptor = _avatar == null ? null : NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(_avatar);
            if (_avatar == null) { Repaint(); return; }
            if (_descriptor == null)
            {
                Add(Severity.Error, "Missing Avatar Descriptor", "Expression assets and VRChat playable layers cannot be inspected without a VRCAvatarDescriptor.", _avatar);
                Repaint();
                return;
            }

            _expressionParameters = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(_descriptor);
            Dictionary<string, NekoExpressionParameterInfo> expressionMap = NekoAvatarDiagnosticsUtil.ParameterMap(_expressionParameters);
            int bits = NekoAvatarDiagnosticsUtil.ParameterBits(_expressionParameters);
            if (bits > 256) Add(Severity.Error, "Synced parameter budget exceeded", bits + " / 256 bits are used.", NekoAvatarDiagnosticsUtil.ExpressionParameters(_descriptor));

            var duplicateNames = new HashSet<string>(StringComparer.Ordinal);
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _expressionParameters.Count; i++) if (!seenNames.Add(_expressionParameters[i].Name)) duplicateNames.Add(_expressionParameters[i].Name);
            foreach (string name in duplicateNames) Add(Severity.Error, "Duplicate Expression Parameter", "'" + name + "' is declared multiple times.", NekoAvatarDiagnosticsUtil.ExpressionParameters(_descriptor));

            UnityEngine.Object menu = NekoAvatarDiagnosticsUtil.ExpressionsMenu(_descriptor);
            if (menu == null) Add(Severity.Warning, "No Expressions Menu", "The descriptor has no root Expressions Menu assigned.", _descriptor);
            else ScanMenu(menu, expressionMap, new HashSet<int>(), "Root");

            _controllers = NekoAvatarDiagnosticsUtil.FindControllers(_avatar, _descriptor);
            for (int i = 0; i < _controllers.Count; i++) ScanController(_controllers[i], expressionMap);

            foreach (NekoExpressionParameterInfo p in _expressionParameters)
            {
                bool vrcft = p.Name.IndexOf("v2/", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!_menuParameters.Contains(p.Name) && !_animatorParameters.Contains(p.Name) && !vrcft)
                    Add(Severity.Info, "Expression parameter appears unused", "'" + p.Name + "' is not referenced by the scanned menu or Animator Controllers. It may still be used by OSC, contacts, Modular Avatar or another build-time tool.", NekoAvatarDiagnosticsUtil.ExpressionParameters(_descriptor));
            }

            foreach (string name in _menuParameters)
                if (!_animatorParameters.Contains(name) && expressionMap.ContainsKey(name))
                    Add(Severity.Info, "Menu parameter has no Animator parameter", "'" + name + "' is used by the menu but was not found in the scanned Animator Controllers. This can be valid for contact/OSC-only controls, but check it if the toggle seems to do nothing.", menu);

            Repaint();
        }

        void ScanMenu(object menu, Dictionary<string, NekoExpressionParameterInfo> expressionMap, HashSet<int> seen, string path)
        {
            if (menu == null || !seen.Add(menu.GetHashCode())) return;
            _menuCount++;
            IEnumerable controls = NekoAvatarDiagnosticsUtil.GetMember(menu, "controls", "Controls") as IEnumerable;
            if (controls == null) return;
            int count = 0;
            foreach (object control in controls)
            {
                if (control == null) continue;
                count++; _controlCount++;
                string label = Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(control, "name", "Name"));
                string type = Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(control, "type", "Type"));
                object parameter = NekoAvatarDiagnosticsUtil.GetMember(control, "parameter", "Parameter");
                string parameterName = parameter == null ? "" : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(parameter, "name", "Name"));
                if (!string.IsNullOrEmpty(parameterName))
                {
                    _menuParameters.Add(parameterName);
                    if (!expressionMap.ContainsKey(parameterName)) Add(Severity.Error, "Menu parameter does not exist", path + " / " + label + " references '" + parameterName + "'.", menu as UnityEngine.Object);
                }

                IEnumerable subs = NekoAvatarDiagnosticsUtil.GetMember(control, "subParameters", "SubParameters") as IEnumerable;
                if (subs != null)
                {
                    foreach (object sub in subs)
                    {
                        if (sub == null) continue;
                        string subName = Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(sub, "name", "Name"));
                        if (string.IsNullOrEmpty(subName)) continue;
                        _menuParameters.Add(subName);
                        NekoExpressionParameterInfo info;
                        if (!expressionMap.TryGetValue(subName, out info)) Add(Severity.Error, "Puppet sub-parameter does not exist", path + " / " + label + " references '" + subName + "'.", menu as UnityEngine.Object);
                        else if (!string.Equals(NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(info.TypeName), "Float", StringComparison.OrdinalIgnoreCase)) Add(Severity.Warning, "Puppet sub-parameter is not Float", path + " / " + label + " uses '" + subName + "' as a puppet axis, but it is " + info.TypeName + ".", menu as UnityEngine.Object);
                    }
                }

                object subMenu = NekoAvatarDiagnosticsUtil.GetMember(control, "subMenu", "SubMenu");
                if (type.IndexOf("SubMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (subMenu == null) Add(Severity.Error, "SubMenu control has no menu", path + " / " + label + " is empty.", menu as UnityEngine.Object);
                    else ScanMenu(subMenu, expressionMap, seen, path + " / " + label);
                }
            }
            if (count > 8) Add(Severity.Error, "Too many controls in one menu", path + " contains " + count + " controls; VRChat menus allow at most 8 controls per menu.", menu as UnityEngine.Object);
        }

        void ScanController(RuntimeAnimatorController runtime, Dictionary<string, NekoExpressionParameterInfo> expressionMap)
        {
            AnimatorController controller = runtime as AnimatorController;
            if (controller == null) return;
            var local = new HashSet<string>(StringComparer.Ordinal);
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter p = parameters[i];
                _animatorParameters.Add(p.name);
                if (!local.Add(p.name)) Add(Severity.Error, "Duplicate Animator parameter", controller.name + " declares '" + p.name + "' multiple times.", controller);
                NekoExpressionParameterInfo expression;
                if (expressionMap.TryGetValue(p.name, out expression))
                {
                    string a = NekoAvatarDiagnosticsUtil.AnimatorTypeName(p.type);
                    string e = NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(expression.TypeName);
                    if (!string.Equals(a, e, StringComparison.OrdinalIgnoreCase)) Add(Severity.Warning, "Expression / Animator type mismatch", "'" + p.name + "' is " + e + " in Expression Parameters but " + a + " in " + controller.name + ".", controller);
                }
            }

            bool writeOn = false, writeOff = false;
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++) ScanMachine(controller, layers[i].stateMachine, ref writeOn, ref writeOff);
            if (writeOn && writeOff) Add(Severity.Warning, "Mixed Write Defaults", controller.name + " contains both Write Defaults On and Off states. Verify the mixture is deliberate.", controller);
        }

        void ScanMachine(AnimatorController controller, AnimatorStateMachine machine, ref bool writeOn, ref bool writeOff)
        {
            if (machine == null) return;
            var inbound = new HashSet<AnimatorState>();
            if (machine.defaultState != null) inbound.Add(machine.defaultState);

            AnimatorStateTransition[] any = machine.anyStateTransitions;
            for (int i = 0; i < any.Length; i++)
            {
                _transitionCount++;
                if (any[i].destinationState != null) inbound.Add(any[i].destinationState);
                CheckTransition(controller, any[i], "Any State");
                if (any[i].conditions == null || any[i].conditions.Length == 0) Add(Severity.Warning, "Unconditional Any State transition", controller.name + " has an Any State transition with no conditions. It can dominate other states unless this is intentional.", controller);
            }

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null) continue;
                _stateCount++;
                if (state.writeDefaultValues) writeOn = true; else writeOff = true;
                AnimatorStateTransition[] transitions = state.transitions;
                for (int t = 0; t < transitions.Length; t++)
                {
                    _transitionCount++;
                    if (transitions[t].destinationState != null) inbound.Add(transitions[t].destinationState);
                    CheckTransition(controller, transitions[t], state.name);
                }
                CheckParameterDrivers(controller, state);
            }

            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state != null && !inbound.Contains(state)) Add(Severity.Info, "Potentially unreachable Animator state", controller.name + " / " + state.name + " has no direct inbound transition in this state machine. Nested state-machine transitions can make this a false positive, so treat it as a review hint.", controller);
            }

            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++) ScanMachine(controller, children[i].stateMachine, ref writeOn, ref writeOff);
        }

        void CheckTransition(AnimatorController controller, AnimatorStateTransition transition, string source)
        {
            if (transition == null) return;
            AnimatorCondition[] conditions = transition.conditions;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!_animatorParameters.Contains(conditions[i].parameter)) Add(Severity.Error, "Transition references missing Animator parameter", controller.name + " / " + source + " uses condition '" + conditions[i].parameter + "' which is not declared in the controller.", controller);
            }
        }

        void CheckParameterDrivers(AnimatorController controller, AnimatorState state)
        {
            StateMachineBehaviour[] behaviours = state.behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                StateMachineBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name.IndexOf("ParameterDriver", StringComparison.OrdinalIgnoreCase) < 0) continue;
                IEnumerable entries = NekoAvatarDiagnosticsUtil.GetMember(behaviour, "parameters", "Parameters") as IEnumerable;
                if (entries == null) continue;
                var writes = new HashSet<string>(StringComparer.Ordinal);
                foreach (object entry in entries)
                {
                    string name = entry == null ? "" : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(entry, "name", "Name", "destParam", "DestParam"));
                    if (string.IsNullOrEmpty(name)) continue;
                    if (!writes.Add(name)) Add(Severity.Warning, "Parameter Driver writes the same parameter twice", controller.name + " / " + state.name + " contains multiple Parameter Driver operations for '" + name + "'. Order-dependent writes can be difficult to debug.", controller);
                }
            }
        }

        void Add(Severity severity, string title, string detail, UnityEngine.Object target)
        {
            _findings.Add(new Finding { Severity = severity, Title = title, Detail = detail, Target = target });
        }

        string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune Expression & Animator Doctor");
            sb.AppendLine("Avatar: " + (_avatar == null ? "None" : _avatar.name));
            sb.AppendLine("Parameters: " + _expressionParameters.Count + " / " + NekoAvatarDiagnosticsUtil.ParameterBits(_expressionParameters) + " synced bits");
            sb.AppendLine("Menus/controls: " + _menuCount + "/" + _controlCount);
            sb.AppendLine("Controllers/states/transitions: " + _controllers.Count + "/" + _stateCount + "/" + _transitionCount);
            sb.AppendLine();
            for (int i = 0; i < _findings.Count; i++) sb.AppendLine("[" + _findings[i].Severity.ToString().ToUpperInvariant() + "] " + _findings[i].Title + " — " + _findings[i].Detail);
            return sb.ToString();
        }
    }
}
