using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NekoSune.WorldUI.Editor
{
    internal sealed class NekoWorldUiDoctorWindow : EditorWindow
    {
        GameObject _root;
        NekoWorldUiPlatform _platform = NekoWorldUiPlatform.VRChat;
        Vector2 _scroll;
        readonly List<Finding> _findings = new List<Finding>();

        sealed class Finding
        {
            public MessageType type;
            public string title;
            public string detail;
            public UnityEngine.Object target;
        }

        [MenuItem("NekoSune/World/UI Builder/UI Doctor", false, 11)]
        public static void Open()
        {
            NekoWorldUiDoctorWindow window = GetWindow<NekoWorldUiDoctorWindow>(false, "World UI Doctor", true);
            window.minSize = new Vector2(650f, 480f);
            window.Show();
        }

        void OnEnable()
        {
            if (_root == null) _root = Selection.activeGameObject;
            Scan();
        }

        void OnGUI()
        {
            GUILayout.Label("NekoSune World UI Doctor", new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 });
            EditorGUILayout.LabelField("Checks common beginner problems in generated or hand-made world-space UI.", EditorStyles.wordWrappedLabel);
            EditorGUI.BeginChangeCheck();
            _root = (GameObject)EditorGUILayout.ObjectField("UI root", _root, typeof(GameObject), true);
            _platform = (NekoWorldUiPlatform)EditorGUILayout.EnumPopup("Platform", _platform);
            if (EditorGUI.EndChangeCheck()) Scan();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan", GUILayout.Height(30f))) Scan();
            if (GUILayout.Button("Fix Safe Setup", GUILayout.Height(30f))) FixSafe();
            EditorGUILayout.EndHorizontal();

            if (_root == null)
            {
                EditorGUILayout.HelpBox("Select the Canvas/root of a world UI panel.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Findings: " + _findings.Count, EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _findings.Count; i++)
            {
                Finding f = _findings[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(f.title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (f.target != null && GUILayout.Button("Select", GUILayout.Width(60f)))
                {
                    Selection.activeObject = f.target;
                    EditorGUIUtility.PingObject(f.target);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox(f.detail, f.type);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        void Scan()
        {
            _findings.Clear();
            if (_root == null) { Repaint(); return; }

            Canvas canvas = _root.GetComponent<Canvas>();
            if (canvas == null) Add(MessageType.Error, "Missing Canvas", "The selected root has no Canvas component.", _root);
            else if (canvas.renderMode != RenderMode.WorldSpace) Add(MessageType.Error, "Canvas is not World Space", "World UI should normally use a World Space Canvas so it exists physically inside the world.", canvas);

            if (_root.GetComponent<GraphicRaycaster>() == null)
                Add(MessageType.Error, "Missing GraphicRaycaster", "Buttons/Toggles/Sliders need a GraphicRaycaster for Unity UI pointer interaction.", _root);

            if ((_platform == NekoWorldUiPlatform.VRChat || _platform == NekoWorldUiPlatform.Both) && NekoWorldUiPlatformBridge.FindType("VRCUiShape", "VRC_UIShape") != null)
            {
                Type t = NekoWorldUiPlatformBridge.FindType("VRCUiShape", "VRC_UIShape");
                if (t != null && _root.GetComponent(t) == null) Add(MessageType.Warning, "Missing VRChat UI Shape", "VRChat interactive world-space Canvas setup is incomplete.", _root);
            }

            if ((_platform == NekoWorldUiPlatform.ChilloutVR || _platform == NekoWorldUiPlatform.Both) && NekoWorldUiPlatformBridge.FindType("CVRCanvasWrapper") != null)
            {
                Type t = NekoWorldUiPlatformBridge.FindType("CVRCanvasWrapper");
                if (t != null && _root.GetComponent(t) == null) Add(MessageType.Warning, "Missing CVR Canvas Wrapper", "ChilloutVR world-space Canvas setup is incomplete.", _root);
            }

            Selectable[] selectables = _root.GetComponentsInChildren<Selectable>(true);
            int navigation = 0;
            int tiny = 0;
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i].navigation.mode != Navigation.Mode.None) navigation++;
                RectTransform rt = selectables[i].transform as RectTransform;
                if (rt != null && (rt.rect.width < 40f || rt.rect.height < 36f)) tiny++;
            }
            if (navigation > 0) Add(MessageType.Warning, "Unity Navigation enabled", navigation + " interactive control(s) still use Unity Navigation. World-space VR UI is usually easier to use with Navigation=None.", _root);
            if (tiny > 0) Add(MessageType.Warning, "Small interaction targets", tiny + " control(s) are below roughly 40x36 UI units and may be difficult to hit accurately in VR.", _root);

            Button[] buttons = _root.GetComponentsInChildren<Button>(true);
            int noAction = 0;
            int linkWithoutUrl = 0;
            int platformAction = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                NekoWorldUiAction action;
                string value;
                if (!NekoWorldUiPlatformBridge.TryParseMeta(buttons[i].gameObject.name, out action, out value))
                {
                    if (buttons[i].onClick.GetPersistentEventCount() == 0) noAction++;
                    continue;
                }
                if (action == NekoWorldUiAction.None && buttons[i].onClick.GetPersistentEventCount() == 0) noAction++;
                if (action == NekoWorldUiAction.OpenLinkCard && string.IsNullOrWhiteSpace(value)) linkWithoutUrl++;
                if (action == NekoWorldUiAction.TeleportPlayer || action == NekoWorldUiAction.RespawnPlayer || action == NekoWorldUiAction.RefreshJson || action == NekoWorldUiAction.ToggleObject || action == NekoWorldUiAction.AnimatorBool || action == NekoWorldUiAction.AnimatorTrigger) platformAction++;
            }
            if (noAction > 0) Add(MessageType.Info, "Buttons without actions", noAction + " button(s) have no NekoSune action metadata or persistent UnityEvent. This may be intentional for unfinished layout work.", _root);
            if (linkWithoutUrl > 0) Add(MessageType.Warning, "Link cards missing URLs", linkWithoutUrl + " link button(s) do not contain a URL value.", _root);
            if (platformAction > 0) Add(MessageType.Info, "Platform runtime wiring remains", platformAction + " control(s) use actions that need Udon/UdonSharp or CVR Interactable/runtime logic. Use the Builder's runtime starter pack and platform notes.", _root);

            Text[] labels = _root.GetComponentsInChildren<Text>(true);
            int tinyText = 0;
            for (int i = 0; i < labels.Length; i++) if (labels[i].fontSize > 0 && labels[i].fontSize < 18) tinyText++;
            if (tinyText > 0) Add(MessageType.Info, "Small text", tinyText + " label(s) use a font size below 18. Test readability at the actual viewing distance in VR.", _root);

            if (_findings.Count == 0) Add(MessageType.Info, "No obvious setup problems", "The basic UI hierarchy looks healthy. Test it in the actual VRChat/ChilloutVR client because Editor pointer behaviour is not a complete runtime test.", _root);
            Repaint();
        }

        void FixSafe()
        {
            if (_root == null) return;
            Canvas canvas = _root.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                Undo.RecordObject(canvas, "Set Canvas World Space");
                canvas.renderMode = RenderMode.WorldSpace;
            }
            if (_root.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(_root);
            List<string> notes = new List<string>();
            NekoWorldUiPlatformBridge.ApplyPlatform(_root, _platform, notes);
            if (notes.Count > 0) Debug.Log("[NekoSune World UI Doctor]\n- " + string.Join("\n- ", notes.ToArray()), _root);
            Scan();
        }

        void Add(MessageType type, string title, string detail, UnityEngine.Object target)
        {
            Finding f = new Finding();
            f.type = type;
            f.title = title;
            f.detail = detail;
            f.target = target;
            _findings.Add(f);
        }
    }
}
