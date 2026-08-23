using UnityEngine;

namespace NekoSune.Avatars.Editor
{
    /// <summary>The five VRChat avatar performance ranks, ordered best to worst.</summary>
    internal enum NekoRank { Excellent = 0, Good = 1, Medium = 2, Poor = 3, VeryPoor = 4 }

    /// <summary>PC and the mobile family (Android / Quest / iOS), which share one limit table.</summary>
    internal enum NekoPlatform { PC = 0, Mobile = 1 }

    internal enum NekoStatFormat { Count, Bytes, Bool, Meters }

    /// <summary>
    /// How far a measured value can be trusted. Exact means we counted the same thing the SDK
    /// counts; Estimated means we approximate a rule the SDK does not fully document; NotMeasured
    /// means we deliberately do not guess, and the reported rank is a best case.
    /// </summary>
    internal enum NekoConfidence { Exact, Estimated, NotMeasured }

    internal enum NekoStat
    {
        Triangles,
        TextureMemory,
        SkinnedMeshes,
        BasicMeshes,
        MaterialSlots,
        PhysBoneComponents,
        PhysBoneTransforms,
        PhysBoneColliders,
        PhysBoneCollisionChecks,
        Contacts,
        ConstraintCount,
        ConstraintDepth,
        Animators,
        Bones,
        Lights,
        ParticleSystems,
        ParticlesActive,
        MeshParticlePolys,
        ParticleTrails,
        ParticleCollision,
        TrailRenderers,
        LineRenderers,
        Raycasts,
        Cloths,
        ClothVertices,
        PhysicsColliders,
        PhysicsRigidbodies,
        AudioSources,
        BoundsSize
    }

    internal sealed class NekoStatDef
    {
        public NekoStat Stat;
        public string LabelKey;
        public NekoStatFormat Format;

        /// <summary>Inclusive upper bound for Excellent, Good, Medium, Poor. Anything above Poor is Very Poor.</summary>
        public long[] Pc;
        public long[] Mobile;

        /// <summary>The component is removed by VRChat on mobile, so the stat always reads zero there.</summary>
        public bool StrippedOnMobile;

        public long[] Limits(NekoPlatform p) { return p == NekoPlatform.PC ? Pc : Mobile; }
    }

    /// <summary>
    /// The official avatar performance limits, transcribed from VRChat's
    /// avatar-performance-ranking-system docs. Worst stat wins: the avatar's rank is the lowest
    /// rank any single stat produces.
    /// </summary>
    internal static class NekoPerfTable
    {
        const long MB = 1024L * 1024L;

        public const int RankCount = 5;

        static NekoStatDef Def(NekoStat s, string key, NekoStatFormat f, long[] pc, long[] mobile, bool stripped = false)
        {
            return new NekoStatDef { Stat = s, LabelKey = key, Format = f, Pc = pc, Mobile = mobile, StrippedOnMobile = stripped };
        }

        // Bool stats encode the allowed value per tier: 0 = must be off, 1 = may be on.
        static readonly long[] BoolOffOffOnOn = { 0, 0, 1, 1 };
        static readonly long[] BoolOffOffOffOn = { 0, 0, 0, 1 };

