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
        public string DescriptionKey { get { return "Build destination and player-to-player teleport menus with consent requests and restricted zones."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "↦"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoPlayerTeleportBuilderWindow.Open(); }
    }

    internal sealed class NekoPlayerTeleportBuilderWindow : EditorWindow
    {
        const string GeneratedFolder = "Assets/NekoSune/PlayerTools/Generated";
        const string GeneratedScriptPath = GeneratedFolder + "/NekoPlayerTeleportSystem.cs";
        const string GeneratedProgramPath = GeneratedFolder + "/NekoPlayerTeleportSystem.asset";

        Vector2 _scroll;
        string _destinationA = "Lobby";
        string _destinationB = "Games";
        string _destinationC = "Gallery";

        [MenuItem("NekoSune/World/Player Teleport Builder", false, 26)]
        public static void Open()
        {
            var w = GetWindow<NekoPlayerTeleportBuilderWindow>(false, "Player Teleport Builder", true);
            w.minSize = new Vector2(790f, 650f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Player Teleport", "NekoSune", "World destinations + player-to-player teleport with creator restricted areas");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Demo destination names", NekoStyles.SlotName);
            _destinationA = EditorGUILayout.TextField("Destination 1", _destinationA);
            _destinationB = EditorGUILayout.TextField("Destination 2", _destinationB);
            _destinationC = EditorGUILayout.TextField("Destination 3", _destinationC);
            if (GUILayout.Button("BUILD PLAYER TELEPORT DEMO", NekoStyles.PrimaryButton, GUILayout.Height(36f))) BuildDemo();
            EditorGUILayout.LabelField("Creates the UI, three editable world destinations and two sample BoxCollider restricted zones. Resize/move the restricted colliders in the Scene view.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("After UdonSharp compiles", NekoStyles.SlotName);
            if (GUILayout.Button("AUTO-WIRE / REPAIR SELECTED TELEPORT UI", NekoStyles.PrimaryButton, GUILayout.Height(34f))) AutoWire(Selection.activeGameObject);
            EditorGUILayout.LabelField("Auto-Wire verifies/repairs the generated UdonSharpProgramAsset before attaching the runtime, then assigns destinations, restricted zones, UI references and buttons.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Player-to-player modes", NekoStyles.SlotName);
            EditorGUILayout.LabelField("TELEPORT ME → TARGET moves your local player next to the selected target player. REQUEST PLAYER → TARGET asks the selected player's client to move next to another selected target player. REQUEST PLAYER → ME is a convenience version that asks the selected player to come to you. Remote movement still requires that player's ALLOW TELEPORT REQUESTS switch.", NekoStyles.WrapLabel);
            EditorGUILayout.HelpBox("Restricted BoxColliders are checked on the client that would teleport. If the target player or computed arrival point is inside a restricted zone, the player-to-player teleport is rejected.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("World destination modes", NekoStyles.SlotName);
            EditorGUILayout.LabelField("TELEPORT ME → DESTINATION keeps the normal fixed-world teleport system. REQUEST PLAYER → DESTINATION asks the selected player's own client to teleport to the selected world marker, still requiring consent.", NekoStyles.WrapLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        void BuildDemo()
        {
            GameObject root = new GameObject("Neko Player Teleport UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Create player teleport UI");
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1180f, 900f);
            root.transform.localScale = Vector3.one * .001f;
            root.GetComponent<Image>().color = new Color(.025f,.032f,.055f,.985f);
            TryAddVrcUiShape(root);

            Text header = TextNode(root.transform,"Header","PLAYER TELEPORT",42,FontStyle.Bold,TextAnchor.MiddleLeft); SetRect(header.rectTransform,new Vector2(.04f,.91f),new Vector2(.72f,.98f));
            Text subtitle = TextNode(root.transform,"Subtitle","Player → player, player → destination, consent requests and restricted zones",18,FontStyle.Normal,TextAnchor.MiddleLeft); subtitle.color=new Color(.66f,.70f,.80f); SetRect(subtitle.rectTransform,new Vector2(.04f,.865f),new Vector2(.96f,.92f));

            GameObject moverCard = Panel(root.transform,"MoverCard",new Vector2(.04f,.68f),new Vector2(.48f,.84f));
            Text moverLabel = TextNode(moverCard.transform,"MoverLabel","PLAYER TO MOVE",14,FontStyle.Bold,TextAnchor.UpperLeft); moverLabel.color=new Color(.45f,.68f,1f); SetRect(moverLabel.rectTransform,new Vector2(.05f,.67f),new Vector2(.95f,.94f));
            Text mover = TextNode(moverCard.transform,"[SelectedPlayer]","You",27,FontStyle.Bold,TextAnchor.MiddleCenter); SetRect(mover.rectTransform,new Vector2(.20f,.26f),new Vector2(.80f,.70f));
            ButtonNode(moverCard.transform,"[PrevPlayer]","‹",new Vector2(.04f,.18f),new Vector2(.18f,.60f));
            ButtonNode(moverCard.transform,"[NextPlayer]","›",new Vector2(.82f,.18f),new Vector2(.96f,.60f));

            GameObject targetCard = Panel(root.transform,"TargetCard",new Vector2(.52f,.68f),new Vector2(.96f,.84f));
            Text targetLabel = TextNode(targetCard.transform,"TargetLabel","TARGET PLAYER",14,FontStyle.Bold,TextAnchor.UpperLeft); targetLabel.color=new Color(.52f,.88f,.68f); SetRect(targetLabel.rectTransform,new Vector2(.05f,.67f),new Vector2(.95f,.94f));
            Text target = TextNode(targetCard.transform,"[SelectedTargetPlayer]","Target player",27,FontStyle.Bold,TextAnchor.MiddleCenter); SetRect(target.rectTransform,new Vector2(.20f,.26f),new Vector2(.80f,.70f));
            ButtonNode(targetCard.transform,"[PrevTargetPlayer]","‹",new Vector2(.04f,.18f),new Vector2(.18f,.60f));
            ButtonNode(targetCard.transform,"[NextTargetPlayer]","›",new Vector2(.82f,.18f),new Vector2(.96f,.60f));

            ButtonNode(root.transform,"[RefreshPlayers]","REFRESH PLAYERS",new Vector2(.38f,.625f),new Vector2(.62f,.67f));

            ButtonNode(root.transform,"[TeleportMeToPlayer]","TELEPORT ME → TARGET",new Vector2(.04f,.545f),new Vector2(.33f,.61f));
            ButtonNode(root.transform,"[RequestPlayerToTarget]","REQUEST PLAYER → TARGET",new Vector2(.35f,.545f),new Vector2(.66f,.61f));
            ButtonNode(root.transform,"[RequestPlayerToMe]","REQUEST PLAYER → ME",new Vector2(.68f,.545f),new Vector2(.96f,.61f));

            GameObject destCard = Panel(root.transform,"DestinationCard",new Vector2(.04f,.33f),new Vector2(.96f,.51f));
            Text destLabel = TextNode(destCard.transform,"DestinationLabel","WORLD DESTINATION",14,FontStyle.Bold,TextAnchor.UpperLeft); destLabel.color=new Color(.48f,.84f,.64f); SetRect(destLabel.rectTransform,new Vector2(.05f,.65f),new Vector2(.95f,.92f));
            Text destination = TextNode(destCard.transform,"[SelectedDestination]",_destinationA,30,FontStyle.Bold,TextAnchor.MiddleCenter); SetRect(destination.rectTransform,new Vector2(.20f,.22f),new Vector2(.80f,.70f));
            ButtonNode(destCard.transform,"[PrevDestination]","‹",new Vector2(.05f,.20f),new Vector2(.20f,.62f));
            ButtonNode(destCard.transform,"[NextDestination]","›",new Vector2(.80f,.20f),new Vector2(.95f,.62f));

            ButtonNode(root.transform,"[TeleportMe]","TELEPORT ME → DESTINATION",new Vector2(.04f,.245f),new Vector2(.47f,.315f));
            ButtonNode(root.transform,"[RequestTeleport]","REQUEST PLAYER → DESTINATION",new Vector2(.49f,.245f),new Vector2(.96f,.315f));

            Button consent = ButtonNode(root.transform,"[ConsentButton]","ALLOW TELEPORT REQUESTS: OFF",new Vector2(.04f,.155f),new Vector2(.47f,.225f)); consent.GetComponent<Image>().color=new Color(.42f,.18f,.22f,1f);
            Text consentText = consent.GetComponentInChildren<Text>(); consentText.name="[ConsentText]";
            Text restricted = TextNode(root.transform,"RestrictedNote","Restricted zones block player-to-player arrival even when consent is ON.",14,FontStyle.Normal,TextAnchor.MiddleLeft); restricted.color=new Color(.95f,.67f,.42f); SetRect(restricted.rectTransform,new Vector2(.50f,.155f),new Vector2(.96f,.225f));
            Text status = TextNode(root.transform,"[StatusText]","Remote requests are OFF by default.",15,FontStyle.Normal,TextAnchor.MiddleLeft); status.color=new Color(.68f,.72f,.82f); SetRect(status.rectTransform,new Vector2(.04f,.055f),new Vector2(.96f,.135f));

            GameObject destinationsRoot = new GameObject("Neko Player Teleport Destinations"); Undo.RegisterCreatedObjectUndo(destinationsRoot,"Create teleport destinations");
            Destination(destinationsRoot.transform,_destinationA,new Vector3(0f,0f,2f));
            Destination(destinationsRoot.transform,_destinationB,new Vector3(3f,0f,2f));
            Destination(destinationsRoot.transform,_destinationC,new Vector3(-3f,0f,2f));

            GameObject restrictedRoot = new GameObject("Neko Player Teleport Restricted Areas"); Undo.RegisterCreatedObjectUndo(restrictedRoot,"Create restricted teleport zones");
            RestrictedArea(restrictedRoot.transform,"Restricted Area - Example A",new Vector3(0f,1.5f,8f),new Vector3(4f,3f,4f));
            RestrictedArea(restrictedRoot.transform,"Restricted Area - Example B",new Vector3(7f,1.5f,0f),new Vector3(3f,3f,5f));

            root.transform.position = Vector3.zero;
            CopyRuntime(); Selection.activeGameObject=root;
            EditorUtility.DisplayDialog("Player Teleport","Created player-to-player + destination UI, three destination transforms and two sample restricted BoxColliders. Let Unity compile, then Auto-Wire / Repair.","OK");
        }

        static Transform Destination(Transform parent,string name,Vector3 position)
        {
            GameObject go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.position=position; return go.transform;
        }

        static BoxCollider RestrictedArea(Transform parent,string name,Vector3 position,Vector3 size)
        {
            GameObject go=new GameObject(name,typeof(BoxCollider)); go.transform.SetParent(parent,false); go.transform.position=position; BoxCollider box=go.GetComponent<BoxCollider>(); box.size=size; box.isTrigger=true; return box;
        }

        void AutoWire(GameObject root)
        {
            if(root==null||root.name!="Neko Player Teleport UI"){EditorUtility.DisplayDialog("Player Teleport","Select the generated Neko Player Teleport UI root.","OK");return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorUtility.DisplayDialog("Player Teleport","Unity is still compiling/importing. Wait and try again.","OK");return;}
            Type type=FindType("NekoPlayerTeleportSystem"); if(type==null){EditorUtility.DisplayDialog("Player Teleport","NekoPlayerTeleportSystem has not compiled yet. Check Console errors and try again.","OK");return;}

            bool createdProgram; if(!EnsureUdonSharpProgramAsset(type,out createdProgram)){EditorUtility.DisplayDialog("Player Teleport","Could not find/create the UdonSharpProgramAsset. Run VRChat SDK → Udon Sharp → Refresh All UdonSharp Assets, then try again.","OK");return;}
            if(createdProgram||EditorApplication.isCompiling||EditorApplication.isUpdating){EditorUtility.DisplayDialog("Player Teleport","Created/repaired NekoPlayerTeleportSystem.asset. Let UdonSharp finish compiling, then Auto-Wire again.","OK");return;}

            Component runtime=root.GetComponent(type); if(runtime==null)runtime=AddUdonSharpComponent(root,type); else RunUdonSharpSetup(runtime);
            if(runtime==null){EditorUtility.DisplayDialog("Player Teleport","UdonSharp could not attach the runtime through its editor API.","OK");return;}

            GameObject destinationsRoot=GameObject.Find("Neko Player Teleport Destinations"); if(destinationsRoot==null||destinationsRoot.transform.childCount==0){EditorUtility.DisplayDialog("Player Teleport","The generated destination root could not be found.","OK");return;}
            int count=destinationsRoot.transform.childCount; Transform[] destinations=new Transform[count]; string[] names=new string[count];
            for(int i=0;i<count;i++){destinations[i]=destinationsRoot.transform.GetChild(i);names[i]=destinations[i].name;}
            Set(runtime,"destinations",destinations); Set(runtime,"destinationNames",names); Set(runtime,"allowRemoteTeleportRequests",false); Set(runtime,"playerArrivalOffset",new Vector3(0f,0f,-1.25f));

            GameObject restrictedRoot=GameObject.Find("Neko Player Teleport Restricted Areas");
            BoxCollider[] restrictedAreas=restrictedRoot==null?new BoxCollider[0]:restrictedRoot.GetComponentsInChildren<BoxCollider>(true); Set(runtime,"restrictedAreas",restrictedAreas);

            Set(runtime,"selectedPlayerText",Find(root,"[SelectedPlayer]").GetComponent<Text>()); Set(runtime,"selectedTargetPlayerText",Find(root,"[SelectedTargetPlayer]").GetComponent<Text>()); Set(runtime,"selectedDestinationText",Find(root,"[SelectedDestination]").GetComponent<Text>()); Set(runtime,"consentText",Find(root,"[ConsentText]").GetComponent<Text>()); Set(runtime,"statusText",Find(root,"[StatusText]").GetComponent<Text>());
            Wire(Find(root,"[PrevPlayer]").GetComponent<Button>(),runtime,"PreviousPlayer"); Wire(Find(root,"[NextPlayer]").GetComponent<Button>(),runtime,"NextPlayer"); Wire(Find(root,"[PrevTargetPlayer]").GetComponent<Button>(),runtime,"PreviousTargetPlayer"); Wire(Find(root,"[NextTargetPlayer]").GetComponent<Button>(),runtime,"NextTargetPlayer"); Wire(Find(root,"[RefreshPlayers]").GetComponent<Button>(),runtime,"RefreshPlayers");
            Wire(Find(root,"[TeleportMeToPlayer]").GetComponent<Button>(),runtime,"TeleportMeToTargetPlayer"); Wire(Find(root,"[RequestPlayerToTarget]").GetComponent<Button>(),runtime,"RequestSelectedPlayerToTargetPlayer"); Wire(Find(root,"[RequestPlayerToMe]").GetComponent<Button>(),runtime,"RequestSelectedPlayerToMe");
            Wire(Find(root,"[PrevDestination]").GetComponent<Button>(),runtime,"PreviousDestination"); Wire(Find(root,"[NextDestination]").GetComponent<Button>(),runtime,"NextDestination"); Wire(Find(root,"[TeleportMe]").GetComponent<Button>(),runtime,"TeleportMe"); Wire(Find(root,"[RequestTeleport]").GetComponent<Button>(),runtime,"RequestSelectedPlayerTeleport"); Wire(Find(root,"[ConsentButton]").GetComponent<Button>(),runtime,"ToggleAllowTeleportRequests");
            ApplyUdonSharpProxy(runtime); EditorUtility.SetDirty(runtime);
            EditorUtility.DisplayDialog("Player Teleport","Auto-wired player→player, player→destination and restricted areas. Remote consent remains OFF by default on each player.","OK");
        }

        static bool EnsureUdonSharpProgramAsset(Type runtimeType,out bool created)
        {
            created=false; Type programType=FindType("UdonSharp.UdonSharpProgramAsset"); if(programType==null)return false;
            MethodInfo getProgram=programType.GetMethod("GetProgramAssetForClass",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(Type)},null);
            if(getProgram!=null){try{if(getProgram.Invoke(null,new object[]{runtimeType})!=null)return true;}catch{}}
            MonoScript source=AssetDatabase.LoadAssetAtPath<MonoScript>(GeneratedScriptPath); if(source==null)return false;
            UnityEngine.Object existing=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GeneratedProgramPath);
            FieldInfo sourceField=programType.GetField("sourceCsScript",BindingFlags.Public|BindingFlags.Instance); if(sourceField==null)return false;
            if(existing==null){ScriptableObject program=ScriptableObject.CreateInstance(programType);sourceField.SetValue(program,source);AssetDatabase.CreateAsset(program,GeneratedProgramPath);EditorUtility.SetDirty(program);AssetDatabase.SaveAssets();created=true;}
            else if(sourceField.GetValue(existing)==null){sourceField.SetValue(existing,source);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssets();created=true;}
            TryCompileUdonSharp(programType); AssetDatabase.ImportAsset(GeneratedProgramPath,ImportAssetOptions.ForceUpdate); return true;
        }

        static void TryCompileUdonSharp(Type programType)
        {
            try{MethodInfo compile=programType.GetMethod("CompileAllCsPrograms",BindingFlags.Public|BindingFlags.Static);if(compile==null)return;ParameterInfo[]p=compile.GetParameters();if(p.Length==2)compile.Invoke(null,new object[]{true,true});else if(p.Length==1)compile.Invoke(null,new object[]{true});else compile.Invoke(null,null);}catch(Exception e){Debug.LogWarning("[NekoSune Player Tools] UdonSharp compile request deferred: "+e.Message);}
        }

        static Component AddUdonSharpComponent(GameObject root,Type runtimeType)
        {
            Type ext=FindType("UdonSharpEditor.UdonSharpComponentExtensions");if(ext==null)return null;MethodInfo add=ext.GetMethod("AddUdonSharpComponent",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(GameObject),typeof(Type)},null);if(add==null)return null;try{return add.Invoke(null,new object[]{root,runtimeType}) as Component;}catch(Exception e){Debug.LogError("[NekoSune Player Tools] UdonSharp AddComponent failed: "+e.Message);return null;}
        }

        static void RunUdonSharpSetup(Component component){InvokeUdonSharpUtility("RunBehaviourSetup",component);}
        static void ApplyUdonSharpProxy(Component component){InvokeUdonSharpUtility("CopyProxyToUdon",component);}
        static void InvokeUdonSharpUtility(string methodName,Component component){Type utility=FindType("UdonSharpEditor.UdonSharpEditorUtility");if(utility==null||component==null)return;MethodInfo[]ms=utility.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);for(int i=0;i<ms.Length;i++){MethodInfo m=ms[i];if(m.Name!=methodName)continue;ParameterInfo[]p=m.GetParameters();if(p.Length==1&&p[0].ParameterType.IsAssignableFrom(component.GetType())){try{m.Invoke(null,new object[]{component});}catch{}return;}}}

        static GameObject Panel(Transform parent,string name,Vector2 min,Vector2 max){GameObject go=Ui(name,parent,typeof(Image));SetRect(go.GetComponent<RectTransform>(),min,max);go.GetComponent<Image>().color=new Color(.055f,.067f,.105f,1f);return go;}
        static GameObject Ui(string name,Transform parent,params Type[] components){Type[] all=new Type[components.Length+1];all[0]=typeof(RectTransform);Array.Copy(components,0,all,1,components.Length);GameObject go=new GameObject(name,all);go.transform.SetParent(parent,false);return go;}
        static Text TextNode(Transform parent,string name,string value,int size,FontStyle style,TextAnchor align){Text t=Ui(name,parent,typeof(Text)).GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=size;t.fontStyle=style;t.alignment=align;t.color=Color.white;return t;}
        static Button ButtonNode(Transform parent,string name,string label,Vector2 min,Vector2 max){GameObject go=Ui(name,parent,typeof(Image),typeof(Button));SetRect(go.GetComponent<RectTransform>(),min,max);go.GetComponent<Image>().color=new Color(.29f,.56f,.98f,1f);Text t=TextNode(go.transform,"Label",label,15,FontStyle.Bold,TextAnchor.MiddleCenter);Stretch(t.rectTransform,6f);return go.GetComponent<Button>();}
        static void SetRect(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
        static void Stretch(RectTransform r,float p){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(p,p);r.offsetMax=new Vector2(-p,-p);}
        static GameObject Find(GameObject root,string name){Transform[] all=root.GetComponentsInChildren<Transform>(true);for(int i=0;i<all.Length;i++)if(all[i].name==name)return all[i].gameObject;return null;}
        static Type FindType(string simpleOrFull){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){Type direct=a.GetType(simpleOrFull,false);if(direct!=null)return direct;Type[] ts;try{ts=a.GetTypes();}catch{continue;}for(int i=0;i<ts.Length;i++)if(ts[i]!=null&&(ts[i].Name==simpleOrFull||ts[i].FullName==simpleOrFull))return ts[i];}return null;}
        static void Set(Component c,string field,object value){FieldInfo f=c.GetType().GetField(field,BindingFlags.Public|BindingFlags.Instance);if(f!=null){Undo.RecordObject(c,"Wire player teleport");f.SetValue(c,value);EditorUtility.SetDirty(c);}}
        static void Wire(Button b,Component c,string method){if(b==null||c==null)return;MethodInfo m=c.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance);if(m==null)return;try{UnityAction a=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),c,m);UnityEventTools.AddPersistentListener(b.onClick,a);EditorUtility.SetDirty(b);}catch(Exception e){Debug.LogWarning("[NekoSune Player Tools] Could not wire "+method+": "+e.Message);}}
        static void TryAddVrcUiShape(GameObject root){Type t=FindType("VRC.SDK3.Components.VRCUiShape");if(t==null)t=FindType("VRCUiShape");if(t==null)t=FindType("VRC_UiShape");if(t!=null&&root.GetComponent(t)==null)Undo.AddComponent(root,t);}
        static void CopyRuntime(){string src=Path.GetFullPath(Path.Combine("Packages/com.nekosune.world-player-tools/Templates/Runtime/NekoPlayerTeleportSystem.cs.txt"));EnsureFolder(GeneratedFolder);string dst=Path.Combine(Directory.GetParent(Application.dataPath).FullName,GeneratedScriptPath.Replace('/',Path.DirectorySeparatorChar));File.Copy(src,dst,true);AssetDatabase.Refresh();}
        static string EnsureFolder(string path){string[]p=path.Split('/');string cur=p[0];for(int i=1;i<p.Length;i++){string n=cur+"/"+p[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(cur,p[i]);cur=n;}return cur;}
    }
}
