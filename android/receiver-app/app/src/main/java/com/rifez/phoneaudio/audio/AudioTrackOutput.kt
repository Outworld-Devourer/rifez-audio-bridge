package com.rifez.phoneaudio.audio

import android.media.AudioAttributes
import android.media.AudioFormat
import android.media.AudioManager
import android.media.AudioTrack
import android.util.Log

class AudioTrackOutput(
    private val sampleRate: Int = 48000,
    private val channelCount: Int = 2
) {
    private var audioTrack: AudioTrack? = null
    private var started = false

    fun ensureCreated() {
        if (audioTrack != null) return

        val channelMask = when (channelCount) {
            1 -> AudioFormat.CHANNEL_OUT_MONO
            else -> AudioFormat.CHANNEL_OUT_STEREO
        }

        val minBuffer = AudioTrack.getMinBufferSize(
            sampleRate,
            channelMask,
            AudioFormat.ENCODING_PCM_16BIT
        )

        val bytesPerSecond = sampleRate * channelCount * 2
        val safeMinBuffer = minBuffer.coerceAtLeast(4096)
        val bufferSize = safeMinBuffer * 2

        audioTrack = AudioTrack(
            AudioAttributes.Builder()
                .setUsage(AudioAttributes.USAGE_MEDIA)
                .setContentType(AudioAttributes.CONTENT_TYPE_MUSIC)
                .build(),
            AudioFormat.Builder()
                .setSampleRate(sampleRate)
                .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
                .setChannelMask(channelMask)
                .build(),
            bufferSize,
            AudioTrack.MODE_STREAM,
            AudioManager.AUDIO_SESSION_ID_GENERATE
        ).also {
            Log.d(
                TAG,
                "AudioTrack created: sampleRate=$sampleRate, channels=$channelCount, " +
                        "minBuffer=$minBuffer, safeMinBuffer=$safeMinBuffer, bufferSize=$bufferSize, " +
                        "bufferMs=${(bufferSize * 1000L) / bytesPerSecond}"
            )
        }
    }

    fun startIfNeeded() {
        if (started) return
        val track = audioTrack ?: return
        track.play()
        started = true
        Log.d(TAG, "AudioTrack playback started")
    }

    fun writePcm16(payload: ByteArray): Int {
        val track = audioTrack ?: return 0
        var offset = 0
        var totalWritten = 0

        while (offset < payload.size) {
            val written = track.write(payload, offset, payload.size - offset)
            if (written <= 0) break
            offset += written
            totalWritten += written
        }

        return totalWritten
    }

    fun stop() {
        try {
            audioTrack?.pause()
        } catch (_: Exception) {
        }

        try {
            audioTrack?.flush()
        } catch (_: Exception) {
        }

        try {
            audioTrack?.stop()
        } catch (_: Exception) {
        }

        try {
            audioTrack?.release()
        } catch (_: Exception) {
        }

        audioTrack = null
        started = false
    }

    companion object {
        private const val TAG = "AudioTrackOutput"
    }
}