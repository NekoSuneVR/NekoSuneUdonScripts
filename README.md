# NekoSune Worlds

World and Udon tooling for VRChat, packaged separately from the NekoSune avatar tools.

**Package ID:** `com.nekosune.worlds`

This branch is now a clean world-development starter. It does **not** contain Lip Sync Studio, avatar binders, visemes, avatar performance tools, or avatar-localization data.

## Status

The package framework is ready for world features:

- VCC / VPM package manifest
- VRChat Worlds SDK dependency
- world-only editor assembly
- `NekoSune → World → Hub`
- reflection-based addon registry
- localization framework with an English starter file
- `Editor/World/` for world editor tooling
- `Runtime/Udon/` for UdonSharp and runtime world content
- GitHub Actions release workflow that creates a VPM-compatible ZIP
- automatic rebuild of the shared VCC listing after a new world release

The included **World Template Guide** is scaffolding, not a finished world feature. It exists so the branch can be installed and used as a clean base for future tools.

---

## Install with VRChat Creator Companion

The recommended install method is the shared NekoSune VCC repository:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

1. Open **VRChat Creator Companion**.
2. Go to **Settings → Packages**.
3. Choose **Add Repository**.
4. Paste the listing URL above.
5. Open a VRChat Worlds project.
6. Add **NekoSune Worlds** to the project.

VCC installs the release ZIP referenced by the generated VPM listing. The Git branch URL is not a VCC repository URL.

### Unity Package Manager Git URL

For development, Unity Package Manager can still install the branch directly:

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

The initial editor template does not directly reference VRChat SDK C# types, so the shared editor framework stays easy to extend. Add direct SDK/UdonSharp API usage inside the world feature that needs it.

---

## Menu

After installation:

- **NekoSune → World → Hub**
- **NekoSune → World → Template Guide**

The world menu intentionally has its own submenu so installing both `com.nekosune.avatars` and `com.nekosune.worlds` does not create duplicate `NekoSune → Hub` menu entries.

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
    NekoWorldTemplateWindow.cs

Runtime/
  Udon/
    README.md

.github/
  workflows/
    release-world.yml
```

### Where future code goes

Use `Editor/World/` for things that only run inside the Unity Editor, for example:

- world setup builders
- scene validators
- Udon configuration helpers
- prefab installers
- lighting checks
- performance checks
- world upload helpers
- inspectors and editor windows

Use `Runtime/Udon/` for content that must exist in the built world, for example:

- UdonSharp behaviours
- runtime helper components
- reusable prefabs
- runtime data assets

Do not put avatar-specific tools on this branch. Avatar tooling belongs on the `avatar` branch/package.

---

## Adding a world editor addon

The Hub discovers addons automatically. Implement `INekoAddon` and add `[NekoAddon]`:

```csharp
using UnityEditor;

namespace NekoSune.Worlds.Editor
{
    [NekoAddon(Order = 20)]
    internal sealed class MyWorldToolAddon : INekoAddon
    {
        public string Id { get { return "my-world-tool"; } }
        public string TitleKey { get { return "mytool.title"; } }
        public string DescriptionKey { get { return "mytool.desc"; } }
        public string CategoryKey { get { return "cat.world"; } }
        public string Glyph { get { return "W"; } }
        public bool IsAvailable { get { return true; } }

        public void Open()
        {
            MyWorldToolWindow.Open();
        }
    }
}
```

Add the corresponding strings to `Editor/Localization/Languages/en.json`. More language files can be added later without changing the localization loader.

---

## Adding UdonSharp features

Keep Udon/runtime code separate from editor code. A suggested layout is:

```text
Runtime/Udon/
  Behaviours/
  Prefabs/
  Data/

Editor/World/
  Builders/
  Inspectors/
  Validators/
```

If a future feature needs another VPM package, add it to `vpmDependencies` in `package.json` only when it becomes necessary.

---

## Releases and VCC listing

World releases use tags in this form:

```text
worlds-v0.1.0
worlds-v0.2.0
worlds-v1.0.0
```

The release workflow:

1. reads the version from `package.json`;
2. creates a ZIP with `package.json` at the ZIP root;
3. publishes that ZIP as a GitHub Release asset;
4. triggers the `main` branch VCC listing workflow.

The main listing scans release ZIPs from this repository, so both Avatar and World packages can appear in the same VCC repository:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

To publish an update, change the world package version in `package.json`. Existing released versions are never overwritten.

---

## Branches

| Branch | Package | Purpose |
| --- | --- | --- |
| `avatar` | `com.nekosune.avatars` | VRChat avatar editor tools |
| `world` | `com.nekosune.worlds` | VRChat world and Udon tools |
| `main` | VPM listing | VCC repository and project landing page |

## License

See `LICENSE` if present; otherwise all rights reserved by NekoSune.
