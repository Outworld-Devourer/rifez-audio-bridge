# Android Release Payload

This folder is intended to contain the signed Android release APK and related release notes.

## Intended contents

Typical contents include:

- signed release APK
- optional checksum or version notes
- optional short install note for demo users

## Validation

Before staging files here, verify:

- APK installs successfully on a real device
- launcher icon is correct
- notification appears correctly
- receiver auto-start works
- persisted device name loads correctly
- Windows can connect and stream successfully
- disconnect and reconnect behavior work