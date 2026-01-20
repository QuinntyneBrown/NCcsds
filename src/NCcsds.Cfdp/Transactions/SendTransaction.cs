using NCcsds.Cfdp.Entity;
using NCcsds.Cfdp.Pdu;

namespace NCcsds.Cfdp.Transactions;

/// <summary>
/// CFDP send (Class 1 or Class 2) transaction.
/// </summary>
public class SendTransaction : CfdpTransaction
{
    private readonly PutRequest _request;
    private readonly ulong _destinationEntityId;
    private readonly TransmissionMode _transmissionMode;
    private readonly ChecksumType _checksumType;

    private byte[]? _fileData;
    private ulong _fileSize;
    private uint _checksum;
    private ulong _bytesSent;
    private bool _eofSent;
    private bool _finishedReceived;
    private int _ackRetries;

    /// <summary>
    /// Creates a new send transaction.
    /// </summary>
    public SendTransaction(
        TransactionId id,
        PutRequest request,
        CfdpEntityConfiguration config,
        Action<byte[], ulong> sendPdu)
        : base(id, config, sendPdu)
    {
        _request = request;
        _destinationEntityId = request.DestinationEntityId;

        // Determine transmission mode
        if (request.TransmissionMode.HasValue)
            _transmissionMode = request.TransmissionMode.Value;
        else if (config.RemoteEntities.TryGetValue(_destinationEntityId, out var remote))
            _transmissionMode = remote.TransmissionMode;
        else
            _transmissionMode = config.DefaultTransmissionMode;

        // Determine checksum type
        if (request.ChecksumType.HasValue)
            _checksumType = request.ChecksumType.Value;
        else if (config.RemoteEntities.TryGetValue(_destinationEntityId, out var remote2))
            _checksumType = remote2.ChecksumType;
        else
            _checksumType = config.DefaultChecksumType;
    }

    /// <inheritdoc />
    public override void Start()
    {
        State = TransactionState.Active;

        // Read file
        var filePath = Path.Combine(Config.FilestoreRoot, _request.SourceFileName);
        _fileData = File.ReadAllBytes(filePath);
        _fileSize = (ulong)_fileData.Length;
        _checksum = CalculateChecksum(_fileData);

        // Send Metadata PDU
        SendMetadata();

        // Send File Data PDUs
        SendFileData();

        // Send EOF PDU
        SendEof();

        // For Class 1, we're done after sending EOF
        if (_transmissionMode == TransmissionMode.Unacknowledged)
        {
            CompleteTransaction(true);
        }
    }

    private void SendMetadata()
    {
        var pdu = new MetadataPdu
        {
            Header = CreateHeader(),
            ClosureRequested = _request.ClosureRequested || _transmissionMode == TransmissionMode.Acknowledged,
            ChecksumType = _checksumType,
            FileSize = _fileSize,
            SourceFileName = _request.SourceFileName,
            DestinationFileName = _request.DestinationFileName
        };

        pdu.Header.Direction = PduDirection.TowardReceiver;
        SendPdu(pdu.Encode(), _destinationEntityId);
    }

    private void SendFileData()
    {
        if (_fileData == null) return;

        var maxSegmentSize = Config.MaxFileSegmentLength;
        if (Config.RemoteEntities.TryGetValue(_destinationEntityId, out var remote))
            maxSegmentSize = Math.Min(maxSegmentSize, remote.MaxFileSegmentLength);

        ulong offset = 0;
        while (offset < _fileSize)
        {
            var remaining = _fileSize - offset;
            var segmentSize = (int)Math.Min((ulong)maxSegmentSize, remaining);

            var pdu = new FileDataPdu
            {
                Header = CreateHeader(),
                Offset = offset,
                Data = _fileData.AsSpan((int)offset, segmentSize).ToArray()
            };
            pdu.Header.Type = PduType.FileData;
            pdu.Header.Direction = PduDirection.TowardReceiver;

            SendPdu(pdu.Encode(), _destinationEntityId);

            offset += (ulong)segmentSize;
            _bytesSent = offset;
        }
    }

    private void SendEof()
    {
        var pdu = new EofPdu
        {
            Header = CreateHeader(),
            ConditionCode = ConditionCode.NoError,
            Checksum = _checksum,
            FileSize = _fileSize
        };
        pdu.Header.Direction = PduDirection.TowardReceiver;

        SendPdu(pdu.Encode(), _destinationEntityId);
        _eofSent = true;
    }

    /// <inheritdoc />
    public override void ProcessPdu(PduHeader header, ReadOnlySpan<byte> data)
    {
        if (_transmissionMode == TransmissionMode.Unacknowledged)
            return; // Class 1 ignores incoming PDUs

        if (header.Type != PduType.FileDirective)
            return;

        var directiveCode = (DirectiveCode)data[0];

        switch (directiveCode)
        {
            case DirectiveCode.Ack:
                ProcessAck(header, data);
                break;
            case DirectiveCode.Nak:
                ProcessNak(header, data);
                break;
            case DirectiveCode.Finished:
                ProcessFinished(header, data);
                break;
        }
    }

    private void ProcessAck(PduHeader header, ReadOnlySpan<byte> data)
    {
        var ack = AckPdu.Decode(header, data);
        if (ack.AcknowledgedDirective == DirectiveCode.Eof)
        {
            // EOF acknowledged, wait for Finished
        }
    }

    private void ProcessNak(PduHeader header, ReadOnlySpan<byte> data)
    {
        if (_fileData == null) return;

        var nak = NakPdu.Decode(header, data);

        // Retransmit requested segments
        foreach (var segment in nak.SegmentRequests)
        {
            var offset = segment.StartOffset;
            var length = (int)(segment.EndOffset - segment.StartOffset);

            if (offset + (ulong)length > _fileSize)
                continue;

            var pdu = new FileDataPdu
            {
                Header = CreateHeader(),
                Offset = offset,
                Data = _fileData.AsSpan((int)offset, length).ToArray()
            };
            pdu.Header.Type = PduType.FileData;
            pdu.Header.Direction = PduDirection.TowardReceiver;

            SendPdu(pdu.Encode(), _destinationEntityId);
        }
    }

    private void ProcessFinished(PduHeader header, ReadOnlySpan<byte> data)
    {
        var finished = FinishedPdu.Decode(header, data);
        _finishedReceived = true;

        // Send ACK of Finished
        var ack = new AckPdu
        {
            Header = CreateHeader(),
            AcknowledgedDirective = DirectiveCode.Finished,
            DirectiveSubtypeCode = 1,
            ConditionCode = finished.ConditionCode,
            TransactionStatus = Pdu.TransactionStatus.Terminated
        };
        ack.Header.Direction = PduDirection.TowardSender;

        SendPdu(ack.Encode(), _destinationEntityId);

        // Complete transaction
        CompleteTransaction(finished.ConditionCode == ConditionCode.NoError);
    }

    private void CompleteTransaction(bool success)
    {
        State = TransactionState.Complete;
        Result = new TransactionResult
        {
            Success = success,
            ConditionCode = success ? ConditionCode.NoError : ConditionCode.CancelRequestReceived,
            BytesTransferred = _bytesSent
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
            DestinationEntityId = _destinationEntityId
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
        // Handle remaining bytes
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
        // Standard CRC-32 implementation
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
