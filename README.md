# NekoSune VRChat Tools

A modular VCC/VPM suite for VRChat and social-VR creators.

The repository uses two lightweight **Hub/template packages** plus separately installable addons. New features live on their own branches/packages and register themselves in the Avatar or World Hub automatically.

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
   ├─ shared styles/localization
   └─ public addon discovery API

world
└─ com.nekosune.worlds
   NekoSune World Hub
   ├─ Hub
   ├─ About
   ├─ shared styles/localization
   └─ public addon discovery API

Installable addons
├─ avatar-tools
├─ world-tools
├─ world-ui-builder
├─ world-gameplay
├─ world-data
├─ world-economy
├─ world-starter-games
├─ world-gallery
├─ world-avatar-search
├─ world-player-tools
├─ optimizer
├─ doctors
└─ converters
```

The Hub branches are **not bundles**. An addon implements `INekoAddon` + `[NekoAddon]`; the Hub discovers its card from the loaded assembly. No central registry edit is required for each new feature branch.

## Base packages

| Branch | Package ID | Purpose |
| --- | --- | --- |
| `avatar` | `com.nekosune.avatars` | Avatar Hub, About, styles/localization and addon API |
| `world` | `com.nekosune.worlds` | World Hub, About, styles/localization and addon API |

### Hub menus

```text
NekoSune
├── Avatar
│   ├── Hub
│   └── About
└── World
    ├── Hub
    └── About
```

Installed addon cards appear automatically in those Hub windows.

---

# Addon packages

## NekoSune Avatar Tools

Branch: `avatar-tools`  
Package: `com.nekosune.avatar-tools`

- Lip Sync Studio
- Rank Advisor
- shared avatar reflection/performance API

## NekoSune World Tools

Branch: `world-tools`  
Package: `com.nekosune.world-tools`

- World Template Guide
- lightweight World extension helpers
- Runtime/Udon starter-layout documentation

## NekoSune World UI Builder

Branch: `world-ui-builder`  
Package: `com.nekosune.world-ui-builder`

Beginner-first world-space UI authoring for VRChat, ChilloutVR and generic Unity UI.

- editable JSON UI blueprints
- settings, mirrors, teleport, media, gallery, supporter wall, shop/catalog, links, credits, player, admin, rules and event templates
- Heading, Text, Button, Toggle, Slider, Image, Card, Divider and Spacer elements
- Neko Dark, Light, Neon, Glass, Pastel, Terminal and Custom themes
- local images and runtime `RawImage` slots
- JSON/image/action starter scripts
- optional `VRCUiShape` / `CVRCanvasWrapper` setup
- UI Doctor + beginner learning explanations

## NekoSune World Gameplay

Branch: `world-gameplay`  
Package: `com.nekosune.world-gameplay`

VRChat gameplay-system builder:

- visual Persistence schema builder
- prefixed PlayerData keys
- `OnPlayerRestored` gating
- typed Get/Set/Add helpers
- storage usage/warning helpers
- Clicker, Idle, Flappy and RPG/Inventory starter schemas
- AI Navigation / NavMeshAgent patrol starter

## NekoSune World Data

Branch: `world-data`  
Package: `com.nekosune.world-data`

Remote-data helpers:

- URL/JSON tester
- `VRCStringDownloader`
- `VRCJson`, `DataDictionary`, `DataList`
- `VRCImageDownloader` + `RawImage`
- trusted-host/rate-limit guidance
- safe downloaded-image disposal
- example news/catalog feeds

## NekoSune World Economy

Branch: `world-economy`  
Package: `com.nekosune.world-economy`

VRChat Creator Economy helpers:

- UdonProduct ownership unlocks
- `OnPurchasesLoaded` lifecycle
- quantity-aware purchase events
- World Store / Listing buttons
- supporter wall via `Store.ListProductOwners`
- starter store/supporter UI

The package never processes payments itself; it connects World Udon to VRChat's own Economy APIs.

## NekoSune World Starter Games

Branch: `world-starter-games`  
Package: `com.nekosune.world-starter-games`

Beginner-editable persistent UI examples:

- **Neko Flappy** — flap input, moving pipes, collisions, persistent best score/runs/medals
- **Neko Clicker** — coins, lifetime clicks, upgrades
- **Neko Idle** — production rate, upgrades, lifetime production, batched persistence saves

```text
Build Selected Starter
        ↓
complete world-space UI
        ↓
readable UdonSharp copied to Assets
        ↓
Unity / UdonSharp compiles
        ↓
Auto-Wire Selected Starter
        ↓
