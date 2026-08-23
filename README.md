# NekoSune VRChat Tools

A modular VCC/VPM suite for VRChat creators.

The repository now uses **feature packages** instead of putting every editor tool into one Avatar or World package. `main` remains the shared VCC listing/landing branch.

## VCC repository

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

## Modular packages

| Branch | Package ID | Package | Main purpose |
| --- | --- | --- | --- |
| `avatar-tools` | `com.nekosune.avatar-tools` | NekoSune Avatar Tools | Lip Sync Studio + Rank Advisor + shared avatar analysis API |
| `world-tools` | `com.nekosune.world-tools` | NekoSune World Tools | World framework + Template Guide + shared world UI API |
| `optimizer` | `com.nekosune.optimizer` | NekoSune Optimizer | Avatar Compressor/Mesh/Quest/VRAM + World Optimizer |
| `doctors` | `com.nekosune.doctors` | NekoSune Doctors | Avatar/PhysBone/Face/Animator + World/Udon diagnostics |
| `converters` | `com.nekosune.converters` | NekoSune Converters | ChilloutVR CCK 3/4 Avatar/Prop/World + Resonite export |

### Compatibility bundles

The original IDs remain available so existing projects are not abandoned:

| Branch | Package ID | Behaviour |
| --- | --- | --- |
| `avatar` | `com.nekosune.avatars` | Bundle that installs Avatar Tools + Optimizer + Doctors + Converters + VRChat Avatars SDK |
| `world` | `com.nekosune.worlds` | Bundle that installs World Tools + Optimizer + Doctors + Converters + VRChat Worlds SDK |

The bundle branches contain **no duplicate editor implementation**. The real source now lives in the modular branches.

---

## NekoSune Avatar Tools

Branch: `avatar-tools`

Package: `com.nekosune.avatar-tools`

Included:

- **Lip Sync Studio**
- **Rank Advisor**
- localization/framework used by avatar-facing NekoSune modules
- shared reflection/performance utilities used by Optimizer, Doctors and Converters

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar-tools
```

---

## NekoSune World Tools

Branch: `world-tools`

Package: `com.nekosune.world-tools`

Included:

- **World Hub**
- **World Template Guide**
- shared world UI/framework used by Optimizer, Doctors and Converters

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#world-tools
```

---

## NekoSune Optimizer

Branch: `optimizer`

Package: `com.nekosune.optimizer`

### Avatar optimization

- Rank-driven **Compressor**
- safe mesh cleanup/material-slot merging
- mesh import compression
- Quest/mobile preparation
- Android-only texture overrides
- VRAM / Texture Inspector
- particle budget controls
- safe PhysBone-collider cleanup assistance

### World optimization

- **World Optimizer**
- scene triangle/material estimates
- unique texture and estimated texture-memory review
- oversized texture warnings
- realtime light/shadow review
- particle/audio counts
- performance advisories kept separate from build/network diagnostics

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#optimizer
```

---

## NekoSune Doctors

Branch: `doctors`

Package: `com.nekosune.doctors`

### Avatar Doctors

- Avatar / Preflight Doctor
- PhysBone Doctor
- Face Tracking Doctor
- Expression + Animator Doctor

### World Doctors

- World Doctor
- Udon Network Doctor

Optimization belongs to Optimizer; conversion belongs to Converters.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#doctors
```

---

## NekoSune Converters

Branch: `converters`

Package: `com.nekosune.converters`

All cross-platform conversion is isolated here.

### ChilloutVR CCK

Supports runtime detection for:

- **CCK 4 stable**
- **CCK 3 legacy**

Included conversion paths:

- VRChat Avatar → ChilloutVR Avatar
- Unity/VRChat hierarchy → ChilloutVR Prop / Spawnable
- VRChat World → ChilloutVR World
- Advanced Avatar Settings conversion
- Bool/Float/Int avatar control conversion where supported
- pickup/object-sync/interactable conversion for props
- non-destructive world scene copy with supported spawn/mirror/station/video/sync/toggle conversion
- optional PhysBone → Dynamic Bone bridge when Dynamic Bone is installed

### Resonite

The Resonite exporter is part of the same cross-platform Converters package. It uses the installed Modular Avatar / NDMF Resonite backend rather than inventing a second incompatible `.resonitepackage` format.

Development Git URL:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#converters
```

---

## Dependency layout

```text
Avatar Tools ─────────────┐
                          ├─ Optimizer
World Tools ──────────────┤
                          ├─ Doctors
                          └─ Converters

Avatar Bundle
  ├─ VRChat Avatars SDK
  ├─ Avatar Tools
  ├─ Optimizer
  ├─ Doctors
  └─ Converters

World Bundle
  ├─ VRChat Worlds SDK
  ├─ World Tools
  ├─ Optimizer
  ├─ Doctors
  └─ Converters
```

Avatar Tools and World Tools deliberately do not force the opposite VRChat SDK into the project. The modular feature packages use reflection/runtime detection where practical.

---

## Current release families

```text
avatar-tools-v1.0.0
world-tools-v1.0.0
optimizer-v1.0.0
doctors-v1.0.0
converters-v1.0.0
avatars-v0.6.0
worlds-v0.4.0
```

Each release ZIP has `package.json` directly at its root so the shared VRChat package listing can index it.

## Publishing

Each package branch has its own release workflow. Bumping that branch's `package.json` version creates its package-specific release tag and then triggers the shared `main` listing rebuild.

`source.json` only needs this repository in `githubRepos`; the package-list builder discovers all compatible release ZIPs and publishes them into one `index.json`.

## Main branch

`main` contains:

```text
.github/workflows/build-listing.yml
Website/
source.json
README.md
```

The website renders package cards dynamically from the generated listing, so new modular packages appear automatically when their releases are indexed.

## License

See the relevant package branch for licensing information; otherwise all rights reserved by NekoSune.
