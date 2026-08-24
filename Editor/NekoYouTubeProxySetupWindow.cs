using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEngine;

namespace NekoSune.WorldYouTubeProxy.Editor
{
    [NekoAddon(Order = 48)]
    public sealed class NekoWorldYouTubeProxyAddon : INekoAddon
    {
        public string Id { get { return "world-youtube-proxy"; } }
        public string TitleKey { get { return "YouTube Proxy"; } }
        public string DescriptionKey { get { return "Bridge the NekoSuneTools stable YouTube relay into VRChat AVPro, Unity Video and community Udon players."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "▶"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoYouTubeProxySetupWindow.Open(); }
    }

    internal sealed class NekoYouTubeProxySetupWindow : EditorWindow
    {
        const string ProxyOrigin = "https://tools.nekosunevr.co.uk";
        const string ProxyTemplate = "https://tools.nekosunevr.co.uk/v/VIDEO_ID?vrc=1";
        const string GeneratedFolder = "Assets/NekoSune/YouTubeProxy/Generated";
        const string GeneratedScriptPath = GeneratedFolder + "/NekoYouTubeProxyPlayer.cs";
        const string GeneratedProgramPath = GeneratedFolder + "/NekoYouTubeProxyPlayer.asset";
        const string RuntimeTemplatePath = "Packages/com.nekosune.world-youtube-proxy/Templates/Runtime/NekoYouTubeProxyPlayer.cs.txt";

        GameObject _target;
        Component _customPlayer;
        string _youtubeUrl = "https://www.youtube.com/watch?v=O9qAGM_JVGI";
        int _quality;
        bool _playOnStart = true;
        bool _syncUrl = true;
        string _customUrlVariable = "url";
        string _customPlayEvent = "Play";
        string _customStopEvent = "Stop";
        Vector2 _scroll;

        static readonly string[] QualityLabels = { "Auto (1080 → 720)", "1080 preferred", "720 only" };
        static readonly string[] QualityValues = { "auto", "1080", "720" };

