package com.rifez.phoneaudio.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.rifez.phoneaudio.ui.screen.ConnectionVisualState
import com.rifez.phoneaudio.ui.theme.AccentBlue
import com.rifez.phoneaudio.ui.theme.AccentBlueSoft
import com.rifez.phoneaudio.ui.theme.ErrorRed
import com.rifez.phoneaudio.ui.theme.SuccessGreen
import com.rifez.phoneaudio.ui.theme.TextPrimary
import com.rifez.phoneaudio.ui.theme.TextSecondary
import com.rifez.phoneaudio.ui.theme.WarningAmber

@Composable
fun DeviceInfoRow(
    deviceName: String,
    state: ConnectionVisualState,
    modifier: Modifier = Modifier
) {
    val accent = when (state) {
        ConnectionVisualState.Ready -> AccentBlue
        ConnectionVisualState.Connected -> AccentBlueSoft
        ConnectionVisualState.Streaming -> SuccessGreen
        ConnectionVisualState.Recovering -> WarningAmber
        ConnectionVisualState.Error -> ErrorRed
    }

    Row(
        modifier = modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column {
            Text(
                text = "RifeZ Audio Bridge Receiver",
                style = MaterialTheme.typography.titleLarge,
                color = TextPrimary
            )
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = deviceName,
                style = MaterialTheme.typography.bodyMedium,
                color = TextSecondary
            )
        }

        StateDot(accent = accent)
    }
}

@Composable
private fun StateDot(accent: Color) {
    androidx.compose.foundation.layout.Box(
        modifier = Modifier
            .size(16.dp)
            .clip(RoundedCornerShape(999.dp))
            .background(accent)
            .border(
                width = 2.dp,
                color = accent.copy(alpha = 0.22f),
                shape = RoundedCornerShape(999.dp)
            )
    )
}