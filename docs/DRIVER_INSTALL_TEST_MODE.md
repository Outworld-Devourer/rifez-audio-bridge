# Driver Installation in Test Mode

## Purpose

This document describes the current test-mode installation flow for the RifeZ virtual audio driver.

## Prerequisites

Before installation, ensure:

- you are running as Administrator
- you understand this is a development/evaluation driver path
- the machine is suitable for test-signed driver evaluation
- the driver package files are present in the expected driver folder

## Important reboot requirement

If the command

`devcon remove Root\sysvad_ComponentizedAudioSample`

reports that the device is **Removed on reboot**, you must reboot Windows before continuing with driver reinstall.

Reinstalling the driver before that reboot can leave the virtual audio endpoint in a broken state where:

- the device appears installed
- the endpoint appears in Sound settings
- but no audio is actually rendered through the driver

## High-level flow

1. Disable Secure boot on the PC 
 - in my case in BIOS under secure boot select Other OS instead of Windows UEFI (ASSUS way of doing things)
1. Enable Windows test mode if required
`bcdedit /set testsigning on
2. Reboot the PC
3. Install the driver package
4. Confirm the virtual audio endpoint appears in Windows
5. Set the virtual endpoint format to the preferred configuration if needed
6. Launch the Windows tray app and validate streaming

## Notes

- On some systems, existing endpoint state can preserve an earlier default format choice
- Manually setting the endpoint to 16-bit / 48 kHz may be required during evaluation
- Some Windows notification/bell-style sounds can still introduce brief choppiness and are tracked as a known issue

## Recommended validation after install

- endpoint appears in Sound settings
- tray app detects the receiver
- Android app reaches `Ready`
- audio streams successfully
- reboot and reconnect scenarios behave as expected