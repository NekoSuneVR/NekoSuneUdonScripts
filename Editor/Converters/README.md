# NekoSune Converters

All cross-platform conversion lives in this package.

## ChilloutVR CCK 3 / CCK 4

- VRChat Avatar → ChilloutVR Avatar
- Unity/VRChat hierarchy → ChilloutVR Prop / Spawnable
- VRChat World → ChilloutVR World
- Animator Bool toggle/interactable conversion where a real CCK equivalent exists
- optional PhysBone → Dynamic Bone bridge when Dynamic Bone is installed

CCK is detected at runtime. CCK 4 stable is preferred; CCK 3 remains a legacy target. The package does not hard-link either CCK assembly.

## Resonite

- VRChat Avatar → Resonite export through the installed Modular Avatar / NDMF Resonite backend.
- NekoSune does not invent a competing `.resonitepackage` implementation.

## Separation

Optimization is in `com.nekosune.optimizer` and diagnostics are in `com.nekosune.doctors`.
