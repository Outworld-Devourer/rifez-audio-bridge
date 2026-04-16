# RifeZPhoneBridge.App starter

This starter adds a WinForms tray-host project that keeps the bridge in one process and uses the existing `Core` + `Host` projects directly.

## Intended next repo changes

1. Add this project to the solution.
2. Keep `Runtime` and `ConsoleTest` as diagnostics for now.
3. Use this tray project as the normal product entry point.
4. Keep `StartupBurstFrames = 0` in the shipped baseline.

## Recommended cleanup right after adding the project

- Move current runtime defaults into a shared config source so `Runtime` and `App` do not drift.
- Add an icon resource instead of `SystemIcons.Application`.
- Add a small settings dialog for receiver/manual host and auto-start options.
- Add reconnect backoff rather than one-shot reconnect only.
- Add structured logs/metrics around session lifetime and stream failures.
