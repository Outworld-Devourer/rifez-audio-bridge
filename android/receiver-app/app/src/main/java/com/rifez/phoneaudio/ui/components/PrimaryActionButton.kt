package com.rifez.phoneaudio.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.unit.dp
import com.rifez.phoneaudio.ui.theme.AccentBlue
import com.rifez.phoneaudio.ui.theme.AccentBlueSoft
import com.rifez.phoneaudio.ui.theme.BackgroundDark
import com.rifez.phoneaudio.ui.theme.SurfaceDark
import com.rifez.phoneaudio.ui.theme.SurfaceElevated
import com.rifez.phoneaudio.ui.theme.SurfaceStroke
import com.rifez.phoneaudio.ui.theme.TextPrimary

@Composable
fun PrimaryActionButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    isPrimary: Boolean = true
) {
    val shape = RoundedCornerShape(22.dp)

    val background = if (isPrimary) {
        Brush.horizontalGradient(
            colors = listOf(
                AccentBlue,
                AccentBlueSoft
            )
        )
    } else {
        Brush.horizontalGradient(
            colors = listOf(
                SurfaceElevated,
                SurfaceDark
            )
        )
    }

    val borderColor = if (isPrimary) {
        AccentBlue.copy(alpha = 0.35f)
    } else {
        SurfaceStroke
    }

    val textColor = if (isPrimary) {
        BackgroundDark
    } else {
        TextPrimary
    }

    Box(
        modifier = modifier
            .clip(shape)
            .background(background)
            .border(1.dp, borderColor, shape)
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = onClick
            ),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = text,
            style = MaterialTheme.typography.labelLarge,
            color = textColor
        )
    }
}