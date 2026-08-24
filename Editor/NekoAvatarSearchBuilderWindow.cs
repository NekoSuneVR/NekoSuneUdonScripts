using System;
using System.IO;
using System.Reflection;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NekoSune.WorldAvatarSearch.Editor
{
    [NekoAddon(Order = 35)]
    public sealed class NekoWorldAvatarSearchAddon : INekoAddon
    {
        public string Id { get { return "world-avatar-search"; } }
        public string TitleKey { get { return "Avatar Search"; } }
        public string DescriptionKey { get { return "Build a stylish paginated JSON/VRCX avatar browser with flexible endpoint mapping and real VRChat avatar switching."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "A"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoAvatarSearchBuilderWindow.Open(); }
    }

    internal sealed class NekoAvatarSearchBuilderWindow : EditorWindow
    {
        const string DemoUrl = "https://vrcavatarsearch.nekosunevr.co.uk/vrcx_search?search=Rindo";
        Vector2 _scroll;
        string _rootKey = "";
        string _idKey = "id";
        string _nameKey = "name";
        string _authorKey = "authorName";
        string _descriptionKey = "description";
        string _statusKey = "releaseStatus";
        string _thumbnailKey = "thumbnailImageUrl";

        [MenuItem("NekoSune/World/Avatar Search Builder", false, 25)]
        public static void Open()
        {
            var w = GetWindow<NekoAvatarSearchBuilderWindow>(false, "Avatar Search Builder", true);
            w.minSize = new Vector2(780f, 620f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Avatar Search", "NekoSune", "Search JSON/VRCX-style endpoints, page through results and switch through a VRChat avatar pedestal");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Build demo browser", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Creates a paginated eight-card browser, selected-avatar details, a real VRC URL search field, and a separate 3D preview pedestal. The URL field starts with the NekoSune Rindo demo so a user can replace the search term in-place.", NekoStyles.WrapLabel);
            if (GUILayout.Button("BUILD AVATAR SEARCH DEMO", NekoStyles.PrimaryButton, GUILayout.Height(36f))) BuildDemo();
            if (GUILayout.Button("AUTO-WIRE SELECTED SEARCH UI", NekoStyles.PrimaryButton, GUILayout.Height(34f))) AutoWire(Selection.activeGameObject);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("JSON adapter", NekoStyles.SlotName);
            _rootKey = EditorGUILayout.TextField("Root array key (blank = root array)", _rootKey);
            _idKey = EditorGUILayout.TextField("Avatar ID", _idKey);
            _nameKey = EditorGUILayout.TextField("Avatar name", _nameKey);
            _authorKey = EditorGUILayout.TextField("Author", _authorKey);
            _descriptionKey = EditorGUILayout.TextField("Description", _descriptionKey);
            _statusKey = EditorGUILayout.TextField("Release status", _statusKey);
            _thumbnailKey = EditorGUILayout.TextField("Thumbnail URL metadata", _thumbnailKey);
            EditorGUILayout.LabelField("The runtime also auto-tries avatars/results/items/data wrappers and common aliases such as avatarId, avatar_id, avatarName, title, author and creatorName. Up to 128 mapped results are kept by default and rendered eight at a time.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("VRChat URL restriction", NekoStyles.SlotName);
            EditorGUILayout.LabelField("A normal InputField cannot be converted into an arbitrary VRCUrl at runtime. The browser therefore uses a real VRCUrlInputField. The demo value is created at editor time; in VRChat, users may edit the complete URL and press SEARCH. Untrusted API domains require Allow Untrusted URLs.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Real avatar interaction", NekoStyles.SlotName);
            EditorGUILayout.LabelField("PREVIEW uses VRCAvatarPedestal.SwitchAvatar(id). USE AVATAR switches the pedestal and calls SetAvatarUse for the local player. VRChat keeps its normal public/private/Marketplace ownership behaviour.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        void BuildDemo()
        {
            Type urlInputType = FindType("VRC.SDK3.Components.VRCUrlInputField");
            Type pedestalType = FindType("VRC.SDK3.Components.VRCAvatarPedestal");
            if (pedestalType == null) pedestalType = FindType("VRC.SDKBase.VRC_AvatarPedestal");
            Type urlType = FindType("VRC.SDKBase.VRCUrl");
            if (urlInputType == null || pedestalType == null || urlType == null)
            {
                EditorUtility.DisplayDialog("Avatar Search", "VRChat Worlds SDK URL/pedestal components were not found.", "OK");
                return;
            }

            GameObject root = new GameObject("Neko Avatar Search UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Create avatar search UI");
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1320f, 900f);
            root.transform.localScale = Vector3.one * 0.001f;
            root.GetComponent<Image>().color = new Color(.025f,.032f,.055f,.985f);
            TryAddVrcUiShape(root);

            Text header = TextNode(root.transform,"Header","AVATAR SEARCH",44,FontStyle.Bold,TextAnchor.MiddleLeft);
            SetRect(header.rectTransform,new Vector2(.03f,.90f),new Vector2(.48f,.98f));
            Text sub = TextNode(root.transform,"SubHeader","VRCX-style JSON browser • flexible mapper • 8 results per page",20,FontStyle.Normal,TextAnchor.MiddleLeft);
            SetRect(sub.rectTransform,new Vector2(.03f,.86f),new Vector2(.60f,.91f));

            GameObject urlBox = UiDynamic("[UrlInput]", root.transform, new[] { typeof(Image), urlInputType });
            SetRect(urlBox.GetComponent<RectTransform>(),new Vector2(.03f,.79f),new Vector2(.66f,.85f));
            Image urlBackground = urlBox.GetComponent<Image>();
            urlBackground.color = new Color(.08f,.10f,.15f,1f);
            Component vrcInput = urlBox.GetComponent(urlInputType);
            Text inputText = TextNode(urlBox.transform,"Text","",18,FontStyle.Normal,TextAnchor.MiddleLeft);
            Stretch(inputText.rectTransform,12f);
            Text placeholder = TextNode(urlBox.transform,"Placeholder","Paste complete search API URL...",18,FontStyle.Italic,TextAnchor.MiddleLeft);
            placeholder.color = new Color(.55f,.58f,.68f,1f);
            Stretch(placeholder.rectTransform,12f);
            SetMember(vrcInput,"textComponent",inputText);
            SetMember(vrcInput,"placeholder",placeholder);
            SetMember(vrcInput,"targetGraphic",urlBackground);
            SetMember(vrcInput,"navigation",new Navigation { mode = Navigation.Mode.None });

            object demoUrl = Activator.CreateInstance(urlType,new object[]{DemoUrl});
            MethodInfo setUrl = urlInputType.GetMethod("SetUrl", BindingFlags.Public | BindingFlags.Instance);
            if (setUrl != null) setUrl.Invoke(vrcInput,new[]{demoUrl});

            ButtonNode(root.transform,"[SearchButton]","SEARCH",new Vector2(.675f,.79f),new Vector2(.79f,.85f));
            ButtonNode(root.transform,"[DemoButton]","DEMO RINDO",new Vector2(.80f,.79f),new Vector2(.97f,.85f));

            GameObject listPanel = Ui("Results",root.transform,typeof(Image));
            SetRect(listPanel.GetComponent<RectTransform>(),new Vector2(.03f,.16f),new Vector2(.56f,.76f));
            listPanel.GetComponent<Image>().color = new Color(.045f,.055f,.085f,1f);
            var layout = listPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12,12,12,12);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            for (int i = 0; i < 8; i++) CreateResultCard(listPanel.transform,i);

            ButtonNode(root.transform,"[PrevPage]","‹ PAGE",new Vector2(.03f,.10f),new Vector2(.14f,.145f));
            Text page = TextNode(root.transform,"[ResultsPage]","Page 0 / 0",16,FontStyle.Bold,TextAnchor.MiddleCenter);
            SetRect(page.rectTransform,new Vector2(.15f,.10f),new Vector2(.44f,.145f));
            ButtonNode(root.transform,"[NextPage]","PAGE ›",new Vector2(.45f,.10f),new Vector2(.56f,.145f));

            GameObject detail = Ui("Details",root.transform,typeof(Image));
            SetRect(detail.GetComponent<RectTransform>(),new Vector2(.58f,.10f),new Vector2(.97f,.76f));
            detail.GetComponent<Image>().color = new Color(.055f,.067f,.105f,1f);
            Text selectedName = TextNode(detail.transform,"[SelectedName]","NO AVATAR SELECTED",34,FontStyle.Bold,TextAnchor.UpperLeft);
            SetRect(selectedName.rectTransform,new Vector2(.06f,.78f),new Vector2(.94f,.94f));
            Text selectedAuthor = TextNode(detail.transform,"[SelectedAuthor]","",21,FontStyle.Bold,TextAnchor.UpperLeft);
            selectedAuthor.color = new Color(.4f,.68f,1f);
            SetRect(selectedAuthor.rectTransform,new Vector2(.06f,.70f),new Vector2(.94f,.80f));
            Text selectedDesc = TextNode(detail.transform,"[SelectedDescription]","Search for an avatar to begin.",19,FontStyle.Normal,TextAnchor.UpperLeft);
            selectedDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            selectedDesc.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(selectedDesc.rectTransform,new Vector2(.06f,.37f),new Vector2(.94f,.69f));
            Text selectedId = TextNode(detail.transform,"[SelectedId]","",14,FontStyle.Normal,TextAnchor.UpperLeft);
            selectedId.color = new Color(.62f,.65f,.72f);
            SetRect(selectedId.rectTransform,new Vector2(.06f,.29f),new Vector2(.94f,.37f));
            ButtonNode(detail.transform,"[PreviewButton]","PREVIEW",new Vector2(.06f,.15f),new Vector2(.46f,.25f));
            ButtonNode(detail.transform,"[UseButton]","USE AVATAR",new Vector2(.50f,.15f),new Vector2(.94f,.25f));
            Text note = TextNode(detail.transform,"Note","3D pedestal preview is shared world content. USE AVATAR only changes your local avatar.",15,FontStyle.Normal,TextAnchor.UpperLeft);
            note.color = new Color(.65f,.68f,.75f);
            SetRect(note.rectTransform,new Vector2(.06f,.04f),new Vector2(.94f,.13f));

            Text status = TextNode(root.transform,"[StatusText]","Edit the URL search term or press DEMO RINDO.",17,FontStyle.Normal,TextAnchor.MiddleLeft);
            status.color = new Color(.64f,.70f,.82f);
            SetRect(status.rectTransform,new Vector2(.03f,.02f),new Vector2(.97f,.08f));

            GameObject pedestalGo = new GameObject("Neko Avatar Search Preview Pedestal", pedestalType);
            Undo.RegisterCreatedObjectUndo(pedestalGo,"Create avatar preview pedestal");
            pedestalGo.transform.position = root.transform.position + root.transform.right * 1.7f;
            Component pedestal = pedestalGo.GetComponent(pedestalType);
            SetMember(pedestal,"ChangeAvatarsOnUse",true);
            SetMember(pedestal,"scale",.85f);
            GameObject placement = new GameObject("Preview Placement");
            placement.transform.SetParent(pedestalGo.transform,false);
            SetMember(pedestal,"Placement",placement.transform);

            CopyRuntime();
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Avatar Search","Created the paginated search UI and preview pedestal. The URL field is prefilled with the Rindo demo. Wait for UdonSharp to compile, then Auto-Wire the selected UI.","OK");
        }

        void CreateResultCard(Transform parent,int index)
        {
            GameObject card = Ui("[Result"+index+"]",parent,typeof(Image),typeof(Button),typeof(LayoutElement));
            card.GetComponent<Image>().color = index % 2 == 0 ? new Color(.075f,.09f,.135f,1f) : new Color(.065f,.078f,.118f,1f);
            card.GetComponent<LayoutElement>().preferredHeight = 54f;
            Text name = TextNode(card.transform,"[Result"+index+"Name]","Avatar Result",18,FontStyle.Bold,TextAnchor.UpperLeft);
            SetRect(name.rectTransform,new Vector2(.03f,.45f),new Vector2(.72f,.94f));
            Text author = TextNode(card.transform,"[Result"+index+"Author]","by creator",13,FontStyle.Normal,TextAnchor.LowerLeft);
            author.color = new Color(.63f,.68f,.78f);
            SetRect(author.rectTransform,new Vector2(.03f,.08f),new Vector2(.72f,.48f));
            Text desc = TextNode(card.transform,"[Result"+index+"Description]","",12,FontStyle.Normal,TextAnchor.MiddleLeft);
            desc.gameObject.SetActive(false);
            Text status = TextNode(card.transform,"[Result"+index+"Status]","PUBLIC",12,FontStyle.Bold,TextAnchor.MiddleCenter);
            status.color = new Color(.45f,.86f,.58f);
            SetRect(status.rectTransform,new Vector2(.75f,.22f),new Vector2(.97f,.78f));
            card.SetActive(false);
        }

        void AutoWire(GameObject root)
        {
            if (root == null || root.name != "Neko Avatar Search UI")
            {
                EditorUtility.DisplayDialog("Avatar Search","Select the generated Neko Avatar Search UI root.","OK");
                return;
            }

            Type runtimeType = FindType("NekoAvatarSearchBrowser");
            Type urlInputType = FindType("VRC.SDK3.Components.VRCUrlInputField");
            Type pedestalType = FindType("VRC.SDK3.Components.VRCAvatarPedestal");
            if (pedestalType == null) pedestalType = FindType("VRC.SDKBase.VRC_AvatarPedestal");
            Type urlType = FindType("VRC.SDKBase.VRCUrl");
            if (runtimeType == null || urlInputType == null || urlType == null)
            {
                EditorUtility.DisplayDialog("Avatar Search","The generated runtime or VRChat URL types are not compiled/available yet.","OK");
                return;
            }

            Component c = root.GetComponent(runtimeType);
            if (c == null) c = Undo.AddComponent(root,runtimeType);
            Component urlInput = Find(root,"[UrlInput]").GetComponent(urlInputType);
            Set(c,"searchUrlInput",urlInput);
            object demoUrl = Activator.CreateInstance(urlType,new object[]{DemoUrl});
            Set(c,"demoSearchUrl",demoUrl);
            MethodInfo setUrl = urlInputType.GetMethod("SetUrl", BindingFlags.Public | BindingFlags.Instance);
            if (setUrl != null) setUrl.Invoke(urlInput,new[]{demoUrl});
            Set(c,"maxResults",128);
            Set(c,"rootKey",_rootKey);
            Set(c,"idKey",_idKey);
            Set(c,"nameKey",_nameKey);
            Set(c,"authorKey",_authorKey);
            Set(c,"descriptionKey",_descriptionKey);
            Set(c,"releaseStatusKey",_statusKey);
            Set(c,"thumbnailKey",_thumbnailKey);

            GameObject[] cards = new GameObject[8];
            Text[] names = new Text[8];
            Text[] authors = new Text[8];
            Text[] descs = new Text[8];
            Text[] statuses = new Text[8];
            for (int i = 0; i < 8; i++)
            {
                cards[i] = Find(root,"[Result"+i+"]");
                names[i] = Find(root,"[Result"+i+"Name]").GetComponent<Text>();
                authors[i] = Find(root,"[Result"+i+"Author]").GetComponent<Text>();
                descs[i] = Find(root,"[Result"+i+"Description]").GetComponent<Text>();
                statuses[i] = Find(root,"[Result"+i+"Status]").GetComponent<Text>();
                Wire(cards[i].GetComponent<Button>(),c,"Select"+i);
            }
            Set(c,"resultCards",cards);
            Set(c,"resultNames",names);
            Set(c,"resultAuthors",authors);
            Set(c,"resultDescriptions",descs);
            Set(c,"resultStatus",statuses);
            Set(c,"resultsPageText",Find(root,"[ResultsPage]").GetComponent<Text>());
            Set(c,"selectedName",Find(root,"[SelectedName]").GetComponent<Text>());
            Set(c,"selectedAuthor",Find(root,"[SelectedAuthor]").GetComponent<Text>());
            Set(c,"selectedDescription",Find(root,"[SelectedDescription]").GetComponent<Text>());
            Set(c,"selectedId",Find(root,"[SelectedId]").GetComponent<Text>());
            Set(c,"statusText",Find(root,"[StatusText]").GetComponent<Text>());

            GameObject p = GameObject.Find("Neko Avatar Search Preview Pedestal");
            if (p != null && pedestalType != null) Set(c,"previewPedestal",p.GetComponent(pedestalType));

            Wire(Find(root,"[SearchButton]").GetComponent<Button>(),c,"SearchFromUrlField");
            Wire(Find(root,"[DemoButton]").GetComponent<Button>(),c,"SearchDemo");
            Wire(Find(root,"[PrevPage]").GetComponent<Button>(),c,"PreviousPage");
            Wire(Find(root,"[NextPage]").GetComponent<Button>(),c,"NextPage");
            Wire(Find(root,"[PreviewButton]").GetComponent<Button>(),c,"PreviewSelected");
            Wire(Find(root,"[UseButton]").GetComponent<Button>(),c,"UseSelectedAvatar");
            EditorUtility.SetDirty(c);
            EditorUtility.DisplayDialog("Avatar Search","Auto-wired with pagination and the prefilled Rindo demo URL. Users can edit the complete URL in VRChat and press SEARCH.","OK");
        }

        static GameObject Ui(string name,Transform parent,params Type[] components) { return UiDynamic(name,parent,components); }

        static GameObject UiDynamic(string name,Transform parent,Type[] components)
        {
            Type[] all = new Type[components.Length + 1];
            all[0] = typeof(RectTransform);
            Array.Copy(components,0,all,1,components.Length);
            GameObject go = new GameObject(name,all);
            go.transform.SetParent(parent,false);
            return go;
        }

        static Text TextNode(Transform parent,string name,string value,int size,FontStyle style,TextAnchor align)
        {
            Text t = Ui(name,parent,typeof(Text)).GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }

        static Button ButtonNode(Transform parent,string name,string label,Vector2 min,Vector2 max)
        {
            GameObject go = Ui(name,parent,typeof(Image),typeof(Button));
            SetRect(go.GetComponent<RectTransform>(),min,max);
            go.GetComponent<Image>().color = new Color(.29f,.56f,.98f,1f);
            Text t = TextNode(go.transform,"Label",label,17,FontStyle.Bold,TextAnchor.MiddleCenter);
            Stretch(t.rectTransform,6f);
            return go.GetComponent<Button>();
        }

        static void SetRect(RectTransform r,Vector2 min,Vector2 max)
        {
            r.anchorMin=min; r.anchorMax=max; r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero;
        }

        static void Stretch(RectTransform r,float p)
        {
            r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=new Vector2(p,p); r.offsetMax=new Vector2(-p,-p);
        }

        static GameObject Find(GameObject root,string name)
        {
            Transform[] all=root.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<all.Length;i++) if(all[i].name==name) return all[i].gameObject;
            return null;
        }

        static Type FindType(string simpleOrFull)
        {
            foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type direct=a.GetType(simpleOrFull,false);
                if(direct!=null)return direct;
                Type[] ts;
                try{ts=a.GetTypes();}catch{continue;}
                for(int i=0;i<ts.Length;i++) if(ts[i]!=null&&(ts[i].Name==simpleOrFull||ts[i].FullName==simpleOrFull)) return ts[i];
            }
            return null;
        }

        static void Set(Component c,string field,object value)
        {
            FieldInfo f=c.GetType().GetField(field,BindingFlags.Public|BindingFlags.Instance);
            if(f!=null)
            {
                Undo.RecordObject(c,"Wire avatar search");
                f.SetValue(c,value);
                EditorUtility.SetDirty(c);
            }
        }

        static void SetMember(Component c,string name,object value)
        {
            if(c==null)return;
            Type t=c.GetType();
            FieldInfo f=t.GetField(name,BindingFlags.Public|BindingFlags.Instance);
            if(f!=null){f.SetValue(c,value);return;}
            PropertyInfo p=t.GetProperty(name,BindingFlags.Public|BindingFlags.Instance);
            if(p!=null&&p.CanWrite)p.SetValue(c,value,null);
        }

        static void Wire(Button b,Component c,string method)
        {
            if(b==null||c==null)return;
            MethodInfo m=c.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance);
            if(m==null)return;
            try
            {
                UnityAction a=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),c,m);
                UnityEventTools.AddPersistentListener(b.onClick,a);
                EditorUtility.SetDirty(b);
            }
            catch(Exception e){Debug.LogWarning("[NekoSune Avatar Search] Could not wire "+method+": "+e.Message);}
        }

        static void TryAddVrcUiShape(GameObject root)
        {
            Type t=FindType("VRC.SDK3.Components.VRCUiShape");
            if(t==null)t=FindType("VRCUiShape");
            if(t==null)t=FindType("VRC_UiShape");
            if(t!=null&&root.GetComponent(t)==null)Undo.AddComponent(root,t);
        }

        static void CopyRuntime()
        {
            string src=Path.GetFullPath(Path.Combine("Packages/com.nekosune.world-avatar-search/Templates/Runtime/NekoAvatarSearchBrowser.cs.txt"));
            string folder=EnsureFolder("Assets/NekoSune/AvatarSearch/Generated");
            string dst=Path.Combine(Directory.GetParent(Application.dataPath).FullName,(folder+"/NekoAvatarSearchBrowser.cs").Replace('/',Path.DirectorySeparatorChar));
            File.Copy(src,dst,true);
            AssetDatabase.Refresh();
        }

        static string EnsureFolder(string path)
        {
            string[]p=path.Split('/'); string cur=p[0];
            for(int i=1;i<p.Length;i++)
            {
                string n=cur+"/"+p[i];
                if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(cur,p[i]);
                cur=n;
            }
            return cur;
        }
    }
}
