# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.0] - 2026-03-12

### Changed

- **BREAKING CHANGE** Bump to Yes! Patch Framework v0.3.0. [`#68`](https://github.com/project-vrcz/content-publisher-connect/pull/68)

## [0.3.0] - 2025-12-27

### Changed

- Rename to `VRChat Content Publisher Connect - Avatars - Continuous Avatar Uploader Extension` [`#63`](https://github.com/project-vrcz/content-publisher-connect/pull/63)

## [0.2.0] - 2025-12-18

### Changed

- Migrate to Yes! Patch Framework. [`#50`](https://github.com/project-vrcz/content-publisher-connect/pull/50)

## [0.2.0-beta.1] - 2025-12-13

### Changed

- Migrate to Yes! Patch Framework. [`#50`](https://github.com/project-vrcz/content-publisher-connect/pull/50)

## [0.1.2] - 2025-12-12

### Changed

- Mark Avatars pacakge as dependence to avoid confuse. [`#49`](https://github.com/project-vrcz/content-publisher-connect/pull/49)

## [0.1.1] - 2025-12-11

### Added

- Smart pre upload check logic [`#40`](https://github.com/project-vrcz/content-publisher-connect/pull/40)
  - Check Connection State:
    - Connected:
      - Check Is Connection Valid:
        - Valid: **Continue Upload**
        - Invalid: **Prevent upload and show dialog.** Disconnect (won't forget session).
    - Diconnected
      - Check Is Last Session Exist:
        - Exist:
          - Try Restore Last Session:
            - Success: **Continue Upload**
            - Failed: **Prevent upload and show dialog.**
        - No Exist:
          - **Prevent upload and show dialog.**

## [0.1.0] - 2025-12-08

### Added

- Show RPC Connection Status in CAU GUI.
- Check RPC Connection Status before CAU upload start
  - Prevent upload if disconnected and use content manager publish flow is enabled.

[unreleased]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.4.0...HEAD
[0.4.0]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.3.0...cau-ext-v0.4.0
[0.3.0]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.2.0...cau-ext-v0.3.0
[0.2.0]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.2.0-beta.1...cau-ext-v0.2.0
[0.2.0-beta.1]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.1.2...cau-ext-v0.2.0-beta.1
[0.1.2]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.1.1...cau-ext-v0.1.2
[0.1.1]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.1.1...cau-ext-v0.1.1
[0.1.1]: https://github.com/project-vrcz/content-publisher-connect/compare/cau-ext-v0.1.0...cau-ext-v0.1.1
[0.1.0]: https://github.com/project-vrcz/content-publisher-connect/releases/tag/cau-ext-v0.1.0
