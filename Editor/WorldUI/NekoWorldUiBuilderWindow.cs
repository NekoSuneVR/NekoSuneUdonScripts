using System;
using System.Collections.Generic;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEngine;

namespace NekoSune.WorldUI.Editor
{
    [NekoAddon(Order = 20)]
    public sealed class NekoWorldUiBuilderAddon : INekoAddon
    {
        public string Id { get { return "world-ui-builder"; } }
        public string TitleKey { get { return "World UI Builder"; } }
        public string DescriptionKey { get { return "Build beginner-friendly VRChat / ChilloutVR world UI from visual templates or JSON blueprints."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "UI"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoWorldUiBuilderWindow.Open(); }
    }

    internal sealed class NekoWorldUiBuilderWindow : EditorWindow
    {
        int _templateIndex;
        NekoWorldUiBlueprint _blueprint;
        NekoWorldUiFeedDocument _feed;
        TextAsset _localJson;
        Vector2 _scroll;
        bool _showElements = true;
        bool _showTheme = true;
        bool _showLearn = true;
        string _status = "Choose a template, edit it, then Build UI.";

        [MenuItem("NekoSune/World/UI Builder", false, 10)]
        public static void Open()
        {
            NekoWorldUiBuilderWindow window = GetWindow<NekoWorldUiBuilderWindow>(false, "World UI Builder", true);
            window.minSize = new Vector2(760f, 640f);
            window.Show();
        }

        void OnEnable()
        {
            if (_blueprint == null) _blueprint = NekoWorldUiTemplates.Create(1);
        }

        void OnGUI()
        {
            if (_blueprint == null) _blueprint = NekoWorldUiTemplates.Create(0);
            EditorGUILayout.Space(8f);
            GUILayout.Label("NekoSune World UI Builder", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 });
            EditorGUILayout.LabelField("Build and learn world UI without manually wiring every Canvas, RectTransform, platform wrapper and starter action.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawTemplateSection();
            EditorGUILayout.Space(8f);
            DrawBlueprintSection();
            EditorGUILayout.Space(8f);
            DrawDataSection();
            EditorGUILayout.Space(8f);
            DrawElements();
            EditorGUILayout.Space(8f);
            DrawBuildSection();
            EditorGUILayout.Space(8f);
            DrawLearn();
            EditorGUILayout.EndScrollView();
        }

