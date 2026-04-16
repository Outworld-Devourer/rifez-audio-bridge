package com.rifez.phoneaudio.service

import com.rifez.phoneaudio.discovery.NsdRegistrationState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlin.math.max

object ReceiverStateRepository {

    private val _runtimeState = MutableStateFlow(ReceiverRuntimeState())
    val runtimeState: StateFlow<ReceiverRuntimeState> = _runtimeState.asStateFlow()

    fun updateState(transform: (ReceiverRuntimeState) -> ReceiverRuntimeState) {
        _runtimeState.value = transform(_runtimeState.value)
    }

    fun setState(newState: ReceiverRuntimeState) {
        _runtimeState.value = newState
    }

    fun setReady() {
        _runtimeState.value = _runtimeState.value.copy(
            receiverEnabled = true,
            connectionState = ReceiverConnectionState.Ready,
            sourcePcName = null,
            lastError = null,
            controlClientConnected = false,
            audioClientConnected = false,
            playbackActive = false,
            sessionStartedUtcMs = null,
            lastFrameUtcMs = null,
            framesReceived = 0,
            bytesReceived = 0,
            throughputKbps = 0.0,
            audioLevel = 0f,
            underrunCount = 0
        )
    }

    fun setConnected(pcName: String) {
        val now = System.currentTimeMillis()
        _runtimeState.value = _runtimeState.value.copy(
            receiverEnabled = true,
            connectionState = ReceiverConnectionState.Connected,
            sourcePcName = pcName,
            lastError = null,
            controlClientConnected = true,
            audioClientConnected = false,
            playbackActive = false,
            sessionStartedUtcMs = now,
            throughputKbps = 0.0,
            audioLevel = 0f
        )
    }

    fun setError(message: String) {
        _runtimeState.value = _runtimeState.value.copy(
            receiverEnabled = true,
            connectionState = ReceiverConnectionState.Error,
            lastError = message,
            playbackActive = false,
            audioClientConnected = false,
            audioLevel = 0f
        )
    }

    fun disconnect() {
        val current = _runtimeState.value
        _runtimeState.value = current.copy(
            receiverEnabled = current.receiverEnabled,
            connectionState = if (current.receiverEnabled) ReceiverConnectionState.Ready else ReceiverConnectionState.Idle,
            sourcePcName = null,
            lastError = null,
            controlClientConnected = false,
            audioClientConnected = false,
            playbackActive = false,
            sessionStartedUtcMs = null,
            lastFrameUtcMs = null,
            framesReceived = 0,
            bytesReceived = 0,
            throughputKbps = 0.0,
            audioLevel = 0f,
            underrunCount = 0
        )
    }

    fun renameDevice(newName: String) {
        _runtimeState.value = _runtimeState.value.copy(deviceName = newName)
    }

    fun updateNsdState(
        nsdState: NsdRegistrationState,
        advertisedServiceName: String? = null
    ) {
        _runtimeState.value = _runtimeState.value.copy(
            nsdState = nsdState,
            advertisedServiceName = advertisedServiceName,
            lastError = if (nsdState is NsdRegistrationState.Failed) nsdState.message else _runtimeState.value.lastError
        )
    }
    fun setRecovering(pcName: String?, message: String? = null) {
        _runtimeState.value = _runtimeState.value.copy(
            receiverEnabled = true,
            connectionState = ReceiverConnectionState.Recovering,
            sourcePcName = pcName,
            lastError = message,
            playbackActive = false,
            audioClientConnected = false,
            audioLevel = 0f,
            smoothedAudioLevel = 0f,
            signalPresent = false,
            lastSignalDetectedUtcMs = null
        )
    }
    fun setControlConnection(
        connected: Boolean,
        pcName: String? = _runtimeState.value.sourcePcName
    ) {
        val current = _runtimeState.value
        _runtimeState.value = current.copy(
            sourcePcName = if (connected) (pcName ?: current.sourcePcName) else null,
            controlClientConnected = connected,
            connectionState = when {
                !current.receiverEnabled -> ReceiverConnectionState.Idle
                connected && current.audioClientConnected -> ReceiverConnectionState.Streaming
                connected -> ReceiverConnectionState.Connected
                current.receiverEnabled -> ReceiverConnectionState.Ready
                else -> ReceiverConnectionState.Idle
            },
            playbackActive = if (connected) current.playbackActive else false,
            audioLevel = if (connected) current.audioLevel else 0f,
            smoothedAudioLevel = if (connected) current.smoothedAudioLevel else 0f,
            signalPresent = if (connected) current.signalPresent else false,
            lastSignalDetectedUtcMs = if (connected) current.lastSignalDetectedUtcMs else null
        )
    }

    fun setAudioConnection(connected: Boolean) {
        val current = _runtimeState.value
        _runtimeState.value = current.copy(
            audioClientConnected = connected,
            playbackActive = connected && current.playbackActive,
            audioLevel = if (connected) current.audioLevel else 0f,
            smoothedAudioLevel = if (connected) current.smoothedAudioLevel else 0f,
            signalPresent = if (connected) current.signalPresent else false,
            lastSignalDetectedUtcMs = if (connected) current.lastSignalDetectedUtcMs else null,
            connectionState = when {
                !connected && current.controlClientConnected -> ReceiverConnectionState.Connected
                !connected && current.receiverEnabled -> ReceiverConnectionState.Ready
                else -> current.connectionState
            }
        )
    }

