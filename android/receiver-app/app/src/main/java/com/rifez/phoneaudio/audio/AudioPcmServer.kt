package com.rifez.phoneaudio.audio

import android.util.Log
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.cancel
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.net.ServerSocket
import java.net.Socket
import java.net.SocketException
import java.util.ArrayDeque
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean
import kotlin.math.abs
import kotlin.math.max

class AudioPcmServer {

    private var serverSocket: ServerSocket? = null
    private var serverJob: Job? = null
    private var scope: CoroutineScope? = null
    private var output: AudioTrackOutput? = null

    @Volatile
    private var playbackActive = false

    @Volatile
    private var currentClientSocket: Socket? = null

    @Volatile
    private var manualDisconnectRequested = false

    fun start(
        port: Int,
        sampleRateProvider: () -> Int = { 48000 },
        channelsProvider: () -> Int = { 2 },
        onAudioSessionStarted: (() -> Unit)? = null,
        onFrameReceived: ((Int, Float) -> Unit)? = null,
        onUnderrun: (() -> Unit)? = null,
        onAudioSessionEnded: ((Boolean, String?) -> Unit)? = null
    ) {
        stop()

        manualDisconnectRequested = false
        scope = CoroutineScope(Dispatchers.IO)
        serverJob = scope?.launch {
            try {
                serverSocket = ServerSocket(port)
                Log.d(TAG, "Audio PCM server listening on port $port")

                while (isActive) {
                    val socket = serverSocket?.accept() ?: break

                    try {
                        currentClientSocket?.close()
                    } catch (_: Exception) {
                    }

                    try {
                        output?.stop()
                    } catch (_: Exception) {
                    } finally {
                        output = null
                    }

                    currentClientSocket = socket
                    playbackActive = true
                    manualDisconnectRequested = false
                    onAudioSessionStarted?.invoke()

                    Log.d(
                        TAG,
                        "Audio client connected from ${socket.inetAddress?.hostAddress}:${socket.port}"
                    )

                    val pcmQueue = ArrayDeque<ByteArray>()
                    val queueLock = Object()
                    val prebufferTargetFrames = 1
                    val playbackStarted = AtomicBoolean(false)
                    val inputEnded = AtomicBoolean(false)
                    val executor = Executors.newSingleThreadExecutor()
                    var endReason: String? = null

                    try {
                        val input = socket.getInputStream()
                        val outputTrack = AudioTrackOutput(
                            sampleRate = sampleRateProvider(),
                            channelCount = channelsProvider()
                        )
                        output = outputTrack
                        outputTrack.ensureCreated()

                        val playbackFuture = executor.submit {
                            try {
                                var emptySinceMs = -1L

                                while (playbackActive) {
                                    val nextFrame: ByteArray? = synchronized(queueLock) {
                                        if (!playbackStarted.get()) {
                                            if (pcmQueue.size >= prebufferTargetFrames) {
                                                Log.d(
                                                    TAG,
                                                    "Prebuffer reached ${pcmQueue.size} frames, starting playback"
                                                )
                                                playbackStarted.set(true)
                                                outputTrack.startIfNeeded()
                                                if (pcmQueue.isNotEmpty()) pcmQueue.removeFirst() else null
                                            } else {
                                                null
                                            }
                                        } else {
                                            if (pcmQueue.isNotEmpty()) pcmQueue.removeFirst() else null
                                        }
                                    }

                                    if (nextFrame != null) {
                                        emptySinceMs = -1L
                                        val written = outputTrack.writePcm16(nextFrame)
                                        if (written <= 0) {
                                            onUnderrun?.invoke()
                                        }
                                    } else {
                                        if (inputEnded.get()) {
                                            if (emptySinceMs < 0L) {
                                                emptySinceMs = System.currentTimeMillis()
                                            }

                                            val drainedForMs = System.currentTimeMillis() - emptySinceMs
                                            if (drainedForMs >= DRAIN_TAIL_MS) {
                                                Log.d(TAG, "PCM queue drained, ending playback")
                                                break
                                            }
                                        }

                                        Thread.sleep(2)
                                    }
                                }
                            } catch (_: InterruptedException) {
                            } catch (e: Exception) {
                                Log.w(TAG, "Playback worker ended with exception: ${e.message}")
                            }
                        }

                        while (true) {
                            if (!playbackActive) {
                                endReason = endReason ?: "Playback stopped"
                                break
                            }

                            val frame = try {
                                AudioFrameIO.readFrame(input)
                            } catch (e: Exception) {
                                endReason = e.message ?: "Audio input ended"
                                Log.d(TAG, "Audio input ended: ${e.message}")
                                null
                            } ?: break

                            if (frame.frameType != AudioFrameIO.FRAME_TYPE_PCM16) {
                                Log.w(TAG, "Unknown audio frame type: ${frame.frameType}")
                                continue
                            }

                            val level = estimateNormalizedAudioLevel(frame.payload)
                            onFrameReceived?.invoke(frame.payload.size, level)

                            synchronized(queueLock) {
                                if (pcmQueue.size < MAX_QUEUE_FRAMES) {
                                    pcmQueue.addLast(frame.payload)

                                    if (pcmQueue.size % 8 == 0) {
                                        Log.d(TAG, "PCM queue depth=${pcmQueue.size}")
                                    }
                                } else {
                                    pcmQueue.removeFirst()
                                    pcmQueue.addLast(frame.payload)
                                    onUnderrun?.invoke()
                                    Log.w(TAG, "PCM queue full, dropping oldest frame")
                                }
                            }
                        }

                        inputEnded.set(true)

                        try {
                            playbackFuture.get()
                        } catch (_: Exception) {
                        }

                        playbackActive = false
                    } finally {
                        playbackActive = false

                        synchronized(queueLock) {
                            pcmQueue.clear()
                        }

                        try {
                            executor.shutdownNow()
                        } catch (_: Exception) {
                        }

                        try {
                            socket.close()
                        } catch (_: Exception) {
                        }

                        if (currentClientSocket === socket) {
                            currentClientSocket = null
                        }

                        try {
                            output?.stop()
                        } catch (_: Exception) {
                        } finally {
                            output = null
                        }

                        onAudioSessionEnded?.invoke(manualDisconnectRequested, endReason)
                        manualDisconnectRequested = false
                    }
                }
            } catch (e: SocketException) {
                Log.d(TAG, "Audio PCM server stopped: ${e.message}")
            } catch (e: Exception) {
                Log.e(TAG, "Audio PCM server failed", e)
                onAudioSessionEnded?.invoke(false, e.message ?: "Audio server failure")
            }
        }
    }

