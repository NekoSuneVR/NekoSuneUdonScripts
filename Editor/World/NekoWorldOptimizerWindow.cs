using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 10)]
    internal sealed class NekoWorldOptimizerAddon : INekoAddon
    {
        public string Id { get { return "world-optimizer"; } }
        public string TitleKey { get { return "optimizer.world.title"; } }
        public string DescriptionKey { get { return "optimizer.world.desc"; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "O"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoWorldOptimizerWindow.Open(); }
    }

    internal sealed class NekoWorldOptimizerWindow : EditorWindow
    {
        Vector2 _scroll;
        int _objects, _renderers, _triangles, _materialSlots, _textures, _realtimeLights, _shadowLights, _particles, _audio;
        long _textureBytes;
        readonly List<UnityEngine.Object> _largeTextures = new List<UnityEngine.Object>();
        readonly List<UnityEngine.Object> _realtimeObjects = new List<UnityEngine.Object>();

        [MenuItem(NekoPaths.MenuRoot + "World/Optimizer", false, 12)]
        public static void Open()
        {
            var w = GetWindow<NekoWorldOptimizerWindow>(false, "World Optimizer", true);
            w.minSize = new Vector2(680f, 520f);
            w.Scan();
            w.Show();
        }

        void OnEnable() { Scan(); }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.Header("NekoSune World Optimizer", "Performance-focused scene analysis separated from World Doctor");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Height(28f))) Scan();
            if (GUILayout.Button("Open World Doctor", GUILayout.Height(28f))) EditorApplication.ExecuteMenuItem("NekoSune/World/World Doctor");
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Card("Scene", "Objects", _objects, "Renderers", _renderers, "Triangles ~", _triangles);
            Card("Rendering", "Material slots", _materialSlots, "Unique textures", _textures, "Texture memory ~MB", (int)(_textureBytes / 1048576L));
            Card("Realtime cost", "Realtime lights", _realtimeLights, "Shadow lights", _shadowLights, "Particle systems", _particles);
            Card("Other", "AudioSources", _audio, "Large textures", _largeTextures.Count, "Realtime review objects", _realtimeObjects.Count);

            if (_largeTextures.Count > 0)
            {
                EditorGUILayout.HelpBox(_largeTextures.Count + " texture(s) are above 2048 on at least one axis. For mobile/Quest worlds, inspect Android platform overrides and whether the visual actually needs that resolution.", MessageType.Warning);
                if (GUILayout.Button("Select first large texture")) Selection.activeObject = _largeTextures[0];
            }
            if (_realtimeLights > 4 || _shadowLights > 1)
                EditorGUILayout.HelpBox("This scene has several realtime lights/shadows. Prefer baked/mixed lighting where the world design permits it, and profile mirrors/avatars in the actual client.", MessageType.Warning);
            if (_materialSlots > 100)
                EditorGUILayout.HelpBox("High material-slot count can become a draw-call problem. Look for repeated materials, modular props that can share atlases, and unnecessary renderer splits.", MessageType.Info);
            if (_triangles > 1000000)
                EditorGUILayout.HelpBox("Scene mesh geometry is above one million triangles before avatars. This is an advisory, not a VRChat hard limit; use LODs/occlusion and profile the visible areas.", MessageType.Info);

            EditorGUILayout.HelpBox("Optimizer focuses on performance opportunities. Build-readiness, Udon/network mistakes and platform rules stay in NekoSune Doctors so the two packages have clear jobs.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        void Card(string title, string a, int av, string b, int bv, string c, int cv)
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label(title, NekoStyles.CardTitle);
            EditorGUILayout.LabelField(a, av.ToString("N0"));
            EditorGUILayout.LabelField(b, bv.ToString("N0"));
            EditorGUILayout.LabelField(c, cv.ToString("N0"));
            EditorGUILayout.EndVertical();
        }

        void Scan()
        {
            _objects = _renderers = _triangles = _materialSlots = _textures = _realtimeLights = _shadowLights = _particles = _audio = 0;
            _textureBytes = 0;
            _largeTextures.Clear();
            _realtimeObjects.Clear();
            var textureSet = new HashSet<Texture>();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
                _objects += transforms.Length;
                Renderer[] renderers = roots[r].GetComponentsInChildren<Renderer>(true);
                _renderers += renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    Material[] mats = renderer.sharedMaterials;
                    _materialSlots += mats == null ? 0 : mats.Length;
                    Mesh mesh = null;
                    var smr = renderer as SkinnedMeshRenderer;
                    if (smr != null) mesh = smr.sharedMesh;
                    else
                    {
                        MeshFilter mf = renderer.GetComponent<MeshFilter>();
                        if (mf != null) mesh = mf.sharedMesh;
                    }
                    if (mesh != null)
                    {
                        for (int s = 0; s < mesh.subMeshCount; s++)
                        {
                            try { _triangles += (int)(mesh.GetIndexCount(s) / 3); } catch { }
                        }
                    }
                    if (mats == null) continue;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        Material mat = mats[m];
                        if (mat == null) continue;
                        string[] props = mat.GetTexturePropertyNames();
                        for (int p = 0; p < props.Length; p++)
                        {
                            Texture tex = mat.GetTexture(props[p]);
                            if (tex == null || !textureSet.Add(tex)) continue;
                            int w = Mathf.Max(1, tex.width), h = Mathf.Max(1, tex.height);
                            _textureBytes += (long)w * h * 4L;
                            if (w > 2048 || h > 2048) _largeTextures.Add(tex);
                        }
                    }
                }

                Light[] lights = roots[r].GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].bakingOutput.isBaked) continue;
                    _realtimeLights++;
                    _realtimeObjects.Add(lights[i]);
                    if (lights[i].shadows != LightShadows.None) _shadowLights++;
                }
                _particles += roots[r].GetComponentsInChildren<ParticleSystem>(true).Length;
                _audio += roots[r].GetComponentsInChildren<AudioSource>(true).Length;
            }
            _textures = textureSet.Count;
            Repaint();
        }
    }
}