        [MenuItem("NekoSune/World/YouTube Proxy", false, 48)]
        public static void Open()
        {
            var w = GetWindow<NekoYouTubeProxySetupWindow>(false, "YouTube Proxy", true);
            w.minSize = new Vector2(720f, 650f);
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            GUILayout.Label("NekoSune World YouTube Proxy", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 });
            GUILayout.Label("Stable NekoSuneTools relay bridge for VRChat world video players", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            CardStart("1. Install / add the bridge");
            _target = (GameObject)EditorGUILayout.ObjectField("Player / bridge object", _target, typeof(GameObject), true);
            if (_target == null && Selection.activeGameObject != null) _target = Selection.activeGameObject;
            if (GUILayout.Button("INSTALL / REPAIR GENERATED UDON RUNTIME", GUILayout.Height(30f))) InstallRepairRuntime();
            if (GUILayout.Button("ADD / REPAIR BRIDGE ON SELECTED PLAYER", GUILayout.Height(34f))) AddBridgeToSelected();
            if (GUILayout.Button("ADD BRIDGES TO ALL STOCK VRCHAT VIDEO PLAYERS", GUILayout.Height(30f))) AddToAllStockPlayers();
            EditorGUILayout.HelpBox("The UdonSharp runtime is generated under Assets/NekoSune/YouTubeProxy/Generated so Unity/UdonSharp can compile a normal project script and program asset. On a first install, click Install/Repair, wait for Unity to finish compiling, then click Add/Repair Bridge.", MessageType.Info);
            EditorGUILayout.HelpBox("Stock detection supports VRCAVProVideoPlayer and VRCUnityVideoPlayer. If both exist on one object, runtime playback prefers AVPro so the same bridge can handle normal VOD and live HLS.", MessageType.Info);
            CardEnd();

            CardStart("2. Creator start URL");
            _youtubeUrl = EditorGUILayout.TextField("YouTube URL or video ID", _youtubeUrl);
            _quality = EditorGUILayout.Popup("Quality", _quality, QualityLabels);
            _playOnStart = EditorGUILayout.Toggle("Play on Start", _playOnStart);
            _syncUrl = EditorGUILayout.Toggle("Sync stable URL", _syncUrl);
            string id = ExtractYouTubeId(_youtubeUrl);
            string shortUrl = string.IsNullOrEmpty(id) ? "Invalid / unsupported YouTube ID" : BuildProxyUrl(id, QualityValues[Mathf.Clamp(_quality, 0, QualityValues.Length - 1)]);
            EditorGUILayout.SelectableLabel(shortUrl, EditorStyles.textField, GUILayout.Height(20f));
            EditorGUILayout.HelpBox("Every generated NekoSune relay URL keeps vrc=1 as the final query parameter.", MessageType.None);
            if (GUILayout.Button("APPLY START URL TO SELECTED BRIDGE", GUILayout.Height(32f))) ApplyStartUrl();
            CardEnd();

            CardStart("3. Runtime URL input");
            EditorGUILayout.HelpBox("Only YouTube URLs are special. Non-YouTube URLs are left to the existing player. VRChat does not allow pure Udon to freely create a new VRCUrl from a rewritten runtime string, so players should enter a complete NekoSune /v/VIDEO_ID?vrc=1 URL for proxy playback.", MessageType.Warning);
            EditorGUILayout.SelectableLabel(ProxyTemplate, EditorStyles.textField, GUILayout.Height(20f));
            if (GUILayout.Button("PREFILL SELECTED VRC URL INPUT WITH PROXY TEMPLATE", GUILayout.Height(30f))) PrepareSelectedUrlInput();
            CardEnd();

            CardStart("4. Community / custom Udon player adapter");
            _customPlayer = (Component)EditorGUILayout.ObjectField("Target UdonBehaviour", _customPlayer, typeof(Component), true);
            _customUrlVariable = EditorGUILayout.TextField("VRCUrl variable", _customUrlVariable);
            _customPlayEvent = EditorGUILayout.TextField("Play custom event", _customPlayEvent);
            _customStopEvent = EditorGUILayout.TextField("Stop custom event", _customStopEvent);
            if (GUILayout.Button("APPLY GENERIC UDON ADAPTER TO SELECTED BRIDGE", GUILayout.Height(30f))) ApplyCustomAdapter();
            EditorGUILayout.HelpBox("Use this for community players that expose a VRCUrl program variable plus a custom event such as Play/PlayUrl/LoadUrl. The bridge calls SetProgramVariable then SendCustomEvent. Variable/event names differ between prefabs, so they are intentionally configurable.", MessageType.Info);
            CardEnd();

            CardStart("Important VRChat rules");
            GUILayout.Label("• tools.nekosunevr.co.uk is the stable URL. Do not sync temporary /api/youtube-relay tokens.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• Only youtube.com, youtu.be and NekoSune /v/... URLs are watched/intercepted. Other URLs remain on the original player's normal path.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• VRChat globally rate-limits new video URLs to about one every five seconds per user. The bridge debounces and retries at 5 / 10 / 20 seconds.", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• The NekoSuneTools domain may require Allow Untrusted URLs unless it is allowed for the world/user.", EditorStyles.wordWrappedLabel);
            CardEnd();

            EditorGUILayout.EndScrollView();
        }

        void InstallRepairRuntime()
        {
            bool changed;
            if (!EnsureGeneratedRuntime(out changed)) return;

            if (changed)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Generated/refreshed:\n" + GeneratedScriptPath + "\n\nUnity/UdonSharp will compile it now. Wait for the spinner to finish, then click ADD / REPAIR BRIDGE.", "OK");
                return;
            }

            Type runtimeType = FindType("NekoSune.WorldYouTubeProxy.NekoYouTubeProxyPlayer");
            if (runtimeType == null)
            {
                AssetDatabase.ImportAsset(GeneratedScriptPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("YouTube Proxy", "The generated runtime already exists, but Unity has not exposed the compiled type yet. Wait for Unity/UdonSharp compilation to finish, then click this button again. If it never appears, check the first red Console error.", "OK");
                return;
            }

            bool createdProgram;
            if (!EnsureUdonSharpProgramAsset(runtimeType, out createdProgram))
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "The C# runtime compiled, but its UdonSharp program asset could not be found/repaired. Use VRChat SDK > Udon Sharp > Refresh All UdonSharp Assets, then click this button again.", "OK");
                return;
            }

