using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    internal sealed class NekoStatResult
    {
        public NekoStatDef Def;
        public NekoStatSample Sample;
        public NekoRank Rank;
        public bool Ranked;

        /// <summary>Value this stat has to reach for the avatar to climb one rank, or -1.</summary>
        public long Target = -1;

        public long Value { get { return Sample.Value; } }
        public NekoStat Stat { get { return Def.Stat; } }
    }

    /// <summary>
    /// One platform's verdict: every stat's own rank, the worst-stat-wins overall rank, and the
    /// exact set of stats that must all come down together before the rank moves.
    /// </summary>
    internal sealed class NekoRankAssessment
    {
        public NekoPlatform Platform;
        public NekoRank Overall;
        public readonly List<NekoStatResult> Stats = new List<NekoStatResult>();
        public readonly List<NekoStatResult> Blockers = new List<NekoStatResult>();

        public bool ReadWriteForcedVeryPoor;
        public bool HasUnmeasured;
        public bool HasEstimates;

        /// <summary>The rank one step better than the current one, or Excellent when already there.</summary>
        public NekoRank NextRank
        {
            get { return Overall == NekoRank.Excellent ? NekoRank.Excellent : (NekoRank)((int)Overall - 1); }
        }

        public bool CanImprove { get { return Overall != NekoRank.Excellent; } }
    }

    internal static class NekoRankAdvisor
    {
        public static NekoRankAssessment Assess(NekoAvatarReport report, NekoPlatform platform)
        {
            var a = new NekoRankAssessment { Platform = platform, Overall = NekoRank.Excellent };
            if (report == null) return a;

            for (int i = 0; i < NekoPerfTable.Defs.Length; i++)
            {
                NekoStatDef def = NekoPerfTable.Defs[i];
                NekoStatSample sample = report.Get(def.Stat);

                var res = new NekoStatResult
                {
                    Def = def,
                    Sample = sample,
                    Ranked = NekoPerfTable.IsRanked(def, platform)
                };

                if (!res.Ranked)
                {
                    res.Rank = NekoRank.Excellent;
                }
                else if (def.Stat == NekoStat.BoundsSize)
                {
                    res.Rank = NekoPerfTable.RankOfBounds(report.BoundsSize);
                }
                else
                {
                    res.Rank = NekoPerfTable.RankOf(def, platform, sample.Value);
                }

                // A stat we did not measure must not be allowed to claim a rank it has not earned.
                if (sample.Confidence == NekoConfidence.NotMeasured)
                {
                    a.HasUnmeasured = true;
                    res.Rank = NekoRank.Excellent;
                }
                else if (sample.Confidence == NekoConfidence.Estimated && res.Ranked)
                {
                    a.HasEstimates = true;
                }

                a.Stats.Add(res);
                if (res.Ranked && res.Rank > a.Overall) a.Overall = res.Rank;
            }

            // Mesh Read/Write off is an automatic Very Poor and blocks the upload entirely.
            if (report.MeshReadWriteDisabled)
            {
                a.ReadWriteForcedVeryPoor = true;
                a.Overall = NekoRank.VeryPoor;
            }

            if (a.CanImprove)
            {
                NekoRank next = a.NextRank;
                for (int i = 0; i < a.Stats.Count; i++)
                {
                    NekoStatResult res = a.Stats[i];
                    if (!res.Ranked || res.Rank < a.Overall) continue;

                    res.Target = res.Stat == NekoStat.BoundsSize
                        ? -1
                        : NekoPerfTable.LimitFor(res.Def, platform, next);

                    a.Blockers.Add(res);
                }

                // Worst overshoot first, so the biggest single job is at the top of the list.
                a.Blockers.Sort(delegate (NekoStatResult x, NekoStatResult y)
                {
                    float ox = Overshoot(x), oy = Overshoot(y);
                    return oy.CompareTo(ox);
                });
            }

            return a;
        }

        /// <summary>How many times over the target a blocking stat is, used only for ordering.</summary>
        static float Overshoot(NekoStatResult res)
        {
            if (res.Target <= 0) return res.Value > 0 ? float.MaxValue : 0f;
            return (float)res.Value / res.Target;
        }

        // ------------------------------------------------------------------ safe fixes

        /// <summary>
        /// Turns Read/Write back on for every model whose mesh has it disabled. This is the one
        /// automatic fix offered: it edits import settings only, never the mesh or the scene.
        /// Returns the number of models changed; <paramref name="skipped"/> counts meshes that do
        /// not come from a model importer and so cannot be fixed this way.
        /// </summary>
        public static int EnableReadWrite(NekoAvatarReport report, out int skipped)
        {
            skipped = 0;
            if (report == null) return 0;

            var meshes = new List<Mesh>();
            for (int i = 0; i < report.UnreadableRenderers.Count; i++)
            {
                Renderer rend = report.UnreadableRenderers[i];
                if (rend == null) continue;

                var smr = rend as SkinnedMeshRenderer;
                if (smr != null && smr.sharedMesh != null) { meshes.Add(smr.sharedMesh); continue; }

                var mf = rend.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) meshes.Add(mf.sharedMesh);
            }
            for (int i = 0; i < report.UnreadableParticles.Count; i++)
            {
                ParticleSystem p = report.UnreadableParticles[i];
                if (p == null) continue;
                var pr = p.GetComponent<ParticleSystemRenderer>();
                if (pr != null && pr.mesh != null) meshes.Add(pr.mesh);
            }

            var paths = new HashSet<string>();
            for (int i = 0; i < meshes.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(meshes[i]);
                if (string.IsNullOrEmpty(path)) { skipped++; continue; }
                paths.Add(path);
            }

            int changed = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in paths)
                {
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) { skipped++; continue; }
                    if (importer.isReadable) continue;
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return changed;
        }

        // ------------------------------------------------------------------ formatting

        public static string Format(NekoStatDef def, long value)
        {
            switch (def.Format)
            {
                case NekoStatFormat.Bytes:
                    return FormatBytes(value);
                case NekoStatFormat.Bool:
                    return NekoLoc.T(value != 0 ? "rank.on" : "rank.off");
                default:
                    return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static string FormatBytes(long bytes)
        {
            const long MB = 1024L * 1024L;
            if (bytes >= MB)
                return (bytes / (double)MB).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " MB";
            return (bytes / 1024.0).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
        }

        public static string FormatBounds(Vector3 size)
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            return size.x.ToString("0.0", c) + " × " + size.y.ToString("0.0", c) + " × " + size.z.ToString("0.0", c) + " m";
        }
    }
}
