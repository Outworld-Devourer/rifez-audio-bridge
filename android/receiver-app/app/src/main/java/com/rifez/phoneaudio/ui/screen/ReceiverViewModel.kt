package com.rifez.phoneaudio.ui.screen

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.rifez.phoneaudio.service.ReceiverConnectionState
import com.rifez.phoneaudio.service.ReceiverRuntimeState
import com.rifez.phoneaudio.service.ReceiverStateRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import kotlin.math.roundToInt

class ReceiverViewModel : ViewModel() {

    private val _uiState = MutableStateFlow(
        mapRuntimeToUi(ReceiverStateRepository.runtimeState.value)
    )
    val uiState: StateFlow<ConnectionUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            ReceiverStateRepository.runtimeState.collectLatest { runtime ->
                _uiState.value = mapRuntimeToUi(runtime)
            }
        }
    }

    fun onServiceStartRequested() {
        ReceiverStateRepository.setReady()
    }

    fun renameDevice(newName: String) {
        ReceiverStateRepository.renameDevice(newName)
    }

    private fun mapRuntimeToUi(runtime: ReceiverRuntimeState): ConnectionUiState {
        val visualState = when (runtime.connectionState) {
            ReceiverConnectionState.Idle -> ConnectionVisualState.Ready
            ReceiverConnectionState.Ready -> ConnectionVisualState.Ready
            ReceiverConnectionState.Connected -> ConnectionVisualState.Connected
            ReceiverConnectionState.Streaming -> ConnectionVisualState.Streaming
            ReceiverConnectionState.Recovering -> ConnectionVisualState.Recovering
            ReceiverConnectionState.Error -> ConnectionVisualState.Error
        }

        val sourceName = runtime.sourcePcName ?: "No source"
        val advertisedName = runtime.advertisedServiceName ?: "Not advertised"
        val audioFormat = buildAudioFormat(runtime)
        val uptime = buildUptime(runtime)
        val metricsSummary =
            "Frames ${runtime.framesReceived} · ${runtime.throughputKbps.roundToInt()} KB/s · Underruns ${runtime.underrunCount}"

        val primaryButtonText = when (runtime.connectionState) {
            ReceiverConnectionState.Idle -> "Start Receiver"
            ReceiverConnectionState.Ready -> "Receiver Ready"
            ReceiverConnectionState.Connected -> "Disconnect Session"
            ReceiverConnectionState.Streaming -> "Disconnect Session"
            ReceiverConnectionState.Recovering -> "Disconnect Session"
            ReceiverConnectionState.Error -> "Disconnect Session"
        }

        val secondaryButtonText = when (runtime.connectionState) {
            ReceiverConnectionState.Idle -> ""
            ReceiverConnectionState.Ready -> "Rename Device"
            ReceiverConnectionState.Connected,
            ReceiverConnectionState.Streaming,
            ReceiverConnectionState.Recovering,
            ReceiverConnectionState.Error -> ""
        }

        return ConnectionUiState(
            visualState = visualState,
            deviceName = runtime.deviceName,
            sourcePcName = runtime.sourcePcName,
            headline = buildHeadline(runtime),
            subheadline = buildSubheadline(runtime),
            endpointInfo = "Endpoint $advertisedName · Port ${runtime.port ?: 49521}",
            audioInfo = audioFormat,
            sessionInfo = "Source $sourceName · Uptime $uptime",
            metricsInfo = metricsSummary,
            signalLevel = runtime.smoothedAudioLevel,
            signalPresent = runtime.signalPresent,
            isStreaming = runtime.connectionState == ReceiverConnectionState.Streaming,
            isWaveAnimated = runtime.receiverEnabled,
            primaryButtonText = primaryButtonText,
            secondaryButtonText = secondaryButtonText
        )
    }

    private fun buildHeadline(runtime: ReceiverRuntimeState): String {
        return when (runtime.connectionState) {
            ReceiverConnectionState.Idle -> "Receiver offline"
            ReceiverConnectionState.Ready -> "Receiver ready"
            ReceiverConnectionState.Connected -> "Control session connected"
            ReceiverConnectionState.Streaming -> "Streaming audio"
            ReceiverConnectionState.Recovering -> "Recovering receiver session"
            ReceiverConnectionState.Error -> "Receiver error"
        }
    }

    private fun buildSubheadline(runtime: ReceiverRuntimeState): String {
        return when (runtime.connectionState) {
            ReceiverConnectionState.Idle -> "Start the receiver service to advertise this endpoint"
            ReceiverConnectionState.Ready -> "Waiting for a Windows source to connect"
            ReceiverConnectionState.Connected -> "Session established and ready for audio"
            ReceiverConnectionState.Streaming -> "Audio frames are being received and rendered"
            ReceiverConnectionState.Recovering -> runtime.lastError ?: "Receiver is trying to recover"
            ReceiverConnectionState.Error -> runtime.lastError ?: "Receiver encountered an error"
        }
    }

    private fun buildAudioFormat(runtime: ReceiverRuntimeState): String {
        val codec = runtime.codec ?: "PCM"
        val rate = runtime.sampleRate ?: 48000
        val channels = when (runtime.channels) {
            1 -> "Mono"
            2 -> "Stereo"
            else -> "${runtime.channels ?: 2} ch"
        }

        return "$codec · ${rate / 1000} kHz · $channels"
    }

    private fun buildUptime(runtime: ReceiverRuntimeState): String {
        val start = runtime.sessionStartedUtcMs ?: return "00:00"
        val elapsedSec = ((System.currentTimeMillis() - start) / 1000L).coerceAtLeast(0)
        val min = elapsedSec / 60
        val sec = elapsedSec % 60
        return "%02d:%02d".format(min, sec)
    }
}