            if (createdProgram || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "The UdonSharp program asset was created/refreshed. Let Unity/UdonSharp finish compiling, then add the bridge.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("YouTube Proxy", "Runtime and UdonSharp program asset are ready.", "OK");
        }

        void AddBridgeToSelected()
        {
            GameObject go = _target != null ? _target : Selection.activeGameObject;
            if (go == null) { EditorUtility.DisplayDialog("YouTube Proxy", "Select a video-player GameObject first.", "OK"); return; }
            Component bridge = EnsureBridge(go);
            if (bridge == null) return;
            AutoWireStock(go, bridge);
            SetField(bridge, "synchronizeUrl", _syncUrl);
            AutoWireInput(go, bridge);
            ApplyProxy(bridge);
            Selection.activeGameObject = go;
            EditorUtility.DisplayDialog("YouTube Proxy", "Bridge added/repaired. YouTube can use the NekoSune /v/... URL; non-YouTube URLs remain untouched by passive watching.", "OK");
        }

        void AddToAllStockPlayers()
        {
            Type avType = FindType("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
            Type unityType = FindType("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
            if (avType == null && unityType == null) { EditorUtility.DisplayDialog("YouTube Proxy", "VRChat Worlds video components were not found.", "OK"); return; }

            bool changedRuntime;
            if (!EnsureGeneratedRuntime(out changedRuntime)) return;
            if (changedRuntime || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Generated/refreshed the Udon runtime first. Let Unity finish compiling, then click ADD BRIDGES TO ALL again.", "OK");
                return;
            }

            var objects = new HashSet<GameObject>();
            CollectSceneObjects(avType, objects);
            CollectSceneObjects(unityType, objects);
            int changed = 0;
            foreach (GameObject go in objects)
            {
                Component bridge = EnsureBridge(go);
                if (bridge == null) continue;
                AutoWireStock(go, bridge);
                SetField(bridge, "synchronizeUrl", _syncUrl);
                AutoWireInput(go, bridge);
                ApplyProxy(bridge);
                changed++;
            }
            EditorUtility.DisplayDialog("YouTube Proxy", "Configured " + changed + " stock VRChat video-player object(s).", "OK");
        }

        void ApplyStartUrl()
        {
            GameObject go = _target != null ? _target : Selection.activeGameObject;
            Component bridge = FindBridge(go);
            if (bridge == null) { EditorUtility.DisplayDialog("YouTube Proxy", "Add/select a Neko YouTube Proxy bridge first.", "OK"); return; }
            string id = ExtractYouTubeId(_youtubeUrl);
            if (string.IsNullOrEmpty(id)) { EditorUtility.DisplayDialog("YouTube Proxy", "Could not extract an 11-character YouTube video ID.", "OK"); return; }
            string proxy = BuildProxyUrl(id, QualityValues[Mathf.Clamp(_quality, 0, QualityValues.Length - 1)]);
            object vrcUrl = CreateVrcUrl(proxy);
            if (vrcUrl == null) { EditorUtility.DisplayDialog("YouTube Proxy", "VRCUrl type/constructor was not found. Is the VRChat Worlds SDK installed?", "OK"); return; }
            SetField(bridge, "startUrl", vrcUrl);
            SetField(bridge, "playStartUrl", _playOnStart);
            SetField(bridge, "synchronizeUrl", _syncUrl);
            ApplyProxy(bridge);
            EditorUtility.DisplayDialog("YouTube Proxy", "Applied stable start URL:\n" + proxy, "OK");
        }

        void PrepareSelectedUrlInput()
        {
            Type inputType = FindType("VRC.SDK3.Components.VRCUrlInputField");
            if (inputType == null) { EditorUtility.DisplayDialog("YouTube Proxy", "VRCUrlInputField was not found.", "OK"); return; }
            GameObject go = Selection.activeGameObject;
            Component input = go == null ? null : go.GetComponent(inputType);
            if (input == null && go != null) input = go.GetComponentInChildren(inputType, true);
            if (input == null) { EditorUtility.DisplayDialog("YouTube Proxy", "Select a GameObject containing a VRCUrlInputField.", "OK"); return; }
            object url = CreateVrcUrl(ProxyTemplate);
            MethodInfo setUrl = inputType.GetMethod("SetUrl", BindingFlags.Public | BindingFlags.Instance);
            if (url == null || setUrl == null) { EditorUtility.DisplayDialog("YouTube Proxy", "Could not prepare this VRCUrlInputField.", "OK"); return; }
            Undo.RecordObject(input, "Prepare Neko proxy URL input");
            setUrl.Invoke(input, new[] { url });
            EditorUtility.SetDirty(input);
            EditorUtility.DisplayDialog("YouTube Proxy", "Prefilled the input field with a proxy URL ending in ?vrc=1. Replace VIDEO_ID with the 11-character YouTube ID.", "OK");
        }

        void ApplyCustomAdapter()
        {
            GameObject go = _target != null ? _target : Selection.activeGameObject;
            Component bridge = FindBridge(go);
            if (bridge == null) { EditorUtility.DisplayDialog("YouTube Proxy", "Add/select a bridge first.", "OK"); return; }
            if (_customPlayer == null || _customPlayer.GetType().Name.IndexOf("UdonBehaviour", StringComparison.OrdinalIgnoreCase) < 0)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Assign the community player's UdonBehaviour component.", "OK");
                return;
            }
            SetField(bridge, "customPlayer", _customPlayer);
            SetField(bridge, "customUrlVariable", _customUrlVariable);
            SetField(bridge, "customPlayEvent", _customPlayEvent);
            SetField(bridge, "customStopEvent", _customStopEvent);
            ApplyProxy(bridge);
            EditorUtility.DisplayDialog("YouTube Proxy", "Generic Udon player adapter configured.", "OK");
        }

        static Component EnsureBridge(GameObject go)
        {
            bool generatedChanged;
            if (!EnsureGeneratedRuntime(out generatedChanged)) return null;
            if (generatedChanged)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Installed/refreshed NekoYouTubeProxyPlayer.cs under Assets. Let Unity/UdonSharp compile, then click ADD / REPAIR BRIDGE again.", "OK");
                return null;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Unity is still compiling/importing. Wait for it to finish, then click Add/Repair again.", "OK");
                return null;
            }

            Type runtimeType = FindType("NekoSune.WorldYouTubeProxy.NekoYouTubeProxyPlayer");
            if (runtimeType == null)
            {
                AssetDatabase.ImportAsset(GeneratedScriptPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("YouTube Proxy", "The generated script exists, but Unity has not exposed NekoYouTubeProxyPlayer yet. Wait for compilation and click Add/Repair again. If it persists, check the first red Console error.", "OK");
                return null;
            }

            bool createdProgram;
            if (!EnsureUdonSharpProgramAsset(runtimeType, out createdProgram))
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Could not find/create the UdonSharp program asset for NekoYouTubeProxyPlayer. Use VRChat SDK > Udon Sharp > Refresh All UdonSharp Assets, then try again.", "OK");
                return null;
            }
            if (createdProgram || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Created/repaired the UdonSharp program asset. Let UdonSharp finish compiling, then click Add/Repair again.", "OK");
                return null;
            }

            Component bridge = go.GetComponent(runtimeType);
            if (bridge != null) return bridge;

            Type extensions = FindType("UdonSharpEditor.UdonSharpComponentExtensions");
            if (extensions != null)
            {
                MethodInfo add = extensions.GetMethod("AddUdonSharpComponent", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(GameObject), typeof(Type) }, null);
                if (add != null)
                {
                    try { return add.Invoke(null, new object[] { go, runtimeType }) as Component; }
                    catch (Exception e)
                    {
                        Exception cause = e.InnerException ?? e;
                        Debug.LogError("[NekoSune YouTube Proxy] UdonSharp AddComponent failed: " + cause.Message);
                    }
                }
            }
            EditorUtility.DisplayDialog("YouTube Proxy", "UdonSharp's editor AddUdonSharpComponent API was not found. Use VRChat SDK > Udon Sharp > Refresh All UdonSharp Assets, then click Repair again.", "OK");
            return null;
        }

