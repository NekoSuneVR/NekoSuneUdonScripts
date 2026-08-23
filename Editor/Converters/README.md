# ChilloutVR conversion

NekoSune Avatars supports both ChilloutVR CCK generations without taking a hard assembly reference:

- **CCK 4 stable** — recommended/current target. Detected by `CVR_CCK_4_OR_NEWER`, the `CVR.CCK` package/folder, or current CCK component types.
- **CCK 3 legacy** — compatibility target. Detected by `CVR_CCK_EXISTS` without the CCK 4 symbol, the legacy `ABI.CCK` folder, or legacy component types.

CCK 3 and CCK 4 are alternative SDK generations and should not be installed together in the same Unity project. NekoSune detects whichever generation is installed and resolves both `ABI.CCK.*` and `CVR.CCK.*` type locations at runtime.

## Avatar converter

Open **NekoSune → Avatar → ChilloutVR → Convert Avatar** (the older **Convert to ChilloutVR** menu entry is retained too).

The converter creates a separate `<avatar> [ChilloutVR]` hierarchy and can carry:

- viewpoint / voice position
- humanoid head voice parent
- face/body skinned mesh
- VRChat viseme names
- detected blink blendshape
- FX Animator Controller copy
- VRChat Expression Parameters into CVR Advanced Avatar Settings
- Bool → Toggle
- Float → Slider
- Int → Dropdown when multiple values are discoverable
- VRChat Expressions Menu labels as friendly CVR setting names
- optional PhysBone-root/settings → Dynamic Bone v1 bridge when Dynamic Bone is installed

The installed CCK is asked to generate/update its AAS Animator when that editor API is exposed. If a CCK build changes that private/editor method, NekoSune leaves the generated AAS data in place and asks you to run the CCK action manually.

## Prop converter

Open **NekoSune → Avatar → ChilloutVR → Convert to Prop**.

The source can be a normal Unity hierarchy or an object copied from VRChat content. The converter creates a separate `<name> [ChilloutVR Prop]` hierarchy and adds a real `CVRSpawnable`.

Supported conversions include:

- VRChat Pickup → CVR Pickup Object
- VRChat Object Sync → CVR Object Sync
- preserve meshes, materials, colliders, Animators, AudioSources and ParticleSystems
- automatically add a Rigidbody when a converted pickup needs one
- Animator Bool parameters → generated CVR Interactable toggle controls
- generated toggles use **On Interact Down + Global Networked Buffered + Toggle Animator Bool Value**
- optional removal of VRChat/Udon components from the generated copy

The generated toggle holder is named **`[NekoSune CVR Prop Toggles - MOVE/STYLE ME]`** on purpose. Position and style those controls for the final prop before upload.

A standalone menu action, **Generate Animator Toggles for Selected Prop**, can add/update a toggle panel on another selected hierarchy.

## What is not automatic

There is no safe one-to-one translation for arbitrary Udon/UdonSharp logic. Float/Int Animator systems, custom networking, contacts, custom events, parameter-driver-heavy systems and custom scripts should be reviewed and mapped to CVR Spawnable values, CVR Interactables, Lua/WASM or another appropriate ChilloutVR system.

All conversion is non-destructive to the VRChat source. Review the generated copy in the installed CCK before publishing.
