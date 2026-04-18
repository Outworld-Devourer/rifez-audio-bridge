# Driver Development Signing

## Purpose

This document describes the current development and evaluation path for installing the RifeZ virtual audio driver in test mode.

## Current scope

This workflow is intended for:

- internal development
- controlled demo setups
- OEM and technical evaluation
- non-production testing environments

## Important note

This is not the final public driver installation path.

The current workflow may require:

- Windows test mode
- administrative privileges
- specific machine configuration for test-signed driver loading

A production-grade end-user installation path requires proper driver signing and is outside the scope of the current demo baseline.

## Evaluation intent

The current driver package is provided so technical evaluators and OEM partners can:

- validate the virtual endpoint behavior
- evaluate audio routing and streaming behavior
- assess integration potential
- review the complete Windows-to-Android bridge concept

## Included helper scripts

The repository includes scripts for:

- enabling test mode
- installing the driver package
- uninstalling the driver package
- disabling test mode when appropriate

Always review and run these scripts from an elevated command prompt or PowerShell session.