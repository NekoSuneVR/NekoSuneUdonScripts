using System;
using System.Collections.Generic;
using UnityEngine;

namespace NekoSune.WorldUI.Editor
{
    internal enum NekoWorldUiPlatform { Generic, VRChat, ChilloutVR, Both }
    internal enum NekoWorldUiDataSource { Static, LocalJson, RemoteJsonSnapshot, VRChatRuntimeJson }
    internal enum NekoWorldUiTheme { NekoDark, Light, Neon, Glass, Pastel, Terminal, Custom }
    internal enum NekoWorldUiElementType { Heading, Text, Image, Button, Toggle, Slider, GridItem, Divider, Spacer }
    internal enum NekoWorldUiAction
    {
        None,
        OpenPage,
        ClosePage,
        ToggleObject,
        EnableObject,
        DisableObject,
        AnimatorBool,
        AnimatorTrigger,
        PlayAudio,
        StopAudio,
        TeleportPlayer,
        RespawnPlayer,
        OpenLinkCard,
        RefreshJson,
        Custom
    }

    [Serializable]
    internal sealed class NekoWorldUiFeedItem
    {
        public string title;
        public string subtitle;
        public string description;
        public string imageUrl;
        public string url;
        public string value;
    }

    [Serializable]
    internal sealed class NekoWorldUiFeedDocument
    {
        public List<NekoWorldUiFeedItem> items = new List<NekoWorldUiFeedItem>();
    }

    [Serializable]
    internal sealed class NekoWorldUiElement
    {
        public string id = "element";
        public NekoWorldUiElementType type = NekoWorldUiElementType.Text;
        public string label = "Text";
        public string secondary = "";
        public NekoWorldUiAction action = NekoWorldUiAction.None;
        public string actionValue = "";
        public string dataKey = "";
        public string imageUrl = "";
        public float height = 56f;
    }

    [Serializable]
    internal sealed class NekoWorldUiBlueprint
    {
        public string name = "World UI";
        public string description = "Custom world-space UI";
        public NekoWorldUiPlatform platform = NekoWorldUiPlatform.VRChat;
        public NekoWorldUiDataSource dataSource = NekoWorldUiDataSource.Static;
        public string dataUrl = "";
        public float width = 1200f;
        public float height = 800f;
        public float worldScale = 0.001f;

        public NekoWorldUiTheme theme = NekoWorldUiTheme.NekoDark;
        public Color backgroundColor = new Color(0.055f, 0.065f, 0.085f, 0.97f);
        public Color panelColor = new Color(0.09f, 0.105f, 0.14f, 0.98f);
        public Color controlColor = new Color(0.14f, 0.16f, 0.22f, 1f);
        public Color primaryColor = new Color(0.18f, 0.42f, 0.72f, 1f);
        public Color accentColor = new Color(0.32f, 0.78f, 0.58f, 1f);
        public Color textColor = Color.white;
        public Color mutedTextColor = new Color(0.72f, 0.76f, 0.84f, 1f);
        public Color linkColor = new Color(0.65f, 0.82f, 1f, 1f);
        public float spacing = 12f;
        public int baseFontSize = 26;
        public float buttonHeight = 58f;
        public float panelPadding = 16f;

        public List<NekoWorldUiElement> elements = new List<NekoWorldUiElement>();
    }