    fun updateAudioFormat(
        codec: String? = _runtimeState.value.codec,
        sampleRate: Int? = _runtimeState.value.sampleRate,
        channels: Int? = _runtimeState.value.channels
    ) {
        _runtimeState.value = _runtimeState.value.copy(
            codec = codec,
            sampleRate = sampleRate,
            channels = channels
        )
    }

    fun noteFrameReceived(payloadBytes: Int, normalizedAudioLevel: Float) {
        val current = _runtimeState.value
        val now = System.currentTimeMillis()
        val nextFrames = current.framesReceived + 1
        val nextBytes = current.bytesReceived + payloadBytes
        val sessionStart = current.sessionStartedUtcMs ?: now
        val sessionSec = max(0.001, (now - sessionStart) / 1000.0)
        val throughput = (nextBytes / 1024.0) / sessionSec

        val clampedLevel = normalizedAudioLevel.coerceIn(0f, 1f)

        // Smooth the instantaneous level so UI motion feels stable.
        val previousSmoothed = current.smoothedAudioLevel
        val smoothedLevel =
            if (clampedLevel >= previousSmoothed) {
                previousSmoothed * 0.65f + clampedLevel * 0.35f
            } else {
                previousSmoothed * 0.85f + clampedLevel * 0.15f
            }

        // Hysteresis + hold:
        // - turn signal ON at a slightly higher threshold
        // - keep it ON for a short hold window
        // - only turn OFF after both level and hold window fall away
        val signalOnThreshold = 0.06f
        val signalOffThreshold = 0.025f
        val holdMs = 350L

        val signalDetectedNow = smoothedLevel >= signalOnThreshold
        val lastSignalDetectedUtcMs =
            if (signalDetectedNow) now else current.lastSignalDetectedUtcMs

        val keepSignalAlive =
            lastSignalDetectedUtcMs != null && (now - lastSignalDetectedUtcMs) <= holdMs

        val signalPresent = when {
            signalDetectedNow -> true
            smoothedLevel >= signalOffThreshold && current.signalPresent -> true
            keepSignalAlive -> true
            else -> false
        }

        _runtimeState.value = current.copy(
            receiverEnabled = true,
            connectionState = ReceiverConnectionState.Streaming,
            audioClientConnected = true,
            controlClientConnected = true,
            playbackActive = true,
            sessionStartedUtcMs = sessionStart,
            lastFrameUtcMs = now,
            framesReceived = nextFrames,
            bytesReceived = nextBytes,
            throughputKbps = throughput,
            audioLevel = clampedLevel,
            smoothedAudioLevel = smoothedLevel,
            signalPresent = signalPresent,
            lastSignalDetectedUtcMs = lastSignalDetectedUtcMs,
            lastError = null
        )
    }

    fun notePlaybackIdle() {
        val current = _runtimeState.value
        _runtimeState.value = current.copy(
            playbackActive = false,
            audioLevel = 0f,
            smoothedAudioLevel = current.smoothedAudioLevel * 0.7f,
            signalPresent = false,
            lastSignalDetectedUtcMs = null,
            connectionState = when {
                current.receiverEnabled && current.controlClientConnected -> ReceiverConnectionState.Connected
                current.receiverEnabled -> ReceiverConnectionState.Ready
                else -> ReceiverConnectionState.Idle
            }
        )
    }

    fun noteUnderrun() {
        val current = _runtimeState.value
        _runtimeState.value = current.copy(
            underrunCount = current.underrunCount + 1,
            playbackActive = false,
            audioLevel = 0f,
            signalPresent = false,
            lastSignalDetectedUtcMs = null
        )
    }

    fun refreshStreamingTimeout(nowUtcMs: Long = System.currentTimeMillis()) {
        val current = _runtimeState.value

        if (current.connectionState != ReceiverConnectionState.Streaming) {
            return
        }

        val lastFrameUtcMs = current.lastFrameUtcMs ?: run {
            _runtimeState.value = current.copy(
                playbackActive = false,
                audioClientConnected = false,
                audioLevel = 0f,
                smoothedAudioLevel = 0f,
                signalPresent = false,
                lastSignalDetectedUtcMs = null,
                connectionState = when {
                    current.controlClientConnected -> ReceiverConnectionState.Connected
                    current.receiverEnabled -> ReceiverConnectionState.Ready
                    else -> ReceiverConnectionState.Idle
                }
            )
            return
        }

        val silenceTimeoutMs = 750L
        val elapsedMs = nowUtcMs - lastFrameUtcMs
        if (elapsedMs < silenceTimeoutMs) {
            return
        }

        _runtimeState.value = current.copy(
            playbackActive = false,
            audioClientConnected = false,
            audioLevel = 0f,
            smoothedAudioLevel = 0f,
            signalPresent = false,
            lastSignalDetectedUtcMs = null,
            connectionState = when {
                current.controlClientConnected -> ReceiverConnectionState.Connected
                current.receiverEnabled -> ReceiverConnectionState.Ready
                else -> ReceiverConnectionState.Idle
            }
        )
    }

}