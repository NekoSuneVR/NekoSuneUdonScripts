# NekoSune Animation Tools

Beat-aware animation authoring for NekoSune Avatar + World Hubs.

## Included

- Lip Sync Studio (moved from Avatar Tools)
- editor AudioClip preview + scrub/seek support
- waveform timeline
- Hardstyle beat/kick/drop preset
- Uptempo beat/kick/drop preset
- Frenchcore beat/kick/drop preset
- custom BPM/sensitivity mode
- auto keyframing with attack/hit/decay curves
- manual mode that creates an empty `.anim` plus a timestamp guide
- animatable-property discovery for hierarchy objects
- shader/material property keyframing when Unity exposes the property
- ParticleSystem/component property keyframing
- humanoid-bone Transform keyframing by selecting the bone in the avatar hierarchy
- timed lyric parser for `[mm:ss.xxx]Text` and `seconds|Text`
- World 3D TextMesh lyric track generator
- Avatar/generic mesh-object lyric toggling
- shader-atlas lyric-index curve generation

## Music presets

The beat mapper uses offline audio energy + low-frequency onset analysis. Presets tune the expected spacing for Hardstyle, Uptempo and Frenchcore. Detection is an editor assistant, not a promise of perfect musical transcription; the waveform and manual workflow are kept for creators who prefer hand keyframing.

## Auto vs manual

**Auto** selects a real Unity animatable binding and generates one `.anim` with beat/kick/drop pulses.

**Manual** creates an empty animation clip and a text timing guide containing every detected marker. Use the waveform scrubber to audition the exact point and key the effect by hand in Unity's Animation window.

## Shader / effect assets

NekoSune does **not** bundle or redistribute third-party shaders, paid assets, particle packs, animations, or creator files.

Examples the property picker can work with after the creator installs them legally:

- Doppelgänger / Dope Shader — obtain through the creator's official Patreon/Discord and follow the current tier/licence terms.
- Leviant ScreenSpace Ubershader — use the creator's official repository/distribution and licence.
- Poiyomi and other avatar/world shaders — obtain from the shader creator's official source.
- Any other material/component whose property Unity exposes through `AnimationUtility`.

The point of Animation Tools is to keyframe **installed** properties; it does not provide those third-party assets.

## Lyrics on avatars vs worlds

Worlds can use the generated 3D TextMesh lyric objects. Avatar mode intentionally does not depend on runtime Canvas/Text components. For avatars, provide child mesh objects (one line/state each) or a shader/text-atlas material and generate an exact-time animation track.

## Package

```text
com.nekosune.animation-tools
```