        void DrawTemplateSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("1. Start from a template", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Templates are editable starting points, not fixed hard-coded panels. Change elements, styling, actions and JSON data, then export the whole design as a blueprint.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            _templateIndex = EditorGUILayout.Popup("Template", _templateIndex, NekoWorldUiTemplates.Names);
            if (GUILayout.Button("Load Template", GUILayout.Width(120f)))
            {
                if (EditorUtility.DisplayDialog("Load template?", "Replace the current in-memory blueprint with " + NekoWorldUiTemplates.Names[_templateIndex] + "?", "Load", "Cancel"))
                {
                    _blueprint = NekoWorldUiTemplates.Create(_templateIndex);
                    _feed = null;
                    _status = "Loaded template: " + _blueprint.name;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Blueprint JSON"))
            {
                try
                {
                    NekoWorldUiBlueprint imported = NekoWorldUiData.ImportBlueprint();
                    if (imported != null) { _blueprint = imported; _status = "Imported blueprint: " + imported.name; }
                }
                catch (Exception e) { EditorUtility.DisplayDialog("Import failed", e.Message, "OK"); }
            }
            if (GUILayout.Button("Export Blueprint JSON")) NekoWorldUiData.ExportBlueprint(_blueprint);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void DrawBlueprintSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("2. Panel setup + theme", EditorStyles.boldLabel);
            _blueprint.name = EditorGUILayout.TextField("UI name", _blueprint.name);
            _blueprint.description = EditorGUILayout.TextField("Purpose", _blueprint.description);
            _blueprint.platform = (NekoWorldUiPlatform)EditorGUILayout.EnumPopup("Target platform", _blueprint.platform);
            _blueprint.width = Mathf.Max(320f, EditorGUILayout.FloatField("Canvas width (px)", _blueprint.width));
            _blueprint.height = Mathf.Max(240f, EditorGUILayout.FloatField("Canvas height (px)", _blueprint.height));
            _blueprint.worldScale = EditorGUILayout.Slider("World scale", _blueprint.worldScale, 0.00025f, 0.01f);
            EditorGUILayout.HelpBox("A 1200 x 800 Canvas at scale 0.001 is roughly 1.2m x 0.8m in the world. Move/rotate/resize the generated RectTransform normally afterward.", MessageType.Info);

            _showTheme = EditorGUILayout.Foldout(_showTheme, "Theme and VR readability", true);
            if (_showTheme)
            {
                EditorGUILayout.BeginVertical("box");
                NekoWorldUiTheme picked = (NekoWorldUiTheme)EditorGUILayout.EnumPopup("Theme preset", _blueprint.theme);
                if (picked != _blueprint.theme)
                {
                    if (picked == NekoWorldUiTheme.Custom) _blueprint.theme = picked;
                    else NekoWorldUiThemePresets.Apply(_blueprint, picked);
                }

                EditorGUI.BeginChangeCheck();
                _blueprint.backgroundColor = EditorGUILayout.ColorField("Background", _blueprint.backgroundColor);
                _blueprint.panelColor = EditorGUILayout.ColorField("Cards / panels", _blueprint.panelColor);
                _blueprint.controlColor = EditorGUILayout.ColorField("Control background", _blueprint.controlColor);
                _blueprint.primaryColor = EditorGUILayout.ColorField("Primary / buttons", _blueprint.primaryColor);
                _blueprint.accentColor = EditorGUILayout.ColorField("Accent / checkmarks", _blueprint.accentColor);
                _blueprint.textColor = EditorGUILayout.ColorField("Text", _blueprint.textColor);
                _blueprint.mutedTextColor = EditorGUILayout.ColorField("Muted text", _blueprint.mutedTextColor);
                _blueprint.linkColor = EditorGUILayout.ColorField("Links", _blueprint.linkColor);
                _blueprint.spacing = EditorGUILayout.Slider("Spacing", _blueprint.spacing, 0f, 40f);
                _blueprint.panelPadding = EditorGUILayout.Slider("Panel padding", _blueprint.panelPadding, 0f, 48f);
                _blueprint.baseFontSize = EditorGUILayout.IntSlider("Base font size", _blueprint.baseFontSize, 16, 48);
                _blueprint.buttonHeight = EditorGUILayout.Slider("Button / toggle height", _blueprint.buttonHeight, 40f, 110f);
                if (EditorGUI.EndChangeCheck()) _blueprint.theme = NekoWorldUiTheme.Custom;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Neko Dark")) NekoWorldUiThemePresets.Apply(_blueprint, NekoWorldUiTheme.NekoDark);
                if (GUILayout.Button("Light")) NekoWorldUiThemePresets.Apply(_blueprint, NekoWorldUiTheme.Light);
                if (GUILayout.Button("Neon")) NekoWorldUiThemePresets.Apply(_blueprint, NekoWorldUiTheme.Neon);
                if (GUILayout.Button("Glass")) NekoWorldUiThemePresets.Apply(_blueprint, NekoWorldUiTheme.Glass);
                if (GUILayout.Button("Pastel")) NekoWorldUiThemePresets.Apply(_blueprint, NekoWorldUiTheme.Pastel);
                if (GUILayout.Button("Terminal")) NekoWorldUiThemePresets.Apply(_blueprint, NekoWorldUiTheme.Terminal);
                EditorGUILayout.EndHorizontal();

                if (_blueprint.baseFontSize < 20 || _blueprint.buttonHeight < 48f)
                    EditorGUILayout.HelpBox("This theme uses small text or controls. It may look fine on a monitor but be uncomfortable to read/select in VR. UI Doctor will flag very small generated controls.", MessageType.Warning);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawDataSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("3. Data / JSON (optional)", EditorStyles.boldLabel);
            _blueprint.dataSource = (NekoWorldUiDataSource)EditorGUILayout.EnumPopup("Data source", _blueprint.dataSource);

            if (_blueprint.dataSource == NekoWorldUiDataSource.LocalJson)
            {
                _localJson = (TextAsset)EditorGUILayout.ObjectField("JSON TextAsset", _localJson, typeof(TextAsset), false);
                if (GUILayout.Button("Load Local JSON Preview"))
                {
                    try { _feed = NekoWorldUiData.LoadLocal(_localJson); _status = FeedStatus(); }
                    catch (Exception e) { EditorUtility.DisplayDialog("JSON error", e.Message, "OK"); }
                }
            }
            else if (_blueprint.dataSource == NekoWorldUiDataSource.RemoteJsonSnapshot || _blueprint.dataSource == NekoWorldUiDataSource.VRChatRuntimeJson)
            {
                _blueprint.dataUrl = EditorGUILayout.TextField("JSON URL", _blueprint.dataUrl);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Download Editor Snapshot"))
                {
                    try { _feed = NekoWorldUiData.DownloadSnapshot(_blueprint.dataUrl); _status = FeedStatus(); }
                    catch (Exception e) { EditorUtility.DisplayDialog("Download failed", e.Message, "OK"); }
                }
                if (_feed != null && GUILayout.Button("Save Snapshot into Assets"))
                {
                    string path = NekoWorldUiData.SaveFeedSnapshot(_feed, _blueprint.name + "-data");
                    _status = "Saved JSON snapshot: " + path;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (_feed != null)
                EditorGUILayout.HelpBox("Loaded " + (_feed.items == null ? 0 : _feed.items.Count) + " JSON item(s). They will be appended as data cards when you build the UI.", MessageType.Info);

            if (_blueprint.dataSource == NekoWorldUiDataSource.VRChatRuntimeJson)
                EditorGUILayout.HelpBox("Runtime JSON is VRChat-specific. The Builder can generate an UdonSharp starter using VRCStringDownloader + VRCJson. For a UI that looks the same in VRChat and ChilloutVR, an Editor snapshot is the most portable option.", MessageType.Warning);

            EditorGUILayout.LabelField("Feed shape: { items: [ { title, subtitle, description, imageUrl, url, value } ] }", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        void DrawElements()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showElements = EditorGUILayout.Foldout(_showElements, "4. UI elements (" + _blueprint.elements.Count + ")", true);
            if (_showElements)
            {
                for (int i = 0; i < _blueprint.elements.Count; i++) DrawElement(i);
                GUILayout.Label("Add an element", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Heading")) AddElement(NekoWorldUiElementType.Heading, "Heading");
                if (GUILayout.Button("+ Text")) AddElement(NekoWorldUiElementType.Text, "New text");
                if (GUILayout.Button("+ Button")) AddElement(NekoWorldUiElementType.Button, "New button");
                if (GUILayout.Button("+ Toggle")) AddElement(NekoWorldUiElementType.Toggle, "New toggle");
                if (GUILayout.Button("+ Slider")) AddElement(NekoWorldUiElementType.Slider, "New slider");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Image")) AddElement(NekoWorldUiElementType.Image, "Image");
                if (GUILayout.Button("+ Card")) AddElement(NekoWorldUiElementType.GridItem, "Card title");
                if (GUILayout.Button("+ Divider")) AddElement(NekoWorldUiElementType.Divider, "Divider");
                if (GUILayout.Button("+ Spacer")) AddElement(NekoWorldUiElementType.Spacer, "Spacer");
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawElement(int index)
        {
            NekoWorldUiElement e = _blueprint.elements[index];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label((index + 1) + ". " + e.type, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUI.enabled = index > 0;
            if (GUILayout.Button("↑", GUILayout.Width(28f))) { Swap(index, index - 1); GUI.enabled = true; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); return; }
            GUI.enabled = index < _blueprint.elements.Count - 1;
            if (GUILayout.Button("↓", GUILayout.Width(28f))) { Swap(index, index + 1); GUI.enabled = true; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); return; }
            GUI.enabled = true;
            if (GUILayout.Button("X", GUILayout.Width(28f))) { _blueprint.elements.RemoveAt(index); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); return; }
            EditorGUILayout.EndHorizontal();

            e.id = EditorGUILayout.TextField("ID", e.id);
            e.type = (NekoWorldUiElementType)EditorGUILayout.EnumPopup("Type", e.type);
            e.label = EditorGUILayout.TextField("Label / text", e.label);
            e.secondary = EditorGUILayout.TextField("Secondary text", e.secondary);
            e.height = Mathf.Max(2f, EditorGUILayout.FloatField("Height", e.height));

            if (e.type == NekoWorldUiElementType.Image)
            {
                Texture current = string.IsNullOrEmpty(e.actionValue) ? null : AssetDatabase.LoadAssetAtPath<Texture>(e.actionValue);
                Texture chosen = (Texture)EditorGUILayout.ObjectField("Local Texture", current, typeof(Texture), false);
                if (chosen != current) e.actionValue = chosen == null ? "" : AssetDatabase.GetAssetPath(chosen);
                e.imageUrl = EditorGUILayout.TextField("Runtime image URL", e.imageUrl);
                e.dataKey = EditorGUILayout.TextField("JSON image key", e.dataKey);
                EditorGUILayout.HelpBox("Local Texture = baked into the world. Runtime URL = a placeholder the platform runtime loader can fill. VRChat runtime image URLs should be declared as VRCUrl fields before upload.", MessageType.None);
            }
            else if (e.type != NekoWorldUiElementType.Divider && e.type != NekoWorldUiElementType.Spacer && e.type != NekoWorldUiElementType.Heading && e.type != NekoWorldUiElementType.Text && e.type != NekoWorldUiElementType.GridItem)
            {
                e.action = (NekoWorldUiAction)EditorGUILayout.EnumPopup("Action", e.action);
                e.actionValue = EditorGUILayout.TextField(ActionValueLabel(e.action), e.actionValue);
                e.dataKey = EditorGUILayout.TextField("JSON key (optional)", e.dataKey);
                DrawActionHint(e);
            }
            else
            {
                e.dataKey = EditorGUILayout.TextField("JSON key (optional)", e.dataKey);
            }
            EditorGUILayout.EndVertical();
        }

        void DrawBuildSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("5. Build / validate", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_status, MessageType.None);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("BUILD WORLD UI", GUILayout.Height(38f))) BuildUi();
            if (GUILayout.Button("Open UI Doctor", GUILayout.Height(38f))) NekoWorldUiDoctorWindow.Open();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate VRChat Runtime Starter Pack")) NekoWorldUiData.GenerateVrchatStarterPack();
            if (GUILayout.Button("Select Last Generated UI"))
            {
                GameObject[] roots = Resources.FindObjectsOfTypeAll<GameObject>();
                for (int i = roots.Length - 1; i >= 0; i--)
                    if (roots[i] != null && roots[i].scene.IsValid() && roots[i].name.StartsWith("NekoWorldUI - ")) { Selection.activeGameObject = roots[i]; break; }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void DrawLearn()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showLearn = EditorGUILayout.Foldout(_showLearn, "Learn what the Builder is doing", true);
            if (_showLearn)
            {
                GUILayout.Label("Canvas + RectTransform", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("The generated Canvas uses World Space. Canvas width/height define its pixel workspace; world scale converts that into physical metres. RectTransforms position every child UI element.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("Layout Groups", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("The generated content uses a VerticalLayoutGroup and ContentSizeFitter, so adding/removing cards does not require manually calculating Y positions.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("GraphicRaycaster", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Unity UI needs a GraphicRaycaster so pointer rays can hit Buttons, Toggles and Sliders.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("VRChat", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("The Builder adds VRC UI Shape when the Worlds SDK is installed and disables Unity Navigation on controls. Teleport, respawn and live JSON need Udon/UdonSharp; use the generated starter pack rather than a normal MonoBehaviour.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("ChilloutVR", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("The Builder adds CVR Canvas Wrapper when CCK 3/4 is installed. CVR-specific player/interactable actions still use CCK components/runtime logic rather than a fake cross-platform Unity callback.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("JSON", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Use Editor snapshots when you want the same baked supporter/shop/event data on both platforms. Use VRChat Runtime JSON only when the world needs live text changes after upload.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("Images", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Local textures are baked directly. Remote image slots are generated as RawImage components so a platform-specific loader can fill them. On VRChat, image URLs are security-limited and should be predeclared as VRCUrl fields.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("Links / Patreon / shops", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("These templates are information/catalog/support panels. URLs are always shown visibly, and you can add a QR image. The Builder does not fake a browser API or process Patreon/store payments inside the world.", EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndVertical();
        }

        void BuildUi()
        {
            try
            {
                List<string> notes = new List<string>();
                GameObject root = NekoWorldUiFactory.Build(_blueprint, _feed, notes);
                _status = "Built " + root.name + ". " + notes.Count + " setup note(s).";
                if (notes.Count > 0) Debug.Log("[NekoSune World UI]\n- " + string.Join("\n- ", notes.ToArray()), root);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("UI build failed", e.Message, "OK");
            }
        }

        void AddElement(NekoWorldUiElementType type, string label)
        {
            NekoWorldUiElement e = new NekoWorldUiElement();
            e.id = "item-" + (_blueprint.elements.Count + 1);
            e.type = type;
            e.label = label;
            if (type == NekoWorldUiElementType.Image) e.height = 220f;
            else if (type == NekoWorldUiElementType.Divider) e.height = 2f;
            else if (type == NekoWorldUiElementType.Spacer) e.height = 20f;
            _blueprint.elements.Add(e);
        }

        void Swap(int a, int b)
        {
            NekoWorldUiElement temp = _blueprint.elements[a];
            _blueprint.elements[a] = _blueprint.elements[b];
            _blueprint.elements[b] = temp;
        }

        string FeedStatus()
        {
            int count = _feed == null || _feed.items == null ? 0 : _feed.items.Count;
            return "Loaded " + count + " JSON item(s).";
        }

        static string ActionValueLabel(NekoWorldUiAction action)
        {
            if (action == NekoWorldUiAction.OpenLinkCard) return "URL";
            if (action == NekoWorldUiAction.TeleportPlayer) return "Target object name";
            if (action == NekoWorldUiAction.EnableObject || action == NekoWorldUiAction.DisableObject || action == NekoWorldUiAction.ToggleObject) return "Target object name";
            if (action == NekoWorldUiAction.AnimatorBool || action == NekoWorldUiAction.AnimatorTrigger) return "Animator parameter / target";
            if (action == NekoWorldUiAction.PlayAudio || action == NekoWorldUiAction.StopAudio) return "AudioSource object name";
            if (action == NekoWorldUiAction.OpenPage) return "Page object name";
            return "Action value";
        }

        static void DrawActionHint(NekoWorldUiElement e)
        {
            if (e.action == NekoWorldUiAction.ToggleObject)
                EditorGUILayout.HelpBox("Dynamic Toggle is a platform action. Name the scene target here; UI Doctor/runtime wiring will tell you what remains.", MessageType.None);
            else if (e.action == NekoWorldUiAction.TeleportPlayer || e.action == NekoWorldUiAction.RespawnPlayer)
                EditorGUILayout.HelpBox("Player movement needs VRChat Udon or a CVR Interactable action; it cannot be implemented by an ordinary Unity Button callback in both runtimes.", MessageType.None);
            else if (e.action == NekoWorldUiAction.OpenLinkCard)
                EditorGUILayout.HelpBox("The URL is always rendered visibly. Add an Image element containing a QR texture for the smoothest cross-platform external-link experience.", MessageType.None);
        }
    }
}
