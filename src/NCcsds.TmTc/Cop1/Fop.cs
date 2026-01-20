using NCcsds.TmTc.Frames;

namespace NCcsds.TmTc.Cop1;

/// <summary>
/// FOP (Frame Operations Procedure) state for COP-1.
/// </summary>
public enum FopState
{
    /// <summary>
    /// FOP is active and ready to transmit.
    /// </summary>
    Active,

    /// <summary>
    /// FOP is waiting for acknowledgment.
    /// </summary>
    RetransmitWithoutWait,

    /// <summary>
    /// FOP is waiting for acknowledgment with wait.
    /// </summary>
    RetransmitWithWait,

    /// <summary>
    /// FOP is suspended.
    /// </summary>
    Suspended,

    /// <summary>
    /// FOP detected a lockout condition.
    /// </summary>
    LockoutDetected
}

/// <summary>
/// FOP-1 implementation for COP-1.
/// </summary>
public class Fop1
{
    private readonly object _lock = new();
    private readonly Queue<TcFrame> _sentQueue = new();

    /// <summary>
    /// Current FOP state.
    /// </summary>
    public FopState State { get; private set; } = FopState.Active;

    /// <summary>
    /// Virtual Channel ID this FOP is handling.
    /// </summary>
    public byte VirtualChannelId { get; }

    /// <summary>
    /// Next Frame Sequence Number V(S).
    /// </summary>
    public byte TransmitterFrameSequenceNumber { get; private set; }

    /// <summary>
    /// Expected Acknowledgement Frame Sequence Number NN(R).
    /// </summary>
    public byte ExpectedAcknowledgement { get; private set; }

    /// <summary>
    /// Transmission Limit (K).
    /// </summary>
    public int TransmissionLimit { get; set; } = 3;

    /// <summary>
    /// Timeout period in milliseconds (T1).
    /// </summary>
    public int TimeoutPeriod { get; set; } = 5000;

    /// <summary>
    /// Sliding window width (K).
    /// </summary>
    public byte WindowWidth { get; set; } = 10;

    /// <summary>
    /// Current transmission count for the frame being transmitted.
    /// </summary>
    public int TransmissionCount { get; private set; }

    /// <summary>
    /// Event raised when a frame should be transmitted.
    /// </summary>
    public event Action<TcFrame>? FrameToTransmit;

    /// <summary>
    /// Event raised when FOP state changes.
    /// </summary>
    public event Action<FopState>? StateChanged;

    /// <summary>
    /// Creates a new FOP-1 instance.
    /// </summary>
    /// <param name="virtualChannelId">Virtual channel ID.</param>
    public Fop1(byte virtualChannelId)
    {
        VirtualChannelId = virtualChannelId;
    }

    /// <summary>
    /// Transmits a frame (Type-A, sequence-controlled).
    /// </summary>
    /// <param name="frame">The TC frame to transmit.</param>
    /// <returns>True if the frame was accepted for transmission.</returns>
    public bool TransmitFrame(TcFrame frame)
    {
        lock (_lock)
        {
            if (State != FopState.Active)
                return false;

            // Check window
            int outstandingFrames = _sentQueue.Count;
            if (outstandingFrames >= WindowWidth)
                return false;

            // Assign sequence number
            frame.FrameSequenceNumber = TransmitterFrameSequenceNumber;
            frame.BypassFlag = false;

            // Add to sent queue
            _sentQueue.Enqueue(frame);

            // Increment V(S)
            TransmitterFrameSequenceNumber = (byte)((TransmitterFrameSequenceNumber + 1) & 0xFF);
            TransmissionCount = 1;

            // Trigger transmission
            FrameToTransmit?.Invoke(frame);

            return true;
        }
    }

    /// <summary>
    /// Transmits a Type-B (bypass) frame.
    /// </summary>
    /// <param name="frame">The TC frame to transmit.</param>
    public void TransmitBypassFrame(TcFrame frame)
    {
        lock (_lock)
        {
            frame.BypassFlag = true;
            FrameToTransmit?.Invoke(frame);
        }
    }

    /// <summary>
    /// Processes a received CLCW.
    /// </summary>
    /// <param name="clcw">The CLCW.</param>
    public void ProcessClcw(Clcw clcw)
    {
        lock (_lock)
        {
            if (clcw.VirtualChannelId != VirtualChannelId)
                return;

            // Check for lockout
            if (clcw.Lockout)
            {
                SetState(FopState.LockoutDetected);
                return;
            }

            // Process report value (acknowledged frames)
            byte reportValue = clcw.ReportValue;

            // Remove acknowledged frames from queue
            while (_sentQueue.Count > 0)
            {
                var frame = _sentQueue.Peek();
                if (IsAcknowledged(frame.FrameSequenceNumber, reportValue))
                {
                    _sentQueue.Dequeue();
                    ExpectedAcknowledgement = (byte)((frame.FrameSequenceNumber + 1) & 0xFF);
                }
                else
                {
                    break;
                }
            }

            // Check retransmit flag
            if (clcw.Retransmit && _sentQueue.Count > 0)
            {
                RetransmitUnacknowledgedFrames();
            }
            else if (clcw.Wait)
            {
                SetState(FopState.RetransmitWithWait);
            }
            else if (_sentQueue.Count == 0)
            {
                SetState(FopState.Active);
            }
        }
    }

    private bool IsAcknowledged(byte frameSeq, byte reportValue)
    {
        // Frame is acknowledged if its sequence number is before the report value
        int diff = (reportValue - frameSeq) & 0xFF;
        return diff > 0 && diff <= 128;
    }

    private void RetransmitUnacknowledgedFrames()
    {
        foreach (var frame in _sentQueue)
        {
            TransmissionCount++;
            if (TransmissionCount > TransmissionLimit)
            {
                // Transmission limit exceeded
                SetState(FopState.Suspended);
                return;
            }
            FrameToTransmit?.Invoke(frame);
        }
        SetState(FopState.RetransmitWithoutWait);
    }

    private void SetState(FopState newState)
    {
        if (State != newState)
        {
            State = newState;
            StateChanged?.Invoke(newState);
        }
    }

    /// <summary>
    /// Initializes the FOP with a specific V(S).
    /// </summary>
    /// <param name="vs">Initial V(S) value.</param>
    public void Initialize(byte vs = 0)
    {
        lock (_lock)
        {
            TransmitterFrameSequenceNumber = vs;
            ExpectedAcknowledgement = vs;
            _sentQueue.Clear();
            TransmissionCount = 0;
            SetState(FopState.Active);
        }
    }

    /// <summary>
    /// Resumes the FOP from a suspended state.
    /// </summary>
    public void Resume()
    {
        lock (_lock)
        {
            if (State == FopState.Suspended || State == FopState.LockoutDetected)
            {
                SetState(FopState.Active);
            }
        }
    }

    /// <summary>
    /// Gets the number of outstanding (unacknowledged) frames.
    /// </summary>
    public int OutstandingFrameCount
    {
        get
        {
            lock (_lock)
            {
                return _sentQueue.Count;
            }
        }
    }
}
