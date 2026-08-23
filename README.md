# NekoSune VRChat Tools

Unity Editor tooling for VRChat creators, split into separate **Avatar** and **World/Udon** VPM packages.

`main` is the **VCC/VPM package-listing branch** and project landing page. Package source lives on the `avatar` and `world` branches.

## Packages

| Branch | Package ID | For | Status |
| --- | --- | --- | --- |
| [`avatar`](../../tree/avatar) | `com.nekosune.avatars` | VRChat avatars | Working |
| [`world`](../../tree/world) | `com.nekosune.worlds` | VRChat worlds / Udon | Working — World Doctor + Network Doctor |
| `main` | `com.nekosune.vrchat-tools` | VCC repository + docs | Active listing |

### Avatar package

The `avatar` branch contains:

- **Lip Sync Studio**
- **Rank Advisor**
- avatar-specific localization and editor code

See the [`avatar` README](../../tree/avatar) for full documentation.

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

### World package

- Unity 2022.3+
- package ID: `com.nekosune.worlds`
- VPM dependency: `com.vrchat.worlds`

VCC resolves the appropriate VRChat SDK package for each package type.

---

## Unity menus

Avatar package:

- **NekoSune → Hub**
- **NekoSune → Avatar → Lip Sync Studio**
- **NekoSune → Avatar → Rank Advisor**

World package:

- **NekoSune → World → Hub**
- **NekoSune → World → World Doctor**
- **NekoSune → World → Udon Network Doctor**
- **NekoSune → World → Template Guide**

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
