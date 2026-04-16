package com.rifez.phoneaudio.service

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.IBinder
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.app.ServiceCompat
import com.rifez.phoneaudio.R
import com.rifez.phoneaudio.audio.AudioPcmServer
import com.rifez.phoneaudio.discovery.NsdRegistrar
import com.rifez.phoneaudio.discovery.NsdRegistrationState
import com.rifez.phoneaudio.protocol.ReceiverCommand
import com.rifez.phoneaudio.protocol.ReceiverProtocolParser
import com.rifez.phoneaudio.protocol.ReceiverResponse
import com.rifez.phoneaudio.stream.FlatBufferHelloServer
import com.rifez.phoneaudio.stream.ReceiverSocketState
import com.rifez.phoneaudio.stream.ReceiverTcpServer
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.cancel
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

class ReceiverForegroundService : Service() {

    private var nsdRegistrar: NsdRegistrar? = null
    private var tcpServer: ReceiverTcpServer? = null
    private var flatBufferHelloServer: FlatBufferHelloServer? = null
    private var audioPcmServer: AudioPcmServer? = null

    private var watchdogScope: CoroutineScope? = null
    private var watchdogJob: Job? = null

    private val sessionRecoveryLock = Any()

    @Volatile
    private var recoveryInProgress = false

    @Volatile
    private var manualSessionDisconnectInProgress = false

