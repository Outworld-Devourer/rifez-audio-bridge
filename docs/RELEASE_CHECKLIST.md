# Release Checklist

## Repository

- [ ] Combined repository structure is up to date
- [ ] Baseline documentation is present
- [ ] Roadmap is updated
- [ ] Known issues are updated
- [ ] Relevant backlog issues are created and tracked

## Android

- [ ] Android receiver app builds in Release mode
- [ ] Signed release APK is generated
- [ ] APK installs successfully on a real device
- [ ] Launcher icon is correct
- [ ] Notification icon is correct
- [ ] Receiver auto-start works
- [ ] Device rename persistence works
- [ ] Receiver reaches `Ready`
- [ ] Windows can connect to the receiver
- [ ] Audio streaming works
- [ ] Disconnect and reconnect behavior works
- [ ] Recovery after PC reboot during streaming works

## Windows tray app

- [ ] Tray app builds in Release mode
- [ ] Embedded icons load correctly
- [ ] App launches from Release output
- [ ] Tray icon appears correctly
- [ ] Status window opens correctly
- [ ] Dashboard updates correctly
- [ ] Auto-start behavior works
- [ ] Auto-restore behavior works
- [ ] Reconnect behavior works
- [ ] Exit behavior closes the process fully

## Windows driver

- [ ] Driver package is assembled
- [ ] Driver install steps are documented
- [ ] Driver uninstall steps are documented
- [ ] Virtual endpoint appears correctly in Windows
- [ ] Driver audio path works after reboot
- [ ] Known driver issues are recorded
- [ ] Production signing path status is documented

## Packaging

- [ ] Android release payload is staged under `packaging/release/android/`
- [ ] Windows app payload is staged under `packaging/release/windows/app/`
- [ ] Windows driver payload is staged under `packaging/release/windows/driver/`
- [ ] Quick start documentation is updated
- [ ] Install documentation is updated

## Release readiness

- [ ] Full end-to-end demo tested on real hardware
- [ ] Reboot scenarios tested
- [ ] Clean-machine installation path reviewed
- [ ] Release tag prepared
- [ ] Release notes drafted