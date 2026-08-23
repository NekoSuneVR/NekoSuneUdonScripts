using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 20)]
    internal class NekoRankAddon : INekoAddon
    {
        public string Id { get { return "rank"; } }
        public string TitleKey { get { return "rank.title"; } }
        public string DescriptionKey { get { return "rank.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "◆"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoRankWindow.Open(); }
    }

    /// <summary>
    /// Shows an avatar's VRChat performance rank on both platforms and, more usefully, the exact
    /// set of changes that would move it up one rank. Analysis only: nothing here edits the
    /// avatar, apart from the opt-in Read/Write import fix.
    /// </summary>
    internal class NekoRankWindow : EditorWindow
    {
        const string PlatformKey = "NekoSune.Avatars.Rank.Platform";

        [SerializeField] GameObject _avatar;

        NekoAvatarReport _report;
        NekoRankAssessment _pc, _mobile;
        NekoPlatform _platform = NekoPlatform.PC;

        Vector2 _scroll;
        bool _foldTable = true;
        string _notice;

        static readonly NekoStat[][] Groups =
        {
            new[] { NekoStat.Triangles, NekoStat.MaterialSlots, NekoStat.SkinnedMeshes, NekoStat.BasicMeshes, NekoStat.TextureMemory, NekoStat.BoundsSize },
            new[] { NekoStat.Bones, NekoStat.Animators, NekoStat.ConstraintCount, NekoStat.ConstraintDepth },
            new[] { NekoStat.PhysBoneComponents, NekoStat.PhysBoneTransforms, NekoStat.PhysBoneColliders, NekoStat.PhysBoneCollisionChecks, NekoStat.Contacts },
            new[] { NekoStat.ParticleSystems, NekoStat.ParticlesActive, NekoStat.MeshParticlePolys, NekoStat.ParticleTrails, NekoStat.ParticleCollision, NekoStat.TrailRenderers, NekoStat.LineRenderers },
            new[] { NekoStat.Lights, NekoStat.AudioSources, NekoStat.Cloths, NekoStat.ClothVertices, NekoStat.PhysicsColliders, NekoStat.PhysicsRigidbodies, NekoStat.Raycasts }
        };

        static readonly string[] GroupKeys =
        {
            "rank.group.mesh", "rank.group.rig", "rank.group.physbones", "rank.group.dynamics", "rank.group.misc"
        };

        [MenuItem(NekoPaths.MenuRoot + "Avatar/Rank Advisor", false, 21)]
        public static void Open()
        {
            var w = GetWindow<NekoRankWindow>(false, "Rank Advisor", true);
            w.minSize = new Vector2(520f, 480f);
            w.Show();
        }

        [MenuItem("GameObject/NekoSune/Rank Advisor", false, 21)]
        static void FromHierarchy()
        {
            var go = Selection.activeGameObject;
            Open();
            var w = GetWindow<NekoRankWindow>();
            if (go != null) w.SetAvatar(go);
        }

        void OnEnable()
        {
            _platform = (NekoPlatform)EditorPrefs.GetInt(PlatformKey, (int)NekoPlatform.PC);
            NekoLoc.LanguageChanged += Repaint;
            if (_avatar != null) Rescan();
        }

        void OnDisable()
        {
            NekoLoc.LanguageChanged -= Repaint;
        }

        void SetAvatar(GameObject go)
        {
            _avatar = go;
            _notice = null;
            Rescan();
        }

        void Rescan()
        {
            if (_avatar == null)
            {
                _report = null;
                _pc = _mobile = null;
                return;
            }

            _report = NekoAvatarStats.Collect(_avatar);
            _pc = NekoRankAdvisor.Assess(_report, NekoPlatform.PC);
            _mobile = NekoRankAdvisor.Assess(_report, NekoPlatform.Mobile);
            Repaint();
        }

        NekoRankAssessment Active { get { return _platform == NekoPlatform.PC ? _pc : _mobile; } }
        NekoRankAssessment Other { get { return _platform == NekoPlatform.PC ? _mobile : _pc; } }

        // ------------------------------------------------------------------ GUI

        void OnGUI()
        {
            NekoStyles.Ensure();

            EditorGUILayout.BeginHorizontal();
            NekoStyles.HeaderBar(NekoLoc.T("rank.header"), "NekoSune", NekoLoc.T("rank.tagline"));
            DrawLanguagePopup();
            EditorGUILayout.EndHorizontal();
            NekoStyles.Rule(2f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawAvatarSlot();

            if (_avatar == null || _report == null)
            {
                GUILayout.Space(8f);
                EditorGUILayout.HelpBox(NekoLoc.T("rank.noAvatar"), MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            GUILayout.Space(4f);
            DrawPlatformTabs();
            GUILayout.Space(4f);
            DrawRankBadge();
            DrawWarnings();
            GUILayout.Space(4f);
            DrawBiggestWins();
            GUILayout.Space(4f);
            DrawTable();

            GUILayout.Space(10f);
            EditorGUILayout.EndScrollView();

            NekoStyles.Rule(2f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(NekoLoc.T("rank.rescan"), EditorStyles.miniButton, GUILayout.Width(110f))) Rescan();
            if (GUILayout.Button(NekoLoc.T("rank.copy"), EditorStyles.miniButton, GUILayout.Width(110f)))
            {
                EditorGUIUtility.systemCopyBuffer = BuildTextReport();
                _notice = NekoLoc.T("rank.copied");
            }
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_notice)) GUILayout.Label(_notice, NekoStyles.SlotMeta);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        void DrawLanguagePopup()
        {
            List<NekoLanguageInfo> langs = NekoLoc.Languages;
            if (langs == null || langs.Count == 0) return;

            EditorGUILayout.BeginVertical(GUILayout.Width(170f));
            GUILayout.Space(12f);
            var names = new string[langs.Count];
            int current = 0;
            for (int i = 0; i < langs.Count; i++)
            {
                names[i] = langs[i].Display;
                if (langs[i].Code == NekoLoc.ActiveCode) current = i;
            }
            int picked = EditorGUILayout.Popup(current, names);
            if (picked != current) NekoLoc.SetLanguage(langs[picked].Code);
            EditorGUILayout.EndVertical();
        }

        void DrawAvatarSlot()
        {
            string title = _avatar != null ? _avatar.name : NekoLoc.T("slot.avatar.empty");
            string meta = _avatar != null ? NekoLoc.T("slot.avatar.filled") : NekoLoc.T("slot.avatar.hint");

            EditorGUILayout.BeginHorizontal(NekoStyles.Card, GUILayout.Height(46f));
            GUILayout.Label("◉", NekoStyles.IconBig, GUILayout.Width(30f), GUILayout.Height(38f));
            EditorGUILayout.BeginVertical();
            GUILayout.Space(3f);
            GUILayout.Label(title, NekoStyles.SlotName);
            GUILayout.Label(meta, NekoStyles.SlotMeta);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (_avatar != null && GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(22f)))
                SetAvatar(null);
            EditorGUILayout.EndHorizontal();

            Rect rect = GUILayoutUtility.GetLastRect();
            if (_avatar == null)
                NekoStyles.Outline(rect, new Color(NekoStyles.Accent.r, NekoStyles.Accent.g, NekoStyles.Accent.b, 0.35f));

            HandleDrop(rect);
        }

        void HandleDrop(Rect rect)
        {
            Event e = Event.current;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            if (!rect.Contains(e.mousePosition)) return;

            GameObject found = null;
            Object[] objs = DragAndDrop.objectReferences;
            for (int i = 0; i < objs.Length; i++)
            {
                var go = objs[i] as GameObject;
                if (go == null) continue;
                if (go.GetComponentInChildren<Renderer>(true) == null &&
                    go.GetComponentInChildren<Animator>(true) == null) continue;
                found = go;
                break;
            }

            if (found == null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                SetAvatar(found);
            }
            e.Use();
        }

        void DrawPlatformTabs()
        {
            var labels = new[] { NekoLoc.T("rank.platform.pc"), NekoLoc.T("rank.platform.mobile") };
            int picked = GUILayout.Toolbar((int)_platform, labels, GUILayout.Height(22f));
            if (picked != (int)_platform)
            {
                _platform = (NekoPlatform)picked;
                EditorPrefs.SetInt(PlatformKey, picked);
            }
        }

        void DrawRankBadge()
        {
            NekoRankAssessment a = Active;
            if (a == null) return;

            Color c = NekoPerfTable.RankColor(a.Overall);

            EditorGUILayout.BeginHorizontal(NekoStyles.Card, GUILayout.Height(54f));
            GUILayout.Label(NekoPerfTable.RankGlyph(a.Overall), NekoStyles.IconBig, GUILayout.Width(34f), GUILayout.Height(40f));

            EditorGUILayout.BeginVertical();
            GUILayout.Space(4f);

            var big = new GUIStyle(NekoStyles.SlotName) { fontSize = 18 };
            big.normal.textColor = c;
            GUILayout.Label(NekoLoc.T(NekoPerfTable.RankKey(a.Overall)), big);

            NekoRankAssessment other = Other;
            if (other != null)
            {
                string otherName = NekoLoc.T(_platform == NekoPlatform.PC ? "rank.platform.mobile" : "rank.platform.pc");
                GUILayout.Label(NekoLoc.T("rank.otherPlatform", otherName,
                                          NekoLoc.T(NekoPerfTable.RankKey(other.Overall))), NekoStyles.SlotMeta);
            }
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            Rect rect = GUILayoutUtility.GetLastRect();
            NekoStyles.Outline(rect, new Color(c.r, c.g, c.b, 0.55f));

            GUILayout.Label(NekoLoc.T("rank.worstWins"), NekoStyles.WrapLabel);
        }

        void DrawWarnings()
        {
            NekoRankAssessment a = Active;
            if (a == null) return;

            if (a.ReadWriteForcedVeryPoor)
            {
                int count = _report.UnreadableRenderers.Count + _report.UnreadableParticles.Count;
                EditorGUILayout.HelpBox(NekoLoc.T("rank.warn.readWrite", count), MessageType.Error);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(NekoLoc.T("rank.fix.readWrite"), NekoStyles.PrimaryButton, GUILayout.Height(24f)))
                    DoEnableReadWrite();
                EditorGUILayout.EndHorizontal();
            }

            if (!_report.HasDescriptor)
                EditorGUILayout.HelpBox(NekoLoc.T("rank.warn.noDescriptor"), MessageType.Warning);

            if (a.HasUnmeasured)
                EditorGUILayout.HelpBox(NekoLoc.T("rank.warn.unmeasured"), MessageType.Info);

            if (a.HasEstimates)
                EditorGUILayout.HelpBox(NekoLoc.T("rank.warn.estimates"), MessageType.Info);

            EditorGUILayout.HelpBox(NekoLoc.T("rank.warn.disabled"), MessageType.None);
        }

        void DoEnableReadWrite()
        {
            int skipped;
            int changed = NekoRankAdvisor.EnableReadWrite(_report, out skipped);

            var sb = new StringBuilder();
            sb.Append(NekoLoc.T("rank.fix.readWriteDone", changed));
            if (skipped > 0) sb.Append("  ").Append(NekoLoc.T("rank.fix.readWriteSkipped", skipped));
            _notice = sb.ToString();

            Rescan();
        }

        void DrawBiggestWins()
        {
            NekoRankAssessment a = Active;
            if (a == null) return;

            GUILayout.Label(NekoLoc.T("rank.wins"), NekoStyles.SectionHeader);

            if (!a.CanImprove)
            {
                EditorGUILayout.BeginVertical(NekoStyles.Banner);
                GUILayout.Label(NekoLoc.T("rank.winsNone"), NekoStyles.WrapLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label(NekoLoc.T("rank.toReach", NekoLoc.T(NekoPerfTable.RankKey(a.NextRank))), NekoStyles.WrapLabel);
            GUILayout.Space(4f);

            if (a.ReadWriteForcedVeryPoor)
            {
                GUILayout.Label("• " + NekoLoc.T("rank.fix.readWrite"), NekoStyles.SlotName);
                GUILayout.Space(2f);
            }

            for (int i = 0; i < a.Blockers.Count; i++)
            {
                NekoStatResult res = a.Blockers[i];
                string label = NekoLoc.T(res.Def.LabelKey);

                string line;
                if (res.Stat == NekoStat.BoundsSize)
                {
                    Vector3 lim = NekoPerfTable.BoundsLimits[(int)a.NextRank];
                    line = NekoLoc.T("rank.blockerBounds", label,
                                     NekoRankAdvisor.FormatBounds(_report.BoundsSize),
                                     NekoRankAdvisor.FormatBounds(lim));
                }
                else
                {
                    line = NekoLoc.T("rank.blockerRow", label,
                                     NekoRankAdvisor.Format(res.Def, res.Value),
                                     res.Target >= 0 ? NekoRankAdvisor.Format(res.Def, res.Target) : "?");
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("• " + line, NekoStyles.WrapLabel);
                GUILayout.FlexibleSpace();
                DrawSelectButton(res);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawSelectButton(NekoStatResult res)
        {
            if (res.Sample.Culprits.Count == 0) return;
            if (GUILayout.Button(NekoLoc.T("rank.select"), EditorStyles.miniButton, GUILayout.Width(60f)))
            {
                Selection.objects = res.Sample.Culprits.ToArray();
                if (res.Sample.Culprits.Count > 0) EditorGUIUtility.PingObject(res.Sample.Culprits[0]);
            }
        }

        void DrawTable()
        {
            NekoRankAssessment a = Active;
            if (a == null) return;

            _foldTable = EditorGUILayout.Foldout(_foldTable, NekoLoc.T("rank.table"), true);
            if (!_foldTable) return;

            for (int g = 0; g < Groups.Length; g++)
            {
                GUILayout.Space(4f);
                GUILayout.Label(NekoLoc.T(GroupKeys[g]).ToUpperInvariant(), EditorStyles.miniBoldLabel);

                for (int s = 0; s < Groups[g].Length; s++)
                {
                    NekoStatResult res = Find(a, Groups[g][s]);
                    if (res != null) DrawStatRow(a, res);
                }
            }
        }

        static NekoStatResult Find(NekoRankAssessment a, NekoStat stat)
        {
            for (int i = 0; i < a.Stats.Count; i++)
                if (a.Stats[i].Stat == stat) return a.Stats[i];
            return null;
        }

        void DrawStatRow(NekoRankAssessment a, NekoStatResult res)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(NekoLoc.T(res.Def.LabelKey), GUILayout.Width(200f));

            // Value
            string valueText = res.Stat == NekoStat.BoundsSize
                ? NekoRankAdvisor.FormatBounds(_report.BoundsSize)
                : NekoRankAdvisor.Format(res.Def, res.Value);

            var valueStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            if (res.Sample.Confidence == NekoConfidence.NotMeasured)
                valueStyle.normal.textColor = NekoStyles.Dim;
            GUILayout.Label(valueText, valueStyle, GUILayout.Width(90f));

            // Bar against the Poor limit for this platform.
            Rect bar = GUILayoutUtility.GetRect(40f, 12f, GUILayout.ExpandWidth(true));
            DrawBar(bar, a, res);

            // Rank chip / status
            string chip;
            Color chipColor;
            if (!res.Ranked)
            {
                chip = NekoLoc.T("rank.notRanked");
                chipColor = NekoStyles.Dim;
            }
            else if (res.Sample.Confidence == NekoConfidence.NotMeasured)
            {
                chip = NekoLoc.T("rank.notMeasured");
                chipColor = NekoStyles.Dim;
            }
            else
            {
                chip = NekoPerfTable.RankGlyph(res.Rank) + " " + NekoLoc.T(NekoPerfTable.RankKey(res.Rank));
                chipColor = NekoPerfTable.RankColor(res.Rank);
                if (res.Sample.Confidence == NekoConfidence.Estimated)
                    chip += " ~";
            }

            var chipStyle = new GUIStyle(EditorStyles.miniLabel);
            chipStyle.normal.textColor = chipColor;
            GUILayout.Label(chip, chipStyle, GUILayout.Width(96f));

            DrawSelectButton(res);
            EditorGUILayout.EndHorizontal();
        }

        void DrawBar(Rect r, NekoRankAssessment a, NekoStatResult res)
        {
            r.y += 3f;
            r.height = 7f;

            Color track = NekoStyles.IsDark ? new Color(1, 1, 1, 0.08f) : new Color(0, 0, 0, 0.10f);
            EditorGUI.DrawRect(r, track);

            if (!res.Ranked || res.Sample.Confidence == NekoConfidence.NotMeasured) return;

            float frac;
            if (res.Stat == NekoStat.BoundsSize)
            {
                Vector3 lim = NekoPerfTable.BoundsLimits[(int)NekoRank.Poor];
                Vector3 sz = _report.BoundsSize;
                frac = Mathf.Max(SafeDiv(sz.x, lim.x), Mathf.Max(SafeDiv(sz.y, lim.y), SafeDiv(sz.z, lim.z)));
            }
            else
            {
                long poor = NekoPerfTable.LimitFor(res.Def, a.Platform, NekoRank.Poor);
                frac = poor > 0 ? res.Value / (float)poor : (res.Value > 0 ? 1.2f : 0f);
            }

            float w = Mathf.Clamp01(frac) * r.width;
            if (w > 0f)
                EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), NekoPerfTable.RankColor(res.Rank));

            // A notch where the Excellent limit sits, so headroom is readable at a glance.
            if (res.Stat != NekoStat.BoundsSize)
            {
                long excellent = NekoPerfTable.LimitFor(res.Def, a.Platform, NekoRank.Excellent);
                long poor = NekoPerfTable.LimitFor(res.Def, a.Platform, NekoRank.Poor);
                if (excellent > 0 && poor > 0)
                {
                    float x = r.x + Mathf.Clamp01(excellent / (float)poor) * r.width;
                    EditorGUI.DrawRect(new Rect(x, r.y - 1f, 1f, r.height + 2f),
                                       NekoStyles.IsDark ? new Color(1, 1, 1, 0.35f) : new Color(0, 0, 0, 0.35f));
                }
            }
        }

        static float SafeDiv(float a, float b) { return b <= 0f ? 0f : a / b; }

        // ------------------------------------------------------------------ text report

        string BuildTextReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("NekoSune Rank Advisor — " + (_avatar != null ? _avatar.name : "?"));
            sb.AppendLine();

            AppendPlatform(sb, _pc, NekoLoc.T("rank.platform.pc"));
            sb.AppendLine();
            AppendPlatform(sb, _mobile, NekoLoc.T("rank.platform.mobile"));

            if (_report != null && _report.MeshReadWriteDisabled)
            {
                sb.AppendLine();
                sb.AppendLine("! " + NekoLoc.T("rank.warn.readWrite",
                    _report.UnreadableRenderers.Count + _report.UnreadableParticles.Count));
            }

            return sb.ToString();
        }

        void AppendPlatform(StringBuilder sb, NekoRankAssessment a, string name)
        {
            if (a == null) return;
            sb.AppendLine("== " + name + ": " + NekoLoc.T(NekoPerfTable.RankKey(a.Overall)));

            for (int i = 0; i < a.Stats.Count; i++)
            {
                NekoStatResult res = a.Stats[i];
                if (!res.Ranked) continue;

                string value = res.Stat == NekoStat.BoundsSize
                    ? NekoRankAdvisor.FormatBounds(_report.BoundsSize)
                    : NekoRankAdvisor.Format(res.Def, res.Value);

                string state = res.Sample.Confidence == NekoConfidence.NotMeasured
                    ? NekoLoc.T("rank.notMeasured")
                    : NekoLoc.T(NekoPerfTable.RankKey(res.Rank));

                sb.Append("  ").Append(NekoLoc.T(res.Def.LabelKey).PadRight(30))
                  .Append(value.PadLeft(12)).Append("  ").AppendLine(state);
            }

            if (a.CanImprove && a.Blockers.Count > 0)
            {
                sb.AppendLine("  -> " + NekoLoc.T("rank.toReach", NekoLoc.T(NekoPerfTable.RankKey(a.NextRank))));
                for (int i = 0; i < a.Blockers.Count; i++)
                {
                    NekoStatResult res = a.Blockers[i];
                    if (res.Stat == NekoStat.BoundsSize) continue;
                    sb.Append("     - ").AppendLine(NekoLoc.T("rank.blockerRow",
                        NekoLoc.T(res.Def.LabelKey),
                        NekoRankAdvisor.Format(res.Def, res.Value),
                        res.Target >= 0 ? NekoRankAdvisor.Format(res.Def, res.Target) : "?"));
                }
            }
        }
    }
}
