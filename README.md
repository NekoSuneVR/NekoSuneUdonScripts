# NekoSune Worlds

World and Udon tooling for VRChat, packaged separately from the NekoSune avatar tools.

**Package ID:** `com.nekosune.worlds`

This branch contains only world-focused tooling. Avatar Lip Sync, avatar binders, visemes, and avatar performance tools remain in `com.nekosune.avatars`.

## Included tools

### World Doctor

Open **NekoSune → World → World Doctor**.

World Doctor scans the active Unity scene and gives creators one place to review common performance and build-readiness problems before opening the VRChat SDK build panel.

It currently reports:

- missing `VRCSceneDescriptor`;
- GameObject and renderer counts;
- approximate scene mesh triangle count;
- material slots and unique materials;
- unique textures and estimated loaded texture memory;
- very large textures;
- large uncompressed textures;
- additional Android/Quest texture advisories;
- realtime lights;
- realtime shadow-casting lights;
- realtime reflection probes;
- particle-system capacity;
- long audio clips using `Decompress On Load`;
- camera count;
- collider and Rigidbody count;
- Udon/UdonSharp behaviour count;
- Android/Quest post-processing warnings;
- a copyable diagnostic report.

The scanner distinguishes **VRChat rules/platform restrictions** from **NekoSune advisory thresholds**. Advisory thresholds are deliberately not presented as official VRChat hard limits: they are signals telling you what is worth profiling.

### Udon Network Doctor

Open **NekoSune → World → Udon Network Doctor**.

The networking scanner analyses UdonSharp C# source attached to the active scene and looks for multiplayer mistakes that are easy to miss during ordinary Unity Play Mode.

Checks currently include:

- number of `[UdonSynced]` fields;
- Manual / Continuous / None / NoVariableSync behaviour modes;
- Manual sync with no `RequestSerialization()` call in the source;
- directly synced `DataList` / `DataDictionary` fields;
- synced variables combined with `NoVariableSync`;
- network calls combined with `BehaviourSyncMode.None`;
- many fields in Continuous sync;
- string/array-like data in Continuous sync;
- `Networking.SetOwner()` usage without ownership callbacks;
- network-only behaviours that could use `NoVariableSync`;
- Udon Graph / compiled Udon count;
- a reminder to use VRChat multi-client Build & Test for real network testing;
- a copyable multiplayer diagnostic report.

Deep source checks are designed for UdonSharp. Udon Graph behaviours are counted but their internal graph is not parsed.

### World Template Guide

Open **NekoSune → World → Template Guide** when extending this package. It documents where new editor and runtime features belong.

---

## Why these tools were built first

Recent VRChat creator discussions repeatedly point to two problems:

1. world performance / Android-Quest conversion and figuring out what is actually expensive;
2. Udon networking, ownership, and synchronization being difficult to debug correctly.

VRChat also documents that ordinary Unity Play Mode does not reproduce synced variables and network events correctly; creators need Build & Test with multiple clients for those systems.

The first NekoSune Worlds release therefore focuses on catching problems *before* that expensive test loop.

---

## Install with VRChat Creator Companion

Use the shared NekoSune VCC repository:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

1. Open **VRChat Creator Companion**.
2. Go to **Settings → Packages**.
3. Choose **Add Repository**.
4. Paste the listing URL above.
5. Open a VRChat Worlds project.
6. Add **NekoSune Worlds**.

### Unity Package Manager Git URL

For development, Unity Package Manager can install the branch directly:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#world
```

That URL is for **Unity Package Manager**, not VCC.

### Manual development clone

```bash
cd YourUnityProject/Packages
git clone -b world https://github.com/NekoSuneVR/NekoSuneUdonScripts.git com.nekosune.worlds
```

---

## Requirements

- Unity **2022.3** or newer
- VRChat Worlds SDK through VCC / VPM
- package dependency: `com.vrchat.worlds`

The editor tools intentionally avoid hard compile-time references to most VRChat SDK/UdonSharp classes where reflection/source analysis is enough. This makes the package easier to keep compatible across SDK updates.

---

## Menu

After installation:

- **NekoSune → World → Hub**
- **NekoSune → World → World Doctor**
- **NekoSune → World → Udon Network Doctor**
- **NekoSune → World → Template Guide**

The world menu intentionally has its own submenu so `com.nekosune.avatars` and `com.nekosune.worlds` can be installed together.

---

## Package layout

```text
package.json
CHANGELOG.md
README.md

Editor/
  NekoSune.Worlds.Editor.asmdef
  Core/
    NekoAddon.cs
    NekoHubWindow.cs
    NekoPaths.cs
    NekoStyles.cs
  Localization/
    NekoLoc.cs
    Languages/
      en.json
  World/
    NekoWorldDoctorWindow.cs
    NekoUdonNetworkDoctorWindow.cs
    NekoWorldTemplateWindow.cs

Runtime/
  Udon/
    README.md

.github/
  workflows/
    release-world.yml
```

Use `Editor/World/` for editor-only creator utilities and `Runtime/Udon/` for behaviours, prefabs, and assets that must ship inside the built world.

---

## Planned next areas

Good next additions to this package include:

- actionable one-click safe fixes for selected World Doctor findings;
- build-size / asset-size breakdown;
- LOD and occlusion auditing;
- Android/Quest shader and import-override inspection;
- network bandwidth estimates and congestion diagnostics;
- prefab-based multiplayer test helpers;
- world setup wizard for common social-world features;
- persistence and Creator Economy helpers.

---

## Releases and VCC listing

World releases use tags such as:

```text
worlds-v0.1.0
worlds-v0.2.0
worlds-v1.0.0
```

The release workflow creates a VPM ZIP with `package.json` at the ZIP root, publishes it as a GitHub Release, and triggers the shared VCC listing rebuild.

The listing can therefore contain both:

```text
com.nekosune.avatars
com.nekosune.worlds
```

from one repository URL:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

To publish an update, bump the version in `package.json`. Existing releases are never overwritten.

---

## Branches

| Branch | Package | Purpose |
| --- | --- | --- |
| `avatar` | `com.nekosune.avatars` | VRChat avatar editor tools |
| `world` | `com.nekosune.worlds` | VRChat world and Udon tools |
| `main` | VPM listing | VCC repository and project landing page |

## License

See `LICENSE` if present; otherwise all rights reserved by NekoSune.
