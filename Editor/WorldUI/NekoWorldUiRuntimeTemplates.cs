using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NekoSune.WorldUI.Editor
{
    internal static class NekoWorldUiRuntimeTemplates
    {
        [MenuItem("NekoSune/World/UI Builder/Generate VRChat Player Action Starter", false, 30)]
        public static void GenerateVrchatPlayerActions()
        {
            string folder = EnsureFolder("Assets/NekoSune/WorldUI/Generated");
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string assetPath = folder + "/NekoWorldUiVrchatActions.cs";
            string absolute = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute)) File.WriteAllText(absolute, VrchatActionsSource, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("NekoSune World UI Builder", "Generated:\n\n" + assetPath + "\n\nAdd it to a helper GameObject after UdonSharp compiles it, assign targets, then connect Button/Toggle events to the public methods you need.", "OK");
        }

        static string EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            return current;
        }

        const string VrchatActionsSource = @"using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class NekoWorldUiVrchatActions : UdonSharpBehaviour
{
    [Header(\"Optional targets\")]
    public GameObject targetObject;
    public Transform teleportTarget;
    public Animator targetAnimator;
    public AudioSource targetAudio;
    public string animatorBoolParameter = \"Enabled\";
    public string animatorTriggerParameter = \"Trigger\";

    public void ToggleObject()
    {
        if (targetObject != null) targetObject.SetActive(!targetObject.activeSelf);
    }

    public void EnableObject()
    {
        if (targetObject != null) targetObject.SetActive(true);
    }

    public void DisableObject()
    {
        if (targetObject != null) targetObject.SetActive(false);
    }

    public void RespawnPlayer()
    {
        if (Networking.LocalPlayer != null) Networking.LocalPlayer.Respawn();
    }

    public void TeleportPlayer()
    {
        if (Networking.LocalPlayer == null || teleportTarget == null) return;
        Networking.LocalPlayer.TeleportTo(teleportTarget.position, teleportTarget.rotation);
    }

    public void ToggleAnimatorBool()
    {
        if (targetAnimator == null || animatorBoolParameter == \"\") return;
        targetAnimator.SetBool(animatorBoolParameter, !targetAnimator.GetBool(animatorBoolParameter));
    }

    public void TriggerAnimator()
    {
        if (targetAnimator != null && animatorTriggerParameter != \"\") targetAnimator.SetTrigger(animatorTriggerParameter);
    }

    public void PlayAudio()
    {
        if (targetAudio != null) targetAudio.Play();
    }

    public void StopAudio()
    {
        if (targetAudio != null) targetAudio.Stop();
    }
}
";
    }
}
