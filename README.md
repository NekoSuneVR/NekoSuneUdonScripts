# NekoSune Avatars

A toolbox of VRChat **avatar** addons for the Unity Editor, all reachable from a single
**NekoSune** menu in the menu bar (right next to *Tools*).

World and Udon tooling is not part of this package — it lives in its own branch/package and
installs alongside this one under the same **NekoSune** menu.

The first addon is **Lip Sync Studio** — drop in an avatar and an audio clip, press one button,
and get a `.anim` that drives the avatar's mouth in time with the audio. It works with songs
(backing music and all), with plain speech, and with avatars from Booth, Gumroad, VRoid, CATS
exports, ARKit-blendshape avatars, or anything you rigged yourself.

---

## Install

### Via the VRChat Creator Companion (VPM)

1. Copy this folder into your project's `Packages/` directory as
   `Packages/com.nekosune.avatars`, **or** add it as a local package from VCC.
2. VCC / the package manager resolves `com.vrchat.avatars` for you.

### Via the Unity Package Manager (UPM)

*Window → Package Manager → + → Add package from disk…* and pick this folder's `package.json`.

### Drop-in (no package manager)

Copy the whole folder into `Assets/NekoSune/Avatars`. Everything lives under an
Editor-only assembly definition, so it never ships in a build and never touches runtime code.

**The VRChat SDK is optional.** Every SDK type is reached by reflection, so the package
compiles and runs in a project with no VRChat SDK at all. When the SDK *is* present, the
avatar descriptor is read first and its viseme mapping is used verbatim.

Unity 2019.4 or newer.

---

## Using Lip Sync Studio

Open it from **NekoSune → Avatar → Lip Sync Studio**, from the **NekoSune → Hub** window,
or by right-clicking an AudioClip in the Project window and choosing
**NekoSune → Lip Sync from this audio**.

1. **Drop an avatar** into the top slot — a scene object or a prefab.
2. **Drop an audio clip** into the second slot. The waveform strip appears with play/stop.
3. Optionally pick a **preset** (Song / music, Speech, Anime, Subtle, Noisy recording) and
   tweak the sliders.
4. Press **Make lip sync**. The clip is written to the output folder and the green banner
   links straight to it in the Project window.

Then drop that `.anim` into an animator layer, a timeline, or wherever you drive it from.

### What the settings do

Every slider has a tooltip; the short version:

| Setting | Effect |
| --- | --- |
| Volume → mouth | How much loudness drives how wide the mouth opens. At 0, every viseme plays at full size. |
| Clarity | High values commit to one crisp viseme per frame instead of blending several. |
| Consonants close mouth | How hard consonant frames pull the mouth shut. |
| Strength | Overall amplitude of the whole animation. |
| Offset, ms | Shifts the animation in time. Negative = mouth moves early. |
| Attack / Release, ms | How fast the mouth reaches a new shape and relaxes back. |
| Silence threshold | Anything quieter becomes the `sil` viseme. |
| Liveliness | A small, deterministic wobble so held vowels don't look frozen. |
| Frames per second | Keyframe rate of the baked clip. 30 is plenty. |
| Quality | Analysis resolution. Higher separates consonants better and bakes slower. |
| Clean vocal, no music | Turn on for an isolated voice track. **Leave it off for songs** — backing music is then suppressed before analysis. |
| Reduce keyframes / tolerance | Drops keys a straight line already covers. Typical saving is 60–90% with no visible difference. |
| Write the sil viseme | Turn off if the silence shape fights another animation layer. |
| Normalize weights | Keeps the sum of all viseme weights at 100 or below. |
| Start / End, s | Bake only a section of the clip. |

### Targets

The **Targets and output** section decides what actually gets animated:

- **Automatic** — visemes if the avatar has them, otherwise the jaw bone, otherwise a single
  mouth-open blendshape.
- **VRC viseme blendshapes** — the 15 standard visemes (`sil, PP, FF, TH, DD, kk, CH, SS, nn,
  RR, aa, E, ih, oh, ou`).
- **Jaw bone** — rotates a bone on a chosen axis up to a chosen maximum angle.
- **Single mouth-open shape** — for avatars with just one "mouth open" blendshape.

The jaw bone and the single shape can also be driven *alongside* visemes with the
**Also drive…** toggles.

### How it finds the mouth on any avatar

The **Avatar binding** section shows exactly what was found, in this order:

