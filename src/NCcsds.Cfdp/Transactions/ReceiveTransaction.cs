using NCcsds.Cfdp.Entity;
using NCcsds.Cfdp.Pdu;

namespace NCcsds.Cfdp.Transactions;

/// <summary>
/// CFDP receive (Class 1 or Class 2) transaction.
/// </summary>
public class ReceiveTransaction : CfdpTransaction
{
    private readonly ulong _sourceEntityId;
    private readonly TransmissionMode _transmissionMode;

    private string _sourceFileName = string.Empty;
    private string _destinationFileName = string.Empty;
    private ulong _fileSize;
    private ChecksumType _checksumType;
    private bool _closureRequested;

    private readonly SortedDictionary<ulong, byte[]> _receivedSegments = new();
    private ulong _bytesReceived;
    private bool _metadataReceived;
    private bool _eofReceived;
    private uint _expectedChecksum;
    private int _nakRetries;

    /// <summary>
    /// Creates a new receive transaction.
    /// </summary>
    public ReceiveTransaction(
        TransactionId id,
        PduHeader initialHeader,
        CfdpEntityConfiguration config,
        Action<byte[], ulong> sendPdu)
        : base(id, config, sendPdu)
    {
        _sourceEntityId = initialHeader.SourceEntityId;
        _transmissionMode = initialHeader.TransmissionMode;
    }

    /// <inheritdoc />
    public override void Start()
    {
        State = TransactionState.Active;
    }

    /// <inheritdoc />
    public override void ProcessPdu(PduHeader header, ReadOnlySpan<byte> data)
    {
        if (State != TransactionState.Active && State != TransactionState.Initial)
            return;

        if (State == TransactionState.Initial)
            State = TransactionState.Active;

        if (header.Type == PduType.FileData)
        {
            ProcessFileData(header, data);
        }
        else if (header.Type == PduType.FileDirective)
        {
            var directiveCode = (DirectiveCode)data[0];
            switch (directiveCode)
            {
                case DirectiveCode.Metadata:
                    ProcessMetadata(header, data);
                    break;
                case DirectiveCode.Eof:
                    ProcessEof(header, data);
                    break;
            }
        }
    }

    private void ProcessMetadata(PduHeader header, ReadOnlySpan<byte> data)
    {
        var metadata = MetadataPdu.Decode(header, data);

        _sourceFileName = metadata.SourceFileName;
        _destinationFileName = metadata.DestinationFileName;
        _fileSize = metadata.FileSize;
        _checksumType = metadata.ChecksumType;
        _closureRequested = metadata.ClosureRequested;
        _metadataReceived = true;
    }

    private void ProcessFileData(PduHeader header, ReadOnlySpan<byte> data)
    {
        var fileData = FileDataPdu.Decode(header, data);

        _receivedSegments[fileData.Offset] = fileData.Data;
        _bytesReceived += (ulong)fileData.Data.Length;
    }

    private void ProcessEof(PduHeader header, ReadOnlySpan<byte> data)
    {
        var eof = EofPdu.Decode(header, data);
        _eofReceived = true;
        _expectedChecksum = eof.Checksum;
        _fileSize = eof.FileSize;

        // Check for gaps
        var gaps = FindGaps();

        if (_transmissionMode == TransmissionMode.Acknowledged && gaps.Count > 0)
        {
            // Send NAK for missing data
            SendNak(gaps);
        }
        else
        {
            // Try to complete the transaction
            TryComplete();
        }
    }

    private List<SegmentRequest> FindGaps()
    {
        var gaps = new List<SegmentRequest>();
        ulong expectedOffset = 0;

        foreach (var kvp in _receivedSegments)
        {
            if (kvp.Key > expectedOffset)
            {
                gaps.Add(new SegmentRequest
                {
                    StartOffset = expectedOffset,
                    EndOffset = kvp.Key
                });
            }
            expectedOffset = kvp.Key + (ulong)kvp.Value.Length;
        }

        if (expectedOffset < _fileSize)
        {
            gaps.Add(new SegmentRequest
            {
                StartOffset = expectedOffset,
                EndOffset = _fileSize
            });
        }

        return gaps;
    }

    private void SendNak(List<SegmentRequest> gaps)
    {
        if (_nakRetries >= Config.MaxNakRetries)
        {
            CompleteTransaction(false, ConditionCode.NakLimitReached);
            return;
        }

        _nakRetries++;

        var nak = new NakPdu
        {
            Header = CreateHeader(),
            StartOfScope = 0,
            EndOfScope = _fileSize,
            SegmentRequests = gaps
        };
        nak.Header.Direction = PduDirection.TowardSender;

        SendPdu(nak.Encode(), _sourceEntityId);
    }

