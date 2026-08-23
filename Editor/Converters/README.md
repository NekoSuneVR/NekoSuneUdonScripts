# VRChat → ChilloutVR World Converter

Open **NekoSune → World → Convert VRChat World to ChilloutVR**.

The converter supports:

- **CCK 4 stable** (recommended/current)
- **CCK 3 legacy**

It has no hard compile-time CCK assembly reference. The editor tool detects `CVR_CCK_4_OR_NEWER`, the legacy CCK symbol, and both `CVR.CCK.*` / `ABI.CCK.*` component locations at runtime.

## Non-destructive scene workflow

The active VRChat scene is never converted in place. NekoSune:

1. asks Unity to save pending source changes,
2. copies the scene into `Assets/NekoSune/Worlds/ChilloutVR/`,
3. opens the copied scene,
4. adds a real `CVRWorld`,
5. converts supported components/interactions,
6. optionally strips VRChat SDK/Udon components from the CVR copy,
7. saves a conversion report beside the copied scene.

This is intentionally similar to the modern CCK philosophy of processing duplicated build content instead of destructively rewriting authoring data.

## Converted world setup

Where the installed SDK exposes matching fields/components, NekoSune carries or creates:

- VRChat scene descriptor → CVR World root
- spawn-point transforms
- reference camera
- respawn height
- VRChat Pickup → CVR Pickup Object
- VRChat Object Sync → CVR Object Sync
- VRChat Mirror Reflection → CVR Mirror
- VRChat video-player markers → CVR Video Player
- VRChat Station → CVR Interactable sit action when the installed CCK exposes the expected interaction model
- Animator Bool parameters → CVR Interactable toggle controls

Generated Animator controls are put under:

`[NekoSune CVR Animator Toggles - MOVE/STYLE ME]`

Each supported Bool control uses the CCK interaction model with **On Interact Down**, **Global Networked Buffered**, and **Toggle Animator Bool Value**. Move/style those generated controls before publishing.

## Udon / custom logic

Udon and UdonSharp are VRChat-specific executable logic, so a generic converter cannot truthfully promise a lossless translation. NekoSune records every detected Udon behaviour in the generated conversion report before optional stripping.

Common replacements may include:

- CVR Interactable actions
- CVR Object Sync
- CVR Spawnable values
- CVR Variable Buffer / APF
- Lua or WASM for custom behaviour

Complex synced games, persistence, custom video logic, networking ownership flows, Udon events, custom pickups and third-party Udon frameworks should be manually verified after conversion.

## CCK versions

CCK 3 and CCK 4 should not be installed side-by-side in one Unity project. Use one CCK generation in the target ChilloutVR project. NekoSune adapts to the installed generation and keeps CCK optional for normal VRChat world projects.
