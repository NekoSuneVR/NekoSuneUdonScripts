using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 15)]
    internal sealed class NekoTextureInspectorAddon : INekoAddon
    {
        public string Id { get { return "texture-inspector"; } }
        public string TitleKey { get { return "texture.title"; } }
        public string DescriptionKey { get { return "texture.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "T"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoTextureInspectorWindow.Open(); }
    }

    internal sealed class NekoTextureInspectorWindow : EditorWindow
    {
        GameObject _avatar;
        List<NekoTextureInfo> _textures = new List<NekoTextureInfo>();
        Vector2 _scroll;
        long _totalBytes;
        int _androidTarget = 1024;
        bool _showOnlyLarge;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/VRAM and Texture Inspector", false, 15)]
        public static void Open()
        {
            var w = GetWindow<NekoTextureInspectorWindow>(false, "VRAM Inspector", true);
            w.minSize = new Vector2(760f, 520f);
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
            NekoStyles.HeaderBar("Inspector", "VRAM", "Find the textures consuming the most avatar memory and prepare Android overrides");

            EditorGUI.BeginChangeCheck();
            _avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", _avatar, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();
            if (_avatar == null) { EditorGUILayout.HelpBox("Select or drop an avatar.", MessageType.Info); return; }

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Texture summary", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Unique textures", _textures.Count.ToString("N0"));
            EditorGUILayout.LabelField("Estimated loaded texture memory", NekoRankAdvisor.FormatBytes(_totalBytes));
            EditorGUILayout.LabelField("Quest/mobile Good target", "18 MB texture memory");
            EditorGUILayout.LabelField("Quest/mobile Poor ceiling", "40 MB texture memory");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Height(28f))) Scan();
            _showOnlyLarge = GUILayout.Toggle(_showOnlyLarge, "Only > 1K / > 8 MB", GUILayout.Width(155f));
            GUILayout.FlexibleSpace();
            _androidTarget = EditorGUILayout.IntPopup(_androidTarget, new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 }, GUILayout.Width(75f));
            if (GUILayout.Button("Apply Android max to all", NekoStyles.PrimaryButton, GUILayout.Width(175f), GUILayout.Height(28f))) ApplyAllAndroidOverrides();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Memory values are Unity runtime-memory estimates. VRChat's platform-compressed accounting can differ, so use Rank Advisor/SDK validation for the final rank. Android overrides do not lower the PC import resolution.", MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _textures.Count; i++)
            {
                NekoTextureInfo t = _textures[i];
                if (_showOnlyLarge && Math.Max(t.Width, t.Height) <= 1024 && t.RuntimeBytes <= 8L * 1024L * 1024L) continue;
                DrawTexture(t);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawTexture(NekoTextureInfo t)
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(t.Texture == null ? "Missing texture" : t.Texture.name, NekoStyles.SlotName);
            GUILayout.FlexibleSpace();
            if (t.Texture != null && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(58f))) { Selection.activeObject = t.Texture; EditorGUIUtility.PingObject(t.Texture); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Resolution", t.Width + " × " + t.Height);
            EditorGUILayout.LabelField("Estimated loaded memory", NekoRankAdvisor.FormatBytes(t.RuntimeBytes));
            EditorGUILayout.LabelField("Importer", string.IsNullOrEmpty(t.ImportCompression) ? "not a TextureImporter" : t.ImportCompression + " · max " + t.MaxTextureSize + " · mipmaps " + (t.Mipmaps ? "on" : "off"));
            EditorGUILayout.LabelField("Unique material uses", t.UseCount.ToString());

            int currentMax = Math.Max(t.Width, t.Height);
            if (currentMax > 1024)
            {
                long save1k = EstimateBytes(t, 1024);
                EditorGUILayout.LabelField("If limited to 1024", "~" + NekoRankAdvisor.FormatBytes(save1k) + " loaded memory (estimate)");
            }
            if (currentMax > 2048)
            {
                long save2k = EstimateBytes(t, 2048);
                EditorGUILayout.LabelField("If limited to 2048", "~" + NekoRankAdvisor.FormatBytes(save2k) + " loaded memory (estimate)");
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(t.Texture == null || string.IsNullOrEmpty(t.AssetPath)))
            {
                if (GUILayout.Button("Android 512", EditorStyles.miniButton)) ApplyAndroid(t, 512);
                if (GUILayout.Button("Android 1024", EditorStyles.miniButton)) ApplyAndroid(t, 1024);
                if (GUILayout.Button("Android 2048", EditorStyles.miniButton)) ApplyAndroid(t, 2048);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        long EstimateBytes(NekoTextureInfo t, int maxDimension)
        {
            int current = Math.Max(t.Width, t.Height);
            if (current <= 0 || current <= maxDimension) return t.RuntimeBytes;
            double scale = maxDimension / (double)current;
            return (long)Math.Max(1, t.RuntimeBytes * scale * scale);
        }

        void ApplyAndroid(NekoTextureInfo t, int max)
        {
            if (NekoAvatarDiagnosticsUtil.TrySetAndroidTextureOverride(t.Texture, max))
            {
                Debug.Log("[NekoSune VRAM Inspector] Android max texture size set to " + max + " for " + t.Texture.name);
                Scan();
            }
        }

        void ApplyAllAndroidOverrides()
        {
            int changed = 0;
            for (int i = 0; i < _textures.Count; i++) if (NekoAvatarDiagnosticsUtil.TrySetAndroidTextureOverride(_textures[i].Texture, _androidTarget)) changed++;
            Debug.Log("[NekoSune VRAM Inspector] Applied Android " + _androidTarget + " max size to " + changed + " texture(s).");
            Scan();
        }

        void Scan()
        {
            _textures = _avatar == null ? new List<NekoTextureInfo>() : NekoAvatarDiagnosticsUtil.CollectTextures(_avatar);
            _totalBytes = 0;
            for (int i = 0; i < _textures.Count; i++) _totalBytes += _textures[i].RuntimeBytes;
            Repaint();
        }
    }
}
