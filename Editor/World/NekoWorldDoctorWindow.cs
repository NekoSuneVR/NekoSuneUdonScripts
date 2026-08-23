using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 0)]
    internal sealed class NekoWorldDoctorAddon : INekoAddon
    {
        public string Id { get { return "world-doctor"; } }
        public string TitleKey { get { return "doctor.title"; } }
        public string DescriptionKey { get { return "doctor.desc"; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "✓"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoWorldDoctorWindow.Open(); }
    }

    internal sealed class NekoWorldDoctorWindow : EditorWindow
    {
        enum TargetPlatform
        {
            PC,
            AndroidQuest,
            Both
        }

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
        }

        sealed class SceneStats
        {
            public int GameObjects;
            public int Renderers;
            public long Triangles;
            public int MaterialSlots;
            public int UniqueMaterials;
            public int UniqueTextures;
            public long TextureMemory;
            public int Lights;
            public int RealtimeLights;
            public int ShadowLights;
            public int ReflectionProbes;
            public int RealtimeReflectionProbes;
            public int ParticleSystems;
            public long ParticleCapacity;
            public int AudioSources;
            public int Cameras;
            public int Colliders;
            public int Rigidbodies;
            public int UdonBehaviours;
        }

        TargetPlatform _platform = TargetPlatform.Both;
        Vector2 _scroll;
        SceneStats _stats;
        List<Finding> _findings = new List<Finding>();
        string _sceneName = "";
        DateTime _scanTime;

        [MenuItem(NekoPaths.MenuRoot + "World/World Doctor", false, 0)]
        public static void Open()
        {
            var window = GetWindow<NekoWorldDoctorWindow>(false, "World Doctor", true);
            window.minSize = new Vector2(640f, 500f);
            window.Show();
        }

        void OnEnable()
        {
            Scan();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header("World Doctor", "Scene performance, VRChat build readiness, and PC / Android-Quest checks");

            GUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            _platform = (TargetPlatform)EditorGUILayout.EnumPopup("Check target", _platform);
            if (GUILayout.Button("Scan scene", NekoStyles.PrimaryButton, GUILayout.Width(120f)))
                Scan();
            if (GUILayout.Button("Copy report", GUILayout.Width(100f)))
                EditorGUIUtility.systemCopyBuffer = BuildReport();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "VRChat-specific compatibility findings are labelled as rules. Performance-count warnings are NekoSune advisory signals, not official VRChat hard limits. Use them to find expensive areas worth profiling.",
                MessageType.None);

            if (_stats == null)
            {
                EditorGUILayout.HelpBox("No scene scan is available yet.", MessageType.Info);
                return;
            }

            GUILayout.Space(4f);
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
            GUILayout.Label("Scene summary", NekoStyles.CardTitle);
            EditorGUILayout.LabelField("Scene", string.IsNullOrEmpty(_sceneName) ? "Untitled" : _sceneName);
            EditorGUILayout.LabelField("GameObjects", _stats.GameObjects.ToString("N0"));
            EditorGUILayout.LabelField("Renderers / triangles", _stats.Renderers.ToString("N0") + " / " + _stats.Triangles.ToString("N0"));
            EditorGUILayout.LabelField("Material slots / unique materials", _stats.MaterialSlots.ToString("N0") + " / " + _stats.UniqueMaterials.ToString("N0"));
            EditorGUILayout.LabelField("Unique textures / estimated loaded memory", _stats.UniqueTextures.ToString("N0") + " / " + FormatBytes(_stats.TextureMemory));
            EditorGUILayout.LabelField("Lights / realtime / shadow-casting", _stats.Lights + " / " + _stats.RealtimeLights + " / " + _stats.ShadowLights);
            EditorGUILayout.LabelField("Reflection probes / realtime", _stats.ReflectionProbes + " / " + _stats.RealtimeReflectionProbes);
            EditorGUILayout.LabelField("Particles / combined maxParticles", _stats.ParticleSystems.ToString("N0") + " / " + _stats.ParticleCapacity.ToString("N0"));
            EditorGUILayout.LabelField("Audio / cameras", _stats.AudioSources + " / " + _stats.Cameras);
            EditorGUILayout.LabelField("Colliders / rigidbodies", _stats.Colliders + " / " + _stats.Rigidbodies);
            EditorGUILayout.LabelField("Udon behaviours", _stats.UdonBehaviours.ToString("N0"));
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
                EditorGUILayout.EndHorizontal();
                GUILayout.Label(finding.Detail, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(5f);
        }

        void Scan()
        {
            _findings = new List<Finding>();
            _stats = new SceneStats();
            _scanTime = DateTime.Now;

            Scene scene = SceneManager.GetActiveScene();
            _sceneName = scene.IsValid() ? scene.name : "";
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Add(Severity.Error, "No loaded scene", "Open a VRChat world scene before running World Doctor.", null);
                Repaint();
                return;
            }

            List<GameObject> gameObjects = GetSceneObjects(scene);
            _stats.GameObjects = gameObjects.Count;

            var renderers = new List<Renderer>();
            var lights = new List<Light>();
            var probes = new List<ReflectionProbe>();
            var particles = new List<ParticleSystem>();
            var audioSources = new List<AudioSource>();
            var cameras = new List<Camera>();
            var colliders = new List<Collider>();
            var rigidbodies = new List<Rigidbody>();
            var monoBehaviours = new List<MonoBehaviour>();
            bool hasSceneDescriptor = false;
            bool hasPostProcessing = false;

            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject go = gameObjects[i];
                AddIfNotNull(renderers, go.GetComponent<Renderer>());
                AddIfNotNull(lights, go.GetComponent<Light>());
                AddIfNotNull(probes, go.GetComponent<ReflectionProbe>());
                AddIfNotNull(particles, go.GetComponent<ParticleSystem>());
                AddIfNotNull(audioSources, go.GetComponent<AudioSource>());
                AddIfNotNull(cameras, go.GetComponent<Camera>());
                AddRange(colliders, go.GetComponents<Collider>());
                AddIfNotNull(rigidbodies, go.GetComponent<Rigidbody>());
                AddRange(monoBehaviours, go.GetComponents<MonoBehaviour>());

                Component[] components = go.GetComponents<Component>();
                for (int c = 0; c < components.Length; c++)
                {
                    Component component = components[c];
                    if (component == null) continue;
                    Type type = component.GetType();
                    string fullName = type.FullName ?? type.Name;
                    if (type.Name == "VRCSceneDescriptor" || fullName.EndsWith(".VRCSceneDescriptor", StringComparison.Ordinal))
                        hasSceneDescriptor = true;
                    if (fullName == "UnityEngine.Rendering.PostProcessing.PostProcessVolume" ||
                        fullName == "UnityEngine.Rendering.Volume")
                        hasPostProcessing = true;
                }
            }

            _stats.Renderers = renderers.Count;
            _stats.Lights = lights.Count;
            _stats.ReflectionProbes = probes.Count;
            _stats.ParticleSystems = particles.Count;
            _stats.AudioSources = audioSources.Count;
            _stats.Cameras = cameras.Count;
            _stats.Colliders = colliders.Count;
            _stats.Rigidbodies = rigidbodies.Count;

            var materials = new HashSet<Material>();
            var textures = new HashSet<Texture>();

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                _stats.Triangles += TriangleCount(renderer);

                Material[] shared = renderer.sharedMaterials;
                _stats.MaterialSlots += shared == null ? 0 : shared.Length;
                if (shared == null) continue;
                for (int m = 0; m < shared.Length; m++)
                {
                    Material material = shared[m];
                    if (material == null) continue;
                    materials.Add(material);
                }
            }

            foreach (Material material in materials)
            {
                string[] names;
                try { names = material.GetTexturePropertyNames(); }
                catch { continue; }
                for (int n = 0; n < names.Length; n++)
                {
                    Texture texture = null;
                    try { texture = material.GetTexture(names[n]); }
                    catch { }
                    if (texture != null) textures.Add(texture);
                }
            }

            _stats.UniqueMaterials = materials.Count;
            _stats.UniqueTextures = textures.Count;
            foreach (Texture texture in textures)
            {
                if (texture == null) continue;
                try { _stats.TextureMemory += Profiler.GetRuntimeMemorySizeLong(texture); }
                catch { }
                CheckTexture(texture);
            }

            for (int i = 0; i < lights.Count; i++)
            {
                Light light = lights[i];
                if (light == null || !light.enabled || !light.gameObject.activeInHierarchy) continue;
                if (light.lightmapBakeType != LightmapBakeType.Baked) _stats.RealtimeLights++;
                if (light.shadows != LightShadows.None && light.lightmapBakeType != LightmapBakeType.Baked) _stats.ShadowLights++;
            }

            for (int i = 0; i < probes.Count; i++)
            {
                ReflectionProbe probe = probes[i];
                if (probe != null && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                    _stats.RealtimeReflectionProbes++;
            }

            for (int i = 0; i < particles.Count; i++)
            {
                ParticleSystem ps = particles[i];
                if (ps == null) continue;
                int max = ps.main.maxParticles;
                _stats.ParticleCapacity += max;
                if (max > 5000)
                    Add(Severity.Warning, "High particle capacity", ps.name + " can hold up to " + max.ToString("N0") + " particles. This is a NekoSune performance advisory; profile the effect on target hardware.", ps);
            }

            for (int i = 0; i < audioSources.Count; i++)
            {
                AudioSource source = audioSources[i];
                if (source == null || source.clip == null) continue;
                AudioClip clip = source.clip;
                if (clip.length >= 15f && clip.loadType == AudioClipLoadType.DecompressOnLoad)
                    Add(Severity.Warning, "Long audio clip decompresses on load", clip.name + " is " + clip.length.ToString("0.0") + "s and uses Decompress On Load. Streaming or compressed-in-memory audio can reduce memory pressure for long ambience/music.", clip);
            }

            for (int i = 0; i < monoBehaviours.Count; i++)
            {
                MonoBehaviour behaviour = monoBehaviours[i];
                if (behaviour == null) continue;
                Type type = behaviour.GetType();
                string fullName = type.FullName ?? type.Name;
                if (fullName == "VRC.Udon.UdonBehaviour" || IsSubclassNamed(type, "UdonSharp.UdonSharpBehaviour"))
                    _stats.UdonBehaviours++;
            }

            if (!hasSceneDescriptor)
                Add(Severity.Error, "Missing VRC Scene Descriptor", "VRChat world scenes need a VRC Scene Descriptor before they can be uploaded. [VRChat rule]", null);

            if ((_platform == TargetPlatform.AndroidQuest || _platform == TargetPlatform.Both) && hasPostProcessing)
                Add(Severity.Warning, "Post-processing found for Android / Quest", "VRChat documents post-processing as disabled on Android/Quest. Keep the mobile version visually correct without relying on these effects. [VRChat platform rule]", null);

            if (_stats.RealtimeLights > 4)
                Add(Severity.Warning, "Many realtime lights", _stats.RealtimeLights + " enabled non-baked lights were found. This is a NekoSune advisory: bake lighting where practical and profile GPU cost, especially on mobile.", FirstActive(lights));

            if (_stats.ShadowLights > 2)
                Add(Severity.Warning, "Multiple realtime shadow lights", _stats.ShadowLights + " enabled non-baked lights cast realtime shadows. Realtime shadows are a common GPU cost in VR. [NekoSune advisory]", FirstShadowLight(lights));

            if (_stats.RealtimeReflectionProbes > 0)
                Add(Severity.Warning, "Realtime reflection probes", _stats.RealtimeReflectionProbes + " realtime reflection probe(s) were found. Prefer baked/custom probes unless realtime updates are required. [NekoSune advisory]", FirstRealtimeProbe(probes));

            if (_stats.MaterialSlots > 250)
                Add(Severity.Warning, "High material-slot count", _stats.MaterialSlots.ToString("N0") + " renderer material slots were found. Material slots often translate into more draw work; combine materials/meshes where it makes sense. [NekoSune advisory]", null);

            if (_stats.Triangles > 1000000)
                Add(Severity.Warning, "Large scene geometry", _stats.Triangles.ToString("N0") + " visible-scene mesh triangles were counted. This is not a VRChat hard limit; use occlusion/LOD and profile the actual view-dependent cost. [NekoSune advisory]", null);

            if (_stats.ParticleCapacity > 50000)
                Add(Severity.Warning, "Large combined particle capacity", "Combined maxParticles is " + _stats.ParticleCapacity.ToString("N0") + ". This is not a VRChat hard limit; inspect always-on effects and overdraw. [NekoSune advisory]", null);

            if (_stats.Cameras > 2)
                Add(Severity.Info, "Several cameras found", _stats.Cameras + " cameras are present. Extra active cameras can add rendering cost when used for mirrors, portals, security feeds, or effects. [NekoSune advisory]", cameras.Count > 0 ? cameras[0] : null);

            if (_stats.UdonBehaviours > 0)
                Add(Severity.Info, "Run Udon Network Doctor", _stats.UdonBehaviours + " Udon/UdonSharp behaviours were found. Use NekoSune → World → Udon Network Doctor to inspect sync modes, ownership, and network-event patterns.", null);

            Add(Severity.Info, "Test on real target hardware", "Use VRChat Build & Test for PC and Android/Quest. Performance in an empty editor scene cannot reproduce avatar load, instance population, mirrors, or real headset GPU/CPU limits.", null);

            Repaint();
        }

        void CheckTexture(Texture texture)
        {
            bool mobile = _platform == TargetPlatform.AndroidQuest || _platform == TargetPlatform.Both;
            int maxDimension = Mathf.Max(texture.width, texture.height);

            if (maxDimension > 4096)
            {
                Add(Severity.Warning, "Very large texture", texture.name + " is " + texture.width + "×" + texture.height + ". Consider whether this resolution is visible in-headset. [NekoSune advisory]", texture);
            }
            else if (mobile && maxDimension > 2048)
            {
                Add(Severity.Info, "Large texture for Android / Quest", texture.name + " is " + texture.width + "×" + texture.height + ". Review mobile import overrides and verify that the extra resolution is useful. [NekoSune advisory]", texture);
            }

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureCompression == TextureImporterCompression.Uncompressed && maxDimension >= 2048)
            {
                Add(Severity.Warning, "Large uncompressed texture", texture.name + " is imported uncompressed at a large resolution. Compression can substantially reduce memory use. [NekoSune advisory]", texture);
            }
        }

        static List<GameObject> GetSceneObjects(Scene scene)
        {
            var result = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                    if (transforms[t] != null) result.Add(transforms[t].gameObject);
            }
            return result;
        }

        static long TriangleCount(Renderer renderer)
        {
            Mesh mesh = null;
            MeshRenderer mr = renderer as MeshRenderer;
            if (mr != null)
            {
                MeshFilter filter = mr.GetComponent<MeshFilter>();
                if (filter != null) mesh = filter.sharedMesh;
            }
            else
            {
                SkinnedMeshRenderer smr = renderer as SkinnedMeshRenderer;
                if (smr != null) mesh = smr.sharedMesh;
            }

            if (mesh == null) return 0;
            long indices = 0;
            try
            {
                for (int i = 0; i < mesh.subMeshCount; i++)
                    indices += (long)mesh.GetIndexCount(i);
            }
            catch { return 0; }
            return indices / 3;
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

        static T FirstActive<T>(List<T> items) where T : Behaviour
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null && items[i].enabled && items[i].gameObject.activeInHierarchy) return items[i];
            return null;
        }

        static Light FirstShadowLight(List<Light> lights)
        {
            for (int i = 0; i < lights.Count; i++)
            {
                Light light = lights[i];
                if (light != null && light.enabled && light.gameObject.activeInHierarchy && light.shadows != LightShadows.None && light.lightmapBakeType != LightmapBakeType.Baked)
                    return light;
            }
            return null;
        }

        static ReflectionProbe FirstRealtimeProbe(List<ReflectionProbe> probes)
        {
            for (int i = 0; i < probes.Count; i++)
                if (probes[i] != null && probes[i].mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime) return probes[i];
            return null;
        }

        static void AddIfNotNull<T>(List<T> list, T item) where T : UnityEngine.Object
        {
            if (item != null) list.Add(item);
        }

        static void AddRange<T>(List<T> list, T[] items) where T : UnityEngine.Object
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null) list.Add(items[i]);
        }

        void Add(Severity severity, string title, string detail, UnityEngine.Object target)
        {
            _findings.Add(new Finding { Severity = severity, Title = title, Detail = detail, Target = target });
        }

        string BuildReport()
        {
            if (_stats == null) return "NekoSune World Doctor: no scan available.";
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune World Doctor");
            sb.AppendLine("Scene: " + _sceneName);
            sb.AppendLine("Target: " + _platform);
            sb.AppendLine("Scanned: " + _scanTime.ToString("u"));
            sb.AppendLine();
            sb.AppendLine("GameObjects: " + _stats.GameObjects.ToString("N0"));
            sb.AppendLine("Renderers: " + _stats.Renderers.ToString("N0"));
            sb.AppendLine("Triangles: " + _stats.Triangles.ToString("N0"));
            sb.AppendLine("Material slots: " + _stats.MaterialSlots.ToString("N0"));
            sb.AppendLine("Unique materials: " + _stats.UniqueMaterials.ToString("N0"));
            sb.AppendLine("Unique textures: " + _stats.UniqueTextures.ToString("N0"));
            sb.AppendLine("Estimated texture memory: " + FormatBytes(_stats.TextureMemory));
            sb.AppendLine("Realtime lights: " + _stats.RealtimeLights);
            sb.AppendLine("Realtime shadow lights: " + _stats.ShadowLights);
            sb.AppendLine("Realtime reflection probes: " + _stats.RealtimeReflectionProbes);
            sb.AppendLine("Particle max total: " + _stats.ParticleCapacity.ToString("N0"));
            sb.AppendLine("Audio sources: " + _stats.AudioSources);
            sb.AppendLine("Cameras: " + _stats.Cameras);
            sb.AppendLine("Udon behaviours: " + _stats.UdonBehaviours);
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

        static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double value = bytes;
            string[] units = { "KB", "MB", "GB" };
            int unit = -1;
            while (value >= 1024.0 && unit < units.Length - 1)
            {
                value /= 1024.0;
                unit++;
            }
            return value.ToString("0.0") + " " + units[unit];
        }
    }
}
