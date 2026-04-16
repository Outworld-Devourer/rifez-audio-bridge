package com.rifez.phoneaudio.protocol

import RifeZ.PhoneAudio.Control.DisconnectRequest
import RifeZ.PhoneAudio.Control.DisconnectResponse
import RifeZ.PhoneAudio.Control.Envelope
import RifeZ.PhoneAudio.Control.HelloRequest
import RifeZ.PhoneAudio.Control.HelloResponse
import RifeZ.PhoneAudio.Control.MessageType
import RifeZ.PhoneAudio.Control.Payload
import RifeZ.PhoneAudio.Control.PingRequest
import RifeZ.PhoneAudio.Control.PongResponse
import RifeZ.PhoneAudio.Control.ReceiverState
import RifeZ.PhoneAudio.Control.StartStreamRequest
import RifeZ.PhoneAudio.Control.StartStreamResponse
import RifeZ.PhoneAudio.Control.StatusRequest
import RifeZ.PhoneAudio.Control.StatusResponse
import RifeZ.PhoneAudio.Control.StreamConfigRequest
import RifeZ.PhoneAudio.Control.StreamConfigResponse
import com.google.flatbuffers.FlatBufferBuilder
import java.nio.ByteBuffer

object FlatBufferControlProtocol {

    sealed class IncomingMessage {
        data class Hello(val clientName: String) : IncomingMessage()
        data object Ping : IncomingMessage()
        data object StatusRequestMsg : IncomingMessage()
        data object StartStream : IncomingMessage()
        data object Disconnect : IncomingMessage()
        data class StreamConfig(
            val sampleRate: Long,
            val channels: Int,
            val sampleFormat: Byte,
            val codec: Byte,
            val frameSamples: Long
        ) : IncomingMessage()
    }

    fun parseIncoming(payload: ByteArray): IncomingMessage {
        val bb = ByteBuffer.wrap(payload)
        val envelope = Envelope.getRootAsEnvelope(bb)

        return when (envelope.messageType()) {
            MessageType.HelloRequest -> {
                val helloRequest = HelloRequest()
                val ok = envelope.payload(helloRequest)
                require(ok != null) { "HelloRequest payload missing" }
                IncomingMessage.Hello(helloRequest.clientName() ?: "")
            }

            MessageType.PingRequest -> {
                val pingRequest = PingRequest()
                val ok = envelope.payload(pingRequest)
                require(ok != null) { "PingRequest payload missing" }
                IncomingMessage.Ping
            }

            MessageType.StatusRequest -> {
                val statusRequest = StatusRequest()
                val ok = envelope.payload(statusRequest)
                require(ok != null) { "StatusRequest payload missing" }
                IncomingMessage.StatusRequestMsg
            }

            MessageType.StartStreamRequest -> {
                val startStreamRequest = StartStreamRequest()
                val ok = envelope.payload(startStreamRequest)
                require(ok != null) { "StartStreamRequest payload missing" }
                IncomingMessage.StartStream
            }

            MessageType.DisconnectRequest -> {
                val disconnectRequest = DisconnectRequest()
                val ok = envelope.payload(disconnectRequest)
                require(ok != null) { "DisconnectRequest payload missing" }
                IncomingMessage.Disconnect
            }

            MessageType.StreamConfigRequest -> {
                val configRequest = StreamConfigRequest()
                val ok = envelope.payload(configRequest)
                require(ok != null) { "StreamConfigRequest payload missing" }

                IncomingMessage.StreamConfig(
                    sampleRate = configRequest.sampleRate(),
                    channels = configRequest.channels(),
                    sampleFormat = configRequest.sampleFormat(),
                    codec = configRequest.codec(),
                    frameSamples = configRequest.frameSamples()
                )
            }

            else -> {
                throw IllegalArgumentException("Unsupported FlatBuffer message type: ${envelope.messageType()}")
            }
        }
    }

    fun buildHelloResponse(receiverName: String): ByteArray {
        val builder = FlatBufferBuilder(256)

        val receiverNameOffset = builder.createString(receiverName)

        HelloResponse.startHelloResponse(builder)
        HelloResponse.addReceiverName(builder, receiverNameOffset)
        val helloResponseOffset = HelloResponse.endHelloResponse(builder)

        Envelope.startEnvelope(builder)
        Envelope.addMessageType(builder, MessageType.HelloResponse)
        Envelope.addPayloadType(builder, Payload.HelloResponse)
        Envelope.addPayload(builder, helloResponseOffset.toLong().toInt())
        val envelopeOffset = Envelope.endEnvelope(builder)

        builder.finish(envelopeOffset, "RFZ1")
        return builder.sizedByteArray()
    }