VRChat Build & Test
```

## NekoSune World Gallery

Branch: `world-gallery`  
Package: `com.nekosune.world-gallery`

Advanced animated image-gallery/slideshow builder.

Data sources:

- local `Texture[]`
- raw `string[]` titles/subtitles
- embedded JSON pasted into the inspector
- `string[]` raw JSON-object rows
- predeclared remote `VRCUrl[]`
- remote JSON metadata through `VRCStringDownloader` + `VRCJson`
- flexible root keys and field mapping

Transitions:

- Cross Fade
- Slide Left / Right / Up
- Zoom
- Spin + Zoom
- shader Wipe
- shader Dissolve
- shader Radial Reveal

The demo builds a three-layer `RawImage` gallery, generates example textures, creates the transition material, copies readable UdonSharp into `Assets`, and auto-wires the controls after compilation.

Remote JSON image strings cannot be turned into arbitrary new `VRCUrl` values by Udon. Remote images are therefore predeclared and JSON maps to them using `imageIndex` or a matching URL string.

## NekoSune World Avatar Search

Branch: `world-avatar-search`  
Package: `com.nekosune.world-avatar-search`

Stylish VRChat avatar-browser/search starter with a flexible JSON adapter.

The included demo is configured for:

```text
https://vrcavatarsearch.nekosunevr.co.uk/vrcx_search?search=Rindo
```

Default VRCX-style fields:

```text
id
name
authorName
description
thumbnailImageUrl
releaseStatus
```

The mapper also accepts root wrappers such as `avatars`, `results`, `items`, and `data`, with aliases including `avatarId`, `avatar_id`, `avatarName`, `title`, `author`, `creatorName`, `desc`, and `summary`.

The generated UI includes:

- VRChat `VRCUrlInputField`
- preset **DEMO RINDO** search
- eight styled result cards
- selected-avatar detail panel
- 3D `VRCAvatarPedestal` preview
- **PREVIEW** button
- **USE AVATAR** button using `SetAvatarUse(Networking.LocalPlayer)`

Because VRChat cannot construct arbitrary `VRCUrl` objects from ordinary runtime strings, free-form user searches use a complete URL entered through `VRCUrlInputField`; creator-defined preset URLs can be generated at editor time.

## NekoSune World Player Tools

Branch: `world-player-tools`  
Package: `com.nekosune.world-player-tools`

Player/world interaction helpers. The first module is **Player Teleport Builder**.

Generated UI:

- player selector
- destination selector
- Teleport Me
- Request Selected Player
- Refresh Players
- Allow Teleport Requests consent toggle
- status/feedback panel

`TeleportTo` only moves the local player in VRChat. Remote-player movement is therefore implemented as a parameterized `[NetworkCallable]` request containing a target player ID and destination index. Every client receives it, only the target handles it, and the target only teleports itself when its local consent switch is ON. Consent defaults **OFF**.

The demo creates three editable destination Transforms (`Lobby`, `Games`, `Gallery` by default) and auto-wires the UdonSharp behaviour after compilation.

## NekoSune Optimizer

Branch: `optimizer`  
Package: `com.nekosune.optimizer`

Avatar:
- Rank-driven Compressor
- Mesh Compression
- PC → Quest Assistant
- VRAM / Texture Inspector
- particle and PhysBone optimization helpers

World:
- World Optimizer
- geometry/material estimates
- texture-memory review
- realtime light/shadow review
- particle/audio review

## NekoSune Doctors

Branch: `doctors`  
Package: `com.nekosune.doctors`

Avatar:
- Avatar Doctor
- PhysBone Doctor
- Face Tracking Doctor
- Expression + Animator Doctor

World:
- World Doctor
- Udon Network Doctor

## NekoSune Converters

Branch: `converters`  
Package: `com.nekosune.converters`

- ChilloutVR CCK 4 stable
- ChilloutVR CCK 3 legacy
- VRChat Avatar → CVR Avatar
- object → CVR Prop / Spawnable
- VRChat World → CVR World
- Advanced Avatar Settings / Animator mappings where supported
- Resonite avatar export through the installed backend

---

# World creator stack

```text
World Hub
│
├─ World UI Builder ───────── visual pages / settings / HUD
├─ World Gallery ──────────── advanced animated image UI
├─ World Avatar Search ────── JSON/VRCX avatar browser + pedestal
├─ World Player Tools ─────── destination/player teleport UI
│
├─ World Data ─────────────── JSON / strings / images
├─ World Economy ──────────── products / store / supporters
├─ World Gameplay ─────────── Persistence / inventories / AI
├─ World Starter Games ────── Flappy / clicker / idle demos
│
├─ Optimizer
├─ Doctors
└─ Converters
```

Example beginner workflow:

```text
Persistence Builder
→ coins / XP / unlock schema

World UI Builder
→ HUD / settings / inventory pages

World Gallery
→ animated image/catalog/event slideshow

World Avatar Search
→ avatar browser/search station

World Player Tools
→ teleport hub / room navigation

World Data
→ JSON / image feeds

World Economy
→ VIP/product ownership + supporters

Starter Game Kit
→ learn from working persistent games
```

---

# Automatic Hub registration

A minimal World addon can register itself like this:

```csharp
using NekoSune.Worlds.Editor;

[NekoAddon(Order = 100)]
public sealed class MyWorldAddon : INekoAddon
{
    public string Id => "my-world-addon";
    public string TitleKey => "My World Addon";
    public string DescriptionKey => "Describe the feature.";
    public string CategoryKey => "cat.world";
    public string Glyph => "+";
    public bool IsAvailable => true;
    public void Open() { /* open your EditorWindow */ }
}
```

The Hub scans loaded assemblies automatically.

## Creating another addon branch

1. create a new branch from `world` or the closest existing addon;
2. give it a unique package ID such as `com.nekosune.my-world-addon`;
3. depend on `com.nekosune.worlds`;
4. reference `NekoSune.Worlds.Editor`;
5. add `[NekoAddon]` registration;
6. put menus under `NekoSune → World → ...`;
7. publish a ZIP with `package.json` at ZIP root.

Templates remain on the base branches:

```text
avatar/Templates/AvatarAddonTemplate.cs.txt
world/Templates/WorldAddonTemplate.cs.txt
```

# Publishing / listing

Every feature branch has its own GitHub release workflow. `main/source.json` points the VCC package-list builder at this repository once; compatible release ZIPs are collected into the same listing:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

`Website/` renders package cards from that generated listing.

## License

See the relevant package branch for licensing information; otherwise all rights reserved by NekoSune.
