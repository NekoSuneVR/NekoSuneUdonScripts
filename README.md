# NekoSune VRChat Tools

Unity Editor tooling for VRChat creators, split into two installable packages that share one
**NekoSune** menu in the Unity menu bar (right next to *Tools*).

**This `main` branch holds no code.** It is the landing page. All the actual tooling lives on the
two branches below, and which one you install depends on what you are making. You can install
both into the same project — they are designed to sit side by side and merge into a single menu.

---

## The two branches

| Branch | Package id | For | Status |
| --- | --- | --- | --- |
| [`avatar`](../../tree/avatar) | `com.nekosune.avatars` | VRChat **avatars** | Working — 2 tools |
| [`world`](../../tree/world) | *(to be assigned)* | VRChat **worlds** and Udon | Placeholder, not started |
| `main` | — | This README | Documentation only |

### `avatar` — NekoSune Avatars

The branch that is actually finished and usable today. Two tools:

**Lip Sync Studio.** Drop in an avatar and an audio clip, press one button, get a `.anim` that
drives the avatar's mouth in time with the audio. It handles full songs with backing music (no
separate vocal stem needed), plain speech, and avatars from Booth, Gumroad, VRoid, CATS exports,
ARKit-blendshape avatars, or anything you rigged yourself. It can drive the 15 VRC visemes, a jaw
bone, a single mouth-open blendshape, or work it out automatically.

**Rank Advisor.** Drop in an avatar and see its VRChat performance rank for PC *and* Quest side by
side, all 29 statistics behind it, and the exact list of what has to come down before the rank
moves. It catches the things that quietly sink an upload: Mesh Read/Write being off (an automatic
Very Poor *and* a hard upload block), disabled objects that still count towards every statistic,
and a missing avatar descriptor. Read-only — the single change it will make is opt-in.

Both are localized into 12 languages (English, Русский, Español, Polski, Deutsch, Français,
Italiano, Português (Brasil), Українська, 日本語, 한국어, 简体中文).

Full documentation is in the README **on that branch**.

### `world` — worlds and Udon

Reserved for world and Udon tooling. **Nothing is built yet** — the branch currently just holds a
copy of the avatar package as a starting point. Don't install it expecting world features. This
row exists so the split is visible; watch the repo if you want to know when it becomes real.

### Why two branches instead of one package

Avatar creators and world creators need completely different dependencies. The avatar package
depends on `com.vrchat.avatars`; world tooling will depend on `com.vrchat.worlds` and UdonSharp.
Shipping one package would force every avatar creator to pull in the worlds SDK and vice versa.
Splitting them keeps each install to only what you actually need, while the shared **NekoSune**
menu means a project with both installed still gets one tidy menu instead of two.

---

## Requirements

- **Unity 2019.4 or newer.**
- **Git** installed and on your `PATH` — only if you use the Package Manager git-URL method below.
- **The VRChat SDK is optional for the avatar package.** Every SDK type is reached by reflection,
  so it compiles and runs in a project with no VRChat SDK installed at all. When the SDK *is*
  present, the avatar descriptor is read first and used directly.

Everything ships inside an Editor-only assembly definition, so none of it ends up in a build and
none of it touches your runtime code.

---

## Install

Four ways, easiest first. **You only ever install a branch, never `main`** — `main` has no
`package.json`, so every method below will fail against it.

### 1. Download a ZIP (no Git needed)

1. Go to the branch you want: [`avatar`](../../tree/avatar) or [`world`](../../tree/world).
2. Green **Code** button → **Download ZIP**.
3. Unzip it. You get a folder like `NekoSuneUdonScripts-avatar`.
4. Copy that folder into your Unity project. Either location works:
   - `Packages/com.nekosune.avatars` — treated as a real package, stays out of your Assets.
   - `Assets/NekoSune/Avatars` — plain drop-in, shows up in the Project window.
5. Switch back to Unity and let it compile. The **NekoSune** menu appears in the menu bar.

Renaming the folder is fine — nothing depends on the folder name.

### 2. Unity Package Manager, from a Git URL

This is the cleanest option if you have Git installed, because updating is one click later.

*Window → Package Manager → **+** → **Add package from git URL…*** and paste:

```
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

The `#avatar` on the end is what picks the branch — without it, UPM lands on `main`, finds no
`package.json`, and errors out. For the world branch use `#world` once it has real content.

