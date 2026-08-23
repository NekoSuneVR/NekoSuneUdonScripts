using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 5)]
    internal sealed class NekoQuestAssistantAddon : INekoAddon
    {
        public string Id { get { return "quest-assistant"; } }
        public string TitleKey { get { return "quest.title"; } }
        public string DescriptionKey { get { return "quest.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "Q"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoQuestAssistantWindow.Open(); }
    }

    internal sealed class NekoQuestAssistantWindow : EditorWindow
    {
        GameObject _source;
        GameObject _existingQuest;
        bool _stripUnsupported = true;
        bool _convertShaders = true;
        bool _androidTextures = true;
        int _androidMaxTexture = 1024;
        Vector2 _scroll;
        NekoAvatarReport _report;
        NekoRankAssessment _mobile;
        List<NekoTextureInfo> _textures = new List<NekoTextureInfo>();
        readonly List<Material> _unsupportedMaterials = new List<Material>();
        int _strippedCount;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/PC to Quest Assistant", false, 5)]
        public static void Open()
        {
            var w = GetWindow<NekoQuestAssistantWindow>(false, "Quest Assistant", true);
            w.minSize = new Vector2(700f, 520f);
            w.Show();
        }

        void OnEnable()
        {
            if (_source == null) _source = NekoAvatarDiagnosticsUtil.SuggestedAvatarFromSelection();
            Scan();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Assistant", "PC → Quest", "Create and maintain a mobile-safe avatar copy without touching the PC hierarchy");

            EditorGUI.BeginChangeCheck();
            _source = (GameObject)EditorGUILayout.ObjectField("PC avatar", _source, typeof(GameObject), true);
            _existingQuest = (GameObject)EditorGUILayout.ObjectField("Existing Quest avatar (optional)", _existingQuest, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();

            if (_source == null)
            {
                EditorGUILayout.HelpBox("Select or drop a VRChat avatar to build a Quest conversion plan.", MessageType.Info);
                return;
            }

            DrawPlan();
            NekoStyles.Rule();
            GUILayout.Label("Safe conversion options", EditorStyles.boldLabel);
            _stripUnsupported = EditorGUILayout.ToggleLeft("Strip components disabled on Android/Quest from the generated copy", _stripUnsupported);
            _convertShaders = EditorGUILayout.ToggleLeft("Duplicate unsupported materials and convert them to a VRChat mobile shader", _convertShaders);
            _androidTextures = EditorGUILayout.ToggleLeft("Create Android texture-import overrides", _androidTextures);
            using (new EditorGUI.DisabledScope(!_androidTextures))
                _androidMaxTexture = EditorGUILayout.IntPopup("Android texture max", _androidMaxTexture, new[] { "512", "1024 (recommended)", "2048" }, new[] { 512, 1024, 2048 });

            EditorGUILayout.HelpBox("NekoSune never decimates the PC avatar automatically. VRChat recommends roughly 10k triangles, one skinned mesh, one material and ~1k textures for mobile; destructive topology changes need creator review or retopology.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Height(30f))) Scan();
            if (GUILayout.Button("Create Quest Copy + Safe Fixes", NekoStyles.PrimaryButton, GUILayout.Height(30f))) CreateQuestCopy();
            EditorGUILayout.EndHorizontal();
        }

        void DrawPlan()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Quest conversion plan", NekoStyles.SlotName);
            if (_mobile != null)
            {
                EditorGUILayout.LabelField("Current mobile rank", NekoLoc.T(NekoPerfTable.RankKey(_mobile.Overall)));
                EditorGUILayout.LabelField("Triangles", _report.Get(NekoStat.Triangles).Value.ToString("N0"));
                EditorGUILayout.LabelField("Skinned meshes", _report.Get(NekoStat.SkinnedMeshes).Value.ToString("N0"));
                EditorGUILayout.LabelField("Material slots", _report.Get(NekoStat.MaterialSlots).Value.ToString("N0"));
                EditorGUILayout.LabelField("Texture memory", NekoRankAdvisor.FormatBytes(_report.Get(NekoStat.TextureMemory).Value));
                EditorGUILayout.LabelField("Quest-incompatible unique materials", _unsupportedMaterials.Count.ToString());
                EditorGUILayout.LabelField("Textures over 1k", CountLargeTextures().ToString());
            }
            EditorGUILayout.EndVertical();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(210f));
            if (_mobile != null && _mobile.Blockers.Count > 0)
            {
                GUILayout.Label("Rank blockers", EditorStyles.boldLabel);
                for (int i = 0; i < _mobile.Blockers.Count; i++)
                {
                    NekoStatResult b = _mobile.Blockers[i];
                    string current = b.Stat == NekoStat.BoundsSize ? NekoRankAdvisor.FormatBounds(_report.BoundsSize) : NekoRankAdvisor.Format(b.Def, b.Value);
                    string target = b.Target < 0 ? "review" : NekoRankAdvisor.Format(b.Def, b.Target);
                    EditorGUILayout.LabelField("• " + NekoLoc.T(b.Def.LabelKey) + ": " + current + " → " + target, NekoStyles.WrapLabel);
                }
            }
            if (_unsupportedMaterials.Count > 0)
            {
                GUILayout.Label("Shader conversions", EditorStyles.boldLabel);
                for (int i = 0; i < _unsupportedMaterials.Count; i++)
                {
                    Material m = _unsupportedMaterials[i];
                    EditorGUILayout.LabelField("• " + m.name + " — " + (m.shader == null ? "missing shader" : m.shader.name), NekoStyles.WrapLabel);
                }
            }
            if (_existingQuest != null) DrawPairStatus();
            EditorGUILayout.EndScrollView();
        }

        void DrawPairStatus()
        {
            Component a = NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(_source);
            Component b = NekoAvatarDiagnosticsUtil.FindAvatarDescriptor(_existingQuest);
            if (a == null || b == null) return;
            List<NekoExpressionParameterInfo> pc = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(a);
            List<NekoExpressionParameterInfo> quest = NekoAvatarDiagnosticsUtil.ReadExpressionParameters(b);
            bool same = pc.Count == quest.Count;
            if (same)
            {
                for (int i = 0; i < pc.Count; i++)
                    if (pc[i].Name != quest[i].Name || NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(pc[i].TypeName) != NekoAvatarDiagnosticsUtil.ExpressionTypeToAnimatorType(quest[i].TypeName)) { same = false; break; }
            }
            GUILayout.Label("PC ↔ Quest parameter sync", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(same ? "Parameter order/types match." : "Parameter order/types differ. Keep the same Expression Parameters ordering/type on both versions for cross-platform sync.", same ? MessageType.Info : MessageType.Warning);
        }

        int CountLargeTextures()
        {
            int n = 0;
            for (int i = 0; i < _textures.Count; i++) if (Math.Max(_textures[i].Width, _textures[i].Height) > 1024) n++;
            return n;
        }

        void Scan()
        {
            _unsupportedMaterials.Clear();
            _textures.Clear();
            _report = null;
            _mobile = null;
            if (_source == null) { Repaint(); return; }
            _report = NekoAvatarStats.Collect(_source);
            _mobile = NekoRankAdvisor.Assess(_report, NekoPlatform.Mobile);
            _textures = NekoAvatarDiagnosticsUtil.CollectTextures(_source);
            HashSet<Material> mats = NekoAvatarDiagnosticsUtil.CollectMaterials(_source);
            foreach (Material m in mats) if (!NekoAvatarDiagnosticsUtil.IsQuestAvatarShader(m)) _unsupportedMaterials.Add(m);
            Repaint();
        }

        void CreateQuestCopy()
        {
            if (_source == null) return;
            GameObject copy = Instantiate(_source, _source.transform.parent);
            copy.name = _source.name + " [Quest]";
            Undo.RegisterCreatedObjectUndo(copy, "Create NekoSune Quest avatar copy");
            copy.SetActive(true);
            _strippedCount = 0;

            if (_stripUnsupported) StripUnsupported(copy);
            if (_convertShaders) ConvertMaterials(copy);
            if (_androidTextures)
            {
                List<NekoTextureInfo> textures = NekoAvatarDiagnosticsUtil.CollectTextures(copy);
                int changed = 0;
                for (int i = 0; i < textures.Count; i++) if (NekoAvatarDiagnosticsUtil.TrySetAndroidTextureOverride(textures[i].Texture, _androidMaxTexture)) changed++;
                Debug.Log("[NekoSune Quest Assistant] Applied Android texture override to " + changed + " texture(s).");
            }

            Selection.activeGameObject = copy;
            EditorGUIUtility.PingObject(copy);
            _existingQuest = copy;
            Debug.Log("[NekoSune Quest Assistant] Created " + copy.name + ". Stripped " + _strippedCount + " PC-only component(s). Review mesh/material rank blockers before upload.");
            Scan();
        }

        void StripUnsupported(GameObject copy)
        {
            Component[] components = copy.GetComponentsInChildren<Component>(true);
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component c = components[i];
                if (c == null || c is Transform || c is Animator || c is Renderer || c is MeshFilter) continue;
                Type t = c.GetType();
                string name = t.Name;
                bool remove = c is Light || c is AudioSource || c is Camera || c is Cloth || c is Rigidbody || c is Collider || c is Joint;
                if (!remove && name.IndexOf("Constraint", StringComparison.OrdinalIgnoreCase) >= 0 && !name.StartsWith("VRC", StringComparison.OrdinalIgnoreCase)) remove = true;
                if (!remove && t.FullName != null && t.FullName.IndexOf("RootMotion.FinalIK", StringComparison.OrdinalIgnoreCase) >= 0) remove = true;
                if (!remove) continue;
                Undo.DestroyObjectImmediate(c);
                _strippedCount++;
            }
        }

        void ConvertMaterials(GameObject copy)
        {
            Shader targetShader = Shader.Find("VRChat/Mobile/Toon Standard");
            if (targetShader == null) targetShader = Shader.Find("VRChat/Mobile/Standard Lite");
            if (targetShader == null) targetShader = Shader.Find("VRChat/Mobile/Toon Lit");
            if (targetShader == null)
            {
                Debug.LogWarning("[NekoSune Quest Assistant] No VRChat mobile avatar shader was found. Install/update the VRChat Avatars SDK before automatic shader conversion.");
                return;
            }

            string folder = EnsureFolder("Assets/NekoSune/Avatars/QuestGenerated/" + SafeName(copy.name) + "/Materials");
            var converted = new Dictionary<Material, Material>();
            Renderer[] renderers = copy.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                bool dirty = false;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material source = materials[m];
                    if (source == null || NekoAvatarDiagnosticsUtil.IsQuestAvatarShader(source)) continue;
                    Material destination;
                    if (!converted.TryGetValue(source, out destination))
                    {
                        destination = new Material(targetShader);
                        destination.name = source.name + "_Quest";
                        if (source.HasProperty("_MainTex") && destination.HasProperty("_MainTex")) destination.SetTexture("_MainTex", source.GetTexture("_MainTex"));
                        if (source.HasProperty("_Color") && destination.HasProperty("_Color")) destination.SetColor("_Color", source.GetColor("_Color"));
                        if (source.HasProperty("_EmissionMap") && destination.HasProperty("_EmissionMap")) destination.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
                        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SafeName(destination.name) + ".mat");
                        AssetDatabase.CreateAsset(destination, path);
                        converted.Add(source, destination);
                    }
                    materials[m] = destination;
                    dirty = true;
                }
                if (dirty) renderer.sharedMaterials = materials;
            }
            AssetDatabase.SaveAssets();
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
