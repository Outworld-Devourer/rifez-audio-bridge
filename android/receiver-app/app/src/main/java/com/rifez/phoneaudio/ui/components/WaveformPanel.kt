package com.rifez.phoneaudio.ui.components

import androidx.compose.animation.core.EaseInOutSine
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.unit.dp
import com.rifez.phoneaudio.ui.theme.AccentBlue
import com.rifez.phoneaudio.ui.theme.AccentBlueSoft
import com.rifez.phoneaudio.ui.theme.SuccessGreen
import com.rifez.phoneaudio.ui.theme.SurfaceDark
import com.rifez.phoneaudio.ui.theme.SurfaceElevated
import com.rifez.phoneaudio.ui.theme.SurfaceStroke
import com.rifez.phoneaudio.ui.theme.TextPrimary
import kotlin.math.sin

@Composable
fun WaveformPanel(
    active: Boolean,
    emphasized: Boolean,
    signalPresent: Boolean,
    audioLevel: Float,
    modifier: Modifier = Modifier
) {
    val infiniteTransition = rememberInfiniteTransition(label = "waveform_transition")

    val phase by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = (Math.PI * 2).toFloat(),
        animationSpec = infiniteRepeatable(
            animation = tween(
                durationMillis = if (emphasized) 900 else 1600,
                easing = LinearEasing
            )
        ),
        label = "wave_phase"
    )

    val glowAlpha by infiniteTransition.animateFloat(
        initialValue = 0.08f,
        targetValue = if (active) 0.18f else 0.10f,
        animationSpec = infiniteRepeatable(
            animation = tween(1800, easing = EaseInOutSine),
            repeatMode = RepeatMode.Reverse
        ),
        label = "wave_glow"
    )

    Column(
        modifier = modifier
            .height(180.dp)
            .background(
                brush = Brush.verticalGradient(
                    listOf(SurfaceElevated, SurfaceDark)
                ),
                shape = RoundedCornerShape(28.dp)
            )
            .border(
                width = 1.dp,
                color = MaterialTheme.colorScheme.primary.copy(alpha = glowAlpha),
                shape = RoundedCornerShape(28.dp)
            )
            .padding(18.dp)
    ) {
        Text(
            text = when {
                emphasized && signalPresent -> "Live audio signal"
                emphasized -> "Streaming silence / low output"
                active -> "Receiver standby"
                else -> "Receiver offline"
            },
            style = MaterialTheme.typography.titleMedium,
            color = TextPrimary
        )

        Spacer(modifier = Modifier.height(14.dp))

        Canvas(
            modifier = Modifier
                .fillMaxWidth()
                .fillMaxSize()
                .padding(vertical = 6.dp)
        ) {
            val width = size.width
            val height = size.height
            val centerY = height / 2f

            val normalizedLevel = audioLevel.coerceIn(0f, 1f)

            val baseAmplitude = when {
                emphasized && signalPresent -> height * (0.05f + 0.22f * normalizedLevel)
                emphasized -> height * 0.015f
                active -> height * 0.035f
                else -> height * 0.008f
            }

            val secondaryAmplitude = when {
                emphasized && signalPresent -> height * (0.02f + 0.10f * normalizedLevel)
                emphasized -> height * 0.008f
                active -> height * 0.02f
                else -> height * 0.004f
            }

            val points = mutableListOf<Offset>()
            val step = 6f

            var x = 0f
            while (x <= width) {
                val normalized = x / width
                val y = centerY +
                        sin((normalized * 10f) + phase).toFloat() * baseAmplitude +
                        sin((normalized * 22f) + phase * 1.4f).toFloat() * secondaryAmplitude
                points.add(Offset(x, y))
                x += step
            }

            drawLine(
                color = SurfaceStroke.copy(alpha = 0.6f),
                start = Offset(0f, centerY),
                end = Offset(width, centerY),
                strokeWidth = 2f
            )

            for (i in 0 until points.size - 1) {
                drawLine(
                    brush = Brush.horizontalGradient(
                        colors = listOf(
                            AccentBlue.copy(alpha = 0.55f),
                            AccentBlueSoft.copy(alpha = 0.95f),
                            SuccessGreen.copy(alpha = if (emphasized) 0.85f else 0.35f)
                        )
                    ),
                    start = points[i],
                    end = points[i + 1],
                    strokeWidth = if (emphasized) 6f else 4f,
                    cap = StrokeCap.Round
                )
            }

            if (active) {
                drawCircle(
                    color = AccentBlueSoft.copy(alpha = 0.10f),
                    radius = 42f,
                    center = Offset(width * 0.88f, centerY)
                )
            }
        }
    }
}