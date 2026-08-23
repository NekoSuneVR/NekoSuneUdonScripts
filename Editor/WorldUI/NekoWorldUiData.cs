using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NekoSune.WorldUI.Editor
{
    internal static class NekoWorldUiData
    {
        const int MaxJsonChars = 8 * 1024 * 1024;
        const string RuntimeTemplateRoot = "Packages/com.nekosune.world-ui-builder/Templates/Runtime/";

        public static NekoWorldUiFeedDocument ParseFeed(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new NekoWorldUiFeedDocument();
            NekoWorldUiFeedDocument doc = JsonUtility.FromJson<NekoWorldUiFeedDocument>(json);
            if (doc == null) doc = new NekoWorldUiFeedDocument();
            if (doc.items == null) doc.items = new System.Collections.Generic.List<NekoWorldUiFeedItem>();
            return doc;
        }

        public static NekoWorldUiFeedDocument LoadLocal(TextAsset asset)
        {
            if (asset == null) return null;
            return ParseFeed(asset.text);
        }

        public static NekoWorldUiFeedDocument DownloadSnapshot(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException("JSON URL must be an absolute http:// or https:// URL.");

            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.UserAgent] = "NekoSune-WorldUIBuilder/1.0";
                string json = client.DownloadString(uri);
                if (json != null && json.Length > MaxJsonChars)
                    throw new InvalidOperationException("JSON response is larger than the 8 MB editor-import safety limit.");
                return ParseFeed(json);
            }
        }

        public static void ExportBlueprint(NekoWorldUiBlueprint blueprint)
        {
            if (blueprint == null) return;
            string path = EditorUtility.SaveFilePanel("Export NekoSune UI Blueprint", Application.dataPath, SafeFile(blueprint.name) + ".nekoui.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, JsonUtility.ToJson(blueprint, true), Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        public static NekoWorldUiBlueprint ImportBlueprint()
        {
            string path = EditorUtility.OpenFilePanel("Import NekoSune UI Blueprint", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return null;
            string json = File.ReadAllText(path, Encoding.UTF8);
            NekoWorldUiBlueprint blueprint = JsonUtility.FromJson<NekoWorldUiBlueprint>(json);
            if (blueprint == null) throw new InvalidOperationException("The selected JSON file is not a valid NekoSune UI blueprint.");
            if (blueprint.elements == null) blueprint.elements = new System.Collections.Generic.List<NekoWorldUiElement>();
            return blueprint;
        }

        public static string SaveFeedSnapshot(NekoWorldUiFeedDocument feed, string suggestedName)
        {
            if (feed == null) return null;
            string folder = EnsureFolder("Assets/NekoSune/WorldUI/Data");
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SafeFile(suggestedName) + ".json");
            string absolute = AssetPathToAbsolute(path);
            File.WriteAllText(absolute, JsonUtility.ToJson(feed, true), Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        public static void GenerateVrchatStarterPack()
        {
            string folder = EnsureFolder("Assets/NekoSune/WorldUI/Generated");
            int created = 0;
            if (CopyRuntimeTemplate("NekoWorldUiVrchatJsonFeed.cs.txt", folder + "/NekoWorldUiVrchatJsonFeed.cs")) created++;
            if (CopyRuntimeTemplate("NekoWorldUiVrchatImageFeed.cs.txt", folder + "/NekoWorldUiVrchatImageFeed.cs")) created++;
            if (CopyRuntimeTemplate("NekoWorldUiVrchatActions.cs.txt", folder + "/NekoWorldUiVrchatActions.cs")) created++;
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "NekoSune World UI Builder",
                "VRChat runtime starter pack is available under:\n\n" + folder
                + "\n\nNew files created: " + created
                + "\n\nExisting generated scripts were left untouched so your edits are not overwritten. UdonSharp will compile these helpers when the VRChat Worlds SDK/UdonSharp is installed.",
                "OK");
        }

        [MenuItem("NekoSune/World/UI Builder/Generate VRChat Player Action Starter", false, 30)]
        public static void GenerateVrchatPlayerActionStarter()
        {
            string folder = EnsureFolder("Assets/NekoSune/WorldUI/Generated");
            bool created = CopyRuntimeTemplate("NekoWorldUiVrchatActions.cs.txt", folder + "/NekoWorldUiVrchatActions.cs");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "NekoSune World UI Builder",
                (created ? "Generated" : "Already exists") + ":\n\n" + folder + "/NekoWorldUiVrchatActions.cs"
                + "\n\nAssign the targets you need, then connect generated UI controls to its public Udon events.",
                "OK");
        }

        static bool CopyRuntimeTemplate(string templateName, string destinationAssetPath)
        {
            string destinationAbsolute = AssetPathToAbsolute(destinationAssetPath);
            if (File.Exists(destinationAbsolute)) return false;

            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(RuntimeTemplateRoot + templateName);
            if (source == null)
                throw new FileNotFoundException("Runtime template was not found in the installed package: " + templateName);

            File.WriteAllText(destinationAbsolute, source.text, Encoding.UTF8);
            return true;
        }

        static string AssetPathToAbsolute(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
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

        static string SafeFile(string value)
        {
            if (string.IsNullOrEmpty(value)) return "WorldUI";
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return value.Replace(' ', '_');
        }
    }
}
