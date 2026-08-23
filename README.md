# NekoSune VRChat Tools

A modular VCC/VPM suite for VRChat and social-VR creators.

The repository is built around **two lightweight base Hub/template packages** and separately installable feature addons. The base branches stay small so new features can be created on new branches without copying the whole toolbox.

## VCC repository

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

## Architecture

```text
avatar
└─ com.nekosune.avatars
   NekoSune Avatar Hub
   ├─ Hub
   ├─ About
   ├─ localization/styles
   └─ public addon discovery API

world
└─ com.nekosune.worlds
   NekoSune World Hub
   ├─ Hub
   ├─ About
   ├─ localization/styles
   └─ public addon discovery API

Installable addons
├─ avatar-tools
├─ world-tools
├─ world-ui-builder
├─ optimizer
├─ doctors
└─ converters
```

The Hub branches are **not bundles**. They are the stable menu/template layer. Addons register themselves through the public `INekoAddon` + `[NekoAddon]` contract and are discovered by reflection when Unity loads their assemblies.

That means adding a new addon does not require editing a central registry or changing the Hub source.

## Base Hub packages

| Branch | Package ID | Purpose |
| --- | --- | --- |
| `avatar` | `com.nekosune.avatars` | Avatar Hub, About page, localization/styles and addon API |
| `world` | `com.nekosune.worlds` | World Hub, About page, localization/styles and addon API |

The World Hub uses a Unity 2021.3 minimum so lightweight World-Hub addons can also be used with ChilloutVR CCK 3 legacy projects while remaining usable in newer VRChat/CCK 4 projects.

### Avatar Hub menu

```text
NekoSune
└── Avatar
    ├── Hub
    └── About
```

### World Hub menu

```text
NekoSune
└── World
    ├── Hub
    └── About
```

If no addons are installed, the Hub simply explains which addon packages are available. As soon as an addon assembly implementing the appropriate Hub interface is installed, its card appears automatically.

---

## Addon packages

### NekoSune Avatar Tools

Branch: `avatar-tools`

Package: `com.nekosune.avatar-tools`

Contains:

- **Lip Sync Studio**
- **Rank Advisor**
- shared avatar reflection/performance API used by other NekoSune addons

It depends on the Avatar Hub, so installing Avatar Tools automatically installs the base Avatar menu/Hub.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar-tools
```

### NekoSune World Tools

Branch: `world-tools`

Package: `com.nekosune.world-tools`

Contains:

- **World Template Guide**
- lightweight world extension helpers
- Runtime/Udon starter-layout documentation

It depends on the World Hub and automatically contributes its World card there.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#world-tools
```

### NekoSune World UI Builder

Branch: `world-ui-builder`

Package: `com.nekosune.world-ui-builder`

A beginner-first visual/data-driven World UI authoring addon for **VRChat, ChilloutVR, both platforms, or generic Unity world-space UI**.

Included:

- editable JSON UI blueprints instead of one hard-coded layout
- templates for world settings, mirrors, teleports, media controls, image galleries, supporter/Patreon-style walls, shops/catalogs, links, credits, player controls, admin/debug, rules and event schedules
- Heading, Text, Button, Toggle, Slider, Image, Card, Divider and Spacer elements
- editable **Neko Dark, Light, Neon, Glass, Pastel, Terminal and Custom** themes
- local images plus runtime `RawImage` slots
- local JSON, Editor-downloaded JSON snapshots and VRChat runtime JSON starter support
- example JSON feeds for supporters, shops, events, galleries, staff/creators, leaderboards and links
- safe Unity action wiring for simple object/page/audio actions
- generated VRChat UdonSharp starter helpers for JSON, remote images, teleport, respawn, object toggles, Animator actions and audio
- optional VRChat `VRCUiShape` / ChilloutVR `CVRCanvasWrapper` setup through reflection
- **UI Doctor** for Canvas, raycaster, platform wrapper, navigation, target size, text size and unfinished runtime-action checks
- built-in beginner learning explanations for Canvas/RectTransform/layout/UI events/JSON/platform differences

Patreon/shop templates are informational catalog/support UIs; the package does not pretend to process external payments inside a world or invent a browser API where the runtime does not expose one.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#world-ui-builder
```

### NekoSune Optimizer

Branch: `optimizer`

Package: `com.nekosune.optimizer`

Avatar modules:

- Rank-driven **Compressor**
- mesh cleanup/material-slot merging
- mesh import compression
- **PC → Quest Assistant**
- **VRAM / Texture Inspector**
- particle-budget controls
- safe PhysBone-collider cleanup assistance

World modules:

- **World Optimizer**
- scene geometry/material estimates
- texture-memory and oversized-texture review
- realtime lighting/shadow review
- particle/audio review

Optimizer registers with both Hubs automatically.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#optimizer
```

