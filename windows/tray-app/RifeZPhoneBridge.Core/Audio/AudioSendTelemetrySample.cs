namespace RifeZPhoneBridge.Core.Audio;

public readonly record struct AudioSendTelemetrySample(
    long FrameIndex,
    int PayloadBytes,
    int SamplesInPayload,
    long PresentationIndex,
    double ElapsedMilliseconds
);