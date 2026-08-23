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
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolute = Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllText(absolute, JsonUtility.ToJson(feed, true), Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        public static void GenerateVrchatStarterPack()
        {
            string folder = EnsureFolder("Assets/NekoSune/WorldUI/Generated");
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string jsonPath = folder + "/NekoWorldUiVrchatJsonFeed.cs";
            string imagePath = folder + "/NekoWorldUiVrchatImageFeed.cs";
            string jsonAbsolute = Path.Combine(projectRoot, jsonPath.Replace('/', Path.DirectorySeparatorChar));
            string imageAbsolute = Path.Combine(projectRoot, imagePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(jsonAbsolute)) File.WriteAllText(jsonAbsolute, VrchatJsonSource, Encoding.UTF8);
            if (!File.Exists(imageAbsolute)) File.WriteAllText(imageAbsolute, VrchatImageSource, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("NekoSune World UI Builder", "Generated VRChat UdonSharp starter scripts under:\n\n" + folder + "\n\nUnity/UdonSharp will compile them if the VRChat Worlds SDK is installed. Add the generated behaviour to a helper GameObject and assign the row/image slots made by the UI Builder.", "OK");
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

        const string VrchatJsonSource = @"using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;

public class NekoWorldUiVrchatJsonFeed : UdonSharpBehaviour
{
    public VRCUrl jsonUrl;
    public Text[] rows;
    public string titleKey = \"title\";
    public string subtitleKey = \"subtitle\";
    public string descriptionKey = \"description\";

    public void RefreshJson()
    {
        if (VRCUrl.IsNullOrEmpty(jsonUrl)) return;
        VRCStringDownloader.LoadUrl(jsonUrl, this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        DataToken rootToken;
        if (!VRCJson.TryDeserializeFromJson(result.Result, out rootToken)) return;
        DataDictionary root = rootToken.DataDictionary;
        DataToken itemsToken;
        if (!root.TryGetValue(\"items\", out itemsToken)) return;
        DataList items = itemsToken.DataList;
        int count = rows.Length < items.Count ? rows.Length : items.Count;
        for (int i = 0; i < count; i++)
        {
            DataDictionary item = items[i].DataDictionary;
            string title = GetString(item, titleKey);
            string subtitle = GetString(item, subtitleKey);
            string description = GetString(item, descriptionKey);
            rows[i].text = title + (subtitle == \"\" ? \"\" : \"\\n\" + subtitle) + (description == \"\" ? \"\" : \"\\n\" + description);
            rows[i].gameObject.SetActive(true);
        }
        for (int i = count; i < rows.Length; i++) rows[i].gameObject.SetActive(false);
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.LogError(\"[NekoSune World UI] JSON download failed: \" + result.ErrorCode + \" - \" + result.Error);
    }

    private string GetString(DataDictionary item, string key)
    {
        DataToken token;
        if (!item.TryGetValue(key, out token)) return \"\";
        return token.String;
    }
}
";

        const string VrchatImageSource = @"using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class NekoWorldUiVrchatImageFeed : UdonSharpBehaviour
{
    public VRCUrl[] imageUrls;
    public RawImage[] slots;
    private VRCImageDownloader _downloader;

    void Start()
    {
        _downloader = new VRCImageDownloader();
    }

    public void LoadImages()
    {
        if (_downloader == null) _downloader = new VRCImageDownloader();
        int count = imageUrls.Length < slots.Length ? imageUrls.Length : slots.Length;
        for (int i = 0; i < count; i++)
        {
            if (VRCUrl.IsNullOrEmpty(imageUrls[i])) continue;
            _downloader.DownloadImage(imageUrls[i], null, (IUdonEventReceiver)this, new TextureInfo());
        }
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        string loaded = result.Url.Get();
        int count = imageUrls.Length < slots.Length ? imageUrls.Length : slots.Length;
        for (int i = 0; i < count; i++)
        {
            if (imageUrls[i].Get() != loaded) continue;
            slots[i].texture = result.Result;
            slots[i].gameObject.SetActive(true);
            return;
        }
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.LogError(\"[NekoSune World UI] Image download failed: \" + result.Error + \" - \" + result.ErrorMessage);
    }

    void OnDestroy()
    {
        if (_downloader != null) _downloader.Dispose();
    }
}
";
    }
}