        static Component FindBridge(GameObject go)
        {
            if (go == null) return null;
            Type runtimeType = FindType("NekoSune.WorldYouTubeProxy.NekoYouTubeProxyPlayer");
            return runtimeType == null ? null : go.GetComponent(runtimeType);
        }

        static bool EnsureGeneratedRuntime(out bool changed)
        {
            changed = false;
            string source = Path.GetFullPath(RuntimeTemplatePath);
            if (!File.Exists(source))
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Runtime template is missing from the installed package:\n" + RuntimeTemplatePath, "OK");
                return false;
            }

            EnsureFolder(GeneratedFolder);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string destination = Path.Combine(projectRoot, GeneratedScriptPath.Replace('/', Path.DirectorySeparatorChar));
            string wanted = File.ReadAllText(source);
            string current = File.Exists(destination) ? File.ReadAllText(destination) : null;

            if (!string.Equals(current, wanted, StringComparison.Ordinal))
            {
                File.WriteAllText(destination, wanted);
                changed = true;
            }

            AssetDatabase.ImportAsset(GeneratedScriptPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            return true;
        }

        static bool EnsureUdonSharpProgramAsset(Type runtimeType, out bool created)
        {
            created = false;
            Type programType = FindType("UdonSharp.UdonSharpProgramAsset");
            if (programType == null) return false;

            MethodInfo getProgram = programType.GetMethod("GetProgramAssetForClass", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Type) }, null);
            if (getProgram != null)
            {
                try { if (getProgram.Invoke(null, new object[] { runtimeType }) != null) return true; }
                catch { }
            }