### NekoSune Doctors

Branch: `doctors`

Package: `com.nekosune.doctors`

Avatar diagnostics:

- Avatar / Preflight Doctor
- PhysBone Doctor
- Face Tracking Doctor
- Expression + Animator Doctor

World diagnostics:

- World Doctor
- Udon Network Doctor

Doctors registers with both Hubs and remains separate from Optimizer/Converters.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#doctors
```

### NekoSune Converters

Branch: `converters`

Package: `com.nekosune.converters`

Cross-platform conversion only:

- ChilloutVR **CCK 4 stable**
- ChilloutVR **CCK 3 legacy**
- VRChat Avatar → CVR Avatar
- Unity/VRChat object → CVR Prop / Spawnable
- VRChat World → CVR World
- Advanced Avatar Settings / Animator toggle conversion where supported
- optional PhysBone → Dynamic Bone bridge
- VRChat Avatar → Resonite through the installed Modular Avatar / NDMF Resonite backend

Converters registers Avatar/Prop cards with the Avatar Hub and World conversion cards with the World Hub.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#converters
```

---

## Automatic Hub registration

The base packages expose a public addon contract.

A minimal Avatar addon looks like:

```csharp
using NekoSune.Avatars.Editor;
using UnityEditor;

[NekoAddon(Order = 100)]
public sealed class MyAvatarAddon : INekoAddon
{
    public string Id => "my-avatar-addon";
    public string TitleKey => "My Avatar Addon";
    public string DescriptionKey => "Describe the feature.";
    public string CategoryKey => "cat.avatar";
    public string Glyph => "+";
    public bool IsAvailable => true;

    public void Open()
        => EditorApplication.ExecuteMenuItem("NekoSune/Avatar/My Addon");
}
```

For World addons use `NekoSune.Worlds.Editor.INekoAddon` and the World Hub assembly.

The Hub scans loaded assemblies automatically. No switch statement, JSON registry, or Hub source edit is required.

## Creating a new addon branch

For a new Avatar addon:

1. Create a new branch from `avatar` or from the closest existing addon.
2. Give the branch/package a unique ID such as `com.nekosune.my-avatar-addon`.
3. Add `com.nekosune.avatars` as a VPM dependency.
4. Reference assembly `NekoSune.Avatars.Editor`.
5. Add your tool and an `[NekoAddon]` registration class.
6. Put menu entries under `NekoSune → Avatar → ...`.
7. Add a release workflow that creates a ZIP with `package.json` at ZIP root.
8. Bump the package version to publish; the shared listing will discover the release automatically.

For a new World addon, do the same from `world`, depend on `com.nekosune.worlds`, reference `NekoSune.Worlds.Editor`, and use `NekoSune → World → ...` menus.

Starter files are included directly on the two base branches:

```text
avatar/Templates/AvatarAddonTemplate.cs.txt
world/Templates/WorldAddonTemplate.cs.txt
```

## Dependency layout

```text
Avatar Hub ────────────┬─ Avatar Tools
                       ├─ Optimizer
                       ├─ Doctors
                       └─ Converters

World Hub ─────────────┬─ World Tools
                       ├─ World UI Builder
                       ├─ Optimizer
                       ├─ Doctors
                       └─ Converters

Avatar Tools
└─ shared avatar analysis used by Optimizer / Doctors / Converters
```

The Hub packages themselves do not force the opposite VRChat SDK into a project. Cross-platform/mixed addons use reflection/runtime detection where practical.

## Current release families

```text
avatars-v0.7.1
worlds-v0.5.2
avatar-tools-v1.1.1
world-tools-v1.1.0
world-ui-builder-v1.0.0
optimizer-v1.1.0
doctors-v1.1.0
converters-v1.1.0
```

## Publishing / listing

Every package branch has its own release workflow. Release ZIPs put `package.json` directly at ZIP root.

`main/source.json` points the VRChat package-list builder at this repository once. The builder then discovers every compatible release ZIP and publishes all package IDs into the same VCC repository:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

`Website/` renders package cards dynamically from that generated listing, so newly published addon packages can appear without manually editing the website package list.

## Main branch

```text
main
├── .github/workflows/build-listing.yml
├── Website/
├── source.json
└── README.md
```

## License

See the relevant package branch for licensing information; otherwise all rights reserved by NekoSune.