    fun stop() {
        manualDisconnectRequested = true
        playbackActive = false

        try {
            currentClientSocket?.close()
        } catch (_: Exception) {
        } finally {
            currentClientSocket = null
        }

        try {
            output?.stop()
        } catch (_: Exception) {
        } finally {
            output = null
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

    fun disconnectCurrentSession() {
        manualDisconnectRequested = true
        playbackActive = false

        try {
            currentClientSocket?.close()
        } catch (_: Exception) {
        } finally {
            currentClientSocket = null
        }

        try {
            output?.stop()
        } catch (_: Exception) {
        } finally {
            output = null
        }

        Log.d(TAG, "Current audio session disconnected")
    }

    private fun estimateNormalizedAudioLevel(payload: ByteArray): Float {
        if (payload.size < 2) return 0f

        var peak = 0
        var i = 0
        while (i + 1 < payload.size) {
            val lo = payload[i].toInt() and 0xFF
            val hi = payload[i + 1].toInt()
            val sample = (hi shl 8) or lo
            peak = max(peak, abs(sample.toShort().toInt()))
            i += 2
        }

        return (peak / 32767f).coerceIn(0f, 1f)
    }

    companion object {
        private const val TAG = "AudioPcmServer"
        private const val MAX_QUEUE_FRAMES = 160
        private const val DRAIN_TAIL_MS = 20L
    }
}