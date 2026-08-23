# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] — 2026-08-23

First release.

### Added

- **NekoSune menu-bar hub** — a root `NekoSune` menu next to *Tools*, with a Hub window that
  lists every addon as a card grouped by category. Addons register themselves through the
  `[NekoAddon]` attribute and are discovered by reflection, so a new tool needs no registry edit.
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
- **Localization** — a `NekoLoc` layer with one JSON file per language in
  `Editor/Localization/Languages/`, hot-reloadable from the Hub. Missing keys fall back to
  English and then to the raw key, so partial translations never break the UI.
  Shipping with 12 languages at 120 keys each: English, Русский, Español, Polski, Deutsch,
  Français, Italiano, Português (Brasil), Українська, 日本語, 한국어, 简体中文.
- **Context menus** — `Assets → NekoSune → Lip Sync from this audio` on AudioClip assets and
  `GameObject → NekoSune → Lip Sync Studio` in the hierarchy.

[0.1.0]: #
