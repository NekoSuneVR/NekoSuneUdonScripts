# NekoSune Avatars

A Unity Editor toolbox for VRChat avatar creation, diagnostics, optimization, Quest preparation, face tracking, and cross-platform conversion.

**Package ID:** `com.nekosune.avatars`

World/Udon tooling is shipped separately as `com.nekosune.worlds`.

## Install with VRChat Creator Companion

Add the shared NekoSune VPM repository to VCC:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

Then open an avatar project and add **NekoSune Avatars**.

For development through Unity Package Manager you can use:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

The Git URL is a UPM URL, not a VCC repository URL.

## Avatar tools

Everything is available from **NekoSune → Hub** and the **NekoSune → Avatar** submenu.

### Avatar Doctor

`NekoSune → Avatar → Avatar Doctor`

A preflight pass for answering “what is actually wrong with this avatar?” before upload.

Checks include:

- missing `VRCAvatarDescriptor`, root Animator, rig and viewpoint setup;
- Expression Parameters presence, duplicate names, synced-bit budget and parameter count;
- recursive Expressions Menu validation, missing parameters, empty submenus and overfilled menus;
- Animator/Expression parameter type mismatches;
- mixed Write Defaults;
- Mesh Read/Write failures;
- current PC and Quest/mobile performance rank;
- large texture/VRAM offenders;
- materials which do not use VRChat Mobile avatar shaders;
- PC-only/mobile-stripped components;
- optional PC ↔ Quest parameter-order/type consistency checks.

It reuses the same measurements as Rank Advisor so the two tools do not maintain conflicting performance calculations.

### PC → Quest Assistant

`NekoSune → Avatar → PC to Quest Assistant`

Builds a conversion plan from the current mobile performance blockers and can create a **separate Quest copy** without touching the PC hierarchy.

Safe automatic operations include:

- duplicate the source avatar as `<name> [Quest]`;
- remove components which are not useful/supported on the mobile avatar copy;
- duplicate unsupported materials instead of modifying the PC materials;
- convert duplicated materials to an available `VRChat/Mobile/*` avatar shader;
- preserve common main texture, colour and emission data where the target shader supports it;
- create Android-only texture import overrides at 512, 1024 or 2048;
- compare the PC and Quest Expression Parameters order/type.

It deliberately does **not** blindly delete triangles. Meshes which exceed mobile budgets are reported as topology/retopology work instead of being destructively damaged.

Generated Quest materials are stored under:

```text
Assets/NekoSune/Avatars/QuestGenerated/
```

### PhysBone Doctor

`NekoSune → Avatar → PhysBone Doctor`

Shows which PhysBones are responsible for the most mobile cost:

- component count;
- estimated affected transforms per chain;
- assigned colliders;
- estimated transform × collider collision checks;
- unused PhysBone colliders;
- multiple PhysBones sharing the same root, which are review/merge candidates;
- mobile Good/Poor targets beside the measurements.

The transform/collision-check numbers are estimates because the exact SDK calculation has implementation details which are not completely represented by a simple hierarchy walk.

### VRAM / Texture Inspector

`NekoSune → Avatar → VRAM and Texture Inspector`

Lists every unique texture used by the avatar, sorted by estimated loaded memory.

For each texture it shows:

- resolution;
- estimated loaded memory;
- TextureImporter compression;
- importer maximum size;
- mipmap state;
- unique material usage count;
- estimated memory if the largest dimension were reduced to 2048 or 1024.

It can create **Android platform overrides only**, so reducing a Quest texture does not have to reduce the PC texture import.

Runtime-memory figures are estimates. VRChat's final platform-compressed accounting can differ, so use Rank Advisor and the VRChat SDK Builder for the final performance verdict.

### Face Tracking Doctor

`NekoSune → Avatar → Face Tracking Doctor`

Scans the avatar's actual blendshapes for common ARKit/Unified Expressions coverage across:

