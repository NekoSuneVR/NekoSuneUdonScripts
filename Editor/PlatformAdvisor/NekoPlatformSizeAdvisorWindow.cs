using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 22)]
    internal sealed class NekoAvatarSizeAdvisorAddon : INekoAddon
    {
        public string Id { get { return "avatar-size-advisor"; } }
        public string TitleKey { get { return "Build Size Advisor"; } }
        public string DescriptionKey { get { return "Check PC/mobile compressed and uncompressed avatar build sizes against upload limits."; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "MB"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoSune.Optimizer.Editor.NekoPlatformSizeAdvisorWindow.OpenAvatar(); }
    }
}

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 13)]
    internal sealed class NekoWorldPlatformAdvisorAddon : INekoAddon
    {
        public string Id { get { return "world-platform-advisor"; } }
        public string TitleKey { get { return "World Platform Advisor"; } }
        public string DescriptionKey { get { return "Review PC/Android world scene metrics and last-build size guidance."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "MB"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoSune.Optimizer.Editor.NekoPlatformSizeAdvisorWindow.OpenWorld(); }
    }
}

namespace NekoSune.Optimizer.Editor
{
    internal sealed class NekoPlatformSizeAdvisorWindow : EditorWindow
    {
        enum ContentKind { Avatar, World }
        enum Platform { PC, AndroidMobile }

        ContentKind _kind;
        Platform _platform;
        float _downloadMb;
        float _uncompressedMb;
        bool _foundDownload;
        bool _foundUncompressed;
        string _logStatus = "No build log scanned yet.";
        Vector2 _scroll;

        int _objects,_renderers,_triangles,_materials,_textures,_lights,_particles,_audio;
        long _textureBytes;

        [MenuItem("NekoSune/Avatar/Build Size Advisor", false, 22)]
        public static void OpenAvatar(){var w=GetWindow<NekoPlatformSizeAdvisorWindow>(false,"Build Size Advisor",true);w._kind=ContentKind.Avatar;w.minSize=new Vector2(650f,520f);w.Show();}
        [MenuItem("NekoSune/World/Platform Advisor", false, 13)]
        public static void OpenWorld(){var w=GetWindow<NekoPlatformSizeAdvisorWindow>(false,"World Platform Advisor",true);w._kind=ContentKind.World;w.minSize=new Vector2(650f,560f);w.ScanWorld();w.Show();}

        void OnGUI()
        {
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(_kind==ContentKind.Avatar?"Avatar Performance + Build Size":"World PC / Mobile Platform Advisor",EditorStyles.boldLabel);
            _platform=(Platform)EditorGUILayout.EnumPopup("Platform",_platform);
            EditorGUILayout.HelpBox(_kind==ContentKind.Avatar?"Avatar Rank Advisor remains the official VRChat component-rank view. This companion panel focuses on the built asset-bundle download/uncompressed limits.":"VRChat does not publish an official World Performance Rank equivalent. This panel reports scene/build risks without pretending its score is an official VRChat rank.",MessageType.Info);
            EditorGUILayout.EndVertical();

            DrawBuildSize();
            if(_kind==ContentKind.World)DrawWorldMetrics();
            else DrawAvatarLimitsNote();
            EditorGUILayout.EndScrollView();
        }

