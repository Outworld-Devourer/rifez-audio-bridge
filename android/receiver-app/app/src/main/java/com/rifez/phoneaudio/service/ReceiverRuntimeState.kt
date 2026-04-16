package com.rifez.phoneaudio.service

import com.rifez.phoneaudio.discovery.NsdRegistrationState

enum class ReceiverConnectionState {
    Idle,
    Ready,
    Connected,
    Streaming,
    Recovering,
    Error
}

data class ReceiverRuntimeState(
    val receiverEnabled: Boolean = false,
    val connectionState: ReceiverConnectionState = ReceiverConnectionState.Idle,

    val deviceName: String = "Kiril Phone",
    val sourcePcName: String? = null,

    val port: Int? = 49521,
    val codec: String? = "PCM",
    val sampleRate: Int? = 48000,
    val channels: Int? = 2,

    val lastError: String? = null,
    val nsdState: NsdRegistrationState = NsdRegistrationState.Idle,
    val advertisedServiceName: String? = null,

    val controlClientConnected: Boolean = false,
    val audioClientConnected: Boolean = false,
    val playbackActive: Boolean = false,

    val sessionStartedUtcMs: Long? = null,
    val lastFrameUtcMs: Long? = null,

    val framesReceived: Long = 0,
    val bytesReceived: Long = 0,
    val throughputKbps: Double = 0.0,
    val audioLevel: Float = 0f,
    val underrunCount: Int = 0,
    val smoothedAudioLevel: Float = 0f,
    val signalPresent: Boolean = false,
    val lastSignalDetectedUtcMs: Long? = null,
)