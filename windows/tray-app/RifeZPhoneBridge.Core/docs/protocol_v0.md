# RifeZ Phone Audio Protocol v0

## Purpose

This document defines the current pre-FlatBuffers control protocol used between the Windows client prototype and the Android receiver.

This version is line-based text over TCP and is intended for early integration testing only.

---

## Transport

- Protocol: TCP
- Default port: `49521`
- Encoding: UTF-8
- Framing: newline-delimited text messages
- Connection mode: one client connects directly to the Android receiver

---

## Service Discovery

- Discovery mechanism: DNS-SD / mDNS
- Windows browse service type: `_rifezaudio._tcp.local.`
- Android advertised service type: `_rifezaudio._tcp`
- Example service name: `RifeZ-Kiril-Phone`

The Android receiver advertises itself on the local network and the Windows client discovers it over mDNS/DNS-SD.

---

## Current Session Flow

Typical sequence:

1. Client discovers or knows the receiver endpoint
2. Client opens TCP connection to receiver
3. Client sends `HELLO <client-name>`
4. Receiver replies `OK HELLO <receiver-name>`
5. Client sends `PING`
6. Receiver replies `OK PONG`
7. Client sends `GET_STATUS`
8. Receiver replies `OK STATUS|<state>|<device-name>|<source-name>`
9. Client sends `STREAM_START`
10. Receiver replies `OK STREAM_START`
11. Client may send another `GET_STATUS`
12. Client sends `DISCONNECT`
13. Receiver replies `OK DISCONNECT`

---

## Commands

### `HELLO <client-name>`

Meaning:
- basic handshake from client to receiver
- announces the Windows client name to the Android receiver

Example
```text
HELLO RifeZ-Windows-Bridge