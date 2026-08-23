# NekoSune Avatars

A toolbox of VRChat **avatar** addons for the Unity Editor, all reachable from a single
**NekoSune** menu in the menu bar (right next to *Tools*).

World and Udon tooling is not part of this package — it lives in its own branch/package and
installs alongside this one under the same **NekoSune** menu.

**Lip Sync Studio** — drop in an avatar and an audio clip, press one button, and get a `.anim`
that drives the avatar's mouth in time with the audio. It works with songs (backing music and
all), with plain speech, and with avatars from Booth, Gumroad, VRoid, CATS exports,
ARKit-blendshape avatars, or anything you rigged yourself.

**Rank Advisor** — drop in an avatar and see its VRChat performance rank for PC *and* Quest,
every statistic behind it, and the exact list of things that have to come down before the rank
moves. Read-only; the one change it will make for you is opt-in.

---

## Install

### Via VRChat Creator Companion (VCC / VPM)

VCC does **not** install this package by using this Git URL as a repository:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

It also does not use this branch's `package.json` as a repository URL. VCC expects a VPM
**package listing** (`index.json`) which points at compatible release ZIP files.

Add this repository URL to VCC:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

Then:

1. Open **VCC → Settings → Packages**.
2. Add the repository URL above under the user/community repositories section.
3. Open your VRChat avatar project.
4. Add **NekoSune Avatars** (`com.nekosune.avatars`).

The release ZIP used by VCC is generated from this branch with `package.json` at the ZIP root,
which is required by the VPM listing builder.

### Via Unity Package Manager (Git URL)

The Git URL **is valid for Unity Package Manager**; it just is not a VCC repository URL.

In Unity open **Window → Package Manager → + → Add package from git URL…** and enter:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

You can also use **Add package from disk…** and select this branch's `package.json` after
downloading/cloning it locally.

### Drop-in (no package manager)

Copy the whole folder into `Assets/NekoSune/Avatars`. Everything lives under an
Editor-only assembly definition, so it never ships in a build and never touches runtime code.

**The VRChat SDK is optional.** Every SDK type is reached by reflection, so the package
compiles and runs in a project with no VRChat SDK at all. When the SDK *is* present, the
avatar descriptor is read first and its viseme mapping is used verbatim.

Unity 2019.4 or newer.

---

## Publishing a VCC release

The `avatar` branch contains `.github/workflows/release-avatar.yml`.

To publish a new version:

1. Make your changes on `avatar`.
2. Change the semantic `version` in `package.json`.
3. Push the `package.json` change, or manually run **Build Avatar Release** from GitHub Actions.
4. The workflow creates a release ZIP shaped like this:

```text
com.nekosune.avatars-0.1.0.zip
├── package.json
├── CHANGELOG.md
├── README.md
└── Editor/
    └── ...
```

5. The VCC listing on `main` is rebuilt so Creator Companion can see the new version.

Do not delete old VPM releases after publishing them because existing projects can depend on
those exact versions.

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

## Using Rank Advisor

Open it from **NekoSune → Avatar → Rank Advisor**, from the **NekoSune → Hub** window, or by
right-clicking an avatar in the Hierarchy and choosing **NekoSune → Rank Advisor**.

Drop an avatar in and you get, for the platform tab you are on:

- **The overall rank.** VRChat takes the *worst single statistic* and makes that the rank of the
  whole avatar, so the badge shows what that worst statistic dragged you down to. The other
  platform's rank is shown underneath, because an avatar that is Good on PC is frequently Very
  Poor on Quest.
- **Biggest wins.** Every statistic currently sitting at or below the overall rank, sorted by how
  far over the line it is, each with its exact target: *Triangles: 94,312 → 70,000 or less*.
  Because the rank is worst-wins, **all** of them have to come down before the rank moves — the
  list is the complete job, not a menu.
- **The full table**, grouped into mesh/material, rig, PhysBones and contacts, particles and
  dynamics, and everything else. Each row has the value, a bar showing where it sits between
  Excellent and Poor, its own rank chip, and a **Select** button that pings the offending object
  in the Hierarchy.
