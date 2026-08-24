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

The World Hub keeps a Unity 2021.3 minimum so lightweight cross-platform World addons can still be used in ChilloutVR CCK 3 legacy projects, while VRChat-specific gameplay packages target the current VRChat Unity generation.

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
- shared avatar reflection/performance API used by other NekoSune addons

## NekoSune World Tools

Branch: `world-tools`  
Package: `com.nekosune.world-tools`

- World Template Guide
- lightweight World extension helpers
- Runtime/Udon starter-layout documentation

## NekoSune World UI Builder

Branch: `world-ui-builder`  
Package: `com.nekosune.world-ui-builder`

Beginner-first visual/data-driven world-space UI authoring for VRChat, ChilloutVR, both platforms, or generic Unity UI.

Included:

- editable JSON UI blueprints
- World Settings, Mirrors, Teleports, Media, Image Gallery, Supporter/Patreon-style Wall, Shop/Catalog, Links, Credits, Player Controls, Admin/Debug, Rules and Event templates
- Heading, Text, Button, Toggle, Slider, Image, Card, Divider and Spacer elements
- Neko Dark, Light, Neon, Glass, Pastel, Terminal and Custom themes
- local images and runtime `RawImage` slots
- local JSON, Editor-downloaded JSON snapshots and VRChat runtime JSON starters
- generated VRChat player-action, JSON and image-loading starter scripts
- optional `VRCUiShape` / `CVRCanvasWrapper` setup
- UI Doctor and beginner learning explanations

Shop/supporter templates are presentation/catalog UIs. They do not pretend to process external payments inside the world.

## NekoSune World Gameplay

Branch: `world-gameplay`  
Package: `com.nekosune.world-gameplay`

A VRChat-specific gameplay system builder focused on Persistence and AI Navigation.

### Persistence Builder

Create a schema visually:

```text
prefix: nekogame_

coins          Int       0
xp             Int       0
level          Int       1
hasSword       Bool      false
playerName     String    Adventurer
```

Then generate readable UdonSharp containing:

- `OnPlayerRestored` gating before PlayerData access
- unique/prefixed PlayerData keys
- typed Get/Set helpers
- Add helpers for numeric fields
- default initialization
- storage usage display
- storage warning/exceeded callbacks
- reset-to-defaults helper

Built-in schema presets:

- Clicker / Currency
- Idle / Incremental
- Flappy-style High Score
- Simple RPG / Inventory

### AI Navigation Builder

Creates a beginner patrol rig with editable waypoints and a readable `NavMeshAgent` UdonSharp starter. Creators can then expand it into wander, follow, guard, escort or game NPC logic.

## NekoSune World Data

Branch: `world-data`  
Package: `com.nekosune.world-data`

Remote-data helpers for VRChat Worlds:

- URL/JSON tester in the Unity Editor
- `VRCStringDownloader` starter generator
- `VRCJson` / `DataDictionary` / `DataList` feed starter
- `VRCImageDownloader` + `RawImage` starter
- trusted-host hints
- queue/rate-limit guidance
- downloaded-image disposal guidance
- example news and catalog feeds

This package is intended to power UI Builder content such as event boards, patch notes, staff lists, image galleries, supporter displays and live catalog data.

## NekoSune World Economy

Branch: `world-economy`  
Package: `com.nekosune.world-economy`

Beginner helpers for VRChat Creator Economy sellers.

Included:

- UdonProduct ownership unlock starter
- ownership checks after `OnPurchasesLoaded`
- quantity-aware `OnPurchaseConfirmedMultiple`
- expiry refresh handling
- Open World Store button
- Open Listing button
- supporter wall using `Store.ListProductOwners`
- starter World Store / Supporter UI

The package never processes payments itself. It connects world Udon to VRChat's own Creator Economy/Store APIs.

## NekoSune World Starter Games

Branch: `world-starter-games`  
Package: `com.nekosune.world-starter-games`

A beginner game kit built around world-space UI + Persistence. It depends on World Gameplay and World UI Builder so creators can inspect the game, extend its save schema, and redesign its UI.

### Neko Flappy

A Flappy-style educational UI mini-game:

- UI bird and moving pipe obstacles
- Jump input + FLAP button
- START button
- persistent best score
- persistent run count
- simple persistent medal counter

### Neko Clicker

- persistent coins
- persistent lifetime clicks
- persistent upgrade level
- increasing click value
- upgrade-cost loop

### Neko Idle

- persistent coins
- persistent production rate
- persistent upgrade level
- persistent lifetime production
- local per-frame generation
- batched PlayerData save every 10 seconds instead of writing every frame

Starter build flow:

```text
Build Selected Starter
        ↓
complete world-space UI is created
        ↓
readable UdonSharp is copied to Assets
        ↓
Unity / UdonSharp compiles
        ↓
Auto-Wire Selected Starter
        ↓
script is attached + fields/buttons are wired
        ↓
VRChat Build & Test
```

The demos are intentionally editable teaching examples, not opaque precompiled game systems.

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

The World packages are designed to compose rather than replace each other:

```text
World Hub
│
├─ World UI Builder ─────────────── visual UI/pages/HUD
│       │
│       ├──── World Data ───────── JSON / strings / images
│       ├──── World Economy ────── products / store / supporters
│       └──── Starter Games ────── example game HUDs
│
├─ World Gameplay ──────────────── Persistence / inventory schema / AI
│       └──── Starter Games ────── persistent game examples
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
→ inventory / HUD / settings pages

World Data
→ remote event/news/gallery data

World Economy
→ VIP/product ownership + supporter list

Starter Game Kit
→ learn from working Flappy/clicker/idle examples
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

For a new World addon:

1. create a new branch from `world` or the closest existing addon;
2. use a unique package ID such as `com.nekosune.my-world-addon`;
3. depend on `com.nekosune.worlds`;
4. reference assembly `NekoSune.Worlds.Editor`;
5. add your `[NekoAddon]` registration;
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
