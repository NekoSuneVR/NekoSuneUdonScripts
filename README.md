# NekoSune VRChat Tools

Unity Editor tooling for VRChat creators, split into separate **Avatar** and **World/Udon** VPM packages.

`main` is the **VCC/VPM package-listing branch** and project landing page. Package source lives on the `avatar` and `world` branches.

## Packages

| Branch | Package ID | For | Status |
| --- | --- | --- | --- |
| [`avatar`](../../tree/avatar) | `com.nekosune.avatars` | VRChat avatars | Working — diagnostics, optimization and platform conversion |
| [`world`](../../tree/world) | `com.nekosune.worlds` | VRChat worlds / Udon | Working — World Doctor + Network Doctor |
| `main` | `com.nekosune.vrchat-tools` | VCC repository + docs | Active listing |

### Avatar package

The `avatar` branch now contains:

- **Avatar Doctor** — upload/preflight checks for descriptor, menus, parameters, Animator setup, performance, texture/VRAM and Quest compatibility.
- **PC → Quest Assistant** — creates a separate Quest copy, mobile-material conversions, Android texture overrides and a mobile blocker plan.
- **PhysBone Doctor** — per-chain transform/collider/collision-check analysis plus unused-collider and overlapping-root detection.
- **VRAM / Texture Inspector** — texture memory/resolution/import analysis with Android-only maximum-size overrides.
- **Face Tracking Doctor** — ARKit/Unified Expressions coverage plus core VRCFaceTracking v2 parameter setup.
- **Expression + Animator Doctor** — menu references, parameter budget/types, transitions, Write Defaults and Parameter Driver diagnostics.
- **Mesh Compressor** — safe mesh cleanup, material-slot merging, import compression and Quest mesh-readiness guidance.
- **Rank Advisor** — full PC/Quest performance ranking and blocker targets.
- **Lip Sync Studio** — audio-to-viseme animation baking.
- **Export to Resonite** — bridges to the installed experimental Modular Avatar Resonite backend and saves its real `.resonitepackage` output.
- **Convert to ChilloutVR** — CCK-gated conversion to a `CVRAvatar` copy with Advanced Avatar Settings, Bool toggles, Float sliders, Int dropdowns, viewpoint/visemes/blink and optional Dynamic Bone bridging.

The Resonite, ChilloutVR CCK and Dynamic Bone integrations are optional and are detected at runtime rather than forced into every VRChat avatar project.

See the [`avatar` README](../../tree/avatar) for full feature details and conversion limitations.

### World package

The `world` branch contains the world-focused creator toolbox:

- **World Doctor** — scans the active scene for performance/build-readiness concerns including geometry, materials, textures, estimated texture memory, realtime lighting/shadows, reflection probes, particles, audio loading, cameras, Udon count, scene descriptor, and Android/Quest-specific warnings.
- **Udon Network Doctor** — analyses attached UdonSharp source for sync modes, `[UdonSynced]`, `RequestSerialization`, direct DataList/DataDictionary sync, network-event compatibility, Continuous-sync payloads, ownership patterns, and multiplayer test reminders.
- **World Template Guide** — package extension guide for future editor/runtime features.

World findings distinguish VRChat rules/platform restrictions from NekoSune performance advisories so advisory thresholds are not presented as official hard limits.

See the [`world` README](../../tree/world) for full documentation.

---

## Install with VRChat Creator Companion

Both packages use the same VCC repository:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

1. Open **VRChat Creator Companion**.
2. Open **Settings → Packages**.
3. Choose **Add Repository**.
4. Paste the listing URL above.
5. Open a VRChat project.
6. Add the package that matches the project:
   - **NekoSune Avatars** for avatar projects.
   - **NekoSune Worlds** for world projects.

A browser can also launch VCC with:

```text
vcc://vpm/addRepo?url=https%3A%2F%2Fnekosunevr.github.io%2FNekoSuneUdonScripts%2Findex.json
```

