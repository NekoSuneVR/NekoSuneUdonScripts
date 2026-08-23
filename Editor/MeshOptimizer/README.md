# NekoSune Avatar Mesh Compressor

Non-destructive mesh analysis and compression for VRChat avatars.

Open it from:

```text
NekoSune -> Avatar -> Mesh Compressor
```

## What it does

### Mesh analysis

For every `SkinnedMeshRenderer` and `MeshFilter` under the selected avatar, the tool reports:

- triangles
- vertices
- skinned/basic mesh count
- material slots
- blendshape count
- Read/Write state
- duplicate material slots that can be merged
- degenerate / zero-area triangles on readable meshes
- large blendshape meshes that should not be blindly decimated
- Quest/mobile topology warnings

### Safe optimized copies

`Create safe optimized copies` creates new `.asset` meshes under:

```text
Assets/NekoSune/Avatars/OptimizedMeshes/
```

It never overwrites the original FBX/model mesh.

The safe cleanup can:

1. Remove repeated-index and zero-area triangles.
2. Merge submeshes when they already use the exact same material.
3. Remove the redundant material slots created by those merged submeshes.
4. Preserve the original vertex array, UVs, normals, tangents, bone weights, bind poses and blendshapes.

Because vertex topology is preserved, this operation is suitable for facial/blendshape meshes in cases where a generic decimator would be risky.

## Import compression presets

The importer action uses Unity's `ModelImporter.meshCompression` setting:

| Preset | Unity mesh compression |
| --- | --- |
| Lossless | Off |
| Balanced | Low |
| Smaller | Medium |
| Quest | High |

Mesh compression makes the stored mesh data smaller by reducing vertex-data precision. It does **not** reduce triangle count.

When safe importer optimization is enabled, polygon-order optimization is enabled. Vertex-order optimization is only enabled for imported model files that do not contain blendshapes; blendshape models are deliberately protected.

## Quest/mobile guidance

The Quest preset highlights the current VRChat mobile performance thresholds relevant to mesh work:

- 10,000 triangles: current maximum for a **Good** mobile rank
- 20,000 triangles: current maximum for a **Poor** mobile rank
- 2 skinned meshes: current **Poor** maximum
- 4 material slots: current **Poor** maximum

These values are used as diagnostics. The compressor will not pretend that import compression lowers triangle count.

If a mesh genuinely needs topology reduction, the report marks it as a retopology/decimation candidate instead of destroying the mesh automatically.

## Why there is no blind percentage decimator

A naive `50%` triangle button commonly damages:

- faces
- lips/eyelids
- blendshapes
- fingers
- clothing silhouettes
- UV seams
- skin weights

NekoSune intentionally separates **safe compression/cleanup** from destructive topology reduction. A future decimator should be blendshape- and skin-weight-aware and provide a before/after preview rather than randomly dropping triangles.
