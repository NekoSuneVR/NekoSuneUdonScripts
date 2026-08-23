using System;
using System.Collections;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    internal static class NekoChilloutVRToggleBuilder
    {
        [MenuItem(NekoPaths.MenuRoot + "Avatar/ChilloutVR/Generate Animator Toggles for Selected Prop", false, 48)]
        static void GenerateForSelection()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null)
            {
                EditorUtility.DisplayDialog("NekoSune CVR Toggles", "Select the ChilloutVR prop/root first.", "OK");
                return;
            }
            int count = Generate(root);
            EditorUtility.DisplayDialog("NekoSune CVR Toggles", "Generated " + count + " CVR Animator Bool toggle control(s). Move/style the generated panel before publishing.", "OK");
        }

        [MenuItem(NekoPaths.MenuRoot + "Avatar/ChilloutVR/Generate Animator Toggles for Selected Prop", true)]
        static bool ValidateGenerate() { return Selection.activeGameObject != null; }

        public static int Generate(GameObject root)
        {
            if (root == null || NekoCckCompatibility.InteractableType == null ||
                NekoCckCompatibility.InteractableActionType == null || NekoCckCompatibility.InteractableOperationType == null)
                return 0;

            GameObject panel = new GameObject("[NekoSune CVR Prop Toggles - MOVE/STYLE ME]");
            panel.transform.SetParent(root.transform, false);
            int generated = 0;
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                RuntimeAnimatorController runtime = animators[i] == null ? null : animators[i].runtimeAnimatorController;
                AnimatorOverrideController over = runtime as AnimatorOverrideController;
                AnimatorController controller = (over == null ? runtime : over.runtimeAnimatorController) as AnimatorController;
                if (controller == null) continue;

                AnimatorControllerParameter[] parameters = controller.parameters;
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (parameters[p].type != AnimatorControllerParameterType.Bool) continue;
                    GameObject control = new GameObject(animators[i].gameObject.name + " - " + parameters[p].name);
                    control.transform.SetParent(panel.transform, false);
                    control.transform.localPosition = new Vector3(0f, generated * 0.12f, 0f);
                    BoxCollider collider = control.AddComponent<BoxCollider>();
                    collider.size = new Vector3(0.8f, 0.1f, 0.05f);
                    TextMesh label = control.AddComponent<TextMesh>();
                    label.text = parameters[p].name;
                    label.characterSize = 0.04f;
                    label.anchor = TextAnchor.MiddleCenter;

                    Component interactable = NekoCckCompatibility.EnsureComponent(control, NekoCckCompatibility.InteractableType);
                    if (interactable != null && AddToggle(interactable, animators[i].gameObject, parameters[p].name))
                    {
                        NekoAvatarDiagnosticsUtil.SetMember(interactable, parameters[p].name, "tooltip", "Tooltip");
                        generated++;
                    }
                }
            }

            if (generated == 0) UnityEngine.Object.DestroyImmediate(panel);
            else Selection.activeGameObject = panel;
            return generated;
        }

        static bool AddToggle(Component interactable, GameObject animatorObject, string parameter)
        {
            try
            {
                object action = Activator.CreateInstance(NekoCckCompatibility.InteractableActionType);
                object operation = Activator.CreateInstance(NekoCckCompatibility.InteractableOperationType);
                NekoAvatarDiagnosticsUtil.SetMember(action, "OnInteractDown", "actionType", "ActionType");
                NekoAvatarDiagnosticsUtil.SetMember(action, "GlobalNetworkedBuffered", "execType", "ExecType", "executionType", "ExecutionType");
                NekoAvatarDiagnosticsUtil.SetMember(operation, "ToggleAnimatorBoolValue", "type", "Type");
                NekoAvatarDiagnosticsUtil.SetMember(operation, parameter, "stringVal", "StringVal", "parameterName", "ParameterName");

                IList targets = NekoAvatarDiagnosticsUtil.GetMember(operation, "targets", "Targets") as IList;
                if (targets != null) targets.Add(animatorObject);
                IList operations = NekoAvatarDiagnosticsUtil.GetMember(action, "operations", "Operations") as IList;
                if (operations == null) return false;
                operations.Add(operation);
                IList actions = NekoAvatarDiagnosticsUtil.GetMember(interactable, "actions", "Actions") as IList;
                if (actions == null) return false;
                actions.Add(action);
                EditorUtility.SetDirty(interactable);
                return true;
            }
            catch { return false; }
        }
    }
}
