package com.rifez.phoneaudio.ui.components

import androidx.compose.animation.core.EaseInOutSine
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.rifez.phoneaudio.ui.screen.ConnectionVisualState
import com.rifez.phoneaudio.ui.theme.AccentBlue
import com.rifez.phoneaudio.ui.theme.AccentBlueSoft
import com.rifez.phoneaudio.ui.theme.ErrorRed
import com.rifez.phoneaudio.ui.theme.SuccessGreen
import com.rifez.phoneaudio.ui.theme.SurfaceDark
import com.rifez.phoneaudio.ui.theme.SurfaceElevated
import com.rifez.phoneaudio.ui.theme.TextMuted
import com.rifez.phoneaudio.ui.theme.TextPrimary
import com.rifez.phoneaudio.ui.theme.TextSecondary
import com.rifez.phoneaudio.ui.theme.WarningAmber

@Composable
fun ConnectionStatusCard(
    visualState: ConnectionVisualState,
    headline: String,
    subheadline: String,
    endpointInfo: String,
    sourcePcName: String?,
    modifier: Modifier = Modifier
) {
    val accent = when (visualState) {
        ConnectionVisualState.Ready -> AccentBlue
        ConnectionVisualState.Connected -> AccentBlueSoft
        ConnectionVisualState.Streaming -> SuccessGreen
        ConnectionVisualState.Recovering -> WarningAmber
        ConnectionVisualState.Error -> ErrorRed
    }

    val infiniteTransition = rememberInfiniteTransition(label = "card_glow")
    val animatedAlpha by infiniteTransition.animateFloat(
        initialValue = 0.10f,
        targetValue = 0.22f,
        animationSpec = infiniteRepeatable(
            animation = tween(2000, easing = EaseInOutSine),
            repeatMode = RepeatMode.Reverse
        ),
        label = "card_glow_alpha"
    )

    val backgroundBrush = Brush.verticalGradient(
        colors = listOf(
            SurfaceElevated,
            SurfaceDark
        )
    )

    Column(
        modifier = modifier
            .background(backgroundBrush, shape = RoundedCornerShape(28.dp))
            .border(
                width = 1.dp,
                color = accent.copy(alpha = if (visualState == ConnectionVisualState.Streaming) animatedAlpha else 0.22f),
                shape = RoundedCornerShape(28.dp)
            )
            .padding(22.dp)
    ) {
        Text(
            text = stateTitle(visualState),
            style = MaterialTheme.typography.headlineMedium,
            color = TextPrimary,
            fontWeight = FontWeight.Bold
        )

        Spacer(modifier = Modifier.height(10.dp))

        Text(
            text = headline,
            style = MaterialTheme.typography.titleMedium,
            color = TextPrimary
        )

        Spacer(modifier = Modifier.height(8.dp))

        Text(
            text = subheadline,
            style = MaterialTheme.typography.bodyLarge,
            color = TextSecondary
        )

        Spacer(modifier = Modifier.height(16.dp))

        StatusPill(
            text = endpointInfo,
            accent = accent
        )

        if (!sourcePcName.isNullOrBlank()) {
            Spacer(modifier = Modifier.height(16.dp))
            Text(
                text = "Connected source",
                style = MaterialTheme.typography.labelMedium,
                color = TextMuted
            )
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = sourcePcName,
                style = MaterialTheme.typography.titleMedium,
                color = TextPrimary
            )
        }
    }
}

@Composable
private fun StatusPill(
    text: String,
    accent: androidx.compose.ui.graphics.Color
) {
    Box(
        modifier = Modifier
            .background(accent.copy(alpha = 0.12f), shape = RoundedCornerShape(999.dp))
            .border(
                width = 1.dp,
                color = accent.copy(alpha = 0.28f),
                shape = RoundedCornerShape(999.dp)
            )
            .padding(horizontal = 14.dp, vertical = 10.dp)
    ) {
        Text(
            text = text,
            style = MaterialTheme.typography.bodyMedium,
            color = TextPrimary
        )
    }
}

private fun stateTitle(state: ConnectionVisualState): String {
    return when (state) {
        ConnectionVisualState.Ready -> "Ready"
        ConnectionVisualState.Connected -> "Connected"
        ConnectionVisualState.Streaming -> "Streaming"
        ConnectionVisualState.Recovering -> "Recovering"
        ConnectionVisualState.Error -> "Error"
    }
}