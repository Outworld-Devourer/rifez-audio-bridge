package com.rifez.phoneaudio.protocol

import RifeZ.PhoneAudio.Control.Envelope
import RifeZ.PhoneAudio.Control.HelloRequest
import RifeZ.PhoneAudio.Control.HelloResponse
import RifeZ.PhoneAudio.Control.MessageType
import RifeZ.PhoneAudio.Control.Payload
import com.google.flatbuffers.FlatBufferBuilder
import java.nio.ByteBuffer

object FlatBufferHelloProtocol {

    fun parseHelloRequest(payload: ByteArray): String {
        val bb = ByteBuffer.wrap(payload)
        val envelope = Envelope.getRootAsEnvelope(bb)

        require(envelope.messageType() == MessageType.HelloRequest) {
            "Unexpected message type: ${envelope.messageType()}"
        }

        val helloRequest = HelloRequest()
        val payloadOk = envelope.payload(helloRequest)
        require(payloadOk != null) { "HelloRequest payload missing" }

        return helloRequest.clientName() ?: ""
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
}