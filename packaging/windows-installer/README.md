# Windows Installer

## Purpose

This folder contains the Windows installer work for RifeZ Audio Bridge.

The intended installer should provide a polished setup flow for:

- the Windows tray app
- the Windows virtual audio driver

## Current goals

The installer should eventually support:

- tray app installation
- optional startup registration
- driver installation
- optional post-install launch

## Current status

Installer implementation is still in progress.

The main long-term requirement for a polished end-user driver install is:

- production-grade driver signing

## Planned contents

This folder is intended to hold:

- installer scripts
- installer assets
- helper scripts
- release packaging logic