            MonoScript source = AssetDatabase.LoadAssetAtPath<MonoScript>(GeneratedScriptPath);
            if (source == null) return false;

            UnityEngine.Object existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GeneratedProgramPath);
            FieldInfo sourceField = programType.GetField("sourceCsScript", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourceField == null) return false;

            if (existing == null)
            {
                ScriptableObject program = ScriptableObject.CreateInstance(programType);
                sourceField.SetValue(program, source);
                AssetDatabase.CreateAsset(program, GeneratedProgramPath);
                EditorUtility.SetDirty(program);
                AssetDatabase.SaveAssets();
                created = true;
            }
            else if (sourceField.GetValue(existing) == null)
            {
                sourceField.SetValue(existing, source);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                created = true;
            }

            TryCompileUdonSharp(programType);
            AssetDatabase.ImportAsset(GeneratedProgramPath, ImportAssetOptions.ForceUpdate);

            if (created) return true;
            if (getProgram == null) return existing != null;
            try { return getProgram.Invoke(null, new object[] { runtimeType }) != null; }
            catch { return false; }
        }

        static void TryCompileUdonSharp(Type programType)
        {
            try
            {
                MethodInfo compile = programType.GetMethod("CompileAllCsPrograms", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (compile == null) return;
                ParameterInfo[] ps = compile.GetParameters();
                if (ps.Length == 2) compile.Invoke(null, new object[] { true, true });
                else if (ps.Length == 1) compile.Invoke(null, new object[] { true });
                else compile.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[NekoSune YouTube Proxy] UdonSharp compile request deferred: " + e.Message);
            }
        }

        static void AutoWireStock(GameObject go, Component bridge)
        {
            Type avType = FindType("VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
            Type unityType = FindType("VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
            if (avType != null) SetField(bridge, "avProPlayer", go.GetComponent(avType));
            if (unityType != null) SetField(bridge, "unityPlayer", go.GetComponent(unityType));
        }

        static void AutoWireInput(GameObject go, Component bridge)
        {
            Type inputType = FindType("VRC.SDK3.Components.VRCUrlInputField");
            if (inputType == null) return;
            Component input = go.GetComponent(inputType);
            if (input == null && go.transform.root != null) input = go.transform.root.gameObject.GetComponentInChildren(inputType, true);
            if (input != null) SetField(bridge, "proxyInput", input);
        }

        static void CollectSceneObjects(Type type, HashSet<GameObject> into)
        {
            if (type == null) return;
            UnityEngine.Object[] found = Resources.FindObjectsOfTypeAll(type);
            for (int i = 0; i < found.Length; i++)
            {
                Component c = found[i] as Component;
                if (c == null || EditorUtility.IsPersistent(c) || !c.gameObject.scene.IsValid()) continue;
                into.Add(c.gameObject);
            }
        }

        static object CreateVrcUrl(string url)
        {
            Type type = FindType("VRC.SDKBase.VRCUrl");
            if (type == null) return null;
            try { return Activator.CreateInstance(type, new object[] { url }); }
            catch { return null; }
        }

        static string BuildProxyUrl(string id, string quality)
        {
            string value = ProxyOrigin + "/v/" + id + "?";
            if (!string.IsNullOrEmpty(quality) && quality != "auto") value += "q=" + quality + "&";
            value += "vrc=1";
            return value;
        }

        static string ExtractYouTubeId(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.Trim();
            if (IsVideoId(value)) return value;

            string[] markers = { "youtu.be/", "youtube.com/shorts/", "youtube.com/live/", "youtube.com/embed/", "youtube.com/watch?v=" };
            for (int i = 0; i < markers.Length; i++)
            {
                int p = value.IndexOf(markers[i], StringComparison.OrdinalIgnoreCase);
                if (p < 0) continue;
                string tail = value.Substring(p + markers[i].Length);
                string id = TakeId(tail);
                if (IsVideoId(id)) return id;
            }

            int q = value.IndexOf("v=", StringComparison.OrdinalIgnoreCase);
            if (q >= 0)
            {
                string id = TakeId(value.Substring(q + 2));
                if (IsVideoId(id)) return id;
            }
            return null;
        }

        static string TakeId(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            int end = value.Length;
            char[] stops = { '&', '?', '#', '/', ' ' };
            for (int i = 0; i < stops.Length; i++)
            {
                int p = value.IndexOf(stops[i]);
                if (p >= 0 && p < end) end = p;
            }
            return value.Substring(0, end);
        }

        static bool IsVideoId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 11) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_')) return false;
            }
            return true;
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

        static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type t = assemblies[i].GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }

        static void SetField(Component component, string fieldName, object value)
        {
            if (component == null) return;
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;
            Undo.RecordObject(component, "Configure Neko YouTube Proxy");
            try { field.SetValue(component, value); EditorUtility.SetDirty(component); }
            catch (Exception e) { Debug.LogWarning("[NekoSune YouTube Proxy] Could not set " + fieldName + ": " + e.Message); }
        }

        static void ApplyProxy(Component component)
        {
            if (component == null) return;
            Type utility = FindType("UdonSharpEditor.UdonSharpEditorUtility");
            if (utility == null) return;
            MethodInfo[] methods = utility.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m.Name != "CopyProxyToUdon") continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != 1 || !ps[0].ParameterType.IsAssignableFrom(component.GetType())) continue;
                try { m.Invoke(null, new object[] { component }); } catch { }
                return;
            }
        }

        static void CardStart(string title)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(title, EditorStyles.boldLabel);
        }

        static void CardEnd()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }
    }
}
