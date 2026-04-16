package com.rifez.phoneaudio.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val DarkColorScheme = darkColorScheme(
    primary = AccentBlue,
    onPrimary = Color(0xFF041118),
    secondary = AccentBlueSoft,
    onSecondary = Color(0xFF041118),
    tertiary = SuccessGreen,
    background = BackgroundDark,
    onBackground = TextPrimary,
    surface = SurfaceDark,
    onSurface = TextPrimary,
    surfaceVariant = SurfaceElevated,
    onSurfaceVariant = TextSecondary,
    outline = SurfaceStroke,
    error = ErrorRed,
    onError = Color.White
)

@Composable
fun PhoneAudioTheme(
    darkTheme: Boolean = true,
    content: @Composable () -> Unit
) {
    val colorScheme = when {
        darkTheme || isSystemInDarkTheme() -> DarkColorScheme
        else -> DarkColorScheme
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = AppTypography,
        shapes = Shapes(
            extraSmall = androidx.compose.foundation.shape.RoundedCornerShape(12),
            small = androidx.compose.foundation.shape.RoundedCornerShape(16),
            medium = androidx.compose.foundation.shape.RoundedCornerShape(22),
            large = androidx.compose.foundation.shape.RoundedCornerShape(28),
            extraLarge = androidx.compose.foundation.shape.RoundedCornerShape(32)
        ),
        content = content
    )
}