        void DrawBuildSize()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Last built content size",EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Read latest sizes from Editor.log"))ReadLog();
            if(_kind==ContentKind.World&&GUILayout.Button("Rescan scene"))ScanWorld();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(_logStatus,MessageType.None);
            _downloadMb=EditorGUILayout.FloatField("Download / compressed MB",_downloadMb);
            _uncompressedMb=EditorGUILayout.FloatField("Uncompressed MB",_uncompressedMb);

            if(_kind==ContentKind.Avatar)
            {
                float dlLimit=_platform==Platform.PC?200f:10f;
                float rawLimit=_platform==Platform.PC?500f:40f;
                DrawLimit("Download",_downloadMb,dlLimit,true);
                DrawLimit("Uncompressed",_uncompressedMb,rawLimit,true);
                if(_downloadMb>dlLimit||_uncompressedMb>rawLimit)
                    EditorGUILayout.HelpBox("UPLOAD BLOCKER: this avatar exceeds a VRChat size limit for the selected platform. Optimize/remove features and rebuild before publishing. Build & Test can still run because those size limits are not enforced there.",MessageType.Error);
            }
            else
            {
                if(_platform==Platform.AndroidMobile)
                {
                    DrawLimit("Compressed Android world",_downloadMb,100f,true);
                    if(_downloadMb>100f)EditorGUILayout.HelpBox("UPLOAD BLOCKER: Android worlds cannot exceed 100 MB after build-time compression.",MessageType.Error);
                    EditorGUILayout.LabelField("Uncompressed",_uncompressedMb.ToString("0.0")+" MB (informational; no public hard uncompressed World cap is asserted here)");
                }
                else
                {
                    DrawLimit("PC world size guidance",_downloadMb,200f,false);
                    EditorGUILayout.LabelField("Uncompressed",_uncompressedMb.ToString("0.0")+" MB (informational)");
                    if(_downloadMb>200f)EditorGUILayout.HelpBox("VRChat's public-world guidance says creators may be asked to reduce very large worlds and recommends trying to keep worlds under about 200 MB. This is guidance, not an official World Performance Rank.",MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();
        }

        static void DrawLimit(string label,float value,float limit,bool hard)
        {
            float ratio=limit<=0?0:value/limit;
            Rect r=EditorGUILayout.GetControlRect();EditorGUI.ProgressBar(r,Mathf.Clamp01(ratio),label+": "+value.ToString("0.0")+" / "+limit.ToString("0")+" MB"+(hard?" hard limit":" guidance"));
        }

        void DrawAvatarLimitsNote()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Current VRChat avatar size caps",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("PC", "200 MB download / 500 MB uncompressed");
            EditorGUILayout.LabelField("Android / Quest / mobile", "10 MB download / 40 MB uncompressed");
            EditorGUILayout.HelpBox("Use NekoSune → Avatar → Rank Advisor for official VRChat component Performance Rank analysis. Size limits and Performance Rank are separate checks.",MessageType.None);
            EditorGUILayout.EndVertical();
        }

        void DrawWorldMetrics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Scene platform snapshot",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("GameObjects",_objects.ToString("N0"));
            EditorGUILayout.LabelField("Renderers",_renderers.ToString("N0"));
            EditorGUILayout.LabelField("Triangles ~",_triangles.ToString("N0"));
            EditorGUILayout.LabelField("Material slots",_materials.ToString("N0"));
            EditorGUILayout.LabelField("Unique textures",_textures.ToString("N0"));
            EditorGUILayout.LabelField("Texture memory estimate",(_textureBytes/1048576f).ToString("0.0")+" MB");
            EditorGUILayout.LabelField("Lights",_lights.ToString("N0"));
            EditorGUILayout.LabelField("ParticleSystems",_particles.ToString("N0"));
            EditorGUILayout.LabelField("AudioSources",_audio.ToString("N0"));
            if(_platform==Platform.AndroidMobile)
            {
                if(_lights>8)EditorGUILayout.HelpBox("Mobile world has many lights. Prefer baked lighting where possible and profile the Android build.",MessageType.Warning);
                if(_textureBytes>512L*1024L*1024L)EditorGUILayout.HelpBox("Large estimated texture memory for a mobile world. Review platform overrides, atlases and texture dimensions.",MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }

        void ReadLog()
        {
            string path=Application.consoleLogPath;
            if(string.IsNullOrEmpty(path)||!File.Exists(path)){_logStatus="Unity Editor log path was not available. Enter last build values manually.";return;}
            string text;
            try{text=File.ReadAllText(path);}catch(Exception e){_logStatus="Could not read Editor.log: "+e.Message;return;}
            int start=Mathf.Max(0,text.Length-2000000);text=text.Substring(start);
            float dl;bool gotDl=TryFindLastSize(text,new[]{"download size","compressed size","compressed"},out dl);
            float raw;bool gotRaw=TryFindLastSize(text,new[]{"uncompressed size","uncompressed"},out raw);
            if(gotDl){_downloadMb=dl;_foundDownload=true;}if(gotRaw){_uncompressedMb=raw;_foundUncompressed=true;}
            _logStatus=(gotDl||gotRaw)?"Read latest matching build-size entries from Editor.log. Verify against the SDK build panel because log wording can change between SDK versions.":"No recognizable compressed/uncompressed build size entries were found. Build the content once, then retry or enter values manually.";
        }

        static bool TryFindLastSize(string text,string[] labels,out float mb)
        {
            mb=0f;int best=-1;float bestValue=0f;
            for(int l=0;l<labels.Length;l++)
            {
                string pattern="(?i)"+Regex.Escape(labels[l])+@"[^\r\n]{0,100}?([0-9]+(?:\.[0-9]+)?)\s*(KB|MB|GB)";
                MatchCollection ms=Regex.Matches(text,pattern);for(int i=0;i<ms.Count;i++){Match m=ms[i];if(m.Index<best)continue;float v;if(!float.TryParse(m.Groups[1].Value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v))continue;string unit=m.Groups[2].Value.ToUpperInvariant();if(unit=="KB")v/=1024f;else if(unit=="GB")v*=1024f;best=m.Index;bestValue=v;}
            }
            if(best<0)return false;mb=bestValue;return true;
        }

        void ScanWorld()
        {
            _objects=_renderers=_triangles=_materials=_textures=_lights=_particles=_audio=0;_textureBytes=0;
            var tex=new HashSet<Texture>();Scene scene=SceneManager.GetActiveScene();if(!scene.IsValid())return;GameObject[] roots=scene.GetRootGameObjects();
            for(int r=0;r<roots.Length;r++)
            {
                Transform[] ts=roots[r].GetComponentsInChildren<Transform>(true);_objects+=ts.Length;
                Renderer[] rs=roots[r].GetComponentsInChildren<Renderer>(true);_renderers+=rs.Length;
                for(int i=0;i<rs.Length;i++)
                {
                    Mesh mesh=null;SkinnedMeshRenderer sk=rs[i] as SkinnedMeshRenderer;if(sk!=null)mesh=sk.sharedMesh;else{MeshFilter mf=rs[i].GetComponent<MeshFilter>();if(mf!=null)mesh=mf.sharedMesh;}if(mesh!=null)_triangles+=(int)(mesh.GetIndexCount(0)/3);
                    Material[] mats=rs[i].sharedMaterials;_materials+=mats==null?0:mats.Length;if(mats!=null)for(int m=0;m<mats.Length;m++)if(mats[m]!=null){string[] props=AssetDatabase.GetAssetPath(mats[m]).Length>=0?mats[m].GetTexturePropertyNames():new string[0];for(int p=0;p<props.Length;p++){Texture t=mats[m].GetTexture(props[p]);if(t!=null)tex.Add(t);}}
                }
                _lights+=roots[r].GetComponentsInChildren<Light>(true).Length;_particles+=roots[r].GetComponentsInChildren<ParticleSystem>(true).Length;_audio+=roots[r].GetComponentsInChildren<AudioSource>(true).Length;
            }
            foreach(Texture t in tex){_textures++;Texture2D t2=t as Texture2D;if(t2!=null)_textureBytes+=(long)t2.width*t2.height*4L;}
        }
    }
}
