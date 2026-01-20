using NCcsds.Cfdp.Entity;
using NCcsds.Cfdp.Pdu;

namespace NCcsds.Cfdp.Transactions;

/// <summary>
/// Base class for CFDP transactions.
/// </summary>
public abstract class CfdpTransaction : IDisposable
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public TransactionId Id { get; }

    /// <summary>
    /// Entity configuration.
    /// </summary>
    protected CfdpEntityConfiguration Config { get; }

    /// <summary>
    /// Callback for sending PDUs.
    /// </summary>
    protected Action<byte[], ulong> SendPdu { get; }

    /// <summary>
    /// Current transaction state.
    /// </summary>
    public TransactionState State { get; protected set; } = TransactionState.Initial;

    /// <summary>
    /// Transaction status for reporting.
    /// </summary>
    public TransactionStatus Status { get; protected set; } = Pdu.TransactionStatus.Active;

    /// <summary>
    /// Whether the transaction is complete.
    /// </summary>
    public bool IsComplete => State == TransactionState.Complete || State == TransactionState.Cancelled;

    /// <summary>
    /// Transaction result.
    /// </summary>
    public TransactionResult Result { get; protected set; } = new();

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    protected CfdpTransaction(TransactionId id, CfdpEntityConfiguration config, Action<byte[], ulong> sendPdu)
    {
        Id = id;
        Config = config;
        SendPdu = sendPdu;
    }

    /// <summary>
    /// Starts the transaction.
    /// </summary>
    public abstract void Start();

    /// <summary>
    /// Processes a received PDU.
    /// </summary>
    public abstract void ProcessPdu(PduHeader header, ReadOnlySpan<byte> data);

    /// <summary>
    /// Cancels the transaction.
    /// </summary>
    public virtual void Cancel()
    {
        State = TransactionState.Cancelled;
        Result = new TransactionResult
        {
            Success = false,
            ConditionCode = ConditionCode.CancelRequestReceived
        };
    }

    /// <summary>
    /// Suspends the transaction.
    /// </summary>
    public virtual void Suspend()
    {
        if (State == TransactionState.Active)
            State = TransactionState.Suspended;
    }

    /// <summary>
    /// Resumes a suspended transaction.
    /// </summary>
    public virtual void Resume()
    {
        if (State == TransactionState.Suspended)
            State = TransactionState.Active;
    }

    /// <inheritdoc />
    public virtual void Dispose() { }
}

/// <summary>
/// Transaction state.
/// </summary>
public enum TransactionState
{
    Initial,
    Active,
    Suspended,
    Complete,
    Cancelled
}

/// <summary>
/// Transaction result.
/// </summary>
public class TransactionResult
{
    /// <summary>
    /// Whether the transaction completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Condition code.
    /// </summary>
    public ConditionCode ConditionCode { get; set; } = ConditionCode.NoError;

    /// <summary>
    /// File status.
    /// </summary>
    public FileStatus FileStatus { get; set; } = FileStatus.Unreported;

    /// <summary>
    /// Bytes transferred.
    /// </summary>
    public ulong BytesTransferred { get; set; }
}
