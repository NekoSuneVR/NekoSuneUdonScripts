# NekoSune Compressor

`NekoSune → Avatar → Compressor` is the avatar-wide optimization front-end for the NekoSune Avatars package.

It exists because avatar performance is not only a mesh problem. The window reuses the same measurements as **Rank Advisor** and groups the things that can actually be reduced into focused modules.

## What Compressor handles

### Meshes + material slots

Opens the existing mesh optimization workflow with the selected avatar already loaded.

- safe optimized mesh copies
- degenerate triangle removal
- duplicate/unused material-slot removal when submeshes already use the exact same material
- Unity ModelImporter mesh compression presets
- Read/Write repair through the same safe importer path as Rank Advisor

It deliberately does **not** pretend Unity mesh compression reduces triangle count. High triangle counts still require topology reduction/retopology or a blendshape-aware decimator.

### Textures / VRAM

Compressor scans every unique texture used by avatar materials and can apply Android-only `TextureImporter` max-size overrides.

- 512
- 1024
- 2048

The PC texture import settings are left alone. This is mainly intended to reduce Quest/mobile texture memory without damaging the PC version.

### PhysBones

Compressor cross-checks all detected PhysBone colliders against all detected PhysBone chains.

A collider that is not referenced by any chain is offered as a removal candidate. Removal uses Unity Undo.

Overlapping chains, long chains and collider-heavy setups are intentionally sent to **PhysBone Doctor** instead of being silently merged because changing PhysBone topology can change avatar motion.

### Particles

The configured `ParticleSystem.main.maxParticles` values are included because VRChat's performance rank measures active-particle capacity.

Compressor can proportionally scale the max-particle values down to a user-selected total cap. This is explicitly marked as a behaviour-changing operation and uses Unity Undo.

## What remains assisted/manual

Some Rank Advisor categories cannot be safely compressed automatically without potentially changing the avatar:

- bones
- Animator components/controllers
- constraints and constraint depth
- contacts
- skinned-mesh renderer count when meshes have different rigs/material layouts
- true triangle reduction
- lights
- AudioSources
- Cloth
- physics colliders/rigidbodies
- renderer/particle bounds

For those categories Compressor links directly to **Avatar Doctor**, **Quest Assistant**, **PhysBone Doctor**, **Expression + Animator Doctor**, and **Rank Advisor**.

The design rule is simple: automate import/settings cleanup where the result is predictable; make behaviour-changing work explicit; never claim a destructive optimization is safe when it is not.
