# Windows Installer

This folder contains the Windows installer work for RifeZ Audio Bridge.

## Current status

A first Inno Setup installer skeleton is present under:

- `inno/RifeZAudioBridge.iss`

The current installer is intended as a baseline for:

- tray app installation
- startup registration option
- staged driver package inclusion
- optional post-install launch

## Notes

The long-term polished end-user install experience still depends on:

- production-grade driver signing
- final driver installation and repair flow
- installer failure handling improvements