1. **VRChat avatar descriptor** — read by reflection, including its viseme blendshape list,
   its lip sync mode, and its jaw bone.
2. **Blendshape name matching** — a fuzzy matcher that understands Booth, Gumroad, VRoid,
   CATS and ARKit naming, common prefixes (`vrc.v_aa`, `Fcl_MTH_A`, `viseme_aa`, …), and
   Japanese kana shapes (`あ い う え お`).
3. **Humanoid jaw bone**, then a name search for a jaw-ish bone.
4. **A single mouth-open blendshape** from a list of common names.

Anything it gets wrong you can override by hand in that same section — each viseme has its own
picker, and the green/red chips tell you at a glance what is mapped.

### Why one preset works on every voice

The analyzer measures the clip's own median first formant and rescales its vowel prototypes to
that voice's vocal tract. A deep male voice, a high anime voice and a pitched-up song all land
on the same vowel decisions without you touching a slider.

---

## Adding a language

Languages live one file per language in `Editor/Localization/Languages/`. To add one, copy
`en.json`, rename it to the language code, translate the `v` values, and press
**Reload languages** in the Hub — no recompile, no code change.

```json
{
  "code": "nl",
  "name": "Dutch",
  "nativeName": "Nederlands",
  "entries": [
    { "k": "common.language", "v": "Taal" }
  ]
}
```

You only need the keys you actually translated. Anything missing falls back to English, and
anything missing from English falls back to the raw key, so a partial translation can never
break the UI.

Shipping now: English, Русский, Español, Polski, Deutsch, Français, Italiano,
Português (Brasil), Українська, 日本語, 한국어, 简体中文 — 120 keys each.

The language is picked from Unity's system language on first run and remembered in
`EditorPrefs` after that.

---

## Adding an addon

Every window in the Hub is discovered by reflection. Implement `INekoAddon`, tag it, done —
it shows up in the Hub grid under its category with no registry to edit:

```csharp
[NekoAddon(Order = 20)]
internal class MyToolAddon : INekoAddon
{
    public string Id           { get { return "mytool"; } }
    public string TitleKey     { get { return "mytool.title"; } }
    public string DescriptionKey { get { return "mytool.desc"; } }
    public string CategoryKey  { get { return "cat.avatar"; } }
    public string Glyph        { get { return "✦"; } }
    public bool   IsAvailable  { get { return true; } }
    public void   Open()       { MyToolWindow.Open(); }
}
```

Add `mytool.title` and `mytool.desc` to `en.json` and the card is fully localized.

---

## Layout

```
package.json                     VPM / UPM manifest
Editor/
  NekoSune.Avatars.Editor.asmdef    Editor-only assembly, no external references
  Core/
    NekoPaths.cs                 Finds the package root under Packages/ or Assets/
    NekoAddon.cs                 [NekoAddon] attribute + reflection registry
    NekoHubWindow.cs             The NekoSune menu-bar hub
    NekoStyles.cs                Shared look and feel, runtime-generated textures
  Localization/
    NekoLoc.cs                   Loader, fallback chain, language switching
    Languages/*.json             One file per language
  LipSync/
    NekoFFT.cs                   Allocation-free radix-2 FFT
    NekoAudioReader.cs           Reads samples from compressed clips safely
    NekoLipSyncAnalyzer.cs       Formants, consonants, music suppression, envelopes
    NekoVisemes.cs               The 15 VRC visemes and their openness values
    NekoAvatarBinder.cs          Descriptor → name matching → jaw → single shape
    NekoAnimClipBuilder.cs       Curve building, key reduction, saving the .anim
    NekoAudioPreview.cs          Editor audio preview
    NekoLipSyncSettings.cs       Settings, presets, preset assets
    NekoLipSyncWindow.cs         The Lip Sync Studio UI
```

---

## Notes and limits

- Reading a compressed clip temporarily flips its importer to `DecompressOnLoad` and reimports
  it. The original import settings are always restored, including if the bake throws.
- Baking is synchronous with a cancellable progress bar. A three-minute song at quality 6 takes
  a few seconds.
- The analyzer is a signal-processing estimator, not a phoneme recognizer. It reads clean vocals
  extremely well, dense mixes reasonably well, and screamed or heavily distorted vocals poorly.
  For a dense mix, leave **Clean vocal** off and raise **Clarity**.
- Key reduction is lossy within the tolerance you set. Set the tolerance to 0 to keep every key.

## License

See `LICENSE` if present; otherwise all rights reserved by NekoSune.