To pin to a specific commit instead of following the branch, put the commit SHA after the `#`:

```
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#f490b0e
```

Unity records the URL in your project's `Packages/manifest.json`, so anyone else opening the
project gets it automatically.

### 3. Clone with Git

Best if you want to pull updates from the command line, or contribute back.

```bash
cd YourUnityProject/Packages
git clone -b avatar https://github.com/NekoSuneVR/NekoSuneUdonScripts.git com.nekosune.avatars
```

To update later:

```bash
cd YourUnityProject/Packages/com.nekosune.avatars
git pull
```

### 4. VRChat Creator Companion (VCC)

**A note on how VCC actually works**, because this trips people up: VCC does not install packages
from a plain GitHub URL. It installs from a *VPM listing* — a JSON index served over HTTP that
lists package versions and where to download them. **This repository does not publish a VPM
listing yet**, so there is no listing URL to paste into *Settings → Packages → Add Repository*.

Until one exists, use VCC like this:

1. Get the package onto disk with method **1** or **3** above, placing it in your project's
   `Packages/` folder.
2. Open the project through VCC as normal.

VCC and the Unity Package Manager both read the `Packages/` folder directly, so the package loads
and resolves its dependencies (`com.vrchat.avatars`) exactly as if VCC had installed it. The only
thing you give up is VCC's update button — you update with `git pull` or by re-downloading.

If a VPM listing is published later, this section will be replaced with the one-line
*Add Repository* URL.

---

## After installing

Everything is reachable from the **NekoSune** menu in the menu bar:

- **NekoSune → Hub** — one window listing every installed tool as a card, grouped by category.
  If you install both the avatar and world packages, they both feed cards into this same Hub.
- **NekoSune → Avatar → Lip Sync Studio**
- **NekoSune → Avatar → Rank Advisor**

There are also right-click shortcuts: right-click an AudioClip in the Project window for
**NekoSune → Lip Sync from this audio**, or an avatar in the Hierarchy for
**NekoSune → Rank Advisor**.

The UI language is picked from your Unity system language on first run and remembered after that;
you can change it from the dropdown in any window.

---

## Uninstalling

- **Installed by ZIP or clone** — delete the folder from `Packages/` or `Assets/`.
- **Installed by git URL** — *Window → Package Manager*, select **NekoSune Avatars**, **Remove**.

Nothing is written outside the package folder except a handful of `EditorPrefs` keys prefixed
`NekoSune.` (your chosen language, last used settings). Those are harmless to leave behind.

---

## Troubleshooting

**"Cannot perform upm operation: Unable to add package"** — you almost certainly pointed UPM at
`main`, or forgot the `#avatar` suffix. `main` deliberately contains no `package.json`.

**"No 'git' executable was found"** — the git-URL method needs Git installed and on your `PATH`.
Install Git, restart Unity, try again. Or just use the ZIP method, which needs nothing.

**The NekoSune menu does not appear** — check the Console for compile errors. A compile error
anywhere in the project stops *all* editor menus from registering, including ones from unrelated
packages. Fix the error and the menu comes back.

**Rank Advisor numbers differ slightly from the SDK build panel** — expected for a few statistics.
Texture memory, PhysBone transforms, PhysBone collision checks, constraint depth and bounds size
are estimates and are marked with a `~` in the window. Raycasts are not measured at all and are
shown as *not measured*; they are deliberately barred from affecting the rank so the tool can
never report a better rank than your avatar has actually earned. Treat it as a very good guide,
not as a replacement for the SDK panel.

---

## Contributing

Pull requests go to the branch the code lives on — `avatar` for avatar tooling, `world` for world
tooling. Please do not open PRs against `main`; it is documentation only.

**Translations are the easiest way to help.** Languages are one JSON file per language in
`Editor/Localization/Languages/`. Copy `en.json`, rename it to the language code, translate the
`v` values, and press **Reload languages** in the Hub — no recompile, no code change. Partial
translations are fine: anything missing falls back to English, and anything missing from English
falls back to the raw key, so an incomplete file can never break the UI.

**Adding a tool** requires no registry edit. Implement `INekoAddon`, tag the class with
`[NekoAddon]`, and it is discovered by reflection and appears in the Hub automatically. The
avatar branch README has the full example.

---

## License

See `LICENSE` on the branch you installed if present; otherwise all rights reserved by NekoSune.