    override fun onCreate() {
        super.onCreate()
        createNotificationChannel()

        nsdRegistrar = NsdRegistrar(this)
        tcpServer = ReceiverTcpServer()
        flatBufferHelloServer = FlatBufferHelloServer()
        audioPcmServer = AudioPcmServer()
        watchdogScope = CoroutineScope(Dispatchers.Default)

        Log.d(TAG, "Service onCreate")
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ReceiverServiceActions.ACTION_START -> startReceiverService()
            ReceiverServiceActions.ACTION_STOP -> stopReceiverService()
            ReceiverServiceActions.ACTION_DISCONNECT_SESSION -> disconnectCurrentSession()
            else -> startReceiverService()
        }
        return START_STICKY
    }

    override fun onDestroy() {
        audioPcmServer?.stop()
        flatBufferHelloServer?.stop()
        tcpServer?.stop()
        nsdRegistrar?.unregister()

        watchdogJob?.cancel()
        watchdogJob = null

        watchdogScope?.cancel()
        watchdogScope = null

        ReceiverStateRepository.setState(
            ReceiverRuntimeState(
                receiverEnabled = false,
                connectionState = ReceiverConnectionState.Idle,
                deviceName = ReceiverStateRepository.runtimeState.value.deviceName
            )
        )

        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun startReceiverService() {
        val notification = buildNotification(
            title = "RifeZ Audio Bridge Receiver",
            text = "Receiver is ready on Wi-Fi"
        )

        ServiceCompat.startForeground(
            this,
            NOTIFICATION_ID,
            notification,
            ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PLAYBACK
        )

        ReceiverStateRepository.setReady()
        startStreamingWatchdog()
        startTcpServer()
        startFlatBufferHelloServer()
        startAudioPcmServer()
        startNsdAdvertisement()
    }

    private fun disconnectCurrentSession() {
        Log.d(TAG, "Manual disconnect requested from UI")
        manualSessionDisconnectInProgress = true
        recycleReceiverSessionToReady(
            reason = "Session disconnected by user",
            expected = true
        )
    }

    private fun recycleReceiverSessionToReady(reason: String, expected: Boolean) {
        synchronized(sessionRecoveryLock) {
            if (recoveryInProgress) {
                Log.d(TAG, "Session recycle already in progress, skipping. reason=$reason")
                return
            }
            recoveryInProgress = true
        }

        try {
            Log.d(TAG, "Recycling receiver session. expected=$expected, reason=$reason")

            try { audioPcmServer?.stop() } catch (_: Exception) {}
            try { flatBufferHelloServer?.stop() } catch (_: Exception) {}
            try { tcpServer?.stop() } catch (_: Exception) {}

            ReceiverStateRepository.disconnect()

            if (!expected) {
                ReceiverStateRepository.setRecovering(
                    pcName = null,
                    message = reason
                )
            }

            ReceiverStateRepository.setReady()

            startTcpServer()
            startFlatBufferHelloServer()
            startAudioPcmServer()
        } finally {
            manualSessionDisconnectInProgress = false
            recoveryInProgress = false
        }
    }

    private fun startStreamingWatchdog() {
        watchdogJob?.cancel()

        watchdogJob = watchdogScope?.launch {
            while (isActive) {
                ReceiverStateRepository.refreshStreamingTimeout()
                delay(300)
            }
        }
    }

    private fun stopReceiverService() {
        audioPcmServer?.stop()
        flatBufferHelloServer?.stop()
        tcpServer?.stop()
        nsdRegistrar?.unregister()

        stopForeground(STOP_FOREGROUND_REMOVE)
        stopSelf()
    }

    private fun startAudioPcmServer() {
        audioPcmServer?.start(
            port = 49523,
            sampleRateProvider = {
                ReceiverStateRepository.runtimeState.value.sampleRate ?: 48000
            },
            channelsProvider = {
                ReceiverStateRepository.runtimeState.value.channels ?: 2
            },
            onAudioSessionStarted = {
                ReceiverStateRepository.setAudioConnection(true)
            },
            onFrameReceived = { payloadBytes, level ->
                ReceiverStateRepository.noteFrameReceived(payloadBytes, level)
            },
            onUnderrun = {
                ReceiverStateRepository.noteUnderrun()
            },
            onAudioSessionEnded = { expected, reason ->
                ReceiverStateRepository.setAudioConnection(false)
                ReceiverStateRepository.notePlaybackIdle()

                if (!expected && !manualSessionDisconnectInProgress) {
                    recycleReceiverSessionToReady(
                        reason = reason ?: "Audio session ended unexpectedly",
                        expected = false
                    )
                }
            }
        )
    }

    private fun startFlatBufferHelloServer() {
        flatBufferHelloServer?.start(
            port = 49522,
            onClientHello = { clientName ->
                ReceiverStateRepository.setConnected(pcName = clientName)
                ReceiverStateRepository.setControlConnection(
                    connected = true,
                    pcName = clientName
                )
            },
            onStartStream = { sourceName ->
                val source =
                    sourceName
                        ?: ReceiverStateRepository.runtimeState.value.sourcePcName
                        ?: "Unknown PC"
                ReceiverStateRepository.setConnected(pcName = source)
            },
            onStreamConfigAccepted = { sampleRate, channels, codec ->
                ReceiverStateRepository.updateAudioFormat(
                    codec = codec,
                    sampleRate = sampleRate,
                    channels = channels
                )
            },
            onControlDisconnected = { expected, reason ->
                ReceiverStateRepository.setControlConnection(false)

                if (!expected && !manualSessionDisconnectInProgress) {
                    recycleReceiverSessionToReady(
                        reason = reason ?: "Control session ended unexpectedly",
                        expected = false
                    )
                }
            }
        )
    }

    private fun startNsdAdvertisement() {
        val runtime = ReceiverStateRepository.runtimeState.value
        val serviceName = buildAdvertisedName(runtime.deviceName)

        nsdRegistrar?.register(
            serviceName = serviceName,
            port = runtime.port ?: NsdRegistrar.DEFAULT_PORT
        ) { nsdState ->
            when (nsdState) {
                is NsdRegistrationState.Registering -> {
                    ReceiverStateRepository.updateNsdState(
                        nsdState = nsdState,
                        advertisedServiceName = nsdState.serviceName
                    )
                }

                is NsdRegistrationState.Registered -> {
                    ReceiverStateRepository.updateState { current ->
                        current.copy(
                            nsdState = nsdState,
                            advertisedServiceName = nsdState.serviceName,
                            connectionState = ReceiverConnectionState.Ready,
                            lastError = null
                        )
                    }

                    val configuredPort =
                        ReceiverStateRepository.runtimeState.value.port ?: NsdRegistrar.DEFAULT_PORT
                    Log.d(TAG, "Receiver advertised as ${nsdState.serviceName}:$configuredPort")
                }

                is NsdRegistrationState.Failed -> {
                    ReceiverStateRepository.updateState { current ->
                        current.copy(
                            nsdState = nsdState,
                            advertisedServiceName = nsdState.serviceName,
                            connectionState = ReceiverConnectionState.Error,
                            lastError = "${nsdState.message} (code=${nsdState.errorCode})"
                        )
                    }

                    Log.e(TAG, "NSD advertising failed: ${nsdState.message}, code=${nsdState.errorCode}")
                }

                is NsdRegistrationState.Idle -> {
                    ReceiverStateRepository.updateNsdState(
                        nsdState = nsdState,
                        advertisedServiceName = null
                    )
                }
            }
        }
    }

    // Keep legacy bootstrap path untouched for now.
    private fun startTcpServer() {
        Log.d(TAG, "tcpServer instance before start = ${tcpServer != null}")

        val runtime = ReceiverStateRepository.runtimeState.value
        val port = runtime.port ?: NsdRegistrar.DEFAULT_PORT

        tcpServer?.start(
            port = port,
            onStateChanged = { socketState ->
                when (socketState) {
                    is ReceiverSocketState.Idle -> Log.d(TAG, "TCP state idle")
                    is ReceiverSocketState.Listening -> Log.d(TAG, "TCP listening on port ${socketState.port}")
                    is ReceiverSocketState.ClientConnected -> {
                        val pcName = socketState.remoteHost
                        ReceiverStateRepository.setConnected(pcName = pcName)
                        Log.d(TAG, "TCP client connected from ${socketState.remoteHost}:${socketState.remotePort}")
                    }
                    is ReceiverSocketState.Failed -> {
                        ReceiverStateRepository.setError("TCP listener failed: ${socketState.message}")
                        Log.e(TAG, "TCP listener failed: ${socketState.message}")
                    }
                }
            },
            onMessageReceived = { message ->
                when (val command = ReceiverProtocolParser.parse(message)) {
                    is ReceiverCommand.Hello -> {
                        val clientName = command.clientName?.takeIf { it.isNotBlank() } ?: "Unknown PC"
                        ReceiverStateRepository.setConnected(pcName = clientName)
                        val receiverName = ReceiverStateRepository.runtimeState.value.deviceName
                        ReceiverResponse.Ok("HELLO", receiverName).asWireLine()
                    }
                    is ReceiverCommand.Ping -> ReceiverResponse.Ok("PONG").asWireLine()
                    is ReceiverCommand.GetStatus -> {
                        val currentRuntime = ReceiverStateRepository.runtimeState.value
                        val stateName = currentRuntime.connectionState.name
                        val deviceName = currentRuntime.deviceName.ifBlank { "UNKNOWN_DEVICE" }
                        val sourceName = currentRuntime.sourcePcName?.ifBlank { "NONE" } ?: "NONE"
                        ReceiverResponse.Ok("STATUS|$stateName|$deviceName|$sourceName").asWireLine()
                    }
                    is ReceiverCommand.StreamStart -> {
                        val source = ReceiverStateRepository.runtimeState.value.sourcePcName ?: "Unknown PC"
                        ReceiverStateRepository.setConnected(pcName = source)
                        ReceiverResponse.Ok("STREAM_START").asWireLine()
                    }
                    is ReceiverCommand.Disconnect -> {
                        ReceiverStateRepository.disconnect()
                        ReceiverResponse.Ok("DISCONNECT").asWireLine()
                    }
                    is ReceiverCommand.Unknown -> {
                        ReceiverResponse.Error("UNKNOWN_COMMAND").asWireLine()
                    }
                }
            }
        )
    }

    private fun buildAdvertisedName(deviceName: String): String {
        return "RifeZ-$deviceName"
            .replace("\\s+".toRegex(), "-")
            .take(48)
    }

    private fun buildNotification(title: String, text: String): Notification {
        val openAppIntent = packageManager.getLaunchIntentForPackage(packageName)?.apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
        }

        val pendingIntent = if (openAppIntent != null) {
            PendingIntent.getActivity(
                this,
                1001,
                openAppIntent,
                PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
            )
        } else {
            null
        }

        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_receiver_notification)
            .setContentTitle(title)
            .setContentText(text)
            .setOngoing(true)
            .setOnlyAlertOnce(true)
            .setContentIntent(pendingIntent)
            .setCategory(NotificationCompat.CATEGORY_SERVICE)
            .setVisibility(NotificationCompat.VISIBILITY_PUBLIC)
            .build()
    }

    private fun createNotificationChannel() {
        val channel = NotificationChannel(
            CHANNEL_ID,
            "Receiver Service",
            NotificationManager.IMPORTANCE_LOW
        ).apply {
            description = "Foreground service for the audio receiver"
            setShowBadge(false)
        }

        val manager = getSystemService(NotificationManager::class.java)
        manager.createNotificationChannel(channel)
    }

    companion object {
        private const val TAG = "ReceiverService"
        const val CHANNEL_ID = "receiver_service_channel"
        const val NOTIFICATION_ID = 2001
    }
}