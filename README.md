# NekoSune Avatar Tools

The lightweight shared Avatar helper package for the NekoSune Hub ecosystem.

## Included

- shared avatar reflection/performance API used by Optimizer, Doctors, Converters and Animation Tools
- beginner **Toggle + Menu Builder**
  - Bool Expression Parameter
  - Expression Menu Toggle
  - OFF / ON AnimationClips
  - FX Animator Bool parameter
  - FX OFF/ON state layer
  - attempts to assign/create the avatar's expression assets and FX controller

The setup builder discovers VRChat SDK types through reflection. Avatar Tools intentionally does not hard-depend on `com.vrchat.avatars`, because Optimizer/Doctors/Converters may be installed in World-only projects too.

## Moved to dedicated addons

- **Lip Sync Studio** → `com.nekosune.animation-tools`
- **Rank Advisor** → `com.nekosune.optimizer`

This keeps Avatar Tools focused on reusable APIs and simple avatar setup helpers.