The `.git#avatar` and `.git#world` URLs are **Unity Package Manager Git URLs**, not VCC repository URLs.

---

## Repository layout

```text
main
├── .github/workflows/build-listing.yml
├── Website/
├── source.json
└── README.md

avatar
├── .github/workflows/release-avatar.yml
├── Editor/
│   ├── Core/
│   ├── Diagnostics/
│   ├── Converters/
│   ├── MeshOptimizer/
│   ├── RankAdvisor/
│   ├── LipSync/
│   └── Localization/
├── package.json        com.nekosune.avatars
├── CHANGELOG.md
└── README.md

world
├── .github/workflows/release-world.yml
├── Editor/
│   ├── Core/
│   ├── Localization/
│   └── World/
│       ├── NekoWorldDoctorWindow.cs
│       ├── NekoUdonNetworkDoctorWindow.cs
│       └── NekoWorldTemplateWindow.cs
├── Runtime/Udon/
├── package.json        com.nekosune.worlds
├── CHANGELOG.md
└── README.md
```

`source.json` lists this GitHub repository as a release source. VRChat's package-list builder scans compatible GitHub Release `.zip` assets, reads the root `package.json` from each ZIP, hashes them, and publishes the combined `index.json`.

---

## Unity Package Manager / Git installs

### Avatar

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

### World

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#world
```

These are useful for development. Normal users should use the VCC listing.

---

## Requirements

### Avatar package

- package ID: `com.nekosune.avatars`
- VPM dependency: `com.vrchat.avatars`
- optional Resonite integration: Modular Avatar Resonite/NDMF Resonite platform
- optional ChilloutVR integration: ChilloutVR CCK
- optional CVR PhysBone bridge: Dynamic Bone v1.x

### World package

- Unity 2022.3+
- package ID: `com.nekosune.worlds`
- VPM dependency: `com.vrchat.worlds`

VCC resolves the appropriate VRChat SDK package for each package type.

---

## Unity menus

Avatar package:

- **NekoSune → Hub**
- **NekoSune → Avatar → Avatar Doctor**
- **NekoSune → Avatar → PC to Quest Assistant**
- **NekoSune → Avatar → PhysBone Doctor**
- **NekoSune → Avatar → VRAM and Texture Inspector**
- **NekoSune → Avatar → Face Tracking Doctor**
- **NekoSune → Avatar → Expression and Animator Doctor**
- **NekoSune → Avatar → Mesh Compressor**
- **NekoSune → Avatar → Rank Advisor**
- **NekoSune → Avatar → Lip Sync Studio**
- **NekoSune → Avatar → Export to Resonite**
- **NekoSune → Avatar → Convert to ChilloutVR**

World package:

- **NekoSune → World → Hub**
- **NekoSune → World → World Doctor**
- **NekoSune → World → Udon Network Doctor**
- **NekoSune → World → Template Guide**

---

## Current release families

```text
avatars-v0.3.0
worlds-v0.2.0
```

Each release workflow builds a VPM ZIP with `package.json` directly at the ZIP root and then rebuilds the shared listing.

---

## Publishing updates

### Avatar

1. Change source on `avatar`.
2. Bump `avatar/package.json`.
3. The workflow creates `avatars-v<version>` with a VPM-compatible ZIP.
4. The shared VCC listing rebuilds.

### World

1. Change source on `world`.
2. Bump `world/package.json`.
3. The workflow creates `worlds-v<version>` with a VPM-compatible ZIP.
4. The shared VCC listing rebuilds.

Published versions are immutable: the workflows refuse to replace an existing release version.

---

## VCC listing

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

It is designed to contain both:

```text
com.nekosune.avatars
com.nekosune.worlds
```

## Contributing

- avatar tooling → `avatar`
- world/Udon tooling → `world`
- VCC listing / landing page → `main`

## License

See the relevant package branch for licensing information; otherwise all rights reserved by NekoSune.
