# RifeZ Audio Bridge – Release Notes
## Version: v0.1.0-demo

## Overview

This release establishes the first stable demo baseline of RifeZ Audio Bridge.

The current demo stack includes:

- a Windows virtual audio driver
- a Windows tray companion app
- an Android receiver app

The system is intended to demonstrate Windows-to-Android Wi-Fi audio streaming using a virtual Windows playback endpoint and an Android-based receiver.

## Included in this release

### Android receiver app
- signed release APK
- receiver auto-start on app launch
- persistent device naming
- receiver dashboard with product-facing states
- disconnect and recovery behavior
- reboot/session recovery improvements

### Windows tray app
- Release build of the tray companion app
- tray icon and status window resource embedding
- dashboard and runtime status behavior
- reconnect and auto-restore behavior
- startup behavior handled by the app itself
- stable shortcut launch behavior from installed location

### Windows virtual audio driver
- virtual playback endpoint
- driver-side PCM capture path
- user-mode bridge path for audio forwarding
- stable demo use baseline on tested setup

## Verified demo behaviors

The following behaviors have been validated during the demo stabilization cycle:

- Android receiver installs and launches correctly from signed APK
- receiver enters `Ready`
- Windows tray app launches correctly from Release output
- Windows tray app launches correctly from installed shortcut
- Windows app and Android app establish session correctly
- audio streaming works end-to-end on real hardware
- disconnect and reconnect behavior work
- Android receiver recovers after session loss
- PC reboot during active streaming is recoverable
- auto-restore behavior on the Windows side works correctly
- embedded Windows assets behave correctly outside debug-only path assumptions

## Repository and packaging status

This release also establishes the first combined product repository baseline.

Included repository work:
- combined repo structure for Android, Windows app, and driver
- baseline documentation
- architecture documentation
- install documentation
- release payload staging
- roadmap and known issues tracking

## Known limitations

The following items are known and intentionally deferred:

- polished end-user Windows driver install flow is not complete yet
- production-grade driver signing is still pending
- Windows driver currently defaults to 16-bit / 44.1 kHz instead of the preferred 16-bit / 48 kHz configuration
- short Windows system sounds / notification sounds can still trigger jitter or lag spikes
- legacy transport/bootstrap pieces are intentionally still retained for stability
- Google Play Store distribution is not currently targeted

## Intended use

This release is intended as a stable internal/demo baseline rather than a final consumer/public release.

## Next planned work

- production-grade driver signing path
- polished Windows installer flow including driver installation
- driver default format tuning
- further driver robustness work around Windows notification/system sounds
- final packaging and release polish