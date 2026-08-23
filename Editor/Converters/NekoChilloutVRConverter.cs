using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 45)]
    internal sealed class NekoChilloutVRConverterAddon : INekoAddon
    {
        public string Id { get { return "chilloutvr-converter"; } }
        public string TitleKey { get { return "cvr.title"; } }
        public string DescriptionKey { get { return "cvr.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "C"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoChilloutVRConverterWindow.Open(); }
    }

    internal sealed class NekoChilloutVRConverterWindow : EditorWindow
    {
        GameObject _avatar;
        bool _convertAdvancedSettings = true;
        bool _generateAasAnimator = true;
        bool _stripVrcComponents = true;
        bool _convertPhysBones = true;
        string _status = "Ready";

        Type CvrAvatarType { get { return NekoCckCompatibility.AvatarType; } }
        Type DynamicBoneType { get { return NekoAvatarDiagnosticsUtil.FindType("DynamicBone"); } }

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Convert to ChilloutVR", false, 45)]
        public static void Open()
        {
            var w = GetWindow<NekoChilloutVRConverterWindow>(false, "ChilloutVR Converter", true);
            w.minSize = new Vector2(700f, 550f);
            w.Show();
        }

        [MenuItem(NekoPaths.MenuRoot + "Avatar/ChilloutVR/Convert Avatar", false, 45)]
        static void OpenFromCvrMenu() { Open(); }

        void OnEnable()
        {
            if (_avatar == null) _avatar = NekoAvatarDiagnosticsUtil.SuggestedAvatarFromSelection();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Converter", "ChilloutVR", "CCK 3 legacy + CCK 4 stable avatar conversion with AAS, toggles, sliders, dropdowns, viewpoint and visemes");
            NekoCckCompatibility.DrawStatusBox();
            _avatar = (GameObject)EditorGUILayout.ObjectField("VRChat avatar", _avatar, typeof(GameObject), true);

            if (!NekoCckCompatibility.Installed || CvrAvatarType == null)
            {
                EditorGUILayout.HelpBox("ChilloutVR CCK was not detected. Install CCK 4 stable (recommended) or CCK 3 legacy. NekoSune creates real CVRAvatar/Advanced Avatar Settings data and does not emit placeholder components.", MessageType.Warning);
                if (GUILayout.Button("Open ChilloutVR CCK setup documentation")) Application.OpenURL("https://docs.chilloutvr.net/cck/setup/");
                return;
            }

            Component descriptor = _avatar == null ? null : NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(_avatar);
            int physBones = _avatar == null ? 0 : NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_avatar, "VRCPhysBone", "VRCPhysBoneBase").Count;
            int parameters = descriptor == null ? 0 : NekoAvatarDiagnosticsUtil.ReadExpressionParameters(descriptor).Count;

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Detected source", NekoStyles.SlotName);
            EditorGUILayout.LabelField("VRChat descriptor", descriptor == null ? "Missing" : "Yes");
            EditorGUILayout.LabelField("Expression parameters", parameters.ToString());
            EditorGUILayout.LabelField("PhysBones", physBones.ToString());
            EditorGUILayout.LabelField("Dynamic Bone v1.x", DynamicBoneType == null ? "Not installed" : "Detected");
            EditorGUILayout.LabelField("State", _status);
            EditorGUILayout.EndVertical();

            GUILayout.Label("Conversion", EditorStyles.boldLabel);
            _convertAdvancedSettings = EditorGUILayout.ToggleLeft("Convert VRChat expression parameters/menu labels to CVR Advanced Avatar Settings", _convertAdvancedSettings);
            _generateAasAnimator = EditorGUILayout.ToggleLeft("Ask installed CCK to generate/update its AAS Animator after conversion", _generateAasAnimator);
            _stripVrcComponents = EditorGUILayout.ToggleLeft("Strip VRChat-only components from the generated ChilloutVR copy", _stripVrcComponents);
            using (new EditorGUI.DisabledScope(DynamicBoneType == null))
                _convertPhysBones = EditorGUILayout.ToggleLeft("Convert PhysBone roots/settings to Dynamic Bone when possible", _convertPhysBones && DynamicBoneType != null);

            if (physBones > 0 && DynamicBoneType == null)
                EditorGUILayout.HelpBox("This avatar has PhysBones but Dynamic Bone v1.x is not installed. If you strip VRChat components, those PhysBones will not have replacement physics until you configure a CVR-supported dynamics solution.", MessageType.Warning);

            EditorGUILayout.HelpBox("Bool parameters become CVR toggles, Float parameters become sliders, and Int parameters become dropdowns where multiple values can be discovered. Menu control names are reused as friendly labels. The VRChat source hierarchy is never modified.", MessageType.Info);

            using (new EditorGUI.DisabledScope(_avatar == null || descriptor == null))
            {
                if (GUILayout.Button("Create ChilloutVR Avatar Copy", NekoStyles.PrimaryButton, GUILayout.Height(34f))) ConvertAvatar();
            }
        }

        void ConvertAvatar()
        {
            Component sourceDescriptor = NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(_avatar);
            if (sourceDescriptor == null) return;
            if (_stripVrcComponents && NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_avatar, "VRCPhysBone", "VRCPhysBoneBase").Count > 0 && DynamicBoneType == null)
            {
                if (!EditorUtility.DisplayDialog("NekoSune ChilloutVR Converter", "Dynamic Bone is not installed. Continuing with Strip VRChat components removes PhysBones from the CVR copy without replacing their physics. Continue?", "Continue", "Cancel")) return;
            }

            try
            {
                _status = "Cloning avatar…"; Repaint();
                GameObject copy = Instantiate(_avatar, _avatar.transform.parent);
                copy.name = _avatar.name + " [ChilloutVR]";
                copy.SetActive(true);
                Undo.RegisterCreatedObjectUndo(copy, "Create ChilloutVR avatar copy");

                Component cvrAvatar = NekoCckCompatibility.EnsureComponent(copy, CvrAvatarType);
                if (cvrAvatar == null) throw new InvalidOperationException("Could not add CVRAvatar from " + NekoCckCompatibility.DisplayName + ".");
                PopulateCvrAvatar(sourceDescriptor, copy, cvrAvatar);

                AnimatorController baseController = CreateCvrBaseController(sourceDescriptor, copy);
                if (_convertAdvancedSettings) ConvertAdvancedSettings(sourceDescriptor, cvrAvatar, baseController);
                if (_convertPhysBones && DynamicBoneType != null) ConvertPhysBones(copy);
                if (_stripVrcComponents) StripVrcComponents(copy, cvrAvatar);
                if (_generateAasAnimator && _convertAdvancedSettings) TryGenerateAasAnimator(cvrAvatar);

                EditorUtility.SetDirty(cvrAvatar);
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = copy;
                EditorGUIUtility.PingObject(copy);
                _status = "Conversion complete — review the CVR Avatar/AAS in " + NekoCckCompatibility.DisplayName;
                Debug.Log("[NekoSune ChilloutVR Converter] Created " + copy.name + " using " + NekoCckCompatibility.DisplayName);
            }
            catch (Exception e)
            {
                _status = "Conversion failed — see Console";
                Debug.LogException(e is TargetInvocationException && e.InnerException != null ? e.InnerException : e);
            }
            Repaint();
        }

        void PopulateCvrAvatar(Component sourceDescriptor, GameObject copy, Component cvrAvatar)
        {
            object view = NekoAvatarDiagnosticsUtil.GetMember(sourceDescriptor, "ViewPosition", "viewPosition");
            if (view is Vector3)
            {
                NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, view, "viewPosition", "ViewPosition");
                NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, view, "voicePosition", "VoicePosition");
            }

            Animator animator = copy.GetComponent<Animator>();
            if (animator != null)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null) NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, head, "voiceParent", "VoiceParent");
            }

            SkinnedMeshRenderer sourceFace = NekoAvatarDiagnosticsUtil.GetMember(sourceDescriptor, "VisemeSkinnedMesh", "visemeSkinnedMesh") as SkinnedMeshRenderer;
            Transform equivalent = sourceFace == null ? null : FindEquivalent(copy.transform, _avatar.transform, sourceFace.transform);
            SkinnedMeshRenderer copyFace = equivalent == null ? null : equivalent.GetComponent<SkinnedMeshRenderer>();
            if (copyFace == null) copyFace = FindFaceMesh(copy);
            if (copyFace != null) NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, copyFace, "bodyMesh", "BodyMesh");

            string[] shapes = ToStringArray(NekoAvatarDiagnosticsUtil.GetMember(sourceDescriptor, "VisemeBlendShapes", "visemeBlendShapes"));
            if (shapes.Length > 0)
            {
                NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, true, "useVisemeLipsync", "UseVisemeLipsync");
                Array targetShapes = NekoAvatarDiagnosticsUtil.GetMember(cvrAvatar, "visemeBlendshapes", "VisemeBlendshapes") as Array;
                if (targetShapes != null)
                {
                    int n = Math.Min(targetShapes.Length, shapes.Length);
                    for (int i = 0; i < n; i++) targetShapes.SetValue(shapes[i], i);
                    NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, targetShapes, "visemeBlendshapes", "VisemeBlendshapes");
                }
            }

            string blink = FindBlinkShape(sourceDescriptor, sourceFace);
            if (!string.IsNullOrEmpty(blink))
            {
                NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, true, "useBlinkBlendshapes", "UseBlinkBlendshapes");
                Array blinkArray = NekoAvatarDiagnosticsUtil.GetMember(cvrAvatar, "blinkBlendshape", "BlinkBlendshape") as Array;
                if (blinkArray != null && blinkArray.Length > 0)
                {
                    blinkArray.SetValue(blink, 0);
                    NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, blinkArray, "blinkBlendshape", "BlinkBlendshape");
                }
            }
            NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, true, "avatarUsesAdvancedSettings", "AvatarUsesAdvancedSettings");
        }

        AnimatorController CreateCvrBaseController(Component sourceDescriptor, GameObject copy)
        {
            RuntimeAnimatorController fx = FindFxController(sourceDescriptor);
            if (fx == null)
            {
                Animator animator = copy.GetComponent<Animator>();
                fx = animator == null ? null : animator.runtimeAnimatorController;
            }
            AnimatorOverrideController over = fx as AnimatorOverrideController;
            AnimatorController sourceController = (over == null ? fx : over.runtimeAnimatorController) as AnimatorController;
            if (sourceController == null) return null;

            string folder = EnsureFolder("Assets/NekoSune/Avatars/ChilloutVR/" + SafeName(copy.name));
            string sourcePath = AssetDatabase.GetAssetPath(sourceController);
            if (!string.IsNullOrEmpty(sourcePath))
            {
                string copyPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/CVR_" + SafeName(sourceController.name) + ".controller");
                if (AssetDatabase.CopyAsset(sourcePath, copyPath))
                {
                    AssetDatabase.ImportAsset(copyPath);
                    return AssetDatabase.LoadAssetAtPath<AnimatorController>(copyPath);
                }
            }
            return sourceController;
        }

        RuntimeAnimatorController FindFxController(Component descriptor)
        {
            IEnumerable layers = NekoAvatarDiagnosticsUtil.GetMember(descriptor, "baseAnimationLayers", "BaseAnimationLayers") as IEnumerable;
            if (layers == null) return null;
            foreach (object layer in layers)
            {
                string type = Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(layer, "type", "Type"));
                if (type.IndexOf("FX", StringComparison.OrdinalIgnoreCase) < 0) continue;
                return NekoAvatarDiagnosticsUtil.GetMember(layer, "animatorController", "AnimatorController") as RuntimeAnimatorController;
            }
            return null;
        }

        void ConvertAdvancedSettings(Component sourceDescriptor, Component cvrAvatar, AnimatorController baseController)
        {
            Type entryType = NekoCckCompatibility.FindType("ABI.CCK.Scripts.CVRAdvancedSettingsEntry", "CVR.CCK.Scripts.CVRAdvancedSettingsEntry");
            Type toggleType = NekoCckCompatibility.FindType("ABI.CCK.Scripts.CVRAdvancesAvatarSettingGameObjectToggle", "CVR.CCK.Scripts.CVRAdvancesAvatarSettingGameObjectToggle");
            Type sliderType = NekoCckCompatibility.FindType("ABI.CCK.Scripts.CVRAdvancesAvatarSettingSlider", "CVR.CCK.Scripts.CVRAdvancesAvatarSettingSlider");
            Type dropdownType = NekoCckCompatibility.FindType("ABI.CCK.Scripts.CVRAdvancesAvatarSettingGameObjectDropdown", "CVR.CCK.Scripts.CVRAdvancesAvatarSettingGameObjectDropdown");
            Type dropdownEntryType = NekoCckCompatibility.FindType("ABI.CCK.Scripts.CVRAdvancedSettingsDropDownEntry", "CVR.CCK.Scripts.CVRAdvancedSettingsDropDownEntry");
            if (entryType == null || toggleType == null || sliderType == null || dropdownType == null || dropdownEntryType == null)
                throw new InvalidOperationException("The installed " + NekoCckCompatibility.DisplayName + " does not expose the expected Advanced Avatar Settings types. Configure AAS manually or update NekoSune for that CCK build.");

            object avatarSettings = NekoAvatarDiagnosticsUtil.GetMember(cvrAvatar, "avatarSettings", "AvatarSettings");
            if (avatarSettings == null) throw new InvalidOperationException("CVRAvatar.avatarSettings could not be read from this CCK generation.");
            if (baseController != null) NekoAvatarDiagnosticsUtil.SetMember(avatarSettings, baseController, "baseController", "BaseController");

            IDictionary friendlyNames = BuildFriendlyMenuNames(sourceDescriptor);
            List<NekoExpressionParameterInfo> parameters = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(sourceDescriptor);
            IList settingsList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entryType));

            for (int i = 0; i < parameters.Count; i++)
            {
                NekoExpressionParameterInfo p = parameters[i];
                object entry = Activator.CreateInstance(entryType);
                string friendly = friendlyNames.Contains(p.Name) ? Convert.ToString(friendlyNames[p.Name]) : p.Name;
                NekoAvatarDiagnosticsUtil.SetMember(entry, friendly, "name", "Name");
                NekoAvatarDiagnosticsUtil.SetMember(entry, p.Name, "machineName", "MachineName", "parameterName", "ParameterName");

                string normalizedType = NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(p.TypeName);
                if (normalizedType == "Bool")
                {
                    object setting = Activator.CreateInstance(toggleType);
                    NekoAvatarDiagnosticsUtil.SetMember(setting, p.DefaultValue != 0f, "defaultValue", "DefaultValue");
                    NekoAvatarDiagnosticsUtil.SetMember(entry, "GameObjectToggle", "type", "Type");
                    NekoAvatarDiagnosticsUtil.SetMember(entry, setting, "setting", "Setting");
                }
                else if (normalizedType == "Float")
                {
                    object setting = Activator.CreateInstance(sliderType);
                    NekoAvatarDiagnosticsUtil.SetMember(setting, p.DefaultValue, "defaultValue", "DefaultValue");
                    NekoAvatarDiagnosticsUtil.SetMember(entry, "Slider", "type", "Type");
                    NekoAvatarDiagnosticsUtil.SetMember(entry, setting, "setting", "Setting");
                }
                else if (normalizedType == "Int")
                {
                    List<int> options = FindIntOptions(p.Name, sourceDescriptor);
                    if (options.Count <= 1)
                    {
                        object setting = Activator.CreateInstance(toggleType);
                        NekoAvatarDiagnosticsUtil.SetMember(setting, p.DefaultValue != 0f, "defaultValue", "DefaultValue");
                        NekoAvatarDiagnosticsUtil.SetMember(entry, "GameObjectToggle", "type", "Type");
                        NekoAvatarDiagnosticsUtil.SetMember(entry, setting, "setting", "Setting");
                    }
                    else
                    {
                        object setting = Activator.CreateInstance(dropdownType);
                        NekoAvatarDiagnosticsUtil.SetMember(setting, (int)p.DefaultValue, "defaultValue", "DefaultValue");
                        IList optionList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dropdownEntryType));
                        options.Sort();
                        for (int o = 0; o < options.Count; o++)
                        {
                            object option = Activator.CreateInstance(dropdownEntryType);
                            NekoAvatarDiagnosticsUtil.SetMember(option, options[o].ToString(), "name", "Name");
                            NekoAvatarDiagnosticsUtil.SetMember(option, options[o], "value", "Value");
                            optionList.Add(option);
                        }
                        NekoAvatarDiagnosticsUtil.SetMember(setting, optionList, "options", "Options");
                        NekoAvatarDiagnosticsUtil.SetMember(entry, "GameObjectDropdown", "type", "Type");
                        NekoAvatarDiagnosticsUtil.SetMember(entry, setting, "setting", "Setting");
                    }
                }
                settingsList.Add(entry);
            }

            if (!NekoAvatarDiagnosticsUtil.SetMember(avatarSettings, settingsList, "settings", "Settings"))
                throw new InvalidOperationException("Could not assign generated Advanced Avatar Settings to this CCK generation.");
            NekoAvatarDiagnosticsUtil.SetMember(cvrAvatar, avatarSettings, "avatarSettings", "AvatarSettings");
        }

        IDictionary BuildFriendlyMenuNames(Component descriptor)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            CollectFriendlyNames(NekoAvatarDiagnosticsUtil.ExpressionsMenu(descriptor), map, new HashSet<int>());
            return map;
        }

        void CollectFriendlyNames(object menu, Dictionary<string, string> map, HashSet<int> seen)
        {
            if (menu == null || !seen.Add(menu.GetHashCode())) return;
            IEnumerable controls = NekoAvatarDiagnosticsUtil.GetMember(menu, "controls", "Controls") as IEnumerable;
            if (controls == null) return;
            foreach (object control in controls)
            {
                if (control == null) continue;
                string name = Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(control, "name", "Name"));
                object parameter = NekoAvatarDiagnosticsUtil.GetMember(control, "parameter", "Parameter");
                string param = parameter == null ? "" : Convert.ToString(NekoAvatarDiagnosticsUtil.GetMember(parameter, "name", "Name"));
                if (!string.IsNullOrEmpty(param) && !map.ContainsKey(param)) map.Add(param, string.IsNullOrEmpty(name) ? param : name);
                object submenu = NekoAvatarDiagnosticsUtil.GetMember(control, "subMenu", "SubMenu");
                if (submenu != null) CollectFriendlyNames(submenu, map, seen);
            }
        }

        List<int> FindIntOptions(string parameterName, Component descriptor)
        {
            var set = new HashSet<int>();
            List<RuntimeAnimatorController> controllers = NekoAvatarDiagnosticsUtil.FindControllers(_avatar, descriptor);
            for (int i = 0; i < controllers.Count; i++)
            {
                AnimatorController ac = controllers[i] as AnimatorController;
                if (ac == null) continue;
                for (int l = 0; l < ac.layers.Length; l++) CollectIntOptions(ac.layers[l].stateMachine, parameterName, set);
            }
            if (set.Count == 0) { set.Add(0); set.Add(1); }
            return new List<int>(set);
        }

        void CollectIntOptions(AnimatorStateMachine machine, string parameterName, HashSet<int> values)
        {
            if (machine == null) return;
            AnimatorStateTransition[] any = machine.anyStateTransitions;
            for (int i = 0; i < any.Length; i++) CollectConditions(any[i].conditions, parameterName, values);
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorStateTransition[] transitions = states[i].state.transitions;
                for (int t = 0; t < transitions.Length; t++) CollectConditions(transitions[t].conditions, parameterName, values);
            }
            ChildAnimatorStateMachine[] children = machine.stateMachines;
            for (int i = 0; i < children.Length; i++) CollectIntOptions(children[i].stateMachine, parameterName, values);
        }

        void CollectConditions(AnimatorCondition[] conditions, string parameterName, HashSet<int> values)
        {
            for (int i = 0; i < conditions.Length; i++) if (conditions[i].parameter == parameterName) values.Add(Mathf.RoundToInt(conditions[i].threshold));
        }

        void ConvertPhysBones(GameObject copy)
        {
            List<Component> bones = NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(copy, "VRCPhysBone", "VRCPhysBoneBase");
            int converted = 0;
            for (int i = 0; i < bones.Count; i++)
            {
                Component pb = bones[i];
                Component dynamicBone = pb.gameObject.AddComponent(DynamicBoneType);
                Transform root = NekoAvatarDiagnosticsUtil.GetMember(pb, "rootTransform", "RootTransform") as Transform;
                if (root == null) root = pb.transform;
                NekoAvatarDiagnosticsUtil.SetMember(dynamicBone, root, "m_Root");
                SetFloatFrom(dynamicBone, pb, "m_Elasticity", "spring", "Spring");
                SetFloatFrom(dynamicBone, pb, "m_Stiffness", "stiffness", "Stiffness");
                SetFloatFrom(dynamicBone, pb, "m_Inert", "immobile", "Immobile");
                SetFloatFrom(dynamicBone, pb, "m_Radius", "radius", "Radius");
                object gravity = NekoAvatarDiagnosticsUtil.GetMember(pb, "gravity", "Gravity");
                if (gravity != null)
                {
                    try { NekoAvatarDiagnosticsUtil.SetMember(dynamicBone, Vector3.down * Convert.ToSingle(gravity), "m_Gravity"); } catch { }
                }
                object ignored = NekoAvatarDiagnosticsUtil.GetMember(pb, "ignoreTransforms", "IgnoreTransforms");
                if (ignored != null) NekoAvatarDiagnosticsUtil.SetMember(dynamicBone, ignored, "m_Exclusions");
                converted++;
            }
            if (converted > 0) Debug.Log("[NekoSune ChilloutVR Converter] Created " + converted + " Dynamic Bone component(s) from PhysBone roots/settings. PhysBone collider geometry is not auto-translated; review dynamics before upload.");
        }

        void SetFloatFrom(Component destination, Component source, string destinationName, params string[] sourceNames)
        {
            object value = NekoAvatarDiagnosticsUtil.GetMember(source, sourceNames);
            if (value == null) return;
            try { NekoAvatarDiagnosticsUtil.SetMember(destination, Mathf.Clamp01(Convert.ToSingle(value)), destinationName); } catch { }
        }

        void StripVrcComponents(GameObject copy, Component keep)
        {
            Component[] components = copy.GetComponentsInChildren<Component>(true);
            int removed = 0;
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component c = components[i];
                if (c == null || c == keep || !NekoCckCompatibility.IsVrcComponent(c)) continue;
                Undo.DestroyObjectImmediate(c);
                removed++;
            }
            Debug.Log("[NekoSune ChilloutVR Converter] Removed " + removed + " VRChat/Udon component(s) from the CVR copy.");
        }

        void TryGenerateAasAnimator(Component cvrAvatar)
        {
            UnityEditor.Editor editor = null;
            try
            {
                editor = UnityEditor.Editor.CreateEditor(cvrAvatar);
                if (editor == null) return;
                MethodInfo create = editor.GetType().GetMethod("CreateAnimator", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (create != null)
                {
                    create.Invoke(editor, null);
                    return;
                }
                Debug.LogWarning("[NekoSune ChilloutVR Converter] " + NekoCckCompatibility.DisplayName + " does not expose the legacy AAS CreateAnimator inspector method. Open CVR Avatar/AAS and generate/update its Animator manually if needed.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[NekoSune ChilloutVR Converter] AAS Animator generation could not be invoked automatically: " + (e.InnerException == null ? e.Message : e.InnerException.Message));
            }
            finally
            {
                if (editor != null) DestroyImmediate(editor);
            }
        }

        string FindBlinkShape(Component descriptor, SkinnedMeshRenderer sourceFace)
        {
            object settings = NekoAvatarDiagnosticsUtil.GetMember(descriptor, "customEyeLookSettings", "CustomEyeLookSettings");
            if (settings == null || sourceFace == null || sourceFace.sharedMesh == null) return null;
            Array indexes = NekoAvatarDiagnosticsUtil.GetMember(settings, "eyelidsBlendshapes", "EyelidsBlendshapes") as Array;
            if (indexes == null || indexes.Length == 0) return null;
            try
            {
                int index = Convert.ToInt32(indexes.GetValue(0));
                if (index >= 0 && index < sourceFace.sharedMesh.blendShapeCount) return sourceFace.sharedMesh.GetBlendShapeName(index);
            }
            catch { }
            return null;
        }

        SkinnedMeshRenderer FindFaceMesh(GameObject copy)
        {
            SkinnedMeshRenderer[] renderers = copy.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null && renderers[i].sharedMesh != null && renderers[i].sharedMesh.blendShapeCount >= 15) return renderers[i];
            return renderers.Length > 0 ? renderers[0] : null;
        }

        static Transform FindEquivalent(Transform copyRoot, Transform sourceRoot, Transform sourceTarget)
        {
            if (sourceTarget == null) return null;
            string path = NekoAvatarDiagnosticsUtil.ObjectPath(sourceRoot, sourceTarget);
            if (path == sourceRoot.name || string.IsNullOrEmpty(path)) return copyRoot;
            return copyRoot.Find(path);
        }

        static string[] ToStringArray(object value)
        {
            string[] direct = value as string[];
            if (direct != null) return direct;
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null) return new string[0];
            var result = new List<string>();
            foreach (object item in enumerable) result.Add(Convert.ToString(item));
            return result.ToArray();
        }

        static string SafeName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Avatar";
            foreach (char c in Path.GetInvalidFileNameChars()) input = input.Replace(c, '_');
            return input.Replace('/', '_').Replace('\\', '_');
        }

        static string EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            return current;
        }
    }
}
