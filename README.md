# NekoSune Avatar Hub

Base/template package for NekoSune avatar addons.

**Package ID:** `com.nekosune.avatars`

This branch intentionally contains only the shared Avatar Hub, About page, localization, styles and the public addon contract. Feature code lives in separate branches/packages.

## Menu

- `NekoSune → Avatar → Hub`
- `NekoSune → Avatar → About`

Installed addon packages are discovered automatically at editor load through `INekoAddon` + `[NekoAddon]`. There is no central feature registry to edit.

## Current addons

- `com.nekosune.avatar-tools` — Lip Sync Studio + Rank Advisor + shared avatar analysis API
- `com.nekosune.optimizer` — Compressor / Mesh / Quest / VRAM and world optimization
- `com.nekosune.doctors` — Avatar / PhysBone / Face / Animator plus World / Udon doctors
- `com.nekosune.converters` — ChilloutVR CCK 3/4 Avatar + Prop + World and Resonite

## Make a new addon branch

1. Create a new branch from whichever feature package is closest, or from this branch for a minimal editor addon.
2. Give it a unique VPM package ID.
3. Add `com.nekosune.avatars` as a VPM dependency when it contributes Avatar Hub cards.
4. Reference assembly `NekoSune.Avatars.Editor`.
5. Implement the public `INekoAddon` interface and add `[NekoAddon]`.
6. Put the actual tool under `NekoSune → Avatar → ...`.
7. Publish a release ZIP with `package.json` at ZIP root.

See `Templates/AvatarAddonTemplate.cs.txt` for the smallest registration example.

## VCC repository

`https://nekosunevr.github.io/NekoSuneUdonScripts/index.json`
