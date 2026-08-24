using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 15)]
    internal sealed class NekoAnimationToolsAvatarAddon : INekoAddon
    {
        public string Id { get { return "animation-tools"; } }
        public string TitleKey { get { return "Animation Tools"; } }
        public string DescriptionKey { get { return "Beat/drop mapping, waveform keyframing, particles/shaders/humanoid helpers and timed lyrics."; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "♫"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoSune.AnimationTools.Editor.NekoAnimationToolsWindow.Open(); }
    }
}

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 15)]
    internal sealed class NekoAnimationToolsWorldAddon : INekoAddon
    {
        public string Id { get { return "animation-tools-world"; } }
        public string TitleKey { get { return "Animation Tools"; } }
        public string DescriptionKey { get { return "Beat/drop animation authoring and world-friendly timed 3D lyric tracks."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "♫"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoSune.AnimationTools.Editor.NekoAnimationToolsWindow.Open(); }
    }
}

namespace NekoSune.AnimationTools.Editor
{
    internal enum NekoMusicPreset { Hardstyle, Uptempo, Frenchcore, Custom }
    internal enum NekoBeatKind { Beat, Kick, Drop }

    [Serializable]
    internal sealed class NekoBeatMarker
    {
        public float time;
        public float strength;
        public NekoBeatKind kind;
    }

    internal static class NekoBeatAnalyzer
    {
        public static List<NekoBeatMarker> Analyze(AudioClip clip, NekoMusicPreset preset, float customBpm, float sensitivity)
        {
            var result = new List<NekoBeatMarker>();
            if (clip == null || clip.samples <= 0 || clip.channels <= 0) return result;

            float bpm = preset == NekoMusicPreset.Hardstyle ? 150f : preset == NekoMusicPreset.Uptempo ? 200f : preset == NekoMusicPreset.Frenchcore ? 200f : Mathf.Clamp(customBpm, 60f, 260f);
            float threshold = preset == NekoMusicPreset.Uptempo ? 1.16f : preset == NekoMusicPreset.Frenchcore ? 1.19f : 1.22f;
            threshold = Mathf.Lerp(1.42f, 1.08f, Mathf.Clamp01(sensitivity));
            float minSpacing = Mathf.Max(.07f, (60f / bpm) * (preset == NekoMusicPreset.Uptempo ? .42f : .48f));

            int channels = clip.channels;
            int total = clip.samples * channels;
            float[] samples = new float[total];
            try
            {
                if (!clip.GetData(samples, 0)) return result;
            }
            catch { return result; }

            int hop = 1024;
            int frames = Mathf.Max(1, clip.samples / hop);
            float[] energy = new float[frames];
            float[] bass = new float[frames];
            float low = 0f;
            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * 170f / Mathf.Max(8000f, clip.frequency));

            for (int f = 0; f < frames; f++)
            {
                int startSample = f * hop;
                double e = 0.0, b = 0.0;
                int count = 0;
                int end = Mathf.Min(clip.samples, startSample + hop);
                for (int s = startSample; s < end; s++)
                {
                    float mono = 0f;
                    int baseIndex = s * channels;
                    for (int c = 0; c < channels; c++) mono += samples[baseIndex + c];
                    mono /= channels;
                    low += alpha * (mono - low);
                    e += mono * mono;
                    b += low * low;
                    count++;
                }
                if (count > 0)
                {
                    energy[f] = Mathf.Sqrt((float)(e / count));
                    bass[f] = Mathf.Sqrt((float)(b / count));
                }
            }

            float lastBeat = -10f;
            float lastDrop = -10f;
            for (int f = 2; f < frames - 2; f++)
            {
                int from = Mathf.Max(0, f - 12);
                float avg = 0f, avgBass = 0f;
                int n = 0;
                for (int p = from; p < f; p++) { avg += energy[p]; avgBass += bass[p]; n++; }
                if (n == 0) continue;
                avg /= n; avgBass /= n;
                float ratio = energy[f] / Mathf.Max(.0001f, avg);
                float bassRatio = bass[f] / Mathf.Max(.0001f, avgBass);
                float time = (f * hop) / (float)clip.frequency;
                bool localPeak = energy[f] >= energy[f - 1] && energy[f] >= energy[f + 1];
                if (!localPeak || ratio < threshold || time - lastBeat < minSpacing) continue;

                NekoBeatKind kind = bassRatio > 1.20f ? NekoBeatKind.Kick : NekoBeatKind.Beat;
                float strength = Mathf.Clamp01((ratio - 1f) / 1.2f);
                if (ratio > threshold + .42f && bassRatio > 1.18f && time - lastDrop > 1.5f)
                {
                    kind = NekoBeatKind.Drop;
                    lastDrop = time;
                    strength = 1f;
                }
                result.Add(new NekoBeatMarker { time = time, strength = strength, kind = kind });
                lastBeat = time;
            }

