using NCcsds.Core.Interfaces;
using NCcsds.TmTc.Frames;

namespace NCcsds.TmTc.Processing;

/// <summary>
/// Demultiplexes TM frames by virtual channel.
/// </summary>
public class VirtualChannelDemux : IFrameHandler<TmFrame>
{
    private readonly Dictionary<byte, VirtualChannelReceiver> _receivers = new();
    private readonly Dictionary<byte, byte> _lastFrameCounts = new();

    /// <summary>
    /// Event raised when a frame sequence gap is detected.
    /// </summary>
    public event Action<byte, byte, byte>? SequenceGapDetected;

    /// <summary>
    /// Event raised when a frame is received on a virtual channel.
    /// </summary>
    public event Action<byte, TmFrame>? FrameReceived;

    /// <summary>
    /// Registers a receiver for a virtual channel.
    /// </summary>
    public void RegisterReceiver(byte vcid, VirtualChannelReceiver receiver)
    {
        _receivers[vcid] = receiver;
    }

    /// <summary>
    /// Handles an incoming TM frame.
    /// </summary>
    public void Handle(TmFrame frame)
    {
        byte vcid = frame.VirtualChannelId.Value;

        // Check sequence continuity
        if (_lastFrameCounts.TryGetValue(vcid, out byte lastCount))
        {
            byte expected = (byte)((lastCount + 1) & 0xFF);
            if (frame.VirtualChannelFrameCount != expected)
            {
                SequenceGapDetected?.Invoke(vcid, expected, frame.VirtualChannelFrameCount);
            }
        }
        _lastFrameCounts[vcid] = frame.VirtualChannelFrameCount;

        // Raise event
        FrameReceived?.Invoke(vcid, frame);

        // Forward to specific receiver
        if (_receivers.TryGetValue(vcid, out var receiver))
        {
            receiver.ProcessFrame(frame);
        }
    }

    /// <summary>
    /// Gets statistics for a virtual channel.
    /// </summary>
    public VirtualChannelStatistics GetStatistics(byte vcid)
    {
        if (_receivers.TryGetValue(vcid, out var receiver))
        {
            return receiver.Statistics;
        }
        return new VirtualChannelStatistics();
    }
}

/// <summary>
/// Receives and processes frames for a specific virtual channel.
/// </summary>
public class VirtualChannelReceiver
{
    /// <summary>
    /// Virtual channel ID.
    /// </summary>
    public byte VirtualChannelId { get; }

    /// <summary>
    /// Statistics for this virtual channel.
    /// </summary>
    public VirtualChannelStatistics Statistics { get; } = new();

    /// <summary>
    /// Event raised when a complete packet is extracted.
    /// </summary>
    public event Action<byte[]>? PacketExtracted;

    private readonly List<byte> _packetBuffer = new();
    private bool _inPacket;

    /// <summary>
    /// Creates a new virtual channel receiver.
    /// </summary>
    public VirtualChannelReceiver(byte vcid)
    {
        VirtualChannelId = vcid;
    }

    /// <summary>
    /// Processes an incoming frame.
    /// </summary>
    public void ProcessFrame(TmFrame frame)
    {
        Statistics.FramesReceived++;

        var data = frame.DataField.AsSpan();
        int fhp = frame.FirstHeaderPointer;

        if (fhp == TmFrame.FhpIdleData)
        {
            // Idle data, discard
            _packetBuffer.Clear();
            _inPacket = false;
            return;
        }

        if (fhp == TmFrame.FhpNoPacketStart)
        {
            // Continuation only
            if (_inPacket)
            {
                _packetBuffer.AddRange(data.ToArray());
                TryExtractPackets();
            }
            return;
        }

        // There's a packet start in this frame
        if (_inPacket && fhp > 0)
        {
            // Complete previous packet with data before FHP
            _packetBuffer.AddRange(data[..fhp].ToArray());
            TryExtractPackets();
        }

        // Start new packet from FHP
        _packetBuffer.Clear();
        _inPacket = true;
        _packetBuffer.AddRange(data[fhp..].ToArray());
        TryExtractPackets();
    }

    private void TryExtractPackets()
    {
        while (_packetBuffer.Count >= 6)
        {
            // Read packet length from CCSDS primary header
            int packetDataLength = (_packetBuffer[4] << 8) | _packetBuffer[5];
            int totalPacketLength = 6 + packetDataLength + 1;

            if (_packetBuffer.Count >= totalPacketLength)
            {
                var packet = _packetBuffer.Take(totalPacketLength).ToArray();
                _packetBuffer.RemoveRange(0, totalPacketLength);

                Statistics.PacketsExtracted++;
                PacketExtracted?.Invoke(packet);
            }
            else
            {
                break; // Wait for more data
            }
        }
    }
}

/// <summary>
/// Statistics for a virtual channel.
/// </summary>
public class VirtualChannelStatistics
{
    /// <summary>
    /// Number of frames received.
    /// </summary>
    public long FramesReceived { get; set; }

    /// <summary>
    /// Number of sequence gaps detected.
    /// </summary>
    public long SequenceGaps { get; set; }

    /// <summary>
    /// Number of packets extracted.
    /// </summary>
    public long PacketsExtracted { get; set; }

    /// <summary>
    /// Number of frame errors.
    /// </summary>
    public long FrameErrors { get; set; }
}
