# Windows Driver

## Overview

This folder contains the Windows virtual audio driver source, prepared evaluation package files, and helper scripts for the current RifeZ Audio Bridge driver workflow.

## Layout

- `audio/sysvad/` – SYSVAD-based driver source tree
- `wil/` – required WIL dependency subtree
- `package/` – prepared driver package for evaluation installs
- `package.cer` - the corespondig auto generated certificate when building the driver
- `scripts/` – install, reinstall, and uninstall helper scripts

## Important source layout note

The driver project currently expects the preserved upstream-style folder layout in order to build successfully.

In particular, the current working layout preserves:

- `audio/sysvad/`
- `wil/`

Flattening or relocating only the `sysvad` subtree without preserving the expected relative structure can break includes and project references.

## Build notes

The driver source is intended for a Windows driver development environment using the appropriate Visual Studio and WDK setup.

The current repository preserves the folder structure needed by the project files rather than attempting to repack the driver source into a flattened standalone layout.

## Evaluation install workflow

Prepared installable driver files are staged under:

- `package/`

Helper scripts for install, reinstall, and uninstall are staged under:

- `scripts/`

The current driver install flow is evaluation-oriented and may require:

- administrative privileges
- Windows test mode
- Secure Boot changes depending on machine configuration
- reboot-aware reinstall behavior when root-device removal is deferred until reboot

See:

- `../../docs/DRIVER_INSTALL_TEST_MODE.md`
- `../../docs/DRIVER_UNINSTALL.md`
- `../../docs/KNOWN_ISSUES.md`

## Upstream-derived material

This driver subtree includes upstream-derived material and preserved notices.

Relevant upstream files include:

- `LICENSE.microsoft-driver-samples`
- `UPSTREAM_DRIVER_SAMPLES_README.md`

The `wil/` subtree also preserves its own upstream notices and related files.

For a repository-level summary, see:

- `../../THIRD_PARTY_NOTICES.md`

## Notes

This folder is organized to support:

- source inspection
- reproducible evaluation builds
- prepared package-based testing
- clearer separation between source, package payload, and helper scripts