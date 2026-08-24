namespace NekoSune.AnimationTools.Compatibility
{
    /// <summary>
    /// Keeps the historical NekoSune.AnimationTools.Editor assembly identity resolvable for
    /// Unity/Burst caches and projects upgraded from Animation Tools 1.0.x. The real authoring
    /// code lives in NekoSune.AnimationTools.Authoring.Editor.
    /// </summary>
    internal static class NekoAnimationToolsAssemblyCompatibility
    {
        internal const int Version = 1;
    }
}
