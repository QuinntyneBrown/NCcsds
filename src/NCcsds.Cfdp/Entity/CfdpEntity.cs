using NCcsds.Cfdp.Pdu;
using NCcsds.Cfdp.Transactions;

namespace NCcsds.Cfdp.Entity;

/// <summary>
/// CFDP entity that manages file transfers.
/// </summary>
public class CfdpEntity : IDisposable
{
    private readonly CfdpEntityConfiguration _config;
    private readonly Dictionary<TransactionId, CfdpTransaction> _transactions = new();
    private readonly object _lock = new();
    private long _sequenceCounter;
    private bool _disposed;

    /// <summary>
    /// Entity ID.
    /// </summary>
    public ulong EntityId => _config.EntityId;

    /// <summary>
    /// Event raised when a transaction completes.
    /// </summary>
    public event Action<TransactionId, TransactionResult>? TransactionCompleted;

    /// <summary>
    /// Event raised when a transaction is created.
    /// </summary>
    public event Action<TransactionId>? TransactionCreated;

    /// <summary>
    /// Event raised when a PDU needs to be sent.
    /// </summary>
    public event Action<byte[], ulong>? PduReady;

    /// <summary>
    /// Creates a new CFDP entity.
    /// </summary>
    public CfdpEntity(CfdpEntityConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Initiates a Put request to send a file.
    /// </summary>
    public TransactionId Put(PutRequest request)
    {
        var transactionId = new TransactionId(EntityId, (ulong)Interlocked.Increment(ref _sequenceCounter));

        var transaction = new SendTransaction(
            transactionId,
            request,
            _config,
            OnPduReady);

        lock (_lock)
        {
            _transactions[transactionId] = transaction;
        }

        TransactionCreated?.Invoke(transactionId);
        transaction.Start();

        return transactionId;
    }

    /// <summary>
    /// Processes a received PDU.
    /// </summary>
    public void ProcessPdu(byte[] pduData)
    {
        var header = PduHeader.Decode(pduData, out var headerLength);
        var transactionId = new TransactionId(header.SourceEntityId, header.TransactionSequenceNumber);

        CfdpTransaction? transaction;
        lock (_lock)
        {
            if (!_transactions.TryGetValue(transactionId, out transaction))
            {
                // This is a new receive transaction
                if (header.Direction == PduDirection.TowardReceiver)
                {
                    transaction = new ReceiveTransaction(
                        transactionId,
                        header,
                        _config,
                        OnPduReady);
                    _transactions[transactionId] = transaction;
                    TransactionCreated?.Invoke(transactionId);
                }
                else
                {
                    return; // Unknown transaction
                }
            }
        }

        transaction.ProcessPdu(header, pduData.AsSpan(headerLength));

        if (transaction.IsComplete)
        {
            lock (_lock)
            {
                _transactions.Remove(transactionId);
            }
            TransactionCompleted?.Invoke(transactionId, transaction.Result);
        }
    }

    /// <summary>
    /// Gets the status of a transaction.
    /// </summary>
    public TransactionStatus? GetTransactionStatus(TransactionId id)
    {
        lock (_lock)
        {
            return _transactions.TryGetValue(id, out var transaction)
                ? transaction.Status
                : null;
        }
    }

    /// <summary>
    /// Gets all active transactions.
    /// </summary>
    public IReadOnlyList<TransactionId> GetActiveTransactions()
    {
        lock (_lock)
        {
            return _transactions.Keys.ToList();
        }
    }

    /// <summary>
    /// Cancels a transaction.
    /// </summary>
    public bool CancelTransaction(TransactionId id)
    {
        lock (_lock)
        {
            if (_transactions.TryGetValue(id, out var transaction))
            {
                transaction.Cancel();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Suspends a transaction.
    /// </summary>
    public bool SuspendTransaction(TransactionId id)
    {
        lock (_lock)
        {
            if (_transactions.TryGetValue(id, out var transaction))
            {
                transaction.Suspend();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Resumes a suspended transaction.
    /// </summary>
    public bool ResumeTransaction(TransactionId id)
    {
        lock (_lock)
        {
            if (_transactions.TryGetValue(id, out var transaction))
            {
                transaction.Resume();
                return true;
            }
        }
        return false;
    }

    private void OnPduReady(byte[] pdu, ulong destinationEntityId)
    {
        PduReady?.Invoke(pdu, destinationEntityId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var transaction in _transactions.Values)
            {
                transaction.Dispose();
            }
            _transactions.Clear();
        }
    }
}

/// <summary>
/// CFDP entity configuration (MIB).
/// </summary>
public class CfdpEntityConfiguration
{
    /// <summary>
    /// Entity ID.
    /// </summary>
    public ulong EntityId { get; set; }

    /// <summary>
    /// Length of entity IDs in bytes.
    /// </summary>
    public int EntityIdLength { get; set; } = 2;

    /// <summary>
    /// Length of sequence numbers in bytes.
    /// </summary>
    public int SequenceNumberLength { get; set; } = 2;

    /// <summary>
    /// Maximum file segment size.
    /// </summary>
    public int MaxFileSegmentLength { get; set; } = 1024;

    /// <summary>
    /// Default transmission mode.
    /// </summary>
    public TransmissionMode DefaultTransmissionMode { get; set; } = TransmissionMode.Unacknowledged;

    /// <summary>
    /// Default checksum type.
    /// </summary>
    public ChecksumType DefaultChecksumType { get; set; } = ChecksumType.Modular;

    /// <summary>
    /// Inactivity timeout.
    /// </summary>
    public TimeSpan InactivityTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// ACK timeout for Class 2 transfers.
    /// </summary>
    public TimeSpan AckTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// NAK timeout for Class 2 transfers.
    /// </summary>
    public TimeSpan NakTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum ACK retries.
    /// </summary>
    public int MaxAckRetries { get; set; } = 3;

    /// <summary>
    /// Maximum NAK retries.
    /// </summary>
    public int MaxNakRetries { get; set; } = 3;

    /// <summary>
    /// Filestore root directory.
    /// </summary>
    public string FilestoreRoot { get; set; } = ".";

    /// <summary>
    /// Whether to use CRC in PDUs.
    /// </summary>
    public bool UseCrc { get; set; } = true;

    /// <summary>
    /// Remote entity configurations.
    /// </summary>
    public Dictionary<ulong, RemoteEntityConfiguration> RemoteEntities { get; set; } = new();
}

/// <summary>
/// Remote entity configuration.
/// </summary>
public class RemoteEntityConfiguration
{
    /// <summary>
    /// Remote entity ID.
    /// </summary>
    public ulong EntityId { get; set; }

    /// <summary>
    /// Maximum file segment length for this remote.
    /// </summary>
    public int MaxFileSegmentLength { get; set; } = 1024;

    /// <summary>
    /// Transmission mode for this remote.
    /// </summary>
    public TransmissionMode TransmissionMode { get; set; } = TransmissionMode.Unacknowledged;

    /// <summary>
    /// Checksum type for this remote.
    /// </summary>
    public ChecksumType ChecksumType { get; set; } = ChecksumType.Modular;
}

/// <summary>
/// CFDP Put request.
/// </summary>
public class PutRequest
{
    /// <summary>
    /// Destination entity ID.
    /// </summary>
    public ulong DestinationEntityId { get; set; }

    /// <summary>
    /// Source file path.
    /// </summary>
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>
    /// Destination file name.
    /// </summary>
    public string DestinationFileName { get; set; } = string.Empty;

    /// <summary>
    /// Transmission mode (null = use default).
    /// </summary>
    public TransmissionMode? TransmissionMode { get; set; }

    /// <summary>
    /// Whether closure (Finished PDU) is requested.
    /// </summary>
    public bool ClosureRequested { get; set; }

    /// <summary>
    /// Checksum type (null = use default).
    /// </summary>
    public ChecksumType? ChecksumType { get; set; }
}

/// <summary>
/// CFDP transaction ID.
/// </summary>
public readonly struct TransactionId : IEquatable<TransactionId>
{
    /// <summary>
    /// Source entity ID.
    /// </summary>
    public ulong SourceEntityId { get; }

    /// <summary>
    /// Transaction sequence number.
    /// </summary>
    public ulong SequenceNumber { get; }

    /// <summary>
    /// Creates a new transaction ID.
    /// </summary>
    public TransactionId(ulong sourceEntityId, ulong sequenceNumber)
    {
        SourceEntityId = sourceEntityId;
        SequenceNumber = sequenceNumber;
    }

    /// <inheritdoc />
    public bool Equals(TransactionId other) =>
        SourceEntityId == other.SourceEntityId && SequenceNumber == other.SequenceNumber;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TransactionId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(SourceEntityId, SequenceNumber);

    /// <inheritdoc />
    public override string ToString() => $"{SourceEntityId}:{SequenceNumber}";

    public static bool operator ==(TransactionId left, TransactionId right) => left.Equals(right);
    public static bool operator !=(TransactionId left, TransactionId right) => !left.Equals(right);
}