        public static readonly NekoStatDef[] Defs =
        {
            Def(NekoStat.Triangles,               "rank.stat.triangles",      NekoStatFormat.Count,
                new long[] { 32000, 70000, 70000, 70000 }, new long[] { 7500, 10000, 15000, 20000 }),

            Def(NekoStat.TextureMemory,           "rank.stat.textureMemory",  NekoStatFormat.Bytes,
                new long[] { 40 * MB, 75 * MB, 110 * MB, 150 * MB }, new long[] { 10 * MB, 18 * MB, 25 * MB, 40 * MB }),

            Def(NekoStat.SkinnedMeshes,           "rank.stat.skinnedMeshes",  NekoStatFormat.Count,
                new long[] { 1, 2, 8, 16 }, new long[] { 1, 1, 2, 2 }),

            Def(NekoStat.BasicMeshes,             "rank.stat.basicMeshes",    NekoStatFormat.Count,
                new long[] { 4, 8, 16, 24 }, new long[] { 1, 1, 2, 2 }),

            Def(NekoStat.MaterialSlots,           "rank.stat.materialSlots",  NekoStatFormat.Count,
                new long[] { 4, 8, 16, 32 }, new long[] { 1, 1, 2, 4 }),

            Def(NekoStat.PhysBoneComponents,      "rank.stat.pbComponents",   NekoStatFormat.Count,
                new long[] { 4, 8, 16, 32 }, new long[] { 0, 4, 6, 8 }),

            Def(NekoStat.PhysBoneTransforms,      "rank.stat.pbTransforms",   NekoStatFormat.Count,
                new long[] { 16, 64, 128, 256 }, new long[] { 0, 16, 32, 64 }),

            Def(NekoStat.PhysBoneColliders,       "rank.stat.pbColliders",    NekoStatFormat.Count,
                new long[] { 4, 8, 16, 32 }, new long[] { 0, 4, 8, 16 }),

            Def(NekoStat.PhysBoneCollisionChecks, "rank.stat.pbChecks",       NekoStatFormat.Count,
                new long[] { 32, 128, 256, 512 }, new long[] { 0, 16, 32, 64 }),

            Def(NekoStat.Contacts,                "rank.stat.contacts",       NekoStatFormat.Count,
                new long[] { 8, 16, 24, 32 }, new long[] { 2, 4, 8, 16 }),

            Def(NekoStat.ConstraintCount,         "rank.stat.constraints",    NekoStatFormat.Count,
                new long[] { 100, 250, 300, 350 }, new long[] { 30, 60, 120, 150 }),

            Def(NekoStat.ConstraintDepth,         "rank.stat.constraintDepth", NekoStatFormat.Count,
                new long[] { 20, 50, 80, 100 }, new long[] { 5, 15, 35, 50 }),

            Def(NekoStat.Animators,               "rank.stat.animators",      NekoStatFormat.Count,
                new long[] { 1, 4, 16, 32 }, new long[] { 1, 1, 1, 2 }),

            Def(NekoStat.Bones,                   "rank.stat.bones",          NekoStatFormat.Count,
                new long[] { 75, 150, 256, 400 }, new long[] { 75, 90, 150, 150 }),

            Def(NekoStat.Lights,                  "rank.stat.lights",         NekoStatFormat.Count,
                new long[] { 0, 0, 0, 1 }, null, true),

            Def(NekoStat.ParticleSystems,         "rank.stat.particleSystems", NekoStatFormat.Count,
                new long[] { 0, 4, 8, 16 }, new long[] { 0, 0, 0, 2 }),

            Def(NekoStat.ParticlesActive,         "rank.stat.particlesActive", NekoStatFormat.Count,
                new long[] { 0, 300, 1000, 2500 }, new long[] { 0, 0, 0, 200 }),

            Def(NekoStat.MeshParticlePolys,       "rank.stat.meshParticlePolys", NekoStatFormat.Count,
                new long[] { 0, 1000, 2000, 5000 }, new long[] { 0, 0, 0, 400 }),

            Def(NekoStat.ParticleTrails,          "rank.stat.particleTrails", NekoStatFormat.Bool,
                BoolOffOffOnOn, BoolOffOffOffOn),

            Def(NekoStat.ParticleCollision,       "rank.stat.particleCollision", NekoStatFormat.Bool,
                BoolOffOffOnOn, BoolOffOffOffOn),

            Def(NekoStat.TrailRenderers,          "rank.stat.trailRenderers", NekoStatFormat.Count,
                new long[] { 1, 2, 4, 8 }, new long[] { 0, 0, 0, 1 }),

            Def(NekoStat.LineRenderers,           "rank.stat.lineRenderers",  NekoStatFormat.Count,
                new long[] { 1, 2, 4, 8 }, new long[] { 0, 0, 0, 1 }),

            Def(NekoStat.Raycasts,                "rank.stat.raycasts",       NekoStatFormat.Count,
                new long[] { 1, 4, 8, 15 }, new long[] { 1, 2, 4, 8 }),

            Def(NekoStat.Cloths,                  "rank.stat.cloths",         NekoStatFormat.Count,
                new long[] { 0, 1, 1, 1 }, null, true),

            Def(NekoStat.ClothVertices,           "rank.stat.clothVertices",  NekoStatFormat.Count,
                new long[] { 0, 50, 100, 200 }, null, true),

            Def(NekoStat.PhysicsColliders,        "rank.stat.physColliders",  NekoStatFormat.Count,
                new long[] { 0, 1, 8, 8 }, null, true),

            Def(NekoStat.PhysicsRigidbodies,      "rank.stat.physRigidbodies", NekoStatFormat.Count,
                new long[] { 0, 1, 8, 8 }, null, true),

            Def(NekoStat.AudioSources,            "rank.stat.audioSources",   NekoStatFormat.Count,
                new long[] { 1, 4, 8, 8 }, null, true),

            Def(NekoStat.BoundsSize,              "rank.stat.bounds",         NekoStatFormat.Meters,
                null, null)
        };

