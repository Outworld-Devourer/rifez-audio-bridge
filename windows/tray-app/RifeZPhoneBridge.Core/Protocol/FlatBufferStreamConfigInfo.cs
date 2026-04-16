using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Protocol;

public sealed class FlatBufferStreamConfigInfo
{
    public bool Accepted { get; }
    public uint SampleRate { get; }
    public byte Channels { get; }
    public string SampleFormat { get; }
    public string Codec { get; }
    public uint FrameSamples { get; }
    public string Reason { get; }

    public FlatBufferStreamConfigInfo(
        bool accepted,
        uint sampleRate,
        byte channels,
        string sampleFormat,
        string codec,
        uint frameSamples,
        string reason)
    {
        Accepted = accepted;
        SampleRate = sampleRate;
        Channels = channels;
        SampleFormat = sampleFormat;
        Codec = codec;
        FrameSamples = frameSamples;
        Reason = reason;
    }

    public override string ToString()
    {
        return $"Accepted={Accepted} | {SampleRate} Hz | {Channels} ch | {SampleFormat} | {Codec} | FrameSamples={FrameSamples} | Reason={Reason}";
    }
}