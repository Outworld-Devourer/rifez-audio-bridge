package com.rifez.phoneaudio

import android.Manifest
import android.app.AlertDialog
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.widget.EditText
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.viewModels
import androidx.compose.runtime.getValue
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.rifez.phoneaudio.service.ReceiverConnectionState
import com.rifez.phoneaudio.service.ReceiverPreferences
import com.rifez.phoneaudio.service.ReceiverServiceController
import com.rifez.phoneaudio.service.ReceiverStateRepository
import com.rifez.phoneaudio.ui.screen.ReceiverScreen
import com.rifez.phoneaudio.ui.screen.ReceiverViewModel
import com.rifez.phoneaudio.ui.theme.PhoneAudioTheme

class MainActivity : ComponentActivity() {

    private val viewModel: ReceiverViewModel by viewModels()
    private lateinit var receiverPreferences: ReceiverPreferences

    private val notificationPermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
            if (granted || Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
                startReceiverService()
            }
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        receiverPreferences = ReceiverPreferences(this)

        // Load persisted receiver name before UI/service startup.
        val persistedDeviceName = receiverPreferences.getDeviceName()
        ReceiverStateRepository.renameDevice(persistedDeviceName)

        setContent {
            PhoneAudioTheme {
                val uiState by viewModel.uiState.collectAsStateWithLifecycle()

                ReceiverScreen(
                    state = uiState,
                    onPrimaryAction = {
                        when (ReceiverStateRepository.runtimeState.value.connectionState) {
                            ReceiverConnectionState.Idle -> {
                                ensureNotificationPermissionThenStart()
                            }

                            ReceiverConnectionState.Ready -> {
                                // Receiver should already be auto-started.
                                // No action needed in normal use.
                            }

                            ReceiverConnectionState.Connected,
                            ReceiverConnectionState.Streaming,
                            ReceiverConnectionState.Recovering,
                            ReceiverConnectionState.Error -> {
                                ReceiverServiceController.disconnectSession(this)
                            }
                        }
                    },
                    onSecondaryAction = {
                        when (ReceiverStateRepository.runtimeState.value.connectionState) {
                            ReceiverConnectionState.Idle -> {
                                // no-op
                            }

                            ReceiverConnectionState.Ready -> {
                                showRenameDeviceDialog()
                            }

                            ReceiverConnectionState.Connected,
                            ReceiverConnectionState.Streaming,
                            ReceiverConnectionState.Recovering,
                            ReceiverConnectionState.Error -> {
                                ReceiverServiceController.disconnectSession(this)
                            }
                        }
                    }
                )
            }
        }

        // Appliance-style behavior: auto-start receiver when app opens.
        ensureNotificationPermissionThenStart()
    }

    private fun ensureNotificationPermissionThenStart() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
            startReceiverService()
            return
        }

        val granted = ContextCompat.checkSelfPermission(
            this,
            Manifest.permission.POST_NOTIFICATIONS
        ) == PackageManager.PERMISSION_GRANTED

        if (granted) {
            startReceiverService()
        } else {
            notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
        }
    }

    private fun startReceiverService() {
        ReceiverServiceController.start(this)
        viewModel.onServiceStartRequested()
    }

    private fun showRenameDeviceDialog() {
        val currentName = ReceiverStateRepository.runtimeState.value.deviceName

        val input = EditText(this).apply {
            setText(currentName)
            setSelection(text.length)
            hint = "Receiver name"
        }

        AlertDialog.Builder(this)
            .setTitle("Rename Device")
            .setMessage("Choose the advertised receiver name shown to Windows clients.")
            .setView(input)
            .setPositiveButton("Save") { _, _ ->
                val newName = input.text?.toString()?.trim().orEmpty()
                if (newName.isNotBlank()) {
                    receiverPreferences.setDeviceName(newName)
                    viewModel.renameDevice(newName)
                }
            }
            .setNegativeButton("Cancel", null)
            .show()
    }
}