
## `docs/DRIVER_UNINSTALL.md`

```md
# Driver Uninstall

## Purpose

This document describes the current uninstall path for the RifeZ virtual audio driver in development/evaluation setups.

## Notes

Driver uninstall is currently intended for technical users and controlled testing environments.

Always run uninstall commands with administrative privileges.

## High-level flow

1. Stop any running tray app or active streaming session
2. Remove the installed driver device instance(s) if needed
3. Remove the root device if required by the current workflow
4. Delete the driver package from the driver store
5. Reboot if required
6. Confirm the virtual endpoint is gone from Windows

## Important note

Published INF names such as `oemXXX.inf` can vary between systems.

Always verify the currently installed published name before deletion.

## Important reboot requirement

Root device removal can be deferred until reboot.

If `devcon remove Root\sysvad_ComponentizedAudioSample` reports `Removed on reboot`, reboot Windows before reinstalling the driver or before expecting the previous audio device state to be fully cleared.

## Current evaluation recommendation

Use the repository uninstall script and follow any reboot instruction it prints before continuing with reinstall or further testing.