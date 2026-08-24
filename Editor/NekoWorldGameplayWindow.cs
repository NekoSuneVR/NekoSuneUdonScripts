using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NekoSune.Worlds.Editor;
using UnityEditor;
using UnityEngine;

namespace NekoSune.WorldGameplay.Editor
{
    [NekoAddon(Order = 30)]
    public sealed class NekoWorldGameplayAddon : INekoAddon
    {
        public string Id { get { return "world-gameplay"; } }
        public string TitleKey { get { return "World Gameplay"; } }
        public string DescriptionKey { get { return "Build VRChat persistence, inventory/currency schemas and AI navigation starters without writing the boilerplate by hand."; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "G"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoWorldGameplayWindow.Open(); }
    }

    internal sealed class NekoWorldGameplayWindow : EditorWindow
    {
        enum PersistType { Int, Float, Bool, String }
        [Serializable] sealed class KeyDef { public string name = "coins"; public PersistType type = PersistType.Int; public string defaultValue = "0"; public string note = "Currency"; }

        readonly List<KeyDef> _keys = new List<KeyDef>();
        Vector2 _scroll;
        int _tab;
        string _systemName = "NekoGame";
        string _prefix = "nekogame_";
        string _status = "Choose a preset or define your own persistent keys.";

        [MenuItem("NekoSune/World/Gameplay", false, 20)]
        public static void Open()
        {
            var w = GetWindow<NekoWorldGameplayWindow>(false, "World Gameplay", true);
            w.minSize = new Vector2(720f, 560f);
            w.Show();
        }

        void OnEnable() { if (_keys.Count == 0) LoadClickerPreset(); }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("World Gameplay", "NekoSune", "Persistence, inventories, save data and AI Navigation starters for VRChat worlds");
            _tab = GUILayout.Toolbar(_tab, new[] { "Persistence Builder", "AI Navigation", "Starter Schemas" });
            NekoStyles.Rule(2f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == 0) DrawPersistence(); else if (_tab == 1) DrawAi(); else DrawPresets();
            EditorGUILayout.EndScrollView();
        }

        void DrawPersistence()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Persistent system", NekoStyles.SlotName);
            _systemName = EditorGUILayout.TextField("System / class name", _systemName);
            _prefix = EditorGUILayout.TextField("PlayerData key prefix", _prefix);
            EditorGUILayout.HelpBox("VRChat recommends unique prefixes because PlayerData is shared by every prefab/script in the world. Generated code waits for OnPlayerRestored before reading or writing.", MessageType.Info);
            EditorGUILayout.EndVertical();

