package com.rifez.phoneaudio.audio

import java.io.InputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

object AudioFrameIO {

    const val FRAME_TYPE_PCM16: Byte = 1
    private const val HEADER_SIZE = 13

    data class AudioFrame(
        val frameType: Byte,
        val payloadLength: Int,
        val presentationIndex: Long,
        val payload: ByteArray
    )

    fun readFrame(inputStream: InputStream): AudioFrame? {
        val header = ByteArray(HEADER_SIZE)
        val headerRead = readExactly(inputStream, header, HEADER_SIZE)
        if (headerRead == 0) return null
        if (headerRead < HEADER_SIZE) {
            throw IllegalStateException("Incomplete audio frame header")
        }

        val bb = ByteBuffer.wrap(header).order(ByteOrder.LITTLE_ENDIAN)
        val frameType = bb.get()
        val payloadLength = bb.int
        val presentationIndex = bb.long

        require(payloadLength > 0) { "Invalid audio payload length: $payloadLength" }

        val payload = ByteArray(payloadLength)
        val payloadRead = readExactly(inputStream, payload, payloadLength)
        if (payloadRead < payloadLength) {
            throw IllegalStateException("Incomplete audio frame payload")
        }

        return AudioFrame(
            frameType = frameType,
            payloadLength = payloadLength,
            presentationIndex = presentationIndex,
            payload = payload
        )
    }

    private fun readExactly(inputStream: InputStream, buffer: ByteArray, count: Int): Int {
        var totalRead = 0
        while (totalRead < count) {
            val read = inputStream.read(buffer, totalRead, count - totalRead)
            if (read == -1) return totalRead
            totalRead += read
        }
        return totalRead
    }
}