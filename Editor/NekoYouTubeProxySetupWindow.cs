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
        public string DescriptionKey { get { return "One-click YouTube relay setup for stock and popular VRChat video players without touching non-YouTube URLs."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "▶"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoYouTubeProxySetupWindow.Open(); }
    }

    internal sealed class NekoYouTubeProxySetupWindow : EditorWindow
    {
        string _startYouTubeUrl = "";
        int _quality;
        bool _playOnStart;
        bool _syncStockUrl = true;
        bool _advanced;
        Vector2 _scroll;

        static readonly string[] QualityLabels = { "Auto (1080 → 720)", "1080 preferred", "720 only" };
        static readonly string[] QualityValues = { "auto", "1080", "720" };

        [MenuItem("NekoSune/World/YouTube Proxy", false, 48)]
        public static void Open()
        {
            var w = GetWindow<NekoYouTubeProxySetupWindow>(false, "YouTube Proxy", true);
            w.minSize = new Vector2(720f, 610f);
            w.Show();
        }

        [MenuItem("NekoSune/World/YouTube Proxy/One Click Auto Setup Whole Scene", false, 49)]
        static void OneClickMenu()
        {
            NekoYouTubeProxyAutoSetup.Begin(false, null, "", "auto", false, true);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            GUILayout.Label("NekoSune World YouTube Proxy", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 });
            GUILayout.Label("One-click setup for YouTube relay support while every non-YouTube URL stays on the player's normal path.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("ONE CLICK SETUP", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            EditorGUILayout.HelpBox(
                "This generates the UdonSharp runtime under Assets, creates/associates its U# program .asset, compiles it, scans the scene, detects supported video-player families, auto-fills URL/player references, and converts creator-time YouTube defaults to NekoSune relay URLs.",
                MessageType.Info);

            GUIStyle big = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 14 };
            if (GUILayout.Button("ONE CLICK AUTO SETUP WHOLE SCENE", big, GUILayout.Height(48f)))
                StartOneClick(false);

            using (new EditorGUI.DisabledScope(Selection.activeGameObject == null))
            {
                if (GUILayout.Button("ONE CLICK SETUP SELECTED PLAYER", GUILayout.Height(34f)))
                    StartOneClick(true);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Optional creator start YouTube URL", EditorStyles.boldLabel);
            _startYouTubeUrl = EditorGUILayout.TextField("YouTube URL / video ID", _startYouTubeUrl);
            _quality = EditorGUILayout.Popup("Relay quality", _quality, QualityLabels);
            _playOnStart = EditorGUILayout.Toggle("Play start URL on stock player", _playOnStart);
            _syncStockUrl = EditorGUILayout.Toggle("Sync stock bridge URL", _syncStockUrl);

            string id = NekoYouTubeProxyAutoSetup.ExtractYouTubeId(_startYouTubeUrl);
            string preview = string.IsNullOrEmpty(id)
                ? "Leave blank to preserve existing player defaults"
                : NekoYouTubeProxyAutoSetup.BuildProxyUrl(id, QualityValues[Mathf.Clamp(_quality, 0, QualityValues.Length - 1)]);
            EditorGUILayout.SelectableLabel(preview, EditorStyles.textField, GUILayout.Height(20f));
            EditorGUILayout.HelpBox("vrc=1 is always the final query parameter.", MessageType.None);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Auto-detected player families", EditorStyles.boldLabel);
            GUILayout.Label(
                "VRChat AVPro / Unity • VideoTXL • ProTV / RiskiPlayer • USharpVideo / ModernUI • YamaPlayer • VizVid • ZPlayer • iwaSync3 • KineL • TopazChat • UdonVR Video Player • JT Playlist • generic VRCUrlInputField video prefabs",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox(
                "Community players keep their own queue, ownership and synchronization code. NekoSune configures their existing URL/default fields instead of bypassing them. Stock VRChat players can be driven directly by the generated bridge.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Runtime URL rule", EditorStyles.boldLabel);
            GUILayout.Label("YouTube relay format:", EditorStyles.label);
            EditorGUILayout.SelectableLabel("https://tools.nekosunevr.co.uk/v/VIDEO_ID?vrc=1", EditorStyles.textField, GUILayout.Height(20f));
            EditorGUILayout.HelpBox(
                "VRChat only permits VRCUrl(string) at editor time. One-click setup automatically converts creator-time/default YouTube URLs. At runtime, users should enter the complete NekoSune /v/VIDEO_ID?...&vrc=1 URL. Non-YouTube URLs are never rewritten by this package.",
                MessageType.Warning);
            EditorGUILayout.EndVertical();

            _advanced = EditorGUILayout.Foldout(_advanced, "Advanced / repair", true);
            if (_advanced)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (GUILayout.Button("REPAIR GENERATED U# SOURCE + PROGRAM ASSET", GUILayout.Height(30f)))
                    NekoYouTubeProxyAutoSetup.BeginRepairOnly();
                if (GUILayout.Button("RESCAN + CONFIGURE SCENE (RUNTIME ALREADY INSTALLED)", GUILayout.Height(30f)))
                    NekoYouTubeProxyAutoSetup.Begin(false, null, _startYouTubeUrl, QualityValues[Mathf.Clamp(_quality, 0, QualityValues.Length - 1)], _playOnStart, _syncStockUrl);
                GUILayout.Label("Generated files:", EditorStyles.boldLabel);
                GUILayout.Label(NekoYouTubeProxyAutoSetup.GeneratedScriptPath, EditorStyles.wordWrappedLabel);
                GUILayout.Label(NekoYouTubeProxyAutoSetup.GeneratedProgramPath, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        void StartOneClick(bool selectedOnly)
        {
            GameObject selected = selectedOnly ? Selection.activeGameObject : null;
            if (selectedOnly && selected == null)
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Select a video-player object or one of its children first.", "OK");
                return;
            }

            NekoYouTubeProxyAutoSetup.Begin(
                selectedOnly,
                selected,
                _startYouTubeUrl,
                QualityValues[Mathf.Clamp(_quality, 0, QualityValues.Length - 1)],
                _playOnStart,
                _syncStockUrl);
        }
    }

    [InitializeOnLoad]
    internal static class NekoYouTubeProxyAutoSetup
    {
        internal const string ProxyOrigin = "https://tools.nekosunevr.co.uk";
        internal const string ProxyTemplate = "https://tools.nekosunevr.co.uk/v/VIDEO_ID?vrc=1";
        internal const string GeneratedFolder = "Assets/NekoSune/YouTubeProxy/Generated";
        internal const string GeneratedScriptPath = GeneratedFolder + "/NekoYouTubeProxyPlayer.cs";
        internal const string GeneratedProgramPath = GeneratedFolder + "/NekoYouTubeProxyPlayer.asset";
        const string RuntimeTemplatePath = "Packages/com.nekosune.world-youtube-proxy/Templates/Runtime/NekoYouTubeProxyPlayer.cs.txt";
        const string RuntimeTypeName = "NekoSune.WorldYouTubeProxy.NekoYouTubeProxyPlayer";

        const string PendingKey = "NekoSune.YouTubeProxy.Pending";
        const string SelectedOnlyKey = "NekoSune.YouTubeProxy.SelectedOnly";
        const string SelectedObjectKey = "NekoSune.YouTubeProxy.SelectedObject";
        const string StartUrlKey = "NekoSune.YouTubeProxy.StartUrl";
        const string QualityKey = "NekoSune.YouTubeProxy.Quality";
        const string PlayStartKey = "NekoSune.YouTubeProxy.PlayStart";
        const string SyncKey = "NekoSune.YouTubeProxy.Sync";
        const string RepairOnlyKey = "NekoSune.YouTubeProxy.RepairOnly";
        const string AttemptKey = "NekoSune.YouTubeProxy.Attempt";

        static bool _resumeQueued;

        sealed class Candidate
        {
            public GameObject root;
            public string family;
            public Component primary;
            public Component input;
            public Component avPro;
            public Component unityVideo;
            public bool community;
        }

        sealed class SetupStats
        {
            public int detected;
            public int bridges;
            public int convertedUrls;
            public int inputs;
            public readonly Dictionary<string, int> families = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public void AddFamily(string family)
            {
                int count;
                families.TryGetValue(family, out count);
                families[family] = count + 1;
            }
        }

        static NekoYouTubeProxyAutoSetup()
        {
            if (SessionState.GetBool(PendingKey, false)) QueueResume();
        }

        internal static void Begin(bool selectedOnly, GameObject selected, string startUrl, string quality, bool playStart, bool sync)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(RepairOnlyKey, false);
            SessionState.SetBool(SelectedOnlyKey, selectedOnly);
            SessionState.SetString(StartUrlKey, startUrl ?? "");
            SessionState.SetString(QualityKey, string.IsNullOrEmpty(quality) ? "auto" : quality);
            SessionState.SetBool(PlayStartKey, playStart);
            SessionState.SetBool(SyncKey, sync);
            SessionState.SetInt(AttemptKey, 0);

            string globalId = "";
            if (selectedOnly && selected != null)
            {
                try { globalId = GlobalObjectId.GetGlobalObjectIdSlow(selected).ToString(); }
                catch { globalId = ""; }
            }
            SessionState.SetString(SelectedObjectKey, globalId);

            bool changed;
            if (!EnsureGeneratedSource(out changed))
            {
                ClearPending();
                return;
            }

            Debug.Log("[NekoSune YouTube Proxy] One-click setup started. Unity/UdonSharp may reload once while the generated runtime is imported.");
            QueueResume();
        }

        internal static void BeginRepairOnly()
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(RepairOnlyKey, true);
            SessionState.SetBool(SelectedOnlyKey, false);
            SessionState.SetString(SelectedObjectKey, "");
            SessionState.SetInt(AttemptKey, 0);

            bool changed;
            if (!EnsureGeneratedSource(out changed))
            {
                ClearPending();
                return;
            }
            QueueResume();
        }

        static void QueueResume()
        {
            if (_resumeQueued) return;
            _resumeQueued = true;
            EditorApplication.delayCall += ResumePending;
        }

        static void ResumePending()
        {
            _resumeQueued = false;
            if (!SessionState.GetBool(PendingKey, false)) return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueResume();
                return;
            }

            int attempt = SessionState.GetInt(AttemptKey, 0) + 1;
            SessionState.SetInt(AttemptKey, attempt);
            if (attempt > 250)
            {
                Fail("Unity never exposed the generated UdonSharp type/program asset. Check the first red Console error, then use Advanced > Repair Generated U# Source + Program Asset.");
                return;
            }

            MonoScript source = AssetDatabase.LoadAssetAtPath<MonoScript>(GeneratedScriptPath);
            Type runtimeType = source != null ? source.GetClass() : null;
            if (runtimeType == null) runtimeType = FindType(RuntimeTypeName);
            if (source == null || runtimeType == null)
            {
                AssetDatabase.ImportAsset(GeneratedScriptPath, ImportAssetOptions.ForceUpdate);
                QueueResume();
                return;
            }

            bool createdProgram;
            UnityEngine.Object programAsset;
            if (!EnsureProgramAsset(source, runtimeType, out programAsset, out createdProgram))
            {
                QueueResume();
                return;
            }

            if (createdProgram)
            {
                CompileUdonSharp();
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(GeneratedProgramPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueResume();
                return;
            }

            if (SessionState.GetBool(RepairOnlyKey, false))
            {
                ClearPending();
                EditorUtility.DisplayDialog(
                    "YouTube Proxy",
                    "UdonSharp runtime repaired and program asset created/associated.\n\n" + GeneratedScriptPath + "\n" + GeneratedProgramPath,
                    "OK");
                return;
            }

            GameObject selected = ResolveSelectedObject();
            bool selectedOnly = SessionState.GetBool(SelectedOnlyKey, false);
            if (selectedOnly && selected == null)
            {
                Fail("The selected scene object could not be restored after Unity's domain reload. Select the player again and click One Click Setup Selected Player.");
                return;
            }

            string startUrl = SessionState.GetString(StartUrlKey, "");
            string quality = SessionState.GetString(QualityKey, "auto");
            bool playStart = SessionState.GetBool(PlayStartKey, false);
            bool sync = SessionState.GetBool(SyncKey, true);

            SetupStats stats = ConfigureScene(runtimeType, selectedOnly ? selected : null, startUrl, quality, playStart, sync);
            AssetDatabase.SaveAssets();
            ClearPending();

            string summary = BuildSummary(stats);
            Debug.Log("[NekoSune YouTube Proxy] One-click setup complete.\n" + summary);
            EditorUtility.DisplayDialog("NekoSune YouTube Proxy", "One-click setup complete.\n\n" + summary, "OK");
        }

        static bool EnsureGeneratedSource(out bool changed)
        {
            changed = false;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string templateFull = Path.Combine(projectRoot, RuntimeTemplatePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(templateFull))
            {
                EditorUtility.DisplayDialog("YouTube Proxy", "Runtime template is missing:\n" + RuntimeTemplatePath, "OK");
                return false;
            }

            EnsureFolder(GeneratedFolder);
            string destination = Path.Combine(projectRoot, GeneratedScriptPath.Replace('/', Path.DirectorySeparatorChar));
            string wanted = File.ReadAllText(templateFull);
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

        static bool EnsureProgramAsset(MonoScript source, Type runtimeType, out UnityEngine.Object programAsset, out bool created)
        {
            programAsset = null;
            created = false;

            Type programType = FindType("UdonSharp.UdonSharpProgramAsset");
            if (programType == null)
            {
                Fail("UdonSharpProgramAsset was not found. Make sure the VRChat Worlds SDK/UdonSharp integration is installed.");
                return false;
            }

            Type utilityType = FindType("UdonSharpEditor.UdonSharpEditorUtility");
            if (utilityType != null)
            {
                MethodInfo getProgram = utilityType.GetMethod(
                    "GetUdonSharpProgramAsset",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(Type) },
                    null);
                if (getProgram != null)
                {
                    try { programAsset = getProgram.Invoke(null, new object[] { runtimeType }) as UnityEngine.Object; }
                    catch { programAsset = null; }
                }
            }

            if (programAsset == null)
                programAsset = AssetDatabase.LoadAssetAtPath(GeneratedProgramPath, programType);

            FieldInfo sourceField = programType.GetField("sourceCsScript", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourceField == null)
            {
                Fail("This UdonSharp version does not expose sourceCsScript on UdonSharpProgramAsset.");
                return false;
            }

            if (programAsset == null)
            {
                ScriptableObject createdAsset = ScriptableObject.CreateInstance(programType);
                sourceField.SetValue(createdAsset, source);
                AssetDatabase.CreateAsset(createdAsset, GeneratedProgramPath);
                EditorUtility.SetDirty(createdAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(GeneratedProgramPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                programAsset = AssetDatabase.LoadAssetAtPath(GeneratedProgramPath, programType);
                created = programAsset != null;
            }
            else
            {
                object currentSource = sourceField.GetValue(programAsset);
                if (currentSource != source)
                {
                    sourceField.SetValue(programAsset, source);
                    EditorUtility.SetDirty(programAsset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(GeneratedProgramPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    created = true;
                }
            }

            if (programAsset == null) return false;

            // Newer UdonSharp discovers program assets after a synchronous import. Compile now
            // so AddUdonSharpComponent can immediately resolve the proxy/backing program pair.
            CompileUdonSharp();
            return true;
        }

        static void CompileUdonSharp()
        {
            try
            {
                Type compilerType = FindType("UdonSharp.Compiler.UdonSharpCompilerV1");
                if (compilerType != null)
                {
                    MethodInfo compileSync = compilerType.GetMethod("CompileSync", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
                    if (compileSync != null)
                    {
                        compileSync.Invoke(null, null);
                        return;
                    }
                }

                Type programType = FindType("UdonSharp.UdonSharpProgramAsset");
                if (programType == null) return;
                MethodInfo compile = programType.GetMethod("CompileAllCsPrograms", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (compile == null) return;
                ParameterInfo[] ps = compile.GetParameters();
                if (ps.Length == 2) compile.Invoke(null, new object[] { true, true });
                else if (ps.Length == 1) compile.Invoke(null, new object[] { true });
                else compile.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[NekoSune YouTube Proxy] UdonSharp compile request deferred: " + (e.InnerException ?? e).Message);
            }
        }

        static SetupStats ConfigureScene(Type runtimeType, GameObject selectedRoot, string startUrl, string quality, bool playStart, bool sync)
        {
            List<Candidate> candidates = FindCandidates(selectedRoot);
            SetupStats stats = new SetupStats();
            stats.detected = candidates.Count;

            string startId = ExtractYouTubeId(startUrl);
            object explicitStart = string.IsNullOrEmpty(startId) ? null : CreateVrcUrl(BuildProxyUrl(startId, quality));

            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                stats.AddFamily(candidate.family);

                candidate.input = FindBestUrlInput(candidate);
                candidate.avPro = FindFirstComponent(candidate.root, "VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer");
                candidate.unityVideo = FindFirstComponent(candidate.root, "VRC.SDK3.Video.Components.VRCUnityVideoPlayer");
                if (candidate.input != null)
                {
                    stats.inputs++;
                    ConfigureInputHint(candidate.input);
                }

                stats.convertedUrls += ConvertSerializedYouTubeUrls(candidate.root);

                if (explicitStart != null)
                    TryApplyCommunityStartUrl(candidate, explicitStart);

                Component bridge = EnsureBridge(candidate.root, runtimeType);
                if (bridge == null) continue;
                stats.bridges++;

                SetBridgeField(bridge, "detectedPlayerFamily", candidate.family);
                SetBridgeField(bridge, "communityPlayerMode", candidate.community);
                SetBridgeField(bridge, "proxyInput", candidate.input);
                SetBridgeField(bridge, "avProPlayer", candidate.avPro);
                SetBridgeField(bridge, "unityPlayer", candidate.unityVideo);
                SetBridgeField(bridge, "stopNativePlayerOnBridgeStart", false);

                if (candidate.community)
                {
                    // Community players already own their queue/ownership/sync path. Do not race it.
                    SetBridgeField(bridge, "autoWatchInput", false);
                    SetBridgeField(bridge, "synchronizeUrl", false);
                    SetBridgeField(bridge, "playStartUrl", false);
                }
                else
                {
                    SetBridgeField(bridge, "autoWatchInput", candidate.input != null);
                    SetBridgeField(bridge, "synchronizeUrl", sync);
                    if (explicitStart != null)
                    {
                        SetBridgeField(bridge, "startUrl", explicitStart);
                        SetBridgeField(bridge, "playStartUrl", playStart);
                    }
                }

                ApplyProxyToBacking(bridge);
            }

            return stats;
        }

        static List<Candidate> FindCandidates(GameObject selectedRoot)
        {
            Component[] components = selectedRoot != null
                ? selectedRoot.GetComponentsInChildren<Component>(true)
                : Resources.FindObjectsOfTypeAll<Component>();

            var byRoot = new Dictionary<int, Candidate>();
            var communityRoots = new List<GameObject>();

            // First pass: known community players. This prevents their nested AVPro/Unity
            // components from being treated as separate stock players.
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (!IsSceneComponent(c)) continue;
                string family = ClassifyCommunity(c);
                if (string.IsNullOrEmpty(family)) continue;

                GameObject root = ResolvePlayerRoot(c.gameObject, family, selectedRoot);
                if (root == null) continue;
                int key = root.GetInstanceID();
                Candidate existing;
                if (!byRoot.TryGetValue(key, out existing))
                {
                    existing = new Candidate { root = root, family = family, primary = c, community = true };
                    byRoot.Add(key, existing);
                    communityRoots.Add(root);
                }
                else if (existing.family == "Generic Video Player")
                {
                    existing.family = family;
                    existing.primary = c;
                    existing.community = true;
                }
            }

            // Second pass: stock players not already living inside a known community prefab.
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (!IsSceneComponent(c)) continue;
                string full = c.GetType().FullName ?? "";
                bool stock = full == "VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer" ||
                             full == "VRC.SDK3.Video.Components.VRCUnityVideoPlayer";
                if (!stock) continue;
                if (IsInsideAny(c.gameObject, communityRoots)) continue;

                GameObject root = ResolvePlayerRoot(c.gameObject, "VRChat Stock", selectedRoot);
                int key = root.GetInstanceID();
                Candidate candidate;
                if (!byRoot.TryGetValue(key, out candidate))
                {
                    candidate = new Candidate { root = root, family = "VRChat Stock", primary = c, community = false };
                    byRoot.Add(key, candidate);
                }
            }

            // Last pass: a generic prefab with a VRCUrlInputField and a video-looking hierarchy.
            Type inputType = FindType("VRC.SDK3.Components.VRCUrlInputField");
            if (inputType != null)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    Component c = components[i];
                    if (!IsSceneComponent(c) || !inputType.IsAssignableFrom(c.GetType())) continue;
                    if (IsInsideAny(c.gameObject, communityRoots)) continue;
                    if (!LooksLikeVideoHierarchy(c.gameObject)) continue;

                    GameObject root = ResolvePlayerRoot(c.gameObject, "Generic Video Player", selectedRoot);
                    int key = root.GetInstanceID();
                    if (!byRoot.ContainsKey(key))
                        byRoot.Add(key, new Candidate { root = root, family = "Generic Video Player", primary = c, community = true });
                }
            }

            var result = new List<Candidate>(byRoot.Values);
            result.Sort(delegate(Candidate a, Candidate b)
            {
                int f = string.Compare(a.family, b.family, StringComparison.OrdinalIgnoreCase);
                return f != 0 ? f : string.Compare(a.root.name, b.root.name, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        static string ClassifyCommunity(Component c)
        {
            Type t = c.GetType();
            string full = (t.FullName ?? t.Name).ToLowerInvariant();
            string hierarchy = GetHierarchyPath(c.gameObject).ToLowerInvariant();

            if (full.StartsWith("texel.") || full.Contains("videotxl")) return "VideoTXL";
            if (full.StartsWith("udonsharp.video.") || full.Contains("usharpvideo") || hierarchy.Contains("usharpvideo")) return "USharpVideo / ModernUI";
            if (full.StartsWith("yamadev.yamastream") || full.Contains("yamaplayer") || hierarchy.Contains("yamaplayer")) return "YamaPlayer";
            if (full.StartsWith("jlchntoz.vrc.vvmw") || full.Contains("vizvid") || hierarchy.Contains("vizvid")) return "VizVid";
            if (full.Contains("architech") || full.Contains("protv") || hierarchy.Contains("protv") || hierarchy.Contains("riskiplayer")) return "ProTV / RiskiPlayer";
            if (full.Contains("zplayer") || hierarchy.Contains("zplayer")) return "ZPlayer";
            if (full.Contains("iwasync") || hierarchy.Contains("iwasync")) return "iwaSync3";
            if (full.Contains("kinel") || hierarchy.Contains("kinel")) return "KineL";
            if (full.Contains("topaz") || hierarchy.Contains("topazchat")) return "TopazChat";
            if (full.StartsWith("udonvr.takato.videoplayer") || hierarchy.Contains("udonvideoplayer")) return "UdonVR Video Player";
            if (full.Contains("jtplaylist") || hierarchy.Contains("jt playlist") || hierarchy.Contains("jtplaylist")) return "JT Playlist";
            return null;
        }

        static bool IsSceneComponent(Component c)
        {
            if (c == null) return false;
            try
            {
                if (EditorUtility.IsPersistent(c)) return false;
                return c.gameObject != null && c.gameObject.scene.IsValid();
            }
            catch { return false; }
        }

        static GameObject ResolvePlayerRoot(GameObject go, string family, GameObject selectedRoot)
        {
            if (selectedRoot != null) return selectedRoot;

            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (prefabRoot != null) return prefabRoot;

            Transform current = go.transform;
            Transform best = current;
            int depth = 0;
            while (current != null && depth < 7)
            {
                string n = current.name.ToLowerInvariant();
                if (n.Contains("video") || n.Contains("player") || n.Contains("tv") || n.Contains("sync") ||
                    n.Contains("yama") || n.Contains("viz") || n.Contains("zplayer") || n.Contains("topaz"))
                    best = current;
                current = current.parent;
                depth++;
            }
            return best.gameObject;
        }

        static bool IsInsideAny(GameObject go, List<GameObject> roots)
        {
            Transform t = go.transform;
            for (int i = 0; i < roots.Count; i++)
            {
                GameObject root = roots[i];
                if (root != null && (go == root || t.IsChildOf(root.transform))) return true;
            }
            return false;
        }

        static bool LooksLikeVideoHierarchy(GameObject go)
        {
            string path = GetHierarchyPath(go).ToLowerInvariant();
            return path.Contains("video") || path.Contains("player") || path.Contains("screen") || path.Contains("tv");
        }

        static string GetHierarchyPath(GameObject go)
        {
            if (go == null) return "";
            string value = go.name;
            Transform p = go.transform.parent;
            int depth = 0;
            while (p != null && depth < 8)
            {
                value = p.name + "/" + value;
                p = p.parent;
                depth++;
            }
            return value;
        }

        static Component FindBestUrlInput(Candidate candidate)
        {
            Type inputType = FindType("VRC.SDK3.Components.VRCUrlInputField");
            if (inputType == null || candidate.root == null) return null;

            Component[] components = candidate.root.GetComponentsInChildren<Component>(true);
            Component best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < components.Length; i++)
            {
                Component owner = components[i];
                if (owner == null) continue;
                FieldInfo[] fields;
                try { fields = owner.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); }
                catch { continue; }

                for (int f = 0; f < fields.Length; f++)
                {
                    FieldInfo field = fields[f];
                    if (!inputType.IsAssignableFrom(field.FieldType)) continue;
                    Component input;
                    try { input = field.GetValue(owner) as Component; }
                    catch { continue; }
                    if (input == null) continue;

                    int score = ScoreInputField(field.Name, owner.GetType().FullName ?? "", candidate.family);
                    if (score > bestScore)
                    {
                        best = input;
                        bestScore = score;
                    }
                }
            }

            if (best != null) return best;
            Component[] direct = candidate.root.GetComponentsInChildren(inputType, true);
            return direct != null && direct.Length > 0 ? direct[0] : null;
        }

        static int ScoreInputField(string fieldName, string ownerType, string family)
        {
            string n = (fieldName ?? "").ToLowerInvariant();
            int score = 0;
            if (n == "urlinputfield" || n == "urlfield" || n == "urlinput" || n == "_urlinputfield" || n == "videourlinputfield") score += 100;
            if (n.Contains("url")) score += 40;
            if (n.Contains("input")) score += 25;
            if (n.Contains("queue")) score -= 15;
            if (n.Contains("alt")) score -= 8;
            if (n.Contains("top")) score -= 3;

            string type = (ownerType ?? "").ToLowerInvariant();
            if (family == "VideoTXL" && type.Contains("inputproxy")) score += 70;
            if (family == "USharpVideo / ModernUI" && type.Contains("videocontrolhandler")) score += 70;
            if (family == "YamaPlayer" && type.Contains("uicontroller")) score += 70;
            if (family == "VizVid" && type.Contains("uihandler")) score += 70;
            return score;
        }

        static void ConfigureInputHint(Component input)
        {
            if (input == null) return;
            const string hint = "YouTube relay: https://tools.nekosunevr.co.uk/v/VIDEO_ID?vrc=1";

            object placeholder = GetMemberValue(input, "placeholder");
            if (placeholder == null) placeholder = GetMemberValue(input, "m_Placeholder");
            if (placeholder != null && TrySetText(placeholder, hint)) return;

            Transform[] children = input.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null || children[i].name.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Component[] comps = children[i].GetComponents<Component>();
                for (int c = 0; c < comps.Length; c++)
                    if (TrySetText(comps[c], hint)) return;
            }
        }

        static bool TrySetText(object target, string text)
        {
            if (target == null) return false;
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty("text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = type.GetField("text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            try
            {
                UnityEngine.Object unityObject = target as UnityEngine.Object;
                if (unityObject != null) Undo.RecordObject(unityObject, "Configure YouTube proxy hint");
                if (property != null && property.CanWrite && property.PropertyType == typeof(string)) property.SetValue(target, text, null);
                else if (field != null && field.FieldType == typeof(string)) field.SetValue(target, text);
                else return false;
                if (unityObject != null) EditorUtility.SetDirty(unityObject);
                return true;
            }
            catch { return false; }
        }

        static object GetMemberValue(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();
            PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanRead) { try { return p.GetValue(target, null); } catch { } }
            FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) { try { return f.GetValue(target); } catch { } }
            return null;
        }

        static int ConvertSerializedYouTubeUrls(GameObject root)
        {
            if (root == null) return 0;
            Type urlType = FindType("VRC.SDKBase.VRCUrl");
            if (urlType == null) return 0;

            int changedCount = 0;
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null) continue;
                FieldInfo[] fields;
                try { fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); }
                catch { continue; }

                bool objectChanged = false;
                for (int f = 0; f < fields.Length; f++)
                {
                    FieldInfo field = fields[f];
                    if (field.IsInitOnly || field.IsLiteral || field.IsNotSerialized) continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true)) continue;
                    string fieldName = field.Name.ToLowerInvariant();
                    if (!(fieldName.Contains("url") || fieldName.Contains("playlist") || fieldName.Contains("video") || fieldName.Contains("stream"))) continue;

                    if (field.FieldType == urlType)
                    {
                        object value;
                        try { value = field.GetValue(component); } catch { continue; }
                        string raw = GetVrcUrlString(value);
                        string id = ExtractYouTubeId(raw);
                        if (string.IsNullOrEmpty(id)) continue;
                        object replacement = CreateVrcUrl(BuildProxyUrl(id, "auto"));
                        if (replacement == null) continue;
                        if (!objectChanged) Undo.RecordObject(component, "Convert YouTube URL to NekoSune relay");
                        try
                        {
                            field.SetValue(component, replacement);
                            objectChanged = true;
                            changedCount++;
                        }
                        catch { }
                    }
                    else if (field.FieldType.IsArray && field.FieldType.GetElementType() == urlType)
                    {
                        Array array;
                        try { array = field.GetValue(component) as Array; } catch { continue; }
                        if (array == null || array.Length == 0) continue;
                        Array clone = (Array)array.Clone();
                        bool arrayChanged = false;
                        for (int a = 0; a < clone.Length; a++)
                        {
                            object value = clone.GetValue(a);
                            string id = ExtractYouTubeId(GetVrcUrlString(value));
                            if (string.IsNullOrEmpty(id)) continue;
                            object replacement = CreateVrcUrl(BuildProxyUrl(id, "auto"));
                            if (replacement == null) continue;
                            clone.SetValue(replacement, a);
                            arrayChanged = true;
                            changedCount++;
                        }
                        if (arrayChanged)
                        {
                            if (!objectChanged) Undo.RecordObject(component, "Convert YouTube URLs to NekoSune relay");
                            try
                            {
                                field.SetValue(component, clone);
                                objectChanged = true;
                            }
                            catch { }
                        }
                    }
                }

                if (objectChanged)
                {
                    EditorUtility.SetDirty(component);
                    try { PrefabUtility.RecordPrefabInstancePropertyModifications(component); } catch { }
                }
            }
            return changedCount;
        }

        static void TryApplyCommunityStartUrl(Candidate candidate, object startUrl)
        {
            if (candidate == null || !candidate.community || candidate.root == null || startUrl == null) return;
            Type urlType = FindType("VRC.SDKBase.VRCUrl");
            if (urlType == null) return;

            string[] preferred = { "defaultUrl", "defaultURL", "videoURL", "videoUrl", "defaultVideoUrl", "defaultVideoURL" };
            Component[] components = candidate.root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c == null) continue;
                Type t = c.GetType();
                for (int p = 0; p < preferred.Length; p++)
                {
                    FieldInfo field = t.GetField(preferred[p], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null || field.FieldType != urlType || field.IsInitOnly) continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true)) continue;
                    try
                    {
                        Undo.RecordObject(c, "Set NekoSune YouTube start URL");
                        field.SetValue(c, startUrl);
                        EditorUtility.SetDirty(c);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                        return;
                    }
                    catch { }
                }
            }

            // USharpVideo and similar players commonly use a serialized VRCUrl[] playlist.
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c == null) continue;
                FieldInfo[] fields = c.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int f = 0; f < fields.Length; f++)
                {
                    FieldInfo field = fields[f];
                    if (!field.FieldType.IsArray || field.FieldType.GetElementType() != urlType) continue;
                    if (field.Name.IndexOf("playlist", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true)) continue;
                    try
                    {
                        Array old = field.GetValue(c) as Array;
                        int length = old == null || old.Length == 0 ? 1 : old.Length;
                        Array next = Array.CreateInstance(urlType, length);
                        if (old != null)
                            for (int a = 0; a < old.Length && a < length; a++) next.SetValue(old.GetValue(a), a);
                        next.SetValue(startUrl, 0);
                        Undo.RecordObject(c, "Set NekoSune YouTube start playlist URL");
                        field.SetValue(c, next);
                        EditorUtility.SetDirty(c);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                        return;
                    }
                    catch { }
                }
            }
        }

        static Component EnsureBridge(GameObject root, Type runtimeType)
        {
            if (root == null || runtimeType == null) return null;
            Component existing = root.GetComponent(runtimeType);
            if (existing != null) return existing;

            Type extensions = FindType("UdonSharpEditor.UdonSharpComponentExtensions");
            if (extensions == null)
            {
                Debug.LogError("[NekoSune YouTube Proxy] UdonSharpComponentExtensions was not found.");
                return null;
            }

            MethodInfo add = extensions.GetMethod(
                "AddUdonSharpComponent",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(GameObject), typeof(Type) },
                null);
            if (add == null)
            {
                Debug.LogError("[NekoSune YouTube Proxy] AddUdonSharpComponent(GameObject, Type) was not found.");
                return null;
            }

            try
            {
                Component bridge = add.Invoke(null, new object[] { root, runtimeType }) as Component;
                if (bridge != null) Undo.RegisterCompleteObjectUndo(bridge, "Add NekoSune YouTube Proxy bridge");
                return bridge;
            }
            catch (Exception e)
            {
                Debug.LogError("[NekoSune YouTube Proxy] Could not add UdonSharp bridge: " + (e.InnerException ?? e).Message);
                return null;
            }
        }

        static void SetBridgeField(Component bridge, string fieldName, object value)
        {
            if (bridge == null) return;
            FieldInfo field = bridge.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;
            if (value != null && !field.FieldType.IsInstanceOfType(value)) return;
            try
            {
                Undo.RecordObject(bridge, "Configure NekoSune YouTube Proxy");
                field.SetValue(bridge, value);
                EditorUtility.SetDirty(bridge);
                PrefabUtility.RecordPrefabInstancePropertyModifications(bridge);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[NekoSune YouTube Proxy] Could not set " + fieldName + ": " + e.Message);
            }
        }

        static void ApplyProxyToBacking(Component bridge)
        {
            if (bridge == null) return;
            Type utility = FindType("UdonSharpEditor.UdonSharpEditorUtility");
            if (utility == null) return;
            MethodInfo[] methods = utility.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "CopyProxyToUdon") continue;
                ParameterInfo[] ps = method.GetParameters();
                if (ps.Length != 1 || !ps[0].ParameterType.IsAssignableFrom(bridge.GetType())) continue;
                try { method.Invoke(null, new object[] { bridge }); }
                catch (Exception e) { Debug.LogWarning("[NekoSune YouTube Proxy] Could not copy proxy to Udon backing: " + (e.InnerException ?? e).Message); }
                return;
            }
        }

        static Component FindFirstComponent(GameObject root, string fullTypeName)
        {
            if (root == null) return null;
            Type type = FindType(fullTypeName);
            if (type == null) return null;
            Component direct = root.GetComponent(type);
            if (direct != null) return direct;
            Component[] children = root.GetComponentsInChildren(type, true);
            return children != null && children.Length > 0 ? children[0] : null;
        }

        static string GetVrcUrlString(object value)
        {
            if (value == null) return "";
            try
            {
                MethodInfo get = value.GetType().GetMethod("Get", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                object raw = get != null ? get.Invoke(value, null) : null;
                return raw as string ?? "";
            }
            catch { return ""; }
        }

        static object CreateVrcUrl(string url)
        {
            Type type = FindType("VRC.SDKBase.VRCUrl");
            if (type == null) return null;
            try { return Activator.CreateInstance(type, new object[] { url }); }
            catch { return null; }
        }

        internal static string BuildProxyUrl(string id, string quality)
        {
            string value = ProxyOrigin + "/v/" + id + "?";
            if (!string.IsNullOrEmpty(quality) && quality != "auto") value += "q=" + quality + "&";
            value += "vrc=1";
            return value;
        }

        internal static string ExtractYouTubeId(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.Trim();
            if (IsVideoId(value)) return value;

            string lower = value.ToLowerInvariant();
            if (lower.IndexOf("youtube.com", StringComparison.Ordinal) < 0 && lower.IndexOf("youtu.be", StringComparison.Ordinal) < 0)
                return null;

            string[] markers = { "youtu.be/", "youtube.com/shorts/", "youtube.com/live/", "youtube.com/embed/", "youtube.com/watch?v=" };
            for (int i = 0; i < markers.Length; i++)
            {
                int p = value.IndexOf(markers[i], StringComparison.OrdinalIgnoreCase);
                if (p < 0) continue;
                string id = TakeId(value.Substring(p + markers[i].Length));
                if (IsVideoId(id)) return id;
            }

            int query = value.IndexOf("v=", StringComparison.OrdinalIgnoreCase);
            if (query >= 0)
            {
                string id = TakeId(value.Substring(query + 2));
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

        static GameObject ResolveSelectedObject()
        {
            string value = SessionState.GetString(SelectedObjectKey, "");
            if (string.IsNullOrEmpty(value)) return Selection.activeGameObject;
            try
            {
                GlobalObjectId id;
                if (GlobalObjectId.TryParse(value, out id))
                    return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
            }
            catch { }
            return Selection.activeGameObject;
        }

        static string BuildSummary(SetupStats stats)
        {
            if (stats == null) return "No setup result was produced.";
            string value = "Detected players: " + stats.detected +
                           "\nBridges configured: " + stats.bridges +
                           "\nURL inputs found: " + stats.inputs +
                           "\nCreator-time YouTube URLs converted: " + stats.convertedUrls +
                           "\nProgram asset: " + GeneratedProgramPath;

            if (stats.families.Count > 0)
            {
                value += "\n\nPlayer families:";
                var names = new List<string>(stats.families.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < names.Count; i++) value += "\n• " + names[i] + ": " + stats.families[names[i]];
            }
            value += "\n\nNon-YouTube URLs were left unchanged.";
            return value;
        }

        static void ClearPending()
        {
            SessionState.EraseBool(PendingKey);
            SessionState.EraseBool(RepairOnlyKey);
            SessionState.EraseString(SelectedObjectKey);
            SessionState.EraseInt(AttemptKey);
        }

        static void Fail(string message)
        {
            ClearPending();
            Debug.LogError("[NekoSune YouTube Proxy] " + message);
            EditorUtility.DisplayDialog("NekoSune YouTube Proxy", message, "OK");
        }
    }
}
