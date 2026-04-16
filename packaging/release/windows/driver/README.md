# Windows Driver Release Payload

This folder is intended to contain the packaged Windows virtual audio driver deliverables.

## Intended contents

The driver payload should include the files needed for installation and validation of the virtual audio endpoint.

Typical contents include:

- INF file
- SYS file
- CAT file, if available
- any supporting package files required for installation

## Current notes

The driver install experience is still under active refinement.

The long-term target is:

- no test mode requirement
- no Secure Boot disable requirement
- production-grade signed install flow

## Validation

Before staging files here, verify:

- package files are complete
- the virtual endpoint appears correctly after install
- audio path works after install
- audio path still works after reboot
- known driver issues are documented