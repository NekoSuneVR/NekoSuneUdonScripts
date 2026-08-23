# Changelog

All notable changes to **NekoSune Worlds** are documented here.

## 0.2.0

First creator-tool release.

### Added

- **World Doctor** scene scanner.
  - scene descriptor validation;
  - renderer and triangle statistics;
  - material and texture analysis;
  - estimated texture memory;
  - large/uncompressed texture findings;
  - PC and Android/Quest target checks;
  - realtime light and shadow checks;
  - realtime reflection-probe checks;
  - particle-capacity analysis;
  - long `Decompress On Load` audio checks;
  - camera/collider/Rigidbody/Udon counts;
  - Android/Quest post-processing warning;
  - selectable findings and copyable reports.
- **Udon Network Doctor** source scanner.
  - `[UdonSynced]` counting;
  - Manual/Continuous/None/NoVariableSync analysis;
  - Manual sync without `RequestSerialization()` warning;
  - invalid direct DataList/DataDictionary sync detection;
  - `NoVariableSync` plus synced-field detection;
  - `BehaviourSyncMode.None` plus network-call detection;
  - Continuous-sync payload advisories;
  - ownership-flow advisories;
  - network-only NoVariableSync recommendation;
  - Udon Graph/compiled Udon counting;
  - multi-client Build & Test reminder;
  - selectable source findings and copyable reports.

### Changed

- Updated the Hub to put creator-facing tools before the Template Guide.
- Expanded English localization for the new tools.
- Updated documentation from package scaffold to usable creator toolbox.

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
