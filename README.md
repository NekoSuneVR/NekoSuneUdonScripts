# NekoSune VRChat Tools

Unity Editor tooling for VRChat creators, split into separate avatar and world packages while sharing one **NekoSune** menu inside Unity.

`main` is the **VCC/VPM package-listing branch** and documentation landing page. The actual package source lives on the package branches.

## Packages

| Branch | Package ID | For | Status |
| --- | --- | --- | --- |
| [`avatar`](../../tree/avatar) | `com.nekosune.avatars` | VRChat avatars | Working |
| [`world`](../../tree/world) | To be assigned | VRChat worlds / Udon | Placeholder |
| `main` | `com.nekosune.vrchat-tools` listing | VCC repository + docs | Listing infrastructure |

### Avatar package

The `avatar` branch currently contains:

- **Lip Sync Studio** — creates mouth/viseme animation clips from speech or music.
- **Rank Advisor** — analyses PC and Quest avatar performance statistics and highlights what is holding the avatar rank back.
- Localization for multiple languages.

See the [`avatar` README](../../tree/avatar) for the full tool documentation.

## Install with VRChat Creator Companion

VCC does **not** install a package from:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

and it does not use the package branch's `package.json` as a repository URL.

VCC expects a **VPM repository listing**: an HTTP-served `index.json` containing package versions and release ZIP URLs.

The NekoSune listing URL is:

```text
https://nekosunevr.github.io/NekoSuneUdonScripts/index.json
```

Once GitHub Pages has deployed the listing:

1. Open **VRChat Creator Companion**.
2. Open **Settings**.
3. Open **Packages**.
4. Under **User Repositories / Community Repositories**, choose **Add Repository**.
5. Paste the listing URL above.
6. Open your avatar project and add **NekoSune Avatars**.

A browser can also launch VCC directly with this URI:

```text
vcc://vpm/addRepo?url=https%3A%2F%2Fnekosunevr.github.io%2FNekoSuneUdonScripts%2Findex.json
```

### How the VCC publishing layout works

This repository now follows the same package-listing model as VRChat's `template-package-listing`:

```text
main
├── .github/
│   └── workflows/
│       └── build-listing.yml
├── Website/
│   ├── app.js
│   ├── index.html
│   └── styles.css
├── source.json
└── README.md

avatar
├── .github/
│   └── workflows/
│       └── release-avatar.yml
├── Editor/
├── package.json
├── CHANGELOG.md
└── README.md
```

`source.json` tells the VRChat package-list action which GitHub repositories contain compatible releases. The listing builder scans GitHub Release `.zip` assets, reads the VPM `package.json` from the root of each ZIP, adds the release URL and hash, and publishes the resulting `index.json`.

The `avatar` branch therefore remains the **package source**, while `main` is the **repository listing** VCC consumes.

## Release format

VCC-compatible releases must have the package manifest at the root of the ZIP:

```text
com.nekosune.avatars-0.1.0.zip
├── package.json
├── CHANGELOG.md
└── Editor/
    └── ...
```

A normal GitHub branch archive is not used as the VPM release artifact because GitHub wraps branch archives in an extra directory. The avatar release workflow builds the ZIP from the package root instead.

## Unity Package Manager / Git install

The Git branch URL is still useful for **Unity Package Manager**, just not as a VCC repository URL.

In Unity use **Window → Package Manager → + → Add package from git URL…** and enter:

```text
https://github.com/NekoSuneVR/NekoSuneUdonScripts.git#avatar
```

That directly installs the `avatar` branch through UPM.

You can also clone it into your project's `Packages` folder:

```bash
cd YourUnityProject/Packages
git clone -b avatar https://github.com/NekoSuneVR/NekoSuneUdonScripts.git com.nekosune.avatars
```

## Manual install

Download the `avatar` branch or a compatible release ZIP and put the package in either:

```text
Packages/com.nekosune.avatars
```

or, for a plain Assets-based installation:

```text
Assets/NekoSune/Avatars
```

## Requirements

- Unity 2019.4 or newer for the current avatar package.
- VRChat Creator Companion for VPM installation.
- Git only when using the UPM Git URL or clone methods.

The avatar package uses `com.vrchat.avatars` as a VPM dependency.

## After installing

Available from the Unity menu:

- **NekoSune → Hub**
- **NekoSune → Avatar → Lip Sync Studio**
- **NekoSune → Avatar → Rank Advisor**

## Updating a VCC release

1. Make package changes on `avatar`.
2. Bump `version` in `avatar/package.json` using semantic versioning.
3. Push the `package.json` change to `avatar`, or run **Build Avatar Release** manually from GitHub Actions.
4. The release workflow creates a root-correct VPM ZIP and GitHub Release.
5. The package listing is rebuilt so VCC can see the new version.

Do not delete old package versions after publishing them. Existing VRChat projects can depend on those exact versions.

## Contributing

Open package-code pull requests against the branch that owns that package:

- avatar tooling → `avatar`
- world/Udon tooling → `world`
- VCC listing / landing page → `main`

## License

See the relevant package branch for licensing information; otherwise all rights reserved by NekoSune.
