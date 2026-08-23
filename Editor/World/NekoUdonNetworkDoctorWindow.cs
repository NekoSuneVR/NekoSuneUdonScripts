using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 10)]
    internal sealed class NekoUdonNetworkDoctorAddon : INekoAddon
    {
        public string Id { get { return "udon-network-doctor"; } }
        public string TitleKey { get { return "network.title"; } }
        public string DescriptionKey { get { return "network.desc"; } }
        public string CategoryKey { get { return "cat.udon"; } }
        public string Glyph { get { return "N"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoUdonNetworkDoctorWindow.Open(); }
    }

    internal sealed class NekoUdonNetworkDoctorWindow : EditorWindow
    {
        enum Severity
        {
            Error,
            Warning,
            Info
        }

        sealed class Finding
        {
            public Severity Severity;
            public string Title;
            public string Detail;
            public UnityEngine.Object Target;
            public string AssetPath;
        }

        sealed class ScriptStats
        {
            public int UdonSharpScripts;
            public int GraphOrCompiledUdon;
            public int SyncedFields;
            public int NetworkEventCalls;
            public int OwnershipCalls;
            public int ManualScripts;
            public int ContinuousScripts;
            public int ExplicitNoneScripts;
        }

        Vector2 _scroll;
        List<Finding> _findings = new List<Finding>();
        ScriptStats _stats;
        string _sceneName = "";
        DateTime _scanTime;

        static readonly Regex SyncedAttribute = new Regex(@"\[\s*UdonSynced(?:\s*\([^\]]*\))?\s*\]", RegexOptions.Compiled);
        static readonly Regex ManualMode = new Regex(@"UdonBehaviourSyncMode\s*\(\s*BehaviourSyncMode\.Manual\s*\)", RegexOptions.Compiled);
        static readonly Regex ContinuousMode = new Regex(@"UdonBehaviourSyncMode\s*\(\s*BehaviourSyncMode\.Continuous\s*\)", RegexOptions.Compiled);
        static readonly Regex NoneMode = new Regex(@"UdonBehaviourSyncMode\s*\(\s*BehaviourSyncMode\.None\s*\)", RegexOptions.Compiled);
        static readonly Regex NoVariableSyncMode = new Regex(@"UdonBehaviourSyncMode\s*\(\s*BehaviourSyncMode\.NoVariableSync\s*\)", RegexOptions.Compiled);
        static readonly Regex SendNetworkEvent = new Regex(@"\bSendCustomNetworkEvent\s*\(", RegexOptions.Compiled);
        static readonly Regex NetworkCallable = new Regex(@"\[\s*NetworkCallable(?:\s*\([^\]]*\))?\s*\]", RegexOptions.Compiled);
        static readonly Regex SetOwner = new Regex(@"\bNetworking\.SetOwner\s*\(", RegexOptions.Compiled);
        static readonly Regex RequestSerialization = new Regex(@"\bRequestSerialization\s*\(", RegexOptions.Compiled);
        static readonly Regex OwnershipCallback = new Regex(@"\bOnOwnershipTransferred\s*\(|\bOnOwnershipRequest\s*\(", RegexOptions.Compiled);
        static readonly Regex SyncedContainer = new Regex(@"\[\s*UdonSynced(?:\s*\([^\]]*\))?\s*\][\s\S]{0,220}?\b(DataList|DataDictionary)\b", RegexOptions.Compiled);
        static readonly Regex SyncedHeavyField = new Regex(@"\[\s*UdonSynced(?:\s*\([^\]]*\))?\s*\][\s\S]{0,180}?\b(string\s*\[|string\b|\w+\s*\[)", RegexOptions.Compiled);

        [MenuItem(NekoPaths.MenuRoot + "World/Udon Network Doctor", false, 1)]
        public static void Open()
        {
            var window = GetWindow<NekoUdonNetworkDoctorWindow>(false, "Udon Network Doctor", true);
            window.minSize = new Vector2(680f, 500f);
            window.Show();
        }

        void OnEnable()
        {
            Scan();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header("Udon Network Doctor", "Sync modes, ownership, serialization, and network-event source checks");

            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Udon", NekoStyles.PrimaryButton, GUILayout.Width(120f)))
                Scan();
            if (GUILayout.Button("Copy report", GUILayout.Width(100f)))
                EditorGUIUtility.systemCopyBuffer = BuildReport();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Deep checks work on UdonSharp C# source attached to the active scene. Udon Graph/compiled Udon is counted, but its graph internals are not parsed. Findings are static analysis: always verify behaviour with VRChat multi-client Build & Test.",
                MessageType.None);

            if (_stats == null)
            {
                EditorGUILayout.HelpBox("No scan available.", MessageType.Info);
                return;
            }

            DrawSummary();
            GUILayout.Space(8f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawFindings(Severity.Error, "Errors");
            DrawFindings(Severity.Warning, "Warnings");
            DrawFindings(Severity.Info, "Recommendations");
            EditorGUILayout.EndScrollView();
        }

        void DrawSummary()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Network summary", NekoStyles.CardTitle);
            EditorGUILayout.LabelField("Scene", string.IsNullOrEmpty(_sceneName) ? "Untitled" : _sceneName);
            EditorGUILayout.LabelField("UdonSharp scripts", _stats.UdonSharpScripts.ToString());
            EditorGUILayout.LabelField("Graph / compiled Udon behaviours", _stats.GraphOrCompiledUdon.ToString());
            EditorGUILayout.LabelField("[UdonSynced] fields", _stats.SyncedFields.ToString());
            EditorGUILayout.LabelField("Network event calls / [NetworkCallable]", _stats.NetworkEventCalls.ToString());
            EditorGUILayout.LabelField("Networking.SetOwner calls", _stats.OwnershipCalls.ToString());
            EditorGUILayout.LabelField("Manual / Continuous / None", _stats.ManualScripts + " / " + _stats.ContinuousScripts + " / " + _stats.ExplicitNoneScripts);
            EditorGUILayout.EndVertical();
        }

        void DrawFindings(Severity severity, string heading)
        {
            int count = 0;
            for (int i = 0; i < _findings.Count; i++)
                if (_findings[i].Severity == severity) count++;
            if (count == 0) return;

            GUILayout.Label(heading + " (" + count + ")", EditorStyles.boldLabel);
            for (int i = 0; i < _findings.Count; i++)
            {
                Finding finding = _findings[i];
                if (finding.Severity != severity) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(finding.Title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (finding.Target != null && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(58f)))
                {
                    Selection.activeObject = finding.Target;
                    EditorGUIUtility.PingObject(finding.Target);
                }
                if (!string.IsNullOrEmpty(finding.AssetPath) && GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(finding.AssetPath);
                    if (asset != null) AssetDatabase.OpenAsset(asset);
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label(finding.Detail, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(5f);
        }

        void Scan()
        {
            _findings = new List<Finding>();
            _stats = new ScriptStats();
            _scanTime = DateTime.Now;

            Scene scene = SceneManager.GetActiveScene();
            _sceneName = scene.IsValid() ? scene.name : "";
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Add(Severity.Error, "No loaded scene", "Open a VRChat world scene before running Udon Network Doctor.", null, null);
                Repaint();
                return;
            }

            List<MonoBehaviour> behaviours = GetSceneBehaviours(scene);
            var analysedScripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < behaviours.Count; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;

                Type type = behaviour.GetType();
                if (IsSubclassNamed(type, "UdonSharp.UdonSharpBehaviour"))
                {
                    MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
                    string assetPath = script == null ? null : AssetDatabase.GetAssetPath(script);
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    string key = assetPath;
                    if (!analysedScripts.Add(key)) continue;
                    _stats.UdonSharpScripts++;
                    AnalyseSource(behaviour, assetPath);
                }
                else if ((type.FullName ?? type.Name) == "VRC.Udon.UdonBehaviour")
                {
                    _stats.GraphOrCompiledUdon++;
                }
            }

            if (_stats.UdonSharpScripts == 0 && _stats.GraphOrCompiledUdon == 0)
            {
                Add(Severity.Info, "No Udon found", "No UdonSharp or UdonBehaviour components were found in the active scene.", null, null);
            }
            else
            {
                Add(Severity.Info, "Use multi-client Build & Test", "VRChat documents that synced variables and network events do not behave fully in ordinary Unity Play Mode. Test networking with at least two local VRChat clients before publishing. [VRChat guidance]", null, null);
            }

            if (_stats.GraphOrCompiledUdon > 0)
            {
                Add(Severity.Info, "Udon Graph requires runtime testing", _stats.GraphOrCompiledUdon + " Udon Graph/compiled behaviour(s) were found. Their graph internals are not source-scanned, so use VRChat's graph status/assembly view and multi-client Build & Test for these behaviours.", null, null);
            }

            Repaint();
        }

        void AnalyseSource(MonoBehaviour target, string assetPath)
        {
            string absolute = ToAbsoluteAssetPath(assetPath);
            if (string.IsNullOrEmpty(absolute) || !File.Exists(absolute)) return;

            string source;
            try { source = File.ReadAllText(absolute); }
            catch (Exception e)
            {
                Add(Severity.Warning, "Could not read UdonSharp source", assetPath + ": " + e.Message, target, assetPath);
                return;
            }

            int synced = SyncedAttribute.Matches(source).Count;
            int networkEvents = SendNetworkEvent.Matches(source).Count + NetworkCallable.Matches(source).Count;
            int ownership = SetOwner.Matches(source).Count;
            bool manual = ManualMode.IsMatch(source);
            bool continuous = ContinuousMode.IsMatch(source);
            bool none = NoneMode.IsMatch(source);
            bool noVariable = NoVariableSyncMode.IsMatch(source);
            bool requestsSerialization = RequestSerialization.IsMatch(source);
            bool ownershipCallback = OwnershipCallback.IsMatch(source);
            bool syncedContainer = SyncedContainer.IsMatch(source);
            bool syncedHeavy = SyncedHeavyField.IsMatch(source);

            _stats.SyncedFields += synced;
            _stats.NetworkEventCalls += networkEvents;
            _stats.OwnershipCalls += ownership;
            if (manual) _stats.ManualScripts++;
            if (continuous) _stats.ContinuousScripts++;
            if (none) _stats.ExplicitNoneScripts++;

            string scriptName = Path.GetFileName(assetPath);

            if (syncedContainer)
            {
                Add(Severity.Error,
                    "DataList/DataDictionary marked [UdonSynced]",
                    scriptName + " appears to directly sync a DataList or DataDictionary. VRChat documentation says Data Lists/Dictionaries cannot be directly synced; serialize them (for example to JSON) into a supported synced value instead. [VRChat rule]",
                    target,
                    assetPath);
            }

            if (manual && synced > 0 && !requestsSerialization)
            {
                Add(Severity.Warning,
                    "Manual sync has no RequestSerialization()",
                    scriptName + " declares " + synced + " synced field(s) with BehaviourSyncMode.Manual but no RequestSerialization() call was found in this source file. Verify that another component is not responsible for requesting serialization.",
                    target,
                    assetPath);
            }

            if (continuous && synced >= 8)
            {
                Add(Severity.Warning,
                    "Many continuously synced fields",
                    scriptName + " has " + synced + " [UdonSynced] field(s) in Continuous mode. Continuous sync updates frequently and can consume unnecessary bandwidth; consider Manual sync for state that changes occasionally. [NekoSune advisory]",
                    target,
                    assetPath);
            }

            if (continuous && syncedHeavy)
            {
                Add(Severity.Warning,
                    "Continuous sync includes string/array-like data",
                    scriptName + " appears to continuously sync a string or array-like field. Review payload size and update frequency; Manual sync or a compact serialized representation may be more appropriate. [NekoSune advisory]",
                    target,
                    assetPath);
            }

            if (none && networkEvents > 0)
            {
                Add(Severity.Error,
                    "Network calls used with BehaviourSyncMode.None",
                    scriptName + " uses network events/[NetworkCallable] while declaring BehaviourSyncMode.None. VRChat documents that network calling does not work on behaviours using None. Use NoVariableSync when you need network events without synced variables. [VRChat rule]",
                    target,
                    assetPath);
            }

            if (noVariable && synced > 0)
            {
                Add(Severity.Error,
                    "Synced fields used with NoVariableSync",
                    scriptName + " declares [UdonSynced] fields while using BehaviourSyncMode.NoVariableSync, which is intended to prohibit variable sync while still allowing network calls. [VRChat rule]",
                    target,
                    assetPath);
            }

            if (ownership > 0 && !ownershipCallback)
            {
                Add(Severity.Info,
                    "Ownership is changed without an ownership callback",
                    scriptName + " calls Networking.SetOwner() but no OnOwnershipTransferred/OnOwnershipRequest callback was found in the same source. That can be valid, but explicit ownership handling usually makes multiplayer state easier to reason about. [NekoSune advisory]",
                    target,
                    assetPath);
            }

            if (synced > 0 && !manual && !continuous && !none && !noVariable)
            {
                Add(Severity.Info,
                    "Sync mode is not explicit in source",
                    scriptName + " has " + synced + " synced field(s), but no [UdonBehaviourSyncMode(...)] attribute was found. Explicit sync modes make networking intent clearer and enable more validation in UdonSharp. [NekoSune advisory]",
                    target,
                    assetPath);
            }

            if (networkEvents > 0 && synced == 0 && !none && !noVariable)
            {
                Add(Severity.Info,
                    "Network-only behaviour can use NoVariableSync",
                    scriptName + " sends/receives network calls but has no synced fields. BehaviourSyncMode.NoVariableSync can document that intent and prevent accidental variable sync. [NekoSune advisory]",
                    target,
                    assetPath);
            }
        }

        static List<MonoBehaviour> GetSceneBehaviours(Scene scene)
        {
            var result = new List<MonoBehaviour>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                MonoBehaviour[] items = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (int n = 0; n < items.Length; n++)
                    if (items[n] != null) result.Add(items[n]);
            }
            return result;
        }

        static bool IsSubclassNamed(Type type, string fullName)
        {
            Type current = type;
            while (current != null)
            {
                if (current.FullName == fullName) return true;
                current = current.BaseType;
            }
            return false;
        }

        static string ToAbsoluteAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (Path.IsPathRooted(assetPath)) return assetPath;
            DirectoryInfo root = Directory.GetParent(Application.dataPath);
            return root == null ? null : Path.GetFullPath(Path.Combine(root.FullName, assetPath));
        }

        void Add(Severity severity, string title, string detail, UnityEngine.Object target, string assetPath)
        {
            _findings.Add(new Finding
            {
                Severity = severity,
                Title = title,
                Detail = detail,
                Target = target,
                AssetPath = assetPath
            });
        }

        string BuildReport()
        {
            if (_stats == null) return "NekoSune Udon Network Doctor: no scan available.";
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune Udon Network Doctor");
            sb.AppendLine("Scene: " + _sceneName);
            sb.AppendLine("Scanned: " + _scanTime.ToString("u"));
            sb.AppendLine();
            sb.AppendLine("UdonSharp scripts: " + _stats.UdonSharpScripts);
            sb.AppendLine("Graph/compiled Udon: " + _stats.GraphOrCompiledUdon);
            sb.AppendLine("Synced fields: " + _stats.SyncedFields);
            sb.AppendLine("Network calls: " + _stats.NetworkEventCalls);
            sb.AppendLine("Ownership calls: " + _stats.OwnershipCalls);
            sb.AppendLine("Manual/Continuous/None: " + _stats.ManualScripts + "/" + _stats.ContinuousScripts + "/" + _stats.ExplicitNoneScripts);
            sb.AppendLine();

            for (int s = 0; s < 3; s++)
            {
                Severity severity = (Severity)s;
                sb.AppendLine(severity.ToString().ToUpperInvariant());
                bool any = false;
                for (int i = 0; i < _findings.Count; i++)
                {
                    Finding finding = _findings[i];
                    if (finding.Severity != severity) continue;
                    any = true;
                    sb.AppendLine("- " + finding.Title + ": " + finding.Detail);
                }
                if (!any) sb.AppendLine("- none");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