        /// <summary>Bounding box allowance per tier. Identical on PC and mobile.</summary>
        public static readonly Vector3[] BoundsLimits =
        {
            new Vector3(2.5f, 2.5f, 2.5f),
            new Vector3(4f, 4f, 4f),
            new Vector3(5f, 6f, 5f),
            new Vector3(5f, 6f, 5f)
        };

        /// <summary>Raycast components are hard capped per avatar on every platform.</summary>
        public const int RaycastHardCap = 80;

        public static NekoStatDef Find(NekoStat s)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].Stat == s) return Defs[i];
            return null;
        }

        /// <summary>Worst rank produced by a single numeric stat. Values above the Poor limit are Very Poor.</summary>
        public static NekoRank RankOf(NekoStatDef def, NekoPlatform platform, long value)
        {
            if (def == null) return NekoRank.Excellent;
            if (platform == NekoPlatform.Mobile && def.StrippedOnMobile) return NekoRank.Excellent;

            long[] limits = def.Limits(platform);
            if (limits == null) return NekoRank.Excellent;

            for (int tier = 0; tier < limits.Length; tier++)
                if (value <= limits[tier]) return (NekoRank)tier;

            return NekoRank.VeryPoor;
        }

        /// <summary>The bounding box must fit inside the tier's box on every axis.</summary>
        public static NekoRank RankOfBounds(Vector3 size)
        {
            for (int tier = 0; tier < BoundsLimits.Length; tier++)
            {
                Vector3 lim = BoundsLimits[tier];
                if (size.x <= lim.x && size.y <= lim.y && size.z <= lim.z) return (NekoRank)tier;
            }
            return NekoRank.VeryPoor;
        }

        /// <summary>Largest value that still reaches <paramref name="rank"/>, or -1 when the stat is not ranked.</summary>
        public static long LimitFor(NekoStatDef def, NekoPlatform platform, NekoRank rank)
        {
            if (def == null || rank == NekoRank.VeryPoor) return -1;
            if (platform == NekoPlatform.Mobile && def.StrippedOnMobile) return -1;
            long[] limits = def.Limits(platform);
            if (limits == null) return -1;
            return limits[(int)rank];
        }

        public static bool IsRanked(NekoStatDef def, NekoPlatform platform)
        {
            if (def == null) return false;
            if (def.Stat == NekoStat.BoundsSize) return true;
            if (platform == NekoPlatform.Mobile && def.StrippedOnMobile) return false;
            return def.Limits(platform) != null;
        }

        public static string RankKey(NekoRank r)
        {
            switch (r)
            {
                case NekoRank.Excellent: return "rank.excellent";
                case NekoRank.Good:      return "rank.good";
                case NekoRank.Medium:    return "rank.medium";
                case NekoRank.Poor:      return "rank.poor";
                default:                 return "rank.veryPoor";
            }
        }

        public static Color RankColor(NekoRank r)
        {
            switch (r)
            {
                case NekoRank.Excellent: return new Color(0.33f, 0.80f, 0.55f);
                case NekoRank.Good:      return new Color(0.45f, 0.75f, 0.35f);
                case NekoRank.Medium:    return new Color(0.95f, 0.72f, 0.25f);
                case NekoRank.Poor:      return new Color(0.94f, 0.51f, 0.24f);
                default:                 return new Color(0.92f, 0.30f, 0.32f);
            }
        }

        /// <summary>A short glyph so the rank reads at a glance without relying on colour alone.</summary>
        public static string RankGlyph(NekoRank r)
        {
            switch (r)
            {
                case NekoRank.Excellent: return "★";
                case NekoRank.Good:      return "●";
                case NekoRank.Medium:    return "◆";
                case NekoRank.Poor:      return "▲";
                default:                 return "✖";
            }
        }
    }
}
