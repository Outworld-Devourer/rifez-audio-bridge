# Driver Installation in Test Mode

## Purpose

This document describes the current test-mode installation flow for the RifeZ virtual audio driver.

## Scope

This workflow is intended for:

- internal development
- controlled demo setups
- OEM / technical evaluation
- non-production testing environments

It is not the final public driver installation path.

## Prerequisites

Before installation, ensure:

- you are running as Administrator
- you understand this is a development/evaluation driver path
- the machine is suitable for test-signed driver evaluation
- the driver package files are present in the expected repository driver folder
- `devcon.exe` is available in the scripts folder if required by the workflow

## Important test-mode note

Depending on machine configuration, test-signed driver loading may require:

- Windows test mode
- Secure Boot changes
- reboot(s)

## Important reboot requirement

If the command

`devcon remove Root\sysvad_ComponentizedAudioSample`

reports that the device is **Removed on reboot**, you must reboot Windows before continuing with driver reinstall.

Reinstalling the driver before that reboot can leave the virtual audio endpoint in a broken state where:

- the device appears installed
- the endpoint appears in Sound settings
- but no audio is actually rendered through the driver

## High-level evaluation flow

### Initial test-mode setup

1. Disable Secure Boot if required by the current test-signed driver workflow
2. Enable Windows test mode if required:

```cmd
bcdedit /set testsigning on