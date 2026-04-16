# Windows Installation

## Overview

The Windows side of RifeZ Audio Bridge consists of:

- the Windows tray companion app
- the Windows virtual audio driver

The intended user experience is a simple install flow that sets up the tray app and the virtual audio endpoint with minimal manual steps.

## Current status

The Windows tray app builds and runs in Release mode.

Current work is still ongoing in these areas:

- polished installer creation
- production-grade driver signing
- driver installation UX for end users
- default driver audio format tuning

## Intended installation flow

The target Windows installation flow is:

1. Install the RifeZ Audio Bridge tray app
2. Install the RifeZ virtual audio driver
3. Launch the tray app
4. Confirm the Android receiver app is in the `Ready` state
5. Start streaming or allow auto-restore to establish the session

## Components

### Tray app

The tray app is responsible for:

- receiver discovery
- control session management
- audio stream startup
- reconnect and auto-restore behavior
- runtime dashboard and metrics
- optional start-on-login behavior

### Virtual audio driver

The virtual audio driver is responsible for:

- exposing the Windows virtual playback endpoint
- capturing rendered audio
- making audio available to the user-mode bridge path

## Current installation note

At the moment, the driver installation experience is still under active refinement.

The final intended product direction is:

- no test mode requirement
- no Secure Boot disable requirement
- polished end-user installation flow

This depends on the production driver signing path being completed.

## Release payload structure

The planned Windows release payload is staged under:

- `packaging/release/windows/app/`
- `packaging/release/windows/driver/`

## Planned installer scope

The intended Windows installer should eventually support:

- tray app installation
- optional startup registration
- driver installation
- optional tray app launch after install

## Validation checklist

Before a Windows release is considered ready, verify:

- tray app launches from Release output
- tray icons load correctly
- status window opens correctly
- embedded resources work outside the development folder structure
- auto-start behavior works
- reconnect works
- Android receiver can be reached
- streaming starts and stops correctly
- exit behavior fully closes the process
- driver package installs successfully