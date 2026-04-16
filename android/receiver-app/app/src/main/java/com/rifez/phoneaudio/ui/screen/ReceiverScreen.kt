package com.rifez.phoneaudio.ui.screen

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.EaseInOutSine
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.rifez.phoneaudio.ui.components.ConnectionStatusCard
import com.rifez.phoneaudio.ui.components.DeviceInfoRow
import com.rifez.phoneaudio.ui.components.PrimaryActionButton
import com.rifez.phoneaudio.ui.components.WaveformPanel
import com.rifez.phoneaudio.ui.theme.PhoneAudioTheme
import com.rifez.phoneaudio.ui.theme.ScreenBackgroundBottom
import com.rifez.phoneaudio.ui.theme.ScreenBackgroundTop
import com.rifez.phoneaudio.ui.theme.SurfaceDark
import com.rifez.phoneaudio.ui.theme.TextPrimary
import com.rifez.phoneaudio.ui.theme.TextSecondary

enum class ConnectionVisualState {
    Ready,
    Connected,
    Streaming,
    Recovering,
    Error
}

data class ConnectionUiState(
    val visualState: ConnectionVisualState,
    val deviceName: String,
    val sourcePcName: String?,
    val headline: String,
    val subheadline: String,
    val endpointInfo: String,
    val audioInfo: String,
    val sessionInfo: String,
    val metricsInfo: String,
    val signalLevel: Float,
    val signalPresent: Boolean,
    val isStreaming: Boolean,
    val isWaveAnimated: Boolean,
    val primaryButtonText: String,
    val secondaryButtonText: String
)

@Composable
fun ReceiverScreen(
    state: ConnectionUiState,
    onPrimaryAction: () -> Unit,
    onSecondaryAction: () -> Unit
) {
    val transition = rememberInfiniteTransition(label = "background_glow")
    val glowShift by transition.animateFloat(
        initialValue = 0.15f,
        targetValue = 0.45f,
        animationSpec = infiniteRepeatable(
            animation = tween(durationMillis = 2600, easing = EaseInOutSine),
            repeatMode = RepeatMode.Reverse
        ),
        label = "glow_shift"
    )

    val backgroundBrush = Brush.verticalGradient(
        colorStops = arrayOf(
            0.0f to ScreenBackgroundTop,
            glowShift to MaterialTheme.colorScheme.primary.copy(alpha = 0.06f),
            1.0f to ScreenBackgroundBottom
        )
    )

    Surface(
        modifier = Modifier.fillMaxSize(),
        color = MaterialTheme.colorScheme.background
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(backgroundBrush)
                .padding(horizontal = 20.dp)
                .padding(top = 28.dp, bottom = 18.dp)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState()),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Spacer(modifier = Modifier.height(12.dp))

                DeviceInfoRow(
                    deviceName = state.deviceName,
                    state = state.visualState
                )

                Spacer(modifier = Modifier.height(24.dp))

                ConnectionStatusCard(
                    visualState = state.visualState,
                    headline = state.headline,
                    subheadline = state.subheadline,
                    endpointInfo = state.endpointInfo,
                    sourcePcName = state.sourcePcName,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(20.dp))

                WaveformPanel(
                    active = state.isWaveAnimated,
                    emphasized = state.isStreaming,
                    signalPresent = state.signalPresent,
                    audioLevel = state.signalLevel,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(20.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    DashboardInfoCard(
                        title = "Audio",
                        body = state.audioInfo,
                        modifier = Modifier.weight(1f)
                    )
                    DashboardInfoCard(
                        title = "Session",
                        body = state.sessionInfo,
                        modifier = Modifier.weight(1f)
                    )
                }

                Spacer(modifier = Modifier.height(14.dp))

                DashboardInfoCard(
                    title = "Receiver Metrics",
                    body = state.metricsInfo,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(24.dp))

                PrimaryActionButton(
                    text = state.primaryButtonText,
                    onClick = onPrimaryAction,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(58.dp)
                )

                AnimatedVisibility(visible = state.secondaryButtonText.isNotBlank()) {
                    Column {
                        Spacer(modifier = Modifier.height(12.dp))
                        PrimaryActionButton(
                            text = state.secondaryButtonText,
                            onClick = onSecondaryAction,
                            isPrimary = false,
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(54.dp)
                        )
                    }
                }

                Spacer(modifier = Modifier.height(24.dp))
            }
        }
    }
}

@Composable
private fun DashboardInfoCard(
    title: String,
    body: String,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .background(SurfaceDark, shape = androidx.compose.foundation.shape.RoundedCornerShape(24.dp))
            .padding(18.dp)
    ) {
        Text(
            text = title,
            style = MaterialTheme.typography.labelLarge,
            color = TextSecondary
        )
        Spacer(modifier = Modifier.height(8.dp))
        Text(
            text = body,
            style = MaterialTheme.typography.bodyLarge,
            color = TextPrimary,
            fontWeight = FontWeight.Medium
        )
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF0A0D12)
@Composable
private fun ReceiverScreenPreview() {
    PhoneAudioTheme {
        ReceiverScreen(
            state = ConnectionUiState(
                visualState = ConnectionVisualState.Streaming,
                deviceName = "Kiril Phone",
                sourcePcName = "RifeZ-PC",
                headline = "Streaming audio",
                subheadline = "Audio frames are being received and rendered",
                endpointInfo = "Endpoint RifeZ-Kiril-Phone · Port 49521",
                audioInfo = "PCM · 48 kHz · Stereo",
                sessionInfo = "Source RifeZ-PC · Uptime 03:42",
                metricsInfo = "Frames 2450 · 187 KB/s · Underruns 0",
                signalLevel = 0.62f,
                true,
                isStreaming = true,
                isWaveAnimated = true,
                primaryButtonText = "Disconnect Session",
                secondaryButtonText = "Clear Session"
            ),
            onPrimaryAction = {},
            onSecondaryAction = {}
        )
    }
}