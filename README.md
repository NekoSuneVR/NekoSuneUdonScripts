# NekoSune Optimizer

Installable optimization addon for both the NekoSune Avatar Hub and World Hub.

**Package ID:** `com.nekosune.optimizer`

## Avatar modules

- **Rank Advisor** — moved here from Avatar Tools; evaluates the existing VRChat avatar PC/mobile performance-rank metrics and explains which limits are hurting the result.
- **Build Size Advisor** — compressed/download + uncompressed asset-bundle checks for PC and Android/Quest/mobile.
- Compressor
- Mesh Compression
- PC → Quest Assistant
- VRAM / Texture Inspector

### Avatar build-size caps shown by the advisor

```text
PC              200 MB download / 500 MB uncompressed
Android/mobile   10 MB download /  40 MB uncompressed
```

The size advisor tries to read the latest matching build-size values from Unity `Editor.log`. SDK log wording can change, so the panel also allows manual last-build values and tells creators to verify against the VRChat SDK build panel. Source texture/FBX size is **not** treated as the final VRChat compressed/uncompressed bundle size.

If a selected-platform hard size cap is exceeded, the advisor presents it as an upload blocker and tells the creator to optimize/remove features and rebuild.

## World modules

- **World Optimizer** — scene geometry/material/texture/light/particle/audio review.
- **World Platform Advisor** — PC vs Android/mobile scene snapshot plus last-build compressed/uncompressed size review.

VRChat does not publish an official World Performance Rank equivalent to avatar ranks, so NekoSune does not invent one. The World Platform Advisor uses platform guidance instead:

- Android/mobile world compressed build: 100 MB hard upload limit.
- PC world: shows the public-world ~200 MB recommendation as guidance, **not** as an official rank or hard cap.
- uncompressed World size is displayed as useful build information without claiming a public hard uncompressed World limit.

## Dependencies

Optimizer depends on Avatar Tools for the shared avatar reflection/performance API. Doctor buttons are optional navigation shims; NekoSune Doctors does not have to be installed for Optimizer to compile.

The package registers its Avatar and World cards automatically with the two base Hubs and does not contain duplicate Hub code.

VCC repository: `https://nekosunevr.github.io/NekoSuneUdonScripts/index.json`
