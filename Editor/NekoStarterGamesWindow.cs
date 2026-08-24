using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NekoSune.WorldStarterGames.Editor
{
    [NekoAddon(Order = 33)]
    public sealed class NekoStarterGamesAddon : INekoAddon
    {
        public string Id { get { return "world-starter-games"; } }
        public string TitleKey { get { return "Starter Game Kit"; } }
        public string DescriptionKey { get { return "Generate persistent UI mini-games: Flappy-style, clicker and idle/incremental examples that beginners can inspect and edit."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "▶"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoStarterGamesWindow.Open(); }
    }

    internal sealed class NekoStarterGamesWindow : EditorWindow
    {
        int _game;
        Vector2 _scroll;
        readonly string[] _games = { "Neko Flappy (Persistent High Score)", "Neko Clicker (Persistent Currency)", "Neko Idle (Persistent Incremental)" };

        [MenuItem("NekoSune/World/Starter Game Kit", false, 23)]
        public static void Open()
        {
            var w = GetWindow<NekoStarterGamesWindow>(false, "Starter Game Kit", true);
            w.minSize = new Vector2(720f, 560f);
            w.Show();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Starter Game Kit", "NekoSune", "Small UI games that demonstrate PlayerData persistence in a real beginner-editable project");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Choose a starter", NekoStyles.SlotName);
            _game = EditorGUILayout.Popup("Game", _game, _games);
            if (_game == 0) EditorGUILayout.LabelField("A world-space Flappy-style mini-game with UI pipes, flap input/button, persistent best score, run count and medals.", NekoStyles.WrapLabel);
            if (_game == 1) EditorGUILayout.LabelField("A clicker with persistent coins/clicks/upgrades. Demonstrates safe PlayerData reads after OnPlayerRestored.", NekoStyles.WrapLabel);
            if (_game == 2) EditorGUILayout.LabelField("An idle/incremental game with persistent coins, production rate and upgrade level. Saves periodically instead of writing PlayerData every frame.", NekoStyles.WrapLabel);
            if (GUILayout.Button("BUILD SELECTED STARTER", NekoStyles.PrimaryButton, GUILayout.Height(36f))) BuildSelected();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("After Unity compiles the generated UdonSharp", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Select the generated game root and click Auto-Wire. The tool finds the compiled starter type, adds it, fills UI references and connects the buttons.", NekoStyles.WrapLabel);
            if (GUILayout.Button("AUTO-WIRE SELECTED STARTER", NekoStyles.PrimaryButton, GUILayout.Height(34f))) AutoWire(Selection.activeGameObject);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Learn / customize", NekoStyles.SlotName);
            EditorGUILayout.LabelField("The scripts are deliberately readable. Change the gameplay values, add your own Persistence keys with World Gameplay, and use World UI Builder to make your own menus, shop pages, rules, galleries or game HUD around the starter.", NekoStyles.WrapLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Persistence Builder")) EditorApplication.ExecuteMenuItem("NekoSune/World/Gameplay");
            if (GUILayout.Button("Open World UI Builder")) EditorApplication.ExecuteMenuItem("NekoSune/World/UI Builder");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox("These are educational starter games, not finished commercial game systems. Test persistence and multiplayer behaviour with VRChat Build & Test before publishing.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        void BuildSelected()
        {
            GameObject root = _game == 0 ? BuildFlappyUi() : (_game == 1 ? BuildClickerUi() : BuildIdleUi());
            string source = _game == 0 ? "NekoFlappyStarter.cs.txt" : (_game == 1 ? "NekoClickerStarter.cs.txt" : "NekoIdleStarter.cs.txt");
            string target = _game == 0 ? "NekoFlappyStarter.cs" : (_game == 1 ? "NekoClickerStarter.cs" : "NekoIdleStarter.cs");
            CopyRuntime(source, target);
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Starter Game Kit", "Created the game UI and generated " + target + ".\n\nWait for Unity/UdonSharp to finish compiling, keep the generated game root selected, then click AUTO-WIRE SELECTED STARTER.", "OK");
        }

        GameObject BuildFlappyUi()
        {
            GameObject root = CreateCanvasRoot("Neko Starter - Flappy", 1000f, 700f);
            GameObject content = Panel(root.transform, "GameArea", new Color(0.06f,0.09f,0.14f,1f));
            Stretch(content.GetComponent<RectTransform>(), 30f);
            Text score = TextNode(content.transform, "[ScoreText]", "Score: 0", 34, TextAnchor.UpperLeft); score.rectTransform.anchorMin = new Vector2(0f,1f); score.rectTransform.anchorMax = new Vector2(0f,1f); score.rectTransform.pivot = new Vector2(0f,1f); score.rectTransform.anchoredPosition = new Vector2(20f,-20f); score.rectTransform.sizeDelta = new Vector2(260f,50f);
            Text best = TextNode(content.transform, "[BestText]", "Best: 0", 30, TextAnchor.UpperRight); best.rectTransform.anchorMin = new Vector2(1f,1f); best.rectTransform.anchorMax = new Vector2(1f,1f); best.rectTransform.pivot = new Vector2(1f,1f); best.rectTransform.anchoredPosition = new Vector2(-20f,-20f); best.rectTransform.sizeDelta = new Vector2(260f,50f);
            Text status = TextNode(content.transform, "[StatusText]", "Press START, then FLAP or Jump", 25, TextAnchor.MiddleCenter); status.rectTransform.anchorMin = new Vector2(.5f,1f); status.rectTransform.anchorMax = new Vector2(.5f,1f); status.rectTransform.pivot = new Vector2(.5f,1f); status.rectTransform.anchoredPosition = new Vector2(0,-72f); status.rectTransform.sizeDelta = new Vector2(500,50);
            GameObject bird = Panel(content.transform, "[Bird]", new Color(1f,.72f,.18f,1f)); RectTransform br = bird.GetComponent<RectTransform>(); br.anchorMin=br.anchorMax=new Vector2(.28f,.5f); br.sizeDelta=new Vector2(54,40); br.anchoredPosition=Vector2.zero;
            for(int i=0;i<2;i++) CreatePipePair(content.transform,i, i==0?260f:650f);
            Button start = ButtonNode(content.transform,"[StartButton]","START",new Vector2(-110f,-280f));
            Button flap = ButtonNode(content.transform,"[FlapButton]","FLAP",new Vector2(110f,-280f));
            return root;
        }

        void CreatePipePair(Transform parent, int index, float x)
        {
            GameObject upper=Panel(parent,"[PipeUpper"+index+"]",new Color(.18f,.72f,.34f,1f)); GameObject lower=Panel(parent,"[PipeLower"+index+"]",new Color(.18f,.72f,.34f,1f));
            RectTransform u=upper.GetComponent<RectTransform>(); RectTransform l=lower.GetComponent<RectTransform>();
            u.anchorMin=u.anchorMax=l.anchorMin=l.anchorMax=new Vector2(.5f,.5f); u.sizeDelta=l.sizeDelta=new Vector2(90f,240f); u.anchoredPosition=new Vector2(x,220f); l.anchoredPosition=new Vector2(x,-220f);
        }

        GameObject BuildClickerUi()
        {
            GameObject root=CreateCanvasRoot("Neko Starter - Clicker",900f,650f); GameObject content=VerticalContent(root.transform);
            TextNode(content.transform,"Title","NEKO CLICKER",44,TextAnchor.MiddleCenter); TextNode(content.transform,"[CoinsText]","Coins: 0",38,TextAnchor.MiddleCenter); TextNode(content.transform,"[LifetimeText]","Lifetime clicks: 0",25,TextAnchor.MiddleCenter); TextNode(content.transform,"[StatusText]","Loading persistence...",20,TextAnchor.MiddleCenter);
            ButtonNodeLayout(content.transform,"[ClickButton]","CLICK +1"); ButtonNodeLayout(content.transform,"[UpgradeButton]","BUY UPGRADE");
            return root;
        }

        GameObject BuildIdleUi()
        {
            GameObject root=CreateCanvasRoot("Neko Starter - Idle",900f,680f); GameObject content=VerticalContent(root.transform);
            TextNode(content.transform,"Title","NEKO IDLE",44,TextAnchor.MiddleCenter); TextNode(content.transform,"[CoinsText]","Coins: 0",36,TextAnchor.MiddleCenter); TextNode(content.transform,"[RateText]","Per second: 1",27,TextAnchor.MiddleCenter); TextNode(content.transform,"[LevelText]","Upgrade level: 0",25,TextAnchor.MiddleCenter); TextNode(content.transform,"[StatusText]","Loading persistence...",20,TextAnchor.MiddleCenter);
            ButtonNodeLayout(content.transform,"[UpgradeButton]","BUY PRODUCTION UPGRADE"); ButtonNodeLayout(content.transform,"[BoostButton]","BOOST +10 COINS");
            return root;
        }

        GameObject CreateCanvasRoot(string name,float width,float height)
        {
            GameObject root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(Image)); Undo.RegisterCreatedObjectUndo(root,"Create starter game");
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace; RectTransform r=root.GetComponent<RectTransform>(); r.sizeDelta=new Vector2(width,height); root.transform.localScale=Vector3.one*.001f; root.GetComponent<Image>().color=new Color(.04f,.05f,.07f,.98f); return root;
        }
        GameObject VerticalContent(Transform parent) { GameObject go=Panel(parent,"Content",new Color(0,0,0,0)); Stretch(go.GetComponent<RectTransform>(),35f); var v=go.AddComponent<VerticalLayoutGroup>(); v.spacing=16; v.padding=new RectOffset(30,30,30,30); v.childControlWidth=true; v.childForceExpandHeight=false; return go; }
        GameObject Panel(Transform parent,string name,Color color) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=color; return go; }
        Text TextNode(Transform parent,string name,string value,int size,TextAnchor anchor) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false); Text t=go.GetComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("Arial.ttf"); t.text=value; t.fontSize=size; t.alignment=anchor; t.color=Color.white; var le=go.AddComponent<LayoutElement>(); le.preferredHeight=Mathf.Max(45,size+18); return t; }
        Button ButtonNodeLayout(Transform parent,string name,string value) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=new Color(.29f,.56f,.98f,1); var le=go.AddComponent<LayoutElement>(); le.preferredHeight=72; Text t=TextNode(go.transform,"Label",value,27,TextAnchor.MiddleCenter); Stretch(t.rectTransform,8); return go.GetComponent<Button>(); }
        Button ButtonNode(Transform parent,string name,string value,Vector2 pos) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); RectTransform rt=go.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=new Vector2(.5f,0f); rt.pivot=new Vector2(.5f,0f); rt.sizeDelta=new Vector2(190,64); rt.anchoredPosition=pos; go.GetComponent<Image>().color=new Color(.29f,.56f,.98f,1); Text t=TextNode(go.transform,"Label",value,25,TextAnchor.MiddleCenter); Stretch(t.rectTransform,6); return go.GetComponent<Button>(); }
        static void Stretch(RectTransform rt,float pad){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(pad,pad);rt.offsetMax=new Vector2(-pad,-pad);}

        void AutoWire(GameObject root)
        {
            if(root==null){EditorUtility.DisplayDialog("Starter Game Kit","Select a generated Neko Starter root first.","OK");return;}
            if(root.name.Contains("Flappy")) WireFlappy(root); else if(root.name.Contains("Clicker")) WireClicker(root); else if(root.name.Contains("Idle")) WireIdle(root); else EditorUtility.DisplayDialog("Starter Game Kit","Selected object is not a recognized generated starter root.","OK");
        }

        void WireFlappy(GameObject root)
        {
            Component c=EnsureScript(root,"NekoFlappyStarter"); if(c==null)return;
            Set(c,"bird",Find(root,"[Bird]").GetComponent<RectTransform>()); Set(c,"scoreText",Find(root,"[ScoreText]").GetComponent<Text>()); Set(c,"bestText",Find(root,"[BestText]").GetComponent<Text>()); Set(c,"statusText",Find(root,"[StatusText]").GetComponent<Text>());
            SetArray(c,"upperPipes",new[]{Find(root,"[PipeUpper0]").GetComponent<RectTransform>(),Find(root,"[PipeUpper1]").GetComponent<RectTransform>()}); SetArray(c,"lowerPipes",new[]{Find(root,"[PipeLower0]").GetComponent<RectTransform>(),Find(root,"[PipeLower1]").GetComponent<RectTransform>()});
            WireButton(Find(root,"[StartButton]").GetComponent<Button>(),c,"StartGame"); WireButton(Find(root,"[FlapButton]").GetComponent<Button>(),c,"Flap"); FinishWire(root,c);
        }
        void WireClicker(GameObject root)
        {
            Component c=EnsureScript(root,"NekoClickerStarter"); if(c==null)return; Set(c,"coinsText",Find(root,"[CoinsText]").GetComponent<Text>()); Set(c,"lifetimeText",Find(root,"[LifetimeText]").GetComponent<Text>()); Set(c,"statusText",Find(root,"[StatusText]").GetComponent<Text>()); WireButton(Find(root,"[ClickButton]").GetComponent<Button>(),c,"Click"); WireButton(Find(root,"[UpgradeButton]").GetComponent<Button>(),c,"BuyUpgrade"); FinishWire(root,c);
        }
        void WireIdle(GameObject root)
        {
            Component c=EnsureScript(root,"NekoIdleStarter"); if(c==null)return; Set(c,"coinsText",Find(root,"[CoinsText]").GetComponent<Text>()); Set(c,"rateText",Find(root,"[RateText]").GetComponent<Text>()); Set(c,"levelText",Find(root,"[LevelText]").GetComponent<Text>()); Set(c,"statusText",Find(root,"[StatusText]").GetComponent<Text>()); WireButton(Find(root,"[UpgradeButton]").GetComponent<Button>(),c,"BuyUpgrade"); WireButton(Find(root,"[BoostButton]").GetComponent<Button>(),c,"Boost"); FinishWire(root,c);
        }
        Component EnsureScript(GameObject root,string typeName){Type t=FindType(typeName);if(t==null){EditorUtility.DisplayDialog("Starter Game Kit",typeName+" is not compiled yet. Wait for Unity/UdonSharp to finish compiling the generated .cs file, then try Auto-Wire again.","OK");return null;} Component c=root.GetComponent(t);return c==null?Undo.AddComponent(root,t):c;}
        static Type FindType(string simple){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){Type[] ts;try{ts=a.GetTypes();}catch{continue;}for(int i=0;i<ts.Length;i++)if(ts[i]!=null&&ts[i].Name==simple)return ts[i];}return null;}
        static GameObject Find(GameObject root,string name){Transform[] all=root.GetComponentsInChildren<Transform>(true);for(int i=0;i<all.Length;i++)if(all[i].name==name)return all[i].gameObject;return null;}
        static void Set(Component c,string field,object value){FieldInfo f=c.GetType().GetField(field,BindingFlags.Public|BindingFlags.Instance);if(f!=null){Undo.RecordObject(c,"Wire starter");f.SetValue(c,value);EditorUtility.SetDirty(c);}}
        static void SetArray(Component c,string field,RectTransform[] value){Set(c,field,value);}
        static void WireButton(Button b,Component c,string method){if(b==null||c==null)return;MethodInfo m=c.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.Instance);if(m==null)return;try{UnityAction a=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),c,m);UnityEventTools.AddPersistentListener(b.onClick,a);EditorUtility.SetDirty(b);}catch(Exception e){Debug.LogWarning("[NekoSune Starter Games] Could not wire "+method+": "+e.Message);}}
        static void FinishWire(GameObject root,Component c){EditorUtility.SetDirty(root);EditorUtility.SetDirty(c);EditorUtility.DisplayDialog("Starter Game Kit","Auto-wired "+root.name+". Test it with VRChat Build & Test before publishing.","OK");}

        static void CopyRuntime(string source,string target){string src=Path.GetFullPath(Path.Combine("Packages/com.nekosune.world-starter-games/Templates/Runtime",source));if(!File.Exists(src)){Debug.LogError("Missing starter template: "+src);return;}string folder=EnsureFolder("Assets/NekoSune/StarterGames/Generated");string dst=Path.Combine(Directory.GetParent(Application.dataPath).FullName,(folder+"/"+target).Replace('/',Path.DirectorySeparatorChar));if(File.Exists(dst)&&!EditorUtility.DisplayDialog("Overwrite generated script?",target+" already exists. Overwrite it?","Overwrite","Keep existing"))return;File.Copy(src,dst,true);AssetDatabase.Refresh();}
        static string EnsureFolder(string path){string[]p=path.Split('/');string cur=p[0];for(int i=1;i<p.Length;i++){string n=cur+"/"+p[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(cur,p[i]);cur=n;}return cur;}
    }
}
