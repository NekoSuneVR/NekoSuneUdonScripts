# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] — 2026-08-23

Major avatar diagnostics, Quest preparation, face-tracking, and cross-platform conversion update.

### Added

- **Avatar Doctor / Preflight Doctor** — `NekoSune → Avatar → Avatar Doctor`.
  - Missing descriptor, Animator/rig/viewpoint and upload-readiness checks.
  - Expression Parameters duplicate-name, synced-bit budget, total-count and menu-reference checks.
  - Recursive submenu validation and the eight-controls-per-menu rule.
  - Expression ↔ Animator parameter type mismatches and mixed Write Defaults warnings.
  - Reuses Rank Advisor for PC/Quest performance, Mesh Read/Write and blocker information.
  - Large texture/VRAM and Quest mobile-shader checks.
  - Optional PC ↔ Quest parameter-order/type comparison.
  - Copyable plain-text preflight report.

- **PC → Quest Avatar Assistant** — `NekoSune → Avatar → PC to Quest Assistant`.
  - Generates a mobile conversion plan from the existing Rank Advisor mobile assessment.
  - Creates a separate `<avatar> [Quest]` hierarchy; the PC source hierarchy is not edited.
  - Duplicates unsupported materials and converts the duplicates to an available VRChat Mobile avatar shader.
  - Preserves common main texture, colour and emission data when the target shader supports them.
  - Applies Android-only texture maximum-size overrides at 512 / 1024 / 2048.
  - Optional removal of mobile-disabled/irrelevant components from the generated copy.
  - PC ↔ Quest Expression Parameter order/type check.
  - Deliberately reports topology/retopology blockers instead of using destructive blind decimation.

- **PhysBone Doctor** — `NekoSune → Avatar → PhysBone Doctor`.
  - Per-chain affected-transform and collider counts.
  - Estimated collision-check cost per chain.
  - Mobile Good/Poor budget guidance.
  - Detects unused PhysBone colliders.
  - Detects several PhysBones using the same root as review/merge candidates.

- **VRAM / Texture Inspector** — `NekoSune → Avatar → VRAM and Texture Inspector`.
  - Sorts unique avatar textures by estimated loaded memory.
  - Shows dimensions, importer compression, maximum size, mipmaps and unique material use count.
  - Estimates the memory impact of 2048/1024 limits.
  - Per-texture and bulk Android-only maximum-size overrides.

- **Face Tracking Doctor** — `NekoSune → Avatar → Face Tracking Doctor`.
  - Scans blendshapes for ARKit / Unified Expressions-style eye, brow, cheek/nose, mouth/jaw and tongue coverage.
  - Reports an ARKit-style coverage count.
  - Detects core VRCFaceTracking v2 parameters.
  - Can add missing core VRCFT v2 entries to the assigned Expression Parameters asset as local, unsynced Float parameters.
  - Does not fabricate missing blendshapes or silently generate face-animation mappings.

- **Expression + Animator Doctor** — `NekoSune → Avatar → Expression and Animator Doctor`.
  - Expression Parameter budget and duplicate-name checks.
  - Recursive menu, submenu and puppet parameter validation.
  - Animator/Expression type mismatch checks.
  - Missing transition-parameter checks.
  - Mixed Write Defaults detection.
  - Unconditional Any State and potential unreachable-state review hints.
  - Detects Parameter Driver states that write the same parameter multiple times.
  - Reports apparently unused parameters while accounting for OSC/contact/build-time false positives.

- **VRChat → Resonite exporter bridge** — `NekoSune → Avatar → Export to Resonite`.
  - Detects the experimental Modular Avatar - Resonite / NDMF Resonite backend at runtime.
  - Drives that backend's real avatar build path and saves its generated `.resonitepackage` rather than creating a competing file format.
  - Uses the upstream converter for the avatar hierarchy/common data, supported mesh/material/texture conversion, viewpoint/visemes and supported PhysBone-to-Resonite dynamics.
  - Falls back to opening the NDMF Console if the experimental upstream private build API changes.
  - Clearly documents that Animator/toggle/animation conversion is limited by the current upstream Resonite exporter.

- **VRChat → ChilloutVR converter** — `NekoSune → Avatar → Convert to ChilloutVR`.
  - Runtime-gated on the ChilloutVR CCK; NekoSune keeps no hard CCK assembly reference.
  - Creates a separate `<avatar> [ChilloutVR]` copy with a real CCK `CVRAvatar` component.
  - Copies viewpoint/voice position, face/body mesh, viseme names and detected blink shape.
  - Duplicates the VRChat FX Animator Controller and uses it as the CVR Advanced Avatar Settings base controller.
  - Converts VRChat Expression Parameters to CCK Advanced Avatar Settings.
  - Uses VRChat Expressions Menu names as CVR friendly setting labels.
  - Bool → GameObject Toggle, Float → Slider, and Int → Dropdown when multiple transition values can be discovered (with simple toggle fallback).
  - Can invoke the installed CCK's AAS Animator generation where the editor API is available.
  - Optional removal of VRChat-only components from the generated CVR copy.
  - Optional PhysBone → Dynamic Bone root/settings bridge when Dynamic Bone v1.x is installed.
  - Warns before stripping PhysBones when no replacement Dynamic Bone authoring component is installed.

- **Shared diagnostic utilities** for SDK-reflection access, expression parameter accounting, Animator discovery, texture/material collection, Android texture overrides and PhysBone hierarchy estimates.
- **Addon fallback labels** so the new cards display useful English text even before all twelve localization JSON files receive translations.

### Conversion limitations

- Modular Avatar's Resonite platform is experimental; its current upstream conversion does not fully translate VRChat Animator/menu toggle behaviour. NekoSune does not claim otherwise.
- ChilloutVR conversion targets normal parameter-driven FX/AAS workflows. VRChat-only StateMachineBehaviours, specialised Parameter Drivers, unusual gesture/contact systems and platform-specific shader behaviour can require manual CVR equivalents.
- PhysBone → Dynamic Bone conversion copies a conservative subset of root/settings data; PhysBone collider geometry is not automatically recreated.
- All converter/optimizer outputs should be reviewed in the target SDK before publishing.

### Changed

- The NekoSune Hub now supports built-in English fallback labels for newly introduced addons while keeping existing JSON localization overrides.
- README reorganized around diagnostics, Quest workflow, cross-platform conversion and non-destructive behaviour.

## [0.2.0] — 2026-08-23

Avatar optimization update focused on safe mesh compression and Quest/mobile readiness.

### Added

- **Mesh Compressor** — `NekoSune → Avatar → Mesh Compressor`.
  - Scans every skinned/basic mesh under an avatar and reports triangles, vertices, material slots, blendshapes, Read/Write state, mergeable material slots, and degenerate triangles.
  - Quest preset highlights the current mobile triangle, skinned-mesh, and material-slot targets without pretending that file compression reduces topology.
  - **Create safe optimized copies** writes new mesh assets instead of overwriting FBX/model sources.
  - Removes repeated-index / zero-area triangles while preserving the original vertex array.
  - Merges submeshes that already use the exact same material, reducing redundant material slots and draw work without touching UVs, bone weights, normals, tangents, or blendshapes.
  - Protects large blendshape/facial meshes from blind destructive decimation and marks them as retopology candidates instead.
  - Copyable mesh report for comparing optimization work.
- **Mesh import compression presets** — Lossless, Balanced, Smaller, and Quest.
  - Uses Unity `ModelImporter.meshCompression` (Off / Low / Medium / High).
  - Optional polygon-order cache optimization.
  - Vertex-order optimization is deliberately left unchanged on model files containing blendshapes.
  - Reimports model assets only after an explicit confirmation dialog.
- Detailed Mesh Compressor documentation under `Editor/MeshOptimizer/README.md`.

### Changed

- Avatar release workflow now uses `actions/checkout@v6` so it runs on the current Node 24 action runtime.
- English Hub localization now includes the Mesh Compressor card. Other languages safely fall back to English until translated.

### Safety

- Mesh compression is presented accurately as stored mesh-data compression; it does not lower triangle count.
- The optimizer does not provide a fake percentage decimator that randomly drops triangles from faces, clothing, or blendshape meshes.
- Safe cleanup is non-destructive and keeps original model assets intact.

## [0.1.0] — 2026-08-23

First release.

### Added

- **NekoSune menu-bar hub** — a root `NekoSune` menu next to *Tools*, with a Hub window that
  lists every addon as a card grouped by category. Addons register themselves through the
  `[NekoAddon]` attribute and are discovered by reflection, so a new tool needs no registry edit.
  Shared with any other NekoSune package installed side by side — this package contributes the
  Avatar category only; world / Udon tooling ships separately.
- **Lip Sync Studio** — bakes a viseme `.anim` from any AudioClip.
  - Avatar and audio drop slots, waveform strip with editor playback and trim shading.
  - Presets: Default, Song / music, Speech, Anime (exaggerated), Subtle, Noisy recording,
    plus user presets saved as `ScriptableObject` assets.
  - Settings: volume-to-mouth, clarity, consonant close, strength, offset, attack, release,
    silence threshold, liveliness, fps, quality, clean-vocal mode, loop, keyframe reduction
    with tolerance, sil-viseme write, weight normalization, and start/end trimming.
  - Targets: VRC viseme blendshapes, jaw bone rotation (axis, max angle, invert), a single
    mouth-open blendshape, or Automatic — with the jaw and single shape drivable alongside
    visemes.
  - Avatar binding panel showing what was detected, its source, and a manual override picker
    for every viseme.
- **Audio analysis** — radix-2 FFT with Hann windowing, spectral flatness/centroid/flux,
  band energies, and F1/F2 formant peak-picking with parabolic interpolation.
  - Per-bin minimum-statistics noise floor suppresses backing music, so songs work without a
    separate vocal stem.
  - Adaptive vocal-tract scaling from the clip's own median F1, so one preset fits any voice.
  - Asymmetric attack/release envelopes and deterministic liveliness jitter.
- **Avatar compatibility** — VRChat avatar descriptor read entirely by reflection (no hard SDK
  dependency), then fuzzy blendshape-name matching covering Booth, Gumroad, VRoid, CATS and
  ARKit conventions plus Japanese kana shapes, then the humanoid jaw bone, then a single
  mouth-open shape.
- **Clip output** — exact piecewise-linear tangents so fast consonants do not ease, greedy
  linear key reduction with a reported saving percentage, unique asset paths, and a
  *Show in Project* link in the result banner.
- **Rank Advisor** — the full VRChat avatar performance ranking, computed for PC and Quest side
  by side, with the concrete work needed to move the rank.
  - All 29 ranked statistics: triangles, texture memory, skinned and basic meshes, material
    slots, bounds size, bones, animators, constraints and constraint depth, PhysBone components /
    transforms / colliders / collision checks, contacts, particle systems / active particles /
    mesh particle polygons / trails / collision, trail and line renderers, lights, audio sources,
    cloths and cloth vertices, physics colliders and rigidbodies, and raycasts.
  - Worst-stat-wins verdict matching VRChat's own rule, with the six statistics that mobile
    strips shown greyed rather than hidden, so the PC/Quest difference is visible.
  - **Biggest wins** list: every blocking statistic sorted by how far over the line it is, each
    with the exact value it has to reach for the next rank, and a Select button that pings the
    offending object.
  - Catches the silent killers: Mesh Read/Write off (an automatic Very Poor *and* an upload
    block), disabled objects and components that still count, and a missing avatar descriptor.
  - One opt-in fix — **Turn Read/Write on** — which flips `isReadable` on the affected model
    importers and reimports them. Import settings only; it never touches the mesh or the scene,
    and meshes that are not from a model file are reported rather than modified.
  - Estimated values are marked `~` and raycasts are reported as *not measured* and barred from
    contributing a rank, so the window can never claim a better rank than the avatar has earned.
  - **Copy report** puts both platforms' full numbers on the clipboard as plain text.
  - VRChat SDK components (PhysBones, colliders, contacts, the avatar descriptor) are found by
    type name, so the whole addon works with no SDK assembly reference.
- **Localization** — a `NekoLoc` layer with one JSON file per language in
  `Editor/Localization/Languages/`, hot-reloadable from the Hub. Missing keys fall back to
  English and then to the raw key, so partial translations never break the UI.
  Shipping with 12 languages at 190 keys each: English, Русский, Español, Polski, Deutsch,
  Français, Italiano, Português (Brasil), Українська, 日本語, 한국어, 简体中文.
- **Context menus** — `Assets → NekoSune → Lip Sync from this audio` on AudioClip assets, and
  `GameObject → NekoSune → Lip Sync Studio` and `GameObject → NekoSune → Rank Advisor` in the
  hierarchy.

[0.3.0]: #
[0.2.0]: #
[0.1.0]: #
