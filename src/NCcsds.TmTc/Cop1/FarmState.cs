namespace NCcsds.TmTc.Cop1;

/// <summary>
/// FARM (Frame Acceptance and Reporting Mechanism) state for COP-1.
/// </summary>
public enum FarmState
{
    /// <summary>
    /// FARM is open and accepting frames.
    /// </summary>
    Open,

    /// <summary>
    /// FARM is waiting (suspended).
    /// </summary>
    Wait,

    /// <summary>
    /// FARM is locked out (requires directive to unlock).
    /// </summary>
    Lockout
}

/// <summary>
/// FARM-1 implementation for COP-1.
/// </summary>
public class Farm1
{
    private readonly object _lock = new();

    /// <summary>
    /// Current FARM state.
    /// </summary>
    public FarmState State { get; private set; } = FarmState.Open;

    /// <summary>
    /// Virtual Channel ID this FARM is handling.
    /// </summary>
    public byte VirtualChannelId { get; }

    /// <summary>
    /// Receiver Frame Sequence Number V(R).
    /// </summary>
    public byte ReceiverFrameSequenceNumber { get; private set; }

    /// <summary>
    /// FARM-B counter for Type-B frames.
    /// </summary>
    public byte FarmBCounter { get; private set; }

    /// <summary>
    /// Retransmit flag.
    /// </summary>
    public bool Retransmit { get; private set; }

    /// <summary>
    /// Sliding window width (W).
    /// </summary>
    public byte WindowWidth { get; }

    /// <summary>
    /// Positive window edge.
    /// </summary>
    private byte PositiveWindow => (byte)((ReceiverFrameSequenceNumber + WindowWidth) & 0xFF);

    /// <summary>
    /// Negative window edge.
    /// </summary>
    private byte NegativeWindow => (byte)((ReceiverFrameSequenceNumber - WindowWidth) & 0xFF);

    /// <summary>
    /// Creates a new FARM-1 instance.
    /// </summary>
    /// <param name="virtualChannelId">Virtual channel ID.</param>
    /// <param name="windowWidth">Sliding window width (default 10).</param>
    public Farm1(byte virtualChannelId, byte windowWidth = 10)
    {
        VirtualChannelId = virtualChannelId;
        WindowWidth = windowWidth;
    }

    /// <summary>
    /// Processes an incoming frame.
    /// </summary>
    /// <param name="sequenceNumber">Frame sequence number.</param>
    /// <param name="isTypeB">True if this is a Type-B (bypass) frame.</param>
    /// <returns>Frame acceptance result.</returns>
    public FrameAcceptanceResult ProcessFrame(byte sequenceNumber, bool isTypeB)
    {
        lock (_lock)
        {
            if (isTypeB)
            {
                // Type-B frames bypass sequence control
                FarmBCounter = (byte)((FarmBCounter + 1) & 0x03);
                return FrameAcceptanceResult.Accept;
            }

            // Type-A frame processing
            switch (State)
            {
                case FarmState.Lockout:
                    return FrameAcceptanceResult.Lockout;

                case FarmState.Wait:
                    return FrameAcceptanceResult.Wait;

                case FarmState.Open:
                    return ProcessTypeAFrame(sequenceNumber);

                default:
                    return FrameAcceptanceResult.Error;
            }
        }
    }

    private FrameAcceptanceResult ProcessTypeAFrame(byte sequenceNumber)
    {
        if (sequenceNumber == ReceiverFrameSequenceNumber)
        {
            // Expected frame
            ReceiverFrameSequenceNumber = (byte)((ReceiverFrameSequenceNumber + 1) & 0xFF);
            Retransmit = false;
            return FrameAcceptanceResult.Accept;
        }

        // Check if in positive window (ahead of expected)
        if (IsInPositiveWindow(sequenceNumber))
        {
            Retransmit = true;
            return FrameAcceptanceResult.PositiveWindow;
        }

        // Check if in negative window (behind expected, possibly retransmission)
        if (IsInNegativeWindow(sequenceNumber))
        {
            return FrameAcceptanceResult.NegativeWindow;
        }

        // Outside window - lockout
        State = FarmState.Lockout;
        return FrameAcceptanceResult.Lockout;
    }

    private bool IsInPositiveWindow(byte sequenceNumber)
    {
        int expected = ReceiverFrameSequenceNumber;
        int positive = PositiveWindow;
        int seq = sequenceNumber;

        if (positive > expected)
        {
            return seq > expected && seq <= positive;
        }
        else // Wrapped around
        {
            return seq > expected || seq <= positive;
        }
    }

    private bool IsInNegativeWindow(byte sequenceNumber)
    {
        int expected = ReceiverFrameSequenceNumber;
        int negative = NegativeWindow;
        int seq = sequenceNumber;

        if (negative < expected)
        {
            return seq >= negative && seq < expected;
        }
        else // Wrapped around
        {
            return seq >= negative || seq < expected;
        }
    }

    /// <summary>
    /// Generates a CLCW for the current state.
    /// </summary>
    public Clcw GenerateClcw(bool noRfAvailable = false, bool noBitLock = false)
    {
        lock (_lock)
        {
            return new Clcw(
                VirtualChannelId,
                ReceiverFrameSequenceNumber,
                lockout: State == FarmState.Lockout,
                wait: State == FarmState.Wait,
                retransmit: Retransmit,
                noRfAvailable: noRfAvailable,
                noBitLock: noBitLock,
                farmBCounter: FarmBCounter
            );
        }
    }

    /// <summary>
    /// Processes an Unlock directive.
    /// </summary>
    public void Unlock()
    {
        lock (_lock)
        {
            State = FarmState.Open;
            Retransmit = false;
        }
    }

    /// <summary>
    /// Processes a SetVR directive.
    /// </summary>
    /// <param name="newVr">New V(R) value.</param>
    public void SetVr(byte newVr)
    {
        lock (_lock)
        {
            ReceiverFrameSequenceNumber = newVr;
            State = FarmState.Open;
            Retransmit = false;
        }
    }

    /// <summary>
    /// Resets the FARM to initial state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            State = FarmState.Open;
            ReceiverFrameSequenceNumber = 0;
            FarmBCounter = 0;
            Retransmit = false;
        }
    }
}

/// <summary>
/// Result of frame acceptance by FARM.
/// </summary>
public enum FrameAcceptanceResult
{
    /// <summary>
    /// Frame accepted for delivery.
    /// </summary>
    Accept,

    /// <summary>
    /// Frame in positive window (ahead of expected).
    /// </summary>
    PositiveWindow,

    /// <summary>
    /// Frame in negative window (possibly retransmission).
    /// </summary>
    NegativeWindow,

    /// <summary>
    /// FARM is in lockout state.
    /// </summary>
    Lockout,

    /// <summary>
    /// FARM is in wait state.
    /// </summary>
    Wait,

    /// <summary>
    /// Error processing frame.
    /// </summary>
    Error
}