    fun buildPongResponse(): ByteArray {
        val builder = FlatBufferBuilder(128)

        PongResponse.startPongResponse(builder)
        val pongOffset = PongResponse.endPongResponse(builder)

        Envelope.startEnvelope(builder)
        Envelope.addMessageType(builder, MessageType.PongResponse)
        Envelope.addPayloadType(builder, Payload.PongResponse)
        Envelope.addPayload(builder, pongOffset.toLong().toInt())
        val envelopeOffset = Envelope.endEnvelope(builder)

        builder.finish(envelopeOffset, "RFZ1")
        return builder.sizedByteArray()
    }

    fun buildStatusResponse(
        stateName: String,
        deviceName: String,
        sourceName: String
    ): ByteArray {
        val builder = FlatBufferBuilder(256)

        val deviceNameOffset = builder.createString(deviceName)
        val sourceNameOffset = builder.createString(sourceName)

        StatusResponse.startStatusResponse(builder)
        StatusResponse.addState(builder, mapState(stateName))
        StatusResponse.addDeviceName(builder, deviceNameOffset)
        StatusResponse.addSourceName(builder, sourceNameOffset)
        val statusOffset = StatusResponse.endStatusResponse(builder)

        Envelope.startEnvelope(builder)
        Envelope.addMessageType(builder, MessageType.StatusResponse)
        Envelope.addPayloadType(builder, Payload.StatusResponse)
        Envelope.addPayload(builder, statusOffset.toLong().toInt())
        val envelopeOffset = Envelope.endEnvelope(builder)

        builder.finish(envelopeOffset, "RFZ1")
        return builder.sizedByteArray()
    }

    fun buildStartStreamResponse(): ByteArray {
        val builder = FlatBufferBuilder(128)

        StartStreamResponse.startStartStreamResponse(builder)
        val startStreamOffset = StartStreamResponse.endStartStreamResponse(builder)

        Envelope.startEnvelope(builder)
        Envelope.addMessageType(builder, MessageType.StartStreamResponse)
        Envelope.addPayloadType(builder, Payload.StartStreamResponse)
        Envelope.addPayload(builder, startStreamOffset.toLong().toInt())
        val envelopeOffset = Envelope.endEnvelope(builder)

        builder.finish(envelopeOffset, "RFZ1")
        return builder.sizedByteArray()
    }

    fun buildDisconnectResponse(): ByteArray {
        val builder = FlatBufferBuilder(128)

        DisconnectResponse.startDisconnectResponse(builder)
        val disconnectOffset = DisconnectResponse.endDisconnectResponse(builder)

        Envelope.startEnvelope(builder)
        Envelope.addMessageType(builder, MessageType.DisconnectResponse)
        Envelope.addPayloadType(builder, Payload.DisconnectResponse)
        Envelope.addPayload(builder, disconnectOffset.toLong().toInt())
        val envelopeOffset = Envelope.endEnvelope(builder)

        builder.finish(envelopeOffset, "RFZ1")
        return builder.sizedByteArray()
    }

    fun buildStreamConfigResponse(
        accepted: Boolean,
        sampleRate: Long,
        channels: Int,
        sampleFormat: Byte,
        codec: Byte,
        frameSamples: Long,
        reason: String
    ): ByteArray {
        val builder = FlatBufferBuilder(256)

        val reasonOffset = builder.createString(reason)

        StreamConfigResponse.startStreamConfigResponse(builder)
        StreamConfigResponse.addAccepted(builder, accepted)
        StreamConfigResponse.addSampleRate(builder, sampleRate)
        StreamConfigResponse.addChannels(builder, channels.toInt())
        StreamConfigResponse.addSampleFormat(builder, sampleFormat)
        StreamConfigResponse.addCodec(builder, codec)
        StreamConfigResponse.addFrameSamples(builder, frameSamples)
        StreamConfigResponse.addReason(builder, reasonOffset)
        val responseOffset = StreamConfigResponse.endStreamConfigResponse(builder)

        Envelope.startEnvelope(builder)
        Envelope.addMessageType(builder, MessageType.StreamConfigResponse)
        Envelope.addPayloadType(builder, Payload.StreamConfigResponse)
        Envelope.addPayload(builder, responseOffset.toLong().toInt())
        val envelopeOffset = Envelope.endEnvelope(builder)

        builder.finish(envelopeOffset, "RFZ1")
        return builder.sizedByteArray()
    }

    private fun mapState(stateName: String): Byte {
        return when (stateName) {
            "Idle" -> ReceiverState.Idle
            "Discoverable" -> ReceiverState.Discoverable
            "Pairing" -> ReceiverState.Pairing
            "Connected" -> ReceiverState.Connected
            "Streaming" -> ReceiverState.Streaming
            "Reconnecting" -> ReceiverState.Reconnecting
            "Error" -> ReceiverState.Error
            else -> ReceiverState.Unknown
        }
    }
}