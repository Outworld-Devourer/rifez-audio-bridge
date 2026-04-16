
using Google.FlatBuffers;
using RifeZ.PhoneAudio.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RifeZPhoneBridge.Core.Protocol;

public static class FlatBufferHelloProtocol
{
    public static byte[] BuildHelloRequest(string clientName)
    {
        var builder = new FlatBufferBuilder(256);

        var clientNameOffset = builder.CreateString(clientName);

        HelloRequest.StartHelloRequest(builder);
        HelloRequest.AddClientName(builder, clientNameOffset);
        var helloOffset = HelloRequest.EndHelloRequest(builder);

        Envelope.StartEnvelope(builder);
        Envelope.AddMessageType(builder, MessageType.HelloRequest);
        Envelope.AddPayloadType(builder, Payload.HelloRequest);
        Envelope.AddPayload(builder, helloOffset.Value);
        var envelopeOffset = Envelope.EndEnvelope(builder);

        builder.Finish(envelopeOffset.Value, "RFZ1");
        return builder.SizedByteArray();
    }

    public static string ParseHelloResponse(byte[] payload)
    {
        var bb = new ByteBuffer(payload);
        var envelope = Envelope.GetRootAsEnvelope(bb);

        if (envelope.MessageType != MessageType.HelloResponse)
            throw new InvalidOperationException($"Unexpected message type: {envelope.MessageType}");

        var response = envelope.Payload<HelloResponse>();
        if (!response.HasValue)
            throw new InvalidOperationException("HelloResponse payload missing.");

        return response.Value.ReceiverName ?? string.Empty;
    }
}