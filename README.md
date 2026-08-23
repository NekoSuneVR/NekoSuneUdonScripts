# NekoSune World Hub

Base/template package for NekoSune world/Udon addons.

**Package ID:** `com.nekosune.worlds`

This branch stays intentionally small: Hub, About page, localization/styles and the public addon contract. Actual creator features are separate VPM addons.

## Menu

- `NekoSune → World → Hub`
- `NekoSune → World → About`

Addon packages implementing `INekoAddon` with `[NekoAddon]` are discovered automatically. The World Hub does not need to be edited when a new addon branch is published.

## Current addons

- `com.nekosune.world-tools` — lightweight world framework/template helpers
- `com.nekosune.optimizer` — Avatar + World optimizer
- `com.nekosune.doctors` — Avatar + World/Udon diagnostics
- `com.nekosune.converters` — ChilloutVR CCK 3/4 Avatar/Prop/World + Resonite

## Make a new World addon branch

1. Create a branch from this template or from the closest existing addon.
2. Use a unique package ID.
3. Depend on `com.nekosune.worlds`.
4. Reference `NekoSune.Worlds.Editor`.
5. Add a class implementing public `INekoAddon` with `[NekoAddon]`.
6. Put the tool menu under `NekoSune → World → ...`.
7. Publish a normal VPM release ZIP.

See `Templates/WorldAddonTemplate.cs.txt`.

## VCC repository

`https://nekosunevr.github.io/NekoSuneUdonScripts/index.json`
