package com.rifez.phoneaudio.protocol

import java.io.InputStream
import java.io.OutputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

object FlatBufferFrameIO {

    fun writeFrame(outputStream: OutputStream, payload: ByteArray) {
        val header = ByteBuffer.allocate(4)
            .order(ByteOrder.LITTLE_ENDIAN)
            .putInt(payload.size)
            .array()

        outputStream.write(header)
        outputStream.write(payload)
        outputStream.flush()
    }

    fun readFrame(inputStream: InputStream): ByteArray? {
        val header = ByteArray(4)
        val headerRead = readExactly(inputStream, header, 4)
        if (headerRead == 0) return null
        if (headerRead < 4) throw IllegalStateException("Incomplete FlatBuffer frame header")

        val length = ByteBuffer.wrap(header)
            .order(ByteOrder.LITTLE_ENDIAN)
            .int

        require(length > 0) { "Invalid FlatBuffer frame length: $length" }

        val payload = ByteArray(length)
        val payloadRead = readExactly(inputStream, payload, length)
        if (payloadRead < length) {
            throw IllegalStateException("Incomplete FlatBuffer frame payload")
        }

        return payload
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