- eyes and eyelids;
- brows;
- cheeks and nose;
- jaw/mouth;
- tongue.

It also checks for a core set of **VRCFaceTracking v2** parameters and can add missing entries to the assigned VRChat Expression Parameters asset as **local, unsynced Float parameters**.

The setup button does not fabricate missing blendshapes or silently create expression animation mappings. It prepares the parameter side and shows what facial data the model actually contains.

### Expression + Animator Doctor

`NekoSune → Avatar → Expression and Animator Doctor`

A deeper expressions/FX debugging pass than Avatar Doctor.

It checks:

- synced Expression Parameter budget;
- duplicate Expression/Animator parameter names;
- recursive menu references;
- puppet sub-parameters and Float compatibility;
- the eight-controls-per-menu rule;
- empty submenus;
- Expression ↔ Animator parameter type mismatches;
- transition conditions which reference missing Animator parameters;
- mixed Write Defaults;
- unconditional/suspicious Any State transitions;
- potentially unreachable states;
- Parameter Driver states which write the same parameter multiple times;
- apparently unused parameters, while noting that OSC, contacts and build-time tools can make those false positives.

A plain-text report can be copied for debugging/commission support.

### Mesh Compressor

`NekoSune → Avatar → Mesh Compressor`

Non-destructive mesh optimization and Quest readiness checks.

- scans triangles, vertices, submeshes/material slots, blendshapes and Read/Write state;
- removes degenerate triangles without rewriting the vertex topology;
- merges submeshes which already use the exact same material;
- creates optimized mesh copies instead of overwriting FBX/model sources;
- offers Unity ModelImporter mesh-compression presets;
- protects blendshape-heavy facial meshes from unsafe vertex-order optimization;
- reports meshes which genuinely need decimation/retopology instead of pretending file compression reduces triangles.

Optimized copies are written under:

```text
Assets/NekoSune/Avatars/OptimizedMeshes/
```

### Rank Advisor

`NekoSune → Avatar → Rank Advisor`

Computes the VRChat performance rank for PC and Quest/mobile, showing the statistics which currently determine the worst-stat-wins result and what each blocker must reach for the next rank.

It covers mesh/material, texture, rig, constraints, PhysBones, contacts, particles, renderers, lights/audio/cloth/physics and bounds. Values which cannot be measured exactly are visibly treated as estimates.

The one opt-in automatic fix is Mesh Read/Write on model importers.

### Lip Sync Studio

`NekoSune → Avatar → Lip Sync Studio`

Bakes a viseme `.anim` from an AudioClip using audio analysis, formant/energy matching, music suppression, attack/release smoothing and optional key reduction.

It can bind through the VRChat descriptor, fuzzy blendshape-name matching, a humanoid jaw, or a single mouth-open blendshape.

## Cross-platform conversion

### VRChat → Resonite

`NekoSune → Avatar → Export to Resonite`

NekoSune does **not** invent its own incompatible `.resonitepackage` format. When the experimental **Modular Avatar - Resonite** integration is installed, NekoSune drives that package's actual Resonite build backend and saves the generated `.resonitepackage`.

That means the export uses the same upstream conversion path for the hierarchy/common avatar data, meshes, textures/materials, viewpoint/visemes and supported PhysBone-to-Resonite dynamics.

Important limitation: Modular Avatar's Resonite platform is experimental. Animator/controller, toggle and animation conversion is still limited by what the upstream exporter currently supports. NekoSune exposes this honestly rather than claiming VRChat FX menus are fully translated when the backend cannot yet represent them.

If the upstream private build API changes, NekoSune opens the NDMF Console so its native Resonite build UI can still be used.

### VRChat → ChilloutVR

`NekoSune → Avatar → Convert to ChilloutVR`

This integration is enabled only when the **ChilloutVR CCK** is installed. NekoSune itself keeps no hard CCK assembly reference, so normal VRChat projects can still install the package.

The converter creates a separate `<name> [ChilloutVR]` copy and can:

- add a real CCK `CVRAvatar` component;
- carry the VRChat viewpoint and voice position;
- carry the face/body mesh, viseme names and detected blink shape;
- duplicate the source FX Animator Controller as the CVR Advanced Avatar Settings base controller;
- convert VRChat Expression Parameters to CVR Advanced Avatar Settings;
- use VRChat Expressions Menu labels as the friendly CVR setting labels;
- convert Bool parameters to CVR GameObject Toggles;
- convert Float parameters to CVR Sliders;
- convert Int parameters to CVR Dropdowns when multiple integer transition values can be discovered, with a toggle fallback for simple 0/1 cases;
- ask the installed CCK to generate/update the AAS Animator when its editor API is available;
- optionally strip VRChat-only components from the generated CVR copy.

#### PhysBones on ChilloutVR

Dynamic Bone is optional and is **not bundled** with NekoSune or ChilloutVR CCK.

When Dynamic Bone v1.x is installed, NekoSune can create Dynamic Bone components from detected PhysBone roots and copy a conservative subset of spring/stiffness/immobile/radius/gravity/ignored-transform data.

PhysBone collider geometry is not automatically translated, and complex dynamics should always be reviewed in the CVR copy before upload.

If Dynamic Bone is not installed, NekoSune warns before stripping a copy which still contains PhysBones.

#### CVR conversion limits

Normal parameter-driven FX toggles/sliders/dropdowns are the target of the automatic conversion. VRChat-only `StateMachineBehaviour`s, specialised Parameter Driver logic, unusual gesture systems, contacts and platform-specific shader behaviour may need manual CVR equivalents after conversion.

The CCK is actively developed, so CCK-facing code uses reflection and reports a clear error if an installed future CCK changes one of the expected editor data types.

## Safety / non-destructive policy

The optimizer/converter tools are designed around this flow:

```text
Analyze → Explain → Duplicate / platform override → Apply safe changes → Review
```

They avoid silently rewriting the source avatar wherever a separate copy or platform-specific import override is practical.

Always keep source control/backups and run the target platform's own SDK validation before publishing.

## Optional integrations

| Feature | Optional dependency |
| --- | --- |
| VRChat → Resonite package | Modular Avatar - Resonite / NDMF Resonite platform |
| VRChat → ChilloutVR | ChilloutVR CCK |
| PhysBone → Dynamic Bone bridge | Dynamic Bone v1.x |

These are not hard VPM dependencies of `com.nekosune.avatars`.

## Package layout

```text
Editor/
├── Core/
│   ├── NekoAddon.cs
│   ├── NekoAddonText.cs
│   ├── NekoHubWindow.cs
│   ├── NekoPaths.cs
│   └── NekoStyles.cs
├── Diagnostics/
│   ├── NekoAvatarDiagnosticsUtil.cs
│   ├── NekoAvatarDoctor.cs
│   ├── NekoQuestAssistant.cs
│   ├── NekoPhysBoneDoctor.cs
│   ├── NekoTextureInspector.cs
│   ├── NekoFaceTrackingDoctor.cs
│   └── NekoExpressionAnimatorDoctor.cs
├── Converters/
│   ├── NekoResoniteExporter.cs
│   └── NekoChilloutVRConverter.cs
├── MeshOptimizer/
├── RankAdvisor/
├── LipSync/
└── Localization/
```

New addon card names have built-in English fallbacks. Existing JSON localization still overrides those labels when translations are added.

## Publishing

Package releases use tags such as:

```text
avatars-v0.1.0
avatars-v0.2.0
avatars-v0.3.0
```

Changing `version` in `package.json` triggers `.github/workflows/release-avatar.yml`, which creates a VPM-compatible ZIP with `package.json` at the ZIP root and asks the shared VCC listing to rebuild.

Published versions should remain immutable because existing VCC projects can depend on them.

## License

See `LICENSE` if present; otherwise all rights reserved by NekoSune.
