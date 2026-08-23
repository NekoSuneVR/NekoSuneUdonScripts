using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 30)]
    internal sealed class NekoAvatarCompressorAddon : INekoAddon
    {
        public string Id { get { return "compressor"; } }
        public string TitleKey { get { return "compressor.title"; } }
        public string DescriptionKey { get { return "compressor.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "C"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoAvatarCompressorWindow.Open(); }
    }

    /// <summary>
    /// Avatar-wide optimization front-end. It intentionally separates safe/import-only actions
    /// from behaviour-changing actions, and routes topology/rig work to the specialised tools
    /// rather than pretending every VRChat performance statistic can be safely auto-fixed.
    /// </summary>
    internal sealed class NekoAvatarCompressorWindow : EditorWindow
    {
        GameObject _avatar;
        NekoAvatarReport _report;
        NekoRankAssessment _pc;
        NekoRankAssessment _mobile;
        NekoPlatform _platform = NekoPlatform.PC;
        List<NekoTextureInfo> _textures = new List<NekoTextureInfo>();
        readonly List<Component> _unusedPhysBoneColliders = new List<Component>();
        Vector2 _scroll;
        int _androidTextureMax = 1024;
        int _particleCap = 200;
        string _notice;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Compressor", false, 25)]
        public static void Open()
        {
            var w = GetWindow<NekoAvatarCompressorWindow>(false, "Compressor", true);
            w.minSize = new Vector2(760f, 560f);
            w.Show();
        }

        void OnEnable()
        {
            if (_avatar == null) _avatar = NekoAvatarDiagnosticsUtil.SuggestedAvatarFromSelection();
            Scan();
        }

        void OnGUI()
        {
            NekoStyles.Ensure();
            NekoStyles.HeaderBar("Compressor", "NekoSune", "Reduce the things Rank Advisor actually measures — not only mesh data");

            GUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", _avatar, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();

            EditorGUILayout.BeginHorizontal();
            int picked = GUILayout.Toolbar((int)_platform, new[] { "PC", "Quest / mobile" }, GUILayout.Height(23f));
            if (picked != (int)_platform) _platform = (NekoPlatform)picked;
            if (GUILayout.Button("Use selection", GUILayout.Width(105f)))
            {
                _avatar = NekoAvatarDiagnosticsUtil.SuggestedAvatarFromSelection();
                Scan();
            }
            if (GUILayout.Button("Rescan", NekoStyles.PrimaryButton, GUILayout.Width(85f))) Scan();
            EditorGUILayout.EndHorizontal();

            if (_avatar == null || _report == null)
            {
                EditorGUILayout.HelpBox("Select or drop an avatar. Compressor will compare it with the same PC / Quest statistics used by Rank Advisor.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawOverview();
            DrawMeshAndMaterialModule();
            DrawTextureModule();
            DrawPhysBoneModule();
            DrawParticleModule();
            DrawManualBlockers();
            EditorGUILayout.EndScrollView();

            NekoStyles.Rule(2f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Rank Advisor", GUILayout.Height(28f))) NekoRankWindow.Open();
            if (GUILayout.Button("Copy compression plan", GUILayout.Height(28f)))
            {
                EditorGUIUtility.systemCopyBuffer = BuildPlan();
                _notice = "Compression plan copied.";
            }
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_notice)) GUILayout.Label(_notice, NekoStyles.SlotMeta);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        NekoRankAssessment ActiveAssessment
        {
            get { return _platform == NekoPlatform.PC ? _pc : _mobile; }
        }

        void DrawOverview()
        {
            NekoRankAssessment a = ActiveAssessment;
            Color c = NekoPerfTable.RankColor(a.Overall);

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Current " + (_platform == NekoPlatform.PC ? "PC" : "Quest / mobile") + " rank", NekoStyles.SlotName);
            GUILayout.FlexibleSpace();
            var rankStyle = new GUIStyle(NekoStyles.SlotName);
            rankStyle.normal.textColor = c;
            GUILayout.Label(NekoPerfTable.RankGlyph(a.Overall) + " " + NekoLoc.T(NekoPerfTable.RankKey(a.Overall)), rankStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Triangles", Value(NekoStat.Triangles));
            EditorGUILayout.LabelField("Material slots", Value(NekoStat.MaterialSlots));
            EditorGUILayout.LabelField("Skinned meshes", Value(NekoStat.SkinnedMeshes));
            EditorGUILayout.LabelField("Texture memory", NekoRankAdvisor.FormatBytes(_report.Get(NekoStat.TextureMemory).Value));
            EditorGUILayout.LabelField("Bones / animators", Value(NekoStat.Bones) + " / " + Value(NekoStat.Animators));
            EditorGUILayout.LabelField("PhysBones / transforms / colliders / checks",
                Value(NekoStat.PhysBoneComponents) + " / " + Value(NekoStat.PhysBoneTransforms) + " / " +
                Value(NekoStat.PhysBoneColliders) + " / " + Value(NekoStat.PhysBoneCollisionChecks));
            EditorGUILayout.EndVertical();

            if (a.Blockers.Count > 0)
                EditorGUILayout.HelpBox("Compressor is prioritising the same blocker categories Rank Advisor is showing. Green/safe actions are automatic; topology, skeleton and controller changes remain assisted/manual because blindly changing them can break the avatar.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("No ranked blocker is currently forcing this platform lower. You can still reduce build size or prepare a Quest variant below.", MessageType.None);
        }

        void DrawMeshAndMaterialModule()
        {
            GUILayout.Space(6f);
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("1. Meshes + material slots", NekoStyles.SlotName);
            GUILayout.Label("Triangles: " + Value(NekoStat.Triangles) + "   •   Skinned: " + Value(NekoStat.SkinnedMeshes) +
                          "   •   Basic: " + Value(NekoStat.BasicMeshes) + "   •   Material slots: " + Value(NekoStat.MaterialSlots),
                NekoStyles.WrapLabel);

            if (_report.MeshReadWriteDisabled)
            {
                EditorGUILayout.HelpBox("Read/Write is off on one or more meshes. That is an upload/rank blocker and also prevents safe geometry cleanup.", MessageType.Error);
                if (GUILayout.Button("Enable mesh Read/Write", NekoStyles.PrimaryButton, GUILayout.Height(26f))) EnableReadWrite();
            }

            EditorGUILayout.HelpBox("Mesh cleanup can remove degenerate triangles, merge duplicate material submeshes and apply Unity mesh import compression. It does not fake triangle reduction. High triangle/skinned-mesh counts still need a blendshape-aware decimator, retopology, or compatible renderer merge.", MessageType.None);
            if (GUILayout.Button("Open Mesh Compression", GUILayout.Height(28f))) NekoMeshCompressorWindow.OpenFor(_avatar);
            EditorGUILayout.EndVertical();
        }

        void DrawTextureModule()
        {
            GUILayout.Space(6f);
            long bytes = _report.Get(NekoStat.TextureMemory).Value;
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("2. Textures / VRAM", NekoStyles.SlotName);
            GUILayout.Label(_textures.Count + " unique texture(s) • estimated " + NekoRankAdvisor.FormatBytes(bytes) + " loaded memory", NekoStyles.WrapLabel);

            EditorGUILayout.BeginHorizontal();
            _androidTextureMax = EditorGUILayout.IntPopup("Android max size", _androidTextureMax,
                new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 });
            if (GUILayout.Button("Apply to all avatar textures", NekoStyles.PrimaryButton, GUILayout.Width(195f), GUILayout.Height(26f)))
                ApplyAndroidTextureMax();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("This writes Android-only TextureImporter overrides, so the PC texture stays at its normal resolution. It can directly reduce Quest/mobile texture memory. Review face/normal maps after aggressive 512 settings.", MessageType.None);
            if (GUILayout.Button("Open VRAM / Texture Inspector", GUILayout.Height(26f))) NekoTextureInspectorWindow.Open();
            EditorGUILayout.EndVertical();
        }

        void DrawPhysBoneModule()
        {
            GUILayout.Space(6f);
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("3. PhysBones", NekoStyles.SlotName);
            GUILayout.Label("Components " + Value(NekoStat.PhysBoneComponents) + " • transforms ~" + Value(NekoStat.PhysBoneTransforms) +
                          " • colliders " + Value(NekoStat.PhysBoneColliders) + " • collision checks ~" + Value(NekoStat.PhysBoneCollisionChecks),
                NekoStyles.WrapLabel);

            if (_unusedPhysBoneColliders.Count > 0)
            {
                EditorGUILayout.HelpBox(_unusedPhysBoneColliders.Count + " PhysBone collider component(s) are not referenced by any detected PhysBone. These are the safest PhysBone count reduction candidates.", MessageType.Info);
                if (GUILayout.Button("Remove unreferenced PhysBone colliders", NekoStyles.PrimaryButton, GUILayout.Height(26f)))
                    RemoveUnusedPhysBoneColliders();
            }
            else
            {
                GUILayout.Label("No unreferenced PhysBone colliders detected.", NekoStyles.SlotMeta);
            }

            EditorGUILayout.HelpBox("Overlapping chains, long transform chains and collider-heavy PhysBones need judgement. Compressor will not auto-merge them because merged dynamics can move differently.", MessageType.None);
            if (GUILayout.Button("Open PhysBone Doctor", GUILayout.Height(26f))) NekoPhysBoneDoctorWindow.Open();
            EditorGUILayout.EndVertical();
        }

        void DrawParticleModule()
        {
            GUILayout.Space(6f);
            long systems = _report.Get(NekoStat.ParticleSystems).Value;
            long active = _report.Get(NekoStat.ParticlesActive).Value;

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("4. Particles", NekoStyles.SlotName);
            GUILayout.Label("Particle systems: " + systems + " • configured max particles: " + active.ToString("N0"), NekoStyles.WrapLabel);

            EditorGUILayout.BeginHorizontal();
            _particleCap = Mathf.Max(0, EditorGUILayout.IntField("Total max-particle cap", _particleCap));
            using (new EditorGUI.DisabledScope(active <= _particleCap || systems == 0))
            {
                if (GUILayout.Button("Scale particle limits to cap", GUILayout.Width(190f), GUILayout.Height(26f))) CapParticles();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Particle capping changes the visual effect, so it is never included in an automatic safe pass. It proportionally reduces each ParticleSystem.main.maxParticles and is Undoable.", MessageType.Warning);
            EditorGUILayout.EndVertical();
        }

        void DrawManualBlockers()
        {
            GUILayout.Space(6f);
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("5. Assisted / manual rank blockers", NekoStyles.SlotName);

            NekoRankAssessment a = ActiveAssessment;
            bool any = false;
            for (int i = 0; i < a.Blockers.Count; i++)
            {
                NekoStatResult r = a.Blockers[i];
                if (IsHandledAbove(r.Stat)) continue;
                any = true;
                GUILayout.Label("• " + NekoLoc.T(r.Def.LabelKey) + ": " + FormatResult(r) + ManualHint(r.Stat), NekoStyles.WrapLabel);
            }

            if (!any) GUILayout.Label("No additional blockers for this platform.", NekoStyles.SlotMeta);

            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Avatar Doctor", GUILayout.Height(26f))) NekoAvatarDoctorWindow.Open();
            if (GUILayout.Button("Open Quest Assistant", GUILayout.Height(26f))) NekoQuestAssistantWindow.Open();
            if (GUILayout.Button("Open Expression / Animator Doctor", GUILayout.Height(26f))) NekoExpressionAnimatorDoctorWindow.Open();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        bool IsHandledAbove(NekoStat stat)
        {
            return stat == NekoStat.Triangles || stat == NekoStat.MaterialSlots || stat == NekoStat.SkinnedMeshes ||
                   stat == NekoStat.BasicMeshes || stat == NekoStat.TextureMemory || stat == NekoStat.PhysBoneComponents ||
                   stat == NekoStat.PhysBoneTransforms || stat == NekoStat.PhysBoneColliders ||
                   stat == NekoStat.PhysBoneCollisionChecks || stat == NekoStat.ParticleSystems || stat == NekoStat.ParticlesActive ||
                   stat == NekoStat.MeshParticlePolys || stat == NekoStat.ParticleTrails || stat == NekoStat.ParticleCollision;
        }

        string ManualHint(NekoStat stat)
        {
            switch (stat)
            {
                case NekoStat.Bones: return " — remove genuinely unused rig bones only after checking skinning, animations and PhysBones.";
                case NekoStat.Animators: return " — merge/remove child Animator components only when their controllers are genuinely redundant.";
                case NekoStat.ConstraintCount:
                case NekoStat.ConstraintDepth: return " — simplify constraint chains; automatic deletion could break driven objects.";
                case NekoStat.Contacts: return " — remove unused contact senders/receivers after checking menus and parameter drivers.";
                case NekoStat.Lights: return " — remove or bake avatar lights where the effect allows it.";
                case NekoStat.AudioSources: return " — consolidate unused/duplicate AudioSource components.";
                case NekoStat.Cloths:
                case NekoStat.ClothVertices: return " — Unity Cloth needs manual replacement/removal for avatars.";
                case NekoStat.PhysicsColliders:
                case NekoStat.PhysicsRigidbodies: return " — remove only components that are not needed by avatar behaviour.";
                case NekoStat.BoundsSize: return " — inspect oversized renderer/particle bounds before changing them.";
                default: return " — inspect this category before changing behaviour.";
            }
        }

        string FormatResult(NekoStatResult r)
        {
            if (r.Stat == NekoStat.BoundsSize) return NekoRankAdvisor.FormatBounds(_report.BoundsSize);
            return NekoRankAdvisor.Format(r.Def, r.Value) + (r.Target >= 0 ? " → " + NekoRankAdvisor.Format(r.Def, r.Target) + " or less" : "");
        }

        void EnableReadWrite()
        {
            int skipped;
            int changed = NekoRankAdvisor.EnableReadWrite(_report, out skipped);
            _notice = "Read/Write enabled on " + changed + " model(s)" + (skipped > 0 ? "; " + skipped + " skipped." : ".");
            Scan();
        }

        void ApplyAndroidTextureMax()
        {
            if (_textures.Count == 0) return;
            string message = "Apply Android max texture size " + _androidTextureMax + " to " + _textures.Count + " avatar texture(s)?\n\n" +
                             "This changes TextureImporter Android overrides for the source texture assets. PC import settings remain unchanged.";
            if (!EditorUtility.DisplayDialog("Compress Quest textures", message, "Apply", "Cancel")) return;

            int changed = 0;
            for (int i = 0; i < _textures.Count; i++)
                if (NekoAvatarDiagnosticsUtil.TrySetAndroidTextureOverride(_textures[i].Texture, _androidTextureMax)) changed++;
            _notice = "Updated Android texture overrides on " + changed + " texture(s).";
            Scan();
        }

        void RemoveUnusedPhysBoneColliders()
        {
            if (_unusedPhysBoneColliders.Count == 0) return;
            if (!EditorUtility.DisplayDialog("Remove unused PhysBone colliders",
                    "Remove " + _unusedPhysBoneColliders.Count + " PhysBone collider component(s) that are not referenced by any detected PhysBone chain?\n\nThis is Undoable, but review prefab overrides before saving.",
                    "Remove", "Cancel")) return;

            int removed = 0;
            for (int i = _unusedPhysBoneColliders.Count - 1; i >= 0; i--)
            {
                Component c = _unusedPhysBoneColliders[i];
                if (c == null) continue;
                Undo.DestroyObjectImmediate(c);
                removed++;
            }
            _notice = "Removed " + removed + " unreferenced PhysBone collider(s).";
            Scan();
        }

        void CapParticles()
        {
            ParticleSystem[] systems = _avatar.GetComponentsInChildren<ParticleSystem>(true);
            long total = 0;
            for (int i = 0; i < systems.Length; i++) if (systems[i] != null) total += systems[i].main.maxParticles;
            if (systems.Length == 0 || total <= _particleCap) return;

            if (!EditorUtility.DisplayDialog("Cap avatar particles",
                    "Scale the configured maxParticles values from " + total.ToString("N0") + " total to about " + _particleCap.ToString("N0") + "?\n\nThis changes the look of particle effects. The changes are Undoable.",
                    "Scale limits", "Cancel")) return;

            double ratio = total <= 0 ? 0.0 : _particleCap / (double)total;
            int changed = 0;
            int assignedTotal = 0;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null) continue;
                int oldMax = ps.main.maxParticles;
                int next = Mathf.Max(0, Mathf.FloorToInt((float)(oldMax * ratio)));
                if (oldMax > 0 && _particleCap > 0 && next == 0) next = 1;
                if (assignedTotal + next > _particleCap) next = Mathf.Max(0, _particleCap - assignedTotal);
                assignedTotal += next;
                if (next == oldMax) continue;
                Undo.RecordObject(ps, "NekoSune particle compression");
                var main = ps.main;
                main.maxParticles = next;
                EditorUtility.SetDirty(ps);
                changed++;
            }
            _notice = "Scaled " + changed + " ParticleSystem limit(s) to " + assignedTotal + " total max particles.";
            Scan();
        }

        void Scan()
        {
            _unusedPhysBoneColliders.Clear();
            _notice = null;
            if (_avatar == null)
            {
                _report = null;
                _pc = _mobile = null;
                _textures = new List<NekoTextureInfo>();
                Repaint();
                return;
            }

            _report = NekoAvatarStats.Collect(_avatar);
            _pc = NekoRankAdvisor.Assess(_report, NekoPlatform.PC);
            _mobile = NekoRankAdvisor.Assess(_report, NekoPlatform.Mobile);
            _textures = NekoAvatarDiagnosticsUtil.CollectTextures(_avatar);
            FindUnusedPhysBoneColliders();
            Repaint();
        }

        void FindUnusedPhysBoneColliders()
        {
            List<Component> bones = NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_avatar, "VRCPhysBone", "VRCPhysBoneBase");
            List<Component> colliders = NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_avatar, "VRCPhysBoneCollider", "VRCPhysBoneColliderBase");
            var referenced = new HashSet<UnityEngine.Object>();

            for (int i = 0; i < bones.Count; i++)
            {
                object value = NekoAvatarDiagnosticsUtil.GetMember(bones[i], "colliders", "Colliders");
                IEnumerable seq = value as IEnumerable;
                if (seq == null) continue;
                foreach (object o in seq)
                    if (o is UnityEngine.Object) referenced.Add((UnityEngine.Object)o);
            }

            for (int i = 0; i < colliders.Count; i++)
                if (colliders[i] != null && !referenced.Contains(colliders[i])) _unusedPhysBoneColliders.Add(colliders[i]);
        }

        string Value(NekoStat stat)
        {
            return _report == null ? "0" : _report.Get(stat).Value.ToString("N0");
        }

        string BuildPlan()
        {
            var sb = new StringBuilder();
            NekoRankAssessment a = ActiveAssessment;
            sb.AppendLine("NekoSune Avatar Compressor plan");
            sb.AppendLine("Avatar: " + (_avatar != null ? _avatar.name : "None"));
            sb.AppendLine("Platform: " + (_platform == NekoPlatform.PC ? "PC" : "Quest/mobile"));
            sb.AppendLine("Current rank: " + NekoLoc.T(NekoPerfTable.RankKey(a.Overall)));
            sb.AppendLine();
            sb.AppendLine("Measured priorities:");
            sb.AppendLine("- Triangles: " + Value(NekoStat.Triangles));
            sb.AppendLine("- Material slots: " + Value(NekoStat.MaterialSlots));
            sb.AppendLine("- Skinned meshes: " + Value(NekoStat.SkinnedMeshes));
            sb.AppendLine("- Texture memory: " + NekoRankAdvisor.FormatBytes(_report.Get(NekoStat.TextureMemory).Value));
            sb.AppendLine("- Bones: " + Value(NekoStat.Bones));
            sb.AppendLine("- Animators: " + Value(NekoStat.Animators));
            sb.AppendLine("- PhysBones: " + Value(NekoStat.PhysBoneComponents));
            sb.AppendLine("- PhysBone transforms: ~" + Value(NekoStat.PhysBoneTransforms));
            sb.AppendLine("- PhysBone colliders: " + Value(NekoStat.PhysBoneColliders));
            sb.AppendLine("- PhysBone checks: ~" + Value(NekoStat.PhysBoneCollisionChecks));
            sb.AppendLine("- Particles configured max: " + Value(NekoStat.ParticlesActive));
            sb.AppendLine();
            sb.AppendLine("Safe/assisted actions:");
            if (_report.MeshReadWriteDisabled) sb.AppendLine("- Enable mesh Read/Write.");
            sb.AppendLine("- Mesh Compression: safe duplicate-slot/degenerate cleanup + importer compression.");
            sb.AppendLine("- Quest textures: Android max-size override " + _androidTextureMax + ".");
            if (_unusedPhysBoneColliders.Count > 0) sb.AppendLine("- Remove " + _unusedPhysBoneColliders.Count + " unreferenced PhysBone collider(s).");
            if (_report.Get(NekoStat.ParticlesActive).Value > _particleCap) sb.AppendLine("- Optional visual change: cap particle max values to about " + _particleCap + ".");
            sb.AppendLine("- Use Avatar/PhysBone/Animator Doctors for behaviour-sensitive reductions.");
            return sb.ToString();
        }
    }
}
