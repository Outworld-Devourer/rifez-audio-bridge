# Release Payloads

This folder contains staged release artifacts for RifeZ Audio Bridge.

## Purpose

The release payload folders are used to collect the tested deliverables for the current demo baseline:

- Android receiver APK
- Windows tray companion app payload
- Windows virtual audio driver payload

These folders are intended to reflect the actual files that should be distributed for a release, rather than development output scattered across project build directories.

## Structure

- `android/`
- `windows/app/`
- `windows/driver/`

## Current release model

The current repository is organized around a demo-oriented release baseline.

At this stage:

- Android release APK is generated and tested by direct install
- Windows tray app Release build is generated and tested
- Windows driver payload is staged separately
- a polished end-user installer is still being prepared
- production-grade driver signing is still pending

## Recommended artifact naming

Use clear versioned file names for staged artifacts, for example:

- `RifeZAudioBridgeReceiver-0.1.0-demo.apk`
- `RifeZAudioBridge-Windows-0.1.0-demo.zip`
- `RifeZAudioDriver-0.1.0-demo.zip`

## Release staging guidance

### Android

Place the signed tested APK in:

- `android/`

### Windows app

Place the tested Release payload for the tray app in:

- `windows/app/`

This should be copied from the tested Release output that successfully launches and runs outside the development folder structure.

### Windows driver

Place the tested driver package payload in:

- `windows/driver/`

This should include the actual installable package contents required for driver setup and validation.

## Validation expectation

Before a release is considered valid, verify that the artifacts staged here are sufficient to reproduce the demo flow without relying on development-only paths.

## Notes

The release payload folders are not the final installer by themselves.

They are the staging point used to prepare:

- release bundles
- installer inputs
- GitHub release assets