            // If a very clean/mastered track produced too few onset peaks, provide a BPM grid rather than an empty tool.
            if (result.Count < 4)
            {
                result.Clear();
                float step = 60f / bpm;
                for (float t = 0f; t < clip.length; t += step)
                    result.Add(new NekoBeatMarker { time = t, strength = .65f, kind = NekoBeatKind.Beat });
            }
            return result;
        }
    }

    internal sealed class NekoLyricLine
    {
        public float time;
        public string text;
    }

    internal sealed class NekoAnimationToolsWindow : EditorWindow
    {
        [SerializeField] AudioClip _audio;
        [SerializeField] GameObject _root;
        [SerializeField] GameObject _target;
        [SerializeField] string _outputFolder = "Assets/NekoSune/AnimationTools";

        NekoMusicPreset _preset = NekoMusicPreset.Hardstyle;
        float _customBpm = 150f;
        float _sensitivity = .62f;
        float _attack = .035f;
        float _decay = .11f;
        float _baseValue;
        float _peakValue = 1f;
        bool _useBeat = true, _useKick = true, _useDrop = true;
        int _tab;
        int _bindingIndex;
        EditorCurveBinding[] _bindings = new EditorCurveBinding[0];
        string[] _bindingNames = new string[0];
        List<NekoBeatMarker> _markers = new List<NekoBeatMarker>();
        float[] _waveform;
        float _scrub;
        Vector2 _scroll;
        string _status = "Assign an AudioClip, then Analyze.";
        string _lyrics = "[00:00.000]First lyric line\n[00:05.000]Second lyric line";
        GameObject _lyricRoot;

        [MenuItem("NekoSune/Animation Tools", false, 4)]
        [MenuItem("NekoSune/Avatar/Animation Tools", false, 19)]
        [MenuItem("NekoSune/World/Animation Tools", false, 19)]
        public static void Open()
        {
            var w = GetWindow<NekoAnimationToolsWindow>(false, "Animation Tools", true);
            w.minSize = new Vector2(760f, 620f);
            w.Show();
        }

        void OnDisable() { NekoSune.Avatars.Editor.NekoAudioPreview.Stop(); }

        void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, new[] { "Beat Mapper", "Keyframing", "Lyrics", "Shader / FX Info" });
            EditorGUILayout.Space(6f);
            DrawCommon();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == 0) DrawBeatMapper();
            else if (_tab == 1) DrawKeyframing();
            else if (_tab == 2) DrawLyrics();
            else DrawShaderInfo();
            EditorGUILayout.EndScrollView();
        }

        void DrawCommon()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _audio = (AudioClip)EditorGUILayout.ObjectField("Audio", _audio, typeof(AudioClip), false);
            _root = (GameObject)EditorGUILayout.ObjectField("Animation root / avatar / world object", _root, typeof(GameObject), true);
            _outputFolder = EditorGUILayout.TextField("Output folder", _outputFolder);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Analyze Audio", GUILayout.Height(28f))) Analyze();
            if (GUILayout.Button("Play from scrub", GUILayout.Height(28f)) && _audio != null) NekoSune.Avatars.Editor.NekoAudioPreview.PlayAt(_audio, _scrub);
            if (GUILayout.Button("Stop", GUILayout.Height(28f))) NekoSune.Avatars.Editor.NekoAudioPreview.Stop();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(_status, MessageType.None);
            EditorGUILayout.EndVertical();
            DrawTimeline();
        }

        void DrawBeatMapper()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Genre-aware kick / beat / drop mapper", EditorStyles.boldLabel);
            _preset = (NekoMusicPreset)EditorGUILayout.EnumPopup("Preset", _preset);
            if (_preset == NekoMusicPreset.Custom) _customBpm = EditorGUILayout.Slider("Reference BPM", _customBpm, 60f, 260f);
            _sensitivity = EditorGUILayout.Slider("Detection sensitivity", _sensitivity, 0f, 1f);
            EditorGUILayout.HelpBox("Hardstyle emphasizes strong low-frequency kick onsets; Uptempo uses tighter minimum spacing; Frenchcore uses a fast kick grid with drop emphasis. Detection is an editor aid, not a claim of perfect musical transcription.", MessageType.Info);
            if (GUILayout.Button("Analyze / Refresh Markers", GUILayout.Height(30f))) Analyze();
            EditorGUILayout.EndVertical();

            int beats=0,kicks=0,drops=0;
            for(int i=0;i<_markers.Count;i++){if(_markers[i].kind==NekoBeatKind.Beat)beats++;else if(_markers[i].kind==NekoBeatKind.Kick)kicks++;else drops++;}
            EditorGUILayout.LabelField("Markers", _markers.Count + " total   •   " + beats + " beats   •   " + kicks + " kicks   •   " + drops + " drops");
            int show = Mathf.Min(_markers.Count, 40);
            for (int i = 0; i < show; i++)
            {
                NekoBeatMarker m = _markers[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(m.kind.ToString(), GUILayout.Width(55f));
                GUILayout.Label(FormatTime(m.time), GUILayout.Width(88f));
                GUILayout.HorizontalSlider(m.strength, 0f, 1f);
                if (GUILayout.Button("Seek", GUILayout.Width(48f))) { _scrub=m.time; if(_audio!=null)NekoSune.Avatars.Editor.NekoAudioPreview.PlayAt(_audio,m.time); }
                EditorGUILayout.EndHorizontal();
            }
            if (_markers.Count > show) EditorGUILayout.LabelField("… " + (_markers.Count-show) + " more markers shown on the waveform.");
        }

        void DrawKeyframing()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Target hierarchy / animatable property", EditorStyles.boldLabel);
            GameObject old = _target;
            _target = (GameObject)EditorGUILayout.ObjectField("Target object", _target, typeof(GameObject), true);
            if (_target != old || GUILayout.Button("Refresh Animatable Properties")) RefreshBindings();
            if (_bindingNames.Length > 0) _bindingIndex = EditorGUILayout.Popup("Property", Mathf.Clamp(_bindingIndex,0,_bindingNames.Length-1), _bindingNames);
            else EditorGUILayout.HelpBox("Choose a target under the animation root. Unity's AnimationUtility will expose animatable Transform, Renderer/material/shader, ParticleSystem, Light and other component properties.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Speed keyframing", EditorStyles.boldLabel);
            _useBeat = EditorGUILayout.ToggleLeft("Key normal beats", _useBeat);
            _useKick = EditorGUILayout.ToggleLeft("Key bass / kicks", _useKick);
            _useDrop = EditorGUILayout.ToggleLeft("Key detected drops", _useDrop);
            _baseValue = EditorGUILayout.FloatField("Base value", _baseValue);
            _peakValue = EditorGUILayout.FloatField("Peak value", _peakValue);
            _attack = EditorGUILayout.Slider("Attack seconds", _attack, .001f, .5f);
            _decay = EditorGUILayout.Slider("Decay seconds", _decay, .005f, 1f);
            if (GUILayout.Button("AUTO KEYFRAME SELECTED PROPERTY", GUILayout.Height(34f))) BuildAutoClip();
            if (GUILayout.Button("MANUAL: CREATE EMPTY .ANIM + TIMING GUIDE", GUILayout.Height(30f))) BuildManualGuide();
            EditorGUILayout.HelpBox("Auto mode writes one .anim curve with attack → hit → decay pulses at detected markers. Manual mode deliberately does not key the target; it creates an empty clip and a readable marker guide for creators who prefer hand keyframing.", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Humanoid / particles / shaders", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Humanoid: choose the Hips, Chest, Head, arm, etc. GameObject from the hierarchy, then choose a local position/rotation animatable property.\n\nParticles: choose the ParticleSystem GameObject and select the exposed module property.\n\nShaders: choose the Renderer GameObject and select a material.* property Unity exposes. This avoids locking the tool to one third-party shader version.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        void DrawLyrics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Timed lyrics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Paste LRC-style [mm:ss.xxx]Text or seconds|Text. Animation Tools uses your timestamps exactly; it does not pretend to speech-to-text the words from the audio.", EditorStyles.wordWrappedLabel);
            _lyrics = EditorGUILayout.TextArea(_lyrics, GUILayout.MinHeight(130f));
            _lyricRoot = (GameObject)EditorGUILayout.ObjectField("Existing lyric object root (avatar mesh mode)", _lyricRoot, typeof(GameObject), true);
            if (GUILayout.Button("WORLD: CREATE 3D TEXT OBJECTS + .ANIM", GUILayout.Height(32f))) BuildWorldLyrics();
            if (GUILayout.Button("AVATAR / GENERIC: ANIMATE EXISTING CHILD OBJECTS", GUILayout.Height(32f))) BuildObjectLyrics();
            if (GUILayout.Button("SHADER ATLAS: KEY SELECTED FLOAT PROPERTY BY LINE INDEX", GUILayout.Height(30f))) BuildShaderLyrics();
            EditorGUILayout.HelpBox("World mode creates 3D TextMesh line objects, so it is not dependent on a Canvas. For avatars, use existing mesh objects or a shader/text-atlas material and animate those instead; NekoSune does not rely on runtime avatar UI/Text components being accepted by VRChat.", MessageType.Warning);
            EditorGUILayout.EndVertical();
        }

        void DrawShaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Third-party shader / effect information", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("NekoSune Animation Tools does NOT include or redistribute paid/community shader packages. Obtain them from the shader creator's official source and follow that creator's licence/terms. Once installed, use the animatable-property picker instead of a hard-coded NekoSune preset.", MessageType.Warning);

            GUILayout.Label("Doppelgänger / Dope Shader", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Screen-space / post-processing style effects commonly used for music animations. Current distribution/tiers are handled by Doppelgänger through their Patreon/Discord; NekoSune does not supply the shader files.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Open official Doppelgänger Patreon")) Application.OpenURL("https://www.patreon.com/dopestuff");

            GUILayout.Space(6f);
            GUILayout.Label("Leviant ScreenSpace Ubershader", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Leviant's public ScreenSpace Ubershader repository contains zoom, shake, blur, chromatic, glitch, color and other properties. Respect the repository licence and creator distribution channels.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Open Leviant official GitHub")) Application.OpenURL("https://github.com/Leviant/ScreenSpace_Ubershader");

            GUILayout.Space(6f);
            GUILayout.Label("Other shaders / particles", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Poiyomi, creator-specific shaders, particle packages and other effects can be used when Unity exposes their properties to AnimationUtility. Animation Tools intentionally discovers the installed material/component properties so creators can use legally obtained assets without NekoSune bundling them.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        void Analyze()
        {
            if (_audio == null) { _status = "Assign an AudioClip first."; return; }
            _markers = NekoBeatAnalyzer.Analyze(_audio,_preset,_customBpm,_sensitivity);
            BuildWaveform();
            _status = _markers.Count + " markers detected for " + _preset + ".";
            Repaint();
        }

        void BuildWaveform()
        {
            _waveform = null;
            if (_audio == null) return;
            int width = 700;
            float[] data = new float[_audio.samples * _audio.channels];
            try { if (!_audio.GetData(data,0)) return; } catch { return; }
            _waveform = new float[width];
            int step = Mathf.Max(1,_audio.samples/width);
            for(int x=0;x<width;x++)
            {
                int s0=x*step, s1=Mathf.Min(_audio.samples,s0+step); float peak=0f;
                for(int s=s0;s<s1;s++)
                {
                    float mono=0f; int bi=s*_audio.channels;
                    for(int c=0;c<_audio.channels;c++)mono+=Mathf.Abs(data[bi+c]);
                    mono/=_audio.channels; if(mono>peak)peak=mono;
                }
                _waveform[x]=peak;
            }
        }

        void DrawTimeline()
        {
            Rect r = GUILayoutUtility.GetRect(100f, 110f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(.055f,.065f,.085f,1f));
            if (_audio == null) { GUI.Label(r,"Assign an AudioClip to show waveform / beat markers.",EditorStyles.centeredGreyMiniLabel); return; }
            if (_waveform == null) BuildWaveform();
            if (_waveform != null)
            {
                Handles.BeginGUI();
                Handles.color = new Color(.35f,.68f,1f,.9f);
                for(int x=0;x<_waveform.Length;x+=2)
                {
                    float xx=r.x+(x/(float)(_waveform.Length-1))*r.width; float h=_waveform[x]*r.height*.43f;
                    Handles.DrawLine(new Vector3(xx,r.center.y-h),new Vector3(xx,r.center.y+h));
                }
                for(int i=0;i<_markers.Count;i++)
                {
                    NekoBeatMarker m=_markers[i]; float xx=r.x+(m.time/Mathf.Max(.001f,_audio.length))*r.width;
                    Handles.color=m.kind==NekoBeatKind.Drop?new Color(1f,.28f,.55f):m.kind==NekoBeatKind.Kick?new Color(1f,.72f,.18f):new Color(.45f,1f,.62f);
                    Handles.DrawLine(new Vector3(xx,r.y),new Vector3(xx,r.yMax));
                }
                float sx=r.x+(_scrub/Mathf.Max(.001f,_audio.length))*r.width; Handles.color=Color.white; Handles.DrawLine(new Vector3(sx,r.y),new Vector3(sx,r.yMax));
                Handles.EndGUI();
            }
            Event e=Event.current;
            if((e.type==EventType.MouseDown||e.type==EventType.MouseDrag)&&r.Contains(e.mousePosition))
            {
                _scrub=Mathf.Clamp01((e.mousePosition.x-r.x)/r.width)*_audio.length;
                if(e.type==EventType.MouseDown&&e.button==0)NekoSune.Avatars.Editor.NekoAudioPreview.PlayAt(_audio,_scrub);
                e.Use(); Repaint();
            }
            GUILayout.Label("Scrub: "+FormatTime(_scrub)+" / "+FormatTime(_audio.length)+"   •   green=beat   amber=kick   pink=drop",EditorStyles.miniLabel);
        }

        void RefreshBindings()
        {
            _bindings = new EditorCurveBinding[0]; _bindingNames = new string[0]; _bindingIndex=0;
            if(_root==null||_target==null)return;
            try{_bindings=AnimationUtility.GetAnimatableBindings(_target,_root);}catch{return;}
            var names=new List<string>(); var usable=new List<EditorCurveBinding>();
            for(int i=0;i<_bindings.Length;i++)
            {
                EditorCurveBinding b=_bindings[i]; if(b.isPPtrCurve)continue;
                usable.Add(b); names.Add(b.type.Name+"  •  "+b.propertyName+"  ["+b.path+"]");
            }
            _bindings=usable.ToArray(); _bindingNames=names.ToArray();
        }

        bool MarkerEnabled(NekoBeatMarker m){return m.kind==NekoBeatKind.Beat?_useBeat:m.kind==NekoBeatKind.Kick?_useKick:_useDrop;}

        void BuildAutoClip()
        {
            if(_audio==null||_root==null||_target==null){_status="Assign Audio, animation root and target.";return;}
            if(_markers.Count==0)Analyze();
            if(_bindings.Length==0)RefreshBindings();
            if(_bindings.Length==0){_status="No float animatable properties found on target.";return;}
            EditorCurveBinding binding=_bindings[Mathf.Clamp(_bindingIndex,0,_bindings.Length-1)];
            AnimationClip clip=new AnimationClip(); clip.name=_audio.name+"_NekoBeatFX"; clip.frameRate=60f;
            var keys=new List<Keyframe>(); keys.Add(new Keyframe(0f,_baseValue));
            for(int i=0;i<_markers.Count;i++)
            {
                NekoBeatMarker m=_markers[i]; if(!MarkerEnabled(m))continue;
                float peak=Mathf.Lerp(_baseValue,_peakValue,Mathf.Clamp01(.45f+m.strength*.55f));
                keys.Add(new Keyframe(Mathf.Max(0f,m.time-_attack),_baseValue));
                keys.Add(new Keyframe(m.time,peak));
                keys.Add(new Keyframe(Mathf.Min(_audio.length,m.time+_decay),_baseValue));
            }
            keys.Sort((a,b)=>a.time.CompareTo(b.time));
            AnimationCurve curve=new AnimationCurve(keys.ToArray());
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
            AnimationUtility.SetEditorCurve(clip,binding,curve);
            string path=SaveClip(clip,_audio.name+"_BeatFX.anim");
            _status="Generated auto-keyframed clip: "+path;
        }

        void BuildManualGuide()
        {
            if(_audio==null){_status="Assign Audio first.";return;}
            if(_markers.Count==0)Analyze();
            AnimationClip clip=new AnimationClip(); clip.name=_audio.name+"_Manual"; clip.frameRate=60f;
            string clipPath=SaveClip(clip,_audio.name+"_Manual.anim");
            string folder=Path.GetDirectoryName(clipPath).Replace('\\','/'); string guide=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+_audio.name+"_BeatGuide.txt");
            var lines=new List<string>(); lines.Add("NekoSune Animation Tools manual timing guide"); lines.Add("Audio: "+_audio.name); lines.Add("Preset: "+_preset); lines.Add("");
            for(int i=0;i<_markers.Count;i++)lines.Add(FormatTime(_markers[i].time)+"\t"+_markers[i].kind+"\tstrength="+_markers[i].strength.ToString("0.00",CultureInfo.InvariantCulture));
            File.WriteAllLines(ToAbsolute(guide),lines.ToArray()); AssetDatabase.ImportAsset(guide); AssetDatabase.Refresh();
            _status="Created empty .anim and timing guide for manual keyframing.";
        }

        List<NekoLyricLine> ParseLyrics()
        {
            var result=new List<NekoLyricLine>();
            string[] lines=(_lyrics??"").Replace("\r","").Split('\n');
            for(int i=0;i<lines.Length;i++)
            {
                string line=lines[i].Trim(); if(string.IsNullOrEmpty(line))continue;
                float time; string text;
                if(line.StartsWith("[")&&line.Contains("]"))
                {
                    int end=line.IndexOf(']'); if(!TryParseTime(line.Substring(1,end-1),out time))continue; text=line.Substring(end+1).Trim();
                }
                else
                {
                    int bar=line.IndexOf('|'); if(bar<=0||!float.TryParse(line.Substring(0,bar),NumberStyles.Float,CultureInfo.InvariantCulture,out time))continue; text=line.Substring(bar+1).Trim();
                }
                result.Add(new NekoLyricLine{time=Mathf.Max(0f,time),text=text});
            }
            result.Sort((a,b)=>a.time.CompareTo(b.time)); return result;
        }

        static bool TryParseTime(string value,out float time)
        {
            time=0f; string[] p=value.Split(':');
            if(p.Length==1)return float.TryParse(p[0],NumberStyles.Float,CultureInfo.InvariantCulture,out time);
            float min,sec;if(!float.TryParse(p[0],NumberStyles.Float,CultureInfo.InvariantCulture,out min)||!float.TryParse(p[1],NumberStyles.Float,CultureInfo.InvariantCulture,out sec))return false;time=min*60f+sec;return true;
        }

        void BuildWorldLyrics()
        {
            List<NekoLyricLine> lines=ParseLyrics(); if(lines.Count==0){_status="No valid timed lyric lines.";return;}
            GameObject root=new GameObject("Neko 3D Lyrics"); Undo.RegisterCreatedObjectUndo(root,"Create 3D lyrics");
            for(int i=0;i<lines.Count;i++)
            {
                GameObject go=new GameObject("Lyric "+(i+1)); go.transform.SetParent(root.transform,false); TextMesh tm=go.AddComponent<TextMesh>(); tm.text=lines[i].text; tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center; tm.characterSize=.05f; tm.fontSize=64; go.SetActive(false);
            }
            _lyricRoot=root; Selection.activeGameObject=root; BuildObjectLyricClip(lines,root);
            _status="Created world 3D TextMesh lyric objects and timing .anim. Move/style the root before publishing.";
        }

        void BuildObjectLyrics()
        {
            List<NekoLyricLine> lines=ParseLyrics(); if(lines.Count==0||_lyricRoot==null){_status="Provide valid lyrics and an existing lyric root.";return;}
            if(_lyricRoot.transform.childCount<lines.Count){_status="Lyric root needs at least "+lines.Count+" child objects (one mesh/object per line).";return;}
            BuildObjectLyricClip(lines,_lyricRoot); _status="Generated exact-timestamp child-object lyric animation.";
        }

        void BuildObjectLyricClip(List<NekoLyricLine> lines,GameObject root)
        {
            AnimationClip clip=new AnimationClip();clip.name="NekoTimedLyrics";clip.frameRate=60f;float frame=1f/60f;
            for(int i=0;i<lines.Count;i++)
            {
                Transform child=root.transform.GetChild(i); string path=AnimationUtility.CalculateTransformPath(child,root.transform); float start=lines[i].time; float end=i+1<lines.Count?Mathf.Max(start+frame,lines[i+1].time-frame):(_audio!=null?Mathf.Max(start+frame,_audio.length):start+3f);
                EditorCurveBinding binding=EditorCurveBinding.FloatCurve(path,typeof(GameObject),"m_IsActive"); AnimationCurve curve=new AnimationCurve(); curve.AddKey(Mathf.Max(0f,start-frame),0f); curve.AddKey(start,1f); curve.AddKey(end,1f); curve.AddKey(end+frame,0f); AnimationUtility.SetEditorCurve(clip,binding,curve);
            }
            SaveClip(clip,"NekoTimedLyrics.anim");
        }

        void BuildShaderLyrics()
        {
            List<NekoLyricLine> lines=ParseLyrics(); if(lines.Count==0){_status="No valid timed lyric lines.";return;}
            if(_bindings.Length==0)RefreshBindings(); if(_bindings.Length==0){_status="Select a target and an animatable float property such as a shader lyric-index property.";return;}
            EditorCurveBinding binding=_bindings[Mathf.Clamp(_bindingIndex,0,_bindings.Length-1)]; AnimationClip clip=new AnimationClip();clip.name="NekoLyricIndex";clip.frameRate=60f; AnimationCurve curve=new AnimationCurve();
            for(int i=0;i<lines.Count;i++){Keyframe k=new Keyframe(lines[i].time,i);curve.AddKey(k);int idx=curve.length-1;AnimationUtility.SetKeyLeftTangentMode(curve,idx,AnimationUtility.TangentMode.Constant);AnimationUtility.SetKeyRightTangentMode(curve,idx,AnimationUtility.TangentMode.Constant);}
            AnimationUtility.SetEditorCurve(clip,binding,curve);SaveClip(clip,"NekoLyricIndex.anim");_status="Generated stepped lyric-index curve. Use it with a legally obtained shader/material atlas that exposes the selected float property.";
        }

        string SaveClip(AnimationClip clip,string fileName)
        {
            EnsureFolder(_outputFolder); string path=AssetDatabase.GenerateUniqueAssetPath(_outputFolder.TrimEnd('/')+"/"+Sanitize(fileName)); AssetDatabase.CreateAsset(clip,path); AssetDatabase.SaveAssets(); Selection.activeObject=clip; return path;
        }

        static string Sanitize(string value){foreach(char c in Path.GetInvalidFileNameChars())value=value.Replace(c,'_');return value;}
        static void EnsureFolder(string path){path=path.Replace('\\','/').TrimEnd('/');string[]p=path.Split('/');if(p.Length==0||p[0]!="Assets")return;string cur="Assets";for(int i=1;i<p.Length;i++){string n=cur+"/"+p[i];if(!AssetDatabase.IsValidFolder(n))AssetDatabase.CreateFolder(cur,p[i]);cur=n;}}
        static string ToAbsolute(string assetPath){return Path.Combine(Directory.GetParent(Application.dataPath).FullName,assetPath.Replace('/',Path.DirectorySeparatorChar));}
        static string FormatTime(float t){int m=Mathf.FloorToInt(t/60f);float s=t-m*60f;return m.ToString("00")+":"+s.ToString("00.000",CultureInfo.InvariantCulture);}
    }
}
