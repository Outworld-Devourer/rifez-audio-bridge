package com.rifez.phoneaudio.stream

import android.util.Log
import com.rifez.phoneaudio.protocol.FlatBufferControlProtocol
import com.rifez.phoneaudio.protocol.FlatBufferFrameIO
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.cancel
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.net.ServerSocket
import java.net.Socket
import java.net.SocketException

class FlatBufferHelloServer {

    private var serverSocket: ServerSocket? = null
    private var serverJob: Job? = null
    private var scope: CoroutineScope? = null

    @Volatile
    private var currentControlSocket: Socket? = null

    @Volatile
    private var manualDisconnectRequested = false

    fun start(
        port: Int,
        onClientHello: ((String) -> Unit)? = null,
        onStartStream: ((String?) -> Unit)? = null,
        onStreamConfigAccepted: ((Int, Int, String) -> Unit)? = null,
        onControlDisconnected: ((Boolean, String?) -> Unit)? = null
    ) {
        stop()

        manualDisconnectRequested = false
        scope = CoroutineScope(Dispatchers.IO)
        serverJob = scope?.launch {
            try {
                serverSocket = ServerSocket(port)
                Log.d(TAG, "FlatBuffer control server listening on port $port")

                while (isActive) {
                    val socket = serverSocket?.accept() ?: break

                    try {
                        currentControlSocket?.close()
                    } catch (_: Exception) {
                    }

                    currentControlSocket = socket
                    manualDisconnectRequested = false

                    Log.d(
                        TAG,
                        "FlatBuffer client connected from ${socket.inetAddress?.hostAddress}:${socket.port}"
                    )

                    var disconnectReason: String? = null

                    try {
                        val input = socket.getInputStream()
                        val output = socket.getOutputStream()

                        while (true) {
                            val requestFrame = try {
                                FlatBufferFrameIO.readFrame(input)
                            } catch (e: Exception) {
                                disconnectReason = e.message ?: "Control socket read failed"
                                null
                            } ?: break

                            when (val message = FlatBufferControlProtocol.parseIncoming(requestFrame)) {
                                is FlatBufferControlProtocol.IncomingMessage.Hello -> {
                                    val clientName = message.clientName.ifBlank { "Unknown PC" }
                                    onClientHello?.invoke(clientName)

                                    Log.d(TAG, "FlatBuffer HELLO received from $clientName")

                                    val receiverName = "Receiver"
                                    val responseFrame =
                                        FlatBufferControlProtocol.buildHelloResponse(receiverName)
                                    FlatBufferFrameIO.writeFrame(output, responseFrame)
                                }

                                FlatBufferControlProtocol.IncomingMessage.Ping -> {
                                    Log.d(TAG, "FlatBuffer PING received")
                                    val responseFrame =
                                        FlatBufferControlProtocol.buildPongResponse()
                                    FlatBufferFrameIO.writeFrame(output, responseFrame)
                                }

                                FlatBufferControlProtocol.IncomingMessage.StatusRequestMsg -> {
                                    Log.d(TAG, "FlatBuffer STATUS requested")
                                    val responseFrame = FlatBufferControlProtocol.buildStatusResponse(
                                        stateName = "READY",
                                        deviceName = "RECEIVER",
                                        sourceName = "NONE"
                                    )
                                    FlatBufferFrameIO.writeFrame(output, responseFrame)
                                }

                                FlatBufferControlProtocol.IncomingMessage.StartStream -> {
                                    onStartStream?.invoke(null)

                                    Log.d(TAG, "FlatBuffer START_STREAM received")
                                    val responseFrame =
                                        FlatBufferControlProtocol.buildStartStreamResponse()
                                    FlatBufferFrameIO.writeFrame(output, responseFrame)
                                }

                                FlatBufferControlProtocol.IncomingMessage.Disconnect -> {
                                    manualDisconnectRequested = true
                                    disconnectReason = "Disconnect requested by client"

                                    Log.d(TAG, "FlatBuffer DISCONNECT received")
                                    val responseFrame =
                                        FlatBufferControlProtocol.buildDisconnectResponse()
                                    FlatBufferFrameIO.writeFrame(output, responseFrame)
                                    break
                                }

                                is FlatBufferControlProtocol.IncomingMessage.StreamConfig -> {
                                    val accepted =
                                        message.sampleRate == 48000L &&
                                                message.channels.toInt() == 2 &&
                                                message.frameSamples > 0L

                                    val reason = if (accepted) "OK" else "Unsupported config"

                                    if (accepted) {
                                        onStreamConfigAccepted?.invoke(
                                            message.sampleRate.toInt(),
                                            message.channels.toInt(),
                                            "PCM"
                                        )
                                    }

                                    Log.d(
                                        TAG,
                                        "FlatBuffer STREAM_CONFIG received: " +
                                                "rate=${message.sampleRate}, ch=${message.channels}, " +
                                                "fmt=${message.sampleFormat}, codec=${message.codec}, frame=${message.frameSamples}"
                                    )

                                    val responseFrame =
                                        FlatBufferControlProtocol.buildStreamConfigResponse(
                                            accepted = accepted,
                                            sampleRate = if (accepted) message.sampleRate else 48000L,
                                            channels = if (accepted) message.channels else 2,
                                            sampleFormat = message.sampleFormat,
                                            codec = message.codec,
                                            frameSamples = if (accepted) message.frameSamples else 480L,
                                            reason = reason
                                        )

                                    FlatBufferFrameIO.writeFrame(output, responseFrame)
                                }
                            }
                        }
                    } finally {
                        try {
                            socket.close()
                        } catch (_: Exception) {
                        }

                        if (currentControlSocket === socket) {
                            currentControlSocket = null
                        }

                        onControlDisconnected?.invoke(manualDisconnectRequested, disconnectReason)
                        manualDisconnectRequested = false
                    }
                }
            } catch (e: SocketException) {
                Log.d(TAG, "FlatBuffer control server stopped: ${e.message}")
            } catch (e: Exception) {
                Log.e(TAG, "FlatBuffer control server failed", e)
                onControlDisconnected?.invoke(false, e.message ?: "Control server failure")
            }
        }
    }

    fun disconnectCurrentSession() {
        manualDisconnectRequested = true

        try {
            currentControlSocket?.close()
        } catch (_: Exception) {
        } finally {
            currentControlSocket = null
        }
    }

    fun stop() {
        manualDisconnectRequested = true

        try {
            currentControlSocket?.close()
        } catch (_: Exception) {
        } finally {
            currentControlSocket = null
        }

        try {
            serverSocket?.close()
        } catch (_: Exception) {
        } finally {
            serverSocket = null
        }

        serverJob?.cancel()
        serverJob = null

        scope?.cancel()
        scope = null
    }

    companion object {
        private const val TAG = "FlatBufferHelloServer"
    }
}