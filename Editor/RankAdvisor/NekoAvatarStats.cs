using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace NekoSune.Avatars.Editor
{
    /// <summary>One measured statistic, with the objects responsible so the UI can select them.</summary>
    internal sealed class NekoStatSample
    {
        public NekoStat Stat;
        public long Value;
        public NekoConfidence Confidence = NekoConfidence.Exact;
        public readonly List<Object> Culprits = new List<Object>();

        public void Blame(Object o)
        {
            if (o == null) return;
            if (!Culprits.Contains(o)) Culprits.Add(o);
        }
    }

    /// <summary>Everything the advisor needs about one avatar, measured once.</summary>
    internal sealed class NekoAvatarReport
    {
        public GameObject Avatar;
        public Vector3 BoundsSize;
        public NekoStatSample[] Samples;

        public bool HasDescriptor;
        public readonly List<Renderer> UnreadableRenderers = new List<Renderer>();
        public readonly List<ParticleSystem> UnreadableParticles = new List<ParticleSystem>();

        /// <summary>Any mesh with Read/Write off forces Very Poor and blocks upload outright.</summary>
        public bool MeshReadWriteDisabled
        {
            get { return UnreadableRenderers.Count > 0 || UnreadableParticles.Count > 0; }
        }

        public NekoStatSample Get(NekoStat s) { return Samples[(int)s]; }
    }

    /// <summary>
    /// Counts every ranked statistic on an avatar without needing the VRChat SDK: SDK components
    /// are found by type name and read by reflection, so the numbers still appear in a project
    /// that has no SDK installed.
    /// </summary>
    internal static class NekoAvatarStats
    {
        const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        public static NekoAvatarReport Collect(GameObject avatar)
        {
            var r = new NekoAvatarReport { Avatar = avatar };

            int statCount = Enum.GetValues(typeof(NekoStat)).Length;
            r.Samples = new NekoStatSample[statCount];
            for (int i = 0; i < statCount; i++)
                r.Samples[i] = new NekoStatSample { Stat = (NekoStat)i };

            if (avatar == null) return r;

            r.HasDescriptor = FindByTypeName(avatar, "VRCAvatarDescriptor", "VRC_AvatarDescriptor").Count > 0;

            CollectMeshes(avatar, r);
            CollectTextures(avatar, r);
            CollectRig(avatar, r);
            CollectPhysBones(avatar, r);
            CollectContacts(avatar, r);
            CollectConstraints(avatar, r);
            CollectParticles(avatar, r);
            CollectMisc(avatar, r);
            CollectBounds(avatar, r);

            // We do not try to guess the raycast stat: reporting a wrong number would be worse
            // than admitting we did not measure it.
            r.Get(NekoStat.Raycasts).Confidence = NekoConfidence.NotMeasured;

            return r;
        }

        // ------------------------------------------------------------------ meshes and materials

        static void CollectMeshes(GameObject avatar, NekoAvatarReport r)
        {
            NekoStatSample tris     = r.Get(NekoStat.Triangles);
            NekoStatSample skinned  = r.Get(NekoStat.SkinnedMeshes);
            NekoStatSample basic    = r.Get(NekoStat.BasicMeshes);
            NekoStatSample slots    = r.Get(NekoStat.MaterialSlots);

            SkinnedMeshRenderer[] smrs = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
            {
                SkinnedMeshRenderer smr = smrs[i];
                if (smr == null) continue;
                skinned.Value++;
                skinned.Blame(smr.gameObject);

                Mesh m = smr.sharedMesh;
                if (m != null)
                {
                    if (!m.isReadable) r.UnreadableRenderers.Add(smr);
                    long t = TriangleCount(m);
                    tris.Value += t;
                    if (t > 0) tris.Blame(smr.gameObject);
                }
            }

            MeshRenderer[] mrs = avatar.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < mrs.Length; i++)
            {
                MeshRenderer mr = mrs[i];
                if (mr == null) continue;
                basic.Value++;
                basic.Blame(mr.gameObject);

                var mf = mr.GetComponent<MeshFilter>();
                Mesh m = mf != null ? mf.sharedMesh : null;
                if (m != null)
                {
                    if (!m.isReadable) r.UnreadableRenderers.Add(mr);
                    long t = TriangleCount(m);
                    tris.Value += t;
                    if (t > 0) tris.Blame(mr.gameObject);
                }
            }

            // Material slots count across every renderer type, including particle and line renderers.
            Renderer[] all = avatar.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Renderer rend = all[i];
                if (rend == null) continue;
                Material[] mats = rend.sharedMaterials;
                if (mats == null) continue;
                slots.Value += mats.Length;
                if (mats.Length > 0) slots.Blame(rend.gameObject);
            }
        }

        static long TriangleCount(Mesh m)
        {
            long total = 0;
            for (int s = 0; s < m.subMeshCount; s++)
            {
                if (m.GetTopology(s) != MeshTopology.Triangles) continue;
                total += (long)(m.GetIndexCount(s) / 3);
            }
            return total;
        }

        // ------------------------------------------------------------------ textures

        static void CollectTextures(GameObject avatar, NekoAvatarReport r)
        {
            NekoStatSample mem = r.Get(NekoStat.TextureMemory);
            // VRChat measures the platform-compressed size; we measure what Unity has loaded,
            // which is close but not identical.
            mem.Confidence = NekoConfidence.Estimated;

            var seenMats = new HashSet<Material>();
            var seenTex = new HashSet<Texture>();

            Renderer[] all = avatar.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Renderer rend = all[i];
                if (rend == null) continue;
                Material[] mats = rend.sharedMaterials;
                if (mats == null) continue;

                for (int k = 0; k < mats.Length; k++)
                {
                    Material mat = mats[k];
                    if (mat == null || !seenMats.Add(mat)) continue;
                    Shader sh = mat.shader;
                    if (sh == null) continue;

                    int props;
                    try { props = ShaderUtil.GetPropertyCount(sh); }
                    catch (Exception) { continue; }

                    for (int p = 0; p < props; p++)
                    {
                        try
                        {
                            if (ShaderUtil.GetPropertyType(sh, p) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                            Texture tex = mat.GetTexture(ShaderUtil.GetPropertyName(sh, p));
                            if (tex == null || !seenTex.Add(tex)) continue;
                            mem.Value += Profiler.GetRuntimeMemorySizeLong(tex);
                            mem.Blame(tex);
                        }
                        catch (Exception) { /* a broken shader property should not kill the scan */ }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ rig

        static void CollectRig(GameObject avatar, NekoAvatarReport r)
        {
            NekoStatSample bones = r.Get(NekoStat.Bones);
            var seen = new HashSet<Transform>();

            SkinnedMeshRenderer[] smrs = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
            {
                if (smrs[i] == null) continue;
                Transform[] bs = smrs[i].bones;
                if (bs == null) continue;
                for (int b = 0; b < bs.Length; b++)
                    if (bs[b] != null) seen.Add(bs[b]);
            }
            bones.Value = seen.Count;

            NekoStatSample anim = r.Get(NekoStat.Animators);
            Animator[] animators = avatar.GetComponentsInChildren<Animator>(true);
            anim.Value = animators.Length;
            for (int i = 0; i < animators.Length; i++) anim.Blame(animators[i].gameObject);
        }

        // ------------------------------------------------------------------ PhysBones

        static void CollectPhysBones(GameObject avatar, NekoAvatarReport r)
        {
            NekoStatSample comps     = r.Get(NekoStat.PhysBoneComponents);
            NekoStatSample transforms = r.Get(NekoStat.PhysBoneTransforms);
            NekoStatSample colliders = r.Get(NekoStat.PhysBoneColliders);
            NekoStatSample checks    = r.Get(NekoStat.PhysBoneCollisionChecks);

            List<Component> bones = FindByTypeName(avatar, "VRCPhysBone", "VRCPhysBoneBase");
            List<Component> cols  = FindByTypeName(avatar, "VRCPhysBoneCollider", "VRCPhysBoneColliderBase");

            comps.Value = bones.Count;
            colliders.Value = cols.Count;
            for (int i = 0; i < bones.Count; i++) comps.Blame(bones[i].gameObject);
            for (int i = 0; i < cols.Count; i++) colliders.Blame(cols[i].gameObject);

            // The exact chain-walking rules (multi-child handling, endpoints) are not fully
            // documented, so these two are approximations rather than the SDK's own numbers.
            transforms.Confidence = NekoConfidence.Estimated;
            checks.Confidence = NekoConfidence.Estimated;

            for (int i = 0; i < bones.Count; i++)
            {
                Component pb = bones[i];
                Type t = pb.GetType();

                var root = GetMember(pb, t, "rootTransform") as Transform;
                if (root == null) root = pb.transform;

                var ignore = new HashSet<Transform>();
                AddTransforms(GetMember(pb, t, "ignoreTransforms"), ignore);

                bool hasEndpoint = false;
                object ep = GetMember(pb, t, "endpointPosition");
                if (ep is Vector3) hasEndpoint = ((Vector3)ep) != Vector3.zero;

                int moved = CountChain(root, ignore, hasEndpoint);
                transforms.Value += moved;
                if (moved > 0) transforms.Blame(pb.gameObject);

                int assigned = CountCollection(GetMember(pb, t, "colliders"));
                if (assigned > 0)
                {
                    checks.Value += (long)moved * assigned;
                    checks.Blame(pb.gameObject);
                }
            }
        }

        /// <summary>Descendants of a PhysBone root that the simulation moves, plus one per leaf endpoint.</summary>
        static int CountChain(Transform root, HashSet<Transform> ignore, bool hasEndpoint)
        {
            if (root == null) return 0;
            int moved = 0;
            var stack = new Stack<Transform>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Transform cur = stack.Pop();
                int kids = 0;
                for (int i = 0; i < cur.childCount; i++)
                {
                    Transform child = cur.GetChild(i);
                    if (child == null || ignore.Contains(child)) continue;
                    kids++;
                    moved++;
                    stack.Push(child);
                }
                if (kids == 0 && hasEndpoint) moved++;
            }
            return moved;
        }

        static void AddTransforms(object collection, HashSet<Transform> into)
        {
            var list = collection as IEnumerable;
            if (list == null) return;
            foreach (object o in list)
            {
                var t = o as Transform;
                if (t != null) into.Add(t);
            }
        }

        static int CountCollection(object collection)
        {
            var list = collection as ICollection;
            if (list != null) return list.Count;

            var seq = collection as IEnumerable;
            if (seq == null) return 0;
            int n = 0;
            foreach (object o in seq) if (o != null) n++;
            return n;
        }

        // ------------------------------------------------------------------ contacts

        static void CollectContacts(GameObject avatar, NekoAvatarReport r)
        {
            NekoStatSample contacts = r.Get(NekoStat.Contacts);
            List<Component> found = FindByTypeName(avatar,
                "VRCContactSender", "VRCContactReceiver", "ContactSender", "ContactReceiver");
            contacts.Value = found.Count;
            for (int i = 0; i < found.Count; i++) contacts.Blame(found[i].gameObject);
        }

        // ------------------------------------------------------------------ constraints

        static void CollectConstraints(GameObject avatar, NekoAvatarReport r)
        {
            NekoStatSample count = r.Get(NekoStat.ConstraintCount);
            NekoStatSample depth = r.Get(NekoStat.ConstraintDepth);
            depth.Confidence = NekoConfidence.Estimated;

            var carriers = new HashSet<Transform>();

            Component[] comps = avatar.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                Component c = comps[i];
                if (c == null) continue;

                bool isConstraint = c is IConstraint;
                if (!isConstraint)
                {
                    string n = c.GetType().Name;
                    isConstraint = n.StartsWith("VRC", StringComparison.Ordinal) &&
                                   n.IndexOf("Constraint", StringComparison.Ordinal) >= 0;
                }
                if (!isConstraint) continue;

                count.Value++;
                count.Blame(c.gameObject);
                carriers.Add(c.transform);
            }

            // Depth approximated as the longest run of constrained transforms along one branch.
            int deepest = 0;
            foreach (Transform t in carriers)
            {
                int d = 0;
                Transform cur = t;
                while (cur != null)
                {
                    if (carriers.Contains(cur)) d++;
                    if (cur == avatar.transform) break;
                    cur = cur.parent;
                }
                if (d > deepest) deepest = d;
            }
            depth.Value = deepest;
        }

        // ------------------------------------------------------------------ particles

        static void CollectParticles(GameObject avatar, NekoAvatarReport r)
        {
            NekoStatSample systems  = r.Get(NekoStat.ParticleSystems);
            NekoStatSample active   = r.Get(NekoStat.ParticlesActive);
            NekoStatSample meshPoly = r.Get(NekoStat.MeshParticlePolys);
            NekoStatSample trails   = r.Get(NekoStat.ParticleTrails);
            NekoStatSample collide  = r.Get(NekoStat.ParticleCollision);

            ParticleSystem[] ps = avatar.GetComponentsInChildren<ParticleSystem>(true);
            systems.Value = ps.Length;

            for (int i = 0; i < ps.Length; i++)
            {
                ParticleSystem p = ps[i];
                if (p == null) continue;
                systems.Blame(p.gameObject);

                int max = p.main.maxParticles;
                active.Value += max;
                if (max > 0) active.Blame(p.gameObject);

                if (p.trails.enabled) { trails.Value = 1; trails.Blame(p.gameObject); }
                if (p.collision.enabled) { collide.Value = 1; collide.Blame(p.gameObject); }

                var pr = p.GetComponent<ParticleSystemRenderer>();
                if (pr != null && pr.renderMode == ParticleSystemRenderMode.Mesh && pr.mesh != null)
                {
                    if (!pr.mesh.isReadable) r.UnreadableParticles.Add(p);
                    long poly = TriangleCount(pr.mesh) * max;
                    meshPoly.Value += poly;
                    if (poly > 0) meshPoly.Blame(p.gameObject);
                }
            }
        }

        // ------------------------------------------------------------------ everything else

        static void CollectMisc(GameObject avatar, NekoAvatarReport r)
        {
            Fill<TrailRenderer>(avatar, r.Get(NekoStat.TrailRenderers));
            Fill<LineRenderer>(avatar, r.Get(NekoStat.LineRenderers));
            Fill<Light>(avatar, r.Get(NekoStat.Lights));
            Fill<AudioSource>(avatar, r.Get(NekoStat.AudioSources));
            Fill<Collider>(avatar, r.Get(NekoStat.PhysicsColliders));
            Fill<Rigidbody>(avatar, r.Get(NekoStat.PhysicsRigidbodies));

            NekoStatSample cloths = r.Get(NekoStat.Cloths);
            NekoStatSample clothVerts = r.Get(NekoStat.ClothVertices);
            Cloth[] cs = avatar.GetComponentsInChildren<Cloth>(true);
            cloths.Value = cs.Length;
            for (int i = 0; i < cs.Length; i++)
            {
                if (cs[i] == null) continue;
                cloths.Blame(cs[i].gameObject);
                Vector3[] v = cs[i].vertices;
                if (v != null)
                {
                    clothVerts.Value += v.Length;
                    clothVerts.Blame(cs[i].gameObject);
                }
            }
        }

        static void Fill<T>(GameObject avatar, NekoStatSample sample) where T : Component
        {
            T[] found = avatar.GetComponentsInChildren<T>(true);
            sample.Value = found.Length;
            for (int i = 0; i < found.Length; i++)
                if (found[i] != null) sample.Blame(found[i].gameObject);
        }

        // ------------------------------------------------------------------ bounds

        static void CollectBounds(GameObject avatar, NekoAvatarReport r)
        {
            // Measured in the avatar's own space so a scaled or rotated scene object still
            // reports the size VRChat would see. Still an approximation of the SDK's own maths.
            r.Get(NekoStat.BoundsSize).Confidence = NekoConfidence.Estimated;

            Renderer[] all = avatar.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            Bounds local = new Bounds(Vector3.zero, Vector3.zero);
            Matrix4x4 toLocal = avatar.transform.worldToLocalMatrix;

            for (int i = 0; i < all.Length; i++)
            {
                Renderer rend = all[i];
                if (rend == null) continue;

                Bounds w = rend.bounds;
                if (w.size == Vector3.zero) continue;

                Vector3 c = w.center, e = w.extents;
                for (int corner = 0; corner < 8; corner++)
                {
                    var p = new Vector3(
                        c.x + ((corner & 1) == 0 ? -e.x : e.x),
                        c.y + ((corner & 2) == 0 ? -e.y : e.y),
                        c.z + ((corner & 4) == 0 ? -e.z : e.z));
                    Vector3 lp = toLocal.MultiplyPoint3x4(p);
                    if (!any) { local = new Bounds(lp, Vector3.zero); any = true; }
                    else local.Encapsulate(lp);
                }
            }

            r.BoundsSize = any ? local.size : Vector3.zero;
        }

        // ------------------------------------------------------------------ reflection helpers

        /// <summary>Finds SDK components by type name so no SDK assembly reference is needed.</summary>
        static List<Component> FindByTypeName(GameObject avatar, params string[] names)
        {
            var hits = new List<Component>();
            Component[] comps = avatar.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                Component c = comps[i];
                if (c == null) continue;

                Type t = c.GetType();
                while (t != null && t != typeof(Component))
                {
                    bool match = false;
                    for (int n = 0; n < names.Length; n++)
                    {
                        if (t.Name == names[n]) { match = true; break; }
                    }
                    if (match) { hits.Add(c); break; }
                    t = t.BaseType;
                }
            }
            return hits;
        }

        static object GetMember(object instance, Type t, string name)
        {
            FieldInfo f = t.GetField(name, Members);
            if (f != null) return f.GetValue(instance);
            PropertyInfo p = t.GetProperty(name, Members);
            if (p != null && p.CanRead)
            {
                try { return p.GetValue(instance, null); }
                catch (Exception) { return null; }
            }
            return null;
        }
    }
}
