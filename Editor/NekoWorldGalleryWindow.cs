using System;
using System.IO;
using System.Reflection;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NekoSune.WorldGallery.Editor
{
    [NekoAddon(Order = 34)]
    public sealed class NekoWorldGalleryAddon : INekoAddon
    {
        public string Id { get { return "world-gallery"; } }
        public string TitleKey { get { return "Image Gallery"; } }
        public string DescriptionKey { get { return "Build local/remote image galleries with JSON metadata, lazy loading and animated or shader transitions."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "▣"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoWorldGalleryWindow.Open(); }
    }

    internal sealed class NekoWorldGalleryWindow : EditorWindow
    {
        readonly string[] _effects = { "Cross Fade", "Slide Left", "Slide Right", "Slide Up", "Zoom", "Spin + Zoom", "Shader Wipe", "Shader Dissolve", "Shader Radial" };
        int _effect;
        float _duration = 0.65f;
        bool _autoPlay = true;
        float _autoSeconds = 7f;
        Vector2 _scroll;

        [MenuItem("NekoSune/World/Image Gallery", false, 24)]
        public static void Open()
        {
            var w = GetWindow<NekoWorldGalleryWindow>(false, "World Image Gallery", true);
            w.minSize = new Vector2(760f, 600f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Image Gallery", "NekoSune", "Stylish galleries for local textures, predeclared VRCUrls and JSON-driven metadata");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Demo gallery", NekoStyles.SlotName);
            _effect = EditorGUILayout.Popup("Transition", _effect, _effects);
            _duration = EditorGUILayout.Slider("Transition time", _duration, 0.1f, 2.5f);
            _autoPlay = EditorGUILayout.Toggle("Auto-play", _autoPlay);
            _autoSeconds = EditorGUILayout.Slider("Auto-play seconds", _autoSeconds, 2f, 30f);
            if (GUILayout.Button("BUILD DEMO IMAGE GALLERY", NekoStyles.PrimaryButton, GUILayout.Height(36f))) BuildDemo();
            EditorGUILayout.LabelField("The demo creates three editable local textures, a full world-space UI, next/previous/auto buttons and a transition material. It also copies the readable runtime UdonSharp script into Assets.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("After UdonSharp compiles", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Keep the generated gallery root selected and click Auto-Wire. The tool attaches NekoImageGalleryRuntime, fills every UI reference, applies the transition settings and wires the buttons.", NekoStyles.WrapLabel);
            if (GUILayout.Button("AUTO-WIRE SELECTED GALLERY", NekoStyles.PrimaryButton, GUILayout.Height(34f))) AutoWire(Selection.activeGameObject);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Data sources", NekoStyles.SlotName);
            EditorGUILayout.LabelField("1. Local Texture[] — fastest and fully baked into the world.\n2. Predeclared VRCUrl[] — lazy runtime image loading.\n3. Optional JSON metadata — title/subtitle plus imageIndex, or imageUrl matched against the predeclared VRCUrl array.\n\nThe JSON mapper accepts a root array or common wrappers such as items, images, gallery and data. Field names remain editable on the generated behaviour.", NekoStyles.WrapLabel);
            if (GUILayout.Button("Copy example gallery JSON into Assets")) CopyExampleJson();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Transition library", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Cross-fade, four-direction style sliding, zoom, spin+zoom, plus shader wipe, dissolve and radial reveal. The runtime uses two RawImage layers for transform effects and a third shader layer for the GPU effects.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox("VRChat cannot construct arbitrary VRCUrl objects from JSON strings at runtime. For remote JSON galleries, predeclare the image URLs in the inspector and let JSON map to them by imageIndex or matching imageUrl. This keeps the gallery compatible with VRChat's URL security model.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        void BuildDemo()
        {
            string assetFolder = EnsureFolder("Assets/NekoSune/Gallery/Generated");
            Texture2D[] demo = CreateDemoTextures(assetFolder);
            Material transitionMaterial = CreateTransitionMaterial(assetFolder);

            GameObject root = new GameObject("Neko Image Gallery", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Create Neko Image Gallery");
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rootRt = root.GetComponent<RectTransform>(); rootRt.sizeDelta = new Vector2(1100f, 760f); root.transform.localScale = Vector3.one * 0.001f;
            root.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.07f, 0.98f);

            GameObject frame = Ui("[Frame]", root.transform, typeof(Image)); Stretch(frame.GetComponent<RectTransform>(), 28f); frame.GetComponent<Image>().color = new Color(0.075f,0.09f,0.135f,1f);
            GameObject viewport = Ui("[Viewport]", frame.transform, typeof(RectMask2D)); RectTransform vr = viewport.GetComponent<RectTransform>(); vr.anchorMin = new Vector2(0f,0.18f); vr.anchorMax = new Vector2(1f,1f); vr.offsetMin = new Vector2(22f,0f); vr.offsetMax = new Vector2(-22f,-78f);

            RawImage front = Raw(viewport.transform, "[FrontImage]"); RawImage back = Raw(viewport.transform, "[BackImage]"); RawImage fx = Raw(viewport.transform, "[ShaderImage]");
            Stretch(front.rectTransform, 0f); Stretch(back.rectTransform, 0f); Stretch(fx.rectTransform, 0f);
            front.texture = demo[0]; back.texture = demo[1];
            CanvasGroup fg = front.gameObject.AddComponent<CanvasGroup>(); CanvasGroup bg = back.gameObject.AddComponent<CanvasGroup>(); bg.alpha = 0f;
            fx.gameObject.SetActive(false); if (transitionMaterial != null) fx.material = transitionMaterial;

            Text title = TextNode(frame.transform, "[TitleText]", "NEKO GALLERY", 32, FontStyle.Bold, TextAnchor.MiddleLeft); RectTransform tr = title.rectTransform; tr.anchorMin = new Vector2(0f,0f); tr.anchorMax = new Vector2(.65f,.18f); tr.offsetMin = new Vector2(30f,64f); tr.offsetMax = new Vector2(0f,-8f);
            Text subtitle = TextNode(frame.transform, "[SubtitleText]", "Local / JSON / remote image demo", 20, FontStyle.Normal, TextAnchor.MiddleLeft); RectTransform sr = subtitle.rectTransform; sr.anchorMin = new Vector2(0f,0f); sr.anchorMax = new Vector2(.7f,.18f); sr.offsetMin = new Vector2(30f,18f); sr.offsetMax = new Vector2(0f,-46f);
            Text page = TextNode(frame.transform, "[PageText]", "1 / 3", 20, FontStyle.Bold, TextAnchor.MiddleCenter); RectTransform pr = page.rectTransform; pr.anchorMin = new Vector2(.75f,0f); pr.anchorMax = new Vector2(.9f,.13f); pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;
            Text status = TextNode(frame.transform, "[StatusText]", "", 15, FontStyle.Normal, TextAnchor.MiddleLeft); RectTransform str = status.rectTransform; str.anchorMin = new Vector2(0f,0f); str.anchorMax = new Vector2(.55f,.08f); str.offsetMin = new Vector2(30f,0f); str.offsetMax = Vector2.zero;

            Button prev = ButtonNode(frame.transform, "[PrevButton]", "‹", new Vector2(.68f,.065f), new Vector2(70f,52f));
            Button next = ButtonNode(frame.transform, "[NextButton]", "›", new Vector2(.94f,.065f), new Vector2(70f,52f));
            Button auto = ButtonNode(frame.transform, "[AutoButton]", "AUTO", new Vector2(.81f,.065f), new Vector2(105f,52f));

            CopyRuntime();
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Image Gallery", "Created the demo gallery. Wait for UdonSharp to compile NekoImageGalleryRuntime.cs, keep the gallery root selected, then click AUTO-WIRE SELECTED GALLERY.", "OK");
        }

        void AutoWire(GameObject root)
        {
            if (root == null || !root.name.Contains("Gallery")) { EditorUtility.DisplayDialog("Image Gallery", "Select the generated Neko Image Gallery root.", "OK"); return; }
            Type type = FindType("NekoImageGalleryRuntime");
            if (type == null) { EditorUtility.DisplayDialog("Image Gallery", "NekoImageGalleryRuntime has not compiled yet. Wait for Unity/UdonSharp and try again.", "OK"); return; }
            Component c = root.GetComponent(type); if (c == null) c = Undo.AddComponent(root, type);
            RawImage front = Find(root,"[FrontImage]").GetComponent<RawImage>(); RawImage back = Find(root,"[BackImage]").GetComponent<RawImage>(); RawImage fx = Find(root,"[ShaderImage]").GetComponent<RawImage>();
            Set(c,"frontImage",front); Set(c,"backImage",back); Set(c,"shaderImage",fx); Set(c,"frontGroup",front.GetComponent<CanvasGroup>()); Set(c,"backGroup",back.GetComponent<CanvasGroup>());
            Set(c,"titleText",Find(root,"[TitleText]").GetComponent<Text>()); Set(c,"subtitleText",Find(root,"[SubtitleText]").GetComponent<Text>()); Set(c,"pageText",Find(root,"[PageText]").GetComponent<Text>()); Set(c,"statusText",Find(root,"[StatusText]").GetComponent<Text>());
            Set(c,"transitionMode",_effect); Set(c,"transitionDuration",_duration); Set(c,"autoPlay",_autoPlay); Set(c,"autoPlaySeconds",_autoSeconds); Set(c,"sourceMode",0);
            string folder = "Assets/NekoSune/Gallery/Generated";
            Texture[] textures = new Texture[] { AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Demo_A.asset"), AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Demo_B.asset"), AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Demo_C.asset") };
            Set(c,"localTextures",textures); Set(c,"transitionMaterial",AssetDatabase.LoadAssetAtPath<Material>(folder+"/NekoGalleryTransition.mat"));
            Wire(Find(root,"[PrevButton]").GetComponent<Button>(), c, "Previous"); Wire(Find(root,"[NextButton]").GetComponent<Button>(), c, "Next"); Wire(Find(root,"[AutoButton]").GetComponent<Button>(), c, "ToggleAutoPlay");
            EditorUtility.SetDirty(c); EditorUtility.DisplayDialog("Image Gallery", "Gallery auto-wired. Assign your own Texture[] or predeclared VRCUrl[] and optional JSON metadata on the generated behaviour.", "OK");
        }

        Texture2D[] CreateDemoTextures(string folder)
        {
            return new[] { MakeDemo(folder+"/Demo_A.asset", new Color(.12f,.38f,.72f), new Color(.52f,.16f,.72f)), MakeDemo(folder+"/Demo_B.asset", new Color(.08f,.62f,.48f), new Color(.08f,.24f,.46f)), MakeDemo(folder+"/Demo_C.asset", new Color(.9f,.35f,.18f), new Color(.34f,.08f,.34f)) };
        }

        Texture2D MakeDemo(string path, Color a, Color b)
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (existing != null) return existing;
            var t = new Texture2D(320,180,TextureFormat.RGBA32,false); t.name = Path.GetFileNameWithoutExtension(path);
            for(int y=0;y<t.height;y++) for(int x=0;x<t.width;x++) { float u=(float)x/(t.width-1); float v=(float)y/(t.height-1); Color c=Color.Lerp(a,b,(u+v)*.5f); float glow=Mathf.Clamp01(1f-Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f))*1.8f); c+=Color.white*glow*.18f; c.a=1f; t.SetPixel(x,y,c); }
            t.Apply(); AssetDatabase.CreateAsset(t,path); return t;
        }

        Material CreateTransitionMaterial(string folder)
        {
            string path = folder + "/NekoGalleryTransition.mat"; Material existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            Shader shader = Shader.Find("NekoSune/WorldGalleryTransition"); if (shader == null) return null;
            Material m = new Material(shader); AssetDatabase.CreateAsset(m,path); return m;
        }

        static GameObject Ui(string name, Transform parent, params Type[] components) { Type[] all = new Type[components.Length + 1]; all[0] = typeof(RectTransform); Array.Copy(components,0,all,1,components.Length); GameObject go = new GameObject(name, all); go.transform.SetParent(parent,false); return go; }
        static RawImage Raw(Transform parent,string name) { return Ui(name,parent,typeof(RawImage)).GetComponent<RawImage>(); }
        static Text TextNode(Transform parent,string name,string value,int size,FontStyle style,TextAnchor align) { Text t=Ui(name,parent,typeof(Text)).GetComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text=value; t.fontSize=size; t.fontStyle=style; t.alignment=align; t.color=Color.white; return t; }
        static Button ButtonNode(Transform parent,string name,string value,Vector2 anchor,Vector2 size) { GameObject go=Ui(name,parent,typeof(Image),typeof(Button)); RectTransform r=go.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=anchor; r.pivot=new Vector2(.5f,.5f); r.sizeDelta=size; go.GetComponent<Image>().color=new Color(.29f,.56f,.98f,1f); Text t=TextNode(go.transform,"Label",value,28,FontStyle.Bold,TextAnchor.MiddleCenter); Stretch(t.rectTransform,4f); return go.GetComponent<Button>(); }
        static void Stretch(RectTransform r,float p){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(p,p);r.offsetMax=new Vector2(-p,-p);}
        static GameObject Find(GameObject root,string name){Transform[] all=root.GetComponentsInChildren<Transform>(true);for(int i=0;i<all.Length;i++)if(all[i].name==name)return all[i].gameObject;return null;}
        static Type FindType(string simple){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){Type[] ts;try{ts=a.GetTypes();}catch{continue;}for(int i=0;i<ts.Length;i++)if(ts[i]!=null&&ts[i].Name==simple)return ts[i];}return null;}
        static void Set(Component c,string field,object value){FieldInfo f=c.GetType().GetField(field,BindingFlags.Public|BindingFlags.Instance);if(f!=null){Undo.RecordObject(c,"Wire gallery");f.SetValue(c,value);EditorUtility.SetDirty(c);}}
        static void Wire(Button b,Component c,string method){if(b==null||c==null)return;MethodInfo m=c.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance);if(m==null)return;try{UnityAction a=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),c,m);UnityEventTools.AddPersistentListener(b.onClick,a);EditorUtility.SetDirty(b);}catch(Exception e){Debug.LogWarning("[NekoSune Gallery] Could not wire "+method+": "+e.Message);}}

        static void CopyRuntime(){CopyPackageFile("Templates/Runtime/NekoImageGalleryRuntime.cs.txt","Assets/NekoSune/Gallery/Generated/NekoImageGalleryRuntime.cs");}
        static void CopyExampleJson(){string dst="Assets/NekoSune/Gallery/Examples/gallery-items.json";EnsureFolder("Assets/NekoSune/Gallery/Examples");CopyPackageFile("Examples/gallery-items.json",dst);EditorUtility.DisplayDialog("Image Gallery","Copied "+dst,"OK");}
        static void CopyPackageFile(string sourceRelative,string target){string src=Path.GetFullPath(Path.Combine("Packages/com.nekosune.world-gallery",sourceRelative));if(!File.Exists(src)){Debug.LogError("Missing package file: "+src);return;}EnsureFolder(Path.GetDirectoryName(target).Replace('\\','/'));string abs=Path.Combine(Directory.GetParent(Application.dataPath).FullName,target.Replace('/',Path.DirectorySeparatorChar));File.Copy(src,abs,true);AssetDatabase.Refresh();}
        static string EnsureFolder(string path){string[] p=path.Split('/');string cur=p[0];for(int i=1;i<p.Length;i++){string n=cur+"/"+p[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(cur,p[i]);cur=n;}return cur;}
    }
}
