# Quick Start

## Android receiver

1. Install the signed Android APK on the receiver phone.
2. Launch the app.
3. Confirm the receiver enters the `Ready` state.

## Windows

1. Install the Windows virtual audio driver.
2. Launch the Windows tray app.
3. Confirm the tray app is running.
4. Set the RifeZ virtual audio device as the Windows output device if needed.
5. Wait for the tray app to connect and start streaming, or use `Start Streaming`.

## Expected behavior

- Android app should move through `Ready`, `Connected`, and `Streaming`
- Windows tray app should show receiver discovery and active streaming state
- Audio should be heard from the Android receiver device

## Notes

This is currently a demo-oriented setup. Packaging and install flow are still being refined.