# Android Installation

## Overview

The Android side of RifeZ Audio Bridge is the receiver app.

Its role is to:

- advertise the receiver on the local network
- accept control connections from the Windows side
- receive PCM audio over Wi-Fi
- play audio through the Android device speaker or receiver hardware
- present receiver state and runtime metrics in the app UI

## Current status

The Android receiver app currently supports:

- signed release APK generation
- direct APK install on Android devices
- automatic receiver startup on app launch
- persistent receiver device naming
- receiver dashboard and session state display
- disconnect and recovery behavior for demo use

## Installation flow

1. Obtain the signed APK from the release bundle
2. Transfer the APK to the Android device
3. Install the APK from the device file manager
4. Open the app
5. Confirm the receiver reaches the `Ready` state

## Expected behavior after launch

After app launch:

- the receiver service should start automatically
- the app should transition into `Ready`
- the device should be available to the Windows tray app for connection

## UI behavior

The Android app is intended to present a simplified receiver workflow:

- `Ready`
- `Connected`
- `Streaming`
- `Recovering`
- `Error`

Typical user actions include:

- Rename Device
- Disconnect Session

## Current distribution model

The current Android distribution model is:

- signed APK distribution
- direct install on test/demo devices

Google Play Store distribution is not currently the target path.

## Release payload structure

The planned Android release payload is staged under:

- `packaging/release/android/`

## Validation checklist

Before an Android release is considered ready, verify:

- signed APK installs successfully
- launcher icon appears correctly
- app opens correctly
- receiver auto-start works
- notification appears correctly
- persisted device name loads correctly
- Windows can connect to the receiver
- audio streaming starts successfully
- disconnect session works
- reconnect after interruption works
- receiver remains recoverable after a PC reboot during streaming