    internal static class NekoWorldUiThemePresets
    {
        public static void Apply(NekoWorldUiBlueprint b, NekoWorldUiTheme theme)
        {
            if (b == null || theme == NekoWorldUiTheme.Custom) return;
            b.theme = theme;
            b.spacing = 12f;
            b.baseFontSize = 26;
            b.buttonHeight = 58f;
            b.panelPadding = 16f;

            switch (theme)
            {
                case NekoWorldUiTheme.Light:
                    b.backgroundColor = new Color(0.90f, 0.92f, 0.96f, 0.98f);
                    b.panelColor = new Color(0.98f, 0.985f, 1f, 1f);
                    b.controlColor = new Color(0.80f, 0.83f, 0.89f, 1f);
                    b.primaryColor = new Color(0.15f, 0.42f, 0.78f, 1f);
                    b.accentColor = new Color(0.12f, 0.62f, 0.45f, 1f);
                    b.textColor = new Color(0.07f, 0.08f, 0.11f, 1f);
                    b.mutedTextColor = new Color(0.32f, 0.36f, 0.44f, 1f);
                    b.linkColor = new Color(0.08f, 0.35f, 0.75f, 1f);
                    break;
                case NekoWorldUiTheme.Neon:
                    b.backgroundColor = new Color(0.025f, 0.025f, 0.055f, 0.98f);
                    b.panelColor = new Color(0.045f, 0.055f, 0.095f, 0.98f);
                    b.controlColor = new Color(0.07f, 0.09f, 0.14f, 1f);
                    b.primaryColor = new Color(0.68f, 0.12f, 0.95f, 1f);
                    b.accentColor = new Color(0.05f, 0.95f, 0.82f, 1f);
                    b.textColor = new Color(0.94f, 0.98f, 1f, 1f);
                    b.mutedTextColor = new Color(0.65f, 0.72f, 0.85f, 1f);
                    b.linkColor = new Color(0.10f, 0.90f, 1f, 1f);
                    break;
                case NekoWorldUiTheme.Glass:
                    b.backgroundColor = new Color(0.035f, 0.055f, 0.08f, 0.76f);
                    b.panelColor = new Color(0.12f, 0.16f, 0.21f, 0.72f);
                    b.controlColor = new Color(0.18f, 0.22f, 0.28f, 0.80f);
                    b.primaryColor = new Color(0.22f, 0.57f, 0.92f, 0.88f);
                    b.accentColor = new Color(0.32f, 0.85f, 0.70f, 0.92f);
                    b.textColor = Color.white;
                    b.mutedTextColor = new Color(0.76f, 0.82f, 0.90f, 1f);
                    b.linkColor = new Color(0.55f, 0.82f, 1f, 1f);
                    break;
                case NekoWorldUiTheme.Pastel:
                    b.backgroundColor = new Color(0.18f, 0.16f, 0.24f, 0.98f);
                    b.panelColor = new Color(0.25f, 0.22f, 0.32f, 1f);
                    b.controlColor = new Color(0.33f, 0.29f, 0.40f, 1f);
                    b.primaryColor = new Color(0.92f, 0.55f, 0.76f, 1f);
                    b.accentColor = new Color(0.55f, 0.88f, 0.78f, 1f);
                    b.textColor = new Color(1f, 0.96f, 0.99f, 1f);
                    b.mutedTextColor = new Color(0.86f, 0.76f, 0.85f, 1f);
                    b.linkColor = new Color(0.66f, 0.82f, 1f, 1f);
                    break;
                case NekoWorldUiTheme.Terminal:
                    b.backgroundColor = new Color(0.015f, 0.025f, 0.018f, 0.99f);
                    b.panelColor = new Color(0.025f, 0.045f, 0.03f, 1f);
                    b.controlColor = new Color(0.035f, 0.07f, 0.045f, 1f);
                    b.primaryColor = new Color(0.05f, 0.48f, 0.20f, 1f);
                    b.accentColor = new Color(0.20f, 1f, 0.42f, 1f);
                    b.textColor = new Color(0.50f, 1f, 0.62f, 1f);
                    b.mutedTextColor = new Color(0.30f, 0.70f, 0.40f, 1f);
                    b.linkColor = new Color(0.35f, 1f, 0.78f, 1f);
                    b.spacing = 8f;
                    b.baseFontSize = 24;
                    break;
                default:
                    b.backgroundColor = new Color(0.055f, 0.065f, 0.085f, 0.97f);
                    b.panelColor = new Color(0.09f, 0.105f, 0.14f, 0.98f);
                    b.controlColor = new Color(0.14f, 0.16f, 0.22f, 1f);
                    b.primaryColor = new Color(0.18f, 0.42f, 0.72f, 1f);
                    b.accentColor = new Color(0.32f, 0.78f, 0.58f, 1f);
                    b.textColor = Color.white;
                    b.mutedTextColor = new Color(0.72f, 0.76f, 0.84f, 1f);
                    b.linkColor = new Color(0.65f, 0.82f, 1f, 1f);
                    break;
            }
        }
    }

    internal static class NekoWorldUiTemplates
    {
        public static readonly string[] Names = {
            "Blank Panel", "World Settings", "Mirror Controls", "Teleport Menu", "Media Controls",
            "Image Gallery", "Supporter / Patreon Wall", "Shop / Catalog", "Social + Links",
            "Credits / About", "Player Interaction", "Admin / Debug", "Rules / Welcome", "Event Schedule"
        };

        public static NekoWorldUiBlueprint Create(int index)
        {
            NekoWorldUiBlueprint b = Base(Names[Math.Max(0, Math.Min(index, Names.Length - 1))]);
            switch (index)
            {
                case 1:
                    b.description = "Beginner settings panel with common world controls.";
                    Add(b, NekoWorldUiElementType.Heading, "World Settings", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Toggle, "Mirror", NekoWorldUiAction.ToggleObject, "Mirror");
                    Add(b, NekoWorldUiElementType.Slider, "Music Volume", NekoWorldUiAction.Custom, "MusicVolume");
                    Add(b, NekoWorldUiElementType.Toggle, "Post Processing", NekoWorldUiAction.ToggleObject, "PostProcessing");
                    Add(b, NekoWorldUiElementType.Button, "Respawn", NekoWorldUiAction.RespawnPlayer);
                    break;
                case 2:
                    Add(b, NekoWorldUiElementType.Heading, "Mirrors", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Button, "Mirror Off", NekoWorldUiAction.DisableObject, "Mirror");
                    Add(b, NekoWorldUiElementType.Button, "Mirror On", NekoWorldUiAction.EnableObject, "Mirror");
                    Add(b, NekoWorldUiElementType.Text, "Tip: bind the target named Mirror in the generated hierarchy.", NekoWorldUiAction.None);
                    break;
                case 3:
                    Add(b, NekoWorldUiElementType.Heading, "Teleport", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Button, "Spawn", NekoWorldUiAction.TeleportPlayer, "SpawnPoint");
                    Add(b, NekoWorldUiElementType.Button, "Club", NekoWorldUiAction.TeleportPlayer, "ClubPoint");
                    Add(b, NekoWorldUiElementType.Button, "Rooftop", NekoWorldUiAction.TeleportPlayer, "RoofPoint");
                    break;
                case 4:
                    Add(b, NekoWorldUiElementType.Heading, "Media", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Button, "Play", NekoWorldUiAction.PlayAudio, "Music");
                    Add(b, NekoWorldUiElementType.Button, "Stop", NekoWorldUiAction.StopAudio, "Music");
                    Add(b, NekoWorldUiElementType.Slider, "Volume", NekoWorldUiAction.Custom, "MusicVolume");
                    Add(b, NekoWorldUiElementType.Button, "Refresh", NekoWorldUiAction.RefreshJson);
                    break;
                case 5:
                    b.description = "Image cards with captions. Use local textures/sprites or the platform runtime-image starter.";
                    Add(b, NekoWorldUiElementType.Heading, "Gallery", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Image, "Image 1", NekoWorldUiAction.None, "", "image");
                    Add(b, NekoWorldUiElementType.Text, "Caption", NekoWorldUiAction.None, "", "caption");
                    Add(b, NekoWorldUiElementType.Button, "Next", NekoWorldUiAction.Custom, "NextImage");
                    break;
                case 6:
                    b.description = "JSON-driven supporter wall for Patreon/member credits. It displays data; it does not process payments.";
                    b.dataSource = NekoWorldUiDataSource.RemoteJsonSnapshot;
                    Add(b, NekoWorldUiElementType.Heading, "Supporters", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.GridItem, "Supporter Name", NekoWorldUiAction.None, "", "title");
                    Add(b, NekoWorldUiElementType.Text, "Tier", NekoWorldUiAction.None, "", "subtitle");
                    Add(b, NekoWorldUiElementType.Text, "Message", NekoWorldUiAction.None, "", "description");
                    break;
                case 7:
                    b.description = "Catalog/shop UI for products, memberships or donation options. External checkout remains outside the world.";
                    b.dataSource = NekoWorldUiDataSource.RemoteJsonSnapshot;
                    Add(b, NekoWorldUiElementType.Heading, "Shop", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.GridItem, "Product", NekoWorldUiAction.None, "", "title");
                    Add(b, NekoWorldUiElementType.Text, "Price / tier", NekoWorldUiAction.None, "", "subtitle");
                    Add(b, NekoWorldUiElementType.Text, "Description", NekoWorldUiAction.None, "", "description");
                    Add(b, NekoWorldUiElementType.Button, "View Link", NekoWorldUiAction.OpenLinkCard, "", "url");
                    break;
                case 8:
                    Add(b, NekoWorldUiElementType.Heading, "Links", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Button, "Website", NekoWorldUiAction.OpenLinkCard, "https://example.com");
                    Add(b, NekoWorldUiElementType.Button, "Patreon", NekoWorldUiAction.OpenLinkCard, "https://patreon.com/example");
                    Add(b, NekoWorldUiElementType.Button, "Discord", NekoWorldUiAction.OpenLinkCard, "https://discord.gg/example");
                    Add(b, NekoWorldUiElementType.Text, "Use a visible URL/QR image where the platform cannot safely open arbitrary browser links.", NekoWorldUiAction.None);
                    break;
                case 9:
                    Add(b, NekoWorldUiElementType.Heading, "About This World", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Text, "Created by YourName", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Text, "Special thanks to everyone who helped.", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Button, "Project Link", NekoWorldUiAction.OpenLinkCard, "https://example.com");
                    break;
                case 10:
                    Add(b, NekoWorldUiElementType.Heading, "Player Controls", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Button, "Respawn", NekoWorldUiAction.RespawnPlayer);
                    Add(b, NekoWorldUiElementType.Button, "Teleport to Safe Area", NekoWorldUiAction.TeleportPlayer, "SafeArea");
                    Add(b, NekoWorldUiElementType.Slider, "Local Music Volume", NekoWorldUiAction.Custom, "LocalVolume");
                    Add(b, NekoWorldUiElementType.Toggle, "Comfort Effects", NekoWorldUiAction.ToggleObject, "ComfortEffects");
                    break;
                case 11:
                    Add(b, NekoWorldUiElementType.Heading, "Admin / Debug", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Toggle, "Debug Overlay", NekoWorldUiAction.ToggleObject, "DebugOverlay");
                    Add(b, NekoWorldUiElementType.Button, "Reset Animator", NekoWorldUiAction.AnimatorTrigger, "Reset");
                    Add(b, NekoWorldUiElementType.Button, "Refresh Data", NekoWorldUiAction.RefreshJson);
                    break;
                case 12:
                    Add(b, NekoWorldUiElementType.Heading, "Welcome", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Text, "Please respect other players and follow the instance rules.", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Text, "1. Be kind\n2. No harassment\n3. Ask before recording", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Button, "Close", NekoWorldUiAction.ClosePage);
                    break;
                case 13:
                    b.dataSource = NekoWorldUiDataSource.RemoteJsonSnapshot;
                    Add(b, NekoWorldUiElementType.Heading, "Events", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.GridItem, "Event title", NekoWorldUiAction.None, "", "title");
                    Add(b, NekoWorldUiElementType.Text, "Date / time", NekoWorldUiAction.None, "", "subtitle");
                    Add(b, NekoWorldUiElementType.Text, "Details", NekoWorldUiAction.None, "", "description");
                    break;
                default:
                    Add(b, NekoWorldUiElementType.Heading, "New Panel", NekoWorldUiAction.None);
                    Add(b, NekoWorldUiElementType.Text, "Select an element below and edit it, or import a JSON blueprint.", NekoWorldUiAction.None);
                    break;
            }
            return b;
        }

        static NekoWorldUiBlueprint Base(string name)
        {
            NekoWorldUiBlueprint b = new NekoWorldUiBlueprint();
            b.name = name;
            NekoWorldUiThemePresets.Apply(b, NekoWorldUiTheme.NekoDark);
            return b;
        }

        static void Add(NekoWorldUiBlueprint b, NekoWorldUiElementType type, string label, NekoWorldUiAction action, string value = "", string dataKey = "")
        {
            NekoWorldUiElement e = new NekoWorldUiElement();
            e.id = "item-" + (b.elements.Count + 1);
            e.type = type;
            e.label = label;
            e.action = action;
            e.actionValue = value;
            e.dataKey = dataKey;
            if (type == NekoWorldUiElementType.Image) e.height = 220f;
            if (type == NekoWorldUiElementType.Spacer) e.height = 20f;
            b.elements.Add(e);
        }
    }
}
