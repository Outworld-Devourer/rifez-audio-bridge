using Google.FlatBuffers;
using RifeZ.PhoneAudio.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RifeZPhoneBridge.Core.Protocol;

public static class FlatBufferControlProtocol
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

    public static byte[] BuildPingRequest()
    {
        var builder = new FlatBufferBuilder(128);

        PingRequest.StartPingRequest(builder);
        var pingOffset = PingRequest.EndPingRequest(builder);

        Envelope.StartEnvelope(builder);
        Envelope.AddMessageType(builder, MessageType.PingRequest);
        Envelope.AddPayloadType(builder, Payload.PingRequest);
        Envelope.AddPayload(builder, pingOffset.Value);
        var envelopeOffset = Envelope.EndEnvelope(builder);

        builder.Finish(envelopeOffset.Value, "RFZ1");
        return builder.SizedByteArray();
    }

    public static byte[] BuildStatusRequest()
    {
        var builder = new FlatBufferBuilder(128);

        StatusRequest.StartStatusRequest(builder);
        var statusOffset = StatusRequest.EndStatusRequest(builder);

        Envelope.StartEnvelope(builder);
        Envelope.AddMessageType(builder, MessageType.StatusRequest);
        Envelope.AddPayloadType(builder, Payload.StatusRequest);
        Envelope.AddPayload(builder, statusOffset.Value);
        var envelopeOffset = Envelope.EndEnvelope(builder);

        builder.Finish(envelopeOffset.Value, "RFZ1");
        return builder.SizedByteArray();
    }

    public static byte[] BuildStartStreamRequest()
    {
        var builder = new FlatBufferBuilder(128);

        StartStreamRequest.StartStartStreamRequest(builder);
        var startStreamOffset = StartStreamRequest.EndStartStreamRequest(builder);

        Envelope.StartEnvelope(builder);
        Envelope.AddMessageType(builder, MessageType.StartStreamRequest);
        Envelope.AddPayloadType(builder, Payload.StartStreamRequest);
        Envelope.AddPayload(builder, startStreamOffset.Value);
        var envelopeOffset = Envelope.EndEnvelope(builder);

        builder.Finish(envelopeOffset.Value, "RFZ1");
        return builder.SizedByteArray();
    }

    public static byte[] BuildDisconnectRequest()
    {
        var builder = new FlatBufferBuilder(128);

        DisconnectRequest.StartDisconnectRequest(builder);
        var disconnectOffset = DisconnectRequest.EndDisconnectRequest(builder);

        Envelope.StartEnvelope(builder);
        Envelope.AddMessageType(builder, MessageType.DisconnectRequest);
        Envelope.AddPayloadType(builder, Payload.DisconnectRequest);
        Envelope.AddPayload(builder, disconnectOffset.Value);
        var envelopeOffset = Envelope.EndEnvelope(builder);

        builder.Finish(envelopeOffset.Value, "RFZ1");
        return builder.SizedByteArray();
    }

    public static byte[] BuildStreamConfigRequest(
        uint sampleRate = 48000,
        byte channels = 2,
        SampleFormat sampleFormat = SampleFormat.PCM16,
        CodecType codec = CodecType.PCM,
        uint frameSamples = 480)
    {
        var builder = new FlatBufferBuilder(256);

        StreamConfigRequest.StartStreamConfigRequest(builder);
        StreamConfigRequest.AddSampleRate(builder, sampleRate);
        StreamConfigRequest.AddChannels(builder, channels);
        StreamConfigRequest.AddSampleFormat(builder, sampleFormat);
        StreamConfigRequest.AddCodec(builder, codec);
        StreamConfigRequest.AddFrameSamples(builder, frameSamples);
        var configOffset = StreamConfigRequest.EndStreamConfigRequest(builder);

        Envelope.StartEnvelope(builder);
        Envelope.AddMessageType(builder, MessageType.StreamConfigRequest);
        Envelope.AddPayloadType(builder, Payload.StreamConfigRequest);
        Envelope.AddPayload(builder, configOffset.Value);
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

    public static bool ParsePongResponse(byte[] payload)
    {
        var bb = new ByteBuffer(payload);
        var envelope = Envelope.GetRootAsEnvelope(bb);

        if (envelope.MessageType != MessageType.PongResponse)
            throw new InvalidOperationException($"Unexpected message type: {envelope.MessageType}");

        var response = envelope.Payload<PongResponse>();
        return response.HasValue;
    }

    public static FlatBufferStatusInfo ParseStatusResponse(byte[] payload)
    {
        var bb = new ByteBuffer(payload);
        var envelope = Envelope.GetRootAsEnvelope(bb);

        if (envelope.MessageType != MessageType.StatusResponse)
            throw new InvalidOperationException($"Unexpected message type: {envelope.MessageType}");

        var response = envelope.Payload<StatusResponse>();
        if (!response.HasValue)
            throw new InvalidOperationException("StatusResponse payload missing.");

        var value = response.Value;

        return new FlatBufferStatusInfo(
            state: value.State.ToString(),
            deviceName: value.DeviceName ?? string.Empty,
            sourceName: value.SourceName ?? string.Empty
        );
    }

    public static bool ParseStartStreamResponse(byte[] payload)
    {
        var bb = new ByteBuffer(payload);
        var envelope = Envelope.GetRootAsEnvelope(bb);

        if (envelope.MessageType != MessageType.StartStreamResponse)
            throw new InvalidOperationException($"Unexpected message type: {envelope.MessageType}");

        var response = envelope.Payload<StartStreamResponse>();
        return response.HasValue;
    }

    public static bool ParseDisconnectResponse(byte[] payload)
    {
        var bb = new ByteBuffer(payload);
        var envelope = Envelope.GetRootAsEnvelope(bb);

        if (envelope.MessageType != MessageType.DisconnectResponse)
            throw new InvalidOperationException($"Unexpected message type: {envelope.MessageType}");

        var response = envelope.Payload<DisconnectResponse>();
        return response.HasValue;
    }

    public static FlatBufferStreamConfigInfo ParseStreamConfigResponse(byte[] payload)
    {
        var bb = new ByteBuffer(payload);
        var envelope = Envelope.GetRootAsEnvelope(bb);

        if (envelope.MessageType != MessageType.StreamConfigResponse)
            throw new InvalidOperationException($"Unexpected message type: {envelope.MessageType}");

        var response = envelope.Payload<StreamConfigResponse>();
        if (!response.HasValue)
            throw new InvalidOperationException("StreamConfigResponse payload missing.");

        var value = response.Value;

        return new FlatBufferStreamConfigInfo(
            accepted: value.Accepted,
            sampleRate: value.SampleRate,
            channels: value.Channels,
            sampleFormat: value.SampleFormat.ToString(),
            codec: value.Codec.ToString(),
            frameSamples: value.FrameSamples,
            reason: value.Reason ?? string.Empty
        );
    }
}