using System;
using System.IO;
using System.Reflection;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NekoSune.WorldPlayerTools.Editor
{
    [NekoAddon(Order = 36)]
    public sealed class NekoWorldPlayerToolsAddon : INekoAddon
    {
        public string Id { get { return "world-player-tools"; } }
        public string TitleKey { get { return "Player Tools"; } }
        public string DescriptionKey { get { return "Build destination teleport menus with player selection and consent-based remote teleport requests."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "↦"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoPlayerTeleportBuilderWindow.Open(); }
    }

    internal sealed class NekoPlayerTeleportBuilderWindow : EditorWindow
    {
        Vector2 _scroll;
        string _destinationA = "Lobby";
        string _destinationB = "Games";
        string _destinationC = "Gallery";

        [MenuItem("NekoSune/World/Player Teleport Builder", false, 26)]
        public static void Open()
        {
            var w = GetWindow<NekoPlayerTeleportBuilderWindow>(false, "Player Teleport Builder", true);
            w.minSize = new Vector2(760f, 590f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Player Teleport", "NekoSune", "World destination menus plus opt-in player-to-player teleport requests");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Demo destination names", NekoStyles.SlotName);
            _destinationA = EditorGUILayout.TextField("Destination 1", _destinationA);
            _destinationB = EditorGUILayout.TextField("Destination 2", _destinationB);
            _destinationC = EditorGUILayout.TextField("Destination 3", _destinationC);
            if (GUILayout.Button("BUILD PLAYER TELEPORT DEMO", NekoStyles.PrimaryButton, GUILayout.Height(36f))) BuildDemo();
            EditorGUILayout.LabelField("Creates a stylish world-space teleport panel and three editable destination transforms. Move the destination objects to the exact positions/rotations you want.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("After UdonSharp compiles", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Keep the generated teleport UI selected and click Auto-Wire. It attaches the runtime, assigns the destinations, fills the UI references, and wires all player/destination/navigation buttons.", NekoStyles.WrapLabel);
            if (GUILayout.Button("AUTO-WIRE SELECTED TELEPORT UI", NekoStyles.PrimaryButton, GUILayout.Height(34f))) AutoWire(Selection.activeGameObject);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("How player teleport works", NekoStyles.SlotName);
            EditorGUILayout.LabelField("TELEPORT ME calls TeleportTo on your local player. REQUEST SELECTED PLAYER sends a small network event containing the selected player ID and destination index. Only the matching player's client handles it, and only when that player has explicitly enabled ALLOW TELEPORT REQUESTS.", NekoStyles.WrapLabel);
            EditorGUILayout.HelpBox("The consent switch defaults OFF on every client. This matches VRChat's rule that Udon can only call TeleportTo on the local player; remote players must teleport themselves after receiving a network request.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Useful UI ideas", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Use the same runtime for teleport hubs, game lobbies, staff/event movement requests, accessibility menus, floor selectors, room navigation, or a player-help panel. Duplicate/add destination Transforms and extend the destinationNames array after generation.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        void BuildDemo()
        {
            GameObject root = new GameObject("Neko Player Teleport UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Create player teleport UI");
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1040f, 720f);
            root.transform.localScale = Vector3.one * .001f;
            root.GetComponent<Image>().color = new Color(.025f,.032f,.055f,.985f);
            TryAddVrcUiShape(root);

            Text header = TextNode(root.transform,"Header","PLAYER TELEPORT",42,FontStyle.Bold,TextAnchor.MiddleLeft); SetRect(header.rectTransform,new Vector2(.05f,.87f),new Vector2(.72f,.97f));
            Text subtitle = TextNode(root.transform,"Subtitle","Choose a destination for yourself or send an opt-in request to another player.",18,FontStyle.Normal,TextAnchor.MiddleLeft); subtitle.color=new Color(.66f,.70f,.80f); SetRect(subtitle.rectTransform,new Vector2(.05f,.81f),new Vector2(.92f,.88f));

            GameObject playerCard = Panel(root.transform,"PlayerCard",new Vector2(.05f,.53f),new Vector2(.95f,.78f));
            Text playerLabel = TextNode(playerCard.transform,"PlayerLabel","SELECTED PLAYER",15,FontStyle.Bold,TextAnchor.UpperLeft); playerLabel.color=new Color(.45f,.68f,1f); SetRect(playerLabel.rectTransform,new Vector2(.05f,.68f),new Vector2(.95f,.93f));
            Text player = TextNode(playerCard.transform,"[SelectedPlayer]","You",31,FontStyle.Bold,TextAnchor.MiddleCenter); SetRect(player.rectTransform,new Vector2(.20f,.30f),new Vector2(.80f,.70f));
            ButtonNode(playerCard.transform,"[PrevPlayer]","‹ PLAYER",new Vector2(.04f,.16f),new Vector2(.22f,.58f));
            ButtonNode(playerCard.transform,"[NextPlayer]","PLAYER ›",new Vector2(.78f,.16f),new Vector2(.96f,.58f));
            ButtonNode(playerCard.transform,"[RefreshPlayers]","REFRESH",new Vector2(.40f,.04f),new Vector2(.60f,.24f));

            GameObject destCard = Panel(root.transform,"DestinationCard",new Vector2(.05f,.28f),new Vector2(.95f,.50f));
            Text destLabel = TextNode(destCard.transform,"DestinationLabel","DESTINATION",15,FontStyle.Bold,TextAnchor.UpperLeft); destLabel.color=new Color(.48f,.84f,.64f); SetRect(destLabel.rectTransform,new Vector2(.05f,.65f),new Vector2(.95f,.92f));
            Text destination = TextNode(destCard.transform,"[SelectedDestination]",_destinationA,32,FontStyle.Bold,TextAnchor.MiddleCenter); SetRect(destination.rectTransform,new Vector2(.20f,.22f),new Vector2(.80f,.70f));
            ButtonNode(destCard.transform,"[PrevDestination]","‹",new Vector2(.05f,.20f),new Vector2(.20f,.62f));
            ButtonNode(destCard.transform,"[NextDestination]","›",new Vector2(.80f,.20f),new Vector2(.95f,.62f));

            ButtonNode(root.transform,"[TeleportMe]","TELEPORT ME",new Vector2(.05f,.15f),new Vector2(.40f,.25f));
            ButtonNode(root.transform,"[RequestTeleport]","REQUEST SELECTED PLAYER",new Vector2(.42f,.15f),new Vector2(.95f,.25f));
            Button consent = ButtonNode(root.transform,"[ConsentButton]","ALLOW TELEPORT REQUESTS: OFF",new Vector2(.05f,.07f),new Vector2(.52f,.13f)); consent.GetComponent<Image>().color=new Color(.42f,.18f,.22f,1f);
            Text consentText = consent.GetComponentInChildren<Text>(); consentText.name="[ConsentText]";
            Text status = TextNode(root.transform,"[StatusText]","Remote requests are OFF by default.",16,FontStyle.Normal,TextAnchor.MiddleLeft); status.color=new Color(.68f,.72f,.82f); SetRect(status.rectTransform,new Vector2(.55f,.065f),new Vector2(.95f,.135f));

            GameObject destinationsRoot = new GameObject("Neko Player Teleport Destinations"); Undo.RegisterCreatedObjectUndo(destinationsRoot,"Create teleport destinations");
            Transform a = Destination(destinationsRoot.transform,_destinationA,new Vector3(0f,0f,2f));
            Transform b = Destination(destinationsRoot.transform,_destinationB,new Vector3(3f,0f,2f));
            Transform c = Destination(destinationsRoot.transform,_destinationC,new Vector3(-3f,0f,2f));
            root.transform.position = Vector3.zero;

            CopyRuntime(); Selection.activeGameObject=root;
            EditorUtility.DisplayDialog("Player Teleport","Created the UI and three destination transforms. Move the destinations where you want, wait for UdonSharp to compile, then Auto-Wire the selected UI.","OK");
        }

        static Transform Destination(Transform parent,string name,Vector3 position)
        {
            GameObject go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.position=position; return go.transform;
        }

        void AutoWire(GameObject root)
        {
            if(root==null||root.name!="Neko Player Teleport UI"){EditorUtility.DisplayDialog("Player Teleport","Select the generated Neko Player Teleport UI root.","OK");return;}
            Type type=FindType("NekoPlayerTeleportSystem"); if(type==null){EditorUtility.DisplayDialog("Player Teleport","NekoPlayerTeleportSystem has not compiled yet. Wait for Unity/UdonSharp and try again.","OK");return;}
            Component runtime=root.GetComponent(type); if(runtime==null)runtime=Undo.AddComponent(root,type);
            GameObject destinationsRoot=GameObject.Find("Neko Player Teleport Destinations"); if(destinationsRoot==null||destinationsRoot.transform.childCount==0){EditorUtility.DisplayDialog("Player Teleport","The generated destination root could not be found.","OK");return;}
            int count=destinationsRoot.transform.childCount; Transform[] destinations=new Transform[count]; string[] names=new string[count];
            for(int i=0;i<count;i++){destinations[i]=destinationsRoot.transform.GetChild(i);names[i]=destinations[i].name;}
            Set(runtime,"destinations",destinations); Set(runtime,"destinationNames",names); Set(runtime,"allowRemoteTeleportRequests",false);
            Set(runtime,"selectedPlayerText",Find(root,"[SelectedPlayer]").GetComponent<Text>()); Set(runtime,"selectedDestinationText",Find(root,"[SelectedDestination]").GetComponent<Text>()); Set(runtime,"consentText",Find(root,"[ConsentText]").GetComponent<Text>()); Set(runtime,"statusText",Find(root,"[StatusText]").GetComponent<Text>());
            Wire(Find(root,"[PrevPlayer]").GetComponent<Button>(),runtime,"PreviousPlayer"); Wire(Find(root,"[NextPlayer]").GetComponent<Button>(),runtime,"NextPlayer"); Wire(Find(root,"[RefreshPlayers]").GetComponent<Button>(),runtime,"RefreshPlayers");
            Wire(Find(root,"[PrevDestination]").GetComponent<Button>(),runtime,"PreviousDestination"); Wire(Find(root,"[NextDestination]").GetComponent<Button>(),runtime,"NextDestination"); Wire(Find(root,"[TeleportMe]").GetComponent<Button>(),runtime,"TeleportMe"); Wire(Find(root,"[RequestTeleport]").GetComponent<Button>(),runtime,"RequestSelectedPlayerTeleport"); Wire(Find(root,"[ConsentButton]").GetComponent<Button>(),runtime,"ToggleAllowTeleportRequests");
            EditorUtility.SetDirty(runtime); EditorUtility.DisplayDialog("Player Teleport","Auto-wired. Remote teleport consent remains OFF by default on each player. Test the request flow with at least two VRChat Build & Test clients.","OK");
        }

        static GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max){GameObject go=Ui(name,parent,typeof(Image));SetRect(go.GetComponent<RectTransform>(),min,max);go.GetComponent<Image>().color=new Color(.055f,.067f,.105f,1f);return go;}
        static GameObject Ui(string name,Transform parent,params Type[] components){Type[] all=new Type[components.Length+1];all[0]=typeof(RectTransform);Array.Copy(components,0,all,1,components.Length);GameObject go=new GameObject(name,all);go.transform.SetParent(parent,false);return go;}
        static Text TextNode(Transform parent,string name,string value,int size,FontStyle style,TextAnchor align){Text t=Ui(name,parent,typeof(Text)).GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=size;t.fontStyle=style;t.alignment=align;t.color=Color.white;return t;}
        static Button ButtonNode(Transform parent,string name,string label,Vector2 min,Vector2 max){GameObject go=Ui(name,parent,typeof(Image),typeof(Button));SetRect(go.GetComponent<RectTransform>(),min,max);go.GetComponent<Image>().color=new Color(.29f,.56f,.98f,1f);Text t=TextNode(go.transform,"Label",label,17,FontStyle.Bold,TextAnchor.MiddleCenter);Stretch(t.rectTransform,6f);return go.GetComponent<Button>();}
        static void SetRect(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
        static void Stretch(RectTransform r,float p){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(p,p);r.offsetMax=new Vector2(-p,-p);}
        static GameObject Find(GameObject root,string name){Transform[] all=root.GetComponentsInChildren<Transform>(true);for(int i=0;i<all.Length;i++)if(all[i].name==name)return all[i].gameObject;return null;}
        static Type FindType(string simpleOrFull){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){Type direct=a.GetType(simpleOrFull,false);if(direct!=null)return direct;Type[] ts;try{ts=a.GetTypes();}catch{continue;}for(int i=0;i<ts.Length;i++)if(ts[i]!=null&&(ts[i].Name==simpleOrFull||ts[i].FullName==simpleOrFull))return ts[i];}return null;}
        static void Set(Component c,string field,object value){FieldInfo f=c.GetType().GetField(field,BindingFlags.Public|BindingFlags.Instance);if(f!=null){Undo.RecordObject(c,"Wire player teleport");f.SetValue(c,value);EditorUtility.SetDirty(c);}}
        static void Wire(Button b,Component c,string method){if(b==null||c==null)return;MethodInfo m=c.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance);if(m==null)return;try{UnityAction a=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),c,m);UnityEventTools.AddPersistentListener(b.onClick,a);EditorUtility.SetDirty(b);}catch(Exception e){Debug.LogWarning("[NekoSune Player Tools] Could not wire "+method+": "+e.Message);}}
        static void TryAddVrcUiShape(GameObject root){Type t=FindType("VRC.SDK3.Components.VRCUiShape");if(t==null)t=FindType("VRCUiShape");if(t==null)t=FindType("VRC_UiShape");if(t!=null&&root.GetComponent(t)==null)Undo.AddComponent(root,t);}
        static void CopyRuntime(){string src=Path.GetFullPath(Path.Combine("Packages/com.nekosune.world-player-tools/Templates/Runtime/NekoPlayerTeleportSystem.cs.txt"));string folder=EnsureFolder("Assets/NekoSune/PlayerTools/Generated");string dst=Path.Combine(Directory.GetParent(Application.dataPath).FullName,(folder+"/NekoPlayerTeleportSystem.cs").Replace('/',Path.DirectorySeparatorChar));File.Copy(src,dst,true);AssetDatabase.Refresh();}
        static string EnsureFolder(string path){string[]p=path.Split('/');string cur=p[0];for(int i=1;i<p.Length;i++){string n=cur+"/"+p[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(cur,p[i]);cur=n;}return cur;}
    }
}
