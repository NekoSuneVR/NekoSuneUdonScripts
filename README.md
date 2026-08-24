# NekoSune VRChat Tools

A modular VCC/VPM suite for VRChat and social-VR creators.

The repository uses lightweight **Avatar** and **World Hub/template packages** plus separately installable addons. Addons register themselves in the appropriate Hub automatically, so new features can live on their own branches without editing one giant package.

## VCC repository

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

## Package architecture

```text
avatar
└─ com.nekosune.avatars
   NekoSune Avatar Hub

world
└─ com.nekosune.worlds
   NekoSune World Hub

Installable addons
├─ avatar-tools
├─ animation-tools
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

The Hub branches are templates/base UI packages, **not bundles**. An addon implements `INekoAddon` + `[NekoAddon]`; the Hub discovers loaded addon assemblies automatically.

---

# Avatar creator addons

## NekoSune Avatar Tools

Branch: `avatar-tools`  
Package: `com.nekosune.avatar-tools`

The lightweight shared Avatar helper package.

- shared avatar reflection/performance API used by other NekoSune addons
- beginner **Toggle + Menu Builder**
  - Bool Expression Parameter
  - Expression Menu Toggle
  - OFF / ON `.anim` clips
  - FX Animator Bool parameter
  - FX OFF/ON state layer
  - creates/reuses expression assets and FX controller when the VRChat Avatars SDK is installed

Lip Sync Studio is no longer stored here. Rank Advisor is no longer stored here.

## NekoSune Animation Tools

Branch: `animation-tools`  
Package: `com.nekosune.animation-tools`

Music/animation authoring addon shared by the Avatar and World Hubs.

### Lip Sync

- existing NekoSune Lip Sync Studio moved into this package
- AudioClip analysis and viseme animation generation
- editor audio preview

### Beat / drop mapper

- waveform timeline
- audio scrub / preview seeking
- bass/low-frequency onset analysis
- beat, kick and drop markers
- presets for:
  - Hardstyle
  - Uptempo
  - Frenchcore
  - custom BPM/sensitivity

Marker colors in the timeline:

```text
green   = beat
amber   = kick / bass hit
pink    = detected drop
```

Detection is an editor assistant, not a promise of perfect musical transcription. Manual keyframing remains available.

### Auto + manual keyframing

**Auto mode** discovers real Unity animatable bindings from the selected hierarchy object and writes attack → hit → decay curves to one `.anim` file.

This can work with properties Unity exposes from:

- Transform / humanoid-bone objects
- ParticleSystem components
- Renderer/material/shader properties
- Lights and other animatable components
- legally installed third-party effect shaders

**Manual mode** creates an empty `.anim` plus a beat/kick/drop timestamp guide, leaving all keys to the creator.

### Timed lyrics

Accepts exact timestamps such as:

```text
[00:12.350]First line
[00:16.100]Second line
```

or:

```text
12.350|First line
16.100|Second line
```

Outputs:

- World: generated 3D `TextMesh` lyric objects + exact-time visibility `.anim`
- Avatar/generic: exact-time animation of existing child mesh objects
- Shader atlas: stepped lyric-index float curve for a compatible installed material/shader

The tool uses supplied timestamps exactly. It does **not** pretend to automatically transcribe song lyrics from audio.

### Third-party shaders

NekoSune does **not** include, leak, redistribute, or unlock paid/community shaders or creator assets.

Animation Tools can keyframe installed shader properties when Unity exposes them as animatable. Examples creators may legally obtain from their official source include:

- Doppelgänger / Dope Shader — use the creator's official Patreon/Discord and current licence/tier terms
- Leviant ScreenSpace Ubershader — use Leviant's official repository/distribution and licence
- Poiyomi and other creator shaders/effect packages

The package contains information/links only; third-party shader files are not bundled.

## NekoSune Optimizer

Branch: `optimizer`  
Package: `com.nekosune.optimizer`

### Avatar

- **Rank Advisor** — moved from Avatar Tools
- PC/mobile VRChat avatar Performance Rank analysis
- **Build Size Advisor**
  - PC compressed/download size
  - PC uncompressed size
  - Android/Quest/mobile compressed/download size
  - Android/Quest/mobile uncompressed size
- Compressor
- Mesh Compression
- PC → Quest Assistant
- VRAM / Texture Inspector
- particle and PhysBone optimization helpers

Current Avatar Build Size Advisor caps:

```text
PC               200 MB download / 500 MB uncompressed
Android/mobile    10 MB download /  40 MB uncompressed
```

The advisor attempts to read the latest built bundle sizes from Unity `Editor.log`, with manual values available when SDK log wording changes. Source FBX/texture size is not treated as the final VRChat bundle size.

### World

- World Optimizer
- **World Platform Advisor**
  - PC scene/build review
  - Android/mobile scene/build review
  - compressed/uncompressed last-build display
  - geometry/material/texture/light/particle/audio snapshot

VRChat does not publish an official World Performance Rank equivalent to avatar ranks, so NekoSune does not invent one. Android world builds are checked against the 100 MB compressed upload limit. PC world ~200 MB is shown as public-world size guidance, not as an official rank/hard cap.

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

# World creator addons

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

- settings, mirrors, teleport, media, gallery, supporter wall, shop/catalog, links, credits, player, admin, rules and event templates
- Heading, Text, Button, Toggle, Slider, Image, Card, Divider and Spacer elements
- Neko Dark, Light, Neon, Glass, Pastel, Terminal and Custom themes
- editable JSON UI blueprints
- local and remote-image starter support
- JSON/action starter scripts
- optional `VRCUiShape` / `CVRCanvasWrapper` setup
- UI Doctor + beginner learning explanations

## NekoSune World Gameplay

Branch: `world-gameplay`  
Package: `com.nekosune.world-gameplay`

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

- UdonProduct ownership unlocks
- `OnPurchasesLoaded` lifecycle
- quantity-aware purchase events
- World Store / Listing buttons
- supporter wall via `Store.ListProductOwners`
- starter store/supporter UI

The package connects World Udon to VRChat's Creator Economy APIs; it does not process external payments itself.

## NekoSune World Starter Games

Branch: `world-starter-games`  
Package: `com.nekosune.world-starter-games`

- **Neko Flappy** — persistent high score/runs/medals
- **Neko Clicker** — persistent coins/clicks/upgrades
- **Neko Idle** — persistent production/upgrades with batched saves

The starter builder creates the world-space UI, copies readable UdonSharp into `Assets`, then Auto-Wire attaches the compiled behaviour after Unity/UdonSharp finishes compiling.

## NekoSune World Gallery

Branch: `world-gallery`  
Package: `com.nekosune.world-gallery`

Sources:

- local `Texture[]`
- raw title/subtitle arrays
- embedded JSON
- raw JSON-object rows
- predeclared remote `VRCUrl[]`
- remote JSON metadata via `VRCStringDownloader` + `VRCJson`

Transitions:

- Cross Fade
- Slide Left / Right / Up
- Zoom
- Spin + Zoom
- shader Wipe
- shader Dissolve
- shader Radial Reveal

## NekoSune World Avatar Search

Branch: `world-avatar-search`  
Package: `com.nekosune.world-avatar-search`

Flexible JSON/VRCX-style VRChat avatar browser.

Demo endpoint:

```text
https://vrcavatarsearch.nekosunevr.co.uk/vrcx_search?search=Rindo
```

Default mapping supports root arrays with:

```text
id
name
authorName
description
thumbnailImageUrl
releaseStatus
```

and wrapped APIs using `avatars`, `results`, `items`, or `data` plus common field aliases.

Paging:

- creator default: **5 or 10 results/page**
- player controls: **5 / PAGE** and **10 / PAGE**
- Previous / Next page
- up to 128 mapped results by default
- ten reusable result cards

Avatar interaction:

- `VRCUrlInputField` search
- 3D `VRCAvatarPedestal` preview
- PREVIEW
- USE AVATAR via `SetAvatarUse(Networking.LocalPlayer)`

## NekoSune World Player Tools

Branch: `world-player-tools`  
Package: `com.nekosune.world-player-tools`

- player selector
- destination selector
- Teleport Me
- Request Selected Player
- Refresh Players
- consent toggle for incoming teleport requests

VRChat `TeleportTo` only moves the local player. Remote movement is therefore a parameterized consent-based network request; the target client teleports itself only when that player's local consent switch is ON. Consent defaults OFF.

---

# Suggested creator stack

```text
Avatar Hub
├─ Avatar Tools ───────────── toggles / menus / shared API
├─ Animation Tools ───────── Lip Sync / beat mapping / keyframes / lyrics
├─ Optimizer ─────────────── Rank / size / Quest / Compressor
├─ Doctors
└─ Converters

World Hub
├─ World UI Builder
├─ Animation Tools ───────── beat/drop animation + 3D timed lyrics
├─ World Gallery
├─ World Avatar Search
├─ World Player Tools
├─ World Data
├─ World Economy
├─ World Gameplay
├─ World Starter Games
├─ Optimizer
├─ Doctors
└─ Converters
```

# Automatic Hub registration

A World addon can register itself like this:

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
    public void Open() { }
}
```

The Hub scans loaded assemblies automatically.

Starter templates remain on the base branches:

```text
avatar/Templates/AvatarAddonTemplate.cs.txt
world/Templates/WorldAddonTemplate.cs.txt
```

# Publishing / listing

Feature branches publish VPM ZIP releases with `package.json` at ZIP root. `main/source.json` points the listing builder at this repository once, and compatible releases are collected into the shared VCC listing:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

## License

See the relevant package branch for licensing information; otherwise all rights reserved by NekoSune.
