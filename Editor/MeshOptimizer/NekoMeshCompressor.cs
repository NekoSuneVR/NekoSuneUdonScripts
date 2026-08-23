using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 30)]
    internal sealed class NekoMeshCompressorAddon : INekoAddon
    {
        public string Id { get { return "mesh-compressor"; } }
        public string TitleKey { get { return "mesh.title"; } }
        public string DescriptionKey { get { return "mesh.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "M"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoMeshCompressorWindow.Open(); }
    }

    internal sealed class NekoMeshCompressorWindow : EditorWindow
    {
        enum CompressionPreset
        {
            Lossless,
            Balanced,
            Smaller,
            Quest
        }

        sealed class MeshEntry
        {
            public GameObject GameObject;
            public Renderer Renderer;
            public MeshFilter MeshFilter;
            public SkinnedMeshRenderer SkinnedRenderer;
            public Mesh Mesh;
            public string AssetPath;
            public int Vertices;
            public long Triangles;
            public int SubMeshes;
            public int MaterialSlots;
            public int BlendShapes;
            public int Bones;
            public bool Readable;
            public int DuplicateMaterialSlots;
            public int DegenerateTriangles;
            public bool DegenerateScanned;
        }

        GameObject _avatarRoot;
        CompressionPreset _preset = CompressionPreset.Balanced;
        bool _optimizeImporter = true;
        bool _scanDegenerates = true;
        Vector2 _scroll;
        readonly List<MeshEntry> _meshes = new List<MeshEntry>();
        long _totalTriangles;
        long _totalVertices;
        int _totalMaterialSlots;
        int _totalBlendShapes;
        int _skinnedMeshes;
        int _basicMeshes;
        int _duplicateMaterialSlots;
        int _degenerateTriangles;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Mesh Compressor", false, 25)]
        public static void Open()
        {
            var window = GetWindow<NekoMeshCompressorWindow>(false, "Mesh Compressor", true);
            window.minSize = new Vector2(720f, 520f);
            window.Show();
        }

        void OnEnable()
        {
            TryUseSelection();
            Scan();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Mesh Compressor", "NekoSune", "Non-destructive mesh cleanup, import compression, and Quest readiness");

            GUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _avatarRoot = (GameObject)EditorGUILayout.ObjectField("Avatar root", _avatarRoot, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();

            EditorGUILayout.BeginHorizontal();
            _preset = (CompressionPreset)EditorGUILayout.EnumPopup("Compression preset", _preset);
            if (GUILayout.Button("Use selection", GUILayout.Width(105f)))
            {
                TryUseSelection();
                Scan();
            }
            if (GUILayout.Button("Rescan", NekoStyles.PrimaryButton, GUILayout.Width(90f))) Scan();
            EditorGUILayout.EndHorizontal();

            _optimizeImporter = EditorGUILayout.ToggleLeft("Enable safe model-import cache optimization", _optimizeImporter);
            _scanDegenerates = EditorGUILayout.ToggleLeft("Deep-scan readable meshes for degenerate triangles", _scanDegenerates);

            DrawPresetHelp();

            if (_avatarRoot == null)
            {
                EditorGUILayout.HelpBox("Select or drag an avatar root to inspect its meshes.", MessageType.Info);
                return;
            }

            DrawSummary();
            DrawActions();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawMeshList();
            EditorGUILayout.EndScrollView();
        }

        void DrawPresetHelp()
        {
            string text;
            switch (_preset)
            {
                case CompressionPreset.Lossless:
                    text = "Lossless: leaves Unity mesh compression Off. Cleanup can still merge identical-material submeshes and remove invalid zero-area triangles.";
                    break;
                case CompressionPreset.Balanced:
                    text = "Balanced: Low Unity mesh compression. Good default when you want smaller build data with very small vertex precision loss.";
                    break;
                case CompressionPreset.Smaller:
                    text = "Smaller: Medium Unity mesh compression. Review face, fingers, accessories, and blendshapes after reimport.";
                    break;
                default:
                    text = "Quest: High Unity mesh compression plus Quest-focused warnings. Compression reduces stored mesh data; it does NOT lower triangle count by itself.";
                    break;
            }

            EditorGUILayout.HelpBox(text, MessageType.None);
            EditorGUILayout.HelpBox(
                "This tool never performs blind topology decimation. Blendshape/facial meshes are preserved. Use the report to identify meshes that need real retopology or a dedicated quality-preserving decimator.",
                MessageType.Info);
        }

        void DrawSummary()
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Avatar mesh summary", NekoStyles.SlotName);
            EditorGUILayout.LabelField("Meshes", _meshes.Count + " (" + _skinnedMeshes + " skinned / " + _basicMeshes + " basic)");
            EditorGUILayout.LabelField("Triangles", _totalTriangles.ToString("N0"));
            EditorGUILayout.LabelField("Vertices", _totalVertices.ToString("N0"));
            EditorGUILayout.LabelField("Material slots", _totalMaterialSlots.ToString("N0"));
            EditorGUILayout.LabelField("Blendshapes", _totalBlendShapes.ToString("N0"));
            EditorGUILayout.LabelField("Mergeable duplicate material slots", _duplicateMaterialSlots.ToString("N0"));
            if (_scanDegenerates)
                EditorGUILayout.LabelField("Degenerate triangles", _degenerateTriangles.ToString("N0"));
            EditorGUILayout.EndVertical();

            if (_preset == CompressionPreset.Quest)
            {
                if (_totalTriangles > 20000)
                    EditorGUILayout.HelpBox("Quest/mobile: this avatar is above VRChat's current Poor-rank triangle maximum of 20,000. Mesh compression cannot fix triangle count; reduce topology.", MessageType.Warning);
                else if (_totalTriangles > 10000)
                    EditorGUILayout.HelpBox("Quest/mobile: triangle count is above the current Good-rank maximum of 10,000. Consider retopology/decimation for a stronger mobile rank.", MessageType.Warning);

                if (_skinnedMeshes > 2)
                    EditorGUILayout.HelpBox("Quest/mobile: more than 2 skinned meshes exceeds the current Poor-rank maximum. Merge compatible clothing/accessory meshes where practical.", MessageType.Warning);
                if (_totalMaterialSlots > 4)
                    EditorGUILayout.HelpBox("Quest/mobile: more than 4 material slots exceeds the current Poor-rank maximum. The Safe Cleanup action can merge submeshes that already use the exact same material.", MessageType.Warning);
            }
        }

        void DrawActions()
        {
            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create safe optimized copies", NekoStyles.PrimaryButton, GUILayout.Height(30f)))
                CreateOptimizedCopies();
            if (GUILayout.Button("Apply import compression", GUILayout.Height(30f)))
                ApplyImportSettings();
            if (GUILayout.Button("Copy report", GUILayout.Height(30f), GUILayout.Width(100f)))
                EditorGUIUtility.systemCopyBuffer = BuildReport();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Safe optimized copies keep every vertex, bone weight, UV, normal, tangent, and blendshape. They only remove degenerate triangles and merge submeshes that use the same material. Originals are never overwritten.",
                MessageType.None);
            GUILayout.Space(4f);
        }

        void DrawMeshList()
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                MeshEntry entry = _meshes[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(entry.GameObject != null ? entry.GameObject.name : entry.Mesh.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(58f)))
                {
                    Selection.activeGameObject = entry.GameObject;
                    EditorGUIUtility.PingObject(entry.GameObject);
                }
                EditorGUILayout.EndHorizontal();

                string kind = entry.SkinnedRenderer != null ? "Skinned" : "Basic";
                GUILayout.Label(kind + " · " + entry.Triangles.ToString("N0") + " tris · " + entry.Vertices.ToString("N0") + " verts · " +
                              entry.MaterialSlots + " material slots · " + entry.BlendShapes + " blendshapes",
                    EditorStyles.wordWrappedMiniLabel);

                if (!entry.Readable)
                    EditorGUILayout.HelpBox("Mesh Read/Write is disabled. VRChat currently requires Read/Write for avatar meshes and the safe geometry cleanup cannot inspect this mesh.", MessageType.Error);

                if (entry.DuplicateMaterialSlots > 0)
                    EditorGUILayout.HelpBox(entry.DuplicateMaterialSlots + " material slot(s) can be removed by merging submeshes that already use the same material.", MessageType.Info);

                if (entry.DegenerateScanned && entry.DegenerateTriangles > 0)
                    EditorGUILayout.HelpBox(entry.DegenerateTriangles + " degenerate / zero-area triangle(s) can be removed without changing visible topology.", MessageType.Info);

                if (entry.BlendShapes > 0 && entry.Triangles > 30000)
                    EditorGUILayout.HelpBox("Large blendshape mesh. Automatic destructive decimation is intentionally blocked; facial/morph topology should be reduced with a blendshape-aware workflow.", MessageType.Warning);

                if (_preset == CompressionPreset.Quest && entry.Triangles > 10000)
                    EditorGUILayout.HelpBox("This single mesh is over 10,000 triangles. It is a primary Quest topology-reduction candidate.", MessageType.Warning);

                if (!string.IsNullOrEmpty(entry.AssetPath))
                    GUILayout.Label(entry.AssetPath, EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }
        }

        void TryUseSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null) return;
            _avatarRoot = FindLikelyAvatarRoot(selected);
        }

        static GameObject FindLikelyAvatarRoot(GameObject selected)
        {
            Transform current = selected.transform;
            GameObject animatorCandidate = null;
            while (current != null)
            {
                Component[] components = current.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component == null) continue;
                    string name = component.GetType().Name;
                    if (name == "VRCAvatarDescriptor" || name == "VRC_AvatarDescriptor")
                        return current.gameObject;
                }

                if (current.GetComponent<Animator>() != null)
                    animatorCandidate = current.gameObject;
                current = current.parent;
            }
            return animatorCandidate != null ? animatorCandidate : selected;
        }

        void Scan()
        {
            _meshes.Clear();
            _totalTriangles = 0;
            _totalVertices = 0;
            _totalMaterialSlots = 0;
            _totalBlendShapes = 0;
            _skinnedMeshes = 0;
            _basicMeshes = 0;
            _duplicateMaterialSlots = 0;
            _degenerateTriangles = 0;

            if (_avatarRoot == null)
            {
                Repaint();
                return;
            }

            SkinnedMeshRenderer[] skinned = _avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] == null || skinned[i].sharedMesh == null) continue;
                AddEntry(skinned[i].gameObject, skinned[i], null, skinned[i], skinned[i].sharedMesh);
            }

            MeshFilter[] filters = _avatarRoot.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.sharedMesh == null) continue;
                Renderer renderer = filter.GetComponent<Renderer>();
                AddEntry(filter.gameObject, renderer, filter, null, filter.sharedMesh);
            }

            Repaint();
        }

        void AddEntry(GameObject go, Renderer renderer, MeshFilter filter, SkinnedMeshRenderer skinned, Mesh mesh)
        {
            var entry = new MeshEntry();
            entry.GameObject = go;
            entry.Renderer = renderer;
            entry.MeshFilter = filter;
            entry.SkinnedRenderer = skinned;
            entry.Mesh = mesh;
            entry.AssetPath = AssetDatabase.GetAssetPath(mesh);
            entry.Vertices = mesh.vertexCount;
            entry.Triangles = CountTriangles(mesh);
            entry.SubMeshes = mesh.subMeshCount;
            entry.MaterialSlots = renderer != null && renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
            entry.BlendShapes = mesh.blendShapeCount;
            entry.Bones = skinned != null && skinned.bones != null ? skinned.bones.Length : 0;
            entry.Readable = mesh.isReadable;
            entry.DuplicateMaterialSlots = CountDuplicateMaterialSlots(renderer, mesh);

            if (_scanDegenerates && entry.Readable)
            {
                entry.DegenerateTriangles = CountDegenerateTriangles(mesh);
                entry.DegenerateScanned = true;
            }

            _meshes.Add(entry);
            _totalTriangles += entry.Triangles;
            _totalVertices += entry.Vertices;
            _totalMaterialSlots += entry.MaterialSlots;
            _totalBlendShapes += entry.BlendShapes;
            _duplicateMaterialSlots += entry.DuplicateMaterialSlots;
            _degenerateTriangles += entry.DegenerateTriangles;
            if (skinned != null) _skinnedMeshes++; else _basicMeshes++;
        }

        static long CountTriangles(Mesh mesh)
        {
            long total = 0;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                if (mesh.GetTopology(s) == MeshTopology.Triangles)
                    total += (long)mesh.GetIndexCount(s) / 3L;
            }
            return total;
        }

        static int CountDuplicateMaterialSlots(Renderer renderer, Mesh mesh)
        {
            if (renderer == null || mesh == null) return 0;
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return 0;

            int usable = Mathf.Min(materials.Length, mesh.subMeshCount);
            var seen = new HashSet<Material>();
            int duplicate = 0;
            for (int i = 0; i < usable; i++)
            {
                Material material = materials[i];
                if (material == null) continue;
                if (!seen.Add(material)) duplicate++;
            }

            if (materials.Length > mesh.subMeshCount)
                duplicate += materials.Length - mesh.subMeshCount;
            return duplicate;
        }

        static int CountDegenerateTriangles(Mesh mesh)
        {
            if (mesh == null || !mesh.isReadable) return 0;
            Vector3[] vertices;
            try { vertices = mesh.vertices; }
            catch { return 0; }

            int removed = 0;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                int[] triangles;
                try { triangles = mesh.GetTriangles(s); }
                catch { continue; }

                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    if (IsDegenerate(vertices, triangles[i], triangles[i + 1], triangles[i + 2])) removed++;
                }
            }
            return removed;
        }

        static bool IsDegenerate(Vector3[] vertices, int a, int b, int c)
        {
            if (a == b || b == c || a == c) return true;
            if (a < 0 || b < 0 || c < 0 || a >= vertices.Length || b >= vertices.Length || c >= vertices.Length) return true;
            Vector3 ab = vertices[b] - vertices[a];
            Vector3 ac = vertices[c] - vertices[a];
            return Vector3.Cross(ab, ac).sqrMagnitude <= 0.000000000001f;
        }

        void CreateOptimizedCopies()
        {
            if (_meshes.Count == 0) return;
            string folder = EnsureOutputFolder();
            int changed = 0;
            int removedTriangles = 0;
            int removedSlots = 0;

            for (int i = 0; i < _meshes.Count; i++)
            {
                MeshEntry entry = _meshes[i];
                if (entry.Mesh == null || !entry.Mesh.isReadable) continue;

                int degenerates;
                int slots;
                Material[] newMaterials;
                Mesh clone = BuildSafeClone(entry, out degenerates, out slots, out newMaterials);
                if (clone == null) continue;

                string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + SanitizeFileName(entry.Mesh.name) + "_NekoOptimized.asset");
                AssetDatabase.CreateAsset(clone, assetPath);

                if (entry.SkinnedRenderer != null)
                {
                    Undo.RecordObject(entry.SkinnedRenderer, "NekoSune mesh optimization");
                    entry.SkinnedRenderer.sharedMesh = clone;
                    if (newMaterials != null) entry.SkinnedRenderer.sharedMaterials = newMaterials;
                    EditorUtility.SetDirty(entry.SkinnedRenderer);
                }
                else if (entry.MeshFilter != null)
                {
                    Undo.RecordObject(entry.MeshFilter, "NekoSune mesh optimization");
                    entry.MeshFilter.sharedMesh = clone;
                    EditorUtility.SetDirty(entry.MeshFilter);
                    if (entry.Renderer != null && newMaterials != null)
                    {
                        Undo.RecordObject(entry.Renderer, "NekoSune material-slot optimization");
                        entry.Renderer.sharedMaterials = newMaterials;
                        EditorUtility.SetDirty(entry.Renderer);
                    }
                }

                changed++;
                removedTriangles += degenerates;
                removedSlots += slots;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Scan();

            EditorUtility.DisplayDialog("NekoSune Mesh Compressor",
                "Created " + changed + " optimized mesh copy/copies.\n\n" +
                "Removed degenerate triangles: " + removedTriangles + "\n" +
                "Removed duplicate/unused material slots: " + removedSlots + "\n\n" +
                "Original mesh assets were not overwritten. Use Undo or reassign the originals if you want to revert.",
                "OK");
        }

        static Mesh BuildSafeClone(MeshEntry entry, out int degenerateRemoved, out int slotsRemoved, out Material[] newMaterials)
        {
            degenerateRemoved = 0;
            slotsRemoved = 0;
            newMaterials = null;
            Mesh mesh = entry.Mesh;
            if (mesh == null || !mesh.isReadable) return null;

            for (int s = 0; s < mesh.subMeshCount; s++)
                if (mesh.GetTopology(s) != MeshTopology.Triangles) return null;

            Vector3[] vertices;
            try { vertices = mesh.vertices; }
            catch { return null; }

            Material[] oldMaterials = entry.Renderer != null ? entry.Renderer.sharedMaterials : new Material[0];
            var groupMaterials = new List<Material>();
            var groupTriangles = new List<List<int>>();

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] triangles = mesh.GetTriangles(s);
                var clean = new List<int>(triangles.Length);
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    int a = triangles[i];
                    int b = triangles[i + 1];
                    int c = triangles[i + 2];
                    if (IsDegenerate(vertices, a, b, c))
                    {
                        degenerateRemoved++;
                        continue;
                    }
                    clean.Add(a);
                    clean.Add(b);
                    clean.Add(c);
                }

                Material material = s < oldMaterials.Length ? oldMaterials[s] : null;
                int group = -1;
                if (material != null)
                {
                    for (int g = 0; g < groupMaterials.Count; g++)
                    {
                        if (groupMaterials[g] == material)
                        {
                            group = g;
                            break;
                        }
                    }
                }

                if (group < 0)
                {
                    group = groupMaterials.Count;
                    groupMaterials.Add(material);
                    groupTriangles.Add(new List<int>());
                }
                groupTriangles[group].AddRange(clean);
            }

            for (int g = groupTriangles.Count - 1; g >= 0; g--)
            {
                if (groupTriangles[g].Count != 0) continue;
                groupTriangles.RemoveAt(g);
                groupMaterials.RemoveAt(g);
            }

            slotsRemoved = Mathf.Max(0, oldMaterials.Length - groupMaterials.Count);
            bool changed = degenerateRemoved > 0 || slotsRemoved > 0 || groupTriangles.Count != mesh.subMeshCount;
            if (!changed) return null;

            Mesh clone = UnityEngine.Object.Instantiate(mesh);
            clone.name = mesh.name + "_NekoOptimized";
            clone.subMeshCount = groupTriangles.Count;
            for (int g = 0; g < groupTriangles.Count; g++)
                clone.SetTriangles(groupTriangles[g], g, false);
            clone.RecalculateBounds();

            newMaterials = groupMaterials.ToArray();
            return clone;
        }

        void ApplyImportSettings()
        {
            var paths = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _meshes.Count; i++)
            {
                MeshEntry entry = _meshes[i];
                if (string.IsNullOrEmpty(entry.AssetPath)) continue;
                if (!(AssetImporter.GetAtPath(entry.AssetPath) is ModelImporter)) continue;

                bool hasBlendShapes;
                if (!paths.TryGetValue(entry.AssetPath, out hasBlendShapes)) hasBlendShapes = false;
                paths[entry.AssetPath] = hasBlendShapes || entry.BlendShapes > 0;
            }

            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("NekoSune Mesh Compressor", "No imported model files were found under this avatar. Generated .asset meshes can still use Safe Cleanup.", "OK");
                return;
            }

            ModelImporterMeshCompression compression = CompressionForPreset(_preset);
            string warning =
                "This changes Unity ModelImporter settings for " + paths.Count + " model file(s).\n\n" +
                "Compression: " + compression + "\n" +
                "Optimize polygon order: " + (_optimizeImporter ? "On" : "Unchanged") + "\n" +
                "Optimize vertex order: only enabled on models without blendshapes.\n\n" +
                "Mesh compression reduces stored mesh data but can introduce vertex-position artifacts at stronger settings. Continue?";

            if (!EditorUtility.DisplayDialog("Apply mesh import settings", warning, "Apply", "Cancel")) return;

            int changed = 0;
            int vertexOptimizationProtected = 0;
            int index = 0;
            try
            {
                foreach (KeyValuePair<string, bool> item in paths)
                {
                    index++;
                    EditorUtility.DisplayProgressBar("NekoSune Mesh Compressor", "Reimporting " + Path.GetFileName(item.Key), index / (float)paths.Count);
                    ModelImporter importer = AssetImporter.GetAtPath(item.Key) as ModelImporter;
                    if (importer == null) continue;

                    importer.meshCompression = compression;
                    if (_optimizeImporter)
                    {
                        importer.optimizeMeshPolygons = true;
                        if (!item.Value)
                            importer.optimizeMeshVertices = true;
                        else
                            vertexOptimizationProtected++;
                    }

                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Scan();
            EditorUtility.DisplayDialog("NekoSune Mesh Compressor",
                "Updated " + changed + " model importer(s).\n" +
                (vertexOptimizationProtected > 0
                    ? vertexOptimizationProtected + " model(s) contained blendshapes, so vertex-order optimization was left unchanged for safety."
                    : "No blendshape models required vertex-order protection."),
                "OK");
        }

        static ModelImporterMeshCompression CompressionForPreset(CompressionPreset preset)
        {
            switch (preset)
            {
                case CompressionPreset.Lossless: return ModelImporterMeshCompression.Off;
                case CompressionPreset.Balanced: return ModelImporterMeshCompression.Low;
                case CompressionPreset.Smaller: return ModelImporterMeshCompression.Medium;
                default: return ModelImporterMeshCompression.High;
            }
        }

        static string EnsureOutputFolder()
        {
            EnsureFolder("Assets", "NekoSune");
            EnsureFolder("Assets/NekoSune", "Avatars");
            EnsureFolder("Assets/NekoSune/Avatars", "OptimizedMeshes");
            return "Assets/NekoSune/Avatars/OptimizedMeshes";
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Mesh";
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++) value = value.Replace(invalid[i], '_');
            return value;
        }

        string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune Avatar Mesh Compressor report");
            sb.AppendLine("Avatar: " + (_avatarRoot != null ? _avatarRoot.name : "None"));
            sb.AppendLine("Preset: " + _preset);
            sb.AppendLine("Meshes: " + _meshes.Count + " (" + _skinnedMeshes + " skinned / " + _basicMeshes + " basic)");
            sb.AppendLine("Triangles: " + _totalTriangles);
            sb.AppendLine("Vertices: " + _totalVertices);
            sb.AppendLine("Material slots: " + _totalMaterialSlots);
            sb.AppendLine("Blendshapes: " + _totalBlendShapes);
            sb.AppendLine("Mergeable duplicate material slots: " + _duplicateMaterialSlots);
            sb.AppendLine("Degenerate triangles: " + _degenerateTriangles);
            sb.AppendLine();

            for (int i = 0; i < _meshes.Count; i++)
            {
                MeshEntry entry = _meshes[i];
                sb.Append("- ").Append(entry.GameObject != null ? entry.GameObject.name : entry.Mesh.name)
                    .Append(": ").Append(entry.Triangles).Append(" tris, ")
                    .Append(entry.Vertices).Append(" verts, ")
                    .Append(entry.MaterialSlots).Append(" material slots, ")
                    .Append(entry.BlendShapes).Append(" blendshapes");
                if (!entry.Readable) sb.Append(" [Read/Write OFF]");
                if (entry.DuplicateMaterialSlots > 0) sb.Append(" [").Append(entry.DuplicateMaterialSlots).Append(" mergeable slots]");
                if (entry.DegenerateTriangles > 0) sb.Append(" [").Append(entry.DegenerateTriangles).Append(" degenerate tris]");
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("Note: Unity mesh compression reduces stored mesh precision/size; it does not reduce triangle count. Safe Cleanup preserves vertex topology and blendshapes.");
            return sb.ToString();
        }
    }
}
