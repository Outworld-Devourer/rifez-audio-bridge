# Known Issues

## Current known items

- Some short Windows notification/bell-style sounds can still introduce brief choppiness or timing disturbance in the live stream even when normal media playback is stable
- Legacy transport/bootstrap pieces are intentionally still present for stability and have not yet been fully unified
- Windows installer flow for the driver is not finalized yet
- Production-grade driver signing path is still pending
- Google Play Store distribution is not currently targeted because foreground service policy/compliance work is still out of scope
- In the current evaluation workflow, driver reinstall may require a reboot between removal and reinstall if root-device removal is deferred until reboot

## Status

These items are known and intentionally deferred while the demo baseline is stabilized.