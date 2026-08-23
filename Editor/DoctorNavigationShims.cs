using UnityEditor;

namespace NekoSune.Avatars.Editor
{
    // Keeps Optimizer independent from com.nekosune.doctors while preserving the buttons.
    internal static class NekoAvatarDoctorWindow { public static void Open() { EditorApplication.ExecuteMenuItem("NekoSune/Avatar/Avatar Doctor"); } }
    internal static class NekoPhysBoneDoctorWindow { public static void Open() { EditorApplication.ExecuteMenuItem("NekoSune/Avatar/PhysBone Doctor"); } }
    internal static class NekoExpressionAnimatorDoctorWindow { public static void Open() { EditorApplication.ExecuteMenuItem("NekoSune/Avatar/Expression and Animator Doctor"); } }
}
