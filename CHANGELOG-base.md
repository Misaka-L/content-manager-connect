# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Will show rpc api error message when rpc api error occurred. [`#67`](https://github.com/project-vrcz/content-publisher-connect/pull/67)

## [0.3.1] - 2025-12-30

### Added

- Refresh Session when Restore Session. [`#66`](https://github.com/project-vrcz/content-publisher-connect/pull/66)

## [0.3.0] - 2025-12-27

### Changed

- Rename to `VRChat Content Publisher Connect - Base`. [`63`](https://github.com/project-vrcz/content-publisher-connect/pull/63)

### Added

- async version of PreUploadCheck API. [`#60`](https://github.com/project-vrcz/content-publisher-connect/pull/60)

## [0.2.0] - 2025-12-18

### Added

- Auto Launch App when reconnect. [`#56`](https://github.com/project-vrcz/content-publisher-connect/pull/56)
  - Allow control over whether the app launches when reconnected at startup.

### Changed

- Migrate to Yes! Patch Framework. [`#50`](https://github.com/project-vrcz/content-publisher-connect/pull/50)
- Migrate to Yes! Patch Framework Logging System. [`#54`](https://github.com/project-vrcz/content-publisher-connect/pull/54)

## [0.2.0-beta.2] - 2025-12-13

### Changed

- Migrate to Yes! Patch Framework. [`#50`](https://github.com/project-vrcz/content-publisher-connect/pull/50)
- Migrate to Yes! Patch Framework Logging System. [`#54`](https://github.com/project-vrcz/content-publisher-connect/pull/54)

## [0.2.0-beta.1] - 2025-12-13

### Changed

- Migrate to Yes! Patch Framework. [`#50`](https://github.com/project-vrcz/content-publisher-connect/pull/50)

## [0.1.3] - 2025-12-11

### Added

- Smart pre upload check logic (For CAU Extension) [`#40`](https://github.com/project-vrcz/content-publisher-connect/pull/40)
- Better Toolbar. [`#44`](https://github.com/project-vrcz/content-publisher-connect/pull/44)
  - Show Connection Status to App in the toolbar `Tools/VRChat Content Manager Connect/Is RPC Connected`.
  - Allow Restore Session from the toolbar `Tools/VRChat Content Manager Connect/Restore Session`.
  - Allow Toggle Enabled Content Mangaer Publish Flow from `Tools/VRChat Content Manager Connect/Enable Content Manager Publish Flow`.

### Changed

- Auto reconnect when reload domain failed will no logner print error in editor console. [`#42`](https://github.com/project-vrcz/content-publisher-connect/pull/42)
- Auto uppercase when enter Challenge Code. [`#39`](https://github.com/project-vrcz/content-publisher-connect/pull/39)
- Brand new Settings UI. [`#37`](https://github.com/project-vrcz/content-publisher-connect/pull/37)
  - Better looking.
  - Avoid confuse caused by old UI (e.g accidently forget App instance when reconnect failed).

## [0.1.2] - 2025-12-09

### Changed

- Rename to `VRChat Content Manager Connect - Base` to avoid confuse. [`#33`](https://github.com/project-vrcz/content-publisher-connect/pull/33)

### Added

- Show Warning if both Avatars and Worlds Connect Packages are not installed. [`#31`](https://github.com/project-vrcz/content-publisher-connect/pull/31)

## [0.1.1] - 2025-12-08

### Added

- Expose interal type to `xyz.misakal.vpm.vcm-connect.avatars.continuous-avatar-uploader-ext`

## [0.1.0] - 2025-12-08

### Added

- Ability to Connect to VRChat Content Manager App.
- Allow Create new Content. [`#17`](https://github.com/project-vrcz/content-publisher-connect/pull/17)
- Allow restore last session. [`#10`](https://github.com/project-vrcz/content-publisher-connect/pull/10)
- Show current connected VRChat Content Manager Instance Name. [`#6`](https://github.com/project-vrcz/content-publisher-connect/pull/6)
- Allow custom Client Name. [`#13`](https://github.com/project-vrcz/content-publisher-connect/pull/13)
- Cancel CAU Upload if use content manager publish flow enabled, and RPC connection is disconnected. [`#14`](https://github.com/project-vrcz/content-publisher-connect/pull/14)

## Fixed

- Fix unable to build and upload when use Content Mangaer publish flow is disbaled. [`#8`](https://github.com/project-vrcz/content-publisher-connect/pull/8)
- Fix Connect Settings show upgrade sdk warning in non-avatar project. [`#24`](https://github.com/project-vrcz/content-publisher-connect/pull/24)

## [0.1.0-beta.1] - 2025-12-07

### Added

- Ability to Connect to VRChat Content Manager App.
- Allow Create new Content. [`#17`](https://github.com/project-vrcz/content-publisher-connect/pull/17)
- Allow restore last session. [`#10`](https://github.com/project-vrcz/content-publisher-connect/pull/10)
- Show current connected VRChat Content Manager Instance Name. [`#6`](https://github.com/project-vrcz/content-publisher-connect/pull/6)
- Allow custom Client Name. [`#13`](https://github.com/project-vrcz/content-publisher-connect/pull/13)
- Cancel CAU Upload if use content manager publish flow enabled, and RPC connection is disconnected. [`#14`](https://github.com/project-vrcz/content-publisher-connect/pull/14)

## Fixed

- Fix unable to build and upload when use Content Mangaer publish flow is disbaled. [`#8`](https://github.com/project-vrcz/content-publisher-connect/pull/8)

[unreleased]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.3.1...HEAD
[0.3.1]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.3.0...base-v0.3.1
[0.3.0]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.2.0...base-v0.3.0
[0.2.0]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.2.0-beta.2...base-v0.2.0
[0.2.0-beta.2]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.2.0-beta.1...base-v0.2.0-beta.2
[0.2.0-beta.1]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.1.3...base-v0.2.0-beta.1
[0.1.3]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.1.2...base-v0.1.3
[0.1.2]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.1.1...base-v0.1.2
[0.1.1]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.1.0...base-v0.1.1
[0.1.0]: https://github.com/project-vrcz/content-publisher-connect/compare/base-v0.1.0-beta.1...base-v0.1.0
[0.1.0-beta.1]: https://github.com/project-vrcz/content-publisher-connect/releases/tag/base-v0.1.0-beta.1