    private void TryComplete()
    {
        // Assemble file
        var fileData = AssembleFile();

        if (fileData == null)
        {
            if (_transmissionMode == TransmissionMode.Acknowledged)
            {
                var gaps = FindGaps();
                if (gaps.Count > 0)
                {
                    SendNak(gaps);
                    return;
                }
            }

            CompleteTransaction(false, ConditionCode.FileSizeError);
            return;
        }

        // Verify checksum
        var actualChecksum = CalculateChecksum(fileData);
        if (actualChecksum != _expectedChecksum && _checksumType != ChecksumType.Null)
        {
            CompleteTransaction(false, ConditionCode.FileChecksumFailure);
            return;
        }

        // Write file
        try
        {
            var destPath = Path.Combine(Config.FilestoreRoot, _destinationFileName);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            File.WriteAllBytes(destPath, fileData);
        }
        catch
        {
            CompleteTransaction(false, ConditionCode.FilestoreRejection);
            return;
        }

        // Send Finished PDU for Class 2 or if closure requested
        if (_transmissionMode == TransmissionMode.Acknowledged || _closureRequested)
        {
            SendFinished(ConditionCode.NoError, FileStatus.RetainedSuccessfully);
        }

        CompleteTransaction(true);
    }

    private byte[]? AssembleFile()
    {
        var fileData = new byte[_fileSize];
        ulong offset = 0;

        foreach (var kvp in _receivedSegments)
        {
            if (kvp.Key != offset)
                return null; // Gap detected

            kvp.Value.CopyTo(fileData, (int)offset);
            offset += (ulong)kvp.Value.Length;
        }

        return offset == _fileSize ? fileData : null;
    }

    private void SendFinished(ConditionCode condition, FileStatus fileStatus)
    {
        var finished = new FinishedPdu
        {
            Header = CreateHeader(),
            ConditionCode = condition,
            DeliveryCode = condition == ConditionCode.NoError,
            FileStatus = fileStatus
        };
        finished.Header.Direction = PduDirection.TowardSender;

        SendPdu(finished.Encode(), _sourceEntityId);
    }

    private void CompleteTransaction(bool success, ConditionCode? conditionCode = null)
    {
        State = TransactionState.Complete;
        Result = new TransactionResult
        {
            Success = success,
            ConditionCode = conditionCode ?? (success ? ConditionCode.NoError : ConditionCode.CancelRequestReceived),
            FileStatus = success ? FileStatus.RetainedSuccessfully : FileStatus.DiscardedDeliberately,
            BytesTransferred = _bytesReceived
        };
    }

    private PduHeader CreateHeader()
    {
        return new PduHeader
        {
            Version = 1,
            Type = PduType.FileDirective,
            TransmissionMode = _transmissionMode,
            CrcPresent = Config.UseCrc,
            LargeFileFlag = _fileSize > uint.MaxValue,
            EntityIdLength = Config.EntityIdLength,
            SequenceNumberLength = Config.SequenceNumberLength,
            SourceEntityId = Id.SourceEntityId,
            TransactionSequenceNumber = Id.SequenceNumber,
            DestinationEntityId = Config.EntityId
        };
    }

    private uint CalculateChecksum(byte[] data)
    {
        return _checksumType switch
        {
            ChecksumType.Modular => CalculateModularChecksum(data),
            ChecksumType.Crc32 => CalculateCrc32(data),
            ChecksumType.Null => 0,
            _ => 0
        };
    }

    private static uint CalculateModularChecksum(byte[] data)
    {
        uint sum = 0;
        int i = 0;
        while (i + 4 <= data.Length)
        {
            sum += (uint)((data[i] << 24) | (data[i + 1] << 16) | (data[i + 2] << 8) | data[i + 3]);
            i += 4;
        }
        if (i < data.Length)
        {
            uint remaining = 0;
            int shift = 24;
            while (i < data.Length)
            {
                remaining |= (uint)(data[i++] << shift);
                shift -= 8;
            }
            sum += remaining;
        }
        return sum;
    }

    private static uint CalculateCrc32(byte[] data)
    {
        const uint polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;

        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
            }
        }

        return ~crc;
    }
}
