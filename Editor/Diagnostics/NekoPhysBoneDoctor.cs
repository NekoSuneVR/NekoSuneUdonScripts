using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    [NekoAddon(Order = 10)]
    internal sealed class NekoPhysBoneDoctorAddon : INekoAddon
    {
        public string Id { get { return "physbone-doctor"; } }
        public string TitleKey { get { return "physbone.title"; } }
        public string DescriptionKey { get { return "physbone.desc"; } }
        public string CategoryKey { get { return "cat.avatar"; } }
        public string Glyph { get { return "P"; } }
        public bool IsAvailable { get { return true; } }
        public void Open() { NekoPhysBoneDoctorWindow.Open(); }
    }

    internal sealed class NekoPhysBoneDoctorWindow : EditorWindow
    {
        sealed class Row
        {
            public Component Component;
            public Transform Root;
            public int Transforms;
            public int Colliders;
            public int Checks;
        }

        GameObject _avatar;
        readonly List<Row> _rows = new List<Row>();
        readonly List<Component> _unusedColliders = new List<Component>();
        readonly List<string> _warnings = new List<string>();
        Vector2 _scroll;
        int _totalTransforms;
        int _totalChecks;

        [MenuItem(NekoPaths.MenuRoot + "Avatar/PhysBone Doctor", false, 10)]
        public static void Open()
        {
            var w = GetWindow<NekoPhysBoneDoctorWindow>(false, "PhysBone Doctor", true);
            w.minSize = new Vector2(680f, 500f);
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
            NekoStyles.HeaderBar("Doctor", "PhysBone", "Find expensive chains, unused colliders and merge candidates");
            EditorGUI.BeginChangeCheck();
            _avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", _avatar, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck()) Scan();
            if (_avatar == null) { EditorGUILayout.HelpBox("Select an avatar.", MessageType.Info); return; }

            EditorGUILayout.BeginVertical(NekoStyles.Card);
            GUILayout.Label("Totals", NekoStyles.SlotName);
            EditorGUILayout.LabelField("PhysBone components", _rows.Count + "  (mobile Good ≤ 4, Poor ≤ 8)");
            EditorGUILayout.LabelField("Affected transforms (estimated)", _totalTransforms + "  (mobile Good ≤ 16, Poor ≤ 64)");
            EditorGUILayout.LabelField("PhysBone colliders", CountAllColliders() + "  (mobile Good ≤ 4, Poor ≤ 16)");
            EditorGUILayout.LabelField("Collision checks (estimated)", _totalChecks + "  (mobile Good ≤ 16, Poor ≤ 64)");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", NekoStyles.PrimaryButton, GUILayout.Height(28f))) Scan();
            if (GUILayout.Button("Open Rank Advisor", GUILayout.Height(28f))) NekoRankWindow.Open();
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_warnings.Count > 0)
            {
                GUILayout.Label("Findings", EditorStyles.boldLabel);
                for (int i = 0; i < _warnings.Count; i++) EditorGUILayout.HelpBox(_warnings[i], MessageType.Warning);
            }

            if (_unusedColliders.Count > 0)
            {
                GUILayout.Label("Unused PhysBone colliders (" + _unusedColliders.Count + ")", EditorStyles.boldLabel);
                for (int i = 0; i < _unusedColliders.Count; i++)
                {
                    Component c = _unusedColliders[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label(c == null ? "Missing" : NekoAvatarDiagnosticsUtil.ObjectPath(_avatar.transform, c.transform), NekoStyles.WrapLabel);
                    if (c != null && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(58f))) { Selection.activeObject = c; EditorGUIUtility.PingObject(c); }
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.Label("PhysBone chains", EditorStyles.boldLabel);
            for (int i = 0; i < _rows.Count; i++) DrawRow(_rows[i]);
            EditorGUILayout.EndScrollView();
        }

        void DrawRow(Row row)
        {
            EditorGUILayout.BeginVertical(NekoStyles.Card);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(row.Component == null ? "Missing PhysBone" : NekoAvatarDiagnosticsUtil.ObjectPath(_avatar.transform, row.Component.transform), NekoStyles.SlotName);
            GUILayout.FlexibleSpace();
            if (row.Component != null && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(58f))) { Selection.activeObject = row.Component; EditorGUIUtility.PingObject(row.Component); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Root", row.Root == null ? "component transform" : NekoAvatarDiagnosticsUtil.ObjectPath(_avatar.transform, row.Root));
            EditorGUILayout.LabelField("Affected transforms", row.Transforms.ToString());
            EditorGUILayout.LabelField("Assigned colliders", row.Colliders.ToString());
            EditorGUILayout.LabelField("Estimated collision checks", row.Checks.ToString());
            if (row.Transforms == 0) EditorGUILayout.HelpBox("This chain affects no child transforms. It may be empty/unneeded or may rely on an endpoint; inspect it manually.", MessageType.Warning);
            if (row.Transforms > 16) EditorGUILayout.HelpBox("This one chain alone exceeds the mobile Good affected-transform budget. Consider shortening/merging the chain if its motion allows it.", MessageType.Info);
            if (row.Checks > 16) EditorGUILayout.HelpBox("This one chain alone exceeds the mobile Good collision-check budget. Reducing colliders or affected transforms can have a large payoff.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        void Scan()
        {
            _rows.Clear();
            _unusedColliders.Clear();
            _warnings.Clear();
            _totalTransforms = 0;
            _totalChecks = 0;
            if (_avatar == null) { Repaint(); return; }

            List<Component> bones = NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_avatar, "VRCPhysBone", "VRCPhysBoneBase");
            List<Component> colliders = NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_avatar, "VRCPhysBoneCollider", "VRCPhysBoneColliderBase");
            var referencedColliders = new HashSet<UnityEngine.Object>();
            var roots = new Dictionary<Transform, List<Component>>();

            for (int i = 0; i < bones.Count; i++)
            {
                Component pb = bones[i];
                Transform root = NekoAvatarDiagnosticsUtil.GetMember(pb, "rootTransform", "RootTransform") as Transform;
                if (root == null) root = pb.transform;
                int transforms = NekoAvatarDiagnosticsUtil.CountAffectedTransforms(pb);
                object assignedObject = NekoAvatarDiagnosticsUtil.GetMember(pb, "colliders", "Colliders");
                int assigned = NekoAvatarDiagnosticsUtil.CountCollection(assignedObject);
                IEnumerable assignedEnumerable = assignedObject as IEnumerable;
                if (assignedEnumerable != null) foreach (object o in assignedEnumerable) if (o is UnityEngine.Object) referencedColliders.Add((UnityEngine.Object)o);
                int checks = transforms * assigned;

                _rows.Add(new Row { Component = pb, Root = root, Transforms = transforms, Colliders = assigned, Checks = checks });
                _totalTransforms += transforms;
                _totalChecks += checks;

                List<Component> onRoot;
                if (!roots.TryGetValue(root, out onRoot)) { onRoot = new List<Component>(); roots.Add(root, onRoot); }
                onRoot.Add(pb);
            }

            foreach (KeyValuePair<Transform, List<Component>> pair in roots)
            {
                if (pair.Value.Count > 1)
                    _warnings.Add(pair.Value.Count + " PhysBone components use the same root '" + NekoAvatarDiagnosticsUtil.ObjectPath(_avatar.transform, pair.Key) + "'. Verify these are intentionally separate; overlapping chains are often merge candidates or accidental duplicates.");
            }

            for (int i = 0; i < colliders.Count; i++) if (!referencedColliders.Contains(colliders[i])) _unusedColliders.Add(colliders[i]);
            if (_unusedColliders.Count > 0) _warnings.Add(_unusedColliders.Count + " PhysBone collider component(s) are not referenced by any detected PhysBone chain. Removing genuinely unused colliders reduces clutter and may reduce the collider statistic.");

            _rows.Sort(delegate (Row a, Row b) { return b.Checks.CompareTo(a.Checks); });
            Repaint();
        }

        int CountAllColliders()
        {
            return _avatar == null ? 0 : NekoAvatarDiagnosticsUtil.FindComponentsByTypeName(_avatar, "VRCPhysBoneCollider", "VRCPhysBoneColliderBase").Count;
        }
    }
}