- **Copy report** puts both platforms' numbers on the clipboard as plain text — useful for a
  commission thread or a bug report.

### The silent killers it catches

- **Mesh Read/Write off** is an automatic Very Poor *and* a hard upload block, and the SDK's own
  message about it is easy to miss. This is the one thing the window will fix for you: **Turn
  Read/Write on** flips `isReadable` on the affected *model importers* and reimports them. It
  edits import settings only — never the mesh, never the scene. Meshes that are not from a model
  file are counted and reported rather than touched.
- **Disabled objects and components still count.** Everything in the table includes them, exactly
  as VRChat does. Hiding a particle system in the Hierarchy does not hide it from the ranking.
- **No avatar descriptor** — the numbers are still correct, but the object cannot be uploaded.
- **The Quest tab strips six statistics** (lights, cloth, cloth vertices, physics colliders,
  physics rigidbodies, audio sources) because mobile removes those components outright. They are
  shown greyed as *not counted here* rather than hidden, so you can see why the two ranks differ.

### What it estimates, and what it does not measure

Honesty is built into the display rather than left to the README:

- Values marked **~** are estimated, not the SDK's own number: texture memory, PhysBone
  transforms, PhysBone collision checks, constraint depth, and bounds size. They are close enough
  to plan against and are called out in the window whenever any are in play.
- **Raycasts are not measured at all.** The stat is shown as *not measured* and is deliberately
  not allowed to contribute a rank, so the window can never invent a rank the avatar has not
  earned — instead it warns that the real rank could be one step worse than shown.

Treat the result as a very good guide, not as a substitute for the SDK build panel.

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
Português (Brasil), Українська, 日本語, 한국어, 简体中文 — 190 keys each.

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

```text
package.json                         VPM / UPM package manifest
CHANGELOG.md
README.md
.github/
  workflows/
    release-avatar.yml               Builds the VPM release ZIP
Editor/
  NekoSune.Avatars.Editor.asmdef     Editor-only assembly, no external references
  Core/
    NekoPaths.cs                     Finds the package root under Packages/ or Assets/
    NekoAddon.cs                     [NekoAddon] attribute + reflection registry
    NekoHubWindow.cs                 The NekoSune menu-bar hub
    NekoStyles.cs                    Shared look and feel, runtime-generated textures
  Localization/
    NekoLoc.cs                       Loader, fallback chain, language switching
    Languages/*.json                 One file per language
  LipSync/
    NekoFFT.cs                       Allocation-free radix-2 FFT
    NekoAudioReader.cs               Reads samples from compressed clips safely
    NekoLipSyncAnalyzer.cs           Formants, consonants, music suppression, envelopes
    NekoVisemes.cs                   The 15 VRC visemes and their openness values
    NekoAvatarBinder.cs              Descriptor → name matching → jaw → single shape
    NekoAnimClipBuilder.cs           Curve building, key reduction, saving the .anim
    NekoAudioPreview.cs              Editor audio preview
    NekoLipSyncSettings.cs           Settings, presets, preset assets
    NekoLipSyncWindow.cs             The Lip Sync Studio UI
  RankAdvisor/
    NekoPerfTable.cs                 The official PC / Quest limits and ranking rules
    NekoAvatarStats.cs               Walks the avatar and counts every statistic
    NekoRankAdvisor.cs               Worst-wins verdict, blocker list, Read/Write fix
    NekoRankWindow.cs                The Rank Advisor UI
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
- Rank Advisor reads the scene; it never edits the avatar. The single exception is the opt-in
  Read/Write fix, which changes model *import settings* and triggers a reimport.
- The limits in `NekoPerfTable.cs` are transcribed from VRChat's published avatar performance
  ranking tables. If VRChat changes them, that one file is the only thing that needs updating.

## License

See `LICENSE` if present; otherwise all rights reserved by NekoSune.
