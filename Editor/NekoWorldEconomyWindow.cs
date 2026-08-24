using System;
using System.IO;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NekoSune.WorldEconomy.Editor
{
    [NekoAddon(Order = 32)]
    public sealed class NekoWorldEconomyAddon : INekoAddon
    {
        public string Id { get { return "world-economy"; } }
        public string TitleKey { get { return "World Economy"; } }
        public string DescriptionKey { get { return "Build Creator Economy product unlocks, store/listing buttons and supporter walls with the correct purchase lifecycle."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "$"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoWorldEconomyWindow.Open(); }
    }

    internal sealed class NekoWorldEconomyWindow : EditorWindow
    {
        string _listingId = "prod_00000000-0000-0000-0000-000000000000";
        string _groupId = "grp_00000000-0000-0000-0000-000000000000";
        int _supporterRows = 12;
        Vector2 _scroll;

        [MenuItem("NekoSune/World/Creator Economy Builder", false, 22)]
        public static void Open()
        {
            var w = GetWindow<NekoWorldEconomyWindow>(false, "World Economy", true);
            w.minSize = new Vector2(720f, 560f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Creator Economy", "NekoSune", "Product unlocks, listings and supporter UI for VRChat sellers");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox("VRChat Creator Economy tools are only useful to creators/sellers with Economy access. This addon does not process payments itself; it generates world-side Udon helpers for VRChat's own Store/UdonProduct APIs.", MessageType.Info);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Store / listing setup", NekoStyles.SlotName);
            _listingId = EditorGUILayout.TextField("Listing ID", _listingId);
            _groupId = EditorGUILayout.TextField("Optional Group ID", _groupId);
            _supporterRows = EditorGUILayout.IntSlider("Supporter rows", _supporterRows, 3, 50);
            EditorGUILayout.LabelField("Create UdonProduct assets with VRChat SDK → UdonProduct Manager, then assign the matching product asset to the generated UdonSharp behaviour.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Generate runtime helpers", NekoStyles.SlotName);
            if (GUILayout.Button("Generate Product Unlock + Store Script", NekoStyles.PrimaryButton, GUILayout.Height(32f))) CopyTemplate("NekoEconomyUnlock.cs.txt", "NekoEconomyUnlock.cs");
            if (GUILayout.Button("Generate Supporter Wall Script")) CopyTemplate("NekoEconomySupporterWall.cs.txt", "NekoEconomySupporterWall.cs");
            if (GUILayout.Button("Create Starter Store UI")) CreateStoreUi();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Safety / lifecycle rules", NekoStyles.SlotName);
            EditorGUILayout.LabelField("• Do not check ownership in Start; wait for OnPurchasesLoaded.\n• Prefer OnPurchaseConfirmedMultiple for quantity-aware purchases.\n• Keep the UdonProduct referenced by an UdonBehaviour so its events are delivered.\n• ListProductOwners requires 'Owners Names in Udon' enabled on VRChat.com.\n• Disabled GameObjects/UdonBehaviours do not receive most Economy events.\n• The generated store button opens VRChat's own store/listing UI; it never takes payment directly.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        void CreateStoreUi()
        {
            GameObject root = new GameObject("Neko Economy UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Create economy UI");
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = root.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(900f, 680f); root.transform.localScale = Vector3.one * 0.001f;
            root.GetComponent<Image>().color = new Color(0.055f, 0.065f, 0.085f, 0.97f);
            GameObject content = NewUi("Content", root.transform); Stretch(content.GetComponent<RectTransform>(), 36f);
            var layout = content.AddComponent<VerticalLayoutGroup>(); layout.spacing = 14f; layout.padding = new RectOffset(20,20,20,20); layout.childControlWidth = true; layout.childForceExpandHeight = false;
            AddText(content.transform, "WORLD STORE", 42, FontStyle.Bold, 62f);
            AddText(content.transform, "Use VRChat Creator Economy products to unlock world features.", 23, FontStyle.Normal, 60f);
            AddButton(content.transform, "OPEN WORLD STORE", "Wire to NekoEconomyUnlock.OpenWorldStore", 62f);
            AddButton(content.transform, "OPEN LISTING", "Listing: " + _listingId, 62f);
            AddText(content.transform, "SUPPORTERS", 32, FontStyle.Bold, 48f);
            for (int i = 0; i < _supporterRows; i++) AddText(content.transform, (i + 1) + ". Supporter", 22, FontStyle.Normal, 34f);
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("World Economy", "Created a world-space store/supporter UI. Generate the runtime scripts, let UdonSharp compile, then attach/wire the Economy behaviour and assign your UdonProduct.", "OK");
        }

        static GameObject NewUi(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
        static void Stretch(RectTransform rt, float pad) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(pad,pad); rt.offsetMax = new Vector2(-pad,-pad); }
        static void AddText(Transform parent, string value, int size, FontStyle style, float height)
        {
            GameObject go = NewUi("Text - " + value, parent); var t = go.AddComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); t.text = value; t.fontSize = size; t.fontStyle = style; t.color = Color.white; t.alignment = TextAnchor.MiddleLeft; var le = go.AddComponent<LayoutElement>(); le.preferredHeight = height;
        }
        static void AddButton(Transform parent, string label, string note, float height)
        {
            GameObject go = NewUi("Button - " + label, parent); var img = go.AddComponent<Image>(); img.color = new Color(0.29f,0.56f,0.98f,1f); go.AddComponent<Button>(); var le = go.AddComponent<LayoutElement>(); le.preferredHeight = height;
            GameObject textGo = NewUi("Label", go.transform); Stretch(textGo.GetComponent<RectTransform>(), 8f); var t = textGo.AddComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); t.text = label + "\n" + note; t.fontSize = 20; t.fontStyle = FontStyle.Bold; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        }

        static void CopyTemplate(string sourceName, string targetName)
        {
            string src = Path.GetFullPath(Path.Combine("Packages/com.nekosune.world-economy/Templates/Runtime", sourceName));
            if (!File.Exists(src)) { EditorUtility.DisplayDialog("World Economy", "Template not found: " + sourceName, "OK"); return; }
            string folder = EnsureFolder("Assets/NekoSune/Economy/Generated");
            string dst = Path.Combine(Directory.GetParent(Application.dataPath).FullName, (folder + "/" + targetName).Replace('/', Path.DirectorySeparatorChar));
            File.Copy(src, dst, true); AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("World Economy", "Generated " + folder + "/" + targetName, "OK");
        }
        static string EnsureFolder(string path) { string[] p = path.Split('/'); string cur=p[0]; for(int i=1;i<p.Length;i++){string n=cur+"/"+p[i]; if(!AssetDatabase.IsValidFolder(n)) AssetDatabase.CreateFolder(cur,p[i]); cur=n;} return cur; }
    }
}
