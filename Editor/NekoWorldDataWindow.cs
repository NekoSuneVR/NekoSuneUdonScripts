using System;
using System.IO;
using System.Net;
using System.Text;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEngine;

namespace NekoSune.WorldData.Editor
{
    [NekoAddon(Order = 31)]
    public sealed class NekoWorldDataAddon : INekoAddon
    {
        public string Id { get { return "world-data"; } }
        public string TitleKey { get { return "World Data"; } }
        public string DescriptionKey { get { return "Build and test VRChat VRCJson, string-loading and runtime image feeds for UI, events, shops, news and live world data."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "{}"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoWorldDataWindow.Open(); }
    }

    internal sealed class NekoWorldDataWindow : EditorWindow
    {
        string _url = "https://example.github.io/world/data.json";
        string _preview = "No data loaded.";
        Vector2 _scroll;
        bool _trustedOnly = true;

        [MenuItem("NekoSune/World/Data Builder", false, 21)]
        public static void Open()
        {
            var w = GetWindow<NekoWorldDataWindow>(false, "World Data", true);
            w.minSize = new Vector2(700f, 520f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("World Data", "NekoSune", "VRCJson, string loading and image loading without hiding the platform limits");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Remote JSON / text tester", NekoStyles.SlotName);
            _url = EditorGUILayout.TextField("URL", _url);
            _trustedOnly = EditorGUILayout.ToggleLeft("Warn when URL is outside common VRChat trusted hosts", _trustedOnly);
            EditorGUILayout.HelpBox("VRChat String Loading expects direct .txt/.json URLs and rate-limits string requests. Test the endpoint here before wiring it into Udon.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Download", NekoStyles.PrimaryButton, GUILayout.Height(30f))) TestDownload();
            if (GUILayout.Button("Copy Preview")) EditorGUIUtility.systemCopyBuffer = _preview;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.TextArea(_preview, GUILayout.MinHeight(150f));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Generate readable UdonSharp starters", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Copies editable scripts into Assets/NekoSune/Data/Generated. The package itself stays editor-only and the generated files compile against the VRChat Worlds SDK.", NekoStyles.WrapLabel);
            if (GUILayout.Button("Generate JSON / String Loader")) CopyTemplate("NekoRemoteJsonFeed.cs.txt", "NekoRemoteJsonFeed.cs");
            if (GUILayout.Button("Generate Image Feed Loader")) CopyTemplate("NekoRemoteImageFeed.cs.txt", "NekoRemoteImageFeed.cs");
            if (GUILayout.Button("Generate Both")) { CopyTemplate("NekoRemoteJsonFeed.cs.txt", "NekoRemoteJsonFeed.cs", false); CopyTemplate("NekoRemoteImageFeed.cs.txt", "NekoRemoteImageFeed.cs", false); AssetDatabase.Refresh(); EditorUtility.DisplayDialog("World Data", "Generated both runtime starters under Assets/NekoSune/Data/Generated.", "OK"); }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Runtime rules to remember", NekoStyles.SlotName);
            EditorGUILayout.LabelField("• String Loading: queue requests instead of polling every frame.\n• VRCJson: deserialize into DataToken/DataDictionary/DataList.\n• Images: max 2048x2048, scene-wide rate limiting, direct image URLs, and dispose downloaded images when replacing them.\n• Trusted URLs: users may need Allow Untrusted URLs for unsupported hosts.\n• JSON imageUrl strings cannot magically become arbitrary VRCUrl values at runtime; predeclare VRCUrl entries or map known IDs to URLs.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        void TestDownload()
        {
            try
            {
                Uri uri;
                if (!Uri.TryCreate(_url, UriKind.Absolute, out uri) || (uri.Scheme != "http" && uri.Scheme != "https")) throw new InvalidOperationException("Enter an absolute http/https URL.");
                if (_trustedOnly && !LooksTrusted(uri.Host)) _preview = "WARNING: Host is not in this tool's common trusted-host hint list. VRChat users may need Allow Untrusted URLs.\n\n";
                else _preview = "";
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers[HttpRequestHeader.UserAgent] = "NekoSune-WorldData/1.0";
                    string text = client.DownloadString(uri);
                    if (text.Length > 2 * 1024 * 1024) text = text.Substring(0, 2 * 1024 * 1024) + "\n...[preview truncated]";
                    _preview += "OK " + uri.Host + "\nCharacters: " + text.Length + "\n\n" + text;
                }
            }
            catch (Exception e) { _preview = "DOWNLOAD FAILED\n" + e.Message; }
        }

        static bool LooksTrusted(string host)
        {
            if (string.IsNullOrEmpty(host)) return false;
            host = host.ToLowerInvariant();
            string[] hints = { "github.io", "vrcdn.cloud", "assets.vrchat.com", "dropboxusercontent.com", "i.imgur.com", "i.ibb.co", "i.postimg.cc", "i.redd.it", "ytimg.com" };
            for (int i = 0; i < hints.Length; i++) if (host == hints[i] || host.EndsWith("." + hints[i])) return true;
            return false;
        }

        static void CopyTemplate(string sourceName, string targetName, bool dialog = true)
        {
            string src = FindPackageFile("Templates/Runtime/" + sourceName);
            if (string.IsNullOrEmpty(src)) { EditorUtility.DisplayDialog("World Data", "Could not find packaged template " + sourceName, "OK"); return; }
            string folder = EnsureFolder("Assets/NekoSune/Data/Generated");
            string dst = ToAbsolute(folder + "/" + targetName);
            File.Copy(src, dst, true);
            AssetDatabase.Refresh();
            if (dialog) EditorUtility.DisplayDialog("World Data", "Generated " + folder + "/" + targetName, "OK");
        }

        static string EnsureFolder(string path) { string[] p = path.Split('/'); string cur = p[0]; for (int i = 1; i < p.Length; i++) { string n = cur + "/" + p[i]; if (!AssetDatabase.IsValidFolder(n)) AssetDatabase.CreateFolder(cur, p[i]); cur = n; } return cur; }
        static string ToAbsolute(string assetPath) { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar)); }
        static string FindPackageFile(string relative) { string p = Path.Combine("Packages/com.nekosune.world-data", relative.Replace('/', Path.DirectorySeparatorChar)); return File.Exists(p) ? Path.GetFullPath(p) : null; }
    }
}
