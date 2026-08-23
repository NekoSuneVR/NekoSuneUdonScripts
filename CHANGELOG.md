# Changelog

All notable changes to **NekoSune Worlds** are documented here.

## 0.1.0

Initial world-package scaffold.

### Changed

- Converted the branch from the copied avatar package into `com.nekosune.worlds`.
- Renamed the editor namespace and assembly to `NekoSune.Worlds.Editor`.
- Moved the Hub to `NekoSune → World → Hub` so it can coexist with the avatar package.
- Reset localization to a clean world-focused English starter.

### Added

- VRChat Worlds VPM dependency.
- `Editor/World/` starter area.
- `Runtime/Udon/` starter area for future UdonSharp/runtime content.
- World Template Guide editor window.
- VPM-compatible world release workflow using `worlds-v<version>` tags.
- Automatic shared VCC listing rebuild after a world release.

### Removed

- Lip Sync Studio.
- avatar audio analysis and animation generation.
- avatar binders and viseme code.
- avatar-specific package identity and paths.
- avatar-specific translated strings copied into the old world placeholder branch.