            GUILayout.Label("Keys", EditorStyles.boldLabel);
            for (int i = 0; i < _keys.Count; i++)
            {
                var k = _keys[i];
                EditorGUILayout.BeginVertical(NekoStyles.Card);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label((i + 1) + ". " + k.name, NekoStyles.SlotName);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(65f))) { _keys.RemoveAt(i); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
                EditorGUILayout.EndHorizontal();
                k.name = EditorGUILayout.TextField("Name", k.name);
                k.type = (PersistType)EditorGUILayout.EnumPopup("Type", k.type);
                k.defaultValue = EditorGUILayout.TextField("Default", k.defaultValue);
                k.note = EditorGUILayout.TextField("Purpose", k.note);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("+ Add persistent key")) _keys.Add(new KeyDef { name = "newValue", type = PersistType.Int, defaultValue = "0", note = "" });

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("What gets generated", NekoStyles.SlotName);
            EditorGUILayout.LabelField("• OnPlayerRestored gate\n• default initialization\n• typed Get/Set helpers\n• Add helpers for numeric keys\n• persistence usage display\n• storage warning callbacks\n• ResetToDefaults helper", NekoStyles.WrapLabel);
            EditorGUILayout.HelpBox(_status, MessageType.None);
            if (GUILayout.Button("Generate UdonSharp Persistence System", NekoStyles.PrimaryButton, GUILayout.Height(34f))) GeneratePersistence();
            EditorGUILayout.EndVertical();
        }

        void DrawAi()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("AI Navigation starter", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Creates a simple NPC root with three editable patrol waypoints and copies a readable UdonSharp NavMeshAgent patrol script into Assets.", NekoStyles.WrapLabel);
            EditorGUILayout.HelpBox("Bake a NavMesh Surface before testing. The starter intentionally uses the default agent setup and leaves game-specific behaviour editable.", MessageType.Info);
            if (GUILayout.Button("Create Patrol NPC Starter", NekoStyles.PrimaryButton, GUILayout.Height(34f))) CreateAiStarter();
            EditorGUILayout.EndVertical();
        }

        void DrawPresets()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Starter persistence schemas", NekoStyles.SlotName);
            EditorGUILayout.LabelField("These presets fill the Persistence Builder. Edit them before generating code.", NekoStyles.WrapLabel);
            if (GUILayout.Button("Clicker / Currency")) { LoadClickerPreset(); _tab = 0; }
            if (GUILayout.Button("Idle / Incremental")) { LoadIdlePreset(); _tab = 0; }
            if (GUILayout.Button("Flappy-style High Score")) { LoadFlappyPreset(); _tab = 0; }
            if (GUILayout.Button("Simple RPG / Inventory")) { LoadRpgPreset(); _tab = 0; }
            EditorGUILayout.EndVertical();
        }

        void GeneratePersistence()
        {
            if (_keys.Count == 0) { EditorUtility.DisplayDialog("Persistence Builder", "Add at least one key.", "OK"); return; }
            string className = SafeIdentifier(_systemName) + "Persistence";
            string folder = EnsureFolder("Assets/NekoSune/Gameplay/Generated");
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + className + ".cs");
            File.WriteAllText(ToAbsolute(path), BuildPersistenceSource(className), Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
            _status = "Generated " + path + ". Attach the compiled UdonSharp behaviour to a GameObject and optionally assign debugText.";
            EditorUtility.DisplayDialog("Persistence Builder", _status, "OK");
        }

        string BuildPersistenceSource(string className)
        {
            var s = new StringBuilder();
            s.AppendLine("using UdonSharp;");
            s.AppendLine("using UnityEngine;");
            s.AppendLine("using UnityEngine.UI;");
            s.AppendLine("using VRC.SDK3.Persistence;");
            s.AppendLine("using VRC.SDKBase;");
            s.AppendLine();
            s.AppendLine("public class " + className + " : UdonSharpBehaviour");
            s.AppendLine("{");
            s.AppendLine("    public Text debugText;");
            s.AppendLine("    private bool _ready;");
            for (int i = 0; i < _keys.Count; i++) s.AppendLine("    private const string Key_" + SafeIdentifier(_keys[i].name) + " = \"" + Escape(_prefix + _keys[i].name) + "\";");
            s.AppendLine();
            s.AppendLine("    public override void OnPlayerRestored(VRCPlayerApi player)");
            s.AppendLine("    {");
            s.AppendLine("        if (!player.isLocal) return;");
            s.AppendLine("        _ready = true;");
            s.AppendLine("        EnsureDefaults();");
            s.AppendLine("        Networking.RequestStorageUsageUpdate();");
            s.AppendLine("        RefreshDebug();");
            s.AppendLine("    }");
            s.AppendLine();
            s.AppendLine("    private void EnsureDefaults()");
            s.AppendLine("    {");
            for (int i = 0; i < _keys.Count; i++)
            {
                KeyDef k = _keys[i]; string id = SafeIdentifier(k.name);
                s.AppendLine("        if (!PlayerData.HasKey(Networking.LocalPlayer, Key_" + id + ")) PlayerData.Set" + k.type + "(Key_" + id + ", " + DefaultLiteral(k) + ");");
            }
            s.AppendLine("    }");
            s.AppendLine();
            for (int i = 0; i < _keys.Count; i++)
            {
                KeyDef k = _keys[i]; string id = SafeIdentifier(k.name); string type = CsType(k.type);
                s.AppendLine("    public " + type + " Get" + id + "() { return PlayerData.Get" + k.type + "(Networking.LocalPlayer, Key_" + id + "); }");
                s.AppendLine("    public void Set" + id + "(" + type + " value) { if (!_ready) return; PlayerData.Set" + k.type + "(Key_" + id + ", value); RefreshDebug(); }");
                if (k.type == PersistType.Int) s.AppendLine("    public void Add" + id + "(int amount) { Set" + id + "(Get" + id + "() + amount); }");
                if (k.type == PersistType.Float) s.AppendLine("    public void Add" + id + "(float amount) { Set" + id + "(Get" + id + "() + amount); }");
                s.AppendLine();
            }
            s.AppendLine("    public void ResetToDefaults()");
            s.AppendLine("    {");
            s.AppendLine("        if (!_ready) return;");
            for (int i = 0; i < _keys.Count; i++) { KeyDef k = _keys[i]; s.AppendLine("        PlayerData.Set" + k.type + "(Key_" + SafeIdentifier(k.name) + ", " + DefaultLiteral(k) + ");"); }
            s.AppendLine("        RefreshDebug();");
            s.AppendLine("    }");
            s.AppendLine();
            s.AppendLine("    public override void OnPersistenceUsageUpdated(VRCPlayerApi player) { if (player.isLocal) RefreshDebug(); }");
            s.AppendLine("    public override void OnPlayerDataStorageWarning(VRCPlayerApi player) { if (player.isLocal) Debug.LogWarning(\"[NekoSune Persistence] PlayerData storage is nearing the limit.\"); }");
            s.AppendLine("    public override void OnPlayerDataStorageExceeded(VRCPlayerApi player) { if (player.isLocal) Debug.LogError(\"[NekoSune Persistence] PlayerData storage limit exceeded. New data may not save.\"); }");
            s.AppendLine();
            s.AppendLine("    public void RefreshDebug()");
            s.AppendLine("    {");
            s.AppendLine("        if (debugText == null || !_ready) return;");
            s.AppendLine("        int used = Networking.GetPlayerDataStorageUsage(Networking.LocalPlayer);");
            s.AppendLine("        int limit = Networking.GetPlayerDataStorageLimit();");
            s.AppendLine("        debugText.text = \"Persistence ready\\nStorage: \" + used + \" / \" + limit + \" bytes\";");
            s.AppendLine("    }");
            s.AppendLine("}");
            return s.ToString();
        }

        void CreateAiStarter()
        {
            GameObject root = new GameObject("Neko AI Patrol NPC");
            Undo.RegisterCreatedObjectUndo(root, "Create AI patrol starter");
            Type agentType = FindType("UnityEngine.AI.NavMeshAgent");
            if (agentType != null) Undo.AddComponent(root, agentType);
            GameObject points = new GameObject("Waypoints"); points.transform.SetParent(root.transform, false);
            for (int i = 0; i < 3; i++) { GameObject p = new GameObject("Point " + (i + 1)); p.transform.SetParent(points.transform, false); p.transform.localPosition = new Vector3(i * 2f, 0f, i % 2 == 0 ? 0f : 2f); }
            string folder = EnsureFolder("Assets/NekoSune/Gameplay/Generated");
            string template = FindPackageFile("Templates/Runtime/NekoAiPatrol.cs.txt");
            if (!string.IsNullOrEmpty(template)) File.Copy(template, ToAbsolute(folder + "/NekoAiPatrol.cs"), true);
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("AI Navigation", "Created an NPC/waypoint starter and copied NekoAiPatrol.cs. After UdonSharp compiles, attach it to the NPC and assign Point 1-3 as waypoints.", "OK");
        }

        void LoadClickerPreset() { SetPreset("NekoClicker", "clicker_", new[] { K("coins", PersistType.Int, "0", "Spendable currency"), K("clicks", PersistType.Int, "0", "Lifetime clicks"), K("bestCombo", PersistType.Int, "0", "Best combo") }); }
        void LoadIdlePreset() { SetPreset("NekoIdle", "idle_", new[] { K("coins", PersistType.Float, "0", "Current coins"), K("coinsPerSecond", PersistType.Float, "1", "Production rate"), K("upgradeLevel", PersistType.Int, "0", "Upgrade level"), K("lifetimeCoins", PersistType.Float, "0", "Lifetime production") }); }
        void LoadFlappyPreset() { SetPreset("NekoFlappy", "flappy_", new[] { K("bestScore", PersistType.Int, "0", "Persistent high score"), K("runs", PersistType.Int, "0", "Total attempts"), K("medals", PersistType.Int, "0", "Earned medals") }); }
        void LoadRpgPreset() { SetPreset("NekoRpg", "rpg_", new[] { K("coins", PersistType.Int, "0", "Currency"), K("xp", PersistType.Int, "0", "Experience"), K("level", PersistType.Int, "1", "Level"), K("hasSword", PersistType.Bool, "false", "Inventory unlock"), K("playerName", PersistType.String, "Adventurer", "Optional display name") }); }
        void SetPreset(string name, string prefix, KeyDef[] keys) { _systemName = name; _prefix = prefix; _keys.Clear(); _keys.AddRange(keys); _status = "Loaded " + name + " schema."; Repaint(); }
        static KeyDef K(string n, PersistType t, string d, string note) { return new KeyDef { name = n, type = t, defaultValue = d, note = note }; }

        static string CsType(PersistType t) { if (t == PersistType.Int) return "int"; if (t == PersistType.Float) return "float"; if (t == PersistType.Bool) return "bool"; return "string"; }
        static string DefaultLiteral(KeyDef k)
        {
            if (k.type == PersistType.String) return "\"" + Escape(k.defaultValue) + "\"";
            if (k.type == PersistType.Bool) return string.Equals(k.defaultValue, "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
            float f; if (!float.TryParse(k.defaultValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) f = 0f;
            if (k.type == PersistType.Int) return Mathf.RoundToInt(f).ToString();
            return f.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) + "f";
        }
        static string SafeIdentifier(string value) { if (string.IsNullOrEmpty(value)) return "Value"; var b = new StringBuilder(); if (!char.IsLetter(value[0]) && value[0] != '_') b.Append('_'); for (int i = 0; i < value.Length; i++) b.Append(char.IsLetterOrDigit(value[i]) || value[i] == '_' ? value[i] : '_'); return b.ToString(); }
        static string Escape(string value) { return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }
        static string EnsureFolder(string path) { string[] parts = path.Split('/'); string cur = parts[0]; for (int i = 1; i < parts.Length; i++) { string next = cur + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]); cur = next; } return cur; }
        static string ToAbsolute(string assetPath) { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar)); }
        static Type FindType(string fullName) { foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) { Type t = a.GetType(fullName, false); if (t != null) return t; } return null; }
        static string FindPackageFile(string relative) { string[] roots = Directory.GetDirectories("Packages", "com.nekosune.world-gameplay", SearchOption.TopDirectoryOnly); string path = roots.Length > 0 ? Path.Combine(roots[0], relative.Replace('/', Path.DirectorySeparatorChar)) : null; return !string.IsNullOrEmpty(path) && File.Exists(path) ? Path.GetFullPath(path) : null; }
    }
}
