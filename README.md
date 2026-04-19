# RifeZ Audio Bridge

RifeZ Audio Bridge is a Windows-to-Android Wi-Fi audio bridge for low-latency speaker streaming demos.

It combines three parts:

- a Windows virtual audio driver
- a Windows tray companion app
- an Android receiver app

The current goal is a stable demo/evaluation product for fast setup audio playback from a Windows PC to an Android-based receiver device over Wi-Fi.

## Current components

### Android receiver app
Located under:

- `android/receiver-app/`

Main responsibilities:

- foreground receiver service
- receiver dashboard and session state UI
- Wi-Fi receiver discovery/advertising
- control session handling
- PCM audio receive and playback

### Windows tray app
Located under:

- `windows/tray-app/`

Main responsibilities:

- background tray application
- receiver discovery and reconnect logic
- control and audio session startup
- status dashboard and runtime metrics
- auto-start / auto-restore workflow

### Windows virtual audio driver
Located under:

- `windows/driver/`

Main responsibilities:

- virtual playback endpoint on Windows
- rendered PCM capture seam
- in-driver PCM ring buffer
- user-mode access path for bridge streaming

## Demo scope

The current demo flow is:

1. install the Android receiver app on a phone
2. install the Windows virtual audio driver
3. run the Windows tray app
4. select the RifeZ virtual audio device in Windows
5. stream Windows audio to the Android receiver over Wi-Fi

## Important Windows driver note

The current Windows driver workflow is intended for:

- internal development
- controlled demo setups
- OEM / technical evaluation

It is not yet the final public end-user installation path.

For the current driver workflow, including test mode requirements and reboot-aware reinstall steps, see:

- `docs/DRIVER_INSTALL_TEST_MODE.md`
- `docs/DRIVER_DEV_SIGNING.md`
- `docs/DRIVER_UNINSTALL.md`

## Repository structure

```text
docs/
android/
  receiver-app/
windows/
  tray-app/
  driver/
packaging/
```
## License

This project is licensed under the MIT License. See `LICENSE` for details.