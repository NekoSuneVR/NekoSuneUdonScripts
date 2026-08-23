# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[0.2.0]: #
[0.1.0]: #
