# NekoSune VRChat Tools

Unity Editor tooling for VRChat creators, split into separate **Avatar** and **World/Udon** VPM packages.

`main` is the **VCC/VPM package-listing branch** and project landing page. Package source lives on the `avatar` and `world` branches.

## Packages

| Branch | Package ID | For | Status |
| --- | --- | --- | --- |
| [`avatar`](../../tree/avatar) | `com.nekosune.avatars` | VRChat avatars | Working |
| [`world`](../../tree/world) | `com.nekosune.worlds` | VRChat worlds / Udon | World template ready |
| `main` | `com.nekosune.vrchat-tools` | VCC repository + docs | Active listing |

### Avatar package

The `avatar` branch contains the avatar-focused editor tools, including:

- **Lip Sync Studio**
- **Rank Advisor**
- avatar-specific localization and editor code

See the [`avatar` README](../../tree/avatar) for full documentation.

### World package

The `world` branch is now a clean package of its own rather than a copy of the avatar branch.

It contains:

- package ID `com.nekosune.worlds`
- dependency on `com.vrchat.worlds`
- `NekoSune.Worlds.Editor` editor assembly
- **NekoSune → World → Hub**
- a starter **World Template Guide**
- `Editor/World/` for editor-side world tooling
- `Runtime/Udon/` for future UdonSharp/runtime content
- a dedicated VPM release workflow using `worlds-v<version>` tags

Avatar Lip Sync/viseme/binder code has been removed from the `world` branch.

See the [`world` README](../../tree/world) for the package layout and contributor guide.

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
├── .github/
│   └── workflows/
│       └── build-listing.yml
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
├── Runtime/
│   └── Udon/
├── package.json        com.nekosune.worlds
├── CHANGELOG.md
└── README.md
```

`source.json` lists this GitHub repository as a release source. VRChat's package-list builder scans compatible GitHub Release `.zip` assets, reads the root `package.json` from each ZIP, hashes them, and publishes the combined `index.json`.

That means the same VCC repository can expose multiple package IDs from the same GitHub repository.

---

## Current VPM release families

Avatar releases use tags such as:

```text
avatars-v0.1.0
```

World releases use tags such as:

```text
worlds-v0.1.0
```

Each release ZIP has `package.json` directly at the ZIP root, which is required by the listing builder.

A normal GitHub branch archive is not used as the VPM package because GitHub wraps branch downloads in an extra top-level directory.

---

## Unity Package Manager / Git installs

### Avatar

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

or:

```bash
cd YourUnityProject/Packages
git clone -b avatar https://github.com/NekoSuneVR/NekoSuneUdonScripts.git com.nekosune.avatars
```

### World

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#world
```

or:

```bash
cd YourUnityProject/Packages
git clone -b world https://github.com/NekoSuneVR/NekoSuneUdonScripts.git com.nekosune.worlds
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
- **NekoSune → World → Template Guide**

The world Hub uses its own submenu so both packages can be installed without defining the same `NekoSune → Hub` menu command twice.

---

## Publishing updates

### Avatar

1. Change the avatar source on `avatar`.
2. Bump the version in `avatar/package.json`.
3. The Avatar release workflow creates a `avatars-v<version>` release and VPM ZIP.
4. The shared VCC listing rebuilds.

### World

1. Change the world/Udon source on `world`.
2. Bump the version in `world/package.json`.
3. The World release workflow creates a `worlds-v<version>` release and VPM ZIP.
4. The shared VCC listing rebuilds.

Published versions are immutable: the workflows refuse to replace an existing release version.

---

## VCC listing

The shared generated repository is:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

It is designed to contain both:

```text
com.nekosune.avatars
com.nekosune.worlds
```

The listing is rebuilt after package releases and when the listing configuration on `main` changes.

## Contributing

Open changes against the branch that owns the code:

- avatar tooling → `avatar`
- world/Udon tooling → `world`
- VCC listing / landing page → `main`

## License

See the relevant package branch for licensing information; otherwise all rights reserved by NekoSune.
