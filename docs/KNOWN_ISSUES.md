# Known Issues

## Current known items

- Windows virtual audio driver currently defaults to 16-bit / 44.1 kHz instead of the preferred 16-bit / 48 kHz configuration
- Short Windows system sounds / notification sounds can introduce jitter or lag spikes in the audio stream even when steady media playback is stable
- Legacy transport/bootstrap pieces are intentionally still present for stability and have not yet been fully unified
- Windows installer flow is not finalized yet
- Production-grade driver signing path is still pending
- Google Play Store distribution is not currently targeted because foreground service policy/compliance work is still out of scope

## Status

These items are known and intentionally deferred while the demo baseline is stabilized.