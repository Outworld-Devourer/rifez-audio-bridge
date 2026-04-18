# Driver Signing Plan

## Goal

Enable a polished end-user Windows installation flow for the RifeZ virtual audio driver without requiring:

- test mode
- Secure Boot disable
- development-only installation steps

## Current status

The current demo baseline is functional, but the driver install path is still development-oriented.

The long-term release goal is a properly signed kernel-mode driver package that can be installed through a normal user-facing setup flow.

## Target signing direction

The intended path is:

- obtain an EV code-signing certificate
- register in the Microsoft Windows Hardware Developer Program
- prepare the driver package for Microsoft submission
- use Partner Center hardware submission for driver signing
- integrate the signed driver package into the Windows installer

## Why this path is needed

Modern Windows kernel-mode drivers intended for broad installation use the Windows Hardware Developer Center / Partner Center signing path.

This is the correct path for a polished product install experience and avoids relying on development-only system configuration changes.

## Planned workstream

### Phase 1 — organizational readiness
- choose the organization identity that will own the driver submission path
- decide who will hold signing and Partner Center access
- define where signing artifacts and credentials will be stored securely

### Phase 2 — EV certificate
- purchase or assign an EV code-signing certificate
- verify certificate availability and organization ownership
- prepare secure handling and renewal tracking

### Phase 3 — Hardware Developer Program registration
- register in the Microsoft Windows Hardware Developer Program
- associate the EV certificate with the hardware dashboard account
- verify dashboard access and submission permissions

### Phase 4 — driver package preparation
- clean and validate the driver package
- review INF/package completeness
- confirm package contents to be submitted
- ensure packaging conventions are stable enough for submission

### Phase 5 — submission path
- prepare CAB package for hardware submission
- sign the CAB with the EV certificate
- submit through Partner Center hardware submission flow
- retrieve the Microsoft-signed package
- validate installation behavior on clean systems

### Phase 6 — installer integration
- replace the development/demo driver install path with the signed package
- integrate the signed package into the Windows installer
- validate end-user installation flow

## Package preparation notes

Before submission, the driver package should be reviewed for:

- stable package contents
- correct INF references
- clean versioning
- release naming consistency
- reproducible build output

## Installer impact

The final Windows installer should eventually be able to:

- install the tray app
- install the signed driver package
- avoid test mode requirements
- avoid Secure Boot disable requirements
- provide a more polished setup experience for non-developer users

## Risks and blockers

Potential blockers include:

- EV certificate acquisition time
- Partner Center registration and verification delays
- submission/package validation issues
- driver package cleanup work before submission
- release process hardening for repeatable signed builds

## Current recommendation

Continue product packaging and documentation work in parallel, but treat proper driver signing as the next major release-enabling workstream.