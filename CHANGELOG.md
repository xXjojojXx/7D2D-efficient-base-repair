# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.1] - 2024-06-09

### Fixed

- Fix performances issues due to useless stats refresh

## [0.1.0] - 2024-06-09

### Fixed

- On dedicated servers, the structure was analysed three times at each refresh.
- Spike blocks at stage dmg=0 or dmg=1 were upgraded for free

### Changed

- The xml parameter `NeedsMaterials` was renamed to `NeedsMaterialsForRepair`
- The default xml parameter `AutoTurnOff` was set to true

### Added

- new batch script to start a local dedicated server
- Add possibility to upgrade structures with a dedicated button
- Add the number of upgradable blocks in the stats pannel of the UI
- Add the total time (repairing + upgrading) in the stats pannel of the UI
- New xml parameter `UpgradeRate`
- New xml parameter `NeedsMaterialsForUpgrade`
- New xml parameter `KeepPaintAfterUpgrade`


## [0.0.3] - 2024-06-04

### Fixed

- Fix translation of the refresh button
- Fix repair timer for dedicated servers
- Fix the UI header text

## [0.0.2] - 2024-06-03

### Fixed

- The block will not generate corrupted chunk anymore

## [0.0.1] - 2024-06-02

### Added

- Implement the repairation blocks algorithm
- Create a dedicated UI for the block
- Set the repairation algorithm parametrizable from xml files


[unreleased]: https://github.com/VisualDev-FR/7D2D-efficient-base-repair/compare/master...unreleased
[0.1.0]: https://github.com/VisualDev-FR/7D2D-efficient-base-repair/compare/0.1.0...0.1.1
[0.1.0]: https://github.com/VisualDev-FR/7D2D-efficient-base-repair/compare/0.0.3...0.1.0
[0.0.3]: https://github.com/VisualDev-FR/7D2D-efficient-base-repair/compare/0.0.2...0.0.3
[0.0.2]: https://github.com/VisualDev-FR/7D2D-efficient-base-repair/compare/0.0.1...0.0.2
[0.0.1]: https://github.com/VisualDev-FR/7D2D-efficient-base-repair/tree/0.0.1
