# World / Udon Runtime Area

This folder is reserved for runtime content used by the **NekoSune Worlds** package.

Use it for things such as:

- UdonSharp behaviours
- reusable world prefabs
- runtime ScriptableObjects
- world-side helper components
- resources that must ship with a VRChat world

Keep Unity Editor-only inspectors, builders, validators, importers, and setup windows under `Editor/World/` instead.

## Suggested structure

```text
Runtime/
  Udon/
    Behaviours/
    Prefabs/
    Data/
Editor/
  World/
    Builders/
    Inspectors/
    Validators/
```

The package already depends on `com.vrchat.worlds`, so future world tooling can be built on top of the VRChat Worlds SDK. Add extra VPM dependencies to the root `package.json` only when a feature actually requires them.
