# Architecture

## Overview

RifeZ Audio Bridge is a Windows-to-Android Wi-Fi audio bridge.

The current demo product is built from three main parts:

- a Windows virtual audio driver
- a Windows tray companion app
- an Android receiver app

Together, these components allow audio rendered to a virtual Windows playback endpoint to be forwarded over Wi-Fi and played on an Android-based receiver.

## High-level flow

1. Windows renders audio to the RifeZ virtual playback device
2. The virtual driver captures rendered PCM
3. The Windows tray/runtime path reads PCM from the driver
4. The Windows side discovers and connects to the Android receiver
5. Audio is streamed over Wi-Fi to the Android device
6. Android plays the PCM stream through its audio output path

## Component breakdown

### Windows virtual audio driver

Located under:

- `windows/driver/`

Responsibilities:

- expose the Windows virtual playback endpoint
- capture rendered PCM from the SYSVAD-based path
- maintain an in-driver PCM ring buffer
- expose a user-mode control/read interface for bridge access

Current notes:

- based on SYSVAD
- current demo focuses on a single endpoint
- driver behavior is stable enough for demo use
- some driver improvements are intentionally deferred

Known later-stage items include:

- preferred default install format at 16-bit / 48 kHz
- further robustness work for short Windows system sounds / notification sounds

### Windows tray companion app

Located under:

- `windows/tray-app/`

Responsibilities:

- run as the user-facing Windows background app
- manage receiver discovery
- manage control and audio session startup
- provide reconnect and auto-restore behavior
- provide tray icon state and runtime dashboard visibility
- expose a simple user experience for starting/stopping/reconnecting

Current notes:

- Release build is working
- tray icons are embedded as resources
- startup and auto-restore behavior has been stabilized
- reconnect behavior has been hardened around receiver readiness and reboot scenarios

### Android receiver app

Located under:

- `android/receiver-app/`

Responsibilities:

- advertise the receiver on the local network
- host the receiver foreground service
- accept control connections from Windows
- accept PCM audio sessions from Windows
- render received PCM via Android audio output
- present receiver state, metrics, and session information in the UI

Current notes:

- signed release APK generation is working
- app auto-starts receiver behavior on launch
- receiver device name persists
- session recovery behavior has been stabilized for reboot/disconnect cases

## Current network/session model

The current product uses a split session model:

- discovery / advertised receiver identity
- control channel
- audio PCM channel

### Discovery

Android advertises the receiver using NSD / mDNS on the local network.

This allows the Windows side to locate the receiver device on the LAN.

### Control session

The Windows side establishes a control connection to the receiver.

This is used for:

- receiver hello / handshake
- session readiness
- stream configuration
- session control transitions

### Audio session

Once configured, the Windows side opens the PCM audio path and streams frames to Android over Wi-Fi.

Android receives those frames and feeds them into its playback output path.

## Session state model

### Windows app

The Windows tray app currently moves through states such as:

- `Idle`
- `Discovering`
- `ReceiverSelected`
- `ControlConnected`
- `StreamConfigured`
- `Streaming`
- `WaitingForReceiver`
- `Faulted`

These states are used for:

- tray icon state
- dashboard status
- reconnect and recovery behavior

### Android app

The Android receiver app currently uses product-facing states such as:

- `Ready`
- `Connected`
- `Streaming`
- `Recovering`
- `Error`

These states reflect:

- receiver service readiness
- control session presence
- active audio streaming
- recovery after unexpected session loss

## Recovery and reconnect behavior

A major design goal of the demo stack is resilience during common interruptions.

Current behavior includes:

- Windows tray app retry/restore behavior on launch
- Android receiver recovery after session loss
- proper reconnect after manual disconnect
- recovery after PC reboot during active streaming

The Android service now owns session recycle behavior so that:

- manual disconnect
- unexpected remote disconnect
- PC reboot during streaming

all converge toward a clean reconnectable receiver state.

## Repository layout

```text
android/
  receiver-app/

windows/
  tray-app/
  driver/

